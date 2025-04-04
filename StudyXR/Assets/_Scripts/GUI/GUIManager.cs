using System;
using System.Collections;
using System.Collections.Generic;
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
    Lighting,
    ShadowIntensity
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
    public ToggleGroup ConfigTogglesGroup;
    SettingType ActiveSettingType;
    public TMP_InputField PidInput;
    public GameObject LightingPanel;
    public ToggleGroup LightingTogglesGroup;
    public GameObject LightingListScrollContent;
    public GameObject LightingListPrefab;

    // USER UI
    public TMP_Text PidText;
    public TMP_Text PromptText;

    public GameObject TutorialPanel;
    public GameObject TutorialButton;
    public GameObject StartButton;
    public GameObject NextButton;
    public GameObject BackButton;
    public GameObject EndingPanel;
    public Image SessionPanel;
    Color SessionPanelColor;
    private Coroutine SessionPanelHighlightCoroutine;
    public Image PromptPanel;
    private Coroutine PromptPanelHighlightCoroutine;
    Color PromptPanelColor;



    void Start()
    {
        AnchorPosition = MainCanvas.transform.position;
        AnchorScale = MainCanvas.transform.localScale;
        AnchorRotation = MainCanvas.transform.rotation;

        ConfigPanel.SetActive(false);
        LightingPanel.SetActive(false);

        SessionPanelColor = SessionPanel.color;
        PromptPanelColor = PromptPanel.color;
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
            AddHapticFeedback(OVRInput.Controller.LTouch, .1f, .7f, .5f);
            isDisplayGUI = !isDisplayGUI;
            MainCanvas.SetActive(isDisplayGUI);
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger) || OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger))
        {
            AddHapticFeedback(OVRInput.Controller.LTouch, .1f, .7f, .3f);
        }

        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger) || OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
        {
            AddHapticFeedback(OVRInput.Controller.RTouch, .1f, .7f, .3f);
        }

        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) || OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
        {
            AddHapticFeedback(OVRInput.Controller.RTouch, .1f, .9f, .3f);
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

    public void AddHapticFeedback(OVRInput.Controller controller, float duration = .1f, float frequency = .5f, float amplitude = .5f)
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        StartCoroutine(StopHapticEvent(controller, duration));
    }

    private IEnumerator StopHapticEvent(OVRInput.Controller controller, float delay)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0, 0, controller);
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
        //Cursor.rotation = Quaternion.LookRotation(CentreEye.position - Cursor.position, Vector3.up);
        Cursor.LookAt(CentreEye);
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
                    case SettingType.Lighting:
                        SetLighting();
                        break;
                    case SettingType.ShadowIntensity:
                        SetShadow();
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

    void SetLighting()
    {
        SystemManager.EnvManager.SetLightingConfig(LeftHandAnchor.position, 
            LeftHandAnchor.rotation,
            ControllerPositionState.GetLMoveHorizontal(LeftHandAnchor.localPosition, CentreEye.forward));
    }

    void SetShadow()
    {
        SystemManager.EnvManager.SetEffectMaterial(
            ControllerPositionState.GetLMoveHorizontal(LeftHandAnchor.localPosition, CentreEye.forward)
        );
    }
    

    public void OnConfigToggleChanged()
    {
        Toggle activeConfigToggle = ConfigTogglesGroup.ActiveToggles().FirstOrDefault();

        switch (activeConfigToggle.transform.GetSiblingIndex())
        {
            case 0:
                ActiveSettingType = SettingType.StimulusPosition;
                LightingPanel.SetActive(false);
                SystemDebugger.Instance.Log("Setting Stimulus Position");
                break;
            case 1:
                ActiveSettingType = SettingType.SitmulusRotation;
                LightingPanel.SetActive(false);
                SystemDebugger.Instance.Log("Setting Stimulus Rotation");
                break;
            case 2:
                ActiveSettingType = SettingType.Lighting;
                LightingPanel.SetActive(true);
                SystemDebugger.Instance.Log("Managing Lighting");
                break;
            case 3:
                ActiveSettingType = SettingType.ShadowIntensity;
                SystemDebugger.Instance.Log("Setting Shadow Intensity");
                break;
            default:
                SystemDebugger.Instance.Log("Default Setting");
                LightingPanel.SetActive(false);
                break;
        }
    }

    public void OnLightingToggleChanged()
    {
        Toggle activeLightingToggle = LightingTogglesGroup.ActiveToggles().FirstOrDefault();

        switch (activeLightingToggle.transform.GetSiblingIndex())
        {
            case 0:
                SystemManager.EnvManager.LightingSetType = LightingSetType.Position;
                SystemDebugger.Instance.Log("Setting Lighting Position");
                break;
            
            case 1:
                SystemManager.EnvManager.LightingSetType = LightingSetType.Rotation;
                SystemDebugger.Instance.Log("Setting Lighting Rotation");
                break;

            case 2:
                SystemManager.EnvManager.LightingSetType = LightingSetType.Intensity;
                SystemDebugger.Instance.Log("Setting Lighting Intensity");
                break;
        }
    }

    public void UpdateLightingList(List<string> options)
    {
        ClearContent();
        
        foreach (string option in options)
        {
            GameObject lightingItem = Instantiate(LightingListPrefab);
            DontDestroyOnLoad(lightingItem);
            lightingItem.transform.SetParent(LightingListScrollContent.transform, worldPositionStays: false);
            lightingItem.GetComponent<TMP_Text>().text = option;
            Toggle toggle = lightingItem.GetComponent<Toggle>();
            toggle.group = LightingListScrollContent.GetComponent<ToggleGroup>();
            toggle.onValueChanged.AddListener(delegate { OnLightingListToggleChanged(); });
        }

        // auto select the last one
        //LightingListScrollContent.GetComponent<ToggleGroup>().ActiveToggles().Last().isOn = true;
    }

    private void ClearContent()
    {
        foreach (Transform child in LightingListScrollContent.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void OnLightingListToggleChanged()
    {
        SystemManager.EnvManager.SelectLight(LightingListScrollContent.GetComponent<ToggleGroup>().ActiveToggles().FirstOrDefault().GetComponent<TMP_Text>().text);
    }

    public void AddHighlightEventToSessionID(float duration = 1f)
    {
        if (SessionPanelHighlightCoroutine != null)
        {
            StopCoroutine(SessionPanelHighlightCoroutine);
        }

        SessionPanelHighlightCoroutine = StartCoroutine(HighlightEvent(SessionPanel, duration));
    }

    [ContextMenu("Add Highlight")]
    public void AddHighlightEventToPrompt(float duration = 1f)
    {
        if (PromptPanelHighlightCoroutine != null)
        {
            StopCoroutine(PromptPanelHighlightCoroutine);
        }

        PromptPanelHighlightCoroutine = StartCoroutine(HighlightEvent(PromptPanel, duration));
    }

    private IEnumerator HighlightEvent(Image image, float duration)
    {
        Color originalColor = image.color;

        if (image == SessionPanel)
        {
            originalColor = SessionPanelColor;
        }
        else if (image == PromptPanel)
        {
            originalColor = PromptPanelColor;
        }

        image.color = new Color(1, 0, 0, 1);

        for (float t = 0; t < duration; t += Time.deltaTime)
        {

            image.color = Color.Lerp(image.color, originalColor, t / duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }

        image.color = originalColor;
    }

    [ContextMenu("Add Light")]
    public void OnAddLightClick()
    {
        SystemManager.EnvManager.AddLight(LeftHandAnchor.position);
    }

    [ContextMenu("Remove Light")]
    public void OnRemoveLightClick()
    {
        SystemManager.EnvManager.RemoveCurrentLight();
    }


    [ContextMenu("Set PID")]
    public void SetPid()
    {
        PidText.text = $"Session ID: {PidInput.text}";
        SystemDebugger.Instance.Log($"Set Session ID: {PidInput.text}");
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

        AddHighlightEventToSessionID(10f);
        AddHighlightEventToPrompt(10f);
    }

    [ContextMenu("Previous Study")]
    public void OnBackClick()
    {
        SystemManager.StudyManager.PreviousStudy();

        AddHighlightEventToSessionID(10f);
        AddHighlightEventToPrompt(10f);
    }

    [ContextMenu("Start Study")]
    public void OnStartClick()
    {
        //SystemManager.StudyManager.StartStudy();
        SystemManager.StudyManager.StartStudyFrom(0);
        StartStudyLayer();

        AddHighlightEventToSessionID(10f);
        AddHighlightEventToPrompt(10f);
    }

    [ContextMenu("Toggle Tutorial")]
    public void ToggleTutorial()
    {
        TutorialPanel.SetActive(!TutorialPanel.activeSelf);
        if(!TutorialPanel.activeSelf)
        {
            AddHighlightEventToSessionID(10f);
            AddHighlightEventToPrompt(10f);
        }
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
