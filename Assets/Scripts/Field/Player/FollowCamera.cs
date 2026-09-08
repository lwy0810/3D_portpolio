using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    //[SerializeField] private GameObject _player;

    private GameObject _character;

    void Start()
    {
        this.transform.position = Vector3.zero;
        _character = FindCharacter();
    }

    void Update()
    {
        // 전투 중에는 BattleManager.CameraZoom() 이 카메라를 몰기 때문에
        // 여기서 손대면 두 코드가 매 프레임 싸워 카메라가 떤다.
        // 씬을 갈지 않으므로 이 컴포넌트는 전투 중에도 계속 살아 있다.
        if (!GameFlow.IsField) return;

        // 목록에 파괴된 오브젝트가 남을 수 있다.
        // 대상이 사라졌으면 다시 찾고, 못 찾으면 이번 프레임은 건너뛴다
        if (_character == null) _character = FindCharacter();
        if (_character == null) return;

        this.transform.position = _character.transform.position + new Vector3(0.0f, 10.0f, -7.0f);

        Vector3 vec = _character.transform.position - this.transform.position;
        this.transform.rotation = Quaternion.LookRotation(vec);
    }

    /// <summary>살아 있는 첫 캐릭터. 목록이 비었거나 전부 파괴된 경우 null.</summary>
    private GameObject FindCharacter()
    {
        if (GameManager.GameInstance == null) return null;

        List<GameObject> _list = GameManager.GameInstance.Characters;
        if (_list == null) return null;

        for (int i = 0; i < _list.Count; i++)
        {
            if (_list[i] != null) return _list[i];
        }

        return null;
    }
}
