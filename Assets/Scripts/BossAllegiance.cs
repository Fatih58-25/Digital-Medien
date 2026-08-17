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
// 6) Sobald ihr den echten Hekate-Kampf separat triggert (GameState.HekateFightStarted,
//    am Ende von Hekates Wahrheits-Konfrontation), dort JoinFightAgainst(hekateTransform)
//    auf dieses Objekt haengen. Silas ist seit BecomeAlly() unsichtbar (SetActive false)
//    und taucht in JoinFightAgainst() ploetzlich wieder auf (optional an "Reappear Point").
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

    [Header("Verschwinden / Wiederauftauchen (statt Folgen)")]
    [Tooltip("Transform des Spielers. Leer lassen, um automatisch per Tag 'Player' zu suchen.")]
    public Transform playerTransform;
    [Tooltip("Abstand, den Silas beim (nicht mehr genutzten) Folgen zum Spieler haelt.")]
    public float followDistance = 3f;
    [Tooltip("Wo Silas wieder auftaucht, wenn der Hekate-Kampf beginnt. Leer lassen, um an der aktuellen Position aufzutauchen.")]
    public Transform reappearPoint;

    [Header("Als Verbuendeter kaempfen (JoinFightAgainst)")]
    [Tooltip("Layer, auf dem der GEGNER liegt, gegen den er als Verbuendeter kaempft (z.B. Hekates wahre Gestalt). Verhindert, dass seine Angriffe stattdessen den echten Spieler treffen.")]
    public LayerMask allyAttackTargetLayer;

    private enum State { Neutral, Following, Fighting }
    private State state = State.Neutral;
    private NavMeshAgent agent;
    private EnemyBase enemyBase;

    // Wird von HekateBetrayalTrigger geprueft: der automatische Verrats-Dialog soll nur
    // laufen, wenn der Spieler Malakor besiegt hat, NICHT wenn er als Verbuendeter im
    // Kampf gegen Hekate stirbt.
    public bool BecameAlly { get; private set; } = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyBase = GetComponent<EnemyBase>();
    }

    // agent.isStopped wirft einen Fehler, wenn der Agent (noch) nicht auf einem NavMesh
    // platziert ist (z.B. direkt nachdem das Objekt wieder aktiviert wurde). Diese Methode
    // verhindert, dass so ein Fehler den Rest von BecomeHostile/BecomeAlly/JoinFightAgainst abbricht.
    private void SetAgentStopped(bool stopped)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning($"[BossAllegiance] SetAgentStopped({stopped}) uebersprungen auf '{name}': agent zugewiesen={agent != null}, aktiv={agent != null && agent.isActiveAndEnabled}, isOnNavMesh={agent != null && agent.isOnNavMesh}");
            return;
        }
        agent.isStopped = stopped;
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
        SetAgentStopped(false);
        if (npcInteractable != null) npcInteractable.enabled = false;
    }

    // Wird aufgerufen, wenn sich der Spieler im Dialog dafuer entscheidet, ihn als Verbuendeten zu gewinnen.
    public void BecomeAlly()
    {
        state = State.Following;
        BecameAlly = true;

        if (aiScript != null) aiScript.enabled = false;
        SetAgentStopped(false);

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

        // Als Verbuendeter zeigt seine eigene KI nicht mehr seine eigene Boss-Healthbar an.
        if (enemyBase != null) enemyBase.SetIsBoss(false);

        // Silas verschwindet, statt sichtbar zu folgen. Er taucht erst beim echten
        // Hekate-Kampf ueber JoinFightAgainst() wieder auf.
        gameObject.SetActive(false);
    }

    // Wird aufgerufen, sobald der eigentliche Hekate-Kampf beginnt (eigener Trigger noetig,
    // siehe Kommentar oben). Nur sinnvoll, wenn Silas vorher per BecomeAlly() Verbuendeter wurde.
    public void JoinFightAgainst(Transform enemyTarget)
    {
        Debug.Log($"[BossAllegiance] JoinFightAgainst() auf '{name}' aufgerufen. state={state}, enemyTarget zugewiesen={enemyTarget != null}");

        if (state != State.Following) return; // war nie Verbuendeter -> nichts zu tun

        state = State.Fighting;

        // Silas war versteckt (siehe BecomeAlly) und taucht jetzt ploetzlich wieder auf.
        if (reappearPoint != null)
        {
            transform.position = reappearPoint.position;
            transform.rotation = reappearPoint.rotation;
        }
        gameObject.SetActive(true);

        if (blackboard != null)
            blackboard.player = enemyTarget; // KI-Combat-Nodes nutzen bb.player als Angriffsziel

        // Angriffs-Treffercheck auf den Gegner-Layer umstellen, sonst trifft er weiterhin
        // den echten Spieler (playerLayer in EnemyBase bleibt sonst auf "Player" stehen).
        if (enemyBase != null && allyAttackTargetLayer.value != 0)
            enemyBase.SetAttackTargetLayer(allyAttackTargetLayer);

        if (aiScript != null) aiScript.enabled = true;
        SetAgentStopped(false);

        // Waehrend des echten Kampfes soll kein Dialog mehr mit ihm moeglich sein.
        if (npcInteractable != null) npcInteractable.enabled = false;
    }
}
