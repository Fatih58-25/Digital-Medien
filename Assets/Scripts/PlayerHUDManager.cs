using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUDManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerFlaskSystem playerFlaskSystem;

    [Header("HP Bar UI Elements")]
    [SerializeField] private RectTransform hpBarContainer; // Barın fiziksel uzunluğunu kontrol edecek obje
    [SerializeField] private Image hpMainFill;
    [SerializeField] private Image hpDamageFill;

    [Header("Stamina Bar UI Elements")]
    [SerializeField] private RectTransform staminaBarContainer; // Stamina barının çerçevesi
    [SerializeField] private Image staminaMainFill;

    [Header("Dynamic Bar Length Settings (Elden Ring Style)")]
    [SerializeField] private float pixelsPerHP = 2f; // 1 Can kaç piksel uzunluk yapsın?
    [SerializeField] private float pixelsPerStamina = 2f; // 1 Stamina kaç piksel?

    [Header("Item Slot / Flask UI")]
    [SerializeField] private Image flaskSlotImage;
    [SerializeField] private TextMeshProUGUI flaskCountText;
    [SerializeField] private Sprite fullFlaskSprite;
    [SerializeField] private Sprite emptyFlaskSprite;

    [Header("Elden Ring Damage Bar Settings")]
    [SerializeField] private float damageBufferDelay = 0.6f;
    [SerializeField] private float shrinkSpeed = 1.5f;

    private float targetHpFill = 1f;
    private float delayTimer = 0f;

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged += UpdateHPBar;
        if (playerStamina != null) playerStamina.OnStaminaChanged += UpdateStaminaBar;
        if (playerFlaskSystem != null) playerFlaskSystem.OnFlaskCountChanged += UpdateFlaskUI;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHPBar;
        if (playerStamina != null) playerStamina.OnStaminaChanged -= UpdateStaminaBar;
        if (playerFlaskSystem != null) playerFlaskSystem.OnFlaskCountChanged -= UpdateFlaskUI;
    }

    private void Start()
    {
        // Oyuna başlar başlamaz UI'ı bir kez manuel güncelle
        if (playerFlaskSystem != null)
        {
            playerFlaskSystem.NotifyUI();
        }

        // 🟢 Oyuna ilk başladığında sarı barı (hasar barını) direkt ana canla aynı seviyeye getir
        if (hpDamageFill != null && hpMainFill != null)
        {
            hpDamageFill.fillAmount = hpMainFill.fillAmount;
        }
    }

    private void Update()
    {
        // Sarı barın yavaşça erimesini sağlayan fonksiyonu çağır
        HandleDelayedDamageBar();
    }

    private void UpdateHPBar(float current, float max)
    {
        targetHpFill = current / max;
        if (hpMainFill != null) hpMainFill.fillAmount = targetHpFill;
        
        // 🟢 İyileşme olduğunda veya Seviye Atlandığında (Can yükseldiğinde) sarı barı da anında eşitle
        if (hpDamageFill != null && hpDamageFill.fillAmount < targetHpFill) 
        {
            hpDamageFill.fillAmount = targetHpFill;
        }
        
        // Max cana göre barın uzunluğunu ayarla
        if (hpBarContainer != null)
        {
            hpBarContainer.sizeDelta = new Vector2(max * pixelsPerHP, hpBarContainer.sizeDelta.y);
        }

        // Hasar aldıysak bekleme süresini baştan başlat
        delayTimer = damageBufferDelay;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaMainFill != null) staminaMainFill.fillAmount = current / max;

        // Max staminaya göre barın uzunluğunu ayarla
        if (staminaBarContainer != null)
        {
            staminaBarContainer.sizeDelta = new Vector2(max * pixelsPerStamina, staminaBarContainer.sizeDelta.y);
        }
    }

    // 🟢 (Daha önce üç nokta bıraktığım ve bozulan yerleri tam olarak ekledim)
    private void UpdateFlaskUI(int currentCount, int maxCount, bool isEmpty)
    {
        if (flaskCountText != null)
        {
            flaskCountText.text = currentCount.ToString();
        }

        if (flaskSlotImage != null)
        {
            if (isEmpty && emptyFlaskSprite != null)
            {
                flaskSlotImage.sprite = emptyFlaskSprite;
            }
            else if (fullFlaskSprite != null)
            {
                flaskSlotImage.sprite = fullFlaskSprite;
            }
        }
    }

    private void HandleDelayedDamageBar()
    {
        if (hpDamageFill == null) return;

        if (delayTimer > 0)
        {
            delayTimer -= Time.deltaTime;
        }
        else if (hpDamageFill.fillAmount > targetHpFill)
        {
            hpDamageFill.fillAmount = Mathf.Lerp(hpDamageFill.fillAmount, targetHpFill, Time.deltaTime * shrinkSpeed);
            if (Mathf.Abs(hpDamageFill.fillAmount - targetHpFill) < 0.001f)
            {
                hpDamageFill.fillAmount = targetHpFill;
            }
        }
    }
}