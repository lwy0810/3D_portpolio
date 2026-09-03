using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 판정. 부수효과는 Hp / Cp / Ep / 효과 부여까지만 하고 연출은 건드리지 않는다.
///
/// 데미지 공식 (Kiseki Wiki, 섬의궤적3~리버리 기준)
///     AttackPower  = (Atk 또는 Ats) × power / 100
///     DefensePower = (Def 또는 Adf)
///     Base     = 2.5 × AttackPower − 1.25 × DefensePower
///     Modified = Base × ((1 + QPRM) × MULT + SPRM) × 속성배율
///     Final    = Modified ± Modified / 15
///     Heal     = power × (1 + Ats / 1000)
///     Break    = Final × breakMult
/// SPRM : 크리티컬 시 +CriticalDmg(기본 0.5), S크래프트 +0.5
/// </summary>
public static class SkillResolver
{
    // CP 획득 계수. 위키에 정확한 값이 없어 200 CP 를 7~10 행동에 채우도록 역산한 제안값.
    private const int CpFlatOnAction = 6;
    private const float CpPerDamageDealt = 40f;
    private const float CpPerDamageTaken = 60f;

    private const float BrokenTakenBonus = 1.1f;    // 브레이크 상태는 받는 데미지 +10%

    // ── 자원 ────────────────────────────────────────────────

    public static bool CanPay(BattleUnit actor, SkillData skill)
    {
        if (actor == null || skill == null) return false;
        if (skill.Type == SkillType.Art && actor.ArtsSealed) return false;

        switch (skill.CostType)
        {
            case CostType.Cp: return actor.Stat.Cp >= skill.Cost;
            case CostType.Ep: return actor.Stat.EnergyPoint >= skill.Cost;
            default: return true;
        }
    }

    private static void Pay(BattleUnit actor, SkillData skill, SkillResult res)
    {
        switch (skill.CostType)
        {
            case CostType.Cp: actor.Stat.AddCp(-skill.Cost); break;
            case CostType.Ep: actor.Stat.AddEp(-skill.Cost); break;
        }
        res.CostPaid = skill.Cost;
    }

    // ── 메인 ────────────────────────────────────────────────

    /// <summary>
    /// 판정을 수행하고 결과를 돌려준다. 큐 삽입은 호출자(BattleFlow)가 한다.
    /// </summary>
    public static SkillResult Resolve(BattleUnit actor, SkillData skill, List<BattleUnit> targets)
    {
        if (actor == null || skill == null)
            return SkillResult.Cancel(actor, skill, "actor 또는 skill 이 null");

        if (!CanPay(actor, skill))
            return SkillResult.Cancel(actor, skill,
                skill.CostType == CostType.Cp ? "CP 부족" : "EP 부족");

        SkillResult res = new SkillResult { Actor = actor, Skill = skill };
        Pay(actor, skill, res);

        // 자신 대상 (버프 · 방어) — 명중 판정 없음
        if (skill.TargetSide == TargetSide.Self)
        {
            TargetResult self = new TargetResult { Target = actor, Hit = HitResult.Hit };
            ApplyEffects(actor, actor, skill, self);
            res.Targets.Add(self);
            GainCpForAction(actor, res, 0, 1);
            return res;
        }

        if (targets == null || targets.Count == 0)
            return SkillResult.Cancel(actor, skill, "대상 없음");

        int dealtTotal = 0;
        int maxHpOfTargets = 1;

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit t = targets[i];
            if (t == null || !t.IsAlive) continue;

            TargetResult tr = new TargetResult { Target = t };

            // ── 회복 ────────────────────────────────────────
            if (skill.IsHeal)
            {
                tr.Hit = HitResult.Hit;
                tr.Heal = CalcHeal(actor, skill);
                t.Stat.AddHp(tr.Heal);
                ApplyEffects(actor, t, skill, tr);
                res.Targets.Add(tr);
                continue;
            }

            // ── 명중 3단 판정 ───────────────────────────────
            tr.Hit = RollHit(actor, t, skill);
            if (!tr.Landed)
            {
                res.Targets.Add(tr);
                continue;
            }

            // ── 데미지 ──────────────────────────────────────
            tr.ElementMultiplier = ElementChart.Multiplier(skill.Element, t.Stat.Element);
            tr.Damage = CalcDamage(actor, t, skill, tr.Hit == HitResult.Critical, tr.ElementMultiplier);

            t.Stat.AddHp(-tr.Damage);
            dealtTotal += tr.Damage;
            maxHpOfTargets = Mathf.Max(maxHpOfTargets, t.Stat.MaxHp);

            // ── 브레이크 ────────────────────────────────────
            if (t.HasBreakGauge && skill.BreakMult > 0f)
            {
                tr.BreakDamage = Mathf.FloorToInt(tr.Damage * skill.BreakMult);
                tr.Broken = t.AddBreakDamage(tr.BreakDamage);
            }

            // ── 언밸런스 ────────────────────────────────────
            tr.Unbalanced = tr.Hit == HitResult.Critical ||
                            Random.value < skill.Unbalance / 100f;

            // ── 부가 효과 ───────────────────────────────────
            ApplyEffects(actor, t, skill, tr);

            // ── 피격 CP ─────────────────────────────────────
            if (t.IsAlly)
                t.Stat.AddCp(Mathf.FloorToInt((float)tr.Damage / Mathf.Max(1, t.Stat.MaxHp) * CpPerDamageTaken));

            tr.Died = !t.IsAlive;
            res.Targets.Add(tr);
        }

        GainCpForAction(actor, res, dealtTotal, maxHpOfTargets);
        return res;
    }

    // ── 명중 3단 판정 ───────────────────────────────────────

    /// <summary>
    /// 1단 공격자 명중률 → 2단 대상 회피율 → 3단 Dex 대 Agl.
    /// 지금까지 Hit / Avoid 는 CSV 에서 읽히기만 하고 참조하는 코드가 없었다.
    /// </summary>
    public static HitResult RollHit(BattleUnit actor, BattleUnit target, SkillData skill)
    {
        // 아츠는 회피되지 않는다 (시리즈 규칙)
        if (skill.Type == SkillType.Art)
            return Random.value < Crit(actor) ? HitResult.Critical : HitResult.Hit;

        // 브레이크 상태 대상은 반드시 맞는다
        if (target.IsBroken)
            return Random.value < Crit(actor) ? HitResult.Critical : HitResult.Hit;

        // 1단 : 명중 보정
        float accuracy = 1f + actor.Effective("Hit");
        if (Random.value > accuracy) return HitResult.Miss;

        // 2단 : 회피율
        if (Random.value < target.Effective("Avoid")) return HitResult.Evade;

        // 3단 : Dex 대 Agl
        float dex = Mathf.Max(1f, actor.Effective("Dex"));
        float agl = Mathf.Max(0f, target.Effective("Agl"));
        float p = 1f - agl / (5f * dex);
        if (Random.value > p) return HitResult.Evade;

        return Random.value < Crit(actor) ? HitResult.Critical : HitResult.Hit;
    }

    private static float Crit(BattleUnit actor)
    {
        return Mathf.Clamp01(actor.Effective("Critical"));
    }

    // ── 데미지 · 회복 ───────────────────────────────────────

    public static int CalcDamage(BattleUnit actor, BattleUnit target, SkillData skill,
                                 bool critical, float elementMult,
                                 float qprm = 0f, float mult = 1f)
    {
        float atkStat = skill.IsMagic ? actor.Effective("Ats") : actor.Effective("Atk");
        float defStat = skill.IsMagic ? target.Effective("Adf") : target.Effective("Def");

        float attackPower = atkStat * skill.Power / 100f;
        float basis = 2.5f * attackPower - 1.25f * defStat;
        if (basis < 1f) basis = 1f;

        float sprm = 0f;
        if (critical) sprm += actor.Stat.CriticalDmg > 0f ? actor.Stat.CriticalDmg : 0.5f;
        if (skill.Type == SkillType.SCraft) sprm += 0.5f;

        float modified = basis * ((1f + qprm) * mult + sprm) * elementMult;
        if (target.IsBroken) modified *= BrokenTakenBonus;

        float variance = modified / 15f;
        float final = modified + Random.Range(-variance, variance);

        return Mathf.Max(1, Mathf.RoundToInt(final));
    }

    /// <summary>섬4 기준 : Heal = power × (1 + Ats / 1000)</summary>
    public static int CalcHeal(BattleUnit actor, SkillData skill)
    {
        float heal = skill.Power * (1f + actor.Effective("Ats") / 1000f);
        return Mathf.Max(1, Mathf.RoundToInt(heal));
    }

    /// <summary>UI 표시용 예상 데미지. 분산과 크리티컬을 제외한 기대값.</summary>
    public static int PreviewDamage(BattleUnit actor, BattleUnit target, SkillData skill)
    {
        float atkStat = skill.IsMagic ? actor.Effective("Ats") : actor.Effective("Atk");
        float defStat = skill.IsMagic ? target.Effective("Adf") : target.Effective("Def");

        float basis = 2.5f * (atkStat * skill.Power / 100f) - 1.25f * defStat;
        if (basis < 1f) basis = 1f;

        float sprm = skill.Type == SkillType.SCraft ? 0.5f : 0f;
        float m = ElementChart.Multiplier(skill.Element, target.Stat.Element);

        return Mathf.Max(1, Mathf.RoundToInt(basis * (1f + sprm) * m));
    }

    // ── 부가 효과 ───────────────────────────────────────────

    private static void ApplyEffects(BattleUnit actor, BattleUnit target,
                                     SkillData skill, TargetResult tr)
    {
        if (skill.EffectIds == null || skill.EffectIds.Length == 0) return;
        if (SkillDataBase.Instance == null) return;

        for (int i = 0; i < skill.EffectIds.Length; i++)
        {
            SkillEffect e = SkillDataBase.Instance.GetEffect(skill.EffectIds[i]);
            if (e == null) continue;

            if (Random.value > e.Chance) continue;

            // 버프는 시전자 쪽, 그 외는 대상에게
            BattleUnit to = (e.Type == EffectType.Buff && skill.TargetSide == TargetSide.Self)
                            ? actor : target;

            if (e.Type == EffectType.AtDelay)
            {
                // 큐 조작이 필요하므로 결과에만 담고 BattleFlow 가 적용한다
                tr.AppliedEffects.Add(e);
                continue;
            }

            to.AddEffect(e);
            tr.AppliedEffects.Add(e);
        }
    }

    // ── CP ──────────────────────────────────────────────────

    private static void GainCpForAction(BattleUnit actor, SkillResult res,
                                        int damageDealt, int targetMaxHp)
    {
        // S크래프트는 CP 를 쓰는 행동이므로 획득하지 않는다
        if (res.Skill.Type == SkillType.SCraft) return;
        if (!actor.IsAlly) return;

        int gain = CpFlatOnAction +
                   Mathf.FloorToInt((float)damageDealt / Mathf.Max(1, targetMaxHp) * CpPerDamageDealt);

        actor.Stat.AddCp(gain);
        res.CpGained = gain;
    }
}
