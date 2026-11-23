using System;
using System.Collections.Generic;
using UnityEngine;

/// Связка генератора карты с игрой:
/// 1) переводит всю сгенерированную карту на слой земли (по нему игрок и спавнер ищут пол);
/// 2) ставит настоящие объекты игры (магазины, аптеки, столы апгрейда) на места, которые генератор выбрал
///    для своих категорий-заглушек, а заглушки удаляет. Объекты не пересоздаются — ссылки менеджеров на них целы.
[RequireComponent(typeof(MapGenerator))]
public class MapGameBinder : MonoBehaviour
{
    [Serializable]
    public class Binding
    {
        [Tooltip("Имя категории в MapGenerator (например «Хилка»)")]
        public string category;
        [Tooltip("Объекты сцены, которые встанут на места этой категории")]
        public List<GameObject> objects = new List<GameObject>();
    }

    [Tooltip("Слой для всей сгенерированной карты: по нему игрок (Player.groundLayer) и спавнер ищут землю")]
    public string groundLayer = "Ground";
    public List<Binding> bindings = new List<Binding>();

    MapGenerator gen;

    void Awake()
    {
        gen = GetComponent<MapGenerator>();
        Configure();
        gen.Generated += OnGenerated; // Awake идёт раньше MapGenerator.Start, где строится карта
    }

    void OnDestroy() { if (gen != null) gen.Generated -= OnGenerated; }

    void OnGenerated() => Bind();

    /// Подгоняет категории генератора под объекты игры: столько же мест и такого же размера.
    public void Configure()
    {
        if (gen == null) gen = GetComponent<MapGenerator>();
        foreach (var b in bindings)
        {
            var c = gen.categories.Find(x => x.name == b.category);
            if (c == null) { Debug.LogWarning($"[MapGameBinder] Нет категории «{b.category}» в MapGenerator.", this); continue; }
            b.objects.RemoveAll(o => o == null || !o.activeSelf);
            if (b.objects.Count == 0) { c.enabled = false; continue; }

            Bounds bounds = WorldBounds(b.objects[0]);
            float side = Mathf.Max(0.5f, Mathf.Max(bounds.size.x, bounds.size.z)); // с запасом на поворот
            c.enabled = true;
            c.prefab = null;
            c.look = MapGenerator.Look.Primitive;
            c.primitive = PrimitiveType.Cube;
            c.size = new Vector3(side, Mathf.Max(0.5f, bounds.size.y), side);
            c.scaleRange = Vector2.one;
            c.minCount = c.maxCount = b.objects.Count;
            c.spawnChance = 1f;
            c.mustBeReachable = true;
            c.clearRadius = Mathf.Max(c.clearRadius, side * 0.75f + 1f);
        }
    }

    /// Переводит карту на слой земли и переставляет объекты игры на места заглушек.
    public void Bind()
    {
        var root = transform.Find("Generated");
        if (root == null) return;

        int layer = LayerMask.NameToLayer(groundLayer);
        if (layer >= 0) foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        else Debug.LogWarning($"[MapGameBinder] Слоя «{groundLayer}» нет в проекте — карта осталась на своих слоях.", this);

        // В редакторе (превью) объекты сцены не двигаем, чтобы не портить сохранённую сцену.
        if (!Application.isPlaying) return;
        BindObjects(root);
    }

    /// Отдельно от Bind, чтобы самопроверка в редакторе могла вызвать перестановку явно.
    public void BindObjects(Transform root)
    {
        foreach (var b in bindings)
        {
            var group = root.Find(b.category);
            var spots = new List<Transform>();
            if (group != null) foreach (Transform t in group) spots.Add(t);

            for (int i = 0; i < b.objects.Count; i++)
            {
                var obj = b.objects[i];
                if (obj == null) continue;
                if (i >= spots.Count)
                {
                    // Генератору не хватило места: на старой позиции объект висел бы в воздухе или в скале.
                    obj.SetActive(false);
                    Debug.LogWarning($"[MapGameBinder] {obj.name}: для категории «{b.category}» не нашлось места, объект выключен.", obj);
                    continue;
                }
                MoveOnto(obj.transform, spots[i]);
                Kill(spots[i].gameObject);
            }
        }
        Physics.SyncTransforms();
    }

    static void MoveOnto(Transform obj, Transform spot)
    {
        Bounds target = WorldBounds(spot.gameObject);
        obj.Rotate(0f, spot.eulerAngles.y - obj.eulerAngles.y, 0f, Space.World);
        Bounds b = WorldBounds(obj.gameObject);
        obj.position += new Vector3(target.center.x - b.center.x, target.min.y - b.min.y, target.center.z - b.center.z);
    }

    /// Габариты по видимым мешам. Частицы, UI и выключенные рендереры не считаются: у магазинов есть
    /// невидимые кубы-триггеры (зона взаимодействия) крупнее модели — по ним магазин висел бы в воздухе.
    /// Без мешей — точка в позиции объекта.
    public static Bounds WorldBounds(GameObject go)
    {
        bool any = false;
        var result = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (!any) { result = r.bounds; any = true; }
            else result.Encapsulate(r.bounds);
        }
        return result;
    }

    static void Kill(GameObject go)
    {
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }
}
