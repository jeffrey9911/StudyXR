using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class XRStudySession
{
    public bool OverideVVPlayback = false;
    public string DomainLink = "";
    public string AssetName = "";
    public string StudyQuestionnaire = "";
    public string PromptText = "";
}

public class StudyManager : MonoBehaviour
{
    [HideInInspector]
    public SystemManager SystemManager;
    public void SetManager(SystemManager systemManager)
    {
        this.SystemManager = systemManager;
    }

    public string PreStudyLink = "";
    public string PreStudyPrompt = "";

    public GameObject VVSPrefab;

    [SerializeField]
    public List<XRStudySession> StudyList = new List<XRStudySession>();

    public Dictionary<XRStudySession, StreamManager> StimulusList = new Dictionary<XRStudySession, StreamManager>();

    public int CurrentStudyIndex = -1;

    [ContextMenu("Initialize Studies")]
    public void InitializeStudies()
    {
        foreach (XRStudySession study in StudyList)
        {
            GameObject VVS = Instantiate(VVSPrefab, SystemManager.EnvManager.StimulusAnchor);
            VVS.transform.localPosition = Vector3.zero;
            VVS.transform.localRotation = Quaternion.identity;
            StreamManager streamManager = VVS.GetComponent<StreamManager>();
            streamManager.SetConfig(study.DomainLink, study.AssetName);

            if (streamManager.streamDebugger == null)
            {
                Debug.LogAssertion("StreamManager Component not found");
            }

            if (SystemDebugger.Instance.DebuggerText == null)
            {
                Debug.LogAssertion("SystemDebugger Component not found");
            }

            streamManager.streamDebugger.textDebug = SystemDebugger.Instance.DebuggerText;
            streamManager.streamDebugger.Inspector.TextureVideoRender = SystemDebugger.Instance.DebuggerImage;

            StimulusList.Add(study, streamManager);
        }

        PreloadAllStudies();
    }

    void PreloadAllStudies()
    {
        foreach (var study in StimulusList)
        {
            if (study.Key.OverideVVPlayback) continue;
            study.Value.PreLoadMeshes();
        }
    }

    public void StartCurrentStudy()
    {
        if (CurrentStudyIndex < 0 || CurrentStudyIndex >= StudyList.Count)
        {
            return;
        }

        CloseAllStudies();

        XRStudySession currentStudy = StudyList[CurrentStudyIndex];

        
        SystemManager.GUIManager.SetPromptText(currentStudy.PromptText);

        // GUI => Set progress bar

        SystemManager.WebManager.LoadWebPage(currentStudy.StudyQuestionnaire);

        if (!currentStudy.OverideVVPlayback) StimulusList[currentStudy].ManualPlay();

        SystemManager.SessionManager.ClearEvents();
    }

    public void CloseAllStudies()
    {
        foreach (var study in StimulusList)
        {
            if (!study.Key.OverideVVPlayback) study.Value.ManualStop();
        }
    }

    public void StartStudyFrom(int index)
    {
        if (index < 0 || index >= StudyList.Count)
        {
            return;
        }

        CurrentStudyIndex = index;
        StartCurrentStudy();
    }

    public void NextStudy()
    {
        CurrentStudyIndex++;
        if (CurrentStudyIndex >= StudyList.Count)
        {
            CloseAllStudies();
            SystemManager.GUIManager.EndUserLayer();
            CurrentStudyIndex = 0;
            return;
        }

        StartCurrentStudy();
    }

    public void DebugPlayNext()
    {
        CurrentStudyIndex++;
        if (CurrentStudyIndex >= StudyList.Count)
        {
            CurrentStudyIndex = 0;
        }

        CloseAllStudies();

        XRStudySession currentStudy = StudyList[CurrentStudyIndex];

        StimulusList[currentStudy].ManualPlay();
    }

    public void PreviousStudy()
    {
        CurrentStudyIndex--;
        if (CurrentStudyIndex < 0)
        {
            // No change
            CurrentStudyIndex = 0;
            return;
        }

        StartCurrentStudy();
    }

    [ContextMenu("Start With PreStudy")]
    public void StartWithPreStudy()
    {
        CloseAllStudies();

        CurrentStudyIndex = -1;

        SystemManager.GUIManager.StartUserLayer();
        SystemManager.GUIManager.SetPromptText(PreStudyPrompt);

        SystemManager.WebManager.LoadWebPage(PreStudyLink);
    }


}
