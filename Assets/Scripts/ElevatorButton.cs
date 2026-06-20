using UnityEngine;

public class ElevatorButton : MonoBehaviour
{
    private SoulsElevator elevatorMain;

    private void Start()
    {
        // En tepedeki asansör ana koduna ulaşıyoruz
        elevatorMain = GetComponentInParent<SoulsElevator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Asansör butonuna biri bastı amk! Basan nesne: " + other.name);
        // Değen nesne bizim oyuncu (Knight) ise
        if (other.CompareTag("Player"))
        {
            elevatorMain.TryActivateElevator();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Oyuncu ayağını çekince 2 saniyelik cezayı başlatıyoruz aga
        if (other.CompareTag("Player"))
        {
            elevatorMain.StartCooldownProcess();
        }
    }
}