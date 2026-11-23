using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Player Weapons (objects on player)")]
    public List<GunWithTracer> playerWeapons = new List<GunWithTracer>(); // 7 слотов
    public int maxSlots = 7;

    [Header("Runtime")]
    public int currentWeaponIndex = -1;   // -1 = ничего / руки
    public HashSet<int> ownedWeaponIDs = new HashSet<int>(); // для быстрого поиска одинаковых

    [Header("UI Sync")]
    public WeaponWheelController weaponWheel;

    
    [Header("UI Sync")]
    
    public List<WeaponWheelButtonController> wheelButtons;

    public Animator anim;
    private void Start()
    {
        // выключаем все оружия при старте
        foreach (var w in playerWeapons)
            if (w != null) w.gameObject.SetActive(false);
    }

    public void Update()
    {
        // выбор оружия через колесо
        if (Input.GetKey(KeyCode.Tab))
        {

            int id = WeaponWheelController.weaponID;
            SelectWeaponByID(id);

        }

        // дроп оружия
        if (Input.GetKeyDown(KeyCode.Q))
        {
            anim.SetBool("DropWeapon", true);
            anim.SetBool("UpPistol", false);
            DropCurrentWeapon();
        }
           

    }
    

    // ----------------------------
    // ПОДБОР ОРУЖИЯ
    // ----------------------------
    public bool TryPickupWeapon(int id)
    {
        // 1. превышение слотов
        if (ownedWeaponIDs.Count >= maxSlots)
            return false;

        // 2. нельзя брать одинаковое оружие
        if (ownedWeaponIDs.Contains(id))
        {
            
            return false;
        }
            

        
       


        // 3. ищем оружие в списке playerWeapons
        foreach (var w in playerWeapons)
        {
            if (w != null && w.GunID == id)
            {
                ownedWeaponIDs.Add(id);
                RefreshWeaponWheel();
                return true;
            }
        }

        return false;
    }

    // ----------------------------
    // ВКЛЮЧЕНИЕ ОРУЖИЯ ПО ID
    // ----------------------------
    public void SelectWeaponByID(int id)
    {
        if (!ownedWeaponIDs.Contains(id)) return;

        // выключаем все
        foreach (var w in playerWeapons)
            if (w != null) w.gameObject.SetActive(false);

        // включаем нужное оружие
        for (int i = 0; i < playerWeapons.Count; i++)
        {
            if (playerWeapons[i] != null && playerWeapons[i].GunID == id)
            {
                playerWeapons[i].gameObject.SetActive(true);
                currentWeaponIndex = i;
                //playerWeapons[i].inHands = true;
                //anim.SetBool("UpPistol", true);
                
                // playerWeapons[i].Animations();
                if (weaponWheel != null)
                    weaponWheel.ApplySelectedWeapon(playerWeapons[i]);


                return;
            }
        }
    }


    // ----------------------------
    // ДРОП ОРУЖИЯ
    // ----------------------------
    public void DropCurrentWeapon()
    {
        if (currentWeaponIndex == -1) return;

        var w = playerWeapons[currentWeaponIndex];
       // playerWeapons[currentWeaponIndex].inHands = false;

        // спавн префаба дропа
        GameObject drop = Instantiate(w.dropPrefab, transform.position + transform.forward, Quaternion.identity);
        drop.GetComponent<DropWeapon>().GunID = w.GunID;

        // убираем из списка
        ownedWeaponIDs.Remove(w.GunID);
        w.gameObject.SetActive(false);

        currentWeaponIndex = -1;

        RefreshWeaponWheel();
       

    }
    public void RefreshWeaponWheel()
    {
        foreach (var button in wheelButtons)
        {
            bool hasWeapon = ownedWeaponIDs.Contains(button.Id);
            button.SetAvailable(hasWeapon);
        }
    }

    public void GunAnims()
    {

    }


}
