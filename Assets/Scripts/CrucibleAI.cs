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

        // --- 1. SALDIRI MOTORU ---
        if (bb.globalCooldownTimer <= 0)
        {
            // Sadece dibindeyse (3.2 metreden az)
            if (realDistance <= 3.2f)
            {
                mono.StartCoroutine(ExecuteSmartAttackRoutine(realDistance));
                return NodeState.RUNNING;
            }
            // 3.2m - 6.0m arası: %30 Saplama (Typ 5)
            else if (realDistance <= 6.0f)
            {
                if (Random.value < 0.30f)
                {
                    mono.StartCoroutine(ExecuteSpecificAttackRoutine(5));
                    return NodeState.RUNNING;
                }
            }
        }

        // --- 2. HAREKET MOTORU ---
        if (realDistance > 12.0f)
        {
            ExecuteChargeMovement();
        }
        else if (realDistance > 6.0f)
        {
            ExecuteStalkMovement();
        }
        else
        {
            ExecuteTacticalMovement(realDistance);
        }

        return NodeState.RUNNING;
    }

    #region SALDIRI DİZİLİMLERİ VE ZORLA YÜRÜTME

    private IEnumerator ExecuteSmartAttackRoutine(float currentDist)
    {
        isAttacking = true;

        if (IsAgentValid()) bb.agent.isStopped = true;

        RotateInstantlyToPlayer();

        float dice = Random.value;

        if (dice < 0.40f)
        {
            yield return mono.StartCoroutine(PlayAttackAnimation(1));

            float newDist = Vector3.Distance(bb.transform.position, bb.player.position);
            if (newDist <= 3.5f && Random.value < 0.50f)
            {
                RotateInstantlyToPlayer();
                int followUp = Random.value > 0.4f ? 2 : 3;
                yield return mono.StartCoroutine(PlayAttackAnimation(followUp));
            }
        }
        else if (dice < 0.70f)
        {
            yield return mono.StartCoroutine(PlayAttackAnimation(2));
        }
        else if (dice < 0.88f)
        {
            yield return mono.StartCoroutine(PlayAttackAnimation(3));
        }
        else
        {
            yield return mono.StartCoroutine(PlayAttackAnimation(4));
        }

        // --- SALDIRI BİTTİ: DONUP KALMASINI ENGELLEMEK İÇİN AJANI VE ANİMATÖRÜ UYANDIR ---
        FinishAttackAndForceMove();
    }

    private IEnumerator ExecuteSpecificAttackRoutine(int attackType)
{
    isAttacking = true;

    if (IsAgentValid()) bb.agent.isStopped = true;

    RotateInstantlyToPlayer();

    // 1. Animasyonu Başlat
    bb.animator.ResetTrigger("Attack");
    bb.animator.SetInteger("AttackTyp", attackType);
    bb.animator.SetTrigger("Attack");

    yield return new WaitForSeconds(0.05f);

    // 2. Eğer Mesafe Kapatma Saldırısıysa (Typ 5) Kodla İleri Kaydır
    if (attackType == 5)
    {
        float dashDuration = 0.5f; // Kaç saniye boyunca öne atılacağı
        float dashSpeed = 12.0f;   // Atılma hızı (İstediğin gibi ayarlayabilirsin)
        float timer = 0f;

        Vector3 dashDirection = (bb.player.position - bb.transform.position).normalized;
        dashDirection.y = 0; // Yüksekliğe kaymayı engelle

        while (timer < dashDuration)
        {
            // Oyuncunun dibine çok girerse durması için mesafe kontrolü
            float dist = Vector3.Distance(bb.transform.position, bb.player.position);
            if (dist < 1.8f) break; 

            // CharacterController veya Transform ile öne kaydır
            bb.transform.position += dashDirection * dashSpeed * Time.deltaTime;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    // Animasyonun bitmesini bekle
    yield return new WaitUntil(() => 
    {
        AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
        return !bb.animator.IsInTransition(0) && state.normalizedTime >= 0.95f;
    });

    FinishAttackAndForceMove();
}

    private IEnumerator PlayAttackAnimation(int type)
    {
        bb.animator.ResetTrigger("Attack"); // Birikmiş trigger'ları temizle
        bb.animator.SetInteger("AttackTyp", type);
        bb.animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.1f);

        yield return new WaitUntil(() => 
        {
            AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
            return !bb.animator.IsInTransition(0) && state.normalizedTime >= 0.95f;
        });
    }

    // Saldırı biter bitmez ajanı serbest bırakan ve yürüten özel metod
    private void FinishAttackAndForceMove()
    {
        bb.globalCooldownTimer = Random.Range(bb.minComboCooldown, bb.maxComboCooldown);
        
        // Agent'ı kesinlikle aç
        if (IsAgentValid()) bb.agent.isStopped = false;

        // Animatördeki donmayı kır: Doğrudan yürüme parametresi ver
        bb.animator.SetFloat("Speed", 1.0f);

        // Modu belirle
        currentMode = Random.value < 0.70f ? CrucibleMode.Strafe : CrucibleMode.Backstep;
        modeTimer = Random.Range(1.5f, 2.5f);
        
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

            if (dice < 0.65f) currentMode = CrucibleMode.Strafe;   
            else if (dice < 0.85f) currentMode = CrucibleMode.Backstep; 
            else currentMode = CrucibleMode.Stalk;                

            modeTimer = Random.Range(1.5f, 3.0f);
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
        if (lookDir != Vector3.zero)
        {
            bb.transform.rotation = Quaternion.LookRotation(lookDir);
        }
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