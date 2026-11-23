using UnityEngine;
using UnityEngine.UI;
public class UoPlayerHealth : MonoBehaviour
{
    public Health _health;
    public money money;

    public int needMoney = 100;
    public Text mone;

    public float hpPercent = 0.05f;

    public int _hpLvl;

   
    public void AddHp()
    {
        
        
        float add = _health.maxHp * hpPercent;
        _health.maxHp += add;
        _hpLvl++;
    }

    public void Buy()
    {
        if (_hpLvl < 15)
        {
            if (money._money >= needMoney)
            {
                money._money -= needMoney;
                AddHp();
               
                mone.text = needMoney.ToString();
                if (_hpLvl >= 15)
                {
                    mone.text = ("MAX LEVEL");
                }
            }

        }
        

    }
}
