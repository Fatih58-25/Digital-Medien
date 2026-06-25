using UnityEngine;

public class MenuSelector : MonoBehaviour
{
    public RectTransform selectionBackground; // Dein Image, das hinter dem Text liegt
    public RectTransform[] menuItems;         // Die "Play", "Settings", "Quit" Texte
    public float moveSpeed = 10f;             // Geschwindigkeit der Gleitbewegung

    private int currentIndex = 0;
    private Vector2 targetPosition;

    void Start()
    {
        // Initialer Startpunkt
        targetPosition = menuItems[currentIndex].position;
        selectionBackground.position = targetPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) ChangeSelection(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) ChangeSelection(1);

        // Sanftes Gleiten zum Ziel
        selectionBackground.position = Vector2.Lerp(selectionBackground.position, targetPosition, Time.deltaTime * moveSpeed);
    }

    void ChangeSelection(int direction)
    {
        currentIndex = Mathf.Clamp(currentIndex + direction, 0, menuItems.Length - 1);
        targetPosition = menuItems[currentIndex].position;
    }
}