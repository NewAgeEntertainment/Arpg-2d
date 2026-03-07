using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(50)]
public class UI_ListNavController : MonoBehaviour
{
    public enum LayoutMode { LinearHorizontal, LinearVertical, Grid }

    [Header("Scope")]
    [SerializeField] private Transform root;

    [Header("Layout")]
    [SerializeField] private LayoutMode layout = LayoutMode.LinearHorizontal;
    [SerializeField] private int gridColumnsOverride = 0;

    [Header("Items (optional explicit order)")]
    [SerializeField] private Selectable[] items;

    [Header("Focus")]
    [SerializeField] private GameObject firstSelect;
    [SerializeField] private bool focusOnEnable = true;
    [SerializeField] private bool refocusNextFrame = true;
    [SerializeField] private bool rememberLast = true;

    [Header("Edge Neighbors (optional)")]
    [SerializeField] private Selectable leftNeighbor;
    [SerializeField] private Selectable rightNeighbor;
    [SerializeField] private Selectable upNeighbor;
    [SerializeField] private Selectable downNeighbor;

    [Header("Rewired (optional)")]
    [SerializeField] private int rewiredPlayerId = 0;
    [SerializeField] private string cancelAction = "UICancel";
    [SerializeField] private bool routeCancelToUIHandleBack = true;

    [Header("Audio")]
    [SerializeField] private bool playMoveSound = true;
    [SerializeField] private string moveSoundName = "UIButtonMove";

    private Rewired.Player rplayer;
    private GameObject lastSelectedGO;
    private GameObject lastSoundedSelection;

    private void OnEnable()
    {
        TryGetRewired();
        Build();

        lastSoundedSelection = null;

        if (focusOnEnable)
            Focus();
    }

    private void OnDisable()
    {
        if (rememberLast && EventSystem.current != null)
            lastSelectedGO = EventSystem.current.currentSelectedGameObject;

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != null &&
            !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        lastSoundedSelection = null;
    }

    private void Update()
    {
        if (rplayer != null && rplayer.GetButtonDown(cancelAction))
        {
            if (routeCancelToUIHandleBack && UI.Instance != null)
                UI.Instance.HandleBackAction();
        }

        if (rememberLast && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelectedGO = EventSystem.current.currentSelectedGameObject;
        }

        HandleSelectionSound();
    }

    public void Build()
    {
        var sel = ChooseItems(items, root);
        if (sel.Length == 0) return;

        switch (layout)
        {
            case LayoutMode.LinearHorizontal:
                WireLinear(sel, true);
                break;

            case LayoutMode.LinearVertical:
                WireLinear(sel, false);
                break;

            case LayoutMode.Grid:
                WireGrid(sel, ResolveCols(root, gridColumnsOverride));
                break;
        }

        WireEdgeNeighbors(sel);
    }

    public void Focus()
    {
        GameObject target = null;

        if (firstSelect && IsFocusable(firstSelect))
            target = firstSelect;
        else if (rememberLast && lastSelectedGO && IsFocusable(lastSelectedGO))
            target = lastSelectedGO;
        else
        {
            var sel = ChooseItems(items, root);
            if (sel.Length > 0)
                target = sel[0].gameObject;
        }

        if (!target) return;

        SetSelected(target);

        // Prevent initial auto-focus from playing the move sound
        lastSoundedSelection = target;

        if (refocusNextFrame)
            StartCoroutine(RefocusNextFrame(target));
    }

    public void SetItems(Selectable[] newItems, bool rebuild = true)
    {
        items = newItems;
        if (rebuild) Build();
    }

    public void SelectIndex(int index)
    {
        var sel = ChooseItems(items, root);
        if (sel.Length == 0) return;

        index = Mathf.Clamp(index, 0, sel.Length - 1);
        var go = sel[index] ? sel[index].gameObject : null;

        if (go)
        {
            SetSelected(go);
            if (refocusNextFrame) StartCoroutine(RefocusNextFrame(go));
        }
    }

    private void HandleSelectionSound()
    {
        if (!playMoveSound) return;
        if (EventSystem.current == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current == null) return;
        if (current == lastSoundedSelection) return;

        if (!IsSelectionInsideScope(current)) return;

        lastSoundedSelection = current;
        PlayMoveSfx();
    }

    private bool IsSelectionInsideScope(GameObject go)
    {
        if (go == null) return false;

        var sel = go.GetComponent<Selectable>();
        if (sel == null || !sel.IsActive() || !sel.interactable)
            return false;

        var chosen = ChooseItems(items, root);
        for (int i = 0; i < chosen.Length; i++)
        {
            if (chosen[i] != null && chosen[i].gameObject == go)
                return true;
        }

        if (leftNeighbor != null && leftNeighbor.gameObject == go) return true;
        if (rightNeighbor != null && rightNeighbor.gameObject == go) return true;
        if (upNeighbor != null && upNeighbor.gameObject == go) return true;
        if (downNeighbor != null && downNeighbor.gameObject == go) return true;

        return false;
    }

    private void PlayMoveSfx()
    {
        if (AudioManager.instance == null) return;
        if (string.IsNullOrWhiteSpace(moveSoundName)) return;

        AudioManager.instance.PlayGlobalSFX(moveSoundName);
    }

    private static Selectable[] ChooseItems(Selectable[] explicitItems, Transform r)
    {
        var chosen = FilterActive(explicitItems);
        return chosen.Length > 0 ? chosen : GetSelectables(r);
    }

    private static Selectable[] FilterActive(Selectable[] arr)
    {
        if (arr == null) return new Selectable[0];
        return arr.Where(s => s && s.IsActive() && s.interactable).ToArray();
    }

    private static Selectable[] GetSelectables(Transform r)
    {
        if (!r) return new Selectable[0];

        return r.GetComponentsInChildren<Selectable>(true)
                .Where(s => s && s.IsActive() && s.interactable)
                .ToArray();
    }

    private static void WireLinear(Selectable[] sel, bool horizontal)
    {
        for (int i = 0; i < sel.Length; i++)
        {
            var s = sel[i];
            if (!s) continue;

            var n = s.navigation;
            n.mode = Navigation.Mode.Explicit;

            var prev = i > 0 ? sel[i - 1] : null;
            var next = i < sel.Length - 1 ? sel[i + 1] : null;

            if (horizontal)
            {
                n.selectOnLeft = prev;
                n.selectOnRight = next;
            }
            else
            {
                n.selectOnUp = prev;
                n.selectOnDown = next;
            }

            s.navigation = n;
        }
    }

    private static void WireGrid(Selectable[] sel, int cols)
    {
        if (cols <= 0) cols = 1;

        for (int i = 0; i < sel.Length; i++)
        {
            var s = sel[i];
            if (!s) continue;

            var n = s.navigation;
            n.mode = Navigation.Mode.Explicit;

            int col = i % cols;
            int left = (col > 0) ? i - 1 : -1;
            int right = (col < cols - 1 && i + 1 < sel.Length) ? i + 1 : -1;
            int up = i - cols;
            int down = i + cols;

            n.selectOnLeft = left >= 0 ? sel[left] : null;
            n.selectOnRight = right >= 0 ? sel[right] : null;
            n.selectOnUp = up >= 0 ? sel[up] : null;
            n.selectOnDown = down < sel.Length ? sel[down] : null;

            s.navigation = n;
        }
    }

    private void WireEdgeNeighbors(Selectable[] sel)
    {
        if (sel == null || sel.Length == 0) return;

        var first = sel[0];
        var last = sel[sel.Length - 1];

        if (leftNeighbor && first)
        {
            var n = first.navigation;
            n.mode = Navigation.Mode.Explicit;
            n.selectOnLeft = leftNeighbor;
            first.navigation = n;

            var ln = leftNeighbor.navigation;
            ln.mode = Navigation.Mode.Explicit;
            ln.selectOnRight = first;
            leftNeighbor.navigation = ln;
        }

        if (rightNeighbor && last)
        {
            var n = last.navigation;
            n.mode = Navigation.Mode.Explicit;
            n.selectOnRight = rightNeighbor;
            last.navigation = n;

            var rn = rightNeighbor.navigation;
            rn.mode = Navigation.Mode.Explicit;
            rn.selectOnLeft = last;
            rightNeighbor.navigation = rn;
        }

        if (layout == LayoutMode.Grid)
        {
            int cols = ResolveCols(root, gridColumnsOverride);
            int topCount = Mathf.Min(cols, sel.Length);

            for (int i = 0; i < topCount; i++)
            {
                if (!sel[i]) continue;
                var n = sel[i].navigation;
                n.mode = Navigation.Mode.Explicit;
                if (upNeighbor) n.selectOnUp = upNeighbor;
                sel[i].navigation = n;
            }

            int start = Mathf.Max(0, (Mathf.CeilToInt(sel.Length / (float)cols) - 1) * cols);
            for (int i = start; i < sel.Length; i++)
            {
                if (!sel[i]) continue;
                var n = sel[i].navigation;
                n.mode = Navigation.Mode.Explicit;
                if (downNeighbor) n.selectOnDown = downNeighbor;
                sel[i].navigation = n;
            }
        }
        else
        {
            if (upNeighbor && first)
            {
                var n = first.navigation;
                n.mode = Navigation.Mode.Explicit;
                n.selectOnUp = upNeighbor;
                first.navigation = n;

                var un = upNeighbor.navigation;
                un.mode = Navigation.Mode.Explicit;
                un.selectOnDown = first;
                upNeighbor.navigation = un;
            }

            if (downNeighbor && last)
            {
                var n = last.navigation;
                n.mode = Navigation.Mode.Explicit;
                n.selectOnDown = downNeighbor;
                last.navigation = n;

                var dn = downNeighbor.navigation;
                dn.mode = Navigation.Mode.Explicit;
                dn.selectOnUp = last;
                downNeighbor.navigation = dn;
            }
        }
    }

    private static int ResolveCols(Transform r, int overrideCols)
    {
        if (overrideCols > 0) return overrideCols;

        var glg = r ? r.GetComponent<GridLayoutGroup>() : null;
        if (glg && glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount && glg.constraintCount > 0)
            return glg.constraintCount;

        return 4;
    }

    private static bool IsFocusable(GameObject go)
    {
        if (!go || !go.activeInHierarchy) return false;

        var s = go.GetComponent<Selectable>();
        return s && s.IsActive() && s.interactable;
    }

    private static void SetSelected(GameObject go)
    {
        if (!go) return;

        EventSystem.current?.SetSelectedGameObject(null);
        var s = go.GetComponent<Selectable>();
        if (s && s.IsActive() && s.interactable) s.Select();
        EventSystem.current?.SetSelectedGameObject(go);
    }

    private IEnumerator RefocusNextFrame(GameObject go)
    {
        yield return null;
        if (IsFocusable(go)) SetSelected(go);
    }

    private void TryGetRewired()
    {
        try { rplayer = Rewired.ReInput.players.GetPlayer(rewiredPlayerId); }
        catch { rplayer = null; }
    }
}