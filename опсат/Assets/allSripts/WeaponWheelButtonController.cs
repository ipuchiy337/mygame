using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponWheelButtonController : MonoBehaviour
{

    public int Id;
    private Animator anim;
    public string itemName;
    public TextMeshProUGUI itemText;
    public Image selectedItem;
    private bool selected = false;
    public GameObject rootObject; // сам объект слота
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private WeaponManager wm;
    void Start()
    {
        anim = GetComponent<Animator>();
        wm = FindAnyObjectByType<WeaponManager>(); // ссылка на менеджер
    }

    // Update is called once per frame
    void Update()
    {

        // если у игрока Ќ≈“ этого оружи€ Ч скрываем слот
        if (!wm.ownedWeaponIDs.Contains(Id))
        {
            rootObject.SetActive(false);
            return;
        }
        else
        {
            rootObject.SetActive(true);
        }

        // если слот выбран Ч показать данные
        if (selected)
        {
            itemText.text = itemName;
        }
    }

    public void Selected()
    {
        selected = true;
        WeaponWheelController.weaponID = Id;
    }

    public void DeSelected()
    {
        selected = false;
        WeaponWheelController.weaponID = 0;
    }

    public void HoverEnter()
    {
        anim.SetBool("Hover", true);
        itemText.text = itemName;
    }

    public void HoverExit()
    {
        anim.SetBool("Hover", false);
        itemText.text = itemName;
    }
    public void SetAvailable(bool hasWeapon)
    {
        rootObject.SetActive(hasWeapon);
    }
}
