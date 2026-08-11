using UnityEngine;
using System.Collections;
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
                new CrucibleCombatNode(bb, this)
            }),
            new PatrolNode(bb)
        });
    }

    void Update() => root.Evaluate();
}

public class CrucibleCombatNode : Node 
{
    private SkeletonBlackboard bb;
    private MonoBehaviour mono;

    private float navmeshRepathTimer = 0f;
    private float modeTimer = 0f;
    private int strafeDirection = 1;
    private bool isAttacking = false;
    private bool hasShownHUD = false;

    public CrucibleMode currentMode = CrucibleMode.Stalk;

    public CrucibleCombatNode(SkeletonBlackboard b, MonoBehaviour m) 
    { 
        bb = b; 
        mono = m;
    }

    public override NodeState Evaluate() 
    {
        if (bb.player == null) return NodeState.FAILURE;
        
        float realDistance = Vector3.Distance(bb.transform.position, bb.player.position);

        if (realDistance > 25.0f) 
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

        if (!isAttacking)
        {
            RotateTowardsPlayer(6f);
        }

        if (isAttacking)
        {
            return NodeState.RUNNING;
        }

        // --- SALDIRI MOTORU ---
        if (bb.globalCooldownTimer <= 0)
        {
            if (realDistance <= 4.2f)
            {
                mono.StartCoroutine(ExecuteSmartAttackRoutine(realDistance));
                return NodeState.RUNNING;
            }
            else if (realDistance <= 7.0f && Random.value < 0.15f)
            {
                mono.StartCoroutine(ExecuteDashAttackOnly());
                return NodeState.RUNNING;
            }
        }

        // --- HAREKET MOTORU ---
        if (realDistance > 12.0f) ExecuteChargeMovement();
        else if (realDistance > 6.0f) ExecuteStalkMovement();
        else ExecuteTacticalMovement(realDistance);

        return NodeState.RUNNING;
    }

    #region SALDIRI VE HIZLI SLICE DÖNÜŞ MANTIĞI

    private IEnumerator ExecuteSmartAttackRoutine(float currentDist)
    {
        isAttacking = true;
        if (IsAgentValid()) bb.agent.isStopped = true;

        float dice = Random.value;

        if (dice < 0.50f)
        {
            yield return mono.StartCoroutine(PlayComboSequence(2, 3));
        }
        else if (dice < 0.55f)
        {
            yield return mono.StartCoroutine(PlayComboSequence(2, 5));
        }
        else
        {
            yield return mono.StartCoroutine(PlaySingleAttack(2));
        }

        FinishAttackAndForceMove();
    }

    private IEnumerator PlayComboSequence(int firstType, int secondType)
    {
        TriggerAnim(firstType);

        if (firstType == 2)
        {
            // 1. İlk savurma takip (%30'a kadar)
            yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.30f, 10f));

            // 2. Tam 2. savurma anına (Örn: %38) kadar operate et
            yield return new WaitUntil(() => 
            {
                AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
                return !bb.animator.IsInTransition(0) && state.normalizedTime >= 0.38f; 
            });

            // 3. ÇAT DİYE DEĞİL, HIZLI VE AKICI BİR DÖNÜŞ (Speed: 35f)
            yield return mono.StartCoroutine(SnapRotateToPlayer(720f, 0.20f));
        }
        else
        {
            yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.45f, 12f));
        }

        float waitWindow = (firstType == 2) ? 0.75f : 0.45f;
        yield return mono.StartCoroutine(WaitForAnimHitWindow(waitWindow));

        // 2. Vuruş
        if (secondType == 5)
        {
            yield return mono.StartCoroutine(ExecuteDashLogic());
        }
        else
        {
            Vector3 stepDir = (bb.player.position - bb.transform.position).normalized;
            stepDir.y = 0;
            bb.transform.position += stepDir * 0.8f;

            TriggerAnim(secondType);
            
            yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.40f, 14f));
            yield return mono.StartCoroutine(WaitForAnimHitWindow(0.70f));
        }
    }

    private IEnumerator PlaySingleAttack(int type)
    {
        TriggerAnim(type);
        
        if (type == 2)
        {
            // 1. İlk savurma takibi
            yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.30f, 10f));

            // 2. 2. Savurma anına kadar bekle
            yield return new WaitUntil(() => 
            {
                AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
                return !bb.animator.IsInTransition(0) && state.normalizedTime >= 0.38f;
            });

            // 3. 0.15 SANİYEDE HIZLICA VE AKICI ŞEKİLDE OYUNCUYA DÖN
            yield return mono.StartCoroutine(SnapRotateToPlayer(720f, 0.20f));

            // 4. İkinci savurma bitişi
            yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.80f, 8f));
        }
        else
        {
            yield return mono.StartCoroutine(TrackPlayerDuringAttack(0.45f, 12f));
            yield return mono.StartCoroutine(WaitForAnimHitWindow(0.70f));
        }
    }

    private IEnumerator ExecuteDashAttackOnly()
    {
        isAttacking = true;
        if (IsAgentValid()) bb.agent.isStopped = true;

        yield return mono.StartCoroutine(ExecuteDashLogic());

        FinishAttackAndForceMove();
    }

    private IEnumerator ExecuteDashLogic()
    {
        TriggerAnim(5);
        yield return new WaitForSeconds(0.05f);

        float dashDuration = 0.35f;
        float dashSpeed = 16.0f;
        float timer = 0f;

        while (timer < dashDuration)
        {
            if (Vector3.Distance(bb.transform.position, bb.player.position) < 1.8f) break;
            
            RotateTowardsPlayer(20f);

            bb.transform.position += bb.transform.forward * dashSpeed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.70f));
    }

    // --- HIZLI VE YUMUŞAK SNAP DÖNÜŞ KORUTİNİ ---
   // Çat diye ışınlanmayan, açıya göre tatlı bir hızla dönen insansı dönüş
private IEnumerator SnapRotateToPlayer(float maxTurnSpeed, float duration)
{
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        if (bb.player != null)
        {
            Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
            lookDir.y = 0;

            if (lookDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                
                // RotateTowards sayesinde saniyede en fazla maxTurnSpeed derece döner. 
                // Bu da "ışınlanma" hissini tamamen yok eder!
                bb.transform.rotation = Quaternion.RotateTowards(
                    bb.transform.rotation, 
                    targetRotation, 
                    maxTurnSpeed * Time.deltaTime
                );

                if (IsAgentValid())
                {
                    bb.agent.transform.rotation = bb.transform.rotation;
                }
            }
        }

        elapsed += Time.deltaTime;
        yield return null;
    }
}

    private IEnumerator TrackPlayerDuringAttack(float untilNormalizedTime, float turnSpeed)
    {
        while (true)
        {
            AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
            
            if (!bb.animator.IsInTransition(0) && state.normalizedTime >= untilNormalizedTime)
            {
                break;
            }

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
            AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
            return !bb.animator.IsInTransition(0) && state.normalizedTime >= targetNormalizedTime;
        });
    }

    private void FinishAttackAndForceMove()
    {
        bb.globalCooldownTimer = Random.Range(0.2f, 0.6f);
        
        if (IsAgentValid()) bb.agent.isStopped = false;
        bb.animator.SetFloat("Speed", 1.0f);

        currentMode = Random.value < 0.60f ? CrucibleMode.Strafe : CrucibleMode.Backstep;
        modeTimer = Random.Range(0.5f, 1.2f);
        
        isAttacking = false;
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

    private void ExecuteTacticalMovement(float realDistance)
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
        if (bb.agent.updateRotation) bb.agent.updateRotation = false;
        if (bb.globalCooldownTimer > 0) bb.globalCooldownTimer -= Time.deltaTime;
        if (navmeshRepathTimer > 0) navmeshRepathTimer -= Time.deltaTime;
    }

    // ANIMATOR'IN ROTASYON BİLİCİSİNİ EZEN GÜÇLENDİRİLMİŞ DÖNÜŞ METODU
    private void RotateTowardsPlayer(float speed = 6f)
    {
        if (bb.player == null) return;
        
        Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
        lookDir.y = 0; 
        
        if (lookDir != Vector3.zero) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            
            // Transform rotasyonunu Slerp ile yumuşakça uygula
            bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, Time.deltaTime * speed);
            
            // NavMeshAgent bileşeni varsa onun da rotasyonunu eşle
            if (IsAgentValid())
            {
                bb.agent.transform.rotation = bb.transform.rotation;
            }
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

        bb.animator.speed = 1.0f;
        isAttacking = false;
        bb.hasTarget = false;
        if (bb.agent != null) bb.agent.updateRotation = true;
    }

    #endregion
}