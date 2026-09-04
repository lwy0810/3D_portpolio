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
        if(ViewManager.ViewInstance.IsMenuActive == false)
        {
            if (SceneManager.GetActiveScene().name == "Field")
            {
                CharacterMove();
            }
            else if (SceneManager.GetActiveScene().name == "CommandBattle")
            {
            }
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
            if (SceneManager.GetActiveScene().name == "Field")
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

    private bool _encounterTriggered = false; // 씬 전환이 끝나기 전에 트리거가 중복 발동하는 것을 방지 (TC 35)

    private void OnTriggerEnter(Collider other)
    {
        if (_encounterTriggered)
        {
            return;
        }

        if (other.gameObject.CompareTag("Monster"))
        {
            if (SceneManager.GetActiveScene().name == "Field")
            {
                _encounterTriggered = true;
                SceneManager.LoadScene("CommandBattle");
            }

        }
    }

}

