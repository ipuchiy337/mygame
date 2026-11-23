using UnityEngine;
using UnityEngine.UI;

public class GrenadeEquipThrow : MonoBehaviour
{
    [Header("Prefabs & Transforms")]
    public GameObject grenadePrefab;
    public Transform handPoint;
    public Camera mainCamera;

    [Header("Throw settings")]
    public float throwForce = 12f;
    public float throwUpForce = 3f;

    GameObject heldGrenade;
    Rigidbody heldRb;

    public bool inHands = false;


    public float inInvGranate;
    public Text countGranate;



    public PlayerWithAim aim;

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
       
        if (aim.isAiming == true)
        {
            if (inInvGranate >= 1)
            {
                if (Input.GetKeyDown(KeyCode.LeftAlt) && heldGrenade == null)
                {
                    EquipGrenade();
                    inHands = true;
                    
                }
            }
            

        }
        // Взять гранату на зажатие Alt

        // Бросить гранату, когда отпустили Alt
        if (Input.GetKeyUp(KeyCode.LeftAlt) && heldGrenade != null)
            {
                ThrowForward();
                
            }

            // Отмена (если нужно)
            if (Input.GetKeyDown(KeyCode.RightAlt) && heldGrenade != null)
            {
                CancelHold();
                inHands = false;
            }
        countGranate.text = inInvGranate.ToString();
    }

    void EquipGrenade()
    {
        if (grenadePrefab == null || handPoint == null) return;

        heldGrenade = Instantiate(grenadePrefab, handPoint.position, handPoint.rotation);
        heldGrenade.transform.SetParent(handPoint);
        heldGrenade.transform.localPosition = Vector3.zero;
        heldGrenade.transform.localRotation = Quaternion.identity;

        heldRb = heldGrenade.GetComponent<Rigidbody>();
        if (heldRb != null)
        {
            heldRb.isKinematic = true;
            heldRb.detectCollisions = false;
        }
    }



    void ThrowForward()
    {
        if (heldGrenade == null) return;

        // Отсоединяем гранату
        heldGrenade.transform.SetParent(null);

        if (heldRb != null)
        {
            heldRb.isKinematic = false;
            heldRb.detectCollisions = true;

            // Направление — туда куда смотрит игрок
            Vector3 dir = transform.forward;

            // Добавляем вертикальный импульс
            Vector3 velocity = dir * throwForce + Vector3.up * throwUpForce;

            heldRb.AddForce(velocity, ForceMode.Impulse);
        }

        // Удаляем ссылки
        heldGrenade = null;
        heldRb = null;
        inHands = false;
        inInvGranate -= 1;
    }

    void CancelHold()
    {
        if (heldGrenade != null)
        {
            Destroy(heldGrenade);
            heldGrenade = null;
            heldRb = null;
        }
    }
}

