using UnityEngine;

public class BushEffect : MonoBehaviour
{
    [Header("Замедление")]
    [Range(0.1f, 1)]
    public float slowMultiplier = 0.5f;

    [Header("Покачивание")]
    public float swayAmount = 4f;
    public float swaySpeed = 10f;
    public float swayDuration = 1.2f;

    private float swayTimer = 0f;
    private Quaternion originalRotation;
    private Transform bushVisual;

    // Счётчик: сколько объектов сейчас внутри триггера
    private System.Collections.Generic.Dictionary<Collider, bool> insideColliders
        = new System.Collections.Generic.Dictionary<Collider, bool>();

    void Start()
    {
        bushVisual = transform.childCount > 0 ? transform.GetChild(0) : transform;
        originalRotation = bushVisual.localRotation;
    }

    void Update()
    {
        if (swayTimer > 0)
        {
            swayTimer -= Time.deltaTime;
            float sway = Mathf.Sin(Time.time * swaySpeed) * swayAmount * (swayTimer / swayDuration);
            bushVisual.localRotation = originalRotation * Quaternion.Euler(0, 0, sway);
        }
        else
        {
            bushVisual.localRotation = Quaternion.Lerp(
                bushVisual.localRotation, originalRotation, Time.deltaTime * 5f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Если этот коллайдер уже внутри — пропускаем (защита от дублей)
        if (insideColliders.ContainsKey(other)) return;
        insideColliders[other] = true;

        swayTimer = swayDuration;

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            // Применяем замедление только если игрок ещё не замедлен этим кустом
            player.walkSpeed *= slowMultiplier;
            player.sprintSpeed *= slowMultiplier;
          
        }

        var agent = other.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed *= slowMultiplier;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Если этого коллайдера нет в словаре — Exit сработал без Enter, игнорируем
        if (!insideColliders.ContainsKey(other)) return;
        insideColliders.Remove(other);

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.walkSpeed /= slowMultiplier;
            player.sprintSpeed /= slowMultiplier;
       
        }

        var agent = other.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed /= slowMultiplier;
        }
    }

    // На случай если объект уничтожается внутри триггера — убираем эффект
    void OnDisable()
    {
        foreach (var kvp in insideColliders)
        {
            Player player = kvp.Key != null ? kvp.Key.GetComponent<Player>() : null;
            if (player != null)
            {
                player.walkSpeed /= slowMultiplier;
                player.sprintSpeed /= slowMultiplier;
             
            }

            var agent = kvp.Key != null ? kvp.Key.GetComponent<UnityEngine.AI.NavMeshAgent>() : null;
            if (agent != null)
                agent.speed /= slowMultiplier;
        }
        insideColliders.Clear();
    }
}