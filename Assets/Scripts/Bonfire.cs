using UnityEngine;

public class Bonfire : MonoBehaviour
{
    [Header("Bonfire Bilgileri (YENİ)")]
    public string bonfireName = "İsimsiz Bonfire"; // Fast Travel menüsünde yazacak isim
    public bool isUnlocked = false;                // Oyuncu burayı yaktı mı/keşfetti mi?

    [Header("Bonfire Görsel Ayarları")]
    [SerializeField] private GameObject unlitSword;
    [SerializeField] private GameObject litSword;
    [SerializeField] private GameObject fireParticle;

    [Header("Kamera & Pozisyon")]
    [SerializeField] private Transform sitPoint;
    [SerializeField] private float maxInteractionDistance = 3.5f;

    private bool isLit = false;
    private bool isPlayerInside = false;
    private bool isResting = false;

    private GameObject playerObj;
    private PlayerHealth playerHealth;
    private PlayerFlaskSystem playerFlaskSystem;
    private CharacterController playerController;
    private Animator playerAnimator;
    
    private BonfireUIManager uiManager; // 🟢 YENİ: Menü Yöneticisi

    private void Start()
    {
        UpdateBonfireVisuals();
        
        // Sahnede UI Manager varsa bul (Canvas'a attığımız script)
        uiManager = FindObjectOfType<BonfireUIManager>();
        
        // Başlangıçta yanıyorsa otomatik açılmış say
        if (isLit) isUnlocked = true; 
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
                Debug.Log("[E] - " + bonfireName + " Yak (Kindle)");
            else
                Debug.Log("[E] - " + bonfireName + " Dinlen");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ClearPlayerReference();
            
            // Eğer oyuncu alandan çıkarsa (bug vs durumu) UI'ı zorla kapat
            if (uiManager != null) uiManager.CloseAllPanels();
        }
    }

    private void Update()
    {
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
            // 🟢 YENİ: Artık E'ye tekrar basarak kalkmayı kaldırdım, çünkü UI'dan "Ayrıl" diyerek kalkmalı.
        }
    }

    private void KindleBonfire()
    {
        isLit = true;
        isUnlocked = true; // 🟢 YENİ: Fast Travel listesine eklenebilir hale geldi
        UpdateBonfireVisuals();
        Debug.Log("🔥 " + bonfireName + " Yakıldı!");
    }

    private void StartResting()
    {
        if (playerObj == null) return;

        isResting = true;

        if (GameManager.Instance != null)
        {
            Vector3 respawnPos = sitPoint != null ? sitPoint.position : transform.position;
            GameManager.Instance.lastCheckpointPosition = respawnPos;
        }

        if (playerController != null) playerController.enabled = false;

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

        RestorePlayer();
        
        // 🟢 YENİ: UI MENÜYÜ AÇ!
        if (uiManager != null)
        {
            uiManager.activeBonfire = this; // UI Manager'a "Şu an bende oturuyor" bilgisini ver
            uiManager.OpenBonfireMenu();
        }

        Debug.Log("💤 " + bonfireName + " dinleniliyor. Düşmanlar resetlendi!");
    }

    // 🟢 YENİ: Bu metodu "public" yaptık ki UI Manager "Ayrıl" butonuna basınca bunu çağırabilsin
    public void StopResting()
    {
        isResting = false;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", false);
            playerAnimator.SetTrigger("StandUpTrigger");
        }
        
        // Menüleri kapat ve fareyi gizle
        if (uiManager != null)
        {
            uiManager.CloseAllPanels();
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
        if (playerHealth != null) playerHealth.RestoreFullHealth(); 
        if (playerFlaskSystem != null) playerFlaskSystem.RefillFlasks(); 

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
    // 🟢 YENİ: Başka bir bonfire'a ışınlandığımızda eskisinden ayağa kalkma animasyonu 
    // oynatmadan (sessizce) ayrılmamızı sağlar.
    public void SilentLeave()
    {
        isResting = false;
        ClearPlayerReference();
    }

    // 🟢 YENİ: Işınlandığımız yeni Bonfire'a "Ben geldim, beni oturt" mesajı yollar.
    public void FastTravelArrival(GameObject player)
    {
        // Oyuncu referanslarını yeni Bonfire'a tanıtıyoruz (çünkü içine yürüyerek girmedik)
        playerObj = player;
        playerHealth = playerObj.GetComponent<PlayerHealth>();
        playerFlaskSystem = playerObj.GetComponent<PlayerFlaskSystem>();
        playerController = playerObj.GetComponent<CharacterController>();
        playerAnimator = playerObj.GetComponentInChildren<Animator>();
        
        isPlayerInside = true;

        // Direkt oturma, can fulleme ve menüyü açma döngüsünü başlat!
        StartResting(); 
    }
}