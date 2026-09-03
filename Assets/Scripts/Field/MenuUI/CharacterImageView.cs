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
        _characters = GameManager.GameInstance.CharacterComponents;
        SetStatusBox(_characters);
    }


    private void SetStatusBox(List<Character> characters)
    {
        for (int i = 0; i < characters.Count; i++) 
        {
            string filePath = $"Images/Character/{characters[i].Stat.Name}";

            Debug.Log(filePath);

            _characterStatusBoxes[i].SetStatus(characters[i]);
            _characterImages[i].sprite = Resources.Load<Sprite>(filePath);

        }

    }








}
