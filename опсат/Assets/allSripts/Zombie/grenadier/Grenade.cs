using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviour
{
    [Header("Взрыв")]
    public float explosionRadius = 4f;
    public float damage = 40f;
    [Tooltip("Урон падает к краю радиуса.")]
    public bool damageFalloff = true;
    public float explosionForce = 600f;
    public float upwardsModifier = 0.5f;
    [Tooltip("Слои, которые получают урон (игрок). Не включайте слой зомби, если не хотите дружественный огонь.")]
    public LayerMask damageMask = ~0;

    [Header("Эффекты")]
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("Поведение")]
    public float bounciness = 0.35f;
    public float randomSpin = 8f;

    private Rigidbody rb;
    private bool exploded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            PhysicsMaterial m = new PhysicsMaterial("GrenadeMat");
            m.bounciness = bounciness;
            m.dynamicFriction = 0.6f;
            m.staticFriction = 0.6f;
            m.bounceCombine = PhysicsMaterialCombine.Maximum;
            c.sharedMaterial = m;
        }
    }

    public void Launch(Vector3 velocity, float fuseTime)
    {
        rb.linearVelocity = velocity;
        rb.angularVelocity = Random.insideUnitSphere * randomSpin;
        Invoke(nameof(Explode), fuseTime);
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector3 pos = transform.position;

        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius, damageMask, QueryTriggerInteraction.Ignore);
        var damaged = new System.Collections.Generic.HashSet<Health>();

        foreach (Collider hit in hits)
        {
            Health hp = hit.GetComponentInParent<Health>();
            if (hp != null && damaged.Add(hp))
            {
                float dmg = damage;
                if (damageFalloff)
                {
                    float d = Vector3.Distance(pos, hit.ClosestPoint(pos));
                    dmg *= 1f - Mathf.Clamp01(d / explosionRadius);
                }
                if (dmg > 0f) hp.TakeDamage(dmg);
            }

            Rigidbody body = hit.attachedRigidbody;
            if (body != null)
                body.AddExplosionForce(explosionForce, pos, explosionRadius, upwardsModifier, ForceMode.Impulse);
        }

        if (explosionEffect != null)
            Destroy(Instantiate(explosionEffect, pos, Quaternion.identity), 5f);
        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, pos, soundVolume);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}