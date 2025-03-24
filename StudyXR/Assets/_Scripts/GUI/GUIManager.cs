using System;
using System.Linq;
using NUnit.Framework.Internal;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum SettingType
{
    StimulusPosition,
    SitmulusRotation,
    LightingDirection,
    LightingIntensity
}

public class ControllerPositionState
{
    public Vector3 LeftControllerPosition = new Vector3(0, 0, 0);
    public Vector3 RightControllerPosition = new Vector3(0, 0, 0);

    public void UpdateControllerPosition(Vector3 left, Vector3 right)
    {
        RightControllerPosition = right;
        LeftControllerPosition = left;
    }

    public float GetDScale(Vector3 right, Vector3 left)
    {
        return (Vector3.Distance(left, right) - Vector3.Distance(LeftControllerPosition, RightControllerPosition));
    }

    public Vector3 GetLMove(Vector3 left)
    {
        return (left - LeftControllerPosition);
    }

    public Vector3 GetRMove(Vector3 right)
    {
        return right - RightControllerPosition;
    }

    public float GetLMoveHorizontal(Vector3 left, Vector3 forward)
    {
        forward = forward.normalized;
        Vector3 leftMove = left - LeftControllerPosition;

        float projection = Vector3.Dot(leftMove, forward);
        Vector3 projectedVector = projection * forward;

        Vector3 horizontalMove = leftMove - projectedVector;

        float distance = horizontalMove.magnitude;

        Vector3 cross = Vector3.Cross(forward, leftMove);
        float sign = Mathf.Sign(cross.y);

        return distance * sign;
    }

}

public class GUIManager : MonoBehaviour
{
    [HideInInspector]
    public SystemManager SystemManager;
    public void SetManager(SystemManager systemManager)
    {
        this.SystemManager = systemManager;
    }

    // MAIN CANVAS UI
    public GameObject MainCanvas;

    public GameObject ScaleIndicator;
    float ScaleSpeed = 0.001f;
    public GameObject MoveIndicator;
    float MoveSpeed = 1f;

    public ControllerPositionState ControllerPositionState = new ControllerPositionState();
    public Transform LeftHandAnchor;
    public Transform RightHandAnchor;
    public Transform CentreEye;

    public Transform Cursor;

    bool isDisplayGUI = true;

    Vector3 AnchorPosition = new Vector3(0, 0, 0);
    Vector3 AnchorScale = new Vector3(0, 0, 0);
    Quaternion AnchorRotation = new Quaternion(0, 0, 0, 0);
    float FollowSpeed = 5f;

    // CONFIG UI
    public GameObject ConfigPanel;
    float ConfigToggleTimer = 0f;
    public ToggleGroup TogglesGroup;
    SettingType ActiveSettingType;
    public TMP_InputField PidInput;

    // USER UI
    public TMP_Text PidText;
    public TMP_Text PromptText;

    public GameObject TutorialPanel;
    public GameObject TutorialButton;
    public GameObject StartButton;
    public GameObject NextButton;
    public GameObject BackButton;
    public GameObject EndingPanel;


    void Start()
    {
        AnchorPosition = MainCanvas.transform.position;
        AnchorScale = MainCanvas.transform.localScale;
        AnchorRotation = MainCanvas.transform.rotation;

        ConfigPanel.SetActive(false);
    }

    void Update()
    {
        UIUpdateAnchor();
        
        FollowAnchor();
        UpdateCursor();
        CheckConfigToggle();

        UpdateConfigSettings();

        UpdateControllerState();
    }

    // MAIN CANVAS UI

    void UIUpdateAnchor()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            isDisplayGUI = !isDisplayGUI;
            MainCanvas.SetActive(isDisplayGUI);
        }

        if (isDisplayGUI)
        {
            bool isLeftHandTrigger = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
            bool isRightHandTrigger = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

            if (isLeftHandTrigger || isRightHandTrigger)
            {
                MoveIndicator.SetActive(true);
                ScaleIndicator.SetActive(false);

                if (isLeftHandTrigger && isRightHandTrigger)
                {
                    MoveIndicator.SetActive(false);
                    ScaleIndicator.SetActive(true);

                    float dScale = ControllerPositionState.GetDScale(LeftHandAnchor.localPosition, RightHandAnchor.localPosition);

                    AnchorScale += new Vector3(dScale, dScale, 0) * ScaleSpeed;
                }
                else if (isLeftHandTrigger)
                {
                    AnchorPosition += ControllerPositionState.GetLMove(LeftHandAnchor.localPosition) * MoveSpeed;
                }
                else if (isRightHandTrigger)
                {
                    AnchorPosition += ControllerPositionState.GetRMove(RightHandAnchor.localPosition) * MoveSpeed;
                }
            }
            else
            {
                MoveIndicator.SetActive(false);
                ScaleIndicator.SetActive(false);
            }

            

            Quaternion lookatRot = Quaternion.LookRotation(CentreEye.position - AnchorPosition, Vector3.up);
            lookatRot *= Quaternion.Euler(0f, 180f, 0f);
            AnchorRotation = lookatRot;
        }
    }

    void UpdateControllerState()
    {
        if (isDisplayGUI)
        {
            ControllerPositionState.UpdateControllerPosition(LeftHandAnchor.localPosition, RightHandAnchor.localPosition);
        }
    }

    void FollowAnchor()
    {
        MainCanvas.transform.position = Vector3.Lerp(MainCanvas.transform.position, AnchorPosition, Time.deltaTime * FollowSpeed);
        MainCanvas.transform.rotation = Quaternion.Lerp(MainCanvas.transform.rotation, AnchorRotation, Time.deltaTime * FollowSpeed);
        MainCanvas.transform.localScale = Vector3.Lerp(MainCanvas.transform.localScale, AnchorScale, Time.deltaTime * FollowSpeed);
    }

    void UpdateCursor()
    {
        Cursor.rotation = Quaternion.LookRotation(CentreEye.position - Cursor.position, Vector3.up);
    }

    // CONFIG UI

    void CheckConfigToggle()
    {
        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick) && OVRInput.Get(OVRInput.Button.SecondaryThumbstick))
        {
            ConfigToggleTimer += Time.deltaTime;
        }
        else if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick) || OVRInput.GetUp(OVRInput.Button.SecondaryThumbstick))
        {
            ConfigToggleTimer = 0;
        }

        if (ConfigToggleTimer > 2f)
        {
            ToggleConfigMode();
            ConfigToggleTimer = 0;
        }
    }

    [ContextMenu("Toggle Config Mode")]
    public void ToggleConfigMode()
    {
        if (ConfigPanel.activeSelf)
        {
            // Turn off config mode
            ConfigPanel.SetActive(false);
            SystemManager.EnvManager.ToggleOffDebugObject();
        }
        else
        {
            ConfigPanel.SetActive(true);
            SystemManager.EnvManager.ToggleOnDebugObject(LeftHandAnchor.position);
        }
    }

    void UpdateConfigSettings()
    {
        if (ConfigPanel.activeSelf)
        {
            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
            {
                SystemManager.EnvManager.PausePhysics();

                switch (ActiveSettingType)
                {
                    case SettingType.StimulusPosition:
                        SetStimulusPosition();
                        break;
                    case SettingType.SitmulusRotation:
                        SetStimulusRotation();
                        break;
                    case SettingType.LightingDirection:
                        SetLightingDirection();
                        break;
                    case SettingType.LightingIntensity:
                        SetLightingIntensity();
                        break;
                }
            }

            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
            {
                SystemManager.EnvManager.ResumePhysics();
            }
        }
    }

    void SetStimulusPosition()
    {
        SystemManager.EnvManager.AddStimulusPosition(ControllerPositionState.GetLMove(LeftHandAnchor.localPosition), LeftHandAnchor.position.y);
    }

    void SetStimulusRotation()
    {
        SystemManager.EnvManager.AddStimulusRotation(ControllerPositionState.GetLMoveHorizontal(LeftHandAnchor.localPosition, CentreEye.forward));
    }

    void SetLightingDirection()
    {
        SystemManager.EnvManager.SetLightingDirection(LeftHandAnchor.rotation);
    }

    void SetLightingIntensity()
    {
        SystemManager.EnvManager.AddLightingIntensity(ControllerPositionState.GetLMoveHorizontal(LeftHandAnchor.localPosition, CentreEye.forward));
    }

    public void OnToggleChanged()
    {
        Toggle activeToggle = TogglesGroup.ActiveToggles().FirstOrDefault();

        switch (activeToggle.transform.GetSiblingIndex())
        {
            case 0:
                ActiveSettingType = SettingType.StimulusPosition;
                SystemDebugger.Instance.Log("Setting Stimulus Position");
                break;
            case 1:
                ActiveSettingType = SettingType.SitmulusRotation;
                SystemDebugger.Instance.Log("Setting Stimulus Rotation");
                break;
            case 2:
                ActiveSettingType = SettingType.LightingDirection;
                SystemDebugger.Instance.Log("Setting Lighting Direction");
                break;
            case 3:
                ActiveSettingType = SettingType.LightingIntensity;
                SystemDebugger.Instance.Log("Setting Lighting Intensity");
                break;
        }
    }


    [ContextMenu("Set PID")]
    public void SetPid()
    {
        PidText.text = $"Your PID: {PidInput.text}";
        SystemDebugger.Instance.Log($"Set PID: {PidInput.text}");
    }

    // USER UI

    public void SetPromptText(string text)
    {
        PromptText.text = text;

        // Add animation
    }

    // Progress bar


    [ContextMenu("Next Study")]
    public void OnNextClick()
    {
        SystemManager.StudyManager.NextStudy();
    }

    [ContextMenu("Previous Study")]
    public void OnBackClick()
    {
        SystemManager.StudyManager.PreviousStudy();
    }

    [ContextMenu("Start Study")]
    public void OnStartClick()
    {
        //SystemManager.StudyManager.StartStudy();
        SystemManager.StudyManager.StartStudyFrom(0);
        StartStudyLayer();

    }

    [ContextMenu("Toggle Tutorial")]
    public void ToggleTutorial()
    {
        TutorialPanel.SetActive(!TutorialPanel.activeSelf);
        StartButton.SetActive(true);
    }

    public void EndUserLayer()
    {
        EndingPanel.SetActive(true);
        TutorialPanel.SetActive(false);
        TutorialButton.SetActive(false);
        StartButton.SetActive(false);
        NextButton.SetActive(false);
        BackButton.SetActive(false);
    }

    public void StartUserLayer()
    {
        EndingPanel.SetActive(false);
        TutorialPanel.SetActive(true);
        TutorialButton.SetActive(true);
        StartButton.SetActive(false);
        NextButton.SetActive(false);
        BackButton.SetActive(false);
    }

    public void StartStudyLayer()
    {
        EndingPanel.SetActive(false);
        TutorialPanel.SetActive(false);
        TutorialButton.SetActive(false);
        StartButton.SetActive(false);
        NextButton.SetActive(true);
        BackButton.SetActive(true);
    }

}
