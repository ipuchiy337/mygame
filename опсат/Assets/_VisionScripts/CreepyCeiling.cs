using UnityEngine;

public class CreepyCeiling : MonoBehaviour
{
    public Transform ceiling;
    public float riseHeight = 5f;
    public float speed = 2f;
    

    private Vector3 startPos;
    private bool hide = false;

    void Start()
    {
        startPos = ceiling.localPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) hide = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) hide = false;
    }

    void Update()
    {
        Vector3 target = startPos + (hide ? Vector3.up * riseHeight : Vector3.zero);
        // плавное движение
        ceiling.localPosition = Vector3.Lerp(ceiling.localPosition, target, Time.deltaTime * speed);

        
    }
}
