using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = 9.81f;
    public float jumpHeight = 2.0f; // Exakte Sprunghöhe in Metern

    [Header("Animation Reference")]
    [SerializeField] private PlayerAnimator playerAnimator; // Jetzt im Inspector sichtbar!

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 moveDirection;
    private float verticalVelocity;

    // Wird vom Combat-System abgefragt
    public bool IsDucking { get; private set; } = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Falls im Inspector nicht zugewiesen, automatisch suchen
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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        float currentSpeed = 0f;
        float actualMoveSpeed = moveSpeed; // Basis-Geschwindigkeit (5)

        // SPRINT-CHECK: Wenn Shift gedrückt wird UND wir uns bewegen
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDir.magnitude >= 0.1f;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            moveDirection = (camForward * inputDir.z) + (camRight * inputDir.x);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Wenn wir sprinten, erhöhen wir das Tempo im Spiel und setzen den Animations-Wert auf 2
            if (isSprinting)
            {
                actualMoveSpeed = moveSpeed * 1.3f; // Macht den Charakter 60% schneller beim Rennen
                currentSpeed = 2f; // 2 bedeutet "Rennen" im Animator
            }
            else
            {
                currentSpeed = 1f; // 1 bedeutet "Gehen/Laufen"
            }
        }
        else
        {
            moveDirection = Vector3.zero;
            currentSpeed = 0f; // 0 bedeutet "Stillstand"
        }

        // ========================================================
        // ZIPLAMA & SMART GRAVITY (Dein perfekt eingestellter Sprung)
        // ========================================================
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * 5f * gravity);
            }
        }
        else
        {
            if (verticalVelocity <= 0) verticalVelocity -= gravity * 8f * Time.deltaTime;
            else verticalVelocity -= gravity * 5f * Time.deltaTime;
        }

        // Werte an den Animator übergeben
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