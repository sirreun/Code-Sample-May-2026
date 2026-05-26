using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Unity.Netcode.Components;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

/// This item class deals with the funcitonality of the item when a player has it in inventory
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class InventoryItem : NetworkBehaviour
{
    public FixedString64Bytes guid { get; set; }
    public NetworkVariable<FixedString64Bytes> GUID_SERVER = new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public ItemInformationSO ItemInformation;

    public NetworkVariable<bool> IsPickedUp_SERVER = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Vector3 itemScale;
    public Vector3 heldItemScale;
    public Vector3 heldItemRotation;

#if UNITY_EDITOR
    [ContextMenu("Save Held Rotation")]
    public void SaveHeldRotation()
    {
        //Vector3 newRotation = this.gameObject.transform.rotation;
        Debug.Log(this.gameObject.name + " new rotation (flip negative signs): " + this.gameObject.transform.rotation.ToString());
    }

    [ContextMenu("Generate GUID")]
    public void MenuGenerateGUID()
    {
        GUID_SERVER.Value = System.Guid.NewGuid().ToString();
    }
#endif

    public void GenerateGUID()
    {
        guid = new FixedString64Bytes();
        guid = System.Guid.NewGuid().ToString();
    }

    private void Awake()
    {
        if (ItemInformation == null)
        {
            Debug.LogWarning(gameObject.name + " does not have the required ItemInformationSO");
        }

        GenerateGUID();

        if (IsSpawned)
        {
            if (IsHost)
            {
                GUID_SERVER.Value = guid;
                ItemDatabase.instance.OnItemSpawned(this);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            GUID_SERVER.Value = guid;
        }
    }
}
