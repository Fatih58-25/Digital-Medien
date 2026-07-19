using UnityEngine;

public class SoulsCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target; // Karakter (Player)

    [Header("Distance Settings")]
    public float distance = 4.0f; 
    public float height = 2.0f;   

    [Header("Rotation Settings")]
    public float xSpeed = 120.0f; 
    public float ySpeed = 120.0f; 
    public float yMinLimit = -10f; 
    public float yMaxLimit = 60f;  

    [Header("Deadzone (Ölü Bölge) Settings")]
    public float deadzoneRadius = 1.5f; 
    public float cameraFollowSmooth = 5.0f;

    [Header("Souls-like Auto Follow")]
    public float autoFollowSpeed = 2.0f; 
    public float idleAutoFollowDelay = 1.0f; 

    [Header("Souls Lock-On Settings")]
    public string enemyTag = "Enemy";     // Düşman objelerinin Tag'i neyse buraya yaz (Örn: Enemy)
    public float maxLockOnDistance = 15f; // En fazla kaç metreden kilitlenebilsin
    public KeyCode lockOnKey = KeyCode.Q; // Kilitlenme tuşu

    private float x = 0.0f;
    private float y = 0.0f;
    private float lastMouseInputTime;
    private Vector3 cameraTargetCenter; 

    // Kilitlenme Değişkenleri
    private Transform lockedTarget = null;
    private bool isLockedOn = false;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target)
        {
            cameraTargetCenter = target.position;
        }
    }

    void Update()
    {
        // Q tuşuna basıldığında kilidi aç veya en yakın düşmanı ara
        if (Input.GetKeyDown(lockOnKey))
        {
            if (isLockedOn)
            {
                UnlockTarget();
            }
            else
            {
                FindBestTarget();
            }
        }

        // Eğer kilitliysek ama düşman çok uzaklaştıysa veya öldüyse (yok olduysa) kilidi otomatik kaldır
        if (isLockedOn)
        {
            if (lockedTarget == null || Vector3.Distance(target.position, lockedTarget.position) > maxLockOnDistance)
            {
                UnlockTarget();
            }
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1. ÖLÜ BÖLGE (DEADZONE) HESAPLAMASI
        Vector3 distanceToTarget = target.position - cameraTargetCenter;
        
        if (distanceToTarget.magnitude > deadzoneRadius)
        {
            Vector3 targetCenterPos = target.position - (distanceToTarget.normalized * deadzoneRadius);
            cameraTargetCenter = Vector3.Lerp(cameraTargetCenter, targetCenterPos, cameraFollowSmooth * Time.deltaTime);
        }

        // 2. ROTASYON HESAPLAMALARI
        if (isLockedOn && lockedTarget != null)
        {
            // --- KİLİTLENME AKTİFKEN ROTASYON ---
            // Kameranın hayali merkezinden düşmana doğru olan yönü bul
         // --- KİLİTLENME AKTİFKEN ROTASYON (Souls Düzleştirilmiş Bakış) ---
// Kameranın hayali merkezinden düşmana doğru olan yönü bul
Vector3 dirToEnemy = lockedTarget.position - cameraTargetCenter;

// Hile Burada: Düşman uzun boylu olsa bile kameranın yukarı bakmasını engellemek için
// Y eksenindeki yükseklik farkını sıfırlıyoruz. Kamera hep düz bir hatta kilitleniyor.
dirToEnemy.y = 0; 

if (dirToEnemy != Vector3.zero)
{
    // Bu düzleştirilmiş yöne bakacak rotasyonu hesapla
    Quaternion lookRot = Quaternion.LookRotation(dirToEnemy);
    
    // Mevcut X ve Y açılarını yumuşakça eşitle
    x = Mathf.LerpAngle(x, lookRot.eulerAngles.y, 10f * Time.deltaTime);
    
    // Y eksenini (yukarı-aşağı eğimi) tamamen düşmana bırakmıyoruz, 
    // sabit bir açıda tutuyoruz (Örn: 15 derece aşağı doğru baksın ki yukardan baksın)
    y = Mathf.LerpAngle(y, 15f, 10f * Time.deltaTime); 
    y = ClampAngle(y, yMinLimit, yMaxLimit);
}

lastMouseInputTime = Time.time;
        }
        else
        {
            // --- NORMAL MANUEL/OTOMATİK KAMERA ROTASYONU ---
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
                float playerSpeed = new Vector3(inputX, 0, inputY).magnitude;
                
                if (playerSpeed > 0.1f && inputY >= 0f && Time.time - lastMouseInputTime > idleAutoFollowDelay)
                {
                    float targetRotationY = target.eulerAngles.y;
                    x = Mathf.LerpAngle(x, targetRotationY, autoFollowSpeed * Time.deltaTime);
                }
            }
        }

        // 3. POZİSYON VE ROTASYON UYGULAMA
        Quaternion rotation = Quaternion.Euler(y, x, 0);

        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + (cameraTargetCenter + Vector3.up * height);

        transform.rotation = rotation;
        transform.position = position;
    }

    // --- AKILLI HEDEF BULMA MOTORU ---
    private void FindBestTarget()
    {
        // Sahnedeki tüm düşmanları bul
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform bestTarget = null;
        float closestToCenter = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(target.position, enemy.transform.position);
            
            // Eğer düşman kilitlenme menzili dışındaysa pas geç
            if (dist > maxLockOnDistance) continue;

            // Düşmanın ekrandaki pozisyonunu bul (Kamera ortasına yakınlık testi için)
            Vector3 screenPos = Camera.main.WorldToViewportPoint(enemy.transform.position);
            
            // Düşman kameranın arkasındaysa pas geç (screenPos.z ekrana olan uzaklıktır, eksi olamaz)
            if (screenPos.z < 0) continue;

            // Ekranın tam ortası (0.5, 0.5) noktasıdır. Düşmanın ortayap olan uzaklığını hesapla
            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            Vector2 enemyPos2D = new Vector2(screenPos.x, screenPos.y);
            float distanceFromCenter = Vector2.Distance(screenCenter, enemyPos2D);

            // Ekranın ortasına en yakın olan düşmanı seç
            if (distanceFromCenter < closestToCenter)
            {
                closestToCenter = distanceFromCenter;
                bestTarget = enemy.transform;
            }
        }

        // Eğer uygun bir düşman bulunduysa kilitle
        if (bestTarget != null)
        {
            lockedTarget = bestTarget;
            isLockedOn = true;
        }
    }

    private void UnlockTarget()
    {
        lockedTarget = null;
        isLockedOn = false;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    private void OnDrawGizmosSelected()
    {
        if (target)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cameraTargetCenter, deadzoneRadius);
        }
    }
}