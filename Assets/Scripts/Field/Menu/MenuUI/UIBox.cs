using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBox : MonoBehaviour
{
    //[SerializeField] private FieldMenuView _fieldMenuHome;

    [SerializeField] private UIButton[] _uiButtons;
    [SerializeField] private GameObject[] _subUiButtons;

    [SerializeField] private Text _menuText;

    private CanvasGroup _canvasGroup;

    private bool _isInit = true;
    private string[] _menuTexts = { "STATUS", "EQUIP", "ITEM", "SYSTEM" };

    public int CountIndex { get; set; } = 0;


    void Awake()
    {

    }

    void Start()
    {   
    }

    void Update()
    {
        ButtonMove();
    }

    void ButtonMove()
    {

        if (_isInit)
        {
            _uiButtons[0].SelectBackgroundShow();
            _isInit = false;
            Debug.Log($"CountIndex: {CountIndex}");
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                _uiButtons[CountIndex].SelectBackgroundUnShow();
                CountIndex--;

                if (CountIndex < 0)
                {
                    CountIndex = 3;
                }
                _uiButtons[CountIndex].SelectBackgroundShow();
                _menuText.text = _menuTexts[CountIndex];
                Debug.Log($"CountIndex: {CountIndex}");
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                _uiButtons[CountIndex].SelectBackgroundUnShow();
                CountIndex++;

                if (CountIndex > 3)
                {
                    CountIndex = 0;
                }
                _uiButtons[CountIndex].SelectBackgroundShow();
                _menuText.text = _menuTexts[CountIndex];
                Debug.Log($"CountIndex: {CountIndex}");
            }

        }
    }


    public void UIButtonsShow(int index)
    {
        foreach (var button in _subUiButtons)
        {
           if(button == _subUiButtons[index])
            {
                button.SetActive(true);
            }
            else
            {
                button.SetActive(false);
            }
        }
    }



}
