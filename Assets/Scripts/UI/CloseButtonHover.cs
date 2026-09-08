using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CloseButtonHover : MonoBehaviour, IPointerClickHandler
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject _parent = transform.parent.gameObject;
        _parent.gameObject.SetActive(false);

        ViewManager.ViewInstance.CommandBattleViewActive(ViewCategory.commandArea, true);


    }



}
