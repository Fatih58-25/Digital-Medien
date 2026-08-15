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
        if (runeTextTMP != null) runeTextTMP.text = currentRunes.ToString();
        if (runeTextLegacy != null) runeTextLegacy.text = currentRunes.ToString();
    }

    private void HandleDeath()
    {
        if (currentRunes <= 0 && activeDroppedRuneInstance == null) return;

        // Üst üste 2 kez ölünce eski rün silinir (Souls kuralı)
        if (activeDroppedRuneInstance != null)
        {
            Destroy(activeDroppedRuneInstance);
            activeDroppedRuneInstance = null;
        }

        if (currentRunes > 0 && droppedRunesPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            
            // 🔴 Rünü öldüğün yerde GİZLİ (KAPALI) olarak oluşturuyoruz!
            activeDroppedRuneInstance = Instantiate(droppedRunesPrefab, spawnPosition, Quaternion.identity);
            activeDroppedRuneInstance.SetActive(false); 

            DroppedRunes runeScript = activeDroppedRuneInstance.GetComponent<DroppedRunes>();
            if (runeScript != null)
            {
                runeScript.runeAmount = currentRunes;
            }

            currentRunes = 0;
            UpdateUI();
        }
    }

    // 🟢 Sadece Bonfire'da doğunca GameManager tarafından çağrılacak!
    public void RevealDroppedRunes()
    {
        if (activeDroppedRuneInstance != null)
        {
            activeDroppedRuneInstance.SetActive(true);
            Debug.Log("✨ Yerdeki rünler görünür kılındı!");
        }
    }
    // Bunu seviye atladığımızda BonfireUIManager çağıracak
    public void SpendRunes(int amount)
    {
        if (currentRunes >= amount)
        {
            currentRunes -= amount;
            UpdateUI();
        }
    }
}