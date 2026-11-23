using UnityEngine;
using UnityEngine.UI;
public class UpgradeMeleeWeapon : MonoBehaviour
{

    [Header("MeleeWeapon")]
    public Button damageBtn;
    public Button attacksBtn;
    public Button rangeBtn;
    public Button x2Btn;

    public Text damagePriceText;
    public Text atacksPriceText;
    public Text rangePriceText;
    public Text x2PriceText;

    public money moneyManager;
    public PauseManager pause;

  
    public GameObject pan;

    MeleeWeapon meleeweapon;
    
    public Text text;

    public void Open()
    {
        pause.Pauz();


        meleeweapon = MeleeWeaponShopManager.Instance.currentMeleeWeapon;
        text.text = " ";

        if (meleeweapon == null)
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

  
    public void RefreshUI()
    {
        meleeweapon = MeleeWeaponShopManager.Instance.currentMeleeWeapon;
        if (meleeweapon == null) { text.text = "Отсутствует оружие ближ.боя"; return; }
        text.text = " ";
        UpdateButtons();
    }
    void UpdateButtons()
    {
        UpdateButton(damageBtn, damagePriceText, meleeweapon.CanAddDamage(), meleeweapon.GetDamagePrice());
        UpdateButton(attacksBtn, atacksPriceText, meleeweapon.CanAddAtacks(), meleeweapon.GetAttackSPrice());
        UpdateButton(rangeBtn, rangePriceText, meleeweapon.CanAddRange(), meleeweapon.GetRangePrice());
        UpdateButton(x2Btn, x2PriceText, meleeweapon.CanAdd2x(), meleeweapon.Get2xPrice());
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
    public void Damage()
    {
        int price = meleeweapon.GetDamagePrice();
        if (meleeweapon.CanAddDamage() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            meleeweapon.AddDmg();
            UpdateButtons();
        }
    }
    public void AttacksSpeed()
    {
        int price = meleeweapon.GetAttackSPrice();
        if (meleeweapon.CanAddAtacks() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            meleeweapon.AddAtackS();
            UpdateButtons();
        }
    }
    public void Range()
    {
        int price = meleeweapon.GetRangePrice();
        if (meleeweapon.CanAddRange() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            meleeweapon.AddRange();
            UpdateButtons();
        }
    }
    public void x2()
    {
        int price = meleeweapon.Get2xPrice();
        if (meleeweapon.CanAdd2x() && moneyManager._money >= price)
        {
            moneyManager._money -= price;
            meleeweapon.Add2x();
            UpdateButtons();
        }
    }
}