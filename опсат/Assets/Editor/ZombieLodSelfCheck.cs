using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Самопроверка склейки ZombieLod: скиннинг склеенного меша в случайной позе должен совпасть с исходными частями.
// Запуск: Unity.exe -batchmode -projectPath <проект> -executeMethod ZombieLodSelfCheck.Run  (код выхода 0 = PASS)
public static class ZombieLodSelfCheck
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/newModels/modela/playerTOtest/T-Pose (1).fbx");
        var go = Object.Instantiate(prefab);
        AnimatorUtility.DeoptimizeTransformHierarchy(go);
        var parts = new List<SkinnedMeshRenderer>(go.GetComponentsInChildren<SkinnedMeshRenderer>()); // тот же порядок, что в ZombieLod

        ZombieLod.Apply(go);

        SkinnedMeshRenderer combined = null;
        foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (s.name == "ZombieCombinedSkin") combined = s;
        bool ok = combined != null && go.GetComponent<LODGroup>() != null && parts.TrueForAll(p => !p.enabled);

        if (ok)
        {
            Random.InitState(1);
            foreach (var b in combined.bones)
                b.localRotation *= Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(-30f, 30f), Random.Range(-30f, 30f));

            var expected = new List<Vector3>();
            foreach (var p in parts) Skin(p.sharedMesh, p.bones, expected);
            var got = new List<Vector3>();
            Skin(combined.sharedMesh, combined.bones, got);

            float maxErr = 0f;
            for (int i = 0; i < got.Count; i++) maxErr = Mathf.Max(maxErr, Vector3.Distance(got[i], expected[i]));
            ok = got.Count == expected.Count && maxErr < 0.001f;
            Debug.Log($"[ZombieLodSelfCheck] parts={parts.Count} bones={combined.bones.Length} verts={got.Count} maxErr={maxErr}");
        }

        Debug.Log("[ZombieLodSelfCheck] " + (ok ? "PASS" : "FAIL"));
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static void Skin(Mesh m, Transform[] bones, List<Vector3> outWorld)
    {
        var v = m.vertices; var w = m.boneWeights; var bp = m.bindposes;
        for (int i = 0; i < v.Length; i++)
        {
            var bw = w[i];
            outWorld.Add(
                bw.weight0 * (bones[bw.boneIndex0].localToWorldMatrix * bp[bw.boneIndex0]).MultiplyPoint3x4(v[i]) +
                bw.weight1 * (bones[bw.boneIndex1].localToWorldMatrix * bp[bw.boneIndex1]).MultiplyPoint3x4(v[i]) +
                bw.weight2 * (bones[bw.boneIndex2].localToWorldMatrix * bp[bw.boneIndex2]).MultiplyPoint3x4(v[i]) +
                bw.weight3 * (bones[bw.boneIndex3].localToWorldMatrix * bp[bw.boneIndex3]).MultiplyPoint3x4(v[i]));
        }
    }
}
