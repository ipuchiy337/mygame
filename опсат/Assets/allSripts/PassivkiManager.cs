using UnityEngine;
using UnityEngine.UI;

public class PassivkiManager : MonoBehaviour
{
    public float regen = 0f;
    public float shit = 0f;
    public float raketamet = 0f; 
    public float mina = 0f;
    public float apteka = 0f;
    public float sprint = 0f;
    public float magnit = 0f;

    public Text Aregen;
    public Text Ashit;
    public Text Araketamet;
    public Text Amina;
    public Text Aapteka;
    public Text Asprint;
    public Text Amagnit;

    public GameObject _Aregen;
    public GameObject _Ashit;
    public GameObject _Araketamet;
   
    public GameObject _Amina;
    public GameObject _Aapteka;
    public GameObject _Asprint;
    public GameObject _Amagnit;

    void Update()
    {
        UpdateSkillUI(regen, _Aregen, Aregen);
        UpdateSkillUI(shit, _Ashit, Ashit);
        UpdateSkillUI(raketamet, _Araketamet, Araketamet);
        
        UpdateSkillUI(mina, _Amina, Amina);
        UpdateSkillUI(apteka, _Aapteka, Aapteka);

       UpdateSkillUI(sprint, _Asprint, Asprint);
        UpdateSkillUI(magnit, _Amagnit, Amagnit);
    }

    void UpdateSkillUI(float value, GameObject obj, Text txt)
    {
        if (obj != null)
            obj.SetActive(value > 0); // включаем если больше 0

        if (txt != null)
            txt.text = (value > 1) ? Mathf.FloorToInt(value).ToString() : ""; // текст только если > 1
    }
}
