using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>스킬 버튼 Hover 시 설명을 띄우는 작은 프록시. SkillSelectView 가 런타임에 붙인다.</summary>
public class SkillHoverProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SkillSelectView _view;
    private SkillData _skill;

    public void Bind(SkillSelectView view, SkillData skill)
    {
        _view = view;
        _skill = skill;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_view != null) _view.ShowDescription(_skill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_view != null) _view.ShowDescription(null);
    }
}
