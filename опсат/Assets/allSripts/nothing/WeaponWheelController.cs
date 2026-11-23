using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponWheelController : MonoBehaviour
{
    public Animator anim;
    private bool weaponWheelSelected = false;
    public Image selectedItem;
    public Sprite noImage;
    public static int weaponID;
    
    void Update()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            weaponWheelSelected = true;
        }
        else
        {
            weaponWheelSelected = false;
        }

        if (weaponWheelSelected)
        {
            anim.SetBool("OpenWeaponWheel", true);
        }
        else
        {
            anim.SetBool("OpenWeaponWheel", false);
        }

        switch (weaponID)
        {
            case 0:
                selectedItem.sprite = noImage;
                break;
            case 1:
                
                break;
            case 2:
                
                break;
            case 3:
                
                break;
            case 4:
                
                break;
            case 5:
                
                break;
            case 6:
                
                break;
            case 7:
                
                break;
        }
    }
    public void ApplySelectedWeapon(GunWithTracer gun)
    {
        selectedItem.sprite = gun.Icon;
    }
   


}
