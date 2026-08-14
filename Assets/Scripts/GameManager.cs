using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Checkpoint / Respawn Settings")]
    public Vector3 lastCheckpointPosition;
    public bool hasCheckpoint = false;

    [Header("YOU DIED Banner Settings")]
    [SerializeField] private CanvasGroup youDiedCanvasGroup; 
    [SerializeField] private float fadeDuration = 1.0f;
    
    // 🟢 Süreyi 2 saniye daha uzattık (3.5 saniye yaptık)
    [SerializeField] private float delayBeforeGameOverMenu = 3.5f;

    [Header("Ana Menü / Game Over Canvas'ı")]
    [SerializeField] private GameObject menuCanvas; 

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

    private void Start()
    {
        SetupInitialCheckpoint();

        // 🛑 BURADAKİ KAPATMA KODUNU KALDIRDIK!
        // Artık oyun başlangıcında Ana Menü Canvas'ına dokunmuyoruz.

        if (youDiedCanvasGroup != null)
        {
            youDiedCanvasGroup.alpha = 0f;
        }
    }

    private void SetupInitialCheckpoint()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && !hasCheckpoint)
        {
            lastCheckpointPosition = playerObj.transform.position;
            hasCheckpoint = true;
        }
    }

    public void SaveCheckpoint(Vector3 newPosition)
    {
        lastCheckpointPosition = newPosition;
        hasCheckpoint = true;
        Debug.Log("🔥 Checkpoint Kaydedildi: " + newPosition);
    }

    public void OnPlayerDiedDirectCall()
    {
        StartCoroutine(PlayerDeathSequence());
    }

    private IEnumerator PlayerDeathSequence()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        // 1. Düşmanlar ölü bedene vurmayı kessin (Collider kapat)
        if (playerObj != null)
        {
            Collider col = playerObj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        yield return new WaitForSeconds(0.5f);

        // 2. YOU DIED Şeridi Yavaşça Gelir
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

        // 🟢 3. YOU DIED YAZISINI UZUNCA İZLET (3.5 saniye bekleme)
        yield return new WaitForSeconds(delayBeforeGameOverMenu);

        // 🟢 4. ÖLDÜKTEN 3.5 SN SONRA MENÜ CANVAS'INI AKTİFLEŞTİR
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }

        // 5. Fare imlecini aç
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // RESTART BUTONUNA BASILINCA
   public void RespawnPlayerInPlace()
{
    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
    if (playerObj == null) return;

    CharacterController controller = playerObj.GetComponent<CharacterController>();
    PlayerHealth health = health = playerObj.GetComponent<PlayerHealth>();
    PlayerRunes runes = playerObj.GetComponent<PlayerRunes>();
    Collider col = playerObj.GetComponent<Collider>();

    // 🟢 1. DOĞRU ANIMATOR'Ü BUL VE NÜKLEER SIFIRLAMA YAP
    Animator anim = playerObj.GetComponentInChildren<Animator>();
    if (anim != null)
    {
        // Konsoldan doğru Animator'ü mü yakaladık kontrol et
        Debug.Log("🟢 Bulunan Animator Objesi: " + anim.gameObject.name);

        // Bütün parametreleri (Die, isDead vb.) sıfırlar ve Entry state'e fırlatır
        anim.Rebind(); 
        anim.Update(0f); // Değişikliği anında sahneye uygular
    }
    else
    {
        Debug.LogError("🔴 HATA: Oyuncunun üzerinde Animator bulunamadı!");
    }

    // 2. Fizik ve Sağlık Sıfırlama
    if (controller != null) controller.enabled = false;
    if (health != null) health.RestoreFullHealth();
    if (col != null) col.enabled = true;

    // 3. Bonfire'a Işınlanma
    playerObj.transform.position = lastCheckpointPosition;
    Physics.SyncTransforms();

    if (controller != null) controller.enabled = true;

    // 4. Flask ve Rünler
    PlayerFlaskSystem flasks = playerObj.GetComponent<PlayerFlaskSystem>();
    if (flasks != null) flasks.RefillFlasks();

    if (runes != null) runes.RevealDroppedRunes();

    // 5. Düşmanları Resetle
    EnemyBase[] allEnemies = FindObjectsOfType<EnemyBase>(true);
    foreach (EnemyBase enemy in allEnemies)
    {
        enemy.RespawnEnemy();
    }

    // 6. UI Kapat
    if (youDiedCanvasGroup != null) youDiedCanvasGroup.alpha = 0f;
    if (menuCanvas != null) menuCanvas.SetActive(false);

    // 7. Fareyi Oyuna Kitle
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}
}