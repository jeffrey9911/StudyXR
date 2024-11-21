using UnityEngine;
using UnityEngine.EventSystems;

public class TestVerify : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventSystem eventSystem = EventSystem.current;
        BaseInputModule inputModule = eventSystem.currentInputModule;
        
        // Check specifically for OVRInputModule
        OVRInputModule ovrModule = inputModule as OVRInputModule;
        if (ovrModule != null)
        {
            SystemDebugger.Instance.Log("OVRInputModule found and active");
        }
        else
        {
            SystemDebugger.Instance.Log("OVRInputModule not active");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
