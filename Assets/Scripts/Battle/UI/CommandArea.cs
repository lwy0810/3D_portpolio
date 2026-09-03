using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;

public class CommandArea : MonoBehaviour
{
    [SerializeField] private Transform _startPos;
    [SerializeField] private Transform _endPos;

    [SerializeField] private GameObject _attackButton;
    [SerializeField] private GameObject _skillButton;
    [SerializeField] private GameObject _instrumentButton;
    [SerializeField] private GameObject _retreatButton;

    void Start()
    {

    }
   

    // Update is called once per frame
    void Update()
    {
    }
}
