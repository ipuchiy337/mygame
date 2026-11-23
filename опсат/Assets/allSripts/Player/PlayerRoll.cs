using UnityEngine;

public class PlayerRoll : MonoBehaviour
{
    [Header("Ќастройки переката")]
    public float rollSpeed = 8f;
    public float rollDuration = 0.45f;
    public float rollCooldown = 1f;
    public float recoveryTime = 0.35f;

    [Header("—сылки")]
    public Player player;

    private Animator anim;
    private Rigidbody rb;
    private Vector3 rollDirection;

    private bool isRolling = false;
    private bool isRecovering = false;
    private float rollTimer = 0f;
    private float cooldownTimer = 0f;
    private float recoveryTimer = 0f;

    public bool IsRolling => isRolling;
    public bool IsRecovering => isRecovering;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (isRecovering)
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0f) isRecovering = false;
        }

        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f) StopRoll();
            return;
        }

        if (InputManager.Instance.Roll && player.isGrounded && cooldownTimer <= 0f && !isRecovering)
            StartRoll();
    }

    void FixedUpdate()
    {
        if (!isRolling) return;

        // только горизонталь Ч вертикаль оставл€ем гравитации
        rb.linearVelocity = new Vector3(
            rollDirection.x * rollSpeed,
            rb.linearVelocity.y,
            rollDirection.z * rollSpeed);
    }

    void StartRoll()
    {
        isRolling = true;
        rollTimer = rollDuration;
        cooldownTimer = rollCooldown;

        Vector2 moveInput = InputManager.Instance.Move;
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = player.mainCamera.transform.forward;
            Vector3 camRight = player.mainCamera.transform.right;
            camForward.y = 0f; camForward.Normalize();
            camRight.y = 0f; camRight.Normalize();
            rollDirection = (camForward * input.z + camRight * input.x).normalized;
        }
        else
        {
            rollDirection = transform.forward;
        }

        transform.rotation = Quaternion.LookRotation(rollDirection, Vector3.up);
        anim.SetTrigger("Roll");
    }

    void StopRoll()
    {
        isRolling = false;
        isRecovering = true;
        recoveryTimer = recoveryTime;

        // гасим горизонтальный импульс чтобы не скользить после переката
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }
}