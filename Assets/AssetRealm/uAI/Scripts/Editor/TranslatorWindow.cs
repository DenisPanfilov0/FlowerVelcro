using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

namespace UAI
{
    public class TranslatorWindow : EditorWindow
    {
        #region Constants
        private const string SYSTEM_INIT_PROMPT = "You are a reliable translator. Your task is to translate text into the specified language and follow the prompt. Answer only with the translated text. Do not add any additional information or explanations.";
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.5f;
        #endregion

        #region UIElements
        
        private VisualElement _rootElement;
        private VisualElement _mainLayout;
        private VisualElement _sidebar;
        private VisualElement _mainContent;
        private VisualElement _responsePanel;
        
        
        private TextField _inputField;
        private TextField _languageField;
        private TextField _extraCharacteristicsField;
        
        
        private Button _translateButton; 
        private Button _copyButton;
        private Button _cancelButton;
        
        
        private ScrollView _responseScrollView;
        private VisualElement _progressBar;
        private Label _progressLabel;
        private Label _copiedLabel;
        #endregion

        #region Private Fields
        private string _apiResponse = "";
        private string _input = "This is just an example text. You can write anything you want. It will be translated into the language you choose. Try it out";
        private string _language = "german";
        private string _extraCharacteristics = "Informal";
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        private bool _copied = false;
        #endregion

        #region Menu Item
        [MenuItem("Tools/uAI Creator/Translator", false, 101)]
        public static void Init()
        {
            TranslatorWindow window = (TranslatorWindow)GetWindow(typeof(TranslatorWindow), false, "Translator");
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
            
            
            var header = new Label("Text Translator");
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 15;
            scrollView.Add(header);
            elementsToHideOnDock.Add(header);
            
            
            var description = new Label("Translate text to any language with AI assistance.");
            description.style.fontSize = 12;
            description.style.marginBottom = 15;
            description.style.whiteSpace = WhiteSpace.Normal;
            scrollView.Add(description);
            elementsToHideOnDock.Add(description);
            
            
            var inputContainer = new Box();
            inputContainer.style.paddingTop = 10;
            inputContainer.style.paddingBottom = 10;
            inputContainer.style.paddingLeft = 10;
            inputContainer.style.paddingRight = 10;
            inputContainer.style.marginBottom = 15;
            scrollView.Add(inputContainer);
            
            var inputLabel = new Label("Text to Translate");
            inputLabel.tooltip = "Enter the text you want to translate.";
            inputLabel.style.fontSize = 14;
            inputLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            inputLabel.style.marginBottom = 5;
            inputLabel.style.whiteSpace = WhiteSpace.Normal;
            inputContainer.Add(inputLabel);
            
            var inputDescription = new Label("Enter the text you want to translate.");
            inputDescription.style.fontSize = 12;
            inputDescription.style.marginBottom = 8;
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
            
            
            var optionsContainer = new VisualElement();
            optionsContainer.style.flexDirection = FlexDirection.Row;
            optionsContainer.style.marginBottom = 15;
            scrollView.Add(optionsContainer);
            
            
            var languageContainer = new Box();
            languageContainer.style.flexGrow = 1;
            languageContainer.style.marginRight = 10;
            languageContainer.style.paddingTop = 10;
            languageContainer.style.paddingBottom = 10;
            languageContainer.style.paddingLeft = 10;
            languageContainer.style.paddingRight = 10;
            optionsContainer.Add(languageContainer);
            
            var languageLabel = new Label("Target Language");
            languageLabel.style.fontSize = 14;
            languageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            languageLabel.style.marginBottom = 5;
            languageLabel.style.whiteSpace = WhiteSpace.Normal;
            languageContainer.Add(languageLabel);
            
            _languageField = new TextField();
            _languageField.value = _language;
            _languageField.style.marginBottom = 10;
            languageContainer.Add(_languageField);
            
            
            var languageExamplesLabel = new Label("Examples: German, French, Spanish, Italian, Japanese, Chinese, Russian, etc.");
            languageExamplesLabel.style.fontSize = 11;
            languageExamplesLabel.style.whiteSpace = WhiteSpace.Normal;
            languageContainer.Add(languageExamplesLabel);
            elementsToHideOnDock.Add(languageExamplesLabel);
            
            
            var characteristicsContainer = new Box();
            characteristicsContainer.style.flexGrow = 1;
            characteristicsContainer.style.marginLeft = 10;
            characteristicsContainer.style.paddingTop = 10;
            characteristicsContainer.style.paddingBottom = 10;
            characteristicsContainer.style.paddingLeft = 10;
            characteristicsContainer.style.paddingRight = 10;
            optionsContainer.Add(characteristicsContainer);
            
            var characteristicsLabel = new Label("Extra Characteristics");
            characteristicsLabel.style.fontSize = 14;
            characteristicsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            characteristicsLabel.style.marginBottom = 5;
            characteristicsLabel.style.whiteSpace = WhiteSpace.Normal;
            characteristicsContainer.Add(characteristicsLabel);
            
            _extraCharacteristicsField = new TextField();
            _extraCharacteristicsField.value = _extraCharacteristics;
            _extraCharacteristicsField.style.marginBottom = 10;
            characteristicsContainer.Add(_extraCharacteristicsField);
            
            
            var characteristicsExamplesLabel = new Label("Examples: Formal, Informal, Scientific, Literary, Business, Friendly, etc.");
            characteristicsExamplesLabel.style.fontSize = 11;
            characteristicsExamplesLabel.style.whiteSpace = WhiteSpace.Normal;
            characteristicsContainer.Add(characteristicsExamplesLabel);
            elementsToHideOnDock.Add(characteristicsExamplesLabel);
            
            
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginTop = 20;
            buttonContainer.style.marginBottom = 10;
            scrollView.Add(buttonContainer);
            
            _translateButton = new Button();
            _translateButton.text = "Translate";
            _translateButton.AddToClassList("primary-button");
            _mainContent.Add(_translateButton);
            
            
            _translateButton.style.height = 40;
            _translateButton.style.width = 250;
            _translateButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _translateButton.style.color = Color.white;
            _translateButton.style.fontSize = 14;
            _translateButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _translateButton.style.borderTopWidth = 0;
            _translateButton.style.borderBottomWidth = 0;
            _translateButton.style.borderLeftWidth = 0;
            _translateButton.style.borderRightWidth = 0;
            
            
            _translateButton.RegisterCallback<MouseEnterEvent>(evt => {
                _translateButton.style.backgroundColor = new Color(0.45f, 0.75f, 1f);
            });
            
            _translateButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _translateButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
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
            
            
            var progressContainer = new VisualElement();
            progressContainer.name = "progress-container";
            progressContainer.style.flexDirection = FlexDirection.Row;
            progressContainer.style.alignItems = Align.Center;
            progressContainer.style.display = DisplayStyle.None;
            progressContainer.style.marginBottom = 10;
            _responsePanel.Add(progressContainer);
            
            _progressLabel = new Label("Translating...");
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
            
            
            var responseTitle = new Label($"Translated Text");
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
                
            
            if (_languageField != null)
                _languageField.RegisterValueChangedCallback(evt => _language = evt.newValue);
                
            
            if (_extraCharacteristicsField != null)
                _extraCharacteristicsField.RegisterValueChangedCallback(evt => _extraCharacteristics = evt.newValue);
                
            
            if (_translateButton != null)
                _translateButton.clicked += () => SendRequestToGPT(BuildPrompt());
        }
        #endregion

        #region Helper Methods
        private string BuildPrompt()
        {
            if (string.IsNullOrEmpty(_extraCharacteristics))
            {
                return $"Translate the text: '{_input}' into {_language}";
            }
            else
            {
                return $"Translate the text: '{_input}' into {_language} with the extra characteristics: {_extraCharacteristics}";
            }
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
                UpdateTranslationTitle();
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
        
        private void UpdateTranslationTitle()
        {
            var responseHeader = _responsePanel?.Children().FirstOrDefault((element) => element is Label label && label.text.Contains("Translated Text"));
            if (responseHeader != null)
            {
                var responseTitle = responseHeader.Q<Label>();
                if (responseTitle != null)
                {
                    string languageCapitalized = !string.IsNullOrEmpty(_language) && _language.Length > 0 ? 
                        char.ToUpper(_language[0]) + _language.Substring(1) : _language;
                    responseTitle.text = $"Translated Text ({languageCapitalized})";
                }
            }
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
        private Box inputContainer;
        private Box instructionsContainer;
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
                _inputField.style.height = 200; 
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