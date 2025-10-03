using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class InventoryMenuController : MonoBehaviour
{
    [Header("CATEGORY (row)")]
    [SerializeField] private Transform categoryRoot;
    [SerializeField] private bool categoryIsHorizontal = true;
    [SerializeField] private GameObject categoryFirstSelect;
    [Tooltip("Optional: drag buttons here to explicitly control order. If empty, will scan 'categoryRoot'.")]
    [SerializeField] private Selectable[] categoryItems;

    [Header("ITEM LIST (grid)")]
    [SerializeField] private Transform itemGridRoot;
    [SerializeField] private int itemGridColumnsOverride = 0;
    [SerializeField] private GameObject itemFirstSelect;
    [SerializeField] private Selectable[] itemGridItems;

    [Header("ACTOR SELECT (grid)")]
    [SerializeField] private Transform actorGridRoot;
    [SerializeField] private int actorGridColumnsOverride = 0;
    [SerializeField] private GameObject actorFirstSelect;
    [SerializeField] private Selectable actorUpNeighbor;
    [SerializeField] private Selectable actorDownNeighbor;
    [SerializeField] private Selectable[] actorGridItems;

    [Header("ASSIGN POPUP (grid/row)")]
    [SerializeField] private Transform assignPopupRoot;
    [SerializeField] private int assignPopupColumnsOverride = 0;
    [SerializeField] private bool assignPopupIsHorizontalIfRow = true;
    [SerializeField] private GameObject assignFirstSelect;
    [SerializeField] private Selectable[] assignPopupItems;

    // ---- Public entry points called by UI_Inventory ----
    public void BuildForCategory()
    {
        var items = ChooseItems(categoryItems, categoryRoot);
        WireLinear(items, categoryIsHorizontal);
        FocusNowOrNextFrame(GetFocusTarget(categoryFirstSelect, items, categoryRoot));
    }

    public void BuildForItemGrid()
    {
        var items = ChooseItems(itemGridItems, itemGridRoot);
        int cols = ResolveColsForItems(items, itemGridColumnsOverride, itemGridRoot);
        WireGrid(items, cols);
        FocusNowOrNextFrame(GetFocusTarget(itemFirstSelect, items, itemGridRoot));
    }

    public void BuildForActorGrid()
    {
        var items = ChooseItems(actorGridItems, actorGridRoot);
        int cols = ResolveColsForItems(items, actorGridColumnsOverride, actorGridRoot);
        WireGrid(items, cols);
        WireGridEdgeNeighbors(items, cols, actorUpNeighbor, actorDownNeighbor);
        FocusNowOrNextFrame(GetFocusTarget(actorFirstSelect, items, actorGridRoot));
    }

    public void BuildForAssignPopup()
    {
        var items = ChooseItems(assignPopupItems, assignPopupRoot);
        if (items.Length > 1)
        {
            int cols = ResolveColsForItems(items, assignPopupColumnsOverride, assignPopupRoot);
            if (cols > 1) WireGrid(items, cols);
            else WireLinear(items, assignPopupIsHorizontalIfRow);
        }
        else
        {
            WireLinear(items, assignPopupIsHorizontalIfRow);
        }
        FocusNowOrNextFrame(GetFocusTarget(assignFirstSelect, items, assignPopupRoot));
    }

    // ----------------- Core wiring helpers -----------------
    private static Selectable[] ChooseItems(Selectable[] explicitItems, Transform root)
    {
        var chosen = FilterActive(explicitItems);
        return (chosen.Length > 0) ? chosen : GetSelectables(root);
    }

    private static Selectable[] FilterActive(Selectable[] items)
    {
        if (items == null) return new Selectable[0];
        return items.Where(s => s && s.IsActive() && s.interactable).ToArray();
    }

    private static Selectable[] GetSelectables(Transform root)
    {
        if (!root) return new Selectable[0];
        return root.GetComponentsInChildren<Selectable>(true)
                   .Where(s => s && s.IsActive() && s.interactable)
                   .ToArray();
    }

    private static void WireLinear(Selectable[] items, bool horizontal)
    {
        if (items == null) return;
        for (int i = 0; i < items.Length; i++)
        {
            var s = items[i]; if (!s) continue;
            var n = s.navigation; n.mode = Navigation.Mode.Explicit;
            var prev = i > 0 ? items[i - 1] : null;
            var next = i < items.Length - 1 ? items[i + 1] : null;
            if (horizontal) { n.selectOnLeft = prev; n.selectOnRight = next; }
            else { n.selectOnUp = prev; n.selectOnDown = next; }
            s.navigation = n;
        }
    }

    private static void WireGrid(Selectable[] items, int cols)
    {
        if (items == null || cols <= 0) return;
        for (int i = 0; i < items.Length; i++)
        {
            var s = items[i]; if (!s) continue;
            int row = i / cols, col = i % cols;
            var n = s.navigation; n.mode = Navigation.Mode.Explicit;

            int left = (col > 0) ? i - 1 : -1;
            int right = (col < cols - 1 && i + 1 < items.Length) ? i + 1 : -1;
            n.selectOnLeft = left >= 0 ? items[left] : null;
            n.selectOnRight = right >= 0 ? items[right] : null;

            int up = i - cols, down = i + cols;
            n.selectOnUp = up >= 0 ? items[up] : null;
            n.selectOnDown = down < items.Length ? items[down] : null;

            s.navigation = n;
        }
    }

    private static void WireGridEdgeNeighbors(Selectable[] items, int cols, Selectable upNeighbor, Selectable downNeighbor)
    {
        if (items == null || items.Length == 0 || cols <= 0) return;

        int rows = Mathf.CeilToInt(items.Length / (float)cols);

        for (int i = 0; i < Mathf.Min(cols, items.Length); i++)
        {
            var s = items[i]; if (!s) continue;
            var n = s.navigation; n.mode = Navigation.Mode.Explicit;
            if (upNeighbor) n.selectOnUp = upNeighbor;
            s.navigation = n;
        }

        int start = Mathf.Max(0, (rows - 1) * cols);
        for (int i = start; i < items.Length; i++)
        {
            var s = items[i]; if (!s) continue;
            var n = s.navigation; n.mode = Navigation.Mode.Explicit;
            if (downNeighbor) n.selectOnDown = downNeighbor;
            s.navigation = n;
        }

        if (upNeighbor)
        {
            var n = upNeighbor.navigation; n.mode = Navigation.Mode.Explicit;
            n.selectOnDown = items[0];
            upNeighbor.navigation = n;
        }
        if (downNeighbor)
        {
            var n = downNeighbor.navigation; n.mode = Navigation.Mode.Explicit;
            n.selectOnUp = items[Mathf.Max(0, items.Length - 1)];
            downNeighbor.navigation = n;
        }
    }

    private static int ResolveColsForItems(Selectable[] items, int colsOverride, Transform root, int defaultCols = 4)
    {
        if (colsOverride > 0) return colsOverride;
        var glg = root ? root.GetComponent<GridLayoutGroup>() : null;
        if (glg && glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount && glg.constraintCount > 0)
            return glg.constraintCount;
        return Mathf.Max(1, defaultCols);
    }

    // ----- Focus helpers -----
    private static GameObject GetFocusTarget(GameObject first, Selectable[] items, Transform root)
    {
        if (first) return first;
        var s = (items != null && items.Length > 0) ? items[0] : GetSelectables(root).FirstOrDefault();
        return s ? s.gameObject : null;
    }

    private void FocusNowOrNextFrame(GameObject go)
    {
        if (!go) return;
        EventSystem.current?.SetSelectedGameObject(null); // clear old selection
        var sel = go.GetComponent<Selectable>();
        if (sel && sel.IsActive() && sel.interactable) sel.Select(); // invoke OnSelect
        EventSystem.current?.SetSelectedGameObject(go);

        StartCoroutine(FocusNextFrame(go)); // survive layout rebuilds
    }

    private static void SelectGO(GameObject go)
    {
        if (!go) return;
        var sel = go.GetComponent<Selectable>();
        if (sel && sel.IsActive() && sel.interactable) sel.Select(); // triggers OnSelect
        EventSystem.current?.SetSelectedGameObject(go);
    }



    private System.Collections.IEnumerator FocusNextFrame(GameObject go)
    {
        yield return null;
        if (!go) yield break;
        EventSystem.current?.SetSelectedGameObject(null);
        var sel = go.GetComponent<Selectable>();
        if (sel && sel.IsActive() && sel.interactable) sel.Select();
        EventSystem.current?.SetSelectedGameObject(go);
    }
}
