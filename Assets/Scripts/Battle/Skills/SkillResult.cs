using System.Collections.Generic;

public enum HitResult { Hit, Critical, Miss, Evade }

/// <summary>대상 1명에 대한 판정 결과.</summary>
public class TargetResult
{
    public BattleUnit Target;
    public HitResult Hit;

    public int Damage;              // 최종 데미지 (회복이면 0)
    public int Heal;                // 최종 회복량
    public int BreakDamage;
    public bool Broken;             // 이 타격으로 브레이크 진입
    public bool Unbalanced;
    public bool Died;

    public readonly List<SkillEffect> AppliedEffects = new List<SkillEffect>();

    public bool Landed => Hit == HitResult.Hit || Hit == HitResult.Critical;
}

/// <summary>
/// 한 번의 스킬 사용에 대한 판정 결과 전체.
///
/// 판정을 한 프레임에 모두 끝내고 이 객체만 연출에 넘긴다.
/// 연출이 몇 초든 결과는 바뀌지 않으므로, WaitForSeconds 뒤에 대상이
/// 사라져 있어도 데미지가 어긋나거나 예외가 나지 않는다.
/// </summary>
public class SkillResult
{
    public BattleUnit Actor;
    public SkillData Skill;

    public bool Cancelled;          // 자원 부족 등으로 실행되지 않음
    public string CancelReason;

    public int CostPaid;
    public int CpGained;
    public int AppliedDelay;        // 이 행동으로 부여된 실제 AT 딜레이
    public bool WasCast;            // 캐스트 예약 단계였는가

    public readonly List<TargetResult> Targets = new List<TargetResult>();

    public int TotalDamage
    {
        get
        {
            int sum = 0;
            for (int i = 0; i < Targets.Count; i++) sum += Targets[i].Damage;
            return sum;
        }
    }

    public bool AnyLanded
    {
        get
        {
            for (int i = 0; i < Targets.Count; i++)
                if (Targets[i].Landed) return true;
            return false;
        }
    }

    public static SkillResult Cancel(BattleUnit actor, SkillData skill, string reason)
    {
        return new SkillResult
        {
            Actor = actor,
            Skill = skill,
            Cancelled = true,
            CancelReason = reason
        };
    }
}
