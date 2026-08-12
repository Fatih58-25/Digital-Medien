using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GenericBehaviorTree;

public enum HammerBossMode { Stalk, Charge, Strafe, Backstep }

public class HammerBossAI : MonoBehaviour
{
    private Node root;
    private SkeletonBlackboard bb;

    void Start()
    {
        bb = GetComponent<SkeletonBlackboard>();

        // Ağaç yapısı: Oyuncuyu görebiliyorsa savaş, yoksa devriye at.
        root = new Selector(new List<Node>
        {
            new Sequence(new List<Node>
            {
                new CanSeePlayer(bb),
                new HammerBossCombatNode(bb, this)
            }),
            new PatrolNode(bb)
        });
    }

    void Update()
    {
        root?.Evaluate();
    }
}

public class HammerBossCombatNode : Node
{
    private SkeletonBlackboard bb;
    private MonoBehaviour mono;

    private float navmeshRepathTimer = 0f;
    private float modeTimer = 0f;
    private int strafeDirection = 1;
    private bool isAttacking = false;
    private bool hasShownHUD = false;
    
    // Aktif saldırı coroutine'ini tutuyoruz ki Node dışına çıkılırsa iptal edebilelim
    private Coroutine activeAttackCoroutine; 

    // 🔴 MESAFELERİN KARESİ (sqrMagnitude) - Performans için
    private const float MIN_ATTACK_DIST_SQR = 2.8f * 2.8f;
    private const float MAX_ATTACK_DIST_SQR = 3.5f * 3.5f;
    private const float CLOSING_ATTACK_DIST_SQR = 6.0f * 6.0f;
    private const float CHARGE_DIST_SQR = 12.0f * 12.0f;
    private const float LOSE_TARGET_DIST_SQR = 25.0f * 25.0f;

    public HammerBossMode currentMode = HammerBossMode.Stalk;

    public HammerBossCombatNode(SkeletonBlackboard b, MonoBehaviour m)
    {
        bb = b;
        mono = m;
    }

    public override NodeState Evaluate()
    {
        // Temel geçerlilik kontrolleri
        if (bb.player == null) 
        {
            ResetAndStopAttack();
            return NodeState.FAILURE;
        }

        EnemyBase enemyBase = bb.transform.GetComponent<EnemyBase>();
        if (enemyBase != null && enemyBase.IsDead)
        {
            ResetAndStopAttack();
            return NodeState.FAILURE;
        }

        // Performanslı mesafe ölçümü (Distance yerine sqrMagnitude)
        Vector3 offsetToPlayer = bb.player.position - bb.transform.position;
        float sqrDistance = offsetToPlayer.sqrMagnitude;

        // Boss oyuncudan çok uzaklaştıysa Node'u sonlandır
        if (sqrDistance > LOSE_TARGET_DIST_SQR)
        {
            ResetAndStopAttack();
            return NodeState.FAILURE;
        }

        // UI Gösterimi (Sadece bir kere tetiklenir)
        if (!hasShownHUD && enemyBase != null && enemyBase.IsBoss)
        {
            BossHUDManager.Instance?.ShowBossHealthBar(enemyBase);
            hasShownHUD = true;
        }

        HandleTimers();

        // Eğer halihazırda saldırıyorsa, başka bir işlem yapmadan devam et
        if (isAttacking)
        {
            return NodeState.RUNNING;
        }

        // Yönelme
        RotateTowardsPlayer(6f);

        // --- SALDIRI MOTORU ---
        if (bb.globalCooldownTimer <= 0)
        {
            if (sqrDistance <= MAX_ATTACK_DIST_SQR)
            {
                StartAttack(ExecuteHammerAttackRoutine(sqrDistance));
                return NodeState.RUNNING;
            }
            else if (sqrDistance <= CLOSING_ATTACK_DIST_SQR && Random.value < 0.35f)
            {
                StartAttack(ExecuteClosingAttackRoutine());
                return NodeState.RUNNING;
            }
        }

        // --- HAREKET MOTORU ---
        if (sqrDistance > CHARGE_DIST_SQR) ExecuteChargeMovement();
        else if (sqrDistance > MAX_ATTACK_DIST_SQR) ExecuteStalkMovement();
        else ExecuteTacticalMovement();

        return NodeState.RUNNING;
    }

    #region YÖNETİM METODLARI (Sızıntı Önleyiciler)

    private void StartAttack(IEnumerator attackRoutine)
    {
        isAttacking = true;
        if (activeAttackCoroutine != null)
        {
            mono.StopCoroutine(activeAttackCoroutine);
        }
        activeAttackCoroutine = mono.StartCoroutine(attackRoutine);
    }

    private void ResetAndStopAttack()
    {
        if (activeAttackCoroutine != null)
        {
            mono.StopCoroutine(activeAttackCoroutine);
            activeAttackCoroutine = null;
        }
        
        ResetBossState();
    }

    #endregion

    #region BALYOZ SALDIRI DÖNGÜLERİ

    private IEnumerator ExecuteHammerAttackRoutine(float currentDistSqr)
    {
        if (IsAgentValid()) bb.agent.isStopped = true;

        yield return mono.StartCoroutine(ApproachToIdealDistance(3.5f, 1.2f));

        float dice = Random.value;

        if (dice < 0.28f) yield return mono.StartCoroutine(PlayTripleHammerCombo());
        else if (dice < 0.68f) yield return mono.StartCoroutine(PlayHeavyQuadCombo());
        else if (dice < 0.88f) yield return mono.StartCoroutine(PlayQuickSingleAttack());
        else yield return mono.StartCoroutine(PlayBlockAndStrike());

        FinishAttackAndForceMove();
    }

    private IEnumerator ExecuteClosingAttackRoutine()
    {
        yield return mono.StartCoroutine(ApproachToIdealDistance(3.5f, 1.5f));

        if (IsAgentValid()) bb.agent.isStopped = true;

        if (Random.value < 0.6f) yield return mono.StartCoroutine(PlayBlockAndStrike());
        else yield return mono.StartCoroutine(PlayQuickSingleAttack());

        FinishAttackAndForceMove();
    }

    private IEnumerator ApproachToIdealDistance(float idealDist, float timeout)
    {
        float timer = 0f;
        float idealSqr = idealDist * idealDist;

        while (timer < timeout && bb.player != null)
        {
            float distSqr = (bb.player.position - bb.transform.position).sqrMagnitude;
            
            if (distSqr <= idealSqr && distSqr >= MIN_ATTACK_DIST_SQR)
            {
                break; // Tatlı noktadaysak dur
            }

            SetAgentDestination(bb.player.position, bb.walkSpeed * 1.2f);
            RotateTowardsPlayer(10f);

            timer += Time.deltaTime;
            yield return null;
        }

        if (IsAgentValid()) bb.agent.isStopped = true;
    }

    private IEnumerator PlayTripleHammerCombo()
    {
        TriggerAnim(1);
        yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.30f, 10f));
        yield return mono.StartCoroutine(SnapRotateAndLungeToPlayer(500f, 0.20f, 2.0f));
        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.55f));

        yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.70f, 12f));
        yield return mono.StartCoroutine(SnapRotateAndLungeToPlayer(500f, 0.18f, 2.5f));
        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.90f));
    }

    private IEnumerator PlayHeavyQuadCombo()
    {
        TriggerAnim(2);
        yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.25f, 8f));
        yield return mono.StartCoroutine(SnapRotateAndLungeToPlayer(600f, 0.20f, 2.5f));
        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.50f));

        yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.70f, 10f));
        yield return mono.StartCoroutine(SnapRotateAndLungeToPlayer(600f, 0.20f, 2.5f));
        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.80f));

        yield return mono.StartCoroutine(SnapRotateAndLungeToPlayer(720f, 0.25f, 3.0f));
        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.95f));
    }

    private IEnumerator PlayQuickSingleAttack()
    {
        TriggerAnim(3);
        yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.25f, 14f));
        yield return mono.StartCoroutine(SnapRotateAndLungeToPlayer(600f, 0.15f, 2.5f));
        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.65f));
    }

    private IEnumerator PlayBlockAndStrike()
    {
        TriggerAnim(4);
        float blockDuration = 0.4f;
        float elapsed = 0f;

        while (elapsed < blockDuration && bb.player != null)
        {
            RotateTowardsPlayer(15f);
            
            if ((bb.player.position - bb.transform.position).sqrMagnitude > MIN_ATTACK_DIST_SQR)
            {
                if (IsAgentValid())
                {
                    // 🔴 Transform yerine agent.Move kullanıldı. Rubberband'i engeller.
                    bb.agent.Move(bb.transform.forward * 2.0f * Time.deltaTime);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.75f));
    }

    private IEnumerator SnapRotateAndLungeToPlayer(float maxTurnSpeed, float duration, float lungeSpeed)
    {
        float elapsed = 0f;

        while (elapsed < duration && bb.player != null)
        {
            Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
            lookDir.y = 0;

            if (lookDir != Vector3.zero)
            {
                // Not: Eğer boss modeli ters dönüyorsa buradaki -lookDir yerine lookDir kullan.
                Quaternion targetRotation = Quaternion.LookRotation(-lookDir);
                bb.transform.rotation = Quaternion.RotateTowards(
                    bb.transform.rotation,
                    targetRotation,
                    maxTurnSpeed * Time.deltaTime
                );

                if ((bb.transform.position - bb.player.position).sqrMagnitude > MIN_ATTACK_DIST_SQR)
                {
                    Vector3 moveStep = bb.transform.forward * lungeSpeed * Time.deltaTime;
                    if (IsAgentValid()) bb.agent.Move(moveStep);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator TrackPlayerDuringAttack(float untilNormalizedTime, float turnSpeed)
    {
        while (bb.player != null)
        {
            AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);

            if (!bb.animator.IsInTransition(0) && state.normalizedTime >= untilNormalizedTime)
                break;

            RotateTowardsPlayer(turnSpeed);
            yield return null;
        }
    }

    private void TriggerAnim(int type)
    {
        bb.animator.ResetTrigger("Attack");
        bb.animator.SetInteger("AttackTyp", type);
        bb.animator.SetTrigger("Attack");
    }

    private IEnumerator WaitForAnimHitWindow(float targetNormalizedTime)
    {
        yield return new WaitUntil(() =>
        {
            if (bb.animator == null) return true;
            AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
            return !bb.animator.IsInTransition(0) && state.normalizedTime >= targetNormalizedTime;
        });
    }

    private void FinishAttackAndForceMove()
    {
        bb.globalCooldownTimer = Random.Range(3.0f, 3.5f);
        if (IsAgentValid()) bb.agent.isStopped = false;
        
        bb.animator.SetFloat("Speed", 1.0f);
        currentMode = HammerBossMode.Strafe;
        strafeDirection = Random.value > 0.5f ? 1 : -1;
        modeTimer = bb.globalCooldownTimer;

        isAttacking = false;
        activeAttackCoroutine = null;
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
        
        if (modeTimer <= 0 && bb.globalCooldownTimer <= 0)
        {
            float dice = Random.value;
            if (dice < 0.70f) currentMode = HammerBossMode.Strafe;
            else if (dice < 0.85f) currentMode = HammerBossMode.Backstep;
            else currentMode = HammerBossMode.Stalk;

            modeTimer = Random.Range(1.5f, 2.5f);
            strafeDirection = Random.value > 0.5f ? 1 : -1;
        }

        switch (currentMode)
        {
            case HammerBossMode.Strafe:
                Vector3 dirToPlayer = (bb.player.position - bb.transform.position).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dirToPlayer);
                Vector3 targetPos = bb.transform.position + right * strafeDirection * 2.0f;
                SetAgentDestination(targetPos, bb.walkSpeed * 0.6f);
                bb.animator.SetFloat("Speed", 0.8f);
                break;

            case HammerBossMode.Backstep:
                Vector3 back = (bb.transform.position - bb.player.position).normalized;
                SetAgentDestination(bb.transform.position + back * 2.5f, bb.walkSpeed * 0.6f);
                bb.animator.SetFloat("Speed", 0.8f);
                break;

            default:
                SetAgentDestination(bb.player.position, bb.walkSpeed * 0.6f);
                bb.animator.SetFloat("Speed", 1.0f);
                break;
        }
    }

    #endregion

    #region YARDIMCI METODLAR

    private void HandleTimers()
    {
        if (IsAgentValid() && bb.agent.updateRotation) 
            bb.agent.updateRotation = false;
            
        if (bb.globalCooldownTimer > 0) bb.globalCooldownTimer -= Time.deltaTime;
        if (navmeshRepathTimer > 0) navmeshRepathTimer -= Time.deltaTime;
    }

    private void RotateTowardsPlayer(float speed = 6f)
    {
        if (bb.player == null) return;

        Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            // Not: "-lookDir" modelin Z eksenine göre ters (arkaya) bakmasına sebep olabilir.
            // Modelin dümdüz ilerlemesi gerekirken yan veya ters gidiyorsa buradaki "-" işaretini kaldır.
            Quaternion targetRotation = Quaternion.LookRotation(-lookDir);
            bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, Time.deltaTime * speed);
        }
    }

    private void SetAgentDestination(Vector3 target, float speed)
    {
        if (!IsAgentValid()) return;
        bb.agent.isStopped = false;
        bb.agent.speed = speed;

        if (navmeshRepathTimer <= 0f)
        {
            bb.agent.SetDestination(target);
            navmeshRepathTimer = 0.15f;
        }
    }

    private bool IsAgentValid() => bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh;

    private void ResetBossState()
    {
        if (hasShownHUD)
        {
            BossHUDManager.Instance?.HideBossHealthBar();
            hasShownHUD = false;
        }

        if (bb.animator != null) bb.animator.speed = 1.0f;
        isAttacking = false;
        bb.hasTarget = false;
        
        if (IsAgentValid()) 
        {
            bb.agent.updateRotation = true;
            bb.agent.isStopped = true;
        }
    }

    #endregion
}