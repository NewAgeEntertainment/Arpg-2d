using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    // Money
    public int gold;

    // Player vital state
    public float currentHealth;
    public float currentMana;

    // EXP/Level
    public int level;
    public float currentExp;

    // Sex EXP/Level (since your game has it)
    public int sexLevel;
    public float sexExp;

    // Backpack, Storage, Equipment Bag: guid -> stackSize
    public SerializableDictionary<string, int> backpack = new SerializableDictionary<string, int>();
    public SerializableDictionary<string, int> storageItems = new SerializableDictionary<string, int>();
    public SerializableDictionary<string, int> storageMaterials = new SerializableDictionary<string, int>();
    public SerializableDictionary<string, int> equipmentBag = new SerializableDictionary<string, int>();

    // Equipped items: slot key -> item guid (one each)
    // Key can be ItemType (string) or custom slot id — I’ll use ItemType string.
    public SerializableDictionary<string, string> equipped = new SerializableDictionary<string, string>();

    // Scene & position (optional; makes respawn nice)
    public string lastScene;
    public Vector3 lastPlayerPosition;
}
