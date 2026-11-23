using UnityEngine;
using System.Collections;

public class ZombieExponentialSpawner : MonoBehaviour
{
    [Header("Ссылки")]
    public GameObject zombiePrefab;
    public Transform player;

    [Header("Настройки дистанции")]
    public float minRadius = 15f;
    public float maxRadius = 30f; // Увеличил радиус для больших толп
    public LayerMask obstacleLayer;

    [Header("Баланс (Экспонента)")]
    public float startZombiesPerMinute = 50f;
    public float multiplierPerMinute = 1.3f;   // Множитель 30%
    public int swarmMultiplier = 3;
    public int maxZombiesOnMap = 500;         // Предохранитель для FPS

    private float currentBaseCount;
    public int currentMinute = 0;

    void Start()
    {
        currentBaseCount = startZombiesPerMinute;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(SpawnFlowRoutine());
    }

    IEnumerator SpawnFlowRoutine()
    {
        while (true)
        {
            // 1. Считаем количество для текущей минуты
            float countForThisMinute = currentBaseCount;

            // 2. Проверка на Рой (каждая 3-я минута)
            bool isSwarm = (currentMinute + 1) % 3 == 0;
            if (isSwarm)
            {
                countForThisMinute *= swarmMultiplier;
                Debug.Log($"<color=red>РОЙ! Цель: {Mathf.RoundToInt(countForThisMinute)} зомби</color>");
            }

            // 3. Расчет паузы
            float delayBetweenSpawns = 60f / countForThisMinute;
            int spawnedThisMinute = 0;

            // 4. Цикл спавна на 60 секунд
            float timer = 0;
            while (timer < 60f)
            {
                // Проверяем лимит зомби на сцене перед спавном
                if (GameObject.FindGameObjectsWithTag("Zombie").Length < maxZombiesOnMap)
                {
                    SpawnZombie();
                    spawnedThisMinute++;
                }

                yield return new WaitForSeconds(delayBetweenSpawns);
                timer += delayBetweenSpawns;
            }

            // 5. Увеличиваем базу на множитель для СЛЕДУЮЩЕЙ минуты
            currentBaseCount *= multiplierPerMinute;
            currentMinute++;

            Debug.Log($"Минута {currentMinute} окончена. База выросла до {currentBaseCount}");
        }
    }

    void SpawnZombie()
    {
        Vector3 spawnPos = GetRandomSpawnPoint();

        if (spawnPos != Vector3.zero)
        {
            Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
            // Debug.Log("Зомби успешно создан!");
        }
        else
        {
            Debug.LogWarning("Не удалось найти пустое место для спавна! Проверь LayerMask или радиус.");
        }
    }

    Vector3 GetRandomSpawnPoint()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minRadius, maxRadius);
            Vector3 randomPos = new Vector3(player.position.x + randomCircle.x, player.position.y, player.position.z + randomCircle.y);

            if (!Physics.CheckSphere(randomPos, 1f, obstacleLayer))
            {
                return randomPos;
            }
        }
        return Vector3.zero;
    }
}