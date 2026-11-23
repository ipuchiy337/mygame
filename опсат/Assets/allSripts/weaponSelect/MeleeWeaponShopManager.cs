using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MeleeWeaponShopManager : MonoBehaviour
{
    public static MeleeWeaponShopManager Instance;
    public Image UpImg;
    public List<MeleeWeapon> allMeleeWeapons = new List<MeleeWeapon>();
    public MeleeWeapon currentMeleeWeapon;
    public Image meleeIcon;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        MeleeWeapon[] weapons = FindObjectsOfType<MeleeWeapon>(true);
        foreach (var w in weapons)
            w.gameObject.SetActive(false);
    }

    private void Start()
    {
        MeleeWeapon[] weapons = FindObjectsOfType<MeleeWeapon>(true);
        allMeleeWeapons.AddRange(weapons);
    }

    public void EquipMeleeByID(int id)
    {
        MeleeWeapon found = allMeleeWeapons.Find(w => w.meleeID == id);
        if (found == null)
        {
            Debug.LogError("MeleeWeapon with ID " + id + " not found!");
            return;
        }

        if (currentMeleeWeapon != null)
            currentMeleeWeapon.gameObject.SetActive(false);

        currentMeleeWeapon = found;
        currentMeleeWeapon.gameObject.SetActive(true);

        if (meleeIcon != null)
            meleeIcon.sprite = currentMeleeWeapon.icon;
         UpImg.sprite = currentMeleeWeapon.icon;
    }

    public bool HasMeleeWeapon(int id)
    {
        return currentMeleeWeapon != null && currentMeleeWeapon.meleeID == id;
    }
}