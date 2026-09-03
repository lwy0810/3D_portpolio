using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ViewCategory
{
    actionBar,
    commandArea,
    commandMemberBar,
    detailView,
    infoView,
    instrumentSelectView,
    skillSelectView,
    targetView,
    MemuButtontInfoBar
};

public class CommandBattleView : MonoBehaviour
{
    [SerializeField] private ActionBar _actionBar;
    [SerializeField] private CommandArea _commandArea;
    [SerializeField] private CommandMemberBar _commandMemberBar;
    [SerializeField] private DetailView _detailView;
    [SerializeField] private InfoView _infoView;
    [SerializeField] private InstrumentSelectView _instrumentSelectView;
    [SerializeField] private SkillSelectView _skillSelectView;
    [SerializeField] private TargetView _targetView;
    [SerializeField] private MemuButtontInfoBar _memuButtontInfoBar;


    private Transform _target;
    private Camera _cam;


    public void ViewShow(ViewCategory _viewCategory, bool active)
    {
        switch(_viewCategory)
        {
            case ViewCategory.actionBar:
                _actionBar.gameObject.SetActive(active);
                break;
            case ViewCategory.commandArea:
                _commandArea.gameObject.SetActive(active);
                break;
            case ViewCategory.commandMemberBar:
                _commandMemberBar.gameObject.SetActive(active);
                break;
            case ViewCategory.detailView:
                _detailView.gameObject.SetActive(active);
                break;
            case ViewCategory.infoView:
                _infoView.gameObject.SetActive(active);
                break;
            case ViewCategory.instrumentSelectView:
                _instrumentSelectView.gameObject.SetActive(active);
                break;
            case ViewCategory.skillSelectView:
                _skillSelectView.gameObject.SetActive(active);
                break;
            case ViewCategory.targetView:
                _targetView.gameObject.SetActive(active);
                break;
            case ViewCategory.MemuButtontInfoBar:
                _memuButtontInfoBar.gameObject.SetActive(active);
                break;

        }

    }
   
    public Vector3 CommandAreaPosSet(Vector3 _lookPos)
    {
        _cam = Camera.main;

        //Debug.Log($"Name = {GameManager.GameInstance.Units[0]}");
        //Debug.Log($"Name = {GameManager.GameInstance.Characters[0]}");
        //Debug.Log($"position = {GameManager.GameInstance.Units[0].transform.position}");
        
        Vector3 screenPos = _cam.WorldToScreenPoint(_lookPos + Vector3.right * 1.2f);

        _commandArea.transform.position = screenPos;

        //Debug.Log($"characterscreenPos = {screenPos}");

        return screenPos;
    }

    public Vector3 TargetViewPosSet(int index)
    {
        _cam = Camera.main;

        Vector3 screenPos = _cam.WorldToScreenPoint(GameManager.GameInstance.Monsters[index].transform.position + Vector3.up * 4.6f);
        _targetView.transform.position = screenPos;

        //Debug.Log($"monsterscreenPos = {screenPos}");

        return screenPos;
    }

    public void MonsterInfoSet(int index)
    {
        _targetView.MonsterInfoSet(index);
    }



}
