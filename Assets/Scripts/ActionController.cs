using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ActionController : MonoBehaviour
{
    [SerializeField] private int _speed2 = 1; // 캐릭터의 이동속도 배수

    [Header("이동 속도")]
    [Tooltip("걷기 속도. 애니메이션 보폭과 안 맞으면 발이 미끄러져 보인다")]
    [SerializeField] private float _walkSpeed = 2.0f;
    [Tooltip("달리기 속도")]
    [SerializeField] private float _runSpeed = 5.0f;

    [Header("전투 조우")]
    [Tooltip("전투 종료 후 이 시간(초) 동안은 조우를 받지 않는다. 종료 직후 즉시 재조우하는 것을 막는다")]
    [SerializeField] private float _encounterGrace = 1.5f;

    [Header("중력")]
    [SerializeField] private float _gravity = -9.8f;
    [Tooltip("접지 중 바닥에 붙여두는 힘. 0 이면 경사에서 통통 튄다")]
    [SerializeField] private float _groundStick = -2.0f;
    [Tooltip("낙하 속도 상한")]
    [SerializeField] private float _maxFallSpeed = 50.0f;

    private float _speed = 5; // 캐릭터의 이동속도
    private CharacterController _characterController;
    private Animator _animator;


    private float oriSpeed;

    /// <summary>필드가 아닌 상태로 넘어가며 Idle 을 강제했는지.</summary>
    private bool _idleForced = false;

    private Vector3 velocity;
    private bool _isAttack = false; // 현재 공격중인지 확인
    private bool _isWalk = false;   // 현재 걷는 중인지 확인
    private bool _isRun = true;     // 현재 뛰는 중인지 확인
    

    public enum PlayerState
    {
        Idle,
        Attack,
        Walk,
        Run,
        FieldBattle,
        CommandBattle,
        Conversation,
    }

    public PlayerState CurrentPlayerState { set; get; } = PlayerState.Idle;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 필드가 아니면 이동 처리를 하지 않는다.
        //
        // 다만 그냥 빠져나가면 애니메이터에 마지막 상태(Run/Walk)가 그대로 남아
        // 전투가 시작된 뒤에도 제자리에서 계속 걷는다. 예전에는 씬이 바뀌면서
        // 캐릭터가 새로 만들어져 저절로 Idle 로 시작했던 부분이다.
        if (!GameFlow.IsField)
        {
            if (!_idleForced)
            {
                ForceIdle();
                _idleForced = true;
            }
            return;
        }

        _idleForced = false;

        // 전환 연출 중에는 입력을 받지 않는다. 섬광이 터지는 사이에 움직이면
        // 대열 계산에 쓴 조우 지점과 실제 위치가 어긋난다.
        if (BattleManager.BattleInstance != null && BattleManager.BattleInstance.IsTransitioning) return;

        if(ViewManager.ViewInstance.IsMenuActive == false)
        {
            CharacterMove();
        }
    }
        
    private void CharacterMove()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direct = new Vector3(horizontal, 0.0f, vertical);

        // 대각선 입력은 길이가 1.41 이 되어 더 빨라진다. 방향만 남긴다
        if (direct.sqrMagnitude > 1.0f) direct = direct.normalized;

        // ── 상태 판정 ───────────────────────────────────────
        if (direct != Vector3.zero)
        {
            if (_isAttack == false)
            {
                if (_isWalk == false && _isRun == true)
                {
                    CurrentPlayerState = PlayerState.Run;
                }
                else if (_isWalk == true && _isRun == false)
                {
                    CurrentPlayerState = PlayerState.Walk;
                }
            }
        }
        else
        {
            CurrentPlayerState = PlayerState.Idle;
        }

        if (Input.GetMouseButtonDown(0) && !_isAttack)
        {
            if (GameFlow.IsField)
            {
                CurrentPlayerState = PlayerState.Attack;
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (_isRun == false && _isWalk == true)
            {
                CurrentPlayerState = PlayerState.Run;
            }
            else
            {
                CurrentPlayerState = PlayerState.Walk;
            }
        }

        // 상태를 먼저 반영해야 이번 프레임의 이동에 새 속도가 쓰인다.
        // 예전에는 이동 계산이 앞에 있어서 한 프레임 늦은 속도로 움직였다
        ApplyState();

        // ── 중력 ────────────────────────────────────────────
        // isGrounded 는 Move 를 부른 결과로만 갱신된다. 그래서 입력이 없어도
        // 매 프레임 Move 를 불러야 한다. 예전에는 입력이 있을 때만 불러서
        // 가만히 서 있으면 중력이 적용되지 않아 캐릭터가 공중에 떠 있었다
        if (_characterController.isGrounded && velocity.y < 0.0f)
        {
            velocity.y = _groundStick;
        }
        else
        {
            velocity.y += _gravity * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, -_maxFallSpeed);
        }

        // ── 이동 ────────────────────────────────────────────
        Vector3 move = _isAttack ? Vector3.zero : direct * _speed * _speed2;
        move.y = velocity.y;

        _characterController.Move(move * Time.deltaTime);

        if (direct != Vector3.zero && _isAttack == false)
        {
            this.transform.rotation = Quaternion.LookRotation(direct);
        }
    }

    /// <summary>
    /// 이동 애니메이션을 즉시 멈춘다. 필드를 벗어날 때 한 번 호출된다.
    /// 전투 연출(AttackSequence)이 같은 애니메이터를 다시 몰기 때문에,
    /// 여기서는 이동 관련 파라미터만 초기값으로 돌려놓는다.
    /// </summary>
    private void ForceIdle()
    {
        CurrentPlayerState = PlayerState.Idle;

        _isAttack = false;
        _isWalk = false;
        _isRun = true;
        velocity = Vector3.zero;

        if (_animator == null) return;

        _animator.SetBool("IsWalk", false);
        _animator.SetBool("IsRun", false);
        _animator.SetFloat("moveSpeed", 0.0f);
    }

    /// <summary>현재 상태에 맞는 속도와 애니메이터 파라미터를 적용한다.</summary>
    private void ApplyState()
    {
        switch (CurrentPlayerState)
        {
            case PlayerState.Idle:
                _animator.SetFloat("moveSpeed", 0.0f);
                break;

            case PlayerState.Walk:
                _isWalk = true;
                _isRun = false;
                _speed = _walkSpeed;
                _animator.SetBool("IsWalk", _isWalk);
                _animator.SetBool("IsRun", _isRun);
                _animator.SetFloat("moveSpeed", _speed);
                break;

            case PlayerState.Run:
                _isWalk = false;
                _isRun = true;
                _speed = _runSpeed;
                _animator.SetBool("IsWalk", _isWalk);
                _animator.SetBool("IsRun", _isRun);
                _animator.SetFloat("moveSpeed", _speed);
                break;

            case PlayerState.Attack:
                Attack();
                break;
        }
    }

    public void EndAttack()
    {
        _isAttack = false;

        Debug.Log($"_isAttack 2= {_isAttack}");
    }

 
    private void Attack()
    {
        if(_isAttack == false)
        {
            _isAttack = true;
            _animator.SetTrigger("Attack");
        }
    }

    // 전환이 끝나기 전에 트리거가 중복 발동하는 것을 방지 (TC 35).
    // 예전에는 씬이 바뀌면서 이 컴포넌트가 새로 만들어져 자동으로 초기화됐지만,
    // 이제 씬을 갈지 않으므로 전투가 끝날 때 BattleManager 가 명시적으로 풀어준다.
    private bool _encounterTriggered = false;

    /// <summary>이 시각까지는 조우를 받지 않는다. 전투 종료 직후의 잔여 겹침을 넘긴다.</summary>
    private float _encounterReadyTime = 0.0f;

    /// <summary>전투 종료 후 다시 조우할 수 있게 한다. BattleManager 가 호출한다.</summary>
    public void ResetEncounter()
    {
        _encounterTriggered = false;

        // 전투 직후에는 잠시 조우를 받지 않는다. 몬스터가 되살아나거나
        // 재생성되는 시점에 플레이어와 겹쳐 있으면 트리거가 즉시 다시 발동한다.
        _encounterReadyTime = Time.time + Mathf.Max(0.0f, _encounterGrace);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_encounterTriggered)
        {
            return;
        }

        if (Time.time < _encounterReadyTime)
        {
            return;
        }

        if (other.gameObject.CompareTag("Monster"))
        {
            if (GameFlow.IsField)
            {
                _encounterTriggered = true;

                if (BattleManager.BattleInstance != null)
                {
                    // 씬 전환이 아니라 이 자리에서 전투를 시작한다
                    BattleManager.BattleInstance.BeginEncounter(other.gameObject);
                }
                else
                {
                    Debug.LogError("[ActionController] BattleManager 가 없어 전투를 시작할 수 없습니다. " +
                                   "Field 씬에 BattleManager 오브젝트가 있는지 확인하세요.");
                    _encounterTriggered = false;
                }
            }

        }
    }

}

