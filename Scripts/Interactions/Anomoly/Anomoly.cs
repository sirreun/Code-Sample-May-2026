using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Anomoly : Interactable
{
    public Vector3 MaxDimenisions;

    void Update()
    {
        
    }

    public override void Interact(PlayerInteract player)
    {
        InventoryManager inventoryManager = player.GetComponent<InventoryManager>();

        // check if player is holding ACU
        if (!inventoryManager.HoldingItem())
        {
            return;
        }
        AnomolyContainmentUnit ACU = inventoryManager.GetCurrentSelectedItem().GetComponent<AnomolyContainmentUnit>();
        if (ACU)
        {
            if (ACU.PickUpAnomoly(this))
            {
                // TODO: tell mission manager that anomoly has be retrieved
            }
        }
    }

    /// <summary>
    /// Overridden from Interactable class. Anomolies can only be picked up when the player is currently holding
    /// an Anomoly Containment Unit (ACU).
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public override bool ConditionMet(PlayerInteract player)
    {
        InventoryManager inventoryManager = player.GetComponent<InventoryManager>();

        if (inventoryManager.HoldingItem())
        {
            GameObject ACUObject = inventoryManager.GetCurrentSelectedItem();
            if (ACUObject != null)
            {
                AnomolyContainmentUnit ACU = ACUObject.GetComponent<AnomolyContainmentUnit>();
                if (ACU)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
}
