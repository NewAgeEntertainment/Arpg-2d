using System;
using UnityEngine;


public class SkillObject_Shard : SkillObject_Base
{

    [SerializeField] private GameObject vfxPrefeb;

    private Transform target;
    private float speed;

    private void Update()
    {
        if (target == null)
            return;

        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }

    // private void MovearoundPlayer(float speed)
    //{
    //
    //}

    public void MoveTowardsClosestTarget(float speed)
    {
        target = FindClosestTarget();
        this.speed = speed;
    }

    public void SetupShard(float detinationTime)
    {
        Invoke(nameof(Explode), detinationTime);
    }


    private void Explode()
    {
        DamageEnemiesInRadius(transform, checkRadius);
        Instantiate(vfxPrefeb, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Enemy>() == null)
            return;

        Explode();
    }
}
