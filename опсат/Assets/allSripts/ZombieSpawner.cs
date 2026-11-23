using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ZombieEntry
    {
        public GameObject prefab;

        [Min(0f)]
        public float spawnChance = 50f;
    }

    [Header("Игрок")]
    public Transform player;

    [Header("Виды зомби")]
    public ZombieEntry[] zombieEntries;

    [Header("Расстояние спавна от игрока")]
    [Min(1f)] public float minSpawnRadius = 8f;
    [Min(1f)] public float maxSpawnRadius = 18f;
    [Min(1)] public int spawnPositionAttempts = 6;

    [Header("Поиск земли")]
    public LayerMask groundLayer = ~0;
    [Min(1f)] public float rayStartHeight = 10f;
    [Min(1f)] public float rayDistance = 30f;

    [Header("Настройки волн")]
    [Min(1)] public int baseZombiesPerWave = 12;
    [Min(0)] public int zombiesAddedPerWave = 5;
    [Min(1)] public int maxZombiesPerWave = 100;
    [Min(1f)] public float waveDuration = 60f;

    [Header("Отдых между волнами")]
    [Min(0f)] public float restDuration = 20f;
    [Min(1)] public int restEveryWaves = 5;

    [Header("Размер пачек")]
    [Min(1)] public int minPackSize = 5;
    [Min(1)] public int maxPackSize = 5;

    [Tooltip("На сколько увеличивать размер пачки с каждой новой волной")]
    [Min(0)] public int packSizeAddedEveryWave = 0;

    [Header("Интервал между пачками")]
    [Min(0.1f)] public float minSecondsBetweenPacks = 3f;
    [Min(0.1f)] public float maxSecondsBetweenPacks = 6f;

    [Header("Орда")]
    public bool enableHordeWaves = true;

    [Tooltip("Во сколько раз увеличить количество зомби в волне орды")]
    [Min(1f)] public float hordeZombieMultiplier = 1.5f;

    [Tooltip("Во сколько раз быстрее появляются пачки во время орды")]
    [Min(0.1f)] public float hordeSpawnSpeedMultiplier = 1.5f;

    [Header("Производительность")]
    [Min(1)] public int maxAliveZombies = 60;

    [Min(0.05f)]
    public float checkLimitInterval = 0.25f;

    [Header("Текущее состояние")]
    public int currentWave;
    public int zombiesThisWave;
    public int zombiesSpawned;
    public bool isResting;
    public bool isHordeWave;

    public System.Action<int, bool> OnWaveStart;
    public System.Action OnRestStart;
    public System.Action OnRestEnd;

    private readonly List<GameObject> aliveZombies =
        new List<GameObject>(128);

    // Точки спавна от генератора карты (MapGenerator). Нет точек — спавн по кольцу вокруг игрока, как раньше.
    private ZombieSpawnPoint[] spawnPoints = System.Array.Empty<ZombieSpawnPoint>();

    private GameObject[] cachedPrefabs = System.Array.Empty<GameObject>();
    private float[] cumulativeWeights = System.Array.Empty<float>();
    private float totalWeight;

    void Start()
    {
        RebuildPrefabCache();
        StartCoroutine(WaveLoop());
    }

    void OnValidate()
    {
        minSpawnRadius = Mathf.Max(1f, minSpawnRadius);
        maxSpawnRadius = Mathf.Max(minSpawnRadius, maxSpawnRadius);

        spawnPositionAttempts = Mathf.Max(1, spawnPositionAttempts);

        baseZombiesPerWave = Mathf.Max(1, baseZombiesPerWave);
        maxZombiesPerWave = Mathf.Max(1, maxZombiesPerWave);
        waveDuration = Mathf.Max(1f, waveDuration);
        restEveryWaves = Mathf.Max(1, restEveryWaves);

        minPackSize = Mathf.Max(1, minPackSize);
        maxPackSize = Mathf.Max(minPackSize, maxPackSize);

        minSecondsBetweenPacks =
            Mathf.Max(0.1f, minSecondsBetweenPacks);

        maxSecondsBetweenPacks =
            Mathf.Max(minSecondsBetweenPacks, maxSecondsBetweenPacks);

        maxAliveZombies = Mathf.Max(1, maxAliveZombies);
        checkLimitInterval = Mathf.Max(0.05f, checkLimitInterval);

        hordeZombieMultiplier = Mathf.Max(1f, hordeZombieMultiplier);
        hordeSpawnSpeedMultiplier =
            Mathf.Max(0.1f, hordeSpawnSpeedMultiplier);
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            currentWave++;

            // Отдых после каждой пятой волны
            if (currentWave > 1 &&
                (currentWave - 1) % restEveryWaves == 0)
            {
                isResting = true;
                OnRestStart?.Invoke();

                yield return new WaitForSeconds(restDuration);

                isResting = false;
                OnRestEnd?.Invoke();
            }

            // Каждая пятая волна — орда
            isHordeWave = enableHordeWaves &&
                          currentWave % restEveryWaves == 0;

            float hordeMultiplier =
                isHordeWave ? hordeZombieMultiplier : 1f;

            // Рассчитываем количество зомби в волне
            long baseCount = (long)baseZombiesPerWave +
                             (long)(currentWave - 1) * zombiesAddedPerWave;

            zombiesThisWave = Mathf.Clamp(
                Mathf.RoundToInt(
                    Mathf.Min((float)baseCount * hordeMultiplier,
                    maxZombiesPerWave)),
                1,
                maxZombiesPerWave
            );

            zombiesSpawned = 0;

            OnWaveStart?.Invoke(currentWave, isHordeWave);

            float waveEndTime = Time.time + waveDuration;

            // Спавним пачки до окончания волны
            while (Time.time < waveEndTime &&
                   zombiesSpawned < zombiesThisWave)
            {
                CleanupDestroyedZombies();

                if (player == null)
                {
                    yield return new WaitForSeconds(checkLimitInterval);
                    continue;
                }

                int aliveCount = AliveCount();
                int freeSlots = maxAliveZombies - aliveCount;

                // Ожидаем, пока освободится место
                if (freeSlots <= 0)
                {
                    yield return new WaitForSeconds(checkLimitInterval);
                    continue;
                }

                // Размер пачки увеличивается с каждой волной
                int packBonus =
                    (currentWave - 1) * packSizeAddedEveryWave;

                int packMin = minPackSize + packBonus;
                int packMax = maxPackSize + packBonus;

                int packSize = Random.Range(packMin, packMax + 1);

                // Орда увеличивает размер пачки
                if (isHordeWave)
                {
                    packSize = Mathf.CeilToInt(
                        packSize * hordeMultiplier);
                }

                // Не превышаем оставшееся количество волны
                int remainingWave = zombiesThisWave - zombiesSpawned;

                packSize = Mathf.Min(packSize, remainingWave);
                packSize = Mathf.Min(packSize, freeSlots);

                // ВАЖНО: создаём всю пачку подряд,
                // без ожидания между отдельными зомби.
                int spawnedInPack = 0;
                // Карта может сгенерироваться позже спавнера или перегенерироваться — обновляем точки на каждую пачку.
                spawnPoints = FindObjectsByType<ZombieSpawnPoint>(FindObjectsSortMode.None);

                for (int i = 0; i < packSize; i++)
                {
                    if (SpawnZombie())
                    {
                        spawnedInPack++;
                    }
                }

                zombiesSpawned += spawnedInPack;

                // Пауза только ПОСЛЕ всей пачки
                float waitTime = Random.Range(
                    minSecondsBetweenPacks,
                    maxSecondsBetweenPacks
                );

                if (isHordeWave)
                {
                    waitTime /= hordeSpawnSpeedMultiplier;
                }

                yield return new WaitForSeconds(waitTime);
            }

            CleanupDestroyedZombies();
        }
    }

    bool SpawnZombie()
    {
        if (player == null || cachedPrefabs.Length == 0)
            return false;

        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
            return false;

        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
            return false;

        GameObject zombie = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );

        // Шаблон зомби на сцене выключен, а Instantiate копирует это состояние — включаем клон.
        zombie.SetActive(true);
        aliveZombies.Add(zombie);
        return true;
    }

    bool TryGetSpawnPosition(out Vector3 position)
    {
        if (TryGetSpawnPoint(out position))
            return true;

        Vector3 playerPosition = player.position;

        for (int i = 0; i < spawnPositionAttempts; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;

            float radius = Random.Range(
                minSpawnRadius,
                maxSpawnRadius
            );

            Vector3 origin = playerPosition + new Vector3(
                Mathf.Cos(angle) * radius,
                rayStartHeight,
                Mathf.Sin(angle) * radius
            );

            if (Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
                return true;
            }
        }

        position = default;
        return false;
    }

    // Случайная точка спавна не ближе minSpawnRadius к игроку. Ближние точки и гнёзда выпадают чаще,
    // чтобы зомби доходили до игрока, а не шли через всю карту.
    bool TryGetSpawnPoint(out Vector3 position)
    {
        position = default;
        ZombieSpawnPoint best = null;
        float bestScore = -1f;
        Vector3 playerPosition = player.position;

        foreach (ZombieSpawnPoint point in spawnPoints)
        {
            if (point == null || !point.isActiveAndEnabled)
                continue;

            float distance = Vector3.Distance(point.transform.position, playerPosition);
            if (distance < minSpawnRadius)
                continue;

            float score = Random.value * (point.isNest ? 2f : 1f) / (1f + distance / maxSpawnRadius);
            if (score > bestScore)
            {
                bestScore = score;
                best = point;
            }
        }

        if (best == null)
            return false;

        Vector2 jitter = Random.insideUnitCircle * 1.5f; // пачка не появляется в одной точке
        position = best.transform.position + new Vector3(jitter.x, 0.2f, jitter.y);
        return true;
    }

    void RebuildPrefabCache()
    {
        List<GameObject> prefabs = new List<GameObject>();
        List<float> weights = new List<float>();

        totalWeight = 0f;

        if (zombieEntries != null)
        {
            foreach (ZombieEntry entry in zombieEntries)
            {
                if (entry == null ||
                    entry.prefab == null ||
                    entry.spawnChance <= 0f)
                {
                    continue;
                }

                totalWeight += entry.spawnChance;

                prefabs.Add(entry.prefab);
                weights.Add(totalWeight);
            }
        }

        cachedPrefabs = prefabs.ToArray();
        cumulativeWeights = weights.ToArray();
    }

    GameObject GetRandomPrefab()
    {
        if (cachedPrefabs.Length == 0 || totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);

        for (int i = 0; i < cumulativeWeights.Length; i++)
        {
            if (roll <= cumulativeWeights[i])
                return cachedPrefabs[i];
        }

        return cachedPrefabs[cachedPrefabs.Length - 1];
    }

    void CleanupDestroyedZombies()
    {
        aliveZombies.RemoveAll(zombie => zombie == null);
    }

    int AliveCount()
    {
        int count = 0;

        for (int i = 0; i < aliveZombies.Count; i++)
        {
            if (aliveZombies[i] != null)
                count++;
        }

        return count;
    }

    public int GetWave() => currentWave;

    public bool IsResting() => isResting;

    public bool IsSwarmWave() => isHordeWave;

    public int AliveZombieCount() => AliveCount();

    void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, minSpawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, maxSpawnRadius);
    }
}