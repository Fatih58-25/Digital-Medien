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

    [Tooltip("Abstand hinter dem Spieler, an dem Hekate erscheint.")]
    public float appearDistance = 3f;

    [Tooltip("Startknoten von Hekates Verrats-Dialog in articy:draft.")]
    public ArticyRef betrayalDialogueStart;

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
            if (playerTransform != null)
            {
                Vector3 behindPlayer = playerTransform.position - playerTransform.forward * appearDistance;
                hekateObject.transform.position = behindPlayer;
                hekateObject.transform.rotation = Quaternion.LookRotation(playerTransform.position - behindPlayer);
            }

            hekateObject.SetActive(true);
            Debug.Log($"[HekateBetrayalTrigger] hekateObject '{hekateObject.name}' aktiviert, activeSelf={hekateObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[HekateBetrayalTrigger] hekateObject ist NICHT zugewiesen!");
        }

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
