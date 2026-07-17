using UnityEngine;
namespace GenericBehaviorTreeTest
{

  public class Team : MonoBehaviour
  {
    public ETeam team = ETeam.RED;
    public float health = 100f;

    public void TakeDamage(float damage)
    {
      health -= damage;
      if (health <= 0)
      {
        Destroy(gameObject);
      }
    }
    private void OnDrawGizmos()
    {
      Gizmos.color = team == ETeam.RED ? Color.red : Color.green;
      Gizmos.DrawWireSphere(transform.position, 30f);
    }
  }

}