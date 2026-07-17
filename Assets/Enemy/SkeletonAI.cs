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
        return Vector3.Distance(bb.transform.position, bb.player.position) < 10f ? NodeState.SUCCESS : NodeState.FAILURE;
    }
}

public class CombatNode : Node {
    private SkeletonBlackboard bb;
    private float walkRange = 3.0f; 
    private float runRange = 8.0f;  
    private float attackRange = 2.0f; 

    public CombatNode(SkeletonBlackboard b) => bb = b;
    public override NodeState Evaluate() {
        float dist = Vector3.Distance(bb.transform.position, bb.player.position);

        if (dist <= attackRange) {
            bb.agent.isStopped = true;
            bb.animator.SetFloat("Speed", 0f);
            bb.animator.SetTrigger("Attack");
            return NodeState.SUCCESS;
        } 
        else if (dist <= walkRange) {
            bb.agent.isStopped = false;
            bb.agent.speed = bb.walkSpeed;
            bb.agent.SetDestination(bb.player.position);
            // Yürüme hızı: 1.0 (Animator'daki 1.5 eşiğinin altında)
            bb.animator.SetFloat("Speed", 1.0f); 
            return NodeState.RUNNING;
        }
        else if (dist <= runRange) {
            bb.agent.isStopped = false;
            bb.agent.speed = bb.runSpeed;
            bb.agent.SetDestination(bb.player.position);
            // Koşma hızı: 2.0 (Animator'daki 1.5 eşiğinin üstünde)
            bb.animator.SetFloat("Speed", 2.0f); 
            return NodeState.RUNNING;
        }
        return NodeState.FAILURE;
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
        
        // Devriyede yavaş yürüsün: 1.0
        bb.animator.SetFloat("Speed", 1.0f);

        if (Vector3.Distance(bb.transform.position, target.position) < 1.5f) {
            bb.currentWaypointIndex = Random.Range(0, bb.waypoints.Count);
        }
        return NodeState.RUNNING;
    }
}