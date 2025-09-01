using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ConquestRosterItem : MonoBehaviour
{
    [Header("Optional visuals")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button button;   // if not assigned, will auto-grab on Init

    public CharacterProfileSO Profile { get; private set; }
    public Entity_Stats Stats { get; private set; }

    private UI_Conquest owner;

    /// <summary>
    /// Call this right after instantiating the item (or on a preplaced item at Start).
    /// </summary>
    public void Init(UI_Conquest owner, CharacterProfileSO profile, Entity_Stats stats)
    {
        this.owner = owner;
        this.Profile = profile;
        this.Stats = stats;

        // Fallbacks
        if (button == null) button = GetComponent<Button>();

        // Label/portrait
        if (nameText != null)
            nameText.text = profile ? (string.IsNullOrEmpty(profile.displayName) ? profile.name : profile.displayName)
                                    : "Unknown";
        if (portraitImage != null)
            portraitImage.sprite = profile ? profile.portrait : null;

        // Wire click → parent selection
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => owner?.OnRosterItemSelected(this));
        }
    }
}
