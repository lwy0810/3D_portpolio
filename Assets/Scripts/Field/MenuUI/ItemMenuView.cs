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
/// 이 화면은 SetActive 가 아니라 CanvasGroup.alpha 로 열리고 닫힌다 (FieldMenuView 방식).
/// 따라서 OnEnable 은 씬 로드 시 한 번만 호출되며, 표시 여부는 매 프레임 확인한다.
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

    [Header("폰트 (Resources 기준)")]
    [Tooltip("한글 TMP 폰트 에셋 경로. 찾지 못하면 아래 대체 목록을 순서대로 시도한다")]
    [SerializeField] private string _fontPath = "Fonts & Materials/NotoSansKR-Regular SDF";

    [Header("입력")]
    [Tooltip("1 / 3 키는 항상 동작한다. 여기 지정한 키는 추가로 받는다")]
    [SerializeField] private KeyCode _prevCategoryKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _nextCategoryKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode _prevItemKey = KeyCode.W;
    [SerializeField] private KeyCode _nextItemKey = KeyCode.S;

    [Tooltip("숨겨진 동안 키 입력을 무시한다. 문제가 생기면 끄면 항상 입력을 받는다")]
    [SerializeField] private bool _blockInputWhenHidden = true;

    [Header("치수와 색")]
    [Tooltip("위치는 화면 상단 중앙 기준 오프셋임")]
    [SerializeField] private ItemMenuStyle _layout = new ItemMenuStyle();

    /// <summary>
    /// 지정 경로에 폰트가 없을 때 시도할 순서.
    /// 프로젝트에 NotoSansKR-Regular SDF 는 아직 없으므로 VF(가변, Regular 포함)로 대체한다.
    /// </summary>
    private static readonly string[] FontFallbacks =
    {
        "Fonts & Materials/NotoSansKR-Regular SDF",
        "Fonts & Materials/NotoSansKR-VF SDF",
        "Fonts & Materials/NotoSansKR-ExtraBold SDF",
        "Fonts & Materials/NotoSerifKR-Regular SDF"
    };

    // ── 만들어진 것들 ───────────────────────────────────────
    private RectTransform _root;
    private RectTransform _strip;
    private RectTransform _panel;
    private Image _marker;
    private ItemMenuNudge _markerNudge;

    private readonly List<Image> _tabBoxes = new List<Image>();
    private readonly List<TextMeshProUGUI> _tabTexts = new List<TextMeshProUGUI>();
    private readonly List<ItemSlot> _slots = new List<ItemSlot>();

    private Sprite _boxSprite;
    private Sprite _markerSprite;

    private readonly Dictionary<int, Sprite> _iconCache = new Dictionary<int, Sprite>();

    private List<ItemData> _shown = new List<ItemData>();

    private CanvasGroup[] _groups;

    private int _categoryIndex;
    private int _itemIndex;
    private int _scroll;
    private bool _built;
    private bool _wasVisible;

    public string CurrentCategory =>
        ItemDataBase.Categories[Mathf.Clamp(_categoryIndex, 0, ItemDataBase.Categories.Length - 1)];

    public ItemData SelectedItem =>
        (_itemIndex >= 0 && _itemIndex < _shown.Count) ? _shown[_itemIndex] : null;

    void OnEnable()
    {
        // 메뉴가 열릴 때마다 만들지 않는다. 한 번 만든 뒤에는 내용만 갈아 끼운다
        if (!_built) Build();
    }

    void Update()
    {
        if (!_built) return;

        bool _visible = !_blockInputWhenHidden || IsVisible();

        // 이 화면은 alpha 로 열리므로, 열린 순간을 감지해 선택을 처음으로 되돌린다
        if (_visible && !_wasVisible)
        {
            _categoryIndex = 0;
            RefreshCategory();
        }
        _wasVisible = _visible;

        // 숨겨진 동안 입력을 받으면 다른 메뉴를 조작하는 중에 카테고리가 바뀐다
        if (!_visible) return;

        if (PrevPressed()) MoveCategory(-1);
        else if (NextPressed()) MoveCategory(1);
        else if (Input.GetKeyDown(_prevItemKey)) MoveItem(-1);
        else if (Input.GetKeyDown(_nextItemKey)) MoveItem(1);
    }

    // ── 표시 여부 ───────────────────────────────────────────

    /// <summary>
    /// 상위 CanvasGroup 까지 곱해 실제로 보이는지 판정한다.
    /// 자기 CanvasGroup 만 보면 메뉴 전체가 닫힌 상태(MenuView alpha 0)를 놓친다.
    /// </summary>
    private bool IsVisible()
    {
        if (_groups == null) _groups = GetComponentsInParent<CanvasGroup>(true);

        float _alpha = 1.0f;

        for (int i = 0; i < _groups.Length; i++)
        {
            if (_groups[i] == null || !_groups[i].enabled) continue;

            _alpha *= _groups[i].alpha;

            if (_groups[i].ignoreParentGroups) break;
        }

        return _alpha > 0.01f;
    }

    // 1 키와 3 키는 인스펙터 값과 무관하게 항상 받는다.
    // 참조 화면의 좌우 아이콘이 1 과 3 이며, 씬에 이미 저장된 KeyCode 값을
    // 코드 기본값 변경만으로는 바꿀 수 없으므로 여기서 직접 처리한다.
    private bool PrevPressed() =>
        Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) ||
        Input.GetKeyDown(_prevCategoryKey);

    private bool NextPressed() =>
        Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3) ||
        Input.GetKeyDown(_nextCategoryKey);

    // ── 만들기 ──────────────────────────────────────────────

    private void Build()
    {
        _root = GetComponent<RectTransform>();

        if (_root == null)
        {
            Debug.LogError("[ItemMenuView] RectTransform 이 없습니다. Canvas 아래에 두어야 합니다.");
            return;
        }

        // 씬에 직렬화된 값이 없는 경우를 대비함
        if (_layout == null) _layout = new ItemMenuStyle();

        _layout.Normalize();

        ItemDataBase.Ensure();

        _boxSprite = Resources.Load<Sprite>(_boxSpritePath);
        if (_boxSprite == null)
        {
            Debug.LogWarning($"[ItemMenuView] {_boxSpritePath} 를 찾지 못했습니다. 단색 사각형으로 대체합니다.");
        }

        _markerSprite = Resources.Load<Sprite>(_markerSpritePath);

        ResolveFont();

        BuildCategoryStrip();
        BuildItemPanel();

        _built = true;
    }

    /// <summary>
    /// 한글 폰트. 인스펙터에 직접 지정한 것이 있으면 그것을 쓰고, 없으면 경로로 로드한다.
    /// 기본 LiberationSans 에는 한글 글리프가 없어 그대로 두면 글자가 깨진다.
    /// </summary>
    private void ResolveFont()
    {
        if (_layout.Font != null) return;

        if (!string.IsNullOrEmpty(_fontPath))
        {
            _layout.Font = Resources.Load<TMP_FontAsset>(_fontPath);
            if (_layout.Font != null) return;
        }

        for (int i = 0; i < FontFallbacks.Length; i++)
        {
            _layout.Font = Resources.Load<TMP_FontAsset>(FontFallbacks[i]);

            if (_layout.Font != null)
            {
                Debug.Log($"[ItemMenuView] 폰트 대체 : {FontFallbacks[i]}");
                return;
            }
        }

        Debug.LogWarning("[ItemMenuView] 한글 TMP 폰트를 찾지 못했습니다. " +
                         "TextMesh Pro/Resources/Fonts & Materials 에 NotoSansKR SDF 에셋이 있는지 확인하세요.");
    }

    private void BuildCategoryStrip()
    {
        _strip = MakeRect("CategoryStrip", _root);

        // 상단 중앙 기준. 폭이 바뀌어도 좌우 여백이 같게 유지된다
        _strip.anchorMin = new Vector2(0.5f, 1.0f);
        _strip.anchorMax = new Vector2(0.5f, 1.0f);
        _strip.pivot = new Vector2(0.5f, 1.0f);
        _strip.anchoredPosition = _layout.StripOffset;
        _strip.sizeDelta = new Vector2(10.0f, _layout.TabSize.y);

        float _x = 0.0f;

        // CATEGORIES 라벨. 배경은 목록과 같은 임시 스프라이트를 쓴다
        Image _labelBox = MakeSpriteImage("CategoriesLabel", _strip, _boxSprite, _layout.LabelBoxTint);
        Place(_labelBox.rectTransform, _x, _layout.LabelSize, Vector2.zero);

        TextMeshProUGUI _labelText = MakeText("Text", _labelBox.rectTransform,
                                              _layout.LabelFontSize, _layout.LabelTextColor,
                                              TextAlignmentOptions.Center);
        StretchTo(_labelText.rectTransform);
        _labelText.text = "CATEGORIES";
        //_labelText.fontStyle = FontStyles.Bold;

        _x += _layout.LabelSize.x + _layout.ArrowGap;

        // 좌측 버튼
        Image _prev = MakeSpriteImage("PrevSelectButton", _strip,
                                      Resources.Load<Sprite>(_prevSpritePath), Color.white);
        Place(_prev.rectTransform, _x, _layout.ArrowSize, _layout.PrevOffset);
        AddClick(_prev.gameObject, -1);

        // 왼쪽을 가리키는 버튼이므로 왼쪽으로 먼저 나간다
        AddNudge(_prev.gameObject, Vector2.left);

        _x += _layout.ArrowSize.x + _layout.ArrowGap;

        // 카테고리 탭
        for (int i = 0; i < ItemDataBase.Categories.Length; i++)
        {
            Image _box = MakeSpriteImage($"CategoryTab_{i}", _strip, _boxSprite, _layout.TabTint);
            Place(_box.rectTransform, _x, _layout.TabSize, Vector2.zero);

            TextMeshProUGUI _text = MakeText("Text", _box.rectTransform,
                                             _layout.TabFontSize, _layout.TabTextColor,
                                             TextAlignmentOptions.Center);
            StretchTo(_text.rectTransform);
            _text.text = ItemDataBase.Categories[i];
            //_text.fontStyle = FontStyles.Bold;

            AddSelect(_box.gameObject, i);

            _tabBoxes.Add(_box);
            _tabTexts.Add(_text);

            _x += _layout.TabSize.x + _layout.TabGap;
        }

        _x += _layout.ArrowGap - _layout.TabGap;

        // 우측 버튼
        Image _next = MakeSpriteImage("NextSelectButton", _strip,
                                      Resources.Load<Sprite>(_nextSpritePath), Color.white);
        Place(_next.rectTransform, _x, _layout.ArrowSize, _layout.NextOffset);
        AddClick(_next.gameObject, 1);

        // 오른쪽을 가리키는 버튼이므로 오른쪽으로 먼저 나간다.
        // Prev 와 방향이 반대여서 두 버튼이 좌우로 함께 벌어졌다 모인다
        AddNudge(_next.gameObject, Vector2.right);

        _x += _layout.ArrowSize.x;

        // 자식을 다 놓은 뒤 폭을 확정한다. 자식은 왼쪽 변 기준이라 폭이 바뀌면 함께 이동한다
        _strip.sizeDelta = new Vector2(_x, _layout.TabSize.y);
    }

    private void BuildItemPanel()
    {
        Image _panelImage = MakeSpriteImage("ItemListPanel", _root, _boxSprite, _layout.PanelTint);
        _panel = _panelImage.rectTransform;

        _panel.anchorMin = new Vector2(0.5f, 1.0f);
        _panel.anchorMax = new Vector2(0.5f, 1.0f);
        _panel.pivot = new Vector2(0.5f, 1.0f);
        _panel.anchoredPosition = _layout.PanelOffset;
        _panel.sizeDelta = _layout.PanelSize;

        // 선택 표식. 패널 왼쪽 바깥에 둔다
        if (_markerSprite != null)
        {
            _marker = MakeSpriteImage("SelectMarker", _panel, _markerSprite, _layout.MarkerTint);

            RectTransform _mr = _marker.rectTransform;
            _mr.anchorMin = new Vector2(0.0f, 1.0f);
            _mr.anchorMax = new Vector2(0.0f, 1.0f);

            // 피벗을 중앙에 둔다. 회전이 피벗을 중심으로 일어나므로, 한쪽 변에 두면
            // 돌리는 순간 표식이 그 변을 축으로 밀려나간다
            _mr.pivot = new Vector2(0.5f, 0.5f);
            _mr.sizeDelta = _layout.MarkerSize;

            // Marker.png 는 아래를 향하는 화살표다. 90 도 돌려 오른쪽을 향하게 한다
            _mr.localRotation = Quaternion.Euler(0.0f, 0.0f, _layout.MarkerRotation);

            // 가리키는 방향으로 흔든다
            _markerNudge = AddNudge(_marker.gameObject, Vector2.right);
        }

        int _rows = Mathf.Max(1, _layout.VisibleRows);

        for (int i = 0; i < _rows; i++)
        {
            GameObject _obj = new GameObject($"ItemSlot_{i}",
                                             typeof(RectTransform), typeof(ItemSlot));
            ItemSlot _slot = _obj.GetComponent<ItemSlot>();
            _slot.Build(_panel, _layout, i);
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

            _tabBoxes[i].color = _on ? _layout.TabSelectedTint : _layout.TabTint;
            _tabTexts[i].color = _on ? _layout.TabSelectedTextColor : _layout.TabTextColor;

            // 선택된 탭만 살짝 키운다. 원본도 선택 탭이 위로 튀어나온다
            float _s = _on ? _layout.TabSelectedScale : 1.0f;
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

        // 줄의 세로 중앙. 패널 좌상단이 기준점이다
        float _y = -(_layout.RowInsetY + _row * _layout.RowHeight + _layout.RowHeight * 0.5f);

        // 패널 왼쪽 바깥. 피벗이 중앙이므로 이 값이 표식의 중심이다
        float _x = _layout.RowInsetX - _layout.MarkerOutset;

        Vector2 _pos = new Vector2(_x, _y) + _layout.MarkerOffset;

        // anchoredPosition 을 직접 쓰면 흔들린 값이 기준으로 굳어 위치가 밀린다
        if (_markerNudge != null) _markerNudge.SetBase(_pos);
        else _marker.rectTransform.anchoredPosition = _pos;
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

    private TextMeshProUGUI MakeText(string name, RectTransform parent,
                                     float size, Color color,
                                     TextAlignmentOptions align)
    {
        GameObject _obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        _obj.transform.SetParent(parent, false);

        TextMeshProUGUI _t = _obj.GetComponent<TextMeshProUGUI>();

        if (_layout.Font != null) _t.font = _layout.Font;

        _t.fontSize = size;
        _t.color = color;
        _t.alignment = align;
        _t.textWrappingMode = TextWrappingModes.NoWrap;
        _t.overflowMode = TextOverflowModes.Overflow;
        _t.raycastTarget = false;
        return _t;
    }

    /// <summary>탭 줄 안에서 왼쪽부터 순서대로 놓는다.</summary>
    private static void Place(RectTransform rt, float x, Vector2 size, Vector2 offset)
    {
        rt.anchorMin = new Vector2(0.0f, 0.5f);
        rt.anchorMax = new Vector2(0.0f, 0.5f);
        rt.pivot = new Vector2(0.0f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(x, 0.0f) + offset;
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
        ItemMenuClickRelay _relay = obj.AddComponent<ItemMenuClickRelay>();
        _relay.Bind(() => MoveCategory(delta));
    }

    /// <summary>
    /// 좌우 흔들림. 진폭이 0 이면 붙이지 않는다 —
    /// 동작하지 않는 컴포넌트가 하이어라키에 남으면 오해를 부른다.
    /// </summary>
    private ItemMenuNudge AddNudge(GameObject obj, Vector2 direction)
    {
        if (_layout.NudgeAmplitude <= 0.0f) return null;

        ItemMenuNudge _nudge = obj.AddComponent<ItemMenuNudge>();
        _nudge.Bind(direction, _layout.NudgeAmplitude, _layout.NudgePeriod, _layout.NudgePause);
        return _nudge;
    }

    /// <summary>탭 직접 클릭.</summary>
    private void AddSelect(GameObject obj, int index)
    {
        ItemMenuClickRelay _relay = obj.AddComponent<ItemMenuClickRelay>();
        _relay.Bind(() => SelectCategory(index));
    }

    // ── 점검 ────────────────────────────────────────────────

    [ContextMenu("치수와 색 기본값으로 되돌리기")]
    private void ResetLayout()
    {
        _layout = new ItemMenuStyle();
        Debug.Log("[ItemMenuView] 치수와 색을 기본값으로 되돌렸습니다. 씬을 저장하세요.");
    }
}
