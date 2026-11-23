using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerWithAim : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody _rb;
    
                 

    [Header("Rotation")]
    public Transform LookPlayer;
    public float rotationSpeed = 15f;

    [Header("Aiming")]
    public Camera mainCamera;
    public bool isAiming = false;

    private Vector3 _input;
    private Vector3 _moveDir;

     

    void Update()
    {
        InputGet();
        
        
        isAiming = Input.GetMouseButton(1);
    }

    

    void InputGet()
    {
        _input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    }

   
   
    
}
