using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerDatabase : NetworkBehaviour
{
    public static PlayerDatabase instance { get; private set; }

    private NetworkList<ulong> currentPlayers_SERVER;
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

        base.OnDestroy();
        instance = null;
        currentPlayers_SERVER.OnListChanged -= NetworkListChanged;
        currentPlayers_SERVER.Dispose();
    }

    public override void OnNetworkSpawn()
    {
        if (currentPlayers_SERVER == null)
        {
            currentPlayers_SERVER = new NetworkList<ulong>(default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);
        }
        else if (IsHost)
        {
            currentPlayers_SERVER.Add(OwnerClientId);
        }

        currentPlayers_SERVER.OnListChanged += NetworkListChanged;

        if (IsOwner)
        {
            Player ownerPlayer = null;
            Player[] playersInScene = FindObjectsOfType<Player>();

            foreach (Player player in playersInScene) 
            {
                if (player.ID == OwnerClientId)
                {
                    ownerPlayer = player;
                }
            }

            if (ownerPlayer != null)
            {
                TryAddPlayerToDictionary(ownerPlayer);
            }
            else
            {
                Debug.LogError("Player not found for owner id: " + OwnerClientId);
            }
        }
    }

    public void AddPlayerToNetworkList(ulong id)
    {
        if (currentPlayers_SERVER == null)
        {
            return;
        }

        if (!currentPlayers_SERVER.Contains(id))
        {
            currentPlayers_SERVER.Add(id);
            Debug.Log("Added player " + id + " to network list");
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
            Debug.Log("Added player with id: " + player.ID);
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

    [ServerRpc(RequireOwnership = false)]
    public void TryRemovePlayerFromDatabaseServerRpc(ulong player)
    {
        if (!IsOwnedByServer) return;

        if (currentPlayers_SERVER.Contains(player))
        {
            currentPlayers_SERVER.Remove(player);
        }

        TryRemovePlayerFromDictionary(player);
        UpdatePlayerListUI();
    }
}
