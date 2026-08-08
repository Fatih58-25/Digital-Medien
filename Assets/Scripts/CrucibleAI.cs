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

        HandleTimers();

        if (!isAttacking)
        {
            RotateTowardsPlayer();
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

    #region EŞİTLENMİŞ VE HIZLANDIRILMIŞ SALDIRI ORANLARI

    private IEnumerator ExecuteSmartAttackRoutine(float currentDist)
    {
        isAttacking = true;
        if (IsAgentValid()) bb.agent.isStopped = true;

        RotateInstantlyToPlayer();

        float dice = Random.value;

// %50 İhtimal: Type 2 -> Type 3
if (dice < 0.50f)
{
    yield return mono.StartCoroutine(PlayComboSequence(2, 3));
}
// %5 İhtimal: Type 2 -> Type 5 (Çok nadir)
else if (dice < 0.55f)
{
    yield return mono.StartCoroutine(PlayComboSequence(2, 5));
}
// %45 İhtimal: Sadece Tekli Type 2
else
{
    yield return mono.StartCoroutine(PlaySingleAttack(2));
}

        FinishAttackAndForceMove();
    }

    private IEnumerator PlayComboSequence(int firstType, int secondType)
    {
        // --- 1. VURUŞ ---
        TriggerAnim(firstType);

        yield return new WaitForSeconds(0.10f); 

        // Type 2'nin ilk darbesinden hemen sonra (%40) ikinci vuruşa geç
        float waitWindow = (firstType == 2) ? 0.60f : 0.45f;
        yield return mono.StartCoroutine(WaitForAnimHitWindow(waitWindow));

        // --- 2. VURUŞ ÖNCESİ DÖN VE ADIM AT ---
        RotateInstantlyToPlayer();

        if (secondType == 5)
        {
            yield return mono.StartCoroutine(ExecuteDashLogic());
        }
        else
        {
            // Type 3 öncesi oyuncuya doğru küçük bir ivme ver (Iskalamasın diye)
            Vector3 stepDir = (bb.player.position - bb.transform.position).normalized;
            stepDir.y = 0;
            bb.transform.position += stepDir * 0.8f;

            TriggerAnim(secondType);
            yield return new WaitForSeconds(0.10f);
            yield return mono.StartCoroutine(WaitForAnimHitWindow(0.70f));
        }
    }

    private IEnumerator PlaySingleAttack(int type)
    {
        TriggerAnim(type);
        yield return new WaitForSeconds(0.15f);
        float waitWindow = (type == 2) ? 0.80f : 0.70f;
        yield return mono.StartCoroutine(WaitForAnimHitWindow(waitWindow));
    }

    private IEnumerator ExecuteDashAttackOnly()
    {
        isAttacking = true;
        if (IsAgentValid()) bb.agent.isStopped = true;
        RotateInstantlyToPlayer();

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
        Vector3 dashDir = (bb.player.position - bb.transform.position).normalized;
        dashDir.y = 0;

        while (timer < dashDuration)
        {
            if (Vector3.Distance(bb.transform.position, bb.player.position) < 1.8f) break;
            bb.transform.position += dashDir * dashSpeed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        yield return mono.StartCoroutine(WaitForAnimHitWindow(0.70f));
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

    private void RotateInstantlyToPlayer()
    {
        Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) bb.transform.rotation = Quaternion.LookRotation(lookDir);
    }

    private void HandleTimers()
    {
        if (bb.agent.updateRotation) bb.agent.updateRotation = false;
        if (bb.globalCooldownTimer > 0) bb.globalCooldownTimer -= Time.deltaTime;
        if (navmeshRepathTimer > 0) navmeshRepathTimer -= Time.deltaTime;
    }

    private void RotateTowardsPlayer()
    {
        Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
        lookDir.y = 0; 
        if (lookDir != Vector3.zero) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, Time.deltaTime * 6f);
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
        bb.animator.speed = 1.0f;
        isAttacking = false;
        bb.hasTarget = false;
        if (bb.agent != null) bb.agent.updateRotation = true;
    }

    #endregion
}