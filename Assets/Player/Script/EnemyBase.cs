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
    [SerializeField] private string deathTriggerName = "Die"; // Name des Triggers im Animator des NPCs
    [SerializeField] private float timeBeforeDestroy = 3.0f;  // Wie lange er tot am Boden liegt

    [Header("Angriff (NPC gegen Spieler)")]
    [SerializeField] private Transform attackPoint;           // Ein leeres GameObject in der Hand des NPCs
    [SerializeField] private float attackRange = 1.5f;        // Angriffsreichweite des NPCs
    [SerializeField] private int attackDamage = 15;           // Schaden, den der NPC dem Spieler zufügt
    [SerializeField] private LayerMask playerLayer;           // Der Layer deines Spielers (z.B. "Player")

    [Header("Setup")]
    [SerializeField] private Renderer myRenderer;

    private int currentHealth;
    private Color originalColor;
    private Rigidbody rb;
    private Animator animator;
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

    // --- SCHADEN EMPFANGEN (Vom Spieler getroffen werden) ---
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

        direction.y = 0f; // Verhindert, dass der Gegner in die Luft fliegt

        // 1. Prüfen, ob eine KI aktiv ist und diese kurz stoppen
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            // Stoppt die KI-Bewegung und schaltet den Agenten kurz ab
            agent.velocity = Vector3.zero;
            agent.enabled = false;

            // Startet eine verzögerte Funktion, die den Agenten nach 0.2 Sekunden wieder anschaltet
            StartCoroutine(ReenableNavMesh(agent, 0.2f));
        }

        // 2. Den physikalischen Impuls flüssig ausführen
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Nulle alte Kräfte für einen sauberen Impuls
            rb.AddForce(direction.normalized * knockbackForce, ForceMode.Impulse);
        }
        else
        {
            // Falls kein Rigidbody da ist, nutzen wir eine sanfte Bewegung per Coroutine statt Teleportation
            StartCoroutine(SmoothMoveFallback(direction.normalized * knockbackForce * 0.2f));
        }
    }

    // Hilfsfunktion: Schaltet die KI nach dem Rückstoß automatisch wieder ein
    private System.Collections.IEnumerator ReenableNavMesh(UnityEngine.AI.NavMeshAgent agent, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (agent != null && !isDead)
        {
            agent.enabled = true;
        }
    }

    // Hilfsfunktion: Schiebt den Gegner sanft zurück, falls absolut kein Rigidbody genutzt werden kann
    private System.Collections.IEnumerator SmoothMoveFallback(Vector3 offset)
    {
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + offset;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
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

        // 2. Collider ausschalten (damit der Spieler nicht an der Leiche hängenbleibt)
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // 3. Rigidbody stoppen und Physik deaktivieren
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
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


    // --- SCHADEN AUSTEILEN (Den Spieler angreifen) ---
    // DIESE FUNKTION PER ANIMATION EVENT IN DER NPC-ANGRIFFSANIMATION AUFRUFEN
    public void OnEnemyAttackHit()
    {
        if (isDead || attackPoint == null) return;

        // Erstellt eine Kugel an der Angriffs-Position des NPCs und checkt, ob der Spieler getroffen wurde
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);

        foreach (Collider hit in hits)
        {
            // Sucht nach der Schadens-Komponente auf dem Spieler
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Zeichnet eine rote Kugel im Editor für die Angriffsreichweite des NPCs
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    // Sicherheitsnetz: Tut beim NPC nichts, verhindert aber Fehlermeldungen!
    public void OnAttackHit()
    {
        // Bleibt leer
    }
    // Properties für eventuelle UI-Anzeigen (z.B. Lebensbalken)
    public int GetCurrentHealth => currentHealth;
    public int GetMaxHealth => maxHealth;
    public float GetHealthPercentage => (float)currentHealth / maxHealth;
    public bool IsDead => isDead;
}