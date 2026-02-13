using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_PanelSwitchButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    public enum PanelKind
    {
        Inventory,
        SkillTree,
        Equipment,
        Status,
        Conquest,
        QuestJournal,
        Options,
        Save
    }

    [Header("Target Panel")]
    [SerializeField] private PanelKind targetPanel;

    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private Animator animator; // can be on this GO or a child

    [Header("Animator Triggers")]
    [SerializeField] private string hoverTrigger = "Hover";
    [SerializeField] private string pressTrigger = "Press";
    [SerializeField] private string selectedTrigger = "Selected";
    [SerializeField] private string disabledTrigger = "Disabled";

    private void Reset()
    {
        button = GetComponent<Button>();
        animator = GetComponentInChildren<Animator>(true);
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        RefreshInteractable();
    }

    private void Update()
    {
        // Keep buttons in sync (optional, safe)
        RefreshInteractable();
    }

    private void RefreshInteractable()
    {
        if (button == null) return;

        bool canSwitch = UI.Instance != null && UI.Instance.CanSwitchPanelsRightNow();
        if (button.interactable != canSwitch)
        {
            button.interactable = canSwitch;

            if (animator != null && !string.IsNullOrEmpty(disabledTrigger))
                animator.SetTrigger(disabledTrigger);
        }
    }

    private void TrySwitch()
    {
        if (UI.Instance == null) return;
        if (!UI.Instance.CanSwitchPanelsRightNow()) return;

        // play button press anim
        if (animator != null && !string.IsNullOrEmpty(pressTrigger))
            animator.SetTrigger(pressTrigger);

        UI.Instance.SwitchToPanelFromButton(Convert(targetPanel));
    }

    private UI.UIPanelKind Convert(PanelKind k)
    {
        // assumes your UI.cs has this enum:
        // Inventory, SkillTree, Equipment, Conquest, QuestJournal, Options, Save
        return k switch
        {
            PanelKind.Inventory => UI.UIPanelKind.Inventory,
            PanelKind.SkillTree => UI.UIPanelKind.SkillTree,
            PanelKind.Equipment => UI.UIPanelKind.Equipment,
            PanelKind.Status => UI.UIPanelKind.Status,

            PanelKind.Conquest => UI.UIPanelKind.Conquest,
            PanelKind.QuestJournal => UI.UIPanelKind.QuestJournal,
            PanelKind.Options => UI.UIPanelKind.Options,
            PanelKind.Save => UI.UIPanelKind.Save,
            _ => UI.UIPanelKind.Inventory
        };
    }

    // ---------- UI events ----------
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animator != null && !string.IsNullOrEmpty(hoverTrigger))
            animator.SetTrigger(hoverTrigger);
    }

    public void OnPointerExit(PointerEventData eventData) { }

    public void OnPointerClick(PointerEventData eventData) => TrySwitch();

    public void OnSelect(BaseEventData eventData)
    {
        if (animator != null && !string.IsNullOrEmpty(selectedTrigger))
            animator.SetTrigger(selectedTrigger);
    }

    public void OnDeselect(BaseEventData eventData) { }

    public void OnSubmit(BaseEventData eventData) => TrySwitch();
}
