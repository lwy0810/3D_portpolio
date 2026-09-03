using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionPopUps : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private float duration = 0.3f;


    IEnumerator FadeIn()
    {
        float time = 0.0f;
        _canvasGroup.alpha = 0.0f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        while ( time < duration)
        {
            time += Time.deltaTime;
            _canvasGroup.alpha = ( time / duration );
            yield return null;
        }

        _canvasGroup.alpha = 1.0f;
    }

    public void Show()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        this.gameObject.SetActive(true);
        StartCoroutine(FadeIn());
    }

    public void UnShow()
    {
        this.gameObject.SetActive(false);
    }

}
