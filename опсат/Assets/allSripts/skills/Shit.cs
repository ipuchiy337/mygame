using UnityEngine;
using UnityEngine.UI;

public class Shit : MonoBehaviour
{
    public PassivkiManager pasiv;
    public Health hp;
    public GameObject shit_;

    [Header("Duration")]
    public float baseDoTime = 5f;
    public float doTimePerLevel = 1f;

    [Header("Cooldown")]
    public float baseCooldown = 60f;
    public float cooldownReductionPerLevel = 2f;
    public float minCooldown = 10f;

    [Header("UI")]
    public Button button;
    public Image cooldownFill;
    public Text cooldownText;

    private float activeTimer;
    private float cooldownTimer;

    private bool isActive;
    private bool isCooldown;

 

    void Update()
    {
        // ? ВРЕМЯ ДЕЙСТВИЯ
        if (isActive)
        {
            activeTimer -= Time.deltaTime;

            float fill = activeTimer / GetDoTime();
            UpdateUI(fill, true);

            if (activeTimer <= 0f)
                Deactivate();
        }
        // ?? КУЛДАУН
        else if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;

            float fill = 1f - (cooldownTimer / GetCooldown());
            UpdateUI(fill, true);

            if (cooldownTimer <= 0f)
            {
                isCooldown = false;
                UpdateUI(1f, false);
            }
        }
    }

    public void Activate()
    {
        if (isActive || isCooldown) return;

        hp.isInvulnerable = true; // ??
        isActive = true;
        activeTimer = GetDoTime();

        hp.enabled = false;
        shit_.SetActive(true);

        button.interactable = false;
    }

    void Deactivate()
    {
        isActive = false;

        hp.isInvulnerable = false; // ??
        hp.enabled = true;
        shit_.SetActive(false);

        // ? старт кулдауна ТОЛЬКО после окончания действия
        isCooldown = true;
        cooldownTimer = GetCooldown();
    }

    float GetDoTime()
    {
        return baseDoTime + pasiv.shit * doTimePerLevel;
    }

    float GetCooldown()
    {
        float cd = baseCooldown - pasiv.shit * cooldownReductionPerLevel;
        return Mathf.Max(minCooldown, cd);
    }

    void UpdateUI(float fill, bool locked)
    {
        if (cooldownFill != null)
            cooldownFill.fillAmount = fill;

        if (cooldownText != null)
            cooldownText.text = locked ? Mathf.Ceil(isActive ? activeTimer : cooldownTimer).ToString() : "";

        if (button != null)
            button.interactable = !locked;
    }
}