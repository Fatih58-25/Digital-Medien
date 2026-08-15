using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BonfireUIManager : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject mainMenuPanel;
    public GameObject levelUpPanel;
    public GameObject fastTravelPanel;
    [HideInInspector] public Bonfire activeBonfire; // Hangi bonfire'da oturuyoruz?

    [Header("Fast Travel Ayarları")]
    public Transform fastTravelContentParent; // 🟢 YENİ: Butonların dizileceği yer (Content)
    public GameObject fastTravelButtonPrefab; // 🟢 YENİ: Kopyalanacak Şablon Buton

    [Header("Sistem Bağlantıları")]
    public PlayerRunes playerRunes; 

    [Header("Mevcut Seviye ve Statlar")]
    public int playerLevel = 1;
    public int vitality = 10;  
    public int endurance = 10; 
    public int strength = 10;  

    // Geçici (Preview) Değişkenler
    private int tempLevel;
    private int tempVitality;
    private int tempEndurance;
    private int tempStrength;
    private int currentCost = 0; 
    private int totalCost = 0;   

    private const int baseRuneCost = 100; 

    [Header("Level Up UI Textleri")]
    public TextMeshProUGUI txtCurrentRunes;
    public TextMeshProUGUI txtRequiredRunes;
    
    public TextMeshProUGUI txtVitCurrent, txtVitPreview;
    public TextMeshProUGUI txtEndCurrent, txtEndPreview;
    public TextMeshProUGUI txtStrCurrent, txtStrPreview;

    private void Start()
    {
        CloseAllPanels();
    }

    // --- PANEL KONTROLLERİ ---
    
    public void OpenBonfireMenu()
    {
        CloseAllPanels();
        mainMenuPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseAllPanels()
    {
        mainMenuPanel.SetActive(false);
        levelUpPanel.SetActive(false);
        fastTravelPanel.SetActive(false);
    }

    public void OpenLevelUpPanel()
    {
        CloseAllPanels();
        levelUpPanel.SetActive(true);
        ResetTempStats(); 
        UpdateLevelUpUI();
    }

    // 🟢 YENİ: Işınlanma Paneli Açıldığında Listeyi Oluşturan Fonksiyon
    public void OpenFastTravelPanel()
    {
        CloseAllPanels();
        fastTravelPanel.SetActive(true);

        // 1. Önce listedeki eski butonları temizle (Şablon hariç)
        foreach (Transform child in fastTravelContentParent)
        {
            if (child.gameObject != fastTravelButtonPrefab)
            {
                Destroy(child.gameObject);
            }
        }

        // 2. Sahnedeki tüm Bonfire'ları bul
        Bonfire[] allBonfires = FindObjectsOfType<Bonfire>();

        // 3. Bulunanları listele
        foreach (Bonfire b in allBonfires)
        {
            if (b.isUnlocked) // Sadece yaktığımız (açılan) bonfire'lar
            {
                // Şablon butonu kopyala ve Content'in içine at
                GameObject newBtn = Instantiate(fastTravelButtonPrefab, fastTravelContentParent);
                newBtn.SetActive(true); // Görünür yap

                // Butonun içindeki yazıyı değiştir
                TextMeshProUGUI btnText = newBtn.GetComponentInChildren<TextMeshProUGUI>();

                // Eğer oyuncu zaten bu bonfire'da oturuyorsa
                if (b == activeBonfire)
                {
                    btnText.text = b.bonfireName + " (you are here)";
                    newBtn.GetComponent<Button>().interactable = false; // Tıklanamaz yap
                }
                else
                {
                    btnText.text = b.bonfireName;
                    
                    // Butona tıklandığında Teleport fonksiyonunu çalıştır ve gideceği Bonfire'ı içine gönder!
                    newBtn.GetComponent<Button>().onClick.AddListener(() => TeleportToBonfire(b));
                }
            }
        }
    }

    public void LeaveBonfire()
    {
        CloseAllPanels();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (activeBonfire != null)
        {
            activeBonfire.StopResting();
        }
    }

    // 🟢 YENİ: Işınlanmayı Gerçekleştiren Fonksiyon
    // 🟢 GÜNCELLENDİ: Kusursuz Işınlanma ve Direkt Oturma Sistemi
    private void TeleportToBonfire(Bonfire targetBonfire)
    {
        // 1. Oyuncuyu bul
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 2. Mevcut (Eski) Bonfire'dan "Sessizce" ayrıl (Ayağa kalkma animasyonu iptal edilir)
        if (activeBonfire != null)
        {
            activeBonfire.SilentLeave();
        }

        // 3. Yeni Bonfire'a oyuncuyu gönder (FastTravelArrival, CharacterController'ı
        // güvenle kapatıp oyuncuyu "sitPoint" noktasına yerleştirecek ve fiziği güncelleyecektir)
        targetBonfire.FastTravelArrival(player);

        Debug.Log("✨ " + targetBonfire.bonfireName + " adlı Bonfire'a ışınlanıldı ve oturuldu!");
        
        // Not: Menüyü kapatmamıza veya fareyi gizlememize gerek kalmadı, 
        // çünkü yeni Bonfire'ın "StartResting" fonksiyonu otomatik olarak ana menüyü tekrar açacak!
    }


    // --- SEVİYE ATLAMA MANTIĞI ---

    private void ResetTempStats()
    {
        tempLevel = playerLevel;
        tempVitality = vitality;
        tempEndurance = endurance;
        tempStrength = strength;
        totalCost = 0;
        CalculateNextLevelCost();
    }

    private void CalculateNextLevelCost()
    {
        currentCost = Mathf.RoundToInt(baseRuneCost * Mathf.Pow(tempLevel, 1.5f));
    }

    private void UpdateLevelUpUI()
    {
        // 🟢 YENİ RENKLERİMİZ (Elden Ring Teması)
        // Rünler için Koyu Altın / Bakır Rengi (184, 134, 11)
        Color32 runeColor = new Color32(184, 134, 11, 255); 
        
        // Stat metinleri için Soluk Kirli Bej/Sarı (210, 180, 140)
        Color32 statDefaultColor = new Color32(210, 180, 140, 255); 
        
        // Artış yapıldığında yanacak renk (Bunu istersen cam göbeği falan yapabilirsin)
        Color32 upgradeColor = Color.green; 
        Color32 whites = Color.white; 

        // RÜN METİNLERİ
        txtCurrentRunes.text = "Runes held: " + playerRunes.CurrentRunes.ToString();
        txtCurrentRunes.color = runeColor; // Mevcut rün yazısını boyadık

        txtRequiredRunes.text = "Runes required: " + (totalCost > 0 ? totalCost.ToString() : currentCost.ToString());
        
        // Rün yetiyorsa Altın rengi, yetmiyorsa Kırmızı
        if (playerRunes.CurrentRunes >= totalCost + currentCost)
            txtRequiredRunes.color = runeColor;
        else
            txtRequiredRunes.color = Color.red;


        // VITALITY
        txtVitCurrent.text = vitality.ToString();
        txtVitCurrent.color = Color.white; // Sol taraftaki mevcut stat rengi
        txtVitPreview.text = tempVitality.ToString();
        txtVitPreview.color = (tempVitality > vitality) ? upgradeColor : whites;

        // ENDURANCE
        txtEndCurrent.text = endurance.ToString();
        txtEndCurrent.color = Color.white;
        txtEndPreview.text = tempEndurance.ToString();
        txtEndPreview.color = (tempEndurance > endurance) ? upgradeColor : whites;

        // STRENGTH
        txtStrCurrent.text = strength.ToString();
        txtStrCurrent.color = Color.white;
        txtStrPreview.text = tempStrength.ToString();
        txtStrPreview.color = (tempStrength > strength) ? upgradeColor : whites;
    }

    public void IncreaseVitality() { TryIncreaseStat(ref tempVitality); }
    public void IncreaseEndurance() { TryIncreaseStat(ref tempEndurance); }
    public void IncreaseStrength() { TryIncreaseStat(ref tempStrength); }

    public void DecreaseVitality() { TryDecreaseStat(ref tempVitality, vitality); }
    public void DecreaseEndurance() { TryDecreaseStat(ref tempEndurance, endurance); }
    public void DecreaseStrength() { TryDecreaseStat(ref tempStrength, strength); }

    private void TryIncreaseStat(ref int stat)
    {
        if (playerRunes.CurrentRunes >= totalCost + currentCost)
        {
            totalCost += currentCost;
            stat++;
            tempLevel++;
            CalculateNextLevelCost(); 
            UpdateLevelUpUI();
        }
    }

    private void TryDecreaseStat(ref int tempStat, int baseStat)
    {
        if (tempStat > baseStat)
        {
            tempStat--;
            tempLevel--;
            CalculateNextLevelCost(); 
            totalCost -= currentCost; 
            UpdateLevelUpUI();
        }
    }

    public void ConfirmLevelUp()
    {
        if (totalCost > 0 && playerRunes.CurrentRunes >= totalCost)
        {
            playerRunes.SpendRunes(totalCost);

            int healthBonus = (tempVitality - vitality) * 25; 
            float staminaBonus = (tempEndurance - endurance) * 15f;
            int damageBonus = (tempStrength - strength) * 3;

            playerLevel = tempLevel;
            vitality = tempVitality;
            endurance = tempEndurance;
            strength = tempStrength;

            PlayerHealth health = FindObjectOfType<PlayerHealth>();
            if (health != null && healthBonus > 0) health.UpgradeMaxHealth(healthBonus);

            PlayerStamina stamina = FindObjectOfType<PlayerStamina>();
            if (stamina != null && staminaBonus > 0) stamina.UpgradeMaxStamina(staminaBonus);

            PlayerCombatSystem combat = FindObjectOfType<PlayerCombatSystem>();
            if (combat != null && damageBonus > 0) combat.UpgradeAttackDamage(damageBonus);

            Debug.Log("✨ Seviye Atlandı! Yeni Seviye: " + playerLevel);
            
            ResetTempStats();
            UpdateLevelUpUI();
        }
    }
}