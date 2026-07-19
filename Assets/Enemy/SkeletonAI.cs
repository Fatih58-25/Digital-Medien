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

        // AĞAÇ YAPISINI DÜZELTTİK:
        // Eğer iskelet bir kez hedefi belirlediyse (bb.hasTarget), CanSeePlayer'ı bypass edip
        // direkt CombatNode içinde kalmasını sağlıyoruz. Böylece anlık kaçışlar ağacı bozmaz.
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

// --- DÜĞÜMLER ---

public class CanSeePlayer : Node {
    private SkeletonBlackboard bb;
    public CanSeePlayer(SkeletonBlackboard b) => bb = b;
    
    public override NodeState Evaluate() {
        if (bb.player == null) return NodeState.FAILURE;

        // EĞER DAHA ÖNCE OYUNCUYU GÖRDÜYSE: Artık mesafeye bakma, hedefe kilitli kal!
        if (bb.hasTarget) {
            return NodeState.SUCCESS;
        }

        // İlk defa görecek mi kontrolü (Görüş menzili: 10 metre)
        if (Vector3.Distance(bb.transform.position, bb.player.position) < 10f) {
            bb.hasTarget = true; // Blackboard'a hedefi bulduğunu kaydet
            return NodeState.SUCCESS;
        }

        return NodeState.FAILURE;
    }
}
public class CombatNode : Node {
    private SkeletonBlackboard bb;
    
    private float attackRange = 2.5f;     
    private float pressureRange = 5.0f;   
    private float runRange = 9.0f;        

    public CombatNode(SkeletonBlackboard b) => bb = b;

    public override NodeState Evaluate() {
        if (bb.player == null) return NodeState.FAILURE;
        
        float dist = Vector3.Distance(bb.transform.position, bb.player.position);

        if (bb.agent.updateRotation) {
            bb.agent.updateRotation = false;
        }

        if (dist > 20f) {
            bb.hasTarget = false;
            bb.comboCount = 0;
            bb.isDodging = false;
            bb.agent.updateRotation = true; 
            return NodeState.FAILURE; 
        }

        if (bb.globalCooldownTimer > 0) {
            bb.globalCooldownTimer -= Time.deltaTime;
        }

        // --- YÜZÜNÜ HEP OYUNCUYA ÇEVİR ---
        if (!bb.isDodging) {
            Vector3 lookDir = (bb.player.position - bb.transform.position).normalized;
            lookDir.y = 0; 
            if (lookDir != Vector3.zero) {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                bb.transform.rotation = Quaternion.Slerp(bb.transform.rotation, targetRotation, Time.deltaTime * 15f);
            }
        }

        // --- 1. OYUNCU ANALİZİ VE SAYAÇLAR ---
        if (!bb.isPlayerAttacking) {
            bb.playerIdleTimer += Time.deltaTime;
        } else {
            bb.playerIdleTimer = 0f;
            
            if (dist <= pressureRange && !bb.isDodging && Random.value < 0.40f) {
                bb.isDodging = true;
                bb.dodgeTimer = 0.6f;
                
                Vector3 dodgeDir = (bb.transform.position - bb.player.position).normalized;
                bb.agent.isStopped = false;
                bb.agent.speed = bb.runSpeed * 1.3f; 
                bb.agent.SetDestination(bb.transform.position + dodgeDir * 2.5f);
                bb.animator.SetFloat("Speed", 2.0f); 
            }
        }

        if (bb.isDodging) {
            bb.dodgeTimer -= Time.deltaTime;
            if (bb.dodgeTimer <= 0) bb.isDodging = false;
            return NodeState.RUNNING;
        }

        // --- 2. SABIR LİMİTİ KONTROLÜ ---
        // Sabır süresi artık Inspector'dan geliyor (Örn: 5 saniye)
        if (bb.playerIdleTimer >= bb.patienceDuration && dist > attackRange) {
            bb.currentMode = "Charge";
        }

        // --- 3. HAREKET VE TAKTİKSEL SÜZME MODLARI ---

        if (dist > runRange || bb.currentMode == "Charge") {
            bb.agent.isStopped = false;
            bb.agent.speed = bb.runSpeed;
            bb.agent.SetDestination(bb.player.position);
            bb.animator.SetFloat("Speed", 2.0f);

            if (dist <= attackRange) {
                bb.currentMode = "Stalk"; // Menzile girince koşmayı bırak, ağır moda geç
            }
            return NodeState.RUNNING;
        }

        // ORTA MESAFEDEYS_E (Baskı Menzili)
        if (dist > attackRange && dist <= pressureRange) {
            bb.agent.isStopped = false;
            bb.agent.speed = bb.walkSpeed;
            
            bb.modeTimer -= Time.deltaTime;
            if (bb.modeTimer <= 0) {
                float dice = Random.value;
                // Süzülme ve bekleme şansını artırdık (%40 Strafe, %40 Geri adım, %20 Üstüne yürüme)
                if (dice < 0.40f) bb.currentMode = "Strafe";      
                else if (dice < 0.80f) bb.currentMode = "Backstep"; 
                else bb.currentMode = "Stalk";                      
                
                bb.modeTimer = Random.Range(3f, 5f); // Karar süresini uzattık (Daha az halay çeker)
                bb.strafeDirection = Random.value > 0.5f ? 1 : -1;
            }

            if (bb.currentMode == "Strafe") {
                Vector3 right = Vector3.Cross(Vector3.up, (bb.player.position - bb.transform.position).normalized);
                bb.agent.SetDestination(bb.transform.position + right * bb.strafeDirection * 1.5f);
                bb.animator.SetFloat("Speed", 1.0f); 
            } 
            else if (bb.currentMode == "Backstep") {
                Vector3 back = (bb.transform.position - bb.player.position).normalized;
                bb.agent.SetDestination(bb.transform.position + back * 1.5f);
                bb.animator.SetFloat("Speed", 1.0f);
            } 
            else { 
                bb.agent.SetDestination(bb.player.position);
                bb.animator.SetFloat("Speed", 1.0f);
            }

            // ERKEN SALDIRI FRENİ: Sadece mod yeni değiştiğinde çok küçük bir ihtimalle tetiklenir
            if (bb.modeTimer > 2.8f && Random.value < bb.earlyAttackChance && bb.globalCooldownTimer <= 0) {
                bb.playerIdleTimer = bb.patienceDuration + 0.5f; 
            }
        }

        // --- 4. ARD ARDA SALDIRI VE KOMBO MOTORU ---
        if (dist <= attackRange && bb.globalCooldownTimer <= 0) {
            
            if (bb.comboCount == 0) {
                // Kombo limiti artık Inspector'daki min ve max değerlerine göre belirleniyor
                bb.maxComboLimit = Random.Range(bb.minComboCount, bb.maxComboCount + 1); 
            }

            if (bb.comboCount < bb.maxComboLimit && Time.time >= bb.nextAttackTime) {
                bb.agent.isStopped = true;
                bb.animator.SetFloat("Speed", 0f);
                
                bb.transform.LookAt(new Vector3(bb.player.position.x, bb.transform.position.y, bb.player.position.z));
                
                bb.animator.SetTrigger("Attack");
                bb.comboCount++;
                bb.nextAttackTime = Time.time + 0.9f; // Vuruş aralığını hafifçe yavaşlattım (0.9s)
            }
            
            if (bb.comboCount >= bb.maxComboLimit) {
                bb.comboCount = 0;
                bb.playerIdleTimer = 0f;
                // Bekleme süresi artık Inspector'dan dinamik geliyor (Örn: 4-6 saniye arası derin nefes)
                bb.globalCooldownTimer = Random.Range(bb.minComboCooldown, bb.maxComboCooldown); 
                bb.currentMode = "Backstep"; // Kombo bitince direkt psikolojik olarak geri adım atsın
            }
        }

        return NodeState.RUNNING;
    }
}

public class PatrolNode : Node {
    private SkeletonBlackboard bb;
    public PatrolNode(SkeletonBlackboard b) => bb = b;

    public override NodeState Evaluate() {
        if (bb.waypoints == null || bb.waypoints.Count == 0) return NodeState.FAILURE;
        
        bb.agent.isStopped = false;
        bb.agent.speed = bb.walkSpeed;
        Transform target = bb.waypoints[bb.currentWaypointIndex];
        bb.agent.SetDestination(target.position);
        
        bb.animator.SetFloat("Speed", 1.0f); 

        if (Vector3.Distance(bb.transform.position, target.position) < 1.5f) {
            bb.currentWaypointIndex = Random.Range(0, bb.waypoints.Count);
        }
        return NodeState.RUNNING;
    }
}