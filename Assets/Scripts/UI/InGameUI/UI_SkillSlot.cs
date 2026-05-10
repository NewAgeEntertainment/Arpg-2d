using Rewired;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UISkillSlotId { SlotA, SlotB, SlotC, SlotD }
public enum UISkillCategory { Combat, Sex }   // UI-only category gate

public class UI_SkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot Setup")]
    public UISkillSlotId slotId = UISkillSlotId.SlotA;
    public UISkillCategory slotCategory = UISkillCategory.Combat;

    [Header("UI")]
    [SerializeField] private Image cooldownImage;          // radial, fill 0..1
    [SerializeField] private string inputKeyName = "";     // e.g. "Q", "R", "A"
    [SerializeField] private TextMeshProUGUI inputKeyText;
    [SerializeField] private GameObject conflictSlot;      // quick pulse when blocked
    [SerializeField] private TextMeshProUGUI mpCostText;   // shows MP cost
    [SerializeField] private CanvasGroup affordOverlay;     // fades when unaffordable (alpha to 1)

    [Header("Binding Label")]
    [SerializeField] private bool useRewiredBindingLabel = true;
    [SerializeField] private string fallbackLabel = "";

    // Visual core
    private UI ui;
    private Image skillIcon;
    private RectTransform rect;
    private Button button;

    // Data
    private Skill_DataSO skillData;

    // Cache for mini pulses
    private Coroutine pulseCo;

    private Coroutine cooldownCo;
    private Coroutine cooldownRoutine;


    public bool IsCoolingDown => cooldownImage != null && cooldownImage.fillAmount > 0.001f;


    private void Awake()
    {
        ui = GetComponentInParent<UI>(true);
        skillIcon = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();

        if (inputKeyText != null) inputKeyText.text = inputKeyName;
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
        if (conflictSlot != null) conflictSlot.SetActive(false);
        if (mpCostText != null) mpCostText.text = "";
        if (affordOverlay != null) affordOverlay.alpha = 0f;
    }

    private void OnValidate()
    {
        gameObject.name = $"UI_SkillSlot - {slotId}";
        if (inputKeyText != null) inputKeyText.text = inputKeyName;
    }

    private void OnEnable()
    {
        var mgr = FindFirstObjectByType<Player_SkillManager>(FindObjectsInactive.Include);
        RefreshVisuals(mgr);
    }

    // ------------ Public API ------------

    public Skill_DataSO Data => skillData;
    public bool HasSkill => skillData != null;
    public bool IsReady => cooldownImage == null || cooldownImage.fillAmount <= 0.001f;

    public float CooldownSeconds =>
        (skillData != null && skillData.upgradeData != null)
            ? Mathf.Max(0f, skillData.upgradeData.cooldown)
            : 5f;

    public float ManaCostFromSO =>
        (skillData != null && skillData.upgradeData != null)
            ? Mathf.Max(0f, skillData.upgradeData.manaCost)
            : 0f;

    /// <summary>Assigns a skill to this slot if it matches the slotCategory.</summary>
    public bool SetupSkillSlot(Skill_DataSO selectedSkill)
    {
        if (selectedSkill == null)
        {
            ClearSlot();
            return false;
        }

        // Category gate (Combat slots ignore Sex skills; Sex slots ignore Combat)
        if (!Accepts(selectedSkill))
        {
            PulseConflict(0.25f);
            return false;
        }

        skillData = selectedSkill;

        if (skillIcon != null) skillIcon.sprite = selectedSkill.icon;
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
        if (conflictSlot != null) conflictSlot.SetActive(false);

        RefreshText(null); // no manager yet – this will fall back to SO cost
        return true;
    }

    /// <summary>Remove the current skill from the slot visually.</summary>
    public void ClearSlot()
    {
        skillData = null;
        if (skillIcon != null) skillIcon.sprite = null;
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
        if (conflictSlot != null) conflictSlot.SetActive(false);
        if (mpCostText != null) mpCostText.text = "";
        if (affordOverlay != null) affordOverlay.alpha = 0f;
    }

    public bool Accepts(Skill_DataSO data)
    {
        if (data == null) return false;

        // Do not allow Dash to appear in in-game skill slots
        if (data.skillType == SkillType.Dash)
            return false;

        if (slotCategory == UISkillCategory.Combat && data.category != SkillCategory.Combat) return false;
        if (slotCategory == UISkillCategory.Sex && data.category != SkillCategory.Sex) return false;

        return true;
    }

    public void SetKeyLabel(string label)
    {
        inputKeyName = label;
        if (inputKeyText != null) inputKeyText.text = label;
    }

    public void StartCooldown(float cooldownSeconds, bool forceRestart = false)
    {
        if (cooldownImage == null) return;

        // NEW: Don't reset/refresh cooldown if already running (unless forced).
        if (!forceRestart && IsCoolingDown) return;

        if (cooldownRoutine != null)
            StopCoroutine(cooldownRoutine);

        cooldownImage.fillAmount = 1f;
        cooldownRoutine = StartCoroutine(CooldownCo(cooldownSeconds));
    }


    public void ResetCooldown()
    {
        if (cooldownRoutine != null)
        {
            StopCoroutine(cooldownRoutine);
            cooldownRoutine = null;
        }

        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
    }

    public void RefreshBindingLabel(Player_SkillManager manager)
    {
        if (inputKeyText == null)
            return;

        string label = !string.IsNullOrWhiteSpace(fallbackLabel)
            ? fallbackLabel
            : inputKeyName;

        if (!useRewiredBindingLabel || manager == null)
        {
            inputKeyText.text = label;
            inputKeyName = label;
            return;
        }

        string keyboardAction = manager.GetKeyboardActionNameForSlot(slotId);
        string controllerAction = manager.GetControllerActionNameForSlot(slotId);
        string modifierAction = manager.SkillModifierActionName;

        string keyboardLabel = GetFirstBindingLabel(manager.RewiredPlayerId, keyboardAction);
        string controllerLabel = GetFirstBindingLabel(manager.RewiredPlayerId, controllerAction);
        string modifierLabel = GetFirstBindingLabel(manager.RewiredPlayerId, modifierAction);

        bool controllerMode =
            InputDeviceModeManager.Instance != null &&
            InputDeviceModeManager.Instance.IsController;

        if (controllerMode)
        {
            if (!string.IsNullOrWhiteSpace(controllerLabel) &&
                !string.IsNullOrWhiteSpace(modifierLabel))
            {
                label = $"{modifierLabel}+{controllerLabel}";
            }
            else if (!string.IsNullOrWhiteSpace(controllerLabel))
            {
                label = controllerLabel;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(keyboardLabel))
                label = keyboardLabel;
        }

        inputKeyText.text = label;
        inputKeyName = label;
    }

    private string GetFirstBindingLabel(int rewiredPlayerId, string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName))
            return "";

        try
        {
            var rewiredPlayer = ReInput.players.GetPlayer(rewiredPlayerId);

            if (rewiredPlayer == null)
                return "";

            bool skipDisabledMaps = true;

            var aem = rewiredPlayer.controllers.maps.GetFirstElementMapWithAction(
                actionName,
                skipDisabledMaps
            );

            if (aem != null && !string.IsNullOrWhiteSpace(aem.elementIdentifierName))
                return aem.elementIdentifierName;
        }
        catch
        {
            return "";
        }

        return "";
    }

    public void RefreshVisuals(Player_SkillManager manager)
    {
        RefreshBindingLabel(manager);
        RefreshText(manager);

        var manaRef = FindFirstObjectByType<Entity_Mana>(FindObjectsInactive.Include);
        UpdateAffordability(manaRef);
    }


    /// <summary>
    /// Refreshes the visible MP cost text using the runtime skill (if available).
    /// Pass null to fall back to SO cost.
    /// </summary>
    public void RefreshText(Player_SkillManager manager)
    {
        if (mpCostText == null) return;

        float cost = ManaCostFromSO;

        if (manager != null && skillData != null)
        {
            var runtime = manager.GetSkillByType(skillData.skillType);
            if (runtime != null)
            {
                // Needs: public float CurrentManaCost => manaCost; in Skill_Base
                cost = Mathf.Max(cost, runtime.CurrentManaCost);
            }
        }

        mpCostText.text = cost > 0f ? Mathf.FloorToInt(cost).ToString() : "";
    }

    /// <summary>Greys out (or not) based on affordability vs. Entity_Mana.</summary>
    public void UpdateAffordability(Entity_Mana mana)
    {
        if (affordOverlay == null) return;
        if (mana == null || !HasSkill)
        {
            affordOverlay.alpha = 0f;
            return;
        }

        float need = ManaCostFromSO;

        bool affordable = mana.GetCurrentMana() >= need;
        affordOverlay.alpha = affordable ? 0f : 1f;
    }

    public void PulseConflict(float seconds = 0.2f)
    {
        if (conflictSlot == null) return;
        if (pulseCo != null) StopCoroutine(pulseCo);
        pulseCo = StartCoroutine(PulseCo(seconds));
    }

    // ------------ IPointer ------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ui == null || ui.skillToolTip == null || rect == null) return;
        if (!HasSkill) return;
        ui.skillToolTip.ShowToolTip(true, rect, skillData, null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ui == null || ui.skillToolTip == null) return;
        ui.skillToolTip.ShowToolTip(false, null);
    }

    // NEW: left-click a slot to complete "pick a slot" mode (but DO NOT close pick mode)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (UI_SkillTree.TryCompleteSlotPick(this))
        {
            eventData.Use();   // we assigned; pick mode stays active until Esc/back
        }
    }

    // ------------ Coroutines ------------

    private IEnumerator CooldownCo(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (cooldownImage != null)
                cooldownImage.fillAmount = 1f - (t / duration);
            yield return null;
        }

        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
        cooldownRoutine = null; // NEW
    }



    private IEnumerator PulseCo(float seconds)
    {
        conflictSlot.SetActive(true);
        yield return new WaitForSeconds(seconds);
        conflictSlot.SetActive(false);
    }

    

}
