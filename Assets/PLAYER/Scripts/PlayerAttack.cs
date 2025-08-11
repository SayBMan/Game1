using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public PlayerController playerController;
    public Transform attackPoint;
    public float attackRange = 0.2f;
    public float attackDamage = 2f;
    public float attackSpeed = 1.5f;
    public float attackCooldown = 1f;
    public float attackTimer;
    public LayerMask enemyLayer;

    void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }
    public void DealDamage()
    {
        switch (playerController.attackPosition)
        {
            case 1:
                attackPoint.localPosition = new Vector2(0, -0.25f);
                break;
            case 2:
                attackPoint.localPosition = new Vector2(-0.25f, 0);
                break;
            case 3:
                attackPoint.localPosition = new Vector2(0.25f, 0);
                break;
            case 4:
                attackPoint.localPosition = new Vector2(0, 0.25f);
                break;
        }
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        if (enemies.Length > 0)
        {
            foreach (Collider2D enemy in enemies)
            {
                if (enemy.isTrigger) continue;
                enemy.GetComponent<EnemyHealth>().ChangeHealth(-attackDamage);
            }

        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
