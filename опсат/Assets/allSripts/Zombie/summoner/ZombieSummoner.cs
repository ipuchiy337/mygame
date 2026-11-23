using UnityEngine;
using System.Collections;

// Способность "призыватель": зомби стоит на дистанции (stopDistance в ZombieFollow),
// замахивается и пускает в сторону игрока волну стенок, которые вылезают из-под земли.
// Вешается на того же зомби, что и ZombieFollow.
[RequireComponent(typeof(ZombieFollow))]
public class ZombieSummoner : MonoBehaviour
{
    [Header("Префаб")]
    public RisingWall wallPrefab;

    [Header("Когда кастовать")]
    public float minCastRange = 2f;
    public float maxCastRange = 14f;
    [Tooltip("Если игрок выше/ниже на столько метров - не кастуем.")]
    public float maxHeightDiff = 3f;
    public float castCooldown = 5f;
    [Tooltip("Пауза до следующей попытки, если каст прервали (урон, отбрасывание и т.д.).")]
    public float interruptedCooldown = 1f;

    [Header("Каст")]
    [Tooltip("Время замаха. Зомби стоит и смотрит на игрока. Если его в это время оглушили - каст срывается.")]
    public float castWindup = 0.7f;
    [Tooltip("Пауза после запуска волны, пока зомби снова не двинется.")]
    public float castRecover = 0.4f;
    [Tooltip("Имя триггера в Animator для анимации каста. Пусто = без анимации.")]
    public string castTrigger = "";
    public float damage = 15f;

    [Header("Форма стенки")]
    [Min(1)] public int columns = 3;
    public float columnSpacing = 1.1f;
    public float rowSpacing = 1.2f;
    [Tooltip("Задержка между рядами. 0 = вся стенка вылезает разом.")]
    public float rowDelay = 0.1f;
    [Min(1)] public int maxRows = 8;
    [Tooltip("Расстояние от зомби до первого ряда.")]
    public float startOffset = 1.5f;
    [Tooltip("На сколько волна уходит дальше игрока.")]
    public float overshoot = 1.5f;

    [Header("Поиск земли")]
    public float groundProbeUp = 3f;
    public float groundProbeDown = 8f;
    [Range(0f, 80f)] public float maxGroundAngle = 50f;

    [Header("Цель")]
    [Tooltip("Слой игрока. Если 0 - берётся автоматически слой из ZombieFollow.player.")]
    public LayerMask targetMask;

    private ZombieFollow zombie;
    private Coroutine castRoutine;
    private float nextCastTime;

    private void Awake()
    {
        zombie = GetComponent<ZombieFollow>();
    }

    private void Start()
    {
        // Разносим первый каст, чтобы вся орда не кастовала синхронно.
        nextCastTime = Time.time + Random.Range(0.5f, Mathf.Max(0.5f, castCooldown));
    }

    private void OnDisable()
    {
        if (zombie != null) zombie.castLocked = false;
        castRoutine = null;
    }

    private void Update()
    {
        if (castRoutine != null || Time.time < nextCastTime) return;

        Transform player = zombie.player;
        if (player == null || wallPrefab == null || !zombie.CanAct) return;

        Vector3 delta = player.position - transform.position;
        if (Mathf.Abs(delta.y) > maxHeightDiff) return;

        delta.y = 0f;
        float sqr = delta.sqrMagnitude;
        if (sqr > maxCastRange * maxCastRange || sqr < minCastRange * minCastRange) return;

        castRoutine = StartCoroutine(CastRoutine());
    }

    private IEnumerator CastRoutine()
    {
        zombie.castLocked = true;

        bool hasTrigger = zombie.anim != null && !string.IsNullOrEmpty(castTrigger);
        if (hasTrigger) zombie.anim.SetTrigger(castTrigger);

        // Замах. Любое оглушение/отбрасывание срывает каст.
        for (float t = 0f; t < castWindup; t += Time.deltaTime)
        {
            if (zombie.player == null || !zombie.CanAct)
            {
                if (hasTrigger) zombie.anim.ResetTrigger(castTrigger);
                Finish(interruptedCooldown);
                yield break;
            }
            yield return null;
        }

        if (zombie.player == null)
        {
            Finish(interruptedCooldown);
            yield break;
        }

        // Направление фиксируется в конце замаха: игрок может увернуться, шагнув вбок.
        Vector3 origin = transform.position;
        Vector3 dir = zombie.player.position - origin;
        dir.y = 0f;
        float dist = dir.magnitude;
        dir = dist > 0.01f ? dir / dist : transform.forward;

        Vector3 right = Vector3.Cross(Vector3.up, dir);
        Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
        int rows = Mathf.Clamp(
            Mathf.CeilToInt((dist + overshoot - startOffset) / Mathf.Max(0.1f, rowSpacing)) + 1,
            1, maxRows);

        if (targetMask.value == 0)
            targetMask = 1 << zombie.player.gameObject.layer;

        for (int r = 0; r < rows; r++)
        {
            float along = startOffset + r * rowSpacing;

            for (int c = 0; c < columns; c++)
            {
                float lateral = (c - (columns - 1) * 0.5f) * columnSpacing;
                Vector3 probe = origin + dir * along + right * lateral;

                if (!TryGetGround(probe, out Vector3 ground)) continue;

                RisingWall piece = Instantiate(wallPrefab, ground, rotation);
                piece.Init(ground, damage, targetMask);
            }

            if (rowDelay > 0f && r < rows - 1)
                yield return new WaitForSeconds(rowDelay);
        }

        yield return new WaitForSeconds(castRecover);
        Finish(castCooldown);
    }

    private void Finish(float cooldown)
    {
        zombie.castLocked = false;
        nextCastTime = Time.time + cooldown * Random.Range(0.9f, 1.1f);
        castRoutine = null;
    }

    private bool TryGetGround(Vector3 probe, out Vector3 ground)
    {
        ground = default;
        float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        Vector3 start = probe + Vector3.up * groundProbeUp;

        // Используем те же слои окружения, что и сам зомби (без игрока и зомби).
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, groundProbeUp + groundProbeDown,
                zombie.environmentMask, QueryTriggerInteraction.Ignore) && hit.normal.y >= minNormalY)
        {
            ground = hit.point;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxCastRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minCastRange);
    }
}
