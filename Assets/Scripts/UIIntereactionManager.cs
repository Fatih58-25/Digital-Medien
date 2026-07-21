using UnityEngine;

public class UIInteractionManager : MonoBehaviour
{
    public static UIInteractionManager Instance { get; private set; }

    [Header("UI Elemanı")]
    [SerializeField] private GameObject interactionPanel; // Ekranda hazırladığın InteractionUI objesi

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideInteractionUI(); // Başlangıçta ekran kapalı gelsin
    }

    public void ShowInteractionUI()
    {
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
        }
    }

    public void HideInteractionUI()
    {
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }
}