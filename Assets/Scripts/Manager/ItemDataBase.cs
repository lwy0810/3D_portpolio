using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이템 목록. Resources/Status/ItemStatus.csv 를 1회 읽어 보관한다.
///
/// SkillDataBase 와 같은 구조다. 헤더 이름으로 읽으므로 CSV 에 컬럼을 추가하거나
/// 순서를 바꿔도 이 코드는 그대로다. 카테고리 목록은 코드가 단일 출처다 —
/// CSV 에서 유도하면 해당 카테고리의 아이템을 다 소진했을 때 탭이 사라진다.
/// </summary>
public class ItemDataBase : MonoBehaviour
{
    public static ItemDataBase ItemInstance;

    /// <summary>탭에 표시할 카테고리. 순서가 곧 탭 순서다.</summary>
    public static readonly string[] Categories =
    {
        "도구", "무기", "방어구", "장식구", "재료"
    };

    private const string FileName = "ItemStatus";

    private readonly List<ItemData> _items = new List<ItemData>();
    private readonly Dictionary<string, List<ItemData>> _byCategory =
        new Dictionary<string, List<ItemData>>();

    private readonly List<ItemData> _empty = new List<ItemData>();

    private bool _loaded;

    public IReadOnlyList<ItemData> All => _items;

    void Awake()
    {
        if (ItemInstance != null && ItemInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        ItemInstance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    /// <summary>씬에 없어도 쓸 수 있게 필요할 때 만든다.</summary>
    public static ItemDataBase Ensure()
    {
        if (ItemInstance != null) return ItemInstance;

        ItemDataBase _found = FindFirstObjectByType<ItemDataBase>(FindObjectsInactive.Include);
        if (_found != null)
        {
            ItemInstance = _found;

            // 비활성 오브젝트에 붙어 있으면 Awake 가 돌지 않아 목록이 비어 있다.
            // 조용히 빈 목록을 반환하면 메뉴에 아무것도 뜨지 않는다
            if (!_found._loaded) _found.Load();

            return ItemInstance;
        }

        GameObject _obj = new GameObject("ItemDataBase");
        ItemInstance = _obj.AddComponent<ItemDataBase>();
        return ItemInstance;
    }

    private void Load()
    {
        _loaded = true;

        _items.Clear();
        _byCategory.Clear();

        for (int i = 0; i < Categories.Length; i++)
        {
            _byCategory[Categories[i]] = new List<ItemData>();
        }

        CsvTable _csv = CSVFileLoader.LoadTable(FileName);

        if (_csv.Rows.Count == 0)
        {
            Debug.LogError($"[ItemDataBase] Resources/Status/{FileName}.csv 에 데이터 행이 없습니다.");
            return;
        }

        for (int i = 0; i < _csv.Rows.Count; i++)
        {
            CsvTable.Row _row = _csv.Rows[i];

            string _name = _row.GetString("name");
            if (string.IsNullOrEmpty(_name)) continue;   // 빈 행 스킵

            ItemData _item = new ItemData
            {
                Index = _row.GetInt("index"),
                Name = _name,
                Category = _row.GetString("category"),
                Description = _row.GetString("description"),
                IconIndex = _row.GetInt("iconIndex"),
                Count = _row.GetInt("count"),
                Price = _row.GetInt("price"),
                SellPrice = _row.GetInt("sellPrice"),
                UsableInBattle = _row.GetBool("usableInBattle"),
                UsableInField = _row.GetBool("usableInField"),
                EffectType = _row.GetString("effectType"),
                EffectValue = _row.GetInt("effectValue"),
                Rarity = _row.GetString("rarity", "N")
            };

            _items.Add(_item);

            if (!_byCategory.ContainsKey(_item.Category))
            {
                // CSV 에만 있고 탭에는 없는 카테고리. 조용히 버리면 아이템이 사라진 것처럼 보인다
                Debug.LogWarning($"[ItemDataBase] {FileName}.csv line {_row.LineNumber} : " +
                                 $"카테고리 \"{_item.Category}\" 는 탭 목록에 없습니다. " +
                                 $"ItemDataBase.Categories 에 추가하거나 CSV 를 고치세요.");
                _byCategory[_item.Category] = new List<ItemData>();
            }

            _byCategory[_item.Category].Add(_item);
        }

        Debug.Log($"[ItemDataBase] 아이템 {_items.Count}개 로드 완료.");
    }

    /// <summary>해당 카테고리의 소지 아이템. 수량 0 은 제외한다.</summary>
    public List<ItemData> ByCategory(string category)
    {
        List<ItemData> _list;
        if (!_byCategory.TryGetValue(category, out _list)) return _empty;

        // 소지하지 않은 아이템은 목록에 넣지 않는다.
        // 매번 새 List 를 만들지 않도록 결과를 재사용할 수도 있지만,
        // 메뉴를 열 때만 부르는 경로라 단순함을 택했다.
        List<ItemData> _owned = new List<ItemData>();

        for (int i = 0; i < _list.Count; i++)
        {
            if (_list[i].Count > 0) _owned.Add(_list[i]);
        }

        return _owned;
    }

    public ItemData Find(int index)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Index == index) return _items[i];
        }
        return null;
    }

    public ItemData Find(string name)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Name == name) return _items[i];
        }
        return null;
    }

    /// <summary>수량을 더하거나 뺀다. 0 아래로는 내려가지 않는다.</summary>
    public void AddCount(int index, int delta)
    {
        ItemData _item = Find(index);
        if (_item == null) return;

        _item.Count = Mathf.Max(0, _item.Count + delta);
    }
}
