using UnityEngine;
using System; // Action için gerekli

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private PlayerCombatSystem combatSystem;
    private Animator animator;
    private bool isDead = false;

    // UI için Event
    public event Action<float, float> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
        combatSystem = GetComponent<PlayerCombatSystem>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

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
                Vector3 hitDir = (attacker != null) ? (transform.position - attacker.position).normalized : Vector3.back;
                combatSystem.ApplyStagger(0.5f, hitDir, 3f);
            }
            else animator?.SetTrigger("Hit");
        }

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // UI Güncellemesini tetikle
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0) Die();
    }

    public void TakeDamage(int damage) => TakeDamage(damage, null);

    private void Die()
    {
        isDead = true;
        animator?.SetTrigger("Die");
    }

    // PlayerHealth.cs içine eklenecek metot:
public void Heal(int amount)
{
    if (isDead) return;

    currentHealth += amount;
    currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

    // UI'a haber ver
    OnHealthChanged?.Invoke(currentHealth, maxHealth);
}
}