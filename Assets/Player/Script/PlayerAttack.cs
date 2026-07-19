using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Animator animator;
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public int damage = 10;
    public LayerMask enemyLayer;

    public void Attack()
    {
        animator.SetTrigger("Attack");
    }

    // Diese Funktion per Animation Event in der Schlag-Animation aufrufen
    public void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}