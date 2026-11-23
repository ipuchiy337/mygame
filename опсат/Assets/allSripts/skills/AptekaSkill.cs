using UnityEngine;
using UnityEngine.UI;

public class AptekaSkill : MonoBehaviour
{
    public PassivkiManager pasiv;

    public Health ehp;

    [Header("Реген")]
    public float baseHp = 20f;
    public float hpPerLevel = 2f;

    [Header("Cooldown")]
    public float cooldown = 60f;
    public float cooldownReductionPerLevel = 2f;
    public float minCooldown = 10f;

    [Header("UI")]
    public Button button;
    public Image cooldownFill;
    public Text cooldownText;

    private float cooldownTimer = 0f;
    private bool isCooldown = false;

    void Start()
    {
        // Кнопка доступна сразу
        button.onClick.AddListener(UseApteka);
        UpdateUI(1f, false);
    }

    void Update()
    {
        if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;

            float fill = 1f - (cooldownTimer / GetCooldown());
            UpdateUI(fill, true);

            if (cooldownTimer <= 0f)
            {
                isCooldown = false;
                UpdateUI(1f, false); // кнопка снова доступна
            }
        }
    }

    public void UseApteka()
    {
        if (isCooldown || ehp.hp >= ehp.maxHp) return;

        // Лечим игрока сразу
        float heal = baseHp + pasiv.apteka * hpPerLevel;
        ehp.hp += heal;
        if (ehp.hp > ehp.maxHp)
            ehp.hp = ehp.maxHp;

        // Старт кулдауна
        cooldownTimer = GetCooldown();
        isCooldown = true;

        UpdateUI(0f, true); // блокируем кнопку и заполняем fill
    }

    float GetCooldown()
    {
        float cd = cooldown - pasiv.apteka * cooldownReductionPerLevel;
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
