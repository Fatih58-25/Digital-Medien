using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Checkpoint / Respawn Settings")]
    public Vector3 lastCheckpointPosition;
    public bool hasCheckpoint = false;

    [Header("UI Settings")]
    [SerializeField] private CanvasGroup youDiedCanvasGroup; 
    [SerializeField] private float fadeDuration = 2.0f;
    [SerializeField] private float respawnDelay = 3.5f;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 🟢 1. Her sahne yüklendiğinde yeni sahnede olan UI Paneli yeniden bul
        FindUIReferences();

        // 2. Oyuncuyu bul ve pozisyonunu ayarla
        FindAndSetupPlayer();
    }

    private void FindUIReferences()
    {
        // Sahnede "YouDiedPanel" ismindeki objeyi ara
        GameObject panelObj = GameObject.Find("YouDiedPanel");
        if (panelObj != null)
        {
            youDiedCanvasGroup = panelObj.GetComponent<CanvasGroup>();
            if (youDiedCanvasGroup != null)
            {
                youDiedCanvasGroup.alpha = 0f;
                youDiedCanvasGroup.gameObject.SetActive(true); // Obje açık kalsın, Alpha ile yöneteceğiz
            }
        }
        else
        {
            Debug.LogWarning("Sahnede 'YouDiedPanel' isimli UI objesi bulunamadı!");
        }
    }

    private void FindAndSetupPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<PlayerHealth>();

            // Kaydedilmiş Checkpoint varsa oyuncuyu oraya ışınla
            if (hasCheckpoint)
            {
                CharacterController controller = playerObj.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;

                playerObj.transform.position = lastCheckpointPosition;

                if (controller != null) controller.enabled = true;
            }
            else
            {
                lastCheckpointPosition = playerObj.transform.position;
                hasCheckpoint = true;
            }
        }
    }

    public void SaveCheckpoint(Vector3 newPosition)
    {
        lastCheckpointPosition = newPosition;
        hasCheckpoint = true;
        Debug.Log("Checkpoint Kaydedildi: " + newPosition);
    }

    public void OnPlayerDiedDirectCall()
    {
        StartCoroutine(PlayerDeathSequence());
    }

    private IEnumerator PlayerDeathSequence()
    {
        yield return new WaitForSeconds(1.0f);

        // Eğer yeni CanvasGroup referansı her ihtimale karşı boşsa tekrar bulmayı dene
        if (youDiedCanvasGroup == null)
        {
            FindUIReferences();
        }

        if (youDiedCanvasGroup != null)
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                youDiedCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            youDiedCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(respawnDelay);

        // Sahneyi yeniden yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}