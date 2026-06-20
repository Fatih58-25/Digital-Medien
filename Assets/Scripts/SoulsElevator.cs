using System.Collections;
using UnityEngine;

public class SoulsElevator : MonoBehaviour
{
    [Header("Asansör Tipi")]
    [SerializeField] private bool startFromTopAndGoDown = false; // EĞER ASANSÖR YUKARIDAN BAŞLAYIP AŞAĞI İNECEKSE BUNU TİKLE AGA!

    [Header("Asansör Ayarlari")]
    [SerializeField] private float moveDistance = 10f; // Buraya her zaman POZİTİF (normal) sayı yaz kankam
    [SerializeField] private float moveSpeed = 3f; 
    [SerializeField] private float resetCooldown = 2f; 

    [Header("Tetikleyici Nesne")]
    [SerializeField] private GameObject triggerButton; 

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isAtTarget = false;
    private bool isCooldown = false;

    private float lerpTime = 0f;

    private void Start()
    {
        startPosition = transform.position;

        // Şaltere göre hedef pozisyonunu otomatik belirliyoruz, eksi yazmana gerek kalmadı!
        if (startFromTopAndGoDown)
        {
            targetPosition = startPosition + (Vector3.down * moveDistance); // Aşağı yönlü
        }
        else
        {
            targetPosition = startPosition + (Vector3.up * moveDistance); // Yukarı yönlü
        }

        if (triggerButton == null)
        {
            Debug.LogError("Aga 'Trigger Button' slotuna o silindir nesnesini sürüklemeyi unuttun amk!");
        }
    }

    private void Update()
    {
        if (isMoving)
        {
            Vector3 start = isAtTarget ? targetPosition : startPosition;
            Vector3 target = isAtTarget ? startPosition : targetPosition;

            lerpTime += Time.deltaTime * (moveSpeed / moveDistance);
            
            // Pürüzsüz Lerp hareketi
            transform.position = Vector3.Lerp(start, target, lerpTime);

            if (lerpTime >= 1f)
            {
                transform.position = target;
                isMoving = false;
                isAtTarget = !isAtTarget;
                lerpTime = 0f; 
            }
        }
    }

    public void TryActivateElevator()
    {
        if (isMoving || isCooldown) return;
        lerpTime = 0f;
        isMoving = true;
    }

    public void StartCooldownProcess()
    {
        if (!isCooldown)
        {
            StartCoroutine(CooldownRoutine());
        }
    }

    private IEnumerator CooldownRoutine()
    {
        isCooldown = true;
        yield return new WaitForSeconds(resetCooldown);
        isCooldown = false;
    }
}