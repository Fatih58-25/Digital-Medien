using UnityEngine;

public class ElevatorSwitch : MonoBehaviour
{
    [Header("Bağlanacak Asansör")]
    [SerializeField] private SoulsElevator connectedElevator; // Şalterin kontrol edeceği asansör

    private bool isPlayerNearby = false;

    private void Update()
    {
        // Oyuncu şalterin dibindeyse ve E'ye basarsa
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (connectedElevator != null)
            {
                connectedElevator.TryActivateElevator();
                Debug.Log("Şalter çekildi, asansör çağrıldı/gönderildi!");
            }
            else
            {
                Debug.LogError("Aga şaltere asansörü bağlamayı unuttun!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Oyuncu şalterin etki alanına girdi
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Oyuncu şalterin yanından uzaklaştı
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
}