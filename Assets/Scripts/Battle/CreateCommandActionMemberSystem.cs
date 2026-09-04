using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 액션 바 슬롯의 생성 · 제거 · 배치만 담당한다. 턴 순서 판단은 하지 않는다.
///
/// 기존 구현은 슬롯을 추가할 때 localPosition 을 CreateCommandActionMember 코루틴
/// 안에서만 계산했다. 그래서 턴이 끝나 CommandActionMemberAdd 로 추가된 슬롯은
/// 위치가 그대로 남아 바에 나타나지 않았다. 이제 목록이 바뀔 때마다
/// Reposition() 이 전체를 다시 배치한다.
/// </summary>
public class CreateCommandActionMemberSystem : MonoBehaviour
{
    [SerializeField] private GameObject _commandActionMemberPrefab;

    [Header("배치")]
    [Tooltip("슬롯 사이 세로 간격")]
    [SerializeField] private float _slotHeight = 70f;
    [Tooltip("바 기준 가로 오프셋")]
    [SerializeField] private float _slotOffsetX = 90f;
    [Tooltip("현재 차례 슬롯 확대 배율")]
    [SerializeField] private float _currentScale = 1f;

    private readonly List<GameObject> _actionMemberlist = new List<GameObject>();
    private readonly List<string> _characterNames = new List<string>();

    private Vector2 _basePos;
    private bool _baseCaptured;

    public List<GameObject> ActionMemberlist => _actionMemberlist;
    public List<string> CharacterNames => _characterNames;

    public float SlotHeight => _slotHeight;
    public float SlotOffsetX => _slotOffsetX;
    public GameObject Prefab => _commandActionMemberPrefab;

    // ── 생성 ────────────────────────────────────────────────

    /// <summary>전투 개시 연출. 순서대로 하나씩 나타난다.</summary>
    public IEnumerator CreateCommandActionMember(List<Unit> _units, Transform _actionMemberBarTransform)
    {
        ClearAll();

        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i] == null || _units[i].Stat == null) continue;

            GameObject slot = CreateSlot(_units[i], _actionMemberBarTransform);
            if (slot == null) continue;

            Reposition();
            StartCoroutine(FadeIn(slot));
            yield return new WaitForSeconds(0.15f);
        }

        Reposition();
    }

    /// <summary>
    /// 턴이 끝났을 때 선두를 빼고 뒤에 다시 넣는다.
    /// removeFirst 를 false 로 주면 제거 없이 추가만 한다.
    /// </summary>
    public GameObject CommandActionMemberAdd(Unit _unit, Transform _actionMemberBarTransform,
                                             bool removeFirst = true)
    {
        if (removeFirst) RemoveFirst();

        GameObject slot = CreateSlot(_unit, _actionMemberBarTransform);

        Reposition();                 // 기존에 빠져 있던 단계
        if (slot != null) StartCoroutine(FadeIn(slot));

        return slot;
    }

    /// <summary>슬롯 1개 생성. 위치는 잡지 않는다 (Reposition 이 담당).</summary>
    public GameObject CreateSlot(Unit _unit, Transform _parent)
    {
        if (_commandActionMemberPrefab == null)
        {
            Debug.LogError("[ActionMember] _commandActionMemberPrefab 이 비어 있습니다.");
            return null;
        }
        if (_unit == null || _unit.Stat == null)
        {
            Debug.LogWarning("[ActionMember] Unit 또는 Stat 이 없어 슬롯을 만들지 않았습니다.");
            return null;
        }
        if (_parent == null)
        {
            Debug.LogError("[ActionMember] 부모 Transform 이 없습니다. ActionBar 를 찾았는지 확인하세요.");
            return null;
        }

        GameObject slot = Instantiate(_commandActionMemberPrefab, _parent);
        slot.name = _unit.Stat.Name;

        CaptureBasePosition(slot);

        _actionMemberlist.Add(slot);
        _characterNames.Add(_unit.Stat.Name);

        ImageReset(_unit, slot);

        return slot;
    }

    public void RemoveFirst()
    {
        GameObject slot = DetachFirst();
        if (slot != null) Destroy(slot);
    }

    /// <summary>
    /// 선두 슬롯을 목록에서만 빼고 오브젝트는 돌려준다.
    /// 호출자가 페이드아웃을 붙인 뒤 직접 파괴할 수 있다.
    /// </summary>
    public GameObject DetachFirst()
    {
        if (_actionMemberlist.Count == 0) return null;

        GameObject slot = _actionMemberlist[0];
        _actionMemberlist.RemoveAt(0);

        if (_characterNames.Count > 0) _characterNames.RemoveAt(0);

        return slot;
    }

    /// <summary>
    /// 이미 만들어진 슬롯을 목록 중간에 끼워 넣는다.
    /// 예상 위치 슬롯(ProjectedSlot)을 실제 슬롯으로 승격시킬 때 쓴다.
    /// </summary>
    public void InsertSlot(int index, GameObject slot, string unitName)
    {
        if (slot == null) return;

        index = Mathf.Clamp(index, 0, _actionMemberlist.Count);

        _actionMemberlist.Insert(index, slot);

        int nameIndex = Mathf.Clamp(index, 0, _characterNames.Count);
        _characterNames.Insert(nameIndex, unitName);

        CaptureBasePosition(slot);
    }

    public void ClearAll()
    {
        for (int i = 0; i < _actionMemberlist.Count; i++)
            if (_actionMemberlist[i] != null) Destroy(_actionMemberlist[i]);

        _actionMemberlist.Clear();
        _characterNames.Clear();
    }

    // ── 배치 ────────────────────────────────────────────────

    /// <summary>
    /// 목록 순서대로 전체를 다시 배치한다. 슬롯이 추가 · 제거될 때마다 호출해야 한다.
    /// </summary>
    public void Reposition()
    {
        Reposition(true);
    }

    /// <summary>
    /// applyPosition 이 false 면 순서와 크기만 맞추고 좌표는 건드리지 않는다.
    /// ActionBar 가 좌표를 부드럽게 옮기는 동안 튀지 않게 하려는 것이다.
    /// </summary>
    public void Reposition(bool applyPosition)
    {
        int row = 0;

        for (int i = 0; i < _actionMemberlist.Count; i++)
        {
            GameObject slot = _actionMemberlist[i];
            if (slot == null) continue;

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt == null) continue;

            if (applyPosition)
                rt.anchoredPosition = _basePos + new Vector2(_slotOffsetX, -row * _slotHeight);

            rt.localScale = Vector3.one * (row == 0 ? _currentScale : 1f);
            rt.SetSiblingIndex(row);

            row++;
        }
    }

    public float CurrentScale => _currentScale;

    /// <summary>프리팹이 들고 있던 원래 위치를 기준점으로 삼는다. 바 위치가 밀리지 않는다.</summary>
    private void CaptureBasePosition(GameObject slot)
    {
        if (_baseCaptured) return;

        RectTransform rt = slot.GetComponent<RectTransform>();
        if (rt == null) return;

        _basePos = rt.anchoredPosition;
        _baseCaptured = true;
    }

    /// <summary>슬롯 1개의 화면상 위치. 딜레이 배지를 붙일 때 쓴다.</summary>
    public Vector2 SlotAnchoredPosition(int row)
    {
        return _basePos + new Vector2(_slotOffsetX, -row * _slotHeight);
    }

    // ── 초상화 · 속성색 ─────────────────────────────────────

    public void ImageReset(Unit _unit, GameObject _actionMember)
    {
        string filePath = $"Image/{_unit.Stat.Name}_head";
        Sprite sprite = Resources.Load<Sprite>(filePath);

        if (sprite == null)
            Debug.LogWarning($"[ActionMember] Resources/{filePath} 을 찾을 수 없습니다.");

        Image[] images = _actionMember.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];

            if (img.name == "MemberImage" && sprite != null)
            {
                img.sprite = sprite;
            }
            else if (img.name == "ElementBackground")
            {
                img.color = SlotBackgroundColor();
            }
        }
    }

    /// <summary>슬롯 배경색. 속성 구분을 없앴으므로 모든 슬롯이 같은 색을 쓴다.</summary>
    public static Color SlotBackgroundColor()
    {
        Color c = Color.gray;
        c.a = 120 / 255f;
        return c;
    }

    // ── 연출 ────────────────────────────────────────────────

    private IEnumerator FadeIn(GameObject _actionMember)
    {
        if (_actionMember == null) yield break;

        CanvasGroup cg = _actionMember.GetComponent<CanvasGroup>();
        if (cg == null) cg = _actionMember.AddComponent<CanvasGroup>();

        float duration = 0.2f;
        float time = 0f;

        while (time < duration)
        {
            if (_actionMember == null) yield break;

            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }

        cg.alpha = 1f;
    }
}
