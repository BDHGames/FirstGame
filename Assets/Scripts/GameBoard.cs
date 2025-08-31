using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GameBoard : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (int i in new IntIterator(0, 3))
        {
            Debug.Log(i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
