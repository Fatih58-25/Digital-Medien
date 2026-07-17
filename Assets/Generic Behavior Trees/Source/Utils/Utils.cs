using System.Numerics;
using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTree
{
  public static class Utils
  {

    private static UnityEngine.Vector3 direction;
    private static NavMeshHit navHit;
    public static UnityEngine.Vector3 GetRandomWayPoint(float radius, NavMeshAgent agent)
    {
      direction = Random.insideUnitSphere * radius;
      direction += agent.transform.position;
      NavMesh.SamplePosition(direction, out navHit, radius, NavMesh.AllAreas);
      return navHit.position;
    }

  }
}