using UnityEditor;
using UnityEngine;

// Самопроверка спавнера: зомби не появляются в объектах/на крыше и выходят со всех сторон.
// Меню Tools/Zombies/Проверка спавнера или batchmode: -executeMethod ZombieSpawnerSelfCheck.Run
public static class ZombieSpawnerSelfCheck
{
    [MenuItem("Tools/Zombies/Проверка спавнера")]
    public static void Run()
    {
        var root = new GameObject("__SpawnerCheck");
        try
        {
            Box(root, "ground", new Vector3(0, -0.5f, 0), new Vector3(200, 1, 200));
            Box(root, "wall", new Vector3(12, 1.5f, 0), new Vector3(6, 3, 30));      // стена справа на кольце
            Box(root, "roof", new Vector3(0, 5f, -13), new Vector3(30, 0.3f, 10));   // крыша сзади
            Physics.SyncTransforms();

            var s = new GameObject("spawner").AddComponent<ZombieSpawner>();
            s.transform.SetParent(root.transform);
            s.minSpawnRadius = 8f; s.maxSpawnRadius = 18f; s.spawnPositionAttempts = 6;

            int ok = 0, bad = 0;
            var quadrants = new int[4];
            float angle = 0f;
            for (int i = 0; i < 400; i++)
            {
                angle += 2.39996323f;
                if (!s.TryGetSpawnPosition(Vector3.zero, angle, out Vector3 p)) continue;
                ok++;
                bool inWall = p.x > 9f - s.zombieRadius && p.x < 15f + s.zombieRadius && Mathf.Abs(p.z) < 15f + s.zombieRadius;
                float d = new Vector2(p.x, p.z).magnitude;
                if (inWall || p.y > 0.5f || d < s.minSpawnRadius - 0.01f || d > s.maxSpawnRadius + 0.01f) bad++;
                quadrants[(p.x >= 0 ? 0 : 1) + (p.z >= 0 ? 0 : 2)]++;
            }

            bool pass = bad == 0 && ok > 300;
            foreach (int q in quadrants) pass &= q > ok / 8; // все стороны, а не одна
            Debug.Log($"ZombieSpawnerSelfCheck {(pass ? "PASS" : "FAIL")}: ok={ok} bad={bad} quadrants={string.Join(",", quadrants)}");
            if (Application.isBatchMode) EditorApplication.Exit(pass ? 0 : 1);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void Box(GameObject root, string name, Vector3 pos, Vector3 size)
    {
        var go = new GameObject(name, typeof(BoxCollider));
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.GetComponent<BoxCollider>().size = size;
    }
}
