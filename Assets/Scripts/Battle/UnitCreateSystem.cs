using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnitCreateSystem : MonoBehaviour
{
    private Vector3 InitCharacterPos = new Vector3(2438.0f, 49.2f, 830.0f);
    private Vector3 InitMonsterPos = new Vector3(2438.0f, 49.2f, 840.0f);

    private Vector3 returnCharacterPos = new Vector3(2438.0f, 49.2f, 780.0f);
    private Vector3 returnMonsterPos = new Vector3(2438.0f, 49.2f, 840.0f);

    private Vector3 fieldPositionOffest = new Vector3(0.0f, 0.0f, -2.0f);
    private Vector3 battlePositionOffest = new Vector3(-3.0f, 0.0f, 0.0f);

    private const int BattleMonsterCount = 3;

    public void CharacterCreate(GameObject[] _characterPrefabs)
    {
        CsvTable csv = CSVFileLoader.LoadTable("CharacterStatus");

        if (csv.Rows.Count < _characterPrefabs.Length)
        {
            Debug.LogError($"[UnitCreateSystem] CharacterStatus.csv 행({csv.Rows.Count})이 " +
                           $"프리팹 수({_characterPrefabs.Length})보다 적습니다.");
        }

        for (int i = 0; i < _characterPrefabs.Length; i++)
        {
            if (SceneManager.GetActiveScene().name == "Field")
            {
                Vector3 basePos = GameManager.GameInstance.IsInit ? InitCharacterPos : returnCharacterPos;

                GameManager.GameInstance.Characters.Add(
                    Instantiate(_characterPrefabs[i], basePos + (i * fieldPositionOffest), Quaternion.identity));

                if (i > 0) GameManager.GameInstance.Characters[i].SetActive(false);
            }
            else if (SceneManager.GetActiveScene().name == "CommandBattle")
            {
                GameManager.GameInstance.Characters.Add(
                    Instantiate(_characterPrefabs[i], Vector3.zero + (i * battlePositionOffest), Quaternion.identity));

                GameManager.GameInstance.Units.Add(
                    GameManager.GameInstance.Characters[i].GetComponent<Character>());
            }

            GameManager.GameInstance.CharacterComponents.Add(
                GameManager.GameInstance.Characters[i].GetComponent<Character>());

            Character _characterComponent = GameManager.GameInstance.CharacterComponents[i];

            if (i < csv.Rows.Count) CharacterStat(csv.Rows[i], _characterComponent);
        }
    }

    public void MonsterCreate(GameObject _monsterPrefabs)
    {
        CsvTable csv = CSVFileLoader.LoadTable("MonsterStatus");

        if (csv.Rows.Count == 0)
        {
            Debug.LogError("[UnitCreateSystem] MonsterStatus.csv 에 데이터 행이 없습니다.");
            return;
        }

        CsvTable.Row row = csv.Rows[0];

        if (SceneManager.GetActiveScene().name == "Field")
        {
            Vector3 basePos = GameManager.GameInstance.IsInit ? InitMonsterPos : returnMonsterPos;

            GameManager.GameInstance.Monster =
                Instantiate(_monsterPrefabs, basePos, Quaternion.Euler(0.0f, 180.0f, 0.0f));

            MonsterStat(row, GameManager.GameInstance.Monster.GetComponent<Monster>());
        }
        else if (SceneManager.GetActiveScene().name == "CommandBattle")
        {
            for (int i = 0; i < BattleMonsterCount; i++)
            {
                GameManager.GameInstance.Monsters.Add(
                    Instantiate(_monsterPrefabs,
                                Vector3.zero + new Vector3(i * -3.0f, 0.0f, 10.0f),
                                Quaternion.Euler(0.0f, 180.0f, 0.0f)));

                Monster _monsterComponent = GameManager.GameInstance.Monsters[i].GetComponent<Monster>();

                // 몬스터는 개체마다 다른 Stat 을 가져야 한다. 3기가 같은 인스턴스를
                // 참조하면 한 마리가 맞을 때 세 마리 체력이 함께 줄어든다.
                MonsterStat(row, _monsterComponent);

                GameManager.GameInstance.Units.Add(_monsterComponent);
            }
        }
    }

    // ── 스탯 부여 ───────────────────────────────────────────

    /// <summary>
    /// 캐릭터 스탯. StatusManager 에 캐시가 있으면 그 참조를 그대로 연결한다.
    /// 값을 복사하지 않으므로 씬이 바뀌어도 Hp / Cp / Ep 가 유지된다.
    /// </summary>
    private void CharacterStat(CsvTable.Row row, Character _character)
    {
        string _name = row.GetString("name");

        if (string.IsNullOrEmpty(_name))
        {
            Debug.LogError($"[UnitCreateSystem] CharacterStatus.csv line {row.LineNumber} : name 이 비어 있습니다.");
            return;
        }

        Stat _stat;

        if (StatusManager.StatusInstance.HasCharacterStat(_name))
        {
            _stat = StatusManager.StatusInstance.GetCharacterStat(_name);
        }
        else
        {
            _stat = BuildStat(row, isCharacter: true);
            StatusManager.StatusInstance.RegisterCharacterStat(_name, _stat);
        }

        _character.SetStat(_stat);
    }

    /// <summary>몬스터 스탯. 매번 새로 만든다 (영속 대상이 아님).</summary>
    private void MonsterStat(CsvTable.Row row, Monster _monster)
    {
        _monster.SetStat(BuildStat(row, isCharacter: false));
    }

    /// <summary>
    /// 컬럼 이름으로 읽으므로 CSV 에 컬럼을 추가하거나 순서를 바꿔도 이 코드는 그대로다.
    /// 없는 컬럼은 fallback 이 적용된다.
    /// </summary>
    private Stat BuildStat(CsvTable.Row row, bool isCharacter)
    {
        Stat s = new Stat();

        if (isCharacter) s.Leader = row.GetString("Leader", "FALSE");

        s.Name = row.GetString("name");
        s.Category = row.GetString("category", isCharacter ? "character" : "monster");
        s.Element = row.GetString("element", "none");
        s.Level = row.GetInt("lv", 1);

        s.MaxHp = row.GetInt("maxHp", 1);
        s.Hp = row.GetInt("hp", s.MaxHp);

        s.EnergyPoint = row.GetInt("ep", 100);

        // sp 컬럼은 CP 로 승계한다. cp / maxCp 컬럼이 있으면 그쪽을 우선한다.
        s.MaxCp = row.Has("maxCp") ? row.GetInt("maxCp", 200) : 200;
        s.Cp = row.Has("cp") ? row.GetInt("cp") : 0;

        s.Atk = row.GetInt("atk");
        s.Def = row.GetInt("def");

        // ats / adf 가 없으면 물리 스탯을 그대로 쓴다 (기존 CSV 하위 호환)
        s.Ats = row.Has("ats") ? row.GetInt("ats") : s.Atk;
        s.Adf = row.Has("adf") ? row.GetInt("adf") : s.Def;

        s.Speed = row.GetInt("speed", 1);
        s.Dex = row.Has("dex") ? row.GetInt("dex") : 20;
        s.Agl = row.Has("agl") ? row.GetInt("agl") : 10;

        s.Avoid = row.GetFloat("avoid");
        s.Critical = row.GetFloat("cri");
        s.CriticalDmg = row.Has("criDmg") ? row.GetFloat("criDmg", 0.5f) : 0.5f;
        s.Hit = row.GetFloat("hit");

        s.MaxBreak = row.GetInt("maxBreak");
        s.Experience = row.GetInt("experience");

        return s;
    }
}
