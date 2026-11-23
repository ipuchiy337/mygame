using System.Collections.Generic;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;

    public List<WeaponCardUI> cards;
    public Animator anim;

    public bool isOpen = false;
    public GameObject stop;


    public PauseManager pause;

    private void Awake()
    {
        Instance = this;
    }
    public void ShowPanel()
    {
        
        stop.gameObject.SetActive(true);
       
        pause.Pauz();
    }

   

    public void ClosePanel()
    {
        isOpen = false;

        pause.Rezume();
        stop.gameObject.SetActive(false);

       // if (Shop.CurrentShop != null)
          //  Shop.CurrentShop.anim.SetTrigger("close");






        //anim.SetTrigger("down");


    }

    public void UpdateCards(List<Weapon> weapons)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            cards[i].SetWeapon(weapons[i]);
        }
    }

    public void Buy(int index)
    {
        Shop.CurrentShop.BuyItem(index);
        Shop.CurrentShop = null;
        //anim.SetTrigger("down");
        ClosePanel();



    }
    
    
    

  

}