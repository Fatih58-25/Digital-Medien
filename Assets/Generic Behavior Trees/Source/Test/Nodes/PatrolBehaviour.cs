using GenericBehaviorTree;
using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTreeTest
{
    public class PatrolBehaviour : Node

    {
        private Transform transform;
        private NavMeshAgent agent;
        private bool isPatrolling = false;
        private float idleCounter = 0;
        private float idleTime = 5f;
        private bool hasPath = false;

        // Node is not a MonoBehaviour, so we can't use it's features.
        // We use the constructor to pass the values that the node needs to evaluate.
        public PatrolBehaviour(Transform _transform)
        {
            transform = _transform;
            agent = transform.GetComponent<NavMeshAgent>();
        }

        public override NodeState Evaluate()
        {

            // If the agent is not enabled, return failure
            // Failure means that the node has finished and the parent should skip this sequence and move to the next one.       
            if (!agent.enabled)
            {
                return NodeState.FAILURE;
            }
            // If the agent is enabled, check if the agent is patrolling.
            if (!isPatrolling)
            {
                // If the agent is not patrolling, increase the idle counter.
                idleCounter += Time.deltaTime;
                // If the idle counter is greater than the idle time, set the agent to patrolling.
                if (idleCounter >= idleTime)
                {
                    idleCounter = 0;
                    isPatrolling = true;
                }
            }
            else
            {
                if (isPatrolling && !hasPath)
                {
                    // If the agent is patrolling, set the destination to a random waypoint.
                    hasPath = agent.SetDestination(Utils.GetRandomWayPoint(10, agent));
                }
                // If the agent has a path, check if the agent has reached the destination.
                if (hasPath && agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathComplete)
                {
                    // If the agent has reached the destination, clear the path and set the agent to not patrolling.
                    hasPath = false;
                    isPatrolling = false;
                    agent.isStopped = true;
                    agent.ResetPath();
                }

            }


            return NodeState.RUNNING;
        }
    }
}