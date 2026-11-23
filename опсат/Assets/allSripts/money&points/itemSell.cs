using UnityEngine;

public class itemSell : MonoBehaviour
{
    public GameObject pan;
    
   
    public Animator anim;
    private bool inTrigger;


    public Animator animinv;

    public PauseManager pauseM;
    void Update()
    {
        if (inTrigger == true && Input.GetKeyDown(KeyCode.E))
        {
            ShopSellUI.Instance.OpenShop();
            pauseM.Pauz();
            animinv.SetTrigger("open");
        }
        if (InputManager.Instance.UIBack)
        {
            pan.SetActive(false);
            pauseM.Rezume();
            animinv.SetTrigger("close");
        }
    }
   
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            anim.SetBool("isOpen", true); // גלוסעמ SetTrigger("close")
            inTrigger = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        anim.SetBool("isOpen", false); // גלוסעמ SetTrigger("close")
        inTrigger = false;
    }
}
