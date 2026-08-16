using UnityEngine;
using System.Collections;

public class UIInteractionManager : MonoBehaviour
{
    public static UIInteractionManager Instance { get; private set; }

    [Header("Standart Etkileşim UI")]
    [SerializeField] private GameObject interactionPanel; // "E'ye bas" yazısı

    [Header("Kilit & Anahtar UI (YENİ)")]
    [SerializeField] private GameObject lockedMessagePanel; // Ekranda belirecek "You need a key!" yazısı/paneli
    [SerializeField] private GameObject itemFoundPanel;     // Boss ölünce ekrana çıkacak "Anahtar.png" paneli
    [SerializeField] private float messageDuration = 2.5f;  // Yazıların ekranda kalma süresi
    [SerializeField] private float keyDisplayDelay = 1.5f;  // 🟢 YENİ: Düşman öldükten kaç saniye sonra anahtar ekrana gelsin?

    public bool hasBossKey = false; // Oyuncu anahtarı aldı mı?

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideInteractionUI();
        
        // Başlangıçta uyarı panellerini gizle
        if (lockedMessagePanel != null) lockedMessagePanel.SetActive(false);
        if (itemFoundPanel != null) itemFoundPanel.SetActive(false);
    }

    public void ShowInteractionUI()
    {
        if (interactionPanel != null) interactionPanel.SetActive(true);
    }

    public void HideInteractionUI()
    {
        if (interactionPanel != null) interactionPanel.SetActive(false);
    }

    public void ShowLockedMessage()
    {
        StartCoroutine(ShowTempPanel(lockedMessagePanel));
    }

    // 🟢 GÜNCELLENDİ: Artık animasyon süresini bekleyip öyle paneli açıyor
    public void GiveKey()
    {
        StartCoroutine(GiveKeyRoutine());
    }

    private IEnumerator GiveKeyRoutine()
    {
        // Adamın yere düşmesi için bekletme süresi
        if (keyDisplayDelay > 0f)
        {
            yield return new WaitForSeconds(keyDisplayDelay);
        }

        hasBossKey = true;
        StartCoroutine(ShowTempPanel(itemFoundPanel));
        Debug.Log("🗝️ Anahtar alındı!");
    }

    private IEnumerator ShowTempPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
            yield return new WaitForSeconds(messageDuration);
            panel.SetActive(false);
        }
    }
}