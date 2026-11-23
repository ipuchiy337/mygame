using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PassiveUI : MonoBehaviour
{
    public Button plusHpBtn;
    public Button plusDmgBtn;
    public Button otravaBtn;
    public Button uclonBtn;
    public Button plusOpitBtn;
    public Button plusMoneyBtn;
    public Button plusKritBtn;
    public Button plusRegenBtn;
    public Button plusMagnitBtn;
    public Button plusStaminaBtn;

    // Фоны-подсветки для каждой кнопки (назначь в Inspector)
    public GameObject plusHpBg;
    public GameObject plusDmgBg;
    public GameObject otravaBg;
    public GameObject uclonBg;
    public GameObject plusOpitBg;
    public GameObject plusMoneyBg;
    public GameObject plusKritBg;
    public GameObject plusRegenBg;
    public GameObject plusMagnitBg;
    public GameObject plusStaminaBg;


    private bool _pendingLeft;
    private bool _pendingRight;
    private bool _pendingConfirm;


    public UnPasivkiManager pasivkiManager;
    public PauseManager pause;
    void OnEnable()
    {
        // Сбрасываем чтобы не сработало сразу при открытии
        _pendingLeft = _pendingRight = _pendingConfirm = false;
    }
    struct PassiveOption
    {
        public Button button;
        public GameObject background;
        public System.Func<bool> canUpgrade;
        public System.Action upgrade;
    }

    // Текущие активные кнопки и индекс выбранной
    List<PassiveOption> activeOptions = new List<PassiveOption>();
    int selectedIndex = 0;

    public void Open()
    {
        pause.Pauzed();
        if (pasivkiManager == null)
        {
            Debug.LogError("pasivkiManager не назначен!");
            gameObject.SetActive(false);
            return;
        }

        // Выключаем все кнопки и фоны
        SetAllInactive();

        List<PassiveOption> allOptions = new List<PassiveOption>
        {
            new PassiveOption { button = plusHpBtn,      background = plusHpBg,      canUpgrade = CanAddPlusHp,      upgrade = AddPlusHp },
            new PassiveOption { button = plusDmgBtn,     background = plusDmgBg,     canUpgrade = CanAddPlusDmg,     upgrade = AddPlusDmg },
            new PassiveOption { button = otravaBtn,      background = otravaBg,      canUpgrade = CanAddOtrava,      upgrade = AddOtrava },
            new PassiveOption { button = uclonBtn,       background = uclonBg,       canUpgrade = CanAddUclon,       upgrade = AddUclon },
            new PassiveOption { button = plusOpitBtn,    background = plusOpitBg,    canUpgrade = CanAddPlusOpit,    upgrade = AddPlusOpit },
            new PassiveOption { button = plusMoneyBtn,   background = plusMoneyBg,   canUpgrade = CanAddPlusMoney,   upgrade = AddPlusMoney },
            new PassiveOption { button = plusKritBtn,    background = plusKritBg,    canUpgrade = CanAddPlusKrit,    upgrade = AddPlusKrit },
            new PassiveOption { button = plusRegenBtn,   background = plusRegenBg,   canUpgrade = CanAddPlusRegen,   upgrade = AddPlusRegen },
            new PassiveOption { button = plusMagnitBtn,  background = plusMagnitBg,  canUpgrade = CanAddPlusMagnit,  upgrade = AddPlusMagnit },
            new PassiveOption { button = plusStaminaBtn, background = plusStaminaBg, canUpgrade = CanAddPlusStamina, upgrade = AddPlusStamina },
        };

        List<PassiveOption> availableOptions = allOptions.Where(opt => opt.canUpgrade()).ToList();

        if (availableOptions.Count == 0)
        {
            gameObject.SetActive(false);
            Debug.Log("Все пассивки вкачаны!");
            return;
        }

        Shuffle(availableOptions);

        int countToShow = Mathf.Min(3, availableOptions.Count);
        activeOptions.Clear();

        for (int i = 0; i < countToShow; i++)
        {
            availableOptions[i].button.gameObject.SetActive(true);
            availableOptions[i].button.interactable = true;
            activeOptions.Add(availableOptions[i]);
        }

        gameObject.SetActive(true);

        // Ставим выбор на первую кнопку
        selectedIndex = 0;
        UpdateHighlight();

        StartCoroutine(AnimateButtonsIn(activeOptions.Select(o => o.button).ToList()));
    }
    void Update()
    {
        if (!gameObject.activeSelf || activeOptions.Count == 0) return;

        // Читаем один раз за кадр
        bool left = InputManager.Instance.UILeft;
        bool right = InputManager.Instance.UIRight;
        bool confirm = InputManager.Instance.UIConfirm;

        if (left)
        {
            selectedIndex = (selectedIndex - 1 + activeOptions.Count) % activeOptions.Count;
            UpdateHighlight();
        }
        else if (right)
        {
            selectedIndex = (selectedIndex + 1) % activeOptions.Count;
            UpdateHighlight();
        }

        if (confirm)
        {
            activeOptions[selectedIndex].upgrade();
            Close();
        }
    }
    void UpdateHighlight()
    {
        for (int i = 0; i < activeOptions.Count; i++)
        {
            if (activeOptions[i].background != null)
                activeOptions[i].background.SetActive(i == selectedIndex);
        }
    }

    void SetAllInactive()
    {
        var buttons = new[] { plusHpBtn, plusDmgBtn, otravaBtn, uclonBtn, plusOpitBtn, plusMoneyBtn, plusKritBtn, plusRegenBtn, plusMagnitBtn, plusStaminaBtn };
        var bgs = new[] { plusHpBg, plusDmgBg, otravaBg, uclonBg, plusOpitBg, plusMoneyBg, plusKritBg, plusRegenBg, plusMagnitBg, plusStaminaBg };

        foreach (var b in buttons) if (b != null) b.gameObject.SetActive(false);
        foreach (var bg in bgs) if (bg != null) bg.SetActive(false);
    }

    // ---------- Анимация ----------

    IEnumerator AnimateButtonsIn(List<Button> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            StartCoroutine(FadeScaleIn(buttons[i].transform, 0.25f));
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    IEnumerator FadeScaleIn(Transform target, float duration)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.gameObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        target.localScale = Vector3.one * 0.7f;
        cg.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            cg.alpha = t;
            target.localScale = Vector3.Lerp(Vector3.one * 0.7f, Vector3.one, t);
            yield return null;
        }

        cg.alpha = 1f;
        target.localScale = Vector3.one;
    }

    // ---------- Остальное ----------

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void Close()
    {
        pause.Resumed();
        gameObject.SetActive(false);
    }

    bool CanAddPlusHp() => pasivkiManager.plusHp < 20;
    bool CanAddPlusDmg() => pasivkiManager.plusDmg < 20;
    bool CanAddOtrava() => pasivkiManager.otrava < 20;
    bool CanAddUclon() => pasivkiManager.uclon < 20;
    bool CanAddPlusOpit() => pasivkiManager.plusOpit < 20;
    bool CanAddPlusMoney() => pasivkiManager.plusMoney < 20;
    bool CanAddPlusKrit() => pasivkiManager.plusKrit < 20;
    bool CanAddPlusRegen() => pasivkiManager.plusRegen < 20;
    bool CanAddPlusMagnit() => pasivkiManager.plusMagnit < 20;
    bool CanAddPlusStamina() => pasivkiManager.plusStamina < 20;

    public void PlusHpUpgrade() { AddPlusHp(); Close(); }
    public void PlusDmgUpgrade() { AddPlusDmg(); Close(); }
    public void OtravaUpgrade() { AddOtrava(); Close(); }
    public void UclonUpgrade() { AddUclon(); Close(); }
    public void PlusOpitUpgrade() { AddPlusOpit(); Close(); }
    public void PlusMoneyUpgrade() { AddPlusMoney(); Close(); }
    public void PlusKritUpgrade() { AddPlusKrit(); Close(); }
    public void PlusRegenUpgrade() { AddPlusRegen(); Close(); }
    public void PlusMagnitUpgrade() { AddPlusMagnit(); Close(); }
    public void PlusStaminaUpgrade() { AddPlusStamina(); Close(); }

    void AddPlusHp() { pasivkiManager.plusHp++; }
    void AddPlusDmg() { pasivkiManager.plusDmg++; }
    void AddOtrava() { pasivkiManager.otrava++; }
    void AddUclon() { pasivkiManager.uclon++; }
    void AddPlusOpit() { pasivkiManager.plusOpit++; }
    void AddPlusMoney() { pasivkiManager.plusMoney++; }
    void AddPlusKrit() { pasivkiManager.plusKrit++; }
    void AddPlusMagnit() { pasivkiManager.plusMagnit++; }
    void AddPlusStamina() { pasivkiManager.plusStamina++; }
    void AddPlusRegen() { pasivkiManager.plusRegen++; }
}