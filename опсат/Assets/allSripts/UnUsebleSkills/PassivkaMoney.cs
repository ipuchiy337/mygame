using UnityEngine;
using UnityEngine.UI;

public class PassivkaMoney : MonoBehaviour
{
    public UnPasivkiManager pasMan;
    public moneyDropped upm;

    public int MaxM = 2;


    public void Addmoney()
    {
        upm.maxMoney += MaxM;
        pasMan.plusMoney++;
    }
}
