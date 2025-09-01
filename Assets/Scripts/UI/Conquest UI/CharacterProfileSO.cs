using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Conquest/Character Profile", fileName = "CharacterProfile")]
public class CharacterProfileSO : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    public string race;
    public string gender;

    [TextArea(2, 5)]
    public string shortDescription;

    [Header("Portrait")]
    public Sprite portrait;

    [Header("Sex Mastery & Affection")]
    public SexMastery mastery = SexMastery.Beginner;
    [Range(0, 100)] public int affection = 0;

    [Header("Sex Skills (labels only)")]
    public List<string> sexSkills = new List<string>();
}
