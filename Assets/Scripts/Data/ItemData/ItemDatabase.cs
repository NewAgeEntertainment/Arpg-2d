using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Database/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDataSO> allItems = new List<ItemDataSO>();

    private Dictionary<string, ItemDataSO> byGuid;
    private Dictionary<string, ItemDataSO> byName;

    public void Init()
    {
        if (byGuid != null) return;

        byGuid = new Dictionary<string, ItemDataSO>(allItems.Count);
        byName = new Dictionary<string, ItemDataSO>(allItems.Count);

        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (!string.IsNullOrEmpty(item.Guid) && !byGuid.ContainsKey(item.Guid))
                byGuid.Add(item.Guid, item);
            if (!string.IsNullOrEmpty(item.name) && !byName.ContainsKey(item.name))
                byName.Add(item.name, item);
        }
    }

    public bool TryGetByGuid(string guid, out ItemDataSO data)
    {
        Init();
        return byGuid.TryGetValue(guid, out data);
    }

    public bool TryGetByName(string assetName, out ItemDataSO data)
    {
        Init();
        return byName.TryGetValue(assetName, out data);
    }
}
