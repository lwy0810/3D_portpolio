using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting;
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

    void Start()
    {
    }

    void Update()
    {
    }

    public void CharacterCreate(GameObject[] _characterPrefabs)
    {
        string text = CSVFileLoader.OnCharacterLoadCSV("CharacterStatus");
        Debug.Log(text);

        string[] _csvDataList = text.Split('\n');

        Debug.Log(text);
        for (int i = 0; i < _characterPrefabs.Length; i++)
        {
            if (SceneManager.GetActiveScene().name == "Field")
            {
                if (GameManager.GameInstance.IsInit == true)
                {
                    GameManager.GameInstance.Characters.Add
                        (Instantiate(_characterPrefabs[i], InitCharacterPos + (i * fieldPositionOffest), Quaternion.identity));
                }
                else
                {
                    GameManager.GameInstance.Characters.Add(
                        Instantiate(_characterPrefabs[i], returnCharacterPos + (i * fieldPositionOffest), Quaternion.identity));
                }

                if (i > 0)
                {
                    GameManager.GameInstance.Characters[i].gameObject.SetActive(false);
                }
            }
            else if (SceneManager.GetActiveScene().name == "CommandBattle")
            {
                GameManager.GameInstance.Characters.Add
                    (Instantiate(_characterPrefabs[i], Vector3.zero + (i * battlePositionOffest), Quaternion.identity));

                GameManager.GameInstance.Units.Add(GameManager.GameInstance.Characters[i].GetComponent<Character>());
            }

            GameManager.GameInstance.CharacterComponents.Add(GameManager.GameInstance.Characters[i].GetComponent<Character>());
            Character _characterComponent = GameManager.GameInstance.CharacterComponents[i];
            CharacterStat(_csvDataList[i + 1], _characterComponent);

        }
    }

    public void MonsterCreate(GameObject _monsterPrefabs)
    {
        List<Monster> _monsters = new List<Monster>();

        string text = CSVFileLoader.OnMonsterLoadCSV("MonsterStatus");

        string[] _csvDataList = text.Split('\n');


        if (SceneManager.GetActiveScene().name == "Field")
        {
            if (GameManager.GameInstance.IsInit == true)
            {
                GameManager.GameInstance.Monster = 
                    Instantiate(_monsterPrefabs, InitMonsterPos, Quaternion.Euler(0.0f, 180.0f, 0.0f));
            }
            else
            {
                GameManager.GameInstance.Monster = 
                    Instantiate(_monsterPrefabs, returnMonsterPos, Quaternion.Euler(0.0f, 180.0f, 0.0f));
            }
            Monster _monsterComponent = GameManager.GameInstance.Monster.GetComponent<Monster>();
            MonsterStat(_csvDataList[1], _monsterComponent);
        }
        else if (SceneManager.GetActiveScene().name == "CommandBattle")
        {
            for (int i = 0; i < 3; i++)
            {
                GameManager.GameInstance.Monsters.Add
                    (Instantiate(_monsterPrefabs, (Vector3.zero + new Vector3(i * -3.0f, 0.0f, 10.0f)), Quaternion.Euler(0.0f, 180.0f, 0.0f)));
                Monster _monsterComponent = GameManager.GameInstance.Monsters[i].GetComponent<Monster>();
                MonsterStat(_csvDataList[1], _monsterComponent);
                GameManager.GameInstance.Units.Add(GameManager.GameInstance.Monsters[i].GetComponent<Monster>());
            }
        }  
    }

    private void CharacterStat(string _characterCsvDataRow, Character _character)
    {
        Debug.Log("111" + _characterCsvDataRow);
        string[] _characteCsvData = _characterCsvDataRow.Split(',');
        string _name = _characteCsvData[2];

        Debug.Log("2. name : " + _name);

        Stat _stat;

        if(StatusManager.StatusInstance.HasCharacterStat(_name))
        {
            _stat = StatusManager.StatusInstance.GetCharacterStat(_name);
        } 
        else
        {
            _stat = new Stat();

            _stat.Leader = _characteCsvData[1];
            _stat.Name = _characteCsvData[2];
            _stat.Category = _characteCsvData[3];
            _stat.Element = _characteCsvData[4];
            _stat.Level = int.Parse(_characteCsvData[5]);
            _stat.Hp = int.Parse(_characteCsvData[6]);
            _stat.MaxHp = int.Parse(_characteCsvData[7]);
            _stat.EnergyPoint = int.Parse(_characteCsvData[8]);
            _stat.SpecialPoint = int.Parse(_characteCsvData[9]);
            _stat.Atk = int.Parse(_characteCsvData[10]);
            _stat.Def = int.Parse(_characteCsvData[11]);
            _stat.Speed = int.Parse(_characteCsvData[12]);
            _stat.Avoid = float.Parse(_characteCsvData[13]);
            _stat.Critical = float.Parse(_characteCsvData[14]);
            _stat.CriticalDmg = float.Parse(_characteCsvData[15]);
            _stat.Hit = float.Parse(_characteCsvData[16]);
            _stat.Experience = int.Parse(_characteCsvData[17]);

            StatusManager.StatusInstance.RegisterCharacterStat(_name, _stat);
        }

        _character.SetStat(_stat);

    }

    private void MonsterStat(string _monsterCsvDataRow, Monster _monster)
    {
        string[] _monsterCsvData = _monsterCsvDataRow.Split(',');

        Stat _stat;

        _stat = new Stat();
        _stat.Name = _monsterCsvData[1];
        _stat.Category = _monsterCsvData[2];
        _stat.Element = _monsterCsvData[3];
        _stat.Level = int.Parse(_monsterCsvData[4]);
        _stat.Hp = int.Parse(_monsterCsvData[5]);
        _stat.MaxHp = int.Parse(_monsterCsvData[6]);
        _stat.EnergyPoint = int.Parse(_monsterCsvData[7]);
        _stat.SpecialPoint = int.Parse(_monsterCsvData[8]);
        _stat.Atk = int.Parse(_monsterCsvData[9]);
        _stat.Def = int.Parse(_monsterCsvData[10]);
        _stat.Speed = int.Parse(_monsterCsvData[11]);
        _stat.Avoid = float.Parse(_monsterCsvData[12]);
        _stat.Critical = float.Parse(_monsterCsvData[13]);
        _stat.Experience = int.Parse(_monsterCsvData[14]);

        _monster.SetStat(_stat);

    }

}
