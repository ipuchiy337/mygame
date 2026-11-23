using UnityEngine;
using UnityEngine.UI;

public enum WeaponRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public class Weapon : MonoBehaviour
{
    public int weaponID;
    public WeaponRarity rarity;
    public int price;
    public Sprite icon;
    public Sprite rare;
    public float _firerate;
    public float _magaz;
    public float _damage;
    public float _toch;
    public float _range;

    int dmgLvl, fireLvl, magLvl, tochLvl, rangeLvl, x2Lvl;
    public GunWithTracer gun;

    // Базовые цены для каждого улучшения
    public int baseDamagePrice = 100;
    public int baseFireRatePrice = 150;
    public int baseMagazPrice = 120;
    public int baseTochPrice = 80;
    public int baseRangePrice = 100;
    public int base2xPrice = 500;

    // Множитель цены (1.5f или 2f на твой выбор)
    public float priceMultiplier = 1.5f;

    // Методы для получения текущей цены каждого улучшения
    public int GetDamagePrice() => Mathf.RoundToInt(baseDamagePrice * Mathf.Pow(priceMultiplier, dmgLvl));
    public int GetFireRatePrice() => Mathf.RoundToInt(baseFireRatePrice * Mathf.Pow(priceMultiplier, fireLvl));
    public int GetMagazPrice() => Mathf.RoundToInt(baseMagazPrice * Mathf.Pow(priceMultiplier, magLvl));
    public int GetTochPrice() => Mathf.RoundToInt(baseTochPrice * Mathf.Pow(priceMultiplier, tochLvl));
    public int GetRangePrice() => Mathf.RoundToInt(baseRangePrice * Mathf.Pow(priceMultiplier, rangeLvl));
    public int Get2xPrice() => Mathf.RoundToInt(base2xPrice * Mathf.Pow(priceMultiplier, x2Lvl));

    // Методы для получения текущего уровня
    public int GetDamageLvl() => dmgLvl;
    public int GetFireRateLvl() => fireLvl;
    public int GetMagazLvl() => magLvl;
    public int GetTochLvl() => tochLvl;
    public int GetRangeLvl() => rangeLvl;
    public int Get2xLvl() => x2Lvl;

    public void AddDamagePassive()
    {
        if (dmgLvl < 5)
        {
            gun.AddDamagePassive();
            dmgLvl++;
        }
    }

    public void AddFireRatePassive()
    {
        if (fireLvl < 5)
        {
            gun.AddFireRatePassive();
            fireLvl++;
        }
    }

    public void AddMagazPassive()
    {
        if (magLvl < 5)
        {
            gun.AddMagazPassive();
            magLvl++;
        }
    }

    public void AddTochnostPassive()
    {
        if (tochLvl < 5)
        {
            gun.AddTochnostPassive();
            tochLvl++;
        }
    }

    public void AddRangePassive()
    {
        if (rangeLvl < 5)
        {
            gun.AddRangePassive();
            rangeLvl++;
        }
    }

    public void Add2xPassive()
    {
        if (x2Lvl < 1)
        {
            gun.Add2xPassive();
            x2Lvl++;
        }
    }

    public bool CanAddDamage() => dmgLvl < 5;
    public bool CanAddFireRate() => fireLvl < 5;
    public bool CanAddMagaz() => magLvl < 5;
    public bool CanAddToch() => tochLvl < 5;
    public bool CanAddRange() => rangeLvl < 5;
    public bool CanAdd2x() => x2Lvl < 1;
}