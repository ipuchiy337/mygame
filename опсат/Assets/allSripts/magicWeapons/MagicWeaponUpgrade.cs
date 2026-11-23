using UnityEngine;
using UnityEngine.UI;

public class MagicWeaponUpgrade : MonoBehaviour
{

    [Header("MeleeWeapon")]
   // public Button damageBtn;
   // public Button attacksBtn;
   // public Button rangeBtn;
  //  public Button x2Btn;

 //   public Text damagePriceText;
 //   public Text atacksPriceText;
//    public Text x2PriceText;

    public money moneyManager;
    public PauseManager pause;


    public GameObject panAura;

    MagicWeapon magicweapon;


    [Header("Aura")]
    public GameObject _aura;
    public AuraDamage _auravis;

    public Text text;

    public void Open()
    {
       


        magicweapon = MagicWeaponShopManager.Instance.currentMagicWeapon;
        text.text = " ";

        if (magicweapon == null)
        {
            text.text = "Отсутсвует магическое оружие";
            gameObject.SetActive(false);
            return;

        }
        if (!_aura.activeInHierarchy)
        {
            panAura.SetActive(false);
            Debug.Log("не назначен");
        }
        else
        {
            Debug.Log("назначен");
            panAura.SetActive(true);
        }

       if (moneyManager == null)
        {
            Debug.LogError("moneyManager не назначен в Inspector!");
            gameObject.SetActive(false);
            return;
        }



    }

    public void Close()
    {
        pause.Rezume();
        panAura.SetActive(false);

        gameObject.SetActive(false);

    }
}