using UnityEngine;
using UnityEngine.UI;

public class PassivkaUclon : MonoBehaviour
{
    public UnPasivkiManager pasMan;
    public Health dodge;

    public float addDodgePerLvl = 3f;
    public float lastDodgeAdded = 0f; 


    public void AddDodge()
    {
        lastDodgeAdded = GetRandomDodgeBonus();
        dodge.AddDodge(lastDodgeAdded);
        pasMan.uclon++;
        
    }
    float GetRandomDodgeBonus()
    {
      
        int max = 8;
        int min = 1;

        while (true)
        {
            int val = Random.Range(min, max + 1);
            float chance = 1f / val; 
            if (Random.value <= chance)
                return val;
        }
    }
}