using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private PlayerCombatSystem combatSystem;
    private Animator animator;
    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        combatSystem = GetComponent<PlayerCombatSystem>();
        animator = GetComponentInChildren<Animator>();
    }

    // Düşman hasar verdiğinde bunu kullanacak: TakeDamage(damage, attackerTransform)
    public void TakeDamage(int damage, Transform attacker = null)
    {
        if (isDead) return;

        if (combatSystem != null && combatSystem.IsInvincible) return;

        int finalDamage = damage;

        if (combatSystem != null && combatSystem.IsParrying)
        {
            finalDamage = Mathf.RoundToInt(damage * (1f - combatSystem.GetParryReduction));
            animator?.SetTrigger("BlockHit");
        }
        else
        {
            if (combatSystem != null)
            {
                // Düşmanın yönünü hesapla ve geri tepmeyi tetikle
                Vector3 hitDir = (attacker != null) ? (transform.position - attacker.position).normalized : Vector3.back;
                combatSystem.ApplyStagger(0.5f, hitDir, 3f);
            }
            else animator?.SetTrigger("Hit");
        }

        currentHealth -= finalDamage;
        if (currentHealth <= 0) Die();
    }

    // IDamageable arayüzü için standart overload
    public void TakeDamage(int damage) => TakeDamage(damage, null);

    private void Die()
    {
        isDead = true;
        animator?.SetTrigger("Die");
    }
}