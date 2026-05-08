using System;
using System.Collections;
using UnityEngine;

public class Skill_Sword : Skill_Base
{
    private SkillObject_Sword currentSword;
    

    
    [Header("Spin Sword Upgrade")]
    [SerializeField] private GameObject spinSwordPrefab;
    public int maxDistance = 5;
    public float attacksPerSecond = 6;
    public float maxSpinDuration = 3;
    [Range(0, 10)]

    [Header("Moving Sword Upgrade")]
    [SerializeField] private float swordSpeed = 7f;

    [Header("Multicast Sword Upgrade")]
    [SerializeField] private int maxCharges = 3;
    [SerializeField] private float currentCharges;
    [SerializeField] private bool isRecharging;

    

    //[Header("Teleport Shard Upgrade")]
    //[SerializeField] private float shardExistDuration = 10;



    protected override void Awake()
    {
        base.Awake();
        currentCharges = maxCharges; // Initialize current charges to max charges
        
    }

    public override void TryUseSkill()
    {
        if (CanUseSkill() == false)
            return;

        if (Unlocked(SkillUpgradeType.Sword_Multicast))
        {
            HandleSwordMulticast();
            return;
        }

        if (Unlocked(SkillUpgradeType.Sword_MoveToEnemy))
        {
            HandleSwordRegular();
            return;
        }

        if (Unlocked(SkillUpgradeType.SwordSpin))
        {
            HandleSwordRegular();
            return;
        }
    }

    private Vector2 GetPlayerFacingDirection()
    {
        // Good for left/right 2D games using localScale flipping.
        if (player != null)
        {
            float facingX = player.transform.localScale.x;

            if (Mathf.Abs(facingX) > 0.01f)
                return facingX > 0 ? Vector2.right : Vector2.left;
        }

        // Fallback
        return transform.right;
    }

    private void HandleSwordMulticast()
    {
        if (currentCharges <= 0)
            return;

        CreateSword();
        currentSword.MoveTowardsClosestTarget(swordSpeed); // move shard towards closest target.
        currentCharges--;

        if (isRecharging == false)
            StartCoroutine(SwordRechargeCo());
    }

    private IEnumerator SwordRechargeCo()
    {
        isRecharging = true;

        while (currentCharges < maxCharges)
        {
            yield return new WaitForSeconds(cooldown);
            currentCharges++;
        }
        isRecharging = false;
    }

    

    private void HandleSwordRegular()
    {
        CreateSword();
        SetSkillOnCooldown();
    }

    public void CreateSword()
    {
        GameObject sword = Instantiate(spinSwordPrefab, transform.position, Quaternion.identity);

        currentSword = sword.GetComponent<SkillObject_Sword>();
        currentSword.SetupSword(this);

        if (Unlocked(SkillUpgradeType.Sword_MoveToEnemy))
        {
            currentSword.LaunchToClosestEnemy(swordSpeed);
        }
        else
        {
            Vector2 launchDirection = GetFacingDirection();
            currentSword.LaunchForward(launchDirection, swordSpeed);
        }
    }

    private Vector2 GetFacingDirection()
    {
        if (player == null)
            return Vector2.down;

        if (player.lastMoveDirection.sqrMagnitude < 0.01f)
            return Vector2.down;

        return player.lastMoveDirection.normalized;
    }

    public void CreateRawSword()
    {
        GameObject sword = Instantiate(spinSwordPrefab, transform.position, Quaternion.identity);

        SkillObject_Sword swordObject = sword.GetComponent<SkillObject_Sword>();
        swordObject.SetupSword(this);

        Vector2 launchDirection = GetFacingDirection();
        swordObject.LaunchForward(launchDirection, swordSpeed);
    }



    private void ForceCooldown()
    {
        if (OnCooldown() == false)
        {
            SetSkillOnCooldown();
            
        }
    }

}
