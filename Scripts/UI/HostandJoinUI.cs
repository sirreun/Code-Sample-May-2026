using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Services.Relay;

public class HostandJoinUI : MonoBehaviour
{
    [SerializeField] private NetworkRelay networkRelay;
    [SerializeField] private string joinCode = "";

    [Space(5)]
    [Header("UI Scene Objects")]
    [SerializeField] private GameObject BackgroundObject;
    [SerializeField] private GameObject MiddleUIObject;
    [SerializeField] private GameObject TopLeftUIObject;
    [SerializeField] private GameObject BottomLeftUIObject;

    [Space(5)]
    [Header("UI Text and Fields")]
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TextMeshProUGUI joinCodeTextMesh;
    [SerializeField] private TextMeshProUGUI errorMessageTextMesh;

    [Space(5)]
    [Header("Connection Type UI")]
    [SerializeField] private TextMeshProUGUI toggleConnectionButtonTextMesh;
    private bool hostingLocally = true;

    private void Awake()
    {
        ChangeToMenuUI();

        SetLocalConnection();
        GameSceneManager.instance.HostandJoinUI = this;
    }

    #region /// Connection Type Toggle ///
    public void ToggleConnectionButton()
    {
        hostingLocally = !hostingLocally;

        if (hostingLocally)
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
    #endregion

    public async void JoinButton()
    {
        if (hostingLocally)
        {
            if (!networkRelay.StartClient())
            {
                return;
            }
            else
            {
                //Debug.Log("Joined local game successfully");
                GameSceneManager.instance.LoadingScreenStart();
                ChangeToGameUI();
                SetJoinCodeUI(joinCode);
            }
        }
        else
        {
            if (joinCode.Length > 0)
            {
                bool connected = await networkRelay.JoinRelay(joinCode);
                if (!connected)
                {
                    return;
                }
                else
                {
                    GameSceneManager.instance.LoadingScreenStart();
                    ChangeToGameUI();
                    SetJoinCodeUI(joinCode);
                }
            }
        }
    }

    public async void HostButton()
    {
        if (hostingLocally)
        {
            if (!networkRelay.StartHost())
            {
                SendUIErrorMessage("/// ERROR: Unable to start host ///");
                return;
            }
            else
            {
                GameSceneManager.instance.LoadingScreenStart();
                ChangeToGameUI();
            }
        }
        else
        {
            try
            {
                bool connected = await networkRelay.CreateRelay();

                if (connected)
                {
                    GameSceneManager.instance.LoadingScreenStart();
                    ChangeToGameUI();
                }
                else
                {
                    SendUIErrorMessage("/// ERROR: Unable to start host ///");
                    return;
                }
                
            }
            catch
            {
                SendUIErrorMessage("/// ERROR: Unable to start host ///");
                return;
            }
        }
    }

    public void SendUIErrorMessage(string message)
    {
        errorMessageTextMesh.text = message;
    }

    private void ChangeToGameUI()
    {
        BackgroundObject.SetActive(false);
        MiddleUIObject.SetActive(false);
        TopLeftUIObject.SetActive(true);
        BottomLeftUIObject.SetActive(false);
    }

    public void ChangeToMenuUI()
    {
        BackgroundObject.SetActive(true);
        MiddleUIObject.SetActive(true);
        TopLeftUIObject.SetActive(false);
        BottomLeftUIObject.SetActive(true);
        errorMessageTextMesh.text = "";
    }

    public void SetJoinCodeUI(string code)
    {
        joinCodeTextMesh.text = code;
    }

    public void OnJoinCodeFieldChange()
    {
        joinCode = joinCodeInputField.text;
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}
