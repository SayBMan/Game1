using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public EnemyController enemyController;
    public Transform attackPoint;
    public float attackSpeed = 1.3f;
    public float attackDamage = 2f;
    public float attackRadius = 0.2f;
    public float attackCooldown = 1f;
    public float attackTimer;
    public LayerMask playerLayer;

    void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }
    public void DealDamage()
    {
        switch (enemyController.attackPosition)
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

        Collider2D[] players = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
        if (players.Length > 0)
        {
            foreach (Collider2D player in players)
            {
                if (player.isTrigger) return;
                player.GetComponent<PlayerHealth>().ChangeHealth(-attackDamage);

            }

        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
