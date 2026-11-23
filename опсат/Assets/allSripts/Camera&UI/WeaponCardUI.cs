using UnityEngine;
using UnityEngine.UI;

public class WeaponCardUI : MonoBehaviour
{
    public Image iconImage;
    public Image rareImage;
    public Text damageText;
    public Text fireRateText;
    public Text magazText;
    public Text priceText;

    private Weapon weapon;

    // ѕрисваиваем оружие карточке и обновл€ем UI
    public void SetWeapon(Weapon w)
    {
        weapon = w;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (weapon == null) return;

        iconImage.sprite = weapon.icon;
        rareImage.sprite = weapon.rare;
        damageText.text = weapon._damage.ToString();
        fireRateText.text = weapon._firerate.ToString();
        magazText.text = weapon._magaz.ToString();
        priceText.text = weapon.price.ToString();
    }
}
