using System.Linq;
using GenericBehaviorTree;
using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTreeTest
{
  public class WalkToEnemy : Node
  {
    private Transform transform;
    private readonly string targetTag = "Enemy";
    private float eyeSight = 30f;
    private NavMeshAgent agent;

    public WalkToEnemy(Transform _transform)
    {
      transform = _transform;
      agent = _transform.GetComponent<NavMeshAgent>();
    }


    public override NodeState Evaluate()
    {
      // Get the target by the tag name that is stored in Tree data;
      Transform target = GetData(targetTag) as Transform;

      // If the target is null, return failure 
      // Failure means that the node has finished and the parent should skip this sequence and move to the next one.
      if (target == null)
      {
        return NodeState.FAILURE;
      }


      // If the target is not null, set the destination of the NavMeshAgent to the target position.
      if (target != null)
      {
        agent.SetDestination(target.position);

        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
          // If the agent is close enough to the target, clear the path to stop the agent from moving.
          agent.ResetPath();
          // Success means that this node has finished and the parent should move to the next node in sequence.
          return NodeState.SUCCESS;
        }
      }

      // If the target is not null, but the agent is not close enough to the target, return running.
      // Running means that this node has not finished yet and the parent should keep evaluating this node. 
      return NodeState.RUNNING;
    }
  }
}