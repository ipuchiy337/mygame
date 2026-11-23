using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Повесь этот скрипт на UI GameObject — зону выброса предмета
// Также добавь на него Image и настрой внешний вид в инспекторе
public class DropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsHovering { get; private set; }

    [Header("Визуал")]
    public Image background;
    public Color normalColor = new Color(1f, 0.3f, 0.3f, 0.5f);   // красноватый
    public Color hoverColor = new Color(1f, 0.1f, 0.1f, 0.85f);   // ярче при наведении

    private void OnEnable()
    {
        IsHovering = false;
        if (background != null)
            background.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsHovering = true;
        if (background != null)
            background.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsHovering = false;
        if (background != null)
            background.color = normalColor;
    }
}