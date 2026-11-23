using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Оптимизация модели зомби (вызывается из ZombieFollow.Start):
// 1) модель состоит из ~28 отдельных SkinnedMeshRenderer -> склеиваем в один
//    (в 28 раз меньше скиннинга и draw call'ов на каждого зомби);
// 2) LODGroup: вблизи полная модель, дальше без теней и 2 кости на вершину, совсем далеко не рисуем;
// 3) Animator не считает кости, пока зомби вне камеры.
public static class ZombieLod
{
    // Доля высоты экрана, ниже которой включается LOD1 / зомби перестаёт рисоваться.
    public static float Lod1ScreenHeight = 0.08f;
    public static float CullScreenHeight = 0.01f;

    struct Combined { public Mesh mesh; public Material[] materials; }

    // Склеенный меш один на всех зомби с одинаковой моделью (ключ - первый исходный меш).
    static readonly Dictionary<Mesh, Combined> cache = new Dictionary<Mesh, Combined>();

    public static void Apply(GameObject root)
    {
        if (root.GetComponentInChildren<LODGroup>(true) != null) return;

        foreach (var anim in root.GetComponentsInChildren<Animator>())
        {
            if (!anim.isActiveAndEnabled) continue;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            // В импорте модели включён Optimize Game Objects: без костей-трансформов меши не склеить.
            if (!anim.hasTransformHierarchy) AnimatorUtility.DeoptimizeTransformHierarchy(anim.gameObject);
        }

        var parts = new List<SkinnedMeshRenderer>();
        foreach (var s in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            if (s.enabled && s.sharedMesh != null && s.bones.Length > 0 && s.bones.Length == s.sharedMesh.bindposeCount)
                parts.Add(s);
        if (parts.Count == 0) return;

        SkinnedMeshRenderer main = parts.Count == 1 ? parts[0] : Combine(parts);
        AddLodGroup(root, main);
    }

    static SkinnedMeshRenderer Combine(List<SkinnedMeshRenderer> parts)
    {
        var first = parts[0];
        var host = new GameObject("ZombieCombinedSkin");
        host.layer = first.gameObject.layer;
        host.transform.SetParent(first.transform.parent, false);
        Matrix4x4 toHost = host.transform.worldToLocalMatrix;

        // Общий список костей. shortcut: одна кость в разных частях считается с одинаковым bindpose
        // (так экспортируют Mixamo/Blender); если у модели иначе - части съедут, тогда дедуп по (кость, bindpose).
        var bones = new List<Transform>();
        var binds = new List<Matrix4x4>();
        var index = new Dictionary<Transform, int>();
        var remaps = new int[parts.Count][];
        var toHostOf = new Matrix4x4[parts.Count];
        for (int i = 0; i < parts.Count; i++)
        {
            toHostOf[i] = toHost * parts[i].transform.localToWorldMatrix;
            Matrix4x4 inv = toHostOf[i].inverse;
            Transform[] sb = parts[i].bones;
            Matrix4x4[] bp = parts[i].sharedMesh.bindposes;
            remaps[i] = new int[sb.Length];
            for (int b = 0; b < sb.Length; b++)
            {
                if (!index.TryGetValue(sb[b], out int j))
                {
                    j = bones.Count;
                    bones.Add(sb[b]);
                    binds.Add(bp[b] * inv);
                    index[sb[b]] = j;
                }
                remaps[i][b] = j;
            }
        }

        if (!cache.TryGetValue(first.sharedMesh, out Combined c))
        {
            c = BuildMesh(parts, toHostOf, remaps, binds);
            cache[first.sharedMesh] = c;
        }

        Transform rootBone = first.rootBone != null ? first.rootBone : first.transform;
        Bounds bounds = LocalBounds(parts, rootBone);

        var smr = host.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = c.mesh;
        smr.sharedMaterials = c.materials;
        smr.bones = bones.ToArray();
        smr.rootBone = rootBone;
        smr.localBounds = bounds;
        smr.updateWhenOffscreen = false;
        smr.shadowCastingMode = first.shadowCastingMode;
        smr.receiveShadows = first.receiveShadows;

        foreach (var p in parts) p.enabled = false;
        return smr;
    }

    static Combined BuildMesh(List<SkinnedMeshRenderer> parts, Matrix4x4[] toHostOf, int[][] remaps, List<Matrix4x4> binds)
    {
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var tangents = new List<Vector4>();
        var uvs = new List<Vector2>();
        var weights = new List<BoneWeight>();
        var mats = new List<Material>();
        var tris = new List<List<int>>();
        bool anyTangents = false;

        for (int i = 0; i < parts.Count; i++)
        {
            Mesh m = parts[i].sharedMesh;
            Matrix4x4 M = toHostOf[i];
            int[] r = remaps[i];
            int baseV = verts.Count;
            Vector3[] v = m.vertices; Vector3[] n = m.normals; Vector4[] t = m.tangents;
            Vector2[] uv = m.uv; BoneWeight[] bw = m.boneWeights;
            anyTangents |= t.Length > 0;

            for (int k = 0; k < v.Length; k++)
            {
                verts.Add(M.MultiplyPoint3x4(v[k]));
                normals.Add(k < n.Length ? M.MultiplyVector(n[k]).normalized : Vector3.up);
                if (k < t.Length)
                {
                    Vector3 tt = M.MultiplyVector(t[k]).normalized;
                    tangents.Add(new Vector4(tt.x, tt.y, tt.z, t[k].w));
                }
                else tangents.Add(new Vector4(1f, 0f, 0f, 1f));
                uvs.Add(k < uv.Length ? uv[k] : Vector2.zero);

                BoneWeight w = k < bw.Length ? bw[k] : new BoneWeight { weight0 = 1f };
                w.boneIndex0 = r[w.boneIndex0]; w.boneIndex1 = r[w.boneIndex1];
                w.boneIndex2 = r[w.boneIndex2]; w.boneIndex3 = r[w.boneIndex3];
                weights.Add(w);
            }

            Material[] sm = parts[i].sharedMaterials;
            for (int s = 0; s < m.subMeshCount; s++)
            {
                Material mat = sm.Length > 0 ? sm[Mathf.Min(s, sm.Length - 1)] : null;
                int mi = mats.IndexOf(mat);
                if (mi < 0) { mi = mats.Count; mats.Add(mat); tris.Add(new List<int>()); }
                foreach (int idx in m.GetTriangles(s)) tris[mi].Add(baseV + idx);
            }
        }

        var mesh = new Mesh { name = "ZombieCombined" };
        mesh.indexFormat = verts.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        if (anyTangents) mesh.SetTangents(tangents);
        mesh.SetUVs(0, uvs);
        mesh.boneWeights = weights.ToArray();
        mesh.bindposes = binds.ToArray();
        mesh.subMeshCount = tris.Count;
        for (int s = 0; s < tris.Count; s++) mesh.SetTriangles(tris[s], s);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        return new Combined { mesh = mesh, materials = mats.ToArray() };
    }

    // Объединённые мировые bounds частей -> в локальные координаты rootBone (так их ждёт SkinnedMeshRenderer).
    static Bounds LocalBounds(List<SkinnedMeshRenderer> parts, Transform rootBone)
    {
        Bounds world = parts[0].bounds;
        for (int i = 1; i < parts.Count; i++) world.Encapsulate(parts[i].bounds);

        Vector3 c = world.center, e = world.extents;
        var local = new Bounds(rootBone.InverseTransformPoint(c), Vector3.zero);
        for (int k = 0; k < 8; k++)
        {
            var corner = c + new Vector3((k & 1) == 0 ? -e.x : e.x, (k & 2) == 0 ? -e.y : e.y, (k & 4) == 0 ? -e.z : e.z);
            local.Encapsulate(rootBone.InverseTransformPoint(corner));
        }
        local.Expand(0.5f); // запас на анимацию (руки при атаке), иначе зомби может мигать на краю экрана
        return local;
    }

    static void AddLodGroup(GameObject root, SkinnedMeshRenderer main)
    {
        // LOD1: тот же меш и кости, но без теней и 2 кости на вершину.
        var farGo = new GameObject("ZombieLod1");
        farGo.layer = main.gameObject.layer;
        farGo.transform.SetParent(main.transform.parent, false);
        farGo.transform.localPosition = main.transform.localPosition;
        farGo.transform.localRotation = main.transform.localRotation;
        farGo.transform.localScale = main.transform.localScale;

        var far = farGo.AddComponent<SkinnedMeshRenderer>();
        far.sharedMesh = main.sharedMesh;
        far.sharedMaterials = main.sharedMaterials;
        far.bones = main.bones;
        far.rootBone = main.rootBone;
        far.localBounds = main.localBounds;
        far.updateWhenOffscreen = false;
        far.quality = SkinQuality.Bone2;
        far.shadowCastingMode = ShadowCastingMode.Off;
        far.receiveShadows = main.receiveShadows;

        var group = root.AddComponent<LODGroup>();
        group.SetLODs(new[]
        {
            new LOD(Lod1ScreenHeight, new Renderer[] { main }),
            new LOD(CullScreenHeight, new Renderer[] { far }),
        });
        group.RecalculateBounds();
    }
}
