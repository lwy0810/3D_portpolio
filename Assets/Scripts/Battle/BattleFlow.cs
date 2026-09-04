using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleOutcome { Ongoing, Win, Lose }

/// <summary>
/// 전투 진행의 조율자. 큐를 소유하고, 판정을 SkillResolver 에 맡기고,
/// 결과를 이벤트로 흘려보낸다. 판정 로직 자체는 갖지 않는다.
///
/// BattleManager 는 입력과 연출만 담당하고 순서 · 판정은 이 클래스를 통한다.
/// attackIndex / oriIndex / nextIndex / TrunMove() 는 더 필요하지 않다.
/// </summary>
public class BattleFlow : MonoBehaviour
{
    public static BattleFlow Instance { get; private set; }

    public ATQueue Queue { get; private set; } = new ATQueue();

    /// <summary>액션 바가 구독해 순서를 렌더한다.</summary>
    public event Action<List<BattleUnit>> OnQueueChanged;

    /// <summary>데미지 숫자 · 이펙트가 구독한다.</summary>
    public event Action<SkillResult> OnSkillResolved;

    /// <summary>사망 연출과 큐 정리가 끝난 뒤 발행.</summary>
    public event Action<BattleUnit> OnUnitDown;

    /// <summary>승패가 확정되면 1회 발행.</summary>
    public event Action<BattleOutcome> OnBattleEnd;

    public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;
    public BattleUnit Current => Queue.Current;
    public bool IsRunning => Outcome == BattleOutcome.Ongoing && Queue.Count > 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        Queue.OnQueueChanged += list => OnQueueChanged?.Invoke(list);
    }

    // ── 개시 ────────────────────────────────────────────────

    /// <summary>
    /// 전투 개시. openingDelay 를 조절하면 선제(낮게) · 기습(높게)을 표현할 수 있다.
    /// </summary>
    public void Begin(IEnumerable<Character> characters, IEnumerable<Monster> monsters,
                      int allyOpening = AT.BaseOpening, int foeOpening = AT.BaseOpening)
    {
        List<BattleUnit> units = new List<BattleUnit>();

        foreach (Character c in characters)
        {
            if (c == null || c.Stat == null) continue;
            BattleUnit u = new BattleUnit(c, true);
            u.At = AT.Delay(u, allyOpening);
            units.Add(u);
        }

        foreach (Monster m in monsters)
        {
            if (m == null || m.Stat == null) continue;
            BattleUnit u = new BattleUnit(m, false);
            u.At = AT.Delay(u, foeOpening);
            units.Add(u);
        }

        Outcome = BattleOutcome.Ongoing;

        // 진영별로 openingDelay 가 다를 수 있으므로 위에서 계산한 At 를 그대로 쓴다
        Queue.Adopt(units);

        Debug.Log($"[BattleFlow] 전투 개시 — {units.Count} 유닛");
        foreach (BattleUnit u in Queue.Entries)
            Debug.Log($"  {u.Stat.Name,-10} SPD {u.EffectiveSpeed,5:0.##}  ATB {u.Atb,3}  → AT {u.At}");
    }

    // ── 행동 ────────────────────────────────────────────────

    /// <summary>
    /// 현재 차례 유닛이 스킬을 사용한다.
    /// primaryTarget 은 플레이어가 고른 대상. 범위 스킬은 여기서 확장된다.
    ///
    /// 반환값의 Cancelled 가 true 면 큐는 전혀 변하지 않았으므로 다시 고를 수 있다.
    /// </summary>
    public SkillResult Execute(SkillData skill, BattleUnit primaryTarget)
    {
        BattleUnit actor = Queue.Current;

        if (actor == null || skill == null)
            return SkillResult.Cancel(actor, skill, "행동 주체 또는 스킬이 없음");

        if (!SkillResolver.CanPay(actor, skill))
            return SkillResult.Cancel(actor, skill,
                skill.CostType == CostType.Cp ? "CP 부족" : "EP 부족");

        // ── 아츠 1단 : 캐스트 예약 ──────────────────────────
        if (skill.NeedsCast && actor.Casting == null)
        {
            if (skill.CostType == CostType.Ep) actor.Stat.AddEp(-skill.Cost);

            SkillResult cast = new SkillResult
            {
                Actor = actor,
                Skill = skill,
                WasCast = true,
                CostPaid = skill.Cost,
                AppliedDelay = AT.Delay(actor, skill.CastDelay)
            };

            Queue.PushCast(actor, skill);
            OnSkillResolved?.Invoke(cast);
            return cast;
        }

        // ── S브레이크 : 차례를 기다리지 않는 경우 ───────────
        // (큐 맨 앞이 아닌 유닛이 쓰는 경우는 InterruptWith 를 쓴다)

        List<BattleUnit> targets = ResolveTargets(actor, skill, primaryTarget);
        SkillResult res = SkillResolver.Resolve(actor, skill, targets);

        if (res.Cancelled) return res;

        FinishAction(actor, skill, res);
        return res;
    }

    /// <summary>캐스트가 끝난 아츠를 발동한다. 큐 맨 앞이 캐스트 중일 때 호출.</summary>
    public SkillResult ResolveCast(BattleUnit primaryTarget)
    {
        BattleUnit actor = Queue.Current;
        if (actor == null || actor.Casting == null)
            return SkillResult.Cancel(actor, null, "캐스트 중이 아님");

        SkillData art = actor.Casting;
        actor.Casting = null;

        List<BattleUnit> targets = ResolveTargets(actor, art, primaryTarget);

        // EP 는 캐스트 시점에 이미 지불했으므로 비용 없는 사본으로 판정한다
        SkillResult res = SkillResolver.Resolve(actor, WithoutCost(art), targets);
        res.Skill = art;

        if (res.Cancelled) return res;

        FinishAction(actor, art, res);
        return res;
    }

    /// <summary>
    /// S브레이크. 차례가 아닌 아군이 S크래프트로 끼어든다.
    /// CP 100 이상이 필요하고, 끼어든 뒤 BaseDelay 40 을 받는다.
    /// </summary>
    public SkillResult InterruptWith(BattleUnit actor, SkillData sCraft, BattleUnit primaryTarget)
    {
        if (actor == null || sCraft == null || !sCraft.CanInterrupt)
            return SkillResult.Cancel(actor, sCraft, "S크래프트가 아님");

        if (!SkillResolver.CanPay(actor, sCraft))
            return SkillResult.Cancel(actor, sCraft, "CP 부족");

        Queue.Interrupt(actor);

        List<BattleUnit> targets = ResolveTargets(actor, sCraft, primaryTarget);
        SkillResult res = SkillResolver.Resolve(actor, sCraft, targets);

        if (res.Cancelled) return res;

        FinishAction(actor, sCraft, res);
        return res;
    }

    /// <summary>차례 시작 시 호출. 지속 피해 · 잔여 턴을 처리하고 행동 가능 여부를 돌려준다.</summary>
    public bool BeginTurn()
    {
        BattleUnit actor = Queue.Current;
        if (actor == null) return false;

        actor.TickTurnStart();

        if (!actor.IsAlive)
        {
            HandleDeaths();
            return false;
        }

        if (actor.IsBroken)
        {
            // 브레이크 상태는 이 차례를 건너뛰고 회복한다
            actor.RecoverBreak();
            Queue.Push(actor, AT.BaseBreak);
            return false;
        }

        if (actor.CannotAct)
        {
            // 행동 불가 : 이동과 같은 BaseDelay 10 을 받고 넘어간다
            Queue.Push(actor, AT.BaseMove);
            return false;
        }

        return true;
    }

    // ── 마무리 ──────────────────────────────────────────────

    private void FinishAction(BattleUnit actor, SkillData skill, SkillResult res)
    {
        // AtDelay 효과를 대상 큐에 적용
        for (int i = 0; i < res.Targets.Count; i++)
        {
            TargetResult tr = res.Targets[i];
            for (int e = 0; e < tr.AppliedEffects.Count; e++)
            {
                SkillEffect ef = tr.AppliedEffects[e];
                if (ef.Type == EffectType.AtDelay)
                    Queue.AddDelay(tr.Target, Mathf.RoundToInt(ef.Value));
            }

            // 브레이크 진입 대상은 추가 딜레이 (섬4 : BaseDelay 10)
            if (tr.Broken) Queue.ApplyBreak(tr.Target);
        }

        res.AppliedDelay = AT.Delay(actor, skill.BaseDelay);
        Queue.Push(actor, skill.BaseDelay);

        OnSkillResolved?.Invoke(res);

        HandleDeaths();
        CheckEnd();
    }

    private void HandleDeaths()
    {
        List<BattleUnit> dead = Queue.RemoveDead();

        for (int i = 0; i < dead.Count; i++)
        {
            BattleUnit u = dead[i];
            OnUnitDown?.Invoke(u);

            Animator anim = u.Unit != null ? u.Unit.GetComponent<Animator>() : null;
            if (anim != null) anim.SetBool("IsDeath", true);
        }
    }

    private void CheckEnd()
    {
        if (Outcome != BattleOutcome.Ongoing) return;

        if (Queue.AllDead(false)) Outcome = BattleOutcome.Win;
        else if (Queue.AllDead(true)) Outcome = BattleOutcome.Lose;
        else return;

        Debug.Log($"[BattleFlow] 전투 종료 — {Outcome}");
        OnBattleEnd?.Invoke(Outcome);
    }

    // ── 타깃 해석 ───────────────────────────────────────────

    /// <summary>
    /// 스킬의 도형과 사거리로 실제 대상 목록을 만든다.
    ///   Single : primary 만
    ///   All    : 해당 진영 전체
    ///   Circle : primary 를 중심으로 area 반경 안
    ///   Line   : actor → primary 방향 직선에서 폭 area 안
    /// </summary>
    public List<BattleUnit> ResolveTargets(BattleUnit actor, SkillData skill, BattleUnit primary)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        if (skill.TargetSide == TargetSide.Self)
        {
            result.Add(actor);
            return result;
        }

        bool wantAlly = skill.TargetSide == TargetSide.Ally;
        List<BattleUnit> pool = Queue.Side(wantAlly == actor.IsAlly);

        if (skill.TargetShape == TargetShape.All) return pool;

        if (primary == null || !primary.IsAlive)
        {
            if (pool.Count > 0) result.Add(pool[0]);
            return result;
        }

        // 사거리 검사
        if (skill.Range > 0f && actor.Transform != null && primary.Transform != null)
        {
            float dist = Vector3.Distance(actor.Transform.position, primary.Transform.position);
            if (dist > skill.Range)
            {
                Debug.Log($"[BattleFlow] 사거리 초과 : {dist:0.0} > {skill.Range}");
                return result;      // 빈 목록 → Resolve 가 "대상 없음" 으로 취소
            }
        }

        switch (skill.TargetShape)
        {
            case TargetShape.Single:
                result.Add(primary);
                break;

            case TargetShape.Circle:
                for (int i = 0; i < pool.Count; i++)
                {
                    if (pool[i].Transform == null || primary.Transform == null) continue;
                    if (Vector3.Distance(pool[i].Transform.position,
                                         primary.Transform.position) <= skill.Area)
                        result.Add(pool[i]);
                }
                if (result.Count == 0) result.Add(primary);
                break;

            case TargetShape.Line:
                if (actor.Transform == null || primary.Transform == null)
                {
                    result.Add(primary);
                    break;
                }
                Vector3 origin = actor.Transform.position;
                Vector3 dir = (primary.Transform.position - origin);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) { result.Add(primary); break; }
                dir.Normalize();

                for (int i = 0; i < pool.Count; i++)
                {
                    if (pool[i].Transform == null) continue;
                    Vector3 to = pool[i].Transform.position - origin;
                    to.y = 0f;

                    float along = Vector3.Dot(to, dir);
                    if (along < 0f) continue;                       // 뒤쪽은 제외
                    float side = (to - dir * along).magnitude;
                    if (side <= skill.Area) result.Add(pool[i]);
                }
                if (result.Count == 0) result.Add(primary);
                break;
        }

        return result;
    }

    // ── 적 AI ───────────────────────────────────────────────

    /// <summary>
    /// 몬스터 차례의 기본 행동 선택. CP 가 되면 크래프트, 아니면 통상공격.
    /// 지금까지 MonsterAttack 은 호출부가 0건이었고 적 차례가 오지 않았다.
    /// </summary>
    public SkillResult MonsterTurn()
    {
        BattleUnit actor = Queue.Current;
        if (actor == null || actor.IsAlly) return SkillResult.Cancel(actor, null, "적 차례가 아님");

        List<BattleUnit> allies = Queue.Side(true);
        if (allies.Count == 0) return SkillResult.Cancel(actor, null, "대상 없음");

        BattleUnit target = allies[UnityEngine.Random.Range(0, allies.Count)];

        SkillData pick = null;
        if (SkillDataBase.Instance != null)
        {
            List<SkillData> crafts = SkillDataBase.Instance.Crafts(actor.Stat.Name, false);
            for (int i = crafts.Count - 1; i >= 0; i--)
            {
                if (SkillResolver.CanPay(actor, crafts[i])) { pick = crafts[i]; break; }
            }
            if (pick == null) pick = SkillDataBase.Instance.BasicAttack;
        }

        if (pick == null) return SkillResult.Cancel(actor, null, "사용할 스킬 없음");

        return Execute(pick, target);
    }

    // ── 내부 ────────────────────────────────────────────────

    private static SkillData WithoutCost(SkillData src)
    {
        SkillData copy = new SkillData
        {
            Index = src.Index, Name = src.Name, Owner = src.Owner, Type = src.Type,
            Power = src.Power,
            CostType = CostType.None, Cost = 0,
            CastDelay = 0, BaseDelay = src.BaseDelay,
            TargetSide = src.TargetSide, TargetShape = src.TargetShape,
            Range = src.Range, Area = src.Area,
            BreakMult = src.BreakMult, Unbalance = src.Unbalance,
            EffectIds = src.EffectIds, AnimTrigger = src.AnimTrigger,
            Description = src.Description
        };
        return copy;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
