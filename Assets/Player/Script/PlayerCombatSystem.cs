using UnityEngine;
using System.Collections;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Stamina Ayarları")]
    [SerializeField] private float attackStaminaCost = 20f; 
    [SerializeField] private float rollStaminaCost = 15f;   

    [Header("Angriff")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackAnimationDuration = 1.0f;
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
    [SerializeField] private float iframeDelay = 0.08f;    
    [SerializeField] private float iframeDuration = 0.35f; 
    [SerializeField] private KeyCode rollKey = KeyCode.X;

    private PlayerAnimator playerAnimator;
    private PlayerController playerController;
    private CharacterController charController;
    private PlayerStamina playerStamina;
    private PlayerFlaskSystem playerFlaskSystem;
    private PlayerHealth playerHealth;

    private float lastAttackTime;
    private float lastRollTime;
    private bool isRolling = false;
    private float rollEndTime;
    private bool isInvincible = false;
    private float iframeEndTime;

    private int currentAttackCombo = 0;
    private int queuedComboIndex = 1;

    private bool isAttacking = false;
    private bool isStaggered = false;

    public float shiftPressTime = 0f;
    private bool isShiftPressed = false;

    private Coroutine iframeCoroutine; 

    private void Start()
    {
        playerAnimator = GetComponent<PlayerAnimator>();
        playerController = GetComponent<PlayerController>();
        charController = GetComponent<CharacterController>();
        playerStamina = GetComponent<PlayerStamina>();
        playerFlaskSystem = GetComponent<PlayerFlaskSystem>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (isStaggered) return;

        HandleRollInput();
        HandleAttackInput();
        HandleBlockInput();
        UpdateRollState();
        UpdateAttackState();
        UpdateComboState();
    }

    // 🟢 RESPAWN OLUNCA SAVAŞ DURUMLARINI TEMİZLE
    public void ResetCombatState()
    {
        isAttacking = false;
        isRolling = false;
        isBlocking = false;
        isStaggered = false;
        isInvincible = false;
        currentAttackCombo = 0;
        
        if (iframeCoroutine != null)
        {
            StopCoroutine(iframeCoroutine);
        }

        CancelInvoke(nameof(ResetStagger));
    }

    private void HandleAttackInput()
    {
        bool isDrinking = playerFlaskSystem != null && playerFlaskSystem.IsDrinking;

        if (Input.GetMouseButtonDown(0) && !isBlocking && !isRolling && !isAttacking && !isDrinking && (playerController != null && !playerController.IsDucking))
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                if (playerStamina != null && playerStamina.HasEnoughStamina(attackStaminaCost))
                {
                    PerformAttack();
                }
            }
        }
    }

    private void HandleBlockInput()
    {
        bool isDrinking = playerFlaskSystem != null && playerFlaskSystem.IsDrinking;

        if (!Input.GetMouseButton(1) || isRolling || isAttacking || isDrinking || (playerController != null && playerController.IsDucking))
        {
            if (isBlocking) StopBlocking();
            return;
        }
        if (Input.GetMouseButton(1) && !isBlocking) StartBlocking();
    }

    private void HandleRollInput()
    {
        bool isDrinking = playerFlaskSystem != null && playerFlaskSystem.IsDrinking;

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftPressTime = Time.time;
            isShiftPressed = true;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) && isShiftPressed)
        {
            isShiftPressed = false;
            float pressDuration = Time.time - shiftPressTime;

            if (pressDuration < 0.2f && !isRolling && !isAttacking && !isDrinking && (playerController != null && !playerController.IsDucking))
            {
                if (Time.time - lastRollTime >= rollCooldown)
                {
                    if (playerStamina != null && playerStamina.HasEnoughStamina(rollStaminaCost))
                    {
                        if (isBlocking) StopBlocking();
                        ResetCombo();
                        PerformRoll();
                    }
                }
            }
        }
    }

    private void PerformAttack()
    {
        playerStamina?.UseStamina(attackStaminaCost);

        isAttacking = true;
        lastAttackTime = Time.time;
        queuedComboIndex = (currentAttackCombo >= maxComboCount) ? 1 : currentAttackCombo + 1;

        playerAnimator?.PlayAttack(queuedComboIndex);
        currentAttackCombo = (currentAttackCombo >= maxComboCount) ? 0 : currentAttackCombo + 1;
    }

    private void PerformRoll()
    {
        playerStamina?.UseStamina(rollStaminaCost);

        lastRollTime = Time.time;
        isRolling = true;
        rollEndTime = Time.time + rollDuration;

        playerAnimator?.GetComponent<Animator>()?.SetTrigger("Roll");

        if (iframeCoroutine != null) StopCoroutine(iframeCoroutine);
        iframeCoroutine = StartCoroutine(ActivateIFrameRoutine());
    }

    private IEnumerator ActivateIFrameRoutine()
    {
        isInvincible = false;
        yield return new WaitForSeconds(iframeDelay);
        isInvincible = true;
        iframeEndTime = Time.time + iframeDuration;
    }

    private void StartBlocking() { isBlocking = true; playerAnimator?.GetComponent<Animator>()?.SetBool("IsBlocking", true); }
    private void StopBlocking() { isBlocking = false; playerAnimator?.GetComponent<Animator>()?.SetBool("IsBlocking", false); }

    public void ApplyStagger(float duration, Vector3 knockbackDir, float force)
    {
        if (isInvincible) return;

        playerFlaskSystem?.InterruptDrink();
        if (iframeCoroutine != null) StopCoroutine(iframeCoroutine);

        isInvincible = false;
        isAttacking = false;
        isRolling = false;
        if (isBlocking) StopBlocking();
        ResetCombo();

        isStaggered = true;
        playerAnimator?.GetComponent<Animator>()?.SetTrigger("Stagger");
        
        StartCoroutine(ApplyKnockbackEffect(knockbackDir, force));
        
        CancelInvoke(nameof(ResetStagger));
        Invoke(nameof(ResetStagger), duration);
    }

    private IEnumerator ApplyKnockbackEffect(Vector3 dir, float force)
    {
        float timer = 0.08f; 
        dir.y = 0;
        
        while (timer > 0 && charController != null)
        {
            charController.Move(dir * force * Time.deltaTime);
            timer -= Time.deltaTime;
            yield return null;
        }
    }

    private void ResetStagger() => isStaggered = false;
    private void UpdateAttackState() { if (isAttacking && Time.time - lastAttackTime >= attackAnimationDuration) isAttacking = false; }
    private void UpdateRollState() 
    { 
        if (isInvincible && Time.time >= iframeEndTime) isInvincible = false; 
        if (isRolling && Time.time >= rollEndTime) isRolling = false; 
    }
    private void UpdateComboState() { if (Time.time - lastAttackTime > comboResetDelay && currentAttackCombo > 0) ResetCombo(); }
    private void ResetCombo() { currentAttackCombo = 0; queuedComboIndex = 1; }

    public void OnAttackHit()
    {
        if (swordPosition == null || isStaggered) return;
        Collider[] hits = Physics.OverlapSphere(swordPosition.position, attackRange, enemyLayer);
        foreach (var hit in hits)
        {
            hit.GetComponentInParent<IDamageable>()?.TakeDamage(attackDamage);
            hit.GetComponentInParent<EnemyBase>()?.ApplyKnockback((hit.transform.position - transform.position).normalized);
        }
    }

    public bool IsAttacking => isAttacking;
    public bool IsStaggered => isStaggered;
    public bool IsParrying => isBlocking;
    public float GetParryReduction => isBlocking ? parryReduction : 1f;
    public bool IsRolling => isRolling;
    public bool IsInvincible => isInvincible;
}