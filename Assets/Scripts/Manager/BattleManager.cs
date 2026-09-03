using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class BattleManager : MonoBehaviour
{

    public enum CommandState
    {
        Select,
        Attack,
        Skill,
        Targeting,
        Instrument,
        Retreat
    };

    private const string FieldSceneName = "Field";
    private const string CommandBattleSceneName = "CommandBattle";

    [SerializeField] private GameObject _commandBattleMemberPrefab;

    [SerializeField] private Transform _commandActionPos;

    [SerializeField] private Transform _commandBattlePos;

    // 액션 바. 비워두면 Battle() 에서 _commandActionPos 에서 찾는다
    [SerializeField] private ActionBar _actionBar;


    public static BattleManager BattleInstance;

    private CreateCommandActionMemberSystem _createCommandActionMemberSystem;
    private CreateCommandBattleMemberSystem _createCommandBattleMemberSystem;

    private CommandState _commandState = CommandState.Select;
    private CommandState _lastUISetState = (CommandState)(-1);

    public bool IsBattle { get; set; } = false;

    public bool TurnOff { get; set; } = false;

    public bool IsMove { get; set; } = false;
    public bool IsAttack { get; set; } = false;

    private bool _isBattleOver = false;

    private int oriIndex = 0;
    private int nextIndex = 0;
    private int attackIndex = 0;

    // 스킬 커맨드에서 고른 스킬. null 이면 통상공격(BaseDelay 20)
    private SkillData _pendingSkill;

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

    // 기본 공격 데미지 계산 결과. 스킬/ATB 시스템(설계 완료·미구현)의 SkillResolver 공식과는
    // 별개로, 커맨드 배틀(기본 공격)만을 위한 단순화된 버전이다.
    private struct DamageResult
    {
        public bool Hit;
        public bool Critical;
        public int Amount;
    }

    // 속성 상성표. 스킬 시스템 쪽 ElementChart.csv 가 만들어지기 전까지 쓰는 경량 버전.
    private static readonly Dictionary<(string atk, string def), float> _elementChart =
        new Dictionary<(string, string), float>
    {
        { ("fire", "wind"), 1.5f },
        { ("wind", "water"), 1.5f },
        { ("water", "fire"), 1.5f },
        { ("wind", "fire"), 0.5f },
        { ("water", "wind"), 0.5f },
        { ("fire", "water"), 0.5f },
    };

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
        if (SceneManager.GetActiveScene().name != CommandBattleSceneName)
        {
            return;
        }

        // 1. _commandActionPos, _commandBattlePos null일 경우, ActionBar, CommandMemberBar 오브젝트 찾아서 할당
        //    오브젝트 이름이 바뀌어 못 찾을 경우 예외를 던지는 대신 로그만 남기고 안전하게 리턴한다. (TC 88)
        if (_commandActionPos == null || _commandBattlePos == null)
        {
            GameObject _actionBarObj = GameObject.Find("ActionBar");
            GameObject _memberBarObj = GameObject.Find("CommandMemberBar");

            if (_actionBarObj == null || _memberBarObj == null)
            {
                Debug.LogError("BattleManager.Battle() : ActionBar 또는 CommandMemberBar 오브젝트를 찾을 수 없습니다. 씬의 오브젝트 이름을 확인하세요.");
                return;
            }

            _commandActionPos = _actionBarObj.transform;
            _commandBattlePos = _memberBarObj.transform;
        }

        EnsureActionBar();

        _isBattleOver = false;
        IsAttack = false;
        TurnOff = false;
        attackIndex = 0;
        oriIndex = 0;
        nextIndex = 0;
        _commandState = CommandState.Select;

        // 2. 배틀 유닛 전투 순서 정렬 - 이 정렬 결과를 GameManager.Units 에 그대로 반영해서
        //    액션 바에 보이는 순서와 실제 턴이 도는 순서가 어긋나지 않게 한다. (TC 144)
        List<Unit> _sortUnit = GameManager.GameInstance.Units
            .Where(u => u != null && u.Stat.Hp > 0)
            .OrderByDescending(s => s.Stat.Speed)
            .ToList();

        GameManager.GameInstance.Units = _sortUnit;
        _battleUnit = _sortUnit;

        // 3. 전투 유닛 순서대로 액션 멤버 생성
        //    목록만 Clear() 하면 GameObject 가 남아 전투를 반복할 때마다 쌓인다.
        //    CreateCommandActionMember 가 시작할 때 ClearAll() 로 파괴까지 처리한다.
        _pendingSkill = null;

        StartCoroutine(_createCommandActionMemberSystem.CreateCommandActionMember(
            _sortUnit, _commandActionPos));

        // 4. 전투 유닛 순서대로 배틀 멤버 생성
        if (_battleMemberList.Count > 0)
        {
            _battleMemberList.Clear();
        }

        _battleMemberList = _createCommandBattleMemberSystem.
            CreateCommandBattleMember(_commandBattleMemberPrefab, _commandBattlePos);
    }

    // 2. 씬 로드 시, 배틀 씬 진입 시 Battle() 호출
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == CommandBattleSceneName)
        {
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

        if (_commandState == CommandState.Targeting && _monsterTarget != null)
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
            _camera.transform.position = _characterTarget.position + offset;
            _camera.transform.LookAt(_characterTarget.transform);
        }
    }

    // 4. 배틀 씬에서 CommandSelect 상태일 때, 입력에 따라 상태 전환. 현재 턴 유닛이 몬스터면
    //    입력을 받지 않고 MonsterTurnRoutine 이 자동으로 처리한다. (TC 145)
    void CommandSelectController()
    {
        if (_isBattleOver) return;

        List<Unit> _units = GameManager.GameInstance.Units;
        if (_units == null || _units.Count == 0) return;

        Unit _currentUnit = _units[0];
        if (_currentUnit == null || _currentUnit is Monster) return;

        if (_commandState == CommandState.Select)
        {
            if (Input.GetMouseButtonDown(0))
            {
                EnterTargeting();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                EnterSkill();
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                EnterInstrument();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                Retreat();
            }
        }
        else if (_commandState == CommandState.Targeting)
        {
            if (Input.GetMouseButtonDown(0) && Monsters.Count > 0)
            {
                _commandState = CommandState.Attack;
            }
        }
        else if (_commandState == CommandState.Attack && IsAttack == false)
        {
            if (Monsters.Count == 0)
            {
                _commandState = CommandState.Select;
                return;
            }

            IsAttack = true;
            PlayerAttack(_currentUnit, Monsters[Mathf.Clamp(nextIndex, 0, Monsters.Count - 1)].gameObject);
        }
    }

    // UI 버튼(CommandButtonHover)에서 호출하는 공개 진입점.
    // 예전에는 버튼 클릭이 죽은 bool 플래그만 세팅하고 실제 상태머신(_commandState)은
    // 건드리지 않아서, 버튼을 눌러도 공격/스킬/도구 커맨드가 실제로 동작하지 않았다.
    public void EnterTargeting()
    {
        if (_isBattleOver) return;
        if (_commandState == CommandState.Select)
        {
            _commandState = CommandState.Targeting;
        }
    }

    public void EnterSkill()
    {
        if (_isBattleOver) return;
        _commandState = CommandState.Skill;
    }

    public void EnterInstrument()
    {
        if (_isBattleOver) return;
        _commandState = CommandState.Instrument;
    }

    // 후퇴 커맨드. 예전에는 R 입력을 받아도 아무 동작이 없었다. (TC 99)
    public void Retreat()
    {
        if (_isBattleOver) return;

        _isBattleOver = true;
        _commandState = CommandState.Retreat;
        StartCoroutine(RetreatRoutine());
    }

    private IEnumerator RetreatRoutine()
    {
        IsBattle = false;
        Debug.Log("전투에서 후퇴했습니다.");

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(FieldSceneName);
    }

    // 6. ESC / 우클릭으로 이전 상태(Select)로 복귀
    void CommandCancel()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            if (Monsters.Count > 0)
            {
                Monsters[Mathf.Clamp(nextIndex, 0, Monsters.Count - 1)].TargetAreaUnShow();
            }

            _commandState = CommandState.Select;
        }
    }

    // 7. CommandState 상태에 따라 UI 활성화/비활성화.
    //    상태가 바뀔 때만 SetActive 를 호출하고, 매 프레임 입력이 필요한 상태만 별도로 갱신한다.
    void CommandUISet()
    {
        if (_commandState != _lastUISetState)
        {
            ApplyCommandStateViews(_commandState);
            _lastUISetState = _commandState;
        }

        switch (_commandState)
        {
            case CommandState.Select:
                if (Monsters.Count > 0)
                {
                    Monsters[Mathf.Clamp(nextIndex, 0, Monsters.Count - 1)].TargetAreaUnShow();
                }
                break;

            case CommandState.Targeting:
                TargetMove();
                CommandCancel();
                break;

            case CommandState.Skill:
            case CommandState.Instrument:
                CommandCancel();
                break;
        }
    }

    private void ApplyCommandStateViews(CommandState state)
    {
        UpdateDelayPreview(state);

        switch (state)
        {
            case CommandState.Select:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, true);
                break;

            case CommandState.Targeting:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
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
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
                break;

            case CommandState.Instrument:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, true);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
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
        StartCoroutine(AttackSequence(_unit, _monster));
    }

    // 기본 공격 판정: 명중/회피 → 방어력 반영 데미지 → 속성 상성 → 크리티컬 순으로 계산한다.
    // (TC 128 방어력, 130 크리티컬, 131 명중/회피, 132 속성 상성)
    private DamageResult ResolveAttack(Stat _attacker, Stat _defender)
    {
        DamageResult result = new DamageResult();

        float hitChance = Mathf.Clamp(_attacker.Hit - _defender.Avoid, 0.05f, 1f);
        if (UnityEngine.Random.value > hitChance)
        {
            result.Hit = false;
            result.Amount = 0;
            return result;
        }

        result.Hit = true;

        float baseDamage = Mathf.Max(1, _attacker.Atk - _defender.Def);

        if (_elementChart.TryGetValue((_attacker.Element, _defender.Element), out float chartMult))
        {
            baseDamage *= chartMult;
        }

        bool isCritical = UnityEngine.Random.value < Mathf.Clamp01(_attacker.Critical);
        if (isCritical)
        {
            float critMult = _attacker.CriticalDmg > 0f ? _attacker.CriticalDmg : 1.5f;
            baseDamage *= critMult;
        }
        result.Critical = isCritical;

        result.Amount = Mathf.Max(1, Mathf.RoundToInt(baseDamage));
        return result;
    }

    // 몬스터의 자동 반격/선공. 현재 턴 유닛이 몬스터일 때 CommandSelectController 대신 호출된다. (TC 136, 145)
    private IEnumerator MonsterTurnRoutine(Monster _monster)
    {
        if (_isBattleOver || _monster == null) yield break;

        yield return new WaitForSeconds(0.8f);

        List<Character> _livingCharacters = Characters.Where(c => c != null && c.Stat.Hp > 0).ToList();
        if (_livingCharacters.Count == 0)
        {
            CheckBattleEnd();
            yield break;
        }

        Character _target = _livingCharacters[UnityEngine.Random.Range(0, _livingCharacters.Count)];

        MonsterAttack(_target, _monster);

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleOver)
        {
            TrunMove(_monster);
        }
    }

    void MonsterAttack(Character _character, Monster _monster)
    {
        if (_character == null || _monster == null) return;

        DamageResult result = ResolveAttack(_monster.Stat, _character.Stat);

        if (result.Hit)
        {
            _character.Stat.Hp = Mathf.Max(0, _character.Stat.Hp - result.Amount);
            Debug.Log($"{_monster.Stat.Name} → {_character.Stat.Name} 데미지 {result.Amount}{(result.Critical ? " (Critical)" : "")}");
        }
        else
        {
            Debug.Log($"{_monster.Stat.Name}의 공격이 빗나갔습니다.");
        }

        UnitDeath(_character);
    }

    // 사망 처리: Hp가 0 이하가 된 유닛을 전투 대열/턴 순서에서 제거하고, 승패를 확인한다. (TC 135, 146)
    void UnitDeath(Unit _unit)
    {
        if (_unit == null || _unit.Stat.Hp > 0)
        {
            return;
        }

        if (_unit is Character _character)
        {
            Animator _animator = _character.GetComponent<ActionController>()?.GetComponent<Animator>();
            if (_animator != null)
            {
                _animator.SetBool("IsDeath", true);
            }

            Characters.Remove(_character);
            GameManager.GameInstance.Characters.Remove(_character.gameObject);
            GameManager.GameInstance.CharacterComponents.Remove(_character);
        }
        else if (_unit is Monster _monster)
        {
            Monsters.Remove(_monster);
            GameManager.GameInstance.Monsters.Remove(_monster.gameObject);
            GameManager.GameInstance.MonsterComponents.Remove(_monster);

            if (Monsters.Count > 0)
            {
                oriIndex = Mathf.Clamp(oriIndex, 0, Monsters.Count - 1);
                nextIndex = Mathf.Clamp(nextIndex, 0, Monsters.Count - 1);
            }
            else
            {
                oriIndex = 0;
                nextIndex = 0;
            }
        }

        GameManager.GameInstance.Units.Remove(_unit);

        StartCoroutine(DelayedDestroy(_unit.gameObject, 1.0f));

        CheckBattleEnd();
    }

    private IEnumerator DelayedDestroy(GameObject _target, float _delay)
    {
        if (_target == null) yield break;
        yield return new WaitForSeconds(_delay);
        if (_target != null)
        {
            Destroy(_target);
        }
    }

    // 승패 판정: 몬스터 전멸 시 승리, 캐릭터 전멸 시 패배. (TC 137, 138)
    private void CheckBattleEnd()
    {
        if (_isBattleOver) return;

        if (Monsters.Count == 0)
        {
            _isBattleOver = true;
            StartCoroutine(BattleResult(true));
        }
        else if (Characters.Count == 0)
        {
            _isBattleOver = true;
            StartCoroutine(BattleResult(false));
        }
    }

    private IEnumerator BattleResult(bool victory)
    {
        IsBattle = false;
        _commandState = CommandState.Select;

        Debug.Log(victory ? "전투 승리" : "전투 패배");

        if (!victory)
        {
            // 파티가 전멸했을 때: 경험치/보상 지급 없이 최소 Hp로 되살려 필드로 복귀시킨다.
            // 정식 게임 오버 연출 및 보상 지급은 스킬/ATB 시스템과 함께 별도 구현 예정. (TC 139, 140)
            foreach (Stat _stat in StatusManager.StatusInstance.AllCharacterStats)
            {
                _stat.Hp = Mathf.Max(1, Mathf.RoundToInt(_stat.MaxHp * 0.3f));
            }
        }

        yield return new WaitForSeconds(1.5f);

        SceneManager.LoadScene(FieldSceneName);
    }

    IEnumerator AttackSequence(Unit _character, GameObject _monster)
    {
        if (_character == null || _monster == null) yield break;

        Monster _monsterComponent = _monster.GetComponent<Monster>();

        Vector3 StartPos = _character.transform.position;

        CharacterController _characterController = _character.GetComponent<CharacterController>();
        Animator _animator = _character.GetComponent<Animator>();

        const float moveSpeedUnits = 5.0f;
        const float arriveThreshold = 0.1f;
        const float maxSeconds = 3.0f; // 안전장치: 무슨 일이 있어도 이 시간 안에는 다음 단계로 넘어간다.

        // 대상 앞까지 이동 (거리에 관계없이 항상 목표 지점에 정확히 도달하도록 정규화된 방향으로 이동) (TC 118, 120)
        float elapsed = 0f;
        while (elapsed < maxSeconds)
        {
            if (_monster == null) yield break; // 연출 중 대상 파괴 (TC 126)

            Vector3 endPos = _monster.transform.position - new Vector3(0.0f, 0.0f, 2.5f);
            Vector3 toTarget = endPos - _character.transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= arriveThreshold)
            {
                break;
            }

            _animator.SetBool("IsWalk", false);
            _animator.SetBool("IsRun", true);
            _animator.SetFloat("moveSpeed", moveSpeedUnits);

            Vector3 dir = toTarget.normalized;
            _characterController.Move(dir * moveSpeedUnits * Time.deltaTime);
            _character.transform.rotation = Quaternion.LookRotation(dir);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _animator.SetBool("IsWalk", false);
        _animator.SetBool("IsRun", false);
        _animator.SetFloat("moveSpeed", 0.0f);

        yield return new WaitForSeconds(0.2f);

        if (_monster == null) yield break;

        _animator.SetTrigger("Attack");

        yield return new WaitForSeconds(1.5f);

        // 원위치로 복귀
        elapsed = 0f;
        while (elapsed < maxSeconds)
        {
            Vector3 toStart = StartPos - _character.transform.position;
            toStart.y = 0f;

            if (toStart.magnitude <= arriveThreshold)
            {
                break;
            }

            _animator.SetBool("IsWalk", false);
            _animator.SetBool("IsRun", true);
            _animator.SetFloat("moveSpeed", moveSpeedUnits);

            Vector3 dir = toStart.normalized;
            _character.transform.LookAt(_character.transform.position + dir);
            _characterController.Move(dir * moveSpeedUnits * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        _character.transform.rotation = Quaternion.identity;

        _animator.SetBool("IsWalk", false);
        _animator.SetBool("IsRun", false);
        _animator.SetFloat("moveSpeed", 0.0f);

        if (_monsterComponent != null)
        {
            MonsterTakeDmg(_character, _monsterComponent);
        }

        yield return new WaitForSeconds(0.5f);

        _commandState = CommandState.Select;
        IsAttack = false;

        if (!_isBattleOver)
        {
            TrunMove(_character);
        }
    }

    void TargetMove()
    {
        if (Monsters.Count == 0) return;

        oriIndex = Mathf.Clamp(oriIndex, 0, Monsters.Count - 1);
        nextIndex = Mathf.Clamp(nextIndex, 0, Monsters.Count - 1);

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (IsMove == false)
        {
            IsMove = true;

            if (scroll < 0.0f)
            {
                nextIndex = oriIndex - 1;
                if (nextIndex < 0) { nextIndex = Monsters.Count - 1; }
            }
            else if (scroll > 0.0f)
            {
                nextIndex = oriIndex + 1;
                if (nextIndex > Monsters.Count - 1) { nextIndex = 0; }
            }

            Monsters[oriIndex].TargetAreaUnShow();
            oriIndex = nextIndex;

            Monsters[nextIndex].TargetAreaShow();
            ViewManager.ViewInstance.MonsterTargetViewPosSet(nextIndex);
            ViewManager.ViewInstance.MonsterInfoSet(nextIndex);

            IsMove = false;
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void MonsterTakeDmg(Unit _attacker, Monster _monster)
    {
        if (_attacker == null || _monster == null) return;

        DamageResult result = ResolveAttack(_attacker.Stat, _monster.Stat);

        if (result.Hit)
        {
            _monster.Stat.Hp = Mathf.Max(0, _monster.Stat.Hp - result.Amount);
            Debug.Log($"{_attacker.Stat.Name} → {_monster.Stat.Name} 데미지 {result.Amount}{(result.Critical ? " (Critical)" : "")}");
        }
        else
        {
            Debug.Log($"{_attacker.Stat.Name}의 공격이 빗나갔습니다.");
        }

        UnitDeath(_monster);
    }

    // 턴 종료 처리: 방금 행동한 유닛(_actedUnit)을 큐 맨 뒤로 보내고, 나머지 생존 유닛을
    // Speed 기준으로 다시 정렬한다. 다음 턴 유닛이 몬스터면 자동으로 MonsterTurnRoutine 을
    // 실행한다. (TC 144, 145, 146, 147, 148)
    void TrunMove(Unit _actedUnit)
    {
        if (_isBattleOver) return;

        List<Unit> _living = GameManager.GameInstance.Units.Where(u => u != null && u.Stat.Hp > 0).ToList();

        if (_living.Count == 0)
        {
            CheckBattleEnd();
            return;
        }

        List<Unit> _sorted = _living
            .Where(u => u != _actedUnit)
            .OrderByDescending(u => u.Stat.Speed)
            .ToList();

        if (_living.Contains(_actedUnit))
        {
            _sorted.Add(_actedUnit);
        }

        GameManager.GameInstance.Units = _sorted;
        _battleUnit = _sorted;

        // 액션 바 갱신. 선두 슬롯(방금 행동한 캐릭터)은 여기서 사라지고,
        // 커맨드 선택 때 미리 만들어 둔 예상 슬롯이 실제 슬롯으로 승격된다.
        if (_actionBar != null)
        {
            _actionBar.CommitTurn(_living.Contains(_actedUnit) ? _actedUnit : null);
        }
        else
        {
            TurnOff = true;
            _createCommandActionMemberSystem.CommandActionMemberAdd(
                _sorted[_sorted.Count - 1], _commandActionPos);
            TurnOff = false;
        }

        attackIndex = 0;

        CameraMove(attackIndex);

        Unit _nextUnit = _sorted[attackIndex];

        if (_nextUnit is Monster _monster)
        {
            StartCoroutine(MonsterTurnRoutine(_monster));
        }
    }

    // ── 딜레이 프리뷰 ───────────────────────────────────────

    /// <summary>
    /// 타깃 지정·스킬 선택 중에는 이번 행동으로 붙는 딜레이를 액션 바에 띄운다.
    ///     Final Delay = floor(100 * BaseDelay / SPD)
    /// 상태가 바뀔 때만 호출되므로 매 프레임 갱신되지 않는다.
    /// </summary>
    /// <summary>
    /// 액션 바 컴포넌트를 확보한다. 씬에 붙어 있지 않으면 직접 붙인다.
    /// 예전에는 null 이면 조용히 리턴해서 딜레이 표시가 안 되는 이유를 알 수 없었다.
    /// </summary>
    private void EnsureActionBar()
    {
        if (_actionBar != null)
        {
            _actionBar.Bind(_commandActionPos, _createCommandActionMemberSystem);
            return;
        }

        if (_commandActionPos == null)
        {
            Debug.LogError("BattleManager : _commandActionPos 가 없어 ActionBar 를 확보할 수 없습니다.");
            return;
        }

        _actionBar = _commandActionPos.GetComponent<ActionBar>();

        if (_actionBar == null)
        {
            _actionBar = _commandActionPos.GetComponentInChildren<ActionBar>(true);
        }

        if (_actionBar == null)
        {
            _actionBar = _commandActionPos.gameObject.AddComponent<ActionBar>();
            Debug.Log($"BattleManager : '{_commandActionPos.name}' 오브젝트에 ActionBar 컴포넌트가 없어 " +
                      "런타임에 추가했습니다. 씬에 미리 붙여두면 인스펙터에서 값을 조절할 수 있습니다.");
        }
        else
        {
            Debug.Log($"BattleManager : ActionBar 확보 — {_actionBar.gameObject.name}");
        }

        _actionBar.Bind(_commandActionPos, _createCommandActionMemberSystem);
    }

    private void UpdateDelayPreview(CommandState state)
    {
        if (_actionBar == null) EnsureActionBar();
        if (_actionBar == null) return;

        switch (state)
        {
            case CommandState.Targeting:
            case CommandState.Skill:
                Unit _actor = CurrentActor();
                int _baseDelay = PendingBaseDelay();

                Debug.Log($"BattleManager : 딜레이 표시 요청 — state={state}, " +
                          $"actor={(_actor != null ? _actor.Stat.Name : "null")}, baseDelay={_baseDelay}");

                _actionBar.ShowDelayPreview(_actor, _baseDelay);
                break;

            case CommandState.Attack:
                // 공격 연출 중에는 배지만 감춘다. 예상 슬롯은 다음 턴에
                // 그대로 실제 슬롯이 되어야 하므로 남겨둔다
                _actionBar.LockPreview();
                break;

            default:
                // 취소해서 커맨드 선택으로 돌아온 경우 예상 슬롯을 없앤다
                _actionBar.ClearPreview();
                break;
        }
    }

    /// <summary>지금 차례인 유닛. 정렬된 목록의 선두다.</summary>
    private Unit CurrentActor()
    {
        List<Unit> _units = GameManager.GameInstance.Units;
        if (_units == null || _units.Count == 0) return null;

        return _units[0];
    }

    /// <summary>이번 행동의 BaseDelay. 통상공격 20, 스킬은 SkillData.csv 값.</summary>
    private int PendingBaseDelay()
    {
        return _pendingSkill != null ? _pendingSkill.BaseDelay : AT.BaseAttack;
    }

    /// <summary>스킬 선택 UI 가 고른 스킬을 넘긴다. null 이면 통상공격으로 되돌린다.</summary>
    public void SetPendingSkill(SkillData skill)
    {
        _pendingSkill = skill;

        if (_actionBar != null && (_commandState == CommandState.Targeting ||
                                   _commandState == CommandState.Skill))
        {
            _actionBar.ShowDelayPreview(CurrentActor(), PendingBaseDelay());
        }
    }

    void CameraMove(int index)
    {
        List<Unit> _units = GameManager.GameInstance.Units;
        if (_units == null || index < 0 || index >= _units.Count) return;

        Transform _lookPos = _units[index].gameObject.transform.Find("LookPos");
        if (_lookPos != null)
        {
            _characterTarget = _lookPos;
        }
    }

}
