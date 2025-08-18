using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float detectionRange = 5f;
    public float attackRange = 1f;
    public float stopDistanceWhileCooldown = 0.5f;

    private float distanceToPlayer;
    private Vector2 facingDirection;
    private Vector2 lastFacingDirection;
    private float moveMagnitude;

    [Header("Combat")]
    private Vector2 attackDirection;
    public int attackPosition;
    private bool isAttacking;
    private int hurtPosition;
    public bool isHurt;
    public bool isDead;
    private int deadPosition;

    [Header("References")]
    private Transform player;
    private PlayerAttack playerAttack;
    private PlayerBreadcrumbs playerBreadcrumbs;
    public Rigidbody2D rb;
    private Animator anim;
    public EnemyHealth enemyHealth;
    public EnemyAttack enemyAttack;
    public Rigidbody2D rb_child;
    public Collider2D childCollider;

    [Header("States")]
    private EnemyState currentState = EnemyState.FreeMovement;

    [Header("Detection (CircleCast)")]
    public LayerMask obstacleMask;             // obstacle layers
    public float colliderRadius = 0.32f;       // düşman collider yarıçapına yakın değer
    public float raycastOffset = 0.0f;         // origin offset (gerekirse)
    public bool hasLineOfSight = false;
    private Vector2? currentCrumbTarget;

    [Header("Crumb / Reach settings")]
    public float crumbReachThreshold = 0.28f;    // hedefe "yakın sayılma" eşiği (tune et)

    void Start()
    {
        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null && childCollider != null)
            Physics2D.IgnoreCollision(enemyCollider, childCollider);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerAttack = player.GetComponent<PlayerAttack>();
            playerBreadcrumbs = player.GetComponent<PlayerBreadcrumbs>();
        }

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        EnemyBrain();
        MovementAnimationControl();
    }

    void FixedUpdate()
    {
        if (currentState == EnemyState.FreeMovement)
        {
            Move();
        }
    }

    #region Enemy Brain
    void EnemyBrain()
    {
        if (isAttacking || isHurt) return;

        distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 1) Player LoS kontrolü (öncelik)
        if (HasLineOfSight(player.position, true))
        {
            hasLineOfSight = true;
            currentCrumbTarget = null;

            if (distanceToPlayer > detectionRange)
            {
                // Çok uzak, takip etmeyi bırak
                facingDirection = Vector2.zero;
                moveMagnitude = 0;
                return;
            }

            if (distanceToPlayer > attackRange)
            {
                Vector2 desired = ((Vector2)player.position - (Vector2)transform.position).normalized;
                facingDirection = desired;
                moveMagnitude = facingDirection.magnitude;
                ChangeState(EnemyState.FreeMovement);
                if (facingDirection != Vector2.zero) lastFacingDirection = facingDirection;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                moveMagnitude = 0;
                if (enemyAttack != null && enemyAttack.attackTimer <= 0f) HandleAttack();
            }

            return;
        }

        // 2) Player görünmüyorsa -> breadcrumb fallback (sondan başa görünür crumb arama)
        hasLineOfSight = false;

        // Eğer zaten bir crumb hedefimiz varsa, önce onun hâlâ erişilebilir olup olmadığına bak
        if (currentCrumbTarget.HasValue)
        {
            bool stillVisible = HasLineOfSight(currentCrumbTarget.Value, false);
            if (!stillVisible)
            {
                currentCrumbTarget = null;
            }
            else
            {
                float distToCurrent = Vector2.Distance(transform.position, currentCrumbTarget.Value);
                if (distToCurrent <= crumbReachThreshold)
                {
                    // ulaştı say
                    currentCrumbTarget = null;
                }
                else
                {
                    // hedefe yönel
                    Vector2 desired = (currentCrumbTarget.Value - (Vector2)transform.position).normalized;
                    facingDirection = desired;
                    moveMagnitude = facingDirection.magnitude;
                    ChangeState(EnemyState.FreeMovement);
                    if (facingDirection != Vector2.zero) lastFacingDirection = facingDirection;
                    return;
                }
            }
        }

        // Eğer şu an hedef yoksa, sondan başa görünür ilk crumb'u al
        Vector2? newCrumb = playerBreadcrumbs != null ? playerBreadcrumbs.GetLastVisibleCrumb(transform, this) : null;
        if (newCrumb.HasValue)
        {
            currentCrumbTarget = newCrumb.Value;
            float distToCrumb = Vector2.Distance(transform.position, currentCrumbTarget.Value);
            if (distToCrumb <= crumbReachThreshold)
            {
                // Çok yakınsa atla
                currentCrumbTarget = null;
                facingDirection = Vector2.zero;
                moveMagnitude = 0;
            }
            else
            {
                Vector2 desired = (currentCrumbTarget.Value - (Vector2)transform.position).normalized;
                facingDirection = desired;
                moveMagnitude = facingDirection.magnitude;
                ChangeState(EnemyState.FreeMovement);
                if (facingDirection != Vector2.zero) lastFacingDirection = facingDirection;
            }
        }
        else
        {
            // Hiç crumb yok -> bekle
            facingDirection = Vector2.zero;
            moveMagnitude = 0;
            currentCrumbTarget = null;
        }
    }

    void OnDrawGizmos()
    {
        // LoS durum balonu (göster/kapamak istiyorsan burayı düzenleyebilirsin)
        Gizmos.color = hasLineOfSight ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.18f);

        // Hedef crumb çizgisi
        if (currentCrumbTarget.HasValue)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentCrumbTarget.Value);
            Gizmos.DrawSphere(currentCrumbTarget.Value, 0.12f);
        }
    }
    #endregion

    #region Movement
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

    #region Hurt & Death
    public void GetHurt()
    {
        isHurt = true;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
        ChangeState(EnemyState.Hurt);
        hurtPosition = GetDirection(lastFacingDirection);
        if (anim != null) { anim.SetTrigger("isHurt"); anim.SetInteger("HurtPosition", hurtPosition); }
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
        if (anim != null) { anim.SetTrigger("isDead"); anim.SetInteger("DeadPosition", deadPosition); }
        if (rb != null) rb.simulated = false;
        if (rb_child != null) rb_child.simulated = false;
    }

    public void Die() => Destroy(gameObject);
    #endregion

    #region Attack
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
        if (enemyAttack != null && anim != null) anim.SetFloat("AttackSpeed", enemyAttack.attackSpeed);
        if (anim != null) { anim.SetTrigger("Attack"); anim.SetInteger("AttackPosition", attackPosition); }
    }

    public void OnAttackAnimationEnd()
    {
        lastFacingDirection = attackDirection;
        isAttacking = false;
        if (enemyAttack != null) enemyAttack.attackTimer = enemyAttack.attackCooldown;
    }
    #endregion

    #region State
    private void ChangeState(EnemyState newState)
    {
        if (currentState != newState)
            currentState = newState;
    }
    #endregion

    #region Utility
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

    // CircleCast tabanlı LoS. targetIsPlayer=true ise Player olup olmadığına göre karar verir.
    // targetIsPlayer=true: CircleCastAll ile en öndeki nesneye bakılır; Player en öndeyse true, aksi halde false.
    // targetIsPlayer=false: sadece obstacleMask ile CircleCast yapıp obstacle yoksa true döner.
    public bool HasLineOfSight(Vector2 targetPos, bool targetIsPlayer = true)
    {
        Vector2 start = (Vector2)transform.position + Vector2.up * raycastOffset;
        Vector2 dir = targetPos - start;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();

        if (targetIsPlayer)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(start, colliderRadius, dir, dist);
            if (hits == null || hits.Length == 0) return false;

            // sort by distance (small -> large) without LINQ
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.collider == null) continue;

                bool isPlayer = h.collider.CompareTag("Player");
                bool isObstacle = ((obstacleMask.value & (1 << h.collider.gameObject.layer)) != 0) && !h.collider.isTrigger;

                if (isPlayer) return true;
                if (isObstacle) return false;
                // otherwise continue (trigger/other)
            }

            return false;
        }
        else
        {
            RaycastHit2D hit = Physics2D.CircleCast(start, colliderRadius, dir, dist, obstacleMask);
            return hit.collider == null;
        }
    }
    #endregion
}

public enum EnemyState
{
    FreeMovement,
    Attack,
    Hurt,
    Dead,
}
