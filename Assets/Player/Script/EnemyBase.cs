using UnityEngine;
using System;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Boss Settings")]
    [SerializeField] private bool isBoss = false;              // Tik atılırsa Boss olur!
    [SerializeField] private string bossName = "Ancient Dragon"; // Ekranın altında yazacak isim

    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float knockbackForce = 5f;

    [Header("Feedback")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Death Settings")]
    [SerializeField] private string deathTriggerName = "Die"; 
    [SerializeField] private float timeBeforeDestroy = 7.0f;  

    [Header("Angriff (NPC gegen Spieler)")]
    [SerializeField] private Transform attackPoint;           
    [SerializeField] private float attackRange = 1.5f;        
    [SerializeField] private int attackDamage = 15;           
    [SerializeField] private LayerMask playerLayer;           

    [Header("Setup")]
    [SerializeField] private Renderer myRenderer;

    private int currentHealth;
    private Color originalColor;
    private Rigidbody rb;
    private Animator animator;
    private bool isDead = false;
    private Coroutine flashRoutine;

    // UI Haberleşmesi için Eventler
    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

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

        // UI Güncelleme Eventi Tetikle
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

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

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.velocity = Vector3.zero;
            agent.enabled = false;
            StartCoroutine(ReenableNavMesh(agent, 0.2f));
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; 
            rb.AddForce(direction.normalized * knockbackForce, ForceMode.Impulse);
        }
        else
        {
            StartCoroutine(SmoothMoveFallback(direction.normalized * knockbackForce * 0.2f));
        }
    }

    private System.Collections.IEnumerator ReenableNavMesh(UnityEngine.AI.NavMeshAgent agent, float delay)
{
    yield return new WaitForSeconds(delay);
    // Yalnızca öldü değilse NavMesh'i tekrar aç
    if (agent != null && !isDead)
    {
        agent.enabled = true;
    }
}

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

    // Ölüm event'ini tetikle (Boss HUD kapanacak)
    OnDied?.Invoke();

    // 1. NavMeshAgent'ı kapat (Hareket etmeyi kessin)
    UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (agent != null)
    {
        agent.isStopped = true;
        agent.enabled = false;
    }

    // 2. Animator'e erişimi sağlamlaştır ve Trigger'ı çalıştır
    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (animator != null)
    {
        animator.SetTrigger(deathTriggerName);
    }

    Collider enemyCollider = GetComponent<Collider>();
    if (enemyCollider != null)
    {
        enemyCollider.enabled = false;
    }

    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    Destroy(gameObject, timeBeforeDestroy);
}

    private System.Collections.IEnumerator FlashDamage()
    {
        if (myRenderer == null) yield break;

        myRenderer.material.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        myRenderer.material.color = originalColor;
    }

    public void OnEnemyAttackHit()
    {
        if (isDead || attackPoint == null) return;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
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

    public void OnAttackHit() { }

    // GETTER PROPERTIES
    public bool IsBoss => isBoss;
    public string BossName => bossName;
    public int GetCurrentHealth => currentHealth;
    public int GetMaxHealth => maxHealth;
    public float GetHealthPercentage => (float)currentHealth / maxHealth;
    public bool IsDead => isDead;
}