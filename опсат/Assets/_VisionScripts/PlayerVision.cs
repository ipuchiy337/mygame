using UnityEngine;

public class PlayerVision : MonoBehaviour
{
    public Transform player;
    public Material fogMaterial;
    public float viewRadius = 3f;

    void Update()
    {
        if (player == null || fogMaterial == null) return;

        fogMaterial.SetVector("_PlayerWorldPos", player.position);
        fogMaterial.SetFloat("_HidingDistance", viewRadius);
    }
}