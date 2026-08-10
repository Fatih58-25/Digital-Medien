using UnityEngine;
using System;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f;    // Başlangıç dolma hızı (Saniyede)
    [SerializeField] private float regenDelay = 1.2f;          // Stamina harcandıktan kaç sn sonra dolmaya başlasın?

    [Header("Elden Ring Progressive Regen")]
    [SerializeField] private float boostDelay = 1.0f;          // Dolmaya başladıktan kaç sn sonra hızlansın?
    [SerializeField] private float boostedRegenMultiplier = 2.5f; // Hızlanınca başlangıç hızının kaç katına çıksın?

    private float currentStamina;
    private float regenTimer;
    private float activeRegenDuration; // Ne kadar süredir kesintisiz dolduğunu tutar

    public event Action<float, float> OnStaminaChanged;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

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
        if (currentStamina < amount) return false;

        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        regenTimer = regenDelay;        // Dolma gecikmesini başlat
        activeRegenDuration = 0f;       // Hızlanma sayacını sıfırla

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }

    public bool HasEnoughStamina(float amount)
    {
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

            // Eğer aktif dolma süresi boostDelay'i (örneğin 1 saniyeyi) geçtiyse hızı katla
            float currentRate = staminaRegenRate;
            if (activeRegenDuration >= boostDelay)
            {
                currentRate *= boostedRegenMultiplier;
            }

            currentStamina += currentRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
        else
        {
            // Dolum tamamlandıysa süreyi sıfırla
            activeRegenDuration = 0f;
        }
    }
}