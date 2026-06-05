using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : NetworkBehaviour
{
    public static GameSceneManager instance { get; private set; }

    public HostandJoinUI HostandJoinUI; // can't be used in awake for hostandjoinscene


    [SerializeField] private string firstSceneName = "HostandJoinScene";
    [SerializeField] private string firstGameSceneName = "TestArea";

    private Dictionary<string, int> nameToIndexSceneDictionary = new Dictionary<string, int>();

    [Space(5)]
    [Tooltip("Must match Build Settings/Scenes in Build")]
    [SerializeField] private string[] indexToNamesSceneDictionary = { "BootScene", "HostandJoinScene", "TestArea" };

    [Space(5)]
    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreenObject;
    [SerializeField] private LoadingScreenImageAnimator loadingScreenImageAnimator;
    private bool fadeOutPlayed = false;

    private void Awake()
    {
        //Debug.Log("GSM Awake");

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Found another GameSceneManager in the scene, destroying new one");
            Destroy(this);
            return;
        }

        InitializeNameToIndexSceneDictionary();
    }

    private void HandleNetworkSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Unload:
                Debug.Log("GSM: Not yet implemented Unload");
                break;
            case SceneEventType.UnloadComplete:
                Debug.Log("GSM: Not yet implemented UnloadComplete");
                break;
            case SceneEventType.Load:
                Debug.Log("GSM: scene event type Load");
                //LoadingScreenEndRpc(RpcTarget.Single(sceneEvent.ClientId, RpcTargetUse.Temp));
                break;
            case SceneEventType.LoadComplete:
                Debug.Log("GSM: Not yet implemented Load Complete");
                break;
            default:
                Debug.LogWarning("GSM: Not yet implemented");
                break;
        }
    }

    private void InitializeNameToIndexSceneDictionary()
    {
        for (int i = 0; i < indexToNamesSceneDictionary.Length; i++)
        {
            nameToIndexSceneDictionary.Add(indexToNamesSceneDictionary[i], i);
        }
    }

    public void ReturnToMenu()
    {
        SceneManager.UnloadSceneAsync(firstGameSceneName);
        if (HostandJoinUI != null)
        {
            HostandJoinUI.ChangeToMenuUI();
        }


    }

    public void ReturnToMenu(ulong id)
    {
        ReturnToMenu();
    }

    void Start()
    {
        //Debug.Log("GSM Start");
        SceneManager.LoadScene(firstSceneName);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsHost)
        {
            //NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleNetworkSceneEvent;
            AddNetworkedScene_TO_SERVER(firstGameSceneName);
        }
    }

    public override void OnNetworkDespawn()
    {
        ReturnToMenu();
        base.OnNetworkDespawn();

        if (IsHost)
        {
            //NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleNetworkSceneEvent;
        }
    }

    /// <summary>
    /// Loads a scene additively over the network (for all players).
    /// </summary>
    /// <param name="sceneName"></param>
    /// <param name="fade">If there should be a fade in loading screen.</param>
    public void AddNetworkedScene_TO_SERVER(string sceneName, bool fade = false)
    {
        int sceneIndex = nameToIndexSceneDictionary[sceneName];
        if (fade)
        {
            //Debug.Log("Start Loading Screen");
            LoadingScreenStartRpc(sceneIndex);
        }
        else
        {
            AddNetworkedSceneServerRpc(sceneIndex);
        }

        
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddNetworkedSceneServerRpc(int sceneIndex)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(indexToNamesSceneDictionary[sceneIndex], LoadSceneMode.Additive);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void LoadingScreenStartRpc(int sceneIndex)
    {
        Debug.Log("Starting Loading Screen RPC");
        LoadingScreenStart();

        if (IsHost)
        {
            AddNetworkedSceneServerRpc(sceneIndex);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void LoadingScreenEndRpc(RpcParams rpcParams)
    {
        if (fadeOutPlayed)
        {
            loadingScreenImageAnimator.FadeIn();
            fadeOutPlayed = false;
        }
    }

    public void LoadingScreenStart()
    {
        loadingScreenImageAnimator.FadeOut();
        fadeOutPlayed = true;
    }

    public void LoadingScreenEnd()
    {
        loadingScreenImageAnimator.FadeIn();
        fadeOutPlayed = false;
    }

}
