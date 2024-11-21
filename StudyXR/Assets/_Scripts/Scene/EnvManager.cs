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

    void Start()
    {
        StimulusDebugObject.SetActive(false);
        LightingDebugObject.SetActive(false);
    }


    public void AddStimulusPosition(Vector3 position)
    {
        StimulusAnchor.position += position;
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
}
