using UnityEngine;

// Одна секция стенки: трясётся под землёй (предупреждение) -> резко вылезает -> стоит -> уходит обратно.
// Урон наносится один раз, когда секция вылезает и пересекается с игроком.
//
// ВАЖНО для префаба: pivot модели должен быть у ОСНОВАНИЯ (низ), а высота модели ~= riseHeight.
// Спавнится через ZombieSummoner, руками на сцену ставить не нужно.
public class RisingWall : MonoBehaviour
{
    [Header("Размеры")]
    [Tooltip("Высота модели. Столько она прячется под землёй.")]
    public float riseHeight = 2f;
    [Tooltip("Зона урона (локальные оси секции). Ширина по X должна быть чуть больше columnSpacing, глубина по Z - чуть больше rowSpacing.")]
    public Vector3 hitSize = new Vector3(1.2f, 2f, 1.2f);

    [Header("Тайминги")]
    [Tooltip("Сколько секунд игрок видит предупреждение, прежде чем стенка вылезет.")]
    public float telegraphTime = 0.6f;
    public float riseTime = 0.12f;
    public float holdTime = 1.5f;
    public float sinkTime = 0.5f;
    [Tooltip("Сколько секунд после выхода секция ещё может нанести урон.")]
    public float damageWindow = 0.3f;

    [Header("Предупреждение")]
    [Tooltip("Насколько секция слегка выпирает из земли перед ударом.")]
    public float telegraphPeek = 0.15f;
    public float shakeAmount = 0.04f;

    [Header("Эффекты (необязательно)")]
    [Tooltip("Декаль/частицы на земле во время предупреждения.")]
    public GameObject warningPrefab;
    [Tooltip("Пыль/осколки в момент выхода из земли.")]
    public GameObject burstPrefab;

    private enum State { Telegraph, Rise, Hold, Sink }

    private static readonly Collider[] hitBuffer = new Collider[8];

    private Rigidbody rb;
    private State state;
    private float t;
    private bool ready;
    private bool hasHit;

    private Vector3 ground;
    private Vector3 hidden;
    private Vector3 riseFrom;
    private float damage;
    private LayerMask targetMask;

    private void Awake()
    {
        // Кинематический Rigidbody, чтобы движущийся коллайдер нормально работал с физикой.
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Init(Vector3 groundPosition, float damageAmount, LayerMask targets)
    {
        ground = groundPosition;
        hidden = groundPosition + Vector3.down * riseHeight;
        riseFrom = hidden + Vector3.up * telegraphPeek;
        damage = damageAmount;
        targetMask = targets;

        transform.position = hidden;
        rb.position = hidden;

        if (warningPrefab != null)
        {
            GameObject warning = Instantiate(warningPrefab, ground + Vector3.up * 0.02f, Quaternion.identity);
            Destroy(warning, telegraphTime + riseTime + 0.2f);
        }

        SetState(State.Telegraph);
        ready = true;
    }

    private void SetState(State next)
    {
        state = next;
        t = 0f;

        if (next == State.Rise && burstPrefab != null)
        {
            GameObject burst = Instantiate(burstPrefab, ground, Quaternion.identity);
            Destroy(burst, 2f);
        }
    }

    private void FixedUpdate()
    {
        if (!ready) return;
        t += Time.fixedDeltaTime;

        switch (state)
        {
            case State.Telegraph:
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, telegraphTime));
                Vector3 shake = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * (shakeAmount * k);
                rb.MovePosition(hidden + Vector3.up * (telegraphPeek * k) + shake);
                if (t >= telegraphTime) SetState(State.Rise);
                break;
            }

            case State.Rise:
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, riseTime));
                float eased = 1f - (1f - k) * (1f - k) * (1f - k); // резкий старт, мягкая остановка
                rb.MovePosition(Vector3.Lerp(riseFrom, ground, eased));
                if (k >= 0.25f) TryDamage();
                if (k >= 1f) SetState(State.Hold);
                break;
            }

            case State.Hold:
                rb.MovePosition(ground);
                if (t < damageWindow) TryDamage();
                if (t >= holdTime) SetState(State.Sink);
                break;

            case State.Sink:
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, sinkTime));
                rb.MovePosition(Vector3.Lerp(ground, hidden, k * k));
                if (k >= 1f) Destroy(gameObject);
                break;
            }
        }
    }

    private void TryDamage()
    {
        if (hasHit) return;

        Vector3 center = transform.position + Vector3.up * (hitSize.y * 0.5f);
        int count = Physics.OverlapBoxNonAlloc(center, hitSize * 0.5f, hitBuffer,
            transform.rotation, targetMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Health hp = hitBuffer[i].GetComponentInParent<Health>();
            if (hp == null) continue;

            hp.TakeDamage(damage);
            hasHit = true;
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * (hitSize.y * 0.5f),
            transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitSize);
    }
}
