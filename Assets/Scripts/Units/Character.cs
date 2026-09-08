using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Character : Unit
{
    [SerializeField] private GameObject _weaponPrefab; // 캐릭터 장착 무기
    [SerializeField] private GameObject _rightHandPrefab; // 캐릭터 오른손
    [SerializeField] private GameObject _fieldMember; // 필드 멤버

    private GameObject weapon;


    void Start()
    {
        if (weapon == null)
        {
            weapon = Instantiate(_weaponPrefab, _rightHandPrefab.transform);
            weapon.transform.localRotation = Quaternion.Euler(-90.0f, 90.0f, -94.0f);
            weapon.transform.localPosition += new Vector3(-0.07f, 0.011f, 0.005f);
        }
    }



}









