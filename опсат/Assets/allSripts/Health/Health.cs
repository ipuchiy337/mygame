using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public float hp = 100f;
    public float maxHp;
    public bool wasDamaged;
    public bool isInvulnerable = false;

    public GameObject damageText;
    public GameObject TakeDamageVFX;

    public GameObject FloorBlood1;
    public GameObject FloorBlood2;

    public GameObject xp;
    public GameObject money;

    [Header("Партикл смерти")]
    public GameObject deathParticle; 

    public float spawnChance = 0.15f;
    public float blood1chance = 0.5f;
    public float blood2chance = 0.5f;

    public Kills kill;

    [Header("Дроп пассивок")]
    public List<GameObject> droppedSkillPrefabs;
    public float skillDropChance = 0.15f;

    public float uclonenie;
    private const float MAX_DODGE = 82f;

    [Header("Критический урон")]
    [Range(0f, 100f)]
    public float critChance = 0f;
    public float critMultiplier = 1f;

    private void Start()
    {
        hp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable) return;

        float dodgeChance = uclonenie / 100f;
        if (Random.value <= dodgeChance)
        {
            Debug.Log("УКЛОНЕНИЕ");
            return;
        }

        bool isCrit = Random.value <= (critChance / 100f);
        if (isCrit)
        {
            damage *= critMultiplier;
            Debug.Log("КРИТИЧЕСКИЙ УРОН! = " + damage);
        }

        hp -= damage;

        if (damageText != null)
        {
            GameObject textGo = Instantiate(damageText, transform.position + Vector3.up, Quaternion.identity);
            TakeDamageEfText damageTextScript = textGo.GetComponent<TakeDamageEfText>();
            if (damageTextScript != null)
                damageTextScript.Setup(damage);
        }

        var damageVFX = Instantiate(TakeDamageVFX, transform.position, Quaternion.identity, transform);
        Destroy(damageVFX, 2f);
        wasDamaged = true;

        if (hp <= 0)
        {
            Instantiate(xp, transform.position + Vector3.up, Quaternion.identity);
            if (Random.value <= spawnChance)
                Instantiate(money, transform.position + Vector3.up, Quaternion.identity);

            kill._killed++;
            Die();
        }
    }

    void Die()
    {
        // Спавн партикла смерти
        if (deathParticle != null)
        {
            GameObject particle = Instantiate(deathParticle, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(particle, 4f);
        }

        if (Random.value <= skillDropChance && droppedSkillPrefabs.Count > 0)
        {
            int index = Random.Range(0, droppedSkillPrefabs.Count);
            GameObject prefab = droppedSkillPrefabs[index];
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f));
            GameObject drop = Instantiate(prefab, transform.position + randomOffset, Quaternion.identity);

            var droppedSkill = drop.GetComponent<DroppedSkill>();
            if (droppedSkill != null)
                droppedSkill.Init(GetRandomSkill());
        }

        // Убрал дублирующийся kill._killed++ который был здесь
        Destroy(gameObject);
    }

    public void AddDodge(float value)
    {
        uclonenie = Mathf.Clamp(uclonenie + value, 0f, MAX_DODGE);
    }

    NameSkill GetRandomSkill()
    {
        NameSkill[] allSkills = (NameSkill[])System.Enum.GetValues(typeof(NameSkill));
        int index = Random.Range(0, allSkills.Length);
        return allSkills[index];
    }
}
