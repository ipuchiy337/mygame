using UnityEngine;

public class Boom : MonoBehaviour
{

    // добавь урон
    public float damage = 50f;

    public float Radius;
    public float Force;

    public GameObject BoomEffect;

    private bool BoomTrue;

    
    public void Explode()
    {
        

        Collider[] overlappedColliders = Physics.OverlapSphere(transform.position, Radius);

        for (int i = 0; i < overlappedColliders.Length; i++)
        {
            Rigidbody rigidbody = overlappedColliders[i].attachedRigidbody;
            if (rigidbody)
            {
                
                ZombieFollow zombie = rigidbody.GetComponent<ZombieFollow>();
                Health health = rigidbody.GetComponentInParent<Health>();
                if (zombie)
                {
                   
                    health.TakeDamage(damage);

                    rigidbody.AddExplosionForce(
                        Force,
                        transform.position,
                        Radius,
                        1f,
                        ForceMode.Impulse
                    );
                }
                Boom boom = rigidbody.GetComponent<Boom>();
                
                if (boom)
                {
                    if (Vector3.Distance(transform.position, rigidbody.position) < Radius / 2f)
                    {
                        boom.ExplodeWithDelay();
                        

                    }
                    

                }
            }
        }
        Destroy(gameObject);
        Instantiate(BoomEffect, transform.position, Quaternion.identity);
    }

    public void ExplodeWithDelay()
    {
        if (BoomTrue) return;
        BoomTrue = true;
        Explode();
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Radius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Radius / 2f);
    }
}
