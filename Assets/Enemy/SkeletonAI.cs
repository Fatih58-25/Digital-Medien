using UnityEngine;
using System.Collections.Generic;
using GenericBehaviorTree;

public enum AIMode { Stalk, Charge, Strafe, Backstep, DashAttack }

public class SkeletonAI : MonoBehaviour
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
    private readonly float sightRangeSqr = 100f; // 10f * 10f

    public CanSeePlayer(SkeletonBlackboard b) => bb = b;
    
    public override NodeState Evaluate() 
    {
        if (bb.player == null) return NodeState.FAILURE;
        if (bb.hasTarget) return NodeState.SUCCESS;

        // Vector3.Distance yerine sqrMagnitude (Karekök hesabını kaldırır, CPU dostudur)
        if ((bb.transform.position - bb.player.position).sqrMagnitude < sightRangeSqr) 
        {
            bb.hasTarget = true;
            return NodeState.SUCCESS;
        }

        return NodeState.FAILURE;
    }
}

// ==========================================
// COMBAT NODE (OPTIMIZED & REFACTORED)
// ==========================================
public class CombatNode : Node 
{
    private SkeletonBlackboard bb;
    
    // Karesel Mesafeler (Performance Fix)
    private readonly float attackRangeSqr = 2.25f;    // 1.5f ^ 2
    private readonly float pressureRangeSqr = 49.0f;  // 7.0f ^ 2
    private readonly float runRangeSqr = 625.0f;      // 25.0f ^ 2
    private readonly float deaggroRangeSqr = 400.0f;  // 20.0f ^ 2

    private float blockHoldTimer = 0f;
    private float walkBlockToggleTimer = 0f;
    private float navmeshRepathTimer = 0f; // NavMesh sorgu sınırlayıcı

    public AIMode currentMode = AIMode.Stalk;

    public CombatNode(SkeletonBlackboard b) => bb = b;

    public override NodeState Evaluate() 
    {
        if (bb.player == null) return NodeState.FAILURE;
        
        Vector3 offset = bb.player.position - bb.transform.position;
        float distSqr = offset.sqrMagnitude;

        // Deaggro Kontrolü
        if (distSqr > deaggroRangeSqr) 
        {
            ResetCombatState();
            return NodeState.FAILURE; 
        }

        HandleTimers();
        RotateTowardsPlayer();

        // 1. Dodge & Blok Refleksi
        if (HandlePlayerAttackDodgeAndBlock(distSqr)) 
        {
            return NodeState.RUNNING;
        }

        // 2. Mod & Sabır Güncellemesi
        UpdateModesAndPatience(distSqr);

        // --- ANA DAVRANIŞ KARAR MOTORU ---
        if (distSqr <= attackRangeSqr)
        {
            ExecuteComboLogic();
        }
        else if (distSqr > runRangeSqr || currentMode == AIMode.Charge) 
        {
            ExecuteChargeMovement();
        }
        else if (distSqr > pressureRangeSqr && distSqr <= runRangeSqr) 
        {
            ExecuteStalkMovement();
        }
        else // Takitksel Alan (attackRangeSqr < distSqr <= pressureRangeSqr)
        {
            ExecuteTacticalMovement(distSqr);
        }

        return NodeState.RUNNING;
    }

    #region OPTİMİZASYON METODLARI

    private void HandleTimers()
    {
        if (bb.agent.updateRotation) bb.agent.updateRotation = false;
        if (bb.globalCooldownTimer > 0) bb.globalCooldownTimer -= Time.deltaTime;

        if (blockHoldTimer > 0) blockHoldTimer -= Time.deltaTime;
        if (walkBlockToggleTimer > 0) walkBlockToggleTimer -= Time.deltaTime;
        if (navmeshRepathTimer > 0) navmeshRepathTimer -= Time.deltaTime;
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

    private bool HandlePlayerAttackDodgeAndBlock(float distSqr)
    {
        if (!bb.isPlayerAttacking) 
        {
            bb.playerIdleTimer += Time.deltaTime;

            if (blockHoldTimer <= 0 && currentMode != AIMode.Backstep)
            {
                SetBlockingState(false);
            }
        } 
        else 
        {
            bb.playerIdleTimer = 0f;
            
            if (distSqr <= pressureRangeSqr && !bb.isDodging) 
            {
                float dice = Random.value;

                if (dice < 0.30f) // Dodge
                {
                    blockHoldTimer = 0f;
                    SetBlockingState(false);
                    bb.isDodging = true;
                    bb.dodgeTimer = 0.6f;
                    currentMode = AIMode.Stalk; 
                    
                    Vector3 dodgeDir = (bb.transform.position - bb.player.position).normalized;
                    SetAgentDestination(bb.transform.position + dodgeDir * 2.5f, bb.runSpeed * 1.3f, true);
                    
                    bb.animator.SetFloat("Speed", 2.0f); 
                    return true;
                }
                else if (dice < 0.80f) // Block
                {
                    blockHoldTimer = Random.Range(1.5f, 2.5f);
                    SetBlockingState(true);
                }
            }
        }

        if (bb.isDodging) 
        {
            bb.dodgeTimer -= Time.deltaTime;
            if (bb.dodgeTimer <= 0) 
            {
                bb.isDodging = false;
                bb.agent.speed = bb.walkSpeed; // Dodge bittiğinde hızı normale çek
            }
            return true;
        }
        return false;
    }

    private void UpdateModesAndPatience(float distSqr)
    {
        if (distSqr <= pressureRangeSqr && currentMode == AIMode.Charge) 
        {
            bb.playerIdleTimer = 0f;
            currentMode = AIMode.Stalk;
        }

        if (bb.playerIdleTimer >= bb.patienceDuration && distSqr > pressureRangeSqr) 
        { 
            currentMode = AIMode.Charge;
        }
    }

    private void ExecuteChargeMovement()
    {
        blockHoldTimer = 0f;
        SetBlockingState(false);

        SetAgentDestination(bb.player.position, bb.runSpeed);
        bb.animator.SetFloat("Speed", 2.0f); 
    }

    private void ExecuteStalkMovement()
    {
        SetAgentDestination(bb.player.position, bb.walkSpeed);
        bb.animator.SetFloat("Speed", 1.0f);

        if (walkBlockToggleTimer <= 0 && blockHoldTimer <= 0)
        {
            walkBlockToggleTimer = Random.Range(1.5f, 3.5f);
            SetBlockingState(Random.value < 0.40f);
            if (bb.animator.GetBool("IsBlocking")) blockHoldTimer = Random.Range(1.5f, 2.5f);
        }
    }

    private void ExecuteTacticalMovement(float distSqr)
    {
        bb.modeTimer -= Time.deltaTime;
        if (bb.modeTimer <= 0)
        {
            float dice = Random.value;

            if (dice < 0.45f) currentMode = AIMode.Strafe;
            else if (dice < 0.55f) currentMode = AIMode.Backstep;
            else if (dice < 0.85f) currentMode = AIMode.Stalk;
            else currentMode = AIMode.DashAttack;

            if (currentMode == AIMode.Backstep && distSqr > 20.25f) // 4.5f ^ 2
                currentMode = AIMode.Stalk;

            bb.modeTimer = currentMode == AIMode.DashAttack ? 2.5f : Random.Range(0.3f, 1.8f);
            bb.strafeDirection = Random.value > 0.5f ? 1 : -1;
        }

        // Kalkan Kararları
        if (currentMode == AIMode.Backstep) SetBlockingState(true);
        else if (currentMode == AIMode.DashAttack) { blockHoldTimer = 0f; SetBlockingState(false); }

        // Hareketler
        switch (currentMode)
        {
            case AIMode.DashAttack:
                SetAgentDestination(bb.player.position, bb.runSpeed * 1.2f);
                bb.animator.SetFloat("Speed", 2.0f);
                break;
            case AIMode.Strafe:
                Vector3 right = Vector3.Cross(Vector3.up, (bb.player.position - bb.transform.position).normalized);
                SetAgentDestination(bb.transform.position + right * bb.strafeDirection * 1.5f, bb.walkSpeed);
                bb.animator.SetFloat("Speed", 1.0f);
                break;
            case AIMode.Backstep:
                Vector3 back = (bb.transform.position - bb.player.position).normalized;
                SetAgentDestination(bb.transform.position + back * 1.5f, bb.walkSpeed * 0.7f);
                bb.animator.SetFloat("Speed", 1.0f);
                break;
            default: // Stalk
                SetAgentDestination(bb.player.position, bb.walkSpeed);
                bb.animator.SetFloat("Speed", 1.0f);
                break;
        }
    }

    private void ExecuteComboLogic()
    {
        if (bb.globalCooldownTimer > 0) return;

        blockHoldTimer = 0f;
        SetBlockingState(false);

        if (currentMode != AIMode.DashAttack && bb.isPlayerAttacking && bb.comboCount == 0 && Random.value < 0.50f) return;

        if (bb.comboCount == 0) 
        {
            bb.maxComboLimit = Random.Range(bb.minComboCount, bb.maxComboCount + 1); 
        }

        if (bb.comboCount < bb.maxComboLimit && Time.time >= bb.nextAttackTime) 
        {
            if (IsAgentValid()) bb.agent.isStopped = true;
            
            bb.animator.SetFloat("Speed", 0f);
            bb.transform.LookAt(new Vector3(bb.player.position.x, bb.transform.position.y, bb.player.position.z));
            
            int currentStep = bb.comboCount + 1;
            bb.animator.SetInteger("AttackTyp", currentStep); 
            bb.animator.SetTrigger("Attack");

            bb.comboCount++;
            bb.nextAttackTime = Time.time + 0.9f; 
            
            if (currentMode == AIMode.DashAttack) currentMode = AIMode.Stalk;
        }
        
        if (bb.comboCount >= bb.maxComboLimit) 
        {
            bb.comboCount = 0;
            bb.playerIdleTimer = 0f;
            bb.globalCooldownTimer = Random.Range(bb.minComboCooldown, bb.maxComboCooldown); 
            
            currentMode = AIMode.Backstep;
            bb.modeTimer = 0f; 
        }
    }

    // Helper: NavMesh sorgularını limitleyen performans dostu SetDestination
    private void SetAgentDestination(Vector3 target, float speed, bool forceImmediate = false)
    {
        if (!IsAgentValid()) return;

        bb.agent.isStopped = false;
        bb.agent.speed = speed;

        if (forceImmediate || navmeshRepathTimer <= 0f)
        {
            bb.agent.SetDestination(target);
            navmeshRepathTimer = 0.1f; // Saniyede max 10 kez repath yapar (Süper Optimizasyon)
        }
    }

    private bool IsAgentValid() => bb.agent != null && bb.agent.isActiveAndEnabled && bb.agent.isOnNavMesh;

    private void SetBlockingState(bool state)
    {
        if (bb.animator != null) bb.animator.SetBool("IsBlocking", state);
    }

    private void ResetCombatState()
    {
        blockHoldTimer = 0f;
        walkBlockToggleTimer = 0f;
        SetBlockingState(false);
        bb.hasTarget = false;
        bb.comboCount = 0;
        bb.isDodging = false;
        if (bb.agent != null) bb.agent.updateRotation = true;
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

        // 1.5f ^ 2 = 2.25f
        if ((bb.transform.position - target.position).sqrMagnitude < 2.25f) 
        {
            bb.currentWaypointIndex = Random.Range(0, bb.waypoints.Count);
        }
        return NodeState.RUNNING;
    }
}