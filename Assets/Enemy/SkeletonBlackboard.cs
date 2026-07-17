using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SkeletonBlackboard : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;
    // HATA BURADA MI? ŞUNU TAM KOPYALA:
    public List<Transform> waypoints = new List<Transform>(); 
    public int currentWaypointIndex = 0;

    public float walkSpeed = 1.5f;
public float runSpeed = 4.0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }
}