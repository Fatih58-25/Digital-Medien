using UnityEngine;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Angriff")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform swordPosition; // Position der Schwertspitze
    
    [Header("Parieren")]
    [SerializeField] private float parryCooldown = 0.5f;
    [SerializeField] private float parryDuration = 0.3f;
    [SerializeField] private float parryReduction = 0.7f; // Prozentsatz des Schadens der reduziert wird
    
    private PlayerAnimator playerAnimator;
    private PlayerController playerController;
    
    private float lastAttackTime;
    private float lastParryTime;
    private bool isParrying = false;
    private float parryEndTime;
    
    // Attackvarianten (unterschiedliche Schwert-Bewegungen)
    private int currentAttackCombo = 0;
    private float lastAttackInputTime;
    private float comboResetTime = 0.5f;
    
    private void Start()
    {
        playerAnimator = GetComponent<PlayerAnimator>();
        playerController = GetComponent<PlayerController>();
    }
    
    private void Update()
    {
        HandleAttackInput();
        HandleParryInput();
        UpdateParryState();
    }
    
    private void HandleAttackInput()
    {
        // NOT: PlayerController içine IsDucking eklediğimiz için artık burası hata vermeyecek!
        if (Input.GetMouseButtonDown(0) && !isParrying && !playerController.IsDucking)
        {
            // Angriff-Cooldown checken
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
            }
        }
    }
    
    private void HandleParryInput()
    {
        // NOT: PlayerController içine IsDucking eklediğimiz için artık burası hata vermeyecek!
        if (Input.GetMouseButtonDown(1) && !playerController.IsDucking)
        {
            // Parry-Cooldown checken
            if (Time.time - lastParryTime >= parryCooldown && !isParrying)
            {
                PerformParry();
            }
        }
    }
    
    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        
        // Kombo sıfırlama kontrolü (Önceki kodda zamanı erken güncellediği için düzeltildi)
        if (Time.time - lastAttackInputTime > comboResetTime)
        {
            currentAttackCombo = 0;
        }
        
        lastAttackInputTime = Time.time; // Giriş zamanını şimdi güncelliyoruz
        currentAttackCombo = (currentAttackCombo % 3) + 1; // 3 verschiedene Attacken
        
        if (playerAnimator != null) playerAnimator.PlayAttack(currentAttackCombo);
        
        Debug.Log($"Angriff #{currentAttackCombo} ausgeführt!");
    }
    
    private void PerformParry()
    {
        lastParryTime = Time.time;
        isParrying = true;
        parryEndTime = Time.time + parryDuration;
        
        if (playerAnimator != null) playerAnimator.PlayParry();
        
        Debug.Log("Parieren!");
    }
    
    private void UpdateParryState()
    {
        if (isParrying && Time.time >= parryEndTime)
        {
            isParrying = false;
        }
    }
    
    // Diese Methode wird vom Animator aufgerufen (Animation Event)
    public void OnAttackHit()
    {
        if (swordPosition == null)
        {
            Debug.LogWarning("Sword Position nicht gesetzt!");
            return;
        }
        
        // Alle Enemys im Angriffsradius finden
        Collider[] hits = Physics.OverlapSphere(swordPosition.position, attackRange, enemyLayer);
        
        foreach (Collider hit in hits)
        {
            // Enemy Komponente suchen und Schaden zufügen
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int finalDamage = attackDamage;
                
                // Verschiedene Attacken unterschiedlicher Schaden
                switch (currentAttackCombo)
                {
                    case 1:
                        finalDamage = attackDamage;
                        break;
                    case 2:
                        finalDamage = Mathf.RoundToInt(attackDamage * 1.2f);
                        break;
                    case 3:
                        finalDamage = Mathf.RoundToInt(attackDamage * 1.5f);
                        break;
                }
                
                damageable.TakeDamage(finalDamage);
                Debug.Log($"Enemy getroffen! Schaden: {finalDamage}");
            }
        }
    }
    
    // Überprüfe ob gerade Pariert wird (für Feinde zum Abschwächen)
    public bool IsParrying => isParrying;
    public float GetParryReduction => isParrying ? parryReduction : 1f;
    
    public int GetCurrentAttackCombo => currentAttackCombo;
}

// Interface für Objekte die Schaden nehmen können
public interface IDamageable
{
    void TakeDamage(int damage);
}