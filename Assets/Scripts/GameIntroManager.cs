using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Articy.Unity;
using Articy.Unity.Interfaces;

// Steuert den Spielstart: Blackscreen mit Hexen-Erzaehlung (aus articy:draft) -> Fade-out -> Spieler freigeben.
// Die Erzaehler-Kette ("Hexe: Frieden vor der Kette..." bis "Hexe: Oeffne die Augen...") liegt als eigene,
// vom Hauptdialog losgeloeste Kette in articy:draft. Dieses Script spielt sie über einen eigenen
// ArticyFlowPlayer ab, eine Zeile pro E-Druck. Ist die Kette zu Ende (Dead End), blendet der Blackscreen
// aus, der Spieler wird freigegeben und läuft zur Hexe, wo NPCInteractable.cs + DialogueUIController.cs
// den eigentlichen, interaktiven articy-Dialog uebernehmen.
[RequireComponent(typeof(ArticyFlowPlayer))]
public class GameIntroManager : MonoBehaviour, IArticyFlowPlayerCallbacks
{
    [Header("UI-Referenzen")]
    [Tooltip("CanvasGroup auf einem vollflaechigen schwarzen Image (Screen Space - Overlay)")]
    public CanvasGroup blackScreen;
    public TMP_Text narrationText;

    [Header("articy:draft")]
    [Tooltip("Zeigt auf den ERSTEN Knoten deiner Hexe-Erzaehler-Kette in articy:draft (Zeile 1 \"Frieden vor der Kette\")")]
    public ArticyRef narrationStart;

    [Header("Steuerung")]
    public KeyCode advanceKey = KeyCode.E;
    public float fadeOutDuration = 1.5f;

    [Header("Spieler sperren waehrend der Intro")]
    public MonoBehaviour playerController;   // z.B. das PlayerController-Script aus Assets/Player/Script
    public MonoBehaviour playerCombat;       // optional: PlayerCombatSystem

    private ArticyFlowPlayer flowPlayer;
    private readonly List<Branch> currentBranches = new List<Branch>();
    private bool introRunning = true;
    private bool waitingForInput = false;

    void Awake()
    {
        flowPlayer = GetComponent<ArticyFlowPlayer>();
    }

    void Start()
    {
        SetPlayerControlEnabled(false);

        blackScreen.alpha = 1f;
        blackScreen.blocksRaycasts = true;
        blackScreen.gameObject.SetActive(true);

        flowPlayer.StartOn = narrationStart.GetObject();
        flowPlayer.Play();
    }

    void Update()
    {
        if (!introRunning || !waitingForInput) return;

        if (Input.GetKeyDown(advanceKey))
        {
            waitingForInput = false;
            if (currentBranches.Count > 0)
            {
                flowPlayer.Play(currentBranches[0]);
            }
            else
            {
                // Kein gueltiger Folgeknoten mehr -> Kette ist zu Ende
                StartCoroutine(EndIntro());
            }
        }
    }

    // Wird vom ArticyFlowPlayer aufgerufen, sobald er auf einem Dialog-Fragment pausiert.
    public void OnFlowPlayerPaused(IFlowObject aObject)
    {
        if (aObject == null)
        {
            // Dead End erreicht -> Erzaehlung ist zu Ende
            StartCoroutine(EndIntro());
            return;
        }

        if (aObject is IObjectWithLocalizableText objWithLocText)
            narrationText.text = objWithLocText.Text;
        else if (aObject is IObjectWithText objWithText)
            narrationText.text = objWithText.Text;
        else
            narrationText.text = string.Empty;
    }

    // Wird direkt nach OnFlowPlayerPaused aufgerufen und liefert die moeglichen Folgeknoten.
    public void OnBranchesUpdated(IList<Branch> aBranches)
    {
        currentBranches.Clear();
        foreach (var branch in aBranches)
            if (branch.IsValid) currentBranches.Add(branch);

        // Die Kette ist rein linear -> maximal 1 gueltiger Branch. Wir warten auf E, statt automatisch
        // weiterzuspielen, damit der Spieler das Erzaehltempo selbst bestimmt.
        waitingForInput = true;
    }

    private IEnumerator EndIntro()
    {
        introRunning = false;
        waitingForInput = false;
        narrationText.text = string.Empty;

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            blackScreen.alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            yield return null;
        }

        blackScreen.alpha = 0f;
        blackScreen.blocksRaycasts = false;
        blackScreen.gameObject.SetActive(false);

        SetPlayerControlEnabled(true);
    }

    private void SetPlayerControlEnabled(bool value)
    {
        if (playerController != null) playerController.enabled = value;
        if (playerCombat != null) playerCombat.enabled = value;
    }
}
