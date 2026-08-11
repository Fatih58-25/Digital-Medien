using UnityEngine;
using System;
using System.Collections;

public class PlayerFlaskSystem : MonoBehaviour
{
    [Header("Flask Settings")]
    [SerializeField] private int maxFlasks = 3;
    [SerializeField] private int healAmount = 40;
    [SerializeField] private float drinkCooldown = 1.8f;
    [SerializeField] private KeyCode drinkKey = KeyCode.R;

    [Header("Timings (Animasyona Göre Ayarla)")]
    [SerializeField] private float showFlaskDelay = 0.15f; // Şişe elinde kaçıncı saniyede gözüksün?
    [SerializeField] private float healDelay = 0.6f;       // İlk içişte canın dolduğu an (saniye)
    [SerializeField] private float hideFlaskDelay = 1.8f;  // Şişenin elinden kaybolduğu an (saniye)

    [Header("Visuals & Props")]
    [SerializeField] private GameObject flaskProp;

    private int currentFlasks;
    private float lastDrinkTime;
    private bool isDrinking = false;

    // Ard arda basışlarda animasyon süresini uzatmak için zamanlayıcı
    private float drinkEndTime;

    private PlayerHealth playerHealth;
    private PlayerCombatSystem combatSystem;
    private Animator animator;

    // UI Güncelleme Event'i
    public event Action<int, int, bool> OnFlaskCountChanged;

    public int CurrentFlasks => currentFlasks;
    public bool IsDrinking => isDrinking;

    private void Awake()
    {
        currentFlasks = maxFlasks;
        playerHealth = GetComponent<PlayerHealth>();
        combatSystem = GetComponent<PlayerCombatSystem>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (flaskProp != null) flaskProp.SetActive(false);
        NotifyUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(drinkKey))
        {
            TryDrinkFlask();
        }
    }

    public void TryDrinkFlask()
    {
        if (currentFlasks <= 0) return;

        // 🟢 İKİNCİ VE ÜÇÜNCÜ BASTIĞINDA: Zaten içiyorsa ANINDA canı doldur!
        if (isDrinking)
        {
            InstantlyDrinkNextFlask();
            return;
        }

        if (Time.time - lastDrinkTime < drinkCooldown) return;
        if (combatSystem != null && (combatSystem.IsAttacking || combatSystem.IsRolling || combatSystem.IsStaggered)) return;

        StartCoroutine(DrinkRoutine());
    }

    // İlk R basışında çalışan normal animasyonlu coroutine
    private IEnumerator DrinkRoutine()
    {
        isDrinking = true;
        lastDrinkTime = Time.time;
        currentFlasks--;

        NotifyUI();

        if (animator != null) animator.SetTrigger("DrinkFlask");

        // 1. Şişeyi Elde Göster
        yield return new WaitForSeconds(showFlaskDelay);
        if (flaskProp != null) flaskProp.SetActive(true);

        // 2. İlk Canı Doldur
        yield return new WaitForSeconds(healDelay - showFlaskDelay);
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        // Bitiş süresini belirle (Üst üste basılırsa bu süre uzayacak)
        drinkEndTime = Time.time + (hideFlaskDelay - healDelay);

        // Şişenin elde kalma süresi bitene kadar bekle
        while (Time.time < drinkEndTime)
        {
            yield return null;
        }

        // 3. Şişeyi Gizle ve Bitir
        if (flaskProp != null) flaskProp.SetActive(false);
        isDrinking = false;
    }

    // 🟢 2. veya 3. kez R'ye basıldığında ANINDA tetiklenen metod
    private void InstantlyDrinkNextFlask()
    {
        currentFlasks--;
        NotifyUI();

        // BEKLEMEDEN ANINDA CANI BASTIK
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        // Karakter iksiri hemen elinden bırakmasın diye içme süresini 0.5 saniye daha uzatıyoruz
        drinkEndTime = Time.time + 0.5f;
    }

    // Darbe yendiğinde veya işlem kesildiğinde çağrılır
    public void InterruptDrink()
    {
        if (isDrinking)
        {
            StopAllCoroutines();
            if (flaskProp != null) flaskProp.SetActive(false);
            isDrinking = false;
        }
    }

    public void NotifyUI()
    {
        OnFlaskCountChanged?.Invoke(currentFlasks, maxFlasks, currentFlasks <= 0);
    }
}