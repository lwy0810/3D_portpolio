using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private Camera _MapCamera;
    [SerializeField] private GameObject _PlayerIcon;
    private Vector3 _playerPos;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        _playerPos = GameManager.GameInstance.Characters[0].transform.position;

        _MapCamera.transform.position = new Vector3(_playerPos.x, (_playerPos.y + 200.0f), _playerPos.z);
        _PlayerIcon.transform.position = new Vector3(_playerPos.x, (_playerPos.y + 190.0f), _playerPos.z);

    }
}
