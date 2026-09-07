using System;
using UnityEngine;

/// <summary>
/// 아이템 메뉴의 치수와 색. 인스펙터에서 조절할 수 있게 별도 클래스로 뺐다.
///
/// 런타임 생성이라 눈으로 보고 고칠 대상이 코드밖에 없다. 값을 여기 모아두면
/// 재컴파일 없이 인스펙터에서 맞출 수 있다.
/// </summary>
[Serializable]
public class ItemMenuStyle
{
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

    [Tooltip("탭 줄 전체의 화면 위치 (좌상단 기준)")]
    public Vector2 StripPosition = new Vector2(180.0f, -70.0f);

    [Header("아이템 목록")]
    [Tooltip("목록 패널 크기")]
    public Vector2 PanelSize = new Vector2(720.0f, 340.0f);
    [Tooltip("목록 패널의 화면 위치 (좌상단 기준)")]
    public Vector2 PanelPosition = new Vector2(250.0f, -140.0f);

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

    [Tooltip("한 화면에 만들 줄 수. 목록이 더 길면 스크롤된다")]
    public int VisibleRows = 8;

    [Header("선택 표식")]
    [Tooltip("선택된 줄 왼쪽에 붙는 화살표 크기")]
    public Vector2 MarkerSize = new Vector2(26.0f, 26.0f);
    [Tooltip("패널 왼쪽 바깥으로 얼마나 내밀지")]
    public float MarkerOutset = 30.0f;

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
}
