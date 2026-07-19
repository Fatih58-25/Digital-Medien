using UnityEngine;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float knockbackForce = 5f;

    [Header("Feedback")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Death Settings")]
    [SerializeField] private string deathTriggerName = "Die"; // Name des Triggers im Animator
    [SerializeField] private float timeBeforeDestroy = 3.0f;  // Wie lange er tot am Boden liegt

    [Header("Setup")]
    [SerializeField] private Renderer myRenderer;

    private int currentHealth;
    private Color originalColor;
    private Rigidbody rb;
    private Animator animator; // Referenz zum Animator
    private bool isDead = false;
    private Coroutine flashRoutine;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>(); // Holt sich den Animator des Gegners

        if (myRenderer == null)
        {
            myRenderer = GetComponentInChildren<Renderer>();
        }

        if (myRenderer != null)
        {
            originalColor = myRenderer.material.color;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} nimmt {damage} Schaden! Verbleibende Health: {currentHealth}");

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashDamage());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockback(Vector3 direction)
    {
        if (isDead) return;

        direction.y = 0f;

        if (rb != null)
        {
            rb.AddForce(direction.normalized * knockbackForce, ForceMode.Impulse);
        }
        else
        {
            transform.position += direction.normalized * knockbackForce * 0.1f;
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} ist besiegt!");

        // 1. Todesanimation abspielen
        if (animator != null)
        {
            animator.SetTrigger(deathTriggerName);
        }

        // 2. Collider ausschalten (damit der Spieler nicht gegen die Leiche läuft)
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // 3. Rigidbody stoppen (damit er nicht wegrutscht oder durch den Boden fällt)
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Stoppt alle Bewegungen
            rb.isKinematic = true;            // Schaltet die Physik für ihn ab
        }

        // 4. Objekt nach der eingestellten Zeit zerstören
        Destroy(gameObject, timeBeforeDestroy);
    }

    private System.Collections.IEnumerator FlashDamage()
    {
        if (myRenderer == null) yield break;

        myRenderer.material.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        myRenderer.material.color = originalColor;
    }

    public int GetCurrentHealth => currentHealth;
    public int GetMaxHealth => maxHealth;
    public float GetHealthPercentage => (float)currentHealth / maxHealth;
    public bool IsDead => isDead;
}