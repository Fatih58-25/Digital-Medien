using UnityEngine;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Angriff")]
    [SerializeField] private float attackCooldown = 0.4f; // Combo arası geçiş süresi için biraz düşürüldü
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform swordPosition;

    [Header("Souls Kombo Ayarları")]
    [SerializeField] private float comboResetDelay = 1.2f; // Tıklama bırakılırsa kombo kaç sn sonra sıfırlansın?
    private int currentAttackCombo = 0; // Şu anki kombo indeksi (0, 1, 2)
    private int maxComboCount = 3;       // 3 farklı animasyonumuz var

    [Header("Souls-like Blocken")]
    [SerializeField] private float parryReduction = 0.7f; // 70% Schadensreduktion beim Blocken
    private bool isBlocking = false;

    [Header("Souls-like Rollen & iFrames")]
    [SerializeField] private float rollCooldown = 1.0f;
    [SerializeField] private float rollDuration = 0.6f;
    [SerializeField] private float iframeDuration = 0.35f;
    [SerializeField] private KeyCode rollKey = KeyCode.X;

    private PlayerAnimator playerAnimator;
    private PlayerController playerController;

    private float lastAttackTime;
    private float lastRollTime;
    private bool isRolling = false;
    private float rollEndTime;
    private bool isInvincible = false;
    private float iframeEndTime;

    private void Start()
    {
        playerAnimator = GetComponent<PlayerAnimator>();
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        HandleRollInput();
        HandleAttackInput();
        HandleBlockInput(); 

        UpdateRollState();
        UpdateComboState(); // Kombo zaman aşımı kontrolü
    }

    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0) && !isBlocking && !isRolling && !playerController.IsDucking)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
            }
        }
    }

    private void UpdateComboState()
    {
        // Eğer oyuncu vurmayı bıraktıysa ve belirlenen süre geçtiyse komboyu sıfırla
        if (Time.time - lastAttackTime > comboResetDelay && currentAttackCombo > 0)
        {
            ResetCombo();
        }
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;

        // Rastgelelik (Random.Range) tamamen kaldırıldı! 
        // Animasyonlar 1, 2, 3 şeklinde gittiği için comboIndex'e 1 ekleyip gönderiyoruz.
        int animationIndex = currentAttackCombo + 1;

        if (playerAnimator != null)
        {
            playerAnimator.PlayAttack(animationIndex);
        }
        Debug.Log($"Souls-like Angriff #{animationIndex} ausgeführt!");

        // Komboyu bir sonraki adıma geçir
        currentAttackCombo++;

        // Eğer kombo serisi bittiyse (3 vuruş yapıldıysa) sıfırla
        if (currentAttackCombo >= maxComboCount)
        {
            currentAttackCombo = 0;
        }
    }

    private void ResetCombo()
    {
        currentAttackCombo = 0;
        Debug.Log("Combo zurückgesetzt.");
    }

    private void HandleBlockInput()
    {
        if (isRolling || playerController.IsDucking)
        {
            if (isBlocking) StopBlocking();
            return;
        }

        if (Input.GetMouseButton(1))
        {
            if (!isBlocking)
            {
                StartBlocking();
            }
        }
        else if (isBlocking)
        {
            StopBlocking();
        }
    }

    private void StartBlocking()
    {
        isBlocking = true;
        if (playerAnimator != null)
        {
            playerAnimator.GetComponent<Animator>().SetBool("IsBlocking", true);
        }
        Debug.Log("Schild hoch! Blocken aktiv.");
    }

    private void StopBlocking()
    {
        isBlocking = false;
        if (playerAnimator != null)
        {
            playerAnimator.GetComponent<Animator>().SetBool("IsBlocking", false);
        }
        Debug.Log("Schild runter! Blocken beendet.");
    }

    private void HandleRollInput()
    {
        if (Input.GetKeyDown(rollKey) && !isRolling && !playerController.IsDucking)
        {
            if (Time.time - lastRollTime >= rollCooldown)
            {
                if (isBlocking) isBlocking = false; 
                ResetCombo(); // Yuvarlanınca kombo zinciri de kırılır (Tam Souls tarzı)
                PerformRoll();
            }
        }
    }

    private void PerformRoll()
    {
        lastRollTime = Time.time;
        isRolling = true;
        isInvincible = true;

        rollEndTime = Time.time + rollDuration;
        iframeEndTime = Time.time + iframeDuration;

        if (playerAnimator != null)
        {
            playerAnimator.GetComponent<Animator>().SetBool("IsBlocking", false); 
            playerAnimator.GetComponent<Animator>().SetTrigger("Roll");
        }
        Debug.Log("Souls-like Rolle ausgeführt!");
    }

    private void UpdateRollState()
    {
        if (isInvincible && Time.time >= iframeEndTime)
        {
            isInvincible = false;
        }
        if (isRolling && Time.time >= rollEndTime)
        {
            isRolling = false;
        }
    }

    public void OnAttackHit()
    {
        if (swordPosition == null) return;
        Collider[] hits = Physics.OverlapSphere(swordPosition.position, attackRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int finalDamage = attackDamage;

                // Burada hasarı o anki kombo sırasına göre hesaplıyoruz.
                // Not: PerformAttack içinde currentAttackCombo arttığı için buradaki kontrolleri bir önceki adıma göre yapıyoruz.
                int hitIndex = currentAttackCombo == 0 ? 3 : currentAttackCombo;

                switch (hitIndex)
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
                Debug.Log($"Gegner mit Combo-Schritt {hitIndex} getroffen! Schaden: {finalDamage}");
            }
        }
    }

    public bool IsParrying => isBlocking;
    public float GetParryReduction => isBlocking ? parryReduction : 1f;
    public bool IsRolling => isRolling;
    public bool IsInvincible => isInvincible;
}

public interface IDamageable
{
    void TakeDamage(int damage);
}