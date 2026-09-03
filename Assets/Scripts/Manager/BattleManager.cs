using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;


public class BattleManager : MonoBehaviour
{

    enum CommandState
    {
        Select,
        Attack,
        Skill,
        Targeting,
        Instrument,
        Retreat
    };

    [SerializeField] private GameObject _commandBattleMemberPrefab;

    [SerializeField] private Transform _commandActionPos;

    [SerializeField] private Transform _commandBattlePos;

  
    public static BattleManager BattleInstance;

    private CreateCommandActionMemberSystem _createCommandActionMemberSystem;
    private CreateCommandBattleMemberSystem _createCommandBattleMemberSystem;

    private GameObject _monsters;
    private GameObject _Members;
    private CommandState _commandState = CommandState.Select;

    public bool IsBattle { get; set; } = false;

    private List<GameObject> _commandActionMembers;
    private List<GameObject> _commandBattleMembers;

    private List<Unit> Units = new List<Unit>();

    public bool CommandSelect { get; set; } = false;
    public bool CommandAttack { get; set; } = false;
    public bool CommandSkill { get; set; } = false;
    public bool CommandInstrument { get; set; } = false;
    public bool CommandRetreat { get; set; } = false;

    public bool TurnOff { get; set; } = false;

    public bool IsMove { get; set; } = false;
    public bool IsAttack { get; set; } = false;

    private int oriIndex = 0;
    private int nextIndex = 0;
    private int attackIndex = 0;

    private Camera _camera;
    public List<Monster> Monsters = new List<Monster>();
    public List<Character> Characters = new List<Character>();

    public List<GameObject> _battleMemberList = new List<GameObject>();
    public List<Unit> _battleUnit = new List<Unit>();

    private Transform _characterTarget;
    private Transform _monsterTarget;

    private Vector3 _characterScreenPos;
    private Vector3 _monsterScreenPos;

    public float distance = 4f;
    public float sensitivityX = 200f;
    public float sensitivityY = 120f;

    GameObject _cameraTarget;

    float angle = 180f;
    float mouseX;
    float mouseY;
    float height;

    void Awake()
    {
        _createCommandActionMemberSystem = GetComponent<CreateCommandActionMemberSystem>();
        _createCommandBattleMemberSystem = GetComponent<CreateCommandBattleMemberSystem>();

        if (BattleInstance != null)
        {
            Destroy(gameObject);
            return;
        }

        BattleInstance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    void Start()
    {
        _camera = Camera.main;

        for (int i = 0; i < GameManager.GameInstance.Monsters.Count; i++)
        {
            Monsters.Add(GameManager.GameInstance.Monsters[i].GetComponent<Monster>());
        }

        for (int i = 0; i < GameManager.GameInstance.Characters.Count; i++)
        {
            Characters.Add(GameManager.GameInstance.Characters[i].GetComponent<Character>());
        }

        _characterTarget = GameManager.GameInstance.Units[0].gameObject.transform.Find("LookPos");
        _monsterTarget = GameManager.GameInstance.Monsters[0].transform.Find("LookPos");
        Debug.Log($"_characterTarget = {GameManager.GameInstance.Units[0].gameObject.transform.Find("LookPos")}");


        _camera.transform.position = _characterTarget.position + new Vector3(0.0f, 0.0f, -4.0f);
        _characterScreenPos = ViewManager.ViewInstance.CommandAreaPosSet(_characterTarget.position);


    }


    void Update()
    {
        CommandUISet();
        CommandSelectController();
        CameraZoom();
    }



    // 배틀 씬 진입 시, 배틀 유닛 전투 순서 정렬 및 액션 멤버, 배틀 멤버 생성
    public void Battle()
    {
        if (SceneManager.GetActiveScene().name == "CommandBattle")
        {
            // 1. _commandActionPos, _commandBattlePos null일 경우, ActionBar, CommandMemberBar 오브젝트 찾아서 할당
            if (_commandActionPos == null && _commandActionPos == null)
            {
                _commandActionPos = GameObject.Find("ActionBar").transform;
                _commandBattlePos = GameObject.Find("CommandMemberBar").transform;
            }

            Debug.Log($"_commandActionPos  = {_commandActionPos}");
            Debug.Log($"_commandBattlePos  = {_commandBattlePos}");
            // 2. 배틀 유닛 전투 순서 정렬
            _battleUnit = GameManager.GameInstance.Units;

            List<Unit> _sortUnit = _battleUnit.
                OrderByDescending(s => s.Stat.Speed).
                ToList();

            
            // 3. _actionMemberlist null이 아닐 경우, _actionMemberlist 초기화 및 전투 유닛 순서대로 액션 멤버 생성
            List<GameObject> _actionMemberlist = _createCommandActionMemberSystem.ActionMemberlist;
            Debug.Log($"_actionMemberlist.Count  = {_actionMemberlist.Count}");
            if(_actionMemberlist.Count > 0)
            {
                _actionMemberlist.Clear();
            }

            if (_actionMemberlist.Count == 0)
            {
                Debug.Log($" _sortUnit[0]  = {_sortUnit[0]}");
                
                StartCoroutine(_createCommandActionMemberSystem.CreateCommandActionMember(
               _sortUnit, _commandActionPos));
            }

            // 4. _battleMemberList null이 아닐 경우, _battleMemberList 초기화 및 전투 유닛 순서대로 배틀 멤버 생성 
            Debug.Log($"_battleMemberList.Count  = {_battleMemberList.Count}");
            if (_battleMemberList.Count > 0)
            {
                _battleMemberList.Clear();
            }

            if (_battleMemberList.Count == 0)
            {
                _battleMemberList = _createCommandBattleMemberSystem.
                    CreateCommandBattleMember(_commandBattleMemberPrefab, _commandBattlePos);
            }
        }

    }

    // 2. 씬 로드 시, 배틀 씬 진입 시 Battle() 호출
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "CommandBattle")
        {
            Debug.Log("SceneLoaded Battle");

            _camera = Camera.main;

            IsBattle = true;
            Battle();
    
        }    
    }

    // 3. 배틀 씬에서 마우스 입력에 따라 카메라 위치 및 회전 변경
    void CameraZoom()
    {
        if (_camera == null || _characterTarget == null)
        {
            return;
        }

        //_characterTarget = GameManager.GameInstance.Characters[0].transform;
        //_monsterTarget = GameManager.GameInstance.Monsters[nextIndex].transform;
        _camera = Camera.main;

        float CameraXSpeed = 1.0f;
        float CameraYSpeed = 0.2f;

        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        angle += mouseX * CameraXSpeed;

        float rad = angle * Mathf.Deg2Rad;

        float x = Mathf.Sin(rad) * distance;
        float z = Mathf.Cos(rad) * distance;

        if (height <= -0.5f)
        {
            distance -= mouseY * CameraYSpeed;

            if (distance > 5.0f)
            {
                height -= mouseY * CameraYSpeed;
            }
        }
        else
        {
            height -= mouseY * CameraYSpeed;
        }

        if (_commandState == CommandState.Targeting)
        {
            distance = 9.0f;
            height = 9.0f;

            Vector3 offset = new Vector3(x, height, z);
            _camera.transform.position = _monsterTarget.position + offset;
            _camera.transform.LookAt(_monsterTarget.transform);

        }
        else
        {
            distance = Mathf.Clamp(distance, 1.0f, 5.02f);
            height = Mathf.Clamp(height, -0.5f, 5.0f);

            Vector3 offset = new Vector3(x, height, z);
            //Debug.Log($"_camera.transform.position = {_camera.transform.position}");
            //Debug.Log($"_characterTarget.position = {_characterTarget.position}");
            _camera.transform.position = _characterTarget.position + offset;
            _camera.transform.LookAt(_characterTarget.transform);
        }
    }

    // 4. 배틀 씬에서 CommandSelect 상태일 때, 마우스 입력에 따라 CommandAttack, CommandSkill, CommandInstrument, CommandRetreat 상태로 전환
    void CommandSelectController()
    {
        if (_commandState == CommandState.Select)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _commandState = CommandState.Targeting;

            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                _commandState = CommandState.Skill;
            }

            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                _commandState = CommandState.Instrument;
            }

            else if (Input.GetKeyDown(KeyCode.R))
            {
                
            }
        }
        else if (_commandState == CommandState.Targeting)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _commandState = CommandState.Attack;
            }
        }
        else if (_commandState == CommandState.Attack && IsAttack == false)
        {
            IsAttack = true;
            PlayerAttack(GameManager.GameInstance.Units[attackIndex], GameManager.GameInstance.Monsters[nextIndex]);
            
        }

    }

    // 6. 배틀 씬에서 CommandSelect, CommandAttack, CommandSkill, CommandInstrument, CommandRetreat 상태에 따라 UI 활성화 및 비활성화
    void CommandCancel()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            AllCommandCancel();

            Monsters[nextIndex].TargetAreaUnShow();
            _commandState = CommandState.Select;

        }
    }

    // 7. 배틀 씬에서 CommandState 상태에 따라 UI 활성화 및 비활성화
    //  - CommandState : CommandSelect, CommandAttack, CommandSkill, CommandInstrument, CommandRetreat
    void CommandUISet()
    {
        switch(_commandState)
        {
            case CommandState.Select:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, true);
                Monsters[nextIndex].TargetAreaUnShow();
                break;
            case CommandState.Targeting:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);

                TargetMove();
                CommandCancel();
                break;

            case CommandState.Attack:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
                
                break;


            case CommandState.Skill:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, true);
                
                CommandCancel();
                break;

            case CommandState.Instrument:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, true);
                
                CommandCancel();
                break;

            case CommandState.Retreat:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
                break;
        }
    }

    // 8. 배틀 씬에서 CommandAttack 상태일 때, 플레이어 캐릭터가 몬스터를 공격하는 코루틴 실행
    void PlayerAttack(Unit _unit, GameObject _monster)
    {
        Debug.Log("Is Character");
        StartCoroutine(AttackSequence(_unit, _monster));
        
        if(GameManager.GameInstance.Units[attackIndex].GetComponent<Monster>() != null) { 
        }
    }

    void MonsterAttack(Character _character, Monster _monster)
    {
        _character.Stat.Hp -= _monster.Stat.Atk;
    }

    void UnitDeath(Unit _unit)
    {
        if (_unit.Stat.Hp <= 0)
        {
            if (_unit.GetComponent<Character>() != null)
            {
                _unit.GetComponent<Character>().GetComponent<ActionController>().GetComponent<Animator>().SetBool("IsDeath", true);
            }

            Destroy(_unit.gameObject);
        }
    }

    IEnumerator AttackSequence(Unit _character, GameObject _monster)
    {
        Monster _monsterComponent = _monster.GetComponent<Monster>();
        float during = 1.0f;
        float time = 0.0f;

        Vector3 StartPos = _character.transform.position;
        Vector3 endPos = _monster.transform.position - new Vector3(0.0f, 0.0f, 2.5f);

        Vector3 vec = Vector3.zero;
        //Vector3 vec2 = Vector3.zero;

        float _magnitude = 0.0f;
      
        CharacterController _characterController = _character.GetComponent<CharacterController>();
        Animator _animator = _character.GetComponent<Animator>();

        Debug.Log("_characterController : " + _characterController);
        Debug.Log("_animator : " + _animator);

        while (time < during)
        {
            vec = _character.transform.position;
            _magnitude = (endPos - vec).magnitude;

            //Debug.Log($"magnitude = {_magnitude}");

            _animator.SetBool("IsWalk", false);
            _animator.SetBool("IsRun", true);

            time += Time.deltaTime;
            _animator.SetFloat("moveSpeed", 5.0f);

            if (_magnitude > 0.1f)
            {
                _characterController.Move((endPos - StartPos) * Time.deltaTime);
            } else
            {
                break;
            }

            yield return null;
        }

        _animator.SetBool("IsWalk", false);
        _animator.SetBool("IsRun", false);
        _animator.SetFloat("moveSpeed", 0.0f);

        yield return new WaitForSeconds(0.2f);

        _animator.SetTrigger("Attack");

        yield return new WaitForSeconds(1.5f);

        time = 0.0f;
        _magnitude = 0.0f;

        while (time < during)
        {
            vec = _character.transform.position;
            _magnitude = (vec - StartPos).magnitude;

            //Debug.Log($"magnitude = {_magnitude}");

            _animator.SetBool("IsWalk", false);
            _animator.SetBool("IsRun", true);
            _animator.SetFloat("moveSpeed", 5.0f);

            time += Time.deltaTime;
            if (_magnitude > 0.1f)
            {
                _character.transform.LookAt(StartPos);
                _characterController.Move((StartPos - endPos) * Time.deltaTime);
            }
            else
            {
                break;
            }

            yield return null;
           
        }

        yield return new WaitForSeconds(0.1f);

        _character.transform.rotation = Quaternion.identity;

        _animator.SetBool("IsWalk", false);
        _animator.SetBool("IsRun", false);
        _animator.SetFloat("moveSpeed", 0.0f);

        MonsterTakeDmg(_character, _monsterComponent);

        yield return new WaitForSeconds(0.5f);

        _commandState = CommandState.Select;

        IsAttack = false;

        TrunMove();

    }

    void AllCommandCancel()
    {
        CommandSelect = false;
        CommandAttack = false;
        CommandSkill = false;
        CommandInstrument = false;
        CommandRetreat = false;
    }

    void TargetMove()
    {
        int index = Monsters.Count / 2;

        _monsterTarget = Monsters[index].transform;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (IsMove == false)
        {
            IsMove = true;
            if (scroll < 0.0f)
            {
                nextIndex = oriIndex - 1;
                if (nextIndex < 0) { nextIndex = 2; }

            }
            else if (scroll > 0.0f)
            {
                nextIndex = oriIndex + 1;
                if (nextIndex > 2) { nextIndex = 0; }

            }

            Monsters[oriIndex].TargetAreaUnShow();
            oriIndex = nextIndex;

            Monsters[nextIndex].TargetAreaShow();
            ViewManager.ViewInstance.MonsterTargetViewPosSet(nextIndex);
            ViewManager.ViewInstance.MonsterInfoSet(nextIndex);

            if (Input.GetMouseButtonDown(0))
            {
                //Debug.Log(11111);
            }

            IsMove = false;
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void MonsterTakeDmg(Unit _character, Monster _monster) 
    {
        _monster.Stat.Hp -= _character.Stat.Atk;
        //Debug.Log($"Atk = {_character.Stat.Atk}");
        //Debug.Log($"Hp = {_monster.Stat.Hp}");
    }

    void TrunMove()
    {
        TurnOff = true;

        for (int i = 0; i < GameManager.GameInstance.Units.Count; i++)
        {
            //Debug.Log($"Units[{i}] = {GameManager.GameInstance.Units.Count}");
        }

        _createCommandActionMemberSystem.CommandActionMemberAdd(
        GameManager.GameInstance.Units[attackIndex], _commandActionPos);

        attackIndex += 1;
        if(attackIndex > 5)
        {
            attackIndex = 0;
        }

        //Debug.Log($"test1 = {_createCommandActionMemberSystem._characterNames[0]}");
        Debug.Log($"test = {_createCommandActionMemberSystem.ActionMemberlist[0]}");
        _createCommandActionMemberSystem.ActionMemberlist[0].GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 0.8f);
  
        CameraMove(attackIndex);

        
    }

    void CameraMove(int index)
    {
        for (int i = 0; i < GameManager.GameInstance.Units.Count; i++)
        {
            //Debug.Log($"Units_name = {GameManager.GameInstance.Units[index].Stat.Name}");
            _characterTarget = GameManager.GameInstance.Units[index].gameObject.transform.Find("LookPos");

            //if (GameManager.GameInstance.Units[i].Stat.Name ==
            //   _createCommandActionMemberSystem._characterNames[0])
            //{
            //    _characterTarget = GameManager.GameInstance.Units[index].gameObject.transform.Find("LookPos");
            //}
        }

    }

    void SelectBoxMove()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

 
        if (scroll > 0.0f)
        {

        }


    }



}





