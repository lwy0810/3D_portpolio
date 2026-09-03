using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 행동 순서 바.
///
/// 두 가지 방식으로 동작한다.
///   · BattleFlow 가 있으면 : AT 큐를 구독해 정렬된 순서를 그대로 렌더한다.
///   · 없으면              : 기존 턴 방식(TrunMove)을 따른다.
///
/// 커맨드를 고르면 이번 행동 뒤에 들어갈 자리에 슬롯(ProjectedSlot)을 미리 만든다.
/// 슬롯은 왼쪽에서 미끄러져 들어오고, 밀려나는 슬롯들은 서서히 아래로 내려간다.
/// 취소하면 사라지고, 실제로 턴이 넘어가면 그 슬롯이 그대로 실제 슬롯이 된다.
/// </summary>
public class ActionBar : MonoBehaviour
{
    [SerializeField] private CreateCommandActionMemberSystem _factory;

    [Header("표시")]
    [Tooltip("바에 보여줄 슬롯 수")]
    [SerializeField] private int _visibleCount = 12;

    [Header("딜레이 배지")]
    [Tooltip("배지의 PosX. 예상 슬롯 오른쪽에 오도록 잡는다")]
    [SerializeField] private float _badgePosX = 250f;
    [SerializeField] private float _badgeFontSize = 20f;
    [SerializeField] private Color _badgeColor = new Color(1f, 0.78f, 0.28f);

    [Header("예상 슬롯")]
    [Tooltip("행동 후 들어갈 자리에 슬롯을 미리 보여준다")]
    [SerializeField] private bool _showProjectedSlot = true;
    [Tooltip("생성 시 이 거리만큼 왼쪽에서 시작해 제자리로 들어온다")]
    [SerializeField] private float _slideInFromX = 220f;

    [Header("이동 연출")]
    [Tooltip("클수록 빠르게 제자리를 찾아간다")]
    [SerializeField] private float _moveSpeed = 9f;
    [Tooltip("사라지는 슬롯의 페이드아웃 시간")]
    [SerializeField] private float _fadeOutTime = 0.18f;

    private readonly List<BattleUnit> _rendered = new List<BattleUnit>();

    private GameObject _badgeRoot;
    private TextMeshProUGUI _badgeText;

    private GameObject _ghostSlot;
    private int _projectedRow = -1;

    private bool _subscribed;
    private bool _warnedNoFactory;

    /// <summary>배지와 예상 슬롯을 붙일 부모. 기본은 자기 자신.</summary>
    private Transform BarRoot => _barRoot != null ? _barRoot : transform;
    private Transform _barRoot;

    private bool HasGhost => _ghostSlot != null && _ghostSlot.activeSelf && _projectedRow >= 0;

    // ── 연결 ────────────────────────────────────────────────

    void Awake()
    {
        if (_factory == null) _factory = GetComponent<CreateCommandActionMemberSystem>();
        if (_factory == null) _factory = FindFirstObjectByType<CreateCommandActionMemberSystem>();

        if (_factory == null)
            Debug.LogWarning("[ActionBar] CreateCommandActionMemberSystem 을 찾지 못했습니다. " +
                             "배지 위치와 예상 슬롯이 제한됩니다.");
    }

    /// <summary>
    /// BattleManager 가 액션 바 오브젝트와 팩토리를 넘겨준다.
    /// 컴포넌트가 어느 오브젝트에 붙어 있든 배지가 올바른 부모 밑에 생성된다.
    /// </summary>
    public void Bind(Transform barRoot, CreateCommandActionMemberSystem factory)
    {
        if (barRoot != null) _barRoot = barRoot;
        if (factory != null) _factory = factory;

        Debug.Log($"[ActionBar] Bind 완료 — bar={(_barRoot != null ? _barRoot.name : "null")}, " +
                  $"factory={(_factory != null ? "OK" : "null")}");
    }

    void OnEnable() { TrySubscribe(); }
    void OnDisable() { Unsubscribe(); ClearPreview(); }
    void OnDestroy() { Unsubscribe(); }

    void Update()
    {
        if (!_subscribed) TrySubscribe();
        AnimateLayout();
    }

    private void TrySubscribe()
    {
        if (_subscribed || BattleFlow.Instance == null) return;

        BattleFlow.Instance.OnQueueChanged += Render;
        _subscribed = true;

        Render(new List<BattleUnit>(BattleFlow.Instance.Queue.Entries));
    }

    private void Unsubscribe()
    {
        if (!_subscribed || BattleFlow.Instance == null) { _subscribed = false; return; }

        BattleFlow.Instance.OnQueueChanged -= Render;
        _subscribed = false;
    }

    // ── 배치 연출 ───────────────────────────────────────────

    /// <summary>
    /// 슬롯을 목표 좌표로 서서히 옮긴다.
    /// 예상 슬롯이 중간에 끼어들면 그 아래 슬롯들이 한 칸씩 내려간다.
    /// </summary>
    private void AnimateLayout()
    {
        if (_factory == null) return;

        float t = 1f - Mathf.Exp(-_moveSpeed * Time.deltaTime);
        List<GameObject> list = _factory.ActionMemberlist;

        for (int i = 0; i < list.Count; i++)
        {
            GameObject slot = list[i];
            if (slot == null) continue;

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt == null) continue;

            int row = i + RowShift(i);
            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition,
                                               _factory.SlotAnchoredPosition(row), t);
        }

        if (HasGhost)
        {
            RectTransform grt = _ghostSlot.GetComponent<RectTransform>();
            grt.anchoredPosition = Vector2.Lerp(grt.anchoredPosition,
                                                _factory.SlotAnchoredPosition(_projectedRow), t);
            RefreshBadgePosition();
        }
    }

    /// <summary>예상 슬롯이 이 슬롯보다 위에 끼어들면 한 칸 밀린다.</summary>
    private int RowShift(int index)
    {
        if (!HasGhost) return 0;
        return index >= _projectedRow ? 1 : 0;
    }

    // ── AT 큐 렌더 ──────────────────────────────────────────

    public void Render(List<BattleUnit> queue)
    {
        if (_factory == null || queue == null) return;

        int want = Mathf.Min(_visibleCount, queue.Count);

        _rendered.Clear();
        for (int i = 0; i < want; i++) _rendered.Add(queue[i]);

        while (_factory.ActionMemberlist.Count > want) RemoveLast();

        while (_factory.ActionMemberlist.Count < want)
        {
            int idx = _factory.ActionMemberlist.Count;
            if (_factory.CreateSlot(_rendered[idx].Unit, BarRoot) == null) break;
        }

        for (int i = 0; i < _factory.ActionMemberlist.Count && i < _rendered.Count; i++)
        {
            GameObject slot = _factory.ActionMemberlist[i];
            if (slot == null) continue;

            BattleUnit u = _rendered[i];

            if (slot.name != u.Stat.Name)
            {
                slot.name = u.Stat.Name;
                _factory.ImageReset(u.Unit, slot);

                if (i < _factory.CharacterNames.Count) _factory.CharacterNames[i] = u.Stat.Name;
            }

            SetSlotAt(slot, u);
        }

        _factory.Reposition(false);          // 좌표는 AnimateLayout 이 맞춘다
    }

    private void RemoveLast()
    {
        List<GameObject> list = _factory.ActionMemberlist;
        if (list.Count == 0) return;

        int last = list.Count - 1;
        if (list[last] != null) Destroy(list[last]);
        list.RemoveAt(last);

        if (_factory.CharacterNames.Count > last) _factory.CharacterNames.RemoveAt(last);
    }

    /// <summary>프리팹에 "AtText" 가 있으면 AT 값을 채운다.</summary>
    private void SetSlotAt(GameObject slot, BattleUnit unit)
    {
        TextMeshProUGUI[] texts = slot.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
            if (texts[i].name == "AtText") { texts[i].text = unit.At.ToString(); return; }
    }

    // ── 딜레이 프리뷰 ───────────────────────────────────────

    /// <summary>AT 큐를 쓰는 경우. Final Delay = floor(100 * BaseDelay / SPD)</summary>
    public void ShowDelayPreview(BattleUnit actor, int baseDelay)
    {
        if (actor == null || baseDelay <= 0)
        {
            Debug.LogWarning($"[ActionBar] 딜레이 표시 생략 — actor={(actor == null ? "null" : actor.Stat.Name)}, baseDelay={baseDelay}");
            ClearPreview();
            return;
        }

        int delay = AT.Delay(actor, baseDelay);
        ShowPreview(actor.Unit, delay, ProjectedRowByAt(actor, actor.At + delay));
    }

    /// <summary>
    /// BattleFlow 가 없을 때. 지금 턴 방식(TrunMove)은 행동한 유닛을 목록 맨 뒤로
    /// 보내므로 예상 자리는 마지막 줄 다음이다.
    /// </summary>
    public void ShowDelayPreview(Unit actor, int baseDelay)
    {
        if (actor == null || actor.Stat == null || baseDelay <= 0)
        {
            Debug.LogWarning($"[ActionBar] 딜레이 표시 생략 — actor={(actor == null ? "null" : actor.name)}, baseDelay={baseDelay}");
            ClearPreview();
            return;
        }

        if (BattleFlow.Instance != null)
        {
            BattleUnit bu = BattleFlow.Instance.Queue.Find(actor);
            if (bu != null) { ShowDelayPreview(bu, baseDelay); return; }
        }

        ShowPreview(actor, AT.Delay(actor.Stat.Speed, baseDelay), ProjectedRowByTurnList());
    }

    private void ShowPreview(Unit actorUnit, int delay, int row)
    {
        _projectedRow = row;

        if (_showProjectedSlot && row >= 0 && actorUnit != null) ShowGhost(actorUnit, row);
        else DestroyGhost();

        EnsureBadge();

        // row 는 현재 캐릭터 슬롯이 아직 0줄에 있는 상태의 줄 번호다.
        // 턴이 넘어가면 전체가 한 줄 올라가므로 1-based 순서와 값이 같다.
        _badgeText.text = row >= 0
            ? $"+{delay} AT\n<size=70%>{row}번째</size>"
            : $"+{delay} AT";

        _badgeRoot.SetActive(true);
        RefreshBadgePosition();
    }

    /// <summary>
    /// 공격 연출이 시작되면 배지만 감추고 예상 슬롯은 남긴다.
    /// 그 슬롯이 다음 턴에 그대로 실제 슬롯이 되어야 하기 때문이다.
    /// </summary>
    public void LockPreview()
    {
        if (_badgeRoot != null) _badgeRoot.SetActive(false);
    }

    /// <summary>커맨드를 취소했을 때. 예상 슬롯과 배지를 모두 없앤다.</summary>
    public void ClearPreview()
    {
        if (_badgeRoot != null) _badgeRoot.SetActive(false);
        DestroyGhost();
    }

    /// <summary>기존 이름 유지. 취소와 같은 동작이다.</summary>
    public void HideDelayPreview() { ClearPreview(); }

    // ── 턴 확정 ─────────────────────────────────────────────

    /// <summary>
    /// 다음 캐릭터 턴이 시작될 때 호출한다.
    ///   1) 방금 행동한 캐릭터의 선두 슬롯을 페이드아웃시켜 없앤다
    ///   2) 미리 만들어 둔 예상 슬롯을 그 자리(계산된 줄)의 실제 슬롯으로 승격시킨다
    ///      예상 슬롯이 없으면(적 턴 등) 새로 만들어 맨 뒤에 붙인다
    /// </summary>
    public void CommitTurn(Unit actedUnit)
    {
        if (_factory == null) return;

        int ghostRow = _projectedRow;

        // 1) 선두 슬롯 분리 후 페이드아웃
        GameObject leaving = _factory.DetachFirst();
        if (leaving != null) StartCoroutine(FadeOutAndDestroy(leaving));

        // 행동한 유닛이 이 턴에 사망했으면 자리를 만들지 않는다
        bool alive = actedUnit != null && actedUnit.Stat != null && actedUnit.Stat.Hp > 0;

        if (!alive)
        {
            DestroyGhost();
            LockPreview();
            _factory.Reposition(false);
            return;
        }

        // 2) 예상 슬롯 승격
        if (_ghostSlot != null && ghostRow >= 0)
        {
            GameObject promoted = _ghostSlot;
            _ghostSlot = null;

            CanvasGroup cg = promoted.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }

            string unitName = actedUnit != null && actedUnit.Stat != null
                              ? actedUnit.Stat.Name : promoted.name;
            promoted.name = unitName;

            // 선두를 뺀 뒤의 인덱스로 환산한다
            _factory.InsertSlot(ghostRow - 1, promoted, unitName);
        }
        else if (actedUnit != null)
        {
            _factory.CreateSlot(actedUnit, BarRoot);
        }

        _projectedRow = -1;
        LockPreview();

        _factory.Reposition(false);          // 좌표는 AnimateLayout 이 맞춘다
    }

    private IEnumerator FadeOutAndDestroy(GameObject slot)
    {
        CanvasGroup cg = slot.GetComponent<CanvasGroup>();
        if (cg == null) cg = slot.AddComponent<CanvasGroup>();

        float time = 0f;

        while (time < _fadeOutTime)
        {
            if (slot == null) yield break;

            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, time / _fadeOutTime);
            yield return null;
        }

        if (slot != null) Destroy(slot);
    }

    // ── 예상 자리 계산 ──────────────────────────────────────

    /// <summary>
    /// AT 큐 기준. 현재 행동 캐릭터 슬롯이 아직 0줄에 있으므로 +1 한다.
    /// </summary>
    private int ProjectedRowByAt(BattleUnit actor, int projectedAt)
    {
        if (BattleFlow.Instance == null) return -1;

        int row = 0;

        foreach (BattleUnit u in BattleFlow.Instance.Queue.Entries)
        {
            if (u == actor) continue;

            if (u.At < projectedAt) { row++; continue; }
            if (u.At == projectedAt && u.EffectiveSpeed > actor.EffectiveSpeed) row++;
        }

        return Mathf.Clamp(row + 1, 1, Mathf.Max(1, _visibleCount));
    }

    /// <summary>
    /// 지금 턴 방식 기준. 살아있는 유닛 수가 곧 마지막 줄 다음 자리다.
    /// 이 +1 이 빠져서 마지막 슬롯과 겹쳐 보였다.
    /// </summary>
    private int ProjectedRowByTurnList()
    {
        if (GameManager.GameInstance == null) return -1;

        List<Unit> units = GameManager.GameInstance.Units;
        if (units == null) return -1;

        int living = 0;
        for (int i = 0; i < units.Count; i++)
            if (units[i] != null && units[i].Stat != null && units[i].Stat.Hp > 0) living++;

        if (living <= 1) return -1;

        return Mathf.Min(living, Mathf.Max(1, _visibleCount));
    }

    // ── 배지 ────────────────────────────────────────────────

    private void EnsureBadge()
    {
        if (_badgeRoot != null) return;

        _badgeRoot = new GameObject("DelayBadge", typeof(RectTransform));
        _badgeRoot.transform.SetParent(BarRoot, false);

        RectTransform rt = _badgeRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 56f);

        _badgeText = _badgeRoot.AddComponent<TextMeshProUGUI>();
        _badgeText.fontSize = _badgeFontSize;
        _badgeText.color = _badgeColor;
        _badgeText.alignment = TextAlignmentOptions.Left;
        _badgeText.enableWordWrapping = false;
        _badgeText.raycastTarget = false;

        if (_badgeText.font == null)
            Debug.LogWarning("[ActionBar] TMP 기본 폰트가 없습니다. " +
                             "Window > TextMeshPro > Import TMP Essential Resources 를 실행하세요.");
    }

    /// <summary>PosX 는 고정이고, 세로는 예상 슬롯과 같은 줄에 맞춘다.</summary>
    private void RefreshBadgePosition()
    {
        if (_badgeRoot == null || !_badgeRoot.activeSelf) return;

        int row = _projectedRow >= 0 ? _projectedRow : 0;
        float y = _factory != null ? _factory.SlotAnchoredPosition(row).y : -row * 70f;

        RectTransform rt = _badgeRoot.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(_badgePosX, y);
        rt.SetAsLastSibling();
    }

    // ── 예상 슬롯 ───────────────────────────────────────────

    private void ShowGhost(Unit actorUnit, int row)
    {
        if (_factory == null || _factory.Prefab == null)
        {
            if (!_warnedNoFactory)
            {
                _warnedNoFactory = true;
                Debug.LogWarning("[ActionBar] 예상 위치 슬롯을 만들 수 없습니다. " +
                                 "CreateCommandActionMemberSystem 의 프리팹이 비어 있습니다.");
            }
            return;
        }

        bool created = false;

        if (_ghostSlot == null)
        {
            _ghostSlot = Instantiate(_factory.Prefab, BarRoot);
            _ghostSlot.name = "ProjectedSlot";
            created = true;
        }

        _factory.ImageReset(actorUnit, _ghostSlot);

        // 예상 슬롯도 실제 슬롯이 될 것이므로 투명하지 않게 둔다
        CanvasGroup cg = _ghostSlot.GetComponent<CanvasGroup>();
        if (cg == null) cg = _ghostSlot.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;

        RectTransform rt = _ghostSlot.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;

        if (created)
        {
            // 왼쪽에서 미끄러져 들어온다. 실제 이동은 AnimateLayout 이 담당
            rt.anchoredPosition = _factory.SlotAnchoredPosition(row) - new Vector2(_slideInFromX, 0f);
        }

        rt.SetAsLastSibling();
        _ghostSlot.SetActive(true);
    }

    private void DestroyGhost()
    {
        _projectedRow = -1;
        if (_ghostSlot == null) return;

        Destroy(_ghostSlot);
        _ghostSlot = null;
    }
}
