using UnityEngine;

public class UI_CharacterProfilePopup : MonoBehaviour
{
    [Header("One reusable profile slot")]
    [SerializeField] private UI_CharacterProfileButton profileButton;

    [Header("Behavior")]
    [SerializeField] private bool closeWhenOutOfItem = true;

    private Inventory_Item itemToUse;
    private Inventory_Player inventory;
    private Player targetPlayer;

    /// <summary>
    /// Opens the popup for a single player and shows live HP/MP.
    /// Clicking the button will use <paramref name="item"/> on <paramref name="player"/>.
    /// </summary>
    public void Open(Inventory_Item item, Player player)
    {
        // Prefer the live player in the scene
        if (player == null || !player.gameObject.scene.IsValid())
            player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (player == null || item == null || item.itemData == null)
        {
            Debug.LogWarning("[CharacterProfilePopup] Open: missing player or item.");
            return;
        }

        itemToUse = item;
        var inv = player.GetComponent<Inventory_Player>()
               ?? FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);

        gameObject.SetActive(true);

        profileButton.Setup(player, null);
        profileButton.SetUseItemContext(itemToUse, inv, afterUse: () =>
        {
            // Make sure bars and HUD reflect changes instantly
            profileButton.RefreshBars();
            player.ui?.inGameUI?.ForceRefreshFromCurrentState();
        });
    }


    public void Close()
    {
        gameObject.SetActive(false);
    }
}
