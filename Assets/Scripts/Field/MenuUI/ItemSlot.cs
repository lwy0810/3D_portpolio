using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 아이템 목록의 한 줄. 아이콘 - 이름 - 수량 구조다.
///
/// 런타임에 만들어지므로 인스펙터 연결이 없다. Build 가 자식들을 만들고
/// 참조를 필드에 담아, 이후 Set 으로 내용만 갈아 끼운다.
/// 줄을 매번 파괴하고 다시 만들면 카테고리를 넘길 때마다 GC 가 튄다.
/// </summary>
public class ItemSlot : MonoBehaviour
{
    private Image _highlight;
    private Image _icon;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _countText;
    private RectTransform _rect;

    private ItemData _item;

    public ItemData Item => _item;
    public RectTransform Rect => _rect;

    /// <summary>비어 있는 줄인지. 목록이 짧으면 남는 줄은 감춘다.</summary>
    public bool IsEmpty => _item == null;

    public void Build(RectTransform parent, ItemMenuStyle style, int index)
    {
        _rect = GetComponent<RectTransform>();
        _rect.SetParent(parent, false);

        // 왼쪽 위 기준으로 아래로 쌓는다. 목록은 위에서 아래로 읽는다
        _rect.anchorMin = new Vector2(0.0f, 1.0f);
        _rect.anchorMax = new Vector2(0.0f, 1.0f);
        _rect.pivot = new Vector2(0.0f, 1.0f);
        _rect.sizeDelta = new Vector2(style.RowWidth, style.RowHeight);
        _rect.anchoredPosition = new Vector2(style.RowInsetX,
                                             -(style.RowInsetY + index * style.RowHeight));

        // ── 선택 강조 ────────────────────────────────────────
        // 아이콘·글자보다 먼저 만들어 뒤에 깔린다 (형제 순서 = 그리는 순서)
        _highlight = MakeImage("Highlight", _rect, style.RowHighlight);
        Stretch(_highlight.rectTransform);
        _highlight.enabled = false;

        // ── 아이콘 ───────────────────────────────────────────
        _icon = MakeImage("Icon", _rect, Color.white);
        RectTransform _ir = _icon.rectTransform;
        _ir.anchorMin = new Vector2(0.0f, 0.5f);
        _ir.anchorMax = new Vector2(0.0f, 0.5f);
        _ir.pivot = new Vector2(0.0f, 0.5f);
        _ir.sizeDelta = new Vector2(style.IconSize, style.IconSize);
        _ir.anchoredPosition = new Vector2(style.IconInsetX, 0.0f);
        _icon.preserveAspect = true;

        // ── 이름 ─────────────────────────────────────────────
        _nameText = MakeText("Name", _rect, style, style.NameColor,
                             TextAlignmentOptions.Left);
        RectTransform _nr = _nameText.rectTransform;
        _nr.anchorMin = new Vector2(0.0f, 0.0f);
        _nr.anchorMax = new Vector2(1.0f, 1.0f);
        _nr.offsetMin = new Vector2(style.IconInsetX + style.IconSize + style.NameGap, 0.0f);
        _nr.offsetMax = new Vector2(-style.CountWidth, 0.0f);

        // ── 수량 (× 88) ──────────────────────────────────────
        _countText = MakeText("Count", _rect, style, style.CountColor,
                              TextAlignmentOptions.Right);
        RectTransform _cr = _countText.rectTransform;
        _cr.anchorMin = new Vector2(1.0f, 0.0f);
        _cr.anchorMax = new Vector2(1.0f, 1.0f);
        _cr.pivot = new Vector2(1.0f, 0.5f);
        _cr.sizeDelta = new Vector2(style.CountWidth, 0.0f);
        _cr.anchoredPosition = new Vector2(-style.CountInsetX, 0.0f);
    }

    /// <summary>내용을 채운다. item 이 null 이면 줄을 감춘다.</summary>
    public void Set(ItemData item, Sprite icon)
    {
        _item = item;

        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        _nameText.text = item.Name;
        _countText.text = $"×  {item.Count}";

        _icon.sprite = icon;

        // 스프라이트가 없으면 흰 사각형이 남아 이름을 가린다
        _icon.enabled = icon != null;
    }

    public void SetSelected(bool selected)
    {
        if (_highlight != null) _highlight.enabled = selected;
    }

    // ── 만들기 도우미 ───────────────────────────────────────

    private static Image MakeImage(string name, RectTransform parent, Color color)
    {
        GameObject _obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _obj.transform.SetParent(parent, false);

        Image _img = _obj.GetComponent<Image>();
        _img.color = color;
        _img.raycastTarget = false;
        return _img;
    }

    private static TextMeshProUGUI MakeText(string name, RectTransform parent,
                                            ItemMenuStyle style, Color color,
                                            TextAlignmentOptions align)
    {
        GameObject _obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        _obj.transform.SetParent(parent, false);

        TextMeshProUGUI _t = _obj.GetComponent<TextMeshProUGUI>();

        // 기본 LiberationSans 에는 한글 글리프가 없어 그대로 두면 글자가 깨진다
        if (style.Font != null) _t.font = style.Font;

        _t.fontSize = style.RowFontSize;
        _t.color = color;
        _t.alignment = align;
        _t.textWrappingMode = TextWrappingModes.NoWrap;
        _t.overflowMode = TextOverflowModes.Overflow;
        _t.raycastTarget = false;
        return _t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
