using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class InventoryNetworkUtilities : NetworkBehaviour
{
    public static InventoryNetworkUtilities instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Calls the server rpc that sets the parent transform for inventory items.
    /// </summary>
    /// <param name="objectTransform"></param>
    /// <param name="parentTransform"></param>
    public void SetParentTransform_TO_SERVER(FixedString64Bytes guid, ulong parent)
    {
        //Debug.Log("1. Calling server rpc to set parent transform: guid: " + guid.ToString() + " parent: " + parent);
        try
        {
            SetParentTransformServerRpc(guid, parent);
        }
        catch
        {
            Debug.LogError("2. Wasn't able to call function to set parent");
        }

        
    }

    /// <summary>
    /// Calls the server rpc that resets parent transform for inventory items. Also sets position, turns off physics,
    /// sets server pick up variable for inventory items to true, and sets local scale and rotation.
    /// </summary>
    /// <param name="guid"></param>
    public void ResetParentTransform_TO_SERVER(FixedString64Bytes guid)
    {
        //Debug.Log("Calling server rpc to set parent transform");
        ResetParentTransformServerRpc(guid);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetParentTransformServerRpc(FixedString64Bytes guid, ulong parent)
    {
        bool itemInDatabase = ItemDatabase.instance.TryGetItemTransform(guid, out Transform objectTransform);
        bool playerInDatabase = PlayerDatabase.instance.TryGetPlayerFromDictionary(parent, out Transform parentTransform);
        //Debug.Log("2. in set parent server rpc");
        if (itemInDatabase && playerInDatabase)
        {
            if (objectTransform.gameObject.GetComponent<Rigidbody>())
            {
                objectTransform.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            }

            Vector3 playerHandPosition = parentTransform.GetChild(1).position;
            objectTransform.position = playerHandPosition;

            if (objectTransform.gameObject.GetComponent<NetworkObject>().TrySetParent(parentTransform, true))
            {
                //Debug.Log(">> 3. server set item parent <<");
            }
            else
            {
                Debug.LogError("3. server unable to set item parent");
            }

            InventoryItem inventoryItem = objectTransform.GetComponent<InventoryItem>();

            if (inventoryItem != null)
            {
                inventoryItem.IsPickedUp_SERVER.Value = true;
                // Make item smaller
                objectTransform.localScale = inventoryItem.heldItemScale;

                // Rotate the object 
                //Debug.Log(" target rotation: " + inventoryItem.heldItemRotation.ToString() + ", player rotation : " + this.gameObject.transform.rotation.eulerAngles);
                Vector3 rotation = parentTransform.rotation.eulerAngles - inventoryItem.heldItemRotation;
                //Debug.Log(" subtracted: " + rotation);
                objectTransform.Rotate(rotation);
            }
        }
        else if (!playerInDatabase) 
        {
            Debug.LogError("Player not found in database. Parent id: " + parent);
        }
        else if (!itemInDatabase)
        {
            Debug.LogError("Item not found in database. GUID: " + guid);
        }
        else
        {
            Debug.LogError("Item and parent not found in databases. GUID: " +  guid + "Parent id: " + parent);
        }
    }

    [ServerRpc(RequireOwnership = false)] 
    private void ResetParentTransformServerRpc(FixedString64Bytes guid)
    {
        bool itemInDatabase = ItemDatabase.instance.TryGetItemTransform(guid, out Transform objectTransform);

        if (itemInDatabase)
        {
            objectTransform.SetParent(null);

            InventoryItem inventoryItem = objectTransform.GetComponent<InventoryItem>();

            if (inventoryItem != null)
            {
                inventoryItem.IsPickedUp_SERVER.Value = false;
                objectTransform.localScale = inventoryItem.itemScale;
            }

            if (objectTransform.gameObject.GetComponent<Rigidbody>())
            {
                objectTransform.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            }
        }
        else
        {
            Debug.LogError("Item not found in database. GUID: " + guid);
        }
    }

    public void ChangeAnomalyTransform_TO_SERVER(float multiplier, Vector3 newPosition, Quaternion newRotation, FixedString64Bytes ACUguid)
    {
        ChangeAnomalyTransformServerRpc(multiplier, newPosition, newRotation, ACUguid);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeAnomalyTransformServerRpc(float multiplier, Vector3 newPosition, Quaternion newRotation, FixedString64Bytes ACUguid)
    {
        bool ACUInDatabase = ItemDatabase.instance.TryGetItemTransform(ACUguid, out Transform ACUTransform);

        if (ACUInDatabase)
        {
            if (ItemDatabase.instance.Anomoly)
            {

                ItemDatabase.instance.Anomoly.localScale *= multiplier;
                ItemDatabase.instance.Anomoly.position = newPosition;
                ItemDatabase.instance.Anomoly.rotation = newRotation;

                ItemDatabase.instance.Anomoly.SetParent(ACUTransform);

                SetACUHoldingAnomolyRpc(ACUguid);
            }
            else
            {
                Debug.LogWarning("No anomoly found in item database");
            }
        }
        else
        {
            Debug.LogWarning("ACU not found in item database");
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetACUHoldingAnomolyRpc(FixedString64Bytes ACUguid)
    {
        bool ACUInDatabase = ItemDatabase.instance.TryGetItemTransform(ACUguid, out Transform ACUTransform);

        if (ACUInDatabase)
        {
            AnomolyContainmentUnit ACU = ACUTransform.GetComponent<AnomolyContainmentUnit>();
            if (ACU)
            {
                Debug.Log("Anomoly contained");
                ACU.SetContainsAnomoly(true);
            }
            else
            {
                Debug.LogWarning("guid passed through not for acu");
            }
        }
    }
}
