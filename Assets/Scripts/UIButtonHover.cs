using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    
    private RectTransform visual;
    private Graphic graphic;
    private Vector2 _originalPos;

    void Start()
    {
        graphic = GetComponentInChildren<TMP_Text>();

        if ( graphic == null)
        {
            graphic = GetComponentInChildren<Text>();
        }

        visual = graphic.GetComponent<RectTransform>();
        _originalPos = visual.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        if ( graphic is TMP_Text )
        {
            graphic.color = Color.red;
        }

        visual.anchoredPosition = _originalPos + new Vector2(0, 10.0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if ( graphic is TMP_Text )
        {
            graphic.color = new Color(80 / 255f, 80 / 255f, 80 / 255f);
        }

        visual.anchoredPosition = _originalPos;
    }



}
