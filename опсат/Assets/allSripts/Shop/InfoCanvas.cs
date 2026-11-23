using UnityEngine;
using UnityEngine.UI;

public class InfoCanvas : MonoBehaviour
{
    public static InfoCanvas Instance;

    [Header("Иконка и название")]
    public Image weaponIcon;
    public Text weaponName;

    [Header("Полоски статов — огнестрел (Image с типом Fill)")]
    public Image damageBar;
    public Image fireRateBar;
    public Image magazBar;
    public Image accuracyBar;
    public Image rangeBar;

    [Header("Полоски статов — ближний бой (Image с типом Fill)")]
    public Image meleeDamageBar;
    public Image meleeSpeedBar;
    public Image meleeRangeBar;

    [Header("Группы UI — включаем нужную")]
    public GameObject rangedStatsGroup;   // родитель полосок огнестрела
    public GameObject meleeStatsGroup;    // родитель полосок ближнего боя

    [Header("Максимумы огнестрел")]
    public float maxDamage = 100f;
    public float maxFireRate = 20f;
    public float maxMagaz = 60f;
    public float maxAccuracy = 100f;
    public float maxRange = 100f;

    [Header("Максимумы ближний бой")]
    public float maxMeleeDamage = 100f;
    public float maxMeleeSpeed = 5f;      // атак в секунду
    public float maxMeleeRange = 5f;      // метров
    [Header("Полоски статов — магия (Image с типом Fill)")]
    public Image magicDamageBar;
    public Image magicAoeBar;
    public Image magicCooldownBar;

    [Header("Группа UI — магия")]
    public GameObject magicStatsGroup;

    [Header("Максимумы магия")]
    public float maxMagicDamage = 150f;
    public float maxMagicAoe = 10f;
    public float maxMagicCooldown = 10f;

    [Header("Кнопки")]
    public Button takeButton;
    public Button closeButton;

    [Header("Прочее")]
    public Animator UIanim;
    public bool buyed;
    public AudioSource TakeBut;
    public AudioSource Select;

    private int currentIndex;
    private Shop currentShop;

    private void Awake()
    {
        Instance = this;
    }

    // --- Огнестрел ---
    public void Show(Weapon weapon, int index, Shop shop)
    {
        currentIndex = index;
        currentShop = shop;

        weaponIcon.sprite = weapon.icon;
        weaponName.text = weapon.name;

        damageBar.fillAmount = Mathf.Clamp01(weapon._damage / maxDamage);
        fireRateBar.fillAmount = Mathf.Clamp01(1f - (weapon._firerate / maxFireRate));
        magazBar.fillAmount = Mathf.Clamp01(weapon._magaz / maxMagaz);
        accuracyBar.fillAmount = Mathf.Clamp01(1f - ((weapon._toch - 1f) / (maxAccuracy - 1f)));
        rangeBar.fillAmount = Mathf.Clamp01(weapon._range / maxRange);

        // Показываем нужную группу статов
        rangedStatsGroup.SetActive(true);
        meleeStatsGroup.SetActive(false);
        magicStatsGroup.SetActive(false);

        PickupCanvas.Instance.PlayPickup(weapon);
    }
    public void ShowMagic(MagicWeapon weapon, int index, Shop shop)
    {
        currentIndex = index;
        currentShop = shop;

        weaponIcon.sprite = weapon.icon;
        weaponName.text = weapon.weaponName;

        magicDamageBar.fillAmount = Mathf.Clamp01(weapon.damage / maxMagicDamage);
        magicAoeBar.fillAmount = Mathf.Clamp01(weapon.aoeRadius / maxMagicAoe);
        // Чем меньше cooldown — тем лучше, поэтому инвертируем
        magicCooldownBar.fillAmount = Mathf.Clamp01(1f - (weapon.cooldown / maxMagicCooldown));

        rangedStatsGroup.SetActive(false);
        meleeStatsGroup.SetActive(false);
        magicStatsGroup.SetActive(true);

        PickupCanvas.Instance.PlayPickup(weapon);
    }
    // --- Ближний бой ---
    public void ShowMelee(MeleeWeapon weapon, int index, Shop shop)
    {
        currentIndex = index;
        currentShop = shop;

        weaponIcon.sprite = weapon.icon;
        weaponName.text = weapon.weaponName;

        meleeDamageBar.fillAmount = Mathf.Clamp01(weapon.damage / maxMeleeDamage);
        meleeSpeedBar.fillAmount = Mathf.Clamp01(weapon.rotationSpeed / maxMeleeSpeed);
        meleeRangeBar.fillAmount = Mathf.Clamp01(weapon.orbitRadius / maxMeleeRange);

        rangedStatsGroup.SetActive(false);
        magicStatsGroup.SetActive(false);
        meleeStatsGroup.SetActive(true);


        PickupCanvas.Instance.PlayPickup(weapon); // <-- добавили
    }

    // --- Покупка (универсальная, тип уже знает Shop) ---
    public void Buy()
    {
        currentShop.BuyItem(currentIndex);
        TakeBut.Play();
        UIanim.SetTrigger("close");
        ShopCameraManager.Instance.shopAnim.SetTrigger("buy");
        ShopCameraManager.Instance.CloseShopAfterBuy();
    }

    public void Hide()
    {
        UIanim.SetTrigger("close");
        ShopCameraManager.Instance.CloseShopNoBuy();
        ShopCameraManager.Instance.shopAnim.SetTrigger("close");
    }

    public void Zelect()
    {
        Select.Play();
    }
}