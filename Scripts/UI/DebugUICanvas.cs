using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUICanvas : MonoBehaviour
{
    [SerializeField] private GameObject ConnectionOptionButtons;
    [SerializeField] private GameObject TopLeftUIObject;
    [SerializeField] private GameObject ResetHealthButton; 

    [Header("Connection Settings")]
    [SerializeField] private TextMeshProUGUI toggleConnectionButtonTextMesh;

    [Header("Debug Log")]
    [SerializeField] private bool showDebugConsoleLog = false;
    [SerializeField] private GameObject debugConsoleLogObject;
    [SerializeField] private GameObject logPrefab;
    [SerializeField] private Transform contentTransform;


    private void Awake()
    {
        debugConsoleLogObject.SetActive(showDebugConsoleLog);
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
    }

    #endregion

    
}
