using System;
using System.Collections;
using UnityEngine;

public class DebugWriter : MonoBehaviour
{
    #region [ Constants ]
    private const string MarkerStreamName = "Fake Marker Stream";
    #endregion
    
    #region [ Serialized Properties ]
    [SerializeField] private int _logCount;
    #endregion
    
    #region [ Unserialised Properties ]
    private IEnumerator _writeFakeLogDataCoroutine;
    private bool _sending;
    private uint _fakeMarkerSendID;
    #endregion
    
    private void OnEnable()
    {
        _sending = true;
        StartCoroutine(_writeFakeLogDataCoroutine = WriteFakeLogData());
    }

    private void OnDisable()
    {
        if (_writeFakeLogDataCoroutine != null) StopCoroutine(_writeFakeLogDataCoroutine);
        _sending = false;
    }

    private IEnumerator WriteFakeLogData()
    {
        while (_sending)
        {
            yield return null;
            
            if (!MarkerConsole.Instance.Initialised) continue;
            for (int i = 0; i < _logCount; i++)
            {
                DateTime time = DateTime.Now;
                MarkerConsole.Instance.AddLog(new LogEntry()
                {
                    time = $"{time:HH:mm:ss.fff}",
                    content = $"Fake Marker: {_fakeMarkerSendID}",
                    stream = MarkerStreamName,
                });   
            
                _fakeMarkerSendID++;
            }
        }
    }
}