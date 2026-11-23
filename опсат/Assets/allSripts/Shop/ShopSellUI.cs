using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Магазин продажи предметов.
/// 
/// КАК ПОДКЛЮЧИТЬ:
/// 1. Создай панель магазина на Canvas (GameObject ? UI ? Panel).
/// 2. Повесь этот скрипт на любой GameObject (например, на саму панель или пустой объект ShopManager).
/// 3. В Inspector заполни все поля (см. описания ниже).
/// 4. В скрипте itemSell (открытие магазина) вместо pan.SetActive(true) вызови ShopSellUI.Instance.OpenShop().
/// 5. SellSlot — создай отдельный Slot-объект на панели магазина (отдельно от инвентаря!).
///    Это обычный GameObject с компонентом Slot, Image (iconImage) и TextMeshProUGUI (amountTxt).
/// </summary>
public class ShopSellUI : MonoBehaviour
{
    public static ShopSellUI Instance { get; private set; }

    [Header("Ссылки")]
    [Tooltip("Панель магазина (та же что pan в itemSell)")]
    public GameObject shopPanel;

    [Tooltip("Инвентарь игрока")]
    public Inventory inventory;

    [Tooltip("Деньги игрока")]
    public money moneyManager;

    [Tooltip("Слот куда игрок перетаскивает предмет для продажи")]
    public Slot sellSlot;

    [Header("Комиссия")]
    [Tooltip("Комиссия магазина от 0 до 1. Например 0.25 = магазин берёт 25%, игрок получает 75%")]
    [Range(0f, 1f)]
    public float commission = 0.25f;

    [Header("UI — текст")]
    [Tooltip("Текст: базовая стоимость предмета в слоте. Пример: 'Стоимость: 100'")]
    public TextMeshProUGUI itemCostText;

    [Tooltip("Текст: размер комиссии в процентах. Пример: 'Комиссия: 25%'")]
    public TextMeshProUGUI commissionText;

    [Tooltip("Текст: итоговая сумма которую получит игрок. Пример: 'Вы получите: 75'")]
    public TextMeshProUGUI finalPriceText;

    [Tooltip("Текст: итог при продаже всего инвентаря. Пример: 'Продать всё за: 300'")]
    public TextMeshProUGUI sellAllPreviewText;

    [Header("Кнопки")]
    [Tooltip("Кнопка 'Продать всё' — продаёт весь инвентарь сразу")]
    public Button sellAllButton;

    [Header("PauseManager")]
    public PauseManager pauseManager;

    // ????????????????????????????????????????????????????????????

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Подписываемся на кнопку
        if (sellAllButton != null)
            sellAllButton.onClick.AddListener(SellAll);

        UpdateCommissionLabel();

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void Update()
    {
        if (shopPanel != null && shopPanel.activeSelf)
        {
            UpdateSlotPriceUI();
            UpdateSellAllPreview();

            if (Input.GetKeyDown(KeyCode.Escape))
                CloseShop();
        }
    }

    // ???????????????????????????????????????????????????????????????
    //  ОТКРЫТИЕ / ЗАКРЫТИЕ
    // ???????????????????????????????????????????????????????????????

    public void OpenShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);

        UpdateCommissionLabel();
        UpdateSlotPriceUI();
        UpdateSellAllPreview();

        
    }

    public void CloseShop()
    {
        // Возвращаем предмет из слота продажи обратно в инвентарь
        ReturnSellSlotToInventory();


    }

    // ???????????????????????????????????????????????????????????????
    //  ОСНОВНАЯ ЛОГИКА
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Вызывается каждый кадр.
    /// Если в слоте продажи появился предмет — продаём 1 штуку и очищаем слот.
    /// Drag&Drop из инвентаря сам поместит предмет в sellSlot через стандартный HandleDrop.
    /// </summary>
    public void SellOneFromSlot(Slot fromSlot)
    {
        if (fromSlot == null || !fromSlot.HasItem()) return;

        ItemSO item = fromSlot.GetItem();
        int earned = CalculateEarnings(item.cost, 1);
        moneyManager.AddMoney(earned);

        Debug.Log($"Продано: {item.itemName} x1 за {earned}");

        fromSlot.RemoveAmount(1); // убираем 1 штуку из инвентаря

        UpdateSlotPriceUI();
        UpdateSellAllPreview();
    }

    /// <summary>
    /// Кнопка "Продать всё" — продаёт весь инвентарь сразу.
    /// </summary>
    public void SellAll()
    {
        // Сначала возвращаем предмет из слота продажи если там что-то есть
        ReturnSellSlotToInventory();

        int totalBase = inventory.GetTotalInventoryCost();
        int totalEarned = CalculateEarnings(totalBase, 1); // комиссия уже на всю сумму

        if (totalEarned <= 0)
        {
            Debug.Log("Инвентарь пуст, продавать нечего.");
            return;
        }

        moneyManager.AddMoney(totalEarned);
        inventory.ClearAllSlots();

        Debug.Log($"Продан весь инвентарь за {totalEarned} (до комиссии: {totalBase})");

        UpdateSlotPriceUI();
        UpdateSellAllPreview();
    }

    // ???????????????????????????????????????????????????????????????
    //  UI
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Обновляет тексты цены/комиссии для предмета в слоте продажи.
    /// </summary>
    private void UpdateSlotPriceUI()
    {
        if (sellSlot != null && sellSlot.HasItem())
        {
            ItemSO item = sellSlot.GetItem();
            int baseCost = item.cost;                          // цена 1 штуки
            int earned = CalculateEarnings(baseCost, 1);       // что получит игрок
            int commissionAmount = baseCost - earned;          // сколько берёт магазин

            if (itemCostText != null)
                itemCostText.text = $"Стоимость: {baseCost}";

            if (commissionText != null)
                commissionText.text = $"Комиссия: {Mathf.RoundToInt(commission * 100)}% (-{commissionAmount})";

            if (finalPriceText != null)
                finalPriceText.text = $"Вы получите: {earned}";
        }
        else
        {
            if (itemCostText != null)
                itemCostText.text = "Стоимость: —";

            if (commissionText != null)
                commissionText.text = $"Комиссия: {Mathf.RoundToInt(commission * 100)}%";

            if (finalPriceText != null)
                finalPriceText.text = "Вы получите: —";
        }
    }

    /// <summary>
    /// Обновляет превью суммы "Продать всё".
    /// </summary>
    private void UpdateSellAllPreview()
    {
        if (sellAllPreviewText == null) return;

        int totalBase = inventory.GetTotalInventoryCost();
        int totalEarned = CalculateEarnings(totalBase, 1);
        sellAllPreviewText.text = $"Продать всё за: {totalEarned}";
    }

    /// <summary>
    /// Показывает процент комиссии в отдельном лейбле (можно поставить статичный текст в магазине).
    /// </summary>
    private void UpdateCommissionLabel()
    {
        if (commissionText != null)
            commissionText.text = $"Комиссия: {Mathf.RoundToInt(commission * 100)}%";
    }

    // ???????????????????????????????????????????????????????????????
    //  УТИЛИТЫ
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Считает сколько игрок получит с учётом комиссии.
    /// multiplier = количество (для одного предмета = 1, для суммы инвентаря передаём уже готовую сумму).
    /// </summary>
    private int CalculateEarnings(int baseValue, int multiplier)
    {
        float raw = baseValue * multiplier * (1f - commission);
        return Mathf.RoundToInt(raw);
    }

    /// <summary>
    /// Если в слоте продажи остался предмет при закрытии — возвращаем в инвентарь.
    /// </summary>
    private void ReturnSellSlotToInventory()
    {
        if (sellSlot == null || !sellSlot.HasItem()) return;

        inventory.AddItem(sellSlot.GetItem(), sellSlot.GetAmount());
        sellSlot.ClearSlot();
    }
}