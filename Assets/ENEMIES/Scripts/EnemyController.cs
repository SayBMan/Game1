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

    [Header("References")]
    private Transform player;
    private PlayerGridPoints playerGridPoints;
    public Rigidbody2D rb;
    private Animator anim;
    public EnemyAttack enemyAttack;
    public Collider2D childCollider;
    public Rigidbody2D rb_child;

    [Header("States")]
    private EnemyState currentState = EnemyState.FreeMovement;

    [Header("Detection")]
    public LayerMask obstacleMask;             // obstacle layers
    public float colliderRadius = 0.18f;       // CircleCast yarıçapı (enemy collider yarıçapına yakın)
    public float raycastOffset = 0.0f;         // raycast origin offset
    public bool hasLineOfSight = false;
    private Vector2? currentTargetPoint;

    [Header("Grid fallback")]
    [Tooltip("Etraftaki kare yarıçapı (PlayerGridPoints tarafından kullanılır)")]
    public int gridCandidateLimit = 0; // 0 = tüm noktalar; >0 = en fazla bu kadar adayı kontrol et

    [Header("Avoidance")]
    public float avoidanceCastDistance = 0.45f;
    [Range(0f, 1f)]
    public float avoidanceStrength = 0.85f;

    // internal chase/fallback state
    private bool isEngaged = false;         // true: enemy en son player'ı görmüştü ve chase session içindedir
    private bool fallbackActive = false;    // true: şu an fallback (grid) ile takip ediliyor
    private bool fallbackExhausted = false; // true: bu session için fallback denendi ve başarısız oldu (tekrar denenmeyecek)

    // ray origin offsets for player raycast
    private Vector2[] rayOriginsOffsets => new Vector2[]
    {
        Vector2.zero,
        Vector2.right * 0.18f,
        Vector2.left  * 0.18f,
        Vector2.up    * 0.12f,
        Vector2.down  * 0.12f
    };

    void Start()
    {
        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null && childCollider != null)
            Physics2D.IgnoreCollision(enemyCollider, childCollider);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerGridPoints = player.GetComponent<PlayerGridPoints>();
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
            Move();
    }

    #region Enemy Brain
    void EnemyBrain()
    {
        if (isAttacking || isHurt) return;

        distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Menzi̇l dışi ise hepsi sıfırlansın
        if (distanceToPlayer > detectionRange)
        {
            StopAllChasing();
            return;
        }

        // 1) Player detection (RAYCAST)
        bool playerVisible = HasLineOfSight(player.position, true);
        if (playerVisible)
        {
            // Direkt player göründü -> ENGAGE (chase session başlar/yenilenir)
            hasLineOfSight = true;
            isEngaged = true;
            fallbackActive = false;
            fallbackExhausted = false;
            currentTargetPoint = null;

            if (distanceToPlayer > attackRange)
            {
                Vector2 desired = ((Vector2)player.position - (Vector2)transform.position).normalized;
                Vector2 final = ComputeAvoidanceDirection(desired);
                facingDirection = final;
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

        // Player görünmüyor
        hasLineOfSight = false;

        // Eğer enemy ENGAGED değilse -> hiçbir grid/fallback denemesi yapma
        if (!isEngaged)
        {
            // pasif durumda kal
            facingDirection = Vector2.zero;
            moveMagnitude = 0;
            currentTargetPoint = null;
            return;
        }

        // Eğer ENGAGED ve fallback daha önce başarısız olduysa -> durup session'ı kapat
        if (fallbackExhausted)
        {
            // fallback daha önce denendi ve başarısız oldu -> artık deneme yok, dur.
            isEngaged = false;
            fallbackActive = false;
            currentTargetPoint = null;
            facingDirection = Vector2.zero;
            moveMagnitude = 0;
            return;
        }

        // Eğer zaten fallback aktifse (önceden bir candidate bulundu) -> onu kullanmaya çalış
        if (fallbackActive && currentTargetPoint.HasValue)
        {
            // validate current target
            if (!HasLineOfSight(currentTargetPoint.Value, false))
            {
                // hedef artık görünmüyor => deneyebileceğimiz başka candidate'lar var mı ona bakalım
                bool foundAnother = TryFindAndSetGridTarget();
                if (!foundAnother)
                {
                    // yoksa fallback bitti -> dur ve session kapat
                    fallbackActive = false;
                    fallbackExhausted = true;
                    isEngaged = false;
                    currentTargetPoint = null;
                    facingDirection = Vector2.zero;
                    moveMagnitude = 0;
                    return;
                }
            }
            else
            {
                // hedef hâlâ görünür -> ona doğru git
                float dcur = Vector2.Distance(transform.position, currentTargetPoint.Value);
                if (dcur <= 0.28f)
                {
                    // ulaştı say
                    // fallbackActive kalabilir; ama hedefi temizle ve bir sonraki döngüde yeni candidate aranır
                    currentTargetPoint = null;
                    // hemen yeni candidate aranacak alt kısımda
                }
                else
                {
                    Vector2 desired = (currentTargetPoint.Value - (Vector2)transform.position).normalized;
                    Vector2 final = ComputeAvoidanceDirection(desired);
                    facingDirection = final;
                    moveMagnitude = facingDirection.magnitude;
                    ChangeState(EnemyState.FreeMovement);
                    if (facingDirection != Vector2.zero) lastFacingDirection = facingDirection;
                    return;
                }
            }
        }

        // Eğer fallback aktif değilse ya da hedef temizlendiyse -> tek seferlik fallback başlat / devam ettir
        if (!fallbackActive)
        {
            bool started = TryStartFallback();
            if (!started)
            {
                // fallback denenip başarısız oldu -> otur ve session'ı bitir
                fallbackExhausted = true;
                isEngaged = false;
                currentTargetPoint = null;
                facingDirection = Vector2.zero;
                moveMagnitude = 0;
                return;
            }
            // TryStartFallback set ettiyse fallbackActive==true ve currentTargetPoint set edilmiş; döngü bir üstteki blokta devam eder
        }

        // Son olarak: fallbackActive true ama currentTargetPoint null olabilir -> hemen yeni hedef ara
        if (fallbackActive && !currentTargetPoint.HasValue)
        {
            bool found = TryFindAndSetGridTarget();
            if (!found)
            {
                fallbackActive = false;
                fallbackExhausted = true;
                isEngaged = false;
                currentTargetPoint = null;
                facingDirection = Vector2.zero;
                moveMagnitude = 0;
                return;
            }
            else
            {
                // yeni hedef bulundu ve ayarlandı; hemen ona yönel
                Vector2 desired = (currentTargetPoint.Value - (Vector2)transform.position).normalized;
                Vector2 final = ComputeAvoidanceDirection(desired);
                facingDirection = final;
                moveMagnitude = facingDirection.magnitude;
                ChangeState(EnemyState.FreeMovement);
                if (facingDirection != Vector2.zero) lastFacingDirection = facingDirection;
                return;
            }
        }

        // Varsa başka durumlarda bekle
        facingDirection = Vector2.zero;
        moveMagnitude = 0;
    }

    // helper: stop and reset
    private void StopAllChasing()
    {
        facingDirection = Vector2.zero;
        moveMagnitude = 0;
        currentTargetPoint = null;
        hasLineOfSight = false;
        isEngaged = false;
        fallbackActive = false;
        fallbackExhausted = false;
    }

    // Attempt to begin fallback once: returns true if fallback started (found at least one candidate and set it)
    private bool TryStartFallback()
    {
        // get candidates and set the best one if any
        if (playerGridPoints == null) return false;
        var allPoints = playerGridPoints.GetAllPoints();
        int checks = 0;
        Vector2? chosen = null;
        float bestScore = float.MaxValue;

        foreach (var pnt in allPoints)
        {
            checks++;
            if (gridCandidateLimit > 0 && checks > gridCandidateLimit) break;

            // Enemy must see the point
            if (!HasLineOfSight(pnt, false)) continue;

            // Player must also see the same point
            if (!PlayerCanSeePoint(pnt)) continue;

            float dEnemy = Vector2.Distance((Vector2)transform.position, pnt);
            float dPlayer = Vector2.Distance((Vector2)player.position, pnt);
            float score = dEnemy + dPlayer;
            if (score < bestScore)
            {
                bestScore = score;
                chosen = pnt;
            }
        }

        if (!chosen.HasValue) return false;

        currentTargetPoint = chosen.Value;
        fallbackActive = true;
        return true;
    }

    // When an existing fallback target disappears, try to find another candidate in same session
    private bool TryFindAndSetGridTarget()
    {
        if (playerGridPoints == null) return false;
        var allPoints = playerGridPoints.GetAllPoints();
        int checks = 0;
        Vector2? chosen = null;
        float bestScore = float.MaxValue;

        foreach (var pnt in allPoints)
        {
            checks++;
            if (gridCandidateLimit > 0 && checks > gridCandidateLimit) break;

            // Enemy must see the point
            if (!HasLineOfSight(pnt, false)) continue;

            // Player must also see the same point
            if (!PlayerCanSeePoint(pnt)) continue;

            float dEnemy = Vector2.Distance((Vector2)transform.position, pnt);
            float dPlayer = Vector2.Distance((Vector2)player.position, pnt);
            float score = dEnemy + dPlayer;
            if (score < bestScore)
            {
                bestScore = score;
                chosen = pnt;
            }
        }

        if (!chosen.HasValue) return false;

        currentTargetPoint = chosen.Value;
        return true;
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

    #region Attack/Hurt/Death
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
        ChangeState(EnemyState.Dead);
        if (anim != null) { anim.SetTrigger("isDead"); }
        if (rb != null) rb.simulated = false;
        if (rb_child != null) rb_child.simulated = false;
    }

    public void Die() => Destroy(gameObject);
    #endregion

    #region State
    private void ChangeState(EnemyState newState)
    {
        if (currentState != newState)
            currentState = newState;
    }
    #endregion

    #region Utility / Detection
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

    // Public wrapper: player için ray, grid nokta için circle
    public bool HasLineOfSight(Vector2 targetPos, bool targetIsPlayer = true)
    {
        if (targetIsPlayer) return HasLineOfSightRay(targetPos);
        else return HasLineOfSightCircle(targetPos);
    }

    // Raycast multiple offsets for player detection (fast)
    private bool HasLineOfSightRay(Vector2 targetPos)
    {
        Vector2 startBase = (Vector2)transform.position + Vector2.up * raycastOffset;
        Vector2 dir = targetPos - startBase;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();

        int playerMask = LayerMask.GetMask("Player");
        int mask = obstacleMask | playerMask;

        foreach (var o in rayOriginsOffsets)
        {
            Vector2 origin = startBase + o;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, mask);
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player"))
                    return true;
                else
                    continue;
            }
        }
        return false;
    }

    // CircleCast for grid points (body radius considered)
    private bool HasLineOfSightCircle(Vector2 targetPos)
    {
        Vector2 start = (Vector2)transform.position + Vector2.up * raycastOffset;
        Vector2 dir = targetPos - start;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();

        RaycastHit2D hit = Physics2D.CircleCast(start, colliderRadius, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    // *** Yeni: Player tarafının da noktayı görebilmesini kontrol et (Player -> point)
    // Basit raycast kullanıyoruz: eğer Player ile point arası obstacleMask içinde bir çarpışma yoksa görünür.
    private bool PlayerCanSeePoint(Vector2 point)
    {
        if (player == null) return false;

        Vector2 start = (Vector2)player.position + Vector2.up * raycastOffset;
        Vector2 dir = point - start;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir.Normalize();

        // Sadece engel layer'larına bak; eğer bir engel varsa player göremez.
        RaycastHit2D hit = Physics2D.Raycast(start, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    // small avoidance: ön tarafa circlecast; engel varsa slide
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

    void OnDrawGizmos()
    {
        // LoS balonu
        Gizmos.color = hasLineOfSight ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.18f);

        // hedef nokta
        if (currentTargetPoint.HasValue)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTargetPoint.Value);
            Gizmos.DrawSphere(currentTargetPoint.Value, 0.12f);
        }

        // grid noktalarını göster (eğer PlayerGridPoints bağlıysa)
        if (playerGridPoints != null)
        {
            var pts = playerGridPoints.GetAllPoints();
            foreach (var p in pts)
            {
                bool enemySees = HasLineOfSight(p, false);
                bool playerSees = PlayerCanSeePoint(p);
                if (enemySees && playerSees)
                    Gizmos.color = Color.green;  // her iki taraf da görüyor => seçilebilir
                else if (enemySees)
                    Gizmos.color = Color.yellow; // sadece enemy görüyor
                else
                    Gizmos.color = Color.red;    // enemy de görmüyor

                Gizmos.DrawSphere(p, 0.04f);
            }
        }

        // debug: fallback status
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"engaged:{isEngaged} fallbackActive:{fallbackActive} exhausted:{fallbackExhausted}");
        #endif
    }
}
public enum EnemyState
{
    FreeMovement,
    Attack,
    Hurt,
    Dead
}
