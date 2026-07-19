using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
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

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // 1. Prüfen, ob der Spieler in der Rolle (iFrames) ist -> Kein Schaden
        if (combatSystem != null && combatSystem.IsInvincible)
        {
            Debug.Log("Schaden ausgewichen durch Rolle (iFrames)!");
            return;
        }

        int finalDamage = damage;

        // 2. Prüfen, ob der Spieler blockt -> Schaden reduzieren
        if (combatSystem != null && combatSystem.IsParrying)
        {
            // Nutzt den parryReduction-Wert (z.B. 0.7 bedeutet 70% weniger Schaden)
            float reduction = combatSystem.GetParryReduction;
            finalDamage = Mathf.RoundToInt(damage * (1f - reduction));

            if (animator != null)
            {
                animator.SetTrigger("BlockHit"); // Optional: Animation für Schildtreffer
            }
            Debug.Log($"Angriff geblockt! Schaden reduziert von {damage} auf {finalDamage}");
        }
        else
        {
            // Normaler Treffer -> Hit-Animation abspielen
            if (animator != null)
            {
                animator.SetTrigger("Hit"); // Standard Hit-Animation
            }
        }

        // 3. Schaden abziehen
        currentHealth -= finalDamage;
        Debug.Log($"Spieler nimmt {finalDamage} Schaden! HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Spieler ist gestorben!");

        if (animator != null)
        {
            animator.SetTrigger("Die"); // Todesanimation für den Spieler
        }

        // Hier kannst du später ein Respawn-System oder "Game Over" aufrufen
    }
}