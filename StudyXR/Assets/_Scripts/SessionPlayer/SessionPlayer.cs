using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class SessionPlayer : MonoBehaviour
{
    [System.Serializable]
    class PerformanceRecord
    {
        public int timems;
        public int studyIndex;
        public string objectName;
        public Vector3 position;
        public Vector3 rotation;

        public PerformanceRecord(string[] values)
        {
            timems = int.Parse(values[0]);
            studyIndex = int.Parse(values[1]);
            objectName = values[2];
            position = new Vector3(
                float.Parse(values[3]),
                float.Parse(values[4]),
                float.Parse(values[5])
            );
            rotation = new Vector3(
                float.Parse(values[6]),
                float.Parse(values[7]),
                float.Parse(values[8])
            );
        }
    }


    public string PerformanceRecordFilePath = "";
    //public string EventRecordFilePath = "";

    public GameObject _undefinedObjectPrefab;

    Dictionary<string, GameObject> PerformanceObjects = new Dictionary<string, GameObject>();
    List<PerformanceRecord> PerformanceRecords = new List<PerformanceRecord>();

    Slider PlaybackSlider;

    bool isPlaying = false;

    public float currentPlaybackTime = 0f;

    int currentRecordIndex = 0;

    int firstTimems = 0;
    int lastTimems = 0;

    void Start()
    {
        LoadScenePerformanceObjects();
        LoadPerformanceData();
        

        PlaybackSlider = transform.GetComponentInChildren<Slider>();
        if (PlaybackSlider != null)
        {
            PlaybackSlider.onValueChanged.AddListener(OnPlaybackSliderValueChanged);
        }
        else
        {
            Debug.LogError("Playback slider not found in children.");
        }
    }

    void Update()
    {
        if (isPlaying)
        {
            // Update current playback time
            currentPlaybackTime += Time.deltaTime * 1000;

            PlaybackSlider.value = (currentPlaybackTime - firstTimems) / (lastTimems - firstTimems);

            ProcessRecordsUpToCurrentTime();
        }
    }

    void OnPlaybackSliderValueChanged(float value)
    {
        if (isPlaying) return;

        currentPlaybackTime = firstTimems + ((lastTimems - firstTimems) * value);
        currentRecordIndex = (int)((currentPlaybackTime - firstTimems) / (lastTimems - firstTimems) * PerformanceRecords.Count);
        if (currentRecordIndex >= PerformanceRecords.Count)
        {
            currentRecordIndex = PerformanceRecords.Count - 1;
        }

        ProcessRecordForced();
    }

    void LoadPerformanceData()
    {
        if (string.IsNullOrEmpty(PerformanceRecordFilePath))
        {
            Debug.LogError("Performance record file path is not set.");
            return;
        }

        try
        {
            PerformanceRecords.Clear();

            string[] lines = File.ReadAllLines(PerformanceRecordFilePath);

            int startLine = 0;
            if (lines.Length > 0 && lines[0].Contains("time_ms"))
            {
                startLine = 1;
            }

            for (int i = startLine; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                if (values.Length >= 9)
                {
                    PerformanceRecords.Add(new PerformanceRecord(values));
                }
                else
                {
                    Debug.LogError($"Invalid line format: {lines[i]}");
                }
            }

            PerformanceRecords.Sort((a, b) => a.timems.CompareTo(b.timems));

            if (PerformanceRecords.Count > 0)
            {
                firstTimems = PerformanceRecords[0].timems;
                lastTimems = PerformanceRecords[PerformanceRecords.Count - 1].timems;
            }

            Debug.Log($"Loaded {PerformanceRecords.Count} performance records.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading performance data: {e.Message}");
            throw;
        }
    }

    void ProcessRecordsUpToCurrentTime()
    {
        while (currentRecordIndex < PerformanceRecords.Count &&
        PerformanceRecords[currentRecordIndex].timems <= currentPlaybackTime)
        {
            ProcessRecord(PerformanceRecords[currentRecordIndex]);
            currentRecordIndex++;
        }
    }

    void ProcessRecordForced()
    {
        if (currentRecordIndex < PerformanceRecords.Count)
        {
            ProcessRecord(PerformanceRecords[currentRecordIndex]);
            currentRecordIndex++;
        }
    }

    void ProcessRecord(PerformanceRecord record)
    {
        if (!PerformanceObjects.TryGetValue(record.objectName, out GameObject obj))
        {
            obj = Instantiate(_undefinedObjectPrefab, record.position, Quaternion.Euler(record.rotation));
            obj.name = record.objectName;
            PerformanceObjects.Add(record.objectName, obj);
        }
        else
        {
            obj.transform.position = record.position;
            obj.transform.rotation = Quaternion.Euler(record.rotation);
        }
    }

    public void StartPerformance()
    {
        if (PerformanceRecords.Count == 0)
        {
            LoadPerformanceData();
            if (PerformanceRecords.Count == 0)
            {
                Debug.LogError("No performance data available!");
                return;
            }
        }

        currentPlaybackTime = firstTimems;
        currentRecordIndex = 0;

        isPlaying = true;
        Debug.Log("Performance started.");
    }

    public void StopPerformance()
    {
        isPlaying = false;
        Debug.Log("Performance stopped.");
    }



    void LoadScenePerformanceObjects()
    {
        PerformanceObject[] objects = FindObjectsByType<PerformanceObject>(FindObjectsSortMode.None);

        foreach (var pobj in objects)
        {
            if (pobj == null) continue;

            pobj.transform.SetParent(transform);
            pobj.gameObject.SetActive(true);

            CleanComponents(pobj.gameObject);
            CleanComponents(pobj.gameObject);

            if (!PerformanceObjects.ContainsKey(pobj.ObjectName))
            {
                PerformanceObjects.Add(pobj.ObjectName, pobj.gameObject);
            }
        }

        // SetActive to false for all other gameobjects in the scene not under this transform
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj == null || obj.transform.IsChildOf(transform) || obj.transform == transform) continue;

            obj.SetActive(false);
        }
    }

/*
    void CleanComponents(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(true);

        // Remove all components that are not Transform or PerformanceObject or MeshRenderer or MeshFilter or UI Components (Keep all visual components)
        Component[] components = obj.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component is Collider ||
                component is Rigidbody)
            {
                Destroy(component);
            }

            if (component is EventSystem es)
            {
                es.enabled = false;
            }
        }

        // also to all children
        foreach (Transform child in obj.transform)
        {
            CleanComponents(child.gameObject);
        }
    }
*/

    void CleanComponents(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(true);

        // Remove all components that are not Transform or visual components
        Component[] components = obj.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component is Transform || 
                component is PerformanceObject || 
                component is MeshRenderer || 
                component is MeshFilter ||
                component is CanvasRenderer || 
                component is Image || 
                component is Text || 
                component is Camera ||
                component is Canvas || 
                component is RectTransform ||
                component is CanvasScaler ||  // Add this
                component is GraphicRaycaster)  // Add this
            {
                continue;
            }

            Destroy(component);
        }

        // Process children recursively
        foreach (Transform child in obj.transform)
        {
            CleanComponents(child.gameObject);
        }
    }
}
