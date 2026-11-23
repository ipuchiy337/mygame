using UnityEngine;

public class BackRooms : MonoBehaviour
{
    public GameObject Player;

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }
}
