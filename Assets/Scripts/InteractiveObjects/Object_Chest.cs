using UnityEngine;

public class Object_Chest : MonoBehaviour, IDamageable
{
    private Animator anim => GetComponentInChildren<Animator>();
    private Entity_DropManager dropManager => GetComponent<Entity_DropManager>();

    [SerializeField] private bool canDropItems = true;

    [Header("Audio")]
    [SerializeField] private string chestOpenSfx = "ChestOpen";

    public void OnUse(Transform actor)
    {
        OpenChest(actor);
    }

    public bool TakeDamage(float damage, float ele, ElementType type, Transform dealer)
    {
        if (!canDropItems) return false;
        OpenChest(dealer);
        return true;
    }

    private void OpenChest(Transform opener = null)
    {
        if (!canDropItems) return;
        canDropItems = false;

        PlayChestOpenSfx();

        if (anim) anim.SetBool("chestOpen", true);
        dropManager?.DropItems();

        var reward = GetComponent<GiveRewardOnInteract>();
        if (reward != null)
        {
            Transform player = opener;

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (player != null)
                reward.Give(player);
            else
                Debug.LogWarning("Chest opened but Player not found.");
        }

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }

    private void PlayChestOpenSfx()
    {
        if (AudioManager.instance == null) return;
        if (string.IsNullOrWhiteSpace(chestOpenSfx)) return;

        AudioManager.instance.PlayGlobalSFX(chestOpenSfx);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) OnUse(null);
    }
}