using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///Intructions: set object layer to interactable
[RequireComponent(typeof(InventoryItem))]
public class InventoryInteractable : Interactable
{
    public IType _Type = IType.Standard;
    [Tooltip("The functions called when pressing Q and holding this item.")]
    public UnityEvent[] ItemInteractFunctions;
    [Tooltip("The functions called when the item is selected in the inventory.")]
    public UnityEvent[] ItemSelectedFunctions;
    [Tooltip("The functions called when the item is unselected in the inventory. Item specific 'turn off' functions go here.")]
    public UnityEvent[] ItemUnselectedFunctions;
    public UnityEvent AttackAnimationFunctions;

    protected InventoryItem inventoryItem;

    private string turnOffUI = "[Q] Turn Off";
    private string turnOnUI = "[Q] Turn On";
    
    [Header("Item Battery")]
    public bool UsesPower = true;
    public bool itemOn { get; private set; }
    protected float powerLevel = 100;
    [Range(100, 600)]
    public float TotalPower = 100;
    protected float rateOfPowerDrain = 20; // Per real time minute
    private System.TimeSpan timeDelta; 
    private System.DateTime startTime; 
    private double parsedTime; // Unit: minutes

    public enum IType
    {
        Standard,
        Weapon
    }

    void Awake()
    {
        if (_Type == IType.Weapon)
        {
            if (GetComponent<Weapon>() == null)
            {
                Debug.LogWarning("Item requires  a weapon component");
            }
        }

        if (this.gameObject.layer != LayerMask.NameToLayer("Interactable"))
        {
            Debug.LogWarning(this.gameObject + ": must be on layer interactable");
        }

        itemOn = false;

        inventoryItem = GetComponent<InventoryItem>();
    }

    protected bool CheckIfOwned()
    {
        if (inventoryItem.ownerPlayerUI == null)
        {
            Debug.LogWarning("Inventory Item is not owned by any player.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Picks up Inventory Item. Called by PlayerInteract.
    /// </summary>
    /// <param name="player"></param>
    public override void Interact(PlayerInteract player)
    {
        // Check server owned status
        if (inventoryItem.IsPickedUp_SERVER.Value)
        {
            Debug.LogWarning("Item already picked up, cannot pick up");
            return;
        }

        InventoryManager inventoryManager = player.GetComponent<InventoryManager>();
        // Add item to inventory through the Inventory Manager
        // Hide the physical object
        if(inventoryManager.AddItemToInventory(gameObject))
        {
            Debug.Log("Added " + gameObject.name + " to inventory.");
        }
    }

    // Item interact function that is used to turn on / off the item when pressing Q
    public void ItemInteract()
    {
        if (UsesPower)
        {
            TogglePower();
        }
        
        foreach (UnityEvent itemInteractFunction in ItemInteractFunctions)
        {
            itemInteractFunction.Invoke();
        }
    }

    public void ItemAttackAnimations()
    {
        AttackAnimationFunctions.Invoke();
    }

    // Toggles the item on and off if it has enough power.
    private void TogglePower()
    {
        if (!CheckIfOwned())
        {
            return;
        }

        if (!UsesPower)
        {
            return;
        }

        if (powerLevel > 0)
        {
            itemOn = !itemOn;

            if (itemOn)
            {
                startTime = System.DateTime.Now;
                inventoryItem.ownerPlayerUI.SetInventoryInteractableText(turnOffUI);
                ElectricityManager.instance.AddPowerSource(this.gameObject.transform);
                UpdatePowerLevelUI();
            }
            else
            {
                timeDelta = System.DateTime.Now.Subtract(startTime);
                inventoryItem.ownerPlayerUI.SetInventoryInteractableText(turnOnUI);
                ElectricityManager.instance.RemovePowerSource(this.gameObject.transform);
            }
        }
        else
        {
            itemOn = false;
        }
    }

    private void TurnOff()
    {
        //Debug.LogWarning("Turning off device");
        if (!UsesPower)
        {
            return;
        }

        if (itemOn)
        {
            TogglePower();
        }
        
        
        parsedTime = timeDelta.TotalMinutes + System.DateTime.Now.Subtract(startTime).TotalMinutes;
            
        if (parsedTime > 1f)
        {
            // A minute has passed, reduce power
            ChangePowerLevel(-1 * rateOfPowerDrain);
            startTime = System.DateTime.Now;
        }

        ElectricityManager.instance.RemovePowerSource(this.gameObject.transform);
        itemOn = false;
    }

    void FixedUpdate()
    {
        if (itemOn && UsesPower)
        {
            parsedTime = timeDelta.TotalMinutes + System.DateTime.Now.Subtract(startTime).TotalMinutes;

            if (parsedTime > 1f)
            {
                // A minute has passed, reduce power
                ChangePowerLevel(-1 * rateOfPowerDrain);
                startTime = System.DateTime.Now;
            }
        }

        InventoryInteractableUpdate();
    }

    protected virtual void InventoryInteractableUpdate()
    {

    }

    private void ChangePowerLevel(float value)
    {
        powerLevel += value;
        UpdatePowerLevelUI();
    }

    public void UpdatePowerLevelUI()
    {
        if (!CheckIfOwned())
        {
            return;
        }

        if (UsesPower)
        {
            inventoryItem.ownerPlayerUI.UpdatePowerLevelUI(powerLevel, TotalPower);
        }
        else 
        {
            inventoryItem.ownerPlayerUI.RemovePowerLevelUI();
        }
    }

    public void InitializeInteractableUI()
    {
        if (!CheckIfOwned())
        {
            return;
        }
        //Debug.LogWarning("initing interactble ui");
        if (UsesPower)
        {
            inventoryItem.ownerPlayerUI.UpdatePowerLevelUI(powerLevel, TotalPower);
            inventoryItem.ownerPlayerUI.SetInventoryInteractableText(turnOnUI);
            itemOn = false;
        }
        else 
        {
            inventoryItem.ownerPlayerUI.RemoveInteractactableUI();
        }
    }

    #region /// FUNCTIONS CALLED BY INVENTORY MANAGER ///

    public void ItemSelected()
    {
        foreach (UnityEvent itemFunction in ItemSelectedFunctions)
        {
            itemFunction.Invoke();
        }
    }

    // Also called before item dropped
    public void ItemUnselected()
    {
        TurnOff();
        foreach (UnityEvent itemFunction in ItemUnselectedFunctions)
        {
            itemFunction.Invoke();
        }
    }
    #endregion
}
