using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PassivkaKrit : MonoBehaviour
{
    public List<Health> ghp;

    public UnPasivkiManager pasMan;
    public float kritShans = 4f;
    public float kritDmg = 0.05f;



    public void AddKrit()
    {
        pasMan.plusKrit++;
        foreach (var h in ghp)
        {
            if (h != null)
            {
                h.critChance += kritShans;
                h.critMultiplier += kritDmg;
               
            }
                

        }
    }

}
