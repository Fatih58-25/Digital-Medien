using UnityEngine;
using Articy.Unity;

// Szenario 1 (Kaempfen-Pfad): Wird ausgeloest, sobald Malakor stirbt (EnemyBase.onDefeated).
// Laesst Hekate hinter dem Spieler erscheinen und startet automatisch (ohne "Sprechen (E)")
// ihren Verrats-Dialog.
//
// Setup:
// 1) Dieses Script auf ein beliebiges Objekt packen (z.B. auf Hekate selbst oder einen Manager).
// 2) Feld "Hekate Object" auf Hekates Root-GameObject ziehen (das, was sonst per SetActive
//    versteckt/gezeigt wird).
// 3) Feld "Player Transform" auf den Spieler ziehen (oder leer lassen, wird automatisch per
//    Tag "Player" gesucht).
// 4) Feld "Betrayal Dialogue Start" auf den ersten Knoten von Hekates Verrats-Dialog in
//    articy:draft ziehen.
// 5) Am Malakor-Objekt (Component EnemyBase) unter "On Defeated": dieses Objekt ->
//    HekateBetrayalTrigger.TriggerBetrayal().
// 6) Malakors "Is Final Boss" MUSS deaktiviert sein, sonst kommt sofort Victory statt Verrat.
public class HekateBetrayalTrigger : MonoBehaviour
{
    [Tooltip("Hekates Root-GameObject (wird hier aktiviert/positioniert).")]
    public GameObject hekateObject;

    [Tooltip("Transform des Spielers. Leer lassen, um automatisch per Tag 'Player' zu suchen.")]
    public Transform playerTransform;

    [Tooltip("Abstand hinter dem Spieler, an dem Hekate erscheint (nur genutzt, wenn Appear Point leer ist).")]
    public float appearDistance = 3f;

    [Tooltip("Fester Punkt in der Szene, an dem Hekate sauber aufrecht erscheinen soll. Wenn gesetzt, wird NICHT relativ zum Spieler berechnet, sondern genau diese Position/Rotation uebernommen (empfohlen).")]
    public Transform appearPoint;

    [Tooltip("Startknoten von Hekates Verrats-Dialog in articy:draft.")]
    public ArticyRef betrayalDialogueStart;

    [Tooltip("Hekates eigenes NPCInteractable (Sprechen (E)). Wird deaktiviert, damit es nicht gleichzeitig mit dem automatischen Dialogstart kollidiert.")]
    public NPCInteractable npcInteractable;

    private bool alreadyTriggered = false;

    public void TriggerBetrayal()
    {
        Debug.Log($"[HekateBetrayalTrigger] TriggerBetrayal() aufgerufen auf '{name}'. alreadyTriggered={alreadyTriggered}, hekateObject zugewiesen={hekateObject != null}, playerTransform zugewiesen={playerTransform != null}, betrayalDialogueStart zugewiesen={betrayalDialogueStart != null}");

        if (alreadyTriggered) return;
        alreadyTriggered = true;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (hekateObject != null)
        {
            if (appearPoint != null)
            {
                hekateObject.transform.position = appearPoint.position;
                hekateObject.transform.rotation = appearPoint.rotation;
            }
            else if (playerTransform != null)
            {
                Vector3 flatForward = playerTransform.forward;
                flatForward.y = 0f;
                if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
                flatForward.Normalize();

                Vector3 behindPlayer = playerTransform.position - flatForward * appearDistance;
                hekateObject.transform.position = behindPlayer;
                hekateObject.transform.rotation = Quaternion.LookRotation(flatForward);
            }

            hekateObject.SetActive(true);
            Debug.Log($"[HekateBetrayalTrigger] hekateObject '{hekateObject.name}' aktiviert, activeSelf={hekateObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[HekateBetrayalTrigger] hekateObject ist NICHT zugewiesen!");
        }

        // Verhindert, dass der Spieler waehrenddessen per "Sprechen (E)" einen zweiten,
        // ueberschneidenden Dialog auf demselben ArticyFlowPlayer startet.
        if (npcInteractable != null)
            npcInteractable.enabled = false;

        if (DialogueUIController.Instance != null && betrayalDialogueStart != null)
        {
            DialogueUIController.Instance.StartDialogue(betrayalDialogueStart.GetObject());
        }
        else
        {
            Debug.LogWarning("[HekateBetrayalTrigger] Dialog konnte nicht gestartet werden, DialogueUIController.Instance oder betrayalDialogueStart fehlt.");
        }
    }
}
