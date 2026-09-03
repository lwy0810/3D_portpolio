using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TargetView : MonoBehaviour
{
    [SerializeField] private Text _monsterNameText;
    [SerializeField] private Text _monsterHpPointText;
    [SerializeField] private Text _monsterMaxHpPointText;

    private List<Monster> _monsters = new List<Monster>();

    void Start()
    {
    }

    public void MonsterInfoSet(int index)
    {
      
        //Debug.Log($"_monsters = {_monsters.Count}");
        _monsterNameText.text = BattleManager.BattleInstance.Monsters[index].Stat.Name;
        _monsterHpPointText.text = BattleManager.BattleInstance.Monsters[index].Stat.Hp.ToString();
        _monsterMaxHpPointText.text = BattleManager.BattleInstance.Monsters[index].Stat.MaxHp.ToString();
    }







}
