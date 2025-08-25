#if PIXELCRUSHERS
using System.Linq;
using UnityEngine;
using PixelCrushers;

public class CompanionPartySaver : Saver
{
    [System.Serializable]
    private class Data
    {
        public string[] activeIds;
        public Data() { }
        public Data(string[] ids) { activeIds = ids; }
    }

    public override string RecordData()
    {
        var mgr = CompanionPartyManager.Instance;
        if (mgr == null) return string.Empty;
        return SaveSystem.Serialize(new Data(mgr.ActiveIds().ToArray()));
    }

    public override void ApplyData(string s)
    {
        var mgr = CompanionPartyManager.Instance;
        if (mgr == null || string.IsNullOrEmpty(s)) return;

        var data = SaveSystem.Deserialize<Data>(s);
        if (data == null) return;

        mgr.DismissAll(destroy: true);
        if (data.activeIds != null)
        {
            foreach (var id in data.activeIds)
                mgr.Recruit(id, silent: true);
        }
    }
}
#endif
