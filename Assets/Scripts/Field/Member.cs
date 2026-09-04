using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Member : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _elementBackground;
    [SerializeField] private Image _MemberImage;

    private MemberBar _memberBar;

    public RectTransform _memberRect;

    public Character _characterComponent { get; set; }

    private Color[] _colorType = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.black,
        Color.gray
    };

    void Start()
    {
        _memberBar = GetComponentInParent<MemberBar>();
        _memberRect = GetComponent<RectTransform>();
    }

    void Update()
    {
    }

    public void MemberImageSet()
    {
        string _filePath = $"Image/{_characterComponent.Stat.Name}_head";

        Sprite sprite = Resources.Load<Sprite>(_filePath);
        _MemberImage.sprite = sprite;
    }

    public void _elementBackgroundSet()
    {
             
        //}
        //else if (_characterComponent.Stat.Element == "wind")
        //{
        //    _elementBackground.color = Color.green;
        //}
        //else if (_characterComponent.Stat.Element == "water")
        //{
        //    _elementBackground.color = Color.blue;
        //}

        Color c = _elementBackground.color;
        c.a = 120 / 255f;
        _elementBackground.color = c;

    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if(ViewManager.ViewInstance.CharacterViewIsActive())
        {
            Debug.Log($"_characterComponent = {_characterComponent}");
            ViewManager.ViewInstance.CharacterViewCharacterInfoSet(_characterComponent);
            ScaleReset(_memberBar.MemberList);
            this.GetComponent<RectTransform>().localScale = new Vector3(1.2f, 1.0f, 1.0f);
        }
    }

    public void MemberScaleReset()
    {
        _memberRect.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void ScaleReset(List<Member> _memberList)
    {
        for (int i = 0; i < _memberList.Count; i++) 
        {
            _memberList[i].MemberScaleReset();
        } 
    }

}




