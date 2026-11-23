using UnityEngine;
using UnityEngine.UI;

public class PassivkaHp : MonoBehaviour
{
    public UnPasivkiManager pasMan;
    public Health health;

    public float addhp = 15f;
  
    public void AddHp()
    {
        health.maxHp += addhp;
        health.hp += addhp;
        pasMan.plusHp++;
    }
}
