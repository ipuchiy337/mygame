using UnityEngine;

public class PickupZone : MonoBehaviour
{
    private DropWeapon drop;
    private bool playerInside = false;
    private WeaponManager wm;
    public GameObject buttonE;

    private void Start()
    {
        drop = GetComponentInParent<DropWeapon>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            wm = other.GetComponent<WeaponManager>();
            playerInside = true;
            buttonE.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            wm = null;
            buttonE.SetActive(false);
        }
    }

    void Update()
    {
        if (playerInside && wm != null && Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    public void UpGun() // UI кнопка
    {
        if (playerInside && wm != null)
        {
            TryPickup();
        }
    }

    private void TryPickup()
    {
        if (wm.TryPickupWeapon(drop.GunID))
        {
            Destroy(transform.parent.gameObject);
            buttonE.SetActive(false);
        }
    }
}
