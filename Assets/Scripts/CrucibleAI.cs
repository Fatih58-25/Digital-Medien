using UnityEngine;
using System.Collections.Generic;
using GenericBehaviorTree;

public enum CrucibleMode { Stalk, Charge, Strafe, Backstep }

public class CrucibleAI : MonoBehaviour
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
                new CrucibleCombatNode(bb)
            }),
            new PatrolNode(bb)
        });
    }

    void Update() => root.Evaluate();
}

// ==========================================================
// CRUCIBLE COMBAT NODE
// FIX: Artık coroutine (StartCoroutine/WaitForSeconds/WaitUntil) YOK.
// Saldırılar da tıpkı hareket kararları gibi HER FRAME Evaluate() ile
// tick'lenen gerçek Node'lar (ActionSequenceNode + adım node'ları).
// Bunun 3 somut faydası var:
//   1) GC alloc yok: coroutine enumerator'ları artık her saldırıda
//      heap'e allocate edilmiyor, saldırı node'ları bosstan bir kere
//      inşa edilip sürekli yeniden kullanılıyor.
//   2) Deaggro anında "kaçak" state kalmıyor: eskiden ResetBossState()
//      çağrılsa bile arka planda çalışan coroutine devam edip geç geç
//      FinishAttackAndForceMove() ile boss'a müdahale edebiliyordu.
//      Artık her şey tek bir Evaluate() zinciri içinde yaşıyor, deaggro
//      anında activeAttack.Abort() ile anında ve tam sıfırlanıyor.
//   3) Tree gerçekten her tick reaktif: RUNNING döndüren adım varsa bile
//      üst seviyedeki deaggro/HUD/timer mantığı her frame çalışıyor.
// ==========================================================
public class CrucibleCombatNode : Node 
{
    private SkeletonBlackboard bb;

    private float navmeshRepathTimer = 0f;
    private float modeTimer = 0f;
    private float dashRollTimer = 0f;
    private int strafeDirection = 1;
    private bool hasShownHUD = false;

    // Karesel mesafeler (Vector3.Distance yerine sqrMagnitude -> sqrt maliyeti yok)
    private readonly float deaggroRangeSqr = 625.0f;      // 25.0f ^ 2
    private readonly float smartAttackRangeSqr = 20.25f;  // 4.5f  ^ 2
    private readonly float dashAttackRangeSqr = 64.0f;    // 8.0f  ^ 2
    private readonly float chargeRangeSqr = 144.0f;       // 12.0f ^ 2
    private readonly float stalkRangeSqr = 36.0f;         // 6.0f  ^ 2

    public CrucibleMode currentMode = CrucibleMode.Stalk;

    // --- Aktif saldırı state'i (coroutine'in yerini bu alıyor) ---
    private ActionSequenceNode activeAttack = null;

    private enum AttackMove { None, Type1, Type4, Type2Solo, Combo23, Combo25 }
    private AttackMove lastMove = AttackMove.None;

    // --- Önceden inşa edilmiş, tekrar kullanılan saldırı node'ları ---
    // (Constructor'da bir kere yaratılıyor, saldırı başına ASLA yeni allocation yok)
    private readonly ActionSequenceNode type1Solo;
    private readonly ActionSequenceNode type4Solo;
    private readonly ActionSequenceNode type2Solo;
    private readonly ActionSequenceNode type2To3Combo;
    private readonly ActionSequenceNode type2To5Combo;
    private readonly ActionSequenceNode type5SoloDash; // 4.5-8m arası "sürpriz dash"

    public CrucibleCombatNode(SkeletonBlackboard b) 
    { 
        bb = b; 

        // NOT: Type1 ve Type4 için özel bir koreografi belirtilmediğinden genel bir
        // "tekli saldırı" kalıbı kullandım (Trigger -> takip et -> hit-window bekle).
        // normalizedTime eşiklerini kendi animasyon kliplerine göre ince ayar yap.
        type1Solo = new ActionSequenceNode(
            new TriggerAnimStep(bb, 1),
            new TrackPlayerWhileWaitingStep(bb, 0.45f, 12f, 3f),
            new WaitForAnimNormalizedTimeStep(bb, 0.75f, 3f)
        );

        type4Solo = new ActionSequenceNode(
            new TriggerAnimStep(bb, 4),
            new TrackPlayerWhileWaitingStep(bb, 0.45f, 12f, 3f),
            new WaitForAnimNormalizedTimeStep(bb, 0.75f, 3f)
        );

        type2Solo = new ActionSequenceNode(
            new TriggerAnimStep(bb, 2),
            new TrackPlayerWhileWaitingStep(bb, 0.30f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.38f, 2f),
            new LungeStep(bb, 720f, 0.22f, 12.0f, 1.6f),
            new TrackPlayerWhileWaitingStep(bb, 0.80f, 8f, 2f)
        );

        type2To3Combo = new ActionSequenceNode(
            new TriggerAnimStep(bb, 2),
            new TrackPlayerWhileWaitingStep(bb, 0.30f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.38f, 2f),
            new LungeStep(bb, 720f, 0.22f, 12.0f, 1.6f),
            new WaitForAnimNormalizedTimeStep(bb, 0.75f, 2f),
            new StepTowardIfFarStep(bb, 2.0f, 1.2f),
            new TriggerAnimStep(bb, 3),
            new TrackPlayerWhileWaitingStep(bb, 0.40f, 14f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.70f, 2f)
        );

        type2To5Combo = new ActionSequenceNode(
            new TriggerAnimStep(bb, 2),
            new TrackPlayerWhileWaitingStep(bb, 0.30f, 10f, 2f),
            new WaitForAnimNormalizedTimeStep(bb, 0.38f, 2f),
            new LungeStep(bb, 720f, 0.22f, 12.0f, 1.6f),
            new WaitForAnimNormalizedTimeStep(bb, 0.75f, 2f),
            new TriggerAnimStep(bb, 5),
            new WaitSecondsStep(0.05f),
            new DashStep(bb, 16.0f, 0.35f, 1.8f, 20f),
            new WaitForAnimNormalizedTimeStep(bb, 0.70f, 2f)
        );

        type5SoloDash = new ActionSequenceNode(
            new TriggerAnimStep(bb, 5),
            new WaitSecondsStep(0.05f),
            new DashStep(bb, 16.0f, 0.35f, 1.8f, 20f),
            new WaitForAnimNormalizedTimeStep(bb, 0.70f, 2f)
        );
    }

    public override NodeState Evaluate() 
    {
        if (bb.player == null) 
        {
            ResetBossState();
            return NodeState.FAILURE;
        }
        if (bb.animator == null) return NodeState.FAILURE; // Animator yoksa hiçbir şey güvenli değil

        Vector3 offset = bb.player.position - bb.transform.position;
        float distSqr = offset.sqrMagnitude;

        if (distSqr > deaggroRangeSqr) 
        {
            ResetBossState();
            return NodeState.FAILURE; 
        }

        if (!hasShownHUD)
        {
            EnemyBase enemyBase = bb.transform.GetComponent<EnemyBase>();
            if (enemyBase != null && enemyBase.IsBoss)
            {
                BossHUDManager.Instance?.ShowBossHealthBar(enemyBase);
                hasShownHUD = true;
            }
        }

        HandleTimers();

        // --- Devam eden bir saldırı varsa: sadece onu tick'le, başka hiçbir karar alma ---
        if (activeAttack != null)
        {
            NodeState attackResult = activeAttack.Evaluate();
            if (attackResult == NodeState.RUNNING) return NodeState.RUNNING;

            FinishAttackAndForceMove();
            return NodeState.RUNNING;
        }

        CrucibleCombatUtils.SlerpRotateTowards(bb, 6f);

        // --- SALDIRI SEÇİMİ ---
        if (bb.globalCooldownTimer <= 0)
        {
            if (distSqr <= smartAttackRangeSqr)
            {
                StartAttack(PickWeightedMove());
                return NodeState.RUNNING;
            }
            else if (distSqr <= dashAttackRangeSqr && dashRollTimer <= 0f)
            {
                // FIX: Bu zar eskiden HER FRAME atılıyordu (%20 ihtimal 60fps'te
                // pratikte ~100ms'de garanti tetikleniyordu). Artık en fazla
                // ~0.4 saniyede bir atılıyor, ihtimal gerçek anlamına kavuşuyor.
                dashRollTimer = 0.4f;
                if (Random.value < 0.005f)
                {
                    StartAttack(type5SoloDash);
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

    #region SALDIRI SEÇİMİ VE YÖNETİMİ

    // FIX / YENİ ÖZELLİK: Artık sadece Type 2/3/5 değil, Type 1 (%5, nadir özel)
    // ve Type 4 (%15, ağır saldırı) de repertuvarda. Ayrıca "aynı hamleyi art arda
    // seçme" ihtimalini azaltan basit bir anti-tekrar mekanizması var, bossun daha
    // az deterministik / daha az öngörülebilir hissettirmesi için.
    private AttackMove PickWeightedMove()
    {
        AttackMove picked = RollWeightedMove();

        if (picked == lastMove)
        {
            picked = RollWeightedMove(); // Art arda tekrar ihtimalini kır (tek seferlik yeniden zar)
        }

        lastMove = picked;
        return picked;
    }

    private AttackMove RollWeightedMove()
{
    float dice = Random.value; // 0.0 ile 1.0 arasında bir değer üretir

    if (dice < 0.28f) return AttackMove.Type1;     // %28 - nadir/özel saldırı (artırıldı)
    if (dice < 0.43f) return AttackMove.Type4;     // %15 - ağır saldırı (0.28 + 0.15)
    if (dice < 0.55f) return AttackMove.Type2Solo; // %12 - tekli (0.43 + 0.12)
    if (dice < 0.95f) return AttackMove.Combo23;   // %40 - en sık combo (0.55 + 0.40)
    return AttackMove.Combo25;                     // %5  - dash finisher combo (düşürüldü)
}

    private void StartAttack(AttackMove move)
    {
        ActionSequenceNode chosen;
        switch (move)
        {
            case AttackMove.Type1: chosen = type1Solo; break;
            case AttackMove.Type4: chosen = type4Solo; break;
            case AttackMove.Type2Solo: chosen = type2Solo; break;
            case AttackMove.Combo23: chosen = type2To3Combo; break;
            case AttackMove.Combo25: chosen = type2To5Combo; break;
            default: chosen = type2Solo; break;
        }
        StartAttack(chosen);
    }

    private void StartAttack(ActionSequenceNode sequence)
    {
        activeAttack = sequence;
        if (CrucibleCombatUtils.IsAgentValid(bb)) bb.agent.isStopped = true;
    }

    private void FinishAttackAndForceMove()
    {
        bb.globalCooldownTimer = Random.Range(0.2f, 0.6f);
        
        if (CrucibleCombatUtils.IsAgentValid(bb)) bb.agent.isStopped = false;
        bb.animator.SetFloat("Speed", 1.0f);

        currentMode = Random.value < 0.60f ? CrucibleMode.Strafe : CrucibleMode.Backstep;
        modeTimer = Random.Range(0.5f, 1.2f);
        
        activeAttack = null;
    }

    #endregion

    #region HAREKET & GERİLİM

    private void ExecuteChargeMovement()
    {
        SetAgentDestination(bb.player.position, bb.runSpeed);
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
            if (dice < 0.60f) currentMode = CrucibleMode.Strafe;   
            else if (dice < 0.85f) currentMode = CrucibleMode.Backstep; 
            else currentMode = CrucibleMode.Stalk;                

            modeTimer = Random.Range(0.8f, 1.5f);
            strafeDirection = Random.value > 0.5f ? 1 : -1;
        }

        switch (currentMode)
        {
            case CrucibleMode.Strafe: 
                Vector3 right = Vector3.Cross(Vector3.up, (bb.player.position - bb.transform.position).normalized);
                SetAgentDestination(bb.transform.position + right * strafeDirection * 2.5f, bb.walkSpeed);
                bb.animator.SetFloat("Speed", 1.0f);
                break;

            case CrucibleMode.Backstep: 
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
        float dt = Time.deltaTime; // Tek seferde cache'le, property'ye tekrar tekrar erişme

        if (bb.agent.updateRotation) bb.agent.updateRotation = false;
        if (bb.globalCooldownTimer > 0) bb.globalCooldownTimer -= dt;
        if (navmeshRepathTimer > 0) navmeshRepathTimer -= dt;
        if (dashRollTimer > 0) dashRollTimer -= dt;
    }

    private void SetAgentDestination(Vector3 target, float speed)
    {
        if (!CrucibleCombatUtils.IsAgentValid(bb)) return;
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

        // FIX: Coroutine olmadığı için "arka planda çalışmaya devam eden" bir şey yok;
        // Abort() aktif saldırının ve tüm alt adımlarının internal state'ini (elapsed
        // sayaçları vb.) senkron olarak sıfırlıyor. Bir sonraki aggro'da her şey temiz başlıyor.
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
// SALDIRI ADIM NODE'LARI (Gerçek BT - Tick Tabanlı, Coroutine YOK)
// Her biri Node'dan türüyor, her frame Evaluate() ile çağrılıyor.
// Kendi internal state'lerini (elapsed vb.) SUCCESS dönerken otomatik
// sıfırlıyorlar; ActionSequenceNode.Abort() de yarım kalan durumda
// dıştan zorla sıfırlayabiliyor (IAttackStep.ResetState).
// ==========================================================

public interface IAttackStep
{
    void ResetState();
}

// Sıralı adım zinciri (coroutine'in yerini alan, kendi yazdığımız minimal Sequence).
// Framework'ün Selector/Sequence'ının resume/reset garantisini bilmediğimiz için
// bu davranışı burada açıkça ve garantili şekilde kendimiz yönetiyoruz.
public class ActionSequenceNode : Node
{
    private readonly Node[] steps;
    private int currentIndex = 0;

    public ActionSequenceNode(params Node[] steps)
    {
        this.steps = steps;
    }

    public override NodeState Evaluate()
    {
        while (currentIndex < steps.Length)
        {
            NodeState result = steps[currentIndex].Evaluate();

            if (result == NodeState.RUNNING) return NodeState.RUNNING;

            if (result == NodeState.FAILURE)
            {
                Abort();
                return NodeState.FAILURE;
            }

            currentIndex++; // SUCCESS -> sıradaki adıma geç (gerekirse aynı frame içinde zincirlenir)
        }

        currentIndex = 0; // Tüm adımlar bitti, bir sonraki kullanım için hazır
        return NodeState.SUCCESS;
    }

    // Deaggro / kesinti anında dışarıdan zorla sıfırlamak için
    public void Abort()
    {
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] is IAttackStep resettable) resettable.ResetState();
        }
        currentIndex = 0;
    }
}

public class TriggerAnimStep : Node, IAttackStep
{
    private readonly SkeletonBlackboard bb;
    private readonly int attackType;

    public TriggerAnimStep(SkeletonBlackboard bb, int attackType)
    {
        this.bb = bb;
        this.attackType = attackType;
    }

    public override NodeState Evaluate()
    {
        bb.animator.ResetTrigger("Attack");
        bb.animator.SetInteger("AttackTyp", attackType);
        bb.animator.SetTrigger("Attack");
        return NodeState.SUCCESS; // Anlık aksiyon, hiçbir state tutmuyor
    }

    public void ResetState() { }
}

public class WaitSecondsStep : Node, IAttackStep
{
    private readonly float duration;
    private float elapsed = 0f;

    public WaitSecondsStep(float duration)
    {
        this.duration = duration;
    }

    public override NodeState Evaluate()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            ResetState();
            return NodeState.SUCCESS;
        }
        return NodeState.RUNNING;
    }

    public void ResetState() => elapsed = 0f;
}

// FIX: Eskiden WaitUntil ile SÜRESİZ bekliyordu -> animator state adı/geçişi
// beklenmedik olursa boss "isAttacking" halinde SONSUZA KADAR kilitlenebiliyordu.
// Artık timeoutSeconds dolarsa güvenli şekilde devam ediyor, asla kilitlenmiyor.
public class WaitForAnimNormalizedTimeStep : Node, IAttackStep
{
    private readonly SkeletonBlackboard bb;
    private readonly float targetNormalizedTime;
    private readonly float timeoutSeconds;
    private float elapsed = 0f;

    public WaitForAnimNormalizedTimeStep(SkeletonBlackboard bb, float targetNormalizedTime, float timeoutSeconds)
    {
        this.bb = bb;
        this.targetNormalizedTime = targetNormalizedTime;
        this.timeoutSeconds = timeoutSeconds;
    }

    public override NodeState Evaluate()
    {
        AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
        bool reached = !bb.animator.IsInTransition(0) && state.normalizedTime >= targetNormalizedTime;

        elapsed += Time.deltaTime;

        if (reached || elapsed >= timeoutSeconds)
        {
            ResetState();
            return NodeState.SUCCESS;
        }
        return NodeState.RUNNING;
    }

    public void ResetState() => elapsed = 0f;
}

// FIX: Eskiden "while(true)" ile SÜRESİZ dönüyordu (timeout bile yoktu, WaitForAnimHitWindow'dan
// da daha riskliydi). Artık timeout garantili, her tick oyuncuya doğru dönüyor.
public class TrackPlayerWhileWaitingStep : Node, IAttackStep
{
    private readonly SkeletonBlackboard bb;
    private readonly float targetNormalizedTime;
    private readonly float turnSpeed;
    private readonly float timeoutSeconds;
    private float elapsed = 0f;

    public TrackPlayerWhileWaitingStep(SkeletonBlackboard bb, float targetNormalizedTime, float turnSpeed, float timeoutSeconds)
    {
        this.bb = bb;
        this.targetNormalizedTime = targetNormalizedTime;
        this.turnSpeed = turnSpeed;
        this.timeoutSeconds = timeoutSeconds;
    }

    public override NodeState Evaluate()
    {
        AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
        bool reached = !bb.animator.IsInTransition(0) && state.normalizedTime >= targetNormalizedTime;

        elapsed += Time.deltaTime;

        if (reached || elapsed >= timeoutSeconds)
        {
            ResetState();
            return NodeState.SUCCESS;
        }

        CrucibleCombatUtils.SlerpRotateTowards(bb, turnSpeed);
        return NodeState.RUNNING;
    }

    public void ResetState() => elapsed = 0f;
}

// Ani "snap" dönüş + oyuncuya doğru ileri adım/süzülüş (orijinal SnapRotateAndLungeToPlayer)
public class LungeStep : Node, IAttackStep
{
    private readonly SkeletonBlackboard bb;
    private readonly float maxTurnSpeed;
    private readonly float duration;
    private readonly float lungeSpeed;
    private readonly float stopDistanceSqr;
    private float elapsed = 0f;

    public LungeStep(SkeletonBlackboard bb, float maxTurnSpeed, float duration, float lungeSpeed, float stopDistance)
    {
        this.bb = bb;
        this.maxTurnSpeed = maxTurnSpeed;
        this.duration = duration;
        this.lungeSpeed = lungeSpeed;
        this.stopDistanceSqr = stopDistance * stopDistance;
    }

    public override NodeState Evaluate()
    {
        CrucibleCombatUtils.RotateTowardsDegrees(bb, maxTurnSpeed);

        if (bb.player != null)
        {
            float distSqr = (bb.player.position - bb.transform.position).sqrMagnitude;
            if (distSqr > stopDistanceSqr)
            {
                // FIX: transform.position'a direkt yazmak yerine agent.Move() -> NavMeshAgent senkron kalır
                CrucibleCombatUtils.MoveSafely(bb, bb.transform.forward * lungeSpeed * Time.deltaTime);
            }
        }

        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            ResetState();
            return NodeState.SUCCESS;
        }
        return NodeState.RUNNING;
    }

    public void ResetState() => elapsed = 0f;
}

// Dash hareketi (orijinal ExecuteDashLogic)
public class DashStep : Node, IAttackStep
{
    private readonly SkeletonBlackboard bb;
    private readonly float dashSpeed;
    private readonly float maxDuration;
    private readonly float stopDistanceSqr;
    private readonly float turnSpeed;
    private float elapsed = 0f;

    public DashStep(SkeletonBlackboard bb, float dashSpeed, float maxDuration, float stopDistance, float turnSpeed)
    {
        this.bb = bb;
        this.dashSpeed = dashSpeed;
        this.maxDuration = maxDuration;
        this.stopDistanceSqr = stopDistance * stopDistance;
        this.turnSpeed = turnSpeed;
    }

    public override NodeState Evaluate()
    {
        if (bb.player == null)
        {
            ResetState();
            return NodeState.SUCCESS; // Oyuncu kaybolduysa saldırıyı güvenle sonlandır
        }

        float distSqr = (bb.player.position - bb.transform.position).sqrMagnitude;
        if (distSqr < stopDistanceSqr || elapsed >= maxDuration)
        {
            ResetState();
            return NodeState.SUCCESS;
        }

        // Homing dash: orijinaldeki gibi Slerp tabanlı, her karede biraz daha oyuncuya döner
        CrucibleCombatUtils.SlerpRotateTowards(bb, turnSpeed);
        // FIX: agent.Move() -> NavMeshAgent'ın internal state'iyle senkron hareket
        CrucibleCombatUtils.MoveSafely(bb, bb.transform.forward * dashSpeed * Time.deltaTime);

        elapsed += Time.deltaTime;
        return NodeState.RUNNING;
    }

    public void ResetState() => elapsed = 0f;
}

// İkinci vuruş öncesi oyuncu uzaktaysa anlık bir adım at (orijinal "step" mantığı)
public class StepTowardIfFarStep : Node, IAttackStep
{
    private readonly SkeletonBlackboard bb;
    private readonly float stepDistance;
    private readonly float thresholdSqr;

    public StepTowardIfFarStep(SkeletonBlackboard bb, float thresholdDistance, float stepDistance)
    {
        this.bb = bb;
        this.stepDistance = stepDistance;
        this.thresholdSqr = thresholdDistance * thresholdDistance;
    }

    public override NodeState Evaluate()
    {
        if (bb.player != null)
        {
            Vector3 toPlayer = bb.player.position - bb.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > thresholdSqr)
            {
                CrucibleCombatUtils.MoveSafely(bb, toPlayer.normalized * stepDistance);
            }
        }
        return NodeState.SUCCESS; // Anlık aksiyon
    }

    public void ResetState() { }
}

// ==========================================================
// ORTAK YARDIMCI METODLAR (adım node'ları arasında kod tekrarını önler)
// ==========================================================
internal static class CrucibleCombatUtils
{
    public static bool IsAgentValid(SkeletonBlackboard bb) =>
        bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh;

    // FIX: NavMeshAgent aktifken transform.position'a direkt yazmak agent'ın iç state'iyle
    // senkronsuz kalıp jitter/teleport riski yaratıyordu. agent.Move() bunu önler.
    public static void MoveSafely(SkeletonBlackboard bb, Vector3 delta)
    {
        if (IsAgentValid(bb)) bb.agent.Move(delta);
        else bb.transform.position += delta;
    }

    // Sabit derece/sn hızında ani ("snap") dönüş - lunge gibi vurgulu anlar için
    public static void RotateTowardsDegrees(SkeletonBlackboard bb, float degreesPerSecond)
    {
        if (bb.player == null) return;
        Vector3 toPlayer = bb.player.position - bb.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized);
        bb.transform.rotation = Quaternion.RotateTowards(bb.transform.rotation, targetRotation, degreesPerSecond * Time.deltaTime);
    }

    // Yumuşak Slerp tabanlı dönüş - normal takip / dash sırasında kullanılır
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