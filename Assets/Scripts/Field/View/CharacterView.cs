using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _characterImage;
    [SerializeField] private StatusView _statusView;

    void Start()
    {
    }

    void Update()
    {
    }

    public void CharacterImageSet(string filePath)
    {
        Sprite sprite = Resources.Load<Sprite>(filePath);
        _characterImage.sprite = sprite;
    }

    public void CharacterViewInfoSet(Character _chareacter)
    {
        Debug.Log($" _chareacter = {_chareacter}");
        _statusView.CharacterStatusView(_chareacter);
    }



}
