using Meta.XR.MRUtilityKit;
using UnityEngine;

public class EnvManager : MonoBehaviour
{
    [HideInInspector]
    public SystemManager SystemManager;
    public void SetManager(SystemManager systemManager)
    {
        this.SystemManager = systemManager;
    }

    public Transform StimulusAnchor;
    public GameObject StimulusDebugObject;

    public Light OverallLighting;
    public GameObject LightingDebugObject;
    public EffectMesh[] EffectMeshes;

    void Start()
    {
        StimulusDebugObject.SetActive(false);
        LightingDebugObject.SetActive(false);

        EnableEffectMeshes();
    }

    public void PausePhysics()
    {
        StimulusAnchor.GetComponent<Rigidbody>().isKinematic = true;
    }

    public void ResumePhysics()
    {
        StimulusAnchor.GetComponent<Rigidbody>().isKinematic = false;
    }


    public void AddStimulusPosition(Vector3 position, float controllerY)
    {
        StimulusAnchor.position += position;
        StimulusAnchor.position = new Vector3(StimulusAnchor.position.x, controllerY, StimulusAnchor.position.z);
    }

    public void AddStimulusRotation(float angle)
    {
        StimulusAnchor.rotation *= Quaternion.Euler(0, angle * -250, 0);
    }

    public void SetLightingDirection(Quaternion quaternion)
    {
        OverallLighting.transform.rotation = quaternion;
    }

    public void AddLightingIntensity(float intensity)
    {
        OverallLighting.intensity += intensity * 50;
    }

    public void ToggleOffDebugObject()
    {
        StimulusDebugObject.SetActive(false);
        LightingDebugObject.SetActive(false);
    }

    public void ToggleOnDebugObject(Vector3 sunPosition)
    {
        OverallLighting.transform.position = sunPosition;
        StimulusDebugObject.SetActive(true);
        LightingDebugObject.SetActive(true);
    }

    public void EnableEffectMeshes()
    {

        try
        {
            foreach (var effectMesh in EffectMeshes)
            {
                effectMesh.CreateMesh();
            }
        }
        catch (System.Exception e)
        {
            SystemDebugger.Instance.Log("Error in EnvManager.EnableEffectMeshes: " + e.Message);
            throw;
        }
    }

}
