using System.Linq;
using GenericBehaviorTree;
using UnityEngine;

namespace GenericBehaviorTreeTest
{
  public class LookingForEnemy : Node
  {
    private Transform transform;
    private readonly string targetTag = "Enemy";
    private float eyeSight = 30f;

    public LookingForEnemy(Transform _transform)
    {
      transform = _transform;
    }


    public override NodeState Evaluate()
    {
      // Get the target by the tag name that is stored in Tree data;
      Transform target = GetData(targetTag) as Transform;

      // If the target is not null, return success
      // Success means that this node has finished and the parent should move to the next node in sequence.
      if (target != null)
      {
        return NodeState.SUCCESS;
      }
      else
      {
        // If the target is null, look for a new target.
        Team newTarget = Physics.OverlapSphere(transform.position, eyeSight)
          .Select(c => c.GetComponent<Team>())
          .FirstOrDefault(f => f != null && f.team != transform.GetComponent<Team>().team);

        // If a new target is found, set the target in the Tree data and return success.
        if (newTarget != null)
        {
          // Set the target in the Tree data
          // The target is stored in the Tree data so that other nodes can access it.
          // Storing the target in the Tree data allows the nodes to communicate with each other.
          parent.parent.SetData(targetTag, newTarget.transform);
          return NodeState.SUCCESS;
        }
      }

      // If no target is found, return failure.
      // Failure means that the node has finished and the parent should skip this sequence and move to the next one.
      return NodeState.FAILURE;
    }
  }
}