using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    //[SerializeField] private GameObject _player;

    private GameObject _character;

    void Start()
    {
        this.transform.position = Vector3.zero;
        _character = GameManager.GameInstance.Characters[0];

    }

    void Update()
    {
        this.transform.position = _character.transform.position + new Vector3(0.0f, 10.0f, -7.0f);

        Vector3 vec = _character.transform.position - this.transform.position;
        this.transform.rotation = Quaternion.LookRotation(vec);

    }
}
