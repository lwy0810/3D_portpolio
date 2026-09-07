using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// StatusMenuView 컨트롤러. StatusMenuView 게임오브젝트에 붙인다.
///
/// 이 화면에는 이미 아래 하이어라키가 만들어져 있다(씬에 존재하지만 아무 스크립트도
/// 붙어있지 않았다). 이 스크립트는 새 오브젝트를 만들지 않고, 그 자리를 채운다:
///
///   LeftMove
///     MemberSelectArea (100x100, PosY -120 부근) ── PrevSelectInfo · MemberList · NextSelectInfo
///     StatusViewBox    ── Name · Lv · HpBox(Hp/Ep/Sp) · StatusBox(STR·DEF·ATS·ADF·SPD·MOV·DEX·AGL·RNG·AVOID·HIT·CRI)
///     CraftView        ── SpecialCraftlBox/SpecialCraftList · CommonCraftBox/CommonCraftList
///     CharacterImage
///   StatusView/StatusView (Manager/StatusView.cs) ── 별도의 상세 스탯 패널. 같이 갱신해준다.
///
/// 왜 SerializeField 참조 대신 transform.Find 인가:
/// ItemMenuView 와 마찬가지로 이 화면도 내용이 파티 구성에 따라 바뀐다. 인스펙터에
/// 하나하나 꽂아두면 개수가 바뀔 때마다 손이 가고, 이름이 이미 하이어라키에
/// 고정돼 있으니 이름으로 찾는 편이 어긋날 일이 적다.
///
/// MOV(이동력) · RNG(사거리) 는 Stat 클래스에 아직 대응 값이 없다. 값 텍스트를
/// "-" 로 둔다 — 나중에 필드가 추가되면 여기만 고치면 된다.
/// </summary>
public class MenuStatus : MonoBehaviour
{
    [Header("스프라이트 경로 (Resources 기준)")]
    [Tooltip("{0} 에 캐릭터 이름이 들어간다. 머리 아이콘은 Character 하위 폴더가 아니라 " +
             "Resources/Image 바로 아래에 {이름}_head.png 로 있다 (Images/Character 와는 다른 폴더다)")]
    [SerializeField] private string _headSpritePathFormat = "Image/{0}_head";
    [Tooltip("HomeMenuView - CharacterImageView 와 동일한 전신 이미지 경로")]
    [SerializeField] private string _bodySpritePathFormat = "Images/Character/{0}";

    [Header("폰트 (Resources 기준, 크래프트 목록에 사용)")]
    [SerializeField] private string _fontPath = "Fonts & Materials/NotoSansKR-Regular SDF";

    [Header("CharacterSelectPanel (MemberSelectArea)")]
    [SerializeField] private Vector2 _panelSize = new Vector2(100.0f, 100.0f);
    [SerializeField] private float _panelPosY = -120.0f;

    [Header("Prev / Next 버튼 (ItemMenuView - CategoryStrip 과 동일 기본값)")]
    [SerializeField] private Vector2 _arrowSize = new Vector2(38.0f, 38.0f);
    [SerializeField] private float _nudgeAmplitude = 5.0f;
    [SerializeField] private float _nudgePeriod = 1.2f;
    [SerializeField] private float _nudgePause = 0.4f;

    [Header("입력")]
    [SerializeField] private KeyCode _prevKey = KeyCode.Q;
    [SerializeField] private KeyCode _nextKey = KeyCode.E;
    [Tooltip("숨겨진 동안 키 입력을 무시한다. ItemMenuView 와 동일한 이유")]
    [SerializeField] private bool _blockInputWhenHidden = true;

    [Header("크래프트 목록 줄 높이")]
    [SerializeField] private float _craftRowHeight = 36.0f;
    [SerializeField] private float _craftFontSize = 22.0f;

    private static readonly string[] FontFallbacks =
    {
        "Fonts & Materials/NotoSansKR-Regular SDF",
        "Fonts & Materials/NotoSansKR-VF SDF",
        "Fonts & Materials/NotoSansKR-ExtraBold SDF",
        "Fonts & Materials/NotoSerifKR-Regular SDF"
    };

    // ── 캐시된 참조 ─────────────────────────────────────────
    private RectTransform _memberSelectArea;
    private RectTransform _memberList;
    private Image _headIcon;
    private GameObject _prevButton;
    private GameObject _nextButton;

    private Image _characterImage;

    private RectTransform _specialCraftList;
    private RectTransform _commonCraftList;

    private Text _nameText, _lvValueText;
    private Text _hpValueText, _hpMaxValueText, _epValueText, _epMaxValueText, _spValueText;
    private Text _strValueText, _defValueText, _atsValueText, _adfValueText, _spdValueText;
    private Text _movValueText, _dexValueText, _aglValueText, _rngValueText;
    private Text _avoidValueText, _hitValueText, _criValueText;

    private StatusMenuView _statusViewComponent;

    private TMP_FontAsset _font;

    private readonly List<GameObject> _specialCraftRows = new List<GameObject>();
    private readonly List<GameObject> _commonCraftRows = new List<GameObject>();

    private List<Character> _party = new List<Character>();
    private int _selected;
    private bool _built;
    private bool _wasVisible;
    private CanvasGroup[] _groups;

    void OnEnable()
    {
        if (!_built) Build();
    }

    void Update()
    {
        if (!_built) return;

        bool _visible = !_blockInputWhenHidden || IsVisible();

        // 이 화면이 alpha 로 열릴 때, 열린 순간을 감지해 0번째 캐릭터로 되돌린다
        if (_visible && !_wasVisible)
        {
            _selected = 0;
            RefreshAll();
        }
        _wasVisible = _visible;

        if (!_visible) return;

        if (Input.GetKeyDown(_prevKey)) MoveMember(-1);
        else if (Input.GetKeyDown(_nextKey)) MoveMember(1);
    }

    /// <summary>상위 CanvasGroup 까지 곱해 실제로 보이는지 판정한다. ItemMenuView 와 동일.</summary>
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

    // ── 만들기 / 참조 잡기 ──────────────────────────────────

    private void Build()
    {
        ResolveFont();
        CacheReferences();
        BuildHeadIcon();
        WireArrowButtons();

        _built = true;
    }

    private void ResolveFont()
    {
        if (!string.IsNullOrEmpty(_fontPath))
        {
            _font = Resources.Load<TMP_FontAsset>(_fontPath);
            if (_font != null) return;
        }

        for (int i = 0; i < FontFallbacks.Length; i++)
        {
            _font = Resources.Load<TMP_FontAsset>(FontFallbacks[i]);
            if (_font != null) return;
        }

        Debug.LogWarning("[MenuStatus] 한글 TMP 폰트를 찾지 못했습니다.");
    }

    private void CacheReferences()
    {
        Transform _leftMove = transform.Find("LeftMove");
        if (_leftMove == null)
        {
            Debug.LogError("[MenuStatus] LeftMove 를 찾지 못했습니다. StatusMenuView 에 붙였는지 확인하세요.");
            return;
        }

        // CharacterSelectPanel (기존 MemberSelectArea)
        _memberSelectArea = Find<RectTransform>(_leftMove, "MemberSelectArea");
        if (_memberSelectArea != null)
        {
            _memberList = Find<RectTransform>(_memberSelectArea, "MemberList");
            _prevButton = ChildObject(_memberSelectArea, "PrevSelectInfo");
            _nextButton = ChildObject(_memberSelectArea, "NextSelectInfo");

            _memberSelectArea.sizeDelta = _panelSize;
            Vector2 _pos = _memberSelectArea.anchoredPosition;
            _memberSelectArea.anchoredPosition = new Vector2(_pos.x, _panelPosY);
        }
        else
        {
            Debug.LogWarning("[MenuStatus] LeftMove/MemberSelectArea 를 찾지 못했습니다.");
        }

        // 전신 이미지
        Transform _charImageT = _leftMove.Find("CharacterImage");
        _characterImage = _charImageT != null ? _charImageT.GetComponent<Image>() : null;
        if (_characterImage == null)
            Debug.LogWarning("[MenuStatus] LeftMove/CharacterImage 를 찾지 못했습니다.");

        // 크래프트 목록
        Transform _craftView = _leftMove.Find("CraftView");
        if (_craftView != null)
        {
            _specialCraftList = Find<RectTransform>(_craftView, "SpecialCraftlBox/SpecialCraftList");
            _commonCraftList = Find<RectTransform>(_craftView, "CommonCraftBox/CommonCraftList");
        }
        else
        {
            Debug.LogWarning("[MenuStatus] LeftMove/CraftView 를 찾지 못했습니다.");
        }

        // 상태 값 텍스트들
        Transform _statusViewBox = _leftMove.Find("StatusViewBox");
        if (_statusViewBox != null)
        {
            _nameText = FindText(_statusViewBox, "Name/NameText");
            _lvValueText = FindText(_statusViewBox, "Lv/LvValueText");

            Transform _hpBox = _statusViewBox.Find("HpBox");
            if (_hpBox != null)
            {
                _hpValueText = FindText(_hpBox, "Hp/HpValueText");
                _hpMaxValueText = FindText(_hpBox, "Hp/HpMaxValueText");
                _epValueText = FindText(_hpBox, "Ep/EpValueText");
                _epMaxValueText = FindText(_hpBox, "Ep/EpMaxValueText");
                _spValueText = FindText(_hpBox, "Sp/SpValueText");
            }

            Transform _statusBox = _statusViewBox.Find("StatusBox");
            if (_statusBox != null)
            {
                _strValueText = FindText(_statusBox, "STR/STRValueText");
                _defValueText = FindText(_statusBox, "DEF/DEFValueText");
                _atsValueText = FindText(_statusBox, "ATS/ATSValueText");
                _adfValueText = FindText(_statusBox, "ADF/ADFValueText");
                _spdValueText = FindText(_statusBox, "SPD/SPDValueText");
                _movValueText = FindText(_statusBox, "MOV/MOVValueText");
                _dexValueText = FindText(_statusBox, "DEX/DEXValueText");
                _aglValueText = FindText(_statusBox, "AGL/AGLValueText");
                _rngValueText = FindText(_statusBox, "RNG/RNGValueText");
                _avoidValueText = FindText(_statusBox, "AVOID/AVOIDValueText");
                _hitValueText = FindText(_statusBox, "HIT/HITValueText");
                _criValueText = FindText(_statusBox, "CRI/CRiValueText");
            }
            else
            {
                Debug.LogWarning("[MenuStatus] LeftMove/StatusViewBox/StatusBox 를 찾지 못했습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[MenuStatus] LeftMove/StatusViewBox 를 찾지 못했습니다.");
        }

        // 상세 스탯 패널 (Manager/StatusView.cs). StatusMenuView 바로 아래 StatusView/StatusView.
        Transform _statusViewInner = transform.Find("StatusView/StatusView");
        _statusViewComponent = _statusViewInner != null ? _statusViewInner.GetComponent<StatusMenuView>() : null;
    }

    private void BuildHeadIcon()
    {
        if (_memberList == null) return;

        GameObject _obj = new GameObject("HeadIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _obj.transform.SetParent(_memberList, false);

        _headIcon = _obj.GetComponent<Image>();

        RectTransform _rt = _headIcon.rectTransform;
        _rt.anchorMin = new Vector2(0.5f, 0.5f);
        _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.sizeDelta = _panelSize;
        _rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>이미 씬에 있는 PrevSelectInfo / NextSelectInfo 에 클릭과 좌우 흔들림을 붙인다.</summary>
    private void WireArrowButtons()
    {
        if (_prevButton != null)
        {
            RectTransform _rt = _prevButton.GetComponent<RectTransform>();
            if (_rt != null) _rt.sizeDelta = _arrowSize;

            ItemMenuClickRelay _relay = _prevButton.GetComponent<ItemMenuClickRelay>();
            if (_relay == null) _relay = _prevButton.AddComponent<ItemMenuClickRelay>();
            _relay.Bind(() => MoveMember(-1));

            AddNudge(_prevButton, Vector2.left);
        }

        if (_nextButton != null)
        {
            RectTransform _rt = _nextButton.GetComponent<RectTransform>();
            if (_rt != null) _rt.sizeDelta = _arrowSize;

            ItemMenuClickRelay _relay = _nextButton.GetComponent<ItemMenuClickRelay>();
            if (_relay == null) _relay = _nextButton.AddComponent<ItemMenuClickRelay>();
            _relay.Bind(() => MoveMember(1));

            AddNudge(_nextButton, Vector2.right);
        }
    }

    private void AddNudge(GameObject obj, Vector2 direction)
    {
        if (_nudgeAmplitude <= 0.0f) return;

        ItemMenuNudge _nudge = obj.GetComponent<ItemMenuNudge>();
        if (_nudge == null) _nudge = obj.AddComponent<ItemMenuNudge>();

        _nudge.Bind(direction, _nudgeAmplitude, _nudgePeriod, _nudgePause);
    }

    // ── 갱신 ────────────────────────────────────────────────

    public void MoveMember(int delta)
    {
        RefreshParty();
        if (_party.Count == 0) return;

        _selected = (_selected + delta + _party.Count) % _party.Count;
        RefreshAll();
    }

    private void RefreshParty()
    {
        if (GameManager.GameInstance == null)
        {
            Debug.LogWarning("[MenuStatus] GameManager 가 없어 초기화를 건너뜁니다.");
            _party.Clear();
            return;
        }

        _party = GameManager.GameInstance.CharacterComponents;
    }

    public void RefreshAll()
    {
        RefreshParty();

        if (_party == null || _party.Count == 0)
        {
            Debug.LogWarning("[MenuStatus] 파티 목록이 비어 있습니다.");
            return;
        }

        _selected = Mathf.Clamp(_selected, 0, _party.Count - 1);
        Character _character = _party[_selected];

        if (_character == null || _character.Stat == null)
        {
            Debug.LogWarning($"[MenuStatus] {_selected}번 캐릭터가 비어 있습니다.");
            return;
        }

        RefreshImages(_character);
        RefreshStatusBox(_character.Stat);
        RefreshCraft(_character.Stat.Name);

        if (_statusViewComponent != null) _statusViewComponent.CharacterStatusView(_character);
    }

    private void RefreshImages(Character character)
    {
        string _name = character.Stat.Name;

        if (_headIcon != null)
        {
            Sprite _head = Resources.Load<Sprite>(string.Format(_headSpritePathFormat, _name));
            if (_head == null) Debug.LogWarning($"[MenuStatus] {string.Format(_headSpritePathFormat, _name)} 를 찾지 못했습니다.");
            _headIcon.sprite = _head;
        }

        if (_characterImage != null)
        {
            Sprite _body = Resources.Load<Sprite>(string.Format(_bodySpritePathFormat, _name));
            if (_body == null) Debug.LogWarning($"[MenuStatus] {string.Format(_bodySpritePathFormat, _name)} 를 찾지 못했습니다.");
            _characterImage.sprite = _body;
        }
    }

    private void RefreshStatusBox(Stat stat)
    {
        SetText(_nameText, stat.Name);
        SetText(_lvValueText, stat.Level.ToString());

        SetText(_hpValueText, stat.Hp.ToString());
        SetText(_hpMaxValueText, stat.MaxHp.ToString());
        SetText(_epValueText, stat.Ep.ToString());
        SetText(_epMaxValueText, stat.MaxEp.ToString());
        SetText(_spValueText, stat.Cp.ToString());

        SetText(_strValueText, stat.Str.ToString());
        SetText(_defValueText, stat.Def.ToString());
        SetText(_atsValueText, stat.Ats.ToString());
        SetText(_adfValueText, stat.Adf.ToString());
        SetText(_spdValueText, stat.Speed.ToString());
        SetText(_dexValueText, stat.Dex.ToString());
        SetText(_aglValueText, stat.Agl.ToString());

        // Stat 에 이동력 / 사거리 값이 아직 없다. 필드가 추가되면 여기만 바꾸면 된다
        SetText(_movValueText, "-");
        SetText(_rngValueText, "-");

        SetText(_avoidValueText, $"{stat.Avoid * 100f:0}%");
        SetText(_hitValueText, $"{stat.Hit * 100f:0}%");
        SetText(_criValueText, $"{stat.Critical * 100f:0}%");
    }

    private void RefreshCraft(string characterName)
    {
        ClearRows(_specialCraftRows);
        ClearRows(_commonCraftRows);

        if (SkillDataBase.Instance == null)
        {
            Debug.LogWarning("[MenuStatus] SkillDataBase 가 없어 크래프트 목록을 건너뜁니다.");
            return;
        }

        List<SkillData> _crafts = SkillDataBase.Instance.Crafts(characterName, includeSCraft: true);

        int _specialIndex = 0;
        int _commonIndex = 0;

        for (int i = 0; i < _crafts.Count; i++)
        {
            SkillData _skill = _crafts[i];

            if (_skill.Type == SkillType.SCraft)
            {
                AddCraftRow(_specialCraftList, _specialCraftRows, _skill, _specialIndex);
                _specialIndex++;
            }
            else
            {
                AddCraftRow(_commonCraftList, _commonCraftRows, _skill, _commonIndex);
                _commonIndex++;
            }
        }
    }

    private void AddCraftRow(RectTransform parent, List<GameObject> rows, SkillData skill, int index)
    {
        if (parent == null) return;

        GameObject _obj = new GameObject($"Craft_{skill.Name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        _obj.transform.SetParent(parent, false);

        RectTransform _rt = _obj.GetComponent<RectTransform>();
        _rt.anchorMin = new Vector2(0.0f, 1.0f);
        _rt.anchorMax = new Vector2(1.0f, 1.0f);
        _rt.pivot = new Vector2(0.5f, 1.0f);
        _rt.sizeDelta = new Vector2(0.0f, _craftRowHeight);
        _rt.anchoredPosition = new Vector2(0.0f, -index * _craftRowHeight);

        TextMeshProUGUI _text = _obj.GetComponent<TextMeshProUGUI>();
        if (_font != null) _text.font = _font;
        _text.fontSize = _craftFontSize;
        _text.color = Color.white;
        _text.alignment = TextAlignmentOptions.MidlineLeft;
        _text.text = skill.Name;
        _text.raycastTarget = false;

        rows.Add(_obj);
    }

    private void ClearRows(List<GameObject> rows)
    {
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] != null) Destroy(rows[i]);

        rows.Clear();
    }

    // ── 도우미 ──────────────────────────────────────────────

    private static T Find<T>(Transform root, string path) where T : Component
    {
        Transform _t = root.Find(path);
        if (_t == null) return null;
        return _t.GetComponent<T>();
    }

    private static GameObject ChildObject(Transform root, string path)
    {
        Transform _t = root.Find(path);
        return _t != null ? _t.gameObject : null;
    }

    private Text FindText(Transform root, string path)
    {
        Transform _t = root.Find(path);
        if (_t == null)
        {
            Debug.LogWarning($"[MenuStatus] {root.name}/{path} 를 찾지 못했습니다.");
            return null;
        }

        Text _text = _t.GetComponent<Text>();
        if (_text == null)
            Debug.LogWarning($"[MenuStatus] {root.name}/{path} 에 Text 컴포넌트가 없습니다.");

        return _text;
    }

    private static void SetText(Text field, string value)
    {
        if (field != null) field.text = value;
    }
}
