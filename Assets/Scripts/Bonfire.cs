using UnityEngine;

public class Bonfire : MonoBehaviour
{
    [Header("Bonfire Görsel Ayarları")]
    [SerializeField] private GameObject unlitSword;   // Alevsiz kılıç nesnesi
    [SerializeField] private GameObject litSword;     // Alevli kılıç nesnesi
    [SerializeField] private GameObject fireParticle; // Varsa alev/ışık efekti

    [Header("Kamera & Pozisyon")]
    [SerializeField] private Transform sitPoint;      // Oyuncunun oturacağı tam nokta
    [SerializeField] private float maxInteractionDistance = 3.5f; // Emniyet mesafesi

    private bool isLit = false;          // Bonfire yakıldı mı?
    private bool isPlayerInside = false; // Oyuncu alan içinde mi?
    private bool isResting = false;      // Şu an oturuyor mu?

    private GameObject playerObj;
    private PlayerHealth playerHealth;
    private PlayerFlaskSystem playerFlaskSystem;
    private CharacterController playerController;
    private Animator playerAnimator;

    private void Start()
    {
        UpdateBonfireVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            playerObj = other.gameObject;
            
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerFlaskSystem = playerObj.GetComponent<PlayerFlaskSystem>();
            playerController = playerObj.GetComponent<CharacterController>();
            playerAnimator = playerObj.GetComponentInChildren<Animator>();

            if (!isLit)
                Debug.Log("[E] - Bonfire'ı Yak (Kindle)");
            else
                Debug.Log("[E] - Bonfire'da Dinlen");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ClearPlayerReference();
        }
    }

    private void Update()
    {
        // Mesafe emniyet kontrolü
        if (!isResting && playerObj != null)
        {
            float distance = Vector3.Distance(transform.position, playerObj.transform.position);
            if (distance > maxInteractionDistance)
            {
                ClearPlayerReference();
                return;
            }
        }

        if (!isPlayerInside && !isResting) return;

        // E TUŞUNA BASILDIĞINDA
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isLit)
            {
                KindleBonfire();
            }
            else if (!isResting)
            {
                StartResting();
            }
            else if (isResting)
            {
                StopResting();
            }
        }
    }

    private void KindleBonfire()
    {
        isLit = true;
        UpdateBonfireVisuals();
        Debug.Log("🔥 Bonfire Yakıldı!");
    }

    private void StartResting()
    {
        if (playerObj == null) return;

        isResting = true;

        // Checkpoint Güncelleme
        if (GameManager.Instance != null)
        {
            Vector3 respawnPos = sitPoint != null ? sitPoint.position : transform.position;
            GameManager.Instance.lastCheckpointPosition = respawnPos;
        }

        // Karakter Hareketini Kapat
        if (playerController != null) playerController.enabled = false;

        // Oturma Pozisyonuna Al
        if (sitPoint != null)
        {
            playerObj.transform.position = sitPoint.position;
            Vector3 lookTarget = new Vector3(transform.position.x, playerObj.transform.position.y, transform.position.z);
            playerObj.transform.LookAt(lookTarget);
            Physics.SyncTransforms();
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", true);
        }

        // ❤️ Can, İksir Doldur ve Peşindeki Düşmanları İlk Yerlerine Işınlayıp Agrolarını Sıfırla
        RestorePlayer();

        Debug.Log("💤 Bonfire'da dinleniliyor. Düşmanlar resetlendi!");
    }

    private void StopResting()
    {
        isResting = false;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", false);
            playerAnimator.SetTrigger("StandUpTrigger");
        }

        Invoke(nameof(EnablePlayerControl), 0.8f);

        Debug.Log("⚔️ Bonfire'dan kalkıldı!");
    }

    private void EnablePlayerControl()
    {
        if (playerController != null) playerController.enabled = true;
    }

    private void RestorePlayer()
    {
        if (playerHealth != null)
        {
            playerHealth.RestoreFullHealth(); 
        }

        if (playerFlaskSystem != null)
        {
            playerFlaskSystem.RefillFlasks(); 
        }

        // 🟢 Sahnedeki tüm düşmanları Resetle (Kovalayanlar kendi spawn noktalarına ışınlanır)
        EnemyBase[] allEnemies = FindObjectsOfType<EnemyBase>(true);
        foreach (EnemyBase enemy in allEnemies)
        {
            enemy.RespawnEnemy();
        }
    }

    private void ClearPlayerReference()
    {
        isPlayerInside = false;
        playerObj = null;
        playerHealth = null;
        playerFlaskSystem = null;
        playerController = null;
        playerAnimator = null;
    }

    private void UpdateBonfireVisuals()
    {
        if (unlitSword != null) unlitSword.SetActive(!isLit);
        if (litSword != null) litSword.SetActive(isLit);
        if (fireParticle != null) fireParticle.SetActive(isLit);
    }
}