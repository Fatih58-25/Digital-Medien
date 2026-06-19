using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Bewegung")]
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float duckSpeed = 2.5f;
    [SerializeField] private float groundDrag = 5f;

    [Header("Springen")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float airDrag = 2f;

    [Header("Boden")]
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Ducken")]
    [SerializeField] private float duckHeight = 0.5f;
    [SerializeField] private float normalHeight = 1.8f;
    [SerializeField] private float duckSmooth = 5f;

    // Komponenten
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private PlayerAnimator playerAnimator;
    private Vector3 moveDirection;
    private Vector3 capsuleCenter;

    // Zustände
    private bool isGrounded;
    private bool isSprinting;
    private bool isDucking;
    private bool canJump = true;

    private float currentSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerAnimator = GetComponent<PlayerAnimator>();
        capsuleCenter = capsuleCollider.center;

        // Rigidbody Setup
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // Boden Check
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundDistance, groundLayer);

        // Input
        HandleMovementInput();
        HandleSprintInput();
        HandleDuckInput();
        HandleJumpInput();

        // Ducken Animation
        HandleDuckAnimation();

        // Geschwindigkeit
        AdjustSpeed();

        // Animation
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        ApplyDrag();
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Bewegungsrichtung relativ zur Kameraausrichtung
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        // Flatten Y axis für korrekte Bodenbewegung
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        moveDirection = forward * vertical + right * horizontal;

        // Character Rotation zum Blick der Kamera
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void HandleSprintInput()
    {
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isDucking && isGrounded;
    }

    private void HandleDuckInput()
    {
        isDucking = Input.GetKey(KeyCode.LeftControl);
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && canJump && !isDucking)
        {
            Jump();
        }
    }

    private void Jump()
    {
        // Y Velocity zurücksetzen
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        // Jump Force anwenden
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        canJump = false;
        playerAnimator.SetJump(true);

        Invoke(nameof(ResetJumpCooldown), jumpCooldown);
    }

    private void ResetJumpCooldown()
    {
        canJump = true;
    }

    private void AdjustSpeed()
    {
        if (isDucking)
        {
            currentSpeed = duckSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = normalSpeed;
        }
    }

    private void MovePlayer()
    {
        // Bewegung nur auf horizontale Achse anwenden
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDirection.x * currentSpeed;
        velocity.z = moveDirection.z * currentSpeed;
        rb.linearVelocity = velocity;
    }

    private void ApplyDrag()
    {
        rb.linearDamping = isGrounded ? groundDrag : airDrag;
    }

    private void HandleDuckAnimation()
    {
        float targetHeight = isDucking ? duckHeight : normalHeight;

        // Capsule Height anpassen
        float newHeight = Mathf.Lerp(capsuleCollider.height, targetHeight, Time.deltaTime * duckSmooth);
        capsuleCollider.height = newHeight;

        // Capsule Center für korrektes Ducken anpassen
        Vector3 newCenter = capsuleCenter;
        newCenter.y = (newHeight / 2f);
        capsuleCollider.center = newCenter;
    }

    private void UpdateAnimations()
    {
        float speed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;

        playerAnimator.SetMovementSpeed(speed);
        playerAnimator.SetIsSprinting(isSprinting);
        playerAnimator.SetIsDucking(isDucking);
        playerAnimator.SetIsGrounded(isGrounded);
    }

    // Getter für andere Systeme
    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;
    public bool IsDucking => isDucking;
    public Vector3 GetMoveDirection => moveDirection;
}
