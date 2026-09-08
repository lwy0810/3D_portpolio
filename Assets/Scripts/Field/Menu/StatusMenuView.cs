using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusMenuView : MonoBehaviour
{
    // TopBar
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _lvValue;
    [SerializeField] private Image _elementImage;

    // MiddleBar
    [SerializeField] private Text _hpValue;
    [SerializeField] private Text _epValue;
    [SerializeField] private Text _spValue;

    // BottomBar
    [SerializeField] private Text _strValue;
    [SerializeField] private Text _atsValue;
    [SerializeField] private Text _spdValue;
    [SerializeField] private Text _dexValue;
    [SerializeField] private Text _aglValue;
    [SerializeField] private Text _rngValue;
    [SerializeField] private Text _defValue;
    [SerializeField] private Text _adfValue;
    [SerializeField] private Text _movValue;
    [SerializeField] private Text _hitValue;
    [SerializeField] private Text _avoidValue;
    [SerializeField] private Text _criValue;
    [SerializeField] private Image _characterImage;

    private List<Character> characters = new List<Character>();

    private Character _currentCharacter;

    void Start()
    {
        if (GameManager.GameInstance == null)
        {
            Debug.LogWarning($"[StatusMenuView] {ObjectPath()} : GameManager 가 없어 초기화를 건너뜁니다.");
            return;
        }

        GetCharacterStatus(GameManager.GameInstance.Characters);

        if (characters.Count == 0)
        {
            Debug.LogWarning($"[StatusMenuView] {ObjectPath()} : 파티 목록이 비어 있어 초기화를 건너뜁니다.");
            return;
        }

        CharacterStatusView(characters[0]);
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    public void CharacterStatusView(Character _character)
    {
        if (_character == null || _character.Stat == null)
        {
            Debug.LogWarning($"[StatusMenuView] {ObjectPath()} : 넘겨받은 캐릭터가 비어 있습니다.");
            return;
        }

        Unsubscribe();

        _currentCharacter = _character;
        _currentCharacter.Stat.OnStatusChanged += HandleStatusChanged;

        Refresh();
    }

    private void HandleStatusChanged(Stat _changedStat)
    {
        if (_currentCharacter != null && _changedStat == _currentCharacter.Stat)
        {
            Refresh();
        }
    }

    /// <summary>
    /// 인스펙터 칸(Text)이 비어 있으면 조용히 넘기지 않고 어느 칸이 비었는지 경고한다.
    /// HomeMenuView 를 복사해 EquipMenuView / SystemMenuView 를 만들 때 이런 참조가
    /// 끊기는 문제가 CharacterImageView 에서도 있었다(같은 원인, 같은 방식으로 방어).
    /// </summary>
    private void Refresh()
    {
        if (_currentCharacter == null || _currentCharacter.Stat == null) return;

        Stat _stat = _currentCharacter.Stat;

        SetText(_nameText, _stat.Name, nameof(_nameText));
        SetText(_lvValue, _stat.Level.ToString(), nameof(_lvValue));
        SetText(_hpValue, _stat.Hp.ToString(), nameof(_hpValue));
        SetText(_epValue, _stat.Ep.ToString(), nameof(_epValue));
        SetText(_spValue, _stat.Cp.ToString(), nameof(_spValue));
        SetText(_strValue, _stat.Str.ToString(), nameof(_strValue));
        SetText(_atsValue, _stat.Ats.ToString(), nameof(_atsValue));
        SetText(_spdValue, _stat.Speed.ToString(), nameof(_spdValue));
        SetText(_dexValue, _stat.Dex.ToString(), nameof(_dexValue));
        SetText(_aglValue, _stat.Agl.ToString(), nameof(_aglValue));
        SetText(_rngValue, _stat.Rng.ToString(), nameof(_rngValue));
        SetText(_defValue, _stat.Def.ToString(), nameof(_defValue));
        SetText(_adfValue, _stat.Adf.ToString(), nameof(_adfValue));
        SetText(_movValue, _stat.Mov.ToString(), nameof(_movValue));
        SetText(_hitValue, $"{_stat.Hit * 100}%", nameof(_hitValue));
        SetText(_avoidValue, $"{_stat.Avoid * 100}%", nameof(_avoidValue));
        SetText(_criValue, $"{_stat.Critical * 100}%", nameof(_criValue));
    }

    private void SetText(Text field, string value, string fieldName)
    {
        if (field == null)
        {
            Debug.LogWarning($"[StatusMenuView] {ObjectPath()} : 인스펙터의 {fieldName} 칸이 비어 있어 건너뜁니다.");
            return;
        }

        field.text = value;
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

    private void Unsubscribe()
    {
        if (_currentCharacter != null)
        {
            _currentCharacter.Stat.OnStatusChanged -= HandleStatusChanged;
        }
    }

 
    private void GetCharacterStatus(List<GameObject> _characters)
    {
        int count = GameManager.GameInstance.Characters.Count;

        for (int i = 0; i < count; i++)
        {
            characters.Add(_characters[i].GetComponent<Character>());
        }
    }




}
