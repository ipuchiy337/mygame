using UnityEngine;
using UnityEngine.UI;

public class RegenSkill : MonoBehaviour
{
    public UnPasivkiManager pasiv;
    public HillManager hill;
    public Health ehp;

    [Header("–еген")]
    public float baseTime = 6f;        // базовый интервал между тиками хила
    public float timePerLevel = 0.3f;  // на сколько интервал уменьшаетс€ за каждый лвл
    public float baseHp = 1f;          // базовый хил за тик
    public float hpPerLevel = 0.5f;    // доп хил за каждый лвл



    float healTimer;
    public void AddRegen()
    {
        pasiv.plusRegen++;
    }
    void Update()
    {
        // реген не прокачан Ч ничего не делаем
        if (pasiv.plusRegen <= 0) return;

        // хп полное Ч реген не нужен
        if (ehp.hp >= ehp.maxHp)
        {
            healTimer = 0;
            SetRegenActive(false);
            return;
        }

        // хп не полное Ч регенируем
        SetRegenActive(true);

        healTimer += Time.deltaTime;

        // интервал между тиками уменьшаетс€ с каждым уровнем, минимум 0.5 сек
        float regenInterval = Mathf.Max(0.5f, baseTime - pasiv.plusRegen * timePerLevel);

        if (healTimer >= regenInterval)
        {
            healTimer = 0;
            DoRegen();
        }
    }

    void DoRegen()
    {
        float heal = baseHp + pasiv.plusRegen * hpPerLevel;
        ehp.hp = Mathf.Min(ehp.hp + heal, ehp.maxHp);
    }

    void SetRegenActive(bool active)
    {
        if (hill != null) hill.hilling = active;
       
    }
}