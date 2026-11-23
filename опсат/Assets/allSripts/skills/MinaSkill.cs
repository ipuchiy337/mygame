using UnityEngine;
using UnityEngine.UI;

public class MinaSkill : MonoBehaviour
{
    public PassivkiManager pasiv;
    public Transform player;
    public GameObject minaPrefab;

    [Header("Damage / Range")]
    public float baseDamage = 30f;
    public float addDamagePerLevel = 10f;
    public float baseRange = 1f;
    public float addRangePerLevel = 0.3f;

    [Header("Cooldown")]
    public float baseCooldown = 100f;
    public float cooldownReductionPerLevel = 5f;
    public float minCooldown = 10f;

    private float cooldownTimer;
    private bool isCooldown;

    [Header("UI")]
    public Button button;
    public Image cooldownFill;
    public Text cooldownText;

    void Start()
    {
        // ¬ начале мина готова Ч закрашиваем на 1
        UpdateUI(1f, false);
    }

    void Update()
    {
        if (!isCooldown) return;

        cooldownTimer -= Time.deltaTime;
        float maxCd = GetCooldown();

        // Ўкала растет от 0 до 1 по мере отката
        float fillValue = 1f - (cooldownTimer / maxCd);

        UpdateUI(fillValue, true);

        if (cooldownTimer <= 0f)
        {
            isCooldown = false;
            UpdateUI(1f, false); // ќткатилось Ч заливка на 1, кнопка доступна
        }
    }

    public void Miner()
    {
        if (isCooldown) return;

        // Ћогика спавна мины (оставл€ем твою)
        float damage = baseDamage + pasiv.mina * addDamagePerLevel;
        float range = baseRange + pasiv.mina * addRangePerLevel;
        Vector3 spawnPos = player.position + player.forward * 1.5f + Vector3.up * 0.5f;
        GameObject mineObj = Instantiate(minaPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = mineObj.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(player.forward * 5f + Vector3.up * 2f, ForceMode.Impulse);

        Boom boom = mineObj.GetComponent<Boom>();
        if (boom != null) { boom.damage = damage; boom.Radius = range; }

        // «јѕ”—   ”Ћƒј”Ќј
        cooldownTimer = GetCooldown();
        isCooldown = true;

        UpdateUI(0f, true); // —разу в 0 и выключаем кнопку
    }

    float GetCooldown()
    {
        float cd = baseCooldown - pasiv.mina * cooldownReductionPerLevel;
        return Mathf.Max(minCooldown, cd);
    }

    void UpdateUI(float fill, bool onCooldown)
    {
        if (cooldownFill != null)
            cooldownFill.fillAmount = fill;

        if (cooldownText != null)
            cooldownText.text = onCooldown ? Mathf.Ceil(cooldownTimer).ToString() : "";

        if (button != null)
            button.interactable = !onCooldown;
    }
}