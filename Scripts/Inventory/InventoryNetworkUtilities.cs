using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.Rendering;
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
        SetParentTransformServerRpc(guid, parent);
    }

    /// <summary>
    /// Calls the server rpc that resets parent transform for inventory items.
    /// </summary>
    /// <param name="guid"></param>
    public void ResetParentTransform_TO_SERVER(FixedString64Bytes guid)
    {
        ResetParentTransformServerRpc(guid);
    }

    [ServerRpc(RequireOwnership = false)] // Callable by the client
    private void SetParentTransformServerRpc(FixedString64Bytes guid, ulong parent)
    {
        bool itemInDatabase = ItemDatabase.instance.TryGetItemTransform(guid, out Transform objectTransform);
        bool playerInDatabase = PlayerDatabase.instance.TryGetPlayerFromDictionary(parent, out Transform parentTransform);

        if (itemInDatabase && playerInDatabase)
        {
            if (objectTransform.gameObject.GetComponent<Rigidbody>())
            {
                objectTransform.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            }
            Vector3 playerHandPosition = parentTransform.GetChild(1).position;
            objectTransform.position = playerHandPosition;
            objectTransform.gameObject.GetComponent<NetworkObject>().TrySetParent(parentTransform);

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

    [ServerRpc(RequireOwnership = false)] // Callable by the client
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
                // Return item to correct scale
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
}
