using UnityEngine;

// Wechselt bei Kampfbeginn von einer "normalen" Erscheinung zu einem zweiten Modell
// (z.B. Hekates wahre Gestalt). Auf ein Controller-GameObject packen (kann z.B. das
// bisherige Hekate-Objekt selbst sein, oder ein neues leeres Parent-Objekt).
//
// Setup:
// 1) Beide Modelle als zwei GETRENNTE GameObjects in der Szene haben:
//    - normalForm: das bisherige Hexen-Modell (aktuell aktiv, spielt alle Dialoge)
//    - trueForm: das zweite Modell/Asset (die wahre Gestalt), zu Spielbeginn DEAKTIVIERT
// 2) Dieses Script auf ein beliebiges GameObject packen (z.B. auf normalForm selbst oder
//    einen gemeinsamen Parent), beide Felder im Inspector zuweisen.
// 3) trueForm braucht eine eigene Konfiguration fuer den Bosskampf: Enemy Base (mit
//    "Is Final Boss" angehakt, damit ihr Tod den Victory-Screen ausloest), die passende
//    Boss-KI, NavMeshAgent, Collider usw. - genau wie beim Malakor-Setup.
// 4) Am DialogueController-Objekt (Component DialogueUIController) unter
//    "On Hekate Fight Started": zweiten Eintrag hinzufuegen -> dieses Objekt ->
//    FormSwap.TransformToTrueForm(), zusaetzlich zum bereits vorhandenen
//    Silas.JoinFightAgainst()-Eintrag.
public class FormSwap : MonoBehaviour
{
    [Tooltip("Das aktuell sichtbare, normale Erscheinungsbild (z.B. Hexen-Modell).")]
    public GameObject normalForm;

    [Tooltip("Das zweite Modell, das beim Kampfbeginn erscheint (die wahre Gestalt).")]
    public GameObject trueForm;

    [Tooltip("Wenn an, wird trueForm exakt an Position/Rotation von normalForm gesetzt, bevor es erscheint.")]
    public bool matchTransform = true;

    public void TransformToTrueForm()
    {
        if (matchTransform && normalForm != null && trueForm != null)
        {
            trueForm.transform.position = normalForm.transform.position;
            trueForm.transform.rotation = normalForm.transform.rotation;
        }

        if (normalForm != null) normalForm.SetActive(false);
        if (trueForm != null) trueForm.SetActive(true);
    }
}
