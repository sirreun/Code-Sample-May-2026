using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactablePromptText;
    [SerializeField] private RectTransform healthBar;
    [SerializeField] private TextMeshProUGUI timeUIText;
    [SerializeField] private GameObject missionQuestUIObject;
    [SerializeField] private GameObject missionQuestTextObject;
    [SerializeField] private TextMeshProUGUI missionQuestUITextMesh;
    [SerializeField] private DebugGraph debugGraph;
    public Camera _Camera;

    [Space(5)]
    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pauseMenuUI;


    [Header("Inventory Interactable UI")]
    [SerializeField] private TextMeshProUGUI InventoryInteractableText;
    [Header("Power Level UI")]
    [SerializeField] private TextMeshProUGUI PowerLevelText;

    [Header("Sprint UI")]
    [SerializeField] private RectTransform sprintBar;
    [SerializeField] private GameObject sprintUI;

    private PlayerInteract playerInteract;

    private void Start()
    {
        playerInteract = GetComponent<PlayerInteract>();
        
        TimeManager.instance.Tick += UpdateTimeUI;
        UpdateTimeUI();
        ShowMissionQuestUI(false);
        UpdateMissionQuestUI("", false);

        ShowSprintBar(false);
        RemoveInteractactableUI(); // Was in Awake
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (TimeManager.instance != null)
        {
            TimeManager.instance.Tick -= UpdateTimeUI; //TODO: causing null refs when timemanager calls tick sometimes
        }
    }

    #region /// Interactable UI Prompt ///

    public void SetInventoryInteractableText(string message)
    {
        InventoryInteractableText.text = message;
    }

    public void RemoveInteractactableUI()
    {
        RemovePowerLevelUI();
        InventoryInteractableText.text = "";
    }

    public bool UpdateText(Interactable interactable)
    {
        if (interactable.ConditionalInteractable)
        {
            if (interactable.ConditionMet(playerInteract))
            {
                interactablePromptText.text = interactable.interactionPrompt;
                return true;
            }
            else
            {
                interactablePromptText.text = "";
                return false;
            }
        }
        else 
        {
            interactablePromptText.text = interactable.interactionPrompt;
            return true;
        }
    }

    /// <summary>
    /// Changes text based on if can interact or not.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="interactable"></param>
    /// <returns>Whether the item can be interacted with.</returns>
    public bool UpdateText(InventoryItem item, Interactable interactable)
    {
        if (item.IsPickedUp_SERVER.Value) 
        {
            interactablePromptText.text = "";
            return false;
        }
        else
        {
            return UpdateText(interactable);
        }
    }

    public void ClearText()
    {
        interactablePromptText.text = "";
    }
    #endregion

    #region /// Power Level UI ///
    public void UpdatePowerLevelUI(float powerLevel, float totalPower)
    {
        PowerLevelText.text = "Power: " + powerLevel + " / " + totalPower;
    }

    public void RemovePowerLevelUI()
    {
        PowerLevelText.text = "";
    }
    #endregion

    #region /// Radiation Graph ///

    public void ShowRadiationGraph()
    {
        debugGraph.gameObject.SetActive(true);
    }

    public void HideRadiationGraph()
    {
        debugGraph.gameObject.SetActive(false);
    }

    public DebugGraph GetRadiationGraph()
    {
        return debugGraph;
    }
    #endregion

    public void ShowMissionQuestUI(bool value)
    {
        //Debug.Log("SHow Mission quest ui: " + value);
        missionQuestTextObject.SetActive(value);
    }

    public void UpdateMissionQuestUI(string message, bool show)
    {
        Debug.Log("Updating player missuion UI: " + message);
        missionQuestUITextMesh.SetText(message);// .text = message;
        missionQuestUITextMesh.ForceMeshUpdate(false, true);
        missionQuestUIObject.SetActive(show);
    }

    public void UpdateHealthBar(float percentHealth)
    {
        // 0 is max health - 900 is zero health
        healthBar.offsetMin = new Vector2(900 * (1f - percentHealth), 0);
    }

    public void UpdateSprintBar(float percentSprint)
    {
        // 0 is max sprint - 100 is zero sprint
        sprintBar.offsetMin = new Vector2(100 * (1f - percentSprint), 0);
    }

    public void ShowSprintBar(bool show)
    {
        sprintUI.SetActive(show);
    }

    public void UpdateTimeUI()
    {
        TFTime currentTime = TimeManager.instance.CurrentTime;

        // Round to the nearest div by 5
        currentTime.Minutes = (int)Math.Floor(currentTime.Minutes / 5f) * 5;

        timeUIText.text = currentTime.ToString();
    }

    public void ShowPauseMenu(bool show)
    {
        pauseMenuUI.SetActive(show);
        Cursor.visible = show;
    }
}
