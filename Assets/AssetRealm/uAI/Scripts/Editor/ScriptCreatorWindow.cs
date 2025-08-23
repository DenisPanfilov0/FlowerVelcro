using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Compilation;
using System.Collections;
using Unity.EditorCoroutines.Editor;

namespace UAI
{ 
    public class ScriptCreatorWindow : EditorWindow
    {
        #region Constants
        private const string DEFAULT_INITIAL_PROMPT = "I need a script that can make a cube move around in a circle.";
        private string SYSTEM_INIT_PROMPT =
            "You are a professional Unity programming assistant. When creating or modifying scripts. " +
            "When improving existing code, preserve the original class names unless specifically asked to rename them. " +
            "This helps users maintain compatibility with previous versions. Don't explain the code overly unless asked. " +
            "(!IMPORTANT) Every code block should be wrapped in '```csharp' and '```' tags. (!IMPORTANT)" +
            "You always answer in " + GPTClient.language;

        private const string PLACEHOLDER_TEXT = "Ask me to create or modify Unity scripts...";
        private const int SHORT_CODE_MAX_LINES = 5; 
        #endregion

        #region UIElements
        // Main layout
        private VisualElement _rootElement;
        private VisualElement _chatPanel;
        private VisualElement _codePanel;
        
        // Chat panel elements
        private ScrollView _chatHistory;
        private VisualElement _inputContainer;
        private TextField _promptField;
        private Button _sendButton;
        private Button _uploadContextButton;
        private Button _clearChatButton;
        private VisualElement _contextFilesContainer;
        private VisualElement _contextFilesScrollView;
        
        // Code panel elements (artifacts)
        private VisualElement _artifactsContainer;
        private ScrollView _artifactsScrollView;
        private Dictionary<int, VisualElement> _artifactElements = new Dictionary<int, VisualElement>();
        
        // Progress indicators
        private VisualElement _progressContainer;
        private VisualElement _progressBar;
        private Label _progressLabel;
        private Button _cancelButton;
        
        // Upload panels
        private VisualElement _uploadPanel;
        private ObjectField _scriptUploadField;
        private TextField _codeUploadField;
        private Button _submitUploadButton;
        private Button _cancelUploadButton;
        #endregion

        #region Private Fields
        private string _currentPrompt = "";
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        private List<ContextFile> _contextFiles = new List<ContextFile>();
        private List<ChatMessage> _chatMessages = new List<ChatMessage>();
        private List<CodeArtifact> _codeArtifacts = new List<CodeArtifact>();
        private MonoScript _uploadedScript;
        private int _nextArtifactId = 1;
        private int _activeArtifactId = 0;
        private VisualElement _streamingMessageContainer = null;
        private Label _streamingMessageText = null;
        private string _streamingBuffer = "";
        private bool _responseFinalizing = false;
        private int _tempArtifactId = -1;
        private DateTime _lastStreamUpdate = DateTime.MinValue;
        private string _pendingStreamContent = "";
        private System.Timers.Timer _streamUpdateTimer = null; 
        private VisualElement _sidebarPanel;
        private Button _toggleSidebarButton;
        private TextField _searchField;
        private ScrollView _chatHistoryList;
        private bool _isSidebarVisible = false;
        private Dictionary<string, Foldout> _dateFoldouts = new Dictionary<string, Foldout>();

        // Current chat session state
        private ChatSession _currentSession;
        private ChatSessionData _allSessions = new ChatSessionData();
        private bool _isNewChat = true;
        #endregion

        public static List<GameObject> SelectedGameObjectsWhenOpened = new List<GameObject>(); 

        #region Data Classes
        [Serializable]
        private class ChatSession
        {
            public string id; 
            public string title; 
            public string timestampStr;
            public List<ChatMessage> messages;
            public List<CodeArtifact> artifacts;
            public List<ContextFile> contextFiles;
            
            [NonSerialized]
            private DateTime _timestamp;
            
            public DateTime timestamp
            {
                get
                {
                    if (_timestamp == default && !string.IsNullOrEmpty(timestampStr))
                    {
                        if (DateTime.TryParse(timestampStr, out DateTime result))
                        {
                            _timestamp = result;
                        }
                        else
                        {
                            _timestamp = DateTime.Now;
                        }
                    }
                    else if (_timestamp == default)
                    {
                        _timestamp = DateTime.Now;
                        timestampStr = _timestamp.ToString("o"); // ISO 8601 format
                    }
                    
                    return _timestamp;
                }
                set
                {
                    _timestamp = value;
                    timestampStr = value.ToString("o"); // ISO 8601 format for reliable parsing
                }
            }
            
            public ChatSession(string title)
            {
                this.id = Guid.NewGuid().ToString();
                this.title = title;
                this.timestamp = DateTime.Now; 
                this.messages = new List<ChatMessage>();
                this.artifacts = new List<CodeArtifact>();
                this.contextFiles = new List<ContextFile>();
            }
        }


        [Serializable]
        private class ChatSessionData
        {
            public List<ChatSession> sessions = new List<ChatSession>();
        }


        [Serializable]
        private class ChatMessage
        {
            public bool isUser;
            public string content;
            public string timestamp;
            public List<CodeBlockReference> codeBlocks;
            public List<InlineCodeBlock> inlineCodeBlocks;
            public bool isSystemMessage;
            public string contextFileId; 

            public ChatMessage(bool isUser, string content)
            {
                this.isUser = isUser;
                this.content = content;
                this.timestamp = DateTime.Now.ToString("HH:mm");
                this.codeBlocks = new List<CodeBlockReference>();
                this.inlineCodeBlocks = new List<InlineCodeBlock>();
                this.isSystemMessage = false; 
                this.contextFileId = null;
            }
        }

        [Serializable]
        private class CodeBlockReference
        {
            public int artifactId;
            public string label;

            public CodeBlockReference(int artifactId, string label)
            {
                this.artifactId = artifactId;
                this.label = label;
            }
        }

        [Serializable]
        private class InlineCodeBlock
        {
            public string code;
            public int position;

            public string originalText;

            public InlineCodeBlock(string code, int position, string originalText)
            {
                this.code = code;
                this.position = position;
                this.originalText = originalText;
            } 
        }

        [Serializable]
        private class CodeArtifact
        {
            public string title;
            public string code;
            public string className;
            public string timestamp;
            public int id;

            public CodeArtifact(string title, string code, string className, int id)
            {
                this.title = title;
                this.code = code;
                this.className = className;
                this.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                this.id = id;
            }
        }

        [Serializable]
        private class ContextFile
        {
            public string filename;
            public string code;
            public int id;
            public string systemMessageId; 

            public VisualElement chatInfoMessage;

            public ContextFile(string filename, string code)
            {
                this.filename = filename;
                this.code = code;
                this.id = UnityEngine.Random.Range(10000, 99999);
                this.systemMessageId = Guid.NewGuid().ToString();
            }
        }

        [Serializable]
        private class ExtractedCodeBlock
        {
            public string code;
            public bool isShort; 

            public ExtractedCodeBlock(string code, bool isShort)
            {
                this.code = code;
                this.isShort = isShort;
            }
        }
        #endregion

        #region Menu Item
        [MenuItem("Tools/uAI Creator/Script Creator", false, 0)]
        public static void Init()
        {
            ScriptCreatorWindow window = (ScriptCreatorWindow)GetWindow(typeof(ScriptCreatorWindow), false, "Script Creator");
            window.minSize = new Vector2(900, 600);
            window.Show();
            SelectedGameObjectsWhenOpened.Clear();
        }
        #endregion

        #region Unity Lifecycle Methods
        private void CreateGUI()
        {
            _rootElement = rootVisualElement;
            
            LoadChatSessions();
             
            
            BuildUIStructure();
            
            InitializeStyles();
            
            SetupEventHandlers();

            InitializeStreamUpdateTimer();
            
            LoadUIState();
            
            EditorApplication.update += UpdateProgress;
        }

        
        private void OnDisable()
        {
            EditorApplication.update -= UpdateProgress;
            
            // Clean up timer
            if (_streamUpdateTimer != null)
            {
                _streamUpdateTimer.Stop();
                _streamUpdateTimer.Dispose();
                _streamUpdateTimer = null;
            }
            
            // Save current session before closing
            if (!_isNewChat && _currentSession != null)
            {
                SaveCurrentSession();
            }
            
            // Save UI state
            SaveUIState();
        }
        #endregion

        #region UI Initialization
        private void BuildUIStructure()
        {
            _rootElement.Clear();
            _contextFiles.Clear();

            var mainLayout = new VisualElement();
            mainLayout.style.flexDirection = FlexDirection.Row;
            mainLayout.style.flexGrow = 1;
            _rootElement.Add(mainLayout);

            BuildSidebarPanel();
            mainLayout.Add(_sidebarPanel);

            _chatPanel = new VisualElement();
            _chatPanel.style.width = new Length(40, LengthUnit.Percent);
            _chatPanel.style.borderRightWidth = 1;
            _chatPanel.style.borderRightColor = new Color(0.2f, 0.2f, 0.2f);
            _chatPanel.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            _chatPanel.style.flexGrow = 0;
            _chatPanel.style.flexShrink = 0;
            mainLayout.Add(_chatPanel);

            _codePanel = new VisualElement();
            _codePanel.style.flexGrow = 1;
            _codePanel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            mainLayout.Add(_codePanel);

            BuildChatPanel();

            BuildCodePanel();

            BuildUploadPanel();

            LoadChatSessions();

            RefreshChatHistoryList();

            if (_currentSession == null || _isNewChat)
            {
                CreateNewChat();
            }


            string currentClassname = EditorPrefs.GetString("PendingComponentClass", "");
            if(currentClassname != "")
            {
                DelayedComponentAdditionStatic();
            }
        }


        private void BuildSidebarPanel()
        {
            _sidebarPanel = new VisualElement();
            _sidebarPanel.style.width = 250;
            _sidebarPanel.style.borderRightWidth = 1;
            _sidebarPanel.style.borderRightColor = new Color(0.2f, 0.2f, 0.2f);
            _sidebarPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            _sidebarPanel.style.display = _isSidebarVisible ? DisplayStyle.Flex : DisplayStyle.None;
            
            // Header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 12;
            header.style.paddingLeft = 12;
            header.style.paddingRight = 12;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            _sidebarPanel.Add(header);
            
            var title = new Label("Chat History");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.95f, 0.95f, 0.95f);
            title.style.flexGrow = 1;
            header.Add(title);
            
            // Search field
            var searchContainer = new VisualElement();
            searchContainer.style.paddingTop = 8;
            searchContainer.style.paddingBottom = 8;
            searchContainer.style.paddingLeft = 12;
            searchContainer.style.paddingRight = 12;
            searchContainer.style.borderBottomWidth = 1;
            searchContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            _sidebarPanel.Add(searchContainer);
            
            _searchField = new TextField();
            _searchField.style.marginBottom = 0;
            _searchField.style.height = 24;
            _searchField.style.minHeight = 24;
            _searchField.Q("unity-text-input").style.paddingLeft = 20;
            

            var searchIcon = new VisualElement();
            searchIcon.style.backgroundImage = EditorGUIUtility.IconContent("d_Search Icon").image as Texture2D;
            searchIcon.style.width = 16;
            searchIcon.style.height = 16;  
            searchIcon.style.marginTop = 2;
            searchIcon.style.marginRight = 2;
            _searchField.Q("unity-text-input").Add(searchIcon);
                
            
            _searchField.RegisterValueChangedCallback(FilterChatHistory);
            searchContainer.Add(_searchField);
            
            
            // Add sort options
            var sortContainer = new VisualElement();
            sortContainer.style.flexDirection = FlexDirection.Row;
            sortContainer.style.justifyContent = Justify.SpaceBetween;
            sortContainer.style.alignItems = Align.Center;
            sortContainer.style.paddingLeft = 12;
            sortContainer.style.paddingRight = 12;
            sortContainer.style.paddingTop = 4;
            sortContainer.style.paddingBottom = 4;
            sortContainer.style.borderBottomWidth = 1;
            sortContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            _sidebarPanel.Add(sortContainer);
            
            var sortLabel = new Label("Sort by:");
            sortLabel.style.fontSize = 12;
            sortLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            sortContainer.Add(sortLabel);
            
            var sortButton = new Button();
            
            sortButton.RegisterCallback<ClickEvent>(evt => ShowSortMenu(sortButton));
            sortButton.text = "Date (Newest) ▼";
            sortButton.style.fontSize = 12;
            sortButton.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            sortButton.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            sortButton.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            sortButton.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);

            sortButton.style.paddingTop = 2;
            sortButton.style.paddingBottom = 2;
            sortContainer.Add(sortButton);
            
           
            // Chat history list
            _chatHistoryList = new ScrollView();
            _chatHistoryList.style.flexGrow = 1;
            _sidebarPanel.Add(_chatHistoryList);
            
            // New chat button
            var newChatButton = new Button(CreateNewChat);
            newChatButton.text = "+ New Chat";
            newChatButton.style.marginTop = 12;
            newChatButton.style.marginLeft = 12;
            newChatButton.style.marginRight = 12;
            newChatButton.style.marginBottom = 12;
            newChatButton.style.minHeight = 20;
            newChatButton.AddToClassList("primary-button");
            _sidebarPanel.Add(newChatButton);

             
            // Add a row of action buttons at the bottom
            var actionButtonsContainer = new VisualElement();
            actionButtonsContainer.name = "action-buttons-container";
            actionButtonsContainer.style.flexDirection = FlexDirection.Row;
            actionButtonsContainer.style.paddingLeft = 12;
            actionButtonsContainer.style.paddingRight = 12;
            actionButtonsContainer.style.paddingBottom = 12;
            actionButtonsContainer.style.minHeight = 40;
            actionButtonsContainer.style.justifyContent = Justify.SpaceBetween;
            _sidebarPanel.Add(actionButtonsContainer);
            
            // Import button
            var importButton = new Button(ImportSession);
            importButton.text = "Import";
            importButton.style.flexGrow = 1;
            importButton.style.marginRight = 4;
            actionButtonsContainer.Add(importButton);
            
            // Export all button
            var exportAllButton = new Button(ExportAllChats);
            exportAllButton.text = "Export All";
            exportAllButton.style.flexGrow = 1;
            exportAllButton.style.marginLeft = 4;
            actionButtonsContainer.Add(exportAllButton); 
        }
         // Method to show sort menu
        void ShowSortMenu(Button sortButton)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Date (Newest)"), false, () => {
                SortChatHistory("newest");
                sortButton.text = "Date (Newest) ▼";
            });
            menu.AddItem(new GUIContent("Date (Oldest)"), false, () => {
                SortChatHistory("oldest");
                sortButton.text = "Date (Oldest) ▼";
            });
            menu.AddItem(new GUIContent("Name"), false, () => {
                SortChatHistory("name");
                sortButton.text = "Name ▼";
            });
            menu.AddItem(new GUIContent("Message Count"), false, () => {
                SortChatHistory("messages");
                sortButton.text = "Messages ▼";
            });
            
            menu.ShowAsContext();
        }
        private void LoadUIState()
        {
            // if (EditorPrefs.HasKey("ScriptCreator_UIState"))
            // {
            //     try
            //     {
            //         string json = EditorPrefs.GetString("ScriptCreator_UIState");
            //         var wrapper = JsonUtility.FromJson<Serializable<Dictionary<string, object>>>(json);
            //         var uiState = wrapper.value;
                    
            //         // Restore sidebar state
            //         if (uiState.TryGetValue("sidebarVisible", out var visibleObj) && visibleObj is bool visible)
            //         {
            //             _isSidebarVisible = visible;
            //             _sidebarPanel.style.display = _isSidebarVisible ? DisplayStyle.Flex : DisplayStyle.None;
            //         }
                    
            //         // Restore current session
            //         if (uiState.TryGetValue("currentSessionId", out var sessionIdObj) && sessionIdObj is string sessionId)
            //         {
            //             // Look for this session ID in loaded sessions
            //             var session = _allSessions.sessions.FirstOrDefault(s => s.id == sessionId);
            //             if (session != null)
            //             {
            //                 LoadChatSession(session);
            //             }
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         Debug.LogError($"Error loading UI state: {ex.Message}");
            //     }
            // }
        }


        private void SaveUIState()
        {
            // Create a DTO for UI state
            var uiState = new Dictionary<string, object>
            {
                { "sidebarVisible", _isSidebarVisible },
                { "currentSessionId", _currentSession?.id },
                { "lastSaveTime", DateTime.Now.ToString("o") }
            };
            
            // Convert to JSON and save
            string json = JsonUtility.ToJson(new Serializable<Dictionary<string, object>>(uiState));
            EditorPrefs.SetString("ScriptCreator_UIState", json);
        }

        
        private void BuildChatPanel()
        { 
            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.justifyContent = Justify.SpaceBetween;
            headerContainer.style.paddingLeft = 12;
            headerContainer.style.paddingRight = 12;
            headerContainer.style.paddingTop = 12;
            headerContainer.style.paddingBottom = 12;
            headerContainer.style.borderBottomWidth = 1;
            headerContainer.style.minHeight = 50;
            headerContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            _chatPanel.Add(headerContainer);
             
            var leftHeaderContainer = new VisualElement();
            leftHeaderContainer.style.flexDirection = FlexDirection.Row;
            leftHeaderContainer.style.alignItems = Align.Center;
            headerContainer.Add(leftHeaderContainer);
             
            _toggleSidebarButton = new Button(ToggleSidebar);
            _toggleSidebarButton.text = "☰";
            _toggleSidebarButton.style.width = 30;
            _toggleSidebarButton.style.height = 30;
            _toggleSidebarButton.style.marginRight = 8;
            _toggleSidebarButton.style.paddingTop = 0;
            _toggleSidebarButton.style.paddingBottom = 0;
            _toggleSidebarButton.style.fontSize = 16;
            leftHeaderContainer.Add(_toggleSidebarButton);
            
            var title = new Label("Chat");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.95f, 0.95f, 0.95f);
            leftHeaderContainer.Add(title);
            
            var buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.Add(buttonsContainer);
            
            _uploadContextButton = new Button(ShowUploadPanel);
            _uploadContextButton.text = "Upload";
            _uploadContextButton.style.marginRight = 8;
            buttonsContainer.Add(_uploadContextButton);
            
            _clearChatButton = new Button(ClearChat);
            _clearChatButton.text = "Clear";
            buttonsContainer.Add(_clearChatButton);
             
            _chatHistory = new ScrollView();
            _chatHistory.style.flexGrow = 1;
            _chatHistory.style.paddingLeft = 12;
            _chatHistory.style.paddingRight = 12;
            _chatHistory.style.paddingTop = 12;
            _chatHistory.style.paddingBottom = 12;
            _chatPanel.Add(_chatHistory);
             
            // Input container
            _inputContainer = new VisualElement();
            _inputContainer.style.paddingLeft = 12;
            _inputContainer.style.paddingRight = 12;
            _inputContainer.style.paddingTop = 12;
            _inputContainer.style.paddingBottom = 12;
            _inputContainer.style.borderTopWidth = 1;
            _inputContainer.style.minHeight = 80;
            _inputContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            _chatPanel.Add(_inputContainer);
            
            // Context files container
            _contextFilesContainer = new VisualElement();
            _contextFilesContainer.style.marginBottom = 8;
            _contextFilesContainer.style.minHeight = 50; 
            _contextFilesContainer.style.flexDirection = FlexDirection.Row;
            _contextFilesContainer.style.display = DisplayStyle.None;
            _inputContainer.Add(_contextFilesContainer);

            _contextFilesScrollView = new ScrollView();
            //horizontal scroll view 
            _contextFilesScrollView.style.flexGrow = 1;
            _contextFilesScrollView.style.flexShrink = 0;
            _contextFilesScrollView.style.overflow = Overflow.Hidden;
            // _contextFilesScrollView.style.width = new Length(100, LengthUnit.Percent);
            _contextFilesScrollView.style.height = 50; 
            _contextFilesScrollView.style.flexDirection = FlexDirection.Row;
            _contextFilesScrollView.contentContainer.style.flexDirection = FlexDirection.Row;
            _contextFilesScrollView.contentContainer.style.flexGrow = 1;
            _contextFilesScrollView.contentContainer.style.flexShrink = 0;

            _contextFilesContainer.Add(_contextFilesScrollView);
            
            _progressContainer = new VisualElement();
            _progressContainer.style.flexDirection = FlexDirection.Row;
            _progressContainer.style.alignItems = Align.Center;
            _progressContainer.style.display = DisplayStyle.None;
            _progressContainer.style.marginBottom = 8;
            _inputContainer.Add(_progressContainer);
            
            _progressLabel = new Label("Generating...");
            _progressLabel.style.marginRight = 8;
            _progressLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            _progressContainer.Add(_progressLabel);
            
            _progressBar = new VisualElement();
            _progressBar.style.flexGrow = 1;
            _progressBar.style.height = 4;
            _progressBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            _progressContainer.Add(_progressBar);
            
            var progressFill = new VisualElement();
            progressFill.name = "progress-fill";
            progressFill.style.width = 0;
            progressFill.style.height = 4;
            progressFill.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _progressBar.Add(progressFill);
            
            _cancelButton = new Button(CancelGeneration);
            _cancelButton.text = "Cancel";
            _cancelButton.style.marginLeft = 8;
            _cancelButton.style.width = 60;
            _progressContainer.Add(_cancelButton);
            
            // Prompt input
            var promptContainer = new VisualElement();
            promptContainer.style.flexDirection = FlexDirection.Row;
            promptContainer.style.minHeight = 60;
            _inputContainer.Add(promptContainer);
            
            _promptField = new TextField();
            _promptField.multiline = true;
            _promptField.style.flexGrow = 1; 
            _promptField.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            _promptField.style.color = new Color(0.9f, 0.9f, 0.9f);
            _promptField.style.borderTopWidth = 1;
            _promptField.style.borderBottomWidth = 1;
            _promptField.style.borderLeftWidth = 1;
            _promptField.style.borderRightWidth = 1;
            _promptField.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            _promptField.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            _promptField.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            _promptField.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            _promptField.style.marginRight = 8;
            
            var textInput = _promptField.Q(TextField.textInputUssName);
            textInput.style.alignSelf = Align.Auto;
            textInput.style.color = new Color(0.9f, 0.9f, 0.9f);
            textInput.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            textInput.style.whiteSpace = WhiteSpace.Normal;
            textInput.style.overflow = Overflow.Visible;
            textInput.style.flexWrap = Wrap.Wrap;
            
            _promptField.value = DEFAULT_INITIAL_PROMPT;
            _currentPrompt = DEFAULT_INITIAL_PROMPT;
            promptContainer.Add(_promptField);
            
            _sendButton = new Button(SendPrompt);
            _sendButton.text = "Send";
            _sendButton.AddToClassList("primary-button");
            _sendButton.style.alignSelf = Align.FlexEnd;
            _sendButton.style.width = 40;
            _sendButton.style.height = 60;
            promptContainer.Add(_sendButton);
        }
        private void BuildCodePanel()
        {
            // Container for artifacts
            _artifactsContainer = new VisualElement();
            _artifactsContainer.style.flexGrow = 1;
            _codePanel.Add(_artifactsContainer);
            
            // Header
            var header = new VisualElement();
            header.style.paddingLeft = 16;
            header.style.paddingRight = 16;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 12;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.25f, 0.25f, 0.25f);
            _artifactsContainer.Add(header);
            
            var title = new Label("Code Artifacts");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.95f, 0.95f, 0.95f);
            header.Add(title);
            
            // Artifacts scroll view
            _artifactsScrollView = new ScrollView();
            _artifactsScrollView.style.flexGrow = 1;
            _artifactsScrollView.style.paddingLeft = 16;
            _artifactsScrollView.style.paddingRight = 16;
            _artifactsScrollView.style.paddingTop = 16;
            _artifactsScrollView.style.paddingBottom = 16;
            _artifactsContainer.Add(_artifactsScrollView);
            
            // Placeholder message
            var placeholder = new Label("No code artifacts yet. Ask me to create a script!");
            placeholder.style.color = new Color(0.7f, 0.7f, 0.7f);
            placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            placeholder.style.marginTop = 50;
            _artifactsScrollView.Add(placeholder);
        }
        
        private void BuildUploadPanel()
        {
            _uploadPanel = new VisualElement();
            _uploadPanel.style.position = Position.Absolute;
            _uploadPanel.style.top = 0;
            _uploadPanel.style.bottom = 0;
            _uploadPanel.style.left = 0;
            _uploadPanel.style.right = 0;
            _uploadPanel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            _uploadPanel.style.justifyContent = Justify.Center;
            _uploadPanel.style.alignItems = Align.Center;
            _uploadPanel.style.display = DisplayStyle.None;
            _rootElement.Add(_uploadPanel);
            
            // Upload dialog
            var dialog = new VisualElement();
            dialog.style.width = 500;
            dialog.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            dialog.style.borderTopWidth = 1;
            dialog.style.borderBottomWidth = 1;
            dialog.style.borderLeftWidth = 1;
            dialog.style.borderRightWidth = 1;
            dialog.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            dialog.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            dialog.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            dialog.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            dialog.style.paddingTop = 16;
            dialog.style.paddingBottom = 16;
            dialog.style.paddingLeft = 16;
            dialog.style.paddingRight = 16;
            _uploadPanel.Add(dialog);
            
            // Header
            var header = new Label("Upload Code for Context");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.95f, 0.95f, 0.95f);
            header.style.marginBottom = 16;
            dialog.Add(header);
            
            // Content
            var content = new VisualElement();
            content.style.marginBottom = 16;
            dialog.Add(content);
            
            var description = new Label("Upload a script file or paste code to provide context to the AI.");
            description.style.color = new Color(0.8f, 0.8f, 0.8f);
            description.style.marginBottom = 16;
            content.Add(description);
            
            // Script file upload
            var fileUploadLabel = new Label("Select a script file:");
            fileUploadLabel.style.marginBottom = 4;
            fileUploadLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
            content.Add(fileUploadLabel);
            
            _scriptUploadField = new ObjectField();
            _scriptUploadField.objectType = typeof(MonoScript);
            _scriptUploadField.style.marginBottom = 16;
            content.Add(_scriptUploadField);
            
            // Or separator
            var separator = new VisualElement();
            separator.style.flexDirection = FlexDirection.Row;
            separator.style.alignItems = Align.Center;
            separator.style.marginBottom = 16;
            content.Add(separator);
            
            var line1 = new VisualElement();
            line1.style.flexGrow = 1;
            line1.style.height = 1;
            line1.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            separator.Add(line1);
            
            var orLabel = new Label("OR");
            orLabel.style.marginLeft = 8;
            orLabel.style.marginRight = 8;
            orLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            separator.Add(orLabel);
            
            var line2 = new VisualElement();
            line2.style.flexGrow = 1;
            line2.style.height = 1;
            line2.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            separator.Add(line2);
            
            // Paste code
            var pasteLabel = new Label("Paste code:");
            pasteLabel.style.marginBottom = 4;
            pasteLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
            content.Add(pasteLabel);
            
            _codeUploadField = new TextField();
            _codeUploadField.multiline = true;
            _codeUploadField.style.height = 200;
            _codeUploadField.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            _codeUploadField.style.color = new Color(0.9f, 0.9f, 0.9f);
            _codeUploadField.style.borderTopWidth = 1;
            _codeUploadField.style.borderBottomWidth = 1;
            _codeUploadField.style.borderLeftWidth = 1;
            _codeUploadField.style.borderRightWidth = 1;
            _codeUploadField.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            _codeUploadField.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            _codeUploadField.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            _codeUploadField.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            
            // Fix text input alignment
            _codeUploadField.Q(TextField.textInputUssName).style.alignSelf = Align.Auto;
            _codeUploadField.Q(TextField.textInputUssName).style.color = new Color(0.9f, 0.9f, 0.9f);
            _codeUploadField.Q(TextField.textInputUssName).style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            
            content.Add(_codeUploadField);
            
            // Buttons
            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.FlexEnd;
            dialog.Add(buttons);
            
            _cancelUploadButton = new Button(HideUploadPanel);
            _cancelUploadButton.text = "Cancel";
            _cancelUploadButton.style.marginRight = 8;
            buttons.Add(_cancelUploadButton);
            
            _submitUploadButton = new Button(SubmitCodeUpload);
            _submitUploadButton.text = "Add Context";
            _submitUploadButton.AddToClassList("primary-button");
            buttons.Add(_submitUploadButton);
        }
        
        private void InitializeStyles()
        {
            // Primary button styles
            _rootElement.Query<Button>().Class("primary-button").ForEach((button) => {
                button.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
                button.style.color = Color.white;
                button.style.borderTopWidth = 0;
                button.style.borderBottomWidth = 0;
                button.style.borderLeftWidth = 0;
                button.style.borderRightWidth = 0;
            });
            
            // Set up hover effects
            _sendButton.RegisterCallback<MouseEnterEvent>(evt => {
                _sendButton.style.backgroundColor = new StyleColor(new Color(0.27f, 0.56f, 0.88f));
            });
            _sendButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _sendButton.style.backgroundColor = new StyleColor(new Color(0.35f, 0.65f, 0.9f));
            });
            
            _submitUploadButton.RegisterCallback<MouseEnterEvent>(evt => {
                _submitUploadButton.style.backgroundColor = new StyleColor(new Color(0.27f, 0.56f, 0.88f));
            });
            _submitUploadButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _submitUploadButton.style.backgroundColor = new StyleColor(new Color(0.35f, 0.65f, 0.9f));
            });
            
            // Style the sidebar toggle button
            _toggleSidebarButton.RegisterCallback<MouseEnterEvent>(evt => {
                _toggleSidebarButton.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
            });
            _toggleSidebarButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _toggleSidebarButton.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            });
            
            // Style foldouts in the sidebar
            _rootElement.Query<Foldout>().ForEach((foldout) => {
                // Style the toggle
                var toggle = foldout.Q<Toggle>();
                if (toggle != null)
                {
                    // Make toggle text color brighter
                    var toggleLabel = toggle.Q<Label>();
                    if (toggleLabel != null)
                    {
                        toggleLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
                        toggleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    }
                }
            });
            
            // Style the search field
            if (_searchField != null)
            {
                var textInput = _searchField.Q(TextField.textInputUssName);
                textInput.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                textInput.style.color = new Color(0.9f, 0.9f, 0.9f);
            }
        }

        
        private void SetupEventHandlers()
        {
            // Prompt field event
            _promptField.RegisterValueChangedCallback(e => _currentPrompt = e.newValue);
            
            // Set up Enter key for sending
            _promptField.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return && evt.shiftKey)
                {
                    // Let Shift+Enter add a newline
                    return;
                }
                else if (evt.keyCode == KeyCode.Return)
                {
                    evt.StopPropagation();
                    SendPrompt();
                }
            });
            
            // Add keyboard shortcut for toggling sidebar (Ctrl+B)
            _rootElement.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.ctrlKey && evt.keyCode == KeyCode.B)
                {
                    evt.StopPropagation();
                    ToggleSidebar();
                }
            });
            
            // Add keyboard shortcut for new chat (Ctrl+N)
            _rootElement.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.ctrlKey && evt.keyCode == KeyCode.N)
                {
                    evt.StopPropagation();
                    CreateNewChat();
                }
            });
            
            // Add keyboard shortcut for search focus (Ctrl+F)
            _rootElement.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.ctrlKey && evt.keyCode == KeyCode.F && _isSidebarVisible)
                {
                    evt.StopPropagation();
                    _searchField.Focus();
                }
            });
        }


        private void InitializeStreamUpdateTimer()
        {
            _streamUpdateTimer = new System.Timers.Timer(50); 
            _streamUpdateTimer.AutoReset = true;
            _streamUpdateTimer.Elapsed += (sender, e) => {
                if (!string.IsNullOrEmpty(_pendingStreamContent))
                {
                    string contentToProcess;
                    lock (_pendingStreamContent)
                    {
                        contentToProcess = _pendingStreamContent;
                        _pendingStreamContent = "";
                    }

                    if (!string.IsNullOrEmpty(contentToProcess))
                    {
                        EditorApplication.delayCall += () => {
                            if (!_isGenerating || _responseFinalizing) return;
                            
                            _streamingBuffer += contentToProcess;
                            
                            var currentContent = _streamingBuffer;
                            string extractedCode = TryExtractPartialCode(currentContent);
                            
                            string displayContent = ProcessStreamingContentForDisplay(currentContent);
                            UpdateStreamingMessage(displayContent);
                            
                            if (!string.IsNullOrEmpty(extractedCode))
                            {
                                UpdateTemporaryArtifact(extractedCode);
                            }
                        };
                    }
                }
            };
            _streamUpdateTimer.Start();
        }
        #endregion

        #region Chat History Management
        private void ExportAllChats()
        {
            string path = EditorUtility.SaveFilePanel(
                "Export All Chats", 
                "", 
                "script_creator_chats_" + DateTime.Now.ToString("yyyy-MM-dd") + ".json", 
                "json"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = JsonUtility.ToJson(_allSessions, true);
                    File.WriteAllText(path, json);
                    EditorUtility.DisplayDialog("Export Successful", 
                        $"All {_allSessions.sessions.Count} chat sessions exported to {path}", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Export Failed", $"Error exporting sessions: {ex.Message}", "OK");
                }
            }
        }

        private void CreateNewChat()
        {
            // Save current session if needed
            if (!_isNewChat && _currentSession != null)
            {
                SaveCurrentSession();
            }
            
            _chatHistory.Clear();
            _artifactsScrollView.Clear();
            _artifactElements.Clear();
            
            _chatMessages = new List<ChatMessage>();
            _codeArtifacts = new List<CodeArtifact>();
            _contextFiles = new List<ContextFile>();
            
            _artifactElements = new Dictionary<int, VisualElement>();
            _activeArtifactId = 0;
            _nextArtifactId = 1;
            
            RefreshContextFilesUI();
            
            _currentSession = new ChatSession("New Chat");
            _isNewChat = true;
            
            var welcomeMessage = "Hello! I'm your AI assistant. I can help you create and modify Unity scripts. What would you like me to do?";
            AddSystemMessage(welcomeMessage);
 
            _promptField.value = DEFAULT_INITIAL_PROMPT;
            
            var placeholder = new Label("No code artifacts yet. Ask me to create a script!");
            placeholder.style.color = new Color(0.7f, 0.7f, 0.7f);
            placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            placeholder.style.marginTop = 50;
            _artifactsScrollView.Add(placeholder);
            
            UpdateChatTitle(); 
        }
 

        private void SaveCurrentSession()
        {
            if (_currentSession == null) return;
             
            _currentSession.messages = new List<ChatMessage>(_chatMessages);
            _currentSession.artifacts = new List<CodeArtifact>(_codeArtifacts);
             
            int existingIndex = _allSessions.sessions.FindIndex(s => s.id == _currentSession.id);
            if (existingIndex >= 0)
            { 
                _allSessions.sessions[existingIndex] = DeepCopySession(_currentSession);
            }
            else
            { 
                _allSessions.sessions.Add(DeepCopySession(_currentSession));
            }
             
            SaveChatSessions(); 
            RefreshChatHistoryList(); 
        }


        private void LoadChatSessions()
        {
            string path = GetChatSessionsFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    _allSessions = JsonUtility.FromJson<ChatSessionData>(json);
                     
                    foreach (var session in _allSessions.sessions)
                    { 
                        DateTime time = session.timestamp;
                         
                        if (time.Year < 2020)
                        { 
                            session.timestamp = DateTime.Now;
                            Debug.LogWarning($"Reset invalid timestamp for session: {session.title}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading chat sessions: {ex.Message}");
                    _allSessions = new ChatSessionData();
                }
            }
            else
            {
                _allSessions = new ChatSessionData();
            }
        }



        private void SaveChatSessions()
        {
            string path = GetChatSessionsFilePath();
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                 
                foreach (var session in _allSessions.sessions)
                { 
                    DateTime existingTime = session.timestamp;
                    session.timestamp = existingTime;
                }
                
                string json = JsonUtility.ToJson(_allSessions, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving chat sessions: {ex.Message}");
            }
        }



        private string GetChatSessionsFilePath()
        { 
            string prefsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Unity", "Editor", "ScriptCreator"
            );
            
            return Path.Combine(prefsPath, "chat_sessions.json");
        }

        private void RefreshChatHistoryList()
        {
            _chatHistoryList.Clear();
            _dateFoldouts.Clear();
             
            var sessionsByDate = _allSessions.sessions
                .OrderByDescending(s => s.timestamp)
                .GroupBy(s => s.timestamp.Date);
            
            foreach (var group in sessionsByDate)
            {
                DateTime date = group.Key;
                string dateKey = date.ToString("yyyy-MM-dd");
                string dateLabel = date.Date.Equals(DateTime.Today) 
                    ? "Today" 
                    : date.Date.Equals(DateTime.Today.AddDays(-1)) 
                        ? "Yesterday" 
                        : date.ToString("MMMM d, yyyy");
                 
                var foldout = new Foldout();
                foldout.text = dateLabel;
                foldout.value = true;  
                foldout.style.marginLeft = 0;
                foldout.style.marginRight = 0;
                foldout.style.paddingLeft = 12;
                foldout.style.paddingRight = 12;
                
                _chatHistoryList.Add(foldout);
                _dateFoldouts[dateKey] = foldout;
                 
                foreach (var session in group)
                {
                    AddSessionToList(session, foldout);
                }
            }
             
            if (_allSessions.sessions.Count == 0)
            {
                var noChats = new Label("No saved chats yet");
                noChats.style.color = new Color(0.7f, 0.7f, 0.7f);
                noChats.style.unityTextAlign = TextAnchor.MiddleCenter;
                noChats.style.paddingTop = 20;
                _chatHistoryList.Add(noChats);
            }
        }


        private void AddSessionToList(ChatSession session, Foldout parentFoldout)
        {
            var sessionItem = new VisualElement();
            sessionItem.name = "session-" + session.id;
            sessionItem.style.flexDirection = FlexDirection.Row;
            sessionItem.style.paddingTop = 6;
            sessionItem.style.paddingBottom = 6;
            sessionItem.style.paddingLeft = 8;
            sessionItem.style.paddingRight = 8;
            sessionItem.style.marginTop = 4;
            sessionItem.style.marginBottom = 4;
            sessionItem.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            sessionItem.style.borderTopWidth = 1;
            sessionItem.style.borderBottomWidth = 1;
            sessionItem.style.borderLeftWidth = 1;
            sessionItem.style.borderRightWidth = 1;
            sessionItem.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            sessionItem.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            sessionItem.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            sessionItem.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            
            // Highlight if this is the current session
            if (_currentSession != null && _currentSession.id == session.id)
            {
                sessionItem.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
            }
            
            // Make the whole item clickable
            var clickable = new Clickable(() => LoadChatSession(session));
            sessionItem.AddManipulator(clickable);
            
            // Add hover effect
            sessionItem.RegisterCallback<MouseEnterEvent>(evt => {
                if (_currentSession == null || _currentSession.id != session.id)
                    sessionItem.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
            });
            sessionItem.RegisterCallback<MouseLeaveEvent>(evt => {
                if (_currentSession == null || _currentSession.id != session.id)
                    sessionItem.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                else
                    sessionItem.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
            });
            
            // Info container (title and time)
            var infoContainer = new VisualElement();
            infoContainer.style.flexGrow = 1;
            sessionItem.Add(infoContainer);
            
            // Title
            var titleLabel = new Label(session.title);
            titleLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
            titleLabel.style.fontSize = 12;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            titleLabel.style.overflow = Overflow.Hidden;
            titleLabel.style.textOverflow = TextOverflow.Ellipsis;
            infoContainer.Add(titleLabel);
            
            // Time and message count
            var timeLabel = new Label($"{session.timestamp.ToString("h:mm tt")} · {session.messages.Count} messages");
            timeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            timeLabel.style.fontSize = 10;
            infoContainer.Add(timeLabel);
             
            
            // Add context menu
            AddContextMenu(sessionItem, session);
            
            parentFoldout.Add(sessionItem);
        }


        private void LoadChatSession(ChatSession session)
        {
            // Save current session if needed
            if (!_isNewChat && _currentSession != null && _currentSession.id != session.id)
            {
                SaveCurrentSession();
            }
             
            // Clear UI
            _chatHistory.Clear();
            _artifactsScrollView.Clear();
            _artifactElements.Clear();
            
            // Clear data collections
            _chatMessages = new List<ChatMessage>();  
            _codeArtifacts = new List<CodeArtifact>();  
            
            // Clear context files UI
            _contextFiles.Clear();
            RefreshContextFilesUI();
            
            // Reset active artifact ID
            _activeArtifactId = 0; 
             
            _currentSession = DeepCopySession(session);
            _isNewChat = false;
             
            foreach (var message in _currentSession.messages)
            {
                _chatMessages.Add(message);
            }
             
            Dictionary<string, ContextFile> contextFilesById = new Dictionary<string, ContextFile>();
             
            if (_currentSession.contextFiles != null && _currentSession.contextFiles.Count > 0)
            { 
                foreach (var contextFile in _currentSession.contextFiles)
                {
                    contextFilesById[contextFile.id.ToString()] = contextFile;
                }
            }
             
            foreach (var message in _chatMessages)
            {
                if (message.isUser)
                { 
                    AddUserMessageUI(message);
                }
                else if (message.isSystemMessage)
                { 
                    ContextFile linkedContextFile = null;
                    if (!string.IsNullOrEmpty(message.contextFileId) && 
                        contextFilesById.TryGetValue(message.contextFileId, out linkedContextFile))
                    { 
                        var systemMessageElement = AddSystemMessageUI(message, linkedContextFile);
                         
                        linkedContextFile.chatInfoMessage = systemMessageElement;
                    }
                    else
                    { 
                        AddSystemMessageUI(message);
                    }
                }
                else
                { 
                    AddAIMessageUI(message);
                }
            }
             
            if (_currentSession.artifacts != null)
            {
                _codeArtifacts = new List<CodeArtifact>(_currentSession.artifacts);
                _nextArtifactId = _codeArtifacts.Count > 0 ? 
                    _codeArtifacts.Max(a => a.id) + 1 : 1;
            }
            else
            {
                _codeArtifacts = new List<CodeArtifact>();
                _nextArtifactId = 1;
            }
             
            if (_codeArtifacts.Count > 0)
            {
                foreach (var artifact in _codeArtifacts)
                {
                    CreateArtifactElement(artifact);
                }
                 
                ShowCodeArtifact(_codeArtifacts.First().id);
            }
            else
            { 
                var placeholder = new Label("No code artifacts yet. Ask me to create a script!");
                placeholder.style.color = new Color(0.7f, 0.7f, 0.7f);
                placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
                placeholder.style.marginTop = 50;
                _artifactsScrollView.Add(placeholder);
            }
             
            UpdateChatTitle();
             
            EditorApplication.delayCall += () => {
                _chatHistory.scrollOffset = new Vector2(0, float.MaxValue);
            };
             
            RefreshChatHistoryList(); 
        }

        private ChatSession DeepCopySession(ChatSession original)
        { 
            var copy = new ChatSession(original.title);
             
            copy.id = original.id;
            copy.timestamp = original.timestamp;
             
            copy.messages.Clear();
            copy.artifacts.Clear();
            copy.contextFiles.Clear();
             
            if (original.messages != null)
            {
                foreach (var message in original.messages)
                {
                    var messageCopy = new ChatMessage(message.isUser, message.content); 
                    messageCopy.timestamp = message.timestamp;
                    messageCopy.isSystemMessage = message.isSystemMessage;
                    messageCopy.contextFileId = message.contextFileId;
                     
                    if (message.codeBlocks != null)
                    {
                        messageCopy.codeBlocks = new List<CodeBlockReference>();
                        foreach (var block in message.codeBlocks)
                        {
                            messageCopy.codeBlocks.Add(new CodeBlockReference(block.artifactId, block.label));
                        }
                    }
                     
                    if (message.inlineCodeBlocks != null)
                    {
                        messageCopy.inlineCodeBlocks = new List<InlineCodeBlock>();
                        foreach (var block in message.inlineCodeBlocks)
                        {
                            messageCopy.inlineCodeBlocks.Add(
                                new InlineCodeBlock(block.code, block.position, block.originalText)
                            );
                        }
                    }
                    
                    copy.messages.Add(messageCopy);
                }
            }
             
            if (original.artifacts != null)
            {
                foreach (var artifact in original.artifacts)
                {
                    var artifactCopy = new CodeArtifact(
                        artifact.title, 
                        artifact.code, 
                        artifact.className, 
                        artifact.id
                    );
                    artifactCopy.timestamp = artifact.timestamp;
                    copy.artifacts.Add(artifactCopy);
                }
            }
             
            if (original.contextFiles != null)
            {
                foreach (var file in original.contextFiles)
                {
                    var fileCopy = new ContextFile(file.filename, file.code);
                    fileCopy.id = file.id;
                    fileCopy.systemMessageId = file.systemMessageId;
                    copy.contextFiles.Add(fileCopy);
                }
            }
            
            return copy;
        }


        private bool IsWelcomeMessage(string content)
        {
            return 
                content.Contains("Hello! I'm your AI assistant") ||
                content.Contains("I can help you create") ||
                content.StartsWith("Welcome to") ||
                content.Contains("I'm your AI") && content.Contains("assistant");
        }

        private bool IsSystemMessage(string content)
        { 
            return 
                content.StartsWith("[SYSTEM]") ||
                content.StartsWith("Hello! I'm your AI assistant") ||
                content.StartsWith("Welcome to the Script Creator tool") ||
                content.Contains("Chat cleared.") ||
                content.StartsWith("Added code from") && content.EndsWith("to context.") ||
                content.StartsWith("Created new script:") ||
                content.StartsWith("Overwritten existing script:") ||
                content.StartsWith("Generation canceled");
        }
        private VisualElement AddSystemMessageUI(ChatMessage message, ContextFile contextFile = null)
        {
            var messageContainer = new VisualElement();
            messageContainer.style.marginBottom = 16;
            messageContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            messageContainer.style.borderLeftWidth = 3;
            messageContainer.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
            messageContainer.style.paddingTop = 8;
            messageContainer.style.paddingBottom = 8;
            messageContainer.style.paddingLeft = 12;
            messageContainer.style.paddingRight = 12;
            _chatHistory.Add(messageContainer);
             
            string displayContent = message.content;
            if (displayContent.StartsWith("[SYSTEM] "))
            {
                displayContent = displayContent.Substring(9);
            }
             
            if (contextFile != null)
            {
                messageContainer.AddManipulator(new Clickable(() => {
                    ShowContextFileInArtifact(contextFile);
                }));
                 
                messageContainer.RegisterCallback<MouseEnterEvent>(evt => {
                    messageContainer.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 0.3f);
                    EditorGUIUtility.AddCursorRect(messageContainer.worldBound, MouseCursor.Link);
                });
                messageContainer.RegisterCallback<MouseLeaveEvent>(evt => {
                    messageContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                });
                 
                var messageContent = new VisualElement();
                messageContent.style.flexDirection = FlexDirection.Row;
                messageContent.style.alignItems = Align.Center;
                messageContainer.Add(messageContent);
                
                var icon = new VisualElement();
                icon.style.width = 16;
                icon.style.height = 16;
                icon.style.marginRight = 8;
                icon.style.backgroundImage = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
                messageContent.Add(icon);
                
                var messageText = new Label(displayContent);
                messageText.style.whiteSpace = WhiteSpace.Normal;
                messageText.style.color = new Color(0.85f, 0.85f, 0.85f);
                messageContent.Add(messageText);
            }
            else
            {
                var messageText = new Label(displayContent);
                messageText.style.whiteSpace = WhiteSpace.Normal;
                messageText.style.color = new Color(0.85f, 0.85f, 0.85f);
                messageContainer.Add(messageText);
            }
            
            return messageContainer;
        }



        
        private void AddUserMessageUI(ChatMessage message)
        {
            var messageContainer = new VisualElement();
            messageContainer.style.marginBottom = 16;
            messageContainer.style.flexDirection = FlexDirection.Row;
            _chatHistory.Add(messageContainer);
            
            // Avatar/icon
            var avatar = new VisualElement();
            avatar.style.width = 24;
            avatar.style.height = 24;
            avatar.style.borderTopLeftRadius = 12;
            avatar.style.borderTopRightRadius = 12;
            avatar.style.borderBottomLeftRadius = 12;
            avatar.style.borderBottomRightRadius = 12;
            avatar.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            avatar.style.marginRight = 8;
            avatar.style.marginTop = 4;
            avatar.style.flexShrink = 0;
            avatar.style.paddingTop = 2;
            avatar.style.paddingLeft = 2;
            messageContainer.Add(avatar);
            
            // Label "User" inside avatar
            var avatarLabel = new Label("U");
            avatarLabel.style.color = Color.white;
            avatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            avatarLabel.style.fontSize = 12;
            avatarLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            avatar.Add(avatarLabel);
            
            // Content
            var content = new VisualElement();
            content.style.flexGrow = 1;
            messageContainer.Add(content);
            
            // Message header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 4;
            content.Add(header);
            
            var nameLabel = new Label("You");
            nameLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.fontSize = 13;
            header.Add(nameLabel);
            
            var timestamp = new Label(message.timestamp);
            timestamp.style.color = new Color(0.6f, 0.6f, 0.6f);
            timestamp.style.fontSize = 11;
            timestamp.style.marginLeft = 8;
            header.Add(timestamp);
            
            // Message text
            var messageText = new Label(message.content);
            messageText.style.whiteSpace = WhiteSpace.Normal;
            messageText.style.color = new Color(0.9f, 0.9f, 0.9f);
            content.Add(messageText);
        }
 
        private void AddAIMessageUI(ChatMessage message)
        {
            var messageContainer = new VisualElement();
            messageContainer.style.marginBottom = 16;
            messageContainer.style.flexDirection = FlexDirection.Row;
            _chatHistory.Add(messageContainer);
            
            // Avatar/icon
            var avatar = new VisualElement();
            avatar.style.width = 24;
            avatar.style.height = 24;
            avatar.style.borderTopLeftRadius = 12;
            avatar.style.borderTopRightRadius = 12;
            avatar.style.borderBottomLeftRadius = 12;
            avatar.style.borderBottomRightRadius = 12;
            avatar.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            avatar.style.marginRight = 8;
            avatar.style.marginTop = 4;
            avatar.style.flexShrink = 0;
            avatar.style.paddingTop = 2;
            avatar.style.paddingLeft = 2;
            messageContainer.Add(avatar);
            
            // Label "AI" inside avatar
            var avatarLabel = new Label("AI");
            avatarLabel.style.color = Color.white;
            avatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            avatarLabel.style.fontSize = 10; 
            avatarLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            avatar.Add(avatarLabel);
            
            // Content
            var content = new VisualElement();
            content.style.flexGrow = 1;
            messageContainer.Add(content);
            
            // Message header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 4;
            content.Add(header);
            
            var nameLabel = new Label("AI Assistant");
            nameLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.fontSize = 13;
            header.Add(nameLabel);
            
            var timestamp = new Label(message.timestamp);
            timestamp.style.color = new Color(0.6f, 0.6f, 0.6f);
            timestamp.style.fontSize = 11;
            timestamp.style.marginLeft = 8;
            header.Add(timestamp);
             
            string processedMessage = message.content;
             
            if (message.codeBlocks.Count > 0 || message.inlineCodeBlocks.Count > 0)
            {
                var messageContent = new VisualElement();
                messageContent.style.flexGrow = 1;
                 
                var allCodePositions = new List<(int position, bool isInline, int index, int length)>();
                 
                foreach (var match in System.Text.RegularExpressions.Regex.Matches(processedMessage, @"\[Code Block\]").Cast<System.Text.RegularExpressions.Match>())
                {
                    allCodePositions.Add((match.Index, false, allCodePositions.Count(p => !p.isInline), match.Length));
                }
                 
                if (message.inlineCodeBlocks.Count > 0)
                {
                    foreach (var inlineBlock in message.inlineCodeBlocks)
                    { 
                        if (inlineBlock.position >= 0 && inlineBlock.originalText != null)
                        {
                            allCodePositions.Add((inlineBlock.position, true, message.inlineCodeBlocks.IndexOf(inlineBlock), inlineBlock.originalText.Length));
                        }
                    }
                }
                 
                allCodePositions.Sort((a, b) => a.position.CompareTo(b.position));
                 
                int lastIndex = 0;
                foreach (var codePos in allCodePositions)
                { 
                    if (codePos.position > lastIndex)
                    {
                        string beforeText = processedMessage.Substring(lastIndex, codePos.position - lastIndex);
                        if (!string.IsNullOrWhiteSpace(beforeText))
                        {
                            var textElement = CreateMarkdownStyledText(beforeText);
                            messageContent.Add(textElement);
                        }
                    }
                    
                    if (codePos.isInline)
                    { 
                        var inlineCode = message.inlineCodeBlocks[codePos.index];
                        
                        var codeContainer = new VisualElement();
                        codeContainer.style.marginTop = 8;
                        codeContainer.style.marginBottom = 8;
                        codeContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
                        codeContainer.style.borderTopWidth = 1;
                        codeContainer.style.borderBottomWidth = 1;
                        codeContainer.style.borderLeftWidth = 1;
                        codeContainer.style.borderRightWidth = 1;
                        codeContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
                        codeContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                        codeContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                        codeContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
                        
                        // Add a header with a copy button
                        var codeHeader = new VisualElement();
                        codeHeader.style.flexDirection = FlexDirection.Row;
                        codeHeader.style.justifyContent = Justify.SpaceBetween;
                        codeHeader.style.paddingLeft = 8;
                        codeHeader.style.paddingRight = 8;
                        codeHeader.style.paddingTop = 4;
                        codeHeader.style.paddingBottom = 4;
                        codeHeader.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
                        codeHeader.style.borderBottomWidth = 1;
                        codeHeader.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                        codeContainer.Add(codeHeader);
                        
                        // Add a title/comment if available in the first line
                        string firstLine = inlineCode.code.Split('\n')[0].Trim();
                        string title = firstLine.StartsWith("//") ? firstLine : "Code";
                        var titleLabel = new Label(title);
                        titleLabel.style.fontSize = 12;
                        titleLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                        codeHeader.Add(titleLabel);
                        
                        // Add copy button
                        var copyButton = new Button(() => CopyToClipboard(inlineCode.code));
                        copyButton.text = "Copy";
                        copyButton.style.height = 20;
                        copyButton.style.paddingTop = 2;
                        copyButton.style.paddingBottom = 2;
                        codeHeader.Add(copyButton);
                        
                        // Code content
                        var codeContent = new VisualElement();
                        codeContent.style.paddingLeft = 12;
                        codeContent.style.paddingRight = 12;
                        codeContent.style.paddingTop = 8;
                        codeContent.style.paddingBottom = 8;
                        codeContainer.Add(codeContent);
                        
                        try {
                            var highlighted = UISyntaxHighlighter.CreateHighlightedCodeElement(inlineCode.code);
                            codeContent.Add(highlighted);
                        }
                        catch (System.Exception) {
                            // Fallback if highlighting fails
                            var codeText = new Label(inlineCode.code);
                            codeText.style.whiteSpace = WhiteSpace.Normal;
                            codeText.style.color = new Color(0.7f, 0.9f, 0.7f);
                            codeText.style.unityFontStyleAndWeight = FontStyle.Bold;
                            codeText.style.fontSize = 12;
                            codeContent.Add(codeText);
                        }
                        
                        messageContent.Add(codeContainer);
                    }
                    else if (codePos.index < message.codeBlocks.Count)
                    {
                        // Add code block button
                        var blockRef = message.codeBlocks[codePos.index];
                        var codeButton = new Button(() => ShowCodeArtifact(blockRef.artifactId));
                        codeButton.text = blockRef.label;
                        codeButton.style.marginTop = 4;
                        codeButton.style.marginBottom = 4;
                        codeButton.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                        codeButton.style.borderTopWidth = 1;
                        codeButton.style.borderBottomWidth = 1;
                        codeButton.style.borderLeftWidth = 1;
                        codeButton.style.borderRightWidth = 1;
                        codeButton.style.borderTopColor = new Color(0.35f, 0.35f, 0.35f);
                        codeButton.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);
                        codeButton.style.borderLeftColor = new Color(0.35f, 0.35f, 0.35f);
                        codeButton.style.borderRightColor = new Color(0.35f, 0.35f, 0.35f);
                        codeButton.style.paddingTop = 4;
                        codeButton.style.paddingBottom = 4;
                        codeButton.style.paddingLeft = 8;
                        codeButton.style.paddingRight = 8;
                        messageContent.Add(codeButton);
                    }
                    
                    lastIndex = codePos.position + codePos.length;
                }
                
                if (lastIndex < processedMessage.Length)
                {
                    string afterText = processedMessage.Substring(lastIndex);
                    if (!string.IsNullOrWhiteSpace(afterText))
                    {
                        var textElement = CreateMarkdownStyledText(afterText);
                        messageContent.Add(textElement);
                    }
                }
                
                content.Add(messageContent);
            }
            else
            {
                var messageContent = CreateMarkdownStyledText(processedMessage);
                content.Add(messageContent);
            }
        }

        private void AddSystemMessageUI(ChatMessage message)
        {
            var messageContainer = new VisualElement();
            messageContainer.style.marginBottom = 16;
            messageContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            messageContainer.style.borderLeftWidth = 3;
            messageContainer.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
            messageContainer.style.paddingTop = 8;
            messageContainer.style.paddingBottom = 8;
            messageContainer.style.paddingLeft = 12;
            messageContainer.style.paddingRight = 12;
            _chatHistory.Add(messageContainer);
            
            string displayContent = message.content;
            if (displayContent.StartsWith("[SYSTEM] "))
            {
                displayContent = displayContent.Substring(9);
            }
            
            var messageText = new Label(displayContent);
            messageText.style.whiteSpace = WhiteSpace.Normal;
            messageText.style.color = new Color(0.85f, 0.85f, 0.85f);
            messageContainer.Add(messageText);
        }

        private void AddContextMenu(VisualElement sessionItem, ChatSession session)
        {
            sessionItem.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) => {
                evt.menu.AppendAction("Rename", (a) => RenameSession(session));
                evt.menu.AppendAction("Delete", (a) => DeleteSession(session));
                evt.menu.AppendAction("Duplicate", (a) => DuplicateSession(session));
                
                evt.menu.AppendSeparator();
                
                evt.menu.AppendAction("Export as JSON", (a) => ExportSessionAsJson(session));
            }));
        }
        private void DuplicateSession(ChatSession session)
        {
            var newSession = new ChatSession(session.title + " (Copy)");
            newSession.messages = new List<ChatMessage>(session.messages);
            newSession.artifacts = new List<CodeArtifact>(session.artifacts);
            newSession.timestamp = DateTime.Now; 
            
            _allSessions.sessions.Add(newSession);
            SaveChatSessions();
            RefreshChatHistoryList();
            
            LoadChatSession(newSession);
        }
        
        private void ExportSessionAsJson(ChatSession session)
        {
            string path = EditorUtility.SaveFilePanel("Export Chat Session", "", session.title.Replace(" ", "_") + ".json", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = JsonUtility.ToJson(session, true);
                    File.WriteAllText(path, json);
                    EditorUtility.DisplayDialog("Export Successful", $"Session exported to {path}", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Export Failed", $"Error exporting session: {ex.Message}", "OK");
                }
            }
        }
        
        private void ImportSession()
        {
            string path = EditorUtility.OpenFilePanel("Import Chat Session", "", "json");
            
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var session = JsonUtility.FromJson<ChatSession>(json);
                    
                    session.id = Guid.NewGuid().ToString();
                    
                    session.title += " (Imported)";
                    session.timestamp = DateTime.Now;
                    _allSessions.sessions.Add(session);
                    SaveChatSessions();
                    RefreshChatHistoryList();
                    
                    LoadChatSession(session);
                    
                    EditorUtility.DisplayDialog("Import Successful", "Chat session imported successfully.", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Import Failed", $"Error importing session: {ex.Message}", "OK");
                }
            }
        }

        


        private void FilterChatHistory(ChangeEvent<string> evt)
        {
            string searchTerm = evt.newValue.ToLowerInvariant().Trim();
            
            EditorApplication.delayCall += () => {
                if (searchTerm != _searchField.value.ToLowerInvariant().Trim())
                    return;
                
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    RefreshChatHistoryList();
                    return;
                }
                
                _chatHistoryList.Clear();
                _dateFoldouts.Clear();
                
                var matchingSessions = _allSessions.sessions
                    .Where(s => s.title.ToLowerInvariant().Contains(searchTerm) || 
                            s.messages.Any(m => m.content.ToLowerInvariant().Contains(searchTerm)))
                    .OrderByDescending(s => s.timestamp)
                    .ToList();
                
                var searchHeader = new Label($"Search Results ({matchingSessions.Count})");
                searchHeader.style.paddingLeft = 12;
                searchHeader.style.paddingTop = 8;
                searchHeader.style.paddingBottom = 8;
                searchHeader.style.color = new Color(0.9f, 0.9f, 0.9f);
                searchHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                _chatHistoryList.Add(searchHeader);
                
                if (matchingSessions.Count == 0)
                {
                    var noResults = new Label("No matching chats found");
                    noResults.style.color = new Color(0.7f, 0.7f, 0.7f);
                    noResults.style.unityTextAlign = TextAnchor.MiddleCenter;
                    noResults.style.paddingTop = 20;
                    _chatHistoryList.Add(noResults);
                    return;
                }
                
                var resultsContainer = new VisualElement();
                resultsContainer.style.paddingLeft = 12;
                resultsContainer.style.paddingRight = 12;
                _chatHistoryList.Add(resultsContainer);
                
                foreach (var session in matchingSessions)
                {
                    var tempFoldout = new Foldout();
                    AddSessionToList(session, tempFoldout);
                    
                    var sessionItem = tempFoldout.Children().First();
                    resultsContainer.Add(sessionItem);
                }
            };
        }
        private void SortChatHistory(string sortOption)
        {
            switch (sortOption)
            {
                case "newest":
                    _allSessions.sessions = _allSessions.sessions
                        .OrderByDescending(s => s.timestamp)
                        .ToList();
                    break;
                case "oldest":
                    _allSessions.sessions = _allSessions.sessions
                        .OrderBy(s => s.timestamp)
                        .ToList();
                    break;
                case "name":
                    _allSessions.sessions = _allSessions.sessions
                        .OrderBy(s => s.title)
                        .ToList();
                    break;
                case "messages":
                    _allSessions.sessions = _allSessions.sessions
                        .OrderByDescending(s => s.messages.Count)
                        .ToList();
                    break;
            }
            
            RefreshChatHistoryList();
        }



        private void RenameSession(ChatSession session)
        {
            var popup = new RenameSessionPopup(session.title, newTitle => {
                if (!string.IsNullOrWhiteSpace(newTitle))
                {
                    session.title = newTitle;
                    SaveChatSessions();
                    RefreshChatHistoryList();
                    
                    if (_currentSession?.id == session.id)
                    {
                        UpdateChatTitle();
                    }
                }
            });
            
            popup.position = new Rect(
                (position.width - 300) / 2,
                (position.height - 150) / 2,
                300, 
                150
            );
            popup.ShowModal();
        }

        private void DeleteSession(ChatSession session)
        {
            if (EditorUtility.DisplayDialog("Delete Chat", $"Are you sure you want to delete the chat '{session.title}'?", "Delete", "Cancel"))
            {
                bool isCurrentSession = _currentSession?.id == session.id;
                
                _allSessions.sessions.RemoveAll(s => s.id == session.id);
                SaveChatSessions();
                RefreshChatHistoryList();
                
                if (isCurrentSession)
                {
                    CreateNewChat();
                }
            }
        }

        private void UpdateChatTitle()
        {
            if (_currentSession != null && !_isNewChat)
            {
                var chatTitles = _chatPanel.Query<Label>().Where(l => l.parent.parent == _chatPanel.Children().First()).ToList();
                if (chatTitles.Count > 0)
                {
                    var chatTitle = chatTitles.FirstOrDefault(t => t.text == "Chat" || t.text.StartsWith(_currentSession.title));
                    if (chatTitle != null)
                    {
                        chatTitle.text = _currentSession.title;
                    }
                }
            }
            else
            {
                var chatTitles = _chatPanel.Query<Label>().Where(l => l.parent.parent == _chatPanel.Children().First()).ToList();
                if (chatTitles.Count > 0)
                {
                    var chatTitle = chatTitles.FirstOrDefault(t => t.text != "Chat");
                    if (chatTitle != null)
                    {
                        chatTitle.text = "Chat";
                    }
                }
            }
        }

        private void ToggleSidebar()
        {
            _isSidebarVisible = !_isSidebarVisible;
            
            float startWidth = _isSidebarVisible ? 0 : 250;
            float targetWidth = _isSidebarVisible ? 250 : 0;
            
            if (_isSidebarVisible)
            {
                _sidebarPanel.style.display = DisplayStyle.Flex;
                _sidebarPanel.style.width = startWidth;
            }
            
            const int animationSteps = 15; 
            float widthStep = (targetWidth - startWidth) / animationSteps;
            
            int currentStep = 0;
            
            EditorApplication.update -= AnimateSidebar;
            
            void AnimateSidebar()
            {
                if (currentStep >= animationSteps)
                {
                    _sidebarPanel.style.width = targetWidth;
                    
                    if (!_isSidebarVisible)
                    {
                        _sidebarPanel.style.display = DisplayStyle.None;
                    }
                    
                    if (!_isNewChat && _currentSession != null)
                    {
                        SaveCurrentSession();
                    }
                    
                    EditorApplication.update -= AnimateSidebar;
                    return;
                }
                
                currentStep++;
                float progress = (float)currentStep / animationSteps;
                float easedProgress = EaseInOutCubic(progress);
                float newWidth = startWidth + (targetWidth - startWidth) * easedProgress;
                
                _sidebarPanel.style.width = newWidth;
            }
            
            EditorApplication.update += AnimateSidebar;
        }

        private float EaseInOutCubic(float t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }


        #endregion

        #region UI Helpers
        private string CreateConversationHistoryPrompt()
        {
            StringBuilder history = new StringBuilder();
            
            int maxMessagesToInclude = 10;
            int startIndex = Math.Max(0, _chatMessages.Count - maxMessagesToInclude);
            
            for (int i = startIndex; i < _chatMessages.Count; i++)
            {
                var message = _chatMessages[i];
                
                if (message.isUser)
                {
                    history.AppendLine("\nUser: " + message.content);
                }
                else
                {
                    string aiMessage = message.content;
                    
                    if (message.codeBlocks != null && message.codeBlocks.Count > 0)
                    {
                        StringBuilder codeReferences = new StringBuilder();
                        
                        foreach (var codeBlock in message.codeBlocks)
                        {
                            var artifact = _codeArtifacts.FirstOrDefault(a => a.id == codeBlock.artifactId);
                            if (artifact != null && !string.IsNullOrEmpty(artifact.code))
                            {
                                string placeholder = "[Code Block]";
                                string replacement = $"[Code Block: {codeBlock.label.ToString()}]";
                                
                                if (aiMessage.Contains(placeholder))
                                {
                                    aiMessage = aiMessage.Replace(placeholder, replacement);
                                }
                                
                                codeReferences.AppendLine($"\n\nReferenced code ({codeBlock.label}):\n```csharp\n{artifact.code}\n```");
                            }
                        }
                        
                        aiMessage += codeReferences.ToString();
                    }
                    
                    history.AppendLine("\nAI Assistant: " + aiMessage);
                }
            }
            
            return history.ToString();
        }

        private void AddUserMessage(string message)
        {
            var chatMessage = new ChatMessage(true, message);
            _chatMessages.Add(chatMessage);
            
            if (_currentSession != null)
            {
                _currentSession.messages.Add(chatMessage);
            }
             
            var messageContainer = new VisualElement();
            messageContainer.style.marginBottom = 16;
            messageContainer.style.flexDirection = FlexDirection.Row;
            _chatHistory.Add(messageContainer);
            
            var avatar = new VisualElement();
            avatar.style.width = 24;
            avatar.style.height = 24;
            avatar.style.borderTopLeftRadius = 12;
            avatar.style.borderTopRightRadius = 12;
            avatar.style.borderBottomLeftRadius = 12;
            avatar.style.borderBottomRightRadius = 12;
            avatar.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            avatar.style.marginRight = 8;
            avatar.style.marginTop = 4;
            avatar.style.flexShrink = 0;
            avatar.style.paddingTop = 2;
            avatar.style.paddingLeft = 2;
            messageContainer.Add(avatar);
            
            var avatarLabel = new Label("U");
            avatarLabel.style.color = Color.white;
            avatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            avatarLabel.style.fontSize = 12;
            
            avatarLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            avatar.Add(avatarLabel);
            
            var content = new VisualElement();
            content.style.flexGrow = 1;
            messageContainer.Add(content);
            
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 4;
            content.Add(header);
            
            var nameLabel = new Label("You");
            nameLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.fontSize = 13;
            header.Add(nameLabel);
            
            var timestamp = new Label(DateTime.Now.ToString("HH:mm"));
            timestamp.style.color = new Color(0.6f, 0.6f, 0.6f);
            timestamp.style.fontSize = 11;
            timestamp.style.marginLeft = 8;
            header.Add(timestamp);
            
            var messageText = new Label(message);
            messageText.style.whiteSpace = WhiteSpace.Normal;
            messageText.style.color = new Color(0.9f, 0.9f, 0.9f);
            content.Add(messageText);
            
            EditorApplication.delayCall += () => {
                _chatHistory.scrollOffset = new Vector2(0, float.MaxValue);
            };
        }

        private void AddAIMessage(string message, List<CodeBlockReference> codeBlocks, List<InlineCodeBlock> inlineCodeBlocks)
        {
            var chatMessage = new ChatMessage(false, message);
            chatMessage.codeBlocks = codeBlocks;
            chatMessage.inlineCodeBlocks = inlineCodeBlocks;
            _chatMessages.Add(chatMessage);
            
            if (_currentSession != null)
            {
                _currentSession.messages.Add(chatMessage);
                
                foreach (var blockRef in codeBlocks)
                {
                    var artifact = _codeArtifacts.FirstOrDefault(a => a.id == blockRef.artifactId);
                    if (artifact != null && !_currentSession.artifacts.Any(a => a.id == artifact.id))
                    {
                        _currentSession.artifacts.Add(artifact);
                    }
                }
                
                SaveCurrentSession();
            }
            
            var messageContainer = new VisualElement();
            messageContainer.style.marginBottom = 16;
            messageContainer.style.flexDirection = FlexDirection.Row;
            _chatHistory.Add(messageContainer);
            
            var avatar = new VisualElement();
            avatar.style.width = 24;
            avatar.style.height = 24;
            avatar.style.borderTopLeftRadius = 12;
            avatar.style.borderTopRightRadius = 12;
            avatar.style.borderBottomLeftRadius = 12;
            avatar.style.borderBottomRightRadius = 12;
            avatar.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            avatar.style.marginRight = 8;
            avatar.style.marginTop = 4;
            avatar.style.flexShrink = 0;
            avatar.style.paddingTop = 2;
            avatar.style.paddingLeft = 2;
            messageContainer.Add(avatar);
            
            var avatarLabel = new Label("AI");
            avatarLabel.style.color = Color.white;
            avatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            avatarLabel.style.fontSize = 10; 
            avatarLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            avatar.Add(avatarLabel);
            
            // Content
            var content = new VisualElement();
            content.style.flexGrow = 1;
            messageContainer.Add(content);
            
            // Message header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 4;
            content.Add(header);
            
            var nameLabel = new Label("AI Assistant");
            nameLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.fontSize = 13;
            header.Add(nameLabel);
            
            var timestamp = new Label(DateTime.Now.ToString("HH:mm"));
            timestamp.style.color = new Color(0.6f, 0.6f, 0.6f);
            timestamp.style.fontSize = 11;
            timestamp.style.marginLeft = 8;
            header.Add(timestamp);
            
            string processedMessage = message;
            
            if (codeBlocks.Count > 0 || inlineCodeBlocks.Count > 0)
            {
                var messageContent = new VisualElement();
                messageContent.style.flexGrow = 1;
                
                var allCodePositions = new List<(int position, bool isInline, int index, int length)>();
                
                foreach (var match in System.Text.RegularExpressions.Regex.Matches(processedMessage, @"\[Code Block\]").Cast<System.Text.RegularExpressions.Match>())
                {
                    allCodePositions.Add((match.Index, false, allCodePositions.Count(p => !p.isInline), match.Length));
                }
                
                var codeBlockMatches = System.Text.RegularExpressions.Regex.Matches(processedMessage, @"```(?:csharp|cs|C#)?\s*([\s\S]*?)```");
                for (int i = 0; i < codeBlockMatches.Count; i++)
                {
                    var match = codeBlockMatches[i];
                    string code = match.Groups[1].Value.Trim();
                    int lineCount = code.Split('\n').Length;
                    
                    if (lineCount <= SHORT_CODE_MAX_LINES)
                    {
                        int inlineIndex = inlineCodeBlocks.FindIndex(block => block.code == code);
                        if (inlineIndex >= 0)
                        {
                            allCodePositions.Add((match.Index, true, inlineIndex, match.Length));
                        }
                    }
                }
                
                allCodePositions.Sort((a, b) => a.position.CompareTo(b.position));
                
                int lastIndex = 0;
                foreach (var codePos in allCodePositions)
                {
                    if (codePos.position > lastIndex)
                    {
                        string beforeText = processedMessage.Substring(lastIndex, codePos.position - lastIndex);
                        if (!string.IsNullOrWhiteSpace(beforeText))
                        { 
                            var textElement = CreateMarkdownStyledText(beforeText);
                            // textElement.style.whiteSpace = WhiteSpace.Normal;
                            // textElement.style.color = new Color(0.9f, 0.9f, 0.9f);
                            messageContent.Add(textElement);
                        }
                    }
                    
                    if (codePos.isInline)
                    {
                        // Add inline code block
                        var inlineCode = inlineCodeBlocks[codePos.index];
                        
                        var codeContainer = new VisualElement();
                        codeContainer.style.marginTop = 8;
                        codeContainer.style.marginBottom = 8;
                        codeContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
                        codeContainer.style.borderTopWidth = 1;
                        codeContainer.style.borderBottomWidth = 1;
                        codeContainer.style.borderLeftWidth = 1;
                        codeContainer.style.borderRightWidth = 1;
                        codeContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
                        codeContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                        codeContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                        codeContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
                        
                        var codeHeader = new VisualElement();
                        codeHeader.style.flexDirection = FlexDirection.Row;
                        codeHeader.style.justifyContent = Justify.SpaceBetween;
                        codeHeader.style.paddingLeft = 8;
                        codeHeader.style.paddingRight = 8;
                        codeHeader.style.paddingTop = 4;
                        codeHeader.style.paddingBottom = 4;
                        codeHeader.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
                        codeHeader.style.borderBottomWidth = 1;
                        codeHeader.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                        codeContainer.Add(codeHeader);
                        
                        string firstLine = inlineCode.code.Split('\n')[0].Trim();
                        string title = firstLine.StartsWith("//") ? firstLine : "Code";
                        var titleLabel = new Label(title);
                        titleLabel.style.fontSize = 12;
                        titleLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                        codeHeader.Add(titleLabel);
                        
                        // Add copy button
                        var copyButton = new Button(() => CopyToClipboard(inlineCode.code));
                        copyButton.text = "Copy";
                        copyButton.style.height = 20;
                        copyButton.style.paddingTop = 2;
                        copyButton.style.paddingBottom = 2;
                        codeHeader.Add(copyButton);
                        
                        // Code content
                        var codeContent = new VisualElement();
                        codeContent.style.paddingLeft = 12;
                        codeContent.style.paddingRight = 12;
                        codeContent.style.paddingTop = 8;
                        codeContent.style.paddingBottom = 8;
                        codeContainer.Add(codeContent);
                        
                        try {
                            var highlighted = UISyntaxHighlighter.CreateHighlightedCodeElement(inlineCode.code);
                            codeContent.Add(highlighted);
                        }
                        catch (System.Exception) {
                            var codeText = new Label(inlineCode.code);
                            codeText.style.whiteSpace = WhiteSpace.Normal;
                            codeText.style.color = new Color(0.7f, 0.9f, 0.7f);
                            codeText.style.unityFontStyleAndWeight = FontStyle.Bold;
                            codeText.style.fontSize = 12;
                            codeContent.Add(codeText);
                        }
                        
                        messageContent.Add(codeContainer);
                    }
                    else
                    {
                        // Add code block button
                        var blockRef = codeBlocks[codePos.index];
                        var codeButton = new Button(() => ShowCodeArtifact(blockRef.artifactId));
                        codeButton.text = blockRef.label;
                        codeButton.style.marginTop = 4;
                        codeButton.style.marginBottom = 4;
                        codeButton.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                        codeButton.style.borderTopWidth = 1;
                        codeButton.style.borderBottomWidth = 1;
                        codeButton.style.borderLeftWidth = 1;
                        codeButton.style.borderRightWidth = 1;
                        codeButton.style.borderTopColor = new Color(0.35f, 0.35f, 0.35f);
                        codeButton.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);
                        codeButton.style.borderLeftColor = new Color(0.35f, 0.35f, 0.35f);
                        codeButton.style.borderRightColor = new Color(0.35f, 0.35f, 0.35f);
                        codeButton.style.paddingTop = 4;
                        codeButton.style.paddingBottom = 4;
                        codeButton.style.paddingLeft = 8;
                        codeButton.style.paddingRight = 8;
                        messageContent.Add(codeButton);
                    }
                    
                    lastIndex = codePos.position + codePos.length;
                }
                
                if (lastIndex < processedMessage.Length)
                {
                    string afterText = processedMessage.Substring(lastIndex);
                    if (!string.IsNullOrWhiteSpace(afterText))
                    {
                        var textElement = CreateMarkdownStyledText(afterText);
                        // textElement.style.whiteSpace = WhiteSpace.Normal;
                        // textElement.style.color = new Color(0.9f, 0.9f, 0.9f);
                        messageContent.Add(textElement);
                    }
                }
                
                content.Add(messageContent);
            }
            else
            {
                var messageContent = CreateMarkdownStyledText(message);
                content.Add(messageContent);
            }
            
            EditorApplication.delayCall += () => {
                _chatHistory.scrollOffset = new Vector2(0, float.MaxValue);
            };
        }
        
        private VisualElement AddSystemMessage(string message, ContextFile contextFile = null)
        {
            if (IsWelcomeMessage(message) && _chatMessages.Any(m => m.isSystemMessage && IsWelcomeMessage(m.content)))
            {
                var existingMessage = _chatMessages.First(m => m.isSystemMessage && IsWelcomeMessage(m.content));
                
                var messageContainer = new VisualElement();
                messageContainer.style.marginBottom = 16;
                messageContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                messageContainer.style.borderLeftWidth = 3;
                messageContainer.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
                messageContainer.style.paddingTop = 8;
                messageContainer.style.paddingBottom = 8;
                messageContainer.style.paddingLeft = 12;
                messageContainer.style.paddingRight = 12;
                _chatHistory.Add(messageContainer);
                
                var messageText = new Label(message);
                messageText.style.whiteSpace = WhiteSpace.Normal;
                messageText.style.color = new Color(0.85f, 0.85f, 0.85f);
                messageContainer.Add(messageText);
                
                return messageContainer;
            }
            
            var chatMessage = new ChatMessage(false, message);
            chatMessage.isSystemMessage = true;
            
            if (contextFile != null)
            {
                chatMessage.contextFileId = contextFile.id.ToString();
                contextFile.systemMessageId = chatMessage.contextFileId;
            }
            
            _chatMessages.Add(chatMessage);
            
            if (_currentSession != null && !message.Contains("Chat cleared"))
            {
                _currentSession.messages.Add(chatMessage);
            }
            
            var container = new VisualElement();
            container.style.marginBottom = 16;
            container.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            container.style.borderLeftWidth = 3;
            container.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft = 12;
            container.style.paddingRight = 12;
            _chatHistory.Add(container);
            
            if (contextFile != null)
            {
                container.AddManipulator(new Clickable(() => {
                    ShowContextFileInArtifact(contextFile);
                }));
                
                container.RegisterCallback<MouseEnterEvent>(evt => {
                    container.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 0.3f);
                    EditorGUIUtility.AddCursorRect(container.worldBound, MouseCursor.Link);
                });
                container.RegisterCallback<MouseLeaveEvent>(evt => {
                    container.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                });
                
                var messageContent = new VisualElement();
                messageContent.style.flexDirection = FlexDirection.Row;
                messageContent.style.alignItems = Align.Center;
                container.Add(messageContent);
                
                var icon = new VisualElement();
                icon.style.width = 16;
                icon.style.height = 16;
                icon.style.marginRight = 8;
                icon.style.backgroundImage = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
                messageContent.Add(icon);
                
                var text = new Label(message);
                text.style.whiteSpace = WhiteSpace.Normal;
                text.style.color = new Color(0.85f, 0.85f, 0.85f);
                messageContent.Add(text);
            }
            else
            {
                var text = new Label(message);
                text.style.whiteSpace = WhiteSpace.Normal;
                text.style.color = new Color(0.85f, 0.85f, 0.85f);
                container.Add(text);
            }
            
            if (contextFile != null)
            {
                contextFile.chatInfoMessage = container;
            }
            
            EditorApplication.delayCall += () => {
                _chatHistory.scrollOffset = new Vector2(0, float.MaxValue);
            };

            return container;
        }

        private void ShowContextFileInArtifact(ContextFile contextFile)
        {
            var artifactId = AddCodeArtifact("Context: " + contextFile.filename, contextFile.code, "");
            ShowCodeArtifact(artifactId);
        }
        private int AddCodeArtifact(string title, string code, string className)
        {
            int artifactId = _nextArtifactId++;
            
            var artifact = new CodeArtifact(title, code, className, artifactId);
            _codeArtifacts.Add(artifact);
            
            CreateArtifactElement(artifact);
            
            return artifactId;
        }
        
        private void CreateArtifactElement(CodeArtifact artifact)
        {
            if (_artifactsScrollView.childCount == 1 && _artifactsScrollView.Children().FirstOrDefault() is Label label && label.text.Contains("No code artifacts yet"))
            {
                _artifactsScrollView.Clear();
            }
            
            var artifactContainer = new VisualElement();
            artifactContainer.style.marginBottom = 24;
            artifactContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            artifactContainer.style.borderTopWidth = 1;
            artifactContainer.style.borderBottomWidth = 1;
            artifactContainer.style.borderLeftWidth = 1;
            artifactContainer.style.borderRightWidth = 1;
            artifactContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            artifactContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            artifactContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            artifactContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.paddingLeft = 12;
            header.style.paddingRight = 12;
            header.style.paddingTop = 8;
            header.style.paddingBottom = 8;
            header.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            artifactContainer.Add(header);
            
            var titleLabel = new Label(artifact.title);
            titleLabel.style.flexGrow = 1;
            titleLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);
            
            // Actions
            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            header.Add(actions);
            
            var copyButton = new Button(() => CopyToClipboard(artifact.code));
            copyButton.text = "Copy";
            copyButton.style.marginRight = 8;
            actions.Add(copyButton);
            
            var createButton = new Button(() => CreateScriptFromCode(artifact.code, artifact.className));
            createButton.text = "Create";
            createButton.style.marginRight = 8;
            actions.Add(createButton);
            
            var closeButton = new Button(() => HideCodeArtifact(artifact.id));
            closeButton.text = "×";
            closeButton.style.width = 24;
            closeButton.style.height = 24;
            closeButton.style.paddingTop = 0;
            closeButton.style.paddingBottom = 0;
            closeButton.style.paddingLeft = 0;
            closeButton.style.paddingRight = 0;
            closeButton.style.fontSize = 16;
            closeButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            actions.Add(closeButton);
            
            var codeContent = new VisualElement();
            codeContent.style.paddingLeft = 12;
            codeContent.style.paddingRight = 12;
            codeContent.style.paddingTop = 12;
            codeContent.style.paddingBottom = 12;
            artifactContainer.Add(codeContent);
            
            try {
                var highlighted = UISyntaxHighlighter.CreateHighlightedCodeElement(artifact.code);
                codeContent.Add(highlighted);
            }
            catch (System.Exception ex) {
                Debug.LogError($"Error applying syntax highlighting: {ex.Message}");
                
                var codeField = new TextField();
                codeField.multiline = true;
                codeField.SetValueWithoutNotify(artifact.code);
                codeField.isReadOnly = true;
                codeField.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                codeField.style.color = new Color(0.9f, 0.9f, 0.9f);
                
                // Fix text input alignment
                codeField.Q(TextField.textInputUssName).style.alignSelf = Align.Auto;
                codeField.Q(TextField.textInputUssName).style.color = new Color(0.9f, 0.9f, 0.9f);
                codeField.Q(TextField.textInputUssName).style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                
                codeContent.Add(codeField);
            }
            
            _artifactElements[artifact.id] = artifactContainer;
            
            if (_activeArtifactId != 0 && _activeArtifactId != artifact.id)
            {
                artifactContainer.style.display = DisplayStyle.None;
            }
            
            _artifactsScrollView.Add(artifactContainer);
        }
        
        private void ShowCodeArtifact(int artifactId)
        {
            foreach (var kvp in _artifactElements)
            {
                kvp.Value.style.display = DisplayStyle.None;
            }
            
            if (_artifactElements.TryGetValue(artifactId, out var element))
            {
                element.style.display = DisplayStyle.Flex;
                _activeArtifactId = artifactId;
            }
            else
            {
                var artifact = _codeArtifacts.Find(a => a.id == artifactId);
                if (artifact != null)
                {
                    CreateArtifactElement(artifact);
                    _activeArtifactId = artifactId;
                }
            }
        }
        
        private void HideCodeArtifact(int artifactId)
        {
            if (_artifactElements.TryGetValue(artifactId, out var element))
            {
                _artifactsScrollView.Remove(element);
                _artifactElements.Remove(artifactId);
                
                if (_activeArtifactId == artifactId)
                {
                    _activeArtifactId = 0;
                    
                    if (_artifactElements.Count == 0)
                    {
                        var placeholder = new Label("No code artifacts yet. Ask me to create a script!");
                        placeholder.style.color = new Color(0.7f, 0.7f, 0.7f);
                        placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
                        placeholder.style.marginTop = 50;
                        _artifactsScrollView.Add(placeholder);
                    }
                    else
                    {
                        var firstArtifactId = _artifactElements.Keys.FirstOrDefault();
                        ShowCodeArtifact(firstArtifactId);
                    }
                }
            }
        }
        
        private void AddContextFileButton(ContextFile file)
        {
            var fileButton = new VisualElement();
            fileButton.style.flexDirection = FlexDirection.Row;
            fileButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            fileButton.style.borderTopWidth = 1;
            fileButton.style.borderBottomWidth = 1;
            fileButton.style.borderLeftWidth = 1;
            fileButton.style.borderRightWidth = 1;
            fileButton.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            fileButton.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            fileButton.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            fileButton.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            fileButton.style.marginRight = 8;
            fileButton.style.marginBottom = 8;
            fileButton.style.paddingTop = 4;
            fileButton.style.paddingBottom = 4;
            fileButton.style.paddingLeft = 8;
            fileButton.style.paddingRight = 4;
            fileButton.style.alignItems = Align.Center;
            
            var clickable = new Clickable(() => {
                var artifactId = AddCodeArtifact("Context: " + file.filename, file.code, "");
                ShowCodeArtifact(artifactId);
            });
            fileButton.AddManipulator(clickable);
            
            var iconContainer = new VisualElement();
            iconContainer.style.width = 16;
            iconContainer.style.height = 16;
            iconContainer.style.marginRight = 8;
            iconContainer.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            fileButton.Add(iconContainer);
            
            var iconLabel = new Label("C#");
            iconLabel.style.fontSize = 8;
            iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            iconLabel.style.color = Color.white;
            iconContainer.Add(iconLabel);
            
            var filename = new Label(file.filename);
            filename.style.color = new Color(0.9f, 0.9f, 0.9f);
            fileButton.Add(filename);
            
            var closeButton = new Button(() => RemoveContextFile(file.id));
            closeButton.text = "×";
            closeButton.style.width = 20;
            closeButton.style.height = 20;
            closeButton.style.marginLeft = 8;
            closeButton.style.paddingTop = 0;
            closeButton.style.paddingBottom = 0;
            closeButton.style.paddingLeft = 0;
            closeButton.style.paddingRight = 0;
            closeButton.style.fontSize = 14;
            closeButton.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            closeButton.style.borderTopWidth = 0;
            closeButton.style.borderBottomWidth = 0;
            closeButton.style.borderLeftWidth = 0;
            closeButton.style.borderRightWidth = 0;
            fileButton.Add(closeButton);
            
            _contextFilesScrollView.Add(fileButton);
        }
        
        private void RemoveContextFile(int fileId)
        {
            int index = _contextFiles.FindIndex(f => f.id == fileId);
            if (index >= 0)
            {
                var contextFile = _contextFiles[index];
                
                if (contextFile.chatInfoMessage != null && contextFile.chatInfoMessage.parent != null)
                {
                    contextFile.chatInfoMessage.parent.Remove(contextFile.chatInfoMessage);
                }
                
                _contextFiles.RemoveAt(index);
                
                if (_currentSession != null && _currentSession.contextFiles != null)
                {
                    int sessionIndex = _currentSession.contextFiles.FindIndex(f => f.id == fileId);
                    if (sessionIndex >= 0)
                    {
                        _currentSession.contextFiles.RemoveAt(sessionIndex);
                        SaveCurrentSession(); 
                    }
                }
            }
            
            RefreshContextFilesUI();
        }

        
        private void RefreshContextFilesUI()
        {
            _contextFilesScrollView.Clear();
            
            if (_contextFiles.Count > 0)
            {
                _contextFilesContainer.style.display = DisplayStyle.Flex;  
                
                foreach (var file in _contextFiles)
                {
                    AddContextFileButton(file);
                }
            }else
            {
                _contextFilesContainer.style.display = DisplayStyle.None; 
            }
        }
        
        private void ShowProgress()
        {
            if (_progressContainer != null)
                _progressContainer.style.display = DisplayStyle.Flex;
            
            _progressValue = 0f;
            _isGenerating = true;
        }
        
        private void HideProgress()
        {
            if (_progressContainer != null)
                _progressContainer.style.display = DisplayStyle.None;
            
            _isGenerating = false;
        }
        
        private void UpdateProgress()
        {
            if (!_isGenerating) return;
            
            _progressValue = Mathf.Repeat(_progressValue + 0.005f, 1f);
            
            if (_progressBar != null)
            {
                var progressFill = _progressBar.Q<VisualElement>("progress-fill");
                if (progressFill != null)
                {
                    progressFill.style.width = new Length(_progressValue * 100, LengthUnit.Percent);
                }
            }
        }
        
        private void ShowUploadPanel()
        {
            if (_uploadPanel != null)
                _uploadPanel.style.display = DisplayStyle.Flex;
        }
        
        private void HideUploadPanel()
        {
            if (_uploadPanel != null)
                _uploadPanel.style.display = DisplayStyle.None;
            
            _scriptUploadField.value = null;
            _codeUploadField.value = "";
        }
        
        private void ClearChat()
        {
            if (EditorUtility.DisplayDialog("Clear Chat", "Are you sure you want to clear the entire chat history?", "Yes", "No"))
            {
                if (!_isNewChat && _currentSession != null)
                {
                    SaveCurrentSession();
                }
                
                CreateNewChat();
            }
        }
        #endregion
        #region Markdown Processing
        private VisualElement CreateMarkdownStyledText(string text)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            
            // Process headers: ### Header
            var headerMatches = System.Text.RegularExpressions.Regex.Matches(text, @"^(#{1,6})\s+(.+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (headerMatches.Count > 0)
            {
                int lastEndIndex = 0;
                
                foreach (System.Text.RegularExpressions.Match match in headerMatches)
                {
                    // Add text before the header
                    if (match.Index > lastEndIndex)
                    {
                        string beforeText = text.Substring(lastEndIndex, match.Index - lastEndIndex);
                        if (!string.IsNullOrWhiteSpace(beforeText))
                        {
                            ProcessTextBlock(container, beforeText);
                        }
                    }
                    
                    // Add header
                    string headerText = match.Groups[2].Value.Trim();
                    int headerLevel = match.Groups[1].Value.Length; 
                    
                    var headerElement = new Label(headerText);
                    headerElement.style.whiteSpace = WhiteSpace.Normal;
                    headerElement.style.color = new Color(0.95f, 0.95f, 0.95f);
                    headerElement.style.unityFontStyleAndWeight = FontStyle.Bold;
                    headerElement.style.marginTop = 8;
                    headerElement.style.marginBottom = 4;
                    
                    // Set font size based on header level
                    switch (headerLevel)
                    {
                        case 1: headerElement.style.fontSize = 22; break;
                        case 2: headerElement.style.fontSize = 20; break;
                        case 3: headerElement.style.fontSize = 18; break;
                        case 4: headerElement.style.fontSize = 16; break;
                        case 5: headerElement.style.fontSize = 14; break;
                        case 6: headerElement.style.fontSize = 13; break;
                    }
                    
                    container.Add(headerElement);
                    
                    lastEndIndex = match.Index + match.Length;
                }
                
                // Add remaining text
                if (lastEndIndex < text.Length)
                {
                    string afterText = text.Substring(lastEndIndex);
                    if (!string.IsNullOrWhiteSpace(afterText))
                    {
                        ProcessTextBlock(container, afterText);
                    }
                }
                
                return container;
            }
            
            ProcessTextBlock(container, text);
            
            return container;
        }

        private void ProcessTextBlock(VisualElement container, string text)
        {
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // Check if line is a bullet point
                if (line.TrimStart().StartsWith("* "))
                {
                    // Calculate indentation
                    int indentation = line.IndexOf('*');
                    string bulletContent = line.Substring(line.IndexOf('*') + 1).Trim();
                    
                    // Create bullet point container
                    var bulletContainer = new VisualElement();
                    bulletContainer.style.flexDirection = FlexDirection.Row;
                    bulletContainer.style.marginLeft = indentation;
                    bulletContainer.style.marginTop = 2;
                    bulletContainer.style.marginBottom = 2;
                    
                    // Add bullet marker
                    var bulletMarker = new Label("•");
                    bulletMarker.style.width = 15;
                    bulletMarker.style.unityTextAlign = TextAnchor.MiddleCenter;
                    bulletMarker.style.color = new Color(0.9f, 0.9f, 0.9f);
                    bulletContainer.Add(bulletMarker);
                    
                    // Add bullet content with bold processing
                    var contentElement = new VisualElement();
                    contentElement.style.flexGrow = 1;
                    ProcessBoldText(contentElement, bulletContent);
                    bulletContainer.Add(contentElement);
                    
                    container.Add(bulletContainer);
                }
                else
                {
                    // Process regular line with bold text
                    ProcessBoldText(container, line);
                    
                    // Add line break if not the last line
                    if (i < lines.Length - 1)
                    {
                        var lineBreak = new VisualElement();
                        lineBreak.style.height = 2;
                        container.Add(lineBreak);
                    }
                }
            }
        }

        // Helper method to process bold text within a line
        private void ProcessBoldText(VisualElement container, string text)
        {
            // Regular expression to match text between ** markers
            var boldMatches = System.Text.RegularExpressions.Regex.Matches(text, @"\*\*(.*?)\*\*");
            
            if (boldMatches.Count > 0)
            {
                // Create a line container with horizontal layout to keep all text segments on one line
                var lineContainer = new VisualElement();
                lineContainer.style.flexDirection = FlexDirection.Row;
                lineContainer.style.flexWrap = Wrap.Wrap; 
                
                int lastEndIndex = 0;
                
                foreach (System.Text.RegularExpressions.Match match in boldMatches)
                {
                    if (match.Index > lastEndIndex)
                    {
                        string beforeText = text.Substring(lastEndIndex, match.Index - lastEndIndex);
                        if (!string.IsNullOrEmpty(beforeText))
                        {
                            var normalText = new Label(beforeText);
                            normalText.style.whiteSpace = WhiteSpace.Normal;
                            normalText.style.color = new Color(0.9f, 0.9f, 0.9f);
                            lineContainer.Add(normalText);
                        }
                    }
                    
                    // Add bold text
                    string boldText = match.Groups[1].Value; 
                    var boldElement = new Label(boldText);
                    boldElement.style.whiteSpace = WhiteSpace.Normal;
                    boldElement.style.color = new Color(0.95f, 0.95f, 0.95f);
                    boldElement.style.unityFontStyleAndWeight = FontStyle.Bold;
                    lineContainer.Add(boldElement);
                    
                    lastEndIndex = match.Index + match.Length;
                }
                
                // Add remaining text after the last bold part
                if (lastEndIndex < text.Length)
                {
                    string afterText = text.Substring(lastEndIndex);
                    if (!string.IsNullOrEmpty(afterText))
                    {
                        var normalText = new Label(afterText);
                        normalText.style.whiteSpace = WhiteSpace.Normal;
                        normalText.style.color = new Color(0.9f, 0.9f, 0.9f);
                        lineContainer.Add(normalText);
                    }
                }
                
                container.Add(lineContainer);
            }
            else
            {
                // If no bold text, add the whole line as normal text
                var normalText = new Label(text);
                normalText.style.whiteSpace = WhiteSpace.Normal;
                normalText.style.color = new Color(0.9f, 0.9f, 0.9f);
                container.Add(normalText);
            }
        }
        #endregion
        #region Business Logic
        private void SendPrompt()
        {
            if (_isGenerating || string.IsNullOrWhiteSpace(_currentPrompt)) return;
            
            AddUserMessage(_currentPrompt);
            
            if (_isNewChat && _currentSession != null)
            {
                string title = _currentPrompt.Length <= 30 ? 
                    _currentPrompt : _currentPrompt.Substring(0, 27) + "...";
                
                _currentSession.title = title;
                _isNewChat = false;
                
                UpdateChatTitle();
            }
            
            string prompt = _currentPrompt;
            
            _promptField.value = "";
            _currentPrompt = "";
            
            SendRequestToGPT(prompt);
            
            if (_contextFiles.Count > 0)
            {
                _contextFiles.Clear();
                RefreshContextFilesUI();
            }
        }


        
        
        private void SendRequestToGPT(string prompt)
        {
            _isGenerating = true;
            _responseFinalizing = false; 
            _streamingBuffer = "";
            _pendingStreamContent = "";
            ShowProgress();
            
            string conversationHistory = CreateConversationHistoryPrompt();
            
            string contextCode = GetContextFilesContent();
            StringBuilder fullPrompt = new StringBuilder();
            
            if (!string.IsNullOrEmpty(conversationHistory))
            {
                fullPrompt.AppendLine("Here's our conversation so far:");
                fullPrompt.AppendLine(conversationHistory);
                fullPrompt.AppendLine("\nNew user message: " + prompt);
            }
            else
            {
                fullPrompt.Append(prompt);
            }
            
            if (!string.IsNullOrEmpty(contextCode))
            {
                fullPrompt.AppendLine("\n\nHere's some context code from my project:");
                fullPrompt.AppendLine("```csharp");
                fullPrompt.AppendLine(contextCode);
                fullPrompt.AppendLine("```");
            }
            
            GPTClient.Instance.SystemInitPrompt = SYSTEM_INIT_PROMPT;
            
            GPTClient.Instance.OnResponseReceived = null;
            GPTClient.Instance.OnPartResponseReceived = null;
            
            CreateStreamingMessageContainer();
            _tempArtifactId = CreateTemporaryArtifact("Code (Generating)");
            
            Action<string> partResponseHandler = (partialResponse) => {
                if (_responseFinalizing) return; 
                
                lock (_pendingStreamContent)
                {
                    _pendingStreamContent += partialResponse;
                }
            };
            
            Action<string, int> responseReceivedHandler = (response, index) => {
                _responseFinalizing = true; 
                
                _streamUpdateTimer.Stop();
                
                EditorApplication.delayCall = () => {
                    if (_streamingMessageContainer != null)
                    {
                        _chatHistory.Remove(_streamingMessageContainer);
                        _streamingMessageContainer = null;
                        _streamingMessageText = null;
                    }
                    
                    // Remove temporary artifact
                    if (_tempArtifactId >= 0)
                    {
                        RemoveArtifact(_tempArtifactId);
                        _tempArtifactId = -1;
                    }
                    
                    string cleanedResponse = HelperFunctions.RemoveScriptTagFromOpenAIResponse(response);
                    
                    var extractedCodeBlocks = ExtractCodeBlocksFromResponse(cleanedResponse);
                    var codeBlockReferences = new List<CodeBlockReference>();
                    var inlineCodeBlocks = new List<InlineCodeBlock>();
                    
                    var codeBlockMatches = System.Text.RegularExpressions.Regex.Matches(cleanedResponse,@"```(?:csharp|cs|C#)?\s*([\s\S]*?)```");
                    
                    foreach (var codeBlock in extractedCodeBlocks)
                    {
                        if (codeBlock.isShort)
                        {
                            string originalText = null;
                            foreach (System.Text.RegularExpressions.Match match in codeBlockMatches)
                            {
                                string matchCode = match.Groups[1].Value.Trim();
                                if (matchCode == codeBlock.code)
                                {
                                    originalText = match.Value;
                                    inlineCodeBlocks.Add(new InlineCodeBlock(codeBlock.code, match.Index, originalText));
                                    break;
                                }
                            }
                        }
                        else
                        {
                            string className = ExtractClassName(codeBlock.code);
                            string title = !string.IsNullOrEmpty(className) ? $"{className}.cs" : "Generated Code";
                            
                            int artifactId = AddCodeArtifact(title, codeBlock.code, className);
                            codeBlockReferences.Add(new CodeBlockReference(artifactId, title));
                        }
                    }
                    
                    string processedMessage = ProcessResponseWithCodeBlocks(cleanedResponse, extractedCodeBlocks);
                    
                    AddAIMessage(processedMessage, codeBlockReferences, inlineCodeBlocks);
                    
                    if (codeBlockReferences.Count > 0)
                    {
                        ShowCodeArtifact(codeBlockReferences[0].artifactId);
                    }
                    
                    _isGenerating = false;
                    _responseFinalizing = true;
                    HideProgress();
                    
                    _streamingBuffer = "";
                    _pendingStreamContent = "";
                    
                    _streamUpdateTimer.Start();
                };
            };
            
            GPTClient.Instance.OnPartResponseReceived = partResponseHandler;
            GPTClient.Instance.OnResponseReceived = responseReceivedHandler;
            
            GPTClient.Instance.SendRequest(fullPrompt.ToString());
            
            _responseFinalizing = false;
        }


        private void CreateStreamingMessageContainer()
        {
            _streamingMessageContainer = new VisualElement();
            _streamingMessageContainer.name = "streaming-message-container";
            _streamingMessageContainer.style.marginBottom = 16;
            _streamingMessageContainer.style.flexDirection = FlexDirection.Row;
            _chatHistory.Add(_streamingMessageContainer);
            
            var avatar = new VisualElement();
            avatar.style.width = 24;
            avatar.style.height = 24;
            avatar.style.borderTopLeftRadius = 12;
            avatar.style.borderTopRightRadius = 12;
            avatar.style.borderBottomLeftRadius = 12;
            avatar.style.borderBottomRightRadius = 12;
            avatar.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            avatar.style.marginRight = 8;
            avatar.style.marginTop = 4;
            avatar.style.flexShrink = 0;
            avatar.style.paddingTop = 2;
            avatar.style.paddingLeft = 2;
            _streamingMessageContainer.Add(avatar);
            
            var avatarLabel = new Label("AI");
            avatarLabel.style.color = Color.white;
            avatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            avatarLabel.style.fontSize = 10;
            avatarLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            avatar.Add(avatarLabel);
            
            var content = new VisualElement();
            content.name = "streaming-content";
            content.style.flexGrow = 1;
            _streamingMessageContainer.Add(content);
            
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 4;
            content.Add(header);
            
            var nameLabel = new Label("AI Assistant");
            nameLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.fontSize = 13;
            header.Add(nameLabel);
            
            var timestamp = new Label(DateTime.Now.ToString("HH:mm"));
            timestamp.style.color = new Color(0.6f, 0.6f, 0.6f);
            timestamp.style.fontSize = 11;
            timestamp.style.marginLeft = 8;
            header.Add(timestamp);
            
            _streamingMessageText = new Label("▌");
            _streamingMessageText.name = "streaming-text";
            _streamingMessageText.style.whiteSpace = WhiteSpace.Normal;
            _streamingMessageText.style.color = new Color(0.9f, 0.9f, 0.9f);
            content.Add(_streamingMessageText);
            
            EditorApplication.delayCall += () => {
                _chatHistory.scrollOffset = new Vector2(0, float.MaxValue);
            };
        }
        
        private void UpdateStreamingMessage(string content)
        {
            if (_streamingMessageText != null)
            {
                _streamingMessageText.text = content + "▌";
                
                EditorApplication.delayCall += () => {
                    _chatHistory.scrollOffset = new Vector2(0, float.MaxValue);
                };
            }
        }
        
        private int CreateTemporaryArtifact(string title)
        {
            int tempId = _nextArtifactId++;
            
            var artifactContainer = new VisualElement();
            artifactContainer.style.marginBottom = 24;
            artifactContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            artifactContainer.style.borderTopWidth = 1;
            artifactContainer.style.borderBottomWidth = 1;
            artifactContainer.style.borderLeftWidth = 1;
            artifactContainer.style.borderRightWidth = 1;
            artifactContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            artifactContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            artifactContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            artifactContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.paddingLeft = 12;
            header.style.paddingRight = 12;
            header.style.paddingTop = 8;
            header.style.paddingBottom = 8;
            header.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            artifactContainer.Add(header);
            
            var titleLabel = new Label(title);
            titleLabel.style.flexGrow = 1;
            titleLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);
            
            var closeButton = new Button(() => HideCodeArtifact(tempId));
            closeButton.text = "×";
            closeButton.style.width = 24;
            closeButton.style.height = 24;
            closeButton.style.paddingTop = 0;
            closeButton.style.paddingBottom = 0;
            closeButton.style.paddingLeft = 0;
            closeButton.style.paddingRight = 0;
            closeButton.style.fontSize = 16;
            closeButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            header.Add(closeButton);
            
            var codeContent = new VisualElement();
            codeContent.name = "code-content-" + tempId;
            codeContent.style.paddingLeft = 12;
            codeContent.style.paddingRight = 12;
            codeContent.style.paddingTop = 12;
            codeContent.style.paddingBottom = 12;
            artifactContainer.Add(codeContent);
            
            var waitingLabel = new Label("Generating code...");
            waitingLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            codeContent.Add(waitingLabel);
            
            if (_artifactsScrollView.childCount == 1 && 
                _artifactsScrollView.Children().FirstOrDefault() is Label label && 
                label.text.Contains("No code artifacts yet"))
            {
                _artifactsScrollView.Clear();
            }
            
            _artifactElements[tempId] = artifactContainer;
            _artifactsScrollView.Add(artifactContainer);
            
            ShowCodeArtifact(tempId);
            
            return tempId;
        }
        
        private void UpdateTemporaryArtifact(string code)
        {
            if (_tempArtifactId < 0 || !_artifactElements.TryGetValue(_tempArtifactId, out var element))
                return;
                
            var codeContent = element.Q<VisualElement>("code-content-" + _tempArtifactId);
            if (codeContent != null)
            {
                codeContent.Clear();
                
                try {
                    var highlighted = UISyntaxHighlighter.CreateHighlightedCodeElement(code);
                    codeContent.Add(highlighted);
                }
                catch (System.Exception) {
                    var codeField = new TextField();
                    codeField.multiline = true;
                    codeField.SetValueWithoutNotify(code);
                    codeField.isReadOnly = true;
                    codeField.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    codeField.style.color = new Color(0.9f, 0.9f, 0.9f);
                    
                    codeField.Q(TextField.textInputUssName).style.alignSelf = Align.Auto;
                    codeField.Q(TextField.textInputUssName).style.color = new Color(0.9f, 0.9f, 0.9f);
                    codeField.Q(TextField.textInputUssName).style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    
                    codeContent.Add(codeField);
                }
            }
        }
        
        private void RemoveArtifact(int artifactId)
        {
            if (artifactId < 0) return;
            
            if (_artifactElements.TryGetValue(artifactId, out var element))
            {
                _artifactsScrollView.Remove(element);
                _artifactElements.Remove(artifactId);
                
                if (_activeArtifactId == artifactId)
                {
                    _activeArtifactId = 0;
                }
            }
        }

        private string ProcessStreamingContentForDisplay(string content)
        {
            try
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    content, 
                    @"```(?:csharp|cs|C#)?\s*([\s\S]*?)(?:```|\Z)"
                );

                if (matches.Count == 0)
                    return content;

                string displayContent = content;
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string fullMatch = match.Value;
                    string codeContent = match.Groups[1].Value.Trim();
                    
                    int lineCount = codeContent.Split('\n').Length;
                    
                    if (lineCount <= SHORT_CODE_MAX_LINES && fullMatch.EndsWith("```"))
                    {
                        continue;
                    }
                    else
                    {
                        displayContent = displayContent.Replace(fullMatch, "[Code Block]");
                    }
                }
                
                return displayContent;
            }
            catch (Exception)
            {
                return content;
            }
        }

        private string TryExtractPartialCode(string content)
        {
            try
            {
                int startIdx = content.IndexOf("```");
                if (startIdx >= 0)
                {
                    int languageEndIdx = content.IndexOf("\n", startIdx);
                    if (languageEndIdx > startIdx)
                    {
                        int endIdx = content.IndexOf("```", languageEndIdx);
                        
                        if (endIdx > languageEndIdx)
                        {
                            return content.Substring(languageEndIdx + 1, endIdx - languageEndIdx - 1).Trim();
                        }
                        
                        return content.Substring(languageEndIdx + 1).Trim();
                    }
                }
                
                if (content.Contains("public class ") || content.Contains("private class ") || content.Contains("internal class ") || content.Contains("protected class "))
                {
                    return content;
                }
            }
            catch (Exception)
            {
                // Ignore exceptions while trying to parse partial content
            }
            
            return "";
        }
        
        private void CancelGeneration()
        {
            if (!_isGenerating) return;
            
            GPTClient.StopGeneration();
            _isGenerating = false;
            _responseFinalizing = false;
            HideProgress();
            
            if (_streamingMessageContainer != null)
            {
                _chatHistory.Remove(_streamingMessageContainer);
                _streamingMessageContainer = null;
                _streamingMessageText = null;
                _streamingBuffer = "";
                _pendingStreamContent = "";
            }
            
            if (_tempArtifactId >= 0)
            {
                RemoveArtifact(_tempArtifactId);
                _tempArtifactId = -1;
            }
            
            AddSystemMessage("Generation canceled.");
        }
        
        private void SubmitCodeUpload()
        {
            string code = "";
            string source = "";
            
            if (_scriptUploadField.value != null)
            {
                MonoScript script = _scriptUploadField.value as MonoScript;
                string path = AssetDatabase.GetAssetPath(script);
                if (File.Exists(path))
                {
                    code = File.ReadAllText(path);
                    source = Path.GetFileName(path);
                }
            }
            else if (!string.IsNullOrEmpty(_codeUploadField.value))
            {
                code = _codeUploadField.value;
                source = "Pasted Code";
                
                string className = ExtractClassName(code);
                if (!string.IsNullOrEmpty(className))
                {
                    source = className + ".cs";
                }
            }
            
            if (!string.IsNullOrEmpty(code))
            {
                var contextFile = new ContextFile(source, code);
                _contextFiles.Add(contextFile);
                
                if (_currentSession != null)
                {
                    _currentSession.contextFiles.Add(contextFile);
                }
                
                RefreshContextFilesUI();
                
                var systemMessage = $"Added code from '{source}' to context.";
                var el = AddSystemMessage(systemMessage, contextFile);
                contextFile.chatInfoMessage = el;
                
                // Hide upload panel
                HideUploadPanel();
            }
            else
            {
                EditorUtility.DisplayDialog("No Code Provided", "Please select a script file or paste code to add context.", "OK");
            }
        }

        
        private string GetContextFilesContent()
        {
            if (_contextFiles.Count == 0) return "";
            
            return string.Join("\n\n// Next file\n\n", _contextFiles.Select(f => $"// {f.filename}\n{f.code}"));
        }
        
        private void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent("Copied to clipboard"));
        }
        
        private void CreateScriptFromCode(string code, string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                className = ExtractClassName(code);
                if (string.IsNullOrEmpty(className))
                {
                    EditorUtility.DisplayDialog("Error", "Could not identify class name in the code.", "OK");
                    return;
                }
            }
            
            string[] existingFiles = AssetDatabase.FindAssets(className);
            bool exists = false;
            string scriptPath = "";
            
            foreach (string guid in existingFiles)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == className && Path.GetExtension(path) == ".cs")
                {
                    exists = true;
                    scriptPath = path;
                    break;
                }
            }
            
            if (exists)
            {
                if (EditorUtility.DisplayDialog("Script already exists", 
                    $"A script with the name '{className}' already exists at {scriptPath}. Do you want to overwrite it?", 
                    "Yes, Overwrite", "No, Cancel"))
                {
                    File.WriteAllText(scriptPath, code);
                    AssetDatabase.ImportAsset(scriptPath);
                    AssetDatabase.Refresh();
                    
                    if (isScriptMonoBehaviour(code) && SelectedGameObjectsWhenOpened.Count > 0)
                    { 
                        EditorPrefs.SetString("PendingComponentClass", className);
                        CompilationPipeline.compilationFinished  += OnCompilationFinished; 
                        AssetDatabase.Refresh();
                    }else{ 
                        // Open the script
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                        AssetDatabase.OpenAsset(Selection.activeObject);
                        // Debug.Log($"Script overwritten: {scriptPath}");
                    }

                    AddSystemMessage($"Overwritten existing script: {className}.cs");
                }
            }
            else
            {
                if (GPTClient.askForSavePath)
                {
                    scriptPath = EditorUtility.SaveFilePanel("Save Script", GPTClient.defaultSavePath, className + ".cs", "cs");
                    
                    if (string.IsNullOrEmpty(scriptPath))
                        return;
                    
                    if (scriptPath.StartsWith(Application.dataPath))
                    {
                        scriptPath = "Assets" + scriptPath.Substring(Application.dataPath.Length);
                    }
                }
                else
                {
                    scriptPath = Path.Combine(GPTClient.defaultSavePath, className + ".cs");
                }
                
                string directory = Path.GetDirectoryName(scriptPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(scriptPath, code);
                AssetDatabase.ImportAsset(scriptPath);
                // Debug.Log($"Script created: {scriptPath}");
                AssetDatabase.Refresh();

                if (isScriptMonoBehaviour(code) && SelectedGameObjectsWhenOpened.Count > 0)
                { 
                    EditorPrefs.SetString("PendingComponentClass", className);
                    CompilationPipeline.compilationFinished += OnCompilationFinished;
                    AssetDatabase.Refresh();
                }
                else
                { 
                    // Open the script
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                    AssetDatabase.OpenAsset(Selection.activeObject);
                }
                
                AddSystemMessage($"Created new script: {className}.cs");
            }
        }

        private void OnCompilationFinished(object obj)
        {
            string currentClassname = EditorPrefs.GetString("PendingComponentClass", ""); 
            if (currentClassname == null || currentClassname == "")
                return;

            CompilationPipeline.compilationFinished -= OnCompilationFinished;

            if (SelectedGameObjectsWhenOpened == null || SelectedGameObjectsWhenOpened.Count == 0)
            {
                Debug.LogWarning("No game objects selected when the script was opened.");
                return;
            }
 
            SelectedGameObjectsWhenOpened = Selection.gameObjects.ToList(); 
        }
        

        static void DelayedComponentAdditionStatic()
        { 
            
            SelectedGameObjectsWhenOpened = Selection.gameObjects.ToList();
            string currentClassname = EditorPrefs.GetString("PendingComponentClass", "");

            EditorApplication.delayCall += () =>
            { 

                Type type = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp")?
                        .GetType(currentClassname);
                if (type != null) {
                    foreach (var obj in SelectedGameObjectsWhenOpened)
                    {
                        if (!obj.GetComponent(type))
                        { 
                            obj.AddComponent(type);
                        } 
                    } 
                } else {
                    Debug.LogWarning($"Could not find type {currentClassname} in Assembly-CSharp or as a MonoBehaviour.");
                }

                EditorPrefs.SetString("PendingComponentClass", "");
                SelectedGameObjectsWhenOpened.Clear();
            };
        }



        private bool isScriptMonoBehaviour(string code)
        {
            bool val = code.Contains("MonoBehaviour") ||
                   code.Contains("Behaviour") ||
                   code.Contains("UnityEngine.MonoBehaviour") ||
                   code.Contains("UnityEngine.Behaviour");

            // Debug.Log($"isScriptMonoBehaviour: {val}");
            return val;
        }
        #endregion

        #region Helper Methods
        private List<ExtractedCodeBlock> ExtractCodeBlocksFromResponse(string response)
        {
            var result = new List<ExtractedCodeBlock>();
            
            var matches = System.Text.RegularExpressions.Regex.Matches(response, @"```(?:csharp|cs|C#)?\s*([\s\S]*?)```");
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string code = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    int lineCount = code.Split('\n').Length;
                    bool isShort = lineCount <= SHORT_CODE_MAX_LINES;
                    
                    result.Add(new ExtractedCodeBlock(code, isShort));
                }
            }
            
            if (result.Count == 0)
            {
                int classIndex = response.IndexOf("public class ");
                if (classIndex >= 0)
                {
                    result.Add(new ExtractedCodeBlock(response.Substring(classIndex).Trim(), false));
                }
            }
            
            return result;
        }
        
        private string ProcessResponseWithCodeBlocks(string response, List<ExtractedCodeBlock> codeBlocks)
        {
            string result = response;
            
            var matches = System.Text.RegularExpressions.Regex.Matches(
                response, 
                @"```(?:csharp|cs|C#)?\s*([\s\S]*?)```"
            );
            
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                string code = match.Groups[1].Value.Trim();
                
                var codeBlock = codeBlocks.FirstOrDefault(cb => cb.code == code);
                
                if (codeBlock != null)
                {
                    if (!codeBlock.isShort)
                    { 
                        result = result.Remove(match.Index, match.Length).Insert(match.Index, "[Code Block]");
                    }
                }
            }
            
            return result;
        }

        private string ExtractClassName(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            
            string className = "";
            int classIndex = -1;
            
            if (code.Contains("public class "))
                classIndex = code.IndexOf("public class ") + 13;
            else if (code.Contains("private class "))
                classIndex = code.IndexOf("private class ") + 14;
            else if (code.Contains("internal class "))
                classIndex = code.IndexOf("internal class ") + 15;
            else if (code.Contains("protected class "))
                classIndex = code.IndexOf("protected class ") + 16;
            
            if (classIndex > 0)
            {
                // Extract text from class definition to first non-alphanumeric character
                string remainingText = code.Substring(classIndex).Trim();
                int endIndex = 0;
                
                while (endIndex < remainingText.Length && 
                      (char.IsLetterOrDigit(remainingText[endIndex]) || remainingText[endIndex] == '_'))
                {
                    endIndex++;
                }
                
                className = remainingText.Substring(0, endIndex).Trim();
            }
            
            return className;
        }
        #endregion

          
        [Serializable]
        private class Serializable<T>
        {
            public T value;
            
            public Serializable(T value)
            {
                this.value = value;
            }
        }
        
        private class RenameSessionPopup : EditorWindow
        {
            private string _currentTitle;
            private System.Action<string> _onRename;
            private TextField _titleField;

            public RenameSessionPopup(string currentTitle, System.Action<string> onRename)
            {
                _currentTitle = currentTitle;
                _onRename = onRename;
                titleContent = new GUIContent("Rename Chat");
            }

            private void CreateGUI()
            {
                var root = rootVisualElement;

                var container = new VisualElement();
                container.style.paddingTop = 16;
                container.style.paddingBottom = 16;
                container.style.paddingLeft = 16;
                container.style.paddingRight = 16;
                root.Add(container);

                var label = new Label("Enter a new name for this chat:");
                label.style.marginBottom = 8;
                container.Add(label);

                _titleField = new TextField();
                _titleField.value = _currentTitle;
                _titleField.style.marginBottom = 16;
                container.Add(_titleField);

                var buttonContainer = new VisualElement();
                buttonContainer.style.flexDirection = FlexDirection.Row;
                buttonContainer.style.justifyContent = Justify.FlexEnd;
                container.Add(buttonContainer);

                var cancelButton = new Button(() => Close());
                cancelButton.text = "Cancel";
                cancelButton.style.marginRight = 8;
                buttonContainer.Add(cancelButton);

                var saveButton = new Button(() =>
                {
                    _onRename?.Invoke(_titleField.value);
                    Close();
                });
                saveButton.text = "Save";
                buttonContainer.Add(saveButton);

                // Set up Enter key for submit
                _titleField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return)
                    {
                        _onRename?.Invoke(_titleField.value);
                        Close();
                    }
                });
            }
        }
    }

  
}