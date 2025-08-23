using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;

namespace UAI
{
    public class SenseiAIWindow : EditorWindow
    {
        #region Constants
        private string SYSTEM_INIT_PROMPT = "You are a Teacher who knows everything about Unity3D Engine. You will fully assist the user in their game development process. You will provide detailed and accurate answers to the user's questions. You will also provide code snippets and examples when necessary. You will not provide any irrelevant information or opinions. You will not ask any questions or make any assumptions about the user's intentions. You will only respond to the user's questions and requests in detail, making sure the user understands the information provided. You will not provide any personal opinions or beliefs. You will only provide factual information and examples related to Unity3D Engine. You will not provide any information that is not related to Unity3D Engine. You will not provide any information that is not relevant to the user's question or request. You will not provide any information that is not accurate or up-to-date. You will only provide information that is relevant to the user's question or request, but still in depth. You allways answer in " + GPTClient.language;
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.5f;
        #endregion

        #region UIElements
        // Main layout elements
        private VisualElement _rootElement;
        private VisualElement _mainLayout;
        private VisualElement _sidebar;
        private VisualElement _mainContent;
        private VisualElement _responsePanel;
        
        // Input elements
        private TextField _gameWorldDescriptionField;
        private TextField _codeField;
        private ObjectField _scriptField;
        
        // Buttons
        private Button _teachMeButton; 
        private Button _copyButton;
        private Button _cancelButton;
        
        // Foldout
        private Foldout _extraOptionsFoldout;
        
        // Response elements
        private ScrollView _responseScrollView;
        private VisualElement _progressBar;
        private Label _progressLabel;
        private Label _copiedLabel;
        #endregion

        #region Private Fields
        private string _apiResponse = "";
        private string _gameWorldDescription = "I have the following code, but don't know how to use it.";
        private string _instructions = "Explain me Step by Step.";
        private string _code = "";
        private MonoScript _script;
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        private bool _copied = false;
        #endregion

        #region Menu Item


        [MenuItem("Tools/uAI Creator/Sensei AI", false, 106)]
        public static void Init()
        {
            SenseiAIWindow window = (SenseiAIWindow)GetWindow(typeof(SenseiAIWindow), false, "Sensei AI");
            window.minSize = new Vector2(750, 600);
            window.Show();
        }
        #endregion

        #region Unity Lifecycle Methods
        private void CreateGUI()
        {
            _rootElement = rootVisualElement;
            BuildUIStructure();
            SetupEventHandlers();
            EditorApplication.update += UpdateProgress;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= UpdateProgress;
        }
        #endregion

        #region UI Building
        private void BuildUIStructure()
        {
            _rootElement.Clear();
            
            _mainLayout = new VisualElement();
            _mainLayout.style.flexDirection = FlexDirection.Row;
            _mainLayout.style.flexGrow = 1;
            _rootElement.Add(_mainLayout);
            
            CreateSidebar();
            
            _mainContent = new VisualElement();
            _mainContent.style.flexGrow = 1;
            _mainContent.style.flexDirection = FlexDirection.Column;
            _mainContent.style.paddingLeft = 15;
            _mainContent.style.paddingRight = 15;
            _mainContent.style.paddingTop = 15;
            _mainContent.style.paddingBottom = 15;
            _mainLayout.Add(_mainContent);
            
            CreateMainPanel();
            
            CreateResponseSection();
        }
        
        private void CreateSidebar()
        {
            _sidebar = new VisualElement();
            _sidebar.style.width = 150;
            _sidebar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            _mainLayout.Add(_sidebar);
            
            // Logo area
            var logoArea = new VisualElement();
            logoArea.style.height = 50;
            logoArea.style.minHeight = 30;
            logoArea.style.alignItems = Align.Center;
            logoArea.style.justifyContent = Justify.Center;
            logoArea.style.marginTop = 10;
            logoArea.style.marginBottom = 10;
            _sidebar.Add(logoArea);
            
            var logoLabel = new Label("UAI");
            logoLabel.style.fontSize = 24;
            logoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            logoLabel.style.color = new Color(0.35f, 0.65f, 0.9f);
            logoArea.Add(logoLabel);
            
            EditorHelper.drawSidebarButtons(_sidebar); 
            
            // Style sidebar buttons
            _rootElement.Query<Button>().Class("sidebar-button").ForEach((button) => {
                button.style.height = 40;
                button.style.marginTop = 5;
                button.style.marginBottom = 5;
                button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                button.style.color = Color.white;
                button.style.borderTopWidth = 0;
                button.style.borderBottomWidth = 0;
                button.style.borderLeftWidth = 0;
                button.style.borderRightWidth = 0;
                
                // Add hover effect
                button.RegisterCallback<MouseEnterEvent>(evt => {
                    button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                });
                
                button.RegisterCallback<MouseLeaveEvent>(evt => {
                    button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                });
            });
        }
        
        private void CreateMainPanel()
        {
            // Create a scroll view to contain all content
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            _mainContent.Add(scrollView);
            
            // Header
            var header = new Label("Sensei AI");
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 15;
            scrollView.Add(header);
            
            // Description
            var description = new Label("Your personal AI teacher for learning everything about Unity3D Engine. Ask questions about code, concepts, or specific Unity features.");
            description.style.fontSize = 12;
            description.style.marginBottom = 15;
            description.style.whiteSpace = WhiteSpace.Normal;
            scrollView.Add(description);
            
            // Main question container
            var questionContainer = new Box();
            questionContainer.style.paddingTop = 15;
            questionContainer.style.paddingBottom = 15;
            questionContainer.style.paddingLeft = 15;
            questionContainer.style.paddingRight = 15;
            questionContainer.style.marginBottom = 15;
            scrollView.Add(questionContainer);
            
            var questionLabel = new Label("What do you want to learn?");
            questionLabel.style.fontSize = 14;
            questionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            questionLabel.style.marginBottom = 10;
            questionLabel.style.whiteSpace = WhiteSpace.Normal;
            questionContainer.Add(questionLabel);
            
            _gameWorldDescriptionField = new TextField();
            _gameWorldDescriptionField.multiline = true;
            _gameWorldDescriptionField.value = _gameWorldDescription;
            _gameWorldDescriptionField.style.height = 100;
            _gameWorldDescriptionField.style.whiteSpace = WhiteSpace.Normal;
            _gameWorldDescriptionField.style.unityTextAlign = TextAnchor.UpperLeft;
            _gameWorldDescriptionField.Q("unity-text-input").style.alignSelf = Align.Auto;
            questionContainer.Add(_gameWorldDescriptionField);
            
            // Extra options foldout
            _extraOptionsFoldout = new Foldout();
            _extraOptionsFoldout.text = "Show more options";
            _extraOptionsFoldout.value = false;
            scrollView.Add(_extraOptionsFoldout);
            
            // Code container
            var codeContainer = new Box();
            codeContainer.style.paddingTop = 10;
            codeContainer.style.paddingBottom = 10;
            codeContainer.style.paddingLeft = 10;
            codeContainer.style.paddingRight = 10;
            codeContainer.style.marginBottom = 10;
            _extraOptionsFoldout.Add(codeContainer);
            
            var codeLabel = new Label("Use this, if you have a question about a code snippet");
            codeLabel.style.fontSize = 14;
            codeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            codeLabel.style.marginBottom = 5;
            codeLabel.style.whiteSpace = WhiteSpace.Normal;
            codeContainer.Add(codeLabel);
            
            _codeField = new TextField();
            _codeField.multiline = true;
            _codeField.value = _code;
            _codeField.style.height = 100;
            _codeField.style.whiteSpace = WhiteSpace.Normal;
            _codeField.style.unityTextAlign = TextAnchor.UpperLeft;
            _codeField.Q("unity-text-input").style.alignSelf = Align.Auto;
            codeContainer.Add(_codeField);
            
            // Script container
            var scriptContainer = new Box();
            scriptContainer.style.paddingTop = 10;
            scriptContainer.style.paddingBottom = 10;
            scriptContainer.style.paddingLeft = 10;
            scriptContainer.style.paddingRight = 10;
            _extraOptionsFoldout.Add(scriptContainer);
            
            var scriptLabel = new Label("Use this, if you have a question about a script");
            scriptLabel.style.fontSize = 14;
            scriptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            scriptLabel.style.marginBottom = 5;
            scriptLabel.style.whiteSpace = WhiteSpace.Normal;
            scriptContainer.Add(scriptLabel);
            
            var scriptWarning = new Label("But be careful, since the length is limited. If the script is too long, copy a part of the script, and paste it in the text field above at the end.");
            scriptWarning.style.fontSize = 12;
            scriptWarning.style.marginBottom = 10;
            scriptWarning.style.whiteSpace = WhiteSpace.Normal;
            scriptContainer.Add(scriptWarning);
            
            _scriptField = new ObjectField("Script");
            _scriptField.objectType = typeof(MonoScript);
            _scriptField.style.marginBottom = 10;
            scriptContainer.Add(_scriptField);
            
            // Generate button (centered)
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginTop = 20;
            buttonContainer.style.marginBottom = 10;
            scrollView.Add(buttonContainer);
            
            _teachMeButton = new Button();
            _teachMeButton.text = "Teach me!";
            _teachMeButton.AddToClassList("primary-button");
            buttonContainer.Add(_teachMeButton);
            
            // Style the button
            _teachMeButton.style.height = 40;
            _teachMeButton.style.width = 250;
            _teachMeButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _teachMeButton.style.color = Color.white;
            _teachMeButton.style.fontSize = 14;
            _teachMeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _teachMeButton.style.borderTopWidth = 0;
            _teachMeButton.style.borderBottomWidth = 0;
            _teachMeButton.style.borderLeftWidth = 0;
            _teachMeButton.style.borderRightWidth = 0;
            
            // Add hover effect
            _teachMeButton.RegisterCallback<MouseEnterEvent>(evt => {
                _teachMeButton.style.backgroundColor = new Color(0.45f, 0.75f, 1f);
            });
            
            _teachMeButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _teachMeButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            });
        }
        
        private void CreateResponseSection()
        {
            _responsePanel = new VisualElement();
            _responsePanel.style.display = string.IsNullOrEmpty(_apiResponse) ? DisplayStyle.None : DisplayStyle.Flex;
            _responsePanel.style.borderTopWidth = 1;
            _responsePanel.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            _responsePanel.style.paddingTop = 15;
            _responsePanel.style.marginTop = 15;
            _mainContent.Add(_responsePanel);
            
            // Progress bar container (shown during generation)
            var progressContainer = new VisualElement();
            progressContainer.name = "progress-container";
            progressContainer.style.flexDirection = FlexDirection.Row;
            progressContainer.style.alignItems = Align.Center;
            progressContainer.style.display = DisplayStyle.None;
            progressContainer.style.marginBottom = 10;
            _responsePanel.Add(progressContainer);
            
            _progressLabel = new Label("Generating response...");
            _progressLabel.style.marginRight = 10;
            progressContainer.Add(_progressLabel);
            
            _progressBar = new VisualElement();
            _progressBar.style.flexGrow = 1;
            _progressBar.style.height = 10;
            _progressBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            progressContainer.Add(_progressBar);
            
            var progressFill = new VisualElement();
            progressFill.name = "progress-fill";
            progressFill.style.width = 0;
            progressFill.style.height = 10;
            progressFill.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _progressBar.Add(progressFill);
            
            _cancelButton = new Button(CancelGeneration);
            _cancelButton.text = "Cancel";
            _cancelButton.style.marginLeft = 10;
            _cancelButton.style.width = 80;
            progressContainer.Add(_cancelButton);
            
            // Response header with controls
            var responseHeader = new VisualElement();
            responseHeader.style.flexDirection = FlexDirection.Row;
            responseHeader.style.justifyContent = Justify.SpaceBetween;
            responseHeader.style.marginBottom = 10;
            _responsePanel.Add(responseHeader);
            
            // Response title
            var responseTitle = new Label("Response");
            responseTitle.style.fontSize = 16;
            responseTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            responseHeader.Add(responseTitle);
            
            // Copy section
            var copySection = new VisualElement();
            copySection.style.flexDirection = FlexDirection.Row;
            copySection.style.alignItems = Align.Center;
            responseHeader.Add(copySection);
            
            _copiedLabel = new Label("Copied to clipboard!");
            _copiedLabel.style.display = _copied ? DisplayStyle.Flex : DisplayStyle.None;
            _copiedLabel.style.marginRight = 10;
            _copiedLabel.style.color = new Color(0.35f, 0.65f, 0.9f);
            copySection.Add(_copiedLabel);
            
            _copyButton = new Button(() => CopyToClipboard(_apiResponse));
            _copyButton.text = "Copy All";
            _copyButton.style.width = 80;
            copySection.Add(_copyButton);
            
            // Response scroll view
            _responseScrollView = new ScrollView();
            _responseScrollView.style.height = 250;
            _responseScrollView.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            _responseScrollView.style.borderTopWidth = 1;
            _responseScrollView.style.borderBottomWidth = 1;
            _responseScrollView.style.borderLeftWidth = 1;
            _responseScrollView.style.borderRightWidth = 1;
            _responseScrollView.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            _responseScrollView.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            _responseScrollView.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            _responseScrollView.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            _responsePanel.Add(_responseScrollView);
            
            if (!string.IsNullOrEmpty(_apiResponse))
            {
                AddResponseText(_apiResponse);
            }
        }
        #endregion

        #region Event Handlers
        private void SetupEventHandlers()
        {
            // Game world description field
            if (_gameWorldDescriptionField != null)
                _gameWorldDescriptionField.RegisterValueChangedCallback(evt => _gameWorldDescription = evt.newValue);
                
            // Code field
            if (_codeField != null)
                _codeField.RegisterValueChangedCallback(evt => _code = evt.newValue);
            
            // Script field
            if (_scriptField != null)
                _scriptField.RegisterValueChangedCallback(evt => {
                    _script = evt.newValue as MonoScript;
                    if (_script != null)
                    {
                        _code = _script.text;
                        _codeField.SetValueWithoutNotify(_code);
                    }
                });
                
            // Generate button
            if (_teachMeButton != null)
                _teachMeButton.clicked += () => SendRequestToGPT(BuildPrompt());
        }
        #endregion

        #region Helper Methods
        private string BuildPrompt()
        {
            string prompt = _gameWorldDescription + " - " + _instructions;
            
            if (!string.IsNullOrEmpty(_code))
            {
                prompt += " - The code: " + _code;
            }
            
            return prompt;
        }
        
        private void SendRequestToGPT(string prompt)
        {
            if (_isGenerating) return;
            
            _apiResponse = "";
            _isGenerating = true;
            _progressValue = 0f;
            _copied = false;
            
            ShowProgressBar();
            ShowResponsePanel();
            
            GPTClient.Instance.SystemInitPrompt = SYSTEM_INIT_PROMPT;
            
            GPTClient.Instance.OnResponseReceived = null;
            GPTClient.Instance.OnResponseReceived += OnAPIResponseReceived;
            
            GPTClient.Instance.OnPartResponseReceived = null;
            GPTClient.Instance.OnPartResponseReceived += (response) =>
            {
                _apiResponse += response;
                _copied = false;
                
                EditorApplication.delayCall += () => {
                    UpdateResponsePanel();
                };
            };
            
            GPTClient.Instance.SendRequest(prompt);
        }
        
        private void OnAPIResponseReceived(string response, int index)
        {
            _apiResponse = response;
            _isGenerating = false;
            _copied = false;
            
            HideProgressBar();
            UpdateResponsePanel();
        }
        
        private void CancelGeneration()
        {
            if (!_isGenerating) return;
            
            GPTClient.StopGeneration();
            _isGenerating = false;
            HideProgressBar();
        }
        
        private void UpdateProgress()
        {
            if (!_isGenerating) return;
            
            _progressValue = Mathf.Repeat(_progressValue + (PROGRESS_BAR_ANIMATION_SPEED * 0.01f), 1f);
            
            // Update progress bar
            if (_progressBar != null)
            {
                var progressFill = _progressBar.Q<VisualElement>("progress-fill");
                if (progressFill != null)
                {
                    progressFill.style.width = new Length(_progressValue * 100, LengthUnit.Percent);
                }
            }
        }
        
        private void ShowProgressBar()
        {
            var progressContainer = _responsePanel?.Q<VisualElement>("progress-container");
            if (progressContainer != null)
                progressContainer.style.display = DisplayStyle.Flex;
        }
        
        private void HideProgressBar()
        {
            var progressContainer = _responsePanel?.Q<VisualElement>("progress-container");
            if (progressContainer != null)
                progressContainer.style.display = DisplayStyle.None;
        }
        
        private void ShowResponsePanel()
        {
            if (_responsePanel != null)
                _responsePanel.style.display = DisplayStyle.Flex;
        }
        
        private void UpdateResponsePanel()
        {
            if (_responseScrollView == null) return;
            
            _responseScrollView.Clear();
            AddResponseText(_apiResponse);
        }
        
        private void AddResponseText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            var responseText = new TextField();
            responseText.multiline = true;
            responseText.SetValueWithoutNotify(text);
            responseText.isReadOnly = true;
            responseText.style.whiteSpace = WhiteSpace.Normal;
            responseText.style.unityTextAlign = TextAnchor.UpperLeft;
            responseText.style.backgroundColor = Color.clear;
            responseText.style.borderTopWidth = 0;
            responseText.style.borderBottomWidth = 0;
            responseText.style.borderLeftWidth = 0;
            responseText.style.borderRightWidth = 0;
            
            // Ensure text input maintains proper alignment
            var textInput = responseText.Q("unity-text-input");
            if (textInput != null)
            {
                textInput.style.alignSelf = Align.Auto;
                textInput.style.backgroundColor = Color.clear;
                textInput.style.borderTopWidth = 0;
                textInput.style.borderBottomWidth = 0;
                textInput.style.borderLeftWidth = 0;
                textInput.style.borderRightWidth = 0;
                textInput.style.color = new Color(0.9f, 0.9f, 0.9f);
            }
            
            _responseScrollView.Add(responseText);
        }
        
        private void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            _copied = true;
            
            if (_copiedLabel != null)
                _copiedLabel.style.display = DisplayStyle.Flex;
                
            EditorApplication.delayCall += () => {
                EditorApplication.delayCall += () => {
                    if (_copiedLabel != null)
                        _copiedLabel.style.display = DisplayStyle.None;
                };
            };
            
            // Debug.Log("Copied to clipboard: " + text);
        } 
        #endregion
    }
}