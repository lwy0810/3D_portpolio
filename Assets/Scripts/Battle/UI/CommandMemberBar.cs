using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandMemberBar : MonoBehaviour
{
    [SerializeField] private GameObject _commandBattleMemberPrefab; // 

    void Start()
    {
        //CreateCommandBattleMember();
    }

    void Update()
    {
    }

    //void CreateCommandBattleMember()
    //{
    //    for (int i = 0; i < GameManager.GameInstance._characterPrefabs.Length; i++)
    //    {
    //        Instantiate(_commandBattleMemberPrefab, this.transform);
    //    }
    //}

}



