
using UnityEngine;
using UnityEngine.UI;


public class Entity_Mana : MonoBehaviour
{
    [SerializeField] private Slider manaBar; // Reference to the mana bar UI element  
    private Entity entity;
    private Entity_Stats entityStats; // Reference to the Entity_Stats component for mana calculations  
    private Skill_Base skill; // Reference to the Skill component for skill-related functionality

    [SerializeField] protected float currentMana; // Current mana points, initialized to maximum mana  
    [SerializeField] protected bool isDead; // Flag to indicate if the entity is dead and cannot use mana  

    [SerializeField] protected float manaCost; // Cost of the last used ability or action that consumed mana  
    [SerializeField] protected bool isManaDepleted;
    [SerializeField] protected bool useMana; // Flag to indicate if mana was used in the last action  

    [Header("Mana regen")]
    [SerializeField] private float manaRegenInterval = 1f; // Amount of mana to regenerate per second  
    [SerializeField] private bool canRegenerateMana = true; // Flag to enable or disable mana regeneration  

    protected virtual void Awake()
    {
        skill = GetComponent<Skill_Base>(); // Get the Skill component attached to the same GameObject
        entity = GetComponent<Entity>(); // Get the Entity component attached to the same GameObject  
        entityStats = GetComponent<Entity_Stats>(); // Get the Entity_Stats component attached to the same GameObject  
        //manaBar = GetComponentInChildren<Slider>(); // Get the Slider component for the mana bar UI  
        currentMana = entityStats.GetMaxMana(); // Initialize current mana points to maximum mana  
        UpdateManaBar(); // Update the mana bar UI to reflect the initial mana points  
        InvokeRepeating(nameof(RegenerateMana), 0, manaRegenInterval); // Start the mana regeneration process at regular intervals  
    }



    public virtual bool UseMana(float manaCost)
    {
        if (isDead || isManaDepleted)
            return false; // If the entity is already dead or mana is depleted, do nothing  

        if (currentMana >= manaCost) // Check if there is enough mana to use  
        {
            currentMana -= manaCost; // Deduct the mana cost from current mana  
            UpdateManaBar(); // Update the mana bar UI  
            return true; // Mana usage was successful  
        }

        return false; // Not enough mana to use  
    }

    private void RegenerateMana()
    {
        if (canRegenerateMana == false)
            return; // If health regeneration is disabled, do nothing  

        float manaRegenAmount = entityStats.resources.manaRegen.GetValue(); // Get the health regeneration amount from the Entity_Stats component  
        IncreaseMana(manaRegenAmount); // Call the method to increase health points by the regeneration amount  
    }

    public void IncreaseMana(float manaRecoveredAmount)
    {
        if (isDead)
            return;

        float newMana = currentMana + manaRecoveredAmount; // Calculate the new health points after healing  
        float maxMana = entityStats.GetMaxMana(); // Get the maximum health points from the Entity_Stats component  

        // Ensure the new health does not exceed the maximum health  
        currentMana = Mathf.Min(newMana, maxMana); // Set the current health to the minimum of new health and maximum health  
        UpdateManaBar(); // Update the health bar UI to reflect the new health points  
    }

    public void ReduceMana(float manaCost) 
    {
        

        currentMana = currentMana - manaCost; // Reduce the mana points by the mana cost amount, ensuring it doesn't go below zero  
        UpdateManaBar(); // Update the mana bar UI to reflect the new mana points  

        if (currentMana < 0)
            return; // If current mana is less than or equal to zero, do nothing  

    }


    

    //private void Die()
    //{
    //    isDead = true; // Set the entity as dead  
    //    entity.EntityDeath(); // Call the EntityDeath method from the Entity class  
    //}

    private void UpdateManaBar()
    {
        if (manaBar == null)
            return; // If the mana bar is not assigned, do nothing  

        manaBar.value = currentMana / entityStats.GetMaxMana(); // Update the mana bar UI based on the current mana points  
    }

}
