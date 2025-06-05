using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LSL;
using UnityEngine;
using UnityEngine.UIElements;

public class MarkerConsole : MonoBehaviour
{
    #region [ Instance ]
    private static MarkerConsole _instance;
    public static MarkerConsole Instance => _instance ? _instance : _instance = FindFirstObjectByType<MarkerConsole>();
    #endregion
    
    #region [ Serialised Fields ]
    [SerializeField] private VisualTreeAsset _streamElement;
    [SerializeField] private VisualTreeAsset _consoleLogElement;
    #endregion

    #region [ Unserialised Fields ]
    private UIDocument _uiDocument;
    private readonly List<LogEntry> _displayedLogEntries = new ();
    private readonly List<LogEntry> _fullLogEntries = new ();
    private bool _autoScrolling;
    private IEnumerator _autoScrollCoroutine;
    #endregion

    #region [ UXML Fields ]
    private VisualElement _root;
    private ScrollView _streamScrollView;
    private Button _streamRefreshButton;
    private TextField _contentFilterField;
    private TextField _streamFilterField;
    private ListView _consoleLogListView;
    private Button _clearLogButton;
    private Button _autoScrollButton;
    #endregion
    
    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        
        QueryUXML();
        Initialise();
    }

    private void QueryUXML()
    {
        _root = _uiDocument.rootVisualElement;
        _streamScrollView = _root.Q<ScrollView>("StreamScrollView");
        _streamRefreshButton = _root.Q<Button>("StreamRefreshButton");
        _contentFilterField = _root.Q<TextField>("ContentFilter");
        _streamFilterField = _root.Q<TextField>("StreamFilter");
        _consoleLogListView = _root.Q<ListView>("LogListView");
        _clearLogButton = _root.Q<Button>("ClearLogButton");
        _autoScrollButton = _root.Q<Button>("AutoScrollButton");
    }

    private void Initialise()
    {
        _consoleLogListView.itemsSource = _displayedLogEntries;
        _consoleLogListView.makeItem += MakeItem;
        _consoleLogListView.bindItem += BindItem;
        
        return;

        VisualElement MakeItem()
        {
            VisualElement element = _consoleLogElement.CloneTree();
            return element;
        }
        void BindItem(VisualElement element, int i)
        {
            Label timeLabel = element.Q<Label>("TimeLabel");
            Label markerLabel = element.Q<Label>("MarkerLabel");
            Label streamLabel = element.Q<Label>("StreamLabel");

            LogEntry logEntry = _displayedLogEntries[i];
            
            timeLabel.text = $"{logEntry.time}";
            markerLabel.text = $"{logEntry.content}";
            streamLabel.text = $"{logEntry.stream}";
        }
    }

    private void OnEnable() => RegisterCallbacks();
    private void RegisterCallbacks()
    {
        _contentFilterField.RegisterValueChangedCallback(OnFilterUpdated);
        _streamFilterField.RegisterValueChangedCallback(OnFilterUpdated);
        _streamRefreshButton.clicked += OnRefreshButton;
        _clearLogButton.clicked += OnClearLogButtonPressed;
        _autoScrollButton.clicked += OnAutoScrollButtonPressed;
    }

    private void OnDisable() => UnregisterCallbacks();
    private void UnregisterCallbacks()
    {
        _contentFilterField.UnregisterValueChangedCallback(OnFilterUpdated);
        _streamFilterField.UnregisterValueChangedCallback(OnFilterUpdated);
        _streamRefreshButton.clicked -= OnRefreshButton;
        _clearLogButton.clicked -= OnClearLogButtonPressed;
        _autoScrollButton.clicked -= OnAutoScrollButtonPressed;
    }

    private void OnFilterUpdated(ChangeEvent<string> evt)
    {
        _displayedLogEntries.Clear();
        
        foreach (LogEntry logEntry in _fullLogEntries.Where(logEntry => SampleFilters(logEntry))) _displayedLogEntries.Add(logEntry);
        _consoleLogListView.Rebuild();
    }

    private void OnRefreshButton()
    {
        _streamScrollView.contentContainer.Clear();
        if (!NetworkManager.Instance.ResolveStreams(out StreamInfo[] streamInfoArray)) return;
        
        for (var i = 0; i < streamInfoArray.Length; i++)
        {
            StreamInfo streamInfo = streamInfoArray[i];
            VisualElement streamElement = _streamElement.CloneTree();
            _streamScrollView.contentContainer.Add(streamElement);

            Label streamLabel = streamElement.Q<Label>("StreamLabel");
            Label streamChannelTypesLabel = streamElement.Q<Label>("ChannelCountLabel");
            Label streamChannelCountLabel = streamElement.Q<Label>("ChannelTypeLabel");
            Label streamFrequencyLabel = streamElement.Q<Label>("FrequencyLabel");
            
            streamLabel.text = $"{streamInfo.name()}";
            streamChannelTypesLabel.text = $"{streamInfo.channel_count()}";
            streamChannelCountLabel.text = $"{streamInfo.channel_format()}";
            streamFrequencyLabel.text = $"{streamInfo.nominal_srate():0.000}";
                
            Button streamConnectionButton = streamElement.Q<Button>("ConnectButton");
            VisualElement selectedVisualElement = streamElement.Q<VisualElement>("Selected");
            
            streamConnectionButton.clicked += () =>
            {
                bool connected = NetworkManager.Instance.ToggleStreamConnection(streamInfo);
                streamConnectionButton.text = (connected) ? "Disconnect" : "Connect";
                selectedVisualElement.style.display = (connected) ? DisplayStyle.Flex : DisplayStyle.None;
            };
        }
    }
    
    public void AddLog(in LogEntry logEntry)
    {
        if (_fullLogEntries.Count >= 1000)
        {
            _displayedLogEntries.Remove(_fullLogEntries[0]);
            _fullLogEntries.RemoveAt(0);
        }
        
        _fullLogEntries.Add(logEntry);
        if (!SampleFilters(logEntry)) return;
        
        _displayedLogEntries.Add(logEntry);
        _consoleLogListView.Rebuild();
    }

    private IEnumerator AutoScroll()
    {
        while (_autoScrolling)
        {
            _consoleLogListView.ScrollToItem(_displayedLogEntries.Count - 1);
            yield return null;
        }
    }

    private bool SampleFilters(in LogEntry logEntry)
    {
        string contentFilter = _contentFilterField.text;
        string streamFilter = _streamFilterField.text;
        
        bool containedInContentFilter = String.IsNullOrWhiteSpace(contentFilter) || SampleFilter(logEntry.content, contentFilter);
        bool containedInStreamFilter = String.IsNullOrWhiteSpace(streamFilter) || SampleFilter(logEntry.stream, streamFilter);

        return containedInContentFilter && containedInStreamFilter;
        bool SampleFilter(string content, string filter)
        {
            return content.Contains(filter);
        }
    }

    private void OnClearLogButtonPressed()
    {
        _displayedLogEntries.Clear();
        _fullLogEntries.Clear();
        _consoleLogListView.Rebuild();
    }

    private void OnAutoScrollButtonPressed()
    {
        _autoScrolling = !_autoScrolling;
        
        if (_autoScrolling)
        {
            StartCoroutine(_autoScrollCoroutine = AutoScroll());
            _autoScrollButton.AddToClassList("button-toggle-active");
        }
        else if (_autoScrollCoroutine != null)
        {
            StopCoroutine(_autoScrollCoroutine);
            _autoScrollButton.RemoveFromClassList("button-toggle-active");
        }
    }
}