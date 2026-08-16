using UnityEngine;
using System.Collections;

public class DoubleDoor : MonoBehaviour
{
    [Header("Kapı Parçaları (Pivot Objeleri)")]
    [SerializeField] private Transform leftDoorPivot;
    [SerializeField] private Transform rightDoorPivot;

    [Header("Kilit Sistemi")]
    [SerializeField] private bool requiresKey = false;

    [Header("Açı ve Hız Ayarları")]
    [SerializeField] private float openAngle = 90f;   // Dönüş açısı
    [SerializeField] private float openSpeed = 1.2f;  // Kapının açılma süresi/hızı
    [SerializeField] private float startDelay = 0.35f; // E'ye bastıktan sonraki Souls ağırlık gecikmesi

    [Header("Açılma Hız Eğrisi (Frenleme Hissi)")]
    // Varsayılan olarak başta normal başlayıp sonda yavaşlayan yumuşak eğri (Ease Out)
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Açılma Yönü (Genelde Y Eksenidir)")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up; 

    private bool isPlayerNearby = false;
    private bool isOpen = false;

    private void Update()
    {
        if (isPlayerNearby && !isOpen && Input.GetKeyDown(KeyCode.E))
        {
            // 🟢 EĞER KAPI KİLİTLİYSE VE ANAHTAR YOKSA
            if (requiresKey && UIInteractionManager.Instance != null && !UIInteractionManager.Instance.hasBossKey)
            {
                UIInteractionManager.Instance.ShowLockedMessage(); // "You need a key!" göster
                return; // Kapıyı açma, metottan çık!
            }

            // Kapı kilitli değilse veya anahtarımız varsa normal şekilde aç
            StartCoroutine(OpenDoorRoutine());
        }
    }

    private IEnumerator OpenDoorRoutine()
    {
        isOpen = true;

        // E'ye basıldığı an arayüzdeki E simgesini kaldır
        if (UIInteractionManager.Instance != null)
        {
            UIInteractionManager.Instance.HideInteractionUI();
        }

        // --- 1. SOULS GECİKMESİ (AĞIRLIK HİSSİ) ---
        // O devasa kapıyı itmeye başlama anındaki o küçük duraksama
        yield return new WaitForSeconds(startDelay);

        // --- 2. DÖNÜŞ HESAPLAMALARI ---
        Quaternion leftStartRot = leftDoorPivot.localRotation;
        Quaternion rightStartRot = rightDoorPivot.localRotation;

        Quaternion leftTargetRot = leftStartRot * Quaternion.Euler(rotationAxis * openAngle);
        Quaternion rightTargetRot = rightStartRot * Quaternion.Euler(rotationAxis * -openAngle);

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * openSpeed;

            // --- 3. YUMUŞAK FRENLEME (EASE OUT / SMOOTHSTEP) ---
            // Eğri üzerinden yumuşatılmış t değerini alıyoruz
            float smoothProgress = movementCurve.Evaluate(progress);

            leftDoorPivot.localRotation = Quaternion.Slerp(leftStartRot, leftTargetRot, smoothProgress);
            rightDoorPivot.localRotation = Quaternion.Slerp(rightStartRot, rightTargetRot, smoothProgress);

            yield return null;
        }

        // Açıyı tam oturt
        leftDoorPivot.localRotation = leftTargetRot;
        rightDoorPivot.localRotation = rightTargetRot;

        Debug.Log("Devasa kapı tam ağırlığıyla açıldı!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOpen)
        {
            isPlayerNearby = true;

            if (UIInteractionManager.Instance != null)
            {
                UIInteractionManager.Instance.ShowInteractionUI();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;

            if (UIInteractionManager.Instance != null)
            {
                UIInteractionManager.Instance.HideInteractionUI();
            }
        }
    }
}