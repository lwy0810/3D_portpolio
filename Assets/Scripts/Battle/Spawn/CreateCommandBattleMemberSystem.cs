using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCommandBattleMemberSystem : MonoBehaviour
{
    // 예전에는 이 필드가 초기화되지 않아 CreateCommandBattleMember() 가 항상 null 을
    // 반환했고, 배틀 씬을 두 번째로 들어갈 때 BattleManager.Battle() 에서
    // NullReferenceException 이 발생했다.
    public List<GameObject> _battleMemberList = new List<GameObject>();


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public List<GameObject> CreateCommandBattleMember(GameObject _battleMember, Transform _BattleMemberBarTransform)
    {
        _battleMemberList.Clear();

        for (int i = 0; i < GameManager.GameInstance._characterPrefabs.Length; i++)
        {
            GameObject _member = Instantiate(_battleMember, _BattleMemberBarTransform);
            _battleMemberList.Add(_member);
        }

        return _battleMemberList;
    }

}
