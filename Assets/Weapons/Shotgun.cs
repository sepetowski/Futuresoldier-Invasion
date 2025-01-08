using UnityEngine;

public class Shotgun : Weapon
{
    [SerializeField] private float angle = 15f;

    public override void Shoot(bool isRunning)
    {
        if (!CanShoot) return;

        timeSinceLastShot = 0f;
        currentAmmoInMagazine -= 3;

        soliderInfo.SetAmmo(currentAmmoInMagazine);

        float[] spreadAngles = { -angle, 0f, angle }; 

        foreach (float baseAngle in spreadAngles)
        {

            Vector3 hitDirection = Quaternion.Euler(0, baseAngle, 0) * bulletsSpawnPosition.forward;
            hitDirection = hitDirection.normalized;


            GameObject spawnedProjectile = Instantiate(bulletPrefab, bulletsSpawnPosition.position, bulletsSpawnPosition.rotation);
            BulletProjectal bullet = spawnedProjectile.GetComponent<BulletProjectal>();

            bullet.direction = hitDirection;
            bullet.damage = damage;
            bullet.maxRange = laserLength + 1f;
            bullet.speed = bulletSpeed;

            audioSource.PlayOneShot(shotSound);          
            InitMuzzleeShootEffect();
        }
    }
}
