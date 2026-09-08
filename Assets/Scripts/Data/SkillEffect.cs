using UnityEngine;

public enum EffectType
{
    Buff,       // statName 을 duration 턴 동안 상승
    Debuff,     // statName 을 duration 턴 동안 감소
    Ailment,    // 상태이상. statName 이 이상 이름
    AtDelay     // 대상에게 추가 AT 딜레이 (value = BaseDelay)
}

public enum ValueType { Flat, Percent }

/// <summary>
/// SkillEffect.csv 1행. SkillData.EffectIds 가 id 로 참조한다.
/// 데미지 · 회복 자체는 SkillData 의 Power 와 TargetSide 로 결정되므로
/// 이 표에는 부가 효과만 들어간다.
/// </summary>
public class SkillEffect
{
    public int Index;
    public EffectType Type;

    /// <summary>
    /// Buff / Debuff : Stat 프로퍼티 이름과 정확히 일치 (Atk, Def, Speed, Ats …)
    /// Ailment       : 상태이상 이름 (Stun, Freeze, Poison, Seal)
    /// </summary>
    public string StatName;

    public ValueType ValueType;
    public float Value;
    public int Duration;            // 지속 턴. 0 이면 즉시 1회
    public float Chance;            // 0 ~ 1
    public string Description;

    public bool IsControl =>
        Type == EffectType.Ailment &&
        (StatName == Ailments.Stun || StatName == Ailments.Freeze);

    public static SkillEffect FromRow(CsvTable.Row r)
    {
        SkillEffect e = new SkillEffect();

        e.Index = r.GetInt("index");
        e.Type = ParseType(r.GetString("effectType", "buff"));
        e.StatName = r.GetString("statName");
        e.ValueType = r.GetString("valueType", "flat").ToLowerInvariant() == "percent"
                      ? ValueType.Percent : ValueType.Flat;
        e.Value = r.GetFloat("value");
        e.Duration = r.GetInt("duration");
        e.Chance = r.Has("chance") ? r.GetFloat("chance", 1f) : 1f;
        e.Description = r.GetString("description");

        return e;
    }

    private static EffectType ParseType(string v)
    {
        switch (v.ToLowerInvariant())
        {
            case "buff": return EffectType.Buff;
            case "debuff": return EffectType.Debuff;
            case "ailment": return EffectType.Ailment;
            case "atdelay": return EffectType.AtDelay;
            default:
                Debug.LogWarning($"[SkillEffect] 알 수 없는 effectType '{v}'. buff 로 처리합니다.");
                return EffectType.Buff;
        }
    }
}

public static class Ailments
{
    public const string Stun = "Stun";       // 행동 불가
    public const string Freeze = "Freeze";   // 행동 불가 + 받는 데미지 증가
    public const string Poison = "Poison";   // 매 턴 지속 피해
    public const string Seal = "Seal";       // 아츠 사용 불가
}

/// <summary>유닛에 붙어 있는 효과 1건. 잔여 턴을 들고 있다.</summary>
public class ActiveEffect
{
    public SkillEffect Data;
    public int TurnsLeft;

    public ActiveEffect(SkillEffect data)
    {
        Data = data;
        TurnsLeft = data.Duration;
    }
}
