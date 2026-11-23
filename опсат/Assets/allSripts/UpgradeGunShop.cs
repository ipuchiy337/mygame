using UnityEngine;

public class UpgradeGunShop : MonoBehaviour
{
    public Animator anim;
    public GameObject nadpis;

    private bool inTrigger = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            anim.SetBool("isOpen", true);
            inTrigger = true;
            nadpis.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            anim.SetBool("isOpen", false);
            inTrigger = false;
            nadpis.SetActive(false);

            // Закрываем если вышел из триггера
            if (UpgradeUIManager.Instance.IsOpen)
                UpgradeUIManager.Instance.Close();
        }
    }

    private void Update()
    {
        if (inTrigger && InputManager.Instance.Interact)
        {
            UpgradeUIManager.Instance.Open();
            nadpis.SetActive(false); // прячем подсказку когда открыли
        }
    }
}