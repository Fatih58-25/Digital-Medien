using UnityEngine;

public class SoulsCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target; // Karakter (Player)

    [Header("Distance Settings")]
    public float distance = 4.0f; // Karakter ile kamera arasındaki sabit mesafe
    public float height = 2.0f;   // Kameranın karakterden yüksekliği

    [Header("Rotation Settings")]
    public float xSpeed = 120.0f; 
    public float ySpeed = 120.0f; 
    public float yMinLimit = -10f; 
    public float yMaxLimit = 60f;  

    [Header("Deadzone (Ölü Bölge) Settings")]
    [Tooltip("Karakter bu yarıçap içinde hareket ederken kamera onu takip etmek için pozisyonunu değiştirmez.")]
    public float deadzoneRadius = 1.5f; 
    [Tooltip("Karakter ölü bölgeden çıktığında kameranın odak noktasının karaktere yetişme hızı.")]
    public float cameraFollowSmooth = 5.0f;

    [Header("Souls-like Auto Follow")]
    public float autoFollowSpeed = 2.0f; 
    public float idleAutoFollowDelay = 1.0f; 

    private float x = 0.0f;
    private float y = 0.0f;
    private float lastMouseInputTime;
    
    // Kameranın asıl takip ettiği hayali merkez noktası (Ölü bölge için)
    private Vector3 cameraTargetCenter; 

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target)
        {
            // Başlangıçta odak noktasını karakterin olduğu yere eşitle
            cameraTargetCenter = target.position;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1. ÖLÜ BÖLGE (DEADZONE) HESAPLAMASI
        // Karakter ile kameranın şu an odaklandığı merkez arasındaki mesafeyi buluyoruz
        Vector3 distanceToTarget = target.position - cameraTargetCenter;
        
        // Eğer karakter ölü bölge sınırını aşarsa, merkez noktasını karaktere doğru kaydırıyoruz
        if (distanceToTarget.magnitude > deadzoneRadius)
        {
            Vector3 targetCenterPos = target.position - (distanceToTarget.normalized * deadzoneRadius);
            // Yavaşça yumuşatılmış geçiş (Lerp) ile kamera merkezini kaydır
            cameraTargetCenter = Vector3.Lerp(cameraTargetCenter, targetCenterPos, cameraFollowSmooth * Time.deltaTime);
        }

        // 2. MOUSE GİRDİLERİ
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        if (mouseX != 0 || mouseY != 0)
        {
            x += mouseX * xSpeed * 0.02f;
            y -= mouseY * ySpeed * 0.02f;
            y = ClampAngle(y, yMinLimit, yMaxLimit);

            lastMouseInputTime = Time.time;
        }
        else
        {
            // Oyuncu hareket ediyorsa (W-A-S-D) ve fareyi bıraktıysa
            float playerSpeed = new Vector3(inputX, 0, inputY).magnitude;
            
            // KRİTİK DEĞİŞİKLİK: inputY >= 0 koşulu eklendi. (S tuşuna basılıyorsa inputY negatif olur, yani < 0)
            // Böylece sadece ileri veya yanlara giderken otomatik arkaya geçecek, S ile geri giderken geçmeyecek.
            if (playerSpeed > 0.1f && inputY >= 0f && Time.time - lastMouseInputTime > idleAutoFollowDelay)
            {
                float targetRotationY = target.eulerAngles.y;
                x = Mathf.LerpAngle(x, targetRotationY, autoFollowSpeed * Time.deltaTime);
            }
        }

        // 3. POZİSYON VE ROTASYON UYGULAMA
        Quaternion rotation = Quaternion.Euler(y, x, 0);

        // Artik "target.position" yerine ölü bölge ile hesaplanmış "cameraTargetCenter"ı baz alıyoruz
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + (cameraTargetCenter + Vector3.up * height);

        transform.rotation = rotation;
        transform.position = position;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    // Editörde ölü bölge alanını görebilmek için gizmo çizdirelim
    private void OnDrawGizmosSelected()
    {
        if (target)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cameraTargetCenter, deadzoneRadius);
        }
    }
}