using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    public Health health;

    public Image HpBack;
    public Image hpImage;

    private Camera cam;

    public Text _curHp;
    public Text _maxHp;

    public float visibleAlpha = 0.7f;
    public float fadeSpeed = 5f;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        // HP bar
        hpImage.fillAmount = health.hp / health.maxHp;

        // numbers
        _curHp.text = Mathf.CeilToInt(health.hp).ToString();
        _maxHp.text = Mathf.CeilToInt(health.maxHp).ToString();

        // billboard
        Vector3 dir = cam.transform.position - transform.position;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        // целевая прозрачность
        float targetAlpha = health.hp < health.maxHp ? visibleAlpha : 0f;

        // HP fill
        Color hpColor = hpImage.color;
        hpColor.a = Mathf.Lerp(hpColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        hpImage.color = hpColor;

        // BACKGROUND
        Color backColor = HpBack.color;
        backColor.a = Mathf.Lerp(backColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        HpBack.color = backColor;

        // CURRENT HP TEXT
        Color curHpColor = _curHp.color;
        curHpColor.a = Mathf.Lerp(curHpColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        _curHp.color = curHpColor;

        // MAX HP TEXT
        Color maxHpColor = _maxHp.color;
        maxHpColor.a = Mathf.Lerp(maxHpColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        _maxHp.color = maxHpColor;
    }
}

