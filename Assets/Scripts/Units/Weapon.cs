using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log(1);
        if (other.gameObject.CompareTag("Monster"))
        {
            Debug.Log(2);
        }
    }





    //private void OnCollisionEnter(Collision collision)
    //{
    //    //Debug.Log(1);
    //    if (collision.gameObject.CompareTag("Monster")) 
    //    {
    //        Debug.Log(2);
    //    }
    //}



}


