using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_StatSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Entity_Stats boundStats;
    private Player_Stats playerStats;
    private Companion_Stats companionStats;

    private RectTransform rect;
    private UI ui;

    [SerializeField] private StatType statSlotType;
    public StatType StatType => statSlotType;

    [SerializeField] private TextMeshProUGUI statName;
    [SerializeField] private TextMeshProUGUI statValue;

    public Inventory_Item hoveredItem;

    private void OnValidate()
    {
        gameObject.name = "UI_Stat - " + statSlotType.GetStatName();

        if (statName != null)
            statName.text = GetStatNameByType(statSlotType);
    }

    private void Awake()
    {
        ui = GetComponentInParent<UI>();
        rect = GetComponent<RectTransform>();
    }

    public void Setup(Entity_Stats stats)
    {
        boundStats = stats;
        playerStats = stats as Player_Stats;
        companionStats = stats as Companion_Stats;

        UpdateStatValue();
    }

    public void Clear()
    {
        boundStats = null;
        playerStats = null;
        companionStats = null;

        if (statValue != null)
            statValue.text = "--";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ui != null && ui.statToolTip != null)
            ui.statToolTip.ShowToolTip(true, rect, statSlotType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ui != null && ui.statToolTip != null)
            ui.statToolTip.ShowToolTip(false, null);
    }

    public void UpdateStatDisplay(string text)
    {
        if (statValue != null)
            statValue.text = text;
    }

    public void UpdateStatValue()
    {
        if (statValue == null)
            return;

        if (boundStats == null)
        {
            statValue.text = "--";
            return;
        }

        float value = 0f;
        string displayText = "";

        switch (statSlotType)
        {
            // -------- Major Stats --------
            case StatType.Strength:
                value = boundStats.major.strength.GetValue();
                //displayText = $"{value}  (Phys: {boundStats.GetBaseDamage()})";
                break;

            case StatType.Luck:
                value = boundStats.major.luck.GetValue();
                break;

            case StatType.Intelligence:
                value = boundStats.major.intelligence.GetValue();
                break;

            case StatType.Vitality:
                value = boundStats.major.vitality.GetValue();
                break;

            // -------- Offensive Stats --------
            case StatType.Damage:
                value = boundStats.GetBaseDamage();
                break;

            case StatType.CritChance:
                value = boundStats.GetCritChance();
                break;

            case StatType.CritPower:
                value = boundStats.GetCritPower();
                break;

            case StatType.ArmorReduction:
                value = boundStats.GetArmorReduction() * 100f;
                break;

            case StatType.AttackSpeed:
                value = boundStats.offense.attackSpeed.GetValue() * 100f;
                break;

            // -------- Defense Stats --------
            case StatType.MaxHealth:
                value = boundStats.GetMaxHealth();
                break;

            case StatType.HealthRegen:
                value = boundStats.resources.healthRegen.GetValue();
                break;

            case StatType.MaxMana:
                value = boundStats.GetMaxMana();
                break;

            case StatType.ManaRegen:
                value = boundStats.resources.manaRegen.GetValue();
                break;

            case StatType.Evasion:
                value = boundStats.GetEvasion();
                break;

            case StatType.Defense:
                value = boundStats.GetBaseArmor();
                break;

            // -------- Elemental damage --------
            case StatType.IceDamage:
                value = boundStats.offense.iceDamage.GetValue();
                break;

            case StatType.FireDamage:
                value = boundStats.offense.fireDamage.GetValue();
                break;

            case StatType.PoisonDamage:
                value = boundStats.offense.poisonDamage.GetValue();
                break;

            case StatType.LightningDamage:
                value = boundStats.offense.lightningDamage.GetValue();
                break;

            // -------- Elemental resistances --------
            case StatType.IceResistance:
                value = boundStats.GetElementalResistance(ElementType.Ice) * 100f;
                break;

            case StatType.FireResistance:
                value = boundStats.GetElementalResistance(ElementType.Fire) * 100f;
                break;

            case StatType.PoisonResistance:
                value = boundStats.GetElementalResistance(ElementType.Poison) * 100f;
                break;

            case StatType.LightningResistance:
                value = boundStats.GetElementalResistance(ElementType.Lightning) * 100f;
                break;

            // -------- Sexual Stats (player only unless companion supports them too) --------
            case StatType.MaxArousal:
                if (playerStats != null)
                    value = playerStats.GetMaxArousel();
                else
                    displayText = "--";
                break;

            case StatType.Stroke:
                if (playerStats != null)
                {
                    value = playerStats.sex.stroke.GetValue();
                    displayText = $"{value}  (Sex: {playerStats.GetBaseSexDamage()})";
                }
                else
                {
                    displayText = "--";
                }
                break;

            case StatType.Resilience:
                if (playerStats != null)
                    value = playerStats.GetBaseResilience();
                else
                    displayText = "--";
                break;

            case StatType.SexualDamage:
                if (playerStats != null)
                    value = playerStats.GetBaseSexDamage();
                else
                    displayText = "--";
                break;

            case StatType.SexualRestraint:
                if (playerStats != null)
                    value = playerStats.sex.sexualRestraint.GetValue();
                else
                    displayText = "--";
                break;

            // -------- Unsupported / combined --------
            case StatType.ElementalDamage:
                displayText = "--";
                break;
        }

        if (string.IsNullOrEmpty(displayText))
        {
            displayText = IsPercentageStat(statSlotType)
                ? $"{value}%"
                : value.ToString();
        }

        statValue.text = displayText;
    }

    private bool IsPercentageStat(StatType type)
    {
        switch (type)
        {
            case StatType.CritChance:
            case StatType.CritPower:
            case StatType.ArmorReduction:
            case StatType.FireResistance:
            case StatType.IceResistance:
            case StatType.PoisonResistance:
            case StatType.LightningResistance:
            case StatType.Evasion:
                return true;

            default:
                return false;
        }
    }

    private string GetStatNameByType(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth: return "Max Health";
            case StatType.HealthRegen: return "Health Regen";
            case StatType.MaxMana: return "Max Mana";
            case StatType.ManaRegen: return "Mana Regen";
            case StatType.Strength: return "Strength";
            case StatType.Luck: return "Luck";
            case StatType.Intelligence: return "Intelligence";
            case StatType.Vitality: return "Vitality";
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.Damage: return "Damage";
            case StatType.CritChance: return "Crit Chance";
            case StatType.CritPower: return "Crit Power";
            case StatType.ArmorReduction: return "Armor Reduction";
            case StatType.FireDamage: return "Fire Damage";
            case StatType.IceDamage: return "Ice Damage";
            case StatType.PoisonDamage: return "Poison Damage";
            case StatType.LightningDamage: return "Lightning Damage";
            case StatType.ElementalDamage: return "Elemental Damage";
            case StatType.Defense: return "Armor";
            case StatType.Evasion: return "Evasion";
            case StatType.IceResistance: return "Ice Resistance";
            case StatType.FireResistance: return "Fire Resistance";
            case StatType.PoisonResistance: return "Poison Resistance";
            case StatType.LightningResistance: return "Lightning Resistance";
            case StatType.Stroke: return "Stroke";
            case StatType.Resilience: return "Resilience";
            case StatType.SexualDamage: return "Sexual Damage";
            case StatType.SexualRestraint: return "Sexual Restraint";
            case StatType.MaxArousal: return "Max Arousal";
            default: return "Unknown Stat";
        }
    }

    private float GetEquippedItemStatValue(StatType type)
    {
        if (ui == null || ui.hoveredItem == null)
        {
            Debug.LogWarning("No hovered item to compare for stat: " + type);
            return 0f;
        }

        return ui.hoveredItem.GetStatValue(type);
    }
}