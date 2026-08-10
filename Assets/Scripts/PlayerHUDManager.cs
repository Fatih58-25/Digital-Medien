using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUDManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerFlaskSystem playerFlaskSystem; // Sahneden Knight'ı buraya sürükle!

    [Header("HP Bar Images (Image Type: Filled)")]
    [SerializeField] private Image hpMainFill;
    [SerializeField] private Image hpDamageFill;

    [Header("Stamina Bar Image")]
    [SerializeField] private Image staminaMainFill;

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
    }

    private void Update()
    {
        HandleDelayedDamageBar();
    }

    private void UpdateHPBar(float current, float max)
    {
        targetHpFill = current / max;
        if (hpMainFill != null) hpMainFill.fillAmount = targetHpFill;
        if (hpDamageFill != null && hpDamageFill.fillAmount < targetHpFill) hpDamageFill.fillAmount = targetHpFill;
        delayTimer = damageBufferDelay;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaMainFill != null) staminaMainFill.fillAmount = current / max;
    }

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