using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GenericBehaviorTree;

public class DemonBossAI : MonoBehaviour
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
                new DemonBossCombatNode(bb, this)
            }),
            new PatrolNode(bb)
        });
    }

    void Update() => root.Evaluate();
}

public class DemonBossCombatNode : Node 
{
    private SkeletonBlackboard bb;
    private MonoBehaviour mono;
    
    // Karesel Mesafeler (Silahı uzun olduğu için menzili biraz geniş tutabilirsin)
    private readonly float attackRangeSqr = 16.0f;    // 4.0f ^ 2 (Uzun silah menzili)
    private readonly float deaggroRangeSqr = 625.0f;  // 25.0f ^ 2

    private float navmeshRepathTimer = 0f;
    private bool isAttacking = false;

    public DemonBossCombatNode(SkeletonBlackboard b, MonoBehaviour m) 
    { 
        bb = b; 
        mono = m;
    }

    public override NodeState Evaluate() 
    {
        if (bb.player == null) return NodeState.FAILURE;
        
        Vector3 offset = bb.player.position - bb.transform.position;
        float distSqr = offset.sqrMagnitude;

        if (distSqr > deaggroRangeSqr) 
        {
            ResetBossState();
            return NodeState.FAILURE; 
        }

        HandleTimers();

        // Saldırı anında Boss dönmesin, oyuncu arkasına geçebilsin
        if (!isAttacking)
        {
            RotateTowardsPlayer();
        }

        if (isAttacking)
        {
            return NodeState.RUNNING;
        }

        // --- TEKLİ SALDIRI MOTORU ---
        if (distSqr <= attackRangeSqr && bb.globalCooldownTimer <= 0)
        {
            mono.StartCoroutine(ExecuteSingleAttackRoutine());
            return NodeState.RUNNING;
        }
        else if (!isAttacking)
        {
            ExecuteApproach(distSqr);
        }

        return NodeState.RUNNING;
    }

    #region TEKLİ SALDIRI MANTIĞI

    private IEnumerator ExecuteSingleAttackRoutine()
    {
        isAttacking = true;

        if (IsAgentValid()) bb.agent.isStopped = true;
        bb.animator.SetFloat("Speed", 0f);
        bb.animator.speed = 1.0f;

        // Vurmadan önce oyuncuya doğru ağır bir şekilde dön
        RotateInstantlyToPlayer();

        // 1, 2 veya 3 nolu saldırı animasyonundan birini rastgele seç
        int attackType = Random.Range(1, 4); 

        bb.animator.SetInteger("AttackTyp", attackType);
        bb.animator.SetTrigger("Attack");

        // --- OTOMATİK ANİMASYON BİTİŞ KONTROLÜ ---
        // 1. Animatörün saldırı durumuna geçmesi için 1-2 kare fırsat ver
        yield return new WaitForSeconds(0.1f);

        // 2. Hangi animasyon olursa olsun, %95'i tamamlanana kadar bekle
        yield return new WaitUntil(() => 
        {
            AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
            return !bb.animator.IsInTransition(0) && state.normalizedTime >= 0.95f;
        });

        // Vuruş bitti! DS1 Demon tarzı ağır Boss olduğu için vuruş sonrası biraz dinlensin
        // Blackboard'daki Cooldown süresine göre bir sonraki hamle için bekler
        bb.globalCooldownTimer = Random.Range(bb.minComboCooldown + 1.0f, bb.maxComboCooldown + 2.0f);
        
        isAttacking = false;
    }

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
            bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, Time.deltaTime * 3f); // Dönüşü biraz yavaşlattık (Ağır hissi)
        }
    }

    private void ExecuteApproach(float distSqr)
    {
        // Ağır demon boss'lar genelde sadece yürür
        float moveSpeed = bb.walkSpeed;
        SetAgentDestination(bb.player.position, moveSpeed);
        bb.animator.SetFloat("Speed", 1.0f); // Yürüyüş animasyonu
    }

    private void SetAgentDestination(Vector3 target, float speed)
    {
        if (!IsAgentValid()) return;

        bb.agent.isStopped = false;
        bb.agent.speed = speed;

        if (navmeshRepathTimer <= 0f)
        {
            bb.agent.SetDestination(target);
            navmeshRepathTimer = 0.2f; 
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