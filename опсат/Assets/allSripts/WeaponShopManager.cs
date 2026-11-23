using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class WeaponShopManager : MonoBehaviour
{
    public static WeaponShopManager Instance;

    public List<Weapon> allWeapons = new List<Weapon>();
    public Weapon currentWeapon;
    public Image img;
    public Image UpImg;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Weapon[] weapons = FindObjectsOfType<Weapon>(true);
        foreach (var w in weapons)
            w.gameObject.SetActive(false);
    }

    private void Start()
    {
        Weapon[] weapons = FindObjectsOfType<Weapon>(true);
        allWeapons.AddRange(weapons);
    }

    public void EquipWeaponByID(int id)
    {
        Weapon newWeapon = allWeapons.Find(w => w.weaponID == id);
        if (newWeapon == null)
        {
            Debug.LogError("Weapon with ID " + id + " not found!");
            return;
        }

        if (currentWeapon != null)
            currentWeapon.gameObject.SetActive(false);

        currentWeapon = newWeapon;
        currentWeapon.gameObject.SetActive(true);
        img.sprite = currentWeapon.icon;
        UpImg.sprite = currentWeapon.icon;
}

    public bool HasWeapon(int id)
    {
        return currentWeapon != null && currentWeapon.weaponID == id;
    }
}