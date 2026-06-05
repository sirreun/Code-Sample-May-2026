using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public class PlayerSettings : MonoBehaviour
{
    private PlayerManager playerManager;
    private InputManager inputManager;

    [Header("Mouse X Sens")]
    [SerializeField] private Slider mouseXSlider;
    [SerializeField] private TMP_InputField mouseXInputField;
    [SerializeField] private float minMouseXSensitvity = 0.01f;
    private float maxXsensitivity;

    [Space(5)]
    [Header("Mouse Y Sens")]
    [SerializeField] private Slider mouseYSlider;
    [SerializeField] private TMP_InputField mouseYInputField;
    [SerializeField] private float minMouseYSensitvity = 0.01f;
    private float maxYsensitivity;

    [Space(5)]
    [Header("Screen Size")]
    [SerializeField] private TextMeshProUGUI toggleFullscreenButtonTextMesh;
    [SerializeField] private TMP_Dropdown screenSizeDropdown;
    private bool isFullscreen = true;

    [SerializeField] private int frameRate = 60;

    private enum ScreenSize
    {
        FourEightyPSixteenNine = 0,
        SevenTwentyPSixteenNine = 1,
        TenEightyP = 2,
        FourteenFourtyP = 3,
        DetectedMonitorSize = 4
    }

    private void Awake()
    {
        playerManager = GetComponent<PlayerManager>();
        inputManager = GetComponent<InputManager>();

        InitializeMouseXSensitivityUI();
        InitializeMouseYSensitivityUI();

        toggleFullscreenButtonTextMesh.text = "Fullscreen";
        // Select drop down
        DetermineScreenSize();

        Application.targetFrameRate = frameRate;
        
    }

    public void OnQuitButton()
    {
       Application.Quit();
    }

    public void OnReturnToGameButton()
    {
        inputManager.ClosePauseMenu();
    }

    #region /// Mouse X Sens ///
    private void InitializeMouseXSensitivityUI()
    {
        maxXsensitivity = mouseXSlider.maxValue;
        mouseXInputField.text = playerManager.xSensitivity.ToString();
        mouseXSlider.value = playerManager.xSensitivity;
    }

    public void OnMouseXInputFieldChanged()
    {
        float newMouseXSens = Int32.Parse(mouseXInputField.text); // no need to check because input field does not take non integers

        if (newMouseXSens > maxXsensitivity) 
        {
            newMouseXSens = maxXsensitivity;
        }
        else if (newMouseXSens <= 0)
        {
            newMouseXSens = minMouseXSensitvity;
        }
        playerManager.xSensitivity = newMouseXSens;
    }

    public void OnMouseXInputFieldEndEdit()
    {
        mouseXSlider.value = Int32.Parse(mouseXInputField.text);
    }

    public void OnMouseXSliderChanged()
    {
        mouseXInputField.text = mouseXSlider.value.ToString();
        playerManager.xSensitivity = mouseXSlider.value;
    }
    #endregion

    #region /// Mouse Y Sens ///
    private void InitializeMouseYSensitivityUI()
    {
        maxYsensitivity = mouseYSlider.maxValue;
        mouseYInputField.text = playerManager.ySensitivity.ToString();
        mouseYSlider.value = playerManager.ySensitivity;
    }

    public void OnMouseYInputFieldChanged()
    {
        float newMouseYSens = Int32.Parse(mouseYInputField.text); // no need to check because input field does not take non integers

        if (newMouseYSens > maxYsensitivity)
        {
            newMouseYSens = maxYsensitivity;
        }
        else if (newMouseYSens <= 0)
        {
            newMouseYSens = minMouseYSensitvity;
        }
        playerManager.ySensitivity = newMouseYSens;
    }

    public void OnMouseYInputFieldEndEdit()
    {
        mouseXSlider.value = Int32.Parse(mouseYInputField.text);
    }

    public void OnMouseYSliderChanged()
    {
        mouseYInputField.text = mouseYSlider.value.ToString();
        playerManager.ySensitivity = mouseYSlider.value;
    }
    #endregion

    #region /// Screen Size ///
    private void DetermineScreenSize()
    {
        int width = Display.main.systemWidth;
        int height = Display.main.systemHeight;
        int index = (int)ScreenSize.DetectedMonitorSize;

        switch (width)
        {
            case 848:
                if (height == 480)
                {
                    index = (int)ScreenSize.FourteenFourtyP;
                }
                break;
            case 1280:
                if (height == 720)
                {
                    index = (int)ScreenSize.SevenTwentyPSixteenNine;
                }
                break;
            case 1920:
                if (height == 1080)
                {
                    index = (int)ScreenSize.TenEightyP;
                }
                break;
            case 2560:
                if (height == 1440)
                {
                    index = (int)ScreenSize.FourteenFourtyP;
                }
                break;
        }

        screenSizeDropdown.SetValueWithoutNotify(index);
    }

    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;

        SetNewScreenResolution(Screen.width, Screen.height);
    }

    private void SetNewScreenResolution(int width, int height)
    {
        if (isFullscreen)
        {
            toggleFullscreenButtonTextMesh.text = "Fullscreen";
            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            toggleFullscreenButtonTextMesh.text = "Windowed";
        }
    }

    public void OnScreenSizeDropdownChanged(Int32 index)
    {
        ScreenSize newScreenSize = (ScreenSize)index;

        switch (newScreenSize) 
        {
            case ScreenSize.FourEightyPSixteenNine:
                //Debug.Log("changing to 480p");
                SetNewScreenResolution(848, 480);
                break;
            case ScreenSize.SevenTwentyPSixteenNine:
                SetNewScreenResolution(1280, 720);
                break;
            case ScreenSize.TenEightyP:
                SetNewScreenResolution(1920, 1080);
                break;
            case ScreenSize.FourteenFourtyP:
                SetNewScreenResolution(2560, 1440);
                break;
            case ScreenSize.DetectedMonitorSize:
                SetNewScreenResolution(Display.main.systemWidth, Display.main.systemHeight); // TODO: not sure if this works for mac
                break;
        }
    }

    #endregion

}
