using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject[] _characterPrefabs;
    public GameObject _monsterPrefabs;

    [SerializeField] private UnitCreateSystem _unitCreateSystem;

    public static GameManager GameInstance;

    public List<GameObject> Characters = new List<GameObject>();
    public List<GameObject> Monsters = new List<GameObject>();

    public List<Character> CharacterComponents = new List<Character>();
    public List<Monster> MonsterComponents = new List<Monster>();

    public GameObject Monster { set; get; }

    private List<Unit> _units = new List<Unit>();

    public List<Unit> Units { 
        set
        {
            _units = value;
        }
        get
        {
            return _units;
        } 
    }

    public bool IsInit { set; get; } = true;

    void Awake()
    {
        if (GameInstance != null)
        {
            Destroy(gameObject);
            return;
        }

        GameInstance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "Field" || scene.name == "CommandBattle")
        {
            if (Characters != null)
            {
                Characters.Clear();
                CharacterComponents.Clear();
            }

            if (Monsters != null)
            {
                Monsters.Clear();
                MonsterComponents.Clear();
            }

            _unitCreateSystem.CharacterCreate(_characterPrefabs);
            _unitCreateSystem.MonsterCreate(_monsterPrefabs);

            IsInit = false;
        }
    }








}
