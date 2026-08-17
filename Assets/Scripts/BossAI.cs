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
    private bool hasShownHUD = false;
    private bool isRetreating = false;
    private Vector3 retreatDestination;

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

        // Bosslebensbalken einblenden, sobald der Kampf wirklich beginnt.
        if (!hasShownHUD)
        {
            EnemyBase enemyBase = bb.transform.GetComponentInChildren<EnemyBase>();
            if (enemyBase != null && enemyBase.IsBoss)
            {
                BossHUDManager.Instance?.ShowBossHealthBar(enemyBase);
                hasShownHUD = true;
            }
        }

        HandleTimers();

        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[BossAI] Evaluate: dist={Mathf.Sqrt(distSqr):F1}, attackRange={Mathf.Sqrt(attackRangeSqr):F1}, cooldown={bb.globalCooldownTimer:F2}, isAttacking={isAttacking}, isRetreating={isRetreating}, agent.isStopped={bb.agent?.isStopped}, agent.isOnNavMesh={bb.agent?.isOnNavMesh}, agent.velocity={bb.agent?.velocity.magnitude:F2}, agent.pathStatus={bb.agent?.pathStatus}");
        }

        if (!isAttacking)
        {
            RotateTowardsPlayer();
        }

        // Saldırı yapılıyorsa Behavior Tree bu node'u RUNNING olarak tutsun
        if (isAttacking)
        {
            return NodeState.RUNNING;
        }

        // Fechter-Rueckzug: nach einer Kombo erst zuruecklaufen, bevor sie wieder angreift.
        if (isRetreating)
        {
            ExecuteRetreat();
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

    private void ExecuteRetreat()
    {
        if (!IsAgentValid())
        {
            isRetreating = false;
            return;
        }

        bb.agent.isStopped = false;
        bb.agent.speed = bb.retreatSpeed;

        if (navmeshRepathTimer <= 0f)
        {
            bb.agent.SetDestination(retreatDestination);
            navmeshRepathTimer = 0.15f;
        }

        bb.animator.SetFloat("Speed", 1.0f);

        float distToDestSqr = (bb.transform.position - retreatDestination).sqrMagnitude;
        bool arrived = distToDestSqr < 0.5f;
        bool pathBlocked = bb.agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial
                         || bb.agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid;

        if (arrived || pathBlocked)
        {
            isRetreating = false;
        }
    }

    #region KOMBO & SALDIRI MANTIĞI

    private IEnumerator ExecuteComboRoutine()
    {
        isAttacking = true;

        if (IsAgentValid()) bb.agent.isStopped = true;
        bb.animator.SetFloat("Speed", 0f);

        // Kaçlı kombo atacağına karar ver
        int comboLimit = Random.Range(bb.minComboCount, bb.maxComboCount + 1);

        float animSpeed = bb.attackAnimSpeed > 0f ? bb.attackAnimSpeed : 1f;

        for (int i = 0; i < comboLimit; i++)
        {
            // Animator schneller laufen lassen, damit der Schlag blitzartig wirkt.
            bb.animator.speed = animSpeed;

            // Vurmadan önce oyuncuya doğru anlık dön
            RotateInstantlyToPlayer();

            // 1, 2 veya 3 nolu animasyonu rastgele seç
            int attackType = Random.Range(1, 4);

            bb.animator.SetInteger("AttackTyp", attackType);
            bb.animator.SetTrigger("Attack");

            // --- HER BİR ANİMASYONUN DOĞAL BİTİŞ SÜRELERİ (an animSpeed angepasst) ---
            if (attackType == 1)
            {
                yield return new WaitForSeconds(1.0f / animSpeed);
            }
            else if (attackType == 2)
            {
                yield return new WaitForSeconds(1.5f / animSpeed);
            }
            else if (attackType == 3)
            {
                yield return new WaitForSeconds(1.4f / animSpeed);
            }

            // Eğer oyuncu kombodan kaçıp çok uzaklaştıysa komboyu yarıda kes
            float currentDistSqr = (bb.player.position - bb.transform.position).sqrMagnitude;
            if (currentDistSqr > attackRangeSqr * 1.5f) break;
        }

        // Kombo bitti, Cooldown ver ve durumu sıfırla
        bb.animator.speed = 1.0f;
        bb.globalCooldownTimer = Random.Range(bb.minComboCooldown, bb.maxComboCooldown);
        isAttacking = false;

        // Fechterin-Verhalten: nach der Kombo aktiv zurueckweichen statt stehen zu bleiben.
        if (bb.retreatAfterCombo && bb.player != null)
        {
            Vector3 awayDir = (bb.transform.position - bb.player.position);
            awayDir.y = 0f;
            if (awayDir.sqrMagnitude < 0.01f) awayDir = -bb.transform.forward;
            awayDir.Normalize();

            retreatDestination = bb.transform.position + awayDir * bb.retreatDistance;
            isRetreating = true;
        }
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
        if (hasShownHUD)
        {
            BossHUDManager.Instance?.HideBossHealthBar();
            hasShownHUD = false;
        }

        bb.animator.speed = 1.0f;
        isAttacking = false;
        isRetreating = false;
        bb.hasTarget = false;
        if (bb.agent != null) bb.agent.updateRotation = true;
    }

    #endregion
}