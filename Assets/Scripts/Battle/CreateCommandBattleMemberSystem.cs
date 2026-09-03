using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCommandBattleMemberSystem : MonoBehaviour
{
    public List<GameObject> _battleMemberList;



    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<GameObject> CreateCommandBattleMember( GameObject _battleMember, Transform _BattleMemberBarTransform)
    {
        for (int i = 0; i < GameManager.GameInstance._characterPrefabs.Length; i++)
        {
            Instantiate(_battleMember, _BattleMemberBarTransform);
        }

        return _battleMemberList;

    }







}
