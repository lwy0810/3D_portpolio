using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 행동 순서의 유일한 진실. attackIndex / oriIndex / nextIndex / TrunMove() 를 모두 대체한다.
///
/// 고정 라운드로빈과의 차이는 자료구조 하나다. AT 값이 붙은 정렬 목록이므로
///   · 행동을 바꾸면 순서가 바뀐다
///   · SPD 를 올리면 순서가 바뀐다
///   · 사망은 목록에서 제거하면 끝이다 (인덱스 개념이 없다)
///   · 다음 N턴을 미리 읽을 수 있다 (액션 바 예측 표시)
/// </summary>
public class ATQueue
{
    private readonly List<BattleUnit> _entries = new List<BattleUnit>();

    /// <summary>큐가 바뀔 때마다 발행. ActionBar 가 구독해 렌더한다.</summary>
    public event Action<List<BattleUnit>> OnQueueChanged;

    public int Count => _entries.Count;
    public BattleUnit Current => _entries.Count > 0 ? _entries[0] : null;
    public IReadOnlyList<BattleUnit> Entries => _entries;

    // ── 초기화 ──────────────────────────────────────────────

    /// <summary>
    /// 전투 개시. 초기 AT = floor(100 × openingDelay / SPD).
    /// 선제 시 아군 openingDelay 를 낮추고 기습 시 올리면 그대로 표현된다.
    /// </summary>
    public void Init(IEnumerable<BattleUnit> units, int openingDelay = AT.BaseOpening)
    {
        _entries.Clear();

        foreach (BattleUnit u in units)
        {
            if (u == null || !u.IsAlive) continue;
            u.At = AT.Delay(u, openingDelay);
            _entries.Add(u);
        }

        Sort();
        Raise();
    }

    /// <summary>
    /// 이미 At 가 계산된 유닛들을 그대로 받아들인다.
    /// 진영별로 다른 openingDelay(선제 · 기습)를 쓸 때 사용.
    /// </summary>
    public void Adopt(IEnumerable<BattleUnit> units)
    {
        _entries.Clear();

        foreach (BattleUnit u in units)
        {
            if (u == null || !u.IsAlive) continue;
            _entries.Add(u);
        }

        Sort();
        Raise();
    }

    // ── 삽입 ────────────────────────────────────────────────

    /// <summary>행동을 마친 유닛을 다시 큐에 넣는다.</summary>
    public void Push(BattleUnit unit, int baseDelay)
    {
        if (unit == null) return;

        int d = AT.Delay(unit, baseDelay);
        unit.At += d;

        Sort();
        Raise();
    }

    /// <summary>아츠 : 캐스트 슬롯을 먼저 꽂는다. 이 슬롯이 돌아오면 실제 발동.</summary>
    public void PushCast(BattleUnit unit, SkillData art)
    {
        if (unit == null || art == null) return;

        unit.Casting = art;
        unit.At += AT.Delay(unit, art.CastDelay);

        Sort();
        Raise();
    }

    /// <summary>브레이크 : 섬4 기준 BaseDelay 10 을 추가하고 캐스트를 취소한다.</summary>
    public void ApplyBreak(BattleUnit unit)
    {
        if (unit == null) return;

        unit.At += AT.Delay(unit, AT.BaseBreak);
        unit.Casting = null;

        Sort();
        Raise();
    }

    /// <summary>AtDelay 효과 : 대상에게 추가 딜레이를 준다.</summary>
    public void AddDelay(BattleUnit unit, int baseDelay)
    {
        if (unit == null || baseDelay <= 0) return;

        unit.At += AT.Delay(unit, baseDelay);

        Sort();
        Raise();
    }

    /// <summary>
    /// S브레이크 : 차례를 기다리지 않고 큐 맨 앞으로 끼어든다.
    /// S크래프트가 특별한 이유는 딜레이가 짧아서가 아니라 이것 때문이다.
    /// </summary>
    public void Interrupt(BattleUnit unit)
    {
        if (unit == null) return;

        int idx = _entries.IndexOf(unit);
        if (idx < 0) return;

        int front = _entries[0].At;
        _entries.RemoveAt(idx);
        unit.At = front;
        _entries.Insert(0, unit);

        Raise();          // 의도적으로 Sort 하지 않는다 (맨 앞 고정)
    }

    // ── 제거 ────────────────────────────────────────────────

    /// <summary>사망 처리. Destroy 보다 먼저 호출해야 큐에 시체가 남지 않는다.</summary>
    public bool Remove(BattleUnit unit)
    {
        if (unit == null) return false;

        bool removed = _entries.Remove(unit);
        if (removed) Raise();
        return removed;
    }

    /// <summary>Hp 0 이하인 유닛을 한 번에 정리하고 제거된 목록을 돌려준다.</summary>
    public List<BattleUnit> RemoveDead()
    {
        List<BattleUnit> dead = new List<BattleUnit>();

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (!_entries[i].IsAlive)
            {
                dead.Add(_entries[i]);
                _entries.RemoveAt(i);
            }
        }

        if (dead.Count > 0) Raise();
        return dead;
    }

    // ── 조회 ────────────────────────────────────────────────

    /// <summary>다음 n 슬롯. 액션 바의 예측 표시가 여기서 나온다.</summary>
    public List<BattleUnit> Peek(int n)
    {
        List<BattleUnit> list = new List<BattleUnit>(Mathf.Min(n, _entries.Count));
        for (int i = 0; i < _entries.Count && i < n; i++) list.Add(_entries[i]);
        return list;
    }

    public BattleUnit Find(Unit unit)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].Unit == unit) return _entries[i];
        return null;
    }

    public List<BattleUnit> Side(bool ally)
    {
        List<BattleUnit> list = new List<BattleUnit>();
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].IsAlly == ally && _entries[i].IsAlive) list.Add(_entries[i]);
        return list;
    }

    public bool AllDead(bool ally)
    {
        return Side(ally).Count == 0;
    }

    /// <summary>
    /// AT 오름차순. 동률이면 유효 SPD 높은 쪽이 먼저 — 실제 게임과 같은 타이브레이크.
    /// </summary>
    private void Sort()
    {
        _entries.Sort((a, b) =>
        {
            if (a.At != b.At) return a.At.CompareTo(b.At);
            return b.EffectiveSpeed.CompareTo(a.EffectiveSpeed);
        });
    }

    private void Raise()
    {
        OnQueueChanged?.Invoke(_entries);
    }
}
