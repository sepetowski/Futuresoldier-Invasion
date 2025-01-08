using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private Sprite weaponImage;
    [SerializeField] private LineRenderer laserSight;
    [SerializeField] private Transform laserSpawnPosition;
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected Transform bulletsSpawnPosition;
    [SerializeField] private float shotLightSpeed =0.2f;
    [SerializeField] protected float bulletSpeed=25f;
    [SerializeField] protected float laserLength = 5f;
    [SerializeField] protected int damage = 10;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private int maxMagazineSize = 30;
    [SerializeField] private int maxMagazines = 4;
    [SerializeField] protected float baseSpread = 1.0f;
    [SerializeField] protected float runningSpreadMultiplier = 2.0f;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private string weaponName;
    [SerializeField] protected bool isSingleShotMode = false;
    [SerializeField] private bool canToggleMode = false;
    [SerializeField] private ParticleSystem glowEffect;
    [SerializeField] private ParticleSystem sparksEffect;
    [SerializeField] private Light shotLight;
    [SerializeField] private string mode = "Automatic";
    [SerializeField] private string weaponUpgradesName;
    [SerializeField] protected AudioClip shotSound;

    private int currentDmg = 0;
    private int currentMaxAmo = 0;


    protected SoliderInfo soliderInfo;
    protected AudioSource audioSource;

    protected int currentAmmoInMagazine;
    protected int remainingMagazines;
    protected float timeSinceLastShot = 0f;
    private bool isReloading = false; 
    public bool IsSingleShotMode => isSingleShotMode;
    public bool CanShoot => !isReloading && timeSinceLastShot >= fireRate && currentAmmoInMagazine > 0;
    public void ShowCorrectWeaponInUi() => soliderInfo.ShowWeaponUI(weaponName);
    public string GetWeaponUpgradesName() => weaponUpgradesName;

    void Awake()
    {
        currentDmg = damage;
        currentMaxAmo = maxMagazineSize;
        audioSource = GetComponent<AudioSource>();
        soliderInfo = FindObjectOfType<SoliderInfo>();

        currentAmmoInMagazine = currentMaxAmo;
        remainingMagazines = maxMagazines - 1;

        soliderInfo.SetMaxAmmo(currentMaxAmo);
        soliderInfo.SetMagAmmount(remainingMagazines);
        soliderInfo.UpdateFireModeUI(isSingleShotMode);
    }


    void Update()
    {
        timeSinceLastShot += Time.deltaTime;
        UpdateLaserSight();
    }

    public void ToggleFiringMode()
    {
        if (!canToggleMode) return;
        
        isSingleShotMode = !isSingleShotMode;
        soliderInfo.UpdateFireModeUI(isSingleShotMode);
    }

    public virtual void Shoot(bool isRunning)
    {
        if (!CanShoot) return;

        timeSinceLastShot = 0f;
        currentAmmoInMagazine--;

        soliderInfo.SetAmmo(currentAmmoInMagazine);

        Vector3 hitDirection = bulletsSpawnPosition.forward;

        float spreadAmount = isRunning ? runningSpreadMultiplier : baseSpread;
        if (isSingleShotMode)
            spreadAmount *= spreadAmount;

        hitDirection.x += Random.Range(-spreadAmount, spreadAmount);
        hitDirection = hitDirection.normalized;

        GameObject spawnedProjectile = Instantiate(bulletPrefab, bulletsSpawnPosition.position, bulletsSpawnPosition.rotation);
        BulletProjectal bullet = spawnedProjectile.GetComponent<BulletProjectal>();
        bullet.direction = hitDirection;
        bullet.damage = currentDmg;
        bullet.maxRange = laserLength + 1f;
        bullet.speed = bulletSpeed;

        audioSource.PlayOneShot(shotSound);
        InitMuzzleeShootEffect();
    }


    public void UpdateWeaponStats(int newDmgBuff, int newMagazineSizeBuff)
    {
        float dmgMultiplier = 1 + (newDmgBuff / 100f);
        float magazineMultiplier = 1 + (newMagazineSizeBuff / 100f);

        currentDmg = Mathf.RoundToInt(damage * dmgMultiplier);
        currentMaxAmo = Mathf.RoundToInt(maxMagazineSize * magazineMultiplier);
    }

    public WeaponInformation GetWeaponInforamtion()
    {
        return new()
        {
            Name= weaponName,
            Damage = currentDmg,
            Magazines = maxMagazines,
            MagazineSize = currentMaxAmo,
            Range = laserLength,
            ReloadTime = reloadTime,
            FireMode = mode,
            weaponImage= weaponImage
        };
        
    }

    public void Reload()
    {
        if (isReloading || remainingMagazines <= 0 || currentAmmoInMagazine ==currentMaxAmo) return;
        soliderInfo.SetAmmo(0);

        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        float reloadProgress = 0f;


        while (reloadProgress < reloadTime)
        {
            reloadProgress += Time.deltaTime;
            float ammoFill = Mathf.Lerp(0, currentMaxAmo, reloadProgress / reloadTime);
            soliderInfo.SetAmmo(Mathf.RoundToInt(ammoFill)); 
            yield return null;
        }

        remainingMagazines--;
        currentAmmoInMagazine = currentMaxAmo;

        isReloading = false;

        soliderInfo.SetAmmo(currentAmmoInMagazine);
        soliderInfo.SetMagAmmount(remainingMagazines);
    }

    public void AddMagazine()
    {

        if (remainingMagazines < maxMagazines - 1)
            remainingMagazines++;


        soliderInfo.SetMagAmmount(remainingMagazines);
    }

    public void ResetWeapon()
    {
        currentAmmoInMagazine = currentMaxAmo;
        remainingMagazines = maxMagazines - 1; 

        soliderInfo.SetMaxAmmo(currentMaxAmo);
        soliderInfo.SetMagAmmount(remainingMagazines);
        soliderInfo.UpdateFireModeUI(isSingleShotMode);
    }

    public void BackToInitial()
    {
        currentDmg = damage;
        currentMaxAmo = maxMagazineSize;
    }

    void UpdateLaserSight()
    {
        Vector3 startPosition = laserSpawnPosition.position;
        laserSight.SetPosition(0, startPosition);

        Vector3 endPosition = startPosition + (laserSpawnPosition.forward * laserLength);


        if (Physics.Raycast(startPosition, laserSpawnPosition.forward, out RaycastHit hitInfo, laserLength))
        {
            Vector3 hitPoint = hitInfo.point;
            laserSight.SetPosition(1, hitPoint);
        }
        else
        {
            laserSight.SetPosition(1, endPosition);
        }
    }

    protected void InitMuzzleeShootEffect()
    {
        if (!glowEffect.isPlaying)
        {
            glowEffect.Play();
        }
    
        if ( !sparksEffect.isPlaying)
        {
            sparksEffect.Play();
        }

        if (!shotLight.enabled)
        {
            shotLight.enabled = true;
            Invoke(nameof(TurnOffMuzzleLight), shotLightSpeed);
        }
    }

    private void TurnOffMuzzleLight ()=> shotLight.enabled = false;
}



[System.Serializable]
public class WeaponInformation
{
    public string Name;
    public int Damage;
    public int Magazines;
    public int MagazineSize;
    public float ReloadTime;
    public float Range;
    public string FireMode;
    public Sprite weaponImage;
}