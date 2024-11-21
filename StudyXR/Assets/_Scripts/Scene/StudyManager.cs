using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VVStudy
{
    public string DomainLink = "";
    public string AssetName = "";
    public string StudyQuestionnaire = "";
}

public class StudyManager : MonoBehaviour
{
    [HideInInspector]
    public SystemManager SystemManager;
    public void SetManager(SystemManager systemManager)
    {
        this.SystemManager = systemManager;
    }

    
    public GameObject VVSPrefab;

    [SerializeField]
    public List<VVStudy> StudyList = new List<VVStudy>();

    public Dictionary<VVStudy, StreamManager> StimulusList = new Dictionary<VVStudy, StreamManager>();

    public int CurrentStudyIndex = 0;

    public void InitializeStudies()
    {
        foreach (VVStudy study in StudyList)
        {
            GameObject VVS = Instantiate(VVSPrefab, SystemManager.EnvManager.StimulusAnchor);
            VVS.transform.localPosition = Vector3.zero;
            StreamManager streamManager = VVS.GetComponent<StreamManager>();
            streamManager.SetConfig(study.DomainLink, study.AssetName);
            streamManager.streamDebugger.textDebug = SystemDebugger.Instance.DebuggerText;

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


}
