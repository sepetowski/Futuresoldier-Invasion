using UnityEngine;
using System.Collections;

public class RangedAttack : MonoBehaviour, IAttack
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletsSpawnPosition;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private float bulletMaxRange = 50f;
    [SerializeField] private float spreadAmount = 1.0f;
    [SerializeField] private AudioClip shotSound; 


    private AudioSource audioSource;
    private float attackAnimationLength;
    private Coroutine attackCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public void Initialize(float animationLength, float damage, float maxHealth)
    {
        attackAnimationLength = animationLength -0.7f;
        bulletDamage = damage;
        GetComponent<Health>().SetMaxHealth(maxHealth);
    }
    public void Attack()
    {
        if (attackCoroutine != null)
           StopCoroutine(attackCoroutine);
        
        attackCoroutine = StartCoroutine(ShootAfterAnimation());
    }

    private IEnumerator ShootAfterAnimation()
    {

        yield return new WaitForSeconds(attackAnimationLength);

        ShootBullet();
        attackCoroutine = null;
    }

    private void ShootBullet()
    {
        Vector3 hitDirection = transform.forward;

        hitDirection.x += Random.Range(-spreadAmount, spreadAmount);
        hitDirection.y += Random.Range(-spreadAmount, spreadAmount);
        hitDirection = hitDirection.normalized;

 
        GameObject spawnedProjectile = Instantiate(bulletPrefab, bulletsSpawnPosition.position, Quaternion.LookRotation(hitDirection));
        BulletProjectal bullet = spawnedProjectile.GetComponent<BulletProjectal>();

        if (bullet != null)
        {
            bullet.direction = hitDirection;
            bullet.damage = bulletDamage;
            bullet.maxRange = bulletMaxRange;
            bullet.speed = bulletSpeed;
        }

        if (shotSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shotSound);
        }
    }
}