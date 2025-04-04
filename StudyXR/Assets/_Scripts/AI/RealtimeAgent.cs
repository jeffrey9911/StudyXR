using OpenAI;
using OpenAI.Audio;
using OpenAI.Images;
using OpenAI.Models;
using OpenAI.Realtime;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utilities.Audio;
using Utilities.Encoding.Wav;
using Utilities.Extensions;

public enum AvatarType
{
    PureBehaviour,
    Performance,
    PureForm
}

public class AgentType
{
    public AvatarType AvatarType;
    public SessionConfiguration SessionConfiguration;
}

public class RealtimeAgent : MonoBehaviour
{
    public AIManager AIManager;
    public AudioSource AudioSource;

    [SerializeField]
    private OpenAIConfiguration configuration;

    private OpenAIClient openAI;

    private RealtimeSession session;



    private readonly CancellationTokenSource cts = new();
    private CancellationToken destroyToken => cts.Token;

    List<Tool> tools = new List<Tool>();

    private bool isMuted = false;

    private readonly ConcurrentQueue<float> sampleQueue = new();
    

#region Models
    public static Model GPT4oMiniRealtime { get; } = new("gpt-4o-mini-realtime-preview", "openai");

#endregion



#region Agent Type
    public AgentType PureBehaviourAgent = new AgentType
    {
        AvatarType = AvatarType.PureBehaviour,
        SessionConfiguration = new SessionConfiguration
        (
            model: GPT4oMiniRealtime,
            voice: Voice.Alloy,
            instructions: $"",
            tools: new List<Tool>
            {
                Tool.GetOrCreateTool(typeof(RealtimeAgent), nameof(TestFunctionCalling), "This function is used when users want to test function calling from the server")
            }
        )
    };

#endregion

#region Tool Pool
    public static void TestFunctionCalling()
    {
       SystemDebugger.Instance.Log("TestFunctionCalling");
    }
#endregion


    public async void InitiateAgent(AgentType agentType)
    {
        openAI = new OpenAIClient(configuration);

        try
        {
            session = await openAI.RealtimeEndpoint.CreateSessionAsync(agentType.SessionConfiguration, destroyToken);

            RecordInputAudio(destroyToken);

            await session.ReceiveUpdatesAsync<IServerEvent>(ServerResponseEvent, destroyToken);
            
        }
        catch (System.Exception e)
        {
            SystemDebugger.Instance.Log(e.Message);
            throw;
        }
        finally
        {
            session?.Dispose();
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (sampleQueue.Count <= 0) { return; }

        for (var i = 0; i < data.Length; i += channels)
        {
            if (sampleQueue.TryDequeue(out var sample))
            {
                for (var j = 0; j < channels; j++)
                {
                    data[i + j] = sample;
                }
            }
            //else
            //{
            //    for (var j = 0; j < channels; j++)
            //    {
            //        data[i + j] = 0f;
            //    }
            //}
        }
    }

    private async void RecordInputAudio(CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();
        var semaphore = new SemaphoreSlim(1, 1);

        try
        {
            // we don't await this so that we can implement buffer copy and send response to realtime api
            // ReSharper disable once MethodHasAsyncOverload
            RecordingManager.StartRecordingStream<WavEncoder>(BufferCallback, 24000, cancellationToken);

            async Task BufferCallback(ReadOnlyMemory<byte> bufferCallback)
            {
                if (!isMuted)
                {
                    try
                    {
                        await semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                        await memoryStream.WriteAsync(bufferCallback, CancellationToken.None).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            }

            do
            {
                var buffer = ArrayPool<byte>.Shared.Rent(1024 * 16);

                try
                {
                    int bytesRead;

                    try
                    {
                        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                        memoryStream.Position = 0;
                        bytesRead = await memoryStream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, memoryStream.Length), cancellationToken).ConfigureAwait(false);
                        memoryStream.SetLength(0);
                    }
                    finally
                    {
                        semaphore.Release();
                    }

                    if (bytesRead > 0)
                    {
                        await session.SendAsync(new InputAudioBufferAppendRequest(buffer.AsMemory(0, bytesRead)), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Yield();
                    }
                }
                catch (Exception e)
                {
                    switch (e)
                    {
                        case TaskCanceledException:
                        case OperationCanceledException:
                            // ignored
                            break;
                        default:
                            Debug.LogError(e);
                            break;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            } while (!cancellationToken.IsCancellationRequested);
        }
        catch (Exception e)
        {
            switch (e)
            {
                case TaskCanceledException:
                case OperationCanceledException:
                    // ignored
                    break;
                default:
                    Debug.LogError(e);
                    break;
            }
        }
        finally
        {
            await memoryStream.DisposeAsync();
        }
    }



    private void ServerResponseEvent(IServerEvent serverEvent)
    {
        switch (serverEvent)
        {
            case InputAudioBufferStartedResponse startedResponse:
                sampleQueue.Clear();
                break;
            
            case ResponseAudioResponse audioResponse:
                if (audioResponse.IsDelta)
                {
                    foreach (var sample in audioResponse.AudioSamples)
                    {
                        sampleQueue.Enqueue(sample);
                    }
                }
                break;
            case ResponseAudioTranscriptResponse transcriptResponse:
                
                break;
                
            case ResponseFunctionCallArgumentsResponse functionCallResponse:
                if (functionCallResponse.IsDone)
                {
                    ToolCall toolCall = functionCallResponse;
                    toolCall.InvokeFunction();
                }
                break;
                
            case ConversationItemInputAudioTranscriptionResponse transcriptionResponse:
                
                break;
            case ConversationItemCreatedResponse conversationItemCreated:
                

                break;
                /*
            case ResponseFunctionCallArgumentsResponse functionCallResponse:
                if (functionCallResponse.IsDone)
                {
                    ProcessToolCall(functionCallResponse);
                }

                break;
                */
        }
    }

    public void EndSession()
    {
        cts.Cancel();
    }


    private void OnDestroy()
    {
        cts.Cancel();
    }

}
