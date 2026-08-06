using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonColor : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    void Awake()
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>();

        label.color = normalColor;
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
        Debug.Log("OnSubmit erkannt!");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("OnPointerClick erkannt!");
    }
}