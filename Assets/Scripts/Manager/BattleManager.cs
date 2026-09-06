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
        Retreat,

        // 몬스터가 행동하는 동안. 플레이어 입력을 받지 않고 커맨드 UI 를 감춘다
        MonsterTurn
    };

    // 씬 이름은 GameFlow 가 단일 출처다. 여기서 다시 선언하면 두 곳이 어긋날 수 있다.
    private const string FieldSceneName = GameFlow.FieldSceneName;
    private const string CommandBattleSceneName = GameFlow.BattleSceneName;

    [SerializeField] private GameObject _commandBattleMemberPrefab;

    [SerializeField] private Transform _commandActionPos;

    [SerializeField] private Transform _commandBattlePos;

    // 액션 바. 비워두면 Battle() 에서 _commandActionPos 에서 찾는다
    [SerializeField] private ActionBar _actionBar;

    // AT 큐. 없으면 Battle() 에서 직접 붙인다
    [SerializeField] private BattleFlow _flow;

    [Header("인플레이스 전환")]
    [Tooltip("조우 지점에 전투 대열을 세우는 컴포넌트. 비어 있으면 씬에서 찾는다")]
    [SerializeField] private BattleStage _stage;
    [Tooltip("섬광이 차오르는 시간(초). 이 뒤에 재배치가 일어난다")]
    [SerializeField] private float _flashIn = 0.12f;
    [Tooltip("완전히 하얀 상태를 유지하는 시간(초)")]
    [SerializeField] private float _flashHold = 0.06f;
    [Tooltip("섬광이 걷히는 시간(초)")]
    [SerializeField] private float _flashOut = 0.35f;
    [SerializeField] private Color _flashColor = Color.white;

    [Header("공격 이동 회피")]
    [Tooltip("이동 경로에 다른 유닛이 있으면 옆으로 비켜 간다. 끄면 직선으로 밀고 들어간다")]
    [SerializeField] private bool _avoidUnitsWhileMoving = true;
    [Tooltip("전방 몇 m 앞의 유닛까지 고려할지")]
    [SerializeField] private float _avoidLookAhead = BattleSteering.DefaultLookAhead;
    [Tooltip("스쳐 지날 때 두 반지름 합에 더할 여유(m)")]
    [SerializeField] private float _avoidClearance = BattleSteering.DefaultClearance;

    /// <summary>필드 ↔ 전투 전환 연출 중인지. 이 동안에는 입력과 전투 로직을 멈춘다.</summary>
    public bool IsTransitioning { get; private set; }

    // 회피 계산에 넘길 장애물 목록. 매 프레임 새로 할당하지 않도록 재사용한다
    private BattleSteering.Obstacle[] _obstacleBuffer = new BattleSteering.Obstacle[16];
    private int _obstacleCount;

    // ── 세팅 검사 ────────────────────────────────────────────────
    //
    // 전투 UI 와 BattleManager 를 CommandBattle 씬에서 Field 씬으로 옮기고 나면
    // 인스펙터 참조가 살아남았는지 확인해야 한다. Play 를 눌러 예외를 만나기 전에
    // 컴포넌트 톱니바퀴 메뉴 → [전투 세팅 검사] 로 즉시 확인할 수 있다.

    [ContextMenu("전투 세팅 검사")]
    public void ValidateBattleSetup()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int bad = 0;

        sb.AppendLine("=== 전투 세팅 검사 ===");
        sb.AppendLine($"이 BattleManager 가 있는 씬 : {gameObject.scene.name}");

        bad += Line(sb, "CreateCommandActionMemberSystem 컴포넌트",
                    GetComponent<CreateCommandActionMemberSystem>() != null,
                    "같은 오브젝트에 붙어 있어야 합니다. 옮길 때 컴포넌트가 빠졌습니다.");

        bad += Line(sb, "CreateCommandBattleMemberSystem 컴포넌트",
                    GetComponent<CreateCommandBattleMemberSystem>() != null,
                    "같은 오브젝트에 붙어 있어야 합니다.");

        bad += Line(sb, "_commandActionPos (ActionMemberPos)",
                    _commandActionPos != null,
                    "ActionBar/Bar/ActionMemberPos 를 다시 끌어다 넣으세요.",
                    _commandActionPos != null ? _commandActionPos.name : "");

        bad += Line(sb, "_commandBattlePos (CommandMemberBar)",
                    _commandBattlePos != null,
                    "CommandBattleView/CommandMemberBar 를 다시 끌어다 넣으세요.",
                    _commandBattlePos != null ? _commandBattlePos.name : "");

        bad += Line(sb, "_actionBar",
                    _actionBar != null,
                    "CommandBattleView/ActionBar 의 ActionBar 컴포넌트를 넣으세요.",
                    _actionBar != null ? _actionBar.gameObject.name : "");

        bad += Line(sb, "_commandBattleMemberPrefab",
                    _commandBattleMemberPrefab != null,
                    "프리팹 에셋이므로 씬을 옮겨도 유지됩니다. 비어 있다면 원래부터 비어 있었습니다.",
                    _commandBattleMemberPrefab != null ? _commandBattleMemberPrefab.name : "");

        // 씬 안에 전투 UI 가 실제로 있는지 (비활성 오브젝트도 포함해 찾는다)
        CommandBattleView _view = FindFirstObjectByType<CommandBattleView>(FindObjectsInactive.Include);
        bad += Line(sb, "씬에 CommandBattleView 존재",
                    _view != null,
                    "전투 UI 를 Field 씬의 Canvas 아래로 옮기지 않았습니다.",
                    _view != null ? _view.gameObject.scene.name + " 씬" : "");

        ViewManager _vm = FindFirstObjectByType<ViewManager>(FindObjectsInactive.Include);
        if (_vm == null)
        {
            bad += Line(sb, "씬에 ViewManager 존재", false, "Field 씬에 ViewManager 가 있어야 합니다.");
        }
        else
        {
            bad += Line(sb, "ViewManager._commandBattleView 연결",
                        _vm.HasCommandBattleView,
                        "ViewManager 인스펙터의 Command Battle View 칸에 옮긴 CommandBattleView 를 넣으세요. " +
                        "비어 있어도 실행 중 Reconnect 로 찾지만, 명시하는 편이 안전합니다.",
                        _vm.CommandBattleViewName);
        }

        // 중복 매니저는 Awake 에서 한쪽이 Destroy 되지만, 어느 쪽이 살지는 순서에 달려 있다
        BattleManager[] _managers = FindObjectsByType<BattleManager>(FindObjectsInactive.Include,
                                                                     FindObjectsSortMode.None);
        bad += Line(sb, "BattleManager 중복 없음",
                    _managers.Length == 1,
                    $"씬에 {_managers.Length} 개 있습니다. CommandBattle 씬의 것을 지우거나 " +
                    "Field 씬의 것 하나만 남기세요.",
                    _managers.Length + "개");

        // BattleManager 는 참조가 비었을 때 이름으로 찾는 폴백이 있다. 그 이름이 유효한지도 본다
        bool _findable = GameObject.Find("ActionBar") != null;
        sb.AppendLine(_findable
            ? "[참고] GameObject.Find(\"ActionBar\") 폴백도 동작합니다."
            : "[참고] GameObject.Find(\"ActionBar\") 폴백은 동작하지 않습니다 " +
              "(비활성 상태이거나 이름이 다름). 인스펙터 참조가 반드시 있어야 합니다.");

        sb.AppendLine(bad == 0
            ? "=== 문제 없음. 전투 세팅이 이 씬에서 완결됩니다. ==="
            : $"=== 문제 {bad}건. 위 [문제] 항목을 고치세요. ===");

        if (bad == 0) Debug.Log(sb.ToString());
        else Debug.LogWarning(sb.ToString());
    }

    /// <summary>검사 한 줄. 문제면 1 을 돌려준다.</summary>
    private int Line(System.Text.StringBuilder sb, string label, bool ok, string how, string detail = "")
    {
        if (ok)
        {
            sb.AppendLine($"[정상] {label}" + (string.IsNullOrEmpty(detail) ? "" : $" — {detail}"));
            return 0;
        }

        sb.AppendLine($"[문제] {label}");
        sb.AppendLine($"        조치: {how}");
        return 1;
    }

    // 이번 행동에 쓰인 BaseDelay. 턴이 끝날 때 큐에 넣는 값이다
    private int _lastActionBaseDelay = AT.BaseAttack;

    [Header("애니메이션 상태 이름")]
    [Tooltip("Animator 에 IsRun 같은 파라미터가 없을 때 이 상태를 직접 재생한다")]
    [SerializeField] private string _runStateName = "RunForward";
    [Tooltip("Animator 에 Attack 트리거가 없을 때 이 상태를 직접 재생한다")]
    [SerializeField] private string _attackStateName = "Attack1";
    [Tooltip("공격 동작이 시작된 뒤 실제로 맞는 시점까지의 시간(초). 이때 데미지와 문자가 나온다")]
    [SerializeField] private float _attackImpactDelay = 0.4f;
    [Tooltip("공격 동작 전체 길이(초). 이 시간이 지나면 원위치로 복귀한다")]
    [SerializeField] private float _attackAnimSeconds = 1.5f;

    [Header("명중 판정")]
    [Tooltip("3단 판정의 DEX 대 AGL 계수. 명중률 = 1 - AGL / (계수 x DEX). 낮추면 회피가 자주 나온다")]
    [SerializeField] private float _dexAglCoefficient = 5f;

    [Header("전투 문자(데미지/회피) UI")]
    [Tooltip("피격 지점 위에 문자가 떠 있는 시간(초)")]
    [SerializeField] private float _combatTextDuration = 1.0f;
    [Tooltip("표시되는 동안 위로 떠오르는 거리(픽셀)")]
    [SerializeField] private float _combatTextRisePx = 60f;
    [Tooltip("폰트 크기(픽셀)")]
    [SerializeField] private float _combatTextFontSizePx = 40f;
    [Tooltip("통상 데미지 색")]
    [SerializeField] private Color _damageColor = new Color(1f, 0.95f, 0.85f, 1f);
    [Tooltip("크리티컬 데미지 색")]
    [SerializeField] private Color _criticalColor = new Color(1f, 0.72f, 0.2f, 1f);
    [Tooltip("회피(AVOID) 색")]
    [SerializeField] private Color _avoidColor = new Color(0.7f, 0.85f, 1f, 1f);
    [Tooltip("켜면 전투 문자가 사라지지 않고 남는다. Hierarchy 에서 확인할 때만 쓴다")]
    [SerializeField] private bool _combatTextDebugHold = false;
    [Tooltip("켜면 문자 뒤에 반투명 빨간 상자를 깔아 캔버스가 그려지는지 확인한다")]
    [SerializeField] private bool _combatTextDebugBackground = false;


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
    // 속성 상성은 쓰지 않는다.
    private struct DamageResult
    {
        public bool Hit;
        public bool Critical;
        public int Amount;

        // 빗나감(Miss)과 회피(Evade)를 구분해 UI 문자를 다르게 띄운다
        public HitResult Judge;
    }

    void Awake()
    {
        SkillResolver.DexAglCoefficient = _dexAglCoefficient;

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

        // 전투 대열 수집과 카메라 배치는 여기서 하지 않는다.
        //
        // 예전에는 Start() 에서 GameManager.Units[0] 을 바로 읽었다. 이 오브젝트가
        // 전투 씬에만 있어서 "Start 가 도는 시점 = 전투 시작" 이 성립했기 때문이다.
        // Field 씬으로 옮기면 필드에서도 Start 가 돌고, 그 시점의 Units 는 비어 있어
        // IndexOutOfRange 가 난다. 게다가 필드 카메라를 전투 위치로 끌어당겨 버린다.
        //
        // 어차피 Battle() 이 대열을 다시 수집하므로(두 번째 전투 대비),
        // 카메라·주시 대상 설정만 InitBattleView() 로 떼어냈다.
        //
        // 전투 상태로 시작한 경우엔 여기서도 한 번 부른다. Battle() 은 코루틴이
        // 한 프레임 미룬 뒤에 돌기 때문에, 이걸 빼면 첫 프레임이 전투 카메라가
        // 잡히기 전의 각도로 한 번 그려진다.
        if (GameFlow.IsBattle) InitBattleView();
    }


    void Update()
    {
        // 필드에서는 전투 입력도 전투 UI 갱신도 하지 않는다.
        // Field 씬으로 옮긴 뒤에는 이 컴포넌트가 필드에서도 계속 살아 있다.
        //
        // 전환 연출 중에도 멈춘다. 대열이 아직 없는 상태에서 CommandUISet 이 돌면
        // 전투 UI 를 건드리다 참조가 비어 예외가 난다.
        if (!GameFlow.IsBattle || IsTransitioning) return;

        CommandUISet();
        CommandSelectController();
        CameraZoom();

        // 카메라가 움직인 뒤에 투영해야 창이 한 프레임 늦게 따라오지 않는다
        UpdateCommandAreaPosition();
    }



    // 배틀 씬 진입 시, 배틀 유닛 전투 순서 정렬 및 액션 멤버, 배틀 멤버 생성
    public void Battle()
    {
        if (!GameFlow.IsBattle)
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

        EnsureBattleSystems();
        EnsureActionBar();

        _isBattleOver = false;
        IsAttack = false;
        TurnOff = false;
        attackIndex = 0;
        oriIndex = 0;
        nextIndex = 0;
        _commandState = CommandState.Select;
        _lastUISetState = (CommandState)(-1);

        // 전투 대열 재수집. Start() 에서만 채우면 두 번째 전투부터
        // 파괴된 이전 씬의 오브젝트를 들고 있게 된다.
        Characters.Clear();
        Monsters.Clear();

        for (int i = 0; i < GameManager.GameInstance.Characters.Count; i++)
        {
            GameObject _obj = GameManager.GameInstance.Characters[i];
            if (_obj == null) continue;

            Character _c = _obj.GetComponent<Character>();
            if (_c != null) Characters.Add(_c);
        }

        for (int i = 0; i < GameManager.GameInstance.Monsters.Count; i++)
        {
            GameObject _obj = GameManager.GameInstance.Monsters[i];
            if (_obj == null) continue;

            Monster _m = _obj.GetComponent<Monster>();
            if (_m != null) Monsters.Add(_m);
        }

        // 2. 배틀 유닛 전투 순서 정렬 - 이 정렬 결과를 GameManager.Units 에 그대로 반영해서
        //    액션 바에 보이는 순서와 실제 턴이 도는 순서가 어긋나지 않게 한다. (TC 144)
        List<Unit> _sortUnit = GameManager.GameInstance.Units
            .Where(u => u != null && u.Stat.Hp > 0)
            .OrderByDescending(s => s.Stat.Speed)
            .ToList();

        GameManager.GameInstance.Units = _sortUnit;
        _battleUnit = _sortUnit;

        // 3. AT 큐 시작.
        //    초기 AT = floor(100 * 20 / SPD) 이므로 Speed 가 빠른 쪽이 먼저 온다.
        //    이후 매 행동마다 딜레이가 더해지고 큐가 재정렬되므로, Speed 는
        //    전투 시작 시 순서뿐 아니라 모든 턴의 간격에 계속 작용한다.
        _pendingSkill = null;
        _lastActionBaseDelay = AT.BaseAttack;

        if (_flow != null)
        {
            _createCommandActionMemberSystem.ClearAll();
            _flow.Begin(Characters, Monsters);
            SyncUnitsFromQueue();
        }
        else
        {
            StartCoroutine(_createCommandActionMemberSystem.CreateCommandActionMember(
                _sortUnit, _commandActionPos));
        }

        // 4. 전투 유닛 순서대로 배틀 멤버 생성
        if (_battleMemberList.Count > 0)
        {
            _battleMemberList.Clear();
        }

        _battleMemberList = _createCommandBattleMemberSystem.
            CreateCommandBattleMember(_commandBattleMemberPrefab, _commandBattlePos);

        // 5. 카메라와 주시 대상. 대열이 확정된 뒤에 잡아야 한다.
        InitBattleView();

        // 6. 첫 턴 시작. 선두가 몬스터면 자동으로 행동한다.
        StartTurn();
    }

    /// <summary>
    /// 전투 카메라와 주시 대상 설정. 예전에 Start() 가 하던 일이다.
    /// 대열(Units / Monsters)이 채워진 뒤에 불러야 한다.
    /// 하나라도 없으면 경고만 남기고 조용히 빠진다 — 여기서 예외가 나면
    /// 전투가 시작되지 않은 채 UI 만 켜진 상태로 남는다.
    /// </summary>
    private void InitBattleView()
    {
        if (_camera == null) _camera = Camera.main;

        List<Unit> _units = GameManager.GameInstance != null
            ? GameManager.GameInstance.Units
            : null;

        if (_units == null || _units.Count == 0 || _units[0] == null)
        {
            Debug.LogWarning("[BattleManager] 전투 대열이 비어 있어 카메라 대상을 잡지 못했습니다.");
            return;
        }

        _characterTarget = _units[0].gameObject.transform.Find("LookPos");

        if (Monsters.Count > 0 && Monsters[0] != null)
        {
            _monsterTarget = Monsters[0].transform.Find("LookPos");
        }

        if (_characterTarget == null)
        {
            Debug.LogWarning($"[BattleManager] {_units[0].name} 에 LookPos 자식이 없습니다.");
            return;
        }

        // 카메라를 아군 뒤쪽에 놓는다.
        //
        // 예전에는 position 만 (0,0,-4) 로 밀고 회전은 그대로 뒀다. 전투 전용 씬의
        // Main Camera 가 이미 정면을 보고 있었으니 성립했던 코드다. 이제는 필드
        // 카메라를 이어받으므로, 위에서 내려다보는 회전이 남아 대상이 화면 밖으로
        // 밀려난다. CommandArea 의 화면 Y 가 1300 을 넘던 원인이다.
        //
        // CameraZoom() 이 angle 로 궤도를 계산하므로 각도만 맞춰주면
        // 다음 프레임부터 위치·회전이 모두 알아서 잡힌다.
        Vector3 _forward = BattleForward();

        angle = Mathf.Atan2(-_forward.x, -_forward.z) * Mathf.Rad2Deg;
        distance = Mathf.Clamp(distance, 4.0f, 5.02f);
        height = Mathf.Clamp(height <= 0.0f ? 3.5f : height, -0.5f, 5.0f);

        if (_camera != null)
        {
            float _rad = angle * Mathf.Deg2Rad;
            Vector3 _offset = new Vector3(Mathf.Sin(_rad) * distance, height, Mathf.Cos(_rad) * distance);

            _camera.transform.position = _characterTarget.position + _offset;
            _camera.transform.LookAt(_characterTarget);
        }

        UpdateCommandAreaPosition();
    }

    /// <summary>전투 대열의 정면. 아군에서 적군을 향하는 방향.</summary>
    private Vector3 BattleForward()
    {
        if (_stage != null)
        {
            Vector3 _f = _stage.EncounterForward;
            if (_f.sqrMagnitude > 0.0001f) return _f.normalized;
        }

        if (_characterTarget != null && _monsterTarget != null)
        {
            Vector3 _f = _monsterTarget.position - _characterTarget.position;
            _f.y = 0.0f;
            if (_f.sqrMagnitude > 0.0001f) return _f.normalized;
        }

        return Vector3.forward;
    }

    /// <summary>
    /// 커맨드 창을 대상 머리 옆에 붙인다.
    ///
    /// 예전에는 InitBattleView 에서 한 번만 계산했다. 전투 전용 씬에서는 카메라가
    /// 거의 고정이라 그걸로 충분했지만, CameraZoom 이 마우스로 궤도를 돌리므로
    /// 매 프레임 다시 투영해야 창이 캐릭터를 따라간다.
    /// </summary>
    private void UpdateCommandAreaPosition()
    {
        if (_characterTarget == null) return;
        if (ViewManager.ViewInstance == null) return;

        _characterScreenPos = ViewManager.ViewInstance.CommandAreaPosSet(_characterTarget.position);
    }

    // ── 인플레이스 전환 ─────────────────────────────────────
    //
    // 예전에는 조우 시 SceneManager.LoadScene("CommandBattle") 로 씬을 갈았다.
    // 그러면 Field 씬이 언로드되면서 전투 UI 와 필드 오브젝트가 모두 파괴되고,
    // DontDestroyOnLoad 로 살아남은 매니저들만 죽은 참조를 들고 남는다.
    // 원본(섬의궤적)은 씬을 갈지 않고 조우한 그 자리에서 전투를 시작한다.

    /// <summary>필드에서 몬스터와 접촉했을 때. ActionController 가 호출한다.</summary>
    public void BeginEncounter(GameObject fieldMonster)
    {
        if (IsTransitioning || GameFlow.IsBattle) return;

        // 트리거에 닿은 것이 몬스터의 자식 콜라이더일 수 있다.
        // 그대로 SetActive(false) 하면 몸통은 남고 콜라이더만 사라진다.
        if (fieldMonster != null)
        {
            Monster _m = fieldMonster.GetComponentInParent<Monster>();
            if (_m != null) fieldMonster = _m.gameObject;
        }

        StartCoroutine(EncounterRoutine(fieldMonster));
    }

    private IEnumerator EncounterRoutine(GameObject fieldMonster)
    {
        IsTransitioning = true;

        // ① 섬광으로 화면을 덮는다. 재배치는 이 뒤에 해야 보이지 않는다
        yield return EncounterFlash.FadeIn(_flashColor, _flashIn);

        // ② 조우 지점에 대열을 세운다
        BattleStage _st = EnsureStage();
        bool _built = _st != null && _st.BuildBattle(fieldMonster);

        if (!_built)
        {
            Debug.LogError("[BattleManager] 전투 대열을 세우지 못해 전투를 시작하지 않습니다.");
            EncounterFlash.Clear();
            ReleaseEncounterGuard();
            IsTransitioning = false;
            yield break;
        }

        // ③ 상태 전환. 이 시점부터 ActionController 의 이동과 FollowCamera 가 멈춘다
        GameFlow.SetState(GameState.Battle);
        IsBattle = true;

        // ④ UI 교체
        if (ViewManager.ViewInstance != null) ViewManager.ViewInstance.EnterBattleUI();

        yield return new WaitForSeconds(Mathf.Max(0.0f, _flashHold));

        // ⑤ 전투 시작. 섬광이 걷히기 전에 불러 첫 프레임부터 대열이 보이게 한다
        Battle();

        // ⑥ 섬광을 걷는다
        yield return EncounterFlash.FadeOut(_flashColor, _flashOut);

        IsTransitioning = false;
    }

    /// <summary>전투 종료 후 필드로 복귀. 씬을 갈지 않는다.</summary>
    private IEnumerator ReturnToFieldRoutine(bool victory)
    {
        IsTransitioning = true;

        yield return EncounterFlash.FadeIn(_flashColor, _flashIn);

        // 전투용 몬스터 정리와 캐릭터 복구
        BattleStage _st = EnsureStage();
        if (_st != null) _st.RestoreField(victory);

        // 전투 상태 정리. 다음 전투가 이전 목록을 물려받지 않게 한다
        Characters.Clear();
        Monsters.Clear();
        if (_battleUnit != null) _battleUnit.Clear();
        _pendingSkill = null;
        _characterTarget = null;
        _monsterTarget = null;
        _commandState = CommandState.Select;
        _lastUISetState = (CommandState)(-1);
        IsBattle = false;

        if (_createCommandActionMemberSystem != null) _createCommandActionMemberSystem.ClearAll();

        GameFlow.SetState(GameState.Field);

        if (ViewManager.ViewInstance != null) ViewManager.ViewInstance.ExitBattleUI();

        // 다시 조우할 수 있게 트리거 잠금을 푼다.
        // 씬을 갈 때는 컴포넌트가 새로 만들어져 저절로 풀렸던 부분이다
        ReleaseEncounterGuard();

        yield return new WaitForSeconds(Mathf.Max(0.0f, _flashHold));
        yield return EncounterFlash.FadeOut(_flashColor, _flashOut);

        IsTransitioning = false;
    }

    private BattleStage EnsureStage()
    {
        if (_stage != null) return _stage;

        _stage = FindFirstObjectByType<BattleStage>(FindObjectsInactive.Include);

        if (_stage == null)
        {
            _stage = gameObject.AddComponent<BattleStage>();
            Debug.Log("[BattleManager] BattleStage 를 찾지 못해 런타임에 추가했습니다. " +
                      "씬에 미리 붙여두면 인스펙터에서 대열 간격을 조절할 수 있습니다.");
        }

        return _stage;
    }

    /// <summary>모든 캐릭터의 조우 잠금을 푼다.</summary>
    private void ReleaseEncounterGuard()
    {
        if (GameManager.GameInstance == null) return;

        List<GameObject> _chars = GameManager.GameInstance.Characters;
        if (_chars == null) return;

        for (int i = 0; i < _chars.Count; i++)
        {
            if (_chars[i] == null) continue;

            ActionController _ac = _chars[i].GetComponent<ActionController>();
            if (_ac != null) _ac.ResetEncounter();
        }
    }

    // ── 턴 진행 ─────────────────────────────────────────────

    /// <summary>
    /// 현재 선두 유닛의 차례를 시작한다.
    /// 몬스터면 자동으로 행동하고, 캐릭터면 커맨드 입력을 기다린다.
    /// </summary>
    private void StartTurn()
    {
        if (_isBattleOver) return;

        // 행동 불가·브레이크로 차례를 넘기는 경우가 이어질 수 있어 반복한다
        for (int guard = 0; guard < 32; guard++)
        {
            if (_flow != null)
            {
                _flow.Queue.RemoveDead();
                SyncUnitsFromQueue();

                if (_flow.Queue.Count == 0) { CheckBattleEnd(); return; }

                // 지속 피해·잔여 턴 처리. false 면 이 유닛은 행동하지 않고 넘어간다
                if (!_flow.BeginTurn()) continue;
            }

            Unit _actor = CurrentActor();
            if (_actor == null) { CheckBattleEnd(); return; }

            CameraMove(0);

            if (_actor is Monster _monster)
            {
                _commandState = CommandState.MonsterTurn;
                StartCoroutine(MonsterTurnRoutine(_monster));
            }
            else
            {
                _commandState = CommandState.Select;
            }

            return;
        }

        Debug.LogWarning("BattleManager : 행동 가능한 유닛을 찾지 못했습니다.");
    }

    /// <summary>
    /// 큐 순서를 GameManager.Units 에 반영한다.
    /// 기존 코드가 Units[0] 을 현재 행동 유닛으로 보기 때문이다.
    /// </summary>
    private void SyncUnitsFromQueue()
    {
        if (_flow == null) return;

        List<Unit> _order = new List<Unit>();

        foreach (BattleUnit _bu in _flow.Queue.Entries)
        {
            if (_bu != null && _bu.Unit != null) _order.Add(_bu.Unit);
        }

        GameManager.GameInstance.Units = _order;
        _battleUnit = _order;
    }

    // 2. 씬 로드 시, 배틀 씬 진입 시 Battle() 호출
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == CommandBattleSceneName)
        {
            _camera = Camera.main;

            IsBattle = true;

            // GameManager 도 같은 sceneLoaded 를 구독한다. 어느 쪽이 먼저 호출되는지는
            // Awake 순서에 달려 있어서, BattleManager 가 먼저 돌면 GameManager 가
            // 유닛을 만들기 전의 목록을 복사하게 된다.
            // 한 프레임 미루면 순서에 상관없이 완성된 목록을 본다.
            StartCoroutine(BeginBattleNextFrame());
        }
    }

    private IEnumerator BeginBattleNextFrame()
    {
        yield return null;
        Battle();
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

        // 몬스터 턴에는 어떤 입력도 받지 않는다
        if (_commandState == CommandState.MonsterTurn) return;

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

        // 후퇴는 승리가 아니므로 조우한 몬스터가 필드에 되살아난다.
        // BattleStage 가 플레이어를 뒤로 밀어내 즉시 재조우하지 않게 한다
        yield return ReturnToFieldRoutine(false);
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
                int _selCount = SafeMonsterCount();
                if (_selCount > 0)
                {
                    Monsters[Mathf.Clamp(nextIndex, 0, _selCount - 1)].TargetAreaUnShow();
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

    // 몬스터 턴으로 넘어갈 때 바닥에 남아 있는 타깃 표시를 모두 지운다
    private void HideAllTargetAreas()
    {
        int _count = SafeMonsterCount();

        for (int i = 0; i < _count; i++)
        {
            if (Monsters[i] != null) Monsters[i].TargetAreaUnShow();
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

            // 몬스터 턴 : 플레이어가 조작할 것이 없으므로 커맨드 UI 전체를 감춘다
            case CommandState.MonsterTurn:
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.skillSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.instrumentSelectView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.MemuButtontInfoBar, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.targetView, false);
                ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, false);
                HideAllTargetAreas();
                break;
        }
    }

    // 8. 배틀 씬에서 CommandAttack 상태일 때, 플레이어 캐릭터가 몬스터를 공격하는 코루틴 실행
    void PlayerAttack(Unit _unit, GameObject _monster)
    {
        _lastActionBaseDelay = PendingBaseDelay();

        Unit _defender = _monster != null ? _monster.GetComponent<Unit>() : null;
        StartCoroutine(AttackSequence(_unit, _defender));
    }

    // 데미지 판정. 공격자가 몬스터인지에 따라 방향만 다르고 공식은 같다.
    // AttackSequence 에서 맞는 순간에 호출된다.
    private void ApplyAttackDamage(Unit _attacker, Unit _defender)
    {
        if (_attacker == null || _defender == null) return;

        if (_attacker is Monster _atkMonster && _defender is Character _defCharacter)
        {
            MonsterAttack(_defCharacter, _atkMonster);
        }
        else if (_defender is Monster _defMonster)
        {
            MonsterTakeDmg(_attacker, _defMonster);
        }
    }

    // 피격 결과를 대상 머리 위에 문자로 띄운다.
    //   명중 : 데미지 수치 (크리티컬이면 색이 바뀌고 뒤에 ! 가 붙는다)
    //   회피 : AVOID
    // FloatingCombatText 가 월드 공간 TMP 를 런타임에 만들기 때문에 프리팹이 필요하지 않다.
    private void ShowCombatText(Unit _target, DamageResult _result)
    {
        if (_target == null) return;

        string _body;
        Color _color;

        if (_result.Hit)
        {
            _body = _result.Critical ? $"{_result.Amount}!" : _result.Amount.ToString();
            _color = _result.Critical ? _criticalColor : _damageColor;
        }
        else
        {
            // 대상이 피한 것과 공격이 빗나간 것을 구분해서 보여준다
            _body = _result.Judge == HitResult.Evade ? "AVOID" : "MISS";
            _color = _avoidColor;
        }

        Debug.Log($"BattleManager : 전투 문자 — {_target.Stat.Name} 위에 \"{_body}\" 표시");

        FloatingCombatText.DebugHold = _combatTextDebugHold;
        FloatingCombatText.DebugBackground = _combatTextDebugBackground;

        float _size = _result.Critical ? _combatTextFontSizePx * 1.3f : _combatTextFontSizePx;

        FloatingCombatText.Show(_target.transform, _body, _color,
                                _size, _combatTextDuration, _combatTextRisePx);
    }

    // 기본 공격 판정: 명중/회피 → 방어력 반영 데미지 → 크리티컬 순으로 계산한다.
    // (TC 128 방어력, 130 크리티컬, 131 명중/회피)
    private DamageResult ResolveAttack(Stat _attacker, Stat _defender)
    {
        DamageResult result = new DamageResult();

        // 명중 판정과 데미지 공식을 SkillResolver 와 공유한다.
        // 예전에는 이 메서드가 자체 공식을 갖고 있어서 스킬 쪽과 값이 달랐다
        result.Judge = SkillResolver.RollHitRaw(
            _attacker.Hit, _defender.Avoid,
            _attacker.Dex, _defender.Agl,
            _attacker.Critical,
            false,      // 통상공격은 확정 명중이 아니다
            false);     // 브레이크 상태는 아직 기본 공격에 반영하지 않는다

        if (result.Judge == HitResult.Miss || result.Judge == HitResult.Evade)
        {
            result.Hit = false;
            result.Amount = 0;
            return result;
        }

        result.Hit = true;
        result.Critical = result.Judge == HitResult.Critical;

        // 통상공격은 위력 100%
        result.Amount = SkillResolver.CalcDamageRaw(
            _attacker.Str, _defender.Def, 100,
            result.Critical, _attacker.CriticalDmg,
            false,      // S크래프트 아님
            false);     // 브레이크 아님

        return result;
    }

    // 몬스터의 자동 반격/선공. 현재 턴 유닛이 몬스터일 때 CommandSelectController 대신 호출된다. (TC 136, 145)
    private IEnumerator MonsterTurnRoutine(Monster _monster)
    {
        if (_isBattleOver || _monster == null) yield break;

        // 어느 경로로 들어와도 몬스터 턴에는 커맨드 UI 가 꺼져 있어야 한다
        _commandState = CommandState.MonsterTurn;

        List<Character> _livingCharacters = Characters.Where(c => c != null && c.Stat.Hp > 0).ToList();
        if (_livingCharacters.Count == 0)
        {
            CheckBattleEnd();
            yield break;
        }

        // 대상은 살아있는 캐릭터 중 무작위
        Character _target = _livingCharacters[UnityEngine.Random.Range(0, _livingCharacters.Count)];

        // 몬스터는 통상공격만 한다
        _lastActionBaseDelay = AT.BaseAttack;

        Debug.Log($"BattleManager : 몬스터 턴 — {_monster.Stat.Name} → {_target.Stat.Name} " +
                  $"(SPD {_monster.Stat.Speed}, 딜레이 {AT.Delay(_monster.Stat.Speed, AT.BaseAttack)} AT)");

        // 캐릭터가 커맨드를 고를 때와 같은 방식으로 예상 슬롯을 띄운다.
        // 몬스터는 통상공격만 하므로 BaseDelay 는 항상 20 이다.
        if (_actionBar != null)
        {
            _actionBar.ShowDelayPreview(_monster, AT.BaseAttack);
        }

        yield return new WaitForSeconds(0.8f);

        if (_isBattleOver || _monster == null) yield break;

        // 연출이 시작되면 배지만 감춘다. 예상 슬롯은 TrunMove 에서 승격된다
        if (_actionBar != null) _actionBar.LockPreview();

        // 캐릭터 공격과 같은 코루틴을 쓴다. 이동 → 공격 → 복귀 → 데미지 → TrunMove
        yield return StartCoroutine(AttackSequence(_monster, _target));
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
            Debug.Log($"{_monster.Stat.Name}의 공격 — {(result.Judge == HitResult.Evade ? "회피됨" : "빗나감")}");
        }

        // 사망 처리보다 먼저 띄운다. 죽은 유닛은 대열에서 빠지므로 위치를 잡을 수 없다
        ShowCombatText(_character, result);

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

        // 큐에서도 빼야 시체가 순서에 남지 않는다
        if (_flow != null)
        {
            BattleUnit _bu = _flow.Queue.Find(_unit);
            if (_bu != null) _flow.Queue.Remove(_bu);

            // 액션 바를 바로 갱신한다. 큐 이벤트만 믿으면 구독이 끊긴 경우
            // 다음 턴이 시작될 때까지 아래 슬롯들이 그대로 남는다
            if (_actionBar != null) _actionBar.RefreshFromQueue();
        }

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

        yield return ReturnToFieldRoutine(victory);
    }

    /// <summary>
    /// 공격 연출. 캐릭터 → 몬스터, 몬스터 → 캐릭터 양쪽에 같은 코루틴을 쓴다.
    /// 이동 → 공격 → 복귀 → 데미지 → 턴 넘김.
    ///
    /// 접근 지점은 방어자를 기준으로 공격자가 있던 쪽에서 approachDistance 만큼
    /// 떨어진 곳이다. 예전에는 -Z 로 고정돼 있어서 방향이 반대면 대상을 지나쳤다.
    /// </summary>
    IEnumerator AttackSequence(Unit _attacker, Unit _defender)
    {
        if (_attacker == null || _defender == null) yield break;

        // 접근 방향을 계산하는 기준점. 공격 후 이 자리로 돌아오지는 않는다
        Vector3 StartPos = _attacker.transform.position;

        CharacterController _controller = _attacker.GetComponent<CharacterController>();
        Animator _animator = _attacker.GetComponent<Animator>();

        // 파라미터(IsRun 등)가 있으면 그것을 쓰고, 없으면 상태 이름으로 직접 재생한다.
        // 몬스터 컨트롤러는 파라미터 없이 상태만 있는 경우가 많다.
        bool _useParams = HasAnimParam(_animator, "IsRun");
        int _idleStateHash = CurrentStateHash(_animator);

        const float moveSpeedUnits = 5.0f;
        const float arriveThreshold = 0.1f;
        const float approachDistance = 2.5f;
        const float maxSeconds = 3.0f; // 안전장치: 무슨 일이 있어도 이 시간 안에는 다음 단계로 넘어간다.

        // 공격자가 서 있던 쪽에서 방어자에게 접근한다
        Vector3 _approachDir = StartPos - _defender.transform.position;
        _approachDir.y = 0f;
        if (_approachDir.sqrMagnitude < 0.0001f) _approachDir = Vector3.back;
        _approachDir = _approachDir.normalized;

        // 회피 대상 수집. 공격자와 방어자는 제외한다.
        // 방어자는 접근 목표이므로 피할 대상이 아니다 (approachDistance 앞에서 멈춘다)
        GatherObstacles(_attacker, _defender);

        // 달리기 재생은 루프 진입 전에 한 번만 한다.
        // 매 프레임 CrossFade 를 부르면 전환이 계속 다시 시작되어 목적 상태의
        // 시간이 0 에서 멈춘 채 포즈가 굳는다 (제자리에서 미끄러지는 것처럼 보임).
        PlayRun(_animator, _useParams, moveSpeedUnits);

        // 대상 앞까지 이동 (거리에 관계없이 목표 지점에 정확히 도달) (TC 118, 120)
        float elapsed = 0f;
        while (elapsed < maxSeconds)
        {
            // 연출 중 대상 · 공격자 파괴, 전투 종료 (TC 126)
            if (_attacker == null || _defender == null || _isBattleOver) yield break;

            // 목표 지점도 영역 안으로 당긴다. 밖이면 경계를 밀며 maxSeconds 를 다 쓴다
            Vector3 endPos = BattleArea.ClampToArea(
                _defender.transform.position + _approachDir * approachDistance);
            Vector3 toTarget = endPos - _attacker.transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= arriveThreshold)
            {
                break;
            }

            // 경로에 동료가 있으면 옆으로 비켜 간다.
            //
            // 직선으로 밀고 들어가면 CharacterController 가 상대 캡슐의 둥근 면을
            // 타고 올라가, 동료 머리를 밟고 넘어가는 것처럼 보인다.
            Vector3 dir = _avoidUnitsWhileMoving
                ? BattleSteering.Steer(_attacker.transform.position, endPos,
                                       _obstacleBuffer, _obstacleCount,
                                       UnitRadius(_attacker), _avoidLookAhead, _avoidClearance)
                : toTarget.normalized;

            if (dir == Vector3.zero) dir = toTarget.normalized;

            MoveUnit(_attacker, _controller, dir * moveSpeedUnits * Time.deltaTime);
            _attacker.transform.rotation = Quaternion.LookRotation(dir);

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopRun(_animator, _useParams, _idleStateHash);

        yield return new WaitForSeconds(0.2f);

        if (_defender == null) yield break;

        // 대상을 바라보고 공격
        Vector3 _lookDir = _defender.transform.position - _attacker.transform.position;
        _lookDir.y = 0f;
        if (_lookDir.sqrMagnitude > 0.0001f)
        {
            _attacker.transform.rotation = Quaternion.LookRotation(_lookDir.normalized);
        }

        // 공격. 트리거가 없으면 Attack1 같은 상태를 직접 재생한다
        if (HasAnimParam(_animator, "Attack")) _animator.SetTrigger("Attack");
        else TryCrossFade(_animator, _attackStateName, 0.05f);

        // 맞는 시점까지 기다린다
        yield return new WaitForSeconds(Mathf.Max(0f, _attackImpactDelay));

        // 데미지 판정과 문자 표시는 맞는 순간에 한다. 복귀를 기다리지 않는다
        ApplyAttackDamage(_attacker, _defender);

        // 남은 공격 동작을 마친다
        yield return new WaitForSeconds(Mathf.Max(0f, _attackAnimSeconds - _attackImpactDelay));

        // 마지막 몬스터를 쓰러뜨렸으면 전투가 끝나고 곧 씬이 바뀐다.
        // 파괴될 오브젝트를 계속 움직이면 예외가 난다
        if (_attacker == null || _isBattleOver) yield break;

        // 공격 후 원위치 복귀는 하지 않는다.
        //
        // 왕복 이동이 턴마다 반복되면 연출이 길어지고, 원본도 행동한 자리에 남는다.
        // 대열이 흐트러지는 것은 전투 영역(BattleArea) 안에서 벌어지므로
        // 유닛이 전장 밖으로 흘러나가지는 않는다.
        //
        // 회전은 되돌리지 않는다. 방금 때린 상대를 계속 보고 있는 것이 자연스럽다.

        yield return new WaitForSeconds(0.3f);

        IsAttack = false;

        if (!_isBattleOver)
        {
            TrunMove(_attacker);
        }

        // 다음 행동 유닛이 몬스터면 커맨드 UI 를 계속 감춘 상태로 둔다.
        // 여기서 Select 로 되돌리면 몬스터 턴 사이에 커맨드 창이 한 순간 깜빡인다
        _commandState = CurrentActor() is Monster
                        ? CommandState.MonsterTurn
                        : CommandState.Select;
    }

    // ── 연출 보조 ───────────────────────────────────────────

    /// <summary>
    /// CharacterController 가 있으면 그것으로, 없으면 Transform 으로 옮긴다.
    /// 몬스터 프리팹에 CharacterController 가 없어도 같은 연출이 돌아간다.
    /// </summary>
    /// <summary>
    /// 회피 계산에 쓸 장애물 목록을 채운다. 전투 중 유닛은 거의 움직이지 않으므로
    /// 이동 루프마다 한 번만 모으면 충분하다.
    /// </summary>
    private void GatherObstacles(Unit _attacker, Unit _defender)
    {
        _obstacleCount = 0;

        if (!_avoidUnitsWhileMoving) return;
        if (GameManager.GameInstance == null) return;

        List<Unit> _units = GameManager.GameInstance.Units;
        if (_units == null) return;

        for (int i = 0; i < _units.Count; i++)
        {
            Unit _u = _units[i];

            if (_u == null) continue;
            if (_u == _attacker || _u == _defender) continue;
            if (!_u.gameObject.activeInHierarchy) continue;
            if (_u.Stat != null && _u.Stat.Hp <= 0) continue;

            if (_obstacleCount >= _obstacleBuffer.Length) break;

            _obstacleBuffer[_obstacleCount].Position = _u.transform.position;
            _obstacleBuffer[_obstacleCount].Radius = UnitRadius(_u);
            _obstacleCount++;
        }
    }

    /// <summary>유닛의 수평 반지름. CharacterController 가 있으면 그 값을 쓴다.</summary>
    private static float UnitRadius(Unit _unit)
    {
        if (_unit == null) return 0.5f;

        CharacterController _cc = _unit.GetComponent<CharacterController>();
        return _cc != null ? _cc.radius : 0.5f;
    }

    /// <summary>
    /// 유닛 이동. 전투 영역 밖으로는 나가지 않는다.
    ///
    /// 공격 후 복귀를 없앴으므로 유닛은 이동한 자리에 계속 남는다.
    /// 경계가 없으면 턴이 반복되는 사이에 지형 밖까지 흘러나간다.
    /// 두 이동 루프가 모두 이 함수를 지나므로 여기 한 곳에서 막는다.
    /// </summary>
    private static void MoveUnit(Unit _unit, CharacterController _controller, Vector3 _delta)
    {
        Vector3 _next = _unit.transform.position + _delta;
        Vector3 _clamped = BattleArea.ClampToArea(_next);

        if (_controller != null)
        {
            _controller.Move(_clamped - _unit.transform.position);
        }
        else
        {
            _unit.transform.position = _clamped;
        }
    }

    /// <summary>
    /// Animator 에 없는 파라미터를 건드리면 콘솔이 경고로 뒤덮인다.
    /// 몬스터 컨트롤러는 캐릭터와 파라미터 구성이 다를 수 있으므로 확인하고 넣는다.
    /// </summary>
    private static bool HasAnimParam(Animator _animator, string _name)
    {
        if (_animator == null) return false;

        for (int i = 0; i < _animator.parameterCount; i++)
        {
            if (_animator.GetParameter(i).name == _name) return true;
        }
        return false;
    }

    private static void AnimBool(Animator _animator, string _name, bool _value)
    {
        if (HasAnimParam(_animator, _name)) _animator.SetBool(_name, _value);
    }

    private static void AnimFloat(Animator _animator, string _name, float _value)
    {
        if (HasAnimParam(_animator, _name)) _animator.SetFloat(_name, _value);
    }

    private static void AnimTrigger(Animator _animator, string _name)
    {
        if (HasAnimParam(_animator, _name)) _animator.SetTrigger(_name);
    }

    /// <summary>현재 재생 중인 상태. 연출이 끝나면 이 상태로 되돌린다.</summary>
    private static int CurrentStateHash(Animator _animator)
    {
        if (_animator == null) return 0;
        return _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
    }

    /// <summary>이름이 같은 상태가 있으면 그것으로 전환한다. 없으면 아무것도 하지 않는다.</summary>
    private static bool TryCrossFade(Animator _animator, string _stateName, float _duration)
    {
        if (_animator == null || string.IsNullOrEmpty(_stateName)) return false;

        int _hash = Animator.StringToHash(_stateName);
        if (!_animator.HasState(0, _hash)) return false;

        _animator.CrossFade(_hash, _duration);
        return true;
    }

    private void PlayRun(Animator _animator, bool _useParams, float _moveSpeed)
    {
        if (_useParams)
        {
            AnimBool(_animator, "IsWalk", false);
            AnimBool(_animator, "IsRun", true);
            AnimFloat(_animator, "moveSpeed", _moveSpeed);
            return;
        }

        if (_animator == null) return;

        int _hash = Animator.StringToHash(_runStateName);
        if (!_animator.HasState(0, _hash)) return;

        // 이미 그 상태거나 그 상태로 전환 중이면 다시 부르지 않는다
        if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _hash) return;

        if (_animator.IsInTransition(0) &&
            _animator.GetNextAnimatorStateInfo(0).shortNameHash == _hash) return;

        _animator.CrossFade(_hash, 0.1f);

        StartCoroutine(WarnIfStateHasNoClip(_animator, _runStateName));
    }

    /// <summary>
    /// 상태는 바뀌었는데 클립이 비어 있으면 포즈가 그대로 굳는다.
    /// Idle 상태의 Motion 슬롯이 비어 있어 캐릭터가 솟았던 것과 같은 종류다.
    /// </summary>
    private static readonly HashSet<string> _warnedEmptyStates = new HashSet<string>();

    private IEnumerator WarnIfStateHasNoClip(Animator _animator, string _stateName)
    {
        yield return null;   // 전환이 시작될 시간을 준다

        if (_animator == null || _warnedEmptyStates.Contains(_stateName)) yield break;

        if (_animator.GetCurrentAnimatorClipInfoCount(0) == 0 &&
            _animator.GetNextAnimatorClipInfoCount(0) == 0)
        {
            _warnedEmptyStates.Add(_stateName);
            Debug.LogWarning($"[BattleManager] '{_stateName}' 상태에 클립이 없습니다. " +
                             "Animator 에서 이 상태의 Motion 슬롯을 확인하세요. " +
                             "상태는 전환되지만 포즈가 움직이지 않습니다.");
        }
    }

    private void StopRun(Animator _animator, bool _useParams, int _idleStateHash)
    {
        if (_useParams)
        {
            AnimBool(_animator, "IsWalk", false);
            AnimBool(_animator, "IsRun", false);
            AnimFloat(_animator, "moveSpeed", 0.0f);
            return;
        }

        if (_animator != null && _idleStateHash != 0) _animator.CrossFade(_idleStateHash, 0.12f);
    }

    /// <summary>
    /// 타깃 인덱스로 안전하게 쓸 수 있는 몬스터 수.
    ///
    /// TargetView 는 BattleManager.Monsters 를, TargetViewPosSet 은
    /// GameManager.Monsters 를 같은 인덱스로 참조한다. 두 목록의 길이가
    /// 어긋나면 짧은 쪽에서 ArgumentOutOfRangeException 이 난다.
    /// 그래서 항상 짧은 쪽을 기준으로 삼는다.
    /// </summary>
    private int SafeMonsterCount()
    {
        PruneMonsters();

        int _mine = Monsters.Count;
        int _shared = GameManager.GameInstance.Monsters != null
                      ? GameManager.GameInstance.Monsters.Count : 0;

        return Mathf.Min(_mine, _shared);
    }

    /// <summary>파괴된 몬스터가 목록에 남아 있으면 걷어낸다.</summary>
    private void PruneMonsters()
    {
        for (int i = Monsters.Count - 1; i >= 0; i--)
        {
            if (Monsters[i] == null) Monsters.RemoveAt(i);
        }

        List<GameObject> _shared = GameManager.GameInstance.Monsters;
        if (_shared == null) return;

        for (int i = _shared.Count - 1; i >= 0; i--)
        {
            if (_shared[i] == null) _shared.RemoveAt(i);
        }
    }

    void TargetMove()
    {
        int _count = SafeMonsterCount();
        if (_count == 0) return;

        oriIndex = Mathf.Clamp(oriIndex, 0, _count - 1);
        nextIndex = Mathf.Clamp(nextIndex, 0, _count - 1);

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (IsMove == false)
        {
            IsMove = true;

            if (scroll < 0.0f)
            {
                nextIndex = oriIndex - 1;
                if (nextIndex < 0) { nextIndex = _count - 1; }
            }
            else if (scroll > 0.0f)
            {
                nextIndex = oriIndex + 1;
                if (nextIndex > _count - 1) { nextIndex = 0; }
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
            Debug.Log($"{_attacker.Stat.Name}의 공격 — {(result.Judge == HitResult.Evade ? "회피됨" : "빗나감")}");
        }

        // 사망 처리보다 먼저 띄운다. 죽은 유닛은 대열에서 빠지므로 위치를 잡을 수 없다
        ShowCombatText(_monster, result);

        UnitDeath(_monster);
    }

    // 턴 종료 처리: 방금 행동한 유닛(_actedUnit)을 큐 맨 뒤로 보내고, 나머지 생존 유닛을
    // Speed 기준으로 다시 정렬한다. 다음 턴 유닛이 몬스터면 자동으로 MonsterTurnRoutine 을
    // 실행한다. (TC 144, 145, 146, 147, 148)
    void TrunMove(Unit _actedUnit)
    {
        if (_isBattleOver) return;

        // AT 큐 모드 : 행동한 유닛에게 딜레이를 부여하고 큐를 재정렬한다.
        //   Final Delay = floor(100 * BaseDelay / SPD)
        // 몬스터도 같은 공식을 쓰고 BaseDelay 는 통상공격 20 뿐이다.
        if (_flow != null)
        {
            BattleUnit _bu = _flow.Queue.Find(_actedUnit);

            if (_bu != null)
            {
                int _delay = AT.Delay(_bu, _lastActionBaseDelay);
                _flow.Queue.Push(_bu, _lastActionBaseDelay);

                Debug.Log($"BattleManager : {_actedUnit.Stat.Name} 행동 종료 — " +
                          $"BaseDelay {_lastActionBaseDelay}, SPD {_bu.EffectiveSpeed:0.##} → +{_delay} AT (AT {_bu.At})");
            }

            if (_actionBar != null) _actionBar.CommitTurn(_actedUnit);

            _lastActionBaseDelay = AT.BaseAttack;
            _pendingSkill = null;

            StartTurn();
            return;
        }

        List<Unit> _living = GameManager.GameInstance.Units.Where(u => u != null && u.Stat.Hp > 0).ToList();

        if (_living.Count == 0)
        {
            CheckBattleEnd();
            return;
        }

        // 매 턴 Speed 로 다시 정렬하면 가장 빠른 유닛만 계속 차례를 가져간다.
        // Zangbi(50) 와 Ubi(35) 가 번갈아 행동하고 SPD 30 인 Gwanwoo·Bear 는
        // 영원히 차례가 오지 않아 몬스터가 공격하지 못했다.
        //
        // 순서는 Battle() 에서 한 번 Speed 로 정하고, 이후에는 행동한 유닛만
        // 맨 뒤로 보낸다. 죽은 유닛은 목록에서 빠지고 나머지 순서는 유지된다.
        List<Unit> _sorted = _living
            .Where(u => u != _actedUnit)
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
            _commandState = CommandState.MonsterTurn;
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
    /// <summary>
    /// 스킬 정의와 AT 큐를 확보한다. 씬에 없으면 직접 붙인다.
    /// SkillDataBase 는 CSV 를 1회 로드하고 DontDestroyOnLoad 로 살아남는다.
    /// </summary>
    private void EnsureBattleSystems()
    {
        if (SkillDataBase.Instance == null)
        {
            GameObject _dbObj = new GameObject("SkillDataBase");
            _dbObj.AddComponent<SkillDataBase>();
            Debug.Log("BattleManager : SkillDataBase 를 런타임에 생성했습니다.");
        }

        if (_flow == null) _flow = GetComponent<BattleFlow>();
        if (_flow == null) _flow = BattleFlow.Instance;

        if (_flow == null)
        {
            _flow = gameObject.AddComponent<BattleFlow>();
            Debug.Log("BattleManager : BattleFlow 를 런타임에 추가했습니다.");
        }
    }

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

            case CommandState.MonsterTurn:
                // 예상 슬롯은 MonsterTurnRoutine 이 직접 관리한다. 건드리지 않는다
                break;

            default:
                // 몬스터 턴에는 몬스터 루틴이 예상 슬롯을 관리한다.
                // 캐릭터 공격이 끝나며 Select 로 바뀐 것이 한 프레임 늦게 반영되어
                // 몬스터가 막 만든 예상 슬롯을 지워버리는 일을 막는다.
                if (CurrentActor() is Monster) break;

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
        // 몬스터는 통상공격만 하므로 항상 20 이다
        Unit _actor = CurrentActor();
        if (_actor is Monster) return AT.BaseAttack;

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
