using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MemberBar : MonoBehaviour
{
    [SerializeField] private GameObject _fieldMember;

    public List<Member> MemberList = new List<Member>();


    void Start()
    {
        CreateFieldMember();
    }


    void CreateFieldMember()
    {
        for (int i = 0; i < GameManager.GameInstance.Characters.Count; i++)
        {
            Member _memberComponent = null;
            
            GameObject _member = Instantiate(_fieldMember, this.transform);
            _memberComponent = _member.GetComponent<Member>();
            MemberList.Add(_memberComponent);

            _memberComponent._characterComponent = GameManager.GameInstance.Characters[i].GetComponent<Character>();
     

            _memberComponent.MemberImageSet();
            _memberComponent._elementBackgroundSet();
        }
    }
}
