using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;
using Articy.Unity;
using Articy.Unity.Interfaces;
using Articy.Digitial_media_story.GlobalVariables;

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

    // Von NPCInteractable.cs abgefragt, um den "Sprechen (E)"-Hinweis waehrend eines laufenden Dialogs auszublenden.
    public bool IsDialogueOpen { get; private set; }

    [Header("UI-Referenzen")]
    public GameObject dialoguePanel;
    public TMP_Text speakerLabel;
    public TMP_Text dialogueText;
    public Button choiceButtonPrefab;
    public Transform choiceButtonContainer;

    [Header("Tasten-Steuerung Antworten")]
    public KeyCode confirmKey = KeyCode.E;

    [Header("Spieler waehrend des Dialogs sperren")]
    [Tooltip("Z.B. das PlayerController-Script aus Assets/Player/Script - wird waehrend des Dialogs deaktiviert.")]
    public MonoBehaviour playerController;
    [Tooltip("Optional: z.B. PlayerCombatSystem, falls Kaempfen waehrend des Dialogs auch gesperrt werden soll.")]
    public MonoBehaviour playerCombat;

    [Header("Bosskampf-Trigger (GameState-Variablen aus articy)")]
    [Tooltip("Wird ausgeloest, wenn GameState.StartBossFightMalakor beim Dialogende true ist. Im Inspector z.B. mit BossManager.StartFight(malakor) verknuepfen.")]
    public UnityEvent onStartBossFightMalakor;
    [Tooltip("Wird ausgeloest, wenn GameState.StartBossFightHekate beim Dialogende true ist (z.B. Silas wird Verbuendeter).")]
    public UnityEvent onStartBossFightHekate;
    [Tooltip("Separater Trigger fuer den ECHTEN Hekate-Kampfbeginn (GameState.HekateFightStarted), z.B. am Ende der Wahrheits-Konfrontation. Hier z.B. Silas.BossAllegiance.JoinFightAgainst(hekate) verknuepfen.")]
    public UnityEvent onHekateFightStarted;
    [Tooltip("Wird ausgeloest, wenn GameState.HekateHidden beim Dialogende true ist (z.B. am Ende von Hekates allererstem Gespraech direkt nach dem Intro). Hier Hekate-Objekt -> SetActive(false) verknuepfen.")]
    public UnityEvent onHekateHidden;

    private ArticyFlowPlayer flowPlayer;
    private readonly List<Button> currentButtons = new List<Button>();
    private int selectedIndex = 0;
    private int buttonsCreatedFrame = -1;

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

        IsDialogueOpen = true;
        SetPlayerControlEnabled(false);

        // Cursor sichtbar/entsperren, damit Antwort-Buttons per Maus klickbar sind
        // (waehrend des normalen Gameplays ist der Cursor fuer die Kamerasteuerung gesperrt).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        flowPlayer.StartOn = startNode;
        flowPlayer.Play();
    }

    private void SetPlayerControlEnabled(bool value)
    {
        if (playerController != null) playerController.enabled = value;
        if (playerCombat != null) playerCombat.enabled = value;
    }

    void Update()
    {
        if (currentButtons.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % currentButtons.Count;
            SelectButton(selectedIndex);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + currentButtons.Count) % currentButtons.Count;
            SelectButton(selectedIndex);
        }
        // Time.frameCount-Check verhindert, dass derselbe E-Druck, der den Dialog geoeffnet hat,
        // im selben Frame sofort die erste Antwort mitbestaetigt.
        else if (Input.GetKeyDown(confirmKey) && Time.frameCount != buttonsCreatedFrame)
        {
            currentButtons[selectedIndex].onClick.Invoke();
        }
    }

    private void SelectButton(int index)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(currentButtons[index].gameObject);
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
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = GetMenuText(branch);

            // WICHTIG: lokale Kopie der Schleifenvariable fuer den Closure-Listener.
            var capturedBranch = branch;
            button.onClick.AddListener(() => OnChoiceSelected(capturedBranch));

            currentButtons.Add(button);
        }

        if (currentButtons.Count > 0)
        {
            selectedIndex = 0;
            buttonsCreatedFrame = Time.frameCount;
            SelectButton(selectedIndex);
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
        currentButtons.Clear();
        selectedIndex = 0;
        foreach (Transform child in choiceButtonContainer)
            Destroy(child.gameObject);
    }

    private void EndDialogue()
    {
        IsDialogueOpen = false;
        ClearChoiceButtons();
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        SetPlayerControlEnabled(true);

        // Cursor wieder fuer die normale Kamerasteuerung sperren.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CheckBossFightTriggers();
    }

    // Prueft nach jedem Dialogende, ob articy die GameState-Trigger gesetzt hat, und feuert die
    // passenden UnityEvents genau einmal. Danach wird die Variable zurueckgesetzt, damit ein
    // spaeterer, unabhaengiger Dialog nicht versehentlich denselben Kampf nochmal auslöst.
    private void CheckBossFightTriggers()
    {
        var vars = ArticyGlobalVariables.Default;
        if (vars == null)
        {
            Debug.LogWarning("[DialogueUIController] CheckBossFightTriggers: ArticyGlobalVariables.Default ist null!");
            return;
        }

        Debug.Log($"[DialogueUIController] CheckBossFightTriggers: StartBossFightMalakor={vars.GameState.StartBossFightMalakor}, StartBossFightHekate={vars.GameState.StartBossFightHekate}, HekateFightStarted={vars.GameState.HekateFightStarted}, HekateHidden={vars.GameState.HekateHidden}");

        if (vars.GameState.StartBossFightMalakor)
        {
            vars.GameState.StartBossFightMalakor = false;
            Debug.Log("[DialogueUIController] onStartBossFightMalakor wird ausgeloest, Listener-Anzahl: " + onStartBossFightMalakor.GetPersistentEventCount());
            onStartBossFightMalakor?.Invoke();
        }

        if (vars.GameState.StartBossFightHekate)
        {
            vars.GameState.StartBossFightHekate = false;
            Debug.Log("[DialogueUIController] onStartBossFightHekate wird ausgeloest, Listener-Anzahl: " + onStartBossFightHekate.GetPersistentEventCount());
            onStartBossFightHekate?.Invoke();
        }

        if (vars.GameState.HekateFightStarted)
        {
            vars.GameState.HekateFightStarted = false;
            Debug.Log("[DialogueUIController] onHekateFightStarted wird ausgeloest, Listener-Anzahl: " + onHekateFightStarted.GetPersistentEventCount());
            onHekateFightStarted?.Invoke();
        }

        if (vars.GameState.HekateHidden)
        {
            vars.GameState.HekateHidden = false;
            onHekateHidden?.Invoke();
        }
    }
}
