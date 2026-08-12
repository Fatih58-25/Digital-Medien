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
    
    [Header("Custom Lock Point Settings")]
    [Tooltip("Düşmanın alt nesnelerinde bu ismi barındıran bir obje arar (örn: LockPoint, Chest, Target). Bulamazsa varsayılan offset'i kullanır.")]
    public string customLockPointName = "Lock"; 

    private float x = 0.0f;
    private float y = 0.0f;
    private float lastMouseInputTime;
    private Vector3 cameraTargetCenter; 

    private Transform lockedTarget = null;     // Ana Düşman Transform'u
    private Transform actualLockPoint = null; // Kilitlenilecek Alt Nesne Transform'u
    private bool isLockedOn = false;

    // 🟢 Dışarıdan kilit durumunu okumak için Getter'lar
    public bool IsLockedOn => isLockedOn;
    public Transform LockedTarget => lockedTarget;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

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
            // Eğer özel kilit noktası (çocuk obje) bulunduysa onun pozisyonunu, yoksa height offset eklenmiş ana pozisyonu al
            Vector3 aimPosition = actualLockPoint != null ? actualLockPoint.position : (lockedTarget.position + Vector3.up * lockOnHeightOffset);

            Vector3 dirToEnemy = aimPosition - cameraTargetCenter;
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
                Vector3 worldTargetPos = aimPosition;
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
        Transform bestLockPoint = null;
        float bestScore = Mathf.Infinity; 

        foreach (GameObject enemy in enemies)
        {
            // Alt nesnelerde "customLockPointName" (Varsayılan: "Lock") adında bir çocuk arar
            Transform childLockPoint = FindChildLockPoint(enemy.transform);
            Transform aimTransform = (childLockPoint != null) ? childLockPoint : enemy.transform;

            float physicalDist = Vector3.Distance(target.position, aimTransform.position);
            if (physicalDist > maxLockOnDistance) continue;

            Vector3 screenPos = Camera.main.WorldToViewportPoint(aimTransform.position);
            if (screenPos.z < 0) continue;

            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            Vector2 enemyPos2D = new Vector2(screenPos.x, screenPos.y);
            float distanceFromScreenCenter = Vector2.Distance(screenCenter, enemyPos2D);

            if (screenPos.x < 0 || screenPos.x > 1 || screenPos.y < 0 || screenPos.y > 1) continue;

            float distanceScore = physicalDist / maxLockOnDistance;
            float screenScore = distanceFromScreenCenter * 2f;      

            float finalScore = (distanceScore * 0.6f) + (screenScore * 0.4f);

            if (finalScore < bestScore)
            {
                bestScore = finalScore;
                bestTarget = enemy.transform;
                bestLockPoint = childLockPoint; // Bulunan alt nesneyi kaydet (bulamadıysa null kalır)
            }
        }

        if (bestTarget != null)
        {
            lockedTarget = bestTarget;
            actualLockPoint = bestLockPoint;
            isLockedOn = true;
            if (lockOnUI != null) lockOnUI.gameObject.SetActive(true);
        }
    }

    // Düşmanın tüm alt çocuklarında isminde "customLockPointName" kelimesi geçen objeyi bulur
    private Transform FindChildLockPoint(Transform parent)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child != parent && child.name.ToLower().Contains(customLockPointName.ToLower()))
            {
                return child;
            }
        }
        return null;
    }

    private void UnlockTarget()
    {
        lockedTarget = null;
        actualLockPoint = null;
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