using UnityEngine;

/// <summary>
/// AT(Action Time) 딜레이 계산. 위키 공식 그대로.
///
///     Final Delay = floor( 100 × BaseDelay / SPD )
///
/// BaseDelay 는 행동이 정하고 SPD 는 유닛이 정한다.
/// 같은 기술이라도 빠른 유닛은 딜레이를 덜 받는다.
///
/// 사용자 실측(ATB.xlsx)의 ATB = 2000 / SPD 는 이 식에서 BaseDelay 20 을
/// 1.0 기준으로 정리한 형태다. BaseDelay = 20 × 배율 이 성립한다.
///     배율 0.5 → 10,  1.0 → 20,  1.25 → 25,  1.5 → 30,  2.0 → 40
/// </summary>
public static class AT
{
    // ── 위키에 명시된 기본값 ────────────────────────────────
    public const int BaseMove = 10;    // 이동 · 행동불가
    public const int BaseItem = 10;    // 아이템 사용
    public const int BaseAttack = 20;    // 통상공격  ← 기준
    public const int BaseGuard = 20;    // 방어
    public const int BaseBreak = 10;    // 브레이크 상태 (섬4)
    public const int BaseEscapeFail = 20;    // 도주 실패

    /// <summary>전투 개시 시 초기 AT 에 쓰는 BaseDelay.</summary>
    public const int BaseOpening = 20;

    /// <summary>
    /// 딜레이 계산. 내림은 마지막에 한 번만 한다.
    /// 중간에 내리면 SPD 53 · BaseDelay 25 에서 47 이 46 이 된다.
    /// </summary>
    public static int Delay(float speed, int baseDelay)
    {
        return Mathf.FloorToInt(100f * baseDelay / Mathf.Max(1f, speed));
    }

    public static int Delay(BattleUnit unit, int baseDelay)
    {
        return Delay(unit.EffectiveSpeed, baseDelay);
    }

    /// <summary>디버그 표시용. 통상공격 1회에 해당하는 AT 값.</summary>
    public static int Atb(float speed)
    {
        return Delay(speed, BaseAttack);
    }
}
