using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIView : MonoBehaviour
{

    protected virtual void Show() 
    {
        this.gameObject.SetActive(true);
    }

    protected virtual void UnShow() 
    {
        this.gameObject.SetActive(false);
    }
}
