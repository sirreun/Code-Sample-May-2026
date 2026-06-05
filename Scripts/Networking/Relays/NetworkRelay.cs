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
using System.Threading.Tasks;

public class NetworkRelay : MonoBehaviour
{
    [SerializeField] private int MaxNumberOfPlayers = 2;
    [SerializeField] private HostandJoinUI uiCanvas;
    public string JoinCode = "";


    #region /// Initializing Class and Relay ///
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async Task<bool> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxNumberOfPlayers - 1);

            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            ShowJoinCode();

            RelayServerData serverData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);
            //Debug.Log("Starting Host...");
            StartHost();
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("RELAY HOST ERROR: " + e);
            GetErrorInformation(e);
            return false;
        }
    }

    private void ShowJoinCode()
    {
        //Debug.Log("Join Code: " + JoinCode);
        uiCanvas.SetJoinCodeUI(JoinCode); // TODO: Change
    }

    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData serverData = new RelayServerData(joinAllocation, "dtls");

            if (NetworkManager.Singleton == null)
            {
                uiCanvas.SendUIErrorMessage("NetworkManager not found.");
                return false;
            }
            else
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);
            }
           

            StartClient();
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("RELAY JOIN ERROR:" + e);
            GetErrorInformation(e);
            return false;
        }
    }

    private void GetErrorInformation(RelayServiceException e)
    {
        string message = "";
        switch (e.Reason)
        {
            case RelayExceptionReason.AllocationNotFound:
                message = "Server timed out";
                break;
            case RelayExceptionReason.BadGateway:
                message = "Bad gateway";
                break;
            case RelayExceptionReason.Conflict:
                message = "Http conflict";
                break;
            case RelayExceptionReason.EntityNotFound:
                message = "Join code not found.";
                break;
            case RelayExceptionReason.FailedDependency:
                message = "Failed dependency";
                break;
            case RelayExceptionReason.Forbidden:
                message = "Forbidden";
                break;
            case RelayExceptionReason.GatewayTimeout:
                message = "Gateway timeout";
                break;
            case RelayExceptionReason.JoinCodeNotFound:
                message = "Join code not found.";
                break;
            case RelayExceptionReason.NoSuitableRelay:
                message = "Server error, try again later.";
                break;
            case RelayExceptionReason.ServiceUnavailable:
                message = "Server is currently down, try again later.";
                break;
            case RelayExceptionReason.InvalidRequest:
                message = "Join code not found.";
                break;
            default:
                message = "Error: " + e.Reason.ToString();
                break;
        }

        uiCanvas.SendUIErrorMessage(message);
    }

    public bool StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            uiCanvas.SendUIErrorMessage("NetworkManager not found.");
            return false;
        }

        try
        {
            NetworkManager.Singleton.StartClient();

            NetworkManager.Singleton.OnTransportFailure += ClientDisconnected;
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("RELAY JOIN ERROR:" + e);
            GetErrorInformation(e);
            return false;
        }
    }

    public bool StartHost()
    {
        if (NetworkManager.Singleton.StartHost() == false)
        {
            uiCanvas.SendUIErrorMessage("Unable to Start Host, check your internet is connected."); // TODO: more error messages
            return false;
        }
        else
        {
            NetworkManager.Singleton.OnTransportFailure += HostDisconnected;
            return true;
        }
    }
    #endregion

    public void ClientDisconnected()
    {
        GameSceneManager.instance.ReturnToMenu();
    }

    public void HostDisconnected()
    {
        GameSceneManager.instance.ReturnToMenu();
    }
}
