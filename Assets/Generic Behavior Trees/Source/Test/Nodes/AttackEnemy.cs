using System.Linq;
using GenericBehaviorTree;
using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTreeTest
{
  public class AttackEnemy : Node
  {
    private Transform transform;
    private readonly string targetTag = "Enemy";
    private NavMeshAgent agent;
    private float hitRate = 2f;
    private float hitCounter = 2f;

    public AttackEnemy(Transform _transform)
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
      // If the target is not null, calculate the distance between the agent and the target.
      float distance = Vector3.Distance(transform.position, target.position);

      // If the distance is greater than the stopping distance of the agent, clear the target from the tree data and return failure. 
      // Failure means that the node has finished and the parent should skip this sequence and move to the next one.
      if (distance > agent.stoppingDistance)
      {
        ClearData(targetTag);
        return NodeState.FAILURE;
      }

      // Check if the hit counter is less than the hit rate.
      // This will limit the number of hits per second.
      if (hitCounter < hitRate)
      {
        hitCounter += Time.deltaTime;
      }
      else
      {
        Debug.Log("Attacking");
        // If the hit counter is greater than the hit rate, reset the hit counter and deal damage to the target.
        hitCounter = 0;
        target.GetComponent<Team>().TakeDamage(Random.Range(10, 20));
        // Success means that this node has finished and the parent should move to the next node in sequence.
        return NodeState.SUCCESS;
      }

      // Running means that this node has not finished yet and the parent should keep evaluating this node.
      return NodeState.FAILURE;
    }
  }
}