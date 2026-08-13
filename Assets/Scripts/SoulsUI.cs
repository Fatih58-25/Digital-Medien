using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SoulsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI soulsTextTMP; // Sürüklediğin TMP objesi
    [SerializeField] private Text soulsTextLegacy;         // Normal UI Text ise bu

    private PlayerRunes playerRunes;

    private void Start()
    {
        FindPlayerRunes();
    }

    private void Update()
    {
        // 🟢 Oyuncu sahnede yoksa (respawn anında vs.) sürekli aramayı dene
        if (playerRunes == null)
        {
            FindPlayerRunes();
            return;
        }

        // 🟢 Her karede oyuncunun rün miktarını oku ve ekrana bas (Event kaçırma riski %0!)
        UpdateSoulsText(playerRunes.CurrentRunes);
    }

    private void FindPlayerRunes()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerRunes = playerObj.GetComponent<PlayerRunes>();
        }
    }

    private void UpdateSoulsText(int amount)
    {
        if (soulsTextTMP != null)
        {
            soulsTextTMP.text = amount.ToString();
        }
        
        if (soulsTextLegacy != null)
        {
            soulsTextLegacy.text = amount.ToString();
        }
    }
}