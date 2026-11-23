using UnityEngine;

public class XpPickup : MonoBehaviour
{
    Xp _exp;

    void Start()
    {
        _exp = FindObjectOfType<Xp>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Xp"))
        {
            UpXp xp = other.GetComponent<UpXp>();
            if (xp != null) xp.Pickup();
        }
        else if (other.CompareTag("Money"))
        {
            moneyDropped money = other.GetComponent<moneyDropped>();
            if (money != null) money.Pickup();
        }
    }
}