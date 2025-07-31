using UnityEngine;

public class UI_InventoryManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject categoryPanel;
    [SerializeField] private GameObject itemListPanel;
    [SerializeField] private GameObject actorSelectPanel;

    // Track current step
    private enum PanelState { None, Category, ItemList, ActorSelect }
    private PanelState currentState = PanelState.None;

    private void Start()
    {
        OpenCategoryPanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleCancel();
        }
    }

    private void HandleCancel()
    {
        switch (currentState)
        {
            case PanelState.ActorSelect:
                CloseActorSelectPanel();
                OpenItemListPanel();
                break;

            case PanelState.ItemList:
                CloseItemListPanel();
                OpenCategoryPanel();
                break;

            case PanelState.Category:
                CloseAll();
                Debug.Log("[UI] Closed entire Inventory");
                break;
        }
    }

    // --- Open/Close methods ---
    public void OpenCategoryPanel()
    {
        CloseAll();
        categoryPanel.SetActive(true);
        currentState = PanelState.Category;
    }

    public void OpenItemListPanel()
    {
        CloseAll();
        itemListPanel.SetActive(true);
        currentState = PanelState.ItemList;
    }

    public void OpenActorSelectPanel()
    {
        CloseAll();
        actorSelectPanel.SetActive(true);
        currentState = PanelState.ActorSelect;
    }

    private void CloseItemListPanel() => itemListPanel.SetActive(false);
    private void CloseActorSelectPanel() => actorSelectPanel.SetActive(false);

    private void CloseAll()
    {
        categoryPanel.SetActive(false);
        itemListPanel.SetActive(false);
        actorSelectPanel.SetActive(false);
        currentState = PanelState.None;
    }

    // --- Called by buttons ---
    public void OnCategoryOk()
    {
        OpenItemListPanel();
    }

    public void OnItemOk()
    {
        OpenActorSelectPanel();
    }

    public void OnActorOk()
    {
        Debug.Log("✅ Item used on Actor! Closing Actor Select.");
        CloseActorSelectPanel();
        OpenItemListPanel();
    }
}

