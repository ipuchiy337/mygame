using UnityEngine;
public class CanvasGun : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        transform.LookAt(cam.transform);
        // Разворачиваем на 180 градусов — иначе объект смотрит "спиной"
        transform.Rotate(0, 180f, 0);
    }
}