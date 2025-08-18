using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{
    #region Inspector fields
    [Header("Movement")]
    public float moveSpeed;
    public float detectionRange;
    public float attackRange;

    [Header("Detection")]
    public LayerMask obstacleMask;
    public float colliderRadius = 0.18f;    // CircleCast yarıçapı
    public float raycastOffset = 0f;        // origin offset
    public float reachThreshold = 0.28f;    // hedefe "ulaştı" sayma eşiği

    [Header("Grid fallback")]
    public int gridCandidateLimit = 0;

    [Header("Avoidance")]
    public float avoidanceCastDistance = 0.45f;
    [Range(0f, 1f)] public float avoidanceStrength = 0.85f;
    #endregion

    #region References & runtime
    // References (otomatik atanır)
    private Transform player;
    private PlayerGridPoints playerGridPoints;
    public Rigidbody2D rb;
    private Animator anim;
    public EnemyAttack enemyAttack;
    public Collider2D childCollider;
    public Rigidbody2D rb_child;
    private EnemyState currentState = EnemyState.FreeMovement;

    // Runtime state
    [Header("Movement")]
    private float distanceToPlayer;
    private Vector2 facingDirection;
    private Vector2 lastFacingDirection;
    private float moveMagnitude;

    // Combat
    [Header("Combat")]
    private Vector2 attackDirection;
    public int attackPosition;
    private bool isAttacking;
    private int hurtPosition;
    public bool isHurt;
    public bool isDead;
    private int deadPosition;

    // Target
    [Header("Targeting")]
    private Vector2? currentTargetPoint;
    private bool hasLineOfSight = false;
    // Chase
    private bool isEngaged = false;         // chase session aktif mi (en son player görüldü)
    private bool fallbackActive = false;    // şu an grid fallback ile takip ediliyor mu
    private bool fallbackExhausted = false; // bu session içinde fallback tükendi mi

    // multiple ray origins for more robust player detection
    private Vector2[] RayOriginsOffsets => new Vector2[]
    {
        Vector2.zero,
        Vector2.right * 0.18f,
        Vector2.left  * 0.18f,
        Vector2.up    * 0.12f,
        Vector2.down  * 0.12f
    };
    #endregion

    #region Start
    void Start()
    {
        // ignore child collision if present
        var enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null && childCollider != null)
            Physics2D.IgnoreCollision(enemyCollider, childCollider);

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerGridPoints = player.GetComponent<PlayerGridPoints>();
        }

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }
    #endregion

    #region Update / FixedUpdate
    void Update()
    {
        if (player == null) return;
        EnemyBrain();
        MovementAnimationControl();
    }

    void FixedUpdate()
    {
        if (currentState == EnemyState.FreeMovement)
            Move();
    }
    #endregion

    #region Brain
    void EnemyBrain()
    {
        if (isAttacking || isHurt) return;

        // Update distance
        distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // If out of detection range -> reset everything
        if (distanceToPlayer > detectionRange)
        {
            ResetChaseState();
            return;
        }

        // 1) Player direct detection (raycast)
        if (PlayerVisibleByRaycast())
        {
            OnPlayerSpotted();
            return;
        }

        // Player is not visible now
        hasLineOfSight = false;

        // If not currently engaged (never saw player recently), do nothing
        if (!isEngaged)
        {
            ClearMovement();
            return;
        }

        // If engaged but fallback was already exhausted this session, stop chasing
        if (fallbackExhausted)
        {
            // end session
            isEngaged = false;
            fallbackActive = false;
            ClearMovement();
            return;
        }

        // If fallback active and we have a target, validate/use it
        if (fallbackActive && currentTargetPoint.HasValue)
        {
            if (!CanEnemySeePoint(currentTargetPoint.Value))
            {
                // current target lost -> try to find another in same session
                if (!TryFindAndSetGridTarget()) // none found
                {
                    // fallback exhausted -> end session
                    fallbackActive = false;
                    fallbackExhausted = true;
                    isEngaged = false;
                    ClearMovement();
                    return;
                }
            }
            else
            {
                // current target still visible -> move to it (or clear if reached)
                float d = Vector2.Distance(transform.position, currentTargetPoint.Value);
                if (d <= reachThreshold)
                {
                    currentTargetPoint = null; // reached; next loop will find next candidate
                }
                else
                {
                    SetMovementToward(currentTargetPoint.Value);
                    return;
                }
            }
        }

        // If fallback not active, try to start fallback once for this session
        if (!fallbackActive)
        {
            if (!TryStartFallback())
            {
                // fallback failed -> mark exhausted & end session
                fallbackExhausted = true;
                isEngaged = false;
                ClearMovement();
                return;
            }
            // if started, loop continues and one of the above blocks handles movement next frame
        }

        // If fallback active but no target currently assigned -> find one now
        if (fallbackActive && !currentTargetPoint.HasValue)
        {
            if (!TryFindAndSetGridTarget())
            {
                fallbackActive = false;
                fallbackExhausted = true;
                isEngaged = false;
                ClearMovement();
                return;
            }
            else
            {
                // new target assigned -> move toward it immediately
                SetMovementToward(currentTargetPoint.Value);
                return;
            }
        }

        // Default idle if none of above applied
        ClearMovement();
    }
    #endregion

    #region Helpers: detection, fallback selection, movement
    // Player direct detection using multiple ray origins (fast)
    private bool PlayerVisibleByRaycast()
    {
        Vector2 startBase = (Vector2)transform.position + Vector2.up * raycastOffset;
        Vector2 dir = (Vector2)player.position - startBase;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();

        int playerMask = LayerMask.GetMask("Player");
        int mask = obstacleMask | playerMask;

        foreach (var offset in RayOriginsOffsets)
        {
            Vector2 origin = startBase + offset;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, mask);
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player"))
                    return true;
                // otherwise hit an obstacle/other so continue checking other origins
            }
        }
        return false;
    }

    // Enemy's circle-based LoS to a point (considers enemy body radius)
    private bool CanEnemySeePoint(Vector2 point)
    {
        Vector2 start = (Vector2)transform.position + Vector2.up * raycastOffset;
        Vector2 dir = point - start;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();
        RaycastHit2D hit = Physics2D.CircleCast(start, colliderRadius, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    // Player also must be able to see the same point (simple raycast from player)
    private bool PlayerCanSeePoint(Vector2 point)
    {
        if (player == null) return false;
        Vector2 start = (Vector2)player.position + Vector2.up * raycastOffset;
        Vector2 dir = point - start;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();
        RaycastHit2D hit = Physics2D.Raycast(start, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    // Try to start fallback once for the current session
    private bool TryStartFallback()
    {
        var best = FindBestGridPoint();
        if (!best.HasValue) return false;
        currentTargetPoint = best.Value;
        fallbackActive = true;
        return true;
    }

    // Try to find another suitable grid target in same session
    private bool TryFindAndSetGridTarget()
    {
        var best = FindBestGridPoint();
        if (!best.HasValue) return false;
        currentTargetPoint = best.Value;
        return true;
    }

    // Finds the best grid point that BOTH Enemy and Player can see.
    // Returns null if none found.
    private Vector2? FindBestGridPoint()
    {
        if (playerGridPoints == null) return null;
        var all = playerGridPoints.GetAllPoints();
        int countChecked = 0;
        float bestScore = float.MaxValue;
        Vector2? best = null;

        foreach (var pnt in all)
        {
            // optional limit for performance
            countChecked++;
            if (gridCandidateLimit > 0 && countChecked > gridCandidateLimit) break;

            // both must see
            if (!CanEnemySeePoint(pnt)) continue;
            if (!PlayerCanSeePoint(pnt)) continue;

            // score: enemy->point + point->player (lower is better)
            float dEnemy = Vector2.Distance(transform.position, pnt);
            float dPlayer = Vector2.Distance(player.position, pnt);
            float score = dEnemy + dPlayer;
            if (score < bestScore)
            {
                bestScore = score;
                best = pnt;
            }
        }
        return best;
    }

    // Called when the Player has just been seen by rays
    private void OnPlayerSpotted()
    {
        hasLineOfSight = true;
        isEngaged = true;
        fallbackActive = false;
        fallbackExhausted = false;
        currentTargetPoint = null;

        // Attack check or move towards player
        if (distanceToPlayer > attackRange)
        {
            Vector2 desired = ((Vector2)player.position - (Vector2)transform.position).normalized;
            SetMovementTowardDirection(desired);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            moveMagnitude = 0f;
            if (enemyAttack != null && enemyAttack.attackTimer <= 0f) HandleAttack();
        }
    }

    // Sets movement toward a world point (applies avoidance)
    private void SetMovementToward(Vector2 worldPoint)
    {
        Vector2 desired = (worldPoint - (Vector2)transform.position).normalized;
        SetMovementTowardDirection(desired);
    }

    // Apply avoidance then set facingDirection & moveMagnitude
    private void SetMovementTowardDirection(Vector2 desired)
    {
        Vector2 final = ComputeAvoidanceDirection(desired);
        facingDirection = final;
        moveMagnitude = facingDirection.magnitude;
        ChangeState(EnemyState.FreeMovement);
        if (facingDirection != Vector2.zero) lastFacingDirection = facingDirection;
    }

    // stop movement variables
    private void ClearMovement()
    {
        facingDirection = Vector2.zero;
        moveMagnitude = 0f;
    }

    // Reset entire chase state (called when out of detection range)
    private void ResetChaseState()
    {
        ClearMovement();
        currentTargetPoint = null;
        hasLineOfSight = false;
        isEngaged = false;
        fallbackActive = false;
        fallbackExhausted = false;
    }

    #endregion

    #region Movement & animations
    void Move()
    {
        rb.linearVelocity = facingDirection * moveSpeed;
    }

    void MovementAnimationControl()
    {
        anim.SetFloat("MoveX", facingDirection.x);
        anim.SetFloat("MoveY", facingDirection.y);
        anim.SetFloat("MoveMagnitude", moveMagnitude);
        anim.SetFloat("LastMoveX", lastFacingDirection.x);
        anim.SetFloat("LastMoveY", lastFacingDirection.y);
    }
    #endregion

    #region Combat
    void HandleAttack()
    {
        attackDirection = (player.position - transform.position).normalized;
        attackPosition = GetDirection(attackDirection);
        StartAttack();
    }

    private void StartAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        ChangeState(EnemyState.Attack);
        anim.SetFloat("AttackSpeed", enemyAttack.attackSpeed);
        anim.SetTrigger("Attack");
        anim.SetInteger("AttackPosition", attackPosition);
    }

    public void OnAttackAnimationEnd()
    {
        lastFacingDirection = attackDirection;
        isAttacking = false;
        enemyAttack.attackTimer = enemyAttack.attackCooldown;
    }

    public void GetHurt()
    {
        isHurt = true;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
        ChangeState(EnemyState.Hurt);
        hurtPosition = GetDirection(lastFacingDirection);
        anim.SetTrigger("isHurt");
        anim.SetInteger("HurtPosition", hurtPosition);
    }

    public void OnHurtAnimationEnd()
    {
        isHurt = false;
        ChangeState(EnemyState.FreeMovement);
    }

    public void GetDeath()
    {
        rb.linearVelocity = Vector2.zero;
        deadPosition = GetDirection(lastFacingDirection);
        ChangeState(EnemyState.Dead);
        anim.SetTrigger("isDead"); 
        anim.SetInteger("DeadPosition", deadPosition);
        rb.simulated = false;
        rb_child.simulated = false;
    }

    public void Die() => Destroy(gameObject);
    #endregion


    #region LoS and Avoidance
    private int GetDirection(Vector2 direction)
    {
        float x = direction.x;
        float y = direction.y;
        if (y < 0 && Mathf.Abs(y) >= Mathf.Abs(x)) return 1; // Down
        else if (y > 0 && Mathf.Abs(y) >= Mathf.Abs(x)) return 4; // Up
        else if (x > 0 && Mathf.Abs(x) >= Mathf.Abs(y)) return 3; // Right
        else if (x < 0 && Mathf.Abs(x) >= Mathf.Abs(y)) return 2; // Left
        else return 1;
    }

    // CircleCast front check -> avoidance slide
    private Vector2 ComputeAvoidanceDirection(Vector2 desired)
    {
        if (desired.sqrMagnitude <= 0.0001f) return desired.normalized;

        Vector2 origin = transform.position;
        RaycastHit2D hit = Physics2D.CircleCast(origin, colliderRadius, desired, avoidanceCastDistance, obstacleMask);
        if (hit.collider == null) return desired.normalized;

        Vector2 n = hit.normal;
        Vector2 slide = desired - Vector2.Dot(desired, n) * n;
        if (slide.sqrMagnitude < 0.0001f)
        {
            Vector2 perp = new Vector2(desired.y, -desired.x).normalized;
            return Vector2.Lerp(desired.normalized, perp, avoidanceStrength).normalized;
        }
        else
        {
            slide.Normalize();
            return Vector2.Lerp(desired.normalized, slide, avoidanceStrength).normalized;
        }
    }
    #endregion

    #region Gizmos / debug
    void OnDrawGizmos()
    {
        // LoS bubble
        Gizmos.color = hasLineOfSight ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.18f);

        // current target
        if (currentTargetPoint.HasValue)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTargetPoint.Value);
            Gizmos.DrawSphere(currentTargetPoint.Value, 0.12f);
        }

        // grid points: color by who sees them
        if (playerGridPoints != null)
        {
            var pts = playerGridPoints.GetAllPoints();
            foreach (var p in pts)
            {
                bool enemySees = CanEnemySeePoint(p);
                bool playerSees = (player != null) ? PlayerCanSeePoint(p) : false;

                if (enemySees && playerSees) Gizmos.color = Color.green;
                else if (enemySees) Gizmos.color = Color.yellow;
                else Gizmos.color = Color.red;

                Gizmos.DrawSphere(p, 0.04f);
            }
        }
    }
    #endregion

    #region State
    private void ChangeState(EnemyState newState)
    {
        if (currentState != newState) currentState = newState;
    }
    #endregion
}

public enum EnemyState
{
    FreeMovement,
    Attack,
    Hurt,
    Dead
}
