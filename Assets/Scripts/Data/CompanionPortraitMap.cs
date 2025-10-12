using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Party/Companion Portrait Map", fileName = "CompanionPortraitMap")]
public class CompanionPortraitMap : ScriptableObject
{
    [Serializable]
    public class Entry { public string id; public Sprite portrait; }

    public List<Entry> entries = new();

    private Dictionary<string, Sprite> _cache;

    public Sprite Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (_cache == null)
        {
            _cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (var e in entries)
            {
                if (e != null && !string.IsNullOrEmpty(e.id))
                    _cache[e.id] = e.portrait;
            }
        }

        return _cache.TryGetValue(id, out var s) ? s : null;
    }
}
