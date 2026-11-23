using UnityEngine;
using UnityEngine.UI;

public class GunSlotCanvas : MonoBehaviour
{
    public Image gunIcon;
    public Button gunButton;
    public Animator UIanim;
    public GameObject Light;
    public AudioSource clickSound;

    [Header("Иконка типа слота (опционально)")]
    public Image slotTypeIcon;
    public Sprite rangedSprite;
    public Sprite meleeSprite;
    public Sprite magicSprite; // <-- вот это забыли

    private int slotIndex;
    private Shop shop;
    private ShopItem currentItem;

    public void Setup(ShopItem item, int index, Shop parentShop)
    {
        slotIndex = index;
        shop = parentShop;
        currentItem = item;

        if (item.type == ShopSlotType.Ranged)
            gunIcon.sprite = item.rangedWeapon.icon;
        else if (item.type == ShopSlotType.Melee)
            gunIcon.sprite = item.meleeWeapon.icon;
        else if (item.type == ShopSlotType.Magic)
            gunIcon.sprite = item.magicWeapon.icon;

        if (slotTypeIcon != null)
        {
            slotTypeIcon.gameObject.SetActive(true);
            if (item.type == ShopSlotType.Ranged) slotTypeIcon.sprite = rangedSprite;
            else if (item.type == ShopSlotType.Melee) slotTypeIcon.sprite = meleeSprite;
            else if (item.type == ShopSlotType.Magic) slotTypeIcon.sprite = magicSprite;
        }

        gunButton.onClick.RemoveAllListeners();
        gunButton.onClick.AddListener(OnClicked);

        Debug.Log("Setup слот " + index + " тип: " + item.type);
    }
    // Добавь в GunSlotCanvas:
    public void SelectWithNavigation()
    {
        ShopCameraManager.Instance.SelectSlot(slotIndex);
        if (currentItem.type == ShopSlotType.Ranged)
            InfoCanvas.Instance.Show(currentItem.rangedWeapon, slotIndex, shop);
        else if (currentItem.type == ShopSlotType.Melee)
            InfoCanvas.Instance.ShowMelee(currentItem.meleeWeapon, slotIndex, shop);
        else if (currentItem.type == ShopSlotType.Magic)
            InfoCanvas.Instance.ShowMagic(currentItem.magicWeapon, slotIndex, shop);

        UIanim.SetTrigger("open"); // ? анимация
        clickSound.Play();          // ? звук
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetLight(bool active)
    {
        Light.SetActive(active);
    }

    private void OnClicked()
    {
        Debug.Log("Кнопка нажата слот: " + slotIndex + " тип: " + currentItem.type);

        ShopCameraManager.Instance.SelectSlot(slotIndex);

        if (currentItem.type == ShopSlotType.Ranged)
            InfoCanvas.Instance.Show(currentItem.rangedWeapon, slotIndex, shop);
        else if (currentItem.type == ShopSlotType.Melee)
            InfoCanvas.Instance.ShowMelee(currentItem.meleeWeapon, slotIndex, shop);
        else if (currentItem.type == ShopSlotType.Magic)
            InfoCanvas.Instance.ShowMagic(currentItem.magicWeapon, slotIndex, shop);

        UIanim.SetTrigger("open");
        clickSound.Play();
    }
}