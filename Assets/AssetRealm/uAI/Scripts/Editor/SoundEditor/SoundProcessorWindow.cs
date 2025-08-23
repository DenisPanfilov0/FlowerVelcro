using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Text;
#if UNITY_2020_2_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.WWW;
#endif

namespace UAI
{
    public class SoundProcessorWindow : EditorWindow
    {
        #region Constants
        private const string WINDOW_TITLE = "Sound Processor";
        private const string WINDOW_MENU_PATH = "Tools/uAI Creator/Sound Processor";
        private const string PREFS_KEY_PREFIX = "UAI_SoundProcessor_";
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.5f;
        #endregion

        #region UI Elements
        private VisualElement _rootElement;
        private VisualElement _processingPanel;
        private VisualElement _batchProcessingPanel;
        private VisualElement _rawEditPanel; 
        private VisualElement _loadingOverlay;
        private VisualElement _playheadIndicator;
        private VisualElement _waveformDisplay;
        
        // Single clip processing elements
        private ObjectField _sourceClipField;
        private Button _sourcePlayButton;
        private Button _sourceStopButton;
        private Toggle _normalizeToggle;
        private Toggle _trimSilenceToggle;
        private Toggle _applyFadesToggle;
        private Toggle _makeLoopableToggle;
        private Toggle _loopToggle;
        private Slider _pitchSlider;
        private Slider _filterSlider;
        private Button _processButton;
        private Button _playButton;
        private Button _pauseButton;
        private Button _stopButton;
        private Button _exportButton;
        private Button _fadeInButton;
        private Button _fadeOutButton; 
        private Button _silenceButton;
        private Slider _timelineSlider;
        private Label _durationLabel;
        private Label _resultLabel;
        private Label _clipNameLabel;
        private Label _clipLengthLabel;
        private Label _clipChannelsLabel;
        private Label _clipFrequencyLabel;
        
        // Batch processing elements
        private ListView _sourceClipsListView;
        private List<AudioClip> _selectedClips = new List<AudioClip>();
        private Toggle _batchNormalizeToggle;
        private Toggle _batchTrimSilenceToggle;
        private Toggle _batchApplyFadesToggle;
        private Toggle _batchMakeLoopableToggle;
        private Slider _batchPitchSlider;
        private Slider _batchFilterSlider;
        private TextField _outputFolderField;
        private Button _batchProcessButton;
         

        // Raw edit elements
        private ObjectField _rawEditClipField;
        private VisualElement _rawWaveformDisplay;
        private RawWaveformCanvas _rawWaveformCanvas; 

        private VisualElement _rawWaveformContainer;
        private Slider _zoomSlider;
        private Slider _rawTimelineSlider;
        private Label _zoomLabel;
        private Label _selectionLabel;
        private Label _cursorLabel;
        private Button _rawPlayButton;
        private Button _rawPauseButton;
        private Button _rawStopButton;
        private Button _cutButton;
        private Button _copyButton;
        private Button _pasteButton;
        private Button _deleteButton;
        private Button _undoButton;
        private Button _redoButton;
        private Button _selectAllButton;
        private Button _rawExportButton;
        private Button _saveSelectionButton;
        private Toggle _rawLoopToggle;
        #endregion

        #region State Variables
        private PanelTypeSP _currentPanel = PanelTypeSP.Processing;
        private string _lastSavePath = "";
        private string _lastBatchFolder = "Assets/Sounds/Processed";
        private AudioClip _sourceClip;
        private AudioClip _processedClip;
        private AudioClip _tempPlaybackClip;
        private bool _isProcessing = false;
        private float _progressValue = 0f;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private float _playbackTime = 0f;
        private float _playbackStartTime = 0f;
        private float _clipLength = 0f;
        private AudioClip _currentlyPlayingPreviewClip;
        private AudioClip _rawPlaybackInstanceClip; 
        private string _rawPlaybackInstanceTempPath; 
        private bool _isRawClipDirtyForPlayback = false; 

        private float _rawPlaybackSegmentStartTimeInClip;
        private float _rawPlaybackSegmentEndTimeInClip;

        private Slider _loopableCrossfadeSlider; 
        private VisualElement _loopableCrossfadeContainer;
        private Slider _batchLoopableCrossfadeSlider;
        private TextField _batchLoopableCrossfadeValueLabel;
        private VisualElement _batchLoopableCrossfadeContainer;

        // Raw edit state
        private AudioClip _rawEditClip;
        private float[] _rawAudioData;
        private float[] _waveformData;
        private float _zoomFactor = 1f;
        private float _scrollPosition = 0f;
        private bool _isSelecting = false;
        private float _selectionStart = 0f;
        private float _selectionEnd = 0f;
        private float _cursorPosition = 0f;
        private bool _hasSelection = false;
        private List<AudioEditAction> _undoStack = new List<AudioEditAction>();
        private List<AudioEditAction> _redoStack = new List<AudioEditAction>();
        private float[] _clipboard;
        private bool _rawIsPlaying = false;
        private bool _rawIsPaused = false;
        private float _rawPlaybackTime = 0f;
        private float _rawPlaybackStartTime = 0f; 

        // Timeline and scrolling
        // private VisualElement _timelineContainer;
        private ScrollView _waveformScrollView;
        private VisualElement _timelineLabels;
        private float _visibleTimeStart = 0f;
        private float _visibleTimeEnd = 1f;
        private float _amplitudeScale = 1f;
        #endregion

        #region Window Lifecycle
        [MenuItem(WINDOW_MENU_PATH, false, 54)]
        public static void ShowWindow()
        {
            SoundProcessorWindow window = GetWindow<SoundProcessorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(950, 600);
            window.Show();
        }

        private void OnEnable()
        {
            _lastSavePath = EditorPrefs.GetString($"{PREFS_KEY_PREFIX}LastSavePath", _lastSavePath);
            _lastBatchFolder = EditorPrefs.GetString($"{PREFS_KEY_PREFIX}LastBatchFolder", _lastBatchFolder);
            
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorPrefs.SetString($"{PREFS_KEY_PREFIX}LastSavePath", _lastSavePath);
            EditorPrefs.SetString($"{PREFS_KEY_PREFIX}LastBatchFolder", _lastBatchFolder);
            
            EditorApplication.update -= OnEditorUpdate;
            StopAudioPlayback();
            StopRawPlayback();
        }
        
        private void OnDestroy()
        {
            StopAudioPlayback();
            StopRawPlayback();
        }

        public void CreateGUI()
        {
            _rootElement = rootVisualElement;
             
            var styleSheet = Resources.Load<StyleSheet>("SoundProcessorStyles");
            if (styleSheet != null)
            {
                _rootElement.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("Failed to load SoundProcessorStyles.uss");
            }
                
            var visualTree = Resources.Load<VisualTreeAsset>("SoundProcessorWindow");
            if (visualTree != null)
            {
                visualTree.CloneTree(_rootElement); 
            }
            else
            {
                Debug.LogError("Failed to load SoundProcessorWindow.uxml");
                var label = new Label("Error loading UI. Please check console for details.");
                _rootElement.Add(label);
                return;
            }
            
            InitializeUIReferences();
            SetupEventHandlers();
            SwitchPanel(_currentPanel); 
            InitializeListViews();
        }
        #endregion

        #region UI Initialization
        private void InitializeUIReferences()
        {
            // Get panels
            _processingPanel = _rootElement.Q<VisualElement>("processing-panel");
            _batchProcessingPanel = _rootElement.Q<VisualElement>("batch-processing-panel");
            _rawEditPanel = _rootElement.Q<VisualElement>("raw-edit-panel");
            _loadingOverlay = _rootElement.Q<VisualElement>("loading-overlay");
            _waveformDisplay = _rootElement.Q<VisualElement>("waveform-display");
            
            // Get Single clip processing elements
            _sourceClipField = _rootElement.Q<ObjectField>("source-clip-field");
            _sourceClipField.objectType = typeof(AudioClip);
            _sourcePlayButton = _rootElement.Q<Button>("source-play-button");
            _sourceStopButton = _rootElement.Q<Button>("source-stop-button");
            _normalizeToggle = _rootElement.Q<Toggle>("normalize-toggle");
            _trimSilenceToggle = _rootElement.Q<Toggle>("trim-silence-toggle");
            _applyFadesToggle = _rootElement.Q<Toggle>("apply-fades-toggle");
            _makeLoopableToggle = _rootElement.Q<Toggle>("make-loopable-toggle");
            _loopToggle = _rootElement.Q<Toggle>("loop-toggle");
            _pitchSlider = _rootElement.Q<Slider>("pitch-slider");
            _filterSlider = _rootElement.Q<Slider>("filter-slider");
            _processButton = _rootElement.Q<Button>("process-button");
            _playButton = _rootElement.Q<Button>("play-button");
            _pauseButton = _rootElement.Q<Button>("pause-button");
            _stopButton = _rootElement.Q<Button>("stop-button");
            _exportButton = _rootElement.Q<Button>("export-button");
            _timelineSlider = _rootElement.Q<Slider>("timeline-slider");
            _durationLabel = _rootElement.Q<Label>("duration-label");
            _resultLabel = _rootElement.Q<Label>("result-label");
            _clipNameLabel = _rootElement.Q<Label>("clip-name-label");
            _clipLengthLabel = _rootElement.Q<Label>("clip-length-label");
            _clipChannelsLabel = _rootElement.Q<Label>("clip-channels-label");
            _clipFrequencyLabel = _rootElement.Q<Label>("clip-frequency-label");
            
            // Get Batch processing elements
            _sourceClipsListView = _rootElement.Q<ListView>("source-clips-listview");
            _batchNormalizeToggle = _rootElement.Q<Toggle>("batch-normalize-toggle");
            _batchTrimSilenceToggle = _rootElement.Q<Toggle>("batch-trim-silence-toggle");
            _batchApplyFadesToggle = _rootElement.Q<Toggle>("batch-apply-fades-toggle");
            _batchMakeLoopableToggle = _rootElement.Q<Toggle>("batch-make-loopable-toggle");
            _batchPitchSlider = _rootElement.Q<Slider>("batch-pitch-slider");
            _batchFilterSlider = _rootElement.Q<Slider>("batch-filter-slider");
            _outputFolderField = _rootElement.Q<TextField>("output-folder-field");
            _outputFolderField.value = _lastBatchFolder;
            _batchProcessButton = _rootElement.Q<Button>("batch-process-button"); 

            // Get Raw edit elements
            _rawEditClipField = _rootElement.Q<ObjectField>("raw-edit-clip-field");
            if (_rawEditClipField != null) _rawEditClipField.objectType = typeof(AudioClip);
            _rawWaveformDisplay = _rootElement.Q<VisualElement>("raw-waveform-display");
            _rawWaveformContainer = _rootElement.Q<VisualElement>("raw-waveform-container");
            _waveformScrollView = _rootElement.Q<ScrollView>("waveform-scroll-view");
            // _timelineContainer = _rootElement.Q<VisualElement>("timeline-container");
            _timelineLabels = _rootElement.Q<VisualElement>("timeline-labels");
            _zoomSlider = _rootElement.Q<Slider>("zoom-slider");
            _rawTimelineSlider = _rootElement.Q<Slider>("raw-timeline-slider");
            _zoomLabel = _rootElement.Q<Label>("zoom-label");
            _selectionLabel = _rootElement.Q<Label>("selection-label");
            _cursorLabel = _rootElement.Q<Label>("cursor-label");
            _rawPlayButton = _rootElement.Q<Button>("raw-play-button");
            _rawPauseButton = _rootElement.Q<Button>("raw-pause-button");
            _rawStopButton = _rootElement.Q<Button>("raw-stop-button");
            _rawLoopToggle = _rootElement.Q<Toggle>("raw-loop-toggle");
            _cutButton = _rootElement.Q<Button>("cut-button");
            _copyButton = _rootElement.Q<Button>("copy-button");
            _pasteButton = _rootElement.Q<Button>("paste-button");
            _deleteButton = _rootElement.Q<Button>("delete-button");
            _undoButton = _rootElement.Q<Button>("undo-button");
            _redoButton = _rootElement.Q<Button>("redo-button");
            _selectAllButton = _rootElement.Q<Button>("select-all-button");
            _rawExportButton = _rootElement.Q<Button>("raw-export-button");
            _saveSelectionButton = _rootElement.Q<Button>("save-selection-button");
            _fadeInButton = _rootElement.Q<Button>("fade-in-button");   
            _fadeOutButton = _rootElement.Q<Button>("fade-out-button"); 
            _silenceButton = _rootElement.Q<Button>("silence-button");
                        
            _loopableCrossfadeSlider = _rootElement.Q<Slider>("loopable-crossfade-slider"); 
            _loopableCrossfadeContainer = _rootElement.Q<VisualElement>("loopable-crossfade-container"); 

            _batchLoopableCrossfadeSlider = _rootElement.Q<Slider>("batch-loopable-crossfade-slider");
            if (_batchLoopableCrossfadeSlider != null)
                _batchLoopableCrossfadeValueLabel = _batchLoopableCrossfadeSlider.Q<TextField>();
            _batchLoopableCrossfadeContainer = _rootElement.Q<VisualElement>("batch-loopable-crossfade-container");

            
            _rawEditClipField = _rootElement.Q<ObjectField>("raw-edit-clip-field");
            if (_rawEditClipField != null) _rawEditClipField.objectType = typeof(AudioClip);

            _rawWaveformContainer = _rootElement.Q<VisualElement>("raw-waveform-container"); 
            if (_rawWaveformContainer != null)
            {
                _rawWaveformCanvas = new RawWaveformCanvas();
                _rawWaveformCanvas.style.flexGrow = 1; // Make it fill the container
                _rawWaveformCanvas.style.width = Length.Percent(100);
                _rawWaveformCanvas.style.height = Length.Percent(100);
                _rawWaveformContainer.Add(_rawWaveformCanvas);
            }
            else
            {
                Debug.LogError("raw-waveform-container not found in UXML!");
            }

            // Set initial button states
            _sourcePlayButton?.SetEnabled(false);
            _sourceStopButton?.SetEnabled(false);
            _playButton?.SetEnabled(false);
            _pauseButton?.SetEnabled(false);
            _stopButton?.SetEnabled(false);
            _exportButton?.SetEnabled(false);

            // Set initial raw edit button states
            UpdateRawEditButtonStates();
        }
         
        
        private void InitializeListViews()
        {
            _sourceClipsListView.makeItem = () => new ObjectField { objectType = typeof(AudioClip) };
            _sourceClipsListView.bindItem = (element, i) => 
            {
                var field = element as ObjectField;
                field.value = _selectedClips[i];
                field.RegisterValueChangedCallback(evt => 
                {
                    _selectedClips[i] = evt.newValue as AudioClip;
                });
            };
            _sourceClipsListView.itemsSource = _selectedClips; 
        }
        
        private void SetupEventHandlers()
        {
            // Navigation buttons
            _rootElement.Q<Button>("processing-button").clicked += () => SwitchPanel(PanelTypeSP.Processing);
            _rootElement.Q<Button>("batch-button").clicked += () => SwitchPanel(PanelTypeSP.BatchProcessing); 
            var rawEditButton = _rootElement.Q<Button>("raw-edit-button");
            if (rawEditButton != null)
                rawEditButton.clicked += () => SwitchPanel(PanelTypeSP.RawEdit);
            _rootElement.Q<Button>("back-to-creator-button").clicked += () => SoundCreatorWindow.ShowWindow();
            
            // Source clip changed
            _sourceClipField.RegisterValueChangedCallback(evt => 
            {
                _sourceClip = evt.newValue as AudioClip;
                UpdateSourceClipInfo();
                
                _sourcePlayButton.SetEnabled(_sourceClip != null);
                _sourceStopButton.SetEnabled(false);
            });
            
            // Source clip playback
            _sourcePlayButton.clicked += PlaySourceAudio;
            _sourceStopButton.clicked += StopSourceAudio;
            
            // Process button
            _processButton.clicked += ProcessClip;
            
            // Playback controls
            _playButton.clicked += PlayAudio;
            _pauseButton.clicked += PauseAudio;
            _stopButton.clicked += StopAudio;
            _exportButton.clicked += ExportProcessedClip;
            
            // Timeline
            _timelineSlider.RegisterValueChangedCallback(evt => 
            {
                if (!_isPlaying && _processedClip != null)
                {
                    _playbackTime = evt.newValue * _processedClip.length;
                    UpdatePlaybackTimeDisplay();
                }
            });

            // Toggle event handlers
            if (_makeLoopableToggle != null)
            {
                _makeLoopableToggle.RegisterValueChangedCallback(evt => 
                {
                    if (_loopableCrossfadeContainer != null)
                        _loopableCrossfadeContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });
            } 
            if (_batchMakeLoopableToggle != null)
            {
                _batchMakeLoopableToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_batchLoopableCrossfadeContainer != null)
                        _batchLoopableCrossfadeContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }
            if (_batchLoopableCrossfadeSlider != null)
            {
                _batchLoopableCrossfadeSlider.RegisterValueChangedCallback(evt => 
                {
                    if (_batchLoopableCrossfadeValueLabel != null)
                        _batchLoopableCrossfadeValueLabel.value = $"{evt.newValue:F2}s";
                });
            }

            // Batch processing
            _batchProcessButton.clicked += ProcessMultipleClips;
            _rootElement.Q<Button>("add-folder-button").clicked += () => AddFolderClips();
            _rootElement.Q<Button>("clear-button").clicked += ClearSourceClips;
            _rootElement.Q<Button>("browse-button").clicked += BrowseForBatchOutputFolder;
             
            // Raw edit event handlers
            SetupRawEditEventHandlers();
        }

        private void SetupRawEditEventHandlers()
        {
            // Raw edit clip selection
            if (_rawEditClipField != null)
            {
                _rawEditClipField.RegisterValueChangedCallback(evt => 
                {
                    LoadRawEditClip(evt.newValue as AudioClip);
                });
            }

            // Zoom control
            if (_zoomSlider != null)
            {
                _zoomSlider.RegisterValueChangedCallback(evt => 
                {
                    _zoomFactor = evt.newValue;
                    UpdateZoom(evt.newValue);
                    UpdateZoomLabel();
                    UpdateScrollLimits();
                    UpdateRawWaveformDisplay();
                    UpdateTimeline();
                });
            }

            // Timeline control
            if (_rawTimelineSlider != null)
            {
                _rawTimelineSlider.RegisterValueChangedCallback(evt =>
                {
                    if (!_rawIsPlaying)
                    {
                        _cursorPosition = evt.newValue;

                        // If the cursor is moved outside the visible range, adjust scroll to show it
                        if (_cursorPosition < _visibleTimeStart || _cursorPosition > _visibleTimeEnd)
                        {
                            // Center the view on the cursor position
                            float visibleRange = _visibleTimeEnd - _visibleTimeStart;
                            _visibleTimeStart = Mathf.Clamp01(_cursorPosition - visibleRange / 2f);
                            _visibleTimeEnd = Mathf.Clamp01(_visibleTimeStart + visibleRange);

                            // Update scroll position
                            if (_zoomFactor > 1f)
                            {
                                float maxScroll = 1f - (1f / _zoomFactor);
                                _scrollPosition = _visibleTimeStart / (1f - 1f / _zoomFactor);
                                _scrollPosition = Mathf.Clamp01(_scrollPosition);
                            }

                            UpdateRawWaveformDisplay();
                            UpdateTimeline();
                        }

                        UpdateCursorLabel();
                        UpdateRawWaveformDisplay();
                    }
                });
                
                
            }

            // Raw playback controls
            if (_rawPlayButton != null)
                _rawPlayButton.clicked += PlayRawAudio;
            if (_rawPauseButton != null)
                _rawPauseButton.clicked += PauseRawAudio;
            if (_rawStopButton != null)
                _rawStopButton.clicked += StopRawAudio;

            // Edit operations
            if (_cutButton != null)
                _cutButton.clicked += CutSelection;
            if (_copyButton != null)
                _copyButton.clicked += CopySelection;
            if (_pasteButton != null)
                _pasteButton.clicked += PasteAtCursor;
            if (_deleteButton != null)
                _deleteButton.clicked += DeleteSelection;
            if (_undoButton != null)
                _undoButton.clicked += UndoLastAction;
            if (_redoButton != null)
                _redoButton.clicked += RedoLastAction;
            if (_selectAllButton != null)
                _selectAllButton.clicked += SelectAll;
            if (_rawExportButton != null)
                _rawExportButton.clicked += ExportRawEditClip;
            if (_saveSelectionButton != null)
                _saveSelectionButton.clicked += SaveSelection;
            if (_fadeInButton != null)
                _fadeInButton.clicked += ApplyFadeInToSelection;
            if (_fadeOutButton != null)
                _fadeOutButton.clicked += ApplyFadeOutToSelection;
            if (_silenceButton != null)
                _silenceButton.clicked += ApplySilenceToSelection;

            // Waveform interaction
            if (_rawWaveformCanvas != null)
            {
                _rawWaveformCanvas.RegisterCallback<MouseDownEvent>(OnWaveformMouseDown);
                _rawWaveformCanvas.RegisterCallback<MouseMoveEvent>(OnWaveformMouseMove);
                _rawWaveformCanvas.RegisterCallback<MouseUpEvent>(OnWaveformMouseUp);
                _rawWaveformCanvas.RegisterCallback<WheelEvent>(OnWaveformWheel);
                _rawWaveformCanvas.RegisterCallback<MouseLeaveEvent>(OnWaveformMouseLeave);
            }
            else if (_rawWaveformContainer != null) // Fallback to container if canvas init failed
            {
                Debug.LogWarning("RawWaveformCanvas is null, registering mouse events on _rawWaveformContainer.");
                _rawWaveformContainer.RegisterCallback<MouseDownEvent>(OnWaveformMouseDown);
                _rawWaveformContainer.RegisterCallback<MouseMoveEvent>(OnWaveformMouseMove);
                _rawWaveformContainer.RegisterCallback<MouseUpEvent>(OnWaveformMouseUp);
                _rawWaveformContainer.RegisterCallback<MouseLeaveEvent>(OnWaveformMouseLeave);
                _rawWaveformContainer.RegisterCallback<WheelEvent>(OnWaveformWheel);
            }


            // Scroll view events 
            _waveformScrollView = _rootElement.Q<ScrollView>("waveform-scroll-view");
            if (_waveformScrollView != null)
            { 
                _waveformScrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
            }
        }

        private void UpdateZoom(float newValue)
        { 
            // take center of the view into account
            float canvasWidth = _rawWaveformCanvas != null ? _rawWaveformCanvas.resolvedStyle.width : _rawWaveformContainer.resolvedStyle.width;
            float mouseX = canvasWidth/2;
            float relativeMouseInView = 0.5f; // Default to center if width is zero
            if (canvasWidth > 0) {
                relativeMouseInView = Mathf.Clamp01(mouseX / canvasWidth);
            }
            float timeAtMouse = _visibleTimeStart + relativeMouseInView * (_visibleTimeEnd - _visibleTimeStart);

            // Calculate new visible window width (normalized)
            float newVisibleWindowWidth = 1f / _zoomFactor;

            // Adjust _visibleTimeStart so timeAtMouse remains at the same relativeMouseInView
            _visibleTimeStart = timeAtMouse - (relativeMouseInView * newVisibleWindowWidth);
            _visibleTimeStart = Mathf.Clamp01(_visibleTimeStart);

            _visibleTimeEnd = _visibleTimeStart + newVisibleWindowWidth;
            _visibleTimeEnd = Mathf.Clamp01(_visibleTimeEnd);

            // If clamping _visibleTimeEnd changed its length from newVisibleWindowWidth, readjust _visibleTimeStart
            _visibleTimeStart = Mathf.Clamp01(_visibleTimeEnd - newVisibleWindowWidth);
            
            _scrollPosition = _visibleTimeStart; // Scroll position is the start of the visible window
                if (_zoomFactor <= 1.001f) // Handle float precision for zoom = 1
            {
                _zoomFactor = 1f;
                _scrollPosition = 0f;
                _visibleTimeStart = 0f;
                _visibleTimeEnd = 1f;
                if (_zoomSlider != null) _zoomSlider.SetValueWithoutNotify(1f);
            }

        }
        #endregion

        #region Panel Management
        private void SwitchPanel(PanelTypeSP panelType)
        {
            StopAudioPlayback();
            StopRawPlayback();
            
            _processingPanel.style.display = DisplayStyle.None;
            _batchProcessingPanel.style.display = DisplayStyle.None;
            if (_rawEditPanel != null)
                _rawEditPanel.style.display = DisplayStyle.None;
            
            _rootElement.Query<Button>(className: "sidebar-button").ForEach(btn => 
            {
                btn.RemoveFromClassList("active");
            });
            
            string buttonName = "";
            
            switch (panelType)
            {
                case PanelTypeSP.Processing:
                    _processingPanel.style.display = DisplayStyle.Flex;
                    buttonName = "processing-button";
                    break;
                    
                case PanelTypeSP.BatchProcessing:
                    _batchProcessingPanel.style.display = DisplayStyle.Flex;
                    buttonName = "batch-button";
                    break;
                    

                case PanelTypeSP.RawEdit:
                    if (_rawEditPanel != null)
                        _rawEditPanel.style.display = DisplayStyle.Flex;
                    buttonName = "raw-edit-button";
                    break;
            }
            
            if (!string.IsNullOrEmpty(buttonName))
            {
                var activeButton = _rootElement.Q<Button>(buttonName);
                if (activeButton != null)
                    activeButton.AddToClassList("active");
            }
            
            _currentPanel = panelType;
        }
        #endregion

        #region Raw Edit Mode Implementation
        private void LoadRawEditClip(AudioClip clip)
        {
            _rawEditClip = clip;
            _isRawClipDirtyForPlayback = false;
            
            if (clip == null)
            {
                _rawAudioData = null;
                _waveformData = null;
                _cursorPosition = 0f;
                _selectionStart = 0f;
                _selectionEnd = 0f;
                _hasSelection = false;
                _zoomFactor = 1f;
                _scrollPosition = 0f;
                _visibleTimeStart = 0f;
                _visibleTimeEnd = 1f;
                        
                if (_zoomSlider != null) _zoomSlider.SetValueWithoutNotify(1f); 
                UpdateUIDueToClipLengthChange(); 
                UpdateRawEditButtonStates();
                return;
            }

            // Load audio data
            _rawAudioData = new float[clip.samples * clip.channels];
            clip.GetData(_rawAudioData, 0);

            // Generate waveform data
            GenerateDetailedWaveformData();

            // Reset editing state
            _cursorPosition = 0f;
            _selectionStart = 0f;
            _selectionEnd = 0f;
            _hasSelection = false;
            _zoomFactor = 1f; 
            _scrollPosition = 0f; 
            _visibleTimeStart = 0f; 
            _visibleTimeEnd = 1f;  
            _amplitudeScale = 1f;
 


            if (_zoomSlider != null) _zoomSlider.SetValueWithoutNotify(_zoomFactor);
            if (_rawTimelineSlider != null) _rawTimelineSlider.value = _cursorPosition;
 
            UpdateUIDueToClipLengthChange();
            UpdateRawEditButtonStates();
            UpdateRawWaveformDisplay(); 
            UpdateTimeline();
            UpdateLabels();

            //Debug.Log($"Loaded raw edit clip: {clip.name}, samples: {clip.samples}, channels: {clip.channels}");
        }
        private void GetSelectionSampleIndices(out int startSampleIndex, out int endSampleIndex)
        {
            if (_rawEditClip == null || _rawAudioData == null)
            {
                startSampleIndex = 0;
                endSampleIndex = 0;
                return;
            }

            int totalSampleFrames = _rawEditClip.samples;
            float startNorm = Mathf.Min(_selectionStart, _selectionEnd);
            float endNorm = Mathf.Max(_selectionStart, _selectionEnd);

            startSampleIndex = Mathf.RoundToInt(startNorm * totalSampleFrames);
            endSampleIndex = Mathf.RoundToInt(endNorm * totalSampleFrames);
            
            startSampleIndex = Mathf.Clamp(startSampleIndex, 0, totalSampleFrames);
            endSampleIndex = Mathf.Clamp(endSampleIndex, startSampleIndex, totalSampleFrames);
        }

        private void GenerateDetailedWaveformData()
        {
            if (_rawAudioData == null || _rawEditClip == null) return;

            int totalAudioSamples = _rawAudioData.Length / _rawEditClip.channels;
            int waveformSamples = Mathf.Min(totalAudioSamples, 4096);
            _waveformData = new float[waveformSamples];

            float samplesPerPoint = (float)totalAudioSamples / waveformSamples;

            for (int i = 0; i < waveformSamples; i++)
            {
                float maxPositive = 0f;
                float maxNegative = 0f;
                
                float startSampleFloat = i * samplesPerPoint;
                float endSampleFloat = (i + 1) * samplesPerPoint;
                
                int startSample = Mathf.FloorToInt(startSampleFloat);
                int endSample = Mathf.CeilToInt(endSampleFloat);
                endSample = Mathf.Min(endSample, totalAudioSamples);

                for (int s = startSample; s < endSample; s++)
                {
                    for (int c = 0; c < _rawEditClip.channels; c++)
                    {
                        int sampleIndex = s * _rawEditClip.channels + c;
                        if (sampleIndex < _rawAudioData.Length)
                        {
                            float sample = _rawAudioData[sampleIndex];
                            if (sample > maxPositive)
                                maxPositive = sample;
                            if (sample < maxNegative)
                                maxNegative = sample;
                        }
                    }
                }

                if (Mathf.Abs(maxPositive) >= Mathf.Abs(maxNegative))
                {
                    _waveformData[i] = maxPositive;
                }
                else
                {
                    _waveformData[i] = maxNegative;
                }
            }
        }



        private void UpdateRawWaveformDisplay()
        {
            if (_rawWaveformCanvas == null) return;
 
            _rawWaveformCanvas.WaveformData = _waveformData; 
            _rawWaveformCanvas.VisibleTimeStart = _visibleTimeStart;
            _rawWaveformCanvas.VisibleTimeEnd = _visibleTimeEnd;
            _rawWaveformCanvas.SelectionStart = _selectionStart;
            _rawWaveformCanvas.SelectionEnd = _selectionEnd;
            _rawWaveformCanvas.HasSelection = _hasSelection;
            _rawWaveformCanvas.CursorPosition = _cursorPosition;
            _rawWaveformCanvas.AmplitudeScale = _amplitudeScale;

            _rawWaveformCanvas.MarkDirtyRepaint();
        }

        private void OnWaveformMouseDown(MouseDownEvent evt)
        {
            if (_rawEditClip == null || _rawWaveformCanvas == null) return;
            _rawWaveformCanvas.Focus(); 

            float canvasWidth = _rawWaveformCanvas.resolvedStyle.width;
            if (canvasWidth <= 0) return;

            float relativeClickInView = Mathf.Clamp01(evt.localMousePosition.x / canvasWidth);
            float timeClicked = _visibleTimeStart + relativeClickInView * (_visibleTimeEnd - _visibleTimeStart);
            timeClicked = Mathf.Clamp01(timeClicked);

            _cursorPosition = timeClicked;
            _isSelecting = true;
            _selectionStart = timeClicked;
            _selectionEnd = timeClicked;
            _hasSelection = false;

            //Debug.Log($"Clicked at time: {timeClicked}, relative: {relativeClickInView}");

            if (_rawTimelineSlider != null) _rawTimelineSlider.SetValueWithoutNotify(_cursorPosition);
            
            UpdateRawWaveformDisplay();
            UpdateLabels();
            UpdateRawEditButtonStates();
        }

        private void OnWaveformMouseMove(MouseMoveEvent evt)
        {
            if (!_isSelecting || _rawEditClip == null || _rawWaveformCanvas == null) return;

            float canvasWidth = _rawWaveformCanvas.resolvedStyle.width;
            if (canvasWidth <= 0) return;
            
            float relativeMoveInView = Mathf.Clamp01(evt.localMousePosition.x / canvasWidth);
            float timeMovedTo = _visibleTimeStart + relativeMoveInView * (_visibleTimeEnd - _visibleTimeStart);
            timeMovedTo = Mathf.Clamp01(timeMovedTo);

            _selectionEnd = timeMovedTo; 

            float minSelectionThresholdView = (_visibleTimeEnd - _visibleTimeStart) / (canvasWidth * 2f) ; 
            float minSelectionThresholdGlobal = minSelectionThresholdView; 
            _hasSelection = Mathf.Abs(_selectionEnd - _selectionStart) > minSelectionThresholdGlobal;

            UpdateRawWaveformDisplay();
            UpdateLabels();
            UpdateRawEditButtonStates();
        }
        private void OnWaveformMouseLeave(MouseLeaveEvent evt)
        {
            if (_rawEditClip == null || _rawWaveformCanvas == null) return;
            if (_isSelecting)
            {
                _isSelecting = false; 
                if (_selectionStart > _selectionEnd)
                {
                    float temp = _selectionStart;
                    _selectionStart = _selectionEnd;
                    _selectionEnd = temp;
                }
                
                UpdateRawWaveformDisplay();
                UpdateLabels();
                UpdateRawEditButtonStates();
            }
            
            
        }
        private void OnWaveformMouseUp(MouseUpEvent evt)
        {
            if (_rawEditClip == null || _rawWaveformCanvas == null) return;
            _isSelecting = false;

            float canvasWidth = _rawWaveformCanvas.resolvedStyle.width;
            if (canvasWidth <= 0)
            {
                UpdateRawEditButtonStates();
                UpdateRawWaveformDisplay();
                UpdateLabels();
                return;
            }

            float relativeReleaseInView = Mathf.Clamp01(evt.localMousePosition.x / canvasWidth);
            float timeReleasedAt = _visibleTimeStart + relativeReleaseInView * (_visibleTimeEnd - _visibleTimeStart);
            timeReleasedAt = Mathf.Clamp01(timeReleasedAt);

            if (!_hasSelection)
            {
                _cursorPosition = timeReleasedAt;
                if (_rawTimelineSlider != null) _rawTimelineSlider.SetValueWithoutNotify(_cursorPosition);
                // Clear any potential minor selection "flicker"
                _selectionStart = _cursorPosition;
                _selectionEnd = _cursorPosition;
            }
            else
            {
                _selectionEnd = timeReleasedAt; 
                if (_selectionStart > _selectionEnd)
                {
                    float temp = _selectionStart;
                    _selectionStart = _selectionEnd;
                    _selectionEnd = temp;
                }
            }

            UpdateRawEditButtonStates();
            UpdateRawWaveformDisplay();
            UpdateLabels();
        }

        
        private void OnWaveformWheel(WheelEvent evt)
        {
            if (_rawEditClip == null) return;

            bool ctrlPressed = evt.ctrlKey || evt.commandKey;
            bool altPressed = evt.altKey;

            if (ctrlPressed) // Zoom
            {
                float oldZoomFactor = _zoomFactor;
                float zoomDelta = evt.delta.y > 0 ? 0.8f : 1.25f; 
                _zoomFactor = Mathf.Clamp(_zoomFactor * zoomDelta, 1f, 50f);

                if (Mathf.Approximately(oldZoomFactor, _zoomFactor)) return;

                if (_zoomSlider != null) _zoomSlider.SetValueWithoutNotify(_zoomFactor);

                // Calculate the time point under the mouse cursor before zoom
                float mouseX = evt.localMousePosition.x;
                float canvasWidth = _rawWaveformCanvas != null ? _rawWaveformCanvas.resolvedStyle.width : _rawWaveformContainer.resolvedStyle.width;
                float relativeMouseInView = 0.5f; 
                if (canvasWidth > 0)
                {
                    relativeMouseInView = Mathf.Clamp01(mouseX / canvasWidth);
                }
                float timeAtMouse = _visibleTimeStart + relativeMouseInView * (_visibleTimeEnd - _visibleTimeStart);

                // Calculate new visible window width (normalized)
                float newVisibleWindowWidth = 1f / _zoomFactor;

                // Adjust _visibleTimeStart so timeAtMouse remains at the same relativeMouseInView
                _visibleTimeStart = timeAtMouse - (relativeMouseInView * newVisibleWindowWidth);
                _visibleTimeStart = Mathf.Clamp01(_visibleTimeStart);

                _visibleTimeEnd = _visibleTimeStart + newVisibleWindowWidth;
                _visibleTimeEnd = Mathf.Clamp01(_visibleTimeEnd);

                // If clamping _visibleTimeEnd changed its length from newVisibleWindowWidth, readjust _visibleTimeStart
                _visibleTimeStart = Mathf.Clamp01(_visibleTimeEnd - newVisibleWindowWidth);

                _scrollPosition = _visibleTimeStart; // Scroll position is the start of the visible window
                if (_zoomFactor <= 1.001f) // Handle float precision for zoom = 1
                {
                    _zoomFactor = 1f;
                    _scrollPosition = 0f;
                    _visibleTimeStart = 0f;
                    _visibleTimeEnd = 1f;
                    if (_zoomSlider != null) _zoomSlider.SetValueWithoutNotify(1f);
                }


                UpdateZoomLabel();
                UpdateRawWaveformDisplay();
                UpdateTimeline();
            }
            else if (altPressed) // Amplitude Scale
            {
                float amplitudeScaleDelta = evt.delta.y > 0 ? 0.8f : 1.25f;
                _amplitudeScale = Mathf.Clamp(_amplitudeScale * amplitudeScaleDelta, 0.1f, 10f);
                UpdateZoomLabel(); 
                UpdateRawWaveformDisplay();
            }
            else // Horizontal Scroll
            {
                if (_zoomFactor <= 1.001f) return; 

                float scrollAmount = (evt.delta.y > 0 ? 0.1f : -0.1f) / _zoomFactor; 

                float maxScroll = 1f - (1f / _zoomFactor); 
                _visibleTimeStart = Mathf.Clamp(_visibleTimeStart - scrollAmount, 0f, maxScroll); 
                _visibleTimeEnd = _visibleTimeStart + (1f / _zoomFactor);
                _visibleTimeEnd = Mathf.Min(_visibleTimeEnd, 1f); 

                _scrollPosition = _visibleTimeStart; 

                UpdateRawWaveformDisplay();
                UpdateTimeline(); 
            }
            
            evt.StopPropagation();
        }


        private void OnScrollViewGeometryChanged(GeometryChangedEvent evt)
        {
            if (_rawWaveformCanvas != null && _rawWaveformCanvas.WaveformData != null)
            {
                UpdateRawWaveformDisplay();
                UpdateTimeline();
            }
        }
 

        private void UpdateScrollLimits()
        {
            if (_waveformScrollView == null) return;
            
            float contentWidth = _rawWaveformContainer.resolvedStyle.width * _zoomFactor;
        }

        private void UpdateTimeline()
        {
            if (_timelineLabels == null || _rawEditClip == null) return;

            _timelineLabels.Clear();

            float containerWidth = _rawWaveformContainer.resolvedStyle.width;
            if (containerWidth <= 0) return;

            float totalDuration = _rawEditClip.length;
            float visibleDuration = totalDuration * (_visibleTimeEnd - _visibleTimeStart);
            float visibleStartTime = totalDuration * _visibleTimeStart;

            // Calculate appropriate time interval for labels
            float[] intervals = { 0.001f, 0.01f, 0.1f, 0.5f, 1f, 5f, 10f, 30f, 60f };
            float targetLabelSpacing = 80f; // pixels
            float timePerPixel = visibleDuration / containerWidth;
            float targetInterval = timePerPixel * targetLabelSpacing;

            float interval = intervals[0];
            foreach (float i in intervals)
            {
                if (i >= targetInterval)
                {
                    interval = i;
                    break;
                }
            }

            // Generate timeline labels
            float startTime = Mathf.Floor(visibleStartTime / interval) * interval;
            for (float time = startTime; time <= visibleStartTime + visibleDuration; time += interval)
            {
                if (time < 0) continue;

                float relativePosition = (time - visibleStartTime) / visibleDuration;
                if (relativePosition < 0 || relativePosition > 1) continue;

                float pixelPosition = relativePosition * containerWidth;

                var timeLabel = new Label(FormatTime(time));
                timeLabel.style.position = Position.Absolute;
                timeLabel.style.left = pixelPosition - 30;
                timeLabel.style.width = 60;
                timeLabel.style.fontSize = 10;
                timeLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                timeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

                _timelineLabels.Add(timeLabel);

                // Add tick mark
                var tick = new VisualElement();
                tick.style.position = Position.Absolute;
                tick.style.left = pixelPosition;
                tick.style.width = 1;
                tick.style.height = 8;
                tick.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1f);

                _timelineLabels.Add(tick);
            }
        }

        private string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds < 1f)
            {
                return $"{(timeInSeconds * 1000):F0}ms";
            }
            else if (timeInSeconds < 60f)
            {
                return $"{timeInSeconds:F2}s";
            }
            else
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
                int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
                return $"{minutes}:{seconds:00}";
            }
        }

        private void UpdateRawEditButtonStates()
        {
            bool hasClip = _rawEditClip != null;
            bool hasData = _rawAudioData != null && _rawAudioData.Length > 0;
            bool hasSelection = _hasSelection && hasData;
            bool hasClipboard = _clipboard != null && _clipboard.Length > 0;
            bool canUndo = _undoStack.Count > 0;
            bool canRedo = _redoStack.Count > 0;
            bool enableFadeButtons = hasSelection; 

            if (_fadeInButton != null)
                _fadeInButton.SetEnabled(enableFadeButtons);
            if (_fadeOutButton != null)
                _fadeOutButton.SetEnabled(enableFadeButtons);
            if (_silenceButton != null)
                _silenceButton.SetEnabled(enableFadeButtons);

            if (_rawPlayButton != null)
                    _rawPlayButton.SetEnabled(hasData);
            if (_rawPauseButton != null)
                _rawPauseButton.SetEnabled(_rawIsPlaying);
            if (_rawStopButton != null)
                _rawStopButton.SetEnabled(_rawIsPlaying || _rawIsPaused);
            if (_cutButton != null)
                _cutButton.SetEnabled(hasSelection);
            if (_copyButton != null)
                _copyButton.SetEnabled(hasSelection);
            if (_pasteButton != null)
                _pasteButton.SetEnabled(hasClipboard && hasData);
            if (_deleteButton != null)
                _deleteButton.SetEnabled(hasSelection);
            if (_undoButton != null)
                _undoButton.SetEnabled(canUndo);
            if (_redoButton != null)
                _redoButton.SetEnabled(canRedo);
            if (_selectAllButton != null)
                _selectAllButton.SetEnabled(hasData);
            if (_rawExportButton != null)
                _rawExportButton.SetEnabled(hasData);
            if (_saveSelectionButton != null)
                _saveSelectionButton.SetEnabled(hasSelection);
        }

        private void UpdateLabels()
        {
            if (_rawEditClip == null)
            {
                if (_zoomLabel != null)
                    _zoomLabel.text = "Zoom: -- | Amplitude: --";
                if (_selectionLabel != null)
                    _selectionLabel.text = "Selection: None";
                if (_cursorLabel != null)
                    _cursorLabel.text = "Cursor: --";
                return;
            }

            float duration = _rawEditClip.length;

            if (_zoomLabel != null)
                _zoomLabel.text = $"Zoom: {_zoomFactor:F1}x | Amplitude: {_amplitudeScale:F1}x";
            if (_cursorLabel != null)
                _cursorLabel.text = $"Cursor: {(_cursorPosition * duration):F3}s";

            if (_hasSelection)
            {
                float selStart = _selectionStart * duration;
                float selEnd = _selectionEnd * duration;
                float selDuration = Mathf.Abs(selEnd - selStart);
                if (_selectionLabel != null)
                    _selectionLabel.text = $"Selection: {selStart:F3}s - {selEnd:F3}s ({selDuration:F3}s)";
            }
            else
            {
                if (_selectionLabel != null)
                    _selectionLabel.text = "Selection: None";
            }
        }

        private void UpdateZoomLabel()
        {
            if (_zoomLabel != null)
                _zoomLabel.text = $"Zoom: {_zoomFactor:F1}x | Amplitude: {_amplitudeScale:F1}x";
        }

        private void UpdateCursorLabel()
        {
            if (_rawEditClip != null && _cursorLabel != null)
            {
                float time = _cursorPosition * _rawEditClip.length;
                _cursorLabel.text = $"Cursor: {time:F3}s";
            }
        }
        #endregion

        #region Raw Edit Operations
        private void CutSelection()
        {
            CopySelection();
            DeleteSelection();
        }

        private void CopySelection()
        {
            if (!_hasSelection || _rawAudioData == null) return;

            int totalSamples = _rawAudioData.Length / _rawEditClip.channels;
            int startSample = Mathf.FloorToInt(_selectionStart * totalSamples);
            int endSample = Mathf.FloorToInt(_selectionEnd * totalSamples);
            
            // Ensure proper order
            if (startSample > endSample)
            {
                int temp = startSample;
                startSample = endSample;
                endSample = temp;
            }
            
            int selectionLength = endSample - startSample;

            if (selectionLength <= 0) return;

            _clipboard = new float[selectionLength * _rawEditClip.channels];
            
            for (int i = 0; i < selectionLength; i++)
            {
                for (int c = 0; c < _rawEditClip.channels; c++)
                {
                    int sourceIndex = (startSample + i) * _rawEditClip.channels + c;
                    int destIndex = i * _rawEditClip.channels + c;
                    
                    if (sourceIndex < _rawAudioData.Length)
                    {
                        _clipboard[destIndex] = _rawAudioData[sourceIndex];
                    }
                }
            }

            UpdateRawEditButtonStates();
            //Debug.Log($"Copied {selectionLength} samples to clipboard (from {_selectionStart:F3} to {_selectionEnd:F3})");
        }

        private void PasteAtCursor()
        {
            if (_clipboard == null || _rawAudioData == null) return;

            SaveUndoState();

            int clipboardSamples = _clipboard.Length / _rawEditClip.channels;
            int totalSamples = _rawAudioData.Length / _rawEditClip.channels;
            int insertPosition = Mathf.FloorToInt(_cursorPosition * totalSamples);

            // Create new audio data array
            float[] newAudioData = new float[(_rawAudioData.Length + _clipboard.Length)];
            
            // Copy data before insertion point
            int beforeInsertBytes = insertPosition * _rawEditClip.channels;
            Array.Copy(_rawAudioData, 0, newAudioData, 0, beforeInsertBytes);
            
            // Copy clipboard data
            Array.Copy(_clipboard, 0, newAudioData, beforeInsertBytes, _clipboard.Length);
            
            // Copy data after insertion point
            int afterInsertStart = beforeInsertBytes + _clipboard.Length;
            int remainingBytes = _rawAudioData.Length - beforeInsertBytes;
            Array.Copy(_rawAudioData, beforeInsertBytes, newAudioData, afterInsertStart, remainingBytes);

            // Update audio data
            _rawAudioData = newAudioData;
            
            // Create new audio clip
            UpdateRawEditClipFromData();
            _isRawClipDirtyForPlayback = true;
            
            // Clear selection and update cursor
            _hasSelection = false;
            _cursorPosition = (float)(insertPosition + clipboardSamples) / (totalSamples + clipboardSamples);
            
            UpdateUIDueToClipLengthChange();
            GenerateDetailedWaveformData();
            UpdateRawWaveformDisplay();
            UpdateLabels();
            UpdateRawEditButtonStates();

            //Debug.Log($"Pasted {clipboardSamples} samples at position {insertPosition}");
        }
        
        private void ApplyFadeInToSelection()
        {
            if (!_hasSelection || _rawAudioData == null || _rawEditClip == null || _rawEditClip.samples == 0 || _rawEditClip.channels == 0)
            {
                return;
            }

            SaveUndoState(); 

            int channels = _rawEditClip.channels;
            
            // Use the helper method to get consistent sample indices
            GetSelectionSampleIndices(out int firstSampleFrame, out int lastSampleFrame);
            
            int numberOfFramesToFade = lastSampleFrame - firstSampleFrame;
            if (numberOfFramesToFade <= 0)
            {
                return;
            }

            for (int i = 0; i < numberOfFramesToFade; i++)
            {
                float multiplier = (numberOfFramesToFade == 1) ? 0.5f : (float)i / (numberOfFramesToFade - 1);
                int currentSampleFrameInClip = firstSampleFrame + i;

                for (int c = 0; c < channels; c++)
                {
                    int sampleIndex = currentSampleFrameInClip * channels + c;
                    if (sampleIndex >= 0 && sampleIndex < _rawAudioData.Length) 
                    {
                        _rawAudioData[sampleIndex] *= multiplier;
                    }
                }
            }

            UpdateRawEditClipFromData(); 
            _isRawClipDirtyForPlayback = true; 

            GenerateDetailedWaveformData(); 
            UpdateLabels();
            UpdateTimeline(); 
            UpdateRawWaveformDisplay();
            UpdateRawEditButtonStates(); 
        }

        private void ApplyFadeOutToSelection()
        {
            if (!_hasSelection || _rawAudioData == null || _rawEditClip == null || _rawEditClip.samples == 0 || _rawEditClip.channels == 0)
            {
                return;
            }

            SaveUndoState();

            int channels = _rawEditClip.channels;
            
            // Use the helper method to get consistent sample indices
            GetSelectionSampleIndices(out int firstSampleFrame, out int lastSampleFrame);
            
            int numberOfFramesToFade = lastSampleFrame - firstSampleFrame;
            if (numberOfFramesToFade <= 0)
            {
                return;
            }
            
            for (int i = 0; i < numberOfFramesToFade; i++)
            {
                float multiplier = (numberOfFramesToFade == 1) ? 0.5f : 1.0f - ((float)i / (numberOfFramesToFade - 1));
                int currentSampleFrameInClip = firstSampleFrame + i;

                for (int c = 0; c < channels; c++)
                {
                    int sampleIndex = currentSampleFrameInClip * channels + c;
                    if (sampleIndex >= 0 && sampleIndex < _rawAudioData.Length)
                    {
                        _rawAudioData[sampleIndex] *= multiplier;
                    }
                }
            }

            UpdateRawEditClipFromData();
            _isRawClipDirtyForPlayback = true;

            GenerateDetailedWaveformData();
            UpdateLabels();
            UpdateTimeline();
            UpdateRawWaveformDisplay();
            UpdateRawEditButtonStates();
        }

        private void ApplySilenceToSelection()
        {
            if (!_hasSelection || _rawAudioData == null || _rawEditClip == null || _rawEditClip.samples == 0 || _rawEditClip.channels == 0)
            {
                return;
            }

            SaveUndoState(); 

            //Debug.Log($"Applying silence to selection: {_selectionStart:F3} - {_selectionEnd:F3}");

            int channels = _rawEditClip.channels;
            
            // Use the helper method to get consistent sample indices
            GetSelectionSampleIndices(out int firstSampleFrame, out int lastSampleFrame);

            int numberOfFramesToSilence = lastSampleFrame - firstSampleFrame;
            if (numberOfFramesToSilence <= 0)
            {
                return;
            }

            for (int i = 0; i < numberOfFramesToSilence; i++)
            {
                int currentSampleFrameInClip = firstSampleFrame + i;
                for (int c = 0; c < channels; c++)
                {
                    int sampleIndex = currentSampleFrameInClip * channels + c;
                    if (sampleIndex >= 0 && sampleIndex < _rawAudioData.Length) 
                    {
                        _rawAudioData[sampleIndex] = 0.0f;
                    }
                }
            }

            UpdateRawEditClipFromData(); 
            _isRawClipDirtyForPlayback = true; 

            GenerateDetailedWaveformData(); 
            UpdateLabels();
            UpdateTimeline(); 
            UpdateRawWaveformDisplay();
            UpdateRawEditButtonStates(); 
        }


        private void DeleteSelection()
        {
            if (!_hasSelection || _rawAudioData == null) return;

            SaveUndoState();

            int totalSamplesOriginal = _rawAudioData.Length / _rawEditClip.channels; // Before modification
            int startSampleGlobal = Mathf.FloorToInt(Mathf.Min(_selectionStart, _selectionEnd) * totalSamplesOriginal);
            int endSampleGlobal = Mathf.FloorToInt(Mathf.Max(_selectionStart, _selectionEnd) * totalSamplesOriginal);

            int deletedSamples = endSampleGlobal - startSampleGlobal;

            if (deletedSamples <= 0) return;

            // Create new audio data array
            int newRawDataLength = _rawAudioData.Length - (deletedSamples * _rawEditClip.channels);
            if (newRawDataLength < 0) newRawDataLength = 0; 
            float[] newAudioData = new float[newRawDataLength];

            int beforeDeleteBytes = startSampleGlobal * _rawEditClip.channels;
            Array.Copy(_rawAudioData, 0, newAudioData, 0, beforeDeleteBytes);

            int afterDeleteOriginalStartSampleIndex = endSampleGlobal * _rawEditClip.channels;
            int remainingBytes = _rawAudioData.Length - afterDeleteOriginalStartSampleIndex;
            if (remainingBytes > 0 && (beforeDeleteBytes + remainingBytes <= newAudioData.Length))
            {
                Array.Copy(_rawAudioData, afterDeleteOriginalStartSampleIndex, newAudioData, beforeDeleteBytes, remainingBytes);
            }

            _rawAudioData = newAudioData;

            float cursorTargetNormalized = Mathf.Min(_selectionStart, _selectionEnd);


            UpdateRawEditClipFromData();
            _isRawClipDirtyForPlayback = true;

            GenerateDetailedWaveformData(); 

            if (_rawEditClip != null)
            {
                //Debug.Log($"After Delete + Update: Clip Name: {_rawEditClip.name}, Samples: {_rawEditClip.samples}, Length: {_rawEditClip.length:F3}s");
                //Debug.Log($"_rawAudioData.Length: {_rawAudioData.Length}, Expected samples per channel: {_rawAudioData.Length / _rawEditClip.channels}");
                if (_rawEditClip.samples != (_rawAudioData.Length / _rawEditClip.channels) && _rawEditClip.channels > 0)
                {
                    Debug.LogError("CRITICAL: _rawEditClip.samples does NOT match _rawAudioData length after update!");
                }
            }
            else Debug.LogError("CRITICAL: _rawEditClip became NULL after UpdateRawEditClipFromData!");

            _hasSelection = false;
            _cursorPosition = cursorTargetNormalized;
            if (_rawEditClip != null && _rawEditClip.length > 0)
            {
                _cursorPosition = Mathf.Clamp(_cursorPosition, 0f, 1f);
                if (_cursorPosition * _rawEditClip.length > _rawEditClip.length - 0.001f)
                {
                    _cursorPosition = 1f;
                }

            }
            else
            {
                _cursorPosition = 0f;
            }
            if (_rawTimelineSlider != null) _rawTimelineSlider.SetValueWithoutNotify(_cursorPosition);

            UpdateUIDueToClipLengthChange();

            UpdateRawWaveformDisplay();
            UpdateLabels();
            UpdateRawEditButtonStates();

            //Debug.Log($"Deleted {deletedSamples} sample frames. Cursor at normalized pos: {_cursorPosition:F3} (time: {(_rawEditClip != null ? _cursorPosition * _rawEditClip.length : 0):F3}s)");
        }

        private void SelectAll()
        {
            if (_rawEditClip == null) return;

            _selectionStart = 0f;
            _selectionEnd = 1f;
            _hasSelection = true;

            UpdateRawWaveformDisplay();
            UpdateLabels();
            UpdateRawEditButtonStates();
        }

        private void UndoLastAction()
        {
            if (_undoStack.Count == 0) return;

            var action = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);

            // Save current state to redo stack
            SaveRedoState();

            // Restore previous state
            _rawAudioData = action.AudioData;
            UpdateRawEditClipFromData();
            _isRawClipDirtyForPlayback = true;
            
            GenerateDetailedWaveformData();
            
            _cursorPosition = 0f;
            _hasSelection = false;
            _selectionStart = 0f;
            _selectionEnd = 0f;

            UpdateUIDueToClipLengthChange();


            UpdateRawWaveformDisplay();
            UpdateLabels();
            UpdateRawEditButtonStates();

            //Debug.Log("Undid last action");
        }

        private void RedoLastAction()
        {
            if (_redoStack.Count == 0) return;

            var action = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);

            // Save current state to undo stack
            SaveUndoState();

            // Apply redo state
            _rawAudioData = action.AudioData;
            UpdateRawEditClipFromData();
            _isRawClipDirtyForPlayback = true;
            
            GenerateDetailedWaveformData();
            
            _cursorPosition = 0f;
            _hasSelection = false;
            _selectionStart = 0f;
            _selectionEnd = 0f;
 
            UpdateUIDueToClipLengthChange(); 
            
            UpdateRawWaveformDisplay();
            UpdateLabels();
            UpdateRawEditButtonStates();

            //Debug.Log("Redid last action");
        }

        private void SaveUndoState()
        {
            if (_rawAudioData == null) return;

            var action = new AudioEditAction
            {
                AudioData = (float[])_rawAudioData.Clone(),
                Timestamp = DateTime.Now
            };

            _undoStack.Add(action);

            // Limit undo stack size
            if (_undoStack.Count > 20)
            {
                _undoStack.RemoveAt(0);
            }

            // Clear redo stack when new action is performed
            _redoStack.Clear();
        }

        private void SaveRedoState()
        {
            if (_rawAudioData == null) return;

            var action = new AudioEditAction
            {
                AudioData = (float[])_rawAudioData.Clone(),
                Timestamp = DateTime.Now
            };

            _redoStack.Add(action);

            // Limit redo stack size
            if (_redoStack.Count > 20)
            {
                _redoStack.RemoveAt(0);
            }
        }

        private void UpdateRawEditClipFromData()
        {
            if (_rawAudioData == null || _rawEditClip == null) return;

            int newSampleCount = _rawAudioData.Length / _rawEditClip.channels;
            
            AudioClip newClip = AudioClip.Create(
                _rawEditClip.name + "_edited",
                newSampleCount,
                _rawEditClip.channels,
                _rawEditClip.frequency,
                false
            );

            newClip.SetData(_rawAudioData, 0);
            _rawEditClip = newClip;
        }

        private void ExportRawEditClip()
        {
            if (_rawEditClip == null) return;

            string defaultName = $"{_rawEditClip.name}_raw_edited.wav";
            string initialDir = "";
            
            if (!string.IsNullOrEmpty(_lastSavePath) && Directory.Exists(Path.GetDirectoryName(_lastSavePath)))
            {
                initialDir = Path.GetDirectoryName(_lastSavePath);
            }
            else 
            {
                initialDir = Path.Combine(Application.dataPath, "Sounds");
                if (!Directory.Exists(initialDir))
                {
                    initialDir = Application.dataPath;
                }
            }
            
            string savePath = EditorUtility.SaveFilePanel("Export Raw Edited Clip", 
                initialDir, defaultName, "wav");
                
            if (string.IsNullOrEmpty(savePath)) return;
            
            try
            {
                string saveDir = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                
                _lastSavePath = savePath;
                SavWav.Save(savePath, _rawEditClip);
                
                EditorUtility.DisplayDialog("Export Successful", 
                    $"Raw edited audio exported to:\n{savePath}", "OK");
                
                EditorUtility.RevealInFinder(savePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error exporting raw edited audio: {ex.Message}");
                EditorUtility.DisplayDialog("Export Error", 
                    $"Failed to export the raw edited audio: {ex.Message}", "OK");
            }
        }

        private void SaveSelection()
        {
            if (!_hasSelection || _rawAudioData == null || _rawEditClip == null) return;

            string defaultName = $"{_rawEditClip.name}_selection.wav";
            string initialDir = "";
            
            if (!string.IsNullOrEmpty(_lastSavePath) && Directory.Exists(Path.GetDirectoryName(_lastSavePath)))
            {
                initialDir = Path.GetDirectoryName(_lastSavePath);
            }
            else 
            {
                initialDir = Path.Combine(Application.dataPath, "Sounds");
                if (!Directory.Exists(initialDir))
                {
                    initialDir = Application.dataPath;
                }
            }
            
            string savePath = EditorUtility.SaveFilePanel("Save Selected Audio", 
                initialDir, defaultName, "wav");
                
            if (string.IsNullOrEmpty(savePath)) return;
            
            try
            {
                // Extract selected audio data
                int totalSamples = _rawAudioData.Length / _rawEditClip.channels;
                int startSample = Mathf.FloorToInt(_selectionStart * totalSamples);
                int endSample = Mathf.FloorToInt(_selectionEnd * totalSamples);
                
                // Ensure proper order
                if (startSample > endSample)
                {
                    int temp = startSample;
                    startSample = endSample;
                    endSample = temp;
                }
                
                int selectionLength = endSample - startSample;

                if (selectionLength <= 0) return;

                float[] selectionData = new float[selectionLength * _rawEditClip.channels];
                
                for (int i = 0; i < selectionLength; i++)
                {
                    for (int c = 0; c < _rawEditClip.channels; c++)
                    {
                        int sourceIndex = (startSample + i) * _rawEditClip.channels + c;
                        int destIndex = i * _rawEditClip.channels + c;
                        
                        if (sourceIndex < _rawAudioData.Length)
                        {
                            selectionData[destIndex] = _rawAudioData[sourceIndex];
                        }
                    }
                }

                // Create a temporary AudioClip for the selection
                AudioClip selectionClip = AudioClip.Create(
                    _rawEditClip.name + "_selection",
                    selectionLength,
                    _rawEditClip.channels,
                    _rawEditClip.frequency,
                    false
                );

                selectionClip.SetData(selectionData, 0);

                string saveDir = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                
                _lastSavePath = savePath;
                SavWav.Save(savePath, selectionClip);
                
                float selectionDuration = (float)selectionLength / _rawEditClip.frequency;
                float startTime = _selectionStart * _rawEditClip.length;
                float endTime = _selectionEnd * _rawEditClip.length;
                
                EditorUtility.DisplayDialog("Selection Saved", $"Selected audio ({selectionDuration:F2}s) from {startTime:F3}s to {endTime:F3}s saved to:\n{savePath}", "OK");
                
                EditorUtility.RevealInFinder(savePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving selection: {ex.Message}");
                EditorUtility.DisplayDialog("Save Error", 
                    $"Failed to save the selected audio: {ex.Message}", "OK");
            }
        }
        
        private void UpdateUIDueToClipLengthChange()
        {
            if (_rawEditClip == null) 
            {
                if (_rawTimelineSlider != null)
                {
                    _rawTimelineSlider.highValue = 1f; 
                    _rawTimelineSlider.value = 0f;
                }
                UpdateLabels(); 
                UpdateTimeline(); 
                UpdateRawWaveformDisplay(); 
                return;
            }
 
            _cursorPosition = Mathf.Clamp01(_cursorPosition);
            if (_rawTimelineSlider != null)
            {
                _rawTimelineSlider.SetValueWithoutNotify(_cursorPosition);
            }
 
            _selectionStart = Mathf.Clamp01(_selectionStart);
            _selectionEnd = Mathf.Clamp01(_selectionEnd);
 
            if (!_hasSelection) {
                _selectionStart = _cursorPosition;
                _selectionEnd = _cursorPosition;
            }

            UpdateLabels();  
            UpdateTimeline();  
            UpdateRawWaveformDisplay(); 
        }

        #endregion

        #region Raw Audio Playback
        private async void PlayRawAudio()
        {
            if (_rawEditClip == null || _rawAudioData == null || _rawAudioData.Length == 0)
            {
                Debug.LogWarning("PlayRawAudio: No raw edit clip or audio data to play.");
                if (_rawIsPlaying || _rawIsPaused)
                {
                    StopRawPlayback(); 
                }
                else
                {
                    UpdateRawEditButtonStates(); 
                }
                return;
            }
            if (_rawIsPaused)
            {
                ResumeRawAudio();
                return;
            }

            AudioPreviewUtility.StopAllClips();

            if (_rawPlaybackInstanceClip != null && _rawPlaybackInstanceClip != _rawEditClip)
            {
                UnityEngine.Object.DestroyImmediate(_rawPlaybackInstanceClip);
            }
            if (!string.IsNullOrEmpty(_rawPlaybackInstanceTempPath))
            {
                try { if (File.Exists(_rawPlaybackInstanceTempPath)) File.Delete(_rawPlaybackInstanceTempPath); }
                catch (Exception ex) { Debug.LogWarning($"Old temp file delete error: {ex.Message}"); }
            }
            _rawPlaybackInstanceClip = null;
            _rawPlaybackInstanceTempPath = null;

            if (_rawEditClip == null || _rawAudioData == null || _rawAudioData.Length == 0)
            {
                Debug.LogWarning("PlayRawAudio: No raw edit clip or audio data to play.");
                StopRawPlayback();
                return;
            }


            _rawIsPlaying = true;
            _rawIsPaused = false;

            // Determine playback segment for this play instance
            if (_hasSelection && _selectionEnd > _selectionStart)
            {
                _rawPlaybackSegmentStartTimeInClip = _selectionStart * _rawEditClip.length;
                _rawPlaybackSegmentEndTimeInClip = _selectionEnd * _rawEditClip.length;
            }
            else if (_zoomFactor > 1.05f)
            {
                _rawPlaybackSegmentStartTimeInClip = _visibleTimeStart * _rawEditClip.length;
                _rawPlaybackSegmentEndTimeInClip = _visibleTimeEnd * _rawEditClip.length;
            }
            else
            {
                _rawPlaybackSegmentStartTimeInClip = _cursorPosition * _rawEditClip.length;
                _rawPlaybackSegmentEndTimeInClip = _rawEditClip.length;
            }

            if (_rawPlaybackSegmentStartTimeInClip >= _rawPlaybackSegmentEndTimeInClip - 0.001f)
            {
                Debug.LogWarning($"Raw playback segment is invalid or too short. Start: {_rawPlaybackSegmentStartTimeInClip:F3}s, End: {_rawPlaybackSegmentEndTimeInClip:F3}s. Not playing.");
                StopRawPlayback();
                return;
            }
            _rawPlaybackSegmentStartTimeInClip = Mathf.Clamp(_rawPlaybackSegmentStartTimeInClip, 0, _rawEditClip.length);
            _rawPlaybackSegmentEndTimeInClip = Mathf.Clamp(_rawPlaybackSegmentEndTimeInClip, 0, _rawEditClip.length);

            _rawPlaybackTime = _rawPlaybackSegmentStartTimeInClip;
            _rawPlaybackStartTime = (float)EditorApplication.timeSinceStartup - _rawPlaybackTime;

            // --- Temporary file logic ---
            _rawPlaybackInstanceClip = _rawEditClip; 

            bool needsTempFile = _isRawClipDirtyForPlayback || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_rawEditClip));

            if (needsTempFile && _rawEditClip != null)
            {
                //Debug.Log("PlayRawAudio: Using temp file for playback of modified/in-memory clip.");
                string tempFolder = Path.Combine(Path.GetTempPath(), "UAISoundProcessorRaw");
                Directory.CreateDirectory(tempFolder);
                string tempFilePath = Path.Combine(tempFolder, $"raw_preview_{DateTime.Now.Ticks}.wav");

                SavWav.Save(tempFilePath, _rawEditClip); // Save current state of _rawEditClip

                AudioClip loadedTempClip = null;
                UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFilePath, AudioType.WAV);
                var asyncOp = www.SendWebRequest();
                while (!asyncOp.isDone) { await Task.Delay(10); }

                if (www.result == UnityWebRequest.Result.Success)
                {
                    loadedTempClip = DownloadHandlerAudioClip.GetContent(www);
                    if (loadedTempClip != null) loadedTempClip.name = _rawEditClip.name + "_playback_instance";
                }
                else { Debug.LogError($"Error loading temp raw audio clip: {www.error}"); }
                www.Dispose(); 

                if (loadedTempClip != null)
                {
                    _rawPlaybackInstanceClip = loadedTempClip;
                    _rawPlaybackInstanceTempPath = tempFilePath; 
                    _isRawClipDirtyForPlayback = false; 
                }
                else
                {
                    Debug.LogWarning("Temporary raw clip creation/loading failed. Playback might use stale or unplayable clip.");
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    { 
                        try { File.Delete(tempFilePath); } catch { }
                    }
                }
            }
            else if (_rawEditClip != null)
            {
                //Debug.Log("PlayRawAudio: Playing existing _rawEditClip directly (assumed to be an unmodified asset or already handled).");
                _rawPlaybackInstanceClip = _rawEditClip; 
            }


            if (_rawPlaybackInstanceClip == null)
            {
                Debug.LogError("PlayRawAudio: _rawPlaybackInstanceClip is null. Cannot play.");
                StopRawPlayback();
                return;
            }

            try
            {
                int startSample = Mathf.FloorToInt(_rawPlaybackTime * _rawPlaybackInstanceClip.frequency);
                if (startSample >= _rawPlaybackInstanceClip.samples || startSample < 0)
                {
                    Debug.LogWarning($"Start sample ({startSample}) is out of bounds for clip with {_rawPlaybackInstanceClip.samples} samples. Clip Time: {_rawPlaybackTime:F3}s. Not playing.");
                    StopRawPlayback();
                    return;
                }

                //Debug.Log($"AudioPreviewUtility.PlayClip starting instance: {_rawPlaybackInstanceClip.name} at sample: {startSample} (Time: {_rawPlaybackTime:F3}s)");
                AudioPreviewUtility.PlayClip(_rawPlaybackInstanceClip, startSample, false);

                if (_rawPlayButton != null) _rawPlayButton.SetEnabled(false);
                if (_rawPauseButton != null) _rawPauseButton.SetEnabled(true);
                if (_rawStopButton != null) _rawStopButton.SetEnabled(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error playing raw audio: {ex.Message}\n{ex.StackTrace}");
                StopRawPlayback();
            }
        }

        private void PauseRawAudio()
        {
            if (!_rawIsPlaying || _rawIsPaused || _rawPlaybackInstanceClip == null) return; // Check _rawPlaybackInstanceClip

            AudioPreviewUtility.StopAllClips();
            _rawIsPaused = true;
            
            //Debug.Log($"Raw audio paused at: {_rawPlaybackTime:F3}s");

            if (_rawPlayButton != null) _rawPlayButton.SetEnabled(true);
            if (_rawPauseButton != null) _rawPauseButton.SetEnabled(false);
            if (_rawStopButton != null) _rawStopButton.SetEnabled(true);
        }

        private void ResumeRawAudio()
        {
            if (!_rawIsPaused || _rawPlaybackInstanceClip == null) return; 

            _rawIsPlaying = true;
            _rawIsPaused = false;

            int startSample = Mathf.FloorToInt(_rawPlaybackTime * _rawPlaybackInstanceClip.frequency);

            if (startSample >= _rawPlaybackInstanceClip.samples)
            {
                Debug.LogWarning($"Resume sample ({startSample}) is at or beyond clip end ({_rawPlaybackInstanceClip.samples}). Not resuming.");
                StopRawPlayback();
                return;
            }
            
            _rawPlaybackStartTime = (float)EditorApplication.timeSinceStartup - _rawPlaybackTime;

            //Debug.Log($"Resuming raw audio from sample: {startSample} (Time: {_rawPlaybackTime:F3}s)");
            AudioPreviewUtility.PlayClip(_rawPlaybackInstanceClip, startSample, false); 

            if (_rawPlayButton != null) _rawPlayButton.SetEnabled(false);
            if (_rawPauseButton != null) _rawPauseButton.SetEnabled(true);
            if (_rawStopButton != null) _rawStopButton.SetEnabled(true);
        }

        private void StopRawAudio()
        {
            bool wasPlayingOrPaused = _rawIsPlaying || _rawIsPaused;
            float lastPlaybackTimeNormalized = _rawPlaybackInstanceClip  != null && _rawPlaybackInstanceClip .length > 0 ? Mathf.Clamp01(_rawPlaybackTime / _rawPlaybackInstanceClip .length) : 0f;

            StopRawPlayback(); 

            if (wasPlayingOrPaused && _rawPlaybackInstanceClip  != null)
            {
                _cursorPosition = lastPlaybackTimeNormalized;

                if (_rawPlaybackTime >= _rawPlaybackSegmentEndTimeInClip && (_rawLoopToggle == null || !_rawLoopToggle.value))
                {
                    _cursorPosition = (_rawPlaybackInstanceClip .length > 0) ? Mathf.Clamp01(_rawPlaybackSegmentEndTimeInClip / _rawPlaybackInstanceClip .length) : 0f;
                }


                if (_rawTimelineSlider != null)
                {
                    _rawTimelineSlider.SetValueWithoutNotify(_cursorPosition);
                }
                UpdateCursorLabel();
                UpdateRawWaveformDisplay(); 
                //Debug.Log($"Raw audio stopped. Cursor at: {_cursorPosition * _rawPlaybackInstanceClip .length:F3}s");
            }
            else if (_rawPlaybackInstanceClip  == null && _rawTimelineSlider != null)
            {
                _cursorPosition = 0f;
                _rawTimelineSlider.SetValueWithoutNotify(0f);
                UpdateCursorLabel();
                UpdateRawWaveformDisplay();
            }
        }

        private void StopRawPlayback() 
        {
            if (_rawIsPlaying || _rawIsPaused) 
            {
                AudioPreviewUtility.StopAllClips();
            }
            _rawIsPlaying = false;
            _rawIsPaused = false;

            if (_rawPlaybackInstanceClip != null && _rawPlaybackInstanceClip != _rawEditClip) 
            {
                UnityEngine.Object.DestroyImmediate(_rawPlaybackInstanceClip);
            }
            _rawPlaybackInstanceClip = null; 

            if (!string.IsNullOrEmpty(_rawPlaybackInstanceTempPath))
            {
                try
                {
                    if (File.Exists(_rawPlaybackInstanceTempPath))
                    {
                        File.Delete(_rawPlaybackInstanceTempPath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not delete temp raw audio file {_rawPlaybackInstanceTempPath}: {ex.Message}");
                }
                _rawPlaybackInstanceTempPath = null;
            }
            _isRawClipDirtyForPlayback = true; 

            if (_rawPlayButton != null)
                _rawPlayButton.SetEnabled(_rawEditClip != null && _rawAudioData != null && _rawAudioData.Length > 0);
            if (_rawPauseButton != null)
                _rawPauseButton.SetEnabled(false);
            if (_rawStopButton != null)
                _rawStopButton.SetEnabled(false);
        }

        private void UpdateRawPlayback()
        {
            if (!_rawIsPlaying || _rawIsPaused || _rawPlaybackInstanceClip == null || _rawPlaybackInstanceClip.length <= 0) return;

            _rawPlaybackTime = (float)EditorApplication.timeSinceStartup - _rawPlaybackStartTime;

            if (_rawPlaybackTime >= _rawPlaybackSegmentEndTimeInClip)
            {
                if (_rawLoopToggle != null && _rawLoopToggle.value)
                {
                    float segmentDuration = _rawPlaybackSegmentEndTimeInClip - _rawPlaybackSegmentStartTimeInClip;
                    if (segmentDuration <= 0.001f) 
                    {
                        StopRawAudio(); 
                        return;
                    }

                    float overshoot = _rawPlaybackTime - _rawPlaybackSegmentEndTimeInClip;
                    _rawPlaybackTime = _rawPlaybackSegmentStartTimeInClip + (overshoot % segmentDuration);
                    _rawPlaybackStartTime = (float)EditorApplication.timeSinceStartup - _rawPlaybackTime;

                    int startSample = Mathf.FloorToInt(_rawPlaybackSegmentStartTimeInClip * _rawPlaybackInstanceClip.frequency);
                    AudioPreviewUtility.PlayClip(_rawPlaybackInstanceClip, startSample, false); 
                    //Debug.Log($"Raw audio loop. Restarting segment at {_rawPlaybackSegmentStartTimeInClip:F3}s. Current time {_rawPlaybackTime:F3}s");
                }
                else
                {
                    _rawPlaybackTime = Mathf.Min(_rawPlaybackTime, _rawPlaybackSegmentEndTimeInClip); 
                    //Debug.Log($"Raw playback finished at segment end: {_rawPlaybackTime:F3}s");
                    StopRawAudio();
                    _cursorPosition = (_rawEditClip != null && _rawEditClip.length > 0) ? Mathf.Clamp01(_rawPlaybackSegmentEndTimeInClip / _rawEditClip.length) : 0f;
                    if (_rawTimelineSlider != null) _rawTimelineSlider.SetValueWithoutNotify(_cursorPosition);
                    UpdateCursorLabel();
                    UpdateRawWaveformDisplay();
                    return;
                }
            }
            
            _rawPlaybackTime = Mathf.Clamp(_rawPlaybackTime, 0, _rawPlaybackInstanceClip.length);

            _cursorPosition = (_rawEditClip != null && _rawEditClip.length > 0) ? 
                            Mathf.Clamp01(_rawPlaybackTime / _rawEditClip.length) : 0f;

            if (_rawTimelineSlider != null)
                _rawTimelineSlider.SetValueWithoutNotify(_cursorPosition);

            UpdateCursorLabel();
            UpdateRawWaveformDisplay(); 
        }


        #endregion

        #region Audio Processing (Existing Methods)
        private async void ProcessClip()
        {
            if (_sourceClip == null)
            {
                EditorUtility.DisplayDialog("No Clip Selected", "Please select a source audio clip to process.", "OK");
                return;
            }

            bool normalize = _normalizeToggle.value;
            bool trimSilence = _trimSilenceToggle.value;
            bool applyFades = _applyFadesToggle.value;
            bool makeLoopable = _makeLoopableToggle.value;
            float pitchShift = _pitchSlider.value;
            float filterCutoff = _filterSlider.value;
            float loopCrossfadeDuration = _loopableCrossfadeSlider != null ? _loopableCrossfadeSlider.value : 0.1f;

            _isProcessing = true;
            ShowLoadingOverlay();

            await Task.Delay(1);

            try
            {
                AudioClip resultClip = _sourceClip;
                string processingSteps = "";

                if (trimSilence)
                {
                    resultClip = SoundProcessor.TrimSilence(resultClip);
                    processingSteps += "• Trimmed silence\n";
                }

                if (normalize)
                {
                    resultClip = SoundProcessor.NormalizeAudio(resultClip);
                    processingSteps += "• Normalized volume\n";
                }

                if (applyFades)
                {
                    resultClip = SoundProcessor.ApplyFades(resultClip);
                    processingSteps += "• Applied fades\n";
                }

                if (Mathf.Abs(pitchShift - 1.0f) > 0.01f)
                {
                    resultClip = SoundProcessor.ChangePitch(resultClip, pitchShift);
                    processingSteps += $"• Shifted pitch by {pitchShift:F2}x\n";
                }

                if (filterCutoff < 0.99f)
                {
                    resultClip = SoundProcessor.ApplyLowPassFilter(resultClip, filterCutoff);
                    processingSteps += $"• Applied low-pass filter ({filterCutoff:F2})\n";
                }

                if (makeLoopable)
                {
                    resultClip = SoundProcessor.CreateSeamlessLoop(resultClip, loopCrossfadeDuration);
                    processingSteps += $"• Made loopable (crossfade: {loopCrossfadeDuration:F2}s)\n";
                }

                _processedClip = resultClip;

                //Debug.Log($"Audio processing complete. Result clip: {_processedClip.name}, duration: {_processedClip.length:F2}s");

                _resultLabel.text = "Processing complete!\nApplied effects:\n" + processingSteps;
                _clipNameLabel.text = $"Clip Name: {_processedClip.name} (Processed)";
                _clipLengthLabel.text = $"Length: {_processedClip.length:F2} seconds";
                _clipChannelsLabel.text = $"Channels: {_processedClip.channels}";
                _clipFrequencyLabel.text = $"Frequency: {_processedClip.frequency} Hz";

                UpdateWaveformDisplay(_processedClip);
                UpdateDurationLabel(_processedClip.length);

                _playButton.SetEnabled(true);
                _pauseButton.SetEnabled(false);
                _stopButton.SetEnabled(false);
                _exportButton.SetEnabled(true);
                _timelineSlider.value = 0;
                _playbackTime = 0;

                await Task.Delay(500);

                if (EditorUtility.DisplayDialog("Processing Complete",
                    "Audio processing completed successfully. Would you like to play the processed audio?",
                    "Play", "Not Now"))
                {
                    PlayAudio();
                }
            }
            catch (Exception ex)
            {
                _resultLabel.text = $"Error during processing: {ex.Message}";
                Debug.LogError($"Error processing clip: {ex.Message}\n{ex.StackTrace}");

                EditorUtility.DisplayDialog("Processing Error",
                    $"An error occurred while processing the clip:\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isProcessing = false;
                HideLoadingOverlay();
            }
        }

        private async void ProcessMultipleClips()
        {
            if (_selectedClips.Count == 0 || _selectedClips.All(c => c == null))
            {
                EditorUtility.DisplayDialog("No Clips Selected", "Please add audio clips to process.", "OK");
                return;
            }
            
            string outputFolder = _outputFolderField.value;
            if (string.IsNullOrEmpty(outputFolder))
            {
                EditorUtility.DisplayDialog("No Output Folder", "Please specify an output folder.", "OK");
                return;
            }
            
            if (!Directory.Exists(outputFolder) && outputFolder.StartsWith("Assets"))
            {
                Directory.CreateDirectory(outputFolder);
                AssetDatabase.Refresh();
            }
            
            _lastBatchFolder = outputFolder;
            
            bool normalize = _batchNormalizeToggle.value;
            bool trimSilence = _batchTrimSilenceToggle.value;
            bool applyFades = _batchApplyFadesToggle.value;
            bool makeLoopable = _batchMakeLoopableToggle.value;
            float pitchShift = _batchPitchSlider.value;
            float filterCutoff = _batchFilterSlider.value;
            float batchLoopCrossfadeDuration = _batchLoopableCrossfadeSlider != null ? _batchLoopableCrossfadeSlider.value : 0.1f;
            
            _isProcessing = true;
            ShowLoadingOverlay();
            
            await Task.Delay(1);
            
            try
            {
                List<CreatedSoundEntry> entries = new List<CreatedSoundEntry>();
                foreach (var clip in _selectedClips.Where(c => c != null))
                {
                    var entry = new CreatedSoundEntry
                    {
                        AudioClip = clip,
                        Prompt = clip.name,
                        GenerationType = "SFX",
                        CreationDate = DateTime.Now,
                        AssetPath = AssetDatabase.GetAssetPath(clip),
                        IsInTrash = false
                    };
                    
                    entries.Add(entry);
                }
                
                List<CreatedSoundEntry> processedEntries = SoundProcessor.BatchProcess(
                    entries,
                    normalize,
                    trimSilence,
                    applyFades,
                    makeLoopable,
                    pitchShift,
                    outputFolder,
                    batchLoopCrossfadeDuration
                );
                
                if (filterCutoff < 0.99f)
                {
                    for (int i = 0; i < processedEntries.Count; i++)
                    {
                        if (processedEntries[i].AudioClip != null)
                        {
                            AudioClip filteredClip = SoundProcessor.ApplyLowPassFilter(processedEntries[i].AudioClip, filterCutoff);
                            
                            string filePath = processedEntries[i].AssetPath;
                            if (filePath.EndsWith(".wav"))
                            {
                                string baseFilePath = filePath.Substring(0, filePath.Length - 4);
                                string filteredPath = baseFilePath + "_filtered.wav";
                                
                                SavWav.Save(filteredPath, filteredClip);
                                AssetDatabase.Refresh();
                                
                                if (filteredPath.StartsWith("Assets"))
                                {
                                    filteredClip = AssetDatabase.LoadAssetAtPath<AudioClip>(filteredPath);
                                }
                                
                                processedEntries[i].AudioClip = filteredClip;
                                processedEntries[i].AssetPath = filteredPath;
                            }
                        }
                    }
                }
                
                SoundProcessor.OptimizeAudioImportSettings(processedEntries);
                
                int successCount = processedEntries.Count;
                EditorUtility.DisplayDialog("Batch Processing Complete", $"Successfully processed {successCount} out of {entries.Count} clips.\nSaved to: {outputFolder}", "OK");
                    
                if (outputFolder.StartsWith("Assets"))
                {
                    var folderObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputFolder);
                    if (folderObject != null)
                    {
                        EditorGUIUtility.PingObject(folderObject);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during batch processing: {ex.Message}\n{ex.StackTrace}");
                
                EditorUtility.DisplayDialog("Processing Error", $"An error occurred during batch processing:\n{ex.Message}", "OK");
            }
            finally
            {
                _isProcessing = false;
                HideLoadingOverlay();
            }
        }
         
        #endregion

        #region Audio Playback (Existing Methods)
        private void PlaySourceAudio()
        {
            if (_sourceClip == null) return;
            
            StopAudioPlayback();
            
            //Debug.Log($"Playing source audio: {_sourceClip.name}");
            
            AudioPreviewUtility.PlayClip(_sourceClip);
            
            _sourcePlayButton.SetEnabled(false);
            _sourceStopButton.SetEnabled(true);
        }
        
        private void StopSourceAudio()
        {
            AudioPreviewUtility.StopAllClips();
            
            _sourcePlayButton.SetEnabled(_sourceClip != null);
            _sourceStopButton.SetEnabled(false);
        }
        
        private void PlayAudio()
        {
            if (_processedClip == null) return;

            if (_isPaused)
            {
                ResumeAudio();
                return;
            }

            StopAudioPlayback();

            _isPlaying = true;
            _isPaused = false;
            _playbackTime = _timelineSlider.value * _processedClip.length;
            _playbackStartTime = (float)EditorApplication.timeSinceStartup - _playbackTime;

            //Debug.Log($"Attempting to play processed audio: {_processedClip.name} at position {_playbackTime:F2}s");

            try
            {
                bool needsTempFile = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_processedClip));

                if (needsTempFile)
                {
                    string tempFolder = Path.Combine(Path.GetTempPath(), "UAISoundProcessor");
                    Directory.CreateDirectory(tempFolder);
                    string tempFilePath = Path.Combine(tempFolder, $"temp_preview_{DateTime.Now.Ticks}.wav");
                    
                    SavWav.Save(tempFilePath, _processedClip);
                    
                    AudioClip savedClip = null; 
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFilePath, AudioType.WAV);
                    var asyncOp = www.SendWebRequest();
                    while (!asyncOp.isDone) { System.Threading.Thread.Sleep(10); }

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        savedClip = DownloadHandlerAudioClip.GetContent(www);
                        if (savedClip != null) savedClip.name = _processedClip.name + "_temp";
                    }
                    else
                    {
                        Debug.LogError($"Error loading temp audio clip: {www.error}");
                    } 

                    if (savedClip != null)
                    {
                        _currentlyPlayingPreviewClip = savedClip;
                        _tempPlaybackClip = savedClip;
                        //Debug.Log($"Playing saved clip from temporary file: {tempFilePath}, Name: {_currentlyPlayingPreviewClip.name}");
                    }
                    else
                    {
                        Debug.LogWarning("Temporary clip creation/loading failed, trying direct playback of processed clip. This might not work for non-asset clips.");
                        _currentlyPlayingPreviewClip = _processedClip;
                    }
                }
                else
                {
                    _currentlyPlayingPreviewClip = _processedClip;
                    //Debug.Log($"Playing asset-based processed clip: {_currentlyPlayingPreviewClip.name}");
                }

                if (_currentlyPlayingPreviewClip != null)
                {
                    _clipLength = _currentlyPlayingPreviewClip.length;
                    int startSample = Mathf.FloorToInt(_playbackTime * _currentlyPlayingPreviewClip.frequency);
                    AudioPreviewUtility.PlayClip(_currentlyPlayingPreviewClip, startSample, _loopToggle.value);
                    _playButton.SetEnabled(false);
                    _pauseButton.SetEnabled(true);
                    _stopButton.SetEnabled(true);
                }
                else
                {
                    Debug.LogError("Could not obtain a valid AudioClip to play.");
                    StopAudioPlayback();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error playing processed clip: {ex.ToString()}");
                StopAudioPlayback();
                EditorUtility.DisplayDialog("Playback Error", 
                    "There was an error playing the processed audio. Check the console for details.",
                    "OK");
            }
        }

        private void PauseAudio()
        {
            if (!_isPlaying || _isPaused || _currentlyPlayingPreviewClip == null) return;

            //Debug.Log("PauseAudio: Calling AudioPreviewUtility.StopAllClips() to simulate pause.");
            AudioPreviewUtility.StopAllClips();
            _isPaused = true;
            
            _playButton.SetEnabled(true);
            _pauseButton.SetEnabled(false);
            _stopButton.SetEnabled(true);
        }

        private void ResumeAudio()
        {
            if (!_isPaused || _currentlyPlayingPreviewClip == null)
            {
                _isPaused = false;
                _playButton.SetEnabled(_currentlyPlayingPreviewClip != null);
                _pauseButton.SetEnabled(false);
                return;
            }

            _isPaused = false;
            _playbackStartTime = (float)EditorApplication.timeSinceStartup - _playbackTime;
            
            //Debug.Log($"Resuming playback of: {_currentlyPlayingPreviewClip.name} from position {_playbackTime:F2}s");
            int startSample = Mathf.FloorToInt(_playbackTime * _currentlyPlayingPreviewClip.frequency);
            AudioPreviewUtility.PlayClip(_currentlyPlayingPreviewClip, startSample, _loopToggle.value);

            _playButton.SetEnabled(false);
            _pauseButton.SetEnabled(true);
            _stopButton.SetEnabled(true);
        }

        private void StopAudio()
        {
            if (_processedClip == null) return;
            
            StopAudioPlayback();
            
            _timelineSlider.value = 0;
            _playbackTime = 0;
            UpdatePlaybackTimeDisplay();
            
            if (_playheadIndicator != null)
            {
                _playheadIndicator.style.left = 0;
            }
        }
        
        private void StopAudioPlayback()
        {
            if (_isPlaying || _isPaused)
            {
                //Debug.Log("StopAudioPlayback: Calling AudioPreviewUtility.StopAllClips()");
                AudioPreviewUtility.StopAllClips();
                _isPlaying = false;
                _isPaused = false;
            }
            
            _currentlyPlayingPreviewClip = null; 
            if (_tempPlaybackClip != null)
            {
                _tempPlaybackClip = null;
            }
            
            if (_playButton != null)
                _playButton.SetEnabled(_processedClip != null);
            if (_pauseButton != null)
                _pauseButton.SetEnabled(false);
            if (_stopButton != null)
                _stopButton.SetEnabled(false);

            if (_sourcePlayButton != null)
                _sourcePlayButton.SetEnabled(_sourceClip != null);
            if (_sourceStopButton != null)
                _sourceStopButton.SetEnabled(false);
        }

        private void UpdatePlayback()
        {
            if (!_isPlaying || _isPaused || _currentlyPlayingPreviewClip == null)
            {
                if(_isPlaying && _currentlyPlayingPreviewClip == null) StopAudioPlayback();
                return;
            }

            _playbackTime = (float)EditorApplication.timeSinceStartup - _playbackStartTime;

            if (_clipLength <= 0) {
                StopAudioPlayback();
                return;
            }
            
            bool hasFinished = _playbackTime >= _clipLength;

            if (_loopToggle.value)
            {
                if (hasFinished)
                {
                    _playbackTime = _playbackTime % _clipLength;
                    _playbackStartTime = (float)EditorApplication.timeSinceStartup - _playbackTime;
                }
            }
            else
            {
                if (hasFinished)
                {
                    //Debug.Log($"Clip '{_currentlyPlayingPreviewClip.name}' finished playing (timer based).");
                    StopAudioPlayback(); 
                    _timelineSlider.value = 1.0f;
                    _playbackTime = _clipLength;
                    if (_playheadIndicator != null && _waveformDisplay.resolvedStyle.width > 0)
                    {
                        _playheadIndicator.style.left = _waveformDisplay.resolvedStyle.width;
                    }
                    UpdatePlaybackTimeDisplay();
                    return;
                }
            }

            float normalizedTime = Mathf.Clamp01(_playbackTime / _clipLength);
            _timelineSlider.SetValueWithoutNotify(normalizedTime);
            
            if (_playheadIndicator != null && _waveformDisplay.resolvedStyle.width > 0)
            {
                _playheadIndicator.style.left = normalizedTime * _waveformDisplay.resolvedStyle.width;
            }
            
            UpdatePlaybackTimeDisplay();
        }

        private void UpdatePlaybackTimeDisplay()
        {
            if (_processedClip == null) return;
            
            float currentTime = _playbackTime;
            float totalTime = _processedClip.length;
            
            int currentMinutes = Mathf.FloorToInt(currentTime / 60f);
            int currentSeconds = Mathf.FloorToInt(currentTime % 60f);
            int totalMinutes = Mathf.FloorToInt(totalTime / 60f);
            int totalSeconds = Mathf.FloorToInt(totalTime % 60f);
            
            _durationLabel.text = string.Format("{0:00}:{1:00} / {2:00}:{3:00}", 
                currentMinutes, currentSeconds, totalMinutes, totalSeconds);
        }
        
        private void UpdateDurationLabel(float duration)
        {
            int minutes = Mathf.FloorToInt(duration / 60f);
            int seconds = Mathf.FloorToInt(duration % 60f);
            _durationLabel.text = string.Format("00:00 / {0:00}:{1:00}", minutes, seconds);
        }
        #endregion

        #region Waveform Display (Existing Methods)
        private void UpdateWaveformDisplay(AudioClip clip)
        {
            if (clip == null) return;
            
            _waveformDisplay.Clear();
            
            string assetPath = AssetDatabase.GetAssetPath(clip);
            string fullPath = assetPath;
            
            if (assetPath.StartsWith("Assets"))
            {
                fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7));
            }
            
            float[] waveformData = WaveformGenerator.GenerateWaveformData(fullPath, 60);
            
            var waveformContainer = new VisualElement();
            waveformContainer.AddToClassList("waveform-container");
            _waveformDisplay.Add(waveformContainer);
            
            float spacing = _waveformDisplay.resolvedStyle.width / waveformData.Length;
            
            for (int i = 0; i < waveformData.Length; i++)
            {
                var bar = new VisualElement();
                bar.AddToClassList("waveform-bar");
                
                float barWidth = Mathf.Max(1, spacing - 1);
                float barHeight = waveformData[i] * _waveformDisplay.resolvedStyle.height * 0.8f;
                
                bar.style.width = barWidth;
                bar.style.height = barHeight;
                bar.style.marginTop = (_waveformDisplay.resolvedStyle.height - barHeight) / 2;
                
                waveformContainer.Add(bar);
            }
            
            var playhead = new VisualElement();
            playhead.AddToClassList("playhead-indicator");
            playhead.style.position = Position.Absolute;
            playhead.style.width = 2;
            playhead.style.backgroundColor = new Color(1f, 1f, 1f, 0.8f);
            playhead.style.height = _waveformDisplay.resolvedStyle.height;
            playhead.style.left = 0;
            
            waveformContainer.Add(playhead);
            
            _playheadIndicator = playhead;
        }
        #endregion

        #region UI Utilities (Existing Methods)
        private void ShowLoadingOverlay()
        {
            if (_loadingOverlay != null)
            {
                _loadingOverlay.style.display = DisplayStyle.Flex;
            }
        }
        
        private void HideLoadingOverlay()
        {
            if (_loadingOverlay != null)
            {
                _loadingOverlay.style.display = DisplayStyle.None;
            }
        }
        
        private void UpdateSourceClipInfo()
        {
            if (_sourceClip == null)
            {
                _clipNameLabel.text = "Clip Name: Not selected";
                _clipLengthLabel.text = "Length: --";
                _clipChannelsLabel.text = "Channels: --";
                _clipFrequencyLabel.text = "Frequency: -- Hz";
                _resultLabel.text = "No processing performed yet.";
                
                _processedClip = null;
                _playButton.SetEnabled(false);
                _pauseButton.SetEnabled(false);
                _stopButton.SetEnabled(false);
                _exportButton.SetEnabled(false);
                _sourcePlayButton.SetEnabled(false);
                _sourceStopButton.SetEnabled(false);
                
                _waveformDisplay.Clear();
                return;
            }
            
            _clipNameLabel.text = $"Clip Name: {_sourceClip.name}";
            _clipLengthLabel.text = $"Length: {_sourceClip.length:F2} seconds";
            _clipChannelsLabel.text = $"Channels: {_sourceClip.channels}";
            _clipFrequencyLabel.text = $"Frequency: {_sourceClip.frequency} Hz";
            _resultLabel.text = "Ready to process.";
            
            UpdateDurationLabel(_sourceClip.length);
            
            _sourcePlayButton.SetEnabled(true);
        }
        
        private void ExportProcessedClip()
        {
            if (_processedClip == null) return;
            
            string defaultName = $"{_sourceClip.name}_processed.wav";
            string initialDir = "";
            
            if (!string.IsNullOrEmpty(_lastSavePath) && Directory.Exists(Path.GetDirectoryName(_lastSavePath)))
            {
                initialDir = Path.GetDirectoryName(_lastSavePath);
            }
            else 
            {
                initialDir = Path.Combine(Application.dataPath, "Sounds");
                
                if (!Directory.Exists(initialDir))
                {
                    initialDir = Application.dataPath;
                }
            }
            
            string savePath = EditorUtility.SaveFilePanel("Export Processed Clip", 
                initialDir, defaultName, "wav");
                
            if (string.IsNullOrEmpty(savePath)) return;
            
            try
            {
                string saveDir = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                
                _lastSavePath = savePath;
                
                SavWav.Save(savePath, _processedClip);
                
                _resultLabel.text = $"Exported processed audio to:\n{savePath}";
                
                EditorUtility.RevealInFinder(savePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error exporting audio: {ex.Message}");
                EditorUtility.DisplayDialog("Export Error", 
                    $"Failed to export the processed audio: {ex.Message}", "OK");
            }
        }
        
        private void AddFolderClips()
        {
            string folder = EditorUtility.OpenFolderPanel("Select Folder with Audio Clips", "Assets", "");
            if (string.IsNullOrEmpty(folder)) return;
            
            string relativePath = folder;
            if (folder.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + folder.Substring(Application.dataPath.Length);
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside your Unity project.", "OK");
                return;
            }
            
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { relativePath });
            List<AudioClip> clips = new List<AudioClip>();
            
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }
            
            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog("No Audio Clips Found", "No audio clips were found in the selected folder.", "OK");
                return;
            }
             
            _selectedClips.AddRange(clips);
            _sourceClipsListView.Rebuild(); 
            
            EditorUtility.DisplayDialog("Clips Added", $"Added {clips.Count} audio clips from the selected folder.", "OK");
        }
        
        private void ClearSourceClips()
        {
            _selectedClips.Clear();
            _sourceClipsListView.Rebuild();
        }
         
        private void BrowseForBatchOutputFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (string.IsNullOrEmpty(folder)) return;
            
            if (folder.StartsWith(Application.dataPath))
            {
                folder = "Assets" + folder.Substring(Application.dataPath.Length);
            }
            
            _outputFolderField.value = folder;
            _lastBatchFolder = folder;
        }
         
        #endregion

        #region Editor Update
        private void OnEditorUpdate()
        {
            if (_isProcessing)
            {
                _progressValue = Mathf.Repeat(_progressValue + (PROGRESS_BAR_ANIMATION_SPEED * 0.01f), 1f);
                
                if (_loadingOverlay != null)
                {
                    var progressFill = _loadingOverlay.Q<VisualElement>("progress-fill");
                    if (progressFill != null)
                    {
                        progressFill.style.width = new Length(_progressValue * 100, LengthUnit.Percent);
                    }
                }
                
                Repaint();
            }
            
            if (_isPlaying && !_isPaused)
            {
                UpdatePlayback();
                Repaint();
            }

            if (_rawIsPlaying && !_rawIsPaused)
            {
                UpdateRawPlayback();
                Repaint();
            }
        }
        #endregion 
    }
    
    #region Supporting Types
    public enum PanelTypeSP
    {
        Processing,
        BatchProcessing, 
        RawEdit
    }

    [Serializable]
    public class AudioEditAction
    {
        public float[] AudioData;
        public DateTime Timestamp;
    }
    
    public static class AudioPreviewUtility
    {
        private static Type audioUtilClass = null;

        private static Type GetAudioUtilClass()
        {
            if (audioUtilClass == null)
                audioUtilClass = Type.GetType("UnityEditor.AudioUtil, UnityEditor");

            if (audioUtilClass == null)
                Debug.LogError("Failed to get UnityEditor.AudioUtil type");

            return audioUtilClass;
        }

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            if (clip == null) return;
            
            //Debug.Log($"Playing clip: {clip.name}, duration: {clip.length:F2}s, loop: {loop}");
            
            Type auClass = GetAudioUtilClass();
            if (auClass == null) return;

            try
            {
                var method = auClass.GetMethod(
                    "PlayPreviewClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null,
                    new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                    null
                );

                if (method != null)
                    method.Invoke(null, new object[] { clip, startSample, loop });
                else
                {
                    var altMethod = auClass.GetMethod(
                        "PlayPreviewClip",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                        null,
                        new Type[] { typeof(AudioClip) },
                        null
                    );
                    
                    if (altMethod != null)
                        altMethod.Invoke(null, new object[] { clip });
                    else
                        Debug.LogError("PlayPreviewClip method not found");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error playing preview clip: {ex.Message}");
            }
        }

        public static void StopAllClips()
        {
            Type auClass = GetAudioUtilClass();
            if (auClass == null) return;

            try
            {
                var method = auClass.GetMethod(
                    "StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
                );

                if (method != null)
                    method.Invoke(null, null);
                else
                    Debug.LogError("StopAllPreviewClips method not found");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping preview clips: {ex.Message}");
            }
        }

        public static bool IsClipPlaying(AudioClip clip)
        {
            if (clip == null) return false;
            
            Type auClass = GetAudioUtilClass();
            if (auClass == null) return false;

            try
            {
                var method = auClass.GetMethod(
                    "IsPreviewClipPlaying",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null,
                    new Type[] { typeof(AudioClip) },
                    null
                );

                if (method != null)
                    return (bool)method.Invoke(null, new object[] { clip });
                else
                {
                    Debug.LogWarning("IsPreviewClipPlaying method not found - using timer-based playback tracking");
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    #endregion
}