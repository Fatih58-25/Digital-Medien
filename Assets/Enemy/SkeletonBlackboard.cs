using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SkeletonBlackboard : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;
    // HATA BURADA MI? ŞUNU TAM KOPYALA:
    public List<Transform> waypoints = new List<Transform>(); 
    public int currentWaypointIndex = 0;

    public float walkSpeed = 1.5f;
public float runSpeed = 4.0f;
public float idleTimer = 0f;          // Oyuncunun hareket etmediği süre
public float lastPlayerPosDist = 0f;  // Oyuncu hamle yaptı mı kontrolü
public float decisionTimer = 0f;      // Karar verme zamanlayıcısı
public int strafeDirection = 1;       // 1 = Sağa, -1 = Sola
public float nextAttackTime = 0f;
public float attackCooldown = 2.5f; // Her 2.5 saniyede bir saldırabilir
public string currentMode = "Idle"; // "Circle", "Charge", "Wait"
public float modeTimer = 0f;
// --- Souls AI Hafıza ve Sayaçlar ---
public float playerIdleTimer = 0f;       // Oyuncunun saldırmadan beklediği süre
public int comboCount = 0;               // Üst üste yapılan saldırı sayısı
public int maxComboLimit = 3;            // Bu tur yapabileceği maks kombo (rastgele 3 veya 4 olacak)
public float globalCooldownTimer = 0f;   // Kombo sonrası iskeletin "nefes alma" süresi
public bool isDodging = false;
public float dodgeTimer = 0f;
public bool hasTarget = false;
[Header("Souls AI İnce Ayarları")]
[Range(1, 3)] public int minComboCount = 1;        // En az kaç vuruş yapsın
[Range(2, 4)] public int maxComboCount = 2;        // En fazla kaç vuruş yapsın (Agresifliği azaltmak için 2 ideal)
public float minComboCooldown = 4f;                // Kombo bittiğinde en az kaç saniye süzülsün (nefes alsın)
public float maxComboCooldown = 6f;                // En fazla kaç saniye süzülsün
[Range(0f, 1f)] public float earlyAttackChance = 0.05f; // %5 şans (Çok agresif olmasın diye düşürdük)
public float patienceDuration = 5f;                // Oyuncu saldırmazsa kaç saniye sonra sabrı taşsın

// Oyuncunun saldırıp saldırmadığını anlamak için (Oyuncu scriptinde saldırırken bu bool'u true yapmalısın)
public bool isPlayerAttacking = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }
}