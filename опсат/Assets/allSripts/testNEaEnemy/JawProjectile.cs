using UnityEngine;
using System.Collections;

public class JawProjectile : MonoBehaviour
{
    public float damage = 20f;
    public float emergeSpeed = 5f;
    public float emergeDistance = 1.2f;
    public float stayDuration = 0.8f;

    [Header("Настройки урона")]
    public float damageInterval = 1f;
    private float nextDamageTime;       
    private bool canDamage = false;

    public void Launch()
    {
        StartCoroutine(SpikeCycle());
    }

    IEnumerator SpikeCycle()
    {
        Vector3 startPos = transform.position;
        Vector3 upPos = transform.position + Vector3.up * emergeDistance;
        float t = 0f;

        
        while (t < 1f)
        {
            t += Time.deltaTime * emergeSpeed;
            transform.position = Vector3.Lerp(startPos, upPos, t);
            yield return null;
        }

        canDamage = true;
        yield return new WaitForSeconds(stayDuration);
        canDamage = false;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * emergeSpeed;
            transform.position = Vector3.Lerp(upPos, startPos, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        // Проверяем: активна ли ловушка, тот ли это объект и пришло ли время наносить урон
        if (canDamage && other.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log("Урон игроку: " + damage + " | Время: " + Time.time);

                // Устанавливаем время следующей возможной атаки
                nextDamageTime = Time.time + damageInterval;
            }
        }
    }
}