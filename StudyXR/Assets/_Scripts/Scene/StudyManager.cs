using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class VVStudy
{
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
    public List<VVStudy> StudyList = new List<VVStudy>();

    public Dictionary<VVStudy, StreamManager> StimulusList = new Dictionary<VVStudy, StreamManager>();

    public int CurrentStudyIndex = -1;

    [ContextMenu("Initialize Studies")]
    public void InitializeStudies()
    {
        foreach (VVStudy study in StudyList)
        {
            GameObject VVS = Instantiate(VVSPrefab, SystemManager.EnvManager.StimulusAnchor);
            VVS.transform.localPosition = Vector3.zero;
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

        VVStudy currentStudy = StudyList[CurrentStudyIndex];

        SystemManager.GUIManager.SetPromptText(currentStudy.PromptText);
        SystemManager.WebManager.LoadWebPage(currentStudy.StudyQuestionnaire);

        StimulusList[currentStudy].ManualPlay();
    }

    public void CloseAllStudies()
    {
        foreach (var study in StimulusList)
        {
            study.Value.ManualStop();
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
