using System.Collections;
using UnityEngine;

// Auf jedes Teleporter-Objekt packen (z.B. eine flache Plattform/ein Portal in der Welt).
// Braucht einen Trigger-Collider (Is Trigger = true) auf diesem GameObject oder einem Kind,
// sowie ein GameObject mit Tag "Player" und einem Collider fuer die Ueberschneidung.
//
// Setup in der Szene:
// 1) Teleporter-GameObject mit Collider (Is Trigger = true) an die gewuenschte Stelle setzen.
// 2) Leeres GameObject an der Zielposition erstellen, ins Feld "Destination" ziehen.
// 3) Optional: UI-Text "Teleportieren (E)" erstellen (wie beim NPC-Sprechen-Hinweis), ins Feld
//    "Interaction Prompt" ziehen. Kann ein eigenes Objekt sein oder (mit anderem Text) getrennt
//    vom NPC-Hinweis gehalten werden, da der Text hier anders lautet.
// 4) Optional: Fade Canvas Group (z.B. eine eigene schwarze CanvasGroup wie beim Intro) fuer einen
//    kurzen Fade-Effekt waehrend des Teleports zuweisen. Ohne Zuweisung wird sofort teleportiert.
[RequireComponent(typeof(Collider))]
public class Teleporter : MonoBehaviour
{
    [Tooltip("Zielposition, zu der teleportiert wird")]
    public Transform destination;

    [Tooltip("Taste, mit der teleportiert wird")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Optional: Hinweis-UI (z.B. \"Teleportieren (E)\")")]
    public GameObject interactionPrompt;

    [Header("Optional: Fade-Effekt waehrend des Teleports")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.4f;

    private bool playerInRange;
    private bool isTeleporting;

    void Update()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(playerInRange && !isTeleporting);

        if (playerInRange && !isTeleporting && Input.GetKeyDown(interactKey))
        {
            if (destination == null)
            {
                Debug.LogWarning($"Teleporter '{name}': kein Ziel (Destination) im Inspector zugewiesen.");
                return;
            }

            StartCoroutine(TeleportRoutine());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
    }

    private IEnumerator TeleportRoutine()
    {
        isTeleporting = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            isTeleporting = false;
            yield break;
        }

        CharacterController controller = player.GetComponent<CharacterController>();

        if (fadeCanvasGroup != null)
            yield return Fade(0f, 1f);

        // CharacterController kurz deaktivieren, damit das direkte Setzen der Position
        // nicht mit der internen Kollisions-/Bewegungsberechnung kollidiert.
        if (controller != null) controller.enabled = false;

        player.transform.position = destination.position;
        player.transform.rotation = destination.rotation;
        Physics.SyncTransforms();

        if (controller != null) controller.enabled = true;

        if (fadeCanvasGroup != null)
            yield return Fade(1f, 0f);

        isTeleporting = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = to;

        if (to <= 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }
}
