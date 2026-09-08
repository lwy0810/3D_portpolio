using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CharacterImageView : MonoBehaviour
{
    [SerializeField] private Image[] _characterImages;
    [SerializeField] private StatusBox[] _characterStatusBoxes;

    private List<Character> _characters = new List<Character>();

    void Start() 
    {
        if (GameManager.GameInstance == null)
        {
            Debug.LogWarning($"[CharacterImageView] {ObjectPath()} : GameManager 가 없어 초기화를 건너뜁니다.");
            return;
        }

        _characters = GameManager.GameInstance.CharacterComponents;
        SetStatusBox(_characters);
    }


    private void SetStatusBox(List<Character> characters)
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning($"[CharacterImageView] {ObjectPath()} : 파티 목록이 비어 있습니다.");
            return;
        }

        int _imageCount = _characterImages != null ? _characterImages.Length : 0;
        int _boxCount = _characterStatusBoxes != null ? _characterStatusBoxes.Length : 0;

        // 파티 인원 수로 순회하면서 인스펙터 배열을 인덱싱하면, 배열이 더 짧을 때
        // IndexOutOfRangeException 이 난다. 두 배열 중 짧은 쪽까지만 채운다
        int _slots = Mathf.Min(_imageCount, _boxCount);
        int _count = Mathf.Min(_slots, characters.Count);

        if (_slots < characters.Count)
        {
            // 조용히 넘기면 화면에 일부 인원이 빠진 채로 남아 원인을 찾기 어렵다.
            // 배열이 비어 있는 경우는 대개 다른 메뉴를 복사할 때 참조가 끊긴 것이다
            Debug.LogWarning($"[CharacterImageView] {ObjectPath()} : " +
                             $"파티 {characters.Count}명이지만 슬롯이 {_slots}개입니다 " +
                             $"(Character Images {_imageCount} / Character Status Boxes {_boxCount}). " +
                             $"인스펙터 배열을 채우거나, 이 화면에서 파티 정보가 필요 없다면 " +
                             $"CharacterImageView 컴포넌트를 제거하세요.");
        }

        for (int i = 0; i < _count; i++) 
        {
            Character _character = characters[i];

            if (_character == null || _character.Stat == null)
            {
                Debug.LogWarning($"[CharacterImageView] {ObjectPath()} : {i}번 캐릭터가 비어 있습니다.");
                continue;
            }

            // 배열 길이는 맞아도 개별 항목이 None 인 경우가 있다
            if (_characterStatusBoxes[i] != null)
            {
                _characterStatusBoxes[i].SetStatus(_character);
            }

            if (_characterImages[i] != null)
            {
                string _filePath = $"Images/Character/{_character.Stat.Name}";
                Sprite _sprite = Resources.Load<Sprite>(_filePath);

                if (_sprite == null)
                {
                    Debug.LogWarning($"[CharacterImageView] {_filePath} 를 찾지 못했습니다.");
                }

                _characterImages[i].sprite = _sprite;
            }
        }
    }


    /// <summary>경고에 쓰는 하이어라키 경로. 같은 스크립트가 여러 화면에 붙어 있어 구분이 필요하다.</summary>
    private string ObjectPath()
    {
        Transform _t = transform;
        string _path = _t.name;

        while (_t.parent != null)
        {
            _t = _t.parent;
            _path = _t.name + "/" + _path;
        }

        return _path;
    }
}
