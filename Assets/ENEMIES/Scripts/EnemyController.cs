using System.Collections;
using UnityEngine;

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
    [HideInInspector] public EnemyState deathState;
    [HideInInspector] public EnemyState attackState;

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

        if (distanceToPlayer <= detectionRange && distanceToPlayer > attackRange)
        {
            facingDirection = (player.position - transform.position).normalized;
            ChangeState(EnemyState.FreeMovement);
        }
        else if (distanceToPlayer <= attackRange)
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
        else if (distanceToPlayer > detectionRange)
        {
            facingDirection = Vector2.zero;
            ChangeState(EnemyState.FreeMovement);
        }

        moveMagnitude = new Vector2(facingDirection.x, facingDirection.y).magnitude;

        if (facingDirection != Vector2.zero)
        {
            lastFacingDirection = facingDirection;
        }
    }
    #endregion

    #region Movement
    void Move()
    {
        rb.linearVelocity = moveSpeed * Time.fixedDeltaTime * facingDirection;
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

    private int GetDirection(Vector2 direction)
    {
        float x = direction.x;
        float y = direction.y;
        if (y < 0 && Mathf.Abs(y) >= Mathf.Abs(x))
        {
            return 1; // Down
        }
        else if (y > 0 && Mathf.Abs(y) >= Mathf.Abs(x))
        {
            return 4; // Up
        }
        else if (x > 0 && Mathf.Abs(x) >= Mathf.Abs(y))
        {
            return 3; // Right
        }
        else if (x < 0 && Mathf.Abs(x) >= Mathf.Abs(y))
        {
            return 2; // Left
        }
        else
        {
            return 1;
        }
    }

}
public enum EnemyState
    {
        FreeMovement,
        Attack,
        Hurt,
        Dead,
    }
