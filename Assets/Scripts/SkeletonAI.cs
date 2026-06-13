using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class SkeletonAI : MonoBehaviour
{
    [Header("Navigation")]
    public List<Transform> waypoints; 
    private NavMeshAgent agent;
    private Transform currentTarget;

    [Header("Movement Speeds")]
    public float walkSpeed = 1.5f; // Devriye gezerken Crucible Knight ağırlığı (Yavaş yürüyüş)
    public float runSpeed = 4.5f;  // Oyuncuyu kovalarken (Şimdilik devriyede yürütüyoruz)

    [Header("Animation")]
    private string speedParam = "Speed"; 
    private Animator animator;

    [Header("AI Logic Variables")]
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Başlangıçta hızı YÜRÜME hızına sabitle
        agent.speed = walkSpeed;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        if (waypoints != null && waypoints.Count > 0)
        {
            GoToRandomWaypoint();
        }
    }

    void Update()
    {
        if (isWaiting || agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            StartCoroutine(DecideNextMove());
        }

        // ANIMATOR FEEDBACK: 
        // Şövalye hareket ediyorsa, animatördeki Speed değerini yumuşakça 0.5f (Walk) yapıyoruz.
        // Duruyorsa 0f (Idle) yapıyoruz.
        if (agent.velocity.magnitude > 0.1f)
        {
            // Eğer devriyedeysek hızı 0.5f'e sabitliyoruz ki Koşma (Run) tetiklenmesin!
            animator.SetFloat(speedParam, 0.5f, 0.1f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat(speedParam, 0f, 0.1f, Time.deltaTime);
        }
    }

    void GoToRandomWaypoint()
    {
        Transform nextTarget = currentTarget;
        if (waypoints.Count > 1)
        {
            while (nextTarget == currentTarget)
            {
                int randomIndex = Random.Range(0, waypoints.Count);
                nextTarget = waypoints[randomIndex];
            }
        }
        else
        {
            nextTarget = waypoints[0];
        }

        currentTarget = nextTarget;
        
        // Noktaya giderken hızı yine garanti olsun diye yürümeye eşitle
        agent.speed = walkSpeed; 
        agent.SetDestination(currentTarget.position);
    }

    IEnumerator DecideNextMove()
    {
        isWaiting = true;
        animator.SetFloat(speedParam, 0f);
        agent.ResetPath(); 

        int chance = Random.Range(0, 100);
        float waitTime = 2f;

        if (chance < 40) { waitTime = 4.0f; }
        else if (chance >= 40 && chance < 80) { waitTime = 1.5f; }
        else { waitTime = 0.1f; }

        yield return new WaitForSeconds(waitTime);

        GoToRandomWaypoint();
        isWaiting = false;
    }
}