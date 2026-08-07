using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GenericBehaviorTree;

public class BossAI : MonoBehaviour
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
                new BossCombatNode(bb, this)
            }),
            new PatrolNode(bb)
        });
    }

    void Update() => root.Evaluate();
}

public class BossCombatNode : Node 
{
    private SkeletonBlackboard bb;
    private MonoBehaviour mono;
    
    // Karesel Mesafeler
    private readonly float attackRangeSqr = 9.0f;     // 3.0f ^ 2
    private readonly float deaggroRangeSqr = 625.0f;  // 25.0f ^ 2

    private float navmeshRepathTimer = 0f;
    private bool isAttacking = false;

    public BossCombatNode(SkeletonBlackboard b, MonoBehaviour m) 
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

        if (!isAttacking)
        {
            RotateTowardsPlayer();
        }

        // Saldırı yapılıyorsa Behavior Tree bu node'u RUNNING olarak tutsun
        if (isAttacking)
        {
            return NodeState.RUNNING;
        }

        // --- SALDIRI & KOMBO MOTORU ---
        if (distSqr <= attackRangeSqr && bb.globalCooldownTimer <= 0)
        {
            mono.StartCoroutine(ExecuteComboRoutine());
            return NodeState.RUNNING;
        }
        else if (!isAttacking)
        {
            ExecuteApproach(distSqr);
        }

        return NodeState.RUNNING;
    }

    #region KOMBO & SALDIRI MANTIĞI

    private IEnumerator ExecuteComboRoutine()
    {
        isAttacking = true;

        if (IsAgentValid()) bb.agent.isStopped = true;
        bb.animator.SetFloat("Speed", 0f);

        // Kaçlı kombo atacağına karar ver
        int comboLimit = Random.Range(bb.minComboCount, bb.maxComboCount + 1);

        for (int i = 0; i < comboLimit; i++)
        {
            // Animatör hızının kesinlikle orijinal (1.0) olduğundan emin ol
            bb.animator.speed = 1.0f;

            // Vurmadan önce oyuncuya doğru anlık dön
            RotateInstantlyToPlayer();

            // 1, 2 veya 3 nolu animasyonu rastgele seç
            int attackType = Random.Range(1, 4); 

            bb.animator.SetInteger("AttackTyp", attackType);
            bb.animator.SetTrigger("Attack");

            // --- HER BİR ANİMASYONUN DOĞAL BİTİŞ SÜRELERİ ---
            if (attackType == 1) 
            {
                // 1. Saldırı animasyonunun bitiş süresi
                yield return new WaitForSeconds(1.0f); 
            }
            else if (attackType == 2) 
            {
                // 2. Saldırı animasyonunun bitiş süresi (Doğal akış)
                yield return new WaitForSeconds(1.5f); 
            }
            else if (attackType == 3) 
            {
                // 3. Saldırı animasyonunun bitiş süresi (Doğal akış)
                yield return new WaitForSeconds(1.4f); 
            }

            // Eğer oyuncu kombodan kaçıp çok uzaklaştıysa komboyu yarıda kes
            float currentDistSqr = (bb.player.position - bb.transform.position).sqrMagnitude;
            if (currentDistSqr > attackRangeSqr * 1.5f) break;
        }

        // Kombo bitti, Cooldown ver ve durumu sıfırla
        bb.animator.speed = 1.0f;
        bb.globalCooldownTimer = Random.Range(bb.minComboCooldown, bb.maxComboCooldown);
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
            bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    private void ExecuteApproach(float distSqr)
    {
        float moveSpeed = (distSqr > 100f) ? bb.runSpeed : bb.walkSpeed;
        SetAgentDestination(bb.player.position, moveSpeed);
        bb.animator.SetFloat("Speed", moveSpeed > bb.walkSpeed ? 2.0f : 1.0f);
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