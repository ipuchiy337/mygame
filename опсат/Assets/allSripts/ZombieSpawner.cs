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
    [Tooltip("Сколько мест проверить для одного зомби, прежде чем сдаться")]
    [Min(1)] public int spawnPositionAttempts = 6;

    [Header("Поиск земли")]
    public LayerMask groundLayer = ~0;
    [Min(1f)] public float rayStartHeight = 10f;
    [Min(1f)] public float rayDistance = 30f;

    [Header("Проверка места (не в объектах)")]
    [Tooltip("С чем зомби не должен пересекаться при появлении: стены, камни, деревья, другие зомби")]
    public LayerMask blockerLayers = ~0;
    [Min(0.1f)] public float zombieRadius = 0.45f;
    [Min(0.5f)] public float zombieHeight = 1.8f;
    [Tooltip("Насколько точка может быть выше/ниже игрока — отсекает крыши и ямы")]
    [Min(0.5f)] public float maxHeightDiff = 2.5f;
    [Tooltip("Максимальный уклон земли в месте появления, градусы")]
    [Range(0f, 89f)] public float maxGroundSlope = 40f;

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

    [Tooltip("Сколько зомби создавать за кадр — пачка растягивается на несколько кадров без фриза")]
    [Min(1)] public int spawnsPerFrame = 2;

    [Header("Текущее состояние")]
    public int currentWave;
    public int zombiesThisWave;
    public int zombiesSpawned;
    public bool isResting;
    public bool isHordeWave;

    public System.Action<int, bool> OnWaveStart;
    public System.Action OnRestStart;
    public System.Action OnRestEnd;

    // Золотой угол: каждый следующий зомби выходит с другой стороны, направления равномерно покрывают круг.
    const float GoldenAngle = 2.39996323f;

    private readonly List<GameObject> aliveZombies =
        new List<GameObject>(128);

    // Точки спавна от генератора карты — запасной вариант, если на кольце вокруг игрока нет свободного места.
    private ZombieSpawnPoint[] spawnPoints = System.Array.Empty<ZombieSpawnPoint>();

    private GameObject[] cachedPrefabs = System.Array.Empty<GameObject>();
    private float[] cumulativeWeights = System.Array.Empty<float>();
    private float totalWeight;
    private float nextAngle;

    void Start()
    {
        RebuildPrefabCache();
        nextAngle = Random.value * Mathf.PI * 2f;
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
        spawnsPerFrame = Mathf.Max(1, spawnsPerFrame);

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

            // Карта могла сгенерироваться позже спавнера или перегенерироваться — обновляем точки раз в волну.
            RefreshSpawnPoints();

            OnWaveStart?.Invoke(currentWave, isHordeWave);

            float waveEndTime = Time.time + waveDuration;

            // Спавним пачки до окончания волны
            while (Time.time < waveEndTime &&
                   zombiesSpawned < zombiesThisWave)
            {
                if (player == null)
                {
                    yield return new WaitForSeconds(checkLimitInterval);
                    continue;
                }

                int freeSlots = maxAliveZombies - AliveCount();

                // Ожидаем, пока освободится место
                if (freeSlots <= 0)
                {
                    yield return new WaitForSeconds(checkLimitInterval);
                    continue;
                }

                // Размер пачки увеличивается с каждой волной
                int packBonus =
                    (currentWave - 1) * packSizeAddedEveryWave;

                int packSize = Random.Range(minPackSize + packBonus, maxPackSize + packBonus + 1);

                // Орда увеличивает размер пачки
                if (isHordeWave)
                {
                    packSize = Mathf.CeilToInt(
                        packSize * hordeMultiplier);
                }

                // Не превышаем оставшееся количество волны и лимит живых
                packSize = Mathf.Min(packSize, zombiesThisWave - zombiesSpawned);
                packSize = Mathf.Min(packSize, freeSlots);

                // Пачка выходит одновременно, но с разных сторон; Instantiate размазан по кадрам.
                for (int i = 0; i < packSize; i++)
                {
                    if (player == null)
                        break;

                    if (SpawnZombie())
                        zombiesSpawned++;

                    if ((i + 1) % spawnsPerFrame == 0)
                        yield return null;
                }

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
        if (cachedPrefabs.Length == 0)
            return false;

        nextAngle += GoldenAngle + Random.Range(-0.35f, 0.35f);

        Vector3 center = player.position;

        if (!TryGetSpawnPosition(center, nextAngle, out Vector3 spawnPosition))
            return false;

        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
            return false;

        Vector3 toPlayer = center - spawnPosition;
        toPlayer.y = 0f;
        Quaternion rotation = toPlayer.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(toPlayer)
            : Quaternion.identity;

        GameObject zombie = Instantiate(prefab, spawnPosition, rotation);

        // Шаблон зомби на сцене выключен, а Instantiate копирует это состояние — включаем клон.
        zombie.SetActive(true);
        aliveZombies.Add(zombie);
        return true;
    }

    // Место на кольце minSpawnRadius..maxSpawnRadius вокруг center в направлении angle (радианы).
    // Не нашли свободное — берём точку генератора карты, ближайшую к этому направлению.
    public bool TryGetSpawnPosition(Vector3 center, float angle, out Vector3 position)
    {
        for (int i = 0; i < spawnPositionAttempts; i++)
        {
            float a = angle + (i == 0 ? 0f : Random.Range(-0.6f, 0.6f));
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);

            Vector3 candidate = center + new Vector3(
                Mathf.Cos(a) * radius,
                0f,
                Mathf.Sin(a) * radius
            );

            if (IsFreeSpot(candidate, center.y, out position))
                return true;
        }

        return TryGetSpawnPoint(center, angle, out position);
    }

    // Земля под точкой: пологая, на уровне игрока (не крыша, не яма), и капсула зомби ни во что не упирается.
    bool IsFreeSpot(Vector3 point, float referenceY, out Vector3 groundPoint)
    {
        groundPoint = default;

        Vector3 origin = new Vector3(point.x, referenceY + rayStartHeight, point.z);

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                rayDistance, groundLayer, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.normal.y < Mathf.Cos(maxGroundSlope * Mathf.Deg2Rad))
            return false;

        if (Mathf.Abs(hit.point.y - referenceY) > maxHeightDiff)
            return false;

        // Капсула приподнята над землёй, чтобы не задевать сам пол (вся карта на слое Ground вместе с домами).
        float bottomY = zombieRadius + 0.25f;
        float topY = Mathf.Max(bottomY, zombieHeight - zombieRadius);

        if (Physics.CheckCapsule(
                hit.point + Vector3.up * bottomY,
                hit.point + Vector3.up * topY,
                zombieRadius, blockerLayers, QueryTriggerInteraction.Ignore))
            return false;

        groundPoint = hit.point;
        return true;
    }

    bool TryGetSpawnPoint(Vector3 center, float angle, out Vector3 position)
    {
        position = default;

        Vector3 wanted = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        ZombieSpawnPoint best = null;
        float bestDot = -2f;
        float maxDistance = maxSpawnRadius * 2f; // дальние точки не берём — иначе зомби идут через всю карту

        foreach (ZombieSpawnPoint point in spawnPoints)
        {
            if (point == null)
            {
                RefreshSpawnPoints(); // карту перегенерировали — со следующего зомби будут новые точки
                return false;
            }

            if (!point.isActiveAndEnabled)
                continue;

            Vector3 offset = point.transform.position - center;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (distance < minSpawnRadius || distance > maxDistance)
                continue;

            float dot = Vector3.Dot(offset / distance, wanted);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = point;
            }
        }

        if (best == null)
            return false;

        Vector2 jitter = Random.insideUnitCircle * 1.5f;
        Vector3 p = best.transform.position + new Vector3(jitter.x, 0f, jitter.y);
        return IsFreeSpot(p, center.y, out position);
    }

    void RefreshSpawnPoints()
    {
        spawnPoints = FindObjectsByType<ZombieSpawnPoint>();
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
        CleanupDestroyedZombies();
        return aliveZombies.Count;
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
