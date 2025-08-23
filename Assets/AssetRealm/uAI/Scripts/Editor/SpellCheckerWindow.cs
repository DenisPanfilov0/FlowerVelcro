using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UAI
{
    public class SpellCheckerWindow : EditorWindow
    {
        #region Constants
        private string SYSTEM_INIT_PROMPT = "You are a reliable spell checker. You can check the spelling of any text and correct it. Your answer only with corrected text. Do not add any other information. You allways answer in " + GPTClient.language;
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.5f;
        #endregion

        #region UIElements
        
        private VisualElement _rootElement;
        private VisualElement _mainLayout;
        private VisualElement _sidebar;
        private VisualElement _mainContent;
        private VisualElement _responsePanel;
        
        
        private TextField _inputField;
        
        
        private Button _checkButton; 
        private Button _copyButton;
        private Button _cancelButton;
        
        
        private ScrollView _responseScrollView;
        private VisualElement _progressBar;
        private Label _progressLabel;
        private Label _copiedLabel;
        #endregion

        #region Private Fields
        private string _apiResponse = "";
        private string _input = "CaN YoU pLeAsE cHeCk ThIs sEnTeNcEe fOr mE?";
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        private bool _copied = false;
        #endregion

        #region Menu Item
        [MenuItem("Tools/uAI Creator/Spell Checker", false, 103)]
        public static void Init()
        {
            SpellCheckerWindow window = (SpellCheckerWindow)GetWindow(typeof(SpellCheckerWindow), false, "Spell Checker");
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
            
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            _mainContent.Add(scrollView);
            
            
            var header = new Label("Spell Checker");
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 15;
            scrollView.Add(header);
            elementsToHideOnDock.Add(header);
            
            
            var description = new Label("Check and correct spelling, grammar, and improve text styling using AI assistance.");
            description.style.fontSize = 12;
            description.style.marginBottom = 15;
            description.style.whiteSpace = WhiteSpace.Normal;
            scrollView.Add(description);
            elementsToHideOnDock.Add(description);
            
            
            var inputContainer = new Box();
            inputContainer.style.paddingTop = 15;
            inputContainer.style.paddingBottom = 15;
            inputContainer.style.paddingLeft = 15;
            inputContainer.style.paddingRight = 15;
            inputContainer.style.marginBottom = 15;
            scrollView.Add(inputContainer);
            
            var inputLabel = new Label("Text to Spell Check");
            inputLabel.tooltip = "Enter the text you want to check for spelling errors. The AI will provide corrections and suggestions.";
            inputLabel.style.fontSize = 14;
            inputLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            inputLabel.style.marginBottom = 10;
            inputLabel.style.whiteSpace = WhiteSpace.Normal;
            inputContainer.Add(inputLabel);
            
            var inputDescription = new Label("Enter the text you want to check for spelling errors. The AI will provide corrections and suggestions.");
            inputDescription.style.fontSize = 12;
            inputDescription.style.marginBottom = 10;
            inputDescription.style.whiteSpace = WhiteSpace.Normal;
            inputContainer.Add(inputDescription);
            elementsToHideOnDock.Add(inputDescription);
            
            _inputField = new TextField();
            _inputField.multiline = true;
            _inputField.value = _input;
            _inputField.style.height = 100;
            _inputField.style.whiteSpace = WhiteSpace.Normal;
            _inputField.style.unityTextAlign = TextAnchor.UpperLeft;
            _inputField.Q("unity-text-input").style.alignSelf = Align.Auto;
            inputContainer.Add(_inputField);
            
            
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginTop = 20;
            buttonContainer.style.marginBottom = 10;
            scrollView.Add(buttonContainer);
            
            _checkButton = new Button();
            _checkButton.text = "Check Spelling";
            _checkButton.AddToClassList("primary-button");
            buttonContainer.Add(_checkButton);
            
            
            _checkButton.style.height = 40;
            _checkButton.style.width = 250;
            _checkButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _checkButton.style.color = Color.white;
            _checkButton.style.fontSize = 14;
            _checkButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _checkButton.style.borderTopWidth = 0;
            _checkButton.style.borderBottomWidth = 0;
            _checkButton.style.borderLeftWidth = 0;
            _checkButton.style.borderRightWidth = 0;
            
            
            _checkButton.RegisterCallback<MouseEnterEvent>(evt => {
                _checkButton.style.backgroundColor = new Color(0.45f, 0.75f, 1f);
            });
            
            _checkButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _checkButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            });
            
            
            var checkTypesContainer = new Box();
            checkTypesContainer.style.marginTop = 15;
            checkTypesContainer.style.paddingTop = 10;
            checkTypesContainer.style.paddingBottom = 10;
            checkTypesContainer.style.paddingLeft = 15;
            checkTypesContainer.style.paddingRight = 15;
            scrollView.Add(checkTypesContainer);
            elementsToHideOnDock.Add(checkTypesContainer);
            
            var checkTypesLabel = new Label("What the Spell Checker Can Fix:");
            checkTypesLabel.style.fontSize = 14;
            checkTypesLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            checkTypesLabel.style.marginBottom = 5;
            checkTypesLabel.style.whiteSpace = WhiteSpace.Normal;
            checkTypesContainer.Add(checkTypesLabel);
            
            var checkTypes = new[] {
                "Spelling errors and typos",
                "Grammar issues and punctuation",
                "Capitalization and formatting",
                "Sentence structure and clarity"
            };
            
            var checkTypesDescription = new VisualElement();
            checkTypesDescription.style.flexDirection = FlexDirection.Row;
            checkTypesDescription.style.flexWrap = Wrap.Wrap;
            checkTypesContainer.Add(checkTypesDescription);
            
            var leftColumn = new VisualElement();
            leftColumn.style.flexGrow = 1;
            leftColumn.style.minWidth = 200;
            checkTypesDescription.Add(leftColumn);
            
            var rightColumn = new VisualElement();
            rightColumn.style.flexGrow = 1;
            rightColumn.style.minWidth = 200;
            checkTypesDescription.Add(rightColumn);
            
            
            for (int i = 0; i < checkTypes.Length; i++)
            {
                var item = new Label("• " + checkTypes[i]);
                item.style.marginTop = 3;
                item.style.marginBottom = 3;
                item.style.whiteSpace = WhiteSpace.Normal;
                
                if (i < checkTypes.Length / 2)
                    leftColumn.Add(item);
                else
                    rightColumn.Add(item);
            }
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
            
            
            var progressContainer = new VisualElement();
            progressContainer.name = "progress-container";
            progressContainer.style.flexDirection = FlexDirection.Row;
            progressContainer.style.alignItems = Align.Center;
            progressContainer.style.display = DisplayStyle.None;
            progressContainer.style.marginBottom = 10;
            _responsePanel.Add(progressContainer);
            
            _progressLabel = new Label("Checking spelling...");
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
            
            
            var responseHeader = new VisualElement();
            responseHeader.style.flexDirection = FlexDirection.Row;
            responseHeader.style.justifyContent = Justify.SpaceBetween;
            responseHeader.style.marginBottom = 10;
            _responsePanel.Add(responseHeader);
            
            
            var responseTitle = new Label("Corrected Text");
            responseTitle.style.fontSize = 16;
            responseTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            responseHeader.Add(responseTitle);
            
            
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
            _copyButton.style.height = 25;
            copySection.Add(_copyButton);
            var closeButton = new Button(() => {
                _responsePanel.style.display = DisplayStyle.None;
                _apiResponse = "";
            });
            closeButton.text = "Close";
            closeButton.style.width = 80;
            closeButton.style.height = 25;
            copySection.Add(closeButton);
            
            
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
            
            if (_inputField != null)
                _inputField.RegisterValueChangedCallback(evt => _input = evt.newValue);
                
            
            if (_checkButton != null)
                _checkButton.clicked += () => SendRequestToGPT(BuildPrompt());
        }
        #endregion

        #region Helper Methods
        private string BuildPrompt()
        {
            return $"Check the spelling of and correct: \"{_input}\".";
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
            GPTClient.Instance.OnResponseReceived += (response, index) =>
            {
                _apiResponse = response;
                _isGenerating = false;
                
                HideProgressBar();
                UpdateResponsePanel();
            };
            
            GPTClient.Instance.OnPartResponseReceived = null;
            GPTClient.Instance.OnPartResponseReceived += (response) =>
            {
                _apiResponse += response;
                
                EditorApplication.delayCall += () => {
                    UpdateResponsePanel();
                };
            };
            
            GPTClient.Instance.SendRequest(prompt);
        }
        
        private void CancelGeneration()
        {
            if (!_isGenerating) return;
            
            GPTClient.StopGeneration();
            _isGenerating = false;
            HideProgressBar();
        }
        
        private bool? lastDockedState = null;
        private List<VisualElement> elementsToHideOnDock = new List<VisualElement>(); 
        private void dockingStateChanged()
        {
            
            if (lastDockedState == true)
            {
                _sidebar.style.display = DisplayStyle.None;
                _mainLayout.style.flexDirection = FlexDirection.Column;
                _mainContent.style.flexGrow = 1; 
                _inputField.style.height = 60; 
            }
            else
            {
                _sidebar.style.display = DisplayStyle.Flex;
                _mainLayout.style.flexDirection = FlexDirection.Row;
                _mainContent.style.flexGrow = 1; 
                _inputField.style.height = 100;
            }

            foreach (var element in elementsToHideOnDock)
            {
                element.style.display = lastDockedState == true ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
        private void UpdateProgress()
        {
            if (lastDockedState == null || docked != lastDockedState)
            {
                lastDockedState = docked;
                dockingStateChanged();
            }
            if (!_isGenerating) return;
            
            _progressValue = Mathf.Repeat(_progressValue + (PROGRESS_BAR_ANIMATION_SPEED * 0.01f), 1f);
            
            
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