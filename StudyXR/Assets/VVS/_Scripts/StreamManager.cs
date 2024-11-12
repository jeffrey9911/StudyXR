using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StreamManager : MonoBehaviour
{
    [HideInInspector]
    public StreamHandler streamHandler;
    [HideInInspector]
    public StreamFrameHandler streamFrameHandler;
    [HideInInspector]
    public StreamContainer streamContainer;
    [HideInInspector]
    public StreamPlayer streamPlayer;
    [HideInInspector]
    public StreamDebugger streamDebugger;

    public bool EnableStreamDebugger = false;
    public bool OverideConfigLink = false;
    public string OverideDomainBaseLink = "";
    public string OverideVVFolderLinkName = "";

    void Start()
    {
        streamHandler = this.AddComponent<StreamHandler>();
        streamFrameHandler = this.AddComponent<StreamFrameHandler>();
        streamContainer = this.AddComponent<StreamContainer>();
        streamPlayer = this.AddComponent<StreamPlayer>();


        streamHandler.SetManager(this);
        streamFrameHandler.SetManager(this);
        streamContainer.SetManager(this);
        streamPlayer.SetManager(this);

        if (EnableStreamDebugger)
        {
            if (TryGetComponent<StreamDebugger>(out streamDebugger))
            {
                streamDebugger.SetManager(this);
            }
            else
            {
                EnableStreamDebugger = false;
                Debug.LogAssertion("StreamDebugger Component not found");
            }
        }

        SendDebugText("Stream Manager Initialized");
    }

    [ContextMenu("Start Load Header")]
    public void StartLoading()
    {
        try
        {
            streamHandler.LoadHeader();
        }
        catch (System.Exception e)
        {
            SendDebugText(e.Message);
            throw;
        }
        
    }

    public void FinishLoadHeader()
    {
        try
        {
            streamContainer.InitializeFrameContainer(streamHandler.vvheader.count);
            streamFrameHandler.StartDownloadFrames();
            
            streamPlayer.InitializePlayer();
        }
        catch (System.Exception e)
        {
            SendDebugText(e.Message);
            throw;
        }
    }

    public void SendDebugText(string text)
    {
        if(EnableStreamDebugger) streamDebugger.DebugText(text);
    }

    public void UpdateDebugTextureFrame(int frame)
    {
        if (EnableStreamDebugger) streamDebugger.Inspector.TextureFrame = frame;
    }

    public void UpdateDebugTargetFrame(int frame)
    {
        if (EnableStreamDebugger) streamDebugger.Inspector.TargetFrame = frame;
    }

    public void UpdateDebugPlayFrame(int frame)
    {
        if (EnableStreamDebugger) streamDebugger.Inspector.PlayFrame = frame;
    }

    public void SetDebugTexturePreview(Texture texture)
    {
        if (EnableStreamDebugger) streamDebugger.Inspector.TextureVideoRender.texture = texture;
    }


    void OnApplicationQuit()
    {
        Destroy(streamHandler);
        Destroy(streamFrameHandler);
        Destroy(streamContainer);
        Destroy(streamPlayer);
        Destroy(streamDebugger);
    }
}
