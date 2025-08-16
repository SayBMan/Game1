using System.Collections;
using UnityEngine;

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
    public Rigidbody2D rb;
    private Animator anim;
    public EnemyHealth enemyHealth;
    public EnemyAttack enemyAttack;
    private PlayerAttack playerAttack;
    public Rigidbody2D rb_child;
    public Collider2D childCollider;

    [Header("States")]
    private EnemyState currentState = EnemyState.FreeMovement;

    [Header("Detection")]
    public LayerMask obstacleMask;
    public float raycastOffsetY = 0.5f;
    public bool hasLineOfSight = false;

    void Start()
    {
        Collider2D enemyCollider = GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(enemyCollider, childCollider);

        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerAttack = player.GetComponent<PlayerAttack>();
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

        // İlk görüş kontrolü
        if (!hasLineOfSight && distanceToPlayer <= detectionRange)
        {
            if (CheckLineOfSight())
            {
                hasLineOfSight = true;
            }
            else
            {
                facingDirection = Vector2.zero;
                moveMagnitude = facingDirection.magnitude;
                return;
            }
        }

        // Takip devam ediyorsa
        if (hasLineOfSight)
        {
            if (distanceToPlayer > detectionRange)
            {
                hasLineOfSight = false;
                facingDirection = Vector2.zero;
                moveMagnitude = facingDirection.magnitude;
                return;
            }

            if (distanceToPlayer > attackRange)
            {
                Vector2 targetDir = (player.position - transform.position).normalized;
                facingDirection = GetDirectionWithAvoidance(targetDir);
                ChangeState(EnemyState.FreeMovement);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;

                if (enemyAttack.attackTimer <= 0f)
                {
                    HandleAttack();
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                    facingDirection = Vector2.zero;
                    ChangeState(EnemyState.FreeMovement);
                }
            }

            moveMagnitude = facingDirection.magnitude;

            if (facingDirection != Vector2.zero)
            {
                lastFacingDirection = facingDirection;
            }
        }
    }
    #endregion

    #region Movement
    void Move()
    {
        rb.linearVelocity = moveSpeed * facingDirection * Time.fixedDeltaTime;
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

    #region Hurt
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
    #endregion

    #region Death
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
    public void Die()
    {
        Destroy(gameObject);
    }
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
        if (y < 0 && Mathf.Abs(y) >= Mathf.Abs(x))
            return 1; // Down
        else if (y > 0 && Mathf.Abs(y) >= Mathf.Abs(x))
            return 4; // Up
        else if (x > 0 && Mathf.Abs(x) >= Mathf.Abs(y))
            return 3; // Right
        else if (x < 0 && Mathf.Abs(x) >= Mathf.Abs(y))
            return 2; // Left
        else
            return 1;
    }

    private bool CheckLineOfSight()
    {
        Vector2 start = (Vector2)transform.position;
        Vector2 direction = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, obstacleMask | LayerMask.GetMask("Player"));
        Debug.DrawRay(start, direction * distance, Color.red);

        if (hit.collider != null)
        {
            Debug.DrawLine(start, hit.point, Color.green);
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    // Basit engelden kaçma metodu
    private Vector2 GetDirectionWithAvoidance(Vector2 targetDir)
    {
        float checkDist = 0.5f;
        float angleStep = 20f;
        int maxTries = 5;

        if (!Physics2D.Raycast(transform.position, targetDir, checkDist, obstacleMask))
            return targetDir;

        for (int i = 1; i <= maxTries; i++)
        {
            Vector2 rightDir = Quaternion.Euler(0, 0, angleStep * i) * targetDir;
            if (!Physics2D.Raycast(transform.position, rightDir, checkDist, obstacleMask))
                return rightDir.normalized;

            Vector2 leftDir = Quaternion.Euler(0, 0, -angleStep * i) * targetDir;
            if (!Physics2D.Raycast(transform.position, leftDir, checkDist, obstacleMask))
                return leftDir.normalized;
        }

        return Vector2.zero;
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
