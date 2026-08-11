using UnityEngine;
using System;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f;    // Başlangıç dolma hızı (Saniyede)
    [SerializeField] private float regenDelay = 1.2f;          // Stamina harcandıktan kaç sn sonra dolmaya başlasın?

    [Header("Exhaustion Settings")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float recoveryThresholdPercent = 0.5f; // Tükenince barın % kaçında tekrar kullanılabilsin? (0.5 = %50)

    [Header("Elden Ring Progressive Regen")]
    [SerializeField] private float boostDelay = 1.0f;          // Dolmaya başladıktan kaç sn sonra hızlansın?
    [SerializeField] private float boostedRegenMultiplier = 2.5f; // Hızlanınca başlangıç hızının kaç katına çıksın?

    private float currentStamina;
    private float regenTimer;
    private float activeRegenDuration; // Ne kadar süredir kesintisiz dolduğunu tutar
    
    // Stamina tamamen bittiğinde kilitlenme durumunu kontrol eder
    private bool isExhausted = false;

    public event Action<float, float> OnStaminaChanged;
    public event Action<bool> OnExhaustionStateChanged; 

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsExhausted => isExhausted; 

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Start()
    {
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void Update()
    {
        HandleRegen();
    }

    public bool UseStamina(float amount)
    {
        // 1. Zaten tükenmişse zamanlayıcıyı yenileme ve çık! (Böylece dolma başlar)
        if (isExhausted) return false;

        // 2. Yeterli stamina yoksa tükenmişlik durumunu başlat
        if (currentStamina < amount)
        {
            SetExhaustedState(true);
            return false;
        }

        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        
        regenTimer = regenDelay;        // Dolma gecikmesini başlat
        activeRegenDuration = 0f;       // Hızlanma sayacını sıfırla

        // 3. Stamina 0 veya altına indiyse tükenmişlik moduna sok
        if (currentStamina <= 0f)
        {
            SetExhaustedState(true);
        }

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }

    public bool HasEnoughStamina(float amount)
    {
        if (isExhausted) return false;
        return currentStamina >= amount;
    }

    private void HandleRegen()
    {
        // 1. Bekleme süresi henüz bitmediyse say
        if (regenTimer > 0)
        {
            regenTimer -= Time.deltaTime;
            return;
        }

        // 2. Dolma aşaması
        if (currentStamina < maxStamina)
        {
            activeRegenDuration += Time.deltaTime;

            float currentRate = staminaRegenRate;

            // Dolma süresi boostDelay'i geçtiyse hızlanma aşamasına girer
            if (activeRegenDuration >= boostDelay)
            {
                currentRate *= boostedRegenMultiplier;
            }

            currentStamina += currentRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

            // YENİ: Eğer tükenmiş durumdaysa ve stamina %50 eşiğini (veya Inspector'dan ayarladığın değeri) geçtiyse kilidi aç!
            if (isExhausted && currentStamina >= (maxStamina * recoveryThresholdPercent))
            {
                SetExhaustedState(false);
            }

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
        else
        {
            activeRegenDuration = 0f;

            if (isExhausted)
            {
                SetExhaustedState(false);
            }
        }
    }

    private void SetExhaustedState(bool exhausted)
    {
        isExhausted = exhausted;
        OnExhaustionStateChanged?.Invoke(isExhausted);
    }
}