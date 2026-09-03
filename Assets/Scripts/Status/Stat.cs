using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stat
{
    public event Action<Stat> OnStatusChanged;

    private string _leader;
    private string _name;
    private string _category;
    private string _element;
    private int _level;
    private int _hp;
    private int _maxHp;
    private int _energyPoint;
    private int _specialPoint;
    private int _atk;
    private int _def;
    private int _speed;
    private float _avoid;
    private float _critical;
    private float _criticalDmg;
    private float _hit;
    private int _experience;

    public string Leader { get => _leader; set { if (SetField(ref _leader, value)) Notify(); } }
    public string Name { get => _name; set { if (SetField(ref _name, value)) Notify(); } }
    public string Category { get => _category; set { if (SetField(ref _category, value)) Notify(); } }
    public string Element { get => _element; set { if (SetField(ref _element, value)) Notify(); } }
    public int Level { get => _level; set { if (SetField(ref _level, value)) Notify(); } }
    public int Hp { get => _hp; set { if (SetField(ref _hp, value)) Notify(); } }
    public int MaxHp { get => _maxHp; set { if (SetField(ref _maxHp, value)) Notify(); } }
    public int EnergyPoint { get => _energyPoint; set { if (SetField(ref _energyPoint, value)) Notify(); } }
    public int SpecialPoint { get => _specialPoint; set { if (SetField(ref _specialPoint, value)) Notify(); } }
    public int Atk { get => _atk; set { if (SetField(ref _atk, value)) Notify(); } }
    public int Def { get => _def; set { if (SetField(ref _def, value)) Notify(); } }
    public int Speed { get => _speed; set { if (SetField(ref _speed, value)) Notify(); } }
    public float Avoid { get => _avoid; set { if (SetField(ref _avoid, value)) Notify(); } }
    public float Critical { get => _critical; set { if (SetField(ref _critical, value)) Notify(); } }
    public float CriticalDmg { get => _criticalDmg; set { if (SetField(ref _criticalDmg, value)) Notify(); } }
    public float Hit { get => _hit; set { if (SetField(ref _hit, value)) Notify(); } }
    public int Experience { get => _experience; set { if (SetField(ref _experience, value)) Notify(); } }

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
