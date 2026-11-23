using UnityEngine;

public class FogSection : MonoBehaviour
{
    public Material fogMaterial;
    public float viewRadius = 5f;
    public float coneAngle = 360f;
    public Transform player;


    private bool playerInside = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    void Update()
    {
        if (playerInside && fogMaterial != null && player != null)
        {
            fogMaterial.SetVector("_PlayerPosition", player.position);
            fogMaterial.SetFloat("_ViewRadius", viewRadius);
            fogMaterial.SetFloat("_ConeAngle", coneAngle);
            
        }
    }
}
