using UnityEngine;
using UnityEngine.UI; // UI elementlerini kontrol etmek için eklendi

public class SoulsCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target; 

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
    public string enemyTag = "Enemy";     
    public float maxLockOnDistance = 15f; 
    public KeyCode lockOnKey = KeyCode.Q; 
    public RectTransform lockOnUI;        // Buraya oluşturduğun UI Image'ı sürükleyeceksin
    public float lockOnHeightOffset = 1.2f; // Noktanın düşmanın neresinde duracağı (1.2f göğüs hizasıdır)

    private float x = 0.0f;
    private float y = 0.0f;
    private float lastMouseInputTime;
    private Vector3 cameraTargetCenter; 

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

        // Başlangıçta UI açıksa kapatalım
        if (lockOnUI != null) lockOnUI.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(lockOnKey))
        {
            if (isLockedOn) UnlockTarget();
            else FindBestTarget();
        }

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

        // 1. ÖLÜ BÖLGE HESAPLAMASI
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
            Vector3 dirToEnemy = lockedTarget.position - cameraTargetCenter;
            dirToEnemy.y = 0; // Dikey bükülmeyi önleme hilesi

            if (dirToEnemy != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dirToEnemy);
                x = Mathf.LerpAngle(x, lookRot.eulerAngles.y, 10f * Time.deltaTime);
                y = Mathf.LerpAngle(y, 15f, 10f * Time.deltaTime); 
                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }

            lastMouseInputTime = Time.time;

            // --- UI NOKTASINI DÜŞMANIN ÜZERİNE YAPIŞTIRMA MOTORU ---
           // --- UI NOKTASINI DÜŞMANIN ÜZERİNE YAPIŞTIRMA & TİTREMESİZ ÖLÇEKLENDİRME ---
if (lockOnUI != null)
{
    // 1. TİTREMESİZ POZİSYONLAMA (SmoothDamp Motoru)
    Vector3 worldTargetPos = lockedTarget.position + Vector3.up * lockOnHeightOffset;
    Vector3 screenPos = Camera.main.WorldToScreenPoint(worldTargetPos);

    // Titremeyi engellemek için anlık pozisyonu yumuşatarak geçiriyoruz
    // (Bunun için sınıfın en üstüne değişken eklemek yerine geçici bir velocity kullanıyoruz)
    Vector3 currentVelocity = Vector3.zero;
    // 0.03f degeri takibin ne kadar yumuşak olacağını belirler (Sıfıra yaklaştıkça sertleşir, büyüdükçe yağ gibi akar)
    lockOnUI.position = Vector3.SmoothDamp(lockOnUI.position, screenPos, ref currentVelocity, 0.02f);


    // 2. ULTRA YUMUŞATILMIŞ BOYUTLANDIRMA (SmoothStep & Törpülenmiş Sınırlar)
    float currentDist = Vector3.Distance(target.position, lockedTarget.position);

    // Mesafe sınırlarını genişletiyoruz ki boyut değişimi çok daha geniş bir alana yayılsın, ani olmasın
    float minDistLimit = 2.0f;   
    float maxDistLimit = 15.0f;  

    float distFactor = Mathf.InverseLerp(minDistLimit, maxDistLimit, currentDist);
    
    // SmoothStep kullanarak lineer geçişi eğrisel (S-Curve) yapıyoruz. 
    // Bu sayede orta mesafelerde büyüklük neredeyse sabit kalacak, ani adımlarda zıplama yapmayacak.
    distFactor = Mathf.SmoothStep(0f, 1f, distFactor);

    // Boyut sınırlarını iyice birbirine yaklaştırdık (Çok az büyüsün, çok az küçülsün)
    float maxScale = 1.05f; // Dibine girince en fazla orijinalin %105'i
    float minScale = 0.75f; // En uzağa gidince en az orijinalin %75'i

    float smoothScale = Mathf.Lerp(maxScale, minScale, distFactor);

    lockOnUI.localScale = new Vector3(smoothScale, smoothScale, 1f);
}
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

    private void FindBestTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform bestTarget = null;
        float closestToCenter = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(target.position, enemy.transform.position);
            if (dist > maxLockOnDistance) continue;

            Vector3 screenPos = Camera.main.WorldToViewportPoint(enemy.transform.position);
            if (screenPos.z < 0) continue;

            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            Vector2 enemyPos2D = new Vector2(screenPos.x, screenPos.y);
            float distanceFromCenter = Vector2.Distance(screenCenter, enemyPos2D);

            if (distanceFromCenter < closestToCenter)
            {
                closestToCenter = distanceFromCenter;
                bestTarget = enemy.transform;
            }
        }

        if (bestTarget != null)
        {
            lockedTarget = bestTarget;
            isLockedOn = true;
            
            // Düşmanı bulunca UI noktasını aktif et
            if (lockOnUI != null) lockOnUI.gameObject.SetActive(true);
        }
    }

    private void UnlockTarget()
    {
        lockedTarget = null;
        isLockedOn = false;
        
        // Kilit açılınca UI noktasını gizle
        if (lockOnUI != null) lockOnUI.gameObject.SetActive(false);
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