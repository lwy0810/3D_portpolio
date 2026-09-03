using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Text _text;

    void Start()
    {
        _text = GetComponentInChildren<Text>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _text.color = Color.red;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _text.color = new Color(243 / 255f, 243 / 255f, 243 / 255f);
    }
}
