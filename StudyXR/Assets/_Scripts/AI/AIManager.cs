using System.Collections.Generic;
using OpenAI;
using OpenAI.Models;
using OpenAI.Realtime;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    [HideInInspector]
    public SystemManager SystemManager;
    public void SetManager(SystemManager systemManager)
    {
        this.SystemManager = systemManager;
    }

    public RealtimeAgent RealtimeAgent;


    [ContextMenu("Start Session")]
    public void StartSession()
    {
        RealtimeAgent.InitiateAgent(RealtimeAgent.PureBehaviourAgent);
    }

    public void EndSession()
    {
        RealtimeAgent.EndSession();
    }



}
