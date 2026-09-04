using System;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 · 몬스터의 영속 스탯. 순수 C# 클래스이므로 씬이 바뀌어도 살아남고,
/// StatusManager 가 이름을 키로 이 인스턴스를 보관한다.
///
/// 전투 중에만 유효한 값(브레이크 게이지, 버프 잔여 턴, 캐스트 중인 아츠)은
/// 여기에 두지 않는다. BattleUnit 이 갖는다.
/// </summary>
[System.Serializable]
public class Stat
{
    public event Action<Stat> OnStatusChanged;

    // ── 식별 ────────────────────────────────────────────────
    private string _leader;
    private string _name;
    private string _category;
    private int _level;

    // ── 자원 ────────────────────────────────────────────────
    private int _hp;
    private int _maxHp;
    private int _ep;     // EP  아츠 자원
    private int _maxEp;
    private int _cp;              // CP  크래프트 자원 (0 ~ MaxCp)
    private int _maxCp = 200;
    private int _bp;              // BP  브레이브 오더 자원

    // ── 물리 ────────────────────────────────────────────────
    private int _str;
    private int _def;

    // ── 아츠 ────────────────────────────────────────────────
    private int _ats;             // 아츠 공격력
    private int _adf;             // 아츠 방어력

    // ── 행동 ────────────────────────────────────────────────
    private int _speed;           // AT 딜레이의 분모
    private int _dex;             // 명중 3단 판정용
    private int _agl;             // 회피 3단 판정용

    // ── 확률 · 배율 ─────────────────────────────────────────
    private float _avoid;
    private float _critical;
    private float _criticalDmg;   // 크리티컬 시 SPRM 가산치 (0.5 = +50%)
    private float _hit;

    // ── 기타 ────────────────────────────────────────────────
    private int _maxBreak;        // 브레이크 게이지 최대치. 0 이면 브레이크 없음
    private int _experience;

    public string Leader { get => _leader; set { if (SetField(ref _leader, value)) Notify(); } }
    public string Name { get => _name; set { if (SetField(ref _name, value)) Notify(); } }
    public string Category { get => _category; set { if (SetField(ref _category, value)) Notify(); } }
    public int Level { get => _level; set { if (SetField(ref _level, value)) Notify(); } }

    public int Hp { get => _hp; set { if (SetField(ref _hp, value)) Notify(); } }
    public int MaxHp { get => _maxHp; set { if (SetField(ref _maxHp, value)) Notify(); } }
    public int Ep { get => _ep; set { if (SetField(ref _ep, value)) Notify(); } }
    public int MaxEp { get => _maxEp; set { if (SetField(ref _maxEp, value)) Notify(); } }
    public int Cp { get => _cp; set { if (SetField(ref _cp, value)) Notify(); } }
    public int MaxCp { get => _maxCp; set { if (SetField(ref _maxCp, value)) Notify(); } }
    public int Bp { get => _bp; set { if (SetField(ref _bp, value)) Notify(); } }

    public int Str { get => _str; set { if (SetField(ref _str, value)) Notify(); } }
    public int Def { get => _def; set { if (SetField(ref _def, value)) Notify(); } }
    public int Ats { get => _ats; set { if (SetField(ref _ats, value)) Notify(); } }
    public int Adf { get => _adf; set { if (SetField(ref _adf, value)) Notify(); } }

    public int Speed { get => _speed; set { if (SetField(ref _speed, value)) Notify(); } }
    public int Dex { get => _dex; set { if (SetField(ref _dex, value)) Notify(); } }
    public int Agl { get => _agl; set { if (SetField(ref _agl, value)) Notify(); } }

    public float Avoid { get => _avoid; set { if (SetField(ref _avoid, value)) Notify(); } }
    public float Critical { get => _critical; set { if (SetField(ref _critical, value)) Notify(); } }
    public float CriticalDmg { get => _criticalDmg; set { if (SetField(ref _criticalDmg, value)) Notify(); } }
    public float Hit { get => _hit; set { if (SetField(ref _hit, value)) Notify(); } }

    public int MaxBreak { get => _maxBreak; set { if (SetField(ref _maxBreak, value)) Notify(); } }
    public int Experience { get => _experience; set { if (SetField(ref _experience, value)) Notify(); } }

    /// <summary>SpecialPoint 는 CP 로 대체되었다. 기존 호출부 호환용.</summary>
    public int SpecialPoint { get => Cp; set { Cp = value; } }

    // ── 편의 ────────────────────────────────────────────────

    public bool IsAlive => _hp > 0;

    /// <summary>이름으로 스탯을 읽는다. SkillEffect.statName 적용에 사용.</summary>
    public float GetByName(string statName)
    {
        switch (statName)
        {
            case "Hp": return _hp;
            case "MaxHp": return _maxHp;
            case "Ep": return _ep;
            case "MaxEp": return _maxEp;
            case "Cp": return _cp;
            case "Bp": return _bp;
            case "Str": return _str;
            case "Def": return _def;
            case "Ats": return _ats;
            case "Adf": return _adf;
            case "Speed": return _speed;
            case "Dex": return _dex;
            case "Agl": return _agl;
            case "Avoid": return _avoid;
            case "Critical": return _critical;
            case "CriticalDmg": return _criticalDmg;
            case "Hit": return _hit;
            default: return 0f;
        }
    }

    public void AddHp(int delta)
    {
        Hp = Math.Max(0, Math.Min(_maxHp, _hp + delta));
    }

    public void AddCp(int delta)
    {
        Cp = Math.Max(0, Math.Min(_maxCp, _cp + delta));
    }

    public void AddEp(int delta)
    {
        Ep = Math.Max(0, _ep + delta);
    }

    private bool SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        return true;
    }

    private void Notify()
    {
        OnStatusChanged?.Invoke(this);
    }
}
