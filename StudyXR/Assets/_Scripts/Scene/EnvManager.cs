using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public enum LightingSetType
{
    Position,
    Rotation,
    Intensity
}

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

    public Dictionary<string, Light> EnvLights = new Dictionary<string, Light>();
    private Light CurrentLight;
    private string CurrentLightName;
    public GameObject LightingDebugObject;
    public EffectMesh[] EffectMeshes;

    public Material effectMaterial;
    float shadowIntensity = 0.5f;

    public LightingSetType LightingSetType = LightingSetType.Position;

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

    private void AddSunLight()
    {
        Light light = new GameObject("EnvLight").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1;
        light.color = Color.white;
        light.transform.rotation = Quaternion.Euler(60, 0, 0);
        light.shadows = LightShadows.Soft;

        EnvLights.Add($"Sun_{EnvLights.Count}", light);

        SystemManager.GUIManager.UpdateLightingList(EnvLights.Keys.ToList());
    }

    private void AddPointLight(Vector3 position)
    {
        Light light = new GameObject("EnvLight").AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 10;
        light.intensity = 1;
        light.color = Color.white;
        light.transform.position = position;

        light.shadows = LightShadows.Soft;

        EnvLights.Add($"Light_{EnvLights.Count}", light);

        SystemManager.GUIManager.UpdateLightingList(EnvLights.Keys.ToList());
    }

    private void AddSpotLight(Vector3 position)
    {
        Light light = new GameObject("EnvLight").AddComponent<Light>();
        light.type = LightType.Spot;
        light.innerSpotAngle = 0;
        light.spotAngle = 60;
        light.range = 10;
        light.intensity = 1;
        light.color = Color.white;
        light.transform.position = position;

        light.shadows = LightShadows.Soft;

        EnvLights.Add($"Light_{EnvLights.Count}", light);

        SystemManager.GUIManager.UpdateLightingList(EnvLights.Keys.ToList());
    }


    public void AddLight(Vector3 position)
    {
        if (EnvLights.Count == 0)
        {
            AddSunLight();
            //AddSpotLight(position);
        }
        else if (EnvLights.Count < 10)
        {
            //AddPointLight(position);
            AddSpotLight(position);
        }
        else
        {
            SystemDebugger.Instance.Log("Max number of lights reached (10)");
        }


        
    }

    public void RemoveCurrentLight()
    {
        if (CurrentLight != null)
        {
            Destroy(CurrentLight.gameObject);
            EnvLights.Remove(CurrentLightName);
        }

        SystemManager.GUIManager.UpdateLightingList(EnvLights.Keys.ToList());
    }

    public void SelectLight(string lightName)
    {
        if (EnvLights.ContainsKey(lightName))
        {
            CurrentLight = EnvLights[lightName];
            CurrentLightName = lightName;
        }
    }

    public void SetLightingConfig(Vector3 pos, Quaternion rot, float intensity)
    {
        switch (LightingSetType)
        {
            case LightingSetType.Position:
                CurrentLight.transform.position = pos;
                break;
            case LightingSetType.Rotation:
                CurrentLight.transform.rotation = rot;
                break;
            case LightingSetType.Intensity:
                CurrentLight.intensity += intensity;
                break;
        }
    }

    public void SetEffectMaterial(float intensity)
    {
        shadowIntensity += intensity;
        effectMaterial.SetFloat("_ShadowIntensity", shadowIntensity);
    }

    public void ToggleOffDebugObject()
    {
        StimulusDebugObject.SetActive(false);
        LightingDebugObject.SetActive(false);
    }

    public void ToggleOnDebugObject(Vector3 sunPosition)
    {
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
