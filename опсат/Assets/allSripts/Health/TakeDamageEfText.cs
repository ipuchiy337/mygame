using UnityEngine;
using UnityEngine.UI;

public class TakeDamageEfText : MonoBehaviour
{
    public Text myText;

    [Header("Настройки эффекта")]
    public float lifetime = 0.5f;
    public float moveSpeed = 2f;
    public float shakeIntensity = 0.1f;

    private Vector3 randomDir;
    private Color textColor;
    private RectTransform textRect; // ? Для изменения размера текста
    private float timer = 0f;

    public void Setup(float damageValue)
    {
        if (myText == null)
        {
            Debug.LogError("myText НЕ НАЗНАЧЕН!");
            return;
        }

        myText.text = "-" + damageValue.ToString("F0");
        textColor = myText.color;
        textRect = myText.GetComponent<RectTransform>(); // ? Получаем RectTransform текста

        randomDir = new Vector3(Random.Range(-1f, 1f), 1f, 0).normalized;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 1. Движение вверх
        transform.position += randomDir * moveSpeed * Time.deltaTime;

        // 2. Эффект тряски
        Vector3 shakeOffset = new Vector3(
            Random.Range(-shakeIntensity, shakeIntensity),
            Random.Range(-shakeIntensity, shakeIntensity),
            0
        );
        transform.position += shakeOffset;

        // 3. Плавное исчезновение
        textColor.a = 1f - (timer / lifetime);
        myText.color = textColor;

        // 4. ? Pop-up эффект (только для текста!)
        if (timer < 0.15f && textRect != null)
        {
            float scale = Mathf.Lerp(1f, 1.3f, timer / 0.15f);
            textRect.localScale = Vector3.one * scale;
        }
    }
}