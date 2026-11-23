using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;

    [Header("Настройки камеры")]
    public float distance = 10f;
    public float pitch = 45f;        // вертикальный угол (вниз)
    public float yaw = 0f;           // горизонтальный угол
    public float followSmooth = 0.15f;

    [Header("Поворот мышью (ЛКМ)")]
    public float rotateSpeed = 3f;
    public float rotationSmooth = 8f;
    public float minPitch = 10f;
    public float maxPitch = 80f;

    [Header("Зум (колёсико)")]
    public float zoomSpeed = 4f;
    public float minDistance = 3f;
    public float maxDistance = 20f;
    public float zoomSmooth = 8f;

    private float currentYaw;
    private float currentPitch;
    private float targetYaw;
    private float targetPitch;
    private float targetDistance;
    private float currentDistance;
    private Vector3 velocity;

    void Start()
    {
        Camera.main.orthographic = false;

        currentYaw = targetYaw = yaw;
        currentPitch = targetPitch = pitch;
        currentDistance = targetDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleRotation();
        HandleZoom();
        UpdatePosition();
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // ПКМ для вращения
        {
            targetYaw += Input.GetAxis("Mouse X") * rotateSpeed;
            targetPitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * rotationSmooth);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * rotationSmooth);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * zoomSmooth);
    }

    void UpdatePosition()
    {
        // Считаем офсет по сферическим координатам
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);

        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, followSmooth);
        transform.LookAt(target.position);
    }
}