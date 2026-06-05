using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager instance { get; private set; }

    public event Action MissionStart;
    public event Action MissionEnd;

    [SerializeField] private Transform missionSpawnTransform;

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        DeclareMissionStart();
    }

    public void DeclareMissionStart()
    {
        Debug.Log("MissionManager: mission start");
        TimeManager.instance.InitializeMissionTime();
        MissionStart?.Invoke();

        if (PlayerDatabase.instance.TryGetPlayerFromDictionary(PlayerDatabase.instance.OwnerClientId, out Transform playerTransform))
        {
            Debug.Log("Set player spawn position");
            playerTransform.GetComponent<PlayerManager>().SetPosition(missionSpawnTransform.position);
        }
        
    }

    private void DeclareMissionEnd()
    {
        MissionEnd?.Invoke();
    }
}
