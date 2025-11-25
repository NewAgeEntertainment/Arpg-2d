using UnityEngine;
using System;

public class LogWhenEnabled : MonoBehaviour
{
    void OnEnable()
    {
        Debug.Log($"{name} was enabled", this);
        Debug.Log(Environment.StackTrace);   // shows who called SetActive / enabled it
    }
}

