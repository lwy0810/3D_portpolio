using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ViewManager : MonoBehaviour
{
    public static ViewManager ViewInstance;

    [SerializeField] private CharacterView _characterView;
    [SerializeField] private FieldMenuView _fieldMenuView;
    [SerializeField] private CommandBattleView _commandBattleView;
    //[SerializeField] private MenuButtons _menuButtons;
    [SerializeField] private MemberBar _memberBar;
    [SerializeField] private KeyInfo _keyInfo;
    [SerializeField] private Map _map;
    [SerializeField] private SystemView _systemView;

    [SerializeField] private GameObject _CharacterMemberPrefab;

    private Camera _mainCamera;
    private GameObject _character;
    private KeyInfo _keyInfoComponent;
    private CommandBattleView _commandBattleViewComponent;

    public bool IsMenuActive = false;


    //public bool IsUISelect { get; set; } = false;


    void Awake()
    {
        if (ViewInstance != null)
        {
            Destroy(gameObject);
            return;
        }

        ViewInstance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
    }

    void Update()
    {

        if (SceneManager.GetActiveScene().name == "Field")
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                _fieldMenuView.gameObject.SetActive(true);

                if (IsMenuActive == false)
                {
                    Debug.Log("1111");
                    _fieldMenuView.FieldMenuViewShow();
                    IsMenuActive = true;
                }
            }


        }
    }


    // Common 
    private void UIConnect()
    {
        if (SceneManager.GetActiveScene().name == "Field")
        {
            _keyInfoComponent = _keyInfo.GetComponent<KeyInfo>();
            //_keyInfo = GameObject.Find("KeyInfoBar").GetComponent<KeyInfo>();
            //_menuButtons = GameObject.Find("MenuButtons").GetComponent<MenuButtons>();
            //_memberBar = GameObject.Find("MemberBar").GetComponent<MemberBar>();
            //_characterView = GameObject.Find("CharacterView").GetComponent<CharacterView>();
        }
        else if (SceneManager.GetActiveScene().name == "CommandBattle")
        {
            _commandBattleView = GameObject.Find("CommandBattleView").GetComponent<CommandBattleView>();
        }
    }


    private void UIShow()
    {
        if (SceneManager.GetActiveScene().name == "Field")
        {
            FieldUIShow(true);
        }
        else if (SceneManager.GetActiveScene().name == "CommandBattle")
        {

            CommandBattleUIShow(true);
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UIConnect();
        UIShow();

    }

    private void FieldUIShow(bool active)
    {
        _keyInfo.gameObject.SetActive(active);
        //_menuButtons.gameObject.SetActive(active);
        //_memberBar.gameObject.SetActive(active);
        _characterView.gameObject.SetActive(false);
    }

    public void CharacterViewShow()
    {
        _keyInfoComponent.MouseLeftButtonColorReset();

        _characterView.gameObject.SetActive(true);
        //_menuButtons.gameObject.SetActive(false);
        _memberBar.gameObject.SetActive(true);
        _map.gameObject.SetActive(false);
        _keyInfo.gameObject.SetActive(false);

        CharacterViewCharacterInfoSet(GameManager.GameInstance.CharacterComponents[0]);
    }

    public void CharacterViewUnShow()
    {
        _characterView.gameObject.SetActive(false);
        _keyInfo.gameObject.SetActive(true);
        //_menuButtons.gameObject.SetActive(true);
        _memberBar.gameObject.SetActive(true);
        _map.gameObject.SetActive(true);
    }


    private void CommandBattleUIShow(bool active)
    {
        _commandBattleView.gameObject.SetActive(active);
    }

    public void SystemViewUIShow()
    {
        _systemView.gameObject.SetActive(true);
    }

    public void SystemViewUIUnShow()
    {
        _systemView.gameObject.SetActive(false);
    }

    public void GoIntroScene()
    {
        SceneManager.LoadScene(0);
    }

    public void GoFieldScene()
    {
        SceneManager.LoadScene(1);
    }




    public void CommandBattleViewShow(bool active)
    {
        _commandBattleView.gameObject.SetActive(active);
    }

    public void CommandBattleViewActive(ViewCategory _viewCategory, bool active)
    {
        _commandBattleView.ViewShow(_viewCategory, active);
    }

    public Vector3 CommandAreaPosSet(Vector3 _lookPos)
    {
        return _commandBattleView.CommandAreaPosSet(_lookPos);
    }

    public Vector3 MonsterTargetViewPosSet(int index)
    {
        return _commandBattleView.TargetViewPosSet(index);
    }

    //public void MonsterInfoSet(string _name, int _hp)
    //{
    //    _commandBattleView.MonsterInfoSet(_name, _hp);
    //}

    public void MonsterInfoSet(int index)
    {
        //Debug.Log($"index = {index}");
        _commandBattleView.MonsterInfoSet(index);
    }


    public void CharacterViewCharacterInfoSet(Character _character)
    {
        if (CharacterViewIsActive())
        {
            Debug.Log("active false");
            _characterView.CharacterViewInfoSet(_character);
        }
    }

    public bool CharacterViewIsActive()
    {
        if (ViewManager.ViewInstance._characterView.gameObject.activeSelf)
        {
            return true;
        }
        else
        {
            return false;
        }
    }



}
