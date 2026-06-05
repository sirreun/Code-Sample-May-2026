using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MissionDropPoint : NetworkBehaviour
{
    [SerializeField] private string anomolyCollectedMessage = "Anomoly [Collected]";
    [SerializeField] private string anomolyNotCollectedMessage = "Anomoly [Missing]";
    [SerializeField] private string allTeammatesPresentMessage = "Teammates [Present]";
    [SerializeField] private string teammatesMissingMessage = "Teammates [Missing]";

    [Space(5)]
    [Header("Drop Point Visuals")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material dropPointConditionFulfilledMaterial;
    [SerializeField] private Material dropPointConditionNotFulfilledMaterial;

    private bool dropPointConditionFulfilled = false;

    public NetworkVariable<bool> anomolyInDropPoint = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playersInDropPoint = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    public string UImessage = "";

    public override void OnNetworkSpawn()
    {
        playersInDropPoint.OnValueChanged += DetermineIfDropPointConditionFulfilled;
        anomolyInDropPoint.OnValueChanged += DetermineIfDropPointConditionFulfilled;
        PlayerDatabase.instance.currentPlayers_SERVER.OnListChanged += DetermineIfDropPointConditionFulfilled;
    }

    public override void OnNetworkDespawn()
    {
        playersInDropPoint.OnValueChanged -= DetermineIfDropPointConditionFulfilled;
        anomolyInDropPoint.OnValueChanged -= DetermineIfDropPointConditionFulfilled;
        PlayerDatabase.instance.currentPlayers_SERVER.OnListChanged -= DetermineIfDropPointConditionFulfilled;
        base.OnNetworkDespawn();
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            PlayerUI playerUI = collider.GetComponent<PlayerUI>();
            //Debug.Log("Player " + playerUI.GetComponent<Player>().OwnerClientId + "enter");
            playerUI.ShowMissionQuestUI(true);
            playerUI.UpdateMissionQuestUI(UImessage, dropPointConditionFulfilled);

            AddPlayerToDropPointServerRpc(collider.GetComponent<InventoryManager>().HasAnomoly());
        }
        else if (collider.tag == "Anomoly")
        {
            //Debug.Log("Anomoly in zone");
            if (!collider.transform.parent.GetComponent<InventoryItem>().IsPickedUp_SERVER.Value)
            {
                //Debug.Log("Enter: Player not holding anomoly");
                ChangeAnomolyInDropPointValueServerRpc(true);
            }
        }
    }

    private string UIMessageFromConditions(bool hasAnomoly, bool allTeammatesPresent)
    {
        //Debug.Log("Updaing UI Message: " + hasAnomoly + " and teammates: " + allTeammatesPresent);
        string message = string.Empty;

        if (hasAnomoly)
        {
            message += anomolyCollectedMessage + "\n";
        }
        else
        {
            message += anomolyNotCollectedMessage + "\n";
        }

        if (allTeammatesPresent)
        {
            message += allTeammatesPresentMessage;
        }
        else
        {
            message += teammatesMissingMessage;
        }

        return message;
    }
    /// <summary>
    /// OnValueChanged delegate for NetworkVariables
    /// </summary>
    /// <param name="previousValue">Not used in function.</param>
    /// <param name="newValue">Not used in funtion.</param>

    private void DetermineIfDropPointConditionFulfilled(int previousValue, int newValue)
    {
        dropPointConditionFulfilled = false;

        if (playersInDropPoint.Value == PlayerDatabase.instance.GetNumberOfPlayers())
        {
            if (anomolyInDropPoint.Value)
            {
                dropPointConditionFulfilled = true;
            }
        }

        UImessage = UIMessageFromConditions(anomolyInDropPoint.Value, playersInDropPoint.Value == PlayerDatabase.instance.GetNumberOfPlayers());

        if (PlayerDatabase.instance.TryGetPlayerFromDictionary(OwnerClientId, out Transform playerTransform))
        {
            PlayerUI playerUI = playerTransform.GetComponent<PlayerUI>();
            playerUI.UpdateMissionQuestUI(UImessage, dropPointConditionFulfilled);
        }

        ChangeDropPointColor();
    }

    private void DetermineIfDropPointConditionFulfilled(bool previousValue, bool newValue)
    {
        DetermineIfDropPointConditionFulfilled(0, 0);
    }

    private void DetermineIfDropPointConditionFulfilled<T>(NetworkListEvent<T> changeEvent)
    {
        DetermineIfDropPointConditionFulfilled(0, 0);
    }

    private void ChangeDropPointColor()
    {
        if (dropPointConditionFulfilled)
        {
            meshRenderer.material = dropPointConditionFulfilledMaterial;
        }
        else 
        {
            meshRenderer.material = dropPointConditionNotFulfilledMaterial;
        }
    }


    private void OnTriggerExit(Collider collider)
    {
        if (collider.tag == "Player")
        {
            PlayerUI playerUI = collider.GetComponent<PlayerUI>();

            playerUI.ShowMissionQuestUI(false);

            RemovePlayerFromDropPointServerRpc(collider.GetComponent<InventoryManager>().HasAnomoly());
        }
        else if (collider.tag == "Anomoly")
        {
            //Debug.Log("Anomoly leaving zone");
            if (!collider.transform.parent.GetComponent<InventoryItem>().IsPickedUp_SERVER.Value)
            {
                //Debug.Log("Exit: Player not holding anomoly");
                ChangeAnomolyInDropPointValueServerRpc(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerToDropPointServerRpc(bool hasAnomoly)
    {
        playersInDropPoint.Value += 1;
        if (playersInDropPoint.Value > PlayerDatabase.instance.GetNumberOfPlayers())
        {
            Debug.LogWarning("Something has gone wrong, value for number of player is over the number of players in game");
            playersInDropPoint.Value = PlayerDatabase.instance.GetNumberOfPlayers();
        }

        if (hasAnomoly)
        {
            anomolyInDropPoint.Value = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemovePlayerFromDropPointServerRpc(bool hasAnomoly)
    {
        playersInDropPoint.Value -= 1;
        if (playersInDropPoint.Value < 0)
        {
            Debug.LogWarning("Something has gone wrong, value for number of player is negative");
            playersInDropPoint.Value = 0;
        }

        if (hasAnomoly)
        {
            anomolyInDropPoint.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeAnomolyInDropPointValueServerRpc(bool value)
    {
        anomolyInDropPoint.Value = value;
    }
}
