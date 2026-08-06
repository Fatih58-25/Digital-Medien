using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuNavigation : MonoBehaviour
{
    [SerializeField] private GameObject firstSelected;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    void Update()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null)
            {
                Button btn = selected.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.Invoke();
                }
            }
        }
    }
}