using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityRandom = UnityEngine.Random;

public class EnemyController : MonoBehaviour
{
    public static event Action OnEnemyDeath;

    [SerializeField] private LayerMask groundMask, playerMask;

    [SerializeField] private Vector3 walkPoint;
    [SerializeField] private float walkPointRange;
    [SerializeField] private float timeBetweenAttacks;
    [SerializeField] private float sightRange, attackRange;
    [SerializeField] private float patrolAreaRange = 20f;
    [SerializeField] private float minDistanceToPlayer = 2f;
    [SerializeField] private float requiredTimeInRange = 1f; 
    [SerializeField] private float randomOffsetRange = 3f; 
    [SerializeField] private float offsetRefreshRate = 1f; 
    [SerializeField] private string attackAnimationName;
    [SerializeField] private bool isMeleeEnemy = false;
    [SerializeField] private float waitTimeAtPoint = 10f; 
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private GameObject ammoPickupPrefab;
    [SerializeField] private float dropChance = 0.1f; 
    [SerializeField] private float healthDropChance = 0.4f;

    public float enemyDamge;
    public float enemyMaxHealth;

    private float lastOffsetTime;
    private Vector3 chaseTargetPosition;
    private IAttack attackBehavior;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Health health;
    private Collider enemyCollider;
    private bool alreadyAttacked;
    private bool walkPointSet;
    private bool playerInSightRange, playerInAttackRange;
    private bool isAttacking;
    private float timeInAttackRange = 0f;
    private float attackAnimationLength;
    private bool isAlive = true;
    private bool isWaiting = false;


    private void Awake()
    {
        player = GameObject.Find("Soldier").transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); 
        attackBehavior = GetComponent<IAttack>();
        health = GetComponent<Health>();
        enemyCollider = GetComponent<Collider>();
        health.onDeath.AddListener(OnDeath);
    }

    private void Start()
    {
        agent.Warp(transform.position);
        attackAnimationLength = animator.runtimeAnimatorController.animationClips.FirstOrDefault(a => a.name == attackAnimationName).length;
        enemyDamge = isMeleeEnemy? enemyDamge *2f :enemyDamge;
        enemyMaxHealth = isMeleeEnemy? enemyMaxHealth *1.5f :enemyMaxHealth;
        attackBehavior?.Initialize(attackAnimationLength, enemyDamge, enemyMaxHealth);
    }

    private void Update()
    {
        if (isAlive)
            HandleEnemyState();
    }

    private void HandleEnemyState()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, playerMask);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerMask);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= minDistanceToPlayer)
        {
            RetreatFromPlayer();
        }
        else if (playerInAttackRange && playerInSightRange && Vector3.Dot((player.position - transform.position).normalized, transform.forward) > 0)
        {
            AttackPlayer();
        }
        else if (playerInAttackRange && playerInSightRange)
        {
            timeInAttackRange += Time.deltaTime;
            if (timeInAttackRange >= requiredTimeInRange)
                AttackPlayer();
            else
                ChasePlayer();
        }
        else
        {
            timeInAttackRange = 0f;

            if (playerInSightRange && !playerInAttackRange)
            {
                ChasePlayer();
            }
            else if (!playerInSightRange)
            {
                agent.isStopped = false;
                Patroling();
            }
        }
    }

    private void Patroling()
    {
        if (isAttacking || isWaiting) return;

        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet)
        {
            animator.SetBool("isWalking", true);
            animator.SetBool("isAttacking", false);

            Vector3 directionToWalkPoint = (walkPoint - transform.position).normalized;
            directionToWalkPoint.y = 0;
            if (directionToWalkPoint != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToWalkPoint);
            }

            agent.SetDestination(walkPoint);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
            StartCoroutine(WaitAtPoint());
        }
    }

    private IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        agent.isStopped = true; 
        animator.SetBool("isWalking", false);

        float elapsedTime = 0f;

        while (elapsedTime < waitTimeAtPoint)
        {
            playerInSightRange = Physics.CheckSphere(transform.position, sightRange, playerMask);
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerMask);

            if (playerInSightRange && agent.isActiveAndEnabled)
            {
                isWaiting = false;
                agent.isStopped = false;
                HandleEnemyState();
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isWaiting = false;
        agent.isStopped = false;
    }

    private void SearchWalkPoint()
    {
        float randomZ = UnityRandom.Range(-patrolAreaRange, patrolAreaRange);
        float randomX = UnityRandom.Range(-patrolAreaRange, patrolAreaRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, groundMask))
        {
            walkPointSet = true;
        }
    }


    private void ChasePlayer()
    {
        if (isAttacking) return;
        agent.isStopped = false;
        animator.SetBool("isWalking", true);
        animator.SetBool("isAttacking", false);

        if (isMeleeEnemy)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            if (Time.time - lastOffsetTime >= offsetRefreshRate)
            {
                Vector3 randomOffset = new Vector3(
                    UnityRandom.Range(-randomOffsetRange, randomOffsetRange),
                    0,
                    UnityRandom.Range(-randomOffsetRange, randomOffsetRange)
                );

                chaseTargetPosition = player.position + randomOffset;
                lastOffsetTime = Time.time;
            }

            agent.SetDestination(chaseTargetPosition);
        }
    }
    private void AttackPlayer()
    {
        if (alreadyAttacked) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        alreadyAttacked = true;
        isAttacking = true;
        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", true);

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        attackBehavior?.Attack();

        float waitTime = Mathf.Max(timeBetweenAttacks, attackAnimationLength);
        Invoke(nameof(ResetAttack), waitTime);
    }


    private void ResetAttack()
    {
        alreadyAttacked = false;

        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerMask);

        if (playerInAttackRange)
        {
            isAttacking = true;
            animator.SetBool("isAttacking", true);
            Invoke(nameof(EndShooting), attackAnimationLength);
        }
        else
        {
            EndShooting();
        }
    }

    private void EndShooting()
    {
        isAttacking = false;
        animator.SetBool("isAttacking", false);
        animator.SetBool("isWalking", true);

        if (playerInSightRange && !playerInAttackRange)
        {
            ChasePlayer();
        }
        else if (!playerInSightRange)
        {
            Patroling();
        }
    }

    private void RetreatFromPlayer()
    {
        if (isAttacking || isMeleeEnemy) return; 
        animator.SetBool("isWalking", true);
        animator.SetBool("isAttacking", false);

        Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
        Vector3 retreatPosition = transform.position + directionAwayFromPlayer * 5f;
        agent.SetDestination(retreatPosition);
    }
    private void OnDeath()
    {
        if (!isAlive) return;

        isAlive = false;
        isAttacking = false;
        alreadyAttacked = false;

        CancelInvoke();
        StopAllCoroutines();
        health.onDeath.RemoveListener(OnDeath);

        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", false);
        animator.SetBool("isDead", true);

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.enabled = false;
        enemyCollider.enabled = false;

        OnEnemyDeath?.Invoke();
        StartCoroutine(DestroyAfterDeathAnimation());
    }
    private IEnumerator DestroyAfterDeathAnimation()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;

        yield return new WaitForSeconds(animationLength);

        TryDropPickup();
        Destroy(gameObject, 10f);
    }

    private void TryDropPickup()
    {
        float randomValue = UnityRandom.Range(0f, 1f);

        if (randomValue <= dropChance)
        {
            float itemTypeRoll = UnityRandom.Range(0f, 1f);

            GameObject pickupToSpawn = null;

            if (itemTypeRoll <= healthDropChance)
            {
                pickupToSpawn = healthPickupPrefab; 
            }
            else
            {
                pickupToSpawn = ammoPickupPrefab; 
            }

            if (pickupToSpawn != null)
            {
                Instantiate(pickupToSpawn, transform.position, Quaternion.identity);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minDistanceToPlayer);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolAreaRange);
    }
}