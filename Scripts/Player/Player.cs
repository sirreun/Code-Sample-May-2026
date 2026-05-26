using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    public ulong ID;
    public string Username;

    public override void OnNetworkSpawn()
    {
        ID = OwnerClientId;

        if (IsHost && OwnerClientId != 0)
        {
            PlayerDatabase.instance.AddPlayerToNetworkList(ID);
        }
        
        if (!IsOwner)
        {
            gameObject.tag = "OtherPlayer";
        }
    }

    public override void OnNetworkDespawn()
    {
        //PlayerDatabase.instance.RemovePlayerFromNetworkList(ID);
    }
}
