using UnityEngine;

public class FlashLight : MonoBehaviour
{
    public Light flashingLight;

    [Header("Ќастройки мерцани€")]
    public float minIntensity = 0f;    // минимум €ркости (выкл)
    public float maxIntensity = 1f;    // максимум €ркости (вкл)
    public float speed = 2f;           // скорость перехода

    private bool increasing = true;    // растет ли €ркость

    private void Start()
    {
        if (flashingLight == null)
            flashingLight = GetComponent<Light>();

        flashingLight.intensity = minIntensity;
    }

    private void Update()
    {
        if (increasing)
        {
            // плавно увеличиваем €ркость
            flashingLight.intensity = Mathf.MoveTowards(
                flashingLight.intensity,
                maxIntensity,
                Time.deltaTime * speed
            );

            if (Mathf.Approximately(flashingLight.intensity, maxIntensity))
                increasing = false;
        }
        else
        {
            // плавно уменьшаем €ркость
            flashingLight.intensity = Mathf.MoveTowards(
                flashingLight.intensity,
                minIntensity,
                Time.deltaTime * speed
            );

            if (Mathf.Approximately(flashingLight.intensity, minIntensity))
                increasing = true;
        }
    }
}
