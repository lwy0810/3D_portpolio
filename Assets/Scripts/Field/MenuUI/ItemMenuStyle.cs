using System;
using UnityEngine;
using TMPro;

/// <summary>
/// 아이템 메뉴의 치수와 색. 인스펙터에서 조절할 수 있게 별도 클래스로 뺐다.
///
/// 런타임 생성이라 눈으로 보고 고칠 대상이 코드밖에 없다. 값을 여기 모아두면
/// 재컴파일 없이 인스펙터에서 맞출 수 있다.
///
/// 위치 값은 모두 "화면 상단 중앙" 기준 오프셋이다. 좌상단 기준이 아니다 —
/// 해상도가 바뀌어도 좌우 여백이 자동으로 같아지게 하려는 것이다.
/// </summary>
[Serializable]
public class ItemMenuStyle
{
    [Header("폰트")]
    [Tooltip("비워두면 ItemMenuView 의 폰트 경로에서 로드한다")]
    public TMP_FontAsset Font;

    [Header("카테고리 탭")]
    [Tooltip("탭 하나의 크기")]
    public Vector2 TabSize = new Vector2(118.0f, 44.0f);
    [Tooltip("탭 사이 간격")]
    public float TabGap = 6.0f;
    [Tooltip("선택된 탭을 이만큼 키운다")]
    public float TabSelectedScale = 1.14f;
    public float TabFontSize = 22.0f;

    [Tooltip("CATEGORIES 라벨 크기")]
    public Vector2 LabelSize = new Vector2(150.0f, 34.0f);
    public float LabelFontSize = 20.0f;

    [Tooltip("Prev / Next 버튼 크기")]
    public Vector2 ArrowSize = new Vector2(38.0f, 38.0f);
    [Tooltip("Prev / Next 버튼과 탭 줄 사이 간격")]
    public float ArrowGap = 10.0f;

    [Tooltip("PrevSelectButton 미세 조정. 계산된 위치에 더한다")]
    public Vector2 PrevOffset = Vector2.zero;
    [Tooltip("NextSelectButton 미세 조정. 계산된 위치에 더한다")]
    public Vector2 NextOffset = Vector2.zero;

    [Tooltip("탭 줄 위치. 화면 상단 중앙 기준 오프셋이며 X 0 이면 좌우 여백이 같다")]
    public Vector2 StripOffset = new Vector2(0.0f, -70.0f);

    [Header("아이템 목록")]
    [Tooltip("목록 패널 크기")]
    public Vector2 PanelSize = new Vector2(720.0f, 500.0f);
    [Tooltip("목록 패널 위치. 화면 상단 중앙 기준 오프셋이며 X 0 이면 좌우 여백이 같다")]
    public Vector2 PanelOffset = new Vector2(0.0f, -140.0f);

    public float RowWidth = 690.0f;
    public float RowHeight = 40.0f;
    [Tooltip("패널 안쪽 여백")]
    public float RowInsetX = 16.0f;
    public float RowInsetY = 14.0f;

    public float IconSize = 30.0f;
    public float IconInsetX = 8.0f;
    [Tooltip("아이콘과 이름 사이 간격")]
    public float NameGap = 10.0f;

    [Tooltip("수량이 차지하는 폭")]
    public float CountWidth = 120.0f;
    public float CountInsetX = 14.0f;
    public float RowFontSize = 22.0f;

    [Tooltip("만들 줄 수. 0 이면 패널 높이에 맞춰 자동 계산한다")]
    public int VisibleRows = 0;

    [Header("선택 표식")]
    [Tooltip("선택된 줄 왼쪽에 붙는 표식 크기")]
    public Vector2 MarkerSize = new Vector2(26.0f, 26.0f);
    [Tooltip("패널 왼쪽 바깥으로 얼마나 내밀지")]
    public float MarkerOutset = 30.0f;
    [Tooltip("표식 회전(도). Marker.png 는 아래를 향하므로 90 이면 오른쪽을 향한다")]
    public float MarkerRotation = 90.0f;
    [Tooltip("표식 미세 조정. 계산된 위치에 더한다")]
    public Vector2 MarkerOffset = Vector2.zero;

    [Header("색")]
    public Color PanelTint = new Color(0.94f, 0.98f, 1.0f, 0.96f);
    public Color TabTint = new Color(0.62f, 0.86f, 0.92f, 0.95f);
    public Color TabSelectedTint = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public Color LabelBoxTint = new Color(0.72f, 0.88f, 0.94f, 0.9f);

    public Color TabTextColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public Color TabSelectedTextColor = new Color(0.05f, 0.24f, 0.29f, 1.0f);
    public Color LabelTextColor = new Color(0.87f, 0.76f, 0.47f, 1.0f);

    public Color NameColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public Color CountColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    [Tooltip("선택된 줄 배경")]
    public Color RowHighlight = new Color(0.16f, 0.58f, 0.63f, 0.85f);
    public Color MarkerTint = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    /// <summary>패널 높이에 들어가는 줄 수.</summary>
    public int FitRows()
    {
        float _usable = PanelSize.y - RowInsetY * 2.0f;
        return Mathf.Max(1, Mathf.FloorToInt(_usable / Mathf.Max(1.0f, RowHeight)));
    }

    /// <summary>
    /// 0 이나 음수로 남은 값을 기본값으로 되돌린다.
    ///
    /// 인스펙터에서 실수로 지운 경우와, 필드를 새로 추가했을 때 기존 씬에
    /// 값이 없어 0 으로 들어오는 경우를 함께 막는다. 크기가 0 이면 오브젝트가
    /// 생성되어도 화면에 보이지 않아 원인 추적이 어렵다.
    /// </summary>
    public void Normalize()
    {
        if (TabSize.x <= 0.0f || TabSize.y <= 0.0f) TabSize = new Vector2(118.0f, 44.0f);
        if (LabelSize.x <= 0.0f || LabelSize.y <= 0.0f) LabelSize = new Vector2(150.0f, 34.0f);
        if (ArrowSize.x <= 0.0f || ArrowSize.y <= 0.0f) ArrowSize = new Vector2(38.0f, 38.0f);
        if (PanelSize.x <= 0.0f || PanelSize.y <= 0.0f) PanelSize = new Vector2(720.0f, 500.0f);
        if (MarkerSize.x <= 0.0f || MarkerSize.y <= 0.0f) MarkerSize = new Vector2(26.0f, 26.0f);

        if (TabSelectedScale <= 0.0f) TabSelectedScale = 1.14f;
        if (RowHeight <= 0.0f) RowHeight = 40.0f;
        if (RowWidth <= 0.0f) RowWidth = PanelSize.x - RowInsetX * 2.0f;
        if (IconSize <= 0.0f) IconSize = 30.0f;
        if (CountWidth <= 0.0f) CountWidth = 120.0f;

        if (TabFontSize <= 0.0f) TabFontSize = 22.0f;
        if (LabelFontSize <= 0.0f) LabelFontSize = 20.0f;
        if (RowFontSize <= 0.0f) RowFontSize = 22.0f;

        if (VisibleRows <= 0) VisibleRows = FitRows();
    }
}
