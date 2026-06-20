using UnityEngine;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float knockbackForce = 5f;
    
    [Header("Feedback")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    
    private int currentHealth;
    private Rigidbody rb;
    
    [Header("Setup")]
    public Renderer myRenderer; // Değişken adını myRenderer yaptık
    private Color originalColor;
    private bool isDead = false;
    
    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        
        // Eğer Inspector'dan bir Renderer sürüklemediysen, otomatik olarak çocuk objelerden bulsun
        if (myRenderer == null)
        {
            myRenderer = GetComponentInChildren<Renderer>();
        }
        
        // Orijinal rengi hafızaya alıyoruz (Sürekli GetComponent yapmaktan kurtulduk)
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
        
        // Knockback anwenden
        if (rb != null)
        {
            Vector3 direction = (transform.position - GetPlayerPosition()).normalized;
            // Y ekseninde saçma sapan uçmaması için knockback yönünü düzleştiriyoruz
            direction.y = 0; 
            rb.AddForce(direction * knockbackForce, ForceMode.Impulse);
        }
        
        // Visuelles Feedback
        StartCoroutine(FlashDamage());
        
        // Death Check
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} ist besiegt!");
        
        // Optionen für Tod:
        // 1. Play Death Animation
        // 2. Loot droppen
        // 3. Punkte geben
        // 4. Nach Delay zerstören
        
        Destroy(gameObject, 3f);
    }
    
    private System.Collections.IEnumerator FlashDamage()
    {
        // Sürekli GetComponent çağırmak yerine startta bulduğumuz myRenderer'ı kullanıyoruz
        if (myRenderer == null) yield break;
        
        myRenderer.material.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        myRenderer.material.color = originalColor;
    }
    
    private Vector3 GetPlayerPosition()
    {
        // FindObjectOfType yerine yeni ve performanslı olan FindFirstObjectByType kullandık
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        return player != null ? player.transform.position : transform.position;
    }
    
    public int GetCurrentHealth => currentHealth;
    public int GetMaxHealth => maxHealth;
    public float GetHealthPercentage => (float)currentHealth / maxHealth;
    public bool IsDead => isDead;
}