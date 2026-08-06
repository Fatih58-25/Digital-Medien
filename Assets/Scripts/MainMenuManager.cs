using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Canvas")]
    [SerializeField] private GameObject menuCanvas; // Menü UI paneliniz/Canvas'ınız

    private void Start()
    {
        // 1. Oyun başladığında menü açık olsun
        if (menuCanvas != null) menuCanvas.SetActive(true);

        // 2. Fare serbest ve görünür olsun
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Oyuncu menüdeyken arkada oyun akmasın (düşmanlar hareket etmesin)
        Time.timeScale = 0f; 
    }

    public void PlayGame()
    {
        

        // 1. Menü panelini gizle
        if (menuCanvas != null) menuCanvas.SetActive(false);

        // 2. FAREYİ KİLİTLE (Artık oyuna girdik, fare kaybolsun)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. Zamanı tekrar başlat
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game...");
        Application.Quit();
    }
}