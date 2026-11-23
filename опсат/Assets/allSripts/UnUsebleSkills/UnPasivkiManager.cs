using UnityEngine;
using UnityEngine.UI;

public class UnPasivkiManager : MonoBehaviour
{
    public float plusHp;
    public float plusDmg;
    public float otrava;
    public float uclon;
    public float plusOpit;
    public float plusMoney;
    public float plusKrit;

    public float plusStamina;
    public float plusMagnit;
    public float plusRegen;

    public Text PlusHp;
    public Text PlusDmg;
    public Text OTRava;
    public Text UNclon;
    public Text PLUZOpit;
    public Text PLUZMoney;
    public Text PLUZKrit;

    public Text PLUZSTAMINA;
    public Text PLUZMAGNIT;
    public Text PLUZREGEN;

    public GameObject _plushp;
    public GameObject _plusdmg;
    public GameObject _otrava;
    public GameObject _uclon;
    public GameObject _opit;
    public GameObject _Money;
    public GameObject _Krit;

    public GameObject _Stamina;
    public GameObject _Magnit;
    public GameObject _Regen;

    void Update()
    {
        UpdateSkillUI(plusHp, PlusHp, _plushp);
        UpdateSkillUI(plusDmg, PlusDmg, _plusdmg);
        UpdateSkillUI(otrava, OTRava, _otrava);
        UpdateSkillUI(uclon, UNclon, _uclon);
        UpdateSkillUI(plusOpit, PLUZOpit, _opit);
        UpdateSkillUI(plusMoney, PLUZMoney, _Money);
        UpdateSkillUI(plusKrit, PLUZKrit, _Krit);

        UpdateSkillUI(plusStamina, PLUZSTAMINA, _Stamina);
        UpdateSkillUI(plusMagnit, PLUZMAGNIT, _Magnit);
        UpdateSkillUI(plusRegen, PLUZREGEN, _Regen);
    }

    void UpdateSkillUI(float value, Text txt, GameObject obj)
    {
        if (obj != null)
            obj.SetActive(value > 0); // включаем если больше 0

        if (txt != null)
            txt.text = (value > 1) ? Mathf.FloorToInt(value).ToString() : ""; // текст только если > 1
    }
}
