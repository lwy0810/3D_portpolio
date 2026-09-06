using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : Unit
{
    [SerializeField] private GameObject _targetArea;

    private Color _targetAreaColor;
    private SpriteRenderer area;

    private float _targetAreaBaseY;
    private bool _targetAreaBaseCaptured = false;

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

    /// <summary>
    /// 타깃 표시 원판을 지면에서 띄운다.
    ///
    /// 예전 전투 전용 씬에서는 Plane 하나가 바닥이어서 프리팹의 기본 높이로 충분했다.
    /// 인플레이스 전투는 지형 위에서 벌어지고, 대열을 hit.point 에 정확히 맞추므로
    /// 원판이 지면과 같은 높이가 되어 일부가 파묻힌다.
    ///
    /// 프리팹의 원래 높이를 기준으로 더하므로 여러 번 불러도 누적되지 않는다.
    /// </summary>
    public void LiftTargetArea(float lift)
    {
        if (_targetArea == null) return;

        if (!_targetAreaBaseCaptured)
        {
            _targetAreaBaseY = _targetArea.transform.localPosition.y;
            _targetAreaBaseCaptured = true;
        }

        Vector3 _p = _targetArea.transform.localPosition;
        _p.y = _targetAreaBaseY + lift;
        _targetArea.transform.localPosition = _p;
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
