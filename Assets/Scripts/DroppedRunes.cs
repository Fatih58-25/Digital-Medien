using UnityEngine;

public class DroppedRunes : MonoBehaviour
{
    // Bu değişkende öldüğün anki rün miktarı saklanacak
    public int runeAmount = 0;

    private void OnTriggerEnter(Collider other)
    {
        // Oyuncu parlayan kürenin içine girdiğinde
        if (other.CompareTag("Player"))
        {
            PlayerRunes playerRunes = other.GetComponent<PlayerRunes>();
            if (playerRunes != null)
            {
                // Rünleri oyuncunun cüzdanına geri yükle
                playerRunes.AddRunes(runeAmount);
                Debug.Log(runeAmount + " rün geri toplandı!");
                
                // Yerdeki parlayan objeyi yok et
                Destroy(gameObject);
            }
        }
    }
}