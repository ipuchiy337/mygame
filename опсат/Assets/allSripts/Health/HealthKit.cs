using UnityEngine;

public class HealthKit : MonoBehaviour
{


    public Health health;

    public bool aidKit;
    public float ADDHP;
    

    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (aidKit == true)
            {
                if (health.hp <= 99)
                {
                    health.hp += ADDHP;
                    Destroy(gameObject);
                }
                
            }
            
            
        }    
    }        
}
