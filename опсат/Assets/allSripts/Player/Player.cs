using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody _rb;
    public Camera mainCamera;

    [Header("Movement")]
    public float walkSpeed = 3.4f;
    public float sprintSpeed = 5f;
    public float acceleration = 18f;
    public float deceleration = 22f;
    public float turnSpeed = 12f;

    [Header("Slope")]
    [Tooltip("Максимальный угол, на который игрок может подниматься.")]
    public float maxSlopeAngle = 45f;

    [Tooltip("Сила прижатия игрока к склону.")]
    public float slopeStickForce = 12f;

    [Tooltip("Скорость наклона персонажа под склон.")]
    public float slopeRotationSpeed = 10f;

    [Tooltip("Дистанция проверки земли (от точки старта луча). Рекомендуется 0.5-0.7.")]
    public float slopeCheckDistance = 0.6f;

    [Tooltip("Насколько высоко начинается Raycast.")]
    public float groundRayStartHeight = 0.25f;

    [Tooltip("Слой земли.")]
    public LayerMask groundLayer;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float fallMultiplier = 2.5f;
    public float coyoteTime = 0.15f;

    [Tooltip("Сколько секунд после прыжка земля игнорируется (иначе склон гасит прыжок).")]
    public float jumpLockTime = 0.2f;

    private float coyoteTimeCounter;
    private float jumpLockTimer;
    private bool _jumpPressed;

    [Header("Stamina")]
    public float stamina = 100f;
    public float maxStamina = 100f;
    public float staminaDrain = 20f;
    public float staminaRegen = 10f;
    public float sprintResumeStamina = 20f;

    public Text _stamina;
    public Image StaminaFill;

    private bool canSprint = true;

    [Header("Effects")]
    public ParticleSystem dustParticles;
    public ParticleSystem walkDustParticles;
    public ParticleSystem landJumpParticles;

    [Header("Camera")]
    public CinemachineCamera virtualCam;

    [Header("FOV")]
    public float baseFOV = 90f;
    public float sprintFOV = 100f;
    public float fovSmoothSpeed = 8f;

    private Vector3 _input;
    private Vector3 currentMoveVelocity;

    public bool isGrounded;

    private Animator anim;
    private PlayerRoll rollScript;

    private bool wasGrounded;
    private bool wasJumping;

    // Slope
    private RaycastHit slopeHit;
    private bool onValidSlope;
    private float currentSlopeAngle;

    // Target movement
    private Vector3 targetMoveVelocity;

    void Awake()
    {
        rollScript = GetComponent<PlayerRoll>();
        anim = GetComponentInChildren<Animator>();

        if (_rb == null)
            _rb = GetComponent<Rigidbody>();

        if (_rb != null)
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rb.freezeRotation = true;
        }
    }

    void Update()
    {
        GatherInput();

        HandleGroundCheck();

        HandleStamina();

        UpdateUI();

        UpdateAnimations();

        HandleSlopeRotation();

        HandleJumpInput();

        HandleCameraFOV();
    }

    void FixedUpdate()
    {
        Move();

        HandleJump();

        ApplyGravity();

        StickToGround();
    }

    // =========================================================
    // INPUT
    // =========================================================

    void GatherInput()
    {
        Vector2 moveInput = InputManager.Instance.Move;

        _input = new Vector3(
            moveInput.x,
            0f,
            moveInput.y
        );

        if (_input.sqrMagnitude > 1f)
            _input.Normalize();
    }

    void HandleJumpInput()
    {
        if (InputManager.Instance.Jump)
        {
            _jumpPressed = true;
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    void Move()
    {
        if (_rb == null)
            return;

        // Во время переката полностью отдаём управление PlayerRoll.
        if (rollScript != null &&
            (rollScript.IsRolling || rollScript.IsRecovering))
        {
            return;
        }

        Vector3 moveDirection = GetCameraRelativeDirection();

        // =====================================================
        // ДВИЖЕНИЕ ПО СКЛОНУ
        // =====================================================

        if (isGrounded && onValidSlope)
        {
            Vector3 slopeDirection =
                Vector3.ProjectOnPlane(
                    moveDirection,
                    slopeHit.normal
                );

            if (slopeDirection.sqrMagnitude > 0.001f)
                moveDirection = slopeDirection.normalized;
            else
                moveDirection = Vector3.zero;
        }

        bool sprinting =
            InputManager.Instance.SprintHeld &&
            CanSprint();

        float speed = sprinting
            ? sprintSpeed
            : walkSpeed;

        targetMoveVelocity = moveDirection * speed;

        // =====================================================
        // ПЛАВНОЕ УСКОРЕНИЕ
        // =====================================================

        float smoothSpeed =
            targetMoveVelocity.sqrMagnitude > 0.01f
                ? acceleration
                : deceleration;

        currentMoveVelocity = Vector3.MoveTowards(
            currentMoveVelocity,
            targetMoveVelocity,
            smoothSpeed * Time.fixedDeltaTime
        );

        // =====================================================
        // ПРИМЕНЯЕМ СКОРОСТЬ
        // =====================================================

        Vector3 velocity = _rb.linearVelocity;

        if (isGrounded && onValidSlope)
        {
            // На склоне идём строго вдоль поверхности.
            velocity = Vector3.ProjectOnPlane(
                currentMoveVelocity,
                slopeHit.normal
            );
        }
        else
        {
            // В воздухе и на ровной земле Y принадлежит физике.
            velocity.x = currentMoveVelocity.x;
            velocity.z = currentMoveVelocity.z;
        }

        _rb.linearVelocity = velocity;

        // =====================================================
        // ПОВОРОТ
        // =====================================================

        RotatePlayer(moveDirection);
    }

    Vector3 GetCameraRelativeDirection()
    {
        if (mainCamera == null)
            return _input;

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 direction =
            camForward * _input.z +
            camRight * _input.x;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        return direction;
    }

    // =========================================================
    // ROTATION
    // =========================================================

    void RotatePlayer(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.001f)
            return;

        Vector3 lookDirection;

        if (isGrounded && onValidSlope)
        {
            lookDirection =
                Vector3.ProjectOnPlane(
                    moveDirection,
                    slopeHit.normal
                );
        }
        else
        {
            lookDirection = moveDirection;
        }

        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation;

        if (isGrounded && onValidSlope)
        {
            targetRotation = Quaternion.LookRotation(
                lookDirection.normalized,
                slopeHit.normal
            );
        }
        else
        {
            Vector3 flat = new Vector3(
                lookDirection.x,
                0f,
                lookDirection.z
            );

            if (flat.sqrMagnitude < 0.001f)
                return;

            targetRotation = Quaternion.LookRotation(flat.normalized);
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime
        );
    }

    // =========================================================
    // GROUND CHECK
    // =========================================================

    void HandleGroundCheck()
    {
        wasGrounded = isGrounded;

        if (jumpLockTimer > 0f)
        {
            // Сразу после прыжка землю не ищем.
            jumpLockTimer -= Time.deltaTime;

            isGrounded = false;
            onValidSlope = false;
            currentSlopeAngle = 0f;
        }
        else
        {
            Vector3 rayStart =
                transform.position +
                Vector3.up * groundRayStartHeight;

            if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out slopeHit,
                slopeCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore))
            {
                isGrounded = true;

                currentSlopeAngle =
                    Vector3.Angle(
                        slopeHit.normal,
                        Vector3.up
                    );

                onValidSlope =
                    currentSlopeAngle > 0.1f &&
                    currentSlopeAngle <= maxSlopeAngle;
            }
            else
            {
                isGrounded = false;
                onValidSlope = false;
                currentSlopeAngle = 0f;
            }
        }

        // =====================================================
        // COYOTE TIME
        // =====================================================

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // =====================================================
        // LANDING
        // =====================================================

        if (!wasGrounded &&
            isGrounded &&
            wasJumping)
        {
            wasJumping = false;

            if (landJumpParticles != null)
                landJumpParticles.Play();
        }
    }

    // =========================================================
    // SLOPE ROTATION
    // =========================================================

    void HandleSlopeRotation()
    {
        if (!isGrounded || !onValidSlope)
            return;

        Vector3 forward =
            Vector3.ProjectOnPlane(
                transform.forward,
                slopeHit.normal
            );

        if (forward.sqrMagnitude < 0.001f)
            return;

        forward.Normalize();

        Quaternion slopeRotation =
            Quaternion.LookRotation(
                forward,
                slopeHit.normal
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                slopeRotation,
                slopeRotationSpeed * Time.deltaTime
            );
    }

    // =========================================================
    // STICK TO GROUND
    // =========================================================

    void StickToGround()
    {
        if (!isGrounded)
            return;

        if (!onValidSlope)
            return;

        _rb.AddForce(
            -slopeHit.normal * slopeStickForce,
            ForceMode.Acceleration
        );
    }

    // =========================================================
    // JUMP
    // =========================================================

    void HandleJump()
    {
        if (!_jumpPressed)
            return;

        _jumpPressed = false;

        if (rollScript != null &&
            (rollScript.IsRolling || rollScript.IsRecovering))
        {
            return;
        }

        if (coyoteTimeCounter <= 0f)
            return;

        Vector3 velocity = _rb.linearVelocity;

        // Горизонталь сохраняем, вертикаль задаёт прыжок.
        velocity.x = currentMoveVelocity.x;
        velocity.z = currentMoveVelocity.z;
        velocity.y = jumpForce;

        _rb.linearVelocity = velocity;

        coyoteTimeCounter = 0f;
        jumpLockTimer = jumpLockTime;
        wasJumping = true;

        // Сразу считаем себя в воздухе, чтобы Move() и StickToGround()
        // не затёрли прыжок до следующего Update.
        isGrounded = false;
        onValidSlope = false;

        if (anim != null)
            anim.SetTrigger("Jump");
    }

    // =========================================================
    // GRAVITY
    // =========================================================

    void ApplyGravity()
    {
        if (isGrounded)
            return;

        if (_rb.linearVelocity.y < 0f)
        {
            _rb.linearVelocity +=
                Vector3.up *
                Physics.gravity.y *
                (fallMultiplier - 1f) *
                Time.fixedDeltaTime;
        }
    }

    // =========================================================
    // SPRINT
    // =========================================================

    bool CanSprint()
    {
        if (stamina <= 0f)
            canSprint = false;

        if (!canSprint &&
            stamina >= sprintResumeStamina)
        {
            canSprint = true;
        }

        return canSprint && stamina > 0f;
    }

    // =========================================================
    // STAMINA
    // =========================================================

    void HandleStamina()
    {
        bool sprinting =
            InputManager.Instance.SprintHeld &&
            CanSprint();

        stamina +=
            (sprinting
                ? -staminaDrain
                : staminaRegen) *
            Time.deltaTime;

        stamina = Mathf.Clamp(
            stamina,
            0f,
            maxStamina
        );

        if (stamina <= 0f)
            canSprint = false;

        if (!canSprint &&
            stamina >= sprintResumeStamina)
        {
            canSprint = true;
        }
    }

    // =========================================================
    // CAMERA FOV
    // =========================================================

    void HandleCameraFOV()
    {
        if (virtualCam == null)
            return;

        bool sprinting =
            InputManager.Instance.SprintHeld &&
            CanSprint();

        var lens = virtualCam.Lens;

        lens.FieldOfView = Mathf.Lerp(
            lens.FieldOfView,
            sprinting ? sprintFOV : baseFOV,
            Time.deltaTime * fovSmoothSpeed
        );

        virtualCam.Lens = lens;
    }

    // =========================================================
    // UI
    // =========================================================

    void UpdateUI()
    {
        if (StaminaFill != null)
        {
            StaminaFill.fillAmount =
                stamina / maxStamina;
        }

        if (_stamina != null)
        {
            _stamina.text =
                Mathf.CeilToInt(stamina).ToString();
        }
    }

    // =========================================================
    // ANIMATIONS
    // =========================================================

    void UpdateAnimations()
    {
        if (anim == null)
            return;

        float speed =
            new Vector3(
                currentMoveVelocity.x,
                0f,
                currentMoveVelocity.z
            ).magnitude;

        bool sprinting =
            InputManager.Instance.SprintHeld &&
            CanSprint() &&
            speed > walkSpeed * 0.8f;

        anim.SetBool(
            "IsWalk",
            speed > 0.1f
        );

        anim.SetBool(
            "IsSprint",
            sprinting
        );

        HandleDustEffect(
            sprinting,
            speed
        );
    }

    // =========================================================
    // DUST
    // =========================================================

    void HandleDustEffect(
        bool sprinting,
        float speed)
    {
        if (dustParticles == null ||
            walkDustParticles == null)
            return;

        bool isSprinting =
            sprinting &&
            isGrounded &&
            speed > walkSpeed * 1.2f;

        bool isWalking =
            isGrounded &&
            speed > 0.1f &&
            speed <= walkSpeed;

        if (isSprinting)
        {
            if (!dustParticles.isPlaying)
                dustParticles.Play();
        }
        else
        {
            if (dustParticles.isPlaying)
                dustParticles.Stop();
        }

        if (isWalking)
        {
            if (!walkDustParticles.isPlaying)
                walkDustParticles.Play();
        }
        else
        {
            if (walkDustParticles.isPlaying)
                walkDustParticles.Stop();
        }
    }
}