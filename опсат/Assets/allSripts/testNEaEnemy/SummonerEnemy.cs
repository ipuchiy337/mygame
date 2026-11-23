using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SummonerEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float detectionRange = 15f;
    public float attackRange = 6f;
    public float moveSpeed = 3f;

    [Header("Attack")]
    public GameObject jawProjectilePrefab;
    public float attackCooldown = 3f;
    public int wallCount = 5;
    public float wallSpread = 0.6f;
    public int waveCount = 4;
    public float timeBetweenWaves = 0.25f;
    public float spawnDepth = 0.8f;

    [Header("Knockback")]
    public float knockbackDuration = 0.3f;

    [Header("—Ò˚ÎÍË")]
    public Animator anim;
    public Health zhp;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Transform player;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private bool isInKnockback = false;

    enum State { Idle, Chase, Attack }
    State state = State.Idle;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange - 1f;
        player = GameObject.FindWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        SetAnim("idle", true);
        SetAnim("run", false);
    }

    void Update()
    {
        if (isAttacking || isInKnockback) return;

        CheckDamaged();

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:
                agent.isStopped = true;
                SetAnim("idle", true);
                SetAnim("run", false);
                if (dist < detectionRange) state = State.Chase;
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.SetDestination(player.position);
                SetAnim("idle", false);
                SetAnim("run", true);
                if (dist > detectionRange) state = State.Idle;
                if (dist < attackRange && Time.time > lastAttackTime + attackCooldown)
                    state = State.Attack;
                break;

            case State.Attack:
                StartCoroutine(DoAttack());
                break;
        }
    }

    // =========================
    // ¿“¿ ¿
    // =========================
    IEnumerator DoAttack()
    {
        isAttacking = true;
        agent.isStopped = true;

        SetAnim("run", false);
        SetAnim("idle", true);
        if (anim) anim.SetTrigger("attack");

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        for (int wave = 0; wave < waveCount; wave++)
        {
            SpawnSingleWave(dir, wave);
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        lastAttackTime = Time.time;
        isAttacking = false;
        state = State.Chase;
    }

    void SpawnSingleWave(Vector3 dir, int waveIndex)
    {
        Vector3 perpendicular = Vector3.Cross(dir, Vector3.up).normalized;
        float distanceFromEnemy = 2f + waveIndex * wallSpread * 1.5f;

        for (int i = 0; i < wallCount; i++)
        {
            float offset = (i - (wallCount - 1) / 2f) * wallSpread;
            Vector3 spawnPos = transform.position
                             + dir * distanceFromEnemy
                             + perpendicular * offset
                             - Vector3.up * spawnDepth;

            GameObject jaw = Instantiate(jawProjectilePrefab, spawnPos, Quaternion.identity);
            jaw.GetComponent<JawProjectile>().Launch();
        }
    }

    // =========================
    // ”–ŒÕ
    // =========================
    void CheckDamaged()
    {
        if (zhp == null || !zhp.wasDamaged) return;
        zhp.wasDamaged = false;
        if (anim) anim.SetTrigger("Damaged");
    }

    // =========================
    // KNOCKBACK
    // =========================
    public void Knockback(Vector3 direction, float force)
    {
        if (isInKnockback) return;
        StartCoroutine(KnockbackRoutine(direction, force));
    }

    IEnumerator KnockbackRoutine(Vector3 direction, float force)
    {
        isInKnockback = true;

        agent.isStopped = true;
        agent.enabled = false;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 finalForce = direction.normalized * force;
        finalForce.y = 2f;
        rb.AddForce(finalForce, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        agent.enabled = true;
        agent.isStopped = false;

        isInKnockback = false;
    }

    // =========================
    // ’≈Àœ≈– ¿Õ»Ã¿÷»…
    // =========================
    void SetAnim(string param, bool val)
    {
        if (anim) anim.SetBool(param, val);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}