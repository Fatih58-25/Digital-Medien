using UnityEngine;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Angriff")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform swordPosition;

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

    private int currentAttackCombo = 0;

    private void Start()
    {
        playerAnimator = GetComponent<PlayerAnimator>();
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        HandleRollInput();
        HandleAttackInput();
        HandleBlockInput(); // Unser neues 3-Phasen-Blocken

        UpdateRollState();
    }

    private void HandleAttackInput()
    {
        // Angreifen blockiert, wenn man rollt, duckt oder GERADE BLOCKT
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
        // Wenn man rollt oder duckt, kann man nicht blocken
        if (isRolling || playerController.IsDucking)
        {
            if (isBlocking) StopBlocking();
            return;
        }

        // Rechtsklick gedrückt HALTEN -> Blocken starten/halten
        if (Input.GetMouseButton(1))
        {
            if (!isBlocking)
            {
                StartBlocking();
            }
        }
        // Rechtsklick LOSLASSEN -> Blocken beenden
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
        // Rollen beendet das Blocken sofort (Cancel-Mechanik wie in Souls)
        if (Input.GetKeyDown(rollKey) && !isRolling && !playerController.IsDucking)
        {
            if (Time.time - lastRollTime >= rollCooldown)
            {
                if (isBlocking) isBlocking = false; // Block-Zustand im Code lösen
                PerformRoll();
            }
        }
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        currentAttackCombo = Random.Range(1, 4);

        if (playerAnimator != null)
        {
            playerAnimator.PlayAttack(currentAttackCombo);
        }
        Debug.Log($"Zufälliger Angriff #{currentAttackCombo} ausgeführt!");
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
            playerAnimator.GetComponent<Animator>().SetBool("IsBlocking", false); // Zur Sicherheit
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
                switch (currentAttackCombo)
                {
                    case 1: finalDamage = attackDamage; break;
                    case 2: finalDamage = Mathf.RoundToInt(attackDamage * 1.2f); break;
                    case 3: finalDamage = Mathf.RoundToInt(attackDamage * 1.5f); break;
                }
                damageable.TakeDamage(finalDamage);
            }
        }
    }

    // Für das Schadenssystem: Wenn der Spieler getroffen wird und IsBlocking wahr ist, reduziere den Schaden!
    public bool IsParrying => isBlocking;
    public float GetParryReduction => isBlocking ? parryReduction : 1f;

    public bool IsRolling => isRolling;
    public bool IsInvincible => isInvincible;
}

// HIER IST DAS INTERFACE WIEDER DA:
public interface IDamageable
{
    void TakeDamage(int damage);
}