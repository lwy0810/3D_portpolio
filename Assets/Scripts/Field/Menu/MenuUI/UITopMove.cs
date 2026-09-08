using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITopMove : MonoBehaviour
{
    
    private RectTransform _rectTransform;
    private Vector2 vec;
    private float _originY = 120.0f;
    private float _TopY = -120.0f;
    private Vector2 _startPos;
    private Vector2 _endPos;


    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        vec = _rectTransform.anchoredPosition;
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnEnable()
    {
        StartCoroutine(TopMove(_originY, _TopY));
    }

    public void OnDisable()
    {
        StartCoroutine(TopMove(_TopY, _originY));

    }


    public IEnumerator TopMove(float startY, float endY)
    {
        _startPos = _rectTransform.anchoredPosition;
        _endPos = new Vector2(_startPos.x, endY);

        float time = 0f;
        float duration = 0.5f;

        while (time < duration) 
        {
            time += Time.deltaTime;
            float t = time / duration;

            _rectTransform.anchoredPosition = Vector2.Lerp(_startPos, _endPos, t);

            yield return null;

        }

        _rectTransform.anchoredPosition = _endPos;

    }












}
