using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Relay;
using UnityEngine;

public class DebugUICanvas : MonoBehaviour
{
    [SerializeField] private GameObject ConnectionOptionButtons;
    [SerializeField] private GameObject TopLeftUIObject;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TextMeshProUGUI errorMessageTextMesh;
    [SerializeField] private GameObject ResetHealthButton; 
    [SerializeField] private TestRelay testRelay;
    [SerializeField] private string joinCode = "";
    [SerializeField] private TextMeshProUGUI joinCodeTextMesh;

    [Header("Connection Settings")]
    [SerializeField] private TextMeshProUGUI toggleConnectionButtonTextMesh;

    [Header("Debug Log")]
    [SerializeField] private bool showDebugConsoleLog = false;
    [SerializeField] private GameObject debugConsoleLogObject;
    [SerializeField] private GameObject logPrefab;
    [SerializeField] private Transform contentTransform;


    private bool localTesting = true;

    private void Awake()
    {
        //Debug.Log("Starting game in windowed mode");
        TopLeftUIObject.SetActive(false);

        debugConsoleLogObject.SetActive(showDebugConsoleLog);

        SetLocalConnection();
    }

    #region /// Debug Console Log ///
    public void ToggleDebugConsoleLog()
    {
        showDebugConsoleLog = !showDebugConsoleLog;
        debugConsoleLogObject.SetActive(showDebugConsoleLog);
    }

    public void AddLog(string message)
    {
        GameObject newLogObject = Instantiate(logPrefab, Vector3.zero, Quaternion.identity, contentTransform);
        TextMeshProUGUI log = newLogObject.GetComponent<TextMeshProUGUI>();

        log.text = message;
        //set scroll vertical to 1
    }

    #endregion

    public void ToggleConnectionButton()
    {
        localTesting = !localTesting;

        if (localTesting)
        {
            SetLocalConnection();
            
        }
        else
        {
            SetOnlineConnection();
        }
    }

    private void SetLocalConnection()
    {
        toggleConnectionButtonTextMesh.text = "Local";
    }

    private void SetOnlineConnection()
    {
        toggleConnectionButtonTextMesh.text = "Online";
    }

    public void JoinButton()
    {
        if (localTesting)
        {
            NetworkManager.Singleton.StartClient();
        }
        else
        {
            try
            {
                if (joinCode.Length > 0)
                {
                    testRelay.JoinRelay(joinCode);
                }
                else
                {
                    errorMessageTextMesh.text = "/// ERROR: Join code field cannot be empty. ///";
                }
            }
            catch (RelayServiceException e)
            {
                errorMessageTextMesh.text = "/// ERROR: " + e.Message + " ///";
                return;
            }
        }

        ChangeToGameUI();
        SetJoinCodeUI(joinCode);
    }

    public void HostButton()
    {
        if (localTesting)
        {
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            try
            {
                testRelay.CreateRelay();
            }
            catch
            {
                errorMessageTextMesh.text = "/// ERROR: Unable to start host ///";
                return;
            }
        }

        ChangeToGameUI();
    }

    private void ChangeToGameUI()
    {
        ConnectionOptionButtons.SetActive(false);
        TopLeftUIObject.SetActive(true);
    }

    public void SetJoinCodeUI(string code)
    {
        joinCodeTextMesh.text = code;
    }

    public void OnJoinCodeFieldChange()
    {
        joinCode = joinCodeInputField.text;
    }

    //TODO: Currently only changes on client side
    public void OnResetHealthButton()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            player.GetComponent<PlayerManager>().ResetHealth();
        }
    }
}
