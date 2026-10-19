using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Makes a series name react to a double click (opens its page) and highlights it on hover
public class SeriesLink : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Text Label;

    public event Action DoubleClicked;

    private Color normal;
    private bool captured;
    private static readonly Color Hover = new Color(1f, 0.8f, 0f);

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.clickCount == 2) { DoubleClicked?.Invoke(); }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!captured) { normal = Label.color; captured = true; }
        Label.color = Hover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(captured) { Label.color = normal; }
    }
}
