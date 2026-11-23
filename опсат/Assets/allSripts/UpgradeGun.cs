using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeGun : MonoBehaviour
{
    [Header("GunUpgrade")]
    public Button damageBtn;
    public Button fireBtn;
    public Button magazBtn;
    public Button tochBtn;
    public Button rangeBtn;
 

    public Text damagePriceText;
    public Text firePriceText;
    public Text magazPriceText;
    public Text tochPriceText;
    public Text rangePriceText;


    public money moneyManager;
    public PauseManager pause;
    Weapon weapon;
 
    public GameObject pan;

    public Text text;

    public void Open()
    {
       

        weapon = WeaponShopManager.Instance.currentWeapon;
        text.text = " ";

        if (weapon == null)
        {
            
            return;
        }
    

        if (moneyManager == null)
        {
            Debug.LogError("moneyManager не назначен в Inspector!");
            gameObject.SetActive(false);
            return;
        }

        UpdateButtons();
       
        
      
    }
    
   

    void UpdateButtons()
    {
        UpdateButton(damageBtn, damagePriceText, weapon.CanAddDamage(), weapon.GetDamagePrice());
        UpdateButton(fireBtn, firePriceText, weapon.CanAddFireRate(), weapon.GetFireRatePrice());
        UpdateButton(magazBtn, magazPriceText, weapon.CanAddMagaz(), weapon.GetMagazPrice());
        UpdateButton(tochBtn, tochPriceText, weapon.CanAddToch(), weapon.GetTochPrice());
        UpdateButton(rangeBtn, rangePriceText, weapon.CanAddRange(), weapon.GetRangePrice());
    
    }

    void UpdateButton(Button btn, Text priceText, bool canUpgrade, int price)
    {
        if (btn == null) return;

        bool hasEnoughMoney = moneyManager._money >= price;
        btn.interactable = canUpgrade && hasEnoughMoney;

        if (priceText != null)
        {
            if (canUpgrade)
            {
                priceText.text = price + "$";
                priceText.color = hasEnoughMoney ? Color.green : Color.red;
            }
            else
            {
                priceText.text = "MAX";
                priceText.color = Color.red;
            }
        }
    }
    public void RefreshUI()
    {
        weapon = WeaponShopManager.Instance.currentWeapon;
        if (weapon == null) { text.text = "Отсутствует огнестрел"; return; }
        text.text = " ";
        UpdateButtons();
    }
    public void Damage()
    {
        int price = weapon.GetDamagePrice();
        if (weapon.CanAddDamage() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            weapon.AddDamagePassive();
            UpdateButtons();
        }
    }

    public void FireRate()
    {
        int price = weapon.GetFireRatePrice();
        if (weapon.CanAddFireRate() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            weapon.AddFireRatePassive();
            UpdateButtons();
        }
    }

    public void Magaz()
    {
        int price = weapon.GetMagazPrice();
        if (weapon.CanAddMagaz() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            weapon.AddMagazPassive();
            UpdateButtons();
        }
    }

    public void Toch()
    {
        int price = weapon.GetTochPrice();
        if (weapon.CanAddToch() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            weapon.AddTochnostPassive();
            UpdateButtons();
        }
    }

    public void Range()
    {
        int price = weapon.GetRangePrice();
        if (weapon.CanAddRange() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            weapon.AddRangePassive();
            UpdateButtons();
        }
    }


}