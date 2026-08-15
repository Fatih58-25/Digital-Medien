using UnityEngine;
using System;
using System.Collections;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Boss Settings")]
    [SerializeField] private bool isBoss = false;
    [SerializeField] private string bossName = "Ancient Dragon";
    [Tooltip("Wenn true, wird beim Tod dieses Gegners GameManager.ShowVictory() ausgeloest (z.B. Malakor oder Hekate).")]
    [SerializeField] private bool isFinalBoss = false;

    [Header("Rune Reward (Rün Ödülü)")]
    [SerializeField] private int runeReward = 100; // Düşman ölünce verilecek rün miktarı

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

    private Vector3 initialPosition;
    private Quaternion initialRotation;
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
        initialPosition = transform.position;
        initialRotation = transform.rotation;
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

    private IEnumerator ReenableNavMesh(UnityEngine.AI.NavMeshAgent agent, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (agent != null && !isDead)
        {
            agent.enabled = true;
        }
    }

    private IEnumerator SmoothMoveFallback(Vector3 offset)
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
    if (isDead) return;
    isDead = true;
    Debug.Log($"{gameObject.name} ist besiegt!");

    // 🟢 1. AI SCRIPT'INI DIREKT KAPAT (Dönmeyi ve saldırmayı anında keser)
    SkeletonAI ai = GetComponent<SkeletonAI>();
    if (ai != null) ai.enabled = false;

    // GARANTİ RÜN VERME
    PlayerRunes playerRunes = FindObjectOfType<PlayerRunes>();
    if (playerRunes != null)
    {
        playerRunes.AddRunes(runeReward);
        Debug.Log($"🟢 {gameObject.name} öldürüldü! Oyuncuya {runeReward} rün eklendi.");
    }
    else
    {
        Debug.LogError("❌ HATA: Sahnede PlayerRunes script'ine sahip bir obje bulunamadı!");
    }

    OnDied?.Invoke();

    if (isFinalBoss)
    {
        GameManager.Instance?.ShowVictory();
    }

    UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (agent != null)
    {
        agent.isStopped = true;
        agent.enabled = false;
    }

    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (animator != null)
    {
        // 🟢 2. ÖNCEKİ SALDIRI TRIGGER'LARINI TEMİZLE (Ölüm animasyonunu kesmesini engeller)
        animator.ResetTrigger("Attack");
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

    StartCoroutine(DisableAfterDeath());
}

    private IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(timeBeforeDestroy);
        gameObject.SetActive(false); // Obje silinmiyor, sadece gizleniyor.
    }

    private IEnumerator FlashDamage()
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

    // 🟢 BONFIRE VE RESTART ANINDA ÇAĞRILAN RESPNAWN METODU
   public void RespawnEnemy()
{
    // 1. Boss ise ve öldüyse doğma!
    if (isBoss && isDead) return;

    // 🟢 SİHİRLİ SATIR: Eğer düşman ÖLMEMİŞSE ve ŞU AN KAPALIYSA (Pusu düşmanı/Henüz tetiklenmemiş) DOKUNMA!
    if (!isDead && !gameObject.activeSelf) return;
SkeletonAI ai = GetComponent<SkeletonAI>();
    if (ai != null) ai.enabled = true;
    // 2. Ölüm sonrası gizlenme sayacını ve diğer coroutines sıfırla
    StopAllCoroutines();

    // 3. Durumları ve Canı Sıfırla
    isDead = false;
    currentHealth = maxHealth;
    OnHealthChanged?.Invoke(currentHealth, maxHealth);

    // Renk sıfırlama
    if (myRenderer != null)
    {
        myRenderer.material.color = originalColor;
    }

    // 4. Obje ve Collider/Rigidbody Ayarlarını Aç
    gameObject.SetActive(true);

    Collider enemyCollider = GetComponent<Collider>();
    if (enemyCollider != null)
    {
        enemyCollider.enabled = true;
    }

    if (rb != null)
    {
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
    }

    // 5. NavMesh ve Pozisyonu İlk Noktaya Işınla (Kovalama Agrosunu Keser)
    UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (agent != null)
    {
        agent.enabled = false;
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        agent.enabled = true;

        if (agent.isOnNavMesh)
        {
            agent.Warp(initialPosition);
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
        }
    }
    else
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    // 6. Animasyonu Sıfırla
    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (animator != null)
    {
        animator.Rebind();
        animator.Update(0f);
    }
}

    // GETTER PROPERTIES
    public bool IsBoss => isBoss;
    public string BossName => bossName;
    public int GetCurrentHealth => currentHealth;
    public int GetMaxHealth => maxHealth;
    public float GetHealthPercentage => (float)currentHealth / maxHealth;
    public bool IsDead => isDead;
}