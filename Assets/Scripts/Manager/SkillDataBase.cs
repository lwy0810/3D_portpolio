using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 정의를 1회만 읽어 보관한다. StatusManager 와 같은 패턴.
///
/// 씬이 바뀔 때마다 CSV 를 다시 읽지 않게 하는 것이 목적이다.
/// Stat 에서 겪은 문제(씬 전환마다 CSV 재로드로 값이 원복)와 같은 성질이다.
/// </summary>
public class SkillDataBase : MonoBehaviour
{
    public static SkillDataBase Instance { get; private set; }

    private const string SkillFile = "SkillData";
    private const string EffectFile = "SkillEffect";

    private readonly Dictionary<int, SkillData> _skills = new Dictionary<int, SkillData>();
    private readonly Dictionary<int, SkillEffect> _effects = new Dictionary<int, SkillEffect>();
    private readonly Dictionary<string, List<SkillData>> _byOwner
        = new Dictionary<string, List<SkillData>>();

    // 자주 쓰는 기본 행동은 캐시해 둔다
    public SkillData BasicAttack { get; private set; }
    public SkillData Move { get; private set; }
    public SkillData Item { get; private set; }
    public SkillData Guard { get; private set; }

    public bool IsLoaded { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ── 로드 ────────────────────────────────────────────────

    public void Load()
    {
        if (IsLoaded) return;

        LoadEffects();
        LoadSkills();

        IsLoaded = true;

        Debug.Log($"[SkillDataBase] 스킬 {_skills.Count}건 / 효과 {_effects.Count}건 로드 완료");
    }

    private void LoadEffects()
    {
        _effects.Clear();

        CsvTable csv = CSVFileLoader.LoadTable(EffectFile);
        foreach (CsvTable.Row r in csv.Rows)
        {
            SkillEffect e = SkillEffect.FromRow(r);
            if (e.Index == 0) continue;

            if (_effects.ContainsKey(e.Index))
            {
                Debug.LogWarning($"[SkillDataBase] 효과 index 중복 : {e.Index} (line {r.LineNumber})");
                continue;
            }
            _effects[e.Index] = e;
        }
    }

    private void LoadSkills()
    {
        _skills.Clear();
        _byOwner.Clear();

        CsvTable csv = CSVFileLoader.LoadTable(SkillFile);
        foreach (CsvTable.Row r in csv.Rows)
        {
            SkillData s = SkillData.FromRow(r);
            if (s.Index == 0) continue;

            if (_skills.ContainsKey(s.Index))
            {
                Debug.LogWarning($"[SkillDataBase] 스킬 index 중복 : {s.Index} (line {r.LineNumber})");
                continue;
            }
            _skills[s.Index] = s;

            string owner = string.IsNullOrEmpty(s.Owner) ? "all" : s.Owner;
            if (!_byOwner.ContainsKey(owner)) _byOwner[owner] = new List<SkillData>();
            _byOwner[owner].Add(s);

            // 참조하는 효과가 실제로 있는지 확인
            if (s.EffectIds != null)
            {
                for (int i = 0; i < s.EffectIds.Length; i++)
                {
                    if (!_effects.ContainsKey(s.EffectIds[i]))
                        Debug.LogWarning($"[SkillDataBase] {s.Name}({s.Index}) 가 없는 효과 " +
                                         $"{s.EffectIds[i]} 를 참조합니다.");
                }
            }

            switch (s.Type)
            {
                case SkillType.Attack: if (BasicAttack == null) BasicAttack = s; break;
                case SkillType.Move: if (Move == null) Move = s; break;
                case SkillType.Item: if (Item == null) Item = s; break;
                case SkillType.Guard: if (Guard == null) Guard = s; break;
            }
        }

        if (BasicAttack == null)
            Debug.LogError("[SkillDataBase] type 이 attack 인 행이 없습니다. 통상공격을 만들 수 없습니다.");
    }

    // ── 조회 ────────────────────────────────────────────────

    public SkillData Get(int index)
    {
        SkillData s;
        return _skills.TryGetValue(index, out s) ? s : null;
    }

    public SkillEffect GetEffect(int index)
    {
        SkillEffect e;
        return _effects.TryGetValue(index, out e) ? e : null;
    }

    /// <summary>
    /// 해당 캐릭터가 쓸 수 있는 스킬 목록. owner 가 이름인 것 + "all" 인 것.
    /// 기본 행동(이동 · 통상공격 · 아이템 · 방어)은 제외하고 크래프트 · 아츠만 돌려준다.
    /// </summary>
    public List<SkillData> ForOwner(string characterName, bool includeBasic = false)
    {
        List<SkillData> list = new List<SkillData>();

        AppendOwner(list, characterName, includeBasic);
        if (characterName != "all") AppendOwner(list, "all", includeBasic);

        return list;
    }

    private void AppendOwner(List<SkillData> list, string owner, bool includeBasic)
    {
        List<SkillData> src;
        if (!_byOwner.TryGetValue(owner, out src)) return;

        for (int i = 0; i < src.Count; i++)
        {
            SkillData s = src[i];
            bool basic = s.Type == SkillType.Move || s.Type == SkillType.Attack ||
                         s.Type == SkillType.Item || s.Type == SkillType.Guard;
            if (basic && !includeBasic) continue;
            list.Add(s);
        }
    }

    /// <summary>크래프트만. S크래프트 포함 여부를 고른다.</summary>
    public List<SkillData> Crafts(string characterName, bool includeSCraft = true)
    {
        List<SkillData> all = ForOwner(characterName);
        List<SkillData> list = new List<SkillData>();

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Type == SkillType.Craft) list.Add(all[i]);
            else if (includeSCraft && all[i].Type == SkillType.SCraft) list.Add(all[i]);
        }
        return list;
    }

    public List<SkillData> Arts(string characterName)
    {
        List<SkillData> all = ForOwner(characterName);
        List<SkillData> list = new List<SkillData>();

        for (int i = 0; i < all.Count; i++)
            if (all[i].Type == SkillType.Art) list.Add(all[i]);

        return list;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
