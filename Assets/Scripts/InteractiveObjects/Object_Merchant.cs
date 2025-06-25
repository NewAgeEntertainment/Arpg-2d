using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_Merchant : Object_NPC, IInteractable
{
    public void Interact()
    {
        Debug.Log("Open merchant's shop!");
    }

    
}
