using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class TestRelay : MonoBehaviour
{
    [SerializeField] private int NumberOfPlayers = 2;
    [SerializeField] private DebugUICanvas debugUICanvas;
    public string JoinCode = "";
    // Same as for the lobby 
    private async void Start()
    {
        await UnityServices.InitializeAsync(); // Called async so it doesnt freeze the game waiting for internet

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync(); // new account for this user
    }

    [ContextMenu("Create Relay")]
    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(NumberOfPlayers - 1);

            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            //Debug.Log("Join Code: " + JoinCode);
            //HostandJoinUI.SetJoinCodeUI(JoinCode);

            RelayServerData serverData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);
            //Debug.Log("Starting Host...");
            NetworkManager.Singleton.StartHost();
            MissionManager.instance.DeclareMissionStart();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData serverData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

            NetworkManager.Singleton.StartClient();
            MissionManager.instance.DeclareMissionStart();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
