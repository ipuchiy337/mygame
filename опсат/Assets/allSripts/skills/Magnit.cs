using UnityEngine;

public class Magnit : MonoBehaviour
{
    public UnPasivkiManager pasiv;

    public float attractorRange = 5f;   // базовый радиус прит€жени€
    public float rangePerLevel = 1f;    // + к радиусу за уровень
    public float moveSpeed = 8f;        // скорость движени€ к игроку
    public float pickupRange = 0.5f;    // дистанци€ подбора

    float GetRange()
    {
        return attractorRange + (pasiv != null ? pasiv.plusMagnit * rangePerLevel : 0);
    }

    private void Update()
    {
        float range = GetRange();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range);

        foreach (Collider hitCollider in hitColliders)
        {
            bool isXp = hitCollider.CompareTag("Xp");
            bool isMoney = hitCollider.CompareTag("Money");

            if (!isXp && !isMoney) continue;

            float dist = Vector3.Distance(transform.position, hitCollider.transform.position);

            // подбор
            if (dist <= pickupRange)
            {
                if (isXp)
                {
                    UpXp xp = hitCollider.GetComponent<UpXp>();
                    if (xp != null) xp.Pickup();
                }
                else if (isMoney)
                {
                    moneyDropped money = hitCollider.GetComponent<moneyDropped>();
                    if (money != null) money.Pickup();
                }
                continue;
            }

            // плавное движение к игроку
            hitCollider.transform.position = Vector3.MoveTowards(
                hitCollider.transform.position,
                transform.position,
                moveSpeed * Time.deltaTime
            );

            // отключаем физику пока летит Ч чтобы не мешала
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GetRange());

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}