using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bonus : MonoBehaviour
{
    public BonusType BonusType;
    private Animator animator;

    public void Awake()
    {
        animator = GetComponent<Animator>();
        transform.localScale = new Vector3(.75f, .75f, .75f);
    }

    public void FixedUpdate()
    {
        animator.SetBool("Done", true);
    }

    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Destroy(gameObject);
            MyCustomMap.EatBonus(transform.position, other.transform.position);
            var player = other.GetComponent<PlayerUnit>();
            animator.SetBool("Done", false);

            switch(BonusType)
            {
                case BonusType.IncrRange:
                    player.explosion_power++;
                    break;
                case BonusType.ExtraBomb:
                    player.avalibleBomb++;
                    break;
                case BonusType.SpeedUp:
                    player.moveSpeed++;
                    break;
                case BonusType.Kick:
                    player.canKick = true;
                    break;
            }
        }
        else if(other.CompareTag("Explosion"))
        {
            var box = other.gameObject.GetComponent<BoxCollider>();
            if (box.isTrigger)
            {
                Destroy(gameObject);
                MyCustomMap.RemoveBonus(transform.position);
            } 
        }
    }
}
