using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

namespace UAI
{
    public class ScriptDocWindow : EditorWindow
    {
        #region Constants
        private string SYSTEM_INIT_PROMPT = "You are a professional Unity/C# developer assistant. Provide clear, concise, and actionable feedback. When suggesting code changes, always provide the complete updated code. You allways answer in " + GPTClient.language;
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.5f;
        
        // PlayerPrefs Keys
        private const string PREF_CODING_PATTERN = "ScriptDoc_CodingPattern";
        private const string PREF_NAMING_FORMAT = "ScriptDoc_NamingFormat";
        private const string PREF_CUSTOM_PROMPTS = "ScriptDoc_CustomPrompts";
        private const string PREF_CONVERSATIONS = "ScriptDoc_Conversations";
        private const string PREF_LAST_SELECTED_SCRIPT = "ScriptDoc_LastSelectedScript";
        private const string PREF_SCRIPTS_TO_ANALYZE = "ScriptDoc_ScriptsToAnalyze";
        #endregion

        #region UIElements
        // Main layout elements
        private VisualElement _rootElement;
        private VisualElement _mainContent;
        private VisualElement _settingsContent;
        
        // Tab system
        private Button _mainTabButton;
        private Button _settingsTabButton;
        
        // Script list panel (left)
        private Button _addScriptButton;
        private Button _clearAllButton;
        private VisualElement _scriptListPanel;
        private ScrollView _scriptListContainer;
        
        // Actions panel
        private VisualElement _actionsPanel;
        
        // Code viewer panel (middle)
        private ScrollView _codeViewerScrollView;
        private Label _codeViewerTitle;
        private VisualElement _codeViewerContent;
        private TextField _codeViewerField;
        private Button _saveChangesButton;
        private Button _revertChangesButton;
        private VisualElement _codeViewerPanel;
        
        // Conversation panel (right)
        private ScrollView _conversationScrollView;
        private TextField _userInputField;
        private Button _sendButton; 
        private DropdownField _quickActionsDropdown;
        private VisualElement _conversationPanel;
        private VisualElement _progressContainer;
        private Label _progressLabel;
        private Button _clearConversationButton;
        private Button _exportConversationButton;
        private Toggle _sendAllScriptsToggle;
        private TextField _additionalInfoField;
        private VisualElement _additionalInfoContainer;
        
        // Settings elements
        private DropdownField _codingPatternDropdown;
        private DropdownField _namingFormatDropdown;
        private TextField _customPromptsField;
        private Button _saveSettingsButton;
        private Button _resetSettingsButton;
        #endregion

        #region Private Fields
        private Dictionary<string, ScriptAnalysisData> _scriptDataCache = new Dictionary<string, ScriptAnalysisData>();
        private List<MonoScript> _scriptsToAnalyze = new List<MonoScript>();
        private MonoScript _selectedScript;
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        private bool _showingSettings = false;
        
        // Settings
        private string _selectedCodingPattern = "None";
        private string _selectedNamingFormat = "PascalCase";
        private string _customPrompts = "";
        
        // Available options
        private readonly List<string> _codingPatterns = new List<string>
        {
            "None", "Railway Pattern", "MVVM", "Clean Architecture", "Repository Pattern", 
            "Factory Pattern", "Observer Pattern", "Singleton Pattern", "Command Pattern"
        };
        
        private readonly List<string> _namingFormats = new List<string>
        {
            "PascalCase", "camelCase", "snake_case", "Custom"
        };
        
        private readonly List<string> _quickActions = new List<string>
        {
            "Select an action...",
            "Analyze for issues",
            "Optimize performance",
            "Add documentation",
            "Improve readability",
            "Fix errors",
            "Refactor code",
            "Add error handling",
            "Convert to async",
            "Add unit tests",
            "Review security"
        };
        #endregion

        #region Data Classes
        [Serializable]
        private class ScriptAnalysisData
        {
            public string scriptGuid;
            public string originalContent;
            public string modifiedContent;
            public List<ConversationEntry> conversation = new List<ConversationEntry>();
            public DateTime lastModified;
            public bool hasChanges;
        }
        
        [Serializable]
        private class ConversationEntry
        {
            public bool isUser;
            public string message;
            public DateTime timestamp;
            public string codeContent; // For storing code snippets in responses
        }
        
        [Serializable]
        private class SerializableConversations
        {
            public List<SerializableScriptData> scripts = new List<SerializableScriptData>();
        }
        
        [Serializable]
        private class SerializableScriptData
        {
            public string scriptGuid;
            public string originalContent;
            public string modifiedContent;
            public List<ConversationEntry> conversation;
            public long lastModifiedTicks;
            public bool hasChanges;
        }
        
        [Serializable]
        public class SerializableStringList
        {
            public List<string> items = new List<string>();
        }
        #endregion

        #region Menu Item
        [MenuItem("Tools/uAI Creator/Script Doctor Pro", false, 159)]
        public static void Init()
        {
            ScriptDocWindow window = GetWindow<ScriptDocWindow>("Script Doctor Pro");
            window.minSize = new Vector2(1600, 800);
            window.Show();
        }
        #endregion

        #region Unity Lifecycle
        private void CreateGUI()
        {
            LoadSettings();
            LoadScriptsList();
            LoadConversations();
            
            _rootElement = rootVisualElement;
            
            // Load UXML and USS
            var visualTree = Resources.Load<VisualTreeAsset>("ScriptDocWindowLayout");
            if (visualTree != null)
            {
                visualTree.CloneTree(_rootElement);
            }
            else
            {
                BuildUIStructureProgrammatically();
            }
            
            var styleSheet = Resources.Load<StyleSheet>("ScriptDocWindow");
            if (styleSheet != null)
            {
                _rootElement.styleSheets.Add(styleSheet);
            }
            
            GetUIReferences();
            InitializeUI();
            SetupEventHandlers();
            
            EditorApplication.update += UpdateProgress;
        }
        
        private void OnDisable()
        {
            SaveSettings();
            SaveScriptsList();
            SaveConversations();
            EditorApplication.update -= UpdateProgress;
        }
        #endregion

        #region UI Building
        private void BuildUIStructureProgrammatically()
        {
            _rootElement.Clear();
            _rootElement.AddToClassList("root");
            
            var container = new VisualElement();
            container.AddToClassList("main-container");
            _rootElement.Add(container);
            
            // Top bar
            CreateTopBar(container);
            
            // Main content
            _mainContent = new VisualElement();
            _mainContent.AddToClassList("main-content");
            container.Add(_mainContent);
            
            // Three column layout
            var threeColumnLayout = new VisualElement();
            threeColumnLayout.AddToClassList("three-column-layout");
            _mainContent.Add(threeColumnLayout);
            
            // Left column with script list and actions
            var leftColumn = new VisualElement();
            leftColumn.AddToClassList("left-column");
            threeColumnLayout.Add(leftColumn);
            
            CreateScriptListPanel(leftColumn);
            CreateActionsPanel(leftColumn);
            
            // Code viewer panel (middle)
            CreateCodeViewerPanel(threeColumnLayout);
            
            // Conversation panel (right)
            CreateConversationPanel(threeColumnLayout);
            
            // Settings content
            _settingsContent = new VisualElement();
            _settingsContent.AddToClassList("settings-content");
            _settingsContent.AddToClassList("hidden");
            container.Add(_settingsContent);
            
            CreateSettingsContent(_settingsContent);
        }
        
        private void CreateTopBar(VisualElement container)
        {
            var topBar = new VisualElement();
            topBar.AddToClassList("top-bar");
            container.Add(topBar);
            
            var logoContainer = new VisualElement();
            logoContainer.AddToClassList("logo-container");
            topBar.Add(logoContainer);
            
            var logoLabel = new Label("UAI Script Doctor Pro");
            logoLabel.AddToClassList("logo");
            logoContainer.Add(logoLabel);
            
            var subtitleLabel = new Label("AI-Powered Code Assistant");
            subtitleLabel.AddToClassList("subtitle");
            logoContainer.Add(subtitleLabel);
            
            var tabContainer = new VisualElement();
            tabContainer.AddToClassList("tab-container");
            topBar.Add(tabContainer);
            
            _mainTabButton = new Button(() => SwitchTab(false));
            _mainTabButton.text = "Assistant";
            _mainTabButton.AddToClassList("tab-button");
            _mainTabButton.AddToClassList("active-tab");
            tabContainer.Add(_mainTabButton);
            
            _settingsTabButton = new Button(() => SwitchTab(true));
            _settingsTabButton.text = "Settings";
            _settingsTabButton.AddToClassList("tab-button");
            tabContainer.Add(_settingsTabButton);
        }
        
        private void CreateScriptListPanel(VisualElement parent)
        {
            _scriptListPanel = new VisualElement();
            _scriptListPanel.AddToClassList("panel-container");
            _scriptListPanel.AddToClassList("script-list-panel");
            parent.Add(_scriptListPanel);
            
            var header = new VisualElement();
            header.AddToClassList("panel-header");
            _scriptListPanel.Add(header);
            
            var title = new Label("Scripts");
            title.AddToClassList("panel-title");
            header.Add(title);
            
            var headerButtons = new VisualElement();
            headerButtons.AddToClassList("header-buttons");
            header.Add(headerButtons);
            
            _clearAllButton = new Button(() => ClearAllScripts());
            _clearAllButton.text = "Clear All";
            _clearAllButton.AddToClassList("header-button");
            _clearAllButton.tooltip = "Remove all scripts";
            headerButtons.Add(_clearAllButton);
            
            var content = new VisualElement();
            content.AddToClassList("panel-content");
            _scriptListPanel.Add(content);
            
            // Script list container
            _scriptListContainer = new ScrollView();
            _scriptListContainer.AddToClassList("script-list-container");
            content.Add(_scriptListContainer);
            
            // Add script button
            _addScriptButton = new Button(() => AddNewScript());
            _addScriptButton.text = "+ Add Script";
            _addScriptButton.AddToClassList("add-script-button");
            content.Add(_addScriptButton);
        }
        
        private void CreateActionsPanel(VisualElement parent)
        {
            _actionsPanel = new VisualElement();
            _actionsPanel.AddToClassList("panel-container");
            _actionsPanel.AddToClassList("actions-panel");
            parent.Add(_actionsPanel);
            
            var header = new VisualElement();
            header.AddToClassList("panel-header");
            _actionsPanel.Add(header);
            
            var title = new Label("Quick Actions");
            title.AddToClassList("panel-title");
            header.Add(title);
            
            var content = new ScrollView();
            content.AddToClassList("panel-content");
            content.AddToClassList("actions-content");
            _actionsPanel.Add(content);
            
            // Build action buttons here
            BuildActionButtons(content);
        }
        
        private void BuildActionButtons(VisualElement parent)
        {
            // Code Quality Checks
            var checkSection = CreateActionSection(parent, "Code Quality Checks");
            AddActionButton(checkSection, "Check for bugs", () => SendQuickAction("Check the following script for bugs"));
            AddActionButton(checkSection, "Check vulnerabilities", () => SendQuickAction("Check the following script for security vulnerabilities"));
            AddActionButton(checkSection, "Check performance", () => SendQuickAction("Check the following script for performance issues"));
            AddActionButton(checkSection, "Check readability", () => SendQuickAction("Check the following script for readability issues"));
            AddActionButton(checkSection, "Check structure", () => SendQuickAction("Check the following script for structure issues"));
            
            // Code Improvements
            var improveSection = CreateActionSection(parent, "Code Improvements");
            AddActionButton(improveSection, "Optimize performance", () => SendQuickAction("Optimize the following script for better performance"));
            AddActionButton(improveSection, "Improve readability", () => SendQuickAction("Improve the readability of the following script"));
            AddActionButton(improveSection, "Improve structure", () => SendQuickAction("Improve the structure of the following script"));
            AddActionButton(improveSection, "Refactor code", () => SendQuickAction("Refactor the following script using best practices"));
            AddActionButton(improveSection, "Better solution", () => SendQuickAction("Give me a better solution for the following script"));
            AddActionButton(improveSection, "Suggestions", () => SendQuickAction("Give me suggestions for improving the following script"));
            
            // Documentation
            var docSection = CreateActionSection(parent, "Documentation");
            AddActionButton(docSection, "Add comments", () => SendQuickAction("Add detailed comments to the following script"));
            AddActionButton(docSection, "Remove comments", () => SendQuickAction("Remove all comments from the following script"));
            AddActionButton(docSection, "Comment functions", () => SendQuickAction("Add comment blocks before each function that explains what they do"));
            AddActionButton(docSection, "Comment classes", () => SendQuickAction("Add comment blocks before each class that explains what they do"));
            AddActionButton(docSection, "Comment variables", () => SendQuickAction("Add comment blocks for important variables that explains what they do"));
            AddActionButton(docSection, "Add XML docs", () => SendQuickAction("Add XML documentation comments to all public members"));
            AddActionButton(docSection, "Generate README", () => SendQuickAction("Generate a README documentation for this script"));
            
            // Error Fixing
            var fixSection = CreateActionSection(parent, "Error Resolution");
            AddActionButton(fixSection, "Fix specific error", () => SendQuickActionWithInput(
                "Fix this error", 
                "Enter the error message:",
                (errorMsg) => $"Fix this error in the script: {errorMsg}"
            ));
            AddActionButton(fixSection, "Fix compile errors", () => SendQuickAction("Fix any compile errors in this script"));
            AddActionButton(fixSection, "Fix warnings", () => SendQuickAction("Fix any warnings in this script"));
            AddActionButton(fixSection, "Add error handling", () => SendQuickAction("Add comprehensive error handling to the following script"));
            AddActionButton(fixSection, "Add null checks", () => SendQuickAction("Add appropriate null checks to prevent NullReferenceExceptions"));
            
            // Unity Specific
            var unitySection = CreateActionSection(parent, "Unity Specific");
            AddActionButton(unitySection, "Unity optimization", () => SendQuickAction("Optimize this script specifically for Unity performance"));
            AddActionButton(unitySection, "Convert to Unity patterns", () => SendQuickAction("Convert this script to follow Unity best practices and patterns"));
            AddActionButton(unitySection, "Add Unity tooltips", () => SendQuickAction("Add [Tooltip] attributes to serialized fields"));
            AddActionButton(unitySection, "Add SerializeField", () => SendQuickAction("Add [SerializeField] to appropriate private fields"));
            AddActionButton(unitySection, "Optimize for mobile", () => SendQuickAction("Optimize this script for mobile performance"));
            AddActionButton(unitySection, "Implement feature", () => SendQuickActionWithInput(
                "Implement feature",
                "Describe the feature to implement:",
                (feature) => $"Implement this feature in the script: {feature}"
            ));
            
            // Advanced Actions
            var advancedSection = CreateActionSection(parent, "Advanced Actions");
            AddActionButton(advancedSection, "Convert to async", () => SendQuickAction("Convert this script to use async/await pattern where appropriate"));
            AddActionButton(advancedSection, "Add unit tests", () => SendQuickAction("Generate unit tests for this script"));
            AddActionButton(advancedSection, "Convert to ECS", () => SendQuickAction("Convert this script to Unity ECS/DOTS"));
            AddActionButton(advancedSection, "Add design patterns", () => SendQuickAction("Apply appropriate design patterns to improve this script"));
            AddActionButton(advancedSection, "Make generic", () => SendQuickAction("Convert appropriate classes/methods to use generics"));
            AddActionButton(advancedSection, "Custom request", () => SendQuickActionWithInput(
                "Custom request",
                "What would you like the AI to do?",
                (request) => request
            ));
        }
        
        private VisualElement CreateActionSection(VisualElement parent, string title)
        {
            var section = new VisualElement();
            section.AddToClassList("action-section");
            parent.Add(section);
            
            var header = new Label(title);
            header.AddToClassList("section-header");
            section.Add(header);
            
            return section;
        }
        
        private void CreateCodeViewerPanel(VisualElement parent)
        {
            _codeViewerPanel = new VisualElement();
            _codeViewerPanel.AddToClassList("panel-container");
            _codeViewerPanel.AddToClassList("code-viewer-panel");
            parent.Add(_codeViewerPanel);
            
            var header = new VisualElement();
            header.AddToClassList("panel-header");
            _codeViewerPanel.Add(header);
            
            _codeViewerTitle = new Label("Code Viewer");
            _codeViewerTitle.AddToClassList("panel-title");
            header.Add(_codeViewerTitle);
            
            var headerButtons = new VisualElement();
            headerButtons.AddToClassList("header-buttons");
            header.Add(headerButtons);
            
            _revertChangesButton = new Button(() => RevertChanges());
            _revertChangesButton.text = "Revert";
            _revertChangesButton.AddToClassList("header-button");
            _revertChangesButton.AddToClassList("hidden");
            headerButtons.Add(_revertChangesButton);
            
            _saveChangesButton = new Button(() => SaveChangesToFile());
            _saveChangesButton.text = "Save";
            _saveChangesButton.AddToClassList("header-button");
            _saveChangesButton.AddToClassList("primary");
            _saveChangesButton.AddToClassList("hidden");
            headerButtons.Add(_saveChangesButton);
            
            _codeViewerScrollView = new ScrollView();
            _codeViewerScrollView.AddToClassList("code-viewer-scroll");
            _codeViewerPanel.Add(_codeViewerScrollView);
            
            _codeViewerContent = new VisualElement();
            _codeViewerContent.AddToClassList("code-viewer-content");
            _codeViewerScrollView.Add(_codeViewerContent);
            
            var emptyState = new Label("Select a script to view its content");
            emptyState.AddToClassList("empty-state-text");
            _codeViewerContent.Add(emptyState);
        }
        
        private void CreateConversationPanel(VisualElement parent)
        {
            _conversationPanel = new VisualElement();
            _conversationPanel.AddToClassList("panel-container");
            _conversationPanel.AddToClassList("conversation-panel");
            parent.Add(_conversationPanel);
            
            var header = new VisualElement();
            header.AddToClassList("panel-header");
            _conversationPanel.Add(header);
            
            var title = new Label("AI Assistant");
            title.AddToClassList("panel-title");
            header.Add(title);
            
            var headerButtons = new VisualElement();
            headerButtons.AddToClassList("header-buttons");
            header.Add(headerButtons);
            
            _exportConversationButton = new Button(() => ExportConversation());
            _exportConversationButton.text = "📥";
            _exportConversationButton.AddToClassList("header-button");
            _exportConversationButton.tooltip = "Export conversation";
            headerButtons.Add(_exportConversationButton);
            
            _clearConversationButton = new Button(() => ClearConversation());
            _clearConversationButton.text = "Clear Chat";
            _clearConversationButton.AddToClassList("header-button");
            _clearConversationButton.tooltip = "Clear conversation history";
            headerButtons.Add(_clearConversationButton);
            
            _conversationScrollView = new ScrollView();
            _conversationScrollView.AddToClassList("conversation-scroll");
            _conversationPanel.Add(_conversationScrollView);
            
            var emptyState = new Label("Select a script and start a conversation");
            emptyState.AddToClassList("empty-state-text");
            _conversationScrollView.Add(emptyState);
            
            // Progress container
            _progressContainer = new VisualElement();
            _progressContainer.AddToClassList("progress-container");
            _progressContainer.AddToClassList("hidden");
            _conversationPanel.Add(_progressContainer);
            
            _progressLabel = new Label("AI is thinking...");
            _progressLabel.AddToClassList("progress-label");
            _progressContainer.Add(_progressLabel);
            
            var progressBar = new VisualElement();
            progressBar.AddToClassList("progress-bar");
            _progressContainer.Add(progressBar);
            
            var progressFill = new VisualElement();
            progressFill.AddToClassList("progress-fill");
            progressBar.Add(progressFill);
            
            // Input section
            var inputSection = new VisualElement();
            inputSection.AddToClassList("input-section");
            _conversationPanel.Add(inputSection);
            
            // Send all scripts toggle
            _sendAllScriptsToggle = new Toggle("Include all scripts for context");
            _sendAllScriptsToggle.AddToClassList("send-all-toggle");
            _sendAllScriptsToggle.tooltip = "When enabled, all scripts in the list will be sent to AI for better context understanding";
            inputSection.Add(_sendAllScriptsToggle);
            
            // Additional info container (hidden by default)
            _additionalInfoContainer = new VisualElement();
            _additionalInfoContainer.AddToClassList("additional-info-container");
            _additionalInfoContainer.AddToClassList("hidden");
            inputSection.Add(_additionalInfoContainer);
            
            var additionalInfoLabel = new Label("Additional Information:");
            additionalInfoLabel.AddToClassList("additional-info-label");
            _additionalInfoContainer.Add(additionalInfoLabel);
            
            _additionalInfoField = new TextField();
            _additionalInfoField.multiline = true;
            _additionalInfoField.AddToClassList("additional-info-field");
            _additionalInfoContainer.Add(_additionalInfoField);
            
            var inputContainer = new VisualElement();
            inputContainer.AddToClassList("input-container");
            inputSection.Add(inputContainer);
            
            _userInputField = new TextField();
            _userInputField.multiline = true;
            _userInputField.AddToClassList("user-input-field");
            var inputWrapper = new VisualElement();
            inputWrapper.AddToClassList("input-wrapper");
            inputWrapper.Add(_userInputField);
            inputContainer.Add(inputWrapper);
            
            _sendButton = new Button(() => SendMessage());
            _sendButton.text = "Send";
            _sendButton.AddToClassList("send-button");
            inputContainer.Add(_sendButton);
        }
        
        private void CreateSettingsContent(VisualElement parent)
        {
            var header = new VisualElement();
            header.AddToClassList("settings-header");
            parent.Add(header);
            
            var title = new Label("Settings");
            title.AddToClassList("settings-title");
            header.Add(title);
            
            var subtitle = new Label("Configure AI behavior and preferences");
            subtitle.AddToClassList("settings-subtitle");
            header.Add(subtitle);
            
            var scrollView = new ScrollView();
            scrollView.AddToClassList("settings-scroll");
            parent.Add(scrollView);
            
            var form = new VisualElement();
            form.AddToClassList("settings-form");
            scrollView.Add(form);
            
            // Coding standards section
            var section = new VisualElement();
            section.AddToClassList("settings-section");
            form.Add(section);
            
            var sectionHeader = new Label("Coding Standards");
            sectionHeader.AddToClassList("settings-section-header");
            section.Add(sectionHeader);
            
            _codingPatternDropdown = new DropdownField("Coding Pattern", _codingPatterns, 0);
            _codingPatternDropdown.AddToClassList("settings-dropdown");
            section.Add(_codingPatternDropdown);
            
            _namingFormatDropdown = new DropdownField("Naming Convention", _namingFormats, 0);
            _namingFormatDropdown.AddToClassList("settings-dropdown");
            section.Add(_namingFormatDropdown);
            
            // Custom prompts section
            var promptsSection = new VisualElement();
            promptsSection.AddToClassList("settings-section");
            form.Add(promptsSection);
            
            var promptsHeader = new Label("Custom Instructions");
            promptsHeader.AddToClassList("settings-section-header");
            promptsSection.Add(promptsHeader);
            
            _customPromptsField = new TextField();
            _customPromptsField.multiline = true;
            _customPromptsField.AddToClassList("custom-prompts-field");
            promptsSection.Add(_customPromptsField);
            
            // Actions
            var actions = new VisualElement();
            actions.AddToClassList("settings-actions");
            form.Add(actions);
            
            _saveSettingsButton = new Button(() => SaveSettings());
            _saveSettingsButton.text = "Save Settings";
            _saveSettingsButton.AddToClassList("primary-button");
            actions.Add(_saveSettingsButton);
            
            _resetSettingsButton = new Button(() => ResetSettings());
            _resetSettingsButton.text = "Reset to Defaults";
            _resetSettingsButton.AddToClassList("secondary-button");
            actions.Add(_resetSettingsButton);
        }
        
        private void AddActionButton(VisualElement container, string text, System.Action clickAction)
        {
            var button = new Button(clickAction);
            button.text = text;
            button.AddToClassList("action-button");
            container.Add(button);
        }
        #endregion

        #region UI Initialization
        private void GetUIReferences()
        {
            // Main tabs
            _mainTabButton = _rootElement.Q<Button>("main-tab-button");
            _settingsTabButton = _rootElement.Q<Button>("settings-tab-button");
            _mainContent = _rootElement.Q<VisualElement>("main-content");
            _settingsContent = _rootElement.Q<VisualElement>("settings-content");
            
            // Script list panel
            _scriptListPanel = _rootElement.Q<VisualElement>("script-list-panel");
            _scriptListContainer = _rootElement.Q<ScrollView>("script-list-container");
            _addScriptButton = _rootElement.Q<Button>("add-script-button");
            _clearAllButton = _rootElement.Q<Button>("clear-all-button");
            
            // Actions panel
            _actionsPanel = _rootElement.Q<VisualElement>("actions-panel");
            
            // Code viewer panel
            _codeViewerScrollView = _rootElement.Q<ScrollView>("code-viewer-scroll");
            _codeViewerTitle = _rootElement.Q<Label>("code-viewer-title");
            _codeViewerContent = _rootElement.Q<VisualElement>("code-viewer-content");
            _saveChangesButton = _rootElement.Q<Button>("save-changes-button");
            _revertChangesButton = _rootElement.Q<Button>("revert-changes-button");
            
            // Conversation panel
            _conversationScrollView = _rootElement.Q<ScrollView>("conversation-scroll");
            _userInputField = _rootElement.Q<TextField>("user-input-field");
            _sendButton = _rootElement.Q<Button>("send-button"); 
            _quickActionsDropdown = _rootElement.Q<DropdownField>("quick-actions-dropdown");
            _progressContainer = _rootElement.Q<VisualElement>("progress-container");
            _progressLabel = _rootElement.Q<Label>("progress-label");
            _clearConversationButton = _rootElement.Q<Button>("clear-conversation-button");
            _exportConversationButton = _rootElement.Q<Button>("export-conversation-button");
            _sendAllScriptsToggle = _rootElement.Q<Toggle>("send-all-scripts-toggle");
            _additionalInfoField = _rootElement.Q<TextField>("additional-info-field");
            _additionalInfoContainer = _rootElement.Q<VisualElement>("additional-info-container");
            
            // Settings
            _codingPatternDropdown = _rootElement.Q<DropdownField>("coding-pattern-dropdown");
            _namingFormatDropdown = _rootElement.Q<DropdownField>("naming-format-dropdown");
            _customPromptsField = _rootElement.Q<TextField>("custom-prompts-field");
            _saveSettingsButton = _rootElement.Q<Button>("save-settings-button");
            _resetSettingsButton = _rootElement.Q<Button>("reset-settings-button");
        }
        
        private void InitializeUI()
        {
            // Initialize dropdowns
            if (_codingPatternDropdown != null)
            {
                _codingPatternDropdown.choices = _codingPatterns;
                _codingPatternDropdown.value = _selectedCodingPattern;
            }
            
            if (_namingFormatDropdown != null)
            {
                _namingFormatDropdown.choices = _namingFormats;
                _namingFormatDropdown.value = _selectedNamingFormat;
            }
            
            if (_customPromptsField != null)
            {
                _customPromptsField.value = _customPrompts;
            }
            
            if (_quickActionsDropdown != null)
            {
                _quickActionsDropdown.choices = _quickActions;
                _quickActionsDropdown.index = 0;
            }
            
            // Build action buttons for the actions panel
            var actionsContent = _actionsPanel?.Q<ScrollView>("actions-content");
            if (actionsContent != null)
            {
                BuildActionButtons(actionsContent);
            }
            
            // Load saved scripts list
            RefreshScriptList();
            
            // Restore last selected script
            string lastSelectedGuid = PlayerPrefs.GetString(PREF_LAST_SELECTED_SCRIPT, "");
            if (!string.IsNullOrEmpty(lastSelectedGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(lastSelectedGuid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && _scriptsToAnalyze.Contains(script))
                {
                    SelectScript(script);
                }
            }
        }
        
        private void SetupEventHandlers()
        {
            // Tab switching
            _mainTabButton?.RegisterCallback<ClickEvent>(evt => SwitchTab(false));
            _settingsTabButton?.RegisterCallback<ClickEvent>(evt => SwitchTab(true));
            
            // Script list
            _addScriptButton?.RegisterCallback<ClickEvent>(evt => AddNewScript());
            _clearAllButton?.RegisterCallback<ClickEvent>(evt => ClearAllScripts());
            
            // Code viewer
            _saveChangesButton?.RegisterCallback<ClickEvent>(evt => SaveChangesToFile());
            _revertChangesButton?.RegisterCallback<ClickEvent>(evt => RevertChanges());
            
            // Conversation
            _sendButton?.RegisterCallback<ClickEvent>(evt => SendMessage());
            _userInputField?.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return && evt.shiftKey)
                {
                    SendMessage();
                }
            });
            _clearConversationButton?.RegisterCallback<ClickEvent>(evt => ClearConversation()); 
            _exportConversationButton?.RegisterCallback<ClickEvent>(evt => ExportConversation());
            
            // Settings
            _codingPatternDropdown?.RegisterValueChangedCallback(evt => _selectedCodingPattern = evt.newValue);
            _namingFormatDropdown?.RegisterValueChangedCallback(evt => _selectedNamingFormat = evt.newValue);
            _customPromptsField?.RegisterValueChangedCallback(evt => _customPrompts = evt.newValue);
            _saveSettingsButton?.RegisterCallback<ClickEvent>(evt => SaveSettings());
            _resetSettingsButton?.RegisterCallback<ClickEvent>(evt => ResetSettings());
        }
        #endregion

        #region Script Management
        private void RefreshScriptList()
        {
            if (_scriptListContainer == null) return;
            
            _scriptListContainer.Clear();
            
            if (_scriptsToAnalyze.Count == 0)
            {
                var emptyLabel = new Label("No scripts added yet.\nClick '+ Add Script' to begin.");
                emptyLabel.AddToClassList("empty-state-text");
                _scriptListContainer.Add(emptyLabel);
                return;
            }
            
            for (int i = 0; i < _scriptsToAnalyze.Count; i++)
            {
                var script = _scriptsToAnalyze[i];
                if (script == null) continue;
                
                var scriptItem = new VisualElement();
                scriptItem.AddToClassList("script-list-item");
                
                // Make item clickable
                scriptItem.RegisterCallback<ClickEvent>(evt => {
                    evt.StopPropagation();
                    SelectScript(script);
                });
                
                // Add selection highlighting
                if (_selectedScript == script)
                {
                    scriptItem.AddToClassList("selected");
                }
                
                // var icon = new Label("📄");
                // icon.AddToClassList("script-icon");
                // scriptItem.Add(icon);
                
                var nameLabel = new Label(script.name);
                nameLabel.AddToClassList("script-name");
                scriptItem.Add(nameLabel);
                
                // Change indicator
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script));
                if (_scriptDataCache.ContainsKey(guid) && _scriptDataCache[guid].hasChanges)
                {
                    var indicator = new VisualElement();
                    indicator.AddToClassList("change-indicator");
                    scriptItem.Add(indicator);
                }
                
                // Remove button
                var removeButton = new Button(() => {
                    RemoveScript(script);
                });
                removeButton.text = "×";
                removeButton.AddToClassList("remove-script-button");
                removeButton.tooltip = "Remove script";
                scriptItem.Add(removeButton);
                
                _scriptListContainer.Add(scriptItem);
            }
        }
        
        private void AddNewScript()
        {
            string path = EditorUtility.OpenFilePanel("Select Script", "Assets", "cs");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert to relative path
                string relativePath = path;
                if (path.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath);
                
                if (script != null && !_scriptsToAnalyze.Contains(script))
                {
                    _scriptsToAnalyze.Add(script);
                    RefreshScriptList();
                    SelectScript(script);
                    SaveScriptsList();
                }
                else if (script == null)
                {
                    EditorUtility.DisplayDialog("Error", "Could not load the selected script. Make sure it's within the Assets folder.", "OK");
                }
                else if (_scriptsToAnalyze.Contains(script))
                {
                    EditorUtility.DisplayDialog("Info", "This script is already in the list.", "OK");
                }
            }
        }
        
        private void RemoveScript(MonoScript script)
        {
            if (script == null) return;
            
            _scriptsToAnalyze.Remove(script);
            
            if (_selectedScript == script)
            {
                _selectedScript = null;
                DisplayEmptyCodeViewer();
                DisplayEmptyConversation();
            }
            
            RefreshScriptList();
            SaveScriptsList();
        }
        
        private void ClearAllScripts()
        {
            if (_scriptsToAnalyze.Count == 0) return;
            
            if (EditorUtility.DisplayDialog("Clear All Scripts", 
                "Remove all scripts from the list?", 
                "Clear All", "Cancel"))
            {
                _scriptsToAnalyze.Clear();
                _selectedScript = null;
                DisplayEmptyCodeViewer();
                DisplayEmptyConversation();
                RefreshScriptList();
                SaveScriptsList();
            }
        }
        
        private void SelectScript(MonoScript script)
        {
            if (script == null) return;
            
            _selectedScript = script;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script));
            PlayerPrefs.SetString(PREF_LAST_SELECTED_SCRIPT, guid);
            
            // Load or create script data
            if (!_scriptDataCache.ContainsKey(guid))
            {
                var data = new ScriptAnalysisData
                {
                    scriptGuid = guid,
                    originalContent = script.text,
                    modifiedContent = script.text,
                    lastModified = DateTime.Now,
                    hasChanges = false
                };
                _scriptDataCache[guid] = data;
            }
            
            // Update UI
            DisplayScriptContent();
            DisplayConversation();
            RefreshScriptList();
            
            _codeViewerTitle.text = script.name + ".cs";
        }
        
        private void DisplayEmptyCodeViewer()
        {
            if (_codeViewerContent == null) return;
            
            _codeViewerContent.Clear();
            var emptyLabel = new Label("Select a script to view its content");
            emptyLabel.AddToClassList("empty-state-text");
            _codeViewerContent.Add(emptyLabel);
            
            _codeViewerTitle.text = "Code Viewer";
            _saveChangesButton?.AddToClassList("hidden");
            _revertChangesButton?.AddToClassList("hidden");
        }
        
        private void DisplayEmptyConversation()
        {
            if (_conversationScrollView == null) return;
            
            _conversationScrollView.Clear();
            var emptyLabel = new Label("Select a script and start a conversation");
            emptyLabel.AddToClassList("empty-state-text");
            _conversationScrollView.Add(emptyLabel);
        }
        
        private void DisplayScriptContent()
        {
            if (_selectedScript == null || _codeViewerContent == null) return;
            
            _codeViewerContent.Clear();
            
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
            var data = _scriptDataCache[guid];
            
            _codeViewerField = new TextField();
            _codeViewerField.multiline = true;
            _codeViewerField.value = data.modifiedContent;
            _codeViewerField.AddToClassList("code-viewer-field");
            
            _codeViewerField.RegisterValueChangedCallback(evt => {
                data.modifiedContent = evt.newValue;
                data.hasChanges = data.modifiedContent != data.originalContent;
                UpdateSaveButtonVisibility();
                RefreshScriptList();
            });
            
            _codeViewerContent.Add(_codeViewerField);
            UpdateSaveButtonVisibility();
        }
        
        private void UpdateSaveButtonVisibility()
        {
            if (_selectedScript == null) return;
            
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
            var data = _scriptDataCache[guid];
            
            if (data.hasChanges)
            {
                _saveChangesButton?.RemoveFromClassList("hidden");
                _revertChangesButton?.RemoveFromClassList("hidden");
            }
            else
            {
                _saveChangesButton?.AddToClassList("hidden");
                _revertChangesButton?.AddToClassList("hidden");
            }
        }
        
        private void SaveChangesToFile()
        {
            if (_selectedScript == null) return;
            
            string path = AssetDatabase.GetAssetPath(_selectedScript);
            string guid = AssetDatabase.AssetPathToGUID(path);
            var data = _scriptDataCache[guid];
            
            if (EditorUtility.DisplayDialog("Save Changes", 
                $"Save changes to {_selectedScript.name}.cs?", 
                "Save", "Cancel"))
            {
                File.WriteAllText(path, data.modifiedContent);
                data.originalContent = data.modifiedContent;
                data.hasChanges = false;
                data.lastModified = DateTime.Now;
                
                AssetDatabase.Refresh();
                UpdateSaveButtonVisibility();
                RefreshScriptList();
                
                EditorUtility.DisplayDialog("Success", "Changes saved successfully!", "OK");
            }
        }
        
        private void RevertChanges()
        {
            if (_selectedScript == null) return;
            
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
            var data = _scriptDataCache[guid];
            
            if (EditorUtility.DisplayDialog("Revert Changes", 
                "Revert all changes to the original content?", 
                "Revert", "Cancel"))
            {
                data.modifiedContent = data.originalContent;
                data.hasChanges = false;
                
                DisplayScriptContent();
                RefreshScriptList();
            }
        }
        
        private void SaveScriptsList()
        {
            var scriptGuids = new SerializableStringList();
            scriptGuids.items = new List<string>();
            
            foreach (var script in _scriptsToAnalyze)
            {
                if (script != null)
                {
                    string path = AssetDatabase.GetAssetPath(script);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        scriptGuids.items.Add(guid);
                    }
                }
            }
            
            string json = JsonUtility.ToJson(scriptGuids);
            PlayerPrefs.SetString(PREF_SCRIPTS_TO_ANALYZE, json);
            PlayerPrefs.Save();
        }
        
        private void LoadScriptsList()
        {
            string json = PlayerPrefs.GetString(PREF_SCRIPTS_TO_ANALYZE, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var scriptGuids = JsonUtility.FromJson<SerializableStringList>(json);
                    _scriptsToAnalyze.Clear();
                    
                    foreach (string guid in scriptGuids.items)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                            if (script != null)
                            {
                                _scriptsToAnalyze.Add(script);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to load scripts list: " + e.Message);
                }
            }
        }
        #endregion

        #region Conversation Management
        private void DisplayConversation()
        {
            if (_selectedScript == null || _conversationScrollView == null) return;
            
            _conversationScrollView.Clear();
            
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
            if (!_scriptDataCache.ContainsKey(guid)) return;
            
            var data = _scriptDataCache[guid];
            
            if (data.conversation.Count == 0)
            {
                var emptyState = new Label($"No conversation yet for {_selectedScript.name}.\nUse the quick actions or ask a question!");
                emptyState.AddToClassList("empty-state-text");
                _conversationScrollView.Add(emptyState);
            }
            else
            {
                foreach (var entry in data.conversation)
                {
                    AddConversationEntry(entry);
                }
            }
            
            // Scroll to bottom
            _conversationScrollView.schedule.Execute(() => {
                _conversationScrollView.scrollOffset = new Vector2(0, _conversationScrollView.contentContainer.layout.height);
            }).ExecuteLater(1);
        }
        
        private void AddConversationEntry(ConversationEntry entry)
        {
            var messageContainer = new VisualElement();
            messageContainer.AddToClassList("message-container");
            messageContainer.AddToClassList(entry.isUser ? "user-message" : "ai-message");
            
            var header = new VisualElement();
            header.AddToClassList("message-header");
            messageContainer.Add(header);
            
            var author = new Label(entry.isUser ? "You" : "AI Assistant");
            author.AddToClassList("message-author");
            header.Add(author);
            
            var timestamp = new Label(entry.timestamp.ToString("HH:mm"));
            timestamp.AddToClassList("message-timestamp");
            header.Add(timestamp);
            
            var content = new VisualElement();
            content.AddToClassList("message-content");
            messageContainer.Add(content);
            
            // Parse message for code blocks
            var parts = SplitMessageByCode(entry.message);
            foreach (var part in parts)
            {
                if (part.isCode)
                {
                    var codeBlock = CreateCodeBlock(part.content, part.language);
                    content.Add(codeBlock);
                }
                else
                {
                    var textLabel = new Label(part.content.Trim());
                    textLabel.AddToClassList("message-text");
                    textLabel.style.whiteSpace = WhiteSpace.Normal;
                    content.Add(textLabel);
                }
            }
            
            _conversationScrollView.Add(messageContainer);
        }
        
        private List<(bool isCode, string content, string language)> SplitMessageByCode(string message)
        {
            var parts = new List<(bool isCode, string content, string language)>();
            var codeBlockPattern = @"```(\w*(?::\w+)?)\n?([\s\S]*?)```";
            var matches = Regex.Matches(message, codeBlockPattern);
            
            int lastIndex = 0;
            foreach (Match match in matches)
            {
                // Add text before code block
                if (match.Index > lastIndex)
                {
                    var textContent = message.Substring(lastIndex, match.Index - lastIndex);
                    if (!string.IsNullOrWhiteSpace(textContent))
                    {
                        parts.Add((false, textContent, ""));
                    }
                }
                
                // Add code block
                string language = match.Groups[1].Value;
                string code = match.Groups[2].Value.Trim();
                parts.Add((true, code, language));
                
                lastIndex = match.Index + match.Length;
            }
            
            // Add remaining text
            if (lastIndex < message.Length)
            {
                var remainingText = message.Substring(lastIndex);
                if (!string.IsNullOrWhiteSpace(remainingText))
                {
                    parts.Add((false, remainingText, ""));
                }
            }
            
            // If no code blocks found, treat entire message as text
            if (parts.Count == 0 && !string.IsNullOrWhiteSpace(message))
            {
                parts.Add((false, message, ""));
            }
            
            return parts;
        }
        
        private VisualElement CreateCodeBlock(string code, string language = "csharp")
        {
            var codeContainer = new VisualElement();
            codeContainer.AddToClassList("code-block-container");
            
            var header = new VisualElement();
            header.AddToClassList("code-block-header");
            codeContainer.Add(header);
            
            var labelContainer = new VisualElement();
            labelContainer.AddToClassList("code-block-label-container");
            header.Add(labelContainer);
            
            // Extract class name if provided in format csharp:ClassName
            string className = "";
            string displayLanguage = language;
            if (language.Contains(":"))
            {
                var parts = language.Split(':');
                displayLanguage = parts[0];
                className = parts.Length > 1 ? parts[1] : "";
            }
            
            var label = new Label(string.IsNullOrEmpty(className) ? displayLanguage : $"{displayLanguage} - {className}");
            label.AddToClassList("code-block-label");
            labelContainer.Add(label);
            
            // Line count
            var lineCount = code.Split('\n').Length;
            var linesLabel = new Label($"{lineCount} lines");
            linesLabel.AddToClassList("code-block-lines");
            labelContainer.Add(linesLabel);
            
            var buttons = new VisualElement();
            buttons.AddToClassList("code-block-buttons");
            header.Add(buttons);
            
            // Expand/Collapse button for long code
            Button expandButton = null;
            var isExpanded = false;
            
            if (lineCount > 20)
            {
                expandButton = new Button();
                expandButton.text = "Expand";
                expandButton.AddToClassList("code-block-button");
                buttons.Add(expandButton);
            }
            
            var copyButton = new Button(() => CopyToClipboard(code));
            copyButton.text = "Copy";
            copyButton.AddToClassList("code-block-button");
            buttons.Add(copyButton);
            
            // Determine which script to apply to
            MonoScript targetScript = _selectedScript;
            if (!string.IsNullOrEmpty(className))
            {
                // Try to find the script by class name
                foreach (var script in _scriptsToAnalyze)
                {
                    if (script != null && (script.name == className || code.Contains($"class {className}")))
                    {
                        targetScript = script;
                        break;
                    }
                }
            }
            
            var applyButton = new Button(() => ApplyCodeToScript(code, targetScript));
            applyButton.text = targetScript == _selectedScript ? "Apply to Script" : $"Apply to {targetScript.name}";
            applyButton.AddToClassList("code-block-button");
            applyButton.AddToClassList("primary");
            buttons.Add(applyButton);
            
            var codeScrollView = new ScrollView();
            codeScrollView.AddToClassList("code-block-scroll");
            
            // Set initial height for long code
            if (lineCount > 20)
            {
                codeScrollView.style.maxHeight = 400; // Show ~20 lines
                codeScrollView.AddToClassList("collapsed");
            }
            
            var codeField = new TextField();
            codeField.multiline = true;
            codeField.value = code;
            codeField.isReadOnly = true;
            codeField.AddToClassList("code-block-field");
            codeScrollView.Add(codeField);
            
            codeContainer.Add(codeScrollView);
            
            // Setup expand/collapse functionality
            if (expandButton != null)
            {
                expandButton.clicked += () => {
                    isExpanded = !isExpanded;
                    if (isExpanded)
                    {
                        codeScrollView.style.maxHeight = StyleKeyword.None;
                        codeScrollView.RemoveFromClassList("collapsed");
                        expandButton.text = "Collapse";
                    }
                    else
                    {
                        codeScrollView.style.maxHeight = 400;
                        codeScrollView.AddToClassList("collapsed");
                        expandButton.text = "Expand";
                    }
                };
            }
            
            return codeContainer;
        }
        
        private void SendMessage()
        {
            if (_selectedScript == null || string.IsNullOrWhiteSpace(_userInputField.value)) return;
            if (_isGenerating) return;
            
            string userMessage = _userInputField.value;
            _userInputField.value = "";
            
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
            var data = _scriptDataCache[guid];
            
            // Add user message
            var userEntry = new ConversationEntry
            {
                isUser = true,
                message = userMessage,
                timestamp = DateTime.Now
            };
            data.conversation.Add(userEntry);
            AddConversationEntry(userEntry);
            
            // Send to AI
            ProcessAIRequest(userMessage, data);
        }
        
        private void SendQuickAction(string action)
        {
            if (_selectedScript == null) return;
            if (_isGenerating) return;
            
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
            var data = _scriptDataCache[guid];
            
            // Add user message
            var userEntry = new ConversationEntry
            {
                isUser = true,
                message = action,
                timestamp = DateTime.Now
            };
            data.conversation.Add(userEntry);
            AddConversationEntry(userEntry);
            
            // Send to AI
            ProcessAIRequest(action, data);
        }
        
        private void SendQuickActionWithInput(string actionName, string inputPrompt, Func<string, string> buildMessage)
        {
            if (_selectedScript == null) return;
            if (_isGenerating) return;
            
            // Show the additional info field
            _additionalInfoContainer?.RemoveFromClassList("hidden");
            _additionalInfoField.value = "";
            
            // Update the label
            var label = _additionalInfoContainer?.Q<Label>(className: "additional-info-label");
            if (label != null)
            {
                label.text = inputPrompt;
            }
            
            // Focus on the additional info field
            _additionalInfoField.Focus();
            
            // Store the original click handler
            System.Action originalHandler = SendMessage;
            
            // Remove the original handler and add the special one
            _sendButton.UnregisterCallback<ClickEvent>(evt => originalHandler());
            
            System.Action specialHandler = null;
            specialHandler = () => {
                string additionalInfo = _additionalInfoField.value;
                if (string.IsNullOrWhiteSpace(additionalInfo))
                {
                    EditorUtility.DisplayDialog("Input Required", $"Please provide {inputPrompt.ToLower()}", "OK");
                    return;
                }
                
                // Hide the additional info field
                _additionalInfoContainer?.AddToClassList("hidden");
                _additionalInfoField.value = "";
                
                // Restore normal send button behavior
                _sendButton.UnregisterCallback<ClickEvent>(evt => specialHandler());
                _sendButton.RegisterCallback<ClickEvent>(evt => originalHandler());
                
                // Send the action with the additional info
                string fullMessage = buildMessage(additionalInfo);
                SendQuickAction(fullMessage);
            };
            
            _sendButton.RegisterCallback<ClickEvent>(evt => specialHandler());
        }
        
        private void ProcessAIRequest(string userMessage, ScriptAnalysisData data)
        {
            _isGenerating = true;
            ShowProgress("AI is analyzing your request...");
            
            string prompt = BuildPromptForConversation(userMessage, data);
            
            GPTClient.Instance.SystemInitPrompt = SYSTEM_INIT_PROMPT;
            GPTClient.Instance.OnResponseReceived = null;
            GPTClient.Instance.OnResponseReceived += (response, index) => {
                _isGenerating = false;
                HideProgress();
                
                var aiEntry = new ConversationEntry
                {
                    isUser = false,
                    message = response,
                    timestamp = DateTime.Now
                };
                data.conversation.Add(aiEntry);
                AddConversationEntry(aiEntry);
                
                // Auto-scroll to bottom
                _conversationScrollView.schedule.Execute(() => {
                    _conversationScrollView.scrollOffset = new Vector2(0, _conversationScrollView.contentContainer.layout.height);
                }).ExecuteLater(1);
                
                SaveConversations();
            };
            
            GPTClient.Instance.SendRequest(prompt);
        }
        
        private string BuildPromptForConversation(string userMessage, ScriptAnalysisData data)
        {
            var promptBuilder = new System.Text.StringBuilder();
            
            // Add context
            promptBuilder.AppendLine($"You are analyzing a Unity C# script named '{_selectedScript.name}'.");
            promptBuilder.AppendLine($"IMPORTANT: The MAIN script you are editing is '{_selectedScript.name}'. This is the script the user wants to modify.");
            promptBuilder.AppendLine("When suggesting code changes:");
            promptBuilder.AppendLine("1. Always provide the COMPLETE updated script code, not just snippets");
            promptBuilder.AppendLine("2. If modifying multiple scripts, clearly label each one with its class name");
            promptBuilder.AppendLine("3. Use the format: ```csharp:ClassName for code blocks to identify which script it belongs to");
            
            // Add settings
            if (_selectedCodingPattern != "None")
                promptBuilder.AppendLine($"Follow {_selectedCodingPattern} principles.");
            promptBuilder.AppendLine($"Use {_selectedNamingFormat} naming convention.");
            
            if (!string.IsNullOrEmpty(_customPrompts))
            {
                promptBuilder.AppendLine("Additional instructions:");
                promptBuilder.AppendLine(_customPrompts);
            }
            
            // Add current script content
            promptBuilder.AppendLine($"\nMAIN SCRIPT TO EDIT - {_selectedScript.name}.cs:");
            promptBuilder.AppendLine("```csharp");
            promptBuilder.AppendLine(data.modifiedContent);
            promptBuilder.AppendLine("```");
            
            // Add all other scripts if toggle is enabled
            if (_sendAllScriptsToggle != null && _sendAllScriptsToggle.value && _scriptsToAnalyze.Count > 1)
            {
                promptBuilder.AppendLine("\nADDITIONAL SCRIPTS FOR CONTEXT (DO NOT EDIT UNLESS NECESSARY):");
                foreach (var script in _scriptsToAnalyze)
                {
                    if (script != null && script != _selectedScript)
                    {
                        string scriptGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script));
                        string scriptContent = script.text;
                        
                        // Use cached content if available
                        if (_scriptDataCache.ContainsKey(scriptGuid))
                        {
                            scriptContent = _scriptDataCache[scriptGuid].modifiedContent;
                        }
                        
                        promptBuilder.AppendLine($"\n--- {script.name}.cs (Reference Only) ---");
                        promptBuilder.AppendLine("```csharp");
                        promptBuilder.AppendLine(scriptContent);
                        promptBuilder.AppendLine("```");
                    }
                }
            }
            
            // Add conversation history (last 5 messages for context)
            if (data.conversation.Count > 1)
            {
                promptBuilder.AppendLine("\nRecent conversation:");
                var recentMessages = data.conversation.TakeLast(6).Take(5); // Exclude the current user message
                foreach (var entry in recentMessages)
                {
                    if (entry.message.Length > 200)
                    {
                        promptBuilder.AppendLine($"{(entry.isUser ? "User" : "Assistant")}: {entry.message.Substring(0, 200)}...");
                    }
                    else
                    {
                        promptBuilder.AppendLine($"{(entry.isUser ? "User" : "Assistant")}: {entry.message}");
                    }
                }
            }
            
            // Add user request
            promptBuilder.AppendLine($"\nUser request: {userMessage}");
            promptBuilder.AppendLine($"\nFocus on modifying the MAIN script '{_selectedScript.name}' unless the user specifically asks about other scripts.");
            promptBuilder.AppendLine("When providing code, use the format ```csharp:ClassName to identify which script the code belongs to.");
            
            return promptBuilder.ToString();
        }
        
        private void ClearConversation()
        {
            if (_selectedScript == null) return;
            
            if (EditorUtility.DisplayDialog("Clear Conversation", 
                "Clear all conversation history for this script?", 
                "Clear", "Cancel"))
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
                if (_scriptDataCache.ContainsKey(guid))
                {
                    _scriptDataCache[guid].conversation.Clear();
                    DisplayConversation();
                    SaveConversations();
                }
            }
        }
        
        private void ExportConversation()
        {
            if (_selectedScript == null) return;
            
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
            if (!_scriptDataCache.ContainsKey(guid) || _scriptDataCache[guid].conversation.Count == 0)
            {
                EditorUtility.DisplayDialog("No Conversation", "No conversation to export.", "OK");
                return;
            }
            
            string path = EditorUtility.SaveFilePanel(
                "Export Conversation", 
                "", 
                $"{_selectedScript.name}_conversation_{DateTime.Now:yyyyMMdd_HHmmss}.txt", 
                "txt");
            
            if (string.IsNullOrEmpty(path)) return;
            
            var data = _scriptDataCache[guid];
            var export = new System.Text.StringBuilder();
            
            export.AppendLine($"Script Doctor Pro - Conversation Export");
            export.AppendLine($"Script: {_selectedScript.name}");
            export.AppendLine($"Date: {DateTime.Now}");
            export.AppendLine(new string('=', 50));
            export.AppendLine();
            
            foreach (var entry in data.conversation)
            {
                export.AppendLine($"{(entry.isUser ? "YOU" : "AI ASSISTANT")} [{entry.timestamp:yyyy-MM-dd HH:mm:ss}]:");
                export.AppendLine(entry.message);
                export.AppendLine();
                export.AppendLine(new string('-', 30));
                export.AppendLine();
            }
            
            File.WriteAllText(path, export.ToString());
            EditorUtility.DisplayDialog("Export Complete", "Conversation exported successfully!", "OK");
        }
        
        private void ApplyCodeToScript(string code, MonoScript targetScript = null)
        {
            if (targetScript == null) targetScript = _selectedScript;
            if (targetScript == null) return;
            
            string scriptName = targetScript.name;
            string confirmMessage = targetScript == _selectedScript 
                ? "Replace the current script content with this code?" 
                : $"Apply this code to {scriptName}.cs?";
            
            if (EditorUtility.DisplayDialog("Apply Code", confirmMessage, "Apply", "Cancel"))
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetScript));
                
                // Ensure we have data for this script
                if (!_scriptDataCache.ContainsKey(guid))
                {
                    _scriptDataCache[guid] = new ScriptAnalysisData
                    {
                        scriptGuid = guid,
                        originalContent = targetScript.text,
                        modifiedContent = targetScript.text,
                        lastModified = DateTime.Now,
                        hasChanges = false
                    };
                }
                
                var data = _scriptDataCache[guid];
                data.modifiedContent = code;
                data.hasChanges = true;
                
                // If applying to current script, update the viewer
                if (targetScript == _selectedScript && _codeViewerField != null)
                {
                    _codeViewerField.value = code;
                    UpdateSaveButtonVisibility();
                }
                
                RefreshScriptList();
                
                // Show a message if applying to a different script
                if (targetScript != _selectedScript)
                {
                    EditorUtility.DisplayDialog("Code Applied", 
                        $"Code has been applied to {scriptName}.cs. Switch to that script to see the changes.", 
                        "OK");
                }
            }
        }
        #endregion

        #region Settings Management
        private void LoadSettings()
        {
            _selectedCodingPattern = PlayerPrefs.GetString(PREF_CODING_PATTERN, "None");
            _selectedNamingFormat = PlayerPrefs.GetString(PREF_NAMING_FORMAT, "PascalCase");
            _customPrompts = PlayerPrefs.GetString(PREF_CUSTOM_PROMPTS, "");
        }
        
        private void SaveSettings()
        {
            PlayerPrefs.SetString(PREF_CODING_PATTERN, _selectedCodingPattern);
            PlayerPrefs.SetString(PREF_NAMING_FORMAT, _selectedNamingFormat);
            PlayerPrefs.SetString(PREF_CUSTOM_PROMPTS, _customPrompts);
            PlayerPrefs.Save();
            
            // EditorUtility.DisplayDialog("Settings Saved", "Your settings have been saved.", "OK");
        }
        
        private void ResetSettings()
        {
            _selectedCodingPattern = "None";
            _selectedNamingFormat = "PascalCase";
            _customPrompts = "";
            
            _codingPatternDropdown.value = _selectedCodingPattern;
            _namingFormatDropdown.value = _selectedNamingFormat;
            _customPromptsField.value = _customPrompts;
            
            SaveSettings();
        }
        
        private void LoadConversations()
        {
            string json = PlayerPrefs.GetString(PREF_CONVERSATIONS, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var saved = JsonUtility.FromJson<SerializableConversations>(json);
                    foreach (var scriptData in saved.scripts)
                    {
                        var data = new ScriptAnalysisData
                        {
                            scriptGuid = scriptData.scriptGuid,
                            originalContent = scriptData.originalContent,
                            modifiedContent = scriptData.modifiedContent,
                            conversation = scriptData.conversation,
                            lastModified = new DateTime(scriptData.lastModifiedTicks),
                            hasChanges = scriptData.hasChanges
                        };
                        _scriptDataCache[scriptData.scriptGuid] = data;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to load conversations: " + e.Message);
                }
            }
        }
        
        private void SaveConversations()
        {
            var toSave = new SerializableConversations();
            
            foreach (var kvp in _scriptDataCache)
            {
                if (kvp.Value.conversation.Count > 0 || kvp.Value.hasChanges)
                {
                    var scriptData = new SerializableScriptData
                    {
                        scriptGuid = kvp.Value.scriptGuid,
                        originalContent = kvp.Value.originalContent,
                        modifiedContent = kvp.Value.modifiedContent,
                        conversation = kvp.Value.conversation,
                        lastModifiedTicks = kvp.Value.lastModified.Ticks,
                        hasChanges = kvp.Value.hasChanges
                    };
                    toSave.scripts.Add(scriptData);
                }
            }
            
            string json = JsonUtility.ToJson(toSave, true);
            PlayerPrefs.SetString(PREF_CONVERSATIONS, json);
            PlayerPrefs.Save();
        }
        #endregion

        #region Helper Methods
        private void SwitchTab(bool showSettings)
        {
            _showingSettings = showSettings;
            
            if (_mainTabButton != null && _settingsTabButton != null)
            {
                _mainTabButton.RemoveFromClassList("active-tab");
                _settingsTabButton.RemoveFromClassList("active-tab");
                
                if (showSettings)
                {
                    _settingsTabButton.AddToClassList("active-tab");
                }
                else
                {
                    _mainTabButton.AddToClassList("active-tab");
                }
            }
            
            if (_mainContent != null && _settingsContent != null)
            {
                if (showSettings)
                {
                    _mainContent.AddToClassList("hidden");
                    _settingsContent.RemoveFromClassList("hidden");
                }
                else
                {
                    _mainContent.RemoveFromClassList("hidden");
                    _settingsContent.AddToClassList("hidden");
                }
            }
        }
        
        private void ShowProgress(string message)
        {
            if (_progressContainer != null)
            {
                _progressContainer.RemoveFromClassList("hidden");
                _progressLabel.text = message;
            }
        }
        
        private void HideProgress()
        {
            if (_progressContainer != null)
            {
                _progressContainer.AddToClassList("hidden");
            }
        }
        
        private void UpdateProgress()
        {
            if (!_isGenerating) return;
            
            _progressValue = Mathf.Repeat(_progressValue + (PROGRESS_BAR_ANIMATION_SPEED * 0.01f), 1f);
            
            var progressFill = _progressContainer?.Q<VisualElement>(className: "progress-fill");
            if (progressFill != null)
            {
                progressFill.style.width = new Length(_progressValue * 100, LengthUnit.Percent);
            }
        }
        
        private void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            Debug.Log("Code copied to clipboard!");
        }
        #endregion
    }
}