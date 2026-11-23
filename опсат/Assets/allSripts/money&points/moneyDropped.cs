using Unity.VisualScripting;
using UnityEngine;

public class moneyDropped : MonoBehaviour
{
    public int _moneyDr;

    public int minMoney = 1; // Минимальное значение
    public int maxMoney = 5; // Максимальное значение (включительно)

    money _dengi;
    private void Start()
    {
        _dengi = FindObjectOfType<money>();

        _moneyDr = Random.Range(minMoney, maxMoney);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _dengi._money += _moneyDr;
            Destroy(gameObject);
        }
    }
    public void Pickup()
    {
        _dengi._money += _moneyDr;
        Destroy(gameObject);
    }
}
