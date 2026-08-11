using UnityEngine;
using Articy.Unity;

// Auf jeden dialogfaehigen NPC packen (Hekate, Corven, Imeth, Malakor).
// dialogueStart zeigt im Inspector auf den jeweiligen Startknoten in articy:draft
// (z.B. HEK_010 fuer Hekate, COR_A_010/COR_B_010 fuer Corven - die A/B-Weiche
// uebernimmt die Condition auf dem ersten Pin in articy:draft, nicht dieses Script).
//
// Braucht einen Trigger-Collider (Is Trigger = true) auf diesem GameObject oder einem Kind
// sowie ein GameObject mit Tag "Player" und einem (ggf. leeren) Collider fuer die Ueberschneidung.
[RequireComponent(typeof(Collider))]
public class NPCInteractable : MonoBehaviour
{
    [Tooltip("Startknoten des Dialogs in articy:draft (Dialogue, Hub oder erstes DialogueFragment)")]
    public ArticyRef dialogueStart;

    [Tooltip("Taste, mit der der Spieler das Gespraech startet")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Optional: Hinweis-UI (z.B. \"E zum Reden\")")]
    public GameObject interactionPrompt;

    private bool playerInRange;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            DialogueUIController.Instance.StartDialogue(dialogueStart.GetObject());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
}
