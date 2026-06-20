using UnityEngine;

public class MinimalCameraInput : MonoBehaviour
{
    [Header("References")]
    public Transform target; // Takip edilecek karakter (Player)

    [Header("Distance Settings")]
    public float distance = 4.0f; // Karakter ile kamera arasındaki sabit mesafe
    public float height = 2.0f;   // Kameranın karakterden yüksekliği

    [Header("Rotation Settings")]
    public float xSpeed = 120.0f; // Mouse sağ-sol hızı
    public float ySpeed = 120.0f; // Mouse yukarı-aşağı hızı
    public float yMinLimit = -10f; // Aşağı bakma sınırı
    public float yMaxLimit = 60f;  // Yukarı bakma sınırı

    [Header("Souls-like Auto Follow")]
    public float autoFollowSpeed = 2.0f; // Karakter arkasına geçme hızı
    public float idleAutoFollowDelay = 1.0f; // Fare bırakıldıktan kaç saniye sonra otomatik dönsün?

    private float x = 0.0f;
    private float y = 0.0f;
    private float lastMouseInputTime;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // İmleci oyun ekranına gizle ve kilitle (Esc ile kurtulabilirsin)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Mouse girdilerini al
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (mouseX != 0 || mouseY != 0)
        {
            // Oyuncu mouse'u hareket ettiriyorsa manuel kontrolü al
            x += mouseX * xSpeed * 0.02f;
            y -= mouseY * ySpeed * 0.02f;
            y = ClampAngle(y, yMinLimit, yMaxLimit);

            lastMouseInputTime = Time.time;
        }
        else
        {
            // Oyuncu fareyi bıraktıysa ve karakter hareket ediyorsa (W-A-S-D)
            float playerSpeed = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).magnitude;
            
            if (playerSpeed > 0.1f && Time.time - lastMouseInputTime > idleAutoFollowDelay)
            {
                // Kameranın yatay açısını (X), karakterin mevcut arkasına doğru yavaşça eşitle
                float targetRotationY = target.eulerAngles.y;
                x = Mathf.LerpAngle(x, targetRotationY, autoFollowSpeed * Time.deltaTime);
            }
        }

        // Hesaplanan açılara göre rotasyonu oluştur
        Quaternion rotation = Quaternion.Euler(y, x, 0);

        // Kameranın pozisyonunu ayarla (Karakterin arkasında ve yukarısında olacak şekilde)
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + (target.position + Vector3.up * height);

        // Pozisyon ve rotasyonu kameraya uygula
        transform.rotation = rotation;
        transform.position = position;
    }

    // Açıları sınırlamak için yardımcı fonksiyon
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
    // PlayerController.cs içindeki "gravity" değişkeninin hemen altına ekleyebilirsin:
public bool IsDucking { get; private set; } // Diğer scriptlerin okuyabilmesi için
}