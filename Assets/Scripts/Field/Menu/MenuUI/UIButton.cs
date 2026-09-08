using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    [SerializeField] private Image _dimBackground;
    [SerializeField] private Image _selectBackground;

    public bool IsSelect { get; private set; }

    // 예전에는 이 메서드가 코루틴이 아닌 일반 메서드에 while(true) 를 그대로 써서,
    // StartCoroutine 없이 직접 호출되는 순간(또는 실수로 코루틴으로 착각해 연결되는 순간)
    // 무한 루프에 걸려 에디터가 그대로 멈추는 지뢰였다. 지금은 실제 코루틴으로 만들고
    // 0 -> 목표 알파 -> 0 으로 한 번만 깜빡이도록 정리했다.
    public void SelectActive()
    {
        StartCoroutine(SelectActiveRoutine());
    }

    private IEnumerator SelectActiveRoutine()
    {
        if (_dimBackground == null) yield break;

        float duration = 0.5f;
        float targetAlpha = 30.0f / 255.0f;

        yield return FadeDimBackground(0f, targetAlpha, duration);
        yield return FadeDimBackground(targetAlpha, 0f, duration);
    }

    private IEnumerator FadeDimBackground(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            Color color = _dimBackground.color;
            color.a = Mathf.Lerp(from, to, time / duration);
            _dimBackground.color = color;

            yield return null;
        }

        Color finalColor = _dimBackground.color;
        finalColor.a = to;
        _dimBackground.color = finalColor;
    }

    public void SelectBackgroundShow()
    {
        _selectBackground.gameObject.SetActive(true);
    }

    public void SelectBackgroundUnShow()
    {
        _selectBackground.gameObject.SetActive(false);
    }

}
