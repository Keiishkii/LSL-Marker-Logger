using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LSL;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    #region [ Instance ]
    private static NetworkManager _instance;
    public static NetworkManager Instance => _instance ? _instance : _instance = FindFirstObjectByType<NetworkManager>();
    #endregion

    #region [ Unserialised Fields ]
    private ContinuousResolver _continuousResolver;
    #endregion

    #region [ Streams ]
    private readonly Dictionary<string, StreamInlet> _inletStreamDictionary = new Dictionary<string, StreamInlet>();
    #endregion


    private void Awake()
    {
        _continuousResolver = new ContinuousResolver();
    }

    public bool ResolveStreams(out StreamInfo[] results, double timeout = 5.0f)
    {
        // wait until an EEG stream shows up
        results = _continuousResolver.results();
        if (results.Length == 0) return false;

        string output = results.Aggregate($"Resolved Streams: {results.Length}", (current, t) => current + $"\n - {t.name()}");
        Debug.Log(output);

        return true;
    }

    public bool ToggleStreamConnection(in StreamInfo streamInfo)
    {
        string streamName = streamInfo.name();
        if (_inletStreamDictionary.TryGetValue(streamName, out StreamInlet inletStream))
        {
            inletStream.Dispose();
            _inletStreamDictionary.Remove(streamName);
            return false;
        }
        else
        {
            inletStream = new (streamInfo, 100);
            _inletStreamDictionary.Add(streamName, inletStream);
            return true;
        }
    }

    private void Update()
    {
        foreach (StreamInlet streamInlet in _inletStreamDictionary.Values)
        {
            if (!RetrieveStreamInfo(streamInlet, out StreamInfo streamInfo)) continue;

            // Pulls an initial sample to update the samples_available variable;
            PullSamples(streamInlet, streamInfo, 1);
            
            int samplesAvailable = Mathf.Min(streamInlet.samples_available(), 100);
            DiscardOldSamples(streamInlet, streamInfo);
            PullSamples(streamInlet, streamInfo, samplesAvailable);
        }
    }

    private bool RetrieveStreamInfo(in StreamInlet streamInlet, out StreamInfo streamInfo)
    {
        try
        {
            streamInfo = streamInlet.info(0.1);
            return true;
        }
        catch (Exception e)
        {
            switch (e)
            {
                case LostException:
                {
                    Debug.Log($"Stream Lost: {e.Message}");
                } break;
                case TimeoutException:
                {
                    Debug.Log($"Timeout: {e.Message}");
                } break;
                default:
                {
                    Debug.Log($"Exception: {e.Message}");
                } break;
            }

            streamInfo = null;
            return false;
        }
    }

    private void DiscardOldSamples(in StreamInlet streamInlet, in StreamInfo streamInfo)
    {
        int channelCount = streamInfo.channel_count();
        string[] sample = new string[channelCount];

        while (streamInlet.samples_available() > 100) streamInlet.pull_sample(sample, 0);
    }
    
    private void PullSamples(in StreamInlet streamInlet, in StreamInfo streamInfo, in int sampleCount)
    {
        int channelCount = streamInfo.channel_count();
        string streamName = streamInfo.name();
     
        string[] sample = new string[channelCount];
        double timestamp = streamInlet.pull_sample(sample, 0);
        
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            if (timestamp <= 0) continue;

            string content = "";
            for (int i = 0; i < channelCount; i++) content += $"{sample[i]}{(i + 1 < channelCount ? ", " : "")}";
            
            DateTime time = DateTime.UnixEpoch.AddSeconds(timestamp);
            MarkerConsole.Instance.AddLog(new LogEntry()
            {
                time = $"{time:HH:mm:ss.fff}",
                content = $"{content}",
                stream = $"{streamName}",
            });
        }
    }
}