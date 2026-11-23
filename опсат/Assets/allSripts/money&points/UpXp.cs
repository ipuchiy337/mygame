using UnityEngine;

public class UpXp : MonoBehaviour
{
   

    Xp _exp;
 
    public int minXp = 1;
    public int maxXp = 10;

    public int addxp;

    private void Start()
    {
        _exp = FindObjectOfType<Xp>();

        addxp = Random.Range(minXp, maxXp + 1);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _exp._xp += addxp;
            Destroy(gameObject);
        }
    }
    public void Pickup()
    {
        _exp._xp += addxp;
        Destroy(gameObject);
    }
}
