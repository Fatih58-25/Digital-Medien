using UnityEngine;

// Auf einen Boss packen, der zwischen mehreren moeglichen Gegnern (z.B. Spieler und
// verbuendeter Silas) das jeweils naeheste als Angriffsziel waehlen soll. Setzt dafuer
// laufend blackboard.player auf das naehere der beiden Ziele, die KI (DemonBossAI usw.)
// greift ja bereits automatisch bb.player an und dreht sich zu ihm, es muss also nichts
// an der eigentlichen Kampf-KI geaendert werden.
//
// Setup:
// 1) Dieses Script auf Hekates wahre Gestalt packen (Hexe_Transform (1)).
// 2) Feld "Blackboard" auf ihre eigene SkeletonBlackboard-Komponente ziehen.
// 3) Feld "Player Transform" auf den Spieler ziehen (oder leer lassen, automatische Suche
//    per Tag "Player").
// 4) Feld "Ally Transform" auf Silas ziehen (optional, wenn leer wird immer der Spieler
//    als Ziel genutzt, z.B. im Kaempfen-Pfad ohne Silas).
public class NearestTargetSelector : MonoBehaviour
{
    [Tooltip("Die eigene Blackboard-Komponente (SkeletonBlackboard), deren 'player'-Feld hier laufend aktualisiert wird.")]
    public SkeletonBlackboard blackboard;

    [Tooltip("Der Spieler. Leer lassen, um automatisch per Tag 'Player' zu suchen.")]
    public Transform playerTransform;

    [Tooltip("Der verbuendete Boss (z.B. Silas), falls vorhanden. Leer lassen, wenn kein Verbuendeter mitkaempft.")]
    public Transform allyTransform;

    [Tooltip("Wie oft pro Sekunde neu geprueft wird, wer naeher ist (muss nicht jeden Frame sein).")]
    public float checkInterval = 0.25f;

    private float timer = 0f;

    void Update()
    {
        if (blackboard == null) return;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        // Nur lebende/aktive Ziele in Betracht ziehen.
        bool allyValid = allyTransform != null && allyTransform.gameObject.activeInHierarchy;
        bool playerValid = playerTransform != null && playerTransform.gameObject.activeInHierarchy;

        if (!playerValid && !allyValid) return;

        if (!allyValid)
        {
            blackboard.player = playerTransform;
            return;
        }

        if (!playerValid)
        {
            blackboard.player = allyTransform;
            return;
        }

        float distToPlayer = Vector3.SqrMagnitude(transform.position - playerTransform.position);
        float distToAlly = Vector3.SqrMagnitude(transform.position - allyTransform.position);

        blackboard.player = (distToAlly < distToPlayer) ? allyTransform : playerTransform;
    }
}
