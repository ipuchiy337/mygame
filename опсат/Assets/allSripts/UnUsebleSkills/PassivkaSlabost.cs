using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class PassivkaSlabost : MonoBehaviour
{
    public UnPasivkiManager pasMan;
    public List<ZombieAttack> zm;

    public float minDamage = 1f;
  

    public void Addotrava()
    {
        foreach (var h in zm)
        {
            if (h != null)
            {
                h.damage = Mathf.Max(0, h.damage - minDamage);
                pasMan.otrava++;
            }
        }
    }
}
