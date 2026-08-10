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
    [SerializeField] private float healDelay = 0.6f;       // Şişeyi kafaya dikip canın dolduğu an (saniye)
    [SerializeField] private float hideFlaskDelay = 1.8f;  // Şişenin elinden kaybolduğu an (saniye)

    [Header("Visuals & Props")]
    [SerializeField] private GameObject flaskProp;

    private int currentFlasks;
    private float lastDrinkTime;
    private bool isDrinking = false;

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
        
        // Başlangıç değerini UI'a gönder
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
        if (isDrinking) return;
        if (Time.time - lastDrinkTime < drinkCooldown) return;
        if (combatSystem != null && (combatSystem.IsAttacking || combatSystem.IsRolling || combatSystem.IsStaggered)) return;

        StartCoroutine(DrinkRoutine());
    }

    private IEnumerator DrinkRoutine()
    {
        isDrinking = true;
        lastDrinkTime = Time.time;
        currentFlasks--;

        // UI'ı anında güncelle
        NotifyUI();

        // Animasyonu Tetikle
        if (animator != null) animator.SetTrigger("DrinkFlask");

        // 1. Şişeyi Elde Göster
        yield return new WaitForSeconds(showFlaskDelay);
        if (flaskProp != null) flaskProp.SetActive(true);

        // 2. Canı Doldur
        yield return new WaitForSeconds(healDelay - showFlaskDelay);
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        // 3. Şişeyi Gizle ve İçmeyi Bitir
        yield return new WaitForSeconds(hideFlaskDelay - healDelay);
        if (flaskProp != null) flaskProp.SetActive(false);
        isDrinking = false;
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