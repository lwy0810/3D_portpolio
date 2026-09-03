using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

public class StatusManager : MonoBehaviour
{

    public static StatusManager StatusInstance;
    private Dictionary<string, Stat> _masterCharacterStats = new Dictionary<string, Stat>();    


    void Awake()
    {
        if (StatusInstance != null)
        {
            Destroy(gameObject);
            return;
        }

        StatusInstance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() {}

    void Update() {}    


    public bool HasCharacterStat(string name)
    {
        return _masterCharacterStats.ContainsKey(name);
    }

    public Stat GetCharacterStat(string name)
    {
        return _masterCharacterStats[name];
    }

    public void RegisterCharacterStat(string name, Stat stat)
    {
        _masterCharacterStats[name] = stat;
    }



}
