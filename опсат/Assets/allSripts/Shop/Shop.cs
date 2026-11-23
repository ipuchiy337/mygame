using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public static Shop CurrentShop;

    public List<ShopItem> shopItems = new List<ShopItem>();

    private bool initialized = false;
    private bool playerInZone = false;
    private bool sold = false;

    public Animator shopAnimator;
    public GameObject CloseAnim;
    public GameObject CloseShop;
    public AudioSource ShopClose;

    public GameObject victoryLight;
    public GameObject greenLigth;
    public GameObject redLight;
    public float victoryLightDuration = 2f;
   
    private void Start()
    {
        //GenerateItems();
    }

    private void Update()
    {
        if (sold) return;
        if (ShopCameraManager.Instance == null) return;

        if (playerInZone && (InputManager.Instance.Interact))
        {
            if (!initialized)
                GenerateItems();

            ShopCameraManager.Instance.OpenShop(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        CurrentShop = this;
        playerInZone = true;
        shopAnimator?.SetBool("isOpen", true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (CurrentShop == this) CurrentShop = null;
        playerInZone = false;
        ShopCameraManager.Instance?.CloseShop();
        shopAnimator?.SetBool("isOpen", false);
    }

    private void GenerateItems()
    {
        if (initialized || ShopManager.Instance == null) return;

        shopItems.Clear();
        shopItems.AddRange(ShopManager.Instance.GetShopItems());

        initialized = true;
    }

    // Вызывается из UI — index 0, 1 или 2
    public void BuyItem(int index)
    {
        if (sold) return;
        if (index >= shopItems.Count) return;

        ShopItem item = shopItems[index];

        if (item.type == ShopSlotType.Ranged)
            WeaponShopManager.Instance.EquipWeaponByID(item.rangedWeapon.weaponID);
        else if (item.type == ShopSlotType.Melee)
            MeleeWeaponShopManager.Instance.EquipMeleeByID(item.meleeWeapon.meleeID);
        else if (item.type == ShopSlotType.Magic)
            MagicWeaponShopManager.Instance.EquipMagicByID(item.magicWeapon.magicID);

        CloseShopAfterPurchase();
    }

    private void CloseShopAfterPurchase()
    {
        sold = true;

        InfoCanvas.Instance.Buy();

        CloseAnim.SetActive(false);
        CloseShop.SetActive(true);

        shopAnimator?.SetBool("isOpen", false);

        CurrentShop = null;
        playerInZone = false;

        redLight.SetActive(false);
        greenLigth.SetActive(true);

        ShopClose.Play();

        if (victoryLight != null)
            StartCoroutine(VictoryLightRoutine());
    }

    private System.Collections.IEnumerator VictoryLightRoutine()
    {
        victoryLight.SetActive(true);
        StartCoroutine(FlickerRoutine());
        StartCoroutine(RotateRoutine());
        yield return new WaitForSeconds(victoryLightDuration);
        victoryLight.SetActive(false);
    }

    private System.Collections.IEnumerator FlickerRoutine()
    {
        Light light = victoryLight.GetComponent<Light>();
        float elapsed = 0f;
        while (elapsed < victoryLightDuration)
        {
            light.enabled = !light.enabled;
            elapsed += 0.15f;
            yield return new WaitForSeconds(0.15f);
        }
        light.enabled = false;
    }

    private System.Collections.IEnumerator RotateRoutine()
    {
        float elapsed = 0f;
        float rotationSpeed = 360f / 2f;
        while (elapsed < victoryLightDuration)
        {
            victoryLight.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}