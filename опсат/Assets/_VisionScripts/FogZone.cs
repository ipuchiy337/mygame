using UnityEngine;

public class FogZone : MonoBehaviour
{
    public Material fogMaterial;
    
    public Transform player;
    public float hidingDistance = 3f;
    public GameObject Fog;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fogMaterial.SetFloat("_Enable", 1); // включаем эффект
            Fog.SetActive(false);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fogMaterial.SetFloat("_Enable", 1); // включаем эффект
            Fog.SetActive(false);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fogMaterial.SetFloat("_Enable", 0); // выключаем эффект
            Fog.SetActive(true);
        }
    }

    void Update()
    {
        if (player != null)
        {
            fogMaterial.SetVector("_PlayerWorldPos", player.position);
            fogMaterial.SetFloat("_HidingDistance", hidingDistance);
        }
    }
}
