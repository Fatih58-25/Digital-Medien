using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonColor : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler
{
    public enum ButtonAction { None, Play, Settings, Quit, Back, Restart }

    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Direkte Aktion")]
    [SerializeField] private MainMenuManager menuManager;
    [SerializeField] private ButtonAction action = ButtonAction.None;

    void Awake()
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>();

        label.color = normalColor;

        if (menuManager == null)
            menuManager = FindFirstObjectByType<MainMenuManager>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        label.color = selectedColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        label.color = normalColor;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        TriggerClick();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerClick();
    }

    private void TriggerClick()
    {
        if (menuManager == null) return;

        switch (action)
        {
            case ButtonAction.Play:
                menuManager.PlayGame();
                break;
            case ButtonAction.Settings:
                menuManager.OpenSettings();
                break;
            case ButtonAction.Quit:
                menuManager.QuitGame();
                break;
            case ButtonAction.Back:
                menuManager.CloseSettings();
                break;
            case ButtonAction.Restart:
                menuManager.RestartGame();
                break;
        }
    }
}