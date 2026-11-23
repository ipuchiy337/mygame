using System.Collections.Generic;
using UnityEngine;


public enum ShopSlotType { Ranged, Melee, Magic }

[System.Serializable]
public class ShopItem
{
    public ShopSlotType type;
    public Weapon rangedWeapon;
    public MeleeWeapon meleeWeapon;
    public MagicWeapon magicWeapon;
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    public WeaponShopManager weaponManager;
    public MeleeWeaponShopManager meleeManager;
    public MagicWeaponShopManager magicManager;

    private List<Weapon> weaponPool;
    private List<MeleeWeapon> meleePool;
    private List<MagicWeapon> magicPool;

    private void Awake()
    {
        Instance = this;
        weaponPool = weaponManager.allWeapons;
        meleePool = meleeManager.allMeleeWeapons;
        magicPool = magicManager.allMagicWeapons;
    }

    // Слот 1 — огнестрел, Слот 2 — ближний, Слот 3 — магия
    public List<ShopItem> GetShopItems()
    {
        List<ShopItem> items = new();

        items.Add(new ShopItem
        {
            type = ShopSlotType.Ranged,
            rangedWeapon = GetWeaponByRarity()
        });

        items.Add(new ShopItem
        {
            type = ShopSlotType.Melee,
            meleeWeapon = GetMeleeByRarity()
        });

        items.Add(new ShopItem
        {
            type = ShopSlotType.Magic,
            magicWeapon = GetMagicByRarity()
        });

        return items;
    }

    // --- Огнестрел ---
    Weapon GetWeaponByRarity()
    {
        int roll = Random.Range(0, 100);
        if (roll < 50) return GetRandomByRarity(WeaponRarity.Common);
        if (roll < 80) return GetRandomByRarity(WeaponRarity.Rare);
        if (roll < 95) return GetRandomByRarity(WeaponRarity.Epic);
        return GetRandomByRarity(WeaponRarity.Legendary);
    }

    Weapon GetRandomByRarity(WeaponRarity rarity)
    {
        var list = weaponPool.FindAll(w => w.rarity == rarity);
        if (list.Count == 0) return weaponPool[Random.Range(0, weaponPool.Count)];
        return list[Random.Range(0, list.Count)];
    }

    // --- Ближний бой ---
    MeleeWeapon GetMeleeByRarity()
    {
        int roll = Random.Range(0, 100);
        if (roll < 50) return GetRandomMeleeByRarity(MeleeRarity.Common);
        if (roll < 80) return GetRandomMeleeByRarity(MeleeRarity.Rare);
        if (roll < 95) return GetRandomMeleeByRarity(MeleeRarity.Epic);
        return GetRandomMeleeByRarity(MeleeRarity.Legendary);
    }

    MeleeWeapon GetRandomMeleeByRarity(MeleeRarity rarity)
    {
        var list = meleePool.FindAll(w => w.rarity == rarity);
        if (list.Count == 0) return meleePool[Random.Range(0, meleePool.Count)];
        return list[Random.Range(0, list.Count)];
    }

    // --- Магия ---
    MagicWeapon GetMagicByRarity()
    {
        int roll = Random.Range(0, 100);
        if (roll < 50) return GetRandomMagicByRarity(MagicRarity.Common);
        if (roll < 80) return GetRandomMagicByRarity(MagicRarity.Rare);
        if (roll < 95) return GetRandomMagicByRarity(MagicRarity.Epic);
        return GetRandomMagicByRarity(MagicRarity.Legendary);
    }

    MagicWeapon GetRandomMagicByRarity(MagicRarity rarity)
    {
        var list = magicPool.FindAll(w => w.rarity == rarity);
        if (list.Count == 0) return magicPool[Random.Range(0, magicPool.Count)];
        return list[Random.Range(0, list.Count)];
    }
}