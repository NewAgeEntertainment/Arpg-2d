using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CraftPreviewSlot : MonoBehaviour
{
    [SerializeField] private Image materialIcon;
    [SerializeField] private TextMeshProUGUI materialNameValue;

    public void SetupPreviewSlot(ItemDataSO itemData, int availiableAmount, int requiredAmount)
    {
        materialIcon.sprite = itemData.itemIcon; // Set the icon of the material
        materialNameValue.text = itemData.itemName + " - " + availiableAmount + "/" + requiredAmount; // Set the name and available/required amount of the material
    }
    

    
}
