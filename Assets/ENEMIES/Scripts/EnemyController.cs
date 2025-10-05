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
    public float colliderRadius = 0.18f;
    public float raycastOffset = 0f;
    public float reachThreshold = 0.28f;

    [Header("Grid fallback")]
    public int gridCandidateLimit = 0;

    [Header("Avoidance")]
    public float avoidanceCastDistance = 0.45f;
    [Range(0f, 1f)] public float avoidanceStrength = 0.85f;
    #endregion

    #region References & runtime
    // References 
    private Transform player;
    private PlayerController playerController;
    private PlayerGridPoints playerGridPoints;
    public Rigidbody2D rb;
    private Animator anim;
    public EnemyAttack enemyAttack;
    public Collider2D childCollider;
    public Rigidbody2D rb_child;

    private EnemyState currentState = EnemyState.FreeMovement;

    // Movement runtime
    private float distanceToPlayer;
    private Vector2 facingDirection;
    private Vector2 lastFacingDirection;
    private float moveMagnitude;

    // Combat runtime
    private Vector2 attackDirection;
    public int attackPosition;
    private bool isAttacking;
    private int hurtPosition;
    public bool isHurt;
    public bool isDead;
    private int deadPosition;
    public event Action OnEnemyDied;

    // Targeting / chase flags
    private Vector2? currentTargetPoint;
    private bool hasLineOfSight = false;
    private bool isEngaged = false;
    private bool fallbackActive = false;
    private bool fallbackExhausted = false;
    #endregion

    #region Ray offsets
    private static readonly Vector2[] rayOriginsOffsets = new Vector2[]
    {
        Vector2.zero,
        Vector2.right * 0.18f,
        Vector2.left  * 0.18f,
        Vector2.up    * 0.12f,
        Vector2.down  * 0.12f
    };
    #endregion

    #region Unity
    void Start()
    {
        var enemyCollider = GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(enemyCollider, childCollider);

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerGridPoints = player.GetComponent<PlayerGridPoints>();
            
            playerController = p.GetComponent<PlayerController>();
        }

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null || (playerController != null && playerController.isDead))
        {
            ClearMovement();
            rb.linearVelocity = Vector2.zero;
            MovementAnimationControl();
            return;
        }

        EnemyBrain();
        MovementAnimationControl();
    }

    void FixedUpdate()
    {
        if (currentState == EnemyState.FreeMovement) Move();
    }
    #endregion

    #region Brain
    void EnemyBrain()
    {
        if (isAttacking || isHurt) return;

        distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange)
        {
            ResetChaseState();
            return;
        }

        if (PlayerVisibleByRaycast())
        {
            OnPlayerSpotted();
            return;
        }

        // player not visible
        hasLineOfSight = false;

        if (!isEngaged)
        {
            ClearMovement();
            return;
        }

        if (fallbackExhausted)
        {
            isEngaged = false;
            fallbackActive = false;
            ClearMovement();
            return;
        }

        if (fallbackActive && currentTargetPoint.HasValue)
        {
            if (!CanEnemySeePoint(currentTargetPoint.Value))
            {
                if (!TryFindAndSetGridTarget())
                {
                    fallbackActive = false;
                    fallbackExhausted = true;
                    isEngaged = false;
                    ClearMovement();
                    return;
                }
            }
            else
            {
                float d = Vector2.Distance(transform.position, currentTargetPoint.Value);
                if (d <= reachThreshold)
                {
                    currentTargetPoint = null;
                }
                else
                {
                    SetMovementToward(currentTargetPoint.Value);
                    return;
                }
            }
        }

        if (!fallbackActive)
        {
            if (!TryStartFallback())
            {
                fallbackExhausted = true;
                isEngaged = false;
                ClearMovement();
                return;
            }
        }

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
                SetMovementToward(currentTargetPoint.Value);
                return;
            }
        }

        ClearMovement();
    }
    #endregion

    #region Raycasting and Fallback point finding
    private Vector2 StartPos() => (Vector2)transform.position + Vector2.up * raycastOffset;

    private bool PlayerVisibleByRaycast()
    {
        Vector2 start = StartPos();
        Vector2 dir = (Vector2)player.position - start;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();

        int playerMask = LayerMask.GetMask("Player");
        int mask = obstacleMask | playerMask;

        foreach (var offset in rayOriginsOffsets)
        {
            RaycastHit2D hit = Physics2D.Raycast(start + offset, dir, dist, mask);
            if (hit.collider != null && hit.collider.CompareTag("Player")) return true;
        }
        return false;
    }

    private bool CanEnemySeePoint(Vector2 point)
    {
        Vector2 start = StartPos();
        Vector2 dir = point - start;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();
        RaycastHit2D hit = Physics2D.CircleCast(start, colliderRadius, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    private bool CanPlayerSeePoint(Vector2 point)
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

    private bool TryStartFallback()
    {
        var best = FindBestGridPoint();
        if (!best.HasValue) return false;
        currentTargetPoint = best.Value;
        fallbackActive = true;
        return true;
    }

    private bool TryFindAndSetGridTarget()
    {
        var best = FindBestGridPoint();
        if (!best.HasValue) return false;
        currentTargetPoint = best.Value;
        return true;
    }

    private Vector2? FindBestGridPoint()
    {
        if (playerGridPoints == null) return null;

        var all = playerGridPoints.GetAllPoints();

        var list = new List<(Vector2 point, float dist)>(all.Count);
        for (int i = 0; i < all.Count; i++)
        {
            var p = all[i];
            float dEnemy = Vector2.Distance((Vector2)transform.position, p);
            list.Add((p, dEnemy));
        }

        list.Sort((a, b) => a.dist.CompareTo(b.dist));

        int maxChecks = (gridCandidateLimit > 0) ? Mathf.Min(gridCandidateLimit, list.Count) : list.Count;

        float bestScore = float.MaxValue;
        Vector2? best = null;
        int checkedCount = 0;

        for (int i = 0; i < list.Count && checkedCount < maxChecks; i++)
        {
            var pnt = list[i].point;
            checkedCount++;

            // both must see
            if (!CanEnemySeePoint(pnt)) continue;
            if (!CanPlayerSeePoint(pnt)) continue;

            float dEnemy = list[i].dist;
            float dPlayer = Vector2.Distance((Vector2)player.position, pnt);
            float score = dEnemy + dPlayer; // you can tweak weights here

            if (score < bestScore)
            {
                bestScore = score;
                best = pnt;
            }
        }

    return best;
    }
    #endregion

    #region Movement helpers
    private void OnPlayerSpotted()
    {
        hasLineOfSight = true;
        isEngaged = true;
        fallbackActive = false;
        fallbackExhausted = false;
        currentTargetPoint = null;

        if (distanceToPlayer > attackRange)
        {
            Vector2 desired = ((Vector2)player.position - (Vector2)transform.position).normalized;
            SetMovementTowardDirection(desired);
        }
        else
        {
            ClearMovement();
            rb.linearVelocity = Vector2.zero;
            if (enemyAttack != null && enemyAttack.attackTimer <= 0f) HandleAttack();
        }
    }

    private void SetMovementToward(Vector2 worldPoint)
    {
        Vector2 desired = (worldPoint - (Vector2)transform.position).normalized;
        SetMovementTowardDirection(desired);
    }

    private void SetMovementTowardDirection(Vector2 desired)
    {
        Vector2 final = ComputeAvoidanceDirection(desired);
        facingDirection = final;
        moveMagnitude = facingDirection.magnitude;
        ChangeState(EnemyState.FreeMovement);
        if (facingDirection != Vector2.zero) lastFacingDirection = facingDirection;
    }

    private void ClearMovement()
    {
        facingDirection = Vector2.zero;
        moveMagnitude = 0f;
    }

    private void ResetChaseState()
    {
        ClearMovement();
        currentTargetPoint = null;
        hasLineOfSight = false;
        isEngaged = false;
        fallbackActive = false;
        fallbackExhausted = false;
    }

    void Move()
    {
        rb.linearVelocity = facingDirection * moveSpeed;
    }

    void MovementAnimationControl()
    {
        if (anim == null) return;
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
        if (enemyAttack != null) anim.SetFloat("AttackSpeed", enemyAttack.attackSpeed);
        if (anim != null)
        {
            anim.SetTrigger("Attack");
            anim.SetInteger("AttackPosition", attackPosition);
        }
    }

    public void OnAttackAnimationEnd()
    {
        lastFacingDirection = attackDirection;
        isAttacking = false;
        if (enemyAttack != null) enemyAttack.attackTimer = enemyAttack.attackCooldown;
    }

    public void GetHurt()
    {
        isHurt = true;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
        ChangeState(EnemyState.Hurt);
        hurtPosition = GetDirection(lastFacingDirection);
        if (anim != null)
        {
            anim.SetTrigger("isHurt");
            anim.SetInteger("HurtPosition", hurtPosition);
        }
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
        isDead = true;
        if (anim != null)
        {
            anim.SetTrigger("isDead");
            anim.SetInteger("DeadPosition", deadPosition);
            Debug.Log("Enemy died");
        }
        if (rb != null) rb.simulated = false;
        if (rb_child != null) rb_child.simulated = false;
    }

    public void Die()
    {
        if (EnemySpawnManager.Instance != null)
        EnemySpawnManager.Instance.NotifyEnemyDied();

        Destroy(gameObject);
    }
    #endregion

    #region LoS & Avoidance
    private int GetDirection(Vector2 direction)
    {
        float x = direction.x, y = direction.y;
        if (y < 0 && Mathf.Abs(y) >= Mathf.Abs(x)) return 1;
        if (y > 0 && Mathf.Abs(y) >= Mathf.Abs(x)) return 4;
        if (x > 0 && Mathf.Abs(x) >= Mathf.Abs(y)) return 3;
        if (x < 0 && Mathf.Abs(x) >= Mathf.Abs(y)) return 2;
        return 1;
    }

    private Vector2 ComputeAvoidanceDirection(Vector2 desired)
    {
        if (desired.sqrMagnitude <= 0.0001f) return desired.normalized;

        if (avoidanceStrength <= 0f || avoidanceCastDistance <= 0f)
            return desired.normalized;

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, colliderRadius, desired, avoidanceCastDistance, obstacleMask);
        if (hit.collider == null) return desired.normalized;

        Vector2 n = hit.normal;
        Vector2 slide = desired - Vector2.Dot(desired, n) * n;
        if (slide.sqrMagnitude < 0.0001f)
        {
            Vector2 perp = new Vector2(desired.y, -desired.x).normalized;
            return Vector2.Lerp(desired.normalized, perp, avoidanceStrength).normalized;
        }
        slide.Normalize();
        return Vector2.Lerp(desired.normalized, slide, avoidanceStrength).normalized;
    }
    #endregion

    #region Gizmos
    void OnDrawGizmos()
    {
        Gizmos.color = hasLineOfSight ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.18f);

        if (currentTargetPoint.HasValue)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTargetPoint.Value);
            Gizmos.DrawSphere(currentTargetPoint.Value, 0.12f);
        }

        if (playerGridPoints != null)
        {
            var pts = playerGridPoints.GetAllPoints();
            foreach (var p in pts)
            {
                bool enemySees = CanEnemySeePoint(p);
                bool playerSees = (player != null) ? CanPlayerSeePoint(p) : false;
                Gizmos.color = enemySees && playerSees ? Color.green : (enemySees ? Color.yellow : Color.red);
                Gizmos.DrawSphere(p, 0.04f);
            }
        }
    }
    #endregion

    #region State helper
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