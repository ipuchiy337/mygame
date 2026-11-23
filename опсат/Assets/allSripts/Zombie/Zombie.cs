using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour
{
    [Header("Stats")]

    public float detectionRadius = 10f;
    public float chaseRadius = 15f;      // радиус, в котором зомби продолжает преследование
    public float attackDistance = 1.5f;
    public float attackCooldown = 1.5f;


    [Header("References")]
    public Transform player;
    public GameObject deathEffect;
   

    private NavMeshAgent agent;
    
    private bool isDead = false;
    private bool hasDetectedPlayer = false;
    private float lastAttackTime;

    void Start()
    {
       
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.stoppingDistance = attackDistance - 0.1f;
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 1?? если игрок в радиусе Ч начинаем преследовать
        if (distance <= detectionRadius)
        {
            hasDetectedPlayer = true;
        }

        // 2?? если зомби уже заметил игрока Ч продолжаем погоню
        if (hasDetectedPlayer)
        {
            agent.SetDestination(player.position);

            // если игрок слишком далеко Ч Утер€емФ его
            if (distance > chaseRadius)
            {
                hasDetectedPlayer = false;
                agent.ResetPath();
            }
        }

        // 3?? атака, если близко
        if (hasDetectedPlayer && distance <= attackDistance)
        {
            TryAttack();
        }

        // анимаци€ ходьбы (если есть)
       
    }

    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        
    }

    

    void Die()
    {
        isDead = true;
        agent.enabled = false;

        

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position + Vector3.up * 1f, Quaternion.identity);
        }

        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}
