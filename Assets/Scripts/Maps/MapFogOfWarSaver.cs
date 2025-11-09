using UnityEngine;

namespace PixelCrushers
{
    [AddComponentMenu("Saving/Map Fog Of War Saver")]
    public class MapFogOfWarSaver : Saver
    {
        public MapFogOfWar fog;

        [System.Serializable]
        public class FogData
        {
            public string pngBase64; // compact & lossless
        }

        public override string RecordData()
        {
            if (!fog) fog = GetComponent<MapFogOfWar>();
            var data = new FogData();
            data.pngBase64 = System.Convert.ToBase64String(fog.GetPngBytes());
            return SaveSystem.Serialize(data);
        }

        public override void ApplyData(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            if (!fog) fog = GetComponent<MapFogOfWar>();
            var data = SaveSystem.Deserialize<FogData>(s);
            if (data == null || string.IsNullOrEmpty(data.pngBase64)) return;
            var bytes = System.Convert.FromBase64String(data.pngBase64);
            fog.InitTexture(bytes);  // rebuild from saved bytes
        }
    }
}
