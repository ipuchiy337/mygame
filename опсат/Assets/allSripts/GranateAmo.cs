using UnityEngine;

public class GranateAmo : MonoBehaviour
{
    public GrenadeEquipThrow granate;

    public float granateCount;

    private void OnTriggerEnter(Collider other)
    {
        if (granate.inInvGranate < 3)
        {
            granate.inInvGranate += granateCount;
            {
                if (granate.inInvGranate > 3)
                {
                    granate.inInvGranate = 3;
                }
            }
            Destroy(gameObject);
        }
    }
}
