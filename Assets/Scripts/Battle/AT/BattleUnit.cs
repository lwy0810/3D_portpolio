using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 1회 동안만 유효한 유닛 상태.
///
/// Stat 은 씬을 넘어 살아남는 영속 데이터이므로, 브레이크 게이지나
/// 버프 잔여 턴처럼 전투가 끝나면 사라져야 하는 값은 여기에 둔다.
/// Stat 은 참조만 하므로 Hp 변경은 그대로 영속 데이터에 반영된다.
/// </summary>
public class BattleUnit
{
    public Unit Unit { get; private set; }
    public Stat Stat => Unit.Stat;
    public bool IsAlly { get; private set; }

    /// <summary>AT 큐 정렬 키. 작을수록 먼저 행동한다.</summary>
    public int At;

    public int Break;
    public bool IsBroken;

    /// <summary>캐스트 중인 아츠. null 이면 캐스트 중이 아니다.</summary>
    public SkillData Casting;

    public readonly List<ActiveEffect> Effects = new List<ActiveEffect>();

    public BattleUnit(Unit unit, bool isAlly)
    {
        Unit = unit;
        IsAlly = isAlly;
        Break = unit.Stat.MaxBreak;
        IsBroken = false;
    }

    public bool IsAlive => Unit != null && Stat != null && Stat.Hp > 0;
    public Transform Transform => Unit != null ? Unit.transform : null;

    // ── 유효 스탯 ───────────────────────────────────────────

    /// <summary>
    /// 버프 · 디버프를 반영한 스탯. percent 는 합산 후 한 번에 곱하고 flat 은 더한다.
    /// </summary>
    public float Effective(string statName)
    {
        float bas = Stat.GetByName(statName);
        float percent = 0f;
        float flat = 0f;

        for (int i = 0; i < Effects.Count; i++)
        {
            SkillEffect e = Effects[i].Data;
            if (e.StatName != statName) continue;
            if (e.Type != EffectType.Buff && e.Type != EffectType.Debuff) continue;

            if (e.ValueType == ValueType.Percent) percent += e.Value;
            else flat += e.Value;
        }

        return bas * (1f + percent / 100f) + flat;
    }

    /// <summary>AT 딜레이의 분모. 버프가 SPD 에 곱해지고 ATB 는 그 결과로 재계산된다.</summary>
    public float EffectiveSpeed => Mathf.Max(1f, Effective("Speed"));

    public int Atb => AT.Atb(EffectiveSpeed);

    // ── 상태이상 ────────────────────────────────────────────

    public bool HasAilment(string name)
    {
        for (int i = 0; i < Effects.Count; i++)
        {
            ActiveEffect a = Effects[i];
            if (a.Data.Type == EffectType.Ailment && a.Data.StatName == name) return true;
        }
        return false;
    }

    /// <summary>기절 · 동결이면 행동할 수 없다.</summary>
    public bool CannotAct => HasAilment(Ailments.Stun) || HasAilment(Ailments.Freeze);

    /// <summary>봉인 상태면 아츠를 쓸 수 없다.</summary>
    public bool ArtsSealed => HasAilment(Ailments.Seal);

    public void AddEffect(SkillEffect effect)
    {
        // 같은 효과가 이미 붙어 있으면 잔여 턴만 갱신한다 (중첩 방지)
        for (int i = 0; i < Effects.Count; i++)
        {
            if (Effects[i].Data.Index == effect.Index)
            {
                Effects[i].TurnsLeft = Mathf.Max(Effects[i].TurnsLeft, effect.Duration);
                return;
            }
        }
        Effects.Add(new ActiveEffect(effect));
    }

    /// <summary>이 유닛의 차례가 시작될 때 1회 호출. 지속 피해와 잔여 턴을 처리한다.</summary>
    public void TickTurnStart()
    {
        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            ActiveEffect a = Effects[i];

            // 독 : 매 턴 최대 체력 비율 피해
            if (a.Data.Type == EffectType.Ailment && a.Data.StatName == Ailments.Poison)
            {
                int dmg = Mathf.Max(1, Mathf.FloorToInt(Stat.MaxHp * Mathf.Abs(a.Data.Value) / 100f));
                Stat.AddHp(-dmg);
            }

            a.TurnsLeft--;
            if (a.TurnsLeft <= 0) Effects.RemoveAt(i);
        }
    }

    // ── 브레이크 ────────────────────────────────────────────

    public bool HasBreakGauge => Stat.MaxBreak > 0;

    /// <summary>브레이크 데미지를 누적한다. 게이지가 0 이 되면 true.</summary>
    public bool AddBreakDamage(int amount)
    {
        if (!HasBreakGauge || IsBroken || amount <= 0) return false;

        Break = Mathf.Max(0, Break - amount);
        if (Break > 0) return false;

        IsBroken = true;
        Casting = null;                 // 캐스트 중이던 아츠는 취소된다
        Effects.RemoveAll(a => a.Data.Type == EffectType.Buff);   // 이로운 효과 소실
        return true;
    }

    public void RecoverBreak()
    {
        IsBroken = false;
        Break = Stat.MaxBreak;
    }

    public override string ToString()
    {
        return $"{Stat.Name} (AT {At}, SPD {EffectiveSpeed:0.##})";
    }
}
