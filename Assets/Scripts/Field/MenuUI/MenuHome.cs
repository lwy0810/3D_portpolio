using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuHome : MonoBehaviour
{
    [SerializeField] private GameObject _fieldMenuHome;

    [SerializeField] private GameObject _leftMoveObjcet;

    [SerializeField] private GameObject _characterImageView;

    private RectTransform _leftRectTransform;
    private CharacterImageView _characterImageViewComponent;

    private Vector2 _leftStartPos;
    private Vector2 _leftEndPos;

    private Vector2 _topStartPos;   
    private Vector2 _topEndPos;


    void Awake()
    {
        _characterImageViewComponent = _characterImageView.GetComponent<CharacterImageView>();
    }


    void Start()
    {
    }


    void Update()
    {  
    }

    
  

}
