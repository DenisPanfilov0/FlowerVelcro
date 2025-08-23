using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Text; 
using System.Net.Http;
using System.Net.Http.Headers; 
using System.Text.RegularExpressions;
using System.Collections;

using UnityEngine.Networking;

namespace UAI
{
    public static class AudioPreviewUtil
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
            Type auClass = GetAudioUtilClass();
            if (auClass == null) return;

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
                Debug.LogError("PlayPreviewClip method not found");
        }

        public static void StopAllClips()
        {
            Type auClass = GetAudioUtilClass();
            if (auClass == null) return;

            var method = auClass.GetMethod(
                "StopAllPreviewClips",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );

            if (method != null)
                method.Invoke(null, null);
            else
                Debug.LogError("StopAllPreviewClips method not found");
        }

        public static void PauseClip(AudioClip clip)
        {
            Type auClass = GetAudioUtilClass();
            if (auClass == null) return;

            var method = auClass.GetMethod(
                "PausePreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip) },
                null
            );

            if (method != null)
                method.Invoke(null, new object[] { clip });
            else
                Debug.LogError("PausePreviewClip method not found");
        }

        public static void ResumeClip(AudioClip clip)
        {
            Type auClass = GetAudioUtilClass();
            if (auClass == null) return;

            var method = auClass.GetMethod(
                "ResumePreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip) },
                null
            );

            if (method != null)
                method.Invoke(null, new object[] { clip });
            else
                Debug.LogError("ResumePreviewClip method not found");
        }

        public static bool IsClipPlaying(AudioClip clip)
        {
            Type auClass = GetAudioUtilClass();
            if (auClass == null) return false;

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
                Debug.LogError("IsPreviewClipPlaying method not found");

            return false;
        }
    }


    public class SoundCreatorWindow : EditorWindow
    {
        #region Constants
        private const string WINDOW_TITLE = "Sound Creator";
        private const string WINDOW_MENU_PATH = "Tools/uAI Creator/Sound Creator";

        private const string DEFAULT_SFX_PROMPT = "Spacious braam suitable for high-impact movie trailer moments";
        private const string DEFAULT_TTS_PROMPT = "Welcome to the game. This is a test of the text to speech system.";
        private const string PREFS_KEY_PREFIX = "UAI_SoundCreator_";
        private const string DEFAULT_SAVE_PATH = "Assets/AI Generated Sounds";
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.1f;
        private const string ELEVENLABS_API_URL = "https://api.elevenlabs.io/v1";
        #endregion

        #region UI Elements
        // Main layout elements
        private VisualElement _rootElement;
        private VisualElement _createPanel;
        private VisualElement _sfxPanel;
        private VisualElement _ttsPanel;
        private VisualElement _voiceChangerPanel;
        private VisualElement _creationsPanel;
        private VisualElement _trashPanel;
        private VisualElement _audioPreviewPanel;
        private VisualElement _settingsPanel;
        private VisualElement _loadingOverlay;
        private VisualElement _playheadIndicator;


        // Microphone recording elements
        private Button _recordVoiceButton;
        private Button _stopRecordingButton;
        private VisualElement _recordingUI;
        private Label _recordingTimeLabel;
        private VisualElement _recordingIndicator;

        // Create panel elements - Sound Effects
        private TextField _sfxPromptField;
        private Slider _durationSlider;
        private Label _durationValueLabel;
        private Slider _promptInfluenceSlider;
        private Label _influenceValueLabel;
        private DropdownField _qualityDropdown;
        private Button _generateSfxButton;

        // Create panel elements - Text to Speech
        private TextField _ttsPromptField;
        private DropdownField _voiceDropdown;
        private DropdownField _modelDropdown;
        private DropdownField _ttsQualityDropdown;
        private Toggle _textNormalizationToggle;
        private Button _generateTtsButton;

        // Create panel elements - Voice Changer
        private ObjectField _audioFileField;
        private DropdownField _targetVoiceDropdown;
        private DropdownField _vcModelDropdown;
        private DropdownField _vcQualityDropdown;
        private Toggle _noiseRemovalToggle;
        private Button _generateVcButton;

        // Tab buttons
        private Button _sfxTabButton;
        private Button _ttsTabButton;
        private Button _vcTabButton;

        // Creations panel elements
        private ScrollView _timelineScrollView;

        // Audio preview panel elements
        private Label _promptInfoLabel;
        private Label _typeInfoLabel;
        private Label _dateInfoLabel;
        private Label _durationLabel;
        private Button _playButton;
        private Button _stopButton;
        private Slider _timelineSlider;
        private VisualElement _waveformDisplay;

        // Settings panel elements
        private TextField _apiKeyField;
        private Label _voicesInfoLabel;
        private TextField _savePathField;
        private TextField _externalPathField;
        private Toggle _useExternalPathToggle;
        private VisualElement _externalPathContainer;
        private DropdownField _defaultModelDropdown;
        private DropdownField _defaultVoiceDropdown;
        private DropdownField _defaultQualityDropdown;
        #endregion

        #region Settings Variables
        // User configurable settings
        private string _apiKey = "";
        private string _savePath = DEFAULT_SAVE_PATH;
        private string _externalSavePath = "";
        private bool _useExternalPath = false;
        private string _defaultVoiceId = "21m00Tcm4TlvDq8ikWAM"; // Default voice (Rachel)
        private string _defaultTtsModel = "eleven_multilingual_v2";
        private string _defaultSfxModel = "eleven_sfx_v1";
        private string _defaultVcModel = "eleven_multilingual_sts_v2";
        private string _defaultQuality = "mp3_44100_128";
        private bool _removeBgNoise = false;
        private float _previewStartTime = 0f;
        private float _previewClipLength = 0f;
        #endregion
        #region Pagination Properties
        private Dictionary<DateTime, List<SoundFileMetadata>> _soundFileMetadata = new Dictionary<DateTime, List<SoundFileMetadata>>();
        private bool _hasMoreSoundsToLoad = false;
        private const int BATCH_SIZE = 10;
        #endregion

        #region State Variables
        // Selected panel state
        private SoundCreatorPanelType _currentPanel = SoundCreatorPanelType.Creations;
        private CreatePT _currentCreatePanel = CreatePT.SFX;

        // Audio generation state
        private string _currentSfxPrompt = DEFAULT_SFX_PROMPT;
        private string _currentTtsPrompt = DEFAULT_TTS_PROMPT;
        private float _duration = 3.0f;
        private float _promptInfluence = 0.3f;
        private string _selectedVoiceId = "";
        private string _selectedTtsModel = "";
        private string _selectedVcModel = "";
        private string _selectedQuality = "";
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        private AudioClip _selectedAudioFile = null;


        // Microphone recording state
        private bool _isRecording = false;
        private string _recordingDevice = null;
        private AudioClip _recordedClip = null;
        private float _recordingStartTime = 0f;
        private const int RECORDING_FREQUENCY = 44100;
        private const int MAX_RECORDING_SECONDS = 60;


        // Created sounds state
        private Dictionary<DateTime, List<CreatedSoundEntry>> _createdSounds = new Dictionary<DateTime, List<CreatedSoundEntry>>();
        private List<CreatedSoundEntry> _trashSounds = new List<CreatedSoundEntry>();
        private CreatedSoundEntry _selectedSound = null;

        // Audio playback state
        private AudioSource _previewAudioSource = null;
        private bool _isPlaying = false;

        // Available voices list
        private List<VoiceInfo> _availableVoices = new List<VoiceInfo>();
        #endregion

        #region Menu Item and Lifecycle Methods
        [MenuItem(WINDOW_MENU_PATH, false, 3)]
        public static void ShowWindow()
        {
            SoundCreatorWindow window = GetWindow<SoundCreatorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(1000, 650);
            window.Show();
        }

        private void OnEnable()
        {
            // Load settings
            SoundCreatorUtility.LoadSettings(PREFS_KEY_PREFIX, ref _apiKey, ref _savePath, ref _externalSavePath,
                ref _useExternalPath, ref _defaultVoiceId, ref _defaultTtsModel, ref _defaultVcModel,
                ref _defaultSfxModel, ref _defaultQuality, ref _removeBgNoise);

            // Initialize collections if needed
            if (_createdSounds == null)
                _createdSounds = new Dictionary<DateTime, List<CreatedSoundEntry>>();

            if (_trashSounds == null)
                _trashSounds = new List<CreatedSoundEntry>();

            // Set current generation settings from defaults
            _selectedVoiceId = _defaultVoiceId;
            _selectedTtsModel = _defaultTtsModel;
            _selectedVcModel = _defaultVcModel;
            _selectedQuality = _defaultQuality;

            // Load saved data
            InitializeSavedSounds();
            LoadTrashSounds();

            // Fetch available voices if we have an API key
            if (!string.IsNullOrEmpty(_apiKey))
            {
                FetchAvailableVoices();
            }

            // Set up update for progress animation
            EditorApplication.update += UpdateProgress;
        }




        private void InitializeSavedSounds()
        {
            // Initialize empty collections
            _createdSounds = new Dictionary<DateTime, List<CreatedSoundEntry>>();
            _soundFileMetadata = new Dictionary<DateTime, List<SoundFileMetadata>>();

            // Look for saved sounds in the project or external path
            string basePath = _useExternalPath ? _externalSavePath : _savePath;
            if (Directory.Exists(basePath))
            {
                // Scan all files without loading them
                _soundFileMetadata = SoundCreatorUtility.ScanSoundFiles(basePath, Application.dataPath);

                // Load the first batch
                bool isInProject = basePath.StartsWith("Assets") || basePath.StartsWith(Application.dataPath);
                SoundCreatorUtility.LoadSoundBatch(
                    _soundFileMetadata,
                    ref _createdSounds,
                    BATCH_SIZE,
                    isInProject,
                    Application.dataPath
                );

                // Check if there are more sounds to load
                _hasMoreSoundsToLoad = SoundCreatorUtility.HasMoreSoundsToLoad(_soundFileMetadata);
            }
        }
        private void LoadMoreSounds()
        {
            string basePath = _useExternalPath ? _externalSavePath : _savePath;
            bool isInProject = basePath.StartsWith("Assets") || basePath.StartsWith(Application.dataPath);

            SoundCreatorUtility.LoadSoundBatch(
                _soundFileMetadata,
                ref _createdSounds,
                BATCH_SIZE,
                isInProject,
                Application.dataPath
            );

            // Check if there are more sounds to load
            _hasMoreSoundsToLoad = SoundCreatorUtility.HasMoreSoundsToLoad(_soundFileMetadata);

            // Refresh the UI
            RefreshCreationsPanel();
        }



        private void LoadTrashSounds()
        {
            _trashSounds = SoundCreatorUtility.LoadTrashSounds(PREFS_KEY_PREFIX, ref _createdSounds);
        }

        private void OnDisable()
        {
            // Save trash sounds
            SoundCreatorUtility.SaveTrashSounds(PREFS_KEY_PREFIX, _trashSounds);

            // Remove update callback
            EditorApplication.update -= UpdateProgress;

            // Clean up resources
            CleanupResources();
        }

        public void CreateGUI()
        {
            _rootElement = rootVisualElement;

            var styleSheet = Resources.Load<StyleSheet>("SoundCreatorStyles");
            if (styleSheet != null)
            {
                _rootElement.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("Failed to load SoundCreatorStyles.uss");
            }

            // Load UXML
            var visualTree = Resources.Load<VisualTreeAsset>("SoundCreatorWindow");
            visualTree.CloneTree(_rootElement);

            // Get references to panels and UI elements
            InitializeUIReferences();

            // Set up event handlers
            SetupEventHandlers();

            // Initialize the first view
            SwitchPanel(_currentPanel);
            SwitchCreatePanel(_currentCreatePanel);

            // Check if API key is set
            if (string.IsNullOrEmpty(_apiKey))
            {
                ShowAPIKeyWarning();
            }

            // Populate the model dropdowns
            PopulateModelDropdowns();

            // Populate the voice dropdowns if we have voices available
            PopulateVoiceDropdowns();
        }

        private void InitializeUIReferences()
        {
            // Get panels
            _createPanel = _rootElement.Q<VisualElement>("create-panel");
            _sfxPanel = _rootElement.Q<VisualElement>("sfx-panel");
            _ttsPanel = _rootElement.Q<VisualElement>("tts-panel");
            _voiceChangerPanel = _rootElement.Q<VisualElement>("voice-changer-panel");
            _creationsPanel = _rootElement.Q<VisualElement>("creations-panel");
            _trashPanel = _rootElement.Q<VisualElement>("trash-panel");
            _audioPreviewPanel = _rootElement.Q<VisualElement>("audio-preview-panel");
            _settingsPanel = _rootElement.Q<VisualElement>("settings-panel");
            _loadingOverlay = _rootElement.Q<VisualElement>("loading-overlay-fullscreen");

            // Tab buttons
            _sfxTabButton = _rootElement.Q<Button>("sfx-tab-button");
            _ttsTabButton = _rootElement.Q<Button>("tts-tab-button");
            _vcTabButton = _rootElement.Q<Button>("vc-tab-button");

            // SFX panel elements
            _sfxPromptField = _rootElement.Q<TextField>("sfx-prompt-field");
            _sfxPromptField.value = _currentSfxPrompt;

            _durationSlider = _rootElement.Q<Slider>("duration-slider");
            _durationSlider.lowValue = 0.5f;
            _durationSlider.highValue = 22.0f;
            _durationSlider.value = _duration;

            _durationValueLabel = _rootElement.Q<Label>("duration-value");
            _durationValueLabel.text = $"{_duration:F1}s";

            _promptInfluenceSlider = _rootElement.Q<Slider>("influence-slider");
            _promptInfluenceSlider.lowValue = 0.0f;
            _promptInfluenceSlider.highValue = 1.0f;
            _promptInfluenceSlider.value = _promptInfluence;

            _influenceValueLabel = _rootElement.Q<Label>("influence-value");
            _influenceValueLabel.text = $"{_promptInfluence:F1}";

            _qualityDropdown = _rootElement.Q<DropdownField>("quality-dropdown");
            _generateSfxButton = _rootElement.Q<Button>("generate-sfx-button");

            // TTS panel elements
            _ttsPromptField = _rootElement.Q<TextField>("tts-prompt-field");
            _ttsPromptField.value = _currentTtsPrompt;

            _voiceDropdown = _rootElement.Q<DropdownField>("voice-dropdown");
            _modelDropdown = _rootElement.Q<DropdownField>("model-dropdown");
            _ttsQualityDropdown = _rootElement.Q<DropdownField>("tts-quality-dropdown");
            _textNormalizationToggle = _rootElement.Q<Toggle>("text-normalization-toggle");
            _generateTtsButton = _rootElement.Q<Button>("generate-tts-button");

            // Voice Changer panel elements
            _audioFileField = _rootElement.Q<ObjectField>("audio-file-field");
            _audioFileField.objectType = typeof(AudioClip);
            _audioFileField.allowSceneObjects = false;

            // Voice recording UI elements
            _recordVoiceButton = _rootElement.Q<Button>("record-voice-button");
            _stopRecordingButton = _rootElement.Q<Button>("stop-recording-button");
            _recordingUI = _rootElement.Q<VisualElement>("recording-ui");
            _recordingTimeLabel = _rootElement.Q<Label>("recording-time-label");
            _recordingIndicator = _rootElement.Q<VisualElement>("recording-indicator");

            // Hide recording UI initially
            _recordingUI.style.display = DisplayStyle.None;

            _targetVoiceDropdown = _rootElement.Q<DropdownField>("target-voice-dropdown");
            _vcModelDropdown = _rootElement.Q<DropdownField>("vc-model-dropdown");
            _vcQualityDropdown = _rootElement.Q<DropdownField>("vc-quality-dropdown");
            _noiseRemovalToggle = _rootElement.Q<Toggle>("noise-removal-toggle");
            _noiseRemovalToggle.value = _removeBgNoise;
            _generateVcButton = _rootElement.Q<Button>("generate-vc-button");

            // Creations panel elements
            _timelineScrollView = _rootElement.Q<ScrollView>("timeline-scroll-view");

            // Audio preview panel elements
            _promptInfoLabel = _rootElement.Q<Label>("prompt-info-label");
            _typeInfoLabel = _rootElement.Q<Label>("type-info-label");
            _dateInfoLabel = _rootElement.Q<Label>("date-info-label");
            _durationLabel = _rootElement.Q<Label>("duration-label");
            _playButton = _rootElement.Q<Button>("play-button");
            _stopButton = _rootElement.Q<Button>("stop-button");
            _timelineSlider = _rootElement.Q<Slider>("timeline-slider");
            _waveformDisplay = _rootElement.Q<VisualElement>("waveform-display");

            // Settings panel elements
            _apiKeyField = _rootElement.Q<TextField>("api-key-field");
            _apiKeyField.value = _apiKey;

            _voicesInfoLabel = _rootElement.Q<Label>("voices-info-label");

            _savePathField = _rootElement.Q<TextField>("save-path-field");
            _savePathField.value = _savePath;

            _externalPathField = _rootElement.Q<TextField>("external-path-field");
            _externalPathField.value = _externalSavePath;

            _useExternalPathToggle = _rootElement.Q<Toggle>("use-external-path-toggle");
            _useExternalPathToggle.value = _useExternalPath;

            _externalPathContainer = _rootElement.Q<VisualElement>("external-path-container");
            _externalPathContainer.style.display = _useExternalPath ? DisplayStyle.Flex : DisplayStyle.None;

            _defaultModelDropdown = _rootElement.Q<DropdownField>("default-tts-model-dropdown");
            _defaultVoiceDropdown = _rootElement.Q<DropdownField>("default-voice-dropdown");
            _defaultQualityDropdown = _rootElement.Q<DropdownField>("default-quality-dropdown");

            // Populate dropdowns
            PopulateDropdowns();

            // Set initial button states
            _stopButton.SetEnabled(false);
        }

        private void PopulateDropdowns()
        {
            // Populate quality dropdown
            List<string> qualityOptions = new List<string> {
                "mp3_44100_128", "mp3_44100_96", "mp3_44100_64", "mp3_44100_32", "mp3_22050_64",
                "mp3_22050_48", "mp3_22050_32", "mp3_22050_24", "mp3_22050_16", "mp3_16000_16",
                "pcm_16000", "pcm_22050", "pcm_24000", "pcm_44100", "ulaw_8000", "mulaw_8000"
            };

            _qualityDropdown.choices = qualityOptions;
            _qualityDropdown.index = qualityOptions.IndexOf(_selectedQuality);
            if (_qualityDropdown.index < 0) _qualityDropdown.index = 0;

            _ttsQualityDropdown.choices = qualityOptions;
            _ttsQualityDropdown.index = qualityOptions.IndexOf(_selectedQuality);
            if (_ttsQualityDropdown.index < 0) _ttsQualityDropdown.index = 0;

            _vcQualityDropdown.choices = qualityOptions;
            _vcQualityDropdown.index = qualityOptions.IndexOf(_selectedQuality);
            if (_vcQualityDropdown.index < 0) _vcQualityDropdown.index = 0;

            // Populate default dropdowns
            _defaultQualityDropdown.choices = qualityOptions;
            _defaultQualityDropdown.index = qualityOptions.IndexOf(_defaultQuality);
            if (_defaultQualityDropdown.index < 0) _defaultQualityDropdown.index = 0;
        }

        private void PopulateModelDropdowns()
        {
            // TTS models
            List<string> ttsModels = new List<string> {"eleven_multilingual_v2", "eleven_turbo_v2", "eleven_english_v1"};
            _modelDropdown.choices = ttsModels;
            _modelDropdown.index = ttsModels.IndexOf(_selectedTtsModel);
            if (_modelDropdown.index < 0) _modelDropdown.index = 0;

            _defaultModelDropdown.choices = ttsModels;
            _defaultModelDropdown.index = ttsModels.IndexOf(_defaultTtsModel);
            if (_defaultModelDropdown.index < 0) _defaultModelDropdown.index = 0;

            // Voice Changer models
            List<string> vcModels = new List<string> {"eleven_multilingual_sts_v2", "eleven_english_sts_v2"};
            _vcModelDropdown.choices = vcModels;
            _vcModelDropdown.index = vcModels.IndexOf(_selectedVcModel);
            if (_vcModelDropdown.index < 0) _vcModelDropdown.index = 0;
        }

        private void PopulateVoiceDropdowns()
        {
            if (_availableVoices.Count == 0)
            {
                // Populate with some defaults if we don't have the list yet
                _voiceDropdown.choices = new List<string> { "Rachel", "Adam", "Antoni", "Elli" };
                _targetVoiceDropdown.choices = new List<string> { "Rachel", "Adam", "Antoni", "Elli" };
                _defaultVoiceDropdown.choices = new List<string> { "Rachel", "Adam", "Antoni", "Elli" };
                return;
            }

            // Create lists of voice names and IDs
            List<string> voiceNames = new List<string>();
            List<string> voiceIds = new List<string>();

            foreach (var voice in _availableVoices)
            {
                voiceNames.Add(voice.name);
                voiceIds.Add(voice.voice_id);
            }

            // Set the choices for all dropdowns
            _voiceDropdown.choices = voiceNames;
            _targetVoiceDropdown.choices = voiceNames;
            _defaultVoiceDropdown.choices = voiceNames;

            // Set the selected index based on the selected voice ID
            int selectedIndex = voiceIds.IndexOf(_selectedVoiceId);
            if (selectedIndex >= 0)
            {
                _voiceDropdown.index = selectedIndex;
                _targetVoiceDropdown.index = selectedIndex;
            }
            else
            {
                _voiceDropdown.index = 0;
                _targetVoiceDropdown.index = 0;
            }

            // Set the default voice dropdown index
            int defaultIndex = voiceIds.IndexOf(_defaultVoiceId);
            if (defaultIndex >= 0)
            {
                _defaultVoiceDropdown.index = defaultIndex;
            }
            else
            {
                _defaultVoiceDropdown.index = 0;
            }
        }

        private void OnDestroy()
        {
            CleanupResources();
        }
        #endregion

        #region Event Handlers
        private void SetupEventHandlers()
        {
            // Sidebar navigation buttons
            _rootElement.Q<Button>("creations-button").clicked += () => SwitchPanel(SoundCreatorPanelType.Creations);
            _rootElement.Q<Button>("trash-button").clicked += () => SwitchPanel(SoundCreatorPanelType.Trash);
            _rootElement.Q<Button>("settings-button").clicked += () => SwitchPanel(SoundCreatorPanelType.Settings);
            _rootElement.Q<Button>("global-settings-button").clicked += () => UAI.SettingsWindow.ShowWindow();

            // Create panel sub-navigation buttons
            _sfxTabButton.clicked += () => SwitchCreatePanel(CreatePT.SFX);
            _ttsTabButton.clicked += () => SwitchCreatePanel(CreatePT.TTS);
            _vcTabButton.clicked += () => SwitchCreatePanel(CreatePT.VoiceChanger);

            // SFX panel
            _sfxPromptField.RegisterValueChangedCallback(evt => _currentSfxPrompt = evt.newValue);

            _durationSlider.RegisterValueChangedCallback(evt =>
            {
                _duration = evt.newValue;
                _durationValueLabel.text = $"{_duration:F1}s";
            });

            _promptInfluenceSlider.RegisterValueChangedCallback(evt =>
            {
                _promptInfluence = evt.newValue;
                _influenceValueLabel.text = $"{_promptInfluence:F1}";
            });

            _qualityDropdown.RegisterValueChangedCallback(evt => _selectedQuality = evt.newValue);
            _generateSfxButton.clicked += OnGenerateSfxClicked;

            // TTS panel
            _ttsPromptField.RegisterValueChangedCallback(evt => _currentTtsPrompt = evt.newValue);

            _voiceDropdown.RegisterValueChangedCallback(evt =>
            {
                if (_availableVoices.Count > _voiceDropdown.index && _voiceDropdown.index >= 0)
                {
                    _selectedVoiceId = _availableVoices[_voiceDropdown.index].voice_id;
                }
            });

            _modelDropdown.RegisterValueChangedCallback(evt => _selectedTtsModel = evt.newValue);
            _ttsQualityDropdown.RegisterValueChangedCallback(evt => _selectedQuality = evt.newValue);
            _generateTtsButton.clicked += OnGenerateTtsClicked;

            // Voice Changer panel
            _audioFileField.RegisterValueChangedCallback(evt => _selectedAudioFile = evt.newValue as AudioClip);

            _targetVoiceDropdown.RegisterValueChangedCallback(evt =>
            {
                if (_availableVoices.Count > _targetVoiceDropdown.index && _targetVoiceDropdown.index >= 0)
                {
                    _selectedVoiceId = _availableVoices[_targetVoiceDropdown.index].voice_id;
                }
            });

            // Voice recording buttons
            _recordVoiceButton.clicked += StartVoiceRecording;
            _stopRecordingButton.clicked += StopVoiceRecording;

            _vcModelDropdown.RegisterValueChangedCallback(evt => _selectedVcModel = evt.newValue);
            _vcQualityDropdown.RegisterValueChangedCallback(evt => _selectedQuality = evt.newValue);
            _noiseRemovalToggle.RegisterValueChangedCallback(evt => _removeBgNoise = evt.newValue);
            _generateVcButton.clicked += OnGenerateVcClicked;

            // Audio preview panel
            _playButton.clicked += PlaySelectedSound;
            _stopButton.clicked += StopPlayback;
            _timelineSlider.RegisterValueChangedCallback(evt =>
            {
                if (_selectedSound != null && _previewAudioSource != null && _previewAudioSource.clip != null)
                {
                    _previewAudioSource.time = evt.newValue * _previewAudioSource.clip.length;

                    // If we're playing, continue playback from new position
                    if (_isPlaying)
                    {
                        _previewAudioSource.Play();
                    }
                }
            });

            _rootElement.Q<Button>("back-to-creations-button").clicked += () =>
            {
                StopPlayback();
                SwitchPanel(SoundCreatorPanelType.Creations);
            };

            _rootElement.Q<Button>("export-sound-button").clicked += ExportSound;
            _rootElement.Q<Button>("delete-sound-button").clicked += DeleteSound;

            // Trash actions
            _rootElement.Q<Button>("empty-trash-button").clicked += EmptyTrash;

            // Settings panel
            _apiKeyField.RegisterValueChangedCallback(evt =>
            {
                _apiKey = evt.newValue;
            });

            _rootElement.Q<Button>("fetch-voices-button").clicked += () =>
            {
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    // Set visual feedback for fetching
                    _voicesInfoLabel.text = "Fetching voices...";
                    FetchAvailableVoices();
                }
                else
                {
                    _voicesInfoLabel.text = "Please enter an API key first.";
                }
            };

            _savePathField.RegisterValueChangedCallback(evt =>
            {
                _savePath = evt.newValue;
            });

            _externalPathField.RegisterValueChangedCallback(evt =>
            {
                _externalSavePath = evt.newValue;
            });

            _useExternalPathToggle.RegisterValueChangedCallback(evt =>
            {
                _useExternalPath = evt.newValue;
                _externalPathContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            _rootElement.Q<Button>("project-browse-button").clicked += () => BrowseForSavePath(false);
            _rootElement.Q<Button>("browse-path-button").clicked += () => BrowseForSavePath(true);

            _defaultVoiceDropdown.RegisterValueChangedCallback(evt =>
            {
                if (_availableVoices.Count > _defaultVoiceDropdown.index && _defaultVoiceDropdown.index >= 0)
                {
                    _defaultVoiceId = _availableVoices[_defaultVoiceDropdown.index].voice_id;
                }
            });

            _defaultModelDropdown.RegisterValueChangedCallback(evt =>
            {
                _defaultTtsModel = evt.newValue;
            });

            _defaultQualityDropdown.RegisterValueChangedCallback(evt =>
            {
                _defaultQuality = evt.newValue;
            });

            _rootElement.Q<Button>("save-settings-button").clicked += SaveSettings;
            _rootElement.Q<Button>("reset-settings-button").clicked += ResetSettings;
        }
        #endregion

        #region UI Update Methods
        private void SwitchPanel(SoundCreatorPanelType panelType)
        {
            // Stop any audio playback when switching panels
            StopPlayback();

            // Hide all panels
            _creationsPanel.style.display = DisplayStyle.None;
            _trashPanel.style.display = DisplayStyle.None;
            _audioPreviewPanel.style.display = DisplayStyle.None;
            _settingsPanel.style.display = DisplayStyle.None;

            // Show selected panel
            switch (panelType)
            {
                case SoundCreatorPanelType.Creations:
                    _creationsPanel.style.display = DisplayStyle.Flex;
                    RefreshCreationsPanel();
                    break;

                case SoundCreatorPanelType.Trash:
                    _trashPanel.style.display = DisplayStyle.Flex;
                    RefreshTrashPanel();
                    break;

                case SoundCreatorPanelType.AudioPreview:
                    _audioPreviewPanel.style.display = DisplayStyle.Flex;
                    UpdateAudioPreview();
                    break;

                case SoundCreatorPanelType.Settings:
                    _settingsPanel.style.display = DisplayStyle.Flex;
                    break;
            }

            // Update sidebar button states
            UpdateSidebarButtonState(panelType);

            // Store current panel
            _currentPanel = panelType;
        }

        private void SwitchCreatePanel(CreatePT panelType)
        {
            // Hide all create sub-panels
            _sfxPanel.style.display = DisplayStyle.None;
            _ttsPanel.style.display = DisplayStyle.None;
            _voiceChangerPanel.style.display = DisplayStyle.None;

            // Show selected panel
            switch (panelType)
            {
                case CreatePT.SFX:
                    _sfxPanel.style.display = DisplayStyle.Flex;
                    break;

                case CreatePT.TTS:
                    _ttsPanel.style.display = DisplayStyle.Flex;
                    break;

                case CreatePT.VoiceChanger:
                    _voiceChangerPanel.style.display = DisplayStyle.Flex;
                    break;
            }

            // Update tab button states
            UpdateCreateTabButtonState(panelType);

            // Store current create panel
            _currentCreatePanel = panelType;
        }

        private void UpdateSidebarButtonState(SoundCreatorPanelType activePanel)
        {
            // Reset all buttons
            _rootElement.Query<Button>(className: "sidebar-button").ForEach(btn =>
            {
                btn.RemoveFromClassList("active");
            });

            // Highlight active button
            string buttonName = "";
            switch (activePanel)
            {
                case SoundCreatorPanelType.Creations: buttonName = "creations-button"; break;
                case SoundCreatorPanelType.Trash: buttonName = "trash-button"; break;
                case SoundCreatorPanelType.Settings: buttonName = "settings-button"; break;
            }

            if (!string.IsNullOrEmpty(buttonName))
            {
                var activeButton = _rootElement.Q<Button>(name: buttonName);
                if (activeButton != null)
                {
                    activeButton.AddToClassList("active");
                }
            }
        }

        private void UpdateCreateTabButtonState(CreatePT activePanel)
        {
            // Reset all tab buttons
            _sfxTabButton.RemoveFromClassList("selected");
            _ttsTabButton.RemoveFromClassList("selected");
            _vcTabButton.RemoveFromClassList("selected");

            // Highlight active button
            switch (activePanel)
            {
                case CreatePT.SFX:
                    _sfxTabButton.AddToClassList("selected");
                    break;
                case CreatePT.TTS:
                    _ttsTabButton.AddToClassList("selected");
                    break;
                case CreatePT.VoiceChanger:
                    _vcTabButton.AddToClassList("selected");
                    break;
            }
        }

        private void RefreshCreationsPanel()
        {
            // Clear existing content
            _timelineScrollView.Clear();


            // Let the utility handle the UI creation
            SoundCreatorUtility.PopulateCreationsPanel(_timelineScrollView, _createdSounds, OnSoundItemClicked, OnSoundContextMenuRequest, LoadMoreSounds, _soundFileMetadata);
        }

        private void OnSoundItemClicked(CreatedSoundEntry sound)
        {
            _selectedSound = sound;
            SwitchPanel(SoundCreatorPanelType.AudioPreview);
        }

        private void OnSoundContextMenuRequest(ContextClickEvent evt, CreatedSoundEntry sound)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Play"), false, () =>
            {
                _selectedSound = sound;
                SwitchPanel(SoundCreatorPanelType.AudioPreview);
                PlaySelectedSound();
            });

            menu.AddItem(new GUIContent("Export"), false, () =>
            {
                _selectedSound = sound;
                ExportSound();
            });

            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                _selectedSound = sound;
                DeleteSound();
            });

            menu.ShowAsContext();
        }

        private void RefreshTrashPanel()
        {
            // Clear existing content
            var trashScrollView = _rootElement.Q<ScrollView>("trash-scroll-view");
            if (trashScrollView == null) return;
            trashScrollView.Clear();

            // Let the utility handle the UI creation
            SoundCreatorUtility.PopulateTrashPanel(trashScrollView, _trashSounds, OnTrashContextMenuRequest);
        }

        private void OnTrashContextMenuRequest(ContextClickEvent evt, CreatedSoundEntry sound)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Restore"), false, () =>
            {
                RestoreSound(sound);
            });

            menu.AddItem(new GUIContent("Delete Permanently"), false, () =>
            {
                DeletePermanently(sound);
            });

            menu.ShowAsContext();
        }

        private void RestoreSound(CreatedSoundEntry sound)
        {
            SoundCreatorUtility.RestoreSoundFromTrash(sound, ref _trashSounds, ref _createdSounds);
            SaveTrashSounds();
            RefreshTrashPanel();
            RefreshCreationsPanel();
        }

        private void DeletePermanently(CreatedSoundEntry sound)
        {
            // Confirm with the user
            bool confirm = EditorUtility.DisplayDialog(
                "Delete Permanently",
                "Are you sure you want to permanently delete this sound? This cannot be undone.",
                "Yes", "No");

            if (!confirm) return;

            // Remove from trash
            _trashSounds.Remove(sound);

            // Save trash state
            SaveTrashSounds();

            // Refresh UI
            RefreshTrashPanel();
        }

        private void UpdateAudioPreview()
        {
            if (_selectedSound == null) return;

            // Update the info labels
            _promptInfoLabel.text = _selectedSound.Prompt;
            _typeInfoLabel.text = GetTypeDisplayName(_selectedSound.GenerationType);
            _dateInfoLabel.text = _selectedSound.CreationDate.ToString("g");

            // Check if audio clip needs to be reloaded (added for robustness)
            if (_selectedSound.AudioClip != null && _selectedSound.AudioClip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.Log("Audio clip not fully loaded, attempting to load it now");

                // Try to reload from disk
                if (!string.IsNullOrEmpty(_selectedSound.AssetPath))
                {
                    if (_selectedSound.AssetPath.StartsWith("Assets"))
                    {
                        AudioClip reloadedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(_selectedSound.AssetPath);
                        if (reloadedClip != null)
                        {
                            _selectedSound.AudioClip = reloadedClip;
                            _selectedSound.AudioClip.LoadAudioData();
                        }
                    }
                    else if (File.Exists(_selectedSound.AssetPath))
                    {
                        // For external files
                        uAI.EditorCoroutine.StartCoroutine(ReloadExternalAudioClip(_selectedSound));
                    }
                }
            }

            // Update duration label and slider
            if (_selectedSound.AudioClip != null)
            {
                float duration = _selectedSound.AudioClip.length;
                int minutes = Mathf.FloorToInt(duration / 60f);
                int seconds = Mathf.FloorToInt(duration % 60f);
                _durationLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);

                // Set up timeline slider
                _timelineSlider.lowValue = 0f;
                _timelineSlider.highValue = 1f;
                _timelineSlider.value = 0f;

                // Debug.Log($"Audio preview updated: Clip duration = {duration}s, Load state = {_selectedSound.AudioClip.loadState}");
            }
            else
            {
                Debug.LogWarning("Audio clip is null in UpdateAudioPreview");
                _durationLabel.text = "00:00";
            }

            // Update waveform display
            UpdateWaveformDisplay(_selectedSound.AudioClip);

            // Reset button states
            _playButton.SetEnabled(true);
            _stopButton.SetEnabled(false);
        }

        private IEnumerator ReloadExternalAudioClip(CreatedSoundEntry soundEntry)
        {
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + soundEntry.AssetPath, AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error loading audio clip: {www.error}");
                yield break;
            }


            if (string.IsNullOrEmpty(www.error))
            { 
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www); 
                if (clip != null)
                {
                    int attempts = 0;
                    while (clip.loadState != AudioDataLoadState.Loaded && attempts < 30)
                    {
                        yield return new WaitForSeconds(0.1f);
                        attempts++;
                    }

                    soundEntry.AudioClip = clip;

                    // Update UI after clip is loaded
                    UpdateWaveformDisplay(soundEntry.AudioClip);

                    if (soundEntry.AudioClip != null)
                    {
                        float duration = soundEntry.AudioClip.length;
                        int minutes = Mathf.FloorToInt(duration / 60f);
                        int seconds = Mathf.FloorToInt(duration % 60f);
                        _durationLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                    }
                }
            }
            else
            {
                Debug.LogError($"Error reloading audio clip: {www.error}");
            }

            www.Dispose();
        }



        private string GetTypeDisplayName(string type)
        {
            switch (type.ToLower())
            {
                case "sfx": return "Sound Effect";
                case "tts": return "Text to Speech";
                case "vc": return "Voice Changer";
                default: return type;
            }
        }

        private void UpdateWaveformDisplay(AudioClip clip)
        {  
            // Clear existing waveform
            _waveformDisplay.Clear();

            if (clip == null)
            {
                Debug.LogWarning("AudioClip is null, cannot display waveform");
                return;
            }

            // Create a container for the waveform
            var waveformContainer = new VisualElement();
            waveformContainer.AddToClassList("waveform-container");
            _waveformDisplay.Add(waveformContainer);

            // Initialize debug info
            StringBuilder debugInfo = new StringBuilder();
            debugInfo.AppendLine($"Clip Info: {clip.name}, length: {clip.length}s, samples: {clip.samples}, channels: {clip.channels}, state: {clip.loadState}");

            // Get data directly from the file
            float[] waveformData = null;
            string assetPath = null;
            string fullPath = null;

            try
            {
                // Try multiple approaches to get the file path

                // Approach 1: Get path from selected sound
                if (_selectedSound != null && !string.IsNullOrEmpty(_selectedSound.AssetPath))
                {
                    assetPath = _selectedSound.AssetPath;
                    debugInfo.AppendLine($"Using asset path from selected sound: {assetPath}");
                }
                // Approach 2: Get path from AssetDatabase
                else
                {
                    assetPath = AssetDatabase.GetAssetPath(clip);
                    debugInfo.AppendLine($"Found asset path from AssetDatabase: {assetPath}");
                }

                // Convert path formats
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // External file (outside project) handling
                    if (!assetPath.StartsWith("Assets") && assetPath.StartsWith(Application.dataPath))
                    {
                        assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                        debugInfo.AppendLine($"Converted to project-relative path: {assetPath}");
                    }

                    // Convert path format if needed
                    fullPath = assetPath;
                    if (assetPath.StartsWith("Assets"))
                    {
                        fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7));
                        debugInfo.AppendLine($"Converted to full path: {fullPath}");
                    }

                    // Check if file exists
                    if (File.Exists(fullPath))
                    {
                        debugInfo.AppendLine($"File exists at: {fullPath}");

                        // Approach 3: Try direct file path to look for external files
                        if (assetPath.Contains("/") && !File.Exists(fullPath))
                        {
                            string fileName = Path.GetFileName(assetPath);
                            // Try to find file in project folders
                            string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName));
                            foreach (string guid in guids)
                            {
                                string candidatePath = AssetDatabase.GUIDToAssetPath(guid);
                                if (Path.GetFileName(candidatePath) == fileName)
                                {
                                    assetPath = candidatePath;
                                    fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7));
                                    debugInfo.AppendLine($"Found file via GUID search: {fullPath}");
                                    break;
                                }
                            }
                        }

                        // Generate waveform data from file
                        waveformData = WaveformGenerator.GenerateWaveformData(fullPath, 60);

                        debugInfo.AppendLine($"Waveform data generated, first few values: {string.Join(", ", waveformData.Length > 5 ? new[] { waveformData[0], waveformData[1], waveformData[2], waveformData[3], waveformData[4] } : waveformData)}");
                    }
                    else
                    {
                        debugInfo.AppendLine($"File not found at: {fullPath}");
                        debugInfo.AppendLine("Attempting alternative approaches...");

                        // Approach 4: Search for file by name in project
                        string fileName = Path.GetFileName(assetPath);
                        debugInfo.AppendLine($"Searching for file by name: {fileName}");

                        string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName));
                        bool found = false;

                        foreach (string guid in guids)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            if (Path.GetFileName(path) == fileName)
                            {
                                assetPath = path;
                                fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7));
                                debugInfo.AppendLine($"Found file via search: {fullPath}");

                                if (File.Exists(fullPath))
                                {
                                    waveformData = WaveformGenerator.GenerateWaveformData(fullPath, 60);
                                    found = true;
                                    break;
                                }
                            }
                        }

                        // Approach 5: For external files saved outside project
                        if (!found && _selectedSound != null && File.Exists(_selectedSound.AssetPath))
                        {
                            debugInfo.AppendLine($"Using direct external path: {_selectedSound.AssetPath}");
                            waveformData = WaveformGenerator.GenerateWaveformData(_selectedSound.AssetPath, 60);
                        }
                    }
                }
                else
                {
                    debugInfo.AppendLine("Could not determine asset path for audio clip");
                }
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error getting waveform data: {ex.Message}");
                Debug.LogError(debugInfo.ToString());
            }

            // Final debugging summary
            debugInfo.AppendLine($"Final path used: {fullPath}");
            debugInfo.AppendLine($"Waveform data null? {waveformData == null}");
            if (waveformData != null)
            {
                debugInfo.AppendLine($"Waveform data length: {waveformData.Length}");
                debugInfo.AppendLine($"Min value: {waveformData.Min()}, Max value: {waveformData.Max()}, Avg: {waveformData.Average()}");
            }

            // Log all debug info
            // Debug.Log(debugInfo.ToString());

            // If we couldn't get waveform data, generate a visible fallback
            if (waveformData == null || waveformData.Length == 0 || waveformData.All(v => v < 0.01f))
            {
                // Debug.Log("Using guaranteed visible fallback waveform generation");

                // Generate a basic fallback waveform that's guaranteed to be visible
                waveformData = new float[60];
                for (int i = 0; i < 60; i++)
                {
                    float phase = (float)i / 60;
                    float amp1 = Mathf.Sin(phase * 2 * Mathf.PI * 2.5f);
                    float amp2 = Mathf.Sin(phase * 2 * Mathf.PI * 5.0f) * 0.5f;
                    float amp3 = Mathf.Sin(phase * 2 * Mathf.PI * 7.5f) * 0.25f;

                    waveformData[i] = Mathf.Abs(amp1 + amp2 + amp3) / 1.75f;

                    // Make sure values are never 0
                    waveformData[i] = Mathf.Max(0.2f, waveformData[i]);

                    // Fade the edges
                    float edgeFade = Mathf.Min(phase * 4, (1 - phase) * 4, 1.0f);
                    waveformData[i] *= edgeFade;

                    // Ensure minimum value
                    waveformData[i] = Mathf.Max(0.08f, waveformData[i]);
                }
            }

            // Create visual bars for each data point
            float spacing = _waveformDisplay.resolvedStyle.width / waveformData.Length;

            // Hard-coded test for debugging - ensures we see SOMETHING
            if (waveformData.All(v => v < 0.05f))
            {
                // Debug.LogWarning("All waveform values are too small, using test pattern");
                for (int i = 0; i < waveformData.Length; i++)
                {
                    waveformData[i] = 0.2f + ((i % 3) == 0 ? 0.5f : 0.2f);
                }
            }

            for (int i = 0; i < waveformData.Length; i++)
            {
                // Create a bar element
                var bar = new VisualElement();
                bar.AddToClassList("waveform-bar");

                // Set bar properties - ensure minimum height so it's always visible
                float barWidth = Mathf.Max(1, spacing - 1); // At least 1px wide

                // Get bar height and ensure it's never too small to see
                float height = waveformData[i]; 
                // float barHeight = Mathf.Min(10, height * _waveformDisplay.resolvedStyle.height * 10f);
                float barHeight = height * 30f;

                bar.style.width = barWidth;
                bar.style.height = barHeight;
                bar.style.marginTop = (_waveformDisplay.resolvedStyle.height - barHeight) / 2;

                if (i % 10 == 0)
                {
                    // Debug.Log($"Bar {i}: Value={height}, Height={barHeight}px");
                }

                // Add the bar to the container
                waveformContainer.Add(bar);
            }

            // Add playhead indicator
            var playhead = new VisualElement();
            playhead.AddToClassList("playhead-indicator");
            playhead.style.position = Position.Absolute;
            playhead.style.width = 2;
            playhead.style.backgroundColor = new Color(1f, 1f, 1f, 0.8f);
            playhead.style.height = _waveformDisplay.resolvedStyle.height;
            playhead.style.left = 0;

            waveformContainer.Add(playhead);

            // Store the playhead for updating during playback
            _playheadIndicator = playhead;
        }



        private void ShowLoadingIndicator()
        { 
            if (_loadingOverlay != null)
            { 
                _loadingOverlay.style.display = DisplayStyle.Flex;
            }
            else
            {
                Debug.LogWarning("Loading overlay not found");
            }
        }

        private void HideLoadingIndicator()
        { 
            if (_loadingOverlay != null)
            { 
                _loadingOverlay.style.display = DisplayStyle.None;
            }
        }

        private void UpdateProgress()
        {
            if (!_isGenerating) return;

            // Update progress animation
            _progressValue = Mathf.Repeat(_progressValue + (PROGRESS_BAR_ANIMATION_SPEED * 0.01f), 1f);

            // Update progress bar in loading overlay
            if (_loadingOverlay != null)
            {
                var progressFill = _loadingOverlay.Q<VisualElement>("progress-fill");
                if (progressFill != null)
                {
                    progressFill.style.width = new Length(_progressValue * 100, LengthUnit.Percent);
                }
            }

            UpdateProgressInPanel(_sfxPanel);
            UpdateProgressInPanel(_ttsPanel);
            UpdateProgressInPanel(_voiceChangerPanel);

            // Repaint window to update animations
            Repaint();
        }

        private void UpdateProgressInPanel(VisualElement panel)
        {
            var progressFill = panel.Q<VisualElement>("progress-fill");
            if (progressFill != null)
            {
                progressFill.style.width = new Length(_progressValue * 100, LengthUnit.Percent);
            }
 
            if (_loadingOverlay != null)
            {
                _loadingOverlay.style.display = _isGenerating ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        #endregion

        #region Recording Methods 
        private void StartVoiceRecording()
        {
            // Check if microphone is available
            if (Microphone.devices.Length == 0)
            {
                EditorUtility.DisplayDialog("No Microphone Found", "No microphone device is available on this system.", "OK");
                return;
            }

            // Stop any previous recording
            if (_isRecording)
            {
                StopVoiceRecording();
            }

            // Use the default microphone device
            _recordingDevice = Microphone.devices[0];
            Debug.Log($"Starting recording with device: {_recordingDevice}");

            // Start recording
            _recordedClip = Microphone.Start(_recordingDevice, false, MAX_RECORDING_SECONDS, RECORDING_FREQUENCY);

            if (_recordedClip != null)
            {
                _isRecording = true;
                _recordingStartTime = (float)EditorApplication.timeSinceStartup;

                // Update UI
                _recordingUI.style.display = DisplayStyle.Flex;
                _recordVoiceButton.SetEnabled(false);

                // Start updating the recording time display
                EditorApplication.update += UpdateRecordingTime;

                // Start indicator animation
                EditorApplication.update += AnimateRecordingIndicator;
            }
            else
            {
                Debug.LogError("Failed to start recording. Check microphone permissions and settings.");
                EditorUtility.DisplayDialog("Recording Failed", "Failed to start recording. Check microphone permissions and settings.", "OK");
            }
        }

        private void StopVoiceRecording()
        {
            if (!_isRecording || string.IsNullOrEmpty(_recordingDevice))
                return;

            // Stop updating time and animation
            EditorApplication.update -= UpdateRecordingTime;
            EditorApplication.update -= AnimateRecordingIndicator;

            // Calculate the recording length in samples
            int recordingPosition = Microphone.GetPosition(_recordingDevice);
            Debug.Log($"Recording stopped at position: {recordingPosition}");

            // Stop the microphone
            Microphone.End(_recordingDevice);

            if (recordingPosition > 0 && _recordedClip != null)
            {
                // Create a new clip with only the recorded data
                AudioClip tempClip = _recordedClip;
                float recordingLength = (float)recordingPosition / RECORDING_FREQUENCY;

                // Create a temporary WAV file
                string tempFile = Path.Combine(Path.GetTempPath(), $"recording_{DateTime.Now.Ticks}.wav");

                // Save the recorded audio to a temporary file
                SaveWave.Save(tempFile, tempClip, 0, recordingPosition);

                // Load the clean clip
                if (File.Exists(tempFile))
                {
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFile, AudioType.WAV);
                    www.SendWebRequest();
                    while (!www.isDone) { } 

                    if (string.IsNullOrEmpty(www.error))
                    {
                        _recordedClip = DownloadHandlerAudioClip.GetContent(www); 

                        // Wait for the clip to load
                        int attempts = 0;
                        while (_recordedClip.loadState != AudioDataLoadState.Loaded && attempts < 30)
                        {
                            System.Threading.Thread.Sleep(100);
                            attempts++;
                        }

                        // Set the recorded clip as the selected audio file
                        _selectedAudioFile = _recordedClip;
                        _audioFileField.value = _recordedClip;

                        Debug.Log($"Recording completed: {recordingLength:F2}s, {_recordedClip.samples} samples");

                        // Clean up temp file
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to delete temp file: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Error loading recorded clip: {www.error}");
                    }

                    www.Dispose();
                }
                else
                {
                    Debug.LogError("Failed to save WAV file");
                }
            }

            _isRecording = false;
            _recordingDevice = null;

            // Update UI
            _recordingUI.style.display = DisplayStyle.None;
            _recordVoiceButton.SetEnabled(true);
        }

        private void UpdateRecordingTime()
        {
            if (!_isRecording)
                return;

            float recordingTime = (float)EditorApplication.timeSinceStartup - _recordingStartTime;
            int minutes = Mathf.FloorToInt(recordingTime / 60f);
            int seconds = Mathf.FloorToInt(recordingTime % 60f);

            _recordingTimeLabel.text = $"Recording... {minutes:00}:{seconds:00}";

            // Automatically stop if we reach MAX_RECORDING_SECONDS
            if (recordingTime >= MAX_RECORDING_SECONDS)
            {
                StopVoiceRecording();
            }
        }

        private void AnimateRecordingIndicator()
        {
            if (!_isRecording)
                return;

            // Pulse the recording indicator
            float pulseValue = Mathf.PingPong((float)EditorApplication.timeSinceStartup * 2f, 1f);
            Color color = new Color(1f, 0f, 0f, 0.5f + pulseValue * 0.5f);
            _recordingIndicator.style.backgroundColor = color;
        }
        #endregion

        #region Audio Playback Methods
        private void PlaySelectedSound()
        {
            if (_selectedSound == null || _selectedSound.AudioClip == null)
            {
                Debug.LogError("Cannot play sound: AudioClip is null");
                return;
            }

            // Set up playback state tracking
            _previewClipLength = _selectedSound.AudioClip.length;
            _previewStartTime = (float)EditorApplication.timeSinceStartup - (_timelineSlider.value * _previewClipLength);

            // Stop any currently playing preview
            AudioPreviewUtil.StopAllClips();

            // Calculate start sample based on timeline position
            int startSample = Mathf.FloorToInt(_timelineSlider.value * _selectedSound.AudioClip.samples);

            // Play the clip using AudioUtil
            AudioPreviewUtil.PlayClip(_selectedSound.AudioClip, startSample, false);

            // Debug.Log($"Playing sound using AudioUtil: {_selectedSound.Prompt}, Clip length: {_previewClipLength}s, Starting at sample: {startSample}");

            _isPlaying = true;

            // Schedule updates for the timeline slider
            EditorApplication.update += UpdateTimelineSlider;

            // Update UI
            _playButton.SetEnabled(false);
            _stopButton.SetEnabled(true);

            // Force a repaint to update UI
            Repaint();
        }
  

        private void StopPlayback()
        {
            if (!_isPlaying) return;

            // Stop all preview clips
            AudioPreviewUtil.StopAllClips();

            _isPlaying = false;

            // Remove update callback
            EditorApplication.update -= UpdateTimelineSlider;

            // Update UI
            _playButton.SetEnabled(true);
            _stopButton.SetEnabled(false);
        }



        private void UpdateTimelineSlider()
        {
            if (!_isPlaying) return;

            // Calculate current playback position based on time since we started playing
            float elapsedTime = (float)EditorApplication.timeSinceStartup - _previewStartTime;
            float normalizedTime = elapsedTime / _previewClipLength;

            // Cap at 1.0 to avoid going beyond the end
            normalizedTime = Mathf.Clamp01(normalizedTime);

            // Update the timeline slider to match the current playback position
            _timelineSlider.SetValueWithoutNotify(normalizedTime);

            // Update playhead position if available
            if (_playheadIndicator != null && _waveformDisplay != null)
            {
                _playheadIndicator.style.left = normalizedTime * _waveformDisplay.resolvedStyle.width;
            }

            // Check if playback has ended
            if (normalizedTime >= 1.0f)
            {
                StopPlayback();
                _timelineSlider.SetValueWithoutNotify(0f);

                // Reset playhead position
                if (_playheadIndicator != null)
                {
                    _playheadIndicator.style.left = 0;
                }
            }

            // Repaint to update UI
            Repaint();
        }



        #endregion

        #region Persistence Methods
        private void SaveTrashSounds()
        {
            SoundCreatorUtility.SaveTrashSounds(PREFS_KEY_PREFIX, _trashSounds);
        }
        #endregion

        #region API Methods
        private async void FetchAvailableVoices()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _voicesInfoLabel.text = "Error: API Key is not set.";
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set up the request
                    client.BaseAddress = new Uri(ELEVENLABS_API_URL);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Add API key to headers
                    client.DefaultRequestHeaders.Add("xi-api-key", _apiKey);

                    // Make the request
                    HttpResponseMessage response = await client.GetAsync("/v1/voices");

                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        // Debug.Log($"Received voices JSON: {jsonResponse.Substring(0, Math.Min(100, jsonResponse.Length))}...");

                        // Parse the JSON manually
                        _availableVoices = ParseVoicesFromJson(jsonResponse);

                        if (_availableVoices.Count > 0)
                        {
                            // Update the dropdowns with the new voices
                            PopulateVoiceDropdowns();

                            // Update success message in UI
                            _voicesInfoLabel.text = $"Successfully fetched {_availableVoices.Count} voices.";
                            _voicesInfoLabel.style.color = new Color(0.5f, 0.8f, 0.5f);
                        }
                        else
                        {
                            _voicesInfoLabel.text = "Error: No voices found in API response.";
                            _voicesInfoLabel.style.color = new Color(0.8f, 0.5f, 0.5f);
                        }
                    }
                    else
                    {
                        string errorMsg = await response.Content.ReadAsStringAsync();
                        _voicesInfoLabel.text = $"Error: {response.StatusCode}. {errorMsg}";
                        _voicesInfoLabel.style.color = new Color(0.8f, 0.5f, 0.5f);
                        Debug.LogError($"Voice API error ({response.StatusCode}): {errorMsg}");
                    }
                }
            }
            catch (Exception ex)
            {
                _voicesInfoLabel.text = $"Error: {ex.Message}";
                _voicesInfoLabel.style.color = new Color(0.8f, 0.5f, 0.5f);
                Debug.LogException(ex);
            }
        }

        // Parse voices from JSON using a simple approach
        private List<VoiceInfo> ParseVoicesFromJson(string json)
        {
            List<VoiceInfo> result = new List<VoiceInfo>();

            try
            {
                // Extract voice_id and name pairs using regex
                var voiceIdMatches = Regex.Matches(json, "\"voice_id\":\"([^\"]+)\"");
                var nameMatches = Regex.Matches(json, "\"name\":\"([^\"]+)\"");

                for (int i = 0; i < Math.Min(voiceIdMatches.Count, nameMatches.Count); i++)
                {
                    string voiceId = voiceIdMatches[i].Groups[1].Value;
                    string name = nameMatches[i].Groups[1].Value;

                    result.Add(new VoiceInfo
                    {
                        voice_id = voiceId,
                        name = name
                    });
                }

                // Debug.Log($"Successfully parsed {result.Count} voices from JSON");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing voices JSON: {ex.Message}");
            }

            return result;
        }


        #endregion

        #region Generation Methods
        private void OnGenerateSfxClicked()
        {
            // Validate prompt
            if (string.IsNullOrWhiteSpace(_currentSfxPrompt))
            {
                EditorUtility.DisplayDialog("Empty Prompt", "Please enter a description of the sound effect you want to generate.", "OK");
                return;
            }

            // Check if API key is set
            if (string.IsNullOrEmpty(_apiKey))
            {
                ShowAPIKeyWarning();
                return;
            }

            // Validate save path
            string savePath = _useExternalPath ? _externalSavePath : _savePath;
            if (string.IsNullOrWhiteSpace(savePath))
            {
                EditorUtility.DisplayDialog("Invalid Save Path", "Please configure a valid save path in the settings.", "OK");
                SwitchPanel(SoundCreatorPanelType.Settings);
                return;
            }

            // Start generation
            _isGenerating = true;
            _progressValue = 0f;
            ShowLoadingIndicator();

            // Disable UI during generation
            _generateSfxButton.SetEnabled(false);

            // Generate sound effect
            SoundCreatorUtility.GenerateSoundEffect(
                _apiKey,
                _currentSfxPrompt,
                _duration,
                _promptInfluence,
                _selectedQuality,
                OnSoundReceived,
                _savePath,
                _externalSavePath,
                _useExternalPath,
                Application.dataPath
            );
        }

        private void OnGenerateTtsClicked()
        {
            // Validate prompt
            if (string.IsNullOrWhiteSpace(_currentTtsPrompt))
            {
                EditorUtility.DisplayDialog("Empty Text", "Please enter text you want to convert to speech.", "OK");
                return;
            }

            // Check if API key is set
            if (string.IsNullOrEmpty(_apiKey))
            {
                ShowAPIKeyWarning();
                return;
            }

            // Check if voice is selected
            if (string.IsNullOrEmpty(_selectedVoiceId))
            {
                EditorUtility.DisplayDialog("No Voice Selected", "Please select a voice for text-to-speech conversion.", "OK");
                return;
            }

            // Validate save path
            string savePath = _useExternalPath ? _externalSavePath : _savePath;
            if (string.IsNullOrWhiteSpace(savePath))
            {
                EditorUtility.DisplayDialog("Invalid Save Path", "Please configure a valid save path in the settings.", "OK");
                SwitchPanel(SoundCreatorPanelType.Settings);
                return;
            }

            // Start generation
            _isGenerating = true;
            _progressValue = 0f;
            ShowLoadingIndicator();

            // Disable UI during generation
            _generateTtsButton.SetEnabled(false);

            // Generate text-to-speech
            SoundCreatorUtility.GenerateTextToSpeech(
                _apiKey,
                _currentTtsPrompt,
                _selectedVoiceId,
                _selectedTtsModel,
                _selectedQuality,
                _textNormalizationToggle.value,
                OnSoundReceived,
                _savePath,
                _externalSavePath,
                _useExternalPath,
                Application.dataPath
            );
        }

        private void OnGenerateVcClicked()
        {
            // Validate audio file
            if (_selectedAudioFile == null)
            {
                EditorUtility.DisplayDialog("No Audio Selected", "Please select an audio file to transform.", "OK");
                return;
            }

            // Check if API key is set
            if (string.IsNullOrEmpty(_apiKey))
            {
                ShowAPIKeyWarning();
                return;
            }

            // Check if voice is selected
            if (string.IsNullOrEmpty(_selectedVoiceId))
            {
                EditorUtility.DisplayDialog("No Voice Selected", "Please select a target voice for the transformation.", "OK");
                return;
            }

            // Validate save path
            string savePath = _useExternalPath ? _externalSavePath : _savePath;
            if (string.IsNullOrWhiteSpace(savePath))
            {
                EditorUtility.DisplayDialog("Invalid Save Path", "Please configure a valid save path in the settings.", "OK");
                SwitchPanel(SoundCreatorPanelType.Settings);
                return;
            }

            // Start generation
            _isGenerating = true;
            _progressValue = 0f;
            ShowLoadingIndicator();

            // Disable UI during generation
            _generateVcButton.SetEnabled(false);

            // Generate voice conversion
            SoundCreatorUtility.GenerateVoiceChange(
                _apiKey,
                _selectedAudioFile,
                _selectedVoiceId,
                _selectedVcModel,
                _selectedQuality,
                _removeBgNoise,
                OnSoundReceived,
                _savePath,
                _externalSavePath,
                _useExternalPath,
                Application.dataPath
            );
        }

        private void OnSoundReceived(CreatedSoundEntry createdEntry, bool success)
        {
            // End generation state
            _isGenerating = false;
            HideLoadingIndicator();

            // Re-enable buttons
            _generateSfxButton.SetEnabled(true);
            _generateTtsButton.SetEnabled(true);
            _generateVcButton.SetEnabled(true);

            if (!success || createdEntry == null)
            {
                EditorUtility.DisplayDialog("Generation Failed", "Failed to generate sound. Please check the console for more details.", "OK");
                return;
            }

            // Show success notification
            string typeText = GetTypeDisplayName(createdEntry.GenerationType);
            EditorUtility.DisplayDialog("Generation Successful", $"{typeText} successfully created!", "OK");

            // Add to the dictionary
            DateTime dateKey = createdEntry.CreationDate.Date;
            if (_createdSounds.ContainsKey(dateKey))
            {
                _createdSounds[dateKey].Add(createdEntry);
            }
            else
            {
                _createdSounds[dateKey] = new List<CreatedSoundEntry> { createdEntry };
            }

            // Also add to metadata collection
            SoundFileMetadata metadata = new SoundFileMetadata(
                createdEntry.AssetPath,
                createdEntry.CreationDate,
                createdEntry.Prompt,
                createdEntry.GenerationType
            );
            metadata.IsLoaded = true; // Mark as already loaded

            if (_soundFileMetadata.ContainsKey(dateKey))
            {
                _soundFileMetadata[dateKey].Add(metadata);
            }
            else
            {
                _soundFileMetadata[dateKey] = new List<SoundFileMetadata> { metadata };
            }

            // Refresh the timeline view
            RefreshCreationsPanel();

            // Select and play the newly created sound
            _selectedSound = createdEntry;
            SwitchPanel(SoundCreatorPanelType.AudioPreview);
            PlaySelectedSound();
        }

        #endregion

        #region Action Methods
        private void ExportSound()
        {
            if (_selectedSound == null) return;

            SoundCreatorUtility.ExportSound(_selectedSound, Application.dataPath);
        }

        private void DeleteSound()
        {
            if (_selectedSound == null) return;

            // Confirm with the user
            bool confirm = EditorUtility.DisplayDialog(
                "Delete Sound",
                "Are you sure you want to move this sound to trash?",
                "Yes", "No");

            if (!confirm) return;

            // Stop playback if this is the sound being played
            StopPlayback();

            // Mark as in trash
            _selectedSound.IsInTrash = true;

            // Find and remove the sound from created sounds
            foreach (var date in _createdSounds.Keys.ToList())
            {
                var sounds = _createdSounds[date];
                if (sounds.Contains(_selectedSound))
                {
                    sounds.Remove(_selectedSound);

                    // Add to trash
                    _trashSounds.Add(_selectedSound);

                    // If date group is now empty, remove it
                    if (sounds.Count == 0)
                    {
                        _createdSounds.Remove(date);
                    }

                    break;
                }
            }

            // Refresh UI
            RefreshCreationsPanel();

            // Save trash state
            SaveTrashSounds();

            // Go back to creations panel
            SwitchPanel(SoundCreatorPanelType.Creations);
        }

        private void EmptyTrash()
        {
            if (_trashSounds.Count == 0) return;

            // Confirm with the user
            bool confirm = EditorUtility.DisplayDialog(
                "Empty Trash",
                "Are you sure you want to permanently delete all sounds in the trash? This cannot be undone.",
                "Yes", "No");

            if (!confirm) return;

            // Delete all trash sounds
            _trashSounds.Clear();

            // Save trash state
            SaveTrashSounds();

            // Refresh trash panel
            RefreshTrashPanel();
        }

        private void BrowseForSavePath(bool isExternal)
        {
            string title = isExternal ? "Select External Save Location" : "Select Project Save Location";
            string defaultPath = isExternal ? _externalSavePath : _savePath;

            if (isExternal)
            {
                // For external paths, use the folder panel
                string path = EditorUtility.OpenFolderPanel(title, defaultPath, "");

                if (!string.IsNullOrEmpty(path))
                {
                    // Update the external path field
                    _externalSavePath = path;
                    _externalPathField.value = path;
                }
            }
            else
            {
                // For project paths, show project folder selection
                string path = EditorUtility.OpenFolderPanel(title, Application.dataPath, "");

                if (!string.IsNullOrEmpty(path))
                {
                    // Make path relative to Assets if within the project
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    }

                    // Update the path field
                    _savePath = path;
                    _savePathField.value = path;
                }
            }
        }

        private void SaveSettings()
        {
            // Get current voice ID if set
            if (_availableVoices.Count > 0 && _defaultVoiceDropdown.index >= 0 && _defaultVoiceDropdown.index < _availableVoices.Count)
            {
                _defaultVoiceId = _availableVoices[_defaultVoiceDropdown.index].voice_id;
            }

            // Get selected model
            _defaultTtsModel = _defaultModelDropdown.value;

            // Get selected quality
            _defaultQuality = _defaultQualityDropdown.value;

            // Save all settings to EditorPrefs
            SoundCreatorUtility.SaveSettings(
                PREFS_KEY_PREFIX,
                _apiKey,
                _savePath,
                _externalSavePath,
                _useExternalPath,
                _defaultVoiceId,
                _defaultTtsModel,
                _defaultVcModel,
                _defaultSfxModel,
                _defaultQuality,
                _removeBgNoise
            );

            // Update current generation settings with defaults
            _selectedVoiceId = _defaultVoiceId;
            _selectedTtsModel = _defaultTtsModel;
            _selectedVcModel = _defaultVcModel;
            _selectedQuality = _defaultQuality;

            // Update UI elements
            if (_voiceDropdown != null && _availableVoices.Count > 0)
            {
                for (int i = 0; i < _availableVoices.Count; i++)
                {
                    if (_availableVoices[i].voice_id == _selectedVoiceId)
                    {
                        _voiceDropdown.index = i;
                        _targetVoiceDropdown.index = i;
                        break;
                    }
                }
            }

            if (_modelDropdown != null)
            {
                List<string> modelOptions = _modelDropdown.choices;
                int modelIndex = modelOptions.IndexOf(_selectedTtsModel);
                if (modelIndex >= 0)
                {
                    _modelDropdown.index = modelIndex;
                }
            }

            if (_vcModelDropdown != null)
            {
                List<string> vcModelOptions = _vcModelDropdown.choices;
                int vcModelIndex = vcModelOptions.IndexOf(_selectedVcModel);
                if (vcModelIndex >= 0)
                {
                    _vcModelDropdown.index = vcModelIndex;
                }
            }

            // Update all quality dropdowns
            List<string> qualityOptions = _qualityDropdown.choices;
            int qualityIndex = qualityOptions.IndexOf(_selectedQuality);
            if (qualityIndex >= 0)
            {
                _qualityDropdown.index = qualityIndex;
                _ttsQualityDropdown.index = qualityIndex;
                _vcQualityDropdown.index = qualityIndex;
            }
 
            // Show confirmation
            // EditorUtility.DisplayDialog("Settings Saved", "Your settings have been saved and applied.", "OK");
        }

        private void ResetSettings()
        {
            // Confirm with the user
            bool confirm = EditorUtility.DisplayDialog(
                "Reset Settings",
                "Are you sure you want to reset all settings to defaults?",
                "Yes", "No");

            if (!confirm) return;

            // Reset settings to defaults
            _apiKey = "";
            _savePath = DEFAULT_SAVE_PATH;
            _externalSavePath = "";
            _useExternalPath = false;
            _defaultVoiceId = "21m00Tcm4TlvDq8ikWAM"; // Default voice (Rachel)
            _defaultTtsModel = "eleven_multilingual_v2";
            _defaultSfxModel = "eleven_sfx_v1";
            _defaultVcModel = "eleven_multilingual_sts_v2";
            _defaultQuality = "mp3_44100_128";
            _removeBgNoise = false;

            // Update UI fields
            _apiKeyField.value = _apiKey;
            _savePathField.value = _savePath;
            _externalPathField.value = _externalSavePath;
            _useExternalPathToggle.value = _useExternalPath;

            // Update dropdowns
            PopulateVoiceDropdowns();
            PopulateModelDropdowns();
            PopulateDropdowns();

            // Save these defaults
            SaveSettings();
        }
        #endregion

        #region Utility Methods
        private void CancelGeneration()
        {
            if (!_isGenerating) return;

            _isGenerating = false;
            HideLoadingIndicator();

            // Re-enable buttons
            _generateSfxButton.SetEnabled(true);
            _generateTtsButton.SetEnabled(true);
            _generateVcButton.SetEnabled(true);
        }

        private void ShowAPIKeyWarning()
        {
            bool openSettings = EditorUtility.DisplayDialog(
                "ElevenLabs API Key Required",
                "The Sound Creator requires an ElevenLabs API key. Would you like to open settings to add your key?",
                "Open Settings", "Cancel");

            if (openSettings)
            {
                SwitchPanel(SoundCreatorPanelType.Settings);
            }
        }

        private void CleanupResources()
        {
            // Stop any playing audio
            if (_isPlaying)
            {
                AudioPreviewUtil.StopAllClips();
                _isPlaying = false;
                EditorApplication.update -= UpdateTimelineSlider;
            }
            if (_isRecording)
            {
                StopVoiceRecording();
            }
        }


        #endregion
    }

    #region Models
    public enum SoundCreatorPanelType
    {
        Creations,
        Trash,
        AudioPreview,
        Settings
    }

    public enum CreatePT
    {
        SFX,
        TTS,
        VoiceChanger
    }

    [Serializable]
    public class CreatedSoundEntry
    {
        public AudioClip AudioClip;
        public string Prompt;
        public string GenerationType; // SFX, TTS, or VoiceChanger
        public DateTime CreationDate;
        public string ModelUsed;
        public string AssetPath;
        public bool IsInTrash;
    }

    [Serializable]
    public class VoiceInfo
    {
        public string voice_id;
        public string name;
    }

    [Serializable]
    public class VoicesResponse
    {
        public List<VoiceInfo> voices;
    }

    [Serializable]
    public class TrashSoundsData
    {
        public List<string> TrashPaths = new List<string>();
    }
    #endregion
}
