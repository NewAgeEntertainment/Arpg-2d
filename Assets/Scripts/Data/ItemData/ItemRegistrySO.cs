using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "RPG Setup/Item Registry")]
public class ItemRegistrySO : ScriptableObject
{
    [Tooltip("Drag ALL ItemDataSO assets here (consumables, weapons, armor, materials, etc).")]
    public List<ItemDataSO> allItems = new List<ItemDataSO>();

    private Dictionary<string, ItemDataSO> _byGuid;

    public ItemDataSO GetByGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;
        EnsureIndex();
        return _byGuid.TryGetValue(guid, out var item) ? item : null;
    }

    private void EnsureIndex()
    {
        if (_byGuid != null) return;
        _byGuid = new Dictionary<string, ItemDataSO>(allItems.Count);
        foreach (var item in allItems)
        {
            if (item == null) continue;
            var gid = item.Guid;
            if (!string.IsNullOrEmpty(gid))
            {
                if (_byGuid.ContainsKey(gid) == false)
                    _byGuid.Add(gid, item);
                else
                    Debug.LogWarning($"[ItemRegistry] Duplicate GUID detected: {gid} on {item.name}");
            }
            else
            {
                Debug.LogWarning($"[ItemRegistry] Item {item.name} has empty GUID. Open it once so it assigns.");
            }
        }
    }

    public static ItemRegistrySO Load()
    {
        // Must live at Assets/Resources/Items/ItemRegistry.asset
        var reg = Resources.Load<ItemRegistrySO>("Items/ItemRegistry");
        if (reg == null) Debug.LogError("[ItemRegistry] Could not find Resources/Items/ItemRegistry.asset");
        return reg;
    }
}
