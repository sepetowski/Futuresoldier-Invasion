using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class CharacterStateController : MonoBehaviour
{

    [SerializeField] private LayerMask ignoreAimingLayerMask;

    [SerializeField] private Weapon equippedWeapon;
    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float deceleration = 3.5f;
    [SerializeField] private float movementAcceleration = 4.0f;
    [SerializeField] private float movementDeceleration = 6.0f;
    [SerializeField] private float maxWalkVelocity = 0.5f;
    [SerializeField] private float maxRunVelocity = 1.5f;
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private LayerMask targetingLayers;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] HealthBar healthBar;
    [SerializeField] private GameObject sniperPrefab;
    [SerializeField] private GameObject riflePrefab;
    [SerializeField] private GameObject shotgunPrefab;
    [SerializeField] private float gravity = -9.81f; 


    private GameObject selectedWeaponPrefab;
    private PlayerInputActions characterControls;
    private CharacterController characterController;
    private Animator animator;
    private Health health;
    private RigBuilder rigBuilder;

    private Vector2 currentMovementInput;
    private Vector3 currentVelocity;
    private Vector3 gravityVelocity;
    private bool isRunPressed;
    private bool isShootPreesed;
    private bool isGrounded;

    private float velocityZ = 0.0f;
    private float velocityX = 0.0f;
    private int velocityZHash;
    private int velocityXHash;

    private readonly int baseHealth = 100;

    void Awake()
    {
        rigBuilder = GetComponent<RigBuilder>();
        characterControls = new PlayerInputActions();
        ConfigureControls();

        InitWeaponType();
        ConfigureHealth();

        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        velocityZHash = Animator.StringToHash("Velocity Z");
        velocityXHash = Animator.StringToHash("Velocity X");
    }

    void ConfigureHealth()
    {
        health = GetComponent<Health>();
        var healthBonus = (GameDataController.Instance.GetHealthPlayerBonus());
        if (healthBonus != 0)
        {
            float healthMultiplier = 1 + (healthBonus / 100f);
            var newHealth = Mathf.RoundToInt(baseHealth * healthMultiplier);
            health.SetMaxHealth(newHealth);
        }
        else
        {
            health.SetMaxHealth(baseHealth);
        }

        healthBar.SetMaxHealth(health.MaxHealth);
        health.onHealthChanged.AddListener(UpdateHealthBar);
        health.onDeath.AddListener(OnPlayerDeath);
    }
    void ConfigureControls()
    {
        characterControls.Player.Move.started += OnMovementInput;
        characterControls.Player.Move.canceled += OnMovementInput;
        characterControls.Player.Move.performed += OnMovementInput;

        characterControls.Player.Shoot.started += ctx => isShootPreesed = true;
        characterControls.Player.Shoot.canceled += ctx => isShootPreesed = false;

        characterControls.Player.Run.started += ctx => isRunPressed = true;
        characterControls.Player.Run.canceled += ctx => isRunPressed = false;

        characterControls.Player.ShootMode.started += ctx => ToggleShootingMode();
        characterControls.Player.Relaod.started += ctx => OnRelaod();
    }

    public void ResetState(Vector3 position)
    {
        characterController.transform.position = position;
        health.onHealthChanged.AddListener(UpdateHealthBar);
        health.onDeath.AddListener(OnPlayerDeath);

        characterController.enabled = true;
        characterControls.Enable();
        rigBuilder.Build();
        animator.SetBool("IsDead", false);
        animator.SetFloat(velocityXHash, 0);
        animator.SetFloat(velocityZHash, 0);
        currentVelocity = Vector3.zero;
        gravityVelocity = Vector3.zero;


        health.ResetHealth();
        equippedWeapon.ResetWeapon();
    }


    void InitWeaponType()
    {
        var currentWeaponIndex = PlayerPrefs.GetInt("SelectedWeaponIndex", 0);


        Transform rightHand = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand");

        if (rightHand == null)
        {
            Debug.LogError("RightHand transform not found! Ensure the hierarchy path is correct.");
            return;
        }

        switch (currentWeaponIndex)
        {
            case 0:
                selectedWeaponPrefab = riflePrefab;
                break;
            case 1:
                selectedWeaponPrefab = shotgunPrefab;
                break;
            case 2:
                selectedWeaponPrefab = sniperPrefab;
                break;

        }


        if (selectedWeaponPrefab != null)
        {
            GameObject weaponInstance = Instantiate(selectedWeaponPrefab, rightHand);
            equippedWeapon = weaponInstance.GetComponent<Weapon>();


            weaponInstance.transform.localPosition = selectedWeaponPrefab.transform.localPosition;
            weaponInstance.transform.localRotation = selectedWeaponPrefab.transform.localRotation;
            weaponInstance.transform.localScale = selectedWeaponPrefab.transform.localScale;



            Transform rightHandGrip = weaponInstance.transform.Find("ref_right_hand_grip");
            Transform leftHandGrip = weaponInstance.transform.Find("ref_left_hand_grip");

            if (rightHandGrip != null && leftHandGrip != null)
            {
                TwoBoneIKConstraint rightHandIK = GameObject.Find("RightHandIK").GetComponent<TwoBoneIKConstraint>();
                TwoBoneIKConstraint leftHandIK = GameObject.Find("LeftHandIK").GetComponent<TwoBoneIKConstraint>();

                rightHandIK.data.target = rightHandGrip;
                leftHandIK.data.target = leftHandGrip;

            }

            equippedWeapon.ShowCorrectWeaponInUi();
            rigBuilder.Build();
        }
    }

    void OnEnable()
    {
        characterControls.Player.Enable();
    }

    void OnDisable()
    {
        characterControls.Player.Disable();
    }

    void OnMovementInput(InputAction.CallbackContext ctx) => currentMovementInput = ctx.ReadValue<Vector2>();

    void ToggleShootingMode()
    {
        equippedWeapon.ToggleFiringMode();
        Debug.Log("Firing mode switched: " + (equippedWeapon.IsSingleShotMode ? "Single Shot" : "Continuous"));
    }

    void OnRelaod()
    {
        equippedWeapon.Reload();
    }


    void Update()
    {
        if (!characterController.enabled) return;

        HandleMovement();
        Aim();
        HandleShooting();
    }

    void HandleShooting()
    {
        if (isShootPreesed && equippedWeapon != null && equippedWeapon.CanShoot)
        {
            var (success, _) = GetMousePosition();
            if (success)
            {
                equippedWeapon.Shoot(isRunPressed);

                if (equippedWeapon.IsSingleShotMode)
                {
                    isShootPreesed = false;
                }
            }
        }
    }

    void HandleMovement()
    {

        gravityVelocity.y += gravity * Time.deltaTime;

        Vector3 movement = currentVelocity + gravityVelocity;
        characterController.Move(movement * Time.deltaTime);

        var currentMaxVelocity = isRunPressed ? maxRunVelocity : maxWalkVelocity;
        bool forwardPressed = currentMovementInput.y > 0.1f;
        bool backwardPressed = currentMovementInput.y < -0.1f;
        bool leftPressed = currentMovementInput.x < -0.1f;
        bool rightPressed = currentMovementInput.x > 0.1f;

        Vector3 inputDirection = new Vector3(currentMovementInput.x, 0, currentMovementInput.y);
        Vector3 targetMovement = transform.TransformDirection(inputDirection.normalized);

        targetMovement *= isRunPressed ? runSpeed : walkSpeed;

        if (inputDirection.magnitude > 0.1f)
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovement, Time.deltaTime * movementAcceleration);
        
        else   
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * movementDeceleration);
        

        characterController.Move(currentVelocity * Time.deltaTime);
        ChangeVelocity(forwardPressed, backwardPressed, leftPressed, rightPressed, isRunPressed, currentMaxVelocity);
        LockOrReset(forwardPressed, backwardPressed, leftPressed, rightPressed, isRunPressed, currentMaxVelocity);

        animator.SetFloat(velocityZHash, velocityZ);
        animator.SetFloat(velocityXHash, velocityX);
    }

    void ChangeVelocity(bool forwardPressed, bool backwardPressed, bool leftPressed, bool rightPressed, bool runPressed, float currentMaxVelocity)
    {
        // go forward
        if (forwardPressed && velocityZ < currentMaxVelocity)
            velocityZ += Time.deltaTime * acceleration;

        // go backward
        if (backwardPressed && velocityZ > -currentMaxVelocity)
            velocityZ -= Time.deltaTime * acceleration;

        // go left
        if (leftPressed && velocityX > -currentMaxVelocity)
            velocityX -= Time.deltaTime * acceleration;

        // go right
        if (rightPressed && velocityX < currentMaxVelocity)
            velocityX += Time.deltaTime * acceleration;

        // stop forward
        if (!forwardPressed && velocityZ > 0.0f)
            velocityZ -= Time.deltaTime * deceleration;

        // stop backward
        if (!backwardPressed && velocityZ < 0.0f)
            velocityZ += Time.deltaTime * deceleration;

        // stop left
        if (!leftPressed && velocityX < 0.0f)
            velocityX += Time.deltaTime * deceleration;

        // stop rigth
        if (!rightPressed && velocityX > 0.0f)
            velocityX -= Time.deltaTime * deceleration;
    }

    void LockOrReset(bool forwardPressed, bool backwardPressed, bool leftPressed, bool rightPressed, bool runPressed, float currentMaxVelocity)
    {
        // reset forward/backward
        if (!forwardPressed && !backwardPressed && velocityZ < 0.05f && velocityZ > -0.05f)
            velocityZ = 0.0f;

        // reset left/rigth
        if (!leftPressed && !rightPressed && velocityX != 0.0f && (velocityX > -0.05f && velocityX < 0.05f))
            velocityX = 0.0f;

        // lock forward
        if (forwardPressed && runPressed && velocityZ > currentMaxVelocity)
            velocityZ = currentMaxVelocity;
        else if (forwardPressed && velocityZ > currentMaxVelocity)
        {
            velocityZ -= Time.deltaTime * deceleration;
            if (velocityZ > currentMaxVelocity && velocityZ < (currentMaxVelocity + 0.05f))
                velocityZ = currentMaxVelocity;
        }
        else if (forwardPressed && velocityZ < currentMaxVelocity && velocityZ > (currentMaxVelocity - 0.05f))
            velocityZ = currentMaxVelocity;

        // lock backward
        if (backwardPressed && runPressed && velocityZ < -currentMaxVelocity)
            velocityZ = -currentMaxVelocity;
        else if (backwardPressed && velocityZ < -currentMaxVelocity)
        {
            velocityZ += Time.deltaTime * deceleration;
            if (velocityZ < -currentMaxVelocity && velocityZ > (-currentMaxVelocity - 0.05f))
                velocityZ = -currentMaxVelocity;
        }
        else if (backwardPressed && velocityZ > -currentMaxVelocity && velocityZ < (-currentMaxVelocity + 0.05f))
            velocityZ = -currentMaxVelocity;
    }

    private void Aim()
    {
        var (success, position) = GetMousePosition();
        if (success)
        {
            var direction = position - transform.position;
            direction.y = 0;
            transform.forward = direction;
        }
    }

    private (bool success, Vector3 position) GetMousePosition()
    {
        //due to camera angle
        int targetingLayerMask = ~ignoreAimingLayerMask;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        while (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, targetingLayerMask))
        {
            if (((1 << hitInfo.collider.gameObject.layer) & ignoreAimingLayerMask) != 0)
            {
                ray.origin = hitInfo.point + ray.direction * 0.01f;
                continue;
            }

            return (success: true, position: hitInfo.point);
        }

        return (success: false, position: Vector3.zero);
    }

    private void UpdateHealthBar(float currentHealth)
    {
        healthBar.SetHealth(currentHealth);
    }

    private void OnPlayerDeath()
    {
        UpdateHealthBar(0);
        health.onHealthChanged.RemoveListener(UpdateHealthBar);
        health.onDeath.RemoveListener(OnPlayerDeath);
        disablePlayer();
        animator.SetBool("IsDead", true);
        rigBuilder.Clear();
    }

    public void disablePlayer()
    {
        isShootPreesed = false;
        isRunPressed = false;
        characterController.enabled = false;
        CancelInvoke();
        StopAllCoroutines();

        currentVelocity = Vector3.zero;
        characterControls.Disable();
    }

    private void OnDestroy()
    {
        rigBuilder.Clear();
    }


    private void OnTriggerEnter(Collider other)
    {

        var layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("HealthPack"))
        {

            if (health != null)
                health.Heal(50f);
            Debug.Log("Player picked up a HealthPack. Health restored by 50.");


            Destroy(other.gameObject);
        }
        else if (layer == LayerMask.NameToLayer("AmmoPack"))
        {

            if (equippedWeapon != null)
                equippedWeapon.AddMagazine();



            Destroy(other.gameObject);
        }
    }
}
