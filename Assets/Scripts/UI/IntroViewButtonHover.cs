using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IntroViewButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text _text;

    private RectTransform visual;
    private Vector2 _originalPos;
    

    void Start()
    {
        visual = _text.gameObject.GetComponent<RectTransform>();
        _originalPos = visual.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        visual.anchoredPosition = _originalPos + new Vector2(0, 10.0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        visual.anchoredPosition = _originalPos;
    }
}
