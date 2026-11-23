using UnityEngine;

public class AptekaMagazin : MonoBehaviour
{
    public Animator anim;
    public Health hepe;
    public float timeRegen;

    private bool inTrigger;

    public HillManager othill;
    public UoPlayerHealth ghp;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            anim.SetBool("isOpen", true); // גלוסעמ SetTrigger("close")
            inTrigger = true;
            othill.hilling = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            anim.SetBool("isOpen", false); // גלוסעמ SetTrigger("close")s
            inTrigger = false;
            timeRegen = 0;
            othill.hilling = false;
        }
    }

    private void Update()
    {
        if (!inTrigger) return;
        if (InputManager.Instance.Interact)
        {
            ghp.Buy();
        }

        timeRegen += Time.deltaTime;

        if (hepe.hp < hepe.maxHp && timeRegen >= 2f)
        {
            hepe.hp += 10;
            
           

            timeRegen = 0;
            if (hepe.hp >= hepe.maxHp)
            {
                hepe.hp = hepe.maxHp;
            }
        }
    }
}
