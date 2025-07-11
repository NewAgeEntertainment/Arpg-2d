using UnityEngine;

public class UI_CharacterProfilePopup : MonoBehaviour
{
    [Header("One reusable profile slot")]
    [SerializeField] private UI_CharacterProfileButton profileButton;

    private Inventory_Item itemToUse;

    /// <summary>
    /// Opens the popup and shows the player profile.
    /// </summary>
    public void Open(Inventory_Item item, Player player)
    {
        itemToUse = item;

        // Enable the popup GameObject
        gameObject.SetActive(true);

        // Setup the single reusable profile slot
        profileButton.Setup(player, () => GiveItemToPlayer(player));
    }

    private void GiveItemToPlayer(Player player)
    {
        if (itemToUse == null) return;

        if (itemToUse.stackSize > 0)
        {
            player.inventory.TryUseItem(itemToUse);

            if (itemToUse.stackSize <= 0)
            {
                Debug.Log("[Popup] Item stack empty — closing popup.");
                Close();
            }
            else
            {
                Debug.Log($"[Popup] Item used. {itemToUse.stackSize} left — popup stays open.");
            }
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
