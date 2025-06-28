using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_StatSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Player_Stats playerStats;
    private RectTransform rect;
    private UI ui;

    [SerializeField] private StatType statSlotType;
    [SerializeField] private TextMeshProUGUI statName;
    [SerializeField] private TextMeshProUGUI statValue;

    public Inventory_Item hoveredItem;
    private void OnValidate()
    {
        // this will give the gameobject a name.
        gameObject.name = "UI_Stat - " + GetStatNameByType(statSlotType);
        statName.text = GetStatNameByType(statSlotType);
    }

    private void Awake()
    {
        ui = GetComponentInParent<UI>();
        rect = GetComponent<RectTransform>();
        playerStats = FindAnyObjectByType<Player_Stats>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ui.statToolTip.ShowToolTip(true, rect, statSlotType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.statToolTip.ShowToolTip(false, null);
        
    }



    public void UpdateStatValue()
    {
        Stat statToUpdate = playerStats.GetStatByType(statSlotType);

        if (statToUpdate == null && statSlotType != StatType.ElementalDamage)
        {
            Debug.LogError("Stat not found: " + statSlotType);
            return;
        }

        float value = 0;

        switch (statSlotType)
        {
            //major Stats
            case StatType.Strength:
                value = playerStats.major.strength.GetValue();
                break;
            case StatType.Luck:
                value = playerStats.major.luck.GetValue();
                break;
            case StatType.Intelligence:
                value = playerStats.major.intelligence.GetValue();
                break;
            case StatType.Vitality:
                value = playerStats.major.vitality.GetValue();
                break;

            //Offensive Stats

            case StatType.Damage:
                value = playerStats.GetBaseDamage();
                break;
            case StatType.CritChance:
                value = playerStats.GetCritChance();
                break;
            case StatType.CritPower:
                value = playerStats.GetCritPower();
                break;
            case StatType.ArmorReduction:
                value = playerStats.GetArmorReduction() * 100;
                break;
            case StatType.AttackSpeed:
                value = playerStats.offense.attackSpeed.GetValue() * 100;
                break;
            

            // Defense Stats
            case StatType.MaxHealth:
                value = playerStats.GetMaxHealth();
                break;
            case StatType.HealthRegen:
                value = playerStats.resources.healthRegen.GetValue();
                break;
            case StatType.MaxMana:
                value = playerStats.GetMaxMana();
                break;
            case StatType.ManaRegen:
                value = playerStats.resources.manaRegen.GetValue();
                break;
            case StatType.Evasion:
                value = playerStats.GetEvasion();
                break;
            case StatType.Armor:
                value = playerStats.GetBaseArmor();
                break;

            // Elemental damage stats
            case StatType.IceDamage:
                value = playerStats.offense.iceDamage.GetValue();
                break;
            case StatType.FireDamage:
                value = playerStats.offense.lightningDamage.GetValue();
                break;
            case StatType.PoisonDamage:
                value = playerStats.GetElementalDamage(out ElementType element, 1);
                break;

            // Elemental resistances stats
            case StatType.IceResistance:
                value = playerStats.GetElementalResistance(ElementType.Ice) * 100;
                break;
            case StatType.FireResistance:
                value = playerStats.GetElementalResistance(ElementType.Fire) * 100;
                break;
            case StatType.PoisonResistance:
                value = playerStats.GetElementalResistance(ElementType.Poison) * 100;
                break;
            case StatType.LightningResistance:
                value = playerStats.GetElementalResistance(ElementType.Lightning) * 100;
                break;

            // sexual Stats
            case StatType.MaxArousal:
                value = playerStats.GetMaxArousel();
                break;
            case StatType.Stroke:
                value = playerStats.sex.stroke.GetValue();
                break;
            case StatType.Resilience:
                    value = playerStats.GetBaseResilience();
                break;
            case StatType.SexualDamage:
                value = playerStats.GetBaseSexDamage();
                break;
            case StatType.SexualRestraint:
                value = playerStats.sex.sexualRestraint.GetValue();
                break;
        }

        statValue.text = IsPercentageStat(statSlotType) ? value + "%"  : value.ToString();
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
            case StatType.Armor: return "Armor";
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

    private float GetEquippedItemStatValue(StatType statType)
    {
        if (ui == null || ui.hoveredItem == null)
        {
            Debug.LogWarning("No hovered item to compare for stat: " + statType);
            return 0f;
        }

        return ui.hoveredItem.GetStatValue(statType);
    }


}
