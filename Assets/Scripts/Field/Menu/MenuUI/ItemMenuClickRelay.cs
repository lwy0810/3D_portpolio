using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 클릭 1건을 대리자에게 넘기는 최소 컴포넌트.
///
/// Button 을 쓰지 않는 이유:
/// Button 은 Selectable 이며, Selectable 은 상위 CanvasGroup 의 interactable 을 검사한다.
/// 이 프로젝트의 MenuView 에는 interactable 이 꺼진 CanvasGroup 이 있어
/// 그 아래의 Button 은 전부 클릭을 받지 못한다.
/// IPointerClickHandler 는 Selectable 이 아니므로 해당 검사를 거치지 않는다.
///
/// MenuView 의 CanvasGroup 을 켜는 방법도 있으나, 그 값은 다른 메뉴와 공유되므로
/// 이 화면 때문에 전역 상태를 바꾸지 않는 쪽을 택하였다.
/// </summary>
public class ItemMenuClickRelay : MonoBehaviour, IPointerClickHandler
{
    private System.Action _onClick;

    public void Bind(System.Action onClick)
    {
        _onClick = onClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_onClick != null) _onClick();
    }
}
