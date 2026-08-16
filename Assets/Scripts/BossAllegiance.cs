using UnityEngine;
using UnityEngine.AI;

// Auf einen Boss packen, der optional zum Verbuendeten werden kann (z.B. Silas/Malakor),
// statt bekaempft zu werden. Wird ueber UnityEvents aus DialogueUIController aufgerufen
// (siehe onStartBossFightMalakor / onStartBossFightHekate im Inspector des DialogueController).
//
// Setup:
// 1) Dieses Script auf den Boss packen (z.B. Silas).
// 2) Feld "Ai Script" auf die jeweilige KI-Komponente ziehen (bei Silas: Demon Boss AI).
// 3) Feld "Blackboard" auf die Skeleton-Blackboard-Komponente desselben Objekts ziehen
//    (die hat das public Transform-Feld "player", das die KI als Angriffsziel benutzt).
// 4) Im Idle-/Dialog-Zustand die KI-Komponente selbst im Inspector deaktivieren (Haekchen weg),
//    damit er waehrend des Redens nicht angreift.
// 5) Am DialogueController-Objekt (Component DialogueUIController):
//    - On Start Boss Fight Malakor -> dieses Silas-Objekt -> BossAllegiance.BecomeHostile()
//    - On Start Boss Fight Hekate  -> dieses Silas-Objekt -> BossAllegiance.BecomeAlly()
// 6) Sobald ihr den echten Hekate-Kampf separat triggert (z.B. eigenes GameState-Flag
//    "HekateFightStarted", da StartBossFightHekate schon fuer BecomeAlly verbraucht ist),
//    dort JoinFightAgainst(hekateTransform) auf dieses Objekt haengen.
[RequireComponent(typeof(NavMeshAgent))]
public class BossAllegiance : MonoBehaviour
{
    [Tooltip("Die KI-Komponente dieses Bosses, z.B. DemonBossAI, HammerBossAI, CrucibleAI.")]
    public MonoBehaviour aiScript;

    [Tooltip("Die Blackboard-Komponente desselben Objekts (SkeletonBlackboard), steuert das Angriffsziel.")]
    public SkeletonBlackboard blackboard;

    [Tooltip("Optional: NPCInteractable, damit nach der Entscheidung kein erneuter Dialog mehr moeglich ist.")]
    public NPCInteractable npcInteractable;

    [Header("Verbuendeter (BecomeAlly)")]
    [Tooltip("Tag, den der Boss als Verbuendeter bekommt (leer lassen, um Tag nicht zu aendern).")]
    public string allyTag = "Untagged";
    [Tooltip("Layer-Name, den der Boss als Verbuendeter bekommt (leer lassen, um Layer nicht zu aendern).")]
    public string allyLayer = "Default";

    [Header("Begleiten (solange Verbuendeter, aber noch kein Kampf)")]
    [Tooltip("Transform des Spielers, dem gefolgt wird. Leer lassen, um automatisch per Tag 'Player' zu suchen.")]
    public Transform playerTransform;
    [Tooltip("Abstand, den Silas beim Folgen zum Spieler haelt.")]
    public float followDistance = 3f;

    private enum State { Neutral, Following, Fighting }
    private State state = State.Neutral;
    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (state != State.Following) return;
        if (playerTransform == null || agent == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (Time.frameCount % 60 == 0)
            Debug.Log($"[BossAllegiance] Update (Following): dist={dist:F1}, agent.enabled={agent.enabled}, agent.isOnNavMesh={agent.isOnNavMesh}, agent.isStopped={agent.isStopped}");

        if (dist > followDistance)
            agent.SetDestination(playerTransform.position);
        else
            agent.ResetPath();
    }

    // Wird aufgerufen, wenn sich der Spieler im Dialog fuer den Kampf entscheidet.
    public void BecomeHostile()
    {
        Debug.Log($"[BossAllegiance] BecomeHostile() auf '{name}' aufgerufen. aiScript zugewiesen: {aiScript != null}, agent zugewiesen: {agent != null}");

        state = State.Fighting;

        if (aiScript != null) aiScript.enabled = true;
        if (agent != null) agent.isStopped = false;
        if (npcInteractable != null) npcInteractable.enabled = false;
    }

    // Wird aufgerufen, wenn sich der Spieler im Dialog dafuer entscheidet, ihn als Verbuendeten zu gewinnen.
    public void BecomeAlly()
    {
        state = State.Following;

        if (aiScript != null) aiScript.enabled = false;
        if (agent != null) agent.isStopped = false;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        Debug.Log($"[BossAllegiance] BecomeAlly() auf '{name}' aufgerufen. agent zugewiesen: {agent != null}, agent.enabled: {agent?.enabled}, agent.isOnNavMesh: {agent?.isOnNavMesh}, playerTransform zugewiesen: {playerTransform != null}");

        if (!string.IsNullOrEmpty(allyTag))
            gameObject.tag = allyTag;

        if (!string.IsNullOrEmpty(allyLayer))
        {
            int layerIndex = LayerMask.NameToLayer(allyLayer);
            if (layerIndex >= 0) gameObject.layer = layerIndex;
        }

        // Weiterhin ansprechbar lassen (z.B. fuer einen kleinen Abschieds-/Verbuendeten-Dialog),
        // daher NPCInteractable hier absichtlich NICHT deaktiviert.
    }

    // Wird aufgerufen, sobald der eigentliche Hekate-Kampf beginnt (eigener Trigger noetig,
    // siehe Kommentar oben). Nur sinnvoll, wenn Silas vorher per BecomeAlly() Verbuendeter wurde.
    public void JoinFightAgainst(Transform enemyTarget)
    {
        if (state != State.Following) return; // war nie Verbuendeter -> nichts zu tun

        state = State.Fighting;

        if (blackboard != null)
            blackboard.player = enemyTarget; // KI-Combat-Nodes nutzen bb.player als Angriffsziel

        if (aiScript != null) aiScript.enabled = true;
        if (agent != null) agent.isStopped = false;
    }
}
