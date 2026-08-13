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

    private CharacterController controller;
    private Transform cameraTransform;
    private SoulsCamera soulsCamera; 
    private Vector3 moveDirection;
    private float verticalVelocity;

    // Referanslar
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
    }

    void Update()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        bool isAttacking = combatSystem != null && combatSystem.IsAttacking;
        bool isDrinking = playerFlaskSystem != null && playerFlaskSystem.IsDrinking;
        bool isExhausted = playerStamina != null && playerStamina.IsExhausted;
        bool isRolling = combatSystem != null && combatSystem.IsRolling;

        // Kamera kilitlenme durumunu ve hedefini kontrol et
        bool isLockedOn = soulsCamera != null && soulsCamera.IsLockedOn && soulsCamera.LockedTarget != null;
        Transform lockedTarget = isLockedOn ? soulsCamera.LockedTarget : null;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        float currentSpeed = 0f;
        float actualMoveSpeed = moveSpeed;

        // İksir içerken hızı %25'e düşür
        if (isDrinking)
        {
            actualMoveSpeed = moveSpeed * 0.25f;
        }

        // Shift'e basılı tutma kontrolü
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && 
                             (combatSystem == null || (Time.time - combatSystem.shiftPressTime) > 0.2f) && 
                             inputDir.magnitude >= 0.1f &&
                             !isAttacking &&
                             !isDrinking &&
                             !isExhausted;

        // Koşma Mantığı
        bool isSprinting = false;
        if (wantsToSprint && playerStamina != null)
        {
            float sprintCostThisFrame = sprintStaminaCostPerSec * Time.deltaTime;
            if (playerStamina.UseStamina(sprintCostThisFrame))
            {
                isSprinting = true;
            }
        }

        // Kamera Yönlerini Hesapla
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        // ⚔️ 1. SALDIRI ANINDAKİ ROTASYON VE HAREKET
        if (isAttacking)
        {
            // 🟢 YALNIZCA SALDIRIRKEN düşmana kilitliysek yüzümüzü anında/pürüzsüzce düşmana dönüyoruz
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
                // İleri basılıyorsa kilitli hedefe (veya kameraya) doğru hafifçe adımla
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
        // 🏃 2. NORMAL HAREKET & YUVARLANMA ANINDAKİ ROTASYON
        else if (inputDir.magnitude >= 0.1f)
        {
            // Hareket yönümüzü kameranın baktığı açıya göre hesapla
            moveDirection = (camForward * inputDir.z) + (camRight * inputDir.x);

            // 🟢 Kilitli olsak bile yuvarlanırken, yürürken veya koşarken serbestçe Bastığımız Yöne dönüyoruz
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

        // ========================================================
        // ZIPLAMA & YERÇEKİMİ
        // ========================================================
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            
            if (!isAttacking && !isDrinking && Input.GetButtonDown("Jump"))
            {
                if (playerStamina != null && !playerStamina.IsExhausted)
                {
                    if (playerStamina.UseStamina(jumpStaminaCost))
                    {
                        verticalVelocity = Mathf.Sqrt(jumpHeight * 5f * gravity);
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
    }
}