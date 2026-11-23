using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Собирает сцену Assets/Scenes/GeneratedMap.unity из SampleScene: старый ручной мир выключается,
/// добавляется MapGenerator + MapGameBinder, включается спавнер зомби. SampleScene не меняется.
/// Меню: Tools/Map/Собрать сцену GeneratedMap. Batchmode: -executeMethod MapIntegrationSetup.Run (код 0 = проверка прошла).
public static class MapIntegrationSetup
{
    const string Source = "Assets/Scenes/SampleScene.unity";
    const string Target = "Assets/Scenes/GeneratedMap.unity";

    // Игровые объекты, которые остаются. Всё, на что они ссылаются (шаблоны, эффекты, UI), тоже остаётся.
    static readonly string[] Gameplay =
    {
        "InputManager", "EventSystem", "UIsounds", "weaponShop", "CloseBut", "gunUI", "CAmeraHolder",
        "Global Volume", "Player", "muzzleFlash", "Rainfall",
    };

    // Категория генератора -> имена объектов игры, которые встают на её места.
    static readonly (string category, string prefix)[] Bindings =
    {
        ("Магазин оружия", "magaz"),
        ("Стол апгрейда", "ApgreydMagaz"),
        ("Хилка", "Apteka"),
        ("Лут", "pushki magaz"),
    };

    [MenuItem("Tools/Map/Собрать сцену GeneratedMap")]
    static void Menu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        bool ok = Build(out string report);
        EditorUtility.DisplayDialog("GeneratedMap", (ok ? "Готово, проверка пройдена.\n\n" : "Есть проблемы:\n\n") + report, "OK");
    }

    public static void Run()
    {
        bool ok = Build(out string report);
        Debug.Log("[MapIntegrationSetup]\n" + report);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Build(out string report)
    {
        var log = new StringBuilder();
        var scene = EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();

        // --- что оставить ---
        var keep = new HashSet<GameObject>(roots.Where(r => r.activeSelf && Gameplay.Contains(r.name)));
        var binder = new List<(string category, List<GameObject> objects)>();
        foreach (var (category, prefix) in Bindings)
        {
            var objs = roots.Where(r => r.activeSelf && (r.name == prefix || r.name.StartsWith(prefix + " ("))).ToList();
            binder.Add((category, objs));
            foreach (var o in objs) keep.Add(o);
        }
        AddReferenced(keep);

        // --- выключить старый мир ---
        var disabled = new List<string>();
        foreach (var r in roots)
            if (r.activeSelf && !keep.Contains(r)) { r.SetActive(false); disabled.Add(r.name); }
        log.AppendLine($"Выключено корневых объектов старого мира: {disabled.Count}. Оставлено: {keep.Count(k => k.activeSelf)}.");

        // --- спавнер зомби ---
        var spawner = Object.FindObjectsByType<ZombieSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (spawner != null) { spawner.gameObject.SetActive(true); spawner.enabled = true; log.AppendLine($"Спавнер включён: {spawner.name}."); }
        else log.AppendLine("ВНИМАНИЕ: ZombieSpawner не найден.");

        // --- генератор ---
        var player = Object.FindObjectsByType<Player>(FindObjectsSortMode.None).Select(p => p.transform.root).FirstOrDefault();
        var go = new GameObject("MapGenerator");
        var gen = go.AddComponent<MapGenerator>();
        gen.player = player;
        gen.randomSeed = true;
        gen.generateOnStart = true;
        var bind = go.AddComponent<MapGameBinder>();
        foreach (var (category, objs) in binder)
            bind.bindings.Add(new MapGameBinder.Binding { category = category, objects = objs });
        log.AppendLine($"Игрок: {(player ? player.name : "НЕ НАЙДЕН")}. Привязки: " +
                       string.Join(", ", binder.Select(b => $"{b.category} <- {b.objects.Count}")));

        EditorSceneManager.SaveScene(scene, Target);
        AddToBuildSettings(Target);

        // --- самопроверка: генерим карту в редакторе и смотрим, что всё стоит на земле ---
        bool ok = Check(gen, bind, player, spawner, log);

        // Откатываем тестовую генерацию: в файле сцены остаётся только настройка.
        EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
        log.AppendLine("Выключенные объекты: " + string.Join(", ", disabled));
        report = log.ToString();
        File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/MapIntegration.txt"), report);
        return ok;
    }

    /// Добавляет в keep корни всех объектов сцены, на которые ссылаются компоненты keep (по цепочке).
    static void AddReferenced(HashSet<GameObject> keep)
    {
        var queue = new Queue<GameObject>(keep);
        while (queue.Count > 0)
        {
            var root = queue.Dequeue();
            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform) continue;
                var it = new SerializedObject(c).GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
                    var refGo = it.objectReferenceValue as GameObject ?? (it.objectReferenceValue as Component)?.gameObject;
                    if (refGo == null || !refGo.scene.IsValid()) continue; // ассеты не интересуют
                    var refRoot = refGo.transform.root.gameObject;
                    if (keep.Add(refRoot)) queue.Enqueue(refRoot);
                }
            }
        }
    }

    static bool Check(MapGenerator gen, MapGameBinder bind, Transform player, ZombieSpawner spawner, StringBuilder log)
    {
        bool ok = true;
        bind.Configure();
        gen.GeneratePreview();
        bind.Bind();
        var root = gen.transform.Find("Generated");
        bind.BindObjects(root);

        int ground = LayerMask.NameToLayer(bind.groundLayer);
        int mask = 1 << ground;
        bool OnGround(Vector3 bottom, float tolerance) =>
            Physics.Raycast(bottom + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, mask, QueryTriggerInteraction.Ignore)
            && Mathf.Abs(hit.point.y - bottom.y) <= tolerance;

        if (root.GetComponentsInChildren<Transform>(true).Any(t => t.gameObject.layer != ground))
        { ok = false; log.AppendLine("FAIL: не вся карта на слое Ground."); }

        Bounds pb = player != null ? MapGameBinder.WorldBounds(player.gameObject) : default;
        if (player == null || !OnGround(new Vector3(pb.center.x, pb.min.y, pb.center.z), 1.5f)) {
            ok = false;
            Physics.Raycast(new Vector3(pb.center.x, pb.max.y + 3f, pb.center.z), Vector3.down, out RaycastHit h, 100f, mask, QueryTriggerInteraction.Ignore);
            log.AppendLine($"FAIL: игрок не стоит на земле карты. pivot={player?.position} bounds={pb} groundHit={h.point} ({h.collider?.name}) " +
                           "renderers: " + string.Join("; ", player.GetComponentsInChildren<Renderer>().Select(r => $"{r.name}:{r.bounds.min.y:F1}..{r.bounds.max.y:F1}")));
        }
        else log.AppendLine($"OK: игрок на земле, {player.position}.");

        foreach (var b in bind.bindings)
            foreach (var o in b.objects)
            {
                if (!o.activeSelf) { log.AppendLine($"WARN: {o.name} выключен — не хватило места."); continue; }
                var bounds = MapGameBinder.WorldBounds(o);
                if (!OnGround(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), 1f))
                { ok = false; log.AppendLine($"FAIL: {o.name} ({b.category}) не на земле."); }
            }
        log.AppendLine($"Объекты игры проверены: {bind.bindings.Sum(b => b.objects.Count)}.");

        var points = root.GetComponentsInChildren<ZombieSpawnPoint>();
        int grounded = points.Count(p => OnGround(p.transform.position, 0.5f));
        if (points.Length == 0 || grounded < points.Length) ok = false;
        log.AppendLine($"Точки спавна зомби: {points.Length}, на земле {grounded}.");
        if (spawner != null && spawner.minSpawnRadius > 0)
            log.AppendLine($"Точек дальше minSpawnRadius ({spawner.minSpawnRadius} м) от игрока: " +
                           points.Count(p => Vector3.Distance(p.transform.position, player.position) >= spawner.minSpawnRadius));

        // Снимки для глаз: карта сверху и вид из-за спины игрока (Logs/map_top.png, Logs/map_player.png).
        var half = gen.mapSize * 0.5f;
        Shot(new Vector3(0f, 300f, 0f), Quaternion.Euler(90f, 0f, 0f), Mathf.Max(half.x, half.y), "map_top.png");
        if (player != null)
        {
            Vector3 eye = player.position - player.forward * 9f + Vector3.up * 5f;
            Shot(eye, Quaternion.LookRotation(player.position + Vector3.up - eye), 0f, "map_player.png");
        }

        log.AppendLine(ok ? "ПРОВЕРКА: PASS" : "ПРОВЕРКА: FAIL");
        return ok;
    }

    static void Shot(Vector3 pos, Quaternion rot, float orthoSize, string file)
    {
        var go = new GameObject("ShotCamera");
        var cam = go.AddComponent<Camera>();
        go.transform.SetPositionAndRotation(pos, rot);
        cam.orthographic = orthoSize > 0f;
        cam.orthographicSize = orthoSize;
        cam.farClipPlane = 1000f;
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(Application.dataPath, "../Logs/" + file), tex.EncodeToPNG());
        Object.DestroyImmediate(go);
    }

    static void AddToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
