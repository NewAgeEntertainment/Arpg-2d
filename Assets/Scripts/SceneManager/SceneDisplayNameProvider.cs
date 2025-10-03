// SceneDisplayNameProvider.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDisplayNameProvider : MonoBehaviour
{
    [SerializeField] private string displayName; // e.g., "Forest", "Dungeon – 2F"
    public string DisplayName => string.IsNullOrWhiteSpace(displayName)
        ? SceneManager.GetActiveScene().name
        : displayName;
}
