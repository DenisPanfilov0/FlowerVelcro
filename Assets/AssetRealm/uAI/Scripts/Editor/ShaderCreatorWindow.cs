using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UAI
{
    public class ShaderCreatorWindow : EditorWindow
    {
        #region Constants
        private string SYSTEM_INIT_PROMPT = "You are a shader programming expert for Unity3D. You can create any shader requested. Always respond with complete, working shader code that compiles correctly in Unity. Structure your code according to Unity's shader format with proper Properties, SubShader blocks, Tags, and Pass definitions. Include meaningful variable names and comments explaining complex logic. Ensure the shader is optimized for the requested platform and follows performance best practices. Make proper use of shader keywords, variants, and fallbacks where appropriate. Support current rendering features like PBR, lighting models, and normal mapping when relevant. If the request is unclear, ask for clarification before generating code. Provide all necessary properties to make the shader easily customizable. You allways answer in " + GPTClient.language;
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.5f;
        private const string DEFAULT_SHADER_DESCRIPTION = "Create a simple color gradient shader that transitions from blue at the bottom to red at the top with a slight emission effect.";
        private const float DEFAULT_PREVIEW_REFRESH_RATE_NORMAL = 0.03f; 
        private const float DEFAULT_PREVIEW_REFRESH_RATE_HIGH = 0.0166f; 
        #endregion

        #region UIElements
        
        private VisualElement _rootElement;
        private VisualElement _mainLayout;
        private VisualElement _sidebar;
        private VisualElement _mainContent;
        private VisualElement _previewPanel;
        private VisualElement _responsePanel;
        private VisualElement _propertiesPanel;
        
        
        private TextField _shaderDescriptionField;
        private ObjectField _modelField;
        
        
        private IMGUIContainer _previewContainer;
        private DropdownField _previewModelDropdown;
        private Slider _rotationSliderY;
        private Slider _rotationSliderX;
        private Slider _zoomSlider;
        private Toggle _autoRotateToggle;
        private Toggle _highFpsToggle;
        
        
        private ScrollView _propertiesScrollView;
        private Dictionary<string, VisualElement> _propertyControls = new Dictionary<string, VisualElement>();
        
        
        private Button _generateButton; 
        private Button _copyButton;
        private Button _loadButton;
        private Button _saveButton;
        private Button _applyButton;
        private Button _cancelButton;
        
        
        private ScrollView _responseScrollView;
        private TextField _shaderCodeField;
        private VisualElement _progressBar;
        private Label _progressLabel;
        private Label _copiedLabel;
        private Label _savedLabel;
        private Label _errorLabel;
        #endregion

        #region Private Fields
        private string _apiResponse = "";
        private string _shaderDescription = DEFAULT_SHADER_DESCRIPTION;
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        private bool _copied = false;
        private bool _saved = false; 
        private float _previewRotationY = 0f;
        private float _previewRotationX = 0f;
        private float _previewZoom = 3f;
        private bool _autoRotate = true;
        private bool _highFps = false;
        private float _lastPreviewUpdateTime = 0f;
        private float _currentPreviewRefreshRate = DEFAULT_PREVIEW_REFRESH_RATE_NORMAL;
        private GameObject _customModel;
        private GameObject _previewModel;
        private int _selectedModelIndex = 0;
        private Material _previewMaterial;
        private Shader _generatedShader;
        private Camera _previewCamera;
        private RenderTexture _previewRenderTexture;
        private Light _previewLight;
        #endregion

        #region Preview Models
        private readonly string[] _defaultModelOptions = new string[] 
        {
            "Sphere", 
            "Cube", 
            "Cylinder", 
            "Plane", 
            "Quad",
            "Card",
            "Custom Model"
        };
        
        private Dictionary<string, Mesh> _defaultMeshes = new Dictionary<string, Mesh>();
        #endregion

        #region Menu Item
        [MenuItem("Tools/uAI Creator/Shader Creator", false, 2)]
        public static void Init()
        {
            ShaderCreatorWindow window = (ShaderCreatorWindow)GetWindow(typeof(ShaderCreatorWindow), false, "Shader Creator");
            window.minSize = new Vector2(1050, 750);
            window.Show();
        }
        #endregion

        #region Unity Lifecycle Methods
        private void CreateGUI()
        {
            _rootElement = rootVisualElement;
            
            InitializePreviewResources();
            BuildUIStructure();
            SetupEventHandlers();
            
            EditorApplication.update += UpdateProgress;
            EditorApplication.update += UpdatePreview;
        }
        
        private void OnDisable()
        {
            
            EditorApplication.update -= UpdateProgress;
            EditorApplication.update -= UpdatePreview;
            
            
            CleanupPreviewResources();
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
            
            CreateHeader();
            
            var mainContainer = new VisualElement();
            mainContainer.style.flexGrow = 1;
            mainContainer.style.flexDirection = FlexDirection.Column;
            _mainContent.Add(mainContainer);
            
            var splitView = new VisualElement();
            splitView.style.flexDirection = FlexDirection.Row;
            splitView.style.flexGrow = 1;
            mainContainer.Add(splitView);
            
            _responsePanel = new VisualElement();
            _responsePanel.style.flexGrow = 1;
            _responsePanel.style.marginRight = 10;
            splitView.Add(_responsePanel);
            
            _propertiesPanel = new VisualElement();
            _propertiesPanel.style.width = 300;
            _propertiesPanel.style.minWidth = 250;
            _propertiesPanel.style.marginRight = 10;
            splitView.Add(_propertiesPanel);
            
            _previewPanel = new VisualElement();
            _previewPanel.style.width = 450;
            _previewPanel.style.minWidth = 400;
            splitView.Add(_previewPanel);
            
            CreateCodePanel();
            
            CreatePropertiesPanel();
            
            CreatePreviewPanel();
            
            CreateInputSection();
        }
        
        private void CreateHeader()
        {
            
            var header = new Label("Shader Creator");
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 5;
            _mainContent.Add(header);
            
            
            var description = new Label("Generate custom shaders with AI. Describe what you want, preview in real-time, and refine iteratively.");
            description.style.fontSize = 12;
            description.style.marginBottom = 15;
            description.style.whiteSpace = WhiteSpace.Normal;
            _mainContent.Add(description);
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
        
        private void CreateCodePanel()
        {
            
            var codeContainer = new Box();
            codeContainer.style.flexGrow = 1;
            _responsePanel.Add(codeContainer);
            
            
            var codeHeader = new VisualElement();
            codeHeader.style.flexDirection = FlexDirection.Row;
            codeHeader.style.justifyContent = Justify.SpaceBetween;
            codeHeader.style.marginBottom = 10;
            codeHeader.style.paddingTop = 10;
            codeHeader.style.paddingLeft = 10;
            codeHeader.style.paddingRight = 10;
            codeContainer.Add(codeHeader);
            
            
            var codeTitle = new Label("Shader Code");
            codeTitle.style.fontSize = 16;
            codeTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            codeHeader.Add(codeTitle);
            
            
            var buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.style.alignItems = Align.Center;
            codeHeader.Add(buttonsContainer);
            
            
            _copiedLabel = new Label("Copied to clipboard!");
            _copiedLabel.style.display = _copied ? DisplayStyle.Flex : DisplayStyle.None;
            _copiedLabel.style.marginRight = 10;
            _copiedLabel.style.color = new Color(0.35f, 0.65f, 0.9f);
            buttonsContainer.Add(_copiedLabel);
            
            _savedLabel = new Label("Shader saved!");
            _savedLabel.style.display = _saved ? DisplayStyle.Flex : DisplayStyle.None;
            _savedLabel.style.marginRight = 10;
            _savedLabel.style.color = new Color(0.35f, 0.65f, 0.9f);
            buttonsContainer.Add(_savedLabel);
            
            
            _copyButton = new Button(() => CopyToClipboard(_apiResponse));
            _copyButton.text = "Copy";
            _copyButton.style.width = 60;
            _copyButton.style.marginRight = 5;
            buttonsContainer.Add(_copyButton);
            
            
            _loadButton = new Button(LoadShaderFromFile);
            _loadButton.text = "Load";
            _loadButton.style.width = 60;
            _loadButton.style.marginRight = 5;
            buttonsContainer.Add(_loadButton);
            
            
            _saveButton = new Button(SaveShaderToFile);
            _saveButton.text = "Save";
            _saveButton.style.width = 60;
            buttonsContainer.Add(_saveButton);
            
            
            _responseScrollView = new ScrollView();
            _responseScrollView.style.flexGrow = 1;
            _responseScrollView.style.height = 400; 
            codeContainer.Add(_responseScrollView);
            
            
            _shaderCodeField = new TextField();
            _shaderCodeField.multiline = true;
            _shaderCodeField.style.flexGrow = 1;
            _shaderCodeField.style.minHeight = 400;
            _shaderCodeField.style.whiteSpace = WhiteSpace.Normal;
            _shaderCodeField.style.unityTextAlign = TextAnchor.UpperLeft;
            _shaderCodeField.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            _shaderCodeField.style.color = new Color(0.9f, 0.9f, 0.9f);
            
            
            if (!string.IsNullOrEmpty(_apiResponse))
            {
                _shaderCodeField.value = _apiResponse;
            }
            else
            {
                _shaderCodeField.value = "Your shader code will appear here\n You can edit it directly or load an existing shader";
            }
            
            _responseScrollView.Add(_shaderCodeField);
            
            
            _applyButton = new Button(ApplyCodeChanges);
            _applyButton.text = "Apply Changes";
            _applyButton.style.height = 30;
            _applyButton.style.marginTop = 10;
            _applyButton.style.marginBottom = 10;
            _applyButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _applyButton.style.color = Color.white;
            _applyButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _applyButton.style.alignSelf = Align.Center;
            codeContainer.Add(_applyButton);
            
            
            _applyButton.RegisterCallback<MouseEnterEvent>(evt => {
                _applyButton.style.backgroundColor = new Color(0.45f, 0.75f, 1f);
            });
            
            _applyButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _applyButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            });
            
            
            var progressContainer = new VisualElement();
            progressContainer.name = "progress-container";
            progressContainer.style.flexDirection = FlexDirection.Row;
            progressContainer.style.alignItems = Align.Center;
            progressContainer.style.display = DisplayStyle.None;
            progressContainer.style.marginTop = 10;
            progressContainer.style.paddingLeft = 10;
            progressContainer.style.paddingRight = 10;
            progressContainer.style.paddingBottom = 10;
            codeContainer.Add(progressContainer);
            
            _progressLabel = new Label("Generating shader...");
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
        }
        
        private void CreatePropertiesPanel()
        {
            
            var propertiesContainer = new Box();
            propertiesContainer.style.flexGrow = 1;
            _propertiesPanel.Add(propertiesContainer);
            
            
            var propertiesTitle = new Label("Shader Properties");
            propertiesTitle.style.fontSize = 16;
            propertiesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            propertiesTitle.style.marginBottom = 10;
            propertiesTitle.style.paddingTop = 10;
            propertiesTitle.style.paddingLeft = 10;
            propertiesTitle.style.paddingRight = 10;
            propertiesContainer.Add(propertiesTitle);
            
            
            _propertiesScrollView = new ScrollView();
            _propertiesScrollView.style.flexGrow = 1;
            _propertiesScrollView.style.paddingLeft = 10;
            _propertiesScrollView.style.paddingRight = 10;
            _propertiesScrollView.style.paddingBottom = 10;
            propertiesContainer.Add(_propertiesScrollView);
            
            
            var placeholderLabel = new Label("No shader properties available yet");
            placeholderLabel.name = "properties-placeholder";
            placeholderLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            placeholderLabel.style.paddingTop = 5;
            placeholderLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _propertiesScrollView.Add(placeholderLabel);
            
            
            _errorLabel = new Label();
            _errorLabel.style.color = new Color(0.9f, 0.3f, 0.3f);
            _errorLabel.style.whiteSpace = WhiteSpace.Normal;
            _errorLabel.style.display = DisplayStyle.None;
            _errorLabel.style.marginTop = 5;
            _errorLabel.style.paddingLeft = 10;
            _errorLabel.style.paddingRight = 10;
            _errorLabel.style.paddingBottom = 10;
            propertiesContainer.Add(_errorLabel);
        }
        
        private void CreatePreviewPanel()
        {
            
            var previewContainer = new Box();
            previewContainer.style.flexGrow = 1;
            _previewPanel.Add(previewContainer);
            
            
            var previewTitle = new Label("Shader Preview");
            previewTitle.style.fontSize = 16;
            previewTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewTitle.style.marginBottom = 10;
            previewTitle.style.paddingTop = 10;
            previewTitle.style.paddingLeft = 10;
            previewContainer.Add(previewTitle);
            
            
            var modelSelectionContainer = new VisualElement();
            modelSelectionContainer.style.flexDirection = FlexDirection.Row;
            modelSelectionContainer.style.marginBottom = 10;
            modelSelectionContainer.style.paddingLeft = 10;
            modelSelectionContainer.style.paddingRight = 10;
            previewContainer.Add(modelSelectionContainer);
            
            var modelLabel = new Label("Preview Model:");
            modelLabel.style.marginRight = 10;
            modelLabel.style.minWidth = 100;
            modelLabel.style.alignSelf = Align.Center;
            modelSelectionContainer.Add(modelLabel);
            
            _previewModelDropdown = new DropdownField();
            _previewModelDropdown.choices = new List<string>(_defaultModelOptions);
            _previewModelDropdown.index = _selectedModelIndex;
            _previewModelDropdown.style.flexGrow = 1;
            modelSelectionContainer.Add(_previewModelDropdown);
            
            
            var customModelContainer = new VisualElement();
            customModelContainer.style.marginBottom = 10;
            customModelContainer.style.paddingLeft = 10;
            customModelContainer.style.paddingRight = 10;
            previewContainer.Add(customModelContainer);
            
            _modelField = new ObjectField("Custom Model:");
            _modelField.objectType = typeof(GameObject);
            _modelField.value = _customModel;
            _modelField.style.display = _selectedModelIndex == _defaultModelOptions.Length - 1 ? DisplayStyle.Flex : DisplayStyle.None;
            customModelContainer.Add(_modelField);
            
            
            var previewBox = new Box();
            previewBox.style.height = 250;
            previewBox.style.marginBottom = 10;
            previewBox.style.marginLeft = 10;
            previewBox.style.marginRight = 10;
            previewContainer.Add(previewBox);
            
            _previewContainer = new IMGUIContainer(OnPreviewGUI);
            _previewContainer.style.flexGrow = 1;
            previewBox.Add(_previewContainer);
            
            
            var controlsContainer = new VisualElement();
            controlsContainer.style.paddingLeft = 10;
            controlsContainer.style.paddingRight = 10;
            previewContainer.Add(controlsContainer);
            
            
            var rotationYContainer = new VisualElement();
            rotationYContainer.style.flexDirection = FlexDirection.Row;
            rotationYContainer.style.marginBottom = 10;
            controlsContainer.Add(rotationYContainer);
            
            var rotationYLabel = new Label("Y Rotation:");
            rotationYLabel.style.minWidth = 80;
            rotationYLabel.style.alignSelf = Align.Center;
            rotationYContainer.Add(rotationYLabel);
            
            _rotationSliderY = new Slider(0, 360);
            _rotationSliderY.value = 0;
            _rotationSliderY.style.flexGrow = 1;
            rotationYContainer.Add(_rotationSliderY);
            
            
            var rotationXContainer = new VisualElement();
            rotationXContainer.style.flexDirection = FlexDirection.Row;
            rotationXContainer.style.marginBottom = 10;
            controlsContainer.Add(rotationXContainer);
            
            var rotationXLabel = new Label("X Rotation:");
            rotationXLabel.style.minWidth = 80;
            rotationXLabel.style.alignSelf = Align.Center;
            rotationXContainer.Add(rotationXLabel);
            
            _rotationSliderX = new Slider(0, 360);
            _rotationSliderX.value = 0;
            _rotationSliderX.style.flexGrow = 1;
            rotationXContainer.Add(_rotationSliderX);
            
            
            var zoomContainer = new VisualElement();
            zoomContainer.style.flexDirection = FlexDirection.Row;
            zoomContainer.style.marginBottom = 10;
            controlsContainer.Add(zoomContainer);
            
            var zoomLabel = new Label("Zoom:");
            zoomLabel.style.minWidth = 80;
            zoomLabel.style.alignSelf = Align.Center;
            zoomContainer.Add(zoomLabel);
            
            _zoomSlider = new Slider(1, 8);
            _zoomSlider.value = _previewZoom;
            _zoomSlider.style.flexGrow = 1;
            zoomContainer.Add(_zoomSlider);
            
            
            var displayOptionsContainer = new VisualElement();
            displayOptionsContainer.style.flexDirection = FlexDirection.Row;
            displayOptionsContainer.style.marginBottom = 10;
            displayOptionsContainer.style.justifyContent = Justify.SpaceBetween;
            controlsContainer.Add(displayOptionsContainer);
            
            
            _autoRotateToggle = new Toggle("Auto-Rotate");
            _autoRotateToggle.value = _autoRotate;
            displayOptionsContainer.Add(_autoRotateToggle);
            
            
            _highFpsToggle = new Toggle("High FPS Preview");
            _highFpsToggle.value = _highFps;
            displayOptionsContainer.Add(_highFpsToggle);
        }
        
        private void CreateInputSection()
        {
            
            var inputContainer = new Box();
            inputContainer.style.marginTop = 15;
            _mainContent.Add(inputContainer);
            
            
            var inputHeader = new VisualElement();
            inputHeader.style.flexDirection = FlexDirection.Row;
            inputHeader.style.justifyContent = Justify.SpaceBetween;
            inputHeader.style.marginBottom = 10;
            inputHeader.style.paddingTop = 10;
            inputHeader.style.paddingLeft = 10;
            inputHeader.style.paddingRight = 10;
            inputContainer.Add(inputHeader);
            
            
            var inputTitle = new Label("Shader Request");
            inputTitle.style.fontSize = 16;
            inputTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            inputHeader.Add(inputTitle);
            
            
            var controlButtons = new VisualElement();
            controlButtons.style.flexDirection = FlexDirection.Row;
            inputHeader.Add(controlButtons);
            
            
            var clearButton = new Button(ClearShader);
            clearButton.text = "Clear Shader";
            clearButton.style.marginRight = 10;
            controlButtons.Add(clearButton);
            
            
            _shaderDescriptionField = new TextField();
            _shaderDescriptionField.multiline = true;
            _shaderDescriptionField.value = _shaderDescription;
            _shaderDescriptionField.style.minHeight = 100;
            _shaderDescriptionField.style.whiteSpace = WhiteSpace.Normal;
            _shaderDescriptionField.style.unityTextAlign = TextAnchor.UpperLeft;
            _shaderDescriptionField.Q("unity-text-input").style.alignSelf = Align.Auto;
            _shaderDescriptionField.style.marginLeft = 10;
            _shaderDescriptionField.style.marginRight = 10;
            inputContainer.Add(_shaderDescriptionField);
            
            
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;
            buttonRow.style.marginTop = 10;
            buttonRow.style.marginBottom = 10;
            inputContainer.Add(buttonRow);
            
            _generateButton = new Button();
            _generateButton.text = "Generate/Update Shader";
            _generateButton.style.height = 40;
            _generateButton.style.width = 250;
            _generateButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _generateButton.style.color = Color.white;
            _generateButton.style.fontSize = 14;
            _generateButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            buttonRow.Add(_generateButton);
            
            
            _generateButton.RegisterCallback<MouseEnterEvent>(evt => {
                _generateButton.style.backgroundColor = new Color(0.45f, 0.75f, 1f);
            });
            
            _generateButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _generateButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            });
            
            
            var examplesContainer = new VisualElement();
            examplesContainer.style.paddingLeft = 10;
            examplesContainer.style.paddingRight = 10;
            examplesContainer.style.paddingBottom = 10;
            inputContainer.Add(examplesContainer);
            
            var examplesTitle = new Label("Example requests:");
            examplesTitle.style.fontSize = 12;
            examplesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            examplesTitle.style.marginBottom = 5;
            examplesContainer.Add(examplesTitle);
            
            var examples = new[] {
                "Create a water shader with realistic waves and reflection",
                "Make a holographic shader with rainbow colors and scanlines",
                "Create a dissolve effect that reveals a second texture underneath",
                "Make the colors more vibrant and add some subtle animation",
                "Add a normal map to make the surface look more detailed"
            };
            
            var examplesGrid = new VisualElement();
            examplesGrid.style.flexDirection = FlexDirection.Row;
            examplesGrid.style.flexWrap = Wrap.Wrap;
            examplesContainer.Add(examplesGrid);
            
            foreach (var example in examples)
            {
                var exampleLabel = new Label("• " + example);
                exampleLabel.style.fontSize = 11;
                exampleLabel.style.marginRight = 15;
                exampleLabel.style.marginBottom = 3;
                exampleLabel.style.whiteSpace = WhiteSpace.Normal;
                exampleLabel.style.width = 300;
                examplesGrid.Add(exampleLabel);
            }
        }

        #endregion

        #region Event Handlers
        private void SetupEventHandlers()
        {
            
            if (_shaderDescriptionField != null)
                _shaderDescriptionField.RegisterValueChangedCallback(evt => _shaderDescription = evt.newValue);
                
            
            if (_shaderCodeField != null)
                _shaderCodeField.RegisterValueChangedCallback(evt => {
                    _apiResponse = evt.newValue;
                });
                
            
            if (_modelField != null)
                _modelField.RegisterValueChangedCallback(evt => {
                    _customModel = evt.newValue as GameObject;
                    UpdatePreviewModel();
                });
                
            
            if (_previewModelDropdown != null)
                _previewModelDropdown.RegisterValueChangedCallback(evt => {
                    _selectedModelIndex = _previewModelDropdown.index;
                    _modelField.style.display = _selectedModelIndex == _defaultModelOptions.Length - 1 ? DisplayStyle.Flex : DisplayStyle.None;
                    UpdatePreviewModel();
                });
                
            
            if (_rotationSliderY != null)
                _rotationSliderY.RegisterValueChangedCallback(evt => {
                    _previewRotationY = evt.newValue;
                    UpdatePreviewModelRotation();
                });
                
            
            if (_rotationSliderX != null)
                _rotationSliderX.RegisterValueChangedCallback(evt => {
                    _previewRotationX = evt.newValue;
                    UpdatePreviewModelRotation();
                });
                
            
            if (_zoomSlider != null)
                _zoomSlider.RegisterValueChangedCallback(evt => {
                    _previewZoom = evt.newValue;
                    UpdatePreviewCameraZoom();
                });
                
            
            if (_autoRotateToggle != null)
                _autoRotateToggle.RegisterValueChangedCallback(evt => {
                    _autoRotate = evt.newValue;
                });
                
            
            if (_highFpsToggle != null)
                _highFpsToggle.RegisterValueChangedCallback(evt => {
                    _highFps = evt.newValue;
                    _currentPreviewRefreshRate = _highFps ? 
                        DEFAULT_PREVIEW_REFRESH_RATE_HIGH : 
                        DEFAULT_PREVIEW_REFRESH_RATE_NORMAL;
                });
                
            
            if (_generateButton != null)
                _generateButton.clicked += () => ProcessShaderRequest(_shaderDescription);
                
            
            if (_applyButton != null)
                _applyButton.clicked += ApplyCodeChanges;
        }
        
        private void UpdatePreviewModelRotation()
        {
            if (_previewModel != null)
            {
                _previewModel.transform.rotation = Quaternion.Euler(_previewRotationX, _previewRotationY, 0);
            }
        }
        
        private void UpdatePreviewCameraZoom()
        {
            if (_previewCamera != null)
            {
                _previewCamera.transform.position = new Vector3(0, 0, -_previewZoom);
            }
        }
        
        private void ApplyCodeChanges()
        {
            if (string.IsNullOrEmpty(_shaderCodeField.value))
                return;
                
            _apiResponse = _shaderCodeField.value;
            ApplyGeneratedShader();
        }
        #endregion

        #region Preview Handling
        private void InitializePreviewResources()
        {
            
            _defaultMeshes.Clear();
            _defaultMeshes.Add("Sphere", Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx"));
            _defaultMeshes.Add("Cube", Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
            _defaultMeshes.Add("Cylinder", Resources.GetBuiltinResource<Mesh>("Cylinder.fbx"));
            _defaultMeshes.Add("Plane", Resources.GetBuiltinResource<Mesh>("New-Plane.fbx"));
            _defaultMeshes.Add("Quad", Resources.GetBuiltinResource<Mesh>("Quad.fbx"));
            _defaultMeshes.Add("Card", CreateCardMesh());
            
            
            if (_previewRenderTexture == null || _previewRenderTexture.width != 512)
            {
                if (_previewRenderTexture != null)
                    Object.DestroyImmediate(_previewRenderTexture);
                    
                _previewRenderTexture = new RenderTexture(512, 512, 24);
                _previewRenderTexture.antiAliasing = 4;
            }
            
            
            CreatePreviewScene();
            
            
            if (_previewMaterial == null)
            {
                _previewMaterial = new Material(Shader.Find("Standard"));
                _previewMaterial.color = Color.white;
            }
            
            
            UpdatePreviewModel();
            
            
            _currentPreviewRefreshRate = _highFps ? 
                DEFAULT_PREVIEW_REFRESH_RATE_HIGH : 
                DEFAULT_PREVIEW_REFRESH_RATE_NORMAL;
        }
        
        private Mesh CreateCardMesh()
        {
            
            Mesh mesh = new Mesh();
            mesh.name = "Card";
            
            
            float width = 1.0f;
            float height = 1.5f;
            
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-width/2, -height/2, 0),  
                new Vector3(width/2, -height/2, 0),   
                new Vector3(-width/2, height/2, 0),   
                new Vector3(width/2, height/2, 0)     
            };
            
            
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),  
                new Vector2(1, 0),  
                new Vector2(0, 1),  
                new Vector2(1, 1)   
            };
            
            
            int[] triangles = new int[6]
            {
                0, 2, 1,  
                2, 3, 1   
            };
            
            
            Vector3[] normals = new Vector3[4]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };
            
            
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.normals = normals;
            
            return mesh;
        }
        
        private void CreatePreviewScene()
        {
            
            if (_previewCamera == null)
            {
                var cameraGO = new GameObject("Preview Camera");
                cameraGO.hideFlags = HideFlags.HideAndDontSave;
                _previewCamera = cameraGO.AddComponent<Camera>();
                _previewCamera.clearFlags = CameraClearFlags.SolidColor;
                _previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1);
                _previewCamera.nearClipPlane = 0.01f;
                _previewCamera.farClipPlane = 100f;
                _previewCamera.transform.position = new Vector3(0, 0, -_previewZoom);
                _previewCamera.transform.LookAt(Vector3.zero);
                _previewCamera.targetTexture = _previewRenderTexture;
            }
            
            
            if (_previewLight == null)
            {
                var lightGO = new GameObject("Preview Light");
                lightGO.hideFlags = HideFlags.HideAndDontSave;
                _previewLight = lightGO.AddComponent<Light>();
                _previewLight.type = LightType.Directional;
                _previewLight.intensity = 1.2f;
                _previewLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }
        
        private void UpdatePreviewModel()
        {
            
            if (_previewModel != null)
            {
                Object.DestroyImmediate(_previewModel);
            }
            
            
            if (_selectedModelIndex < _defaultModelOptions.Length - 1)
            {
                
                string modelName = _defaultModelOptions[_selectedModelIndex];
                _previewModel = new GameObject("Preview Model: " + modelName);
                _previewModel.hideFlags = HideFlags.HideAndDontSave;
                
                var meshFilter = _previewModel.AddComponent<MeshFilter>();
                if (_defaultMeshes.ContainsKey(modelName))
                {
                    meshFilter.mesh = _defaultMeshes[modelName];
                }
                
                var meshRenderer = _previewModel.AddComponent<MeshRenderer>();
                meshRenderer.material = _previewMaterial;
            }
            else if (_customModel != null)
            {
                
                _previewModel = Object.Instantiate(_customModel);
                _previewModel.name = "Preview Model: Custom";
                _previewModel.hideFlags = HideFlags.HideAndDontSave;
                
                
                Renderer[] renderers = _previewModel.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    Material[] materials = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = _previewMaterial;
                    }
                    renderer.sharedMaterials = materials;
                }
            }

            if (_previewModel != null)
            {
                
                UpdatePreviewModelRotation();

                
                _previewModel.transform.position   = Vector3.zero;
                _previewModel.transform.rotation   = Quaternion.identity;
                _previewModel.transform.localScale = Vector3.one;
 
                Bounds bounds = CalculateModelBounds(_previewModel); 
                float scale = 1.8f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z); 
                _previewModel.transform.localScale = Vector3.one * scale; 
                bounds = CalculateModelBounds(_previewModel); 
                _previewModel.transform.position = -bounds.center;
            }
        }
        
        private Bounds CalculateModelBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);
                
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }
        
        private void OnPreviewGUI()
        {
            if (_previewRenderTexture != null)
            {
                _previewCamera.Render();
                GUI.DrawTexture(GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)), 
                                _previewRenderTexture, ScaleMode.ScaleToFit);
            }
        }
        
        private void UpdatePreview()
        {
            
            float currentTime = (float)EditorApplication.timeSinceStartup;
            bool shouldUpdate = (currentTime - _lastPreviewUpdateTime) >= _currentPreviewRefreshRate;
            
            if (shouldUpdate)
            {
                
                if (_previewModel != null && _autoRotate)
                {
                    _previewRotationY = (_previewRotationY + 0.3f) % 360f;
                    UpdatePreviewModelRotation();
                    _rotationSliderY.SetValueWithoutNotify(_previewRotationY);
                }
                
                
                _previewContainer.MarkDirtyRepaint();
                
                
                _lastPreviewUpdateTime = currentTime;
            }
        }
        
        private void CleanupPreviewResources()
        {
            if (_previewModel != null)
                Object.DestroyImmediate(_previewModel);
                
            if (_previewCamera != null)
                Object.DestroyImmediate(_previewCamera.gameObject);
                
            if (_previewLight != null)
                Object.DestroyImmediate(_previewLight.gameObject);
                
            if (_previewRenderTexture != null)
                Object.DestroyImmediate(_previewRenderTexture);
                
            if (_previewMaterial != null)
                Object.DestroyImmediate(_previewMaterial);
        }
        #endregion

        #region Shader Generation
        private void ProcessShaderRequest(string request)
        {
            if (_isGenerating) return;
            
            _isGenerating = true;
            _progressValue = 0f;
            _copied = false;
            _saved = false; 
            _errorLabel.style.display = DisplayStyle.None;
            
            ShowProgressBar();
            
            _progressLabel.text = string.IsNullOrEmpty(_apiResponse) || _apiResponse.Contains(" Your shader code will appear here") ?"Generating shader..." : "Updating shader...";
            
            
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            
            GPTClient.Instance.SystemInitPrompt = SYSTEM_INIT_PROMPT + 
                (currentPipeline != null ? $"\nCurrent Render Pipeline: {currentPipeline.GetType().Name}" : "");
            
            GPTClient.Instance.OnResponseReceived = null;
            GPTClient.Instance.OnResponseReceived += OnShaderResponseReceived;
            
            
            if (string.IsNullOrEmpty(_apiResponse) || _apiResponse.Contains("Your shader code will appear here"))
            {
                _apiResponse = "";
            }
            
            string fullResponse = ""; 
            
            GPTClient.Instance.OnPartResponseReceived = null;
            GPTClient.Instance.OnPartResponseReceived += (response) =>
            {
                
                fullResponse += response;
                
                
                _apiResponse = fullResponse;
                
                EditorApplication.delayCall += () => {
                    
                    if (_shaderCodeField != null)
                        _shaderCodeField.SetValueWithoutNotify(_apiResponse);
                };
            };
            
            string prompt;
            
            if (string.IsNullOrEmpty(_apiResponse))
            {
                
                prompt = $"Create a Unity shader that does the following: {request}. Return only the complete shader code without any explanations or comments outside the code.";
            }
            else
            {
                
                prompt = $"Here is a shader I've created:\n\n```\n{_apiResponse}\n```\n\nPlease modify it according to these requirements: {request}. Return only the complete updated shader code without any explanations or comments outside the code.";
            }
            
            GPTClient.Instance.SendRequest(prompt);
        }
        
        private void OnShaderResponseReceived(string response, int index)
        {
            _apiResponse = response;
            _isGenerating = false;
            
            HideProgressBar();
            
            
            if (_shaderCodeField != null)
                _shaderCodeField.SetValueWithoutNotify(_apiResponse);
            
            
            ApplyGeneratedShader();
        }
        
        private void ClearShader()
        {
            _apiResponse = "";
            _shaderDescription = DEFAULT_SHADER_DESCRIPTION;
            _shaderDescriptionField.value = DEFAULT_SHADER_DESCRIPTION; 
            _errorLabel.style.display = DisplayStyle.None;
            
            
            if (_previewMaterial != null)
            {
                _previewMaterial.shader = Shader.Find("Standard");
                _previewMaterial.color = Color.white;
            }
            
            
            ClearShaderPropertyControls();
            
            
            if (_shaderCodeField != null)
                _shaderCodeField.SetValueWithoutNotify(" Your shader code will appear here\n You can edit it directly or load an existing shader");
            
            UpdatePreviewModel();
        }
        
        private void LoadShaderFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Load Shader", "Assets", "shader");
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    
                    string shaderCode = File.ReadAllText(path);
                    
                    
                    _apiResponse = shaderCode;
                    
                    
                    if (_shaderCodeField != null)
                        _shaderCodeField.SetValueWithoutNotify(_apiResponse);
                    
                    
                    ApplyGeneratedShader();
                    
                    
                    string shaderName = Path.GetFileNameWithoutExtension(path);
                    _shaderDescription = $"Loaded shader: {shaderName}";
                    _shaderDescriptionField.SetValueWithoutNotify(_shaderDescription);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading shader: {e.Message}");
                    EditorUtility.DisplayDialog("Load Error", $"Failed to load shader: {e.Message}", "OK");
                }
            }
        }
        
        private void ApplyGeneratedShader()
        {
            try
            {
                string shaderCode = _apiResponse;
                
                
                if (shaderCode.Contains("```"))
                {
                    int startIndex = shaderCode.IndexOf("```");
                    int endIndex = shaderCode.LastIndexOf("```");
                    
                    if (startIndex >= 0 && endIndex > startIndex)
                    {
                        
                        startIndex = shaderCode.IndexOf('\n', startIndex) + 1;
                        if (startIndex <= 0) startIndex = shaderCode.IndexOf("```") + 3;
                        
                        
                        endIndex = shaderCode.LastIndexOf("```");
                        
                        if (startIndex < endIndex)
                        {
                            shaderCode = shaderCode.Substring(startIndex, endIndex - startIndex).Trim();
                        }
                    }
                }
                
                
                shaderCode = FixShaderSyntax(shaderCode);
                
                
                string tempShaderPath = "Assets/Temp/GeneratedShader.shader";
                string directoryPath = Path.GetDirectoryName(tempShaderPath);
                
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                
                File.WriteAllText(tempShaderPath, shaderCode);
                AssetDatabase.ImportAsset(tempShaderPath);
                
                
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(tempShaderPath);
                if (shader != null)
                {
                    _generatedShader = shader;
                    
                    
                    _apiResponse = shaderCode;
                    if (_shaderCodeField != null)
                        _shaderCodeField.SetValueWithoutNotify(_apiResponse);
                    
                    
                    if (_previewMaterial == null)
                    {
                        _previewMaterial = new Material(shader);
                    }
                    else
                    {
                        _previewMaterial.shader = shader;
                    }
                    
                    
                    UpdatePreviewModel();
                    
                    
                    CreateShaderPropertyControls(shader);
                     
                    _errorLabel.style.display = DisplayStyle.None;
                }
                else
                {
                    ShowShaderError("Failed to load the generated shader. It may have compilation errors.");
                }
            }
            catch (System.Exception e)
            {
                ShowShaderError($"Error applying shader: {e.Message}");
            }
        }
        
        private void CreateShaderPropertyControls(Shader shader)
        {
            
            _propertyControls.Clear();
            _propertiesScrollView.Clear();
            
            
            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            
            if (propertyCount == 0)
            {
                
                var placeholderLabel = new Label("No shader properties available");
                placeholderLabel.name = "properties-placeholder";
                placeholderLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                placeholderLabel.style.paddingTop = 5;
                placeholderLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _propertiesScrollView.Add(placeholderLabel);
                return;
            }
            
            
            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                string propertyDescription = ShaderUtil.GetPropertyDescription(shader, i);
                ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(shader, i);
                
                
                var propertyContainer = new VisualElement();
                propertyContainer.style.marginBottom = 8;
                _propertiesScrollView.Add(propertyContainer);
                
                
                var propertyLabel = new Label(string.IsNullOrEmpty(propertyDescription) ? propertyName : propertyDescription);
                propertyLabel.style.marginBottom = 2;
                propertyContainer.Add(propertyLabel);
                
                
                VisualElement control = null;
                
                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        
                        var colorField = new ColorField();
                        colorField.value = _previewMaterial.GetColor(propertyName);
                        colorField.RegisterValueChangedCallback(evt => {
                            _previewMaterial.SetColor(propertyName, evt.newValue);
                        });
                        control = colorField;
                        break;
                        
                    case ShaderUtil.ShaderPropertyType.Vector:
                        
                        var vectorField = new Vector4Field();
                        vectorField.value = _previewMaterial.GetVector(propertyName);
                        vectorField.RegisterValueChangedCallback(evt => {
                            _previewMaterial.SetVector(propertyName, evt.newValue);
                        });
                        control = vectorField;
                        break;
                        
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        
                        if (propertyType == ShaderUtil.ShaderPropertyType.Range)
                        {
                            
                            float rangeMin = ShaderUtil.GetRangeLimits(shader, i, 1);
                            float rangeMax = ShaderUtil.GetRangeLimits(shader, i, 2);
                            
                            
                            var slider = new Slider(propertyName, rangeMin, rangeMax);
                            slider.value = _previewMaterial.GetFloat(propertyName);
                            slider.RegisterValueChangedCallback(evt => {
                                _previewMaterial.SetFloat(propertyName, evt.newValue);
                            });
                            control = slider;
                        }
                        else
                        {
                            
                            var floatField = new FloatField();
                            floatField.value = _previewMaterial.GetFloat(propertyName);
                            floatField.RegisterValueChangedCallback(evt => {
                                _previewMaterial.SetFloat(propertyName, evt.newValue);
                            });
                            control = floatField;
                        }
                        break;
                        
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        
                        var textureField = new ObjectField();
                        textureField.objectType = typeof(Texture);
                        textureField.value = _previewMaterial.GetTexture(propertyName);
                        textureField.RegisterValueChangedCallback(evt => {
                            _previewMaterial.SetTexture(propertyName, evt.newValue as Texture);
                        });
                        control = textureField;
                        break;
                        
                    default:
                        
                        var unsupportedLabel = new Label($"Unsupported property type: {propertyType}");
                        unsupportedLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                        control = unsupportedLabel;
                        break;
                }
                
                if (control != null)
                {
                    
                    control.style.marginLeft = 10;
                    propertyContainer.Add(control);
                    
                    
                    _propertyControls[propertyName] = control;
                }
            }
        }
        
        private void ClearShaderPropertyControls()
        {
            _propertyControls.Clear();
            _propertiesScrollView.Clear();
            
            
            var placeholderLabel = new Label("No shader properties available yet");
            placeholderLabel.name = "properties-placeholder";
            placeholderLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            placeholderLabel.style.paddingTop = 5;
            placeholderLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _propertiesScrollView.Add(placeholderLabel);
        }
        
        private string FixShaderSyntax(string shaderCode)
        {
            
            
            
            int openBraces = 0;
            int closeBraces = 0;
            
            foreach (char c in shaderCode)
            {
                if (c == '{') openBraces++;
                if (c == '}') closeBraces++;
            }
            
            
            if (openBraces > closeBraces)
            {
                for (int i = 0; i < openBraces - closeBraces; i++)
                {
                    shaderCode += "\n}";
                }
            }
            
            
            if (!shaderCode.Contains("FallBack ") && !shaderCode.Contains("Fallback "))
            {
                
                int lastBraceIndex = shaderCode.LastIndexOf('}');
                if (lastBraceIndex > 0)
                {
                    shaderCode = shaderCode.Insert(lastBraceIndex, "\n    FallBack \"Diffuse\"\n");
                }
            }
            
            
            if (!shaderCode.Contains("Shader \""))
            {
                
                if (shaderCode.TrimStart().StartsWith("{"))
                {
                    shaderCode = "Shader \"Custom/GeneratedShader\" " + shaderCode;
                }
            }
            
            return shaderCode;
        }
        
        private void ShowShaderError(string errorMessage)
        { 
            _errorLabel.text = errorMessage;
            _errorLabel.style.display = DisplayStyle.Flex;
            
            
            if (_previewMaterial != null)
            {
                _previewMaterial.shader = Shader.Find("Standard");
            }
        }
        
        private void SaveShaderToFile()
        {
            if (string.IsNullOrEmpty(_apiResponse) || 
                _apiResponse.Contains("Your shader code will appear here")) return;
            
            string shaderCode = _apiResponse;
            
            
            if (shaderCode.Contains("```"))
            {
                int startIndex = shaderCode.IndexOf("```");
                int endIndex = shaderCode.LastIndexOf("```");
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    
                    startIndex = shaderCode.IndexOf('\n', startIndex) + 1;
                    if (startIndex <= 0) startIndex = shaderCode.IndexOf("```") + 3;
                    
                    
                    endIndex = shaderCode.LastIndexOf("```");
                    
                    if (startIndex < endIndex)
                    {
                        shaderCode = shaderCode.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                }
            }
            
            
            shaderCode = FixShaderSyntax(shaderCode);
            
            
            string shaderName = "GeneratedShader";
            if (shaderCode.Contains("Shader \""))
            {
                int nameStartIndex = shaderCode.IndexOf("Shader \"") + 8;
                int nameEndIndex = shaderCode.IndexOf("\"", nameStartIndex);
                if (nameStartIndex >= 8 && nameEndIndex > nameStartIndex)
                {
                    shaderName = shaderCode.Substring(nameStartIndex, nameEndIndex - nameStartIndex);
                    shaderName = shaderName.Replace("/", "_");
                }
            }
            
            string path = EditorUtility.SaveFilePanel("Save Shader", "Assets", shaderName, "shader");
            
            if (!string.IsNullOrEmpty(path))
            {
                
                string projectPath = System.IO.Path.GetFullPath(Application.dataPath + "/../");
                
                if (path.StartsWith(projectPath))
                {
                    
                    File.WriteAllText(path, shaderCode);
                    
                    
                    string relativePath = "Assets" + path.Substring(projectPath.Length + 6);
                    AssetDatabase.ImportAsset(relativePath);
                    
                    
                    _saved = true;
                    _savedLabel.style.display = DisplayStyle.Flex;
                    
                    EditorApplication.delayCall += () => {
                        EditorApplication.delayCall += () => {
                            if (_savedLabel != null)
                                _savedLabel.style.display = DisplayStyle.None;
                        };
                    };
                    
                    
                    Shader savedShader = AssetDatabase.LoadAssetAtPath<Shader>(relativePath);
                    if (savedShader != null)
                    {
                        Selection.activeObject = savedShader;
                        EditorGUIUtility.PingObject(savedShader);
                    }
                }
                else
                {
                    
                    File.WriteAllText(path, shaderCode);
                    
                    
                    _saved = true;
                    _savedLabel.style.display = DisplayStyle.Flex;
                    
                    EditorApplication.delayCall += () => {
                        EditorApplication.delayCall += () => {
                            if (_savedLabel != null)
                                _savedLabel.style.display = DisplayStyle.None;
                        };
                    };
                }
            }
        }
        #endregion

        #region Helper Methods
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
        
        private void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text) || 
                text.Contains("Your shader code will appear here")) return;
                
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
            
            // Debug.Log("Copied to clipboard");
        } 
        #endregion
    }
}