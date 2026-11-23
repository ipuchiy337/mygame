using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public ItemSO item;
    public int amount = 1;
    public Animator anim;
    public Image iconImage;
    public Text costext;

    void Start()
    {
        if (iconImage != null && item != null)
            iconImage.sprite = item.icon;
        if (costext != null && item != null)
            costext.text = item.cost.ToString(item.cost + "$");
    }

    public void Pickup()
    {
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null) return;

        if (item != null && item.pickUpSound != null)
        {
            try { AudioSource.PlayClipAtPoint(item.pickUpSound, transform.position); }
            catch { }
        }

        int added = inventory.AddItem(item, amount);
        amount -= added;
        if (amount <= 0)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Inventory>())
            anim.SetBool("isOpen", true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Inventory>())
            anim.SetBool("isOpen", false);
    }
}