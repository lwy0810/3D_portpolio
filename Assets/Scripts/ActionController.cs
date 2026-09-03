using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ActionController : MonoBehaviour
{
    [SerializeField] private int _speed2 = 1; // 캐릭터의 이동속도 배수

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
        
        if (_characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 바닥 붙이기
        }
        else
        {
            velocity.y += -9.8f * Time.deltaTime;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direct = new Vector3(horizontal, 0.0f, vertical);

        Vector3 move = direct * _speed * _speed2;
        move.y = velocity.y;

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

                //Debug.Log($"direct = {direct}");
                //Debug.Log($"move = {move}");
                _characterController.Move(move * Time.deltaTime);
                this.transform.rotation = Quaternion.LookRotation(new Vector3(horizontal, 0.0f, vertical));

            }
        }
        else
        {
            CurrentPlayerState = PlayerState.Idle;
        }

        if (Input.GetMouseButtonDown(0) && !_isAttack)
        {
            if( SceneManager.GetActiveScene().name == "Field")
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

        switch (CurrentPlayerState)
        {
            case PlayerState.Idle:
                _animator.SetFloat("moveSpeed", 0.0f);
                break;
            case PlayerState.Walk:
                _isWalk = true;
                _isRun = false;
                _speed = 2.0f;
                _animator.SetBool("IsWalk", _isWalk);
                _animator.SetBool("IsRun", _isRun);
                _animator.SetFloat("moveSpeed", _speed);
                break;
            case PlayerState.Run:
                _isWalk = false;
                _isRun = true;
                _speed = 5.0f;
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

