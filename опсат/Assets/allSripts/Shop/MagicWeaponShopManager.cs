using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MagicWeaponShopManager : MonoBehaviour
{
    public static MagicWeaponShopManager Instance;

    public List<MagicWeapon> allMagicWeapons = new List<MagicWeapon>();
    public MagicWeapon currentMagicWeapon;
    public Image magicIcon;
    public Image UpImg;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        MagicWeapon[] weapons = FindObjectsOfType<MagicWeapon>(true);
        foreach (var w in weapons)
            w.gameObject.SetActive(false);
    }

    private void Start()
    {
        MagicWeapon[] weapons = FindObjectsOfType<MagicWeapon>(true);
        allMagicWeapons.AddRange(weapons);
    }

    public void EquipMagicByID(int id)
    {
        MagicWeapon found = allMagicWeapons.Find(w => w.magicID == id);
        if (found == null)
        {
            Debug.LogError("MagicWeapon with ID " + id + " not found!");
            return;
        }

        if (currentMagicWeapon != null)
            currentMagicWeapon.gameObject.SetActive(false);

        currentMagicWeapon = found;
        currentMagicWeapon.gameObject.SetActive(true);

        if (magicIcon != null)
            magicIcon.sprite = currentMagicWeapon.icon;
        UpImg.sprite = currentMagicWeapon.icon;
    }

    public bool HasMagicWeapon(int id)
    {
        return currentMagicWeapon != null && currentMagicWeapon.magicID == id;
    }
}

