using UnityEngine;
using UnityEngine.UI;

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
    public RectTransform lockOnUI;        
    public float lockOnHeightOffset = 1.2f; 

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
            Vector3 dirToEnemy = lockedTarget.position - cameraTargetCenter;
            dirToEnemy.y = 0; 

            if (dirToEnemy != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dirToEnemy);
                x = Mathf.LerpAngle(x, lookRot.eulerAngles.y, 10f * Time.deltaTime);
                y = Mathf.LerpAngle(y, 15f, 10f * Time.deltaTime); 
                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }

            lastMouseInputTime = Time.time;

            // --- UI TAKİP VE TİTREMESİZ ÖLÇEKLENDİRME ---
            if (lockOnUI != null)
            {
                Vector3 worldTargetPos = lockedTarget.position + Vector3.up * lockOnHeightOffset;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldTargetPos);

                Vector3 currentVelocity = Vector3.zero;
                lockOnUI.position = Vector3.SmoothDamp(lockOnUI.position, screenPos, ref currentVelocity, 0.02f);

                float currentDist = Vector3.Distance(target.position, lockedTarget.position);
                float minDistLimit = 2.0f;   
                float maxDistLimit = 15.0f;  

                float distFactor = Mathf.InverseLerp(minDistLimit, maxDistLimit, currentDist);
                distFactor = Mathf.SmoothStep(0f, 1f, distFactor);

                float maxScale = 1.05f; 
                float minScale = 0.75f; 

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
        
        // En düşük skor en iyi hedef olacağı için başlangıcı sonsuz yapıyoruz
        float bestScore = Mathf.Infinity; 

        foreach (GameObject enemy in enemies)
        {
            // 1. Fiziksel Mesafe Kontrolü
            float physicalDist = Vector3.Distance(target.position, enemy.transform.position);
            if (physicalDist > maxLockOnDistance) continue;

            // 2. Ekran Sınırları Kontrolü (Kameranın arkasındaysa pas geç)
            Vector3 screenPos = Camera.main.WorldToViewportPoint(enemy.transform.position);
            if (screenPos.z < 0) continue;

            // 3. Ekran Ortasına Uzaklık Hesabı
            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            Vector2 enemyPos2D = new Vector2(screenPos.x, screenPos.y);
            float distanceFromScreenCenter = Vector2.Distance(screenCenter, enemyPos2D);

            // Ekran dışında kalanları eliyoruz (0 ile 1 arası viewport koordinatları)
            if (screenPos.x < 0 || screenPos.x > 1 || screenPos.y < 0 || screenPos.y > 1) continue;

            // --- AKILLI SOULS PUANLAMA MOTORU ---
            // İki değeri de adil kıyaslamak için 0-1 aralığına normalize ediyoruz.
            float distanceScore = physicalDist / maxLockOnDistance; // Yakın olanın skoru sıfıra yaklaşır (avantaj)
            float screenScore = distanceFromScreenCenter * 2f;      // Tam ortada olanın skoru sıfır olur (avantaj)

            // AĞIRLIK FORMÜLÜ: Yakınlığa %60, Ekran ortalamasına %40 önem veriyoruz.
            // Bu oranları oyun testlerine göre (Örn: 0.5f'e 0.5f) değiştirebilirsin aga.
            float finalScore = (distanceScore * 0.6f) + (screenScore * 0.4f);

            // En düşük skora sahip olan (En ideal kombinasyon) düşmanı seçiyoruz
            if (finalScore < bestScore)
            {
                bestScore = finalScore;
                bestTarget = enemy.transform;
            }
        }

        if (bestTarget != null)
        {
            lockedTarget = bestTarget;
            isLockedOn = true;
            if (lockOnUI != null) lockOnUI.gameObject.SetActive(true);
        }
    }

    private void UnlockTarget()
    {
        lockedTarget = null;
        isLockedOn = false;
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