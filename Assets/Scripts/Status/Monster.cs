using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : Unit
{
    [SerializeField] private GameObject _targetArea;

    private Color _targetAreaColor;
    private SpriteRenderer area;

    void Start()
    {
        area = _targetArea.GetComponent<SpriteRenderer>();

        _targetAreaColor = area.color;
        _targetAreaColor.a = 0f;
        area.color = _targetAreaColor;
    }


    // Update is called once per frame
    void Update()
    {
    }

    public void TargetAreaShow()
    {
        _targetAreaColor.a = 80 / 255f;
        area.color = _targetAreaColor;
    }

    public void TargetAreaUnShow()
    {
        _targetAreaColor.a = 0f;
        area.color = _targetAreaColor;
    }



}
