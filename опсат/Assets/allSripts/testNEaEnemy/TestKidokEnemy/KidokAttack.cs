using UnityEngine;

public class KidokAttack : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f;
    public float damage = 20f;

    private Vector3 direction;

    public void Launch(Vector3 dir)
    {
        direction = dir;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    // Если у тебя уже есть своя система HP — замени на вызов своего метода
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<Health>()?.TakeDamage(damage);
            Debug.Log("Попал! Урон: " + damage);
            Destroy(gameObject);
        }
    }
}