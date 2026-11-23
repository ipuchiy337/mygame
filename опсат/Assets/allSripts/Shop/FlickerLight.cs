using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    [Header("Настройки мигания")]
    public Light flickerLight;
    public AudioSource audioSource;
    public AudioClip flickerSound;

    [Header("Интервалы")]
    public float minFlickerTime = 0.05f;
    public float maxFlickerTime = 0.2f;
    public float minStableTime = 0.5f;   // как долго лампа горит стабильно
    public float maxStableTime = 3f;
    public float minOffTime = 0.1f;      // как долго лампа не горит
    public float maxOffTime = 0.5f;

    private bool isFlickering = false;

    private void Start()
    {
        StartCoroutine(FlickerRoutine());
    }

    private System.Collections.IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Лампа стабильно горит
            flickerLight.enabled = true;
            yield return new WaitForSeconds(Random.Range(minStableTime, maxStableTime));

            // Решаем — просто моргнёт или будет долго барахлить
            int flickerCount = Random.Range(1, 8);
            isFlickering = flickerCount > 3;

            for (int i = 0; i < flickerCount; i++)
            {
                // Выключаем
                flickerLight.enabled = false;
                PlaySound();
                yield return new WaitForSeconds(Random.Range(minFlickerTime, maxFlickerTime));

                // Включаем
                flickerLight.enabled = true;
                PlaySound();
                yield return new WaitForSeconds(Random.Range(minFlickerTime, maxFlickerTime));
            }

            // Иногда лампа надолго гаснет
            if (Random.value < 0.2f)
            {
                flickerLight.enabled = false;
                PlaySound();
                yield return new WaitForSeconds(Random.Range(minOffTime, maxOffTime));
            }
        }
    }

    private void PlaySound()
    {
        if (audioSource != null && flickerSound != null)
        {
            audioSource.pitch = Random.Range(0.8f, 1.2f); // чуть рандомный питч
            audioSource.PlayOneShot(flickerSound);
        }
    }
}