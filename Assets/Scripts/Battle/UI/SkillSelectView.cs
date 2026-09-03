using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 크래프트 · 아츠 목록. SkillDataBase 에서 현재 차례 캐릭터의 스킬을 가져와 버튼을 만든다.
///
/// 섬의궤적 UI 의 핵심 정보는 세 가지다.
///   1) 자원(CP · EP)이 부족한 항목은 고를 수 없다는 것을 즉시 보여준다
///   2) 이 기술을 쓰면 다음 차례가 얼마나 밀리는지(예상 딜레이)를 미리 보여준다
///   3) 대상에게 들어갈 예상 데미지
///
/// _skillButtonPrefab 에는 Button 하나와, 이름으로 찾을 수 있는 자식 텍스트가 필요하다.
///   "NameText"  "CostText"  "DelayText"  "DamageText"
/// 없는 텍스트는 그냥 건너뛴다.
/// </summary>
public class SkillSelectView : MonoBehaviour
{
    [SerializeField] private GameObject _skillButtonPrefab;
    [SerializeField] private Transform _listRoot;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Header("표시할 종류")]
    [SerializeField] private bool _showCrafts = true;
    [SerializeField] private bool _showArts = false;

    private readonly List<GameObject> _spawned = new List<GameObject>();

    /// <summary>선택된 스킬. BattleManager 가 구독해 타깃 지정 단계로 넘어간다.</summary>
    public event System.Action<SkillData> OnSkillPicked;

    void OnEnable()
    {
        Rebuild();
    }

    void OnDisable()
    {
        Clear();
    }

    public void SetMode(bool crafts, bool arts)
    {
        _showCrafts = crafts;
        _showArts = arts;
        if (gameObject.activeInHierarchy) Rebuild();
    }

    public void Rebuild()
    {
        Clear();

        if (SkillDataBase.Instance == null)
        {
            Debug.LogWarning("[SkillSelectView] SkillDataBase 가 없습니다. 씬에 배치했는지 확인하세요.");
            return;
        }

        BattleUnit actor = BattleFlow.Instance != null ? BattleFlow.Instance.Current : null;
        if (actor == null) return;

        BattleUnit preview = FirstEnemy();

        List<SkillData> list = new List<SkillData>();
        if (_showCrafts) list.AddRange(SkillDataBase.Instance.Crafts(actor.Stat.Name));
        if (_showArts) list.AddRange(SkillDataBase.Instance.Arts(actor.Stat.Name));

        for (int i = 0; i < list.Count; i++) Spawn(actor, list[i], preview);
    }

    private void Spawn(BattleUnit actor, SkillData skill, BattleUnit preview)
    {
        if (_skillButtonPrefab == null || _listRoot == null) return;

        GameObject go = Instantiate(_skillButtonPrefab, _listRoot);
        go.name = skill.Name;
        _spawned.Add(go);

        bool usable = SkillResolver.CanPay(actor, skill);

        // 예상 딜레이 = floor(100 x BaseDelay / SPD). 아츠는 캐스트 딜레이를 먼저 보여준다
        int shownBase = skill.NeedsCast ? skill.CastDelay : skill.BaseDelay;
        int delay = AT.Delay(actor, shownBase);

        SetText(go, "NameText", skill.Name);
        SetText(go, "CostText", skill.CostType == CostType.None
                                ? "-"
                                : $"{(skill.CostType == CostType.Cp ? "CP" : "EP")} {skill.Cost}");
        SetText(go, "DelayText", skill.NeedsCast
                                ? $"캐스트 {delay} AT"
                                : $"딜레이 {delay} AT");

        if (preview != null && skill.IsOffensive)
            SetText(go, "DamageText", SkillResolver.PreviewDamage(actor, preview, skill).ToString());
        else if (skill.IsHeal)
            SetText(go, "DamageText", "+" + SkillResolver.CalcHeal(actor, skill));
        else
            SetText(go, "DamageText", "-");

        Button btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = usable;

            SkillData captured = skill;
            btn.onClick.AddListener(() => OnSkillPicked?.Invoke(captured));

            // Hover 시 설명 표시
            SkillHoverProxy hover = go.GetComponent<SkillHoverProxy>();
            if (hover == null) hover = go.AddComponent<SkillHoverProxy>();
            hover.Bind(this, captured);
        }

        // 자원 부족은 색으로도 알린다
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = usable ? 1f : 0.45f;
    }

    public void ShowDescription(SkillData skill)
    {
        if (_descriptionText == null) return;
        _descriptionText.text = skill == null ? "" : skill.Description;
    }

    private BattleUnit FirstEnemy()
    {
        if (BattleFlow.Instance == null) return null;

        List<BattleUnit> foes = BattleFlow.Instance.Queue.Side(false);
        return foes.Count > 0 ? foes[0] : null;
    }

    private void SetText(GameObject root, string childName, string value)
    {
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == childName) { texts[i].text = value; return; }
        }
    }

    private void Clear()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i] != null) Destroy(_spawned[i]);

        _spawned.Clear();
        if (_descriptionText != null) _descriptionText.text = "";
    }
}
