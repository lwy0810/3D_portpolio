using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    [SerializeField] private Image _dimBackground;
    [SerializeField] private Image _selectBackground;

    public bool IsSelect { get; private set; }

    public void SelectActive()
    {
        float duration = 0.5f;

        //IsSelect = true;

        while (true)
        {
            float time = 0f;
            time += Time.deltaTime;

            if(time < duration)
            {
                float alpha = Mathf.Lerp(0.0f, (30.0f / 255.0f), (time / duration));

                Color color = _dimBackground.color;
                color.a =  alpha;
                _dimBackground.color = color;
            }

            if (time < duration)
            {
                time = 0f;
                time += Time.deltaTime;

                float alpha = Mathf.Lerp((30.0f / 255.0f), 0.0f, (time / duration));

                Color color = _dimBackground.color;
                color.a = alpha;
                _dimBackground.color = color;
            }

        }

    }

    public void SelectBackgroundShow()
    {
        _selectBackground.gameObject.SetActive(true);
        //_isActive = true;
    }

    public void SelectBackgroundUnShow()
    {
        //_isActive = false;
        _selectBackground.gameObject.SetActive(false);
        
    }








}
