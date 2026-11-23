using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private LayerMask itemLayer;

    private Item nearestItem;

    void Update()
    {
        // Каждый кадр ищем ближайший предмет
        nearestItem = FindNearestItem();

        if ((InputManager.Instance.Interact) && nearestItem != null)
        {
            nearestItem.Pickup();
        }
    }

    Item FindNearestItem()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, itemLayer);

        Item closest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDist && col.TryGetComponent(out Item item))
            {
                minDist = dist;
                closest = item;
            }
        }

        return closest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}