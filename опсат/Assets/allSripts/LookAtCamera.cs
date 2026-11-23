using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;
    private Quaternion animationRotation;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            

            // Поворачиваем к камере
            transform.rotation = mainCamera.transform.rotation;

            
        }
    }
}