using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    [HideInInspector]
    public SystemManager SystemManager;
    public void SetManager(SystemManager systemManager)
    {
        this.SystemManager = systemManager;
    }

    public EventRecorder EventRecorder;

    [SerializeField]
    private string ServerIP = "";

    [SerializeField]
    private int ServerPort = 0;

    [SerializeField]
    private float PerformanceSendRate = 0.1f;

    [SerializeField]
    private float EventSendRate = 1f;

    List<PerformanceObject> PerformanceObjects;

    public bool IsRecording = false;

    UdpClient udpClient;
    IPEndPoint serverEndPoint;
    Thread sendThread;

    Queue<string> performanceQueue = new Queue<string>();
    Queue<string> eventQueue = new Queue<string>();

    private bool threadRunning = false;


    void Start()
    {
        udpClient = new UdpClient();

        serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);


        PerformanceObjects = new List<PerformanceObject>(FindObjectsByType<PerformanceObject>(FindObjectsSortMode.None));
    }

    public void TestRecordEvent()
    {
        RecordEvent($"{SystemManager.StudyManager.CurrentStudyIndex}-QuestionAsked");
    }

    public void RecordEvent(string eventName)
    {
        EventRecorder.RecordEvent(eventName);
    }

    public void ClearEvents()
    {
        EventRecorder.ClearEventData();
    }

    public void StartRecording()
    {
        if (IsRecording) return;

        IsRecording = true;

        StartCoroutine(RecordPerformanceObjects());
        StartCoroutine(RecordEventData());

        threadRunning = true;
        sendThread = new Thread(new ThreadStart(SendData));
        sendThread.Start();

        SystemDebugger.Instance.Log("PerformanceManager: StartRecording");
    }

    public void StopRecording()
    {
        IsRecording = false;
        threadRunning = false;
        if (sendThread != null)
        {
            sendThread.Join();
            sendThread = null;
        }

        SystemDebugger.Instance.Log("PerformanceManager: StopRecording");
    }

    IEnumerator RecordPerformanceObjects()
    {
        while (IsRecording)
        {
            string dataPacket = GetPerformanceDataPacket();

            lock (performanceQueue)
            {
                performanceQueue.Enqueue(dataPacket);
            }

            yield return new WaitForSeconds(PerformanceSendRate);
        }
    }

    IEnumerator RecordEventData()
    {
        while (IsRecording)
        {
            string dataPacket = GetEventDataPacket();

            lock (eventQueue)
            {
                eventQueue.Enqueue(dataPacket);
            }

            yield return new WaitForSeconds(EventSendRate);
        }
    }

    string GetPerformanceDataPacket()
    {
        StringBuilder stringBuilder = new StringBuilder();

        foreach (var pobj in PerformanceObjects)
        {
            if (pobj == null) continue;

            string data = pobj.GetObjectData();
            if (data == null) continue;

            stringBuilder.Append($"PERF:{SystemManager.StudyManager.CurrentStudyIndex}:{data};");
            // example:
            // PERF:0:Cube,1.0,2.0,3.0,0.0,90.0,0.0;
        }

        return stringBuilder.ToString();
    }

    string GetEventDataPacket()
    {
        if (EventRecorder.EventDataPair.Count == 0) return null;

        StringBuilder stringBuilder = new StringBuilder();

        foreach (var eventPair in EventRecorder.EventDataPair)
        {
            stringBuilder.Append($"EVNT:{SystemManager.StudyManager.CurrentStudyIndex}:{eventPair.Key},{eventPair.Value};");
            // example:
            // EVNT:0:QuestionAsked,1;
        }

        return stringBuilder.ToString();
    }

    void SendData()
    {
        while (threadRunning)
        {
            if (performanceQueue.Count > 0)
            {
                string dataToSend;

                lock (performanceQueue)
                {
                    dataToSend = performanceQueue.Dequeue();
                }

                if (string.IsNullOrEmpty(dataToSend)) continue;

                //SystemDebugger.Instance.Log($"SendData: {dataToSend}");
                Debug.Log($"SendData: {dataToSend}");

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);

                try
                {
                    udpClient.Send(data, data.Length, serverEndPoint);
                }
                catch (System.Exception e)
                {
                    //SystemDebugger.Instance.Log(e.Message);
                    Debug.Log(e.Message);
                    throw;
                }
            }

            if (eventQueue.Count > 0)
            {
                string dataToSend;
                
                lock (eventQueue)
                {
                    dataToSend = eventQueue.Dequeue();
                }

                if (string.IsNullOrEmpty(dataToSend)) continue;

                //SystemDebugger.Instance.Log($"SendData: {dataToSend}");

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);

                try
                {
                    udpClient.Send(data, data.Length, serverEndPoint);
                }
                catch (System.Exception e)
                {
                    //SystemDebugger.Instance.Log(e.Message);
                    Debug.Log(e.Message);
                    throw;
                }
            }

            Thread.Sleep(5);
        }
    }

    void OnDestroy()
    {
        StopRecording();
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
}
