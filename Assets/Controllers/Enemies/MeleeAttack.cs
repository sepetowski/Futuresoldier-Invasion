using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour, IAttack
{

    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask playerMask;

    private float attackAnimationLength;
    public Coroutine attackCoroutine;


    public void Initialize(float animationLength, float damage, float maxHealth)
    {
        attackAnimationLength = animationLength - 1.5f;
        meleeDamage = damage;
        GetComponent<Health>().SetMaxHealth(maxHealth);
    }

    public void Attack()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        
        attackCoroutine = StartCoroutine(DealDamageAfterAnimation());
    }

    private IEnumerator DealDamageAfterAnimation()
    {
        yield return new WaitForSeconds(attackAnimationLength);

        if (GetComponent<Health>().CurrentHealth<=0) yield break;

        //Is player in range
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, attackRange, playerMask);
        foreach (Collider player in hitPlayers)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(meleeDamage);
            }
        }

        attackCoroutine = null;
    }

}