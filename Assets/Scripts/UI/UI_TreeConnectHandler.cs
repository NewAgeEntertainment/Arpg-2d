using System;
using UnityEngine;


[Serializable]
public class UI_TreeConnectionDetails
{
    // direction type
    public NodeDirectionType direction;
    // length of the connection
    [Range(100f, 350f)] public float length;
}
public class UI_TreeConnectHandler : MonoBehaviour
{
    [SerializeField] private UI_TreeConnectionDetails[] Details;
    [SerializeField] private UI_TreeConnection[] connection;
}
