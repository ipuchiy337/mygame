using UnityEngine;
using UnityEngine.UI;

public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance;

    [Header("Корневой объект")]
    public GameObject rootPanel;

    [Header("Панели типов оружия")]
    public GameObject gunPanel;
    public GameObject meleePanel;
    public GameObject magicPanel;

    [Header("Подсветки вкладок")]
    public GameObject gunTabHighlight;
    public GameObject meleeTabHighlight;
    public GameObject magicTabHighlight;

    [Header("Скрипты")]
    public UpgradeGun upgradeGun;
    public UpgradeMeleeWeapon upgradeMelee;
    public MagicWeaponUpgrade upgradeMagic;
    public PauseManager pause;

    [Header("Gun — кнопки")]
    public Button gunDamageBtn;
    public Button gunFireBtn;
    public Button gunMagazBtn;
    public Button gunTochBtn;
    public Button gunRangeBtn;

    [Header("Gun — подсветки")]
    public GameObject gunDamageBg;
    public GameObject gunFireBg;
    public GameObject gunMagazBg;
    public GameObject gunTochBg;
    public GameObject gunRangeBg;

    [Header("Melee — кнопки")]
    public Button meleeDamageBtn;
    public Button meleeAttacksBtn;
    public Button meleeRangeBtn;
    public Button meleeX2Btn;

    [Header("Melee — подсветки")]
    public GameObject meleeDamageBg;
    public GameObject meleeAttacksBg;
    public GameObject meleeRangeBg;
    public GameObject meleeX2Bg;

    // внутреннее состояние
    private int currentPanel = 0; // 0=Gun 1=Melee 2=Magic
    private int currentBtn = 0;
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    void Awake() => Instance = this;

    void Start()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    // ??? Открытие / Закрытие ??????????????????????????????????????

    public void Open()
    {
        isOpen = true;
        currentPanel = 0;
        currentBtn = 0;
        rootPanel.SetActive(true);
        pause.Pauz();
        ShowPanel(0);
        UpdateTabHighlights();
        UpdateButtonHighlight();
    }

    public void Close()
    {
        isOpen = false;
        rootPanel.SetActive(false);
        ClearAllHighlights();
        pause.Rezume();
    }

    // ??? Update ???????????????????????????????????????????????????

    void Update()
    {
        if (!isOpen) return;

        // Листаем панели — A/D или D-Pad Left/Right
        if (InputManager.Instance.UILeft)
        {
            currentPanel = (currentPanel - 1 + 3) % 3;
            currentBtn = 0;
            ShowPanel(currentPanel);
            UpdateTabHighlights();
            UpdateButtonHighlight();
        }
        else if (InputManager.Instance.UIRight)
        {
            currentPanel = (currentPanel + 1) % 3;
            currentBtn = 0;
            ShowPanel(currentPanel);
            UpdateTabHighlights();
            UpdateButtonHighlight();
        }

        // Листаем кнопки — W/S или D-Pad Up/Down
        int btnCount = GetButtonCount();
        if (btnCount > 0)
        {
            if (InputManager.Instance.UIUp)
            {
                currentBtn = (currentBtn - 1 + btnCount) % btnCount;
                UpdateButtonHighlight();
            }
            else if (InputManager.Instance.UIDown)
            {
                currentBtn = (currentBtn + 1) % btnCount;
                UpdateButtonHighlight();
            }

            // Покупка — E или A геймпад
            if (InputManager.Instance.UIConfirm)
            {
                TryBuy();
            }
        }

        // Закрыть — Z или B
        if (InputManager.Instance.UIBack)
            Close();
    }

    // ??? Панели ???????????????????????????????????????????????????

    void ShowPanel(int index)
    {
        gunPanel.SetActive(index == 0);
        meleePanel.SetActive(index == 1);
        magicPanel.SetActive(index == 2);

        if (index == 0) upgradeGun.RefreshUI();
        else if (index == 1) upgradeMelee.RefreshUI();
    }

    // ??? Покупка ??????????????????????????????????????????????????

    void TryBuy()
    {
        if (currentPanel == 0)
        {
            switch (currentBtn)
            {
                case 0: if (gunDamageBtn.interactable) { upgradeGun.Damage(); upgradeGun.RefreshUI(); } break;
                case 1: if (gunFireBtn.interactable) { upgradeGun.FireRate(); upgradeGun.RefreshUI(); } break;
                case 2: if (gunMagazBtn.interactable) { upgradeGun.Magaz(); upgradeGun.RefreshUI(); } break;
                case 3: if (gunTochBtn.interactable) { upgradeGun.Toch(); upgradeGun.RefreshUI(); } break;
                case 4: if (gunRangeBtn.interactable) { upgradeGun.Range(); upgradeGun.RefreshUI(); } break;
            }
        }
        else if (currentPanel == 1)
        {
            switch (currentBtn)
            {
                case 0: if (meleeDamageBtn.interactable) { upgradeMelee.Damage(); upgradeMelee.RefreshUI(); } break;
                case 1: if (meleeAttacksBtn.interactable) { upgradeMelee.AttacksSpeed(); upgradeMelee.RefreshUI(); } break;
                case 2: if (meleeRangeBtn.interactable) { upgradeMelee.Range(); upgradeMelee.RefreshUI(); } break;
                case 3: if (meleeX2Btn.interactable) { upgradeMelee.x2(); upgradeMelee.RefreshUI(); } break;
            }
        }
    }

    // ??? Подсветки ????????????????????????????????????????????????

    void UpdateTabHighlights()
    {
        gunTabHighlight.SetActive(currentPanel == 0);
        meleeTabHighlight.SetActive(currentPanel == 1);
        magicTabHighlight.SetActive(currentPanel == 2);
    }

    void UpdateButtonHighlight()
    {
        // Сначала гасим все
        ClearButtonHighlights();

        if (currentPanel == 0)
        {
            switch (currentBtn)
            {
                case 0: if (gunDamageBg) gunDamageBg.SetActive(true); break;
                case 1: if (gunFireBg) gunFireBg.SetActive(true); break;
                case 2: if (gunMagazBg) gunMagazBg.SetActive(true); break;
                case 3: if (gunTochBg) gunTochBg.SetActive(true); break;
                case 4: if (gunRangeBg) gunRangeBg.SetActive(true); break;
            }
        }
        else if (currentPanel == 1)
        {
            switch (currentBtn)
            {
                case 0: if (meleeDamageBg) meleeDamageBg.SetActive(true); break;
                case 1: if (meleeAttacksBg) meleeAttacksBg.SetActive(true); break;
                case 2: if (meleeRangeBg) meleeRangeBg.SetActive(true); break;
                case 3: if (meleeX2Bg) meleeX2Bg.SetActive(true); break;
            }
        }
    }

    void ClearButtonHighlights()
    {
        if (gunDamageBg) gunDamageBg.SetActive(false);
        if (gunFireBg) gunFireBg.SetActive(false);
        if (gunMagazBg) gunMagazBg.SetActive(false);
        if (gunTochBg) gunTochBg.SetActive(false);
        if (gunRangeBg) gunRangeBg.SetActive(false);

        if (meleeDamageBg) meleeDamageBg.SetActive(false);
        if (meleeAttacksBg) meleeAttacksBg.SetActive(false);
        if (meleeRangeBg) meleeRangeBg.SetActive(false);
        if (meleeX2Bg) meleeX2Bg.SetActive(false);
    }

    void ClearAllHighlights()
    {
        ClearButtonHighlights();
        gunTabHighlight.SetActive(false);
        meleeTabHighlight.SetActive(false);
        magicTabHighlight.SetActive(false);
    }

    int GetButtonCount()
    {
        if (currentPanel == 0) return 5; // gun: damage, fire, magaz, toch, range
        if (currentPanel == 1) return 4; // melee: damage, attacks, range, x2
        return 0;                         // magic пока без кнопок
    }
}