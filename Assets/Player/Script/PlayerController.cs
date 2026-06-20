using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = 9.81f;

    [Header("Animation Reference")]
    private PlayerAnimator playerAnimator; // Senin animatör scriptini referans alıyoruz

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 moveDirection;
    private float verticalVelocity;

    // Diğer scriptlerin (Combat) aradığı IsDucking değişkeni
    public bool IsDucking { get; private set; } = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerAnimator = GetComponent<PlayerAnimator>(); // Aynı objede olduklarını varsayıyoruz

        // Eğer sahne açıldığında otomatik bulamadıysa alt objelere de baksın
        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<PlayerAnimator>();
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
        // Girdileri al (W, A, S, D)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        float currentSpeed = 0f;

        // Karakter hareket ediyorsa
        if (inputDir.magnitude >= 0.1f)
        {
            // Kameranın açısına göre hareket yönünü hesapla
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            // Gerçek hareket yönü
            moveDirection = (camForward * inputDir.z) + (camRight * inputDir.x);

            // Karakteri hareket yönüne doğru yumuşakça döndür
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Animatöre göndermek için hız değerini alıyoruz (0 ile moveSpeed arasında bir değer)
            currentSpeed = inputDir.magnitude * moveSpeed;
        }
        else
        {
            moveDirection = Vector3.zero;
            currentSpeed = 0f; // Hareket yoksa hız sıfır
        }

        // Yerçekimi hesaplaması
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; 
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // ANIMATÖRÜ GÜNCELLEME KISMI (Burada senin scripti çağırıyoruz)
        if (playerAnimator != null)
        {
            // 1. Karakterin hızını animatöre gönderiyoruz (Animator içindeki "Speed" parametresini tetikler)
            playerAnimator.SetMovementSpeed(currentSpeed);

            // 2. Karakter yerde mi havada mı bilgisini gönderiyoruz ("IsGrounded" parametresi)
            playerAnimator.SetIsGrounded(controller.isGrounded);
        }

        moveDirection.y = verticalVelocity;

        // Karakteri hareket ettir
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);
    }
}