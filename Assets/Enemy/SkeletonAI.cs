using UnityEngine;
using System.Collections.Generic;
using GenericBehaviorTree;

public class SkeletonAI : MonoBehaviour
{
    private Node root;
    private SkeletonBlackboard bb;

    void Start()
    {
        bb = GetComponent<SkeletonBlackboard>();

        // AĞAÇ YAPISI: Hedef bir kez bulunduysa direkt CombatNode çalışır
        root = new Selector(new List<Node>
        {
            new Sequence(new List<Node>
            {
                new CanSeePlayer(bb),
                new CombatNode(bb)
            }),
            new PatrolNode(bb)
        });
    }

    void Update() => root.Evaluate();
}

// ==========================================
// CAN SEE PLAYER NODE
// ==========================================
public class CanSeePlayer : Node 
{
    private SkeletonBlackboard bb;
    public CanSeePlayer(SkeletonBlackboard b) => bb = b;
    
    public override NodeState Evaluate() 
    {
        if (bb.player == null) return NodeState.FAILURE;
        if (bb.hasTarget) return NodeState.SUCCESS;

        if (Vector3.Distance(bb.transform.position, bb.player.position) < 10f) 
        {
            bb.hasTarget = true;
            return NodeState.SUCCESS;
        }

        return NodeState.FAILURE;
    }
}

// ==========================================
// COMBAT NODE (ADVANCED BLOCK INTEGRATED)
// ==========================================
public class CombatNode : Node 
{
    private SkeletonBlackboard bb;
    
    private readonly float attackRange = 1.5f;     
    private readonly float pressureRange = 7.0f;   
    private readonly float runRange = 25.0f;       
    private readonly float deaggroRange = 20.0f;

    // --- KALKAN VE BLOK SÜRE YÖNETİCİLERİ ---
    private float blockHoldTimer = 0f;       // Kalkanın ne kadar süre havada kalacağını tutar
    private float walkBlockToggleTimer = 0f; // Üstümüze yürürken kalkan aç/kapat kararı timer'ı

    public CombatNode(SkeletonBlackboard b) => bb = b;

    public override NodeState Evaluate() 
    {
        if (bb.player == null) return NodeState.FAILURE;
        
        float dist = Vector3.Distance(bb.transform.position, bb.player.position);

        // Deaggro Kontrolü (Çok uzaklaşınca hedefi bırak)
        if (dist > deaggroRange) 
        {
            ResetCombatState();
            return NodeState.FAILURE; 
        }

        // Zamanlayıcıları Güncelle
        HandleTimers();

        // Her Koşulda Oyuncuya Odaklan (Dodge etmediği sürece)
        RotateTowardsPlayer();

        // 1. Oyuncu Saldırı Analizi, Dodge ve REFLEKS BLOK MOTORU
        if (HandlePlayerAttackDodgeAndBlock(dist)) 
        {
            return NodeState.RUNNING;
        }

        // 2. Mesafe & Mod Güncellemeleri ve Sabır Yönetimi
        UpdateModesAndPatience(dist);

        // --- ANA DAVRANIŞ KARAR MOTORU ---
        
        // ÖNCELİK 1: Saldırı Menzili (Tüm süzülmeleri ezer, direkt dalar)
        if (dist <= attackRange)
        {
            ExecuteComboLogic();
        }
        // ÖNCELİK 2: Uzak Mesafe Koşusu veya Sabır Taşması (Charge)
        else if (dist > runRange || bb.currentMode == "Charge") 
        {
            ExecuteChargeMovement(dist);
        }
        // ÖNCELİK 3: Orta-Uzak Mesafe (Asil Souls Yürüyüş Koridoru)
        else if (dist > pressureRange && dist <= runRange) 
        {
            ExecuteStalkMovement();
        }
        // ÖNCELİK 4: Yakın Taktiksel Bölge (Strafe, Backstep, Stalk)
        else if (dist > attackRange && dist <= pressureRange) 
        {
            ExecuteTacticalMovement(dist);
        }

        return NodeState.RUNNING;
    }

    #region OPTİMİZASYON METODLARI (CLEAN CODE)

    private void HandleTimers()
    {
        if (bb.agent.updateRotation) bb.agent.updateRotation = false;
        if (bb.globalCooldownTimer > 0) bb.globalCooldownTimer -= Time.deltaTime;

        // Kalkan zamanlayıcılarını zamanla azalt
        if (blockHoldTimer > 0) blockHoldTimer -= Time.deltaTime;
        if (walkBlockToggleTimer > 0) walkBlockToggleTimer -= Time.deltaTime;
    }

    private void RotateTowardsPlayer()
    {
        if (bb.isDodging) return;

        Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
        lookDir.y = 0; 
        if (lookDir != Vector3.zero) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    private bool HandlePlayerAttackDodgeAndBlock(float dist)
    {
        if (!bb.isPlayerAttacking) 
        {
            bb.playerIdleTimer += Time.deltaTime;

            // Kalkan süresi bittiyse ve özel kalkan gerektiren bir modda (Backstep) değilse kalkanı indir
            if (blockHoldTimer <= 0 && bb.currentMode != "Backstep")
            {
                SetBlockingState(false);
            }
        } 
        else 
        {
            bb.playerIdleTimer = 0f;
            
            if (dist <= pressureRange && !bb.isDodging) 
            {
                float dice = Random.value;

                // %30 İhtimalle Dodge At
                if (dice < 0.30f)
                {
                    blockHoldTimer = 0f;
                    SetBlockingState(false); // Dodge atarken kalkan iner
                    bb.isDodging = true;
                    bb.dodgeTimer = 0.6f;
                    bb.currentMode = "Stalk"; 
                    
                    Vector3 dodgeDir = (bb.transform.position - bb.player.position).normalized;
                    if (bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh)
                    {
                        bb.agent.isStopped = false;
                        bb.agent.speed = bb.runSpeed * 1.3f;
                        bb.agent.SetDestination(bb.transform.position + dodgeDir * 2.5f);
                    }
                    bb.animator.SetFloat("Speed", 2.0f); 
                    return true;
                }
                // %50 İhtimalle KALKANI KALDIR (1.5 - 2.5 sn süresince tut!)
                else if (dice < 0.80f)
                {
                    blockHoldTimer = Random.Range(1.5f, 2.5f);
                    SetBlockingState(true);
                }
            }
        }

        if (bb.isDodging) 
        {
            bb.dodgeTimer -= Time.deltaTime;
            if (bb.dodgeTimer <= 0) bb.isDodging = false;
            return true;
        }
        return false;
    }

    private void UpdateModesAndPatience(float dist)
    {
        // Dibindeyse Charge modunu zorla kır, sakinleş
        if (dist <= pressureRange && bb.currentMode == "Charge") 
        {
            bb.playerIdleTimer = 0f;
            bb.currentMode = "Stalk";
        }

        // Sabır limiti taşma kontrolü (Sadece uzaktayken sabrı taşabilir)
        if (bb.playerIdleTimer >= bb.patienceDuration && dist > pressureRange) 
        { 
            bb.currentMode = "Charge";
        }
    }

    private void ExecuteChargeMovement(float dist)
    {
        blockHoldTimer = 0f;
        SetBlockingState(false); // Hücum koşusunda kalkan kesin kapalı

        if (bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh)
        {
            bb.agent.isStopped = false;
            bb.agent.speed = bb.runSpeed;
            bb.agent.SetDestination(bb.player.position);
        }
        bb.animator.SetFloat("Speed", 2.0f); 

        if (dist <= pressureRange) 
        {
            bb.currentMode = "Stalk";
            bb.playerIdleTimer = 0f;
            bb.modeTimer = 0f; 
        }
    }

    private void ExecuteStalkMovement()
    {
        if (bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh)
        {
            bb.agent.isStopped = false;
            bb.agent.speed = bb.walkSpeed;
            bb.agent.SetDestination(bb.player.position);
        }
        bb.animator.SetFloat("Speed", 1.0f);

        // Düz yürürken rastgele kalkan kaldırma/indirme ihtimali
        if (walkBlockToggleTimer <= 0 && blockHoldTimer <= 0)
        {
            walkBlockToggleTimer = Random.Range(1.5f, 3.5f);
            if (Random.value < 0.40f)
            {
                blockHoldTimer = Random.Range(1.5f, 2.5f);
                SetBlockingState(true);
            }
            else
            {
                SetBlockingState(false);
            }
        }
    }

    private void ExecuteTacticalMovement(float dist)
    {
        if (bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh)
        {
            bb.agent.isStopped = false;
        }

        bb.modeTimer -= Time.deltaTime;
        if (bb.modeTimer <= 0)
        {
            float dice = Random.value;

            if (dice < 0.45f) bb.currentMode = "Strafe";
            else if (dice < 0.55f) bb.currentMode = "Backstep";
            else if (dice < 0.85f) bb.currentMode = "Stalk";
            else bb.currentMode = "DashAttack";

            if (bb.currentMode == "Backstep" && dist > 4.5f) bb.currentMode = "Stalk";

            bb.modeTimer = bb.currentMode == "DashAttack" ? 2.5f : Random.Range(0.3f, 1.8f);
            bb.strafeDirection = Random.value > 0.5f ? 1 : -1;
        }

        // --- MOD BAZLI KALKAN KARARLARI ---
        if (bb.currentMode == "Backstep")
        {
            SetBlockingState(true); // Geri adım atarken kesinlikle kalkan kaldırır!
        }
        else if (bb.currentMode == "DashAttack")
        {
            blockHoldTimer = 0f;
            SetBlockingState(false); // Dash atarken kalkan iner
        }
        else // Strafe veya Stalk
        {
            if (blockHoldTimer <= 0 && walkBlockToggleTimer <= 0)
            {
                walkBlockToggleTimer = Random.Range(1.5f, 3.0f);
                if (Random.value < 0.35f)
                {
                    blockHoldTimer = Random.Range(1.2f, 2.0f);
                    SetBlockingState(true);
                }
                else
                {
                    SetBlockingState(false);
                }
            }
        }

        // --- MOD HAREKETLERİ UYGULAMASI ---
        if (bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh)
        {
            if (bb.currentMode == "DashAttack")
            {
                bb.agent.speed = bb.runSpeed * 1.2f;
                bb.agent.SetDestination(bb.player.position);
            }
            else if (bb.currentMode == "Strafe")
            {
                bb.agent.speed = bb.walkSpeed;
                Vector3 right = Vector3.Cross(Vector3.up, (bb.player.position - bb.transform.position).normalized);
                bb.agent.SetDestination(bb.transform.position + right * bb.strafeDirection * 1.5f);
            }
            else if (bb.currentMode == "Backstep")
            {
                bb.agent.speed = bb.walkSpeed * 0.7f; // Kalkanlı geri çekilme yavaşlığı
                Vector3 back = (bb.transform.position - bb.player.position).normalized;
                bb.agent.SetDestination(bb.transform.position + back * 1.5f);
            }
            else // Stalk
            {
                bb.agent.speed = bb.walkSpeed;
                bb.agent.SetDestination(bb.player.position);
            }
        }

        if (bb.currentMode == "DashAttack")
        {
            bb.animator.SetFloat("Speed", 2.0f);
        }
        else
        {
            bb.animator.SetFloat("Speed", 1.0f);
        }

        if (bb.modeTimer > 2.5f && Random.value < bb.earlyAttackChance && bb.globalCooldownTimer <= 0)
        {
            bb.playerIdleTimer = bb.patienceDuration + 0.5f;
        }
    }

    private void ExecuteComboLogic()
    {
        if (bb.globalCooldownTimer > 0) return;

        // Vurmaya karar verdiği an kalkanı ve timer'ı şak diye indirir
        blockHoldTimer = 0f;
        SetBlockingState(false);

        if (bb.currentMode != "DashAttack" && bb.isPlayerAttacking && bb.comboCount == 0 && Random.value < 0.50f) return;

        if (bb.comboCount == 0) 
        {
            bb.maxComboLimit = Random.Range(bb.minComboCount, bb.maxComboCount + 1); 
        }

        if (bb.comboCount < bb.maxComboLimit && Time.time >= bb.nextAttackTime) 
        {
            if (bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh)
            {
                bb.agent.isStopped = true;
            }
            bb.animator.SetFloat("Speed", 0f);
            
            bb.transform.LookAt(new Vector3(bb.player.position.x, bb.transform.position.y, bb.player.position.z));
            
            int currentStep = bb.comboCount + 1;
            
            bb.animator.SetInteger("AttackTyp", currentStep); 
            bb.animator.SetTrigger("Attack");
            
            Debug.Log($"Düşman Kombo Adımı #{currentStep} Tetiklendi! (Mode: {bb.currentMode})");

            bb.comboCount++;
            bb.nextAttackTime = Time.time + 0.9f; 
            
            if (bb.currentMode == "DashAttack")
            {
                bb.currentMode = "Stalk";
            }
        }
        
        if (bb.comboCount >= bb.maxComboLimit) 
        {
            bb.comboCount = 0;
            bb.playerIdleTimer = 0f;
            bb.globalCooldownTimer = Random.Range(bb.minComboCooldown, bb.maxComboCooldown); 
            
            bb.currentMode = "Backstep"; // Kombo bittiğinde kalkanı kaldırıp geriye adım atar
            bb.modeTimer = 0f; 
        }
    }

    private void SetBlockingState(bool state)
    {
        if (bb.animator != null)
        {
            bb.animator.SetBool("IsBlocking", state);
        }
    }

    private void ResetCombatState()
    {
        blockHoldTimer = 0f;
        walkBlockToggleTimer = 0f;
        SetBlockingState(false);
        bb.hasTarget = false;
        bb.comboCount = 0;
        bb.isDodging = false;
        bb.agent.updateRotation = true;
    }

    #endregion
}

// ==========================================
// PATROL NODE
// ==========================================
public class PatrolNode : Node 
{
    private SkeletonBlackboard bb;
    public PatrolNode(SkeletonBlackboard b) => bb = b;

    public override NodeState Evaluate() 
    {
        if (bb.waypoints == null || bb.waypoints.Count == 0) return NodeState.FAILURE;

        Transform target = bb.waypoints[bb.currentWaypointIndex];
        if (bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh)
        {
            bb.agent.isStopped = false;
            bb.agent.speed = bb.walkSpeed;
            bb.agent.SetDestination(target.position);
        }

        bb.animator.SetFloat("Speed", 1.0f); 

        if (Vector3.Distance(bb.transform.position, target.position) < 1.5f) 
        {
            bb.currentWaypointIndex = Random.Range(0, bb.waypoints.Count);
        }
        return NodeState.RUNNING;
    }
}