using UnityEngine;
using System.Collections.Generic;
using GenericBehaviorTree;

public enum RolokarMode { Stalk, Charge, Strafe, Backstep }

public class RolokarAI : MonoBehaviour
{
    private Node root;
    private SkeletonBlackboard bb;

    void Start()
    {
        bb = GetComponent<SkeletonBlackboard>();

        root = new Selector(new List<Node>
        {
            new Sequence(new List<Node>
            {
                new CanSeePlayer(bb),
                new RolokarCombatNode(bb)
            }),
            new PatrolNode(bb)
        });
    }

    void Update() => root.Evaluate();
}

// ==========================================================
// ROLOKAR COMBAT NODE (MARGIT STYLE BOSS AI)
// ==========================================================
public class RolokarCombatNode : Node 
{
    private SkeletonBlackboard bb;
    private EnemyBase enemyBase;

    private float navmeshRepathTimer = 0f;
    private float modeTimer = 0f;
    private float dashRollTimer = 0f;
    private int strafeDirection = 1;
    private bool hasShownHUD = false;
    private bool isPhase2 = false;

    // Karesel mesafeler (sqrMagnitude)
    private readonly float deaggroRangeSqr = 625.0f;      // 25.0m
    private readonly float closeAttackRangeSqr = 20.25f;  // 4.5m
    private readonly float midRangeThrustSqr = 81.0f;     // 9.0m
    private readonly float chargeRangeSqr = 144.0f;       // 12.0m
    private readonly float stalkRangeSqr = 36.0f;         // 6.0m

    public RolokarMode currentMode = RolokarMode.Stalk;

    // Aktif saldırı node'u
    private ActionSequenceNode activeAttack = null;

    private enum AttackMove 
    { 
        None, 
        Type1Solo, 
        Type3Solo, 
        Type4Solo, 
        Combo12, 
        Combo123, 
        Combo124_MargitMixup 
    }
    private AttackMove lastMove = AttackMove.None;

    // Önceden allocate edilmiş, tekrar kullanılan saldırı adımları
    private readonly ActionSequenceNode type1Solo;
    private readonly ActionSequenceNode type3FastThrustSolo;
    private readonly ActionSequenceNode type4DelayedThrustSolo; // Margit Bait
    private readonly ActionSequenceNode combo12;
    private readonly ActionSequenceNode combo123;
    private readonly ActionSequenceNode combo124_MargitMixup;   // Ana Tehlike Kombosu

    public RolokarCombatNode(SkeletonBlackboard b) 
    { 
        bb = b; 
        if (bb != null) enemyBase = bb.GetComponent<EnemyBase>();

        // ------------------------------------------------------------------
        // SALDIRI KOLEKSİYONU
        // ------------------------------------------------------------------

        // Typ 1 Solo (Tekli Sağa Savurma)
        type1Solo = new ActionSequenceNode(
            new TriggerAnimStep(bb, 1),
            new TrackPlayerWhileWaitingStep(bb, 0.35f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.70f, 2f)
        );

        // Typ 3 Solo (Tekli Hızlı Şişleme - Orta Mesafe Cezalandırıcı)
        type3FastThrustSolo = new ActionSequenceNode(
            new TriggerAnimStep(bb, 3),
            new TrackPlayerWhileWaitingStep(bb, 0.20f, 14f, 1.5f),
            new LungeStep(bb, 720f, 0.20f, 14.0f, 1.5f), // Hızlı İleri Atılış
            new WaitForAnimNormalizedTimeStep(bb, 0.70f, 2f)
        );

        // Typ 4 Solo (Margit Usulü Gecikmeli Şişleme - Tekli Tuzak)
        type4DelayedThrustSolo = new ActionSequenceNode(
            new TriggerAnimStep(bb, 4),
            // Mızrağı kaldırıp beklerken oyuncuyu YAVAŞÇA takip eder (erken roll attırmak için)
            new TrackPlayerWhileWaitingStep(bb, 0.65f, 4.0f, 3.5f), 
            // Tam roll recovery anında patlayıcı fırlama!
            new LungeStep(bb, 1080f, 0.25f, 18.0f, 1.2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.85f, 2f)
        );

        // Kombo 1-2 (Sağa Savurma -> Sola Savurma)
        combo12 = new ActionSequenceNode(
            new TriggerAnimStep(bb, 1),
            new TrackPlayerWhileWaitingStep(bb, 0.35f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.50f, 2f),
            new TriggerAnimStep(bb, 2),
            new TrackPlayerWhileWaitingStep(bb, 0.30f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.70f, 2f)
        );

        // Kombo 1-2-3 (Sağa -> Sola -> HIZLI ŞİŞLEME)
        combo123 = new ActionSequenceNode(
            new TriggerAnimStep(bb, 1),
            new TrackPlayerWhileWaitingStep(bb, 0.35f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.50f, 2f),
            new TriggerAnimStep(bb, 2),
            new TrackPlayerWhileWaitingStep(bb, 0.30f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.55f, 2f),
            new StepTowardIfFarStep(bb, 2.0f, 1.2f),
            new TriggerAnimStep(bb, 3),
            new TrackPlayerWhileWaitingStep(bb, 0.20f, 14f, 1.5f),
            new LungeStep(bb, 720f, 0.20f, 14.0f, 1.5f),
            new WaitForAnimNormalizedTimeStep(bb, 0.75f, 2f)
        );

        // Kombo 1-2-4 (Sağa -> Sola -> GECİKMELİ MARGİT ŞİŞLEMESİ)
        combo124_MargitMixup = new ActionSequenceNode(
            new TriggerAnimStep(bb, 1),
            new TrackPlayerWhileWaitingStep(bb, 0.35f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.50f, 2f),
            new TriggerAnimStep(bb, 2),
            new TrackPlayerWhileWaitingStep(bb, 0.30f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.55f, 2f),
            new StepTowardIfFarStep(bb, 2.0f, 1.2f),
            new TriggerAnimStep(bb, 4),
            // Oyuncu 2. vuruştan kaçıp nefes alacağını sanırken mızrak havada asılı kalır...
            new TrackPlayerWhileWaitingStep(bb, 0.65f, 4.0f, 3.5f),
            new LungeStep(bb, 1080f, 0.25f, 18.0f, 1.2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.85f, 2f)
        );
    }

    public override NodeState Evaluate() 
    {
        if (bb.player == null || bb.animator == null) 
        {
            ResetBossState();
            return NodeState.FAILURE;
        }

        Vector3 offset = bb.player.position - bb.transform.position;
        float distSqr = offset.sqrMagnitude;

        if (distSqr > deaggroRangeSqr) 
        {
            ResetBossState();
            return NodeState.FAILURE; 
        }

        // HUD & Faz 2 Kontrolü
        CheckHUDAndPhase2();

        HandleTimers();

        // --- Devam eden bir saldırı varsa: Sadece onu yürüt ---
        if (activeAttack != null)
        {
            NodeState attackResult = activeAttack.Evaluate();
            if (attackResult == NodeState.RUNNING) return NodeState.RUNNING;

            FinishAttackAndForceMove();
            return NodeState.RUNNING;
        }

        RolokarUtils.SlerpRotateTowards(bb, isPhase2 ? 8f : 6f);

        // --- SALDIRI KARAR MEKANİZMASI ---
        if (bb.globalCooldownTimer <= 0)
        {
            // Yakın Dövüş Mesafesi (<= 4.5m)
            if (distSqr <= closeAttackRangeSqr)
            {
                StartAttack(PickWeightedCloseMove());
                return NodeState.RUNNING;
            }
            // Orta Mesafe (4.5m - 9m arası Gap-Closer)
            else if (distSqr <= midRangeThrustSqr && dashRollTimer <= 0f)
            {
                dashRollTimer = isPhase2 ? 0.25f : 0.4f;
                float chance = isPhase2 ? 0.02f : 0.008f; // Frame başı zar ihtimali

                if (Random.value < chance)
                {
                    // Orta mesafede hızlı mı yoksa gecikmeli mi geleceği de zarla belirlenir!
                    ActionSequenceNode midAttack = (Random.value < 0.5f) ? type3FastThrustSolo : type4DelayedThrustSolo;
                    StartAttack(midAttack);
                    return NodeState.RUNNING;
                }
            }
        }

        // --- HAREKET MOTORU ---
        if (distSqr > chargeRangeSqr) ExecuteChargeMovement();
        else if (distSqr > stalkRangeSqr) ExecuteStalkMovement();
        else ExecuteTacticalMovement();

        return NodeState.RUNNING;
    }

    #region FAZ 2 VE HUD KONTROLÜ

    private void CheckHUDAndPhase2()
    {
        if (enemyBase == null) enemyBase = bb.transform.GetComponent<EnemyBase>();

        if (!hasShownHUD && enemyBase != null && enemyBase.IsBoss)
        {
            BossHUDManager.Instance?.ShowBossHealthBar(enemyBase);
            hasShownHUD = true;
        }

        // 🟢 DÜZELTİLDİ: EnemyBase içindeki GetMaxHealth ve GetHealthPercentage kullanıldı
        if (!isPhase2 && enemyBase != null && enemyBase.GetMaxHealth > 0)
        {
            if (enemyBase.GetHealthPercentage <= 0.50f)
            {
                isPhase2 = true;
            }
        }
    }

    #endregion

    #region SALDIRI SEÇİMİ VE YÖNETİMİ

    private AttackMove PickWeightedCloseMove()
    {
        AttackMove picked = RollWeightedMove();

        // Üst üste aynı hamleyi yapmama filtresi
        if (picked == lastMove)
        {
            picked = RollWeightedMove();
        }

        lastMove = picked;
        return picked;
    }

    private AttackMove RollWeightedMove()
    {
        float dice = Random.value;

        // FAZ 2: Çok daha agresif, gecikmeli Margit mixup'ı ve 3'lü kombolar domine eder!
        if (isPhase2)
        {
            if (dice < 0.10f) return AttackMove.Type1Solo;
            if (dice < 0.20f) return AttackMove.Combo12;
            if (dice < 0.50f) return AttackMove.Combo123;                // %30 - Hızlı Sonlandırıcı
            if (dice < 0.85f) return AttackMove.Combo124_MargitMixup;    // %35 - GECİKMELİ TUZAK!
            return AttackMove.Type4Solo;                                 // %15 - Tekli Gecikmeli Şişleme
        }
        // FAZ 1: Daha dengeli ve öğrenilebilir
        else
        {
            if (dice < 0.15f) return AttackMove.Type1Solo;
            if (dice < 0.45f) return AttackMove.Combo12;                 // %30 - Standart 1-2
            if (dice < 0.70f) return AttackMove.Combo123;                // %25 - Hızlı 3'lü
            if (dice < 0.90f) return AttackMove.Combo124_MargitMixup;    // %20 - Margit Tuzak
            return AttackMove.Type3Solo;                                 // %10 - Tekli Hızlı
        }
    }

    private void StartAttack(AttackMove move)
    {
        ActionSequenceNode chosen;
        switch (move)
        {
            case AttackMove.Type1Solo: chosen = type1Solo; break;
            case AttackMove.Type3Solo: chosen = type3FastThrustSolo; break;
            case AttackMove.Type4Solo: chosen = type4DelayedThrustSolo; break;
            case AttackMove.Combo12: chosen = combo12; break;
            case AttackMove.Combo123: chosen = combo123; break;
            case AttackMove.Combo124_MargitMixup: chosen = combo124_MargitMixup; break;
            default: chosen = combo12; break;
        }
        StartAttack(chosen);
    }

    private void StartAttack(ActionSequenceNode sequence)
    {
        activeAttack = sequence;
        if (RolokarUtils.IsAgentValid(bb)) bb.agent.isStopped = true;
    }

    private void FinishAttackAndForceMove()
    {
        // Faz 2'de dinlenme süresi (Cooldown) kısılarak boss sürekli baskı kurar
        bb.globalCooldownTimer = isPhase2 ? Random.Range(0.10f, 0.35f) : Random.Range(0.35f, 0.75f);
        
        if (RolokarUtils.IsAgentValid(bb)) bb.agent.isStopped = false;
        bb.animator.SetFloat("Speed", 1.0f);

        currentMode = Random.value < 0.60f ? RolokarMode.Strafe : RolokarMode.Backstep;
        modeTimer = isPhase2 ? Random.Range(0.3f, 0.7f) : Random.Range(0.6f, 1.3f);
        
        activeAttack = null;
    }

    #endregion

    #region HAREKET & TAKTİK

    private void ExecuteChargeMovement()
    {
        SetAgentDestination(bb.player.position, isPhase2 ? bb.runSpeed * 1.15f : bb.runSpeed);
        bb.animator.SetFloat("Speed", 2.0f);
    }

    private void ExecuteStalkMovement()
    {
        SetAgentDestination(bb.player.position, bb.walkSpeed);
        bb.animator.SetFloat("Speed", 1.0f);
    }

    private void ExecuteTacticalMovement()
    {
        modeTimer -= Time.deltaTime;
        if (modeTimer <= 0)
        {
            float dice = Random.value;
            if (dice < 0.65f) currentMode = RolokarMode.Strafe;   
            else if (dice < 0.85f) currentMode = RolokarMode.Backstep; 
            else currentMode = RolokarMode.Stalk;                

            modeTimer = isPhase2 ? Random.Range(0.4f, 0.9f) : Random.Range(0.8f, 1.5f);
            strafeDirection = Random.value > 0.5f ? 1 : -1;
        }

        switch (currentMode)
        {
            case RolokarMode.Strafe: 
                Vector3 right = Vector3.Cross(Vector3.up, (bb.player.position - bb.transform.position).normalized);
                SetAgentDestination(bb.transform.position + right * strafeDirection * 2.5f, bb.walkSpeed);
                bb.animator.SetFloat("Speed", 1.0f);
                break;

            case RolokarMode.Backstep: 
                Vector3 back = (bb.transform.position - bb.player.position).normalized;
                SetAgentDestination(bb.transform.position + back * 2.5f, bb.walkSpeed * 0.8f);
                bb.animator.SetFloat("Speed", 1.0f);
                break;

            default: 
                SetAgentDestination(bb.player.position, bb.walkSpeed);
                bb.animator.SetFloat("Speed", 1.0f);
                break;
        }
    }

    #endregion

    #region YARDIMCI METODLAR

    private void HandleTimers()
    {
        float dt = Time.deltaTime;

        if (bb.agent.updateRotation) bb.agent.updateRotation = false;
        if (bb.globalCooldownTimer > 0) bb.globalCooldownTimer -= dt;
        if (navmeshRepathTimer > 0) navmeshRepathTimer -= dt;
        if (dashRollTimer > 0) dashRollTimer -= dt;
    }

    private void SetAgentDestination(Vector3 target, float speed)
    {
        if (!RolokarUtils.IsAgentValid(bb)) return;
        bb.agent.isStopped = false;
        bb.agent.speed = speed;

        if (navmeshRepathTimer <= 0f)
        {
            bb.agent.SetDestination(target);
            navmeshRepathTimer = 0.15f; 
        }
    }

    private void ResetBossState()
    {
        if (hasShownHUD)
        {
            BossHUDManager.Instance?.HideBossHealthBar();
            hasShownHUD = false;
        }

        if (activeAttack != null)
        {
            activeAttack.Abort();
            activeAttack = null;
        }

        if (bb.animator != null) bb.animator.speed = 1.0f;
        bb.hasTarget = false;
        lastMove = AttackMove.None;
        if (bb.agent != null) bb.agent.updateRotation = true;
    }

    #endregion
}

// ==========================================================
// ORTAK YARDIMCI METODLAR (RolokarUtils)
// ==========================================================
internal static class RolokarUtils
{
    public static bool IsAgentValid(SkeletonBlackboard bb) =>
        bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh;

    public static void MoveSafely(SkeletonBlackboard bb, Vector3 delta)
    {
        if (IsAgentValid(bb)) bb.agent.Move(delta);
        else bb.transform.position += delta;
    }

    public static void RotateTowardsDegrees(SkeletonBlackboard bb, float degreesPerSecond)
    {
        if (bb.player == null) return;
        Vector3 toPlayer = bb.player.position - bb.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized);
        bb.transform.rotation = Quaternion.RotateTowards(bb.transform.rotation, targetRotation, degreesPerSecond * Time.deltaTime);
    }

    public static void SlerpRotateTowards(SkeletonBlackboard bb, float speed)
    {
        if (bb.player == null) return;
        Vector3 toPlayer = bb.player.position - bb.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized);
        bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, speed * Time.deltaTime);
    }
}