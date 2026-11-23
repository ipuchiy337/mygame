using UnityEngine;
using UnityEngine.UI;

public class ButtonIconSwapper : MonoBehaviour
{
    public Sprite keyboardIcon;  // например картинка "E"
    public Sprite gamepadIcon;   // например картинка "A (Xbox)"

    Image img;

    void Awake()
    {
        img = GetComponent<Image>();
    }

    void OnEnable()
    {
        DeviceDetector.OnDeviceChanged += UpdateIcon;
        UpdateIcon(DeviceDetector.IsGamepad); // сразу применить
    }

    void OnDisable()
    {
        DeviceDetector.OnDeviceChanged -= UpdateIcon;
    }

    void UpdateIcon(bool isGamepad)
    {
        if (img == null) return;
        img.sprite = isGamepad ? gamepadIcon : keyboardIcon;
    }
}