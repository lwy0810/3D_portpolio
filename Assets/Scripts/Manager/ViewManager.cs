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

        if (GameFlow.IsField)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (_fieldMenuView == null) return;

                _fieldMenuView.gameObject.SetActive(true);

                if (IsMenuActive == false)
                {
                    Debug.Log("1111");
                    _fieldMenuView.FieldMenuViewShow();
                    IsMenuActive = true;
                    _keyInfo.gameObject.SetActive(false);
                }
            }


        }
    }


    // Common 
    /// <summary>
    /// 씬이 바뀔 때 뷰 참조를 다시 잡는다.
    ///
    /// ViewManager 는 DontDestroyOnLoad 로 살아남지만 인스펙터에 꽂아둔 뷰들은
    /// 씬 안의 오브젝트다. 씬을 다시 불러오면 그 오브젝트가 파괴되어 참조가 죽고,
    /// 건드리는 순간 NullReferenceException 이 난다. 그래서 매번 새로 찾는다.
    /// </summary>
    private void UIConnect()
    {
        GameState _state = GameFlow.State;

        if (_state == GameState.Field)
        {
            _keyInfo = Reconnect(_keyInfo);
            _characterView = Reconnect(_characterView);
            _fieldMenuView = Reconnect(_fieldMenuView);
            _memberBar = Reconnect(_memberBar);
            _map = Reconnect(_map);
            _systemView = Reconnect(_systemView);

            _keyInfoComponent = _keyInfo;
        }
        else if (_state == GameState.Battle)
        {
            _commandBattleView = Reconnect(_commandBattleView);
        }
    }

    /// <summary>
    /// 참조가 살아 있으면 그대로 쓰고, 죽었으면 씬에서 다시 찾는다.
    /// 비활성 오브젝트도 찾아야 하므로 Include 를 쓴다.
    /// </summary>
    private T Reconnect<T>(T current) where T : Component
    {
        if (current != null) return current;

        T found = FindFirstObjectByType<T>(FindObjectsInactive.Include);

        if (found == null)
            Debug.LogWarning($"[ViewManager] {typeof(T).Name} 를 씬에서 찾지 못했습니다.");

        return found;
    }


    private void UIShow()
    {
        if (GameFlow.IsField)
        {
            FieldUIShow(true);
        }
        else if (GameFlow.IsBattle)
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
        if (_keyInfo == null || _characterView == null)
        {
            Debug.LogWarning("[ViewManager] 필드 UI 참조가 없어 표시를 건너뜁니다.");
            return;
        }

        _keyInfo.gameObject.SetActive(active);
        //_menuButtons.gameObject.SetActive(active);
        //_memberBar.gameObject.SetActive(active);
        _characterView.gameObject.SetActive(false);
    }

    public void CharacterViewShow()
    {
        if (_characterView == null || _keyInfo == null || _memberBar == null || _map == null)
        {
            Debug.LogWarning("[ViewManager] 필드 UI 참조가 없어 캐릭터 창을 열 수 없습니다.");
            return;
        }

        if (_keyInfoComponent != null) _keyInfoComponent.MouseLeftButtonColorReset();

        _characterView.gameObject.SetActive(true);
        //_menuButtons.gameObject.SetActive(false);
        _memberBar.gameObject.SetActive(true);
        _map.gameObject.SetActive(false);
        _keyInfo.gameObject.SetActive(false);

        CharacterViewCharacterInfoSet(GameManager.GameInstance.CharacterComponents[0]);
    }

    public void CharacterViewUnShow()
    {
        if (_characterView == null || _keyInfo == null || _memberBar == null || _map == null) return;

        _characterView.gameObject.SetActive(false);
        _keyInfo.gameObject.SetActive(true);
        //_menuButtons.gameObject.SetActive(true);
        _memberBar.gameObject.SetActive(true);
        _map.gameObject.SetActive(true);
    }


    private void CommandBattleUIShow(bool active)
    {
        if (_commandBattleView == null)
        {
            Debug.LogWarning("[ViewManager] CommandBattleView 를 찾지 못해 표시를 건너뜁니다.");
            return;
        }

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




    // 아래 넘김 메서드들은 전부 null 가드가 있어야 한다.
    // 전투 UI 를 Field 씬으로 옮기기 전에는 "전투 씬에 있으니 참조가 반드시 있다" 는
    // 전제가 성립했지만, 이제 같은 씬에서 필드/전투를 오가므로 참조가 없는 시점이 생긴다.
    // 예전에는 여기서 NullReferenceException 이 났다.

    public void CommandBattleViewShow(bool active)
    {
        if (_commandBattleView == null) return;
        _commandBattleView.gameObject.SetActive(active);
    }

    public void CommandBattleViewActive(ViewCategory _viewCategory, bool active)
    {
        if (_commandBattleView == null) return;
        _commandBattleView.ViewShow(_viewCategory, active);
    }

    public Vector3 CommandAreaPosSet(Vector3 _lookPos)
    {
        if (_commandBattleView == null) return Vector3.zero;
        return _commandBattleView.CommandAreaPosSet(_lookPos);
    }

    public Vector3 MonsterTargetViewPosSet(int index)
    {
        if (_commandBattleView == null) return Vector3.zero;
        return _commandBattleView.TargetViewPosSet(index);
    }

    // ── 필드 ↔ 전투 UI 교체 ──────────────────────────────────
    //
    // 예전에는 씬 로드가 이 일을 대신했다. 씬을 갈지 않으므로
    // 어느 UI 를 켜고 끌지 명시적으로 전환해야 한다.

    /// <summary>전투 UI 를 켜고 필드 UI 를 끈다.</summary>
    public void EnterBattleUI()
    {
        UIConnect();

        if (_keyInfo != null) _keyInfo.gameObject.SetActive(false);
        if (_characterView != null) _characterView.gameObject.SetActive(false);
        if (_map != null) _map.gameObject.SetActive(false);
        if (_fieldMenuView != null) _fieldMenuView.gameObject.SetActive(false);

        IsMenuActive = false;

        if (_commandBattleView != null)
        {
            _commandBattleView.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("[ViewManager] CommandBattleView 가 연결되지 않아 전투 UI 를 켤 수 없습니다. " +
                           "인스펙터의 Command Battle View 칸을 확인하세요.");
        }
    }

    /// <summary>필드 UI 를 켜고 전투 UI 를 끈다.</summary>
    public void ExitBattleUI()
    {
        if (_commandBattleView != null) _commandBattleView.gameObject.SetActive(false);

        UIConnect();

        if (_keyInfo != null) _keyInfo.gameObject.SetActive(true);
        if (_characterView != null) _characterView.gameObject.SetActive(false);

        IsMenuActive = false;
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

    /// <summary>
    /// 전투 UI 참조가 연결돼 있는지. 세팅 검사기가 읽는다.
    /// 참조 자체를 공개하지 않고 유무만 알려주므로 외부에서 뷰를 직접 만질 수는 없다.
    /// </summary>
    public bool HasCommandBattleView => _commandBattleView != null;

    /// <summary>연결된 전투 UI 오브젝트 이름. 없으면 빈 문자열.</summary>
    public string CommandBattleViewName =>
        _commandBattleView != null ? _commandBattleView.gameObject.name : "";

    public bool CharacterViewIsActive()
    {
        if (_characterView == null) return false;

        if (_characterView.gameObject.activeSelf)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void KeyInfoBarUnShow()
    {
        if (_keyInfo != null)
        {
            _keyInfo.gameObject.SetActive(false);
        }
    }

    public void KeyInfoBarShow()
    {
        if (_keyInfo != null)
        {
            _keyInfo.gameObject.SetActive(true);
        }
    }
}
