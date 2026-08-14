using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private PlayerCombatSystem combatSystem;
    private Animator animator;
    private bool isDead = false;

    // Dışarıdan okunabilir ölüm durumu
    public bool IsDead => isDead;

    // UI ve GameManager için Event'ler
    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDied; // 🟢 GameManager'a haber vermek için ekledik

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

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0) Die();
    }

    public void TakeDamage(int damage) => TakeDamage(damage, null);

    private void Die()
{
    isDead = true;
    animator?.SetTrigger("Die");

    // Event'i tetikle
    OnPlayerDied?.Invoke();

    // 🟢 GARANTİ ÇÖZÜM: GameManager'a doğrudan haber ver!
    if (GameManager.Instance != null)
    {
        GameManager.Instance.OnPlayerDiedDirectCall();
    }
    else
    {
        Debug.LogError("Sahnede GameManager bulunamadı!");
    }
}

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    public void RestoreFullHealth()
{
    if (isDead) isDead = false; // Eğer ölü durumu kaldıysa sıfırla

    currentHealth = maxHealth;
    OnHealthChanged?.Invoke(currentHealth, maxHealth); // UI'ı anında güncelle
}
}