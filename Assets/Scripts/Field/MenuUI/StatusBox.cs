using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusBox : MonoBehaviour
{
    [SerializeField] private Image _leadershipImage;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _lvText;
    [SerializeField] private TextMeshProUGUI _HpText;
    [SerializeField] private TextMeshProUGUI _EpText;
    [SerializeField] private TextMeshProUGUI _spText;

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
        string filePath = $"Images/Water";

        if (_character.Stat.Leader == "active")
        {
            _leadershipImage.gameObject.SetActive(true);
            _leadershipImage.sprite = Resources.Load<Sprite>(filePath);
        } else
        {
            _leadershipImage.gameObject.SetActive(false);
        }

        _nameText.text = _character.Stat.Name;
        _lvText.text = _character.Stat.Level.ToString();
        _HpText.text = _character.Stat.Hp.ToString();
        _EpText.text = _character.Stat.Ep.ToString();
        _spText.text = _character.Stat.Cp.ToString();
    }

    private void Unsubscribe()
    {
        if (_character != null)
        {
            _character.Stat.OnStatusChanged -= HandleStatusChanged;
        }
    }
}
