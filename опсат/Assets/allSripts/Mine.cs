using UnityEngine;

public class Mine : MonoBehaviour
{
    public Boom boom;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            boom.ExplodeWithDelay();
        }
    }
}
