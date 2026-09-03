using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CommandButtonHover : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform visual;
    private TMP_Text _tmpText;
    private Vector2 _originalPos;
    private float _fontOriginSize;

    void Start()
    {
        _tmpText = GetComponentInChildren<TMP_Text>();
        _originalPos = visual.anchoredPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        string buttonText = _tmpText.text.ToString();

        Debug.Log($"buttonText = {buttonText}");

        ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.actionBar, false);
        ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
        ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandMemberBar, false);

        if (buttonText == "attack")
        {
            BattleManager.BattleInstance.CommandAttack = true;
            BattleManager.BattleInstance.CommandSelect = false;
            GameManager.GameInstance.Monsters[0].GetComponent<Monster>().TargetAreaShow();
            ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, true);
        }

        if (buttonText == "skill")
        {
            ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, true);
        }

        if (buttonText == "instrument")
        {
            ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, true);
        }

        if (buttonText == "retreat")
        {
            SceneManager.LoadScene(1);
        }
    }


}
