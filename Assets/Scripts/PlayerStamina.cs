using UnityEngine;
using System;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f; // Saniyede dolma miktarı
    [SerializeField] private float regenDelay = 1.2f;       // Stamina harcandıktan kaç sn sonra dolmaya başlasın?

    private float currentStamina;
    private float regenTimer;

    // UI'ın dinlemesi için event (Performans dostu haberleşme)
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

    // Stamina kullanma fonksiyonu (Yuvarlanma, Saldırı, Zıplama vs. çağıracak)
    public bool UseStamina(float amount)
    {
        if (currentStamina < amount) return false; // Yetersiz stamina

        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        regenTimer = regenDelay; // Dolma sayacını sıfırla

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }

    // Yeterli stamina var mı kontrolü
    public bool HasEnoughStamina(float amount)
    {
        return currentStamina >= amount;
    }

    private void HandleRegen()
    {
        if (regenTimer > 0)
        {
            regenTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }
}