using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusView : MonoBehaviour
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
    [SerializeField] private Text _defValue;
    [SerializeField] private Text _speedValue;
    [SerializeField] private Text _hitValue;
    [SerializeField] private Text _criValue;
    [SerializeField] private Text _criDmgValue;
    [SerializeField] private Text _avoidValue;

    [SerializeField] private Image _characterImage;

    private List<Character> characters = new List<Character>();

    private Character _currentCharacter;

    void Start()
    {
        GetCharacterStatus(GameManager.GameInstance.Characters);
        CharacterStatusView(characters[0]);
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    public void CharacterStatusView(Character _character)
    {
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

    private void Refresh()
    {
        Stat _stat = _currentCharacter.Stat;

        _nameText.text = _stat.Name;
        _lvValue.text = _stat.Level.ToString();
        _hpValue.text = _stat.Hp.ToString();
        _epValue.text = _stat.Ep.ToString();
        _spValue.text = _stat.Cp.ToString();
        _strValue.text = _stat.Str.ToString();
        _defValue.text = _stat.Def.ToString();
        _speedValue.text = _stat.Speed.ToString();
        _hitValue.text = $"{_stat.Hit * 100}%";
        _criValue.text = $"{_stat.Critical * 100}%";
        _avoidValue.text = $"{_stat.Avoid * 100}%";
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
