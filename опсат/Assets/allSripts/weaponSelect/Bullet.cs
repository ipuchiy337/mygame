using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 25f;
    public float lifeTime = 5f;

    [Header("Effects")]
    public GameObject hitEffect;
    public GameObject tracerPrefab;

    [Header("Ricochet Settings")]
    public int maxRicochets = 0;
    [Range(0f, 1f)]
    public float ricochetChance = 0f;
    public LayerMask enemyMask;
    public float ricochetSearchRadius = 15f;
    public float bulletSpeed = 120f;

    private Transform tracer;
    private int currentRicochets = 0;
    private float currentDamage;
    private Rigidbody rb;
    private bool hasHit = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentDamage = damage;
        Destroy(gameObject, lifeTime);

        // Трассер создаётся один раз здесь (убран из GunWithTracer)
        if (tracerPrefab != null)
        {
            GameObject t = Instantiate(tracerPrefab, transform.position, Quaternion.identity);
            tracer = t.transform;
        }
    }

    private void Update()
    {
        // ФИКС: трассер просто следует за пулей напрямую
        // Раньше было Lerp с коэффициентом > 1 (100 * deltaTime), что ломало интерполяцию
        if (tracer != null)
        {
            tracer.position = transform.position;
            if (rb != null && rb.linearVelocity != Vector3.zero)
                tracer.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        // Эффект попадания
        if (hitEffect != null)
        {
            Instantiate(
                hitEffect,
                collision.contacts[0].point,
                Quaternion.LookRotation(collision.contacts[0].normal)
            );
        }

        // Урон
        if (collision.collider.CompareTag("Zombie"))
        {
            Health hp = collision.collider.GetComponentInParent<Health>();
            if (hp != null)
            {
                hp.TakeDamage(currentDamage);
                Debug.Log($"Hit for {currentDamage:F1} damage");
            }
        }

        // Проверка рикошета
        if (currentRicochets < maxRicochets && Random.value <= ricochetChance)
        {
            Transform nearestEnemy = FindNearestEnemy(collision.contacts[0].point);

            if (nearestEnemy != null)
                PerformSmartRicochet(nearestEnemy);
            else
                PerformPhysicalRicochet(collision);
        }
        else
        {
            if (tracer != null)
                Destroy(tracer.gameObject);
            Destroy(gameObject);
        }
    }

    void PerformSmartRicochet(Transform target)
    {
        currentRicochets++;
        currentDamage /= 2f;

        Vector3 directionToEnemy = (target.position - transform.position).normalized;
        rb.linearVelocity = directionToEnemy * bulletSpeed;
        transform.forward = directionToEnemy;

        // ФИКС: сбрасываем hasHit через FixedUpdate чтобы не задетектить тот же коллайдер повторно
        StartCoroutine(ResetHit());

        Debug.Log($"Smart Ricochet {currentRicochets}/{maxRicochets} -> {target.name} | Damage: {currentDamage:F1}");
    }

    void PerformPhysicalRicochet(Collision collision)
    {
        currentRicochets++;
        currentDamage /= 2f;

        Vector3 normal = collision.contacts[0].normal;
        Vector3 reflectedVelocity = Vector3.Reflect(rb.linearVelocity, normal);

        rb.linearVelocity = reflectedVelocity;
        transform.forward = reflectedVelocity.normalized;

        // ФИКС: то же самое — задержка через FixedUpdate
        StartCoroutine(ResetHit());

        Debug.Log($"Physical Ricochet {currentRicochets}/{maxRicochets} | Damage: {currentDamage:F1}");
    }

    // ФИКС: ждём один физический кадр прежде чем снова принимать коллизии
    // Без этого пуля детектила тот же объект дважды сразу после рикошета
    IEnumerator ResetHit()
    {
        yield return new WaitForFixedUpdate();
        hasHit = false;
    }

    Transform FindNearestEnemy(Vector3 fromPosition)
    {
        Collider[] hits = Physics.OverlapSphere(fromPosition, ricochetSearchRadius, enemyMask);

        float minDist = Mathf.Infinity;
        Transform nearest = null;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Zombie")) continue;

            Health hp = hit.GetComponentInParent<Health>();
            if (hp != null && hp.hp > 0)
            {
                float dist = Vector3.Distance(fromPosition, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = hit.transform;
                }
            }
        }

        return nearest;
    }

    public void SetRicochetStats(int ricochets, float chance, LayerMask mask, float radius, float speed)
    {
        maxRicochets = ricochets;
        ricochetChance = Mathf.Clamp01(chance);
        enemyMask = mask;
        ricochetSearchRadius = radius;
        bulletSpeed = speed;
    }

    private void OnDestroy()
    {
        if (tracer != null)
            Destroy(tracer.gameObject);
    }
}