using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerDatabase : NetworkBehaviour
{
    public static PlayerDatabase instance { get; private set; }

    public NetworkList<ulong> currentPlayers_SERVER;
    private Dictionary<ulong, Transform> players = new Dictionary<ulong, Transform>();

    [SerializeField] private TextMeshProUGUI playerListUI;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("PlayerDatabase: found another instance of PlayerDatabase, destroying new one.");
            Destroy(this);
            return;
        }
        instance = this;

        currentPlayers_SERVER = new NetworkList<ulong>(default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);
    }

    public override void OnDestroy()
    {
        if (!IsOwner) return;
        
        instance = null;
        currentPlayers_SERVER.Dispose();
        base.OnDestroy();
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnConnectionEvent -= ParseConnectionEvents;
        currentPlayers_SERVER.Dispose();
        base.OnNetworkDespawn();
    }

    public override void OnNetworkSpawn()
    {
        if (currentPlayers_SERVER == null)
        {
            currentPlayers_SERVER = new NetworkList<ulong>(default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);
        }
        currentPlayers_SERVER.OnListChanged += NetworkListChanged;

        AddPlayerToNetworkListServerRpc(OwnerClientId);

        if (IsHost)
        {
            NetworkManager.Singleton.OnConnectionEvent += ParseConnectionEvents;
        }
        else
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += GameSceneManager.instance.ReturnToMenu;
        }
    }

    public void ParseConnectionEvents(NetworkManager networkManager, ConnectionEventData connectionEventData)
    {
        switch (connectionEventData.EventType)
        {
            case ConnectionEvent.ClientDisconnected:
                TryRemovePlayerFromServerList_TO_SERVER(connectionEventData.ClientId);
                break;
            case ConnectionEvent.ClientConnected:
                AddPlayerToNetworkList_TO_SERVER(connectionEventData.ClientId);
                break;
        }
    }

    public bool GetPlayerByIDOnNetworkSpawn(ulong id, out Transform playerTransform)
    {
        playerTransform = null;
        var foundPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (var player in foundPlayers)
        {
            Player playerComponent = player.GetComponent<Player>();
            
            if (playerComponent != null)
            {
                if (playerComponent.OwnerClientId == id)
                {
                    playerTransform = player.transform;
                    return true;
                }
            }
        }

        return false;
    }

    public int GetNumberOfPlayers()
    {
        return players.Count;
    }

    public void AddPlayerToNetworkList_TO_SERVER(ulong id)
    {
        AddPlayerToNetworkListServerRpc(id);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerToNetworkListServerRpc(ulong id)
    {
        if (currentPlayers_SERVER == null)
        {
            return;
        }

        if (!currentPlayers_SERVER.Contains(id))
        {
            currentPlayers_SERVER.Add(id);
            //Debug.Log("Added player " + id + " to network list");
        }
        else
        {
            Debug.LogWarning("Already have player " + id + " in current player list");
        }
    }

    /// <summary>
    /// Called when the network list for player ids is changed. When this occurs, each
    /// player database dictionary is updated.
    /// </summary>
    /// <param name="changeEvent"></param>
    public void NetworkListChanged(NetworkListEvent<ulong> changeEvent)
    {
        Player[] playersInScene = FindObjectsOfType<Player>();
        players.Clear();

        // Use new list to make sure all player transforms added in dictionary
        foreach (ulong id in currentPlayers_SERVER)
        {
            if (TryGetPlayerFromDictionary(id, out Transform playerTransform))
            {
                continue;
            }
            else
            {
                foreach (Player playerObject in playersInScene)
                {
                    if (playerObject.ID == id)
                    {
                        TryAddPlayerToDictionary(playerObject);
                    }
                }
            }
        }

        UpdatePlayerListUI();
    }

    private void TryRemovePlayerFromDictionary(ulong player)
    {
        if (players.TryGetValue(player, out Transform playerTransform))
        {
            players.Remove(player);
        }
        else
        {
            Debug.LogError("ItemDatabase: player not in database: guid: " + player);
        }
    }

    private void TryAddPlayerToDictionary(Player player, bool isHost = false)
    {
        if (players.TryGetValue(player.ID, out Transform dupPlayerTransform))
        {
            Debug.LogError("ItemDatabase: player has duplicate guid: " + player.ID + " with " + dupPlayerTransform.gameObject.GetComponent<Player>().ID);
        }
        else
        {
            players.Add(player.ID, player.transform);
            //Debug.Log("Added player with id: " + player.ID);
        }
    }

    public bool TryGetPlayerFromDictionary(ulong id, out Transform playerTransform)
    {
        if (players.TryGetValue(id, out playerTransform))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void UpdatePlayerListUI()
    {
        string uiText = "Players:\n";

        foreach (ulong player in players.Keys)
        {
            uiText += player.ToString() + "\n";
        }

        playerListUI.text = uiText;
    }

    public void TryRemovePlayerFromServerList_TO_SERVER(ulong player)
    {
        if (!IsHost) return;
        //Debug.Log("1. try remove player calling server");
        TryRemovePlayerFromDatabaseServerRpc(player);
    }

    [ServerRpc(RequireOwnership = true)]
    private void TryRemovePlayerFromDatabaseServerRpc(ulong player)
    {
        //Debug.Log("2. is not owned by server, attempting to remove from playerdb");

        if (currentPlayers_SERVER.Contains(player))
        {
            currentPlayers_SERVER.Remove(player);
        }

        TryRemovePlayerFromDictionary(player);
    }
}
