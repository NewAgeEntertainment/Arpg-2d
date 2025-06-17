using UnityEngine;


public enum SkillUpgradeType
{
    None,

    // ------ Dash Tree -------
    Dash, // dash to avoid Damage
    Dash_CloneOnStart, // dash to create a clone on start
    Dash_CloneOnStartAndArrival, // dash to create a clone on arrival
    Dash_ShardOnStart, // dash to create a shard on start
    Dash_ShardOnStartAndArrival, // dash to create a shard on arrival

    // ------ Thrust Tree -------
    Thrust, // thrust to damage enemies

    //------- Shard Tree -------
    Shard, // create a time shard
    Shard_MoveToEnemy, // create a time shard that moves to the enemy
    Shard_Multicast, // create a time shard that casts 3 shards
    Shard_Teleport, // create a time shard that teleports the player to the shard
    Shard_TeleportHpRewind, // create a time shard that teleports the player to the shard and heals the player

    // ------ Sword Tree -------
    SwordSpin, // Your sword will spin at one point and damage enemies. Like a chainsaw
    Sword_MoveToEnemy, // create a time shard that moves to the enemy
    Sword_Multicast, // create a time shard that casts 3 shards
    SwordThrow_Bounce, // Bounce sword will bounce between enemies

    DeepBreath, // Deep Breath skill to heal the player
}
