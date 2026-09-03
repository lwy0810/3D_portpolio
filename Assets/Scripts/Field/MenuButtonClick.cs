using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuButtonClick : MonoBehaviour, IPointerClickHandler
{
    private Text _text;

    void Start()
    {
        _text = GetComponentInChildren<Text>();
    }

    void Update()
    {
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _text.color = Color.white;
    }



}
