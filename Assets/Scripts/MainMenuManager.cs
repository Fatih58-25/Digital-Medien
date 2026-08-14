using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.SceneManagement;


public class MainMenuManager : MonoBehaviour

{

    private static bool startInGameOnLoad = false;


    [Header("UI Canvas")]

    [SerializeField] private GameObject menuCanvas;


    [Header("Keyboard / Gamepad Settings")]

    [SerializeField] private GameObject firstSelectedButton;


    [Header("Settings Panel")]

    [SerializeField] private GameObject mainMenuButtons;

    [SerializeField] private GameObject hudCanvas;

    [SerializeField] private GameObject settingsPanel;


    [Header("Audio")]

    [SerializeField] private AudioSource backgroundMusic;

    [SerializeField] private float menuMusicVolume = 0.5f;

    [SerializeField] private float gameMusicVolume = 0.2f;


    private void Start()

    {

        if (backgroundMusic != null && !backgroundMusic.isPlaying)

        {

            backgroundMusic.volume = menuMusicVolume;

            backgroundMusic.Play();

        }


        if (startInGameOnLoad)

        {

            startInGameOnLoad = false;

            PlayGame();

            return;

        }


        if (menuCanvas != null) menuCanvas.SetActive(true);

        if (hudCanvas != null) hudCanvas.SetActive(false);

        if (mainMenuButtons != null) mainMenuButtons.SetActive(true);

        if (settingsPanel != null) settingsPanel.SetActive(false);


        Cursor.lockState = CursorLockMode.None;

        Cursor.visible = true;


        if (firstSelectedButton != null && EventSystem.current != null)

        {

            EventSystem.current.SetSelectedGameObject(null);

            EventSystem.current.SetSelectedGameObject(firstSelectedButton);

        }


        Time.timeScale = 0f;

    }


    public void PlayGame()

    {

        if (menuCanvas != null) menuCanvas.SetActive(false);

        if (hudCanvas != null) hudCanvas.SetActive(true);


        Cursor.lockState = CursorLockMode.Locked;

        Cursor.visible = false;


        Time.timeScale = 1f;


        if (backgroundMusic != null)

        {

            if (!backgroundMusic.isPlaying) backgroundMusic.Play();

            backgroundMusic.volume = gameMusicVolume;

        }

    }


    public void QuitGame()

    {

        Debug.Log("Quit Game...");

        Application.Quit();

    }


    public void OpenSettings()

    {

        Debug.Log("OpenSettings wurde aufgerufen!");

        if (mainMenuButtons != null) mainMenuButtons.SetActive(false);

        if (settingsPanel != null) settingsPanel.SetActive(true);

    }


    public void CloseSettings()

    {

        Debug.Log("CloseSettings wurde aufgerufen!");

        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (mainMenuButtons != null) mainMenuButtons.SetActive(true);

    }


    public void RestartGame()
{
    Debug.Log("Restart butonuna basıldı! Bonfire'da doğuluyor...");

    Time.timeScale = 1f;

    // Fare imlecini tekrar oyuna kitle
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;

    // HUD Panellerini Aç/Kapat
    if (menuCanvas != null) menuCanvas.SetActive(false);
    if (hudCanvas != null) hudCanvas.SetActive(true);

    // 🟢 Bonfire'a ışınlanma ve Game Over ekranını kapatma işlemini başlat
    if (GameManager.Instance != null)
    {
        GameManager.Instance.RespawnPlayerInPlace();
    }
}
} 