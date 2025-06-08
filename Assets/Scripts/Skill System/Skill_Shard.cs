using System;
using System.Collections;
using UnityEngine;

public class Skill_Shard : Skill_Base
{
    private SkillObject_Shard currentShard;

    [SerializeField] private GameObject shardPrefab;
    [SerializeField] private float detonateTime = 2;

    [Header("Moving Shard Upgrade")]
    [SerializeField] private float shardSpeed = 7f;

    [Header("Multicast Shard Upgrade")]
    [SerializeField] private int maxCharges = 3;
    [SerializeField] private float currentCharges;
    [SerializeField] private bool isRecharging;

    [Header("Teleport Shard Upgrade")]
    [SerializeField] private float shardExistDuration = 10;

    protected override void Awake()
    {
        base.Awake();
        currentCharges = maxCharges; // Initialize current charges to max charges
    }

    public override void TryUseSkill()
    {
        // check is mana is enough.

        if (CanUseSkill() == false)
            return;

        if (Unlocked(SkillUpgradeType.Shard))
            HandleShardRegular();

        if (Unlocked(SkillUpgradeType.Shard_MoveToEnemy))
            HandleSharingMoving();

        if (Unlocked(SkillUpgradeType.Shard_Multicast))
            HandleShardMulticast();

    }

    private void HandleShardTelport()
    {
        if (currentShard == null)
        {
            CreateShard();
        }
        else
        {
            SwapPlayerAndShard();
            SetSkillOnCooldown();
        }
    }

    private void SwapPlayerAndShard()
    {
        Vector3 shardPosition = currentShard.transform.position;
        Vector3 playerPosition = player.transform.position;

        currentShard.transform.position = playerPosition;
        player.transform.position = shardPosition;
    }

    private void HandleShardMulticast()
    {
        if (currentCharges <= 0)
            return;

        CreateShard();
        currentShard.MoveTowardsClosestTarget(shardSpeed); // move shard towards closest target.
        currentCharges--;

        if ( isRecharging == false)
            StartCoroutine(ShardRechargeCo());
    }

    private IEnumerator ShardRechargeCo()
    {
        isRecharging = true;

        while (currentCharges < maxCharges)
        {
            yield return new WaitForSeconds(cooldown);
            currentCharges++;
        }
        isRecharging = false;
    }

    private void HandleSharingMoving()
    {
        // move shard around player.

        CreateShard();
        currentShard.MoveTowardsClosestTarget(shardSpeed); //
        
        SetSkillOnCooldown();
    }

    private void HandleShardRegular()
    {
        CreateShard();
        SetSkillOnCooldown();
    }

    public void CreateShard()
    {
        float detonateTime = GetDetonateTime();

        GameObject shard = Instantiate(shardPrefab, transform.position, Quaternion.identity);
        currentShard = shard.GetComponent<SkillObject_Shard>();
        currentShard.SetupShard(detonateTime);
    }

    public float GetDetonateTime()
    {
        if (Unlocked(SkillUpgradeType.Shard_Teleport) || Unlocked(SkillUpgradeType.Shard_TeleportAndHeal))
            return shardExistDuration;
    
            return detonateTime;
    }

}
