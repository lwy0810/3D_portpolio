using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private Stat _stat;
    public Stat Stat { get => _stat; }
    public void SetStat(Stat stat)
    {
        _stat = stat;
    }


    void Awake()
    {
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
