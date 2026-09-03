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

        // 예전에는 여기서 BattleManager.CommandAttack 같은 죽은 bool 플래그만 세팅하고
        // 실제 커맨드 상태머신(BattleManager._commandState)은 건드리지 않았다.
        // 그래서 버튼을 눌러도 실제 공격/스킬/도구 진입이 되지 않았다. 이제는 BattleManager의
        // 공개 진입점(EnterTargeting/EnterSkill/EnterInstrument/Retreat)을 그대로 호출해서
        // 마우스 좌클릭/키보드 입력과 동일한 상태 전환 경로를 타게 한다.
        ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.actionBar, false);
        ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
        ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandMemberBar, false);

        if (buttonText == "attack")
        {
            BattleManager.BattleInstance.EnterTargeting();
        }

        if (buttonText == "skill")
        {
            BattleManager.BattleInstance.EnterSkill();
        }

        if (buttonText == "instrument")
        {
            BattleManager.BattleInstance.EnterInstrument();
        }

        if (buttonText == "retreat")
        {
            BattleManager.BattleInstance.Retreat();
        }
    }


}
