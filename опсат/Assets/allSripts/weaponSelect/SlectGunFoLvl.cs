using UnityEngine;

public class SlectGunFoLvl : MonoBehaviour
{
    public GameObject gun1;
    public GameObject gun2;
    public GameObject gun3;
   

    public int cost1;
    public int cost2;
    public int cost3;

    public GameObject panelSelect;

    private Collider col;

    public money dengi;

    public Animator anim;

    public bool inTigger = false;
    public void Start()
    {
        col = gameObject.GetComponent<Collider>();
    }
    public void GunSelected1()
    {
        if (dengi._money >= cost1)
        {
            gun1.SetActive(true);
            //deactivate
            gun2.SetActive(false);
            gun3.SetActive(false);
            panelSelect.SetActive(false);
            dengi._money -= cost1;

            col.enabled = false;
            anim.SetTrigger("buy");

            inTigger = false;
        }
       
    }
    public void GunSelected2()
    {
        if (dengi._money >= cost2)
        {
            gun2.SetActive(true);
            //deactivate
            gun1.SetActive(false);
            gun3.SetActive(false);

            dengi._money -= cost2;
            anim.SetTrigger("buy");
            inTigger = false;
            col.enabled = false;
            panelSelect.SetActive(false);
        }
    }
    public void GunSelected3()
    {
        if (dengi._money >= cost3)
        {
            gun3.SetActive(true);
            //deactivate
            gun1.SetActive(false);
            gun2.SetActive(false);
            col.enabled = false;
            dengi._money -= cost3;
           anim.SetTrigger("buy");
        

           panelSelect.SetActive(false);
        }
            
    }
   
    
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            panelSelect.SetActive(true);
            inTigger = true;
        }
    }
    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            panelSelect.SetActive(false);
            inTigger = false;
        }
    }
}
