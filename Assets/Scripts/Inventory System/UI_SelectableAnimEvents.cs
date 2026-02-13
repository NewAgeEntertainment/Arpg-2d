using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UI_SelectableAnimEvents : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] private Animator anim;

    [Header("Bool Parameters")]
    [SerializeField] private string tabInBool = "TabIn";
    [SerializeField] private string tabOutBool = "TabOut";

    // Optional: if you want hover to also count as TabIn, leave this true.
    [Header("Behavior")]
    [SerializeField] private bool hoverAlsoTabsIn = true;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>(true);
    }

    // Called by your nav controller auto-wiring block:
    public void SetAnimator(Animator a) => anim = a;
    public void SetBoolNames(string tabIn, string tabOut)
    {
        tabInBool = tabIn;
        tabOutBool = tabOut;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverAlsoTabsIn) TabIn();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverAlsoTabsIn) TabOut();
    }

    public void OnSelect(BaseEventData eventData)
    {
        TabIn();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        TabOut();
    }

    private void TabIn()
    {
        if (!anim) return;

        if (!string.IsNullOrEmpty(tabOutBool))
            anim.SetBool(tabOutBool, false);

        if (!string.IsNullOrEmpty(tabInBool))
            anim.SetBool(tabInBool, true);
    }

    private void TabOut()
    {
        if (!anim) return;

        if (!string.IsNullOrEmpty(tabInBool))
            anim.SetBool(tabInBool, false);

        if (!string.IsNullOrEmpty(tabOutBool))
            anim.SetBool(tabOutBool, true);
    }
}
