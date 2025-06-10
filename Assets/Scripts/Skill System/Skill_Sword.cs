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
        // check is mana is enough.


        if (CanUseSkill() == false)
            return;

        if (Unlocked(SkillUpgradeType.SwordSpin))
            HandleSwordRegular();

        if (Unlocked(SkillUpgradeType.Sword_MoveToEnemy))
            HandleSharingMoving();

        if (Unlocked(SkillUpgradeType.Sword_Multicast))
            HandleSwordMulticast();

        
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

    private void HandleSharingMoving()
    {
        // move shard around player.

        CreateSword();
        currentSword.MoveTowardsClosestTarget(swordSpeed); //

        SetSkillOnCooldown();
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

        



    }


    public void CreateRawSword()
    {
        bool canMove = Unlocked(SkillUpgradeType.Sword_MoveToEnemy) || Unlocked(SkillUpgradeType.Sword_Multicast);


        GameObject sword = Instantiate(spinSwordPrefab, transform.position, Quaternion.identity);
        sword.GetComponent<SkillObject_Sword>().SetupSword(this, canMove, swordSpeed);
    }

    

    private void ForceCooldown()
    {
        if (OnCooldown() == false)
        {
            SetSkillOnCooldown();
            
        }
    }

}
