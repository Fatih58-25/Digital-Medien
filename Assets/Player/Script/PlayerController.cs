using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float jumpStaminaCost = 10f;             
    [SerializeField] private float sprintStaminaCostPerSec = 12f;     

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = 9.81f;
    public float jumpHeight = 2.0f;
    private PlayerHealth playerHealth;

    [Header("Animation Reference")]
    [SerializeField] private PlayerAnimator playerAnimator;

    [Header("Düşme Hasarı (Fall Damage)")]
    [SerializeField] private float safeFallDistance = 5.0f;    // Hasarsız düşülebilecek max mesafe
    [SerializeField] private float lethalFallDistance = 15.0f;  // Tamamen ölünen mesafe
    [SerializeField] private float maxFallDamage = 100f;        // 1000 yerine 100 yapıldı (Can barına uygun)
    
    [Header("Boşluk Sınırı (Void Limit)")]
    [SerializeField] private float voidYLevel = -50.0f;        // Bu Y yüksekliğinin altına düşerse anında ölür

    private float highestFallY; 
    private bool wasGrounded;

    private CharacterController controller;
    private Transform cameraTransform;
    private SoulsCamera soulsCamera; 
    private Vector3 moveDirection;
    private float verticalVelocity;

    private PlayerCombatSystem combatSystem;
    private PlayerStamina playerStamina;
    private PlayerFlaskSystem playerFlaskSystem;

    public bool IsDucking { get; private set; } = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        combatSystem = GetComponent<PlayerCombatSystem>();
        playerStamina = GetComponent<PlayerStamina>();
        playerFlaskSystem = GetComponent<PlayerFlaskSystem>();
        playerHealth = GetComponent<PlayerHealth>();

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<PlayerAnimator>();
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<PlayerAnimator>();
            }
        }

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            soulsCamera = Camera.main.GetComponent<SoulsCamera>(); 
        }

        highestFallY = transform.position.y;
    }

    void Update()
    { 
        if (controller == null || !controller.enabled) return;
        MovePlayer();
    }

    void MovePlayer()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        
        bool isAttacking = combatSystem != null && combatSystem.IsAttacking;
        bool isBlocking = combatSystem != null && combatSystem.IsBlocking;
        bool isDrinking = playerFlaskSystem != null && playerFlaskSystem.IsDrinking;
        bool isExhausted = playerStamina != null && playerStamina.IsExhausted;
        bool isRolling = combatSystem != null && combatSystem.IsRolling;

        bool isLockedOn = soulsCamera != null && soulsCamera.IsLockedOn && soulsCamera.LockedTarget != null;
        Transform lockedTarget = isLockedOn ? soulsCamera.LockedTarget : null;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        float currentSpeed = 0f;
        float actualMoveSpeed = moveSpeed;

        if (isDrinking)
        {
            actualMoveSpeed = moveSpeed * 0.25f;
        }

        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && 
                             (combatSystem == null || (Time.time - combatSystem.shiftPressTime) > 0.2f) && 
                             inputDir.magnitude >= 0.1f &&
                             !isAttacking &&
                             !isBlocking &&
                             !isDrinking &&
                             !isExhausted;

        bool isSprinting = false;
        if (wantsToSprint && playerStamina != null)
        {
            float sprintCostThisFrame = sprintStaminaCostPerSec * Time.deltaTime;
            if (playerStamina.UseStamina(sprintCostThisFrame))
            {
                isSprinting = true;
            }
        }

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        if (isAttacking)
        {
            if (isLockedOn)
            {
                Vector3 dirToEnemy = lockedTarget.position - transform.position;
                dirToEnemy.y = 0;
                if (dirToEnemy != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(dirToEnemy);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2.5f * Time.deltaTime);
                }
            }

            if (vertical > 0.1f)
            {
                Vector3 stepDir = isLockedOn ? (lockedTarget.position - transform.position).normalized : camForward;
                stepDir.y = 0;
                moveDirection = stepDir * vertical;
                actualMoveSpeed = moveSpeed * 0.8f;
                currentSpeed = 1f;
            }
            else
            {
                moveDirection = Vector3.zero;
                currentSpeed = 0f;
            }
        }
        else if (isBlocking && isLockedOn)
        {
            Vector3 dirToEnemy = lockedTarget.position - transform.position;
            dirToEnemy.y = 0;
            if (dirToEnemy != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToEnemy);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2.5f * Time.deltaTime);
            }

            actualMoveSpeed = moveSpeed * 0.6f; 

            if (inputDir.magnitude >= 0.1f)
            {
                moveDirection = (camForward * inputDir.z) + (camRight * inputDir.x);
                currentSpeed = 1f;
            }
            else
            {
                moveDirection = Vector3.zero;
                currentSpeed = 0f;
            }
        }
        else if (inputDir.magnitude >= 0.1f)
        {
            moveDirection = (camForward * inputDir.z) + (camRight * inputDir.x);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            if (isSprinting)
            {
                actualMoveSpeed = moveSpeed * 1.5f;
                currentSpeed = 2f;
            }
            else
            {
                currentSpeed = 1f;
            }
        }
        else
        {
            moveDirection = Vector3.zero;
            currentSpeed = 0f;
        }

        // ZIPLAMA & YERÇEKİMİ
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (playerAnimator != null)
            {
                Animator anim = playerAnimator.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetBool("Jump", false);
                }
            }
            
            if (!isAttacking && !isDrinking && Input.GetButtonDown("Jump"))
            {
                if (playerStamina != null && !playerStamina.IsExhausted)
                {
                    if (playerStamina.UseStamina(jumpStaminaCost))
                    {
                        verticalVelocity = Mathf.Sqrt(jumpHeight * 5f * gravity);

                        if (playerAnimator != null)
                        {
                            Animator anim = playerAnimator.GetComponent<Animator>();
                            if (anim != null)
                            {
                                anim.SetTrigger("Jump");
                                anim.SetBool("Jump", true);
                            }
                            playerAnimator.SetIsGrounded(false);
                        }
                    }
                }
            }
        }
        else
        {
            if (verticalVelocity <= 0) verticalVelocity -= gravity * 8f * Time.deltaTime;
            else verticalVelocity -= gravity * 5f * Time.deltaTime;
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetMovementSpeed(currentSpeed);
            playerAnimator.SetIsGrounded(controller.isGrounded);
        }

        Vector3 finalMove = moveDirection * actualMoveSpeed;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);

        // Düşme Mantığı Kontrolü
        HandleFallDamage(controller.isGrounded);
    }

    private void HandleFallDamage(bool isGrounded)
    {
        // 🟢 1. BOŞLUĞA DÜŞME KONTROLÜ (Void Check)
        if (transform.position.y < voidYLevel)
        {
            if (playerHealth != null && !playerHealth.IsDead)
            {
                // Boşluğa düşerse anında öldür (veya devasa hasar ver)
                playerHealth.TakeFallDamage(99999);
            }
            return;
        }

        // 🟢 2. YERE İNİŞ ANINDA HASAR HESAPLAMA
        if (isGrounded && !wasGrounded)
        {
            float fallDistance = highestFallY - transform.position.y;

            if (fallDistance > safeFallDistance)
            {
                ApplyFallDamage(fallDistance);
            }
        }

        // 🟢 3. YÜKSEKLİK TAKİBİ
        if (isGrounded)
        {
            highestFallY = transform.position.y;
        }
        else
        {
            // Zıplarken veya havada yukarı doğru fırlarken en yüksek noktayı güncelle
            if (transform.position.y > highestFallY)
            {
                highestFallY = transform.position.y;
            }
        }

        wasGrounded = isGrounded;
    }

    private void ApplyFallDamage(float distance)
    {
        if (playerHealth == null) return;

        float excessDistance = distance - safeFallDistance;
        float lethalRange = lethalFallDistance - safeFallDistance;

        if (lethalRange <= 0f) return;

        // 0.0 (güvenli) ile 1.0 (ölümcül) arasında oran hesapla
        float damageMultiplier = Mathf.Clamp01(excessDistance / lethalRange);
        
        // Hasarı hesapla (Örn: maxFallDamage 100 ise 10m ekstra düşüş 100 HP götürür)
        int damageToTake = Mathf.RoundToInt(damageMultiplier * maxFallDamage);

        if (damageToTake > 0)
        {
            playerHealth.TakeFallDamage(damageToTake); 
        }
    }

    public void ResetFallData()
    {
        highestFallY = transform.position.y;
    }
}