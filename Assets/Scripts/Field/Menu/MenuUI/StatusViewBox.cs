using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class StatusViewBox : MonoBehaviour
{
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _lvText;
    [SerializeField] private Text _hpText;
    [SerializeField] private Text _hpMaxText;
    [SerializeField] private Text _epText;
    [SerializeField] private Text _epMaxText;
    [SerializeField] private Text _spText;
    [SerializeField] private Text _spMaxText;
    [SerializeField] private Text _strText;
    [SerializeField] private Text _defText;
    [SerializeField] private Text _spdText;
    [SerializeField] private Text _avdText;
    [SerializeField] private Text _criText;
    [SerializeField] private Text _hitText;



    private Character _character;

    void OnDisable()
    {
        Unsubscribe();
    }

    public void SetStatus(Character character)
    {
        Unsubscribe();

        _character = character;
        _character.Stat.OnStatusChanged += HandleStatusChanged;

        Refresh();
    }

    private void HandleStatusChanged(Stat _changedStat)
    {
        if (_character != null && _changedStat == _character.Stat)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        _nameText.text = _character.Stat.Name;
        _lvText.text = _character.Stat.Level.ToString();
        _hpText.text = _character.Stat.Hp.ToString();
        _hpMaxText.text = _character.Stat.MaxHp.ToString();
        _epText.text = _character.Stat.Ep.ToString();
        _epMaxText.text = _character.Stat.MaxEp.ToString();
        _spText.text = _character.Stat.Cp.ToString();
        _spMaxText.text = _character.Stat.MaxCp.ToString();
        _strText.text = _character.Stat.Str.ToString();
        _defText.text = _character.Stat.Def.ToString();
        _spdText.text = _character.Stat.Speed.ToString();
        _avdText.text = _character.Stat.Avoid.ToString();
        _criText.text = _character.Stat.Critical.ToString();
        _hitText.text = _character.Stat.Hit.ToString();
    }

    private void Unsubscribe()
    {
        if (_character != null)
        {
            _character.Stat.OnStatusChanged -= HandleStatusChanged;
        }
    }
}

