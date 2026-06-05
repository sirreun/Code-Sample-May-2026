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

        if (!inventoryManager.HoldingItem())
        {
            return;
        }
        AnomolyContainmentUnit ACU = inventoryManager.GetCurrentSelectedItem().GetComponent<AnomolyContainmentUnit>();
    }

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
