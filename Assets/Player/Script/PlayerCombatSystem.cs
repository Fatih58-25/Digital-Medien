using UnityEngine;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Angriff")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform swordPosition;

    [Header("Souls Kombo Ayarları")]
    [SerializeField] private float comboResetDelay = 1.2f;
    [SerializeField] private int maxComboCount = 3;

    [Header("Souls-like Blocken")]
    [SerializeField] private float parryReduction = 0.7f;
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

    private int currentAttackCombo = 0;
    private int queuedComboIndex = 1;

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
        UpdateComboState();
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

    private void HandleRollInput()
    {
        if (Input.GetKeyDown(rollKey) && !isRolling && !playerController.IsDucking)
        {
            if (Time.time - lastRollTime >= rollCooldown)
            {
                if (isBlocking) StopBlocking();
                ResetCombo();
                PerformRoll();
            }
        }
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        queuedComboIndex = currentAttackCombo + 1;

        if (queuedComboIndex > maxComboCount)
        {
            queuedComboIndex = 1;
            currentAttackCombo = 0;
        }

        if (playerAnimator != null)
        {
            playerAnimator.PlayAttack(queuedComboIndex);
        }

        Debug.Log($"Souls-like Angriff #{queuedComboIndex} ausgeführt!");

        currentAttackCombo++;
        if (currentAttackCombo >= maxComboCount)
        {
            currentAttackCombo = 0;
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

    private void UpdateComboState()
    {
        if (Time.time - lastAttackTime > comboResetDelay && currentAttackCombo > 0)
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        currentAttackCombo = 0;
        queuedComboIndex = 1;
        Debug.Log("Combo zurückgesetzt.");
    }

    // DIESE METHODE WIRD EXAKT EINMAL VOM ANIMATION EVENT AUFGERUFEN
    public void OnAttackHit()
    {
        if (swordPosition == null) return;

        // Erstellt eine Kugel an der Schwertposition und checkt, wer im EnemyLayer getroffen wurde
        Collider[] hits = Physics.OverlapSphere(swordPosition.position, attackRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();

            if (damageable != null)
            {
                int finalDamage = attackDamage;

                // Multipliziert den Schaden basierend auf dem aktuellen Combo-Schritt
                switch (queuedComboIndex)
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

                if (enemy != null)
                {
                    Vector3 dir = enemy.transform.position - transform.position;
                    enemy.ApplyKnockback(dir);
                }

                Debug.Log($"Gegner mit Combo-Schritt {queuedComboIndex} getroffen! Schaden: {finalDamage}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Zeichnet eine rote Kugel im Editor, damit du die Reichweite (Attack Range) visuell anpassen kannst
        if (swordPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(swordPosition.position, attackRange);
        }
    }

    public bool IsParrying => isBlocking;
    public float GetParryReduction => isBlocking ? parryReduction : 1f;
    public bool IsRolling => isRolling;
    public bool IsInvincible => isInvincible;
}