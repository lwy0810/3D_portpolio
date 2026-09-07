using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FieldMenuView : MonoBehaviour
{
    [SerializeField] private GameObject _homeView;
    [SerializeField] private GameObject[] _menuViewes;

    [SerializeField] private GameObject _bottomTopBox;

    [SerializeField] private UIBox _uiBox;

    [SerializeField] private GameObject _homeUIButtonBox;
    [SerializeField] private GameObject _homeUIButtonSubBox;

    [SerializeField] private GameObject _bottomLineReverse;

    [SerializeField] private GameObject _backGround;
    [SerializeField] private Image _semiBackground_Left;
    [SerializeField] private Image _semiBackground_Right;

    [SerializeField] private GameObject _topMove;


    private List<CanvasGroup> _canvasGroups = new List<CanvasGroup>();
    private List<RectTransform> _rectTransforms = new List<RectTransform>();
    private List<Vector2> _anchoredPositions = new List<Vector2>();

    private Vector2 _homeVec;
    private Vector2 _statusVec;
    private Vector2 _equipVec;
    private Vector2 _itemVec;
    private Vector2 _systemVec;

    private Vector2 _startPos;
    private Vector2 _endPos;

    private MenuHome _menuHome;


    public bool IsMainShow { get; set; } = true;

    private CanvasGroup _homeViewCanvasGroup;

    private CanvasGroup _bottomTopBoxCanvasGroup;
    private CanvasGroup _homeUIButtonBoxCanvasGroup;
    private CanvasGroup _homeUIButtonSubBoxCanvasGroup;


    public GameObject[] HomeMenuView { get => _menuViewes; }
    //public int MoveButtonIndex { get => _moveButtonIndex; set => _moveButtonIndex = value; }
    //public int SelectedViewIndex { get => _selectedViewIndex; set => _selectedViewIndex = value; }


    void Awake()
    {
        _homeViewCanvasGroup = _homeView.GetComponent<CanvasGroup>();

        _bottomTopBoxCanvasGroup = _bottomTopBox.GetComponent<CanvasGroup>();
        _homeUIButtonBoxCanvasGroup = _homeUIButtonBox.GetComponent<CanvasGroup>();
        _homeUIButtonSubBoxCanvasGroup = _homeUIButtonSubBox.GetComponent<CanvasGroup>();

        for (int i = 0; i < _menuViewes.Length; i++)
        {
            CanvasGroup canvasGroup = _menuViewes[i].GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                _canvasGroups.Add(canvasGroup);
            }

            RectTransform rectTransform = _menuViewes[i].GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                _rectTransforms.Add(rectTransform);
            }

            _anchoredPositions.Add(rectTransform.anchoredPosition);
        }


    }


    void Start()
    {

    }

    void Update()
    {
        ViewConvert();
    }

    public void FieldMenuViewShow()
    {
        this.GetComponent<CanvasGroup>().alpha = 1f;
        StartCoroutine(LeftRightMove(_homeView, 0f));
        StartCoroutine(UpDownMove(_topMove, 0f));

    }

    public void FieldMenuViewUnShow()
    {
        this.GetComponent<CanvasGroup>().alpha = 0f;
        StartCoroutine(LeftRightMove(_homeView, 200.0f));
        StartCoroutine(UpDownMove(_topMove, -350.0f));
    }



    public IEnumerator LeftRightMove(GameObject view, float endX)
    {
        RectTransform _viewRectTransform = view.GetComponent<RectTransform>();

        Vector2 _leftStartPos = _viewRectTransform.anchoredPosition;
        Vector2 _leftEndPos = new Vector2(endX, _leftStartPos.y);

        Debug.Log($"StartPos: {_leftStartPos}, EndPos: {_leftEndPos}");

        float time = 0f;
        float duration = 0.15f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            _viewRectTransform.anchoredPosition = Vector2.Lerp(_leftStartPos, _leftEndPos, t);

            yield return null;

        }

        _viewRectTransform.anchoredPosition = _leftEndPos;
        Debug.Log("LeftMove");

    }


    public IEnumerator UpDownMove(GameObject view, float endY)
    {
        RectTransform _viewRectTransform = view.GetComponent<RectTransform>();

        Vector2 _topStartPos = _viewRectTransform.anchoredPosition;
        Vector2 _topEndPos = new Vector2(_topStartPos.x, endY);

        Debug.Log($"StartPos: {_topStartPos}, EndPos: {_topEndPos}");

        float time = 0f;
        float duration = 0.1f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            _viewRectTransform.anchoredPosition = Vector2.Lerp(_topStartPos, _topEndPos, t);

            yield return null;

        }

        _viewRectTransform.anchoredPosition = _topEndPos;
        Debug.Log("TopMove");

    }

    public void BottomTopBoxInvisible(GameObject obj)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    public void BottomTopBoxVisible(GameObject obj)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
    }

    void ViewConvert()
    {
        if(ViewManager.ViewInstance.IsMenuActive == true)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (IsMainShow)
                {
                    Debug.Log("Enter");
                    _homeViewCanvasGroup.alpha = 0f;
                    _canvasGroups[_uiBox.CountIndex].alpha = 1f;

                    _bottomTopBoxCanvasGroup.alpha = 0f;
                    _homeUIButtonBoxCanvasGroup.alpha = 0f;
                    _homeUIButtonSubBoxCanvasGroup.alpha = 1f;

                    _uiBox.UIButtonsShow(_uiBox.CountIndex);

                    StartCoroutine(LeftRightMove(_menuViewes[_uiBox.CountIndex], 0f));
                    StartCoroutine(LeftRightMove(_homeView, 200f));

                    StartCoroutine(UpDownMove(_homeUIButtonBox, -650.0f));
                    StartCoroutine(UpDownMove(_bottomTopBox, -370.0f));
                    StartCoroutine(UpDownMove(_homeUIButtonSubBox, -420.0f));

                    StartCoroutine(UpDownMove(_bottomLineReverse, -390.0f));
                    IsMainShow = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!IsMainShow)
                {
                    Debug.Log("Escape");
                    _canvasGroups[_uiBox.CountIndex].alpha = 0f;
                    _homeViewCanvasGroup.alpha = 1f;

                    _bottomTopBoxCanvasGroup.alpha = 1f;
                    _homeUIButtonBoxCanvasGroup.alpha = 1f;
                    _homeUIButtonSubBoxCanvasGroup.alpha = 0f;

                    StartCoroutine(LeftRightMove(_menuViewes[_uiBox.CountIndex], 200f));
                    StartCoroutine(LeftRightMove(_homeView, 0f));

                    StartCoroutine(UpDownMove(_homeUIButtonBox, -400.0f));
                    StartCoroutine(UpDownMove(_bottomTopBox, 0f));
                    StartCoroutine(UpDownMove(_homeUIButtonSubBox, -540.0f));
                    StartCoroutine(UpDownMove(_bottomLineReverse, -670.0f));

                    IsMainShow = true;
                }
                else
                {
                    FieldMenuViewUnShow();
                    ViewManager.ViewInstance.IsMenuActive = false;
                    if (this._homeView.transform.position.x < 200f)
                    {
                        Debug.Log(this._homeView.transform.position.x);
                    }
                    ViewManager.ViewInstance.KeyInfoBarShow();



                }
            }
        }

    }
}




