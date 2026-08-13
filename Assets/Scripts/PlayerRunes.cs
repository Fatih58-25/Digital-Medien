using UnityEngine;
using TMPro; 
using UnityEngine.UI; 

public class PlayerRunes : MonoBehaviour
{
    [Header("Rün Ayarları")]
    [SerializeField] private int currentRunes = 0;
    [SerializeField] private GameObject droppedRunesPrefab; 

    [Header("UI Text Bağlantısı")]
    [SerializeField] private TextMeshProUGUI runeTextTMP;
    [SerializeField] private Text runeTextLegacy;

    private PlayerHealth playerHealth;
    private static GameObject activeDroppedRuneInstance; 

    public int CurrentRunes => currentRunes;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        UpdateUI();

        // 🟢 OYUNCU DOĞDUĞUNDA: Eğer yerde doğmayı bekleyen gizli bir rün varsa görünür yap!
        if (activeDroppedRuneInstance != null)
        {
            activeDroppedRuneInstance.SetActive(true);
        }
    }

    private void Update()
    {
        // 🧪 TEST: 'K' tuşuna basınca 500 Rün ekler
        if (Input.GetKeyDown(KeyCode.K))
        {
            AddRunes(500);
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnPlayerDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnPlayerDied -= HandleDeath;
    }

    public void AddRunes(int amount)
    {
        currentRunes += amount;
        Debug.Log("Rün Eklendi! Yeni Miktar: " + currentRunes);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (runeTextTMP != null)
        {
            runeTextTMP.text = currentRunes.ToString();
        }
        
        if (runeTextLegacy != null)
        {
            runeTextLegacy.text = currentRunes.ToString();
        }
    }

    private void HandleDeath()
    {
        if (currentRunes <= 0 && activeDroppedRuneInstance == null) return;

        // Eski toplanmamış rün varsa sil
        if (activeDroppedRuneInstance != null)
        {
            Destroy(activeDroppedRuneInstance);
            activeDroppedRuneInstance = null;
        }

        if (currentRunes > 0 && droppedRunesPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            
            // Rünü öldüğün konumda oluştur
            activeDroppedRuneInstance = Instantiate(droppedRunesPrefab, spawnPosition, Quaternion.identity);
            
            // 🔴 İŞTE SİHİRLİ DOKUNUŞ: Ölüm anında rünü GİZLE! (Kamerayı kapatmasın)
            activeDroppedRuneInstance.SetActive(false);

            DontDestroyOnLoad(activeDroppedRuneInstance);

            DroppedRunes runeScript = activeDroppedRuneInstance.GetComponent<DroppedRunes>();
            if (runeScript != null)
            {
                runeScript.runeAmount = currentRunes;
            }

            currentRunes = 0;
            UpdateUI();
        }
    }
}