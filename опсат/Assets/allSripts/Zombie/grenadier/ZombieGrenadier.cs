using UnityEngine;
using System.Collections;

// Зомби-гранатомётчик. Работает вместе с ZombieFollow:
// ZombieFollow останавливает зомби на stopDistance, а этот скрипт бросает гранаты.
[RequireComponent(typeof(ZombieFollow))]
public class ZombieGrenadier : MonoBehaviour
{
    [Header("Ссылки")]
    public Grenade grenadePrefab;
    [Tooltip("Точка, откуда вылетает граната (рука). Если пусто - берётся позиция зомби + смещение.")]
    public Transform throwPoint;
    public Vector3 fallbackThrowOffset = new Vector3(0f, 1.5f, 0.4f);
    public Animator anim;
    public string throwTrigger = "Throw";

    [Header("Дистанция броска")]
    public float minThrowDistance = 6f;
    public float maxThrowDistance = 18f;

    [Header("Параметры броска")]
    [Tooltip("Угол вылета в градусах. Скорость подбирается автоматически, чтобы граната долетела.")]
    [Range(15f, 70f)] public float throwAngle = 40f;
    [Tooltip("Целимся с упреждением по скорости игрока.")]
    public bool leadTarget = true;
    [Tooltip("Разброс точки приземления (метры).")]
    public float scatter = 1f;
    public float cooldown = 3.5f;
    [Tooltip("Задержка между началом анимации и вылетом гранаты.")]
    public float throwDelay = 0.4f;
    [Tooltip("Сколько стоим после броска.")]
    public float recoverTime = 0.4f;

    [Header("Граната")]
    public float fuseTime = 4f;

    private ZombieFollow follow;
    private Rigidbody playerRb;
    private float nextThrowTime;
    private bool throwing;

    private void Awake()
    {
        follow = GetComponent<ZombieFollow>();
        nextThrowTime = Time.time + Random.Range(1f, cooldown);
    }

    private void Start()
    {
        if (follow.player != null)
            playerRb = follow.player.GetComponentInParent<Rigidbody>();
    }

    private void Update()
    {
        if (throwing || follow.player == null || grenadePrefab == null) return;
        if (Time.time < nextThrowTime || !follow.CanAct) return;

        Vector3 flat = follow.player.position - transform.position;
        flat.y = 0f;
        float dist = flat.magnitude;
        if (dist < minThrowDistance || dist > maxThrowDistance) return;

        StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        throwing = true;
        follow.castLocked = true; // стоим и смотрим на игрока

        if (anim != null && !string.IsNullOrEmpty(throwTrigger))
            anim.SetTrigger(throwTrigger);

        yield return new WaitForSeconds(throwDelay);

        if (follow.player != null && follow.CanAct)
            ThrowGrenade();

        yield return new WaitForSeconds(recoverTime);

        follow.castLocked = false;
        nextThrowTime = Time.time + cooldown * Random.Range(0.9f, 1.1f);
        throwing = false;
    }

    private void ThrowGrenade()
    {
        Vector3 origin = throwPoint != null
            ? throwPoint.position
            : transform.TransformPoint(fallbackThrowOffset);

        Vector3 target = follow.player.position;

        // Упреждение: граната летит ~1-2 сек, учитываем движение игрока.
        if (leadTarget && playerRb != null)
        {
            Vector3 v = playerRb.linearVelocity;
            v.y = 0f;
            float flightGuess = Vector3.Distance(origin, target) / 12f;
            target += v * flightGuess;
        }

        Vector2 rnd = Random.insideUnitCircle * scatter;
        target += new Vector3(rnd.x, 0f, rnd.y);

        Vector3 velocity = CalcThrowVelocity(origin, target, throwAngle);

        Grenade g = Instantiate(grenadePrefab, origin, Quaternion.identity);
        g.Launch(velocity, fuseTime);

        // Чтобы граната не сталкивалась с самим зомби при вылете.
        Collider gc = g.GetComponent<Collider>();
        foreach (Collider c in GetComponentsInChildren<Collider>())
            if (gc != null) Physics.IgnoreCollision(gc, c);
    }

    // Баллистика: начальная скорость под заданным углом, чтобы попасть в точку target.
    private static Vector3 CalcThrowVelocity(Vector3 from, Vector3 to, float angleDeg)
    {
        Vector3 diff = to - from;
        Vector3 flat = new Vector3(diff.x, 0f, diff.z);
        float d = flat.magnitude;
        float h = diff.y;
        float g = Mathf.Abs(Physics.gravity.y);
        float a = angleDeg * Mathf.Deg2Rad;

        float denom = 2f * Mathf.Cos(a) * Mathf.Cos(a) * (d * Mathf.Tan(a) - h);
        float v2 = denom > 0.0001f ? g * d * d / denom : 0f;

        // Если угол не подходит (цель слишком высоко) - бросаем наугад по дуге.
        if (v2 <= 0f)
            return flat.normalized * 10f + Vector3.up * 8f;

        float v = Mathf.Sqrt(v2);
        Vector3 dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : transform_forward_fallback();
        return dir * (v * Mathf.Cos(a)) + Vector3.up * (v * Mathf.Sin(a));
    }

    private static Vector3 transform_forward_fallback() => Vector3.forward;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minThrowDistance);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, maxThrowDistance);
    }
}