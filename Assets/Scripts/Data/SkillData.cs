using UnityEngine;

public enum SkillType
{
    Move,       // 이동            BaseDelay 10
    Attack,     // 통상공격        BaseDelay 20
    Item,       // 아이템          BaseDelay 10
    Guard,      // 방어            BaseDelay 20
    Craft,      // 크래프트  CP    BaseDelay 20 / 25 / 30 / 40
    Art,        // 아츠      EP    캐스트 → 발동 2단
    SCraft      // S크래프트 CP    큐 맨 앞 삽입 + BaseDelay 40
}

public enum CostType { None, Cp, Ep }
public enum TargetSide { Enemy, Ally, Self }
public enum TargetShape { Single, Line, Circle, All }

/// <summary>
/// SkillData.csv 1행 = 이 클래스 1개. 종류당 하나만 존재하고 값이 바뀌지 않는다.
/// 유닛은 이 인스턴스를 참조만 하므로 스킬 정의가 복사되지 않는다.
/// </summary>
public class SkillData
{
    public int Index;
    public string Name;
    public string Owner;            // 캐릭터 이름 또는 "all"
    public SkillType Type;

    public int Power;               // 위력 %. 100 = 공격력 그대로
    public CostType CostType;
    public int Cost;

    /// <summary>아츠 캐스트 예약 BaseDelay. 0 이면 캐스트 없이 즉시 발동.</summary>
    public int CastDelay;

    /// <summary>발동 후 BaseDelay. 위키 기준 정수 (이동 10 / 통상공격 20 / 크래프트 20~40).</summary>
    public int BaseDelay;

    public TargetSide TargetSide;
    public TargetShape TargetShape;
    public float Range;             // 사거리 (월드 유닛)
    public float Area;              // 범위 반경 · 폭

    public float BreakMult;         // 브레이크 데미지 배율

    public int[] EffectIds;         // SkillEffect.csv 참조. '|' 구분
    public string AnimTrigger;
    public string Description;

    // ── 파생 ────────────────────────────────────────────────

    public bool IsMagic => Type == SkillType.Art;
    public bool NeedsCast => CastDelay > 0;
    public bool IsHeal => TargetSide == TargetSide.Ally && Power > 0;
    public bool IsOffensive => TargetSide == TargetSide.Enemy && Power > 0;

    /// <summary>S크래프트는 차례를 기다리지 않고 큐 맨 앞으로 끼어든다.</summary>
    public bool CanInterrupt => Type == SkillType.SCraft;

    public static SkillData FromRow(CsvTable.Row r)
    {
        SkillData s = new SkillData();

        s.Index = r.GetInt("index");
        s.Name = r.GetString("name");
        s.Owner = r.GetString("owner", "all");
        s.Type = ParseType(r.GetString("type", "attack"));

        s.Power = r.GetInt("power");
        s.CostType = ParseCost(r.GetString("costType", "none"));
        s.Cost = r.GetInt("cost");

        s.CastDelay = r.GetInt("castDelay");
        s.BaseDelay = r.GetInt("baseDelay", AT.BaseAttack);

        s.TargetSide = ParseSide(r.GetString("targetSide", "enemy"));
        s.TargetShape = ParseShape(r.GetString("targetShape", "single"));
        s.Range = r.GetFloat("range");
        s.Area = r.GetFloat("area");

        s.BreakMult = r.GetFloat("breakMult", 1f);

        s.EffectIds = r.GetIntList("effectIds");
        s.AnimTrigger = r.GetString("animTrigger");
        s.Description = r.GetString("description");

        if (s.BaseDelay <= 0)
        {
            Debug.LogWarning($"[SkillData] {s.Name}({s.Index}) baseDelay 가 0 입니다. 20 으로 보정합니다.");
            s.BaseDelay = AT.BaseAttack;
        }

        return s;
    }

    private static SkillType ParseType(string v)
    {
        switch (v.ToLowerInvariant())
        {
            case "move": return SkillType.Move;
            case "attack": return SkillType.Attack;
            case "item": return SkillType.Item;
            case "guard": return SkillType.Guard;
            case "craft": return SkillType.Craft;
            case "art": return SkillType.Art;
            case "scraft": return SkillType.SCraft;
            default:
                Debug.LogWarning($"[SkillData] 알 수 없는 type '{v}'. attack 으로 처리합니다.");
                return SkillType.Attack;
        }
    }

    private static CostType ParseCost(string v)
    {
        switch (v.ToLowerInvariant())
        {
            case "cp": return CostType.Cp;
            case "ep": return CostType.Ep;
            default: return CostType.None;
        }
    }

    private static TargetSide ParseSide(string v)
    {
        switch (v.ToLowerInvariant())
        {
            case "ally": return TargetSide.Ally;
            case "self": return TargetSide.Self;
            default: return TargetSide.Enemy;
        }
    }

    private static TargetShape ParseShape(string v)
    {
        switch (v.ToLowerInvariant())
        {
            case "line": return TargetShape.Line;
            case "circle": return TargetShape.Circle;
            case "all": return TargetShape.All;
            default: return TargetShape.Single;
        }
    }
}
