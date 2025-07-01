using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item List", fileName = "List of Items - ")]

public class ItemListDataSO : ScriptableObject
{
    public ItemDataSO[] itemList;
}
