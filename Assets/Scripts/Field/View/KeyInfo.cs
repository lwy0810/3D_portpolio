using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyInfo : MonoBehaviour
{
    [SerializeField] private Image _FrontWKey;
    [SerializeField] private Image _BackSKey;
    [SerializeField] private Image _LeftAKey;
    [SerializeField] private Image _RightDKey;
    [SerializeField] private Image _MouseLeftKey;
    [SerializeField] private Image _MouseRightKey;

    private Color _originColor = new Color(255 / 255f, 255 / 255f, 255 / 255f);


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            _FrontWKey.color = new Color(217 / 255f, 40 / 255f, 40 / 255f);
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            _FrontWKey.color = _originColor;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            _BackSKey.color = new Color(217 / 255f, 40 / 255f, 40 / 255f);
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            _BackSKey.color = _originColor;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            _LeftAKey.color = new Color(217 / 255f, 40 / 255f, 40 / 255f);
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            _LeftAKey.color = _originColor;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            _RightDKey.color = new Color(217 / 255f, 40 / 255f, 40 / 255f);
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            _RightDKey.color = _originColor;
        }

        if (Input.GetMouseButtonDown(0))
        {
            _MouseLeftKey.color = new Color(217 / 255f, 40 / 255f, 40 / 255f);
        }
        if (Input.GetMouseButtonUp(0))
        {
            _MouseLeftKey.color = _originColor;
        }


        if (Input.GetMouseButtonDown(1))
        {
            _MouseRightKey.color = new Color(217 / 255f, 40 / 255f, 40 / 255f);
        }
        if (Input.GetMouseButtonUp(1))
        {
            _MouseRightKey.color = _originColor;
        }

    }

    public void MouseLeftButtonColorReset()
    {
        _MouseLeftKey.color = _originColor;
    }



}
