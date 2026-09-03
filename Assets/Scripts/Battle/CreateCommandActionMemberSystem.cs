using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.CanvasScaler;

public class CreateCommandActionMemberSystem : MonoBehaviour
{
    [SerializeField] private Image _memberImage;
    [SerializeField] private Image _background;
    [SerializeField] private GameObject _commandActionMemberPrefab;

    private List<GameObject> _actionMemberlist = new List<GameObject>();
    public List<string> _characterNames = new List<string>();

    public List<GameObject> ActionMemberlist { get => _actionMemberlist; }

    public List<string> CharacterNames { get => _characterNames; }

    private Color[] _colorType = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
    };


    // Start is called before the first frame update
    void Start()
    {
        //UnitCollect();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator CreateCommandActionMember(List<Unit> _units, Transform _actionMemberBarTransform)
    {
        for(int i = 0; i < _units.Count; i++)
        {
            Debug.Log($"Test {i} : {_units[i]}");
        }


        //for (int i = 0; i < _sortUnit.Count; i++)
        //{
        //    //Debug.Log($"{i} = {_sortUnit[i]}");www
        //    CommandActionMemberAdd(_sortUnit[i], _actionMember, _actionMemberBarTransform);
        //    yield return new WaitForSeconds(0.1f);
        //    ActionMemberPositionUp(_actionMemberlist[i], i);
        //    yield return new WaitForSeconds(0.2f);
        //}
        for (int i = 0; i < _units.Count; i++)
        {
            CommandActionMemberAdd(_units[i], _actionMemberBarTransform);
            StartCoroutine(FadeIn(_actionMemberlist[i]));
            _actionMemberlist[i].GetComponent<RectTransform>().localPosition += new Vector3(90f, i * -70f, 0f);
            _actionMemberlist[i].GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 0.8f);
            StartCoroutine(ActionMemberPositionUp(_actionMemberlist[i]));
            yield return new WaitForSeconds(0.15f);
        }   
    }


    public void CommandActionMemberAdd(Unit _unit, Transform _actionMemberBarTransform)
    {
        if (BattleManager.BattleInstance.TurnOff == true)
        {
            //FadeOut(_actionMemberlist[0]);
            Destroy(_actionMemberlist[0]);
            _actionMemberlist.RemoveAt(0);
            _characterNames.RemoveAt(0);
        }

        Debug.Log($"_unit = {_unit}");
        //GameObject actionMember = Instantiate(_actionMember, _actionMemberBarTransform);
        GameObject actionMember = Instantiate(_commandActionMemberPrefab, _actionMemberBarTransform);

        actionMember.name = _unit.Stat.Name;

        _characterNames.Add(_unit.Stat.Name);
        _actionMemberlist.Add(actionMember);

        ImageReset(_unit, actionMember);
        _actionMemberlist[0].GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 0.8f);
    }


    public void CommandActionMemberRemove(Unit _units)
    {

    }

    private void ImageReset(Unit _unit, GameObject _actionMember)
    {
        //Debug.Log(_unit);
        string filePath = null;
        filePath = $"Image/{_unit.Stat.Name}_head";
        //Debug.Log(filePath);

        Sprite sprite = Resources.Load<Sprite>(filePath);
        //Debug.Log(sprite);
        Image[] images = _actionMember.GetComponentsInChildren<Image>();

        foreach (var img in images)
        {
            if (img.name == "MemberImage")
            {
                img.sprite = sprite;
            }

            if (img.name == "ElementBackground")
            {
                if (_unit.Stat.Element == "fire")
                {
                    img.color = Color.red;
                }
                else if (_unit.Stat.Element == "wind")
                {
                    img.color = Color.green;
                }
                else if (_unit.Stat.Element == "water")
                {
                    img.color = Color.blue;
                }

                Color c = img.color;
                c.a = 120 / 255f;
                img.color = c;
            }
        }
    }

    //public void ScaleUp(GameObject _actionMember)
    //{
    //    _actionMember.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 0.8f);
    //}


    void FadeOut(GameObject _actionMember) 
    {
        float duration = 0.2f; // 페이드 아웃 지속 시간
        float time = 0f;

        CanvasGroup _CanvasGroup = _actionMember.GetComponent<CanvasGroup>();

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time / duration);
            _CanvasGroup.alpha = alpha;
        }
    }

    IEnumerator FadeIn(GameObject _actionMember)
    {
        float duration = 0.2f; // 페이드 아웃 지속 시간
        float time = 0f;

        CanvasGroup _CanvasGroup = _actionMember.GetComponent<CanvasGroup>();

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, time / duration);
            _CanvasGroup.alpha = alpha;

            yield return null;
        }

        _CanvasGroup.alpha = 1f;

    }



    IEnumerator ActionMemberPositionUp(GameObject _actionMember)
    {
        float time = 0.0f;
        float duration = 2.0f;


        while(time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            //_actionMember.GetComponent<RectTransform>().localPosition.y = Mathf.Lerp(t, 0f, t);
            yield return null;
        }  


    }




}
