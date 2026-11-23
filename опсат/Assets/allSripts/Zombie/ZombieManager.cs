using UnityEngine;

// Единый «мозг» орды. Своих данных о конкретных зомби не хранит:
// всё состояние лежит в ZombieAgent, менеджер лишь обрабатывает агентов по очереди.
public class ZombieManager : MonoBehaviour
{
    public static ZombieManager Instance { get; private set; }

    [Header("Цель")]
    public Transform player;

    [Header("Слои")]
    [Tooltip("Земля, стены, окружение. Слои Player и Zombie исключите.")]
    public LayerMask environmentMask = ~0;
    public LayerMask zombieMask;

    [Header("Общее")]
    public float fallY = -50f;

    [Header("Оптимизация")]
    [Tooltip("Дальше этой дистанции зомби «думают» реже.")]
    public float farDistance = 30f;
    [Min(1f)] public float farCheckMultiplier = 3f;
    [Tooltip("Макс. проверок обхода препятствий за один физ. тик на всех зомби.")]
    [Min(1)] public int maxSteerChecksPerStep = 20;

    Health playerHealth;
    int steerBudget;
    int startOffset;
    float farSqr;

    void Awake()
    {
        Instance = this;
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
        if (player != null)
        {
            environmentMask &= ~(1 << player.gameObject.layer);
            playerHealth = player.GetComponent<Health>();
            if (playerHealth == null) playerHealth = player.GetComponentInParent<Health>();
        }
        if (zombieMask.value != 0) environmentMask &= ~zombieMask;
        farSqr = farDistance * farDistance;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ====================== Update: атака, анимации ======================
    void Update()
    {
        if (player == null) return;
        float now = Time.time;
        var list = ZombieAgent.All;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var a = list[i];
            if (a != null) UpdateAgent(a, now);
        }
    }

    void UpdateAgent(ZombieAgent a, float now)
    {
        if (a.isKnocked || a.isInKnockback) return;

        // получение урона (флаг читаем и сбрасываем только у ЭТОГО зомби)
        if (a.zhp != null && a.zhp.wasDamaged)
        {
            a.zhp.wasDamaged = false;
            a.stunUntil = now + a.hitStunDuration;
            a.CancelAttack();
            if (a.anim != null) a.anim.SetTrigger(ZombieAgent.DamagedHash);
        }
        if (a.IsLocked || a.IsStunned) return;

        bool inReach = !a.climbing && a.grounded && InReach(a, a.attackRange);
        a.attaked = inReach;

        if (inReach)
        {
            a.SetAnim(ZombieAgent.RunHash, false);
            a.SetAnim(ZombieAgent.IdleHash, false);

            if (a.isAttacking)
            {
                if (now >= a.attackHitAt)
                {
                    a.isAttacking = false;
                    if (playerHealth != null && InReach(a, a.attackRange + 0.25f))
                        playerHealth.TakeDamage(a.damage);
                }
            }
            else if (now >= a.lastAttackTime + a.atkCooldown)
            {
                a.isAttacking = true;
                a.lastAttackTime = now;
                a.attackHitAt = now + a.attackAnimationDelay;
                if (a.anim != null)
                {
                    a.anim.ResetTrigger(ZombieAgent.AttackHash);
                    a.anim.SetTrigger(ZombieAgent.AttackHash);
                }
            }
        }
        else
        {
            if (a.isAttacking) a.CancelAttack();
            a.SetAnim(ZombieAgent.IdleHash, !a.wantsToMove);
            a.SetAnim(ZombieAgent.RunHash, a.wantsToMove);
        }
    }

    bool InReach(ZombieAgent a, float range)
    {
        Vector3 p = a.transform.position, t = player.position;
        if (Mathf.Abs(p.y - t.y) > a.maxAttackHeightDiff) return false;
        p.y = 0f; t.y = 0f;
        return (p - t).sqrMagnitude <= range * range;
    }

    // ====================== FixedUpdate: движение ======================
    void FixedUpdate()
    {
        var list = ZombieAgent.All;
        int n = list.Count;
        if (n == 0) return;

        steerBudget = maxSteerChecksPerStep;
        startOffset = (startOffset + 1) % n; // ротация, чтобы бюджет доставался всем поровну
        float now = Time.time;

        for (int k = 0; k < n; k++)
        {
            var a = list[(startOffset + k) % n];
            if (a != null) Step(a, now);
        }
    }

    void Step(ZombieAgent a, float now)
    {
        if (a.transform.position.y < fallY) { Respawn(a); return; }
        if (a.isInKnockback) { KnockbackStep(a, now); return; }
        if (a.isKnocked) { NokedStep(a, now); return; }

        Vector3 toPlayer = player != null ? Flat(player.position - a.transform.position) : Vector3.zero;
        float flatSqr = toPlayer.sqrMagnitude;
        float lod = flatSqr > farSqr ? farCheckMultiplier : 1f;

        if (!a.climbing && now >= a.nextGroundCheckTime)
        {
            a.nextGroundCheckTime = now + 0.05f * lod;
            UpdateGround(a);
        }

        if (a.jumping && now < a.jumpStartTime + ZombieAgent.JumpMinAirTime)
        {
            a.grounded = false;
            a.onValidSlope = false;
        }

        a.rb.useGravity = !a.climbing;
        if (a.grounded && !a.climbing && now >= a.nextSafePosTime)
        {
            a.nextSafePosTime = now + 0.5f;
            a.lastSafePosition = a.transform.position;
        }

        a.rb.angularVelocity = Vector3.zero;
        a.wantsToMove = false;

        if (player == null || a.IsLocked || a.IsStunned)
        {
            if (a.climbing) a.rb.linearVelocity = Vector3.zero;
            else if (a.grounded) StopMoving(a);
            return;
        }

        Vector3 desired = flatSqr > 0.0001f ? toPlayer / Mathf.Sqrt(flatSqr) : Vector3.zero;

        if (a.climbing)
        {
            a.wantsToMove = true;
            ClimbStep(a, desired);
            return;
        }

        if (a.mantleTimer > 0f)
        {
            a.wantsToMove = true;
            a.mantleTimer -= Time.fixedDeltaTime;
            a.rb.linearVelocity = new Vector3(a.climbWallDir.x * a.speed, a.rb.linearVelocity.y, a.climbWallDir.z * a.speed);
            FaceDirection(a, a.climbWallDir);
            return;
        }

        if (a.jumping) { JumpStep(a, now); return; }

        if (!a.grounded)
        {
            a.wantsToMove = true;
            Vector3 v = a.rb.linearVelocity;
            Vector3 h = new Vector3(v.x, 0f, v.z);
            h = Vector3.Lerp(h, desired * a.speed, a.airControl * Time.fixedDeltaTime);
            a.rb.linearVelocity = new Vector3(h.x, v.y, h.z);
            return;
        }

        if (InReach(a, a.attackRange))
        {
            StopMoving(a);
            FaceDirection(a, desired);
            return;
        }

        if (desired == Vector3.zero) { StopMoving(a); return; }

        a.wantsToMove = true;
        UpdateStuck(a, now);
        Vector3 moveDir = ChooseMoveDirection(a, desired, now, lod, out bool started);
        if (started) return;

        if (now >= a.nextStepAssistTime)
        {
            a.nextStepAssistTime = now + 0.12f * lod;
            StepAssist(a, moveDir);
        }
        Move(a, moveDir);
        FaceDirection(a, moveDir);
    }

    // ====================== Выбор направления ======================
    Vector3 ChooseMoveDirection(ZombieAgent a, Vector3 desired, float now, float lod, out bool started)
    {
        started = false;

        if (InReach(a, a.attackRange + 0.35f))
        {
            a.steerDir = desired;
            return desired;
        }

        if (now >= a.nextSteerTime && steerBudget > 0)
        {
            steerBudget--;
            a.nextSteerTime = now + (a.steerInterval + Random.Range(0f, a.steerInterval * 0.25f)) * lod;
            a.cachedDirectBlocked = IsBlocked(a, desired, a.lookAhead, out a.cachedBlockNormal);

            if (!a.cachedDirectBlocked) { a.steerDir = desired; a.followSide = 0; }
            else a.steerDir = FindSimpleDetour(a, desired);
        }

        if (!a.cachedDirectBlocked) return desired;
        if (a.steerDir.sqrMagnitude > 0.001f) return a.steerDir;

        bool playerNotBelow = player.position.y > a.transform.position.y - a.climbIgnoreIfPlayerBelow;

        if (a.canJump && now >= a.jumpBlockedUntil && now >= a.nextJumpCheckTime && playerNotBelow)
        {
            a.nextJumpCheckTime = now + 0.5f;
            if (TryFindJumpable(a, desired, out float h, out float wallDist) && wallDist <= a.jumpTriggerDistance)
            {
                StartJump(a, desired, h, now);
                started = true;
                return desired;
            }
        }

        bool stuck = now < a.forceDetourUntil;
        if (!stuck && a.canClimb && playerNotBelow && now >= a.climbBlockedUntil && now >= a.nextClimbCheckTime)
        {
            a.nextClimbCheckTime = now + 0.3f;
            if (TryDetectWall(a, desired, out RaycastHit wall))
            {
                StartClimb(a, wall.normal);
                started = true;
                return desired;
            }
        }

        return WallFollow(a, desired, a.cachedBlockNormal);
    }

    Vector3 FindSimpleDetour(ZombieAgent a, Vector3 desired)
    {
        Vector3 left = Quaternion.AngleAxis(-45f, Vector3.up) * desired;
        Vector3 right = Quaternion.AngleAxis(45f, Vector3.up) * desired;
        bool lb = IsBlocked(a, left, a.lookAhead, out _);
        bool rb2 = IsBlocked(a, right, a.lookAhead, out _);

        if (!lb && !rb2)
        {
            if (a.followSide == 0) a.followSide = Random.value < 0.5f ? -1 : 1;
            return a.followSide < 0 ? left : right;
        }
        if (!lb) { a.followSide = -1; return left; }
        if (!rb2) { a.followSide = 1; return right; }
        return Vector3.zero;
    }

    Vector3 WallFollow(ZombieAgent a, Vector3 desired, Vector3 wallNormal)
    {
        Vector3 normal = Flat(wallNormal);
        if (normal.sqrMagnitude < 0.0001f) return desired;
        normal.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, normal);
        if (a.followSide == 0) a.followSide = Vector3.Dot(tangent, desired) >= 0f ? 1 : -1;
        return (tangent * a.followSide + normal * 0.15f).normalized;
    }

    bool IsBlocked(ZombieAgent a, Vector3 dir, float distance, out Vector3 normal)
    {
        normal = Vector3.zero;
        if (dir.sqrMagnitude < 0.0001f) return false;

        float cr = a.radius * 0.9f;
        Vector3 pos = a.transform.position;
        Vector3 p1 = pos + Vector3.up * (a.stepHeight + cr);
        Vector3 p2 = pos + Vector3.up * Mathf.Max(a.stepHeight + cr, a.height - cr);
        if (!Physics.CapsuleCast(p1, p2, cr, dir, out RaycastHit hit, distance,
                environmentMask, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.normal.y >= a.minNormalY) return false;
        normal = hit.normal;
        return true;
    }

    void UpdateStuck(ZombieAgent a, float now)
    {
        if (now < a.stuckCheckTime) return;

        float movedSqr = Flat(a.transform.position - a.stuckCheckPosition).sqrMagnitude;
        float minMove = a.speed * 0.5f * 0.25f;
        a.stuckCheckPosition = a.transform.position;
        a.stuckCheckTime = now + 0.5f;

        if (movedSqr < minMove * minMove)
        {
            a.forceDetourUntil = now + a.stuckLimit;
            a.followSide = -a.followSide;
            a.steerDir = Vector3.zero;
        }
    }

    // ====================== Движение ======================
    void Move(ZombieAgent a, Vector3 dir)
    {
        Vector3 v;
        if (a.grounded && a.onValidSlope)
            v = Vector3.ProjectOnPlane(dir * a.speed, a.groundNormal);
        else
            v = new Vector3(dir.x * a.speed, a.rb.linearVelocity.y, dir.z * a.speed);

        a.rb.linearVelocity = v;
        if (a.grounded && a.onValidSlope)
            a.rb.AddForce(-a.groundNormal * a.slopeStickForce, ForceMode.Acceleration);
    }

    void StopMoving(ZombieAgent a)
    {
        if (a.grounded && a.onValidSlope) a.rb.linearVelocity = Vector3.zero;
        else a.rb.linearVelocity = new Vector3(0f, a.rb.linearVelocity.y, 0f);
    }

    void FaceDirection(ZombieAgent a, Vector3 direction)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion target;
        if (a.alignToSlope && a.grounded && a.onValidSlope)
        {
            Vector3 look = Vector3.ProjectOnPlane(direction, a.groundNormal);
            if (look.sqrMagnitude < 0.0001f) return;
            target = Quaternion.LookRotation(look.normalized, a.groundNormal);
        }
        else target = Quaternion.LookRotation(direction, Vector3.up);

        a.rb.MoveRotation(Quaternion.RotateTowards(a.rb.rotation, target, a.rotSpeed * Time.fixedDeltaTime));
    }

    void StepAssist(ZombieAgent a, Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Vector3 pos = a.transform.position;
        if (!Physics.Raycast(pos + Vector3.up * 0.05f, dir, out RaycastHit low, a.radius + 0.2f,
                environmentMask, QueryTriggerInteraction.Ignore) || low.normal.y >= a.minNormalY)
            return;

        if (!Physics.Raycast(pos + Vector3.up * (a.stepHeight + 0.05f), dir, a.radius + 0.35f,
                environmentMask, QueryTriggerInteraction.Ignore))
            a.rb.position += Vector3.up * a.stepSmooth;
    }

    void UpdateGround(ZombieAgent a)
    {
        Vector3 start = a.transform.position + Vector3.up * a.groundRayStartHeight;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, a.slopeCheckDistance,
                environmentMask, QueryTriggerInteraction.Ignore) && hit.normal.y >= a.minNormalY)
        {
            a.grounded = true;
            a.groundNormal = hit.normal;
            a.onValidSlope = hit.normal.y < 0.99999f;
        }
        else
        {
            a.grounded = false;
            a.onValidSlope = false;
            a.groundNormal = Vector3.up;
        }
    }

    // ====================== Прыжок ======================
    bool TryFindJumpable(ZombieAgent a, Vector3 dir, out float obstacleHeight, out float wallDistance)
    {
        obstacleHeight = 0f; wallDistance = 0f;
        float cr = a.radius * 0.9f;
        Vector3 pos = a.transform.position;
        Vector3 origin = pos + Vector3.up * (a.stepHeight + cr + 0.02f);

        if (!Physics.SphereCast(origin, cr, dir, out RaycastHit wall, a.lookAhead,
                environmentMask, QueryTriggerInteraction.Ignore)) return false;
        if (wall.normal.y >= a.minNormalY) return false;
        Vector3 fn = Flat(wall.normal);
        if (fn.sqrMagnitude < 0.0001f || Vector3.Dot(fn.normalized, dir) > -0.2f) return false;

        Vector3 probe = wall.point + dir * 0.3f;
        probe.y = pos.y + a.maxJumpHeight + 0.3f;
        if (Physics.CheckSphere(probe, 0.1f, environmentMask, QueryTriggerInteraction.Ignore)) return false;
        if (!Physics.Raycast(probe, Vector3.down, out RaycastHit top, a.maxJumpHeight + 0.4f,
                environmentMask, QueryTriggerInteraction.Ignore)) return false;
        if (top.normal.y < a.minNormalY) return false;

        float h = top.point.y - pos.y;
        if (h <= a.stepHeight || h > a.maxJumpHeight) return false;
        Vector3 cb = top.point + Vector3.up * (a.radius + 0.05f);
        Vector3 ct = top.point + Vector3.up * Mathf.Max(a.radius + 0.06f, a.height - a.radius);
        if (Physics.CheckCapsule(cb, ct, a.radius * 0.8f, environmentMask, QueryTriggerInteraction.Ignore))
            return false;

        obstacleHeight = h;
        wallDistance = wall.distance;
        return true;
    }

    void StartJump(ZombieAgent a, Vector3 dir, float obstacleHeight, float now)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float vy = Mathf.Sqrt(2f * g * (obstacleHeight + a.jumpClearance));
        float hs = Mathf.Max(a.speed, a.jumpForwardSpeed);

        a.jumping = true;
        a.jumpDir = dir;
        a.jumpStartTime = now;
        a.jumpStartY = a.transform.position.y;
        a.grounded = false;
        a.onValidSlope = false;
        a.rb.useGravity = true;
        a.rb.linearVelocity = new Vector3(dir.x * hs, vy, dir.z * hs);

        if (a.anim != null && !string.IsNullOrEmpty(a.jumpTrigger))
            a.anim.SetTrigger(a.jumpTrigger);
    }

    void JumpStep(ZombieAgent a, float now)
    {
        bool landed = a.grounded && now > a.jumpStartTime + ZombieAgent.JumpMinAirTime;
        bool timedOut = now > a.jumpStartTime + 2f;
        if (landed || timedOut)
        {
            a.jumping = false;
            if (a.transform.position.y > a.jumpStartY + a.stepHeight)
            {
                a.jumpFailCount = 0;
                a.jumpBlockedUntil = now + 1f;
            }
            else
            {
                a.jumpFailCount++;
                a.jumpBlockedUntil = now + (a.jumpFailCount >= 2 ? 3f : 0.5f);
                if (a.jumpFailCount >= 2) a.jumpFailCount = 0;
            }
            return;
        }

        a.wantsToMove = true;
        float hs = Mathf.Max(a.speed, a.jumpForwardSpeed);
        Vector3 v = a.rb.linearVelocity;
        a.rb.linearVelocity = new Vector3(a.jumpDir.x * hs, v.y, a.jumpDir.z * hs);
        FaceDirection(a, a.jumpDir);
    }

    // ====================== Карабканье ======================
    bool TryDetectWall(ZombieAgent a, Vector3 dir, out RaycastHit wallHit)
    {
        wallHit = default;
        float cr = a.radius * 0.9f;
        Vector3 origin = a.transform.position + Vector3.up * (a.stepHeight + cr + 0.02f);
        if (!Physics.SphereCast(origin, cr, dir, out RaycastHit hit, a.wallCheckDistance,
                environmentMask, QueryTriggerInteraction.Ignore)) return false;
        if (hit.normal.y >= a.minNormalY) return false;
        Vector3 fn = Flat(hit.normal);
        if (fn.sqrMagnitude < 0.0001f || Vector3.Dot(fn.normalized, dir) > -0.2f) return false;
        wallHit = hit;
        return true;
    }

    void StartClimb(ZombieAgent a, Vector3 wallNormal)
    {
        Vector3 into = -Flat(wallNormal);
        if (into.sqrMagnitude < 0.0001f) return;
        a.climbing = true;
        a.climbWallDir = into.normalized;
        a.climbStartY = a.transform.position.y;
        a.rb.useGravity = false;
        a.rb.linearVelocity = Vector3.zero;
    }

    void StopClimb(ZombieAgent a)
    {
        a.climbing = false;
        a.rb.useGravity = true;
    }

    void ClimbStep(ZombieAgent a, Vector3 dirToPlayer)
    {
        Vector3 pos = a.transform.position;
        if (pos.y - a.climbStartY > a.maxClimbHeight) { StopClimb(a); return; }

        bool playerBelow = player.position.y < pos.y - a.climbIgnoreIfPlayerBelow;
        bool playerAside = dirToPlayer.sqrMagnitude > 0.0001f &&
                           Vector3.Dot(dirToPlayer, a.climbWallDir) < a.climbAbortDot;
        if (playerBelow || playerAside)
        {
            StopClimb(a);
            a.climbBlockedUntil = Time.time + a.climbCooldown;
            return;
        }

        if (!Physics.Raycast(pos + Vector3.up * 0.15f, a.climbWallDir, a.radius + 0.4f,
                environmentMask, QueryTriggerInteraction.Ignore))
        {
            StopClimb(a);
            a.mantleTimer = a.mantleTime;
            return;
        }

        Vector3 side = Vector3.ProjectOnPlane(dirToPlayer, a.climbWallDir);
        side.y = 0f;
        a.rb.linearVelocity = Vector3.up * a.climbSpeed + a.climbWallDir * 0.8f + side * a.speed * a.climbSideSpeedFactor;
        FaceDirection(a, a.climbWallDir);
    }

    // ====================== Откидывание / Noked ======================
    void KnockbackStep(ZombieAgent a, float now)
    {
        if (now < a.knockEndAt) return;

        UpdateGround(a);
        if (a.grounded || now >= a.knockTimeoutAt)
        {
            a.rb.linearVelocity = new Vector3(0f, a.rb.linearVelocity.y, 0f);
            a.isInKnockback = false;
        }
    }

    void NokedStep(ZombieAgent a, float now)
    {
        if (a.nokedPhase == 0)
        {
            if (now < a.nokedUntil) return;
            a.rb.linearVelocity = Vector3.zero;
            a.rb.angularVelocity = Vector3.zero;
            a.nokedStartRot = a.rb.rotation;
            a.nokedT = 0f;
            a.nokedPhase = 1;
            return;
        }

        Quaternion target = Quaternion.Euler(0f, a.nokedStartRot.eulerAngles.y, 0f);
        a.rb.MoveRotation(Quaternion.Slerp(a.nokedStartRot, target, a.nokedT));
        a.rb.angularVelocity = Vector3.zero;
        a.nokedT += Time.fixedDeltaTime * 2f;

        if (a.nokedT >= 1f)
        {
            a.rb.MoveRotation(target);
            a.rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            a.isKnocked = false;
            a.nokedPhase = 0;
        }
    }

    void Respawn(ZombieAgent a)
    {
        StopClimb(a);
        a.jumping = false;
        a.mantleTimer = 0f;
        a.rb.linearVelocity = Vector3.zero;
        a.rb.angularVelocity = Vector3.zero;
        a.rb.position = a.lastSafePosition + Vector3.up * 0.2f;
        a.transform.position = a.rb.position;
        a.transform.rotation = Quaternion.Euler(0f, a.transform.eulerAngles.y, 0f);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
}