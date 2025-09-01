using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Conquest/Character Profiles Database")]
public class CharacterProfilesDatabase : ScriptableObject
{
    public List<CharacterProfileSO> allProfiles = new();
}
