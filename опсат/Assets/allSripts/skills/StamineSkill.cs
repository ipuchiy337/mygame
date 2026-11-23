using UnityEngine;

public class StaminaSkill : MonoBehaviour
{
    public UnPasivkiManager pasiv;  // менеджер пассивок
    public Player player;          // игрок

    [Header("Базовые значения")]
    public float baseStamina = 50f;      // базовая стамина
    public float baseRegen = 15f;        // базовый реген стамины
    public float baseSpeed = 4.9f;

    [Header("Увеличение за уровень пассивки")]
    public float addStaminaPerLvl = 20f; // сколько добавляется к maxStamina за 1 уровень
    public float addRegenPerLvl = 4f;    // сколько добавляется к regen за 1 уровень
    public float addSpeed = 0.2f;

    private float lastSprintLevel = 0;     // хранит последний учтённый уровень пассивки

    public void AddStamina()
    {
        pasiv.plusStamina++;
    }
    void Update()
    {
        // обновляем стамину только если уровень пассивки изменился
        if (pasiv.plusStamina != lastSprintLevel)
        {
            UpdateStamina();
        }
    }

    void UpdateStamina()
    {
        // считаем новые значения
        float maxS = baseStamina + pasiv.plusStamina * addStaminaPerLvl;
        float regenS = baseRegen + pasiv.plusStamina * addRegenPerLvl;
        float speedS = baseSpeed + pasiv.plusStamina * addSpeed;

        // присваиваем игроку
        player.maxStamina = maxS;
        player.staminaRegen = regenS;
        player.sprintSpeed = speedS;

        // запоминаем текущий уровень, чтобы не обновлять каждый кадр
        lastSprintLevel = pasiv.plusStamina;

        Debug.Log($"Stamina updated: max={maxS}, regen={regenS}");
    }
}
