using UnityEngine;

/// <summary>
/// 아이템 한 개. 아이콘 · 이름 · 수량을 가진 객체다.
///
/// Stat 과 달리 MonoBehaviour 가 아니다. 아이템은 씬에 존재하는 것이 아니라
/// 소지 목록의 항목이므로, 데이터만 들고 있으면 된다.
/// 아이콘은 Sprite 를 직접 들지 않고 인덱스만 갖는다 —
/// 로드 시점을 UI 가 결정할 수 있게 하려는 것이다.
/// </summary>
public class ItemData
{
    public int Index;
    public string Name = "";
    public string Category = "";
    public string Description = "";

    /// <summary>Resources/Icons/skill_NNN 의 NNN. 아이템 전용 아이콘이 준비되면 경로만 바꾼다.</summary>
    public int IconIndex;

    /// <summary>소지 수량. 0 이면 목록에 나타나지 않는다.</summary>
    public int Count;

    public int Price;
    public int SellPrice;

    public bool UsableInBattle;
    public bool UsableInField;

    public string EffectType = "";
    public int EffectValue;

    /// <summary>N / R / SR. 색 구분에 쓸 수 있다.</summary>
    public string Rarity = "N";

    public override string ToString() => $"{Name} x{Count} ({Category})";
}
