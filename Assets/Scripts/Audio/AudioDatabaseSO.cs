using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Database")]
public class AudioDatabaseSO : ScriptableObject
{
    public List<AudioClipData> player;
    public List<AudioClipData> enemy;
    public List<AudioClipData> sexytime;
    public List<AudioClipData> chest;
    public List<AudioClipData> items_Audio;
    public List<AudioClipData> uiAudio;

    [Header("Music Lists")]
    public List<AudioClipData> mainMenuMusic;
    public List<AudioClipData> levelMusic;

    private Dictionary<string, AudioClipData> clipCollection;

    private void OnEnable()
    {
        RebuildDatabase();
    }

    public void RebuildDatabase()
    {
        clipCollection = new Dictionary<string, AudioClipData>();

        AddToCollection(player);
        AddToCollection(enemy);
        AddToCollection(sexytime);
        AddToCollection(items_Audio);
        AddToCollection(chest);
        AddToCollection(uiAudio);
        AddToCollection(mainMenuMusic);
        AddToCollection(levelMusic);
    }

    public AudioClipData Get(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return null;

        if (clipCollection == null)
            RebuildDatabase();

        return clipCollection.TryGetValue(groupName, out var data) ? data : null;
    }

    private void AddToCollection(List<AudioClipData> listToAdd)
    {
        if (listToAdd == null)
            return;

        foreach (var data in listToAdd)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.audioName))
                continue;

            if (!clipCollection.ContainsKey(data.audioName))
                clipCollection.Add(data.audioName, data);
            else
                Debug.LogWarning("[AudioDatabaseSO] Duplicate audio name found: " + data.audioName);
        }
    }
}

[System.Serializable]
public class AudioClipData
{
    public string audioName;
    public List<AudioClip> clips = new List<AudioClip>();
    [Range(0f, 1f)] public float maxVolume = 1f;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Count == 0)
            return null;

        return clips[Random.Range(0, clips.Count)];
    }
}