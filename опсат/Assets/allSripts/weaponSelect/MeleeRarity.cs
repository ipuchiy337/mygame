using System.Collections.Generic;
using UnityEngine;

public enum MeleeRarity { Common, Rare, Epic, Legendary }

public class MeleeWeapon : MonoBehaviour
{
    [Header("Sword Visual")]
    public GameObject swordPrefab;

    [Header("Identification")]
    public int meleeID;
    public string weaponName;
    public Sprite icon;
    public MeleeRarity rarity;

    [Header("Orbit")]
    public int swordCount = 1;
    public float orbitRadius = 2f;
    public float rotationSpeed = 180f;

    [Header("Damage")]
    public float damage = 25f;
    public float hitRadius = 0.5f;
    public float knockbackForce = 10f;

    [Header("Gizmos")]
    public bool showDamageGizmos = true;
    public Color gizmoHitColor = Color.red;

    [Header("Layers")]
    public LayerMask enemyLayer;

    [Header("Upgrades")]
    public float AddHitRadius;
    public float AddDamage;
    public float AddAttackSpeed;

    [Header("Prices")]
    public float priceMultiplier;
    public float baseDamagePrice;
    public float baseAttackSpeedPrice;
    public float baseRangePrice;
    public float baseX2WeaponPrice;

    private List<Transform> swords = new();
    private List<HashSet<Collider>> hitLists = new();
    private float currentAngle;
    private float attackCooldown;

    int dmgLvl, atkS, rang, x2weapons;

    void Start()
    {
        CreateSwords();
    }

    void Update()
    {
        RotateOrbit();
        HandleDamage();
    }

    // =========================
    // —Œ«ƒ¿Õ»≈ Ã≈◊≈…
    // =========================
    void CreateSwords()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        swords.Clear();
        hitLists.Clear();

        for (int i = 0; i < swordCount; i++)
        {
            GameObject sword = Instantiate(swordPrefab, transform);
            swords.Add(sword.transform);
            hitLists.Add(new HashSet<Collider>());
        }
    }

    // =========================
    // ¬–¿Ÿ≈Õ»≈
    // =========================
    void RotateOrbit()
    {
        transform.rotation = Quaternion.identity;
        currentAngle += rotationSpeed * Time.deltaTime;

        for (int i = 0; i < swords.Count; i++)
        {
            float angle = currentAngle + (360f / swords.Count) * i;
            float rad = angle * Mathf.Deg2Rad;

            swords[i].localPosition = new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                0f,
                Mathf.Sin(rad) * orbitRadius
            );

            swords[i].localRotation = Quaternion.Euler(0f, -angle + 90f, -90f);
        }
    }

    // =========================
    // ”–ŒÕ
    // =========================
    void HandleDamage()
    {
        attackCooldown -= Time.deltaTime;

        if (attackCooldown <= 0f)
        {
            attackCooldown = 360f / rotationSpeed;
            foreach (var list in hitLists)
                list.Clear();
        }

        for (int i = 0; i < swords.Count; i++)
        {
            Collider[] hits = Physics.OverlapSphere(
                swords[i].position,
                hitRadius,
                enemyLayer
            );

            foreach (Collider col in hits)
            {
                if (hitLists[i].Contains(col)) continue;

                Health hp = col.GetComponent<Health>();
                if (hp == null) continue;

                hitLists[i].Add(col);
                hp.TakeDamage(damage); // ? ÚÓÎ¸ÍÓ Ó‰ËÌ ‡Á

                ZombieFollow zombie = col.GetComponent<ZombieFollow>();
                if (zombie != null)
                {
                    Vector3 dir = (col.transform.position - swords[i].position).normalized;
                    dir.y = 1f;
                    dir.Normalize();
                  

                }
                KiokController kiok = col.GetComponent<KiokController>();
                if (kiok != null)
                {
                    Vector3 dir = (col.transform.position - swords[i].position).normalized;
                    dir.y = 1f;
                    dir.Normalize();
                    kiok.Knockback(dir, knockbackForce);
                }
                SummonerEnemy SummonerEnemy = col.GetComponent<SummonerEnemy>();
                if (SummonerEnemy != null)
                {
                    Vector3 dir = (col.transform.position - swords[i].position).normalized;
                    dir.y = 1f;
                    dir.Normalize();
                    SummonerEnemy.Knockback(dir, knockbackForce);
                }
            }
        }
    }

    // =========================
    // ÷≈Õ€
    // =========================
    public int GetDamagePrice() => Mathf.RoundToInt(baseDamagePrice * Mathf.Pow(priceMultiplier, dmgLvl));
    public int GetAttackSPrice() => Mathf.RoundToInt(baseAttackSpeedPrice * Mathf.Pow(priceMultiplier, atkS));
    public int GetRangePrice() => Mathf.RoundToInt(baseRangePrice * Mathf.Pow(priceMultiplier, rang));
    public int Get2xPrice() => Mathf.RoundToInt(baseX2WeaponPrice * Mathf.Pow(priceMultiplier, x2weapons));

    // =========================
    // ”–Œ¬Õ»
    // =========================
    public int GetDamageLvl() => dmgLvl;
    public int GetAttaSl() => atkS;
    public int GetRangeLvl() => rang;
    public int Get2xLvl() => x2weapons;

    // =========================
    // ¿œ√–≈…ƒ€
    // =========================
    public void AddDmg()
    {
        if (dmgLvl >= 5) return;
        dmgLvl++;
        damage += AddDamage;
    }

    public void AddAtackS()
    {
        if (atkS >= 5) return;
        atkS++;
        rotationSpeed += AddAttackSpeed;
    }

    public void AddRange()
    {
        if (rang >= 5) return;
        rang++;
        orbitRadius += AddHitRadius;
    }

    public void Add2x()
    {
        if (x2weapons >= 3) return;
        x2weapons++;
        swordCount++;
        CreateSwords();
    }

    // =========================
    // œ–Œ¬≈– »
    // =========================
    public bool CanAddDamage() => dmgLvl < 5;
    public bool CanAddAtacks() => atkS < 5;
    public bool CanAddRange() => rang < 5;
    public bool CanAdd2x() => x2weapons < 3;

    // =========================00
    // GIZMOS
    // =========================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);

        if (swords == null || !showDamageGizmos) return;

        foreach (Transform sword in swords)
        {
            if (sword == null) continue;

            Gizmos.color = gizmoHitColor;
            Gizmos.DrawWireSphere(sword.position, hitRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, sword.position);
        }
    }
}