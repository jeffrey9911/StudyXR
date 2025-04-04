using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventRecorder : MonoBehaviour
{
    public Dictionary<string, int> EventDataPair = new Dictionary<string, int>();

    public void RecordEvent(string eventName)
    {
        if (EventDataPair.ContainsKey(eventName))
        {
            EventDataPair[eventName] = EventDataPair[eventName] + 1;
        }
        else
        {
            EventDataPair.Add(eventName, 1);
        }

        SystemDebugger.Instance.Log($"Event: {eventName} - Count: {EventDataPair[eventName]}");
    }

    public void ClearEventData()
    {
        EventDataPair.Clear();
    }
}
