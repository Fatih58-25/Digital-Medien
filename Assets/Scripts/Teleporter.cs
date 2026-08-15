using System.Collections;
using UnityEngine;

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
        
        // 🟢 ÇÖZÜM: Işınlanma başladığı an menzilden çıktığını manuel olarak belirtiyoruz.
        // Böylece haritanın başka yerinde E'ye bassan da bu script bir daha çalışmaz.
        playerInRange = false; 

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            isTeleporting = false;
            yield break;
        }

        CharacterController controller = player.GetComponent<CharacterController>();

        if (fadeCanvasGroup != null)
            yield return Fade(0f, 1f);

        // CharacterController kapatılıyor
        if (controller != null) controller.enabled = false;

        player.transform.position = destination.position;
        player.transform.rotation = destination.rotation;
        Physics.SyncTransforms();

        // CharacterController yeniden açılıyor
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