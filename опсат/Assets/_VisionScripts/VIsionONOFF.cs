using UnityEngine;

public class VIsionONOFF : MonoBehaviour
{
    public bool On;

    public GameObject Fog;


    void Update()
    {
        if (On == true)
        {
            Fog.SetActive(false);
        }
        else
        {
            Fog.SetActive(true);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) On = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) On = false;
    }

}
