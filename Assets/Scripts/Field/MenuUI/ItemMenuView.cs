using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 아이템 메뉴 화면. 카테고리 탭 줄과 아이템 목록을 런타임에 만든다.
///
/// 구성:
///   CategoryStrip ─ CategoriesLabel · PrevSelectButton · 탭 N개 · NextSelectButton
///   ItemListPanel ─ 배경 · ItemSlot N개 · 선택 표식
///
/// 왜 런타임 생성인가:
/// 카테고리 수와 아이템 수가 CSV 에 따라 바뀐다. 씬에 미리 깔아두면 CSV 를 고칠 때마다
/// 씬도 같이 고쳐야 하고, 두 곳이 어긋나면 조용히 빈 칸이 남는다.
/// ActionBar 도 같은 이유로 런타임 생성이다.
///
/// 배경 스프라이트는 임시로 CraftBox 를 쓴다. 전용 리소스가 준비되면 경로만 바꾼다.
/// </summary>
public class ItemMenuView : MonoBehaviour
{
    [Header("스프라이트 경로 (Resources 기준)")]
    [Tooltip("목록 배경과 카테고리 박스에 임시로 쓰는 스프라이트")]
    [SerializeField] private string _boxSpritePath = "Images/CraftBox";
    [Tooltip("카테고리 좌측 버튼")]
    [SerializeField] private string _prevSpritePath = "Images/PrevSelectInfo";
    [Tooltip("카테고리 우측 버튼")]
    [SerializeField] private string _nextSpritePath = "Images/NextSelectInfo";
    [Tooltip("선택된 줄 왼쪽 표식. 없으면 표시하지 않는다")]
    [SerializeField] private string _markerSpritePath = "Images/Marker";
    [Tooltip("아이템 아이콘 경로 형식. {0} 에 iconIndex 가 3자리로 들어간다")]
    [SerializeField] private string _iconPathFormat = "Icons/skill_{0:000}";

    [Header("입력")]
    [SerializeField] private KeyCode _prevCategoryKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode _nextCategoryKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode _prevItemKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode _nextItemKey = KeyCode.DownArrow;

    [Header("치수와 색")]
    [SerializeField] private ItemMenuStyle _style = new ItemMenuStyle();

    // ── 만들어진 것들 ───────────────────────────────────────
    private RectTransform _root;
    private RectTransform _strip;
    private RectTransform _panel;
    private Image _marker;

    private readonly List<Image> _tabBoxes = new List<Image>();
    private readonly List<TextMeshProUGUI> _tabTexts = new List<TextMeshProUGUI>();
    private readonly List<ItemSlot> _slots = new List<ItemSlot>();

    private Sprite _boxSprite;
    private Sprite _markerSprite;

    private readonly Dictionary<int, Sprite> _iconCache = new Dictionary<int, Sprite>();

    private List<ItemData> _shown = new List<ItemData>();

    private int _categoryIndex;
    private int _itemIndex;
    private int _scroll;
    private bool _built;

    public string CurrentCategory =>
        ItemDataBase.Categories[Mathf.Clamp(_categoryIndex, 0, ItemDataBase.Categories.Length - 1)];

    public ItemData SelectedItem =>
        (_itemIndex >= 0 && _itemIndex < _shown.Count) ? _shown[_itemIndex] : null;

    void OnEnable()
    {
        // 메뉴가 열릴 때마다 만들지 않는다. 한 번 만든 뒤에는 내용만 갈아 끼운다
        if (!_built) Build();

        _categoryIndex = 0;
        _itemIndex = 0;
        _scroll = 0;

        RefreshCategory();
    }

    void Update()
    {
        if (!_built) return;

        if (Input.GetKeyDown(_prevCategoryKey)) MoveCategory(-1);
        else if (Input.GetKeyDown(_nextCategoryKey)) MoveCategory(1);
        else if (Input.GetKeyDown(_prevItemKey)) MoveItem(-1);
        else if (Input.GetKeyDown(_nextItemKey)) MoveItem(1);
    }

    // ── 만들기 ──────────────────────────────────────────────

    private void Build()
    {
        _root = GetComponent<RectTransform>();

        if (_root == null)
        {
            Debug.LogError("[ItemMenuView] RectTransform 이 없습니다. Canvas 아래에 두어야 합니다.");
            return;
        }

        ItemDataBase.Ensure();

        _boxSprite = Resources.Load<Sprite>(_boxSpritePath);
        if (_boxSprite == null)
        {
            Debug.LogWarning($"[ItemMenuView] {_boxSpritePath} 를 찾지 못했습니다. 단색 사각형으로 대체합니다.");
        }

        _markerSprite = Resources.Load<Sprite>(_markerSpritePath);

        BuildCategoryStrip();
        BuildItemPanel();

        _built = true;
    }

    private void BuildCategoryStrip()
    {
        _strip = MakeRect("CategoryStrip", _root);
        _strip.anchorMin = new Vector2(0.0f, 1.0f);
        _strip.anchorMax = new Vector2(0.0f, 1.0f);
        _strip.pivot = new Vector2(0.0f, 1.0f);
        _strip.anchoredPosition = _style.StripPosition;
        _strip.sizeDelta = new Vector2(10.0f, _style.TabSize.y);

        float _x = 0.0f;

        // CATEGORIES 라벨. 배경은 목록과 같은 임시 스프라이트를 쓴다
        Image _labelBox = MakeSpriteImage("CategoriesLabel", _strip, _boxSprite, _style.LabelBoxTint);
        Place(_labelBox.rectTransform, _x, _style.LabelSize);

        TextMeshProUGUI _labelText = MakeText("Text", _labelBox.rectTransform,
                                              _style.LabelFontSize, _style.LabelTextColor,
                                              TextAlignmentOptions.Center);
        StretchTo(_labelText.rectTransform);
        _labelText.text = "CATEGORIES";
        _labelText.fontStyle = FontStyles.Bold;

        _x += _style.LabelSize.x + _style.ArrowGap;

        // 좌측 버튼
        Image _prev = MakeSpriteImage("PrevSelectButton", _strip,
                                      Resources.Load<Sprite>(_prevSpritePath), Color.white);
        Place(_prev.rectTransform, _x, _style.ArrowSize);
        AddClick(_prev.gameObject, -1);

        _x += _style.ArrowSize.x + _style.ArrowGap;

        // 카테고리 탭
        for (int i = 0; i < ItemDataBase.Categories.Length; i++)
        {
            Image _box = MakeSpriteImage($"CategoryTab_{i}", _strip, _boxSprite, _style.TabTint);
            Place(_box.rectTransform, _x, _style.TabSize);

            TextMeshProUGUI _text = MakeText("Text", _box.rectTransform,
                                             _style.TabFontSize, _style.TabTextColor,
                                             TextAlignmentOptions.Center);
            StretchTo(_text.rectTransform);
            _text.text = ItemDataBase.Categories[i];
            _text.fontStyle = FontStyles.Bold;

            AddSelect(_box.gameObject, i);

            _tabBoxes.Add(_box);
            _tabTexts.Add(_text);

            _x += _style.TabSize.x + _style.TabGap;
        }

        _x += _style.ArrowGap - _style.TabGap;

        // 우측 버튼
        Image _next = MakeSpriteImage("NextSelectButton", _strip,
                                      Resources.Load<Sprite>(_nextSpritePath), Color.white);
        Place(_next.rectTransform, _x, _style.ArrowSize);
        AddClick(_next.gameObject, 1);

        _x += _style.ArrowSize.x;

        _strip.sizeDelta = new Vector2(_x, _style.TabSize.y);
    }

    private void BuildItemPanel()
    {
        Image _panelImage = MakeSpriteImage("ItemListPanel", _root, _boxSprite, _style.PanelTint);
        _panel = _panelImage.rectTransform;

        _panel.anchorMin = new Vector2(0.0f, 1.0f);
        _panel.anchorMax = new Vector2(0.0f, 1.0f);
        _panel.pivot = new Vector2(0.0f, 1.0f);
        _panel.anchoredPosition = _style.PanelPosition;
        _panel.sizeDelta = _style.PanelSize;

        // 선택 표식. 패널 왼쪽 바깥에 둔다
        if (_markerSprite != null)
        {
            _marker = MakeSpriteImage("SelectMarker", _panel, _markerSprite, _style.MarkerTint);

            RectTransform _mr = _marker.rectTransform;
            _mr.anchorMin = new Vector2(0.0f, 1.0f);
            _mr.anchorMax = new Vector2(0.0f, 1.0f);
            _mr.pivot = new Vector2(1.0f, 0.5f);
            _mr.sizeDelta = _style.MarkerSize;
        }

        int _rows = Mathf.Max(1, _style.VisibleRows);

        for (int i = 0; i < _rows; i++)
        {
            GameObject _obj = new GameObject($"ItemSlot_{i}",
                                             typeof(RectTransform), typeof(ItemSlot));
            ItemSlot _slot = _obj.GetComponent<ItemSlot>();
            _slot.Build(_panel, _style, i);
            _slots.Add(_slot);
        }
    }

    // ── 갱신 ────────────────────────────────────────────────

    /// <summary>카테고리가 바뀌었을 때. 목록을 다시 읽고 선택을 맨 위로 되돌린다.</summary>
    public void RefreshCategory()
    {
        if (!_built) return;

        ItemDataBase _db = ItemDataBase.Ensure();
        _shown = _db.ByCategory(CurrentCategory);

        _itemIndex = 0;
        _scroll = 0;

        RefreshTabs();
        RefreshRows();
    }

    private void RefreshTabs()
    {
        for (int i = 0; i < _tabBoxes.Count; i++)
        {
            bool _on = i == _categoryIndex;

            _tabBoxes[i].color = _on ? _style.TabSelectedTint : _style.TabTint;
            _tabTexts[i].color = _on ? _style.TabSelectedTextColor : _style.TabTextColor;

            // 선택된 탭만 살짝 키운다. 원본도 선택 탭이 위로 튀어나온다
            float _s = _on ? _style.TabSelectedScale : 1.0f;
            _tabBoxes[i].rectTransform.localScale = new Vector3(_s, _s, 1.0f);
        }
    }

    private void RefreshRows()
    {
        int _rows = _slots.Count;

        // 선택이 화면 밖으로 나가면 그만큼 스크롤한다
        if (_itemIndex < _scroll) _scroll = _itemIndex;
        else if (_itemIndex >= _scroll + _rows) _scroll = _itemIndex - _rows + 1;

        _scroll = Mathf.Clamp(_scroll, 0, Mathf.Max(0, _shown.Count - _rows));

        for (int i = 0; i < _rows; i++)
        {
            int _dataIndex = _scroll + i;

            ItemData _item = _dataIndex < _shown.Count ? _shown[_dataIndex] : null;

            _slots[i].Set(_item, _item != null ? IconOf(_item.IconIndex) : null);
            _slots[i].SetSelected(_item != null && _dataIndex == _itemIndex);
        }

        RefreshMarker();
    }

    private void RefreshMarker()
    {
        if (_marker == null) return;

        int _row = _itemIndex - _scroll;

        bool _visible = _shown.Count > 0 && _row >= 0 && _row < _slots.Count;
        _marker.enabled = _visible;

        if (!_visible) return;

        float _y = -(_style.RowInsetY + _row * _style.RowHeight + _style.RowHeight * 0.5f);
        _marker.rectTransform.anchoredPosition = new Vector2(_style.MarkerOutset * -1.0f + _style.RowInsetX, _y);
    }

    // ── 이동 ────────────────────────────────────────────────

    public void MoveCategory(int delta)
    {
        int _n = ItemDataBase.Categories.Length;
        if (_n <= 0) return;

        // 양쪽 끝에서 반대편으로 넘어간다. 원본도 순환한다
        _categoryIndex = (_categoryIndex + delta + _n) % _n;

        RefreshCategory();
    }

    public void SelectCategory(int index)
    {
        int _n = ItemDataBase.Categories.Length;
        if (index < 0 || index >= _n) return;

        _categoryIndex = index;
        RefreshCategory();
    }

    public void MoveItem(int delta)
    {
        if (_shown.Count == 0) return;

        _itemIndex = Mathf.Clamp(_itemIndex + delta, 0, _shown.Count - 1);
        RefreshRows();
    }

    // ── 아이콘 ──────────────────────────────────────────────

    /// <summary>
    /// 아이콘. 한 번 읽은 것은 캐시한다.
    /// 아이템 전용 아이콘이 없어 임시로 스킬 아이콘을 쓴다 — 경로 형식만 바꾸면 된다.
    /// </summary>
    private Sprite IconOf(int iconIndex)
    {
        if (iconIndex <= 0) return null;

        Sprite _cached;
        if (_iconCache.TryGetValue(iconIndex, out _cached)) return _cached;

        string _path = string.Format(_iconPathFormat, iconIndex);
        Sprite _sprite = Resources.Load<Sprite>(_path);

        if (_sprite == null)
        {
            Debug.LogWarning($"[ItemMenuView] 아이콘 {_path} 를 찾지 못했습니다.");
        }

        _iconCache[iconIndex] = _sprite;
        return _sprite;
    }

    // ── 만들기 도우미 ───────────────────────────────────────

    private static RectTransform MakeRect(string name, RectTransform parent)
    {
        GameObject _obj = new GameObject(name, typeof(RectTransform));
        _obj.transform.SetParent(parent, false);
        return _obj.GetComponent<RectTransform>();
    }

    private static Image MakeSpriteImage(string name, RectTransform parent,
                                         Sprite sprite, Color tint)
    {
        GameObject _obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _obj.transform.SetParent(parent, false);

        Image _img = _obj.GetComponent<Image>();
        _img.sprite = sprite;
        _img.color = tint;

        // 9슬라이스 정보가 있으면 늘려도 테두리가 뭉개지지 않는다
        _img.type = (sprite != null && sprite.border != Vector4.zero)
                    ? Image.Type.Sliced
                    : Image.Type.Simple;

        return _img;
    }

    private static TextMeshProUGUI MakeText(string name, RectTransform parent,
                                            float size, Color color,
                                            TextAlignmentOptions align)
    {
        GameObject _obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        _obj.transform.SetParent(parent, false);

        TextMeshProUGUI _t = _obj.GetComponent<TextMeshProUGUI>();
        _t.fontSize = size;
        _t.color = color;
        _t.alignment = align;
        _t.textWrappingMode = TextWrappingModes.NoWrap;
        _t.overflowMode = TextOverflowModes.Overflow;
        _t.raycastTarget = false;
        return _t;
    }

    /// <summary>탭 줄 안에서 왼쪽부터 순서대로 놓는다.</summary>
    private static void Place(RectTransform rt, float x, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.0f, 0.5f);
        rt.anchorMax = new Vector2(0.0f, 0.5f);
        rt.pivot = new Vector2(0.0f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(x, 0.0f);
    }

    private static void StretchTo(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Prev / Next 버튼. 마우스로도 넘길 수 있게 한다.</summary>
    private void AddClick(GameObject obj, int delta)
    {
        Button _btn = obj.AddComponent<Button>();
        _btn.onClick.AddListener(() => MoveCategory(delta));
    }

    /// <summary>탭 직접 클릭.</summary>
    private void AddSelect(GameObject obj, int index)
    {
        Button _btn = obj.AddComponent<Button>();
        _btn.onClick.AddListener(() => SelectCategory(index));
    }
}
