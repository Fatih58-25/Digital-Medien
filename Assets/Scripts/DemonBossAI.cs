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
    
    // ⚔️ MENZİL VE SÜRELER
    private readonly float attackRange = 3.5f;        
    private readonly float deaggroRangeSqr = 625.0f;  

    private float navmeshRepathTimer = 0f;
    private bool isAttacking = false;
    private bool hasShownHUD = false;

    // Taktiksel Hareket Değişkenleri
    private float strafeTimer = 0f;
    private int strafeDirection = 1;
    private bool isStrafing = false;

    public DemonBossCombatNode(SkeletonBlackboard b, MonoBehaviour m) 
    { 
        bb = b; 
        mono = m;
    }

    public override NodeState Evaluate() 
    {
        if (bb.player == null) return NodeState.FAILURE;
        
        float distanceToPlayer = Vector3.Distance(bb.transform.position, bb.player.position);

        if (distanceToPlayer * distanceToPlayer > deaggroRangeSqr) 
        {
            ResetBossState();
            return NodeState.FAILURE; 
        }

        // --- BOSS HP BARINI EKRANA GETİR ---
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

        if (isAttacking)
        {
            return NodeState.RUNNING;
        }

        // --- SALDIRI VEYA YAKLAŞMA MANTIĞI ---
        if (distanceToPlayer <= attackRange && bb.globalCooldownTimer <= 0)
        {
            mono.StartCoroutine(ExecuteSingleAttackRoutine());
            return NodeState.RUNNING;
        }
        else
        {
            ExecuteMovementLogic(distanceToPlayer);
        }

        return NodeState.RUNNING;
    }

    #region SALDIRI VE KİLİTLİ DÖNÜŞ MANTIĞI

    private IEnumerator ExecuteSingleAttackRoutine()
    {
        isAttacking = true;

        if (IsAgentValid()) 
        {
            bb.agent.isStopped = true;
            bb.agent.velocity = Vector3.zero;
        }

        bb.animator.SetFloat("Speed", 0f);

        // 🟢 1. ANINDA ERKEN DÖNÜŞ: Vurmaya karar verdiği o İLK SALİSENE tam olarak oyuncunun yüzüne kilitlenir.
        SnapDirectlyToPlayer();

        // 1, 2 veya 3 nolu saldırı animasyonunu seç ve başlat
        int attackType = Random.Range(1, 4); 
        bb.animator.SetInteger("AttackTyp", attackType);
        bb.animator.SetTrigger("Attack");

        // 🟢 2. DÖNÜŞÜ BİR DAHA ASLA TETİKLEME: Animasyon bitene kadar hiçbir şekilde dönüş kodu çalışmaz!
        float maxWaitTime = 1.6f;
        float elapsed = 0f;

        while (elapsed < maxWaitTime)
        {
            AnimatorStateInfo state = bb.animator.GetCurrentAnimatorStateInfo(0);
            if (!bb.animator.IsInTransition(0) && state.normalizedTime >= 0.88f)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 🟢 3. BEKLEME (COOLDOWN) SÜRESİ: Saldırı bitti!
        bb.globalCooldownTimer = Random.Range(3.5f, 4.4f);
        
        // Saldırı sonrası hareket kararı
        isStrafing = Random.value < 0.50f; 
        strafeTimer = Random.Range(1.0f, 1.5f);
        strafeDirection = Random.value > 0.5f ? 1 : -1;

        isAttacking = false;
        
        if (IsAgentValid()) bb.agent.isStopped = false;
    }

    // Boss'u salisesinde direkt olarak oyuncuya döndüren tam açı kilidi
    private void SnapDirectlyToPlayer()
    {
        if (bb.player == null) return;

        Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            bb.transform.rotation = targetRotation; // Slerp/Lerp yok, anında kilitlenir!
            
            if (IsAgentValid())
            {
                bb.agent.transform.rotation = targetRotation;
            }
        }
    }

    #endregion

    #region HAREKET VE YAKLAŞMA MANTIĞI

    private void ExecuteMovementLogic(float distanceToPlayer)
    {
        strafeTimer -= Time.deltaTime;

        // 🟢 KRİTİK DEĞİŞİKLİK: Yalnızca Cooldown BİTTİYSE ve yürüyorsa yavaşça dönmesine izin ver
        // Saldırı sonrası dinlenirken/beklerken ASLA dönmez!
        if (bb.globalCooldownTimer <= 0)
        {
            RotateTowardsPlayer(3.5f);
        }

        if (isStrafing && strafeTimer > 0 && distanceToPlayer <= attackRange + 2.0f)
        {
            Vector3 right = Vector3.Cross(Vector3.up, (bb.player.position - bb.transform.position).normalized);
            Vector3 strafeTarget = bb.transform.position + right * strafeDirection * 2.0f;
            
            SetAgentDestination(strafeTarget, bb.walkSpeed * 0.85f);
            bb.animator.SetFloat("Speed", 1.0f);
        }
        else
        {
            SetAgentDestination(bb.player.position, bb.walkSpeed);
            bb.animator.SetFloat("Speed", 1.0f);
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

    private void RotateTowardsPlayer(float speed)
    {
        if (bb.player == null) return;

        Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
        lookDir.y = 0; 
        if (lookDir != Vector3.zero) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, Time.deltaTime * speed);
            
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