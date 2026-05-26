using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactablePromptText;
    [SerializeField] private RectTransform healthBar;
    [SerializeField] private TextMeshProUGUI timeUIText;
    [SerializeField] private DebugGraph debugGraph;
    public Camera _Camera;

    [Space(5)]
    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pauseMenuUI;


    [Header("Inventory Interactable UI")]
    [SerializeField] private TextMeshProUGUI InventoryInteractableText;
    [Header("Power Level UI")]
    [SerializeField] private TextMeshProUGUI PowerLevelText;

    private float previousPercentSprint = 1f;
    private float lerpPercent = 0f;
    private bool isLerping = false;
    [Header("Sprint UI")]
    [SerializeField] private RectTransform sprintBar;
    [SerializeField] private GameObject sprintUI;
    [SerializeField] private float lerpDuration = 5f;

    private PlayerInteract playerInteract;

    private void Start()
    {
        playerInteract = GetComponent<PlayerInteract>();
        
        TimeManager.instance.Tick += UpdateTimeUI;
        UpdateTimeUI();

        ShowSprintBar(false);
        RemoveInteractactableUI();
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        TimeManager.instance.Tick -= UpdateTimeUI;
    }

    private void OnApplicationQuit()
    {
        TimeManager.instance.Tick -= UpdateTimeUI;
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

    public void UpdateText(Interactable interactable)
    {
        if (interactable.ConditionalInteractable)
        {
            if (interactable.ConditionMet(playerInteract))
            {
                interactablePromptText.text = interactable.interactionPrompt;
            }
            else
            {
                interactablePromptText.text = "";
            }
        }
        else 
        {
            interactablePromptText.text = interactable.interactionPrompt;
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


    #region /// Health UI ///
    public void UpdateHealthBar(float percentHealth)
    {
        // 0 is max health - 900 is zero health
        healthBar.offsetMin = new Vector2(900 * (1f - percentHealth), 0); // for corruption 900 will need to be variable
    }
    #endregion

    #region /// Sprint UI ///
    public void UpdateSprintBar(float percentSprint)
    {
        if (!isLerping)
        {
            StartCoroutine(FloatLerp(previousPercentSprint, percentSprint, lerpDuration));
        }

        // 0 is max sprint - 100 is zero sprint
        sprintBar.offsetMin = new Vector2(100 * (1f - lerpPercent), 0);
    }

    private IEnumerator FloatLerp(float start, float end, float duration)
    {
        float timeElapsed = 0;
        isLerping = true;

        while (timeElapsed < duration)
        {
            lerpPercent = Mathf.Lerp(start, end, timeElapsed / duration);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        lerpPercent = end;
        isLerping = false;
        previousPercentSprint = end;
    }

    public void ShowSprintBar(bool show)
    {
        sprintUI.SetActive(show);
    }
    #endregion

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
