using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Xp : MonoBehaviour
{
    public int _xp;
    public int needXp = 50;
    public int level = 1;

    public static Xp Instance;
    public PassiveUI passiveUI;

   

    
    public Text levelText;

    public Image xpBar; // XP BAR
    public float smoothXp;

    private void Update()
    {
        levelText.text = level.ToString();

        UpdateBar();
        CheckLevelUp();
    }

    void UpdateBar()
    {
        float target = (float)_xp / needXp;
        smoothXp = Mathf.Lerp(smoothXp, target, Time.deltaTime * 6f);
        xpBar.fillAmount = smoothXp;
    }

    void CheckLevelUp()
    {
        if (_xp >= needXp)
        {
            _xp -= needXp;       // остаток XP
            level++;
            needXp = Mathf.RoundToInt(50 * Mathf.Pow(1.19f, level - 1));

            // вызов окна пассивок для текущего оружия
            LevelUp();
        }
    }

    public void LevelUp()
    {
        if (passiveUI != null)
            passiveUI.Open();
        else
            Debug.LogWarning("PassiveUI не назначен в Xp!");
    }



    private void Awake()
    {
        Instance = this;
    }

    

}
