using UnityEngine;
using System.Collections;

public class ShopCameraManager : MonoBehaviour
{
    public static ShopCameraManager Instance;

    [Header("Весь UI магаза (вкл/выкл)")]
    public GameObject shopView;
    public GameObject maincam;
    public GameObject dirLight;
    public GameObject GunUI;
    public Animator shopAnim;
    public GameObject CloseBut;

    [Header("3 слота — дочерние объекты ShopView")]
    public GunSlotCanvas[] gunSlots;

    public bool isOpen = false;

    private int selectedSlot = 0;
    private Shop currentShop;

    // Полная блокировка управления внутри этого менеджера
    private bool inputLocked = false;

    private void Awake()
    {
        Instance = this;
        shopView.SetActive(false);
    }

    public void OpenShop(Shop shop)
    {
        if (isOpen) return;

        isOpen = true;
        currentShop = shop;

        CloseBut.SetActive(true);

        shopAnim.SetTrigger("open");

        shopView.SetActive(true);
        maincam.SetActive(false);
        dirLight.SetActive(false);

        Time.timeScale = 0f;

        for (int i = 0; i < gunSlots.Length; i++)
        {
            if (i < shop.shopItems.Count)
            {
                gunSlots[i].Setup(shop.shopItems[i], i, shop);
                gunSlots[i].Show();
            }
            else
            {
                gunSlots[i].Hide();
            }
        }

        ShowInfoForSlot(1);

        // После открытия магазина блокируем управление на 1.8 секунды
        StartCoroutine(LockInputForSeconds(1.8f));
    }

    private void Update()
    {
        if (!isOpen)
            return;

        // Пока заблокировано — никакие кнопки магазина не работают
        if (inputLocked)
            return;

        if (InputManager.Instance.UIUp)
        {
            int activeCount = Mathf.Min(
                gunSlots.Length,
                currentShop.shopItems.Count
            );

            if (activeCount > 0)
            {
                selectedSlot =
                    (selectedSlot - 1 + activeCount) % activeCount;

                ShowInfoForSlot(selectedSlot);
            }
        }

        if (InputManager.Instance.UIDown)
        {
            int activeCount = Mathf.Min(
                gunSlots.Length,
                currentShop.shopItems.Count
            );

            if (activeCount > 0)
            {
                selectedSlot =
                    (selectedSlot + 1) % activeCount;

                ShowInfoForSlot(selectedSlot);
            }
        }

        if (InputManager.Instance.UIConfirm)
        {
            InfoCanvas.Instance.Buy();
        }

        if (InputManager.Instance.UIBack)
        {
            InfoCanvas.Instance.Hide();
        }
    }

    private void ShowInfoForSlot(int index)
    {
        if (currentShop == null)
            return;

        if (index < 0 || index >= currentShop.shopItems.Count)
            return;

        gunSlots[index].SelectWithNavigation();
    }

    public void SelectSlot(int index)
    {
        selectedSlot = index;

        for (int i = 0; i < gunSlots.Length; i++)
        {
            gunSlots[i].SetLight(i == index);
        }
    }

    public void CloseShop()
    {
        if (!isOpen)
            return;

        isOpen = false;

        shopView.SetActive(false);
        maincam.SetActive(true);
        dirLight.SetActive(true);

        Time.timeScale = 1f;

        // Возвращаем управление
        inputLocked = false;
    }

    public void CloseShopAfterBuy()
    {
        // Полностью блокируем управление
        inputLocked = true;

        CloseBut.SetActive(false);

        StartCoroutine(CloseAfterDelay(5.1f));
    }

    public void CloseShopNoBuy()
    {
        // Полностью блокируем управление
        inputLocked = true;

        CloseBut.SetActive(false);

        StartCoroutine(CloseAfterDelay(1.8f));
    }

    private IEnumerator LockInputForSeconds(float seconds)
    {
        inputLocked = true;

        // Используем realtime, потому что Time.timeScale = 0
        yield return new WaitForSecondsRealtime(seconds);

        inputLocked = false;
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        CloseShop();
    }
}