// Assets/Scripts/Title/SaveUtility.cs
using PixelCrushers;
using UnityEngine;

public static class SaveUtility
{
    public static void DeleteAllSlots()
    {
        for (int i = 0; i <= SaveSystem.maxSaveSlot; i++)
        {
            if (SaveSystem.HasSavedGameInSlot(i))
                SaveSystem.DeleteSavedGameInSlot(i);
        }
        PlayerPrefs.DeleteKey(SaveSystem.LastSavedGameSlotPlayerPrefsKey);
        PlayerPrefs.Save();
        if (SaveSystem.debug) Debug.Log("[SaveUtility] Deleted all save slots.");
    }
}
