using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using UnityEngine;


public class PlayerManager : Damageable
{
    [SerializeField] private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    
    [Header("Player Movement")]
    private float playerSpeed = 3f;
    public float playerSpeedMultiplier = 2f;
    public float playerHealthySpeed = 3f;
    public float playerBleedingSpeed = 1.5f;
    public float gravity = -9.8f;
    public float jumpHeight = 1.5f;
    private int jumpXDirection;
    private int jumpZDirection;
    private bool isStunned = false;

    [Header("Player Look")]
    public Camera cam;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;
    private float xRotation = 0f;

    [Header("Player Crouch")]
    public float crouchTimer = 0f;
    private bool lerpCrouch;
    private bool isCrouching = false;
    private float crouchSpeedReduction = 0.5f;
    private float crouchJumpReduction = 0.7f;

    [Header("Player Sprint")]
    [SerializeField] private int sprintUsePerTick = 5;
    [SerializeField] private int sprintRecoveryPerTick = 5;
    public int MaxSprintStamina = 100;
    private int sprintStamina;
    private bool isSprinting = false;

    private PlayerUI playerUI;
    private InventoryManager inventoryManager;

    // OS Variables //
    private bool isMacUser = false;

    // Start is called before the first frame update
    void Start()
    {
        if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
        {
            isMacUser = true;
        }

        sprintStamina = MaxSprintStamina;
        
        controller = GetComponent<CharacterController>();
        playerSpeed = playerHealthySpeed;
        playerUI = GetComponent<PlayerUI>();
        inventoryManager = GetComponent<InventoryManager>();
        
        TimeManager.instance.Tick += UseAndRecoverSprint;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;

        // Calls the implement crouch function
        if (lerpCrouch)
        {
            ImplementCrouch();
        }

        float percentSprint = sprintStamina / (float)MaxSprintStamina;
        playerUI.UpdateSprintBar(percentSprint);
    }

    public void SetPosition(Vector3 position)
    {
        controller.transform.position = position;
    }


    /// <summary>
    /// Called by Tick from TimeManager
    /// </summary>
    public void UseAndRecoverSprint()
    {
        if (isSprinting && sprintStamina > 0)
        {
            sprintStamina -= sprintUsePerTick;
            if (sprintStamina < 0)
            {
                sprintStamina = 0;
                playerSpeed = DefaultSpeed();
                isSprinting = false;
                //TODO: be slower for a bit vv
            }
        }
        else if (!isSprinting && sprintStamina != MaxSprintStamina)
        {
            sprintStamina += sprintRecoveryPerTick;

            if (sprintStamina > MaxSprintStamina)
            {
                sprintStamina = MaxSprintStamina;
            }
        }
        else if (!isSprinting && sprintStamina == MaxSprintStamina)
        {
            playerUI.ShowSprintBar(false);
        }
    }

    /// <summary>
    /// Changes the player speed based their condition of health. Overriden from
    /// Damageable.
    /// </summary>
    protected override void ChangeSpeed()
    {
        playerUI.UpdateHealthBar((float)Health / (float)MaxHealth);
        switch (_HealthStatus)
        {
            case HealthStatus.Healthy:
                playerSpeed = playerHealthySpeed;
                break;
            case HealthStatus.Bleeding:
                playerSpeed = playerBleedingSpeed;
                isCrouching = true;
                playerSpeed *= crouchSpeedReduction;
                jumpHeight -= crouchJumpReduction;

                // Stop sprint if is sprinting
                if (isSprinting)
                {
                    isSprinting = false;
                    playerSpeed = DefaultSpeed();
                }
                break;
        }
    }

    public void ResetHealth()
    {
        Health = MaxHealth;
        ChangeSpeed();
    }

    /// <summary>
    /// Receives movement inputs for InputManager.cs and applies them to the character controller.
    /// </summary>
    /// <param name="input"></param>
    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        
        if (!isGrounded)
        {
            if (input.x > 0)
            {
                if (jumpXDirection < 0)
                {
                    input.x *= -1;
                }
            }
            else if (input.x < 0)
            {
                if (jumpXDirection > 0)
                {
                    input.x *= -1;
                }
            }

            if (input.y > 0)
            {
                if (jumpZDirection < 0)
                {
                    input.y *= -1;
                }
            }
            else if (input.y < 0)
            {
                if (jumpZDirection > 0)
                {
                    input.y *= -1;
                }
            }
        }
        
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        if (moveDirection.x == 0 && moveDirection.z == 0)
        {
            // Stop sprinting if not moving
            if (isSprinting)
            {
                Sprint();
            }
        }

        if (isStunned)
        {
            moveDirection = Vector3.zero;
        }
        

        controller.Move(transform.TransformDirection(moveDirection) * playerSpeed * Time.deltaTime);

        // Accounts for gravity
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    /// Receive jump inputs for InputManager.cs and apply them to the character controller
    public void Jump()
    {
        playerVelocity.y = Mathf.Sqrt(jumpHeight * -1.5f * gravity);
        
        if (playerVelocity.x > 0)
        {
            jumpXDirection = 1;
        }
        else if (playerVelocity.x < 0)
        {
            jumpXDirection = -1;
        }

        if (playerVelocity.z > 0)
        {
            jumpZDirection = 1;
        }
        else if (playerVelocity.z < 0)
        {
            jumpZDirection = -1;
        }
    }

    /// Receive looking inputs for InputManager.cs and apply them to the character controller
    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        // Calculate the camera rotation for looking up and down
        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80, 80); // has a min of -80 and max of 80

        // Apply the rotation to the camera transform
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // Rotate player to look left and right
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);

    }

    public void ProcessScroll(Vector2 input)
    {
        //Debug.Log("scroll direction: " + input.y);
        float direction = input.y;

        if (direction != 0)
        {
            if (!isMacUser)
            {
                direction *= -1f;
            }
            inventoryManager.ChangeCurrentInventorySlot(direction);
        }
    }

    private void ImplementCrouch()
    {
        crouchTimer += Time.deltaTime;
        float p = crouchTimer / 1;
        p *= p;

        if (isCrouching)
        {
            controller.height = Mathf.Lerp(controller.height, 1, p);
        }
        else
        {
            controller.height = Mathf.Lerp(controller.height, 2, p);
        }

        if (p > 1)
        {
            // reset
            lerpCrouch = false;
            crouchTimer = 0f;
        }
    }

    /// Toggles if the player is crouching
    public void Crouch()
    {
        if (_HealthStatus == HealthStatus.Bleeding)
        {
            return;
        }

        isCrouching = !isCrouching;
        crouchTimer = 0f;
        lerpCrouch = true;
        
        if (isCrouching)
        {
            playerSpeed *= crouchSpeedReduction;
            jumpHeight -= crouchJumpReduction;

            // Stop sprint if sprinting
            if(isSprinting)
            {
                isSprinting = false;
                playerSpeed = DefaultSpeed();
            }
        }
        else
        {
            playerSpeed = DefaultSpeed();
            jumpHeight += crouchJumpReduction;
        }
    }

    /// Toggles if the player is sprinting
    public void Sprint()
    {
        isSprinting = !isSprinting;

        if (isCrouching)
        {
            isSprinting = false;
            playerSpeed = DefaultSpeed() * crouchSpeedReduction;
            return;
        }
        
        if (isSprinting)
        {
            playerSpeed *= playerSpeedMultiplier;
            playerUI.ShowSprintBar(true);
        }
        else
        {
            playerSpeed = DefaultSpeed();
        }
    }

    private float DefaultSpeed()
    {
        if (_HealthStatus == HealthStatus.Bleeding)
        {
            return playerBleedingSpeed;
        }

        return playerHealthySpeed;
    }

    public void DropItem()
    {
        inventoryManager.RemoveCurrentItemFromInventory();
    }
}
