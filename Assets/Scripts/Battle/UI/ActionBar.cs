using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 행동 순서 바.
///
/// 두 가지 방식으로 동작한다.
///   · BattleFlow 가 있으면 (AT 큐 모드)
///       유닛 1명에 슬롯 1개를 두고, 큐가 바뀌면 각 슬롯의 목표 줄만 바꾼다.
///       슬롯이 재생성되지 않으므로 초상화가 그 자리에서 미끄러져 이동한다.
///   · 없으면 (기존 턴 모드)
///       CreateCommandActionMemberSystem 의 목록을 그대로 쓴다.
///
/// 커맨드를 고르면 이번 행동 뒤에 들어갈 자리에 슬롯(ProjectedSlot)을 미리 만든다.
/// 왼쪽에서 미끄러져 들어오고, 밀려나는 슬롯들은 서서히 아래로 내려간다.
/// 취소하면 사라지고, 턴이 넘어가면 실제 순서에 반영된다.
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
    [SerializeField] private Color _badgeColor = new Color(1f, 1f, 1f);

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

    // ── AT 큐 모드 상태 ─────────────────────────────────────
    private readonly List<BattleUnit> _queueOrder = new List<BattleUnit>();
    private readonly Dictionary<BattleUnit, GameObject> _slotByUnit
        = new Dictionary<BattleUnit, GameObject>();

    // ── 공용 ────────────────────────────────────────────────
    private GameObject _badgeRoot;
    private TextMeshProUGUI _badgeText;

    private GameObject _ghostSlot;
    private int _projectedRow = -1;

    private bool _subscribed;
    private bool _warnedNoFactory;

    private Transform BarRoot => _barRoot != null ? _barRoot : transform;
    private Transform _barRoot;

    private bool QueueMode => BattleFlow.Instance != null;
    private bool HasGhost => _ghostSlot != null && _ghostSlot.activeSelf && _projectedRow >= 0;

    private float SlotHeight => _factory != null ? _factory.SlotHeight : 70f;

    // ── 연결 ────────────────────────────────────────────────

    void Awake()
    {
        if (_factory == null) _factory = GetComponent<CreateCommandActionMemberSystem>();
        if (_factory == null) _factory = FindFirstObjectByType<CreateCommandActionMemberSystem>();

        if (_factory == null)
            Debug.LogWarning("[ActionBar] CreateCommandActionMemberSystem 을 찾지 못했습니다.");
    }

    public void Bind(Transform barRoot, CreateCommandActionMemberSystem factory)
    {
        if (barRoot != null) _barRoot = barRoot;
        if (factory != null) _factory = factory;

        Debug.Log($"[ActionBar] Bind 완료 — bar={(_barRoot != null ? _barRoot.name : "null")}, " +
                  $"factory={(_factory != null ? "OK" : "null")}, mode={(QueueMode ? "AT 큐" : "기존 턴")}");
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

    // ── AT 큐 모드 렌더 ─────────────────────────────────────

    /// <summary>
    /// 큐가 바뀔 때마다 호출된다. 유닛별 슬롯을 유지하고 목표 줄만 갱신하므로
    /// 초상화가 새로 그려지지 않고 그 자리에서 이동한다.
    /// </summary>
    public void Render(List<BattleUnit> queue)
    {
        if (_factory == null || queue == null) return;

        int want = Mathf.Min(_visibleCount, queue.Count);

        _queueOrder.Clear();
        for (int i = 0; i < want; i++) _queueOrder.Add(queue[i]);

        // 보이지 않게 된 유닛의 슬롯은 정리한다
        List<BattleUnit> gone = null;

        foreach (KeyValuePair<BattleUnit, GameObject> kv in _slotByUnit)
        {
            if (_queueOrder.Contains(kv.Key)) continue;

            if (gone == null) gone = new List<BattleUnit>();
            gone.Add(kv.Key);
        }

        if (gone != null)
        {
            for (int i = 0; i < gone.Count; i++)
            {
                GameObject slot = _slotByUnit[gone[i]];
                _slotByUnit.Remove(gone[i]);
                if (slot != null) StartCoroutine(FadeOutAndDestroy(slot));
            }
        }

        // 없는 슬롯은 만들고, AT 값을 갱신한다
        for (int i = 0; i < _queueOrder.Count; i++)
        {
            BattleUnit u = _queueOrder[i];
            if (u == null || u.Unit == null) continue;

            GameObject slot;

            if (!_slotByUnit.TryGetValue(u, out slot) || slot == null)
            {
                slot = CreateQueueSlot(u, i);
                if (slot == null) continue;

                _slotByUnit[u] = slot;
            }

            SetSlotAt(slot, u);
        }
    }

    private GameObject CreateQueueSlot(BattleUnit unit, int row)
    {
        if (_factory.Prefab == null)
        {
            WarnNoFactory();
            return null;
        }

        GameObject slot = Instantiate(_factory.Prefab, BarRoot);
        slot.name = unit.Stat.Name;

        _factory.ImageReset(unit.Unit, slot);

        RectTransform rt = slot.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = SlotPos(row, rt, 1f) - new Vector2(_slideInFromX, 0f);
        }

        CanvasGroup cg = slot.GetComponent<CanvasGroup>();
        if (cg == null) cg = slot.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        return slot;
    }

    // ── 배치 연출 ───────────────────────────────────────────

    private void AnimateLayout()
    {
        if (_factory == null) return;

        float t = 1f - Mathf.Exp(-_moveSpeed * Time.deltaTime);

        if (QueueMode) AnimateQueueMode(t);
        else AnimateLegacyMode(t);

        if (HasGhost)
        {
            RectTransform grt = _ghostSlot.GetComponent<RectTransform>();
            grt.anchoredPosition = Vector2.Lerp(grt.anchoredPosition,
                                                SlotPos(_projectedRow, grt, grt.localScale.x), t);
            RefreshBadgePosition();
        }
    }

    private void AnimateQueueMode(float t)
    {
        for (int i = 0; i < _queueOrder.Count; i++)
        {
            BattleUnit u = _queueOrder[i];

            GameObject slot;
            if (!_slotByUnit.TryGetValue(u, out slot) || slot == null) continue;

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt == null) continue;

            int row = i + RowShift(i);

            // 배율을 먼저 맞추고, 그 배율에 맞는 왼쪽 맞춤 위치로 옮긴다.
            // 순서를 바꾸면 커지는 동안 왼쪽 변이 흔들린다
            rt.localScale = Vector3.Lerp(rt.localScale,
                Vector3.one * (row == 0 ? _factory.CurrentScale : 1f), t);

            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition,
                                               SlotPos(row, rt, rt.localScale.x), t);
        }
    }

    private void AnimateLegacyMode(float t)
    {
        List<GameObject> list = _factory.ActionMemberlist;

        for (int i = 0; i < list.Count; i++)
        {
            GameObject slot = list[i];
            if (slot == null) continue;

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt == null) continue;

            int row = i + RowShift(i);
            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition,
                                               SlotPos(row, rt, rt.localScale.x), t);
        }
    }

    /// <summary>예상 슬롯이 이 줄보다 위에 끼어들면 한 칸 밀린다.</summary>
    private int RowShift(int index)
    {
        if (!HasGhost) return 0;
        return index >= _projectedRow ? 1 : 0;
    }

    private Vector2 SlotPos(int row)
    {
        return _factory != null ? _factory.SlotAnchoredPosition(row)
                                : new Vector2(0f, -row * SlotHeight);
    }

    /// <summary>배율이 반영된 슬롯 위치. 커진 슬롯도 왼쪽 변이 맞도록 X 를 보정한다.</summary>
    private Vector2 SlotPos(int row, RectTransform rt, float scale)
    {
        return _factory != null ? _factory.SlotAnchoredPosition(row, rt, scale)
                                : new Vector2(0f, -row * SlotHeight);
    }

    /// <summary>프리팹에 "AtText" 가 있으면 AT 값을 채운다.</summary>
    private void SetSlotAt(GameObject slot, BattleUnit unit)
    {
        TextMeshProUGUI[] texts = slot.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
            if (texts[i].name == "AtText") { texts[i].text = unit.At.ToString(); return; }
    }

    // ── 딜레이 프리뷰 ───────────────────────────────────────

    /// <summary>AT 큐 모드. Final Delay = floor(100 * BaseDelay / SPD)</summary>
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

    /// <summary>기존 턴 모드. 행동한 유닛은 목록 맨 뒤로 가므로 마지막 줄 다음이다.</summary>
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
            ? $"+{delay} Delay"
            : $"+{delay} AT";

        _badgeRoot.SetActive(true);
        RefreshBadgePosition();
    }

    /// <summary>연출 중에는 배지만 감춘다. 예상 슬롯은 턴 확정 때 처리된다.</summary>
    public void LockPreview()
    {
        if (_badgeRoot != null) _badgeRoot.SetActive(false);
    }

    /// <summary>취소. 예상 슬롯과 배지를 모두 없앤다.</summary>
    public void ClearPreview()
    {
        if (_badgeRoot != null) _badgeRoot.SetActive(false);
        DestroyGhost();
    }

    public void HideDelayPreview() { ClearPreview(); }

    // ── 턴 확정 ─────────────────────────────────────────────

    /// <summary>
    /// 다음 캐릭터 턴이 시작될 때 호출한다.
    ///
    /// AT 큐 모드에서는 큐가 이미 새 순서를 알고 있으므로 예상 슬롯만 없앤다.
    /// 각 유닛의 슬롯이 목표 줄로 미끄러져 이동한다.
    ///
    /// 기존 턴 모드에서는 선두 슬롯을 페이드아웃시키고, 미리 만들어 둔
    /// 예상 슬롯을 그 자리의 실제 슬롯으로 승격시킨다.
    /// </summary>
    public void CommitTurn(Unit actedUnit)
    {
        if (_factory == null) return;

        if (QueueMode)
        {
            DestroyGhost();
            LockPreview();
            return;
        }

        int ghostRow = _projectedRow;

        GameObject leaving = _factory.DetachFirst();
        if (leaving != null) StartCoroutine(FadeOutAndDestroy(leaving));

        bool alive = actedUnit != null && actedUnit.Stat != null && actedUnit.Stat.Hp > 0;

        if (!alive)
        {
            DestroyGhost();
            LockPreview();
            _factory.Reposition(false);
            return;
        }

        if (_ghostSlot != null && ghostRow >= 0)
        {
            GameObject promoted = _ghostSlot;
            _ghostSlot = null;
            _projectedRow = -1;

            CanvasGroup cg = promoted.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }

            string unitName = actedUnit.Stat.Name;
            promoted.name = unitName;

            _factory.InsertSlot(ghostRow - 1, promoted, unitName);
        }
        else
        {
            _factory.CreateSlot(actedUnit, BarRoot);
        }

        _projectedRow = -1;
        LockPreview();

        _factory.Reposition(false);
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
    /// AT 큐 기준. 현재 행동 유닛 슬롯이 아직 0줄에 있으므로 +1 한다.
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

    /// <summary>기존 턴 모드 기준. 살아있는 유닛 수가 곧 마지막 줄 다음 자리다.</summary>
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
        _badgeText.fontStyle = FontStyles.Bold;
        _badgeText.color = _badgeColor;
        _badgeText.alignment = TextAlignmentOptions.Left;
        _badgeText.enableWordWrapping = false;
        _badgeText.raycastTarget = false;

        if (_badgeText.font == null)
            Debug.LogWarning("[ActionBar] TMP 기본 폰트가 없습니다. " +
                             "Window > TextMeshPro > Import TMP Essential Resources 를 실행하세요.");
    }

    private void RefreshBadgePosition()
    {
        if (_badgeRoot == null || !_badgeRoot.activeSelf) return;

        int row = _projectedRow >= 0 ? _projectedRow : 0;

        RectTransform rt = _badgeRoot.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(_badgePosX, SlotPos(row).y);
        rt.SetAsLastSibling();
    }

    // ── 예상 슬롯 ───────────────────────────────────────────

    private void ShowGhost(Unit actorUnit, int row)
    {
        if (_factory == null || _factory.Prefab == null) { WarnNoFactory(); return; }

        bool created = false;

        if (_ghostSlot == null)
        {
            _ghostSlot = Instantiate(_factory.Prefab, BarRoot);
            _ghostSlot.name = "ProjectedSlot";
            created = true;
        }

        _factory.ImageReset(actorUnit, _ghostSlot);

        CanvasGroup cg = _ghostSlot.GetComponent<CanvasGroup>();
        if (cg == null) cg = _ghostSlot.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;

        RectTransform rt = _ghostSlot.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;

        if (created)
        {
            // 왼쪽에서 미끄러져 들어온다. 실제 이동은 AnimateLayout 이 담당
            rt.anchoredPosition = SlotPos(row, rt, 1f) - new Vector2(_slideInFromX, 0f);
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

    private void WarnNoFactory()
    {
        if (_warnedNoFactory) return;

        _warnedNoFactory = true;
        Debug.LogWarning("[ActionBar] 슬롯을 만들 수 없습니다. " +
                         "CreateCommandActionMemberSystem 의 프리팹이 비어 있습니다.");
    }
}
