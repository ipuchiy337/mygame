using UnityEngine;
using UnityEngine.UI;

public class money : MonoBehaviour
{
    public int _money;
    public Text mT;

    public void AddMoney(int amount)
    {
        _money += amount;
        Debug.Log("Δενόγθ: " + _money);

    }
    private void Update()
    {
        mT.text = _money.ToString();
    }
}

