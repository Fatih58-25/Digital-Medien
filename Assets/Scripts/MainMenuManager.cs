using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Canvas")]
    [SerializeField] private GameObject menuCanvas;

    [Header("Keyboard / Gamepad Settings")]
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Settings Panel")]
    [SerializeField] private GameObject mainMenuButtons;
    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
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
}