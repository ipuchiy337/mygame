using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Оптимизированный ИИ орды без NavMesh.
// Сохранены: прыжки, карабканье по стенам, движение по склонам,
// атака, урон, Knockback, Noked, StartDelay и поле attaked.
// Игрок всегда является целью: проверки линии видимости удалены.
// Добавлено: режим кастера (stopDistance) и блокировка движения на время каста (castLocked),
// которые использует ZombieSummoner.
// Своих Update/FixedUpdate у зомби нет: это «пустышка» с настройками и состоянием,
// всю орду одним проходом ведёт ZombieHorde (Tick / FixedTick). Настройки у каждого шаблона свои —
// отсюда разнообразие зомби (скорость, урон, прыжки, кастеры), менеджер его не трогает.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class ZombieFollow : MonoBehaviour
{
    // Все активные зомби — их перебирает ZombieHorde.
    public static readonly List<ZombieFollow> All = new List<ZombieFollow>(1024);

    [Header("Ссылки")]
    public Transform player;
    public Health zhp;
    public Animator anim;

    [Header("Скорость")]
    public float minSpeed = 2.5f;
    public float maxSpeed = 4.5f;
    public float rotationSpeed = 540f;

    [Header("Атака")]
    public float attackRange = 1.5f;
    public float maxAttackHeightDiff = 1.5f;
    public float damage = 10f;
    public float attackCooldown = 1f;
    public float attackAnimationDelay = 0.3f;

    [Header("Дистанция (кастер)")]
    [Tooltip("0 = обычный зомби. Больше 0 - останавливается на этой дистанции от игрока.")]
    public float stopDistance = 0f;
    [Tooltip("Снова пойдёт к игроку, только если он отошёл на stopDistance + это значение.")]
    public float stopHysteresis = 1f;
    [HideInInspector] public bool castLocked;

    [Header("Получение урона")]
    public float hitStunDuration = 0.5f;

    [Header("Отбрасывание")]
    public float knockbackDuration = 0.3f;

    [Header("Слои")]
    [Tooltip("Только земля, стены и другие объекты окружения. Исключите слои Player, Zombie и Wall.")]
    public LayerMask environmentMask = ~0;
    [Tooltip("Необязательно: слой зомби. Если задан, он исключается из environmentMask.")]
    public LayerMask zombieMask;

    [Header("Оптимизация")]
    [Tooltip("Интервал проверки препятствий. Увеличение снижает CPU-нагрузку, но делает обход менее отзывчивым.")]
    [Min(0.05f)] public float steerInterval = 0.25f;
    [Tooltip("Дальность проверки препятствия перед зомби.")]
    [Min(0.5f)] public float lookAhead = 2f;
    [Tooltip("Сколько секунд почти без движения считать застреванием.")]
    [Min(0.2f)] public float stuckLimit = 1.2f;
    [Tooltip("Разносит проверку препятствий разных зомби по времени.")]
    public bool staggerChecks = true;
    [Tooltip("Дальше этой дистанции от игрока проверки земли/препятствий делаются реже.")]
    public float farDistance = 30f;
    [Tooltip("Во сколько раз реже проверки у дальних зомби.")]
    [Min(1f)] public float farCheckMultiplier = 3f;
    [Tooltip("Склеить меши модели в один + LODGroup + отсечение анимации вне камеры.")]
    public bool optimizeModel = true;

    [Header("Склоны и ступеньки")]
    public float stepHeight = 0.4f;
    public float stepSmooth = 0.08f;
    [Range(0f, 80f)] public float maxSlopeAngle = 50f;
    public float airControl = 2f;
    public bool zeroFriction = true;
    public float groundRayStartHeight = 0.25f;
    public float slopeCheckDistance = 0.6f;
    public float slopeStickForce = 12f;
    public bool alignToSlope = true;

    [Header("Прыжки")]
    public bool canJump = true;
    public float maxJumpHeight = 1.8f;
    public float jumpClearance = 0.35f;
    public float jumpForwardSpeed = 5f;
    public float jumpTriggerDistance = 0.9f;
    public string jumpTrigger = "";

    [Header("Карабканье по стенам")]
    public bool canClimb = true;
    public float climbSpeed = 3.5f;
    public float wallCheckDistance = 0.8f;
    public float maxClimbHeight = 40f;
    public float mantleTime = 0.4f;
    public float climbIgnoreIfPlayerBelow = 1.5f;
    public float climbAbortDot = 0.3f;
    public float climbCooldown = 0.4f;
    public float climbSideSpeedFactor = 0.6f;

    [Header("Падение")]
    public float fallY = -50f;

    [Header("Звуки и состояния")]
    public bool attaked;

    private Rigidbody rb;
    private CapsuleCollider col;
    private float speed;
    private float individualRotationSpeed;
    private float individualAttackCooldown;
    private float radius;
    private float height;
    private float minNormalY;

    private bool isKnocked;
    private bool isInKnockback;
    private bool isAttacking;
    private bool movementLocked;
    private bool grounded;
    private bool onValidSlope;
    private bool wantsToMove;
    private bool holdingDistance;
    private Vector3 groundNormal = Vector3.up;

    private float stunUntil;
    private float lastAttackTime = -999f;
    private Coroutine attackCoroutine;

    private Vector3 steerDir;
    private float nextSteerTime;
    private float nextJumpCheckTime;
    private float nextStepAssistTime;
    private float nextGroundCheckTime;
    private float nextClimbCheckTime;
    private bool cachedDirectBlocked;
    private Vector3 cachedBlockNormal;
    private int followSide;
    private float forceDetourUntil;
    private float stuckCheckTime;
    private Vector3 stuckCheckPosition;

    private bool jumping;
    private Vector3 jumpDir;
    private float jumpStartTime;
    private float jumpStartY;
    private float jumpBlockedUntil;
    private int jumpFailCount;
    private const float JumpMinAirTime = 0.25f;

    private bool climbing;
    private Vector3 climbWallDir;
    private float climbStartY;
    private float mantleTimer;
    private float climbBlockedUntil;

    private float nextSafePosTime;
    private Vector3 lastSafePosition;
    private bool layerFilteringReady;
    private float lod = 1f;

    private static PhysicsMaterial sharedNoFriction;

    private static readonly int IdleHash = Animator.StringToHash("idle");
    private static readonly int RunHash = Animator.StringToHash("run");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DamagedHash = Animator.StringToHash("Damaged");

    private bool IsStunned => Time.time < stunUntil;

    // Может ли зомби сейчас начать/продолжать действие (каст и т.п.).
    public bool CanAct => !isKnocked && !isInKnockback && !movementLocked && !IsStunned
                          && !climbing && !jumping && mantleTimer <= 0f && grounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        radius = col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        height = col.height * transform.lossyScale.y;
        minNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);

        speed = Random.Range(minSpeed, maxSpeed);
        individualRotationSpeed = rotationSpeed * Random.Range(0.9f, 1.1f);
        individualAttackCooldown = attackCooldown * Random.Range(0.9f, 1.1f);

        // Не позволяем физическим проверкам считать игрока препятствием для прыжка/обхода.
        // Все дочерние коллайдеры игрока тоже должны быть на его слое.
        if (player != null)
            environmentMask &= ~(1 << player.gameObject.layer);

        // Соседи по орде — не препятствия: иначе в толпе зомби обходят, перепрыгивают и залезают
        // друг на друга (гора зомби), а лучи/капсулы зря упираются в соседей.
        environmentMask &= ~(1 << gameObject.layer);

        layerFilteringReady = zombieMask.value != 0;
        if (layerFilteringReady)
            environmentMask &= ~zombieMask;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // Для обычных зомби часто достаточно Discrete; включайте Continuous только если они быстро проходят сквозь геометрию.
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (zeroFriction)
        {
            // Один общий физический материал на всю орду, а не отдельный на каждого зомби.
            if (sharedNoFriction == null)
            {
                sharedNoFriction = new PhysicsMaterial("ZombieNoFriction_Shared");
                sharedNoFriction.dynamicFriction = 0f;
                sharedNoFriction.staticFriction = 0f;
                sharedNoFriction.bounciness = 0f;
                sharedNoFriction.frictionCombine = PhysicsMaterialCombine.Minimum;
                sharedNoFriction.bounceCombine = PhysicsMaterialCombine.Minimum;
            }
            col.sharedMaterial = sharedNoFriction;
        }

        lastSafePosition = transform.position;
        stuckCheckPosition = transform.position;
        float offset = staggerChecks ? Random.Range(0f, steerInterval) : 0f;
        nextSteerTime = Time.time + offset;
        nextStepAssistTime = Time.time + Random.Range(0f, 0.12f);
        nextGroundCheckTime = Time.time + Random.Range(0f, 0.05f);
        nextClimbCheckTime = Time.time + Random.Range(0f, 0.3f);
        stuckCheckTime = Time.time + 0.5f + (staggerChecks ? Random.Range(0f, 0.5f) : 0f);

        SetAnim(IdleHash, true);
        SetAnim(RunHash, false);
        if (anim != null)
        {
            anim.ResetTrigger(AttackHash);
            anim.ResetTrigger(DamagedHash);
        }
    }

    private void Start()
    {
        // В Start, а не в Awake: Animator модели к этому моменту уже инициализирован.
        if (optimizeModel) ZombieLod.Apply(gameObject);
    }

    private void OnEnable()
    {
        All.Add(this);
        ZombieHorde.Ensure();
    }

    private void OnDisable() => All.Remove(this);

    // Вызывается из ZombieHorde раз в кадр (бывший Update).
    internal void Tick()
    {
        if (player == null || isKnocked || isInKnockback)
            return;

        CheckDamaged();
        if (movementLocked || IsStunned)
            return;

        bool inReach = !climbing && grounded && IsPlayerInReach(attackRange);
        attaked = inReach;

        if (inReach)
        {
            SetAnim(RunHash, false);
            SetAnim(IdleHash, false);
            if (!isAttacking && Time.time >= lastAttackTime + individualAttackCooldown)
                attackCoroutine = StartCoroutine(AttackRoutine());
        }
        else
        {
            if (isAttacking)
                StopAttackAnimation();

            SetAnim(IdleHash, !wantsToMove);
            SetAnim(RunHash, wantsToMove);
        }
    }

    // Шаг 1 физтика (ZombieHorde): нужен ли сейчас луч «есть ли земля». Лучи всей орды
    // менеджер пускает одной пачкой в фоновых потоках и возвращает результат в ApplyGround.
    internal bool PrepareGroundCheck(out RaycastCommand cmd)
    {
        cmd = default;
        if (transform.position.y < fallY || isKnocked || isInKnockback)
            return false;

        // Проверка земли максимум 20 раз/сек на зомби вместо каждого физического тика.
        lod = player != null && Flat(player.position - transform.position).sqrMagnitude > farDistance * farDistance
            ? farCheckMultiplier : 1f;
        // Сглаживание движения нужно только вблизи: у дальних его не видно, а на 1000 тел оно стоит пару мс.
        var interp = lod > 1f ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
        if (rb.interpolation != interp) rb.interpolation = interp;

        if (climbing || Time.time < nextGroundCheckTime)
            return false;
        nextGroundCheckTime = Time.time + 0.05f * lod;
        cmd = new RaycastCommand(transform.position + Vector3.up * groundRayStartHeight, Vector3.down,
            new QueryParameters(environmentMask, false, QueryTriggerInteraction.Ignore, false), slopeCheckDistance);
        return true;
    }

    // Промах луча даёт нулевую нормаль: normal.y = 0 < minNormalY (склон до 80°), то есть «не на земле».
    internal void ApplyGround(RaycastHit hit) => SetGround(hit.normal.y >= minNormalY, hit.normal);

    // Шаг 2 физтика (ZombieHorde): движение (бывший FixedUpdate).
    internal void FixedTick()
    {
        if (transform.position.y < fallY)
        {
            Respawn();
            return;
        }

        if (isKnocked || isInKnockback)
            return;

        if (jumping && Time.time < jumpStartTime + JumpMinAirTime)
        {
            grounded = false;
            onValidSlope = false;
        }

        rb.useGravity = !climbing;
        if (grounded && !climbing && Time.time >= nextSafePosTime)
        {
            nextSafePosTime = Time.time + 0.5f;
            lastSafePosition = transform.position;
        }

        rb.angularVelocity = Vector3.zero;
        wantsToMove = false;

        if (player == null || movementLocked || IsStunned)
        {
            if (climbing) rb.linearVelocity = Vector3.zero;
            else if (grounded) StopMoving();
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        Vector3 flatToPlayer = Flat(toPlayer);
        float flatDistanceSqr = flatToPlayer.sqrMagnitude;
        Vector3 desired = flatDistanceSqr > 0.0001f ? flatToPlayer / Mathf.Sqrt(flatDistanceSqr) : Vector3.zero;

        // Каст призывателя: стоим на месте и смотрим на игрока.
        if (castLocked && grounded && !climbing && !jumping)
        {
            StopMoving();
            FaceDirection(desired);
            return;
        }

        if (climbing)
        {
            wantsToMove = true;
            ClimbStep(desired);
            return;
        }

        if (mantleTimer > 0f)
        {
            wantsToMove = true;
            mantleTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector3(climbWallDir.x * speed, rb.linearVelocity.y, climbWallDir.z * speed);
            FaceDirection(climbWallDir);
            return;
        }

        if (jumping)
        {
            JumpStep();
            return;
        }

        if (!grounded)
        {
            wantsToMove = true;
            Vector3 velocity = rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.Lerp(horizontal, desired * speed, airControl * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
            return;
        }

        if (IsPlayerInReach(attackRange))
        {
            StopMoving();
            FaceDirection(desired);
            return;
        }

        // Режим кастера: останавливаемся на заданной дистанции от игрока.
        if (stopDistance > 0f)
        {
            float dist = Mathf.Sqrt(flatDistanceSqr);
            holdingDistance = holdingDistance
                ? dist < stopDistance + stopHysteresis
                : dist <= stopDistance;

            if (holdingDistance)
            {
                StopMoving();
                FaceDirection(desired);
                return;
            }
        }

        if (desired == Vector3.zero)
        {
            StopMoving();
            return;
        }

        wantsToMove = true;
        UpdateStuck();
        Vector3 moveDirection = ChooseMoveDirection(desired, out bool startedAction);
        if (startedAction) return;

        // StepAssist тоже делает физические запросы: не вызываем его каждый FixedUpdate.
        if (Time.time >= nextStepAssistTime)
        {
            nextStepAssistTime = Time.time + 0.12f * lod;
            StepAssist(moveDirection);
        }
        Move(moveDirection);
        FaceDirection(moveDirection);
    }

    // Упрощённый обход. Прямой путь и боковые направления пересчитываются по таймеру,
    // а прыжок рассматривается ТОЛЬКО если оба боковых пути заблокированы.
    private Vector3 ChooseMoveDirection(Vector3 desired, out bool startedAction)
    {
        startedAction = false;

        // Не прыгать и не карабкаться, когда уже можно атаковать игрока.
        bool closeToPlayer = IsPlayerInReach(attackRange + 0.35f);
        if (closeToPlayer)
        {
            steerDir = desired;
            return desired;
        }

        if (Time.time >= nextSteerTime)
        {
            nextSteerTime = Time.time + (steerInterval + (staggerChecks ? Random.Range(0f, steerInterval * 0.25f) : 0f)) * lod;
            cachedDirectBlocked = IsBlocked(desired, lookAhead, out cachedBlockNormal);

            if (!cachedDirectBlocked)
            {
                steerDir = desired;
                followSide = 0;
            }
            else
            {
                steerDir = FindSimpleDetour(desired);
            }
        }

        // Свободно прямо — двигаемся прямо.
        if (!cachedDirectBlocked)
            return desired;

        // Если можно обойти сбоку, предпочитаем обход прыжку. Это устраняет прыжки
        // на месте и постоянные попытки запрыгнуть на игрока/соседнего зомби.
        if (steerDir.sqrMagnitude > 0.001f)
            return steerDir;

        bool playerNotBelow = player.position.y > transform.position.y - climbIgnoreIfPlayerBelow;

        // Прыгать можно только если оба боковых направления заблокированы, препятствие
        // близко, и cooldown истёк. После прыжка есть длинная пауза перед новой попыткой.
        if (canJump && Time.time >= jumpBlockedUntil && Time.time >= nextJumpCheckTime && playerNotBelow)
        {
            nextJumpCheckTime = Time.time + 0.5f;
            if (TryFindJumpable(desired, out float obstacleHeight, out float wallDistance) &&
                wallDistance <= jumpTriggerDistance)
            {
                StartJump(desired, obstacleHeight);
                startedAction = true;
                return desired;
            }
        }

        bool stuck = Time.time < forceDetourUntil;
        if (!stuck && canClimb && playerNotBelow && Time.time >= climbBlockedUntil &&
            Time.time >= nextClimbCheckTime)
        {
            nextClimbCheckTime = Time.time + 0.3f;
            if (TryDetectWall(desired, out RaycastHit wall))
            {
                StartClimb(wall.normal);
                startedAction = true;
                return desired;
            }
        }

        return WallFollow(desired, cachedBlockNormal);
    }

    private Vector3 FindSimpleDetour(Vector3 desired)
    {
        // Всего три кандидата; не перебираем круг из 8–12 направлений.
        Vector3 left = Quaternion.AngleAxis(-45f, Vector3.up) * desired;
        Vector3 right = Quaternion.AngleAxis(45f, Vector3.up) * desired;
        bool leftBlocked = IsBlocked(left, lookAhead, out _);
        bool rightBlocked = IsBlocked(right, lookAhead, out _);

        if (!leftBlocked && !rightBlocked)
        {
            if (followSide == 0) followSide = Random.value < 0.5f ? -1 : 1;
            return followSide < 0 ? left : right;
        }
        if (!leftBlocked) { followSide = -1; return left; }
        if (!rightBlocked) { followSide = 1; return right; }
        return Vector3.zero;
    }

    private Vector3 WallFollow(Vector3 desired, Vector3 wallNormal)
    {
        Vector3 normal = Flat(wallNormal);
        if (normal.sqrMagnitude < 0.0001f) return desired;
        normal.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, normal);
        if (followSide == 0) followSide = Vector3.Dot(tangent, desired) >= 0f ? 1 : -1;
        return (tangent * followSide + normal * 0.15f).normalized;
    }

    private bool IsBlocked(Vector3 dir, float distance, out Vector3 normal)
    {
        normal = Vector3.zero;
        if (dir.sqrMagnitude < 0.0001f) return false;

        float castRadius = radius * 0.9f;
        Vector3 p1 = transform.position + Vector3.up * (stepHeight + castRadius);
        Vector3 p2 = transform.position + Vector3.up * Mathf.Max(stepHeight + castRadius, height - castRadius);
        if (!Physics.CapsuleCast(p1, p2, castRadius, dir, out RaycastHit hit, distance,
                environmentMask, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.normal.y >= minNormalY) return false;
        normal = hit.normal;
        return true;
    }

    private void UpdateStuck()
    {
        if (Time.time < stuckCheckTime) return;

        float movedSqr = Flat(transform.position - stuckCheckPosition).sqrMagnitude;
        float minMove = speed * 0.5f * 0.25f;
        stuckCheckPosition = transform.position;
        stuckCheckTime = Time.time + 0.5f;

        if (movedSqr < minMove * minMove)
        {
            forceDetourUntil = Time.time + stuckLimit;
            followSide = -followSide;
            steerDir = Vector3.zero;
        }
    }

    private void Move(Vector3 dir)
    {
        Vector3 velocity;
        if (grounded && onValidSlope)
            velocity = Vector3.ProjectOnPlane(dir * speed, groundNormal);
        else
            velocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);

        rb.linearVelocity = velocity;
        if (grounded && onValidSlope)
            rb.AddForce(-groundNormal * slopeStickForce, ForceMode.Acceleration);
    }

    private void StopMoving()
    {
        if (grounded && onValidSlope)
            rb.linearVelocity = Vector3.zero;
        else
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation;
        if (alignToSlope && grounded && onValidSlope)
        {
            Vector3 look = Vector3.ProjectOnPlane(direction, groundNormal);
            if (look.sqrMagnitude < 0.0001f) return;
            targetRotation = Quaternion.LookRotation(look.normalized, groundNormal);
        }
        else targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation,
            individualRotationSpeed * Time.fixedDeltaTime));
    }

    private bool TryFindJumpable(Vector3 dir, out float obstacleHeight, out float wallDistance)
    {
        obstacleHeight = 0f;
        wallDistance = 0f;
        float castRadius = radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (stepHeight + castRadius + 0.02f);

        if (!Physics.SphereCast(origin, castRadius, dir, out RaycastHit wall, lookAhead,
                environmentMask, QueryTriggerInteraction.Ignore))
            return false;
        if (wall.normal.y >= minNormalY) return false;
        Vector3 flatNormal = Flat(wall.normal);
        if (flatNormal.sqrMagnitude < 0.0001f || Vector3.Dot(flatNormal.normalized, dir) > -0.2f)
            return false;

        Vector3 probe = wall.point + dir * 0.3f;
        probe.y = transform.position.y + maxJumpHeight + 0.3f;
        if (Physics.CheckSphere(probe, 0.1f, environmentMask, QueryTriggerInteraction.Ignore))
            return false;
        if (!Physics.Raycast(probe, Vector3.down, out RaycastHit top, maxJumpHeight + 0.4f,
                environmentMask, QueryTriggerInteraction.Ignore))
            return false;
        if (top.normal.y < minNormalY) return false;

        float h = top.point.y - transform.position.y;
        if (h <= stepHeight || h > maxJumpHeight) return false;
        Vector3 capsuleBottom = top.point + Vector3.up * (radius + 0.05f);
        Vector3 capsuleTop = top.point + Vector3.up * Mathf.Max(radius + 0.06f, height - radius);
        if (Physics.CheckCapsule(capsuleBottom, capsuleTop, radius * 0.8f,
                environmentMask, QueryTriggerInteraction.Ignore))
            return false;

        obstacleHeight = h;
        wallDistance = wall.distance;
        return true;
    }

    private void StartJump(Vector3 dir, float obstacleHeight)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        float verticalSpeed = Mathf.Sqrt(2f * gravity * (obstacleHeight + jumpClearance));
        float horizontalSpeed = Mathf.Max(speed, jumpForwardSpeed);

        jumping = true;
        jumpDir = dir;
        jumpStartTime = Time.time;
        jumpStartY = transform.position.y;
        grounded = false;
        onValidSlope = false;
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(dir.x * horizontalSpeed, verticalSpeed, dir.z * horizontalSpeed);

        if (anim != null && !string.IsNullOrEmpty(jumpTrigger))
            anim.SetTrigger(jumpTrigger);
    }

    private void JumpStep()
    {
        bool landed = grounded && Time.time > jumpStartTime + JumpMinAirTime;
        bool timedOut = Time.time > jumpStartTime + 2f;
        if (landed || timedOut)
        {
            jumping = false;
            bool reachedHigherGround = transform.position.y > jumpStartY + stepHeight;
            if (reachedHigherGround)
            {
                jumpFailCount = 0;
                jumpBlockedUntil = Time.time + 1.0f;
            }
            else
            {
                jumpFailCount++;
                jumpBlockedUntil = Time.time + (jumpFailCount >= 2 ? 3f : 0.5f);
                if (jumpFailCount >= 2) jumpFailCount = 0;
            }
            return;
        }

        wantsToMove = true;
        float horizontalSpeed = Mathf.Max(speed, jumpForwardSpeed);
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(jumpDir.x * horizontalSpeed, velocity.y, jumpDir.z * horizontalSpeed);
        FaceDirection(jumpDir);
    }

    private bool TryDetectWall(Vector3 dir, out RaycastHit wallHit)
    {
        wallHit = default;
        float castRadius = radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (stepHeight + castRadius + 0.02f);
        if (!Physics.SphereCast(origin, castRadius, dir, out RaycastHit hit, wallCheckDistance,
                environmentMask, QueryTriggerInteraction.Ignore))
            return false;
        if (hit.normal.y >= minNormalY) return false;
        Vector3 flatNormal = Flat(hit.normal);
        if (flatNormal.sqrMagnitude < 0.0001f || Vector3.Dot(flatNormal.normalized, dir) > -0.2f)
            return false;
        wallHit = hit;
        return true;
    }

    private void StartClimb(Vector3 wallNormal)
    {
        Vector3 intoWall = -Flat(wallNormal);
        if (intoWall.sqrMagnitude < 0.0001f) return;
        climbing = true;
        climbWallDir = intoWall.normalized;
        climbStartY = transform.position.y;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
    }

    private void ClimbStep(Vector3 dirToPlayer)
    {
        if (transform.position.y - climbStartY > maxClimbHeight)
        {
            StopClimb();
            return;
        }

        bool playerBelow = player.position.y < transform.position.y - climbIgnoreIfPlayerBelow;
        bool playerAside = dirToPlayer.sqrMagnitude > 0.0001f &&
                           Vector3.Dot(dirToPlayer, climbWallDir) < climbAbortDot;
        if (playerBelow || playerAside)
        {
            StopClimb();
            climbBlockedUntil = Time.time + climbCooldown;
            return;
        }

        Vector3 feet = transform.position + Vector3.up * 0.15f;
        if (!Physics.Raycast(feet, climbWallDir, radius + 0.4f,
                environmentMask, QueryTriggerInteraction.Ignore))
        {
            StopClimb();
            mantleTimer = mantleTime;
            return;
        }

        Vector3 side = Vector3.ProjectOnPlane(dirToPlayer, climbWallDir);
        side.y = 0f;
        rb.linearVelocity = Vector3.up * climbSpeed + climbWallDir * 0.8f + side * speed * climbSideSpeedFactor;
        FaceDirection(climbWallDir);
    }

    private void StopClimb()
    {
        climbing = false;
        if (rb != null) rb.useGravity = true;
    }

    private void StepAssist(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Vector3 lowOrigin = transform.position + Vector3.up * 0.05f;
        if (!Physics.Raycast(lowOrigin, dir, out RaycastHit low, radius + 0.2f,
                environmentMask, QueryTriggerInteraction.Ignore) || low.normal.y >= minNormalY)
            return;

        Vector3 highOrigin = transform.position + Vector3.up * (stepHeight + 0.05f);
        if (!Physics.Raycast(highOrigin, dir, radius + 0.35f,
                environmentMask, QueryTriggerInteraction.Ignore))
            rb.position += Vector3.up * stepSmooth;
    }

    private void UpdateGround()
    {
        Vector3 start = transform.position + Vector3.up * groundRayStartHeight;
        bool hitGround = Physics.Raycast(start, Vector3.down, out RaycastHit hit, slopeCheckDistance,
            environmentMask, QueryTriggerInteraction.Ignore) && hit.normal.y >= minNormalY;
        SetGround(hitGround, hit.normal);
    }

    private void SetGround(bool hitGround, Vector3 normal)
    {
        grounded = hitGround;
        onValidSlope = hitGround && normal.y < 0.99999f;
        groundNormal = hitGround ? normal : Vector3.up;
    }

    private bool IsPlayerInReach(float range)
    {
        if (player == null) return false;
        Vector3 a = transform.position;
        Vector3 b = player.position;
        if (Mathf.Abs(a.y - b.y) > maxAttackHeightDiff) return false;
        a.y = 0f;
        b.y = 0f;
        return (a - b).sqrMagnitude <= range * range;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        if (anim != null)
        {
            anim.ResetTrigger(AttackHash);
            anim.SetTrigger(AttackHash);
        }

        if (attackAnimationDelay > 0f)
            yield return new WaitForSeconds(attackAnimationDelay);

        if (player != null && IsPlayerInReach(attackRange + 0.25f))
        {
            Health hp = player.GetComponent<Health>();
            if (hp == null) hp = player.GetComponentInParent<Health>();
            if (hp != null) hp.TakeDamage(damage);
        }

        isAttacking = false;
        attackCoroutine = null;
    }

    private void StopAttackAnimation()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        isAttacking = false;
        if (anim != null) anim.ResetTrigger(AttackHash);
    }

    private void CheckDamaged()
    {
        if (zhp == null || !zhp.wasDamaged) return;
        zhp.wasDamaged = false;
        stunUntil = Time.time + hitStunDuration;
        StopAttackAnimation();
        if (anim != null) anim.SetTrigger(DamagedHash);
    }

    public void Knockback(Vector3 direction, float force)
    {
        if (isKnocked || isInKnockback) return;
        StartCoroutine(KnockbackRoutine(direction, force));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float force)
    {
        isInKnockback = true;
        attaked = false;
        wantsToMove = false;
        StopAttackAnimation();
        StopClimb();
        jumping = false;
        mantleTimer = 0f;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Vector3 impulse = direction.normalized * force;
        impulse.y = 2f;
        rb.AddForce(impulse, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackDuration);
        float timeout = 2f;
        while (timeout > 0f)
        {
            UpdateGround();
            if (grounded) break;
            timeout -= Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        isInKnockback = false;
    }

    public void Noked()
    {
        if (isKnocked || isInKnockback) return;
        StartCoroutine(NokedRoutine());
    }

    private IEnumerator NokedRoutine()
    {
        isKnocked = true;
        attaked = false;
        wantsToMove = false;
        StopAttackAnimation();
        StopClimb();
        jumping = false;
        mantleTimer = 0f;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        yield return new WaitForSeconds(3f);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Quaternion startRotation = rb.rotation;
        Quaternion targetRotation = Quaternion.Euler(0f, startRotation.eulerAngles.y, 0f);
        float t = 0f;
        while (t < 1f)
        {
            rb.MoveRotation(Quaternion.Slerp(startRotation, targetRotation, t));
            rb.angularVelocity = Vector3.zero;
            t += Time.fixedDeltaTime * 2f;
            yield return new WaitForFixedUpdate();
        }
        rb.MoveRotation(targetRotation);
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        isKnocked = false;
    }

    public void StartDelay(float delay)
    {
        StartCoroutine(DelayedStart(delay));
    }

    private IEnumerator DelayedStart(float delay)
    {
        movementLocked = true;
        SetAnim(RunHash, false);
        SetAnim(IdleHash, true);
        yield return new WaitForSeconds(delay);
        movementLocked = false;
    }

    private void Respawn()
    {
        StopClimb();
        jumping = false;
        mantleTimer = 0f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = lastSafePosition + Vector3.up * 0.2f;
        transform.position = rb.position;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    private void SetAnim(int hash, bool value)
    {
        if (anim != null) anim.SetBool(hash, value);
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (stopDistance > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * wallCheckDistance);
        Gizmos.color = Color.magenta;
        Vector3 groundStart = transform.position + Vector3.up * groundRayStartHeight;
        Gizmos.DrawLine(groundStart, groundStart + Vector3.down * slopeCheckDistance);
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up * (height * 0.85f), player.position + Vector3.up);
            if (steerDir.sqrMagnitude > 0.0001f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, steerDir * lookAhead);
            }
        }
    }
}