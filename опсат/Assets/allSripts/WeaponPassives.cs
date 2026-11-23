using UnityEngine;

public class WeaponPassives : MonoBehaviour
{
    [Header("Stats")]
    public float damage;
    public float fireRate;
    public int magaz;

    [Header("Passive Adds")]
    public float addDamage = 5f;
    public float addFireRate = 0.05f;
    public int addMagaz = 5;

    int dmgLvl, fireLvl, magLvl;

    public void AddDamagePassive()
    {
        if (dmgLvl >= 3) return;
        damage += addDamage;
        dmgLvl++;
    }

    public void AddFireRatePassive()
    {
        if (fireLvl >= 3) return;
        fireRate = Mathf.Max(0.05f, fireRate - addFireRate);
        fireLvl++;
    }

    public void AddMagazPassive()
    {
        if (magLvl >= 3) return;
        magaz += addMagaz;
    }

    public bool CanAddDamage() => dmgLvl < 3;
    public bool CanAddFireRate() => fireLvl < 3;
    public bool CanAddMagaz() => magLvl < 3;
}