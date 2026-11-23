using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Data")]
public class WeaponData : ScriptableObject
{
    public int weaponID;
    public string weaponName;
    public WeaponRarity rarity;
    public int price;

    public Sprite icon;

    [Header("Stats")]
    public int damage;
    public float fireRate;
    public int magazine;
    [TextArea]
    public string description;
}
