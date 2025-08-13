using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    private Animator anim;
    public PlayerHealth playerHealth;
    public PlayerAttack playerAttack;
    public PlayerStamina playerStamina;
    public Collider2D childCollider;

    [Header("Movement")]
    public float moveSpeed;
    private float moveX;
    private float moveY;
    private float moveMagnitude;
    public float sprintModifier;
    public bool isSprinting;
    public float currentSpeed;
    private Vector2 facingDirection;
    private Vector2 lastFacingDirection;

    [Header("Dash")]
    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;
    private bool canDash = true;
    [HideInInspector] public bool isDashing;

    [Header("Combat")]
    [HideInInspector] public Vector2 attackDirection;
    public bool isAttacking;
    [HideInInspector] public int attackPosition;
    public bool isHurt;
    private int hurtPosition;
    public bool isDead;
    private int deadPosition;

    [Header("State")]
    private PlayerState currentState = PlayerState.FreeMovement;

    void Start()
    {
        Collider2D playerCollider = GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(playerCollider, childCollider);

        anim = GetComponent<Animator>();
        currentSpeed = moveSpeed;
    }

    void Update()
    {
        if (currentState == PlayerState.FreeMovement)
        {
            GetInput();
            MovementAnimationControl();
        }
    }

    void FixedUpdate()
    {
        if (currentState == PlayerState.FreeMovement)
        {
            Move();
        }
    }

    #region Input
    private void GetInput()
    {
        moveX = Input.GetAxisRaw("Horizontal");
        moveY = Input.GetAxisRaw("Vertical");
        facingDirection = new Vector2(moveX, moveY).normalized;
        moveMagnitude = new Vector2(moveX, moveY).magnitude;

        if (facingDirection != Vector2.zero)
        {
            lastFacingDirection = facingDirection;
        }

        // Sprint
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (playerStamina.HasStamina(playerStamina.sprintStaminaCost * Time.deltaTime))
            {
                Sprint();
            }
            else
            {
                StopSprinting();
            }
        }
        else
        {
            StopSprinting();
        }

        // Dash
        if (Input.GetKeyDown(KeyCode.Space) && canDash && facingDirection != Vector2.zero)
        {
            StartCoroutine(Dash());
        }

        // Attack
        if (Input.GetMouseButtonDown(0) && playerAttack.attackTimer <= 0f)
        {
            HandleAttack();
        }
    }
    #endregion

    #region Move
    private void Move()
    {
        rb.linearVelocity = currentSpeed * Time.fixedDeltaTime * facingDirection;
    }
    private void MovementAnimationControl()
    {
        Vector2 adjustedDirection = GetPriorityDirection(facingDirection);

        anim.SetFloat("MoveX", adjustedDirection.x);
        anim.SetFloat("MoveY", adjustedDirection.y);
        anim.SetFloat("MoveMagnitude", moveMagnitude);
        anim.SetFloat("LastMoveX", lastFacingDirection.x);
        anim.SetFloat("LastMoveY", lastFacingDirection.y);
    }

    private Vector2 GetPriorityDirection(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return Vector2.zero;

        float x = dir.x;
        float y = dir.y;

        if (y > 0 && Mathf.Abs(y) >= Mathf.Abs(x))
            return Vector2.up;

        if (y < 0 && Mathf.Abs(y) >= Mathf.Abs(x))
            return x > 0 ? Vector2.right : (x < 0 ? Vector2.left : Vector2.down);

        return x > 0 ? Vector2.right : Vector2.left;
    }
    #endregion

    #region Sprint
    private void Sprint()
    {
        if (!isSprinting)
        {
            isSprinting = true;
            anim.SetFloat("runSpeed", 1.5f);
            currentSpeed = moveSpeed * sprintModifier;
        }
        if (facingDirection != Vector2.zero)
        {
            playerStamina.UseStamina(playerStamina.sprintStaminaCost * Time.deltaTime);
        }
        else
        {
            isSprinting = false;
        }
        
    }
    private void StopSprinting()
    {
        isSprinting = false;
        anim.SetFloat("runSpeed", 1f);
        currentSpeed = moveSpeed;
    }
    #endregion

    #region Dash
    private IEnumerator Dash()
    {
        if (!playerStamina.HasStamina(playerStamina.dashStaminaCost)) yield break;
        playerStamina.UseStamina(playerStamina.dashStaminaCost);

        ChangeState(PlayerState.Dash);
        canDash = false;

        // Disable collision on Dash
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"),LayerMask.NameToLayer("Character Sensor"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"),LayerMask.NameToLayer("Character Sensor"), true);

        rb.linearVelocity = dashSpeed * facingDirection;
        isDashing = true;

        float elapsed = 0f;
        bool dashCancelled = false;

        while (elapsed < dashDuration)
        {
            if (isHurt)
            {
                dashCancelled = true;
                isDashing = false;
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        if (dashCancelled)
        {
            ChangeState(PlayerState.Hurt);
        }
        else
        {
            ChangeState(PlayerState.FreeMovement);
        }

        isDashing = false;

        // Enable collision after Dash
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"),LayerMask.NameToLayer("Character Sensor"), false);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"),LayerMask.NameToLayer("Character Sensor"), false);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    #endregion

    #region Hurt
    public void GetHurt()
    {
        if (isDead || isHurt) return;

        isHurt = true;
        isAttacking = false;
        isDashing = false;
        isSprinting = false;
        
        rb.linearVelocity = Vector2.zero;
        ChangeState(PlayerState.Hurt);
        hurtPosition = GetDirection(lastFacingDirection);
        anim.SetTrigger("isHurt");
        anim.SetInteger("HurtPosition", hurtPosition);
    }

    public void OnHurtAnimationEnd()
    {   
        isHurt = false;
        ChangeState(PlayerState.FreeMovement);
    }
    #endregion

    #region Death
    public void GetDeath()
    {   
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;
        ChangeState(PlayerState.Dead);
        deadPosition = GetDirection(lastFacingDirection);
        anim.SetTrigger("isDead");
        anim.SetInteger("DeadPosition", deadPosition);
    }
    public IEnumerator Die()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
    #endregion

    #region Attack
    private void HandleAttack()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        attackDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        attackPosition = GetDirection(attackDirection);
        StartAttack();
    }
    private void StartAttack()
    {
        if (isDead || isHurt) return;
        if (!playerStamina.HasStamina(playerStamina.attackStaminaCost)) return;
        playerStamina.UseStamina(playerStamina.attackStaminaCost);

        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        ChangeState(PlayerState.Attack);
        anim.SetFloat("AttackSpeed", playerAttack.attackSpeed);
        anim.SetTrigger("Attack");
        anim.SetInteger("AttackPosition", attackPosition);
        playerAttack.attackTimer = playerAttack.attackCooldown;
    }

    public void OnAttackAnimationEnd()
    {   
        isAttacking = false;
        lastFacingDirection = attackDirection;
        ChangeState(PlayerState.FreeMovement);
    }
    #endregion

    #region State
    private void ChangeState(PlayerState newState)
    {
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

public enum PlayerState
{
    FreeMovement,
    Attack,
    Dash,
    Hurt,
    Dead,
}