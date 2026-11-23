using UnityEngine;
using UnityEngine.UI;

public class AuraDamage : MonoBehaviour
{
    [Header("Aura Stats")]
    public float damage = 10f;
    public float radius = 5f;
    public float attackRate = 1f;
    public LayerMask enemyMask;

    [Header("Upgrades")]
    public float addDamage = 5f;
    public float addRadius = 0.5f;
    public float addAttackRate = 0.2f;

    public float maxDamage;
    public float maxRadius;
    public float maxAttackRate;

    [Header("Visual")]
    public Transform auraVisualTransform;
    public float rotateSpeed = 30f;

    private float _timer;

    

    void Update()
    {
        _timer += Time.deltaTime;
        float interval = attackRate > 0f ? 1f / attackRate : 999f;

        if (_timer >= interval)
        {
            _timer = 0f;
            DealAuraDamage();
        }

        if (auraVisualTransform != null)
            auraVisualTransform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (auraVisualTransform == null) return;
        float size = radius * 2f;
        auraVisualTransform.localScale = new Vector3(size, auraVisualTransform.localScale.y, size);
    }

    void DealAuraDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyMask);
        foreach (var hit in hits)
        {
            Health h = hit.GetComponent<Health>();
            if (h != null)
                h.TakeDamage(damage);
        }
    }

    int dmgLvl, radLvl, atkLvl;

    public int baseDamagePrice = 100;
    public int baseRadiusPrice = 120;
    public int baseAttackRatePrice = 150;
    public float priceMultiplier = 1.5f;

    public int GetDamagePrice() => Mathf.RoundToInt(baseDamagePrice * Mathf.Pow(priceMultiplier, dmgLvl));
    public int GetRadiusPrice() => Mathf.RoundToInt(baseRadiusPrice * Mathf.Pow(priceMultiplier, radLvl));
    public int GetAttackRatePrice() => Mathf.RoundToInt(baseAttackRatePrice * Mathf.Pow(priceMultiplier, atkLvl));

    public bool CanAddDamage() => dmgLvl < 5;
    public bool CanAddRadius() => radLvl < 5;
    public bool CanAddAttackRate() => atkLvl < 5;

    public void AddDamagePassive()
    {
        if (!CanAddDamage()) return;
        damage += addDamage;
        maxDamage++;
        dmgLvl++;
    }

    public void AddRadiusPassive()
    {
        if (!CanAddRadius()) return;
        radius += addRadius;
        maxRadius++;
        radLvl++;
        UpdateVisual();
    }

    public void AddAttackRatePassive()
    {
        if (!CanAddAttackRate()) return;
        attackRate += addAttackRate;
        maxAttackRate++;
        atkLvl++;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}