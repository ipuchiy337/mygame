
using UnityEngine;

public enum MagicRarity { Common, Rare, Epic, Legendary }

public class MagicWeapon : MonoBehaviour
{
    [Header("Identification")]
    public int magicID;
    public string weaponName;
    public Sprite icon;
    public MagicRarity rarity;

    [Header("Stats")]
    [Tooltip("Урон заклинания")]
    public float damage = 30f;

    [Tooltip("Радиус поражения (AOE)")]
    public float aoeRadius = 3f;

    [Tooltip("Мана-стоимость / откат в секундах")]
    public float cooldown = 2f;

   
}