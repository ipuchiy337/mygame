using UnityEngine;
using System.Collections.Generic;

// Идентификатор и контейнер состояния зомби. Логики здесь нет: ею занимается ZombieManager.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class ZombieAgent : MonoBehaviour
{
    public static readonly List<ZombieAgent> All = new List<ZombieAgent>(256);

    [Header("Ссылки")]
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

    [Header("Урон / отбрасывание")]
    public float hitStunDuration = 0.5f;
    public float knockbackDuration = 0.3f;

    [Header("Обход препятствий")]
    [Min(0.05f)] public float steerInterval = 0.25f;
    [Min(0.5f)] public float lookAhead = 2f;
    [Min(0.2f)] public float stuckLimit = 1.2f;

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

    [Header("Карабканье")]
    public bool canClimb = true;
    public float climbSpeed = 3.5f;
    public float wallCheckDistance = 0.8f;
    public float maxClimbHeight = 40f;
    public float mantleTime = 0.4f;
    public float climbIgnoreIfPlayerBelow = 1.5f;
    public float climbAbortDot = 0.3f;
    public float climbCooldown = 0.4f;
    public float climbSideSpeedFactor = 0.6f;

    [Header("Состояния")]
    public bool attaked;

    // ---------- Рантайм-состояние (у КАЖДОГО зомби своё) ----------
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public CapsuleCollider col;
    [HideInInspector] public float speed, rotSpeed, atkCooldown, radius, height, minNormalY;

    [HideInInspector] public bool isKnocked, isInKnockback, isAttacking;
    [HideInInspector] public bool grounded, onValidSlope, wantsToMove;
    [HideInInspector] public Vector3 groundNormal = Vector3.up;

    [HideInInspector] public float lockedUntil, stunUntil, lastAttackTime = -999f, attackHitAt;
    [HideInInspector] public float knockEndAt, knockTimeoutAt;
    [HideInInspector] public float nokedUntil, nokedT; [HideInInspector] public int nokedPhase;
    [HideInInspector] public Quaternion nokedStartRot;

    [HideInInspector] public Vector3 steerDir, cachedBlockNormal, stuckCheckPosition, lastSafePosition;
    [HideInInspector]
    public float nextSteerTime, nextJumpCheckTime, nextStepAssistTime,
        nextGroundCheckTime, nextClimbCheckTime, forceDetourUntil, stuckCheckTime, nextSafePosTime;
    [HideInInspector] public bool cachedDirectBlocked;
    [HideInInspector] public int followSide;

    [HideInInspector] public bool jumping;
    [HideInInspector] public Vector3 jumpDir;
    [HideInInspector] public float jumpStartTime, jumpStartY, jumpBlockedUntil;
    [HideInInspector] public int jumpFailCount;

    [HideInInspector] public bool climbing;
    [HideInInspector] public Vector3 climbWallDir;
    [HideInInspector] public float climbStartY, mantleTimer, climbBlockedUntil;

    public const float JumpMinAirTime = 0.25f;

    static PhysicsMaterial sharedNoFriction;
    public static readonly int IdleHash = Animator.StringToHash("idle");
    public static readonly int RunHash = Animator.StringToHash("run");
    public static readonly int AttackHash = Animator.StringToHash("Attack");
    public static readonly int DamagedHash = Animator.StringToHash("Damaged");

    public bool IsStunned => Time.time < stunUntil;
    public bool IsLocked => Time.time < lockedUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        radius = col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        height = col.height * transform.lossyScale.y;
        minNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);

        speed = Random.Range(minSpeed, maxSpeed);
        rotSpeed = rotationSpeed * Random.Range(0.9f, 1.1f);
        atkCooldown = attackCooldown * Random.Range(0.9f, 1.1f);

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (zeroFriction)
        {
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

        float t = Time.time;
        lastSafePosition = transform.position;
        stuckCheckPosition = transform.position;
        nextSteerTime = t + Random.Range(0f, steerInterval);
        nextStepAssistTime = t + Random.Range(0f, 0.12f);
        nextGroundCheckTime = t + Random.Range(0f, 0.05f);
        nextClimbCheckTime = t + Random.Range(0f, 0.3f);
        stuckCheckTime = t + 0.5f + Random.Range(0f, 0.5f);

        SetAnim(IdleHash, true);
        SetAnim(RunHash, false);
        if (anim != null) { anim.ResetTrigger(AttackHash); anim.ResetTrigger(DamagedHash); }
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public void SetAnim(int hash, bool value)
    {
        if (anim != null) anim.SetBool(hash, value);
    }

    // ---------- Публичный API (имена как в старом ZombieFollow) ----------
    public void StartDelay(float delay)
    {
        lockedUntil = Time.time + delay;
        SetAnim(RunHash, false);
        SetAnim(IdleHash, true);
    }

    public void Knockback(Vector3 direction, float force)
    {
        if (isKnocked || isInKnockback) return;
        isInKnockback = true;
        attaked = false;
        wantsToMove = false;
        CancelAttack();
        climbing = false; jumping = false; mantleTimer = 0f;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Vector3 impulse = direction.normalized * force;
        impulse.y = 2f;
        rb.AddForce(impulse, ForceMode.Impulse);
        knockEndAt = Time.time + knockbackDuration;
        knockTimeoutAt = knockEndAt + 2f;
    }

    public void Noked()
    {
        if (isKnocked || isInKnockback) return;
        isKnocked = true;
        attaked = false;
        wantsToMove = false;
        CancelAttack();
        climbing = false; jumping = false; mantleTimer = 0f;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        nokedPhase = 0;
        nokedUntil = Time.time + 3f;
    }

    public void CancelAttack()
    {
        isAttacking = false;
        if (anim != null) anim.ResetTrigger(AttackHash);
    }
}