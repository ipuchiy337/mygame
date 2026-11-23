using System;
using UnityEngine;
using UnityEngine.UI;

public class PickupCanvas : MonoBehaviour
{
    public static PickupCanvas Instance;

    public Image weaponIcon;

    public Button gunButton;

    
    private void Awake()
    {
        Instance = this;
        
    }
   
    public void PlayPickup(Weapon weapon)
    {
       
       
        weaponIcon.sprite = weapon.icon;
       
       
    }
    public void PlayPickup(MagicWeapon weapon)
    {
        weaponIcon.sprite = weapon.icon;
    }
    public void PlayPickup(MeleeWeapon weapon)
    {
        weaponIcon.sprite = weapon.icon;
    }

}
