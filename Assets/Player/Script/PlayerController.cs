using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float jumpStaminaCost = 10f;             // Zıplama stamina maliyeti
    [SerializeField] private float sprintStaminaCostPerSec = 12f;     // Saniyede harcanan koşma staminası

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = 9.81f;
    public float jumpHeight = 2.0f; // Exakte Sprunghöhe in Metern

    [Header("Animation Reference")]
    [SerializeField] private PlayerAnimator playerAnimator;

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 moveDirection;
    private float verticalVelocity;

    // Referanslar
    private PlayerCombatSystem combatSystem;
    private PlayerStamina playerStamina; // Stamina referansı

    public bool IsDucking { get; private set; } = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        combatSystem = GetComponent<PlayerCombatSystem>();
        playerStamina = GetComponent<PlayerStamina>(); // Component'i alıyoruz

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
        }
    }

    void Update()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        bool isAttacking = combatSystem != null && combatSystem.IsAttacking;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        float currentSpeed = 0f;
        float actualMoveSpeed = moveSpeed;

        // Shift'e basılı tutma kontrolü
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && 
                             (Time.time - combatSystem.shiftPressTime) > 0.2f && 
                             inputDir.magnitude >= 0.1f &&
                             !isAttacking;

        // Koşmak istiyor ve YETERLİ STAMİNA var mı?
        bool isSprinting = false;
        if (wantsToSprint)
        {
            float sprintCostThisFrame = sprintStaminaCostPerSec * Time.deltaTime;

            if (playerStamina != null && playerStamina.HasEnoughStamina(sprintCostThisFrame))
            {
                playerStamina.UseStamina(sprintCostThisFrame); // Zamana bağlı stamina düşüşü
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

        // ⚔️ SALDIRI ANINDAKİ HAREKET KONTROLÜ
        if (isAttacking)
        {
            if (vertical > 0.1f)
            {
                moveDirection = camForward * vertical;
                actualMoveSpeed = moveSpeed * 0.8f;
                currentSpeed = 1f;
            }
            else
            {
                moveDirection = Vector3.zero;
                currentSpeed = 0f;
            }
        }
        // 🏃 NORMAL HAREKET
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

        // ========================================================
        // ZIPLAMA & YERÇEKİMİ
        // ========================================================
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            
            // Zıplama Girdisi & Stamina Kontrolü
            if (!isAttacking && Input.GetButtonDown("Jump"))
            {
                if (playerStamina != null && playerStamina.HasEnoughStamina(jumpStaminaCost))
                {
                    playerStamina.UseStamina(jumpStaminaCost); // Zıplama staminası düşer
                    verticalVelocity = Mathf.Sqrt(jumpHeight * 5f * gravity);
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