// Assets/Scripts/Title/LoadMenu.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers;

public class LoadMenu : MonoBehaviour
{
    [Header("List Setup")]
    [SerializeField] private Transform content;          // Vertical layout parent
    [SerializeField] private SaveSlotListItem itemPrefab;

    [Header("UI")]
    [SerializeField] private Button backButton;          // ESC alternative
    [SerializeField] private Text emptyLabel;            // "No saves" text (optional)

    private readonly List<SaveSlotListItem> pool = new();

    private void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(BackToTitle);
    }

    private void OnEnable()
    {
        // Also allow ESC to back out handled by TitleMenuManager.Update()
    }

    public void RefreshList()
    {
        ClearPool();

        bool any = false;
        int max = SaveSystem.maxSaveSlot;   // from your SaveSystem
        for (int i = 0; i <= max; i++)
        {
            if (!SaveSystem.HasSavedGameInSlot(i)) continue;

            var entry = GetItem();
            entry.Bind(i, TryLoadSlot, DeleteSlot);
            any = true;
        }

        if (emptyLabel != null) emptyLabel.gameObject.SetActive(!any);
    }

    private SaveSlotListItem GetItem()
    {
        SaveSlotListItem item = null;
        if (pool.Count < content.childCount)
        {
            // reuse existing child if you pre-placed any
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i).GetComponent<SaveSlotListItem>();
                if (child != null && !pool.Contains(child))
                {
                    item = child;
                    break;
                }
            }
        }
        if (item == null) item = Instantiate(itemPrefab, content);
        item.gameObject.SetActive(true);
        pool.Add(item);
        return item;
    }

    private void ClearPool()
    {
        foreach (var item in pool) if (item != null) item.gameObject.SetActive(false);
        pool.Clear();
    }

    private void TryLoadSlot(int slot)
    {
        // Pixel Crushers API: will load saved scene if SaveSystem.saveCurrentScene == true (default).
        SaveSystem.LoadFromSlot(slot);
    }

    private void DeleteSlot(int slot)
    {
        SaveSystem.DeleteSavedGameInSlot(slot);
        RefreshList();
    }

    private void BackToTitle()
    {
        // Delegated up: find TitleMenuManager and close load panel
        var t = GetComponentInParent<TitleMenuManager>(true);
        t?.CloseLoad();
    }
}
