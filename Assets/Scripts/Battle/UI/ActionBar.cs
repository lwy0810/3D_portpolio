using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;


public class ActionBar : MonoBehaviour
{
    //[SerializeField] private GameObject _commandActionMemberPrefab; // 

    private GameObject _commandActionMember;
    private int _initActionMemberCount;

    private Queue que = new Queue();
  
   
    void Start()
    {
        //CreateCommandActionMember();
    }


    void Update()
    {

    }


    void CommandActionMemberBarPositioning()
    {
        List<GameObject> _cbjectList = new List<GameObject>();
        var _characters = GameObject.FindGameObjectsWithTag("Character");
        var _monsters = GameObject.FindGameObjectsWithTag("Monster");

        foreach(var _character in _characters)
        {
            _cbjectList.Add(_character);
        }

        foreach (var _monster in _monsters)
        {
            _cbjectList.Add(_monster);
        }

        List<GameObject> positionignList = new List<GameObject>();


    }

    //void CreateCommandActionMember()
    //{
    //    for (int i = 0; i < GameManager.GameInstance._characterPrefabs.Length; i++)
    //    {
    //        Instantiate(_commandActionMemberPrefab, this.transform);
    //    }
    //}



}


