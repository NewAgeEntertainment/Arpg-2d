using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/ManaHeal effect", fileName = "Item effect data - manaheal")]
public class ItemEffect_ManaRestore : ItemEffect_DataSO
{
    [SerializeField] private float restorePercent = .1f;



    public override void ExecuteEffect()
    {

        Player player = FindFirstObjectByType<Player>();

        float restoreAmount = player.stats.GetMaxMana() * restorePercent;

        player.mana.IncreaseMana(restoreAmount);

        
    }
}
