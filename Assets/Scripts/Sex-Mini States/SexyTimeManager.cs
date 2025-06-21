using UnityEngine;

public class SexyTimeManager : MonoBehaviour
{
    public static SexyTimeManager Instance { get; private set; }

    [Header("Minigame Setup")]
    [SerializeField] private GameObject sexyTimeLogicPrefab; // Prefab with SexyTimeLogic on it
    [SerializeField] private Transform minigameSpawnPoint;

    private SexyTimeLogic activeSexyTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Starts a sexy time session between two entities.
    /// </summary>
    public void StartSexyTime(Entity_Stats playerStats, Entity_Stats partnerStats)
    {
        if (activeSexyTime != null)
        {
            Debug.LogWarning("Another sexy time is already active. Ending it...");
            EndSexyTime();
        }

        GameObject instance = Instantiate(sexyTimeLogicPrefab, minigameSpawnPoint.position, Quaternion.identity);
        activeSexyTime = instance.GetComponent<SexyTimeLogic>();

        if (activeSexyTime == null)
        {
            Debug.LogError("❌ SexyTimeLogic component not found on prefab!");
            return;
        }

        //// Assign player & partner data
        //activeSexyTime.SetPlayerStats(playerStats);
        //activeSexyTime.SetPartnerStats(partnerStats);

        // Start the minigame
        activeSexyTime.StartSexyTime();
    }

    public void EndSexyTime()
    {
        if (activeSexyTime != null)
        {
            Destroy(activeSexyTime.gameObject);
            activeSexyTime = null;
        }
    }
}

// Example from a trigger script or interaction logic:
//SexyTimeManager.Instance.StartSexyTime(playerStats, npcStats);
