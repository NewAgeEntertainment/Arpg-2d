using System;
using UnityEngine;

[Serializable]
public class Stat
{
    [SerializeField] private float baseValue; // Base value of the stat

    public float GetValue()
    {
        return baseValue; // Returns the base value of the stat
    }

    // buff or items affecting base vlaue
    // all calculations should be done here
}
