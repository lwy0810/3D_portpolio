using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuView : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _optionButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _initStartButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private OptionPopUps _optionPopUps;

    public void OnClickStart()
    {
        _startButton.gameObject.SetActive(false);
        _optionButton.gameObject.SetActive(false);
        _exitButton.gameObject.SetActive(false);
        _initStartButton.gameObject.SetActive(true);
        _continueButton.gameObject.SetActive(true);
        _backButton.gameObject.SetActive(true);
    }

    public void OnClickOption()
    {
        _optionPopUps.Show();
    }

    public void OnClickInitStart()
    {
        SceneManager.LoadScene(1);

    }

    public void OnClickContinue()
    {
        SceneManager.LoadScene(2);

    }

    public void OnClickBack()
    {
        _initStartButton.gameObject.SetActive(false);
        _continueButton.gameObject.SetActive(false);
        _backButton.gameObject.SetActive(false);
        _startButton.gameObject.SetActive(true);
        _optionButton.gameObject.SetActive(true);
        _exitButton.gameObject.SetActive(true);
    }

}
