using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Articy.Unity;
using Articy.Unity.Interfaces;

// Zentrale Dialog-Steuerung fuer die Souls-like Dialoge (Hekate, Corven, Imeth, Malakor).
// Baut auf denselben Interfaces auf, die ArticyDebugFlowPlayer.cs (Assets/ArticyImporter/Helper/Scripts)
// in diesem Projekt bereits verwendet -> garantiert kompatibel mit der hier installierten Importer-Version.
//
// Setup in der Szene:
// 1) Leeres GameObject "DialogueController" erstellen.
// 2) Component ArticyFlowPlayer hinzufuegen.
// 3) Dieses Script (DialogueUIController) auf dasselbe GameObject packen.
// 4) UI-Panel (Canvas) mit speakerLabel, dialogueText, choiceButtonPrefab und choiceButtonContainer
//    im Inspector verknuepfen. choiceButtonPrefab braucht selbst nur ein Text/TMP-Kind + Button-Component.
[RequireComponent(typeof(ArticyFlowPlayer))]
public class DialogueUIController : MonoBehaviour, IArticyFlowPlayerCallbacks
{
    public static DialogueUIController Instance { get; private set; }

    [Header("UI-Referenzen")]
    public GameObject dialoguePanel;
    public Text speakerLabel;
    public Text dialogueText;
    public Button choiceButtonPrefab;
    public Transform choiceButtonContainer;

    private ArticyFlowPlayer flowPlayer;

    void Awake()
    {
        Instance = this;
        flowPlayer = GetComponent<ArticyFlowPlayer>();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    // Wird von NPCInteractable.cs aufgerufen, wenn der Spieler mit einem NPC interagiert.
    public void StartDialogue(IArticyObject startNode)
    {
        if (startNode == null)
        {
            Debug.LogWarning("DialogueUIController: startNode ist null - ArticyRef im Inspector des NPCs gesetzt?");
            return;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        flowPlayer.StartOn = startNode;
        flowPlayer.Play();
    }

    // Wird vom ArticyFlowPlayer aufgerufen, sobald er auf einem "PauseOn"-Objekt (z.B. DialogueFragment) pausiert.
    public void OnFlowPlayerPaused(IFlowObject aObject)
    {
        if (aObject == null)
        {
            // Dead End erreicht -> keine weiteren Verbindungen, Dialog ist zu Ende.
            EndDialogue();
            return;
        }

        // Sprecher-Name anzeigen (Pattern aus ArticyDebugFlowPlayer.cs).
        if (aObject is IObjectWithDisplayName objWithDisplayName)
            speakerLabel.text = objWithDisplayName.DisplayName;
        else if (aObject is IObjectWithLocalizableDisplayName objWithLocDisplayName)
            speakerLabel.text = objWithLocDisplayName.DisplayName;
        else if (aObject is IObjectWithSpeaker objWithSpeaker && objWithSpeaker.Speaker is IObjectWithDisplayName speakerName)
            speakerLabel.text = speakerName.DisplayName;
        else
            speakerLabel.text = string.Empty;

        // Gesprochenen Text anzeigen.
        if (aObject is IObjectWithText objWithText)
            dialogueText.text = objWithText.Text;
        else if (aObject is IObjectWithLocalizableText objWithLocText)
            dialogueText.text = objWithLocText.Text;
        else
            dialogueText.text = string.Empty;
    }

    // Wird direkt NACH OnFlowPlayerPaused aufgerufen und liefert alle moeglichen Folgeknoten (Branches).
    public void OnBranchesUpdated(IList<Branch> aBranches)
    {
        ClearChoiceButtons();

        var validBranches = new List<Branch>();
        foreach (var branch in aBranches)
        {
            if (branch.IsValid) // ungueltige Branches wurden per Condition ausgeschlossen (z.B. Player.trustedHekate == false)
                validBranches.Add(branch);
        }

        // Genau EIN gueltiger Folgeknoten ohne eigenen Menu-Text -> kein sinnloser "Weiter"-Button,
        // sondern automatisch weiterspielen (z.B. reine Erzaehl-Ketten wie HEK_010 -> HEK_020).
        if (validBranches.Count == 1 && string.IsNullOrEmpty(GetMenuText(validBranches[0])))
        {
            flowPlayer.Play(validBranches[0]);
            return;
        }

        foreach (var branch in validBranches)
        {
            var button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = GetMenuText(branch);

            // WICHTIG: lokale Kopie der Schleifenvariable fuer den Closure-Listener.
            var capturedBranch = branch;
            button.onClick.AddListener(() => OnChoiceSelected(capturedBranch));
        }
    }

    private string GetMenuText(Branch branch)
    {
        var target = branch.Target;

        if (target is IObjectWithMenuText objWithMenuText && !string.IsNullOrEmpty(objWithMenuText.MenuText))
            return objWithMenuText.MenuText;
        if (target is IObjectWithLocalizableMenuText objWithLocMenuText && !string.IsNullOrEmpty(objWithLocMenuText.MenuText))
            return objWithLocMenuText.MenuText;

        if (target is IObjectWithText objWithText)
            return objWithText.Text;
        if (target is IObjectWithLocalizableText objWithLocText)
            return objWithLocText.Text;

        return "Weiter";
    }

    private void OnChoiceSelected(Branch branch)
    {
        flowPlayer.Play(branch);
    }

    private void ClearChoiceButtons()
    {
        foreach (Transform child in choiceButtonContainer)
            Destroy(child.gameObject);
    }

    private void EndDialogue()
    {
        ClearChoiceButtons();
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}
