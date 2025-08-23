using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using uAI.PixelArtScaler;

namespace UAI
{
    public class AssetCreatorWindow : EditorWindow
    {
        #region Constants
        private const string WINDOW_TITLE = "Asset Creator"; 
         
        private const string DEFAULT_PROMPT = "Interior Tileset in a classic pixel art style, clear silhouettes, and crisp edges. 32×32, include modular tiles.";
        private const string PREFS_KEY_PREFIX = "UAI_AssetCreator_";
        private const string DEFAULT_SAVE_PATH = "Assets/AI Generated Images";
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.5f;
        private const int DEFAULT_IMAGE_LOAD_LIMIT = 10; 
        #endregion

        #region UI Elements
        // Main layout elements
        private VisualElement _rootElement;
        private VisualElement _createPanel;
        private VisualElement _creationsPanel;
        private VisualElement _trashPanel;
        private VisualElement _maskDrawingPanel;
        private VisualElement _imagePreviewPanel;
        private VisualElement _settingsPanel;

        private BatchImageGeneratorElement _batchGenerationPanel;
        
        // Create panel elements
        private TextField _promptField;
        private VisualElement _loadingOverlay;
        private VisualElement _referenceThumbnailsContainer;
        private Button _addReferenceButton;
        private List<ObjectField> _referenceImageFields = new List<ObjectField>();
        private DropdownField _modelDropdown;
        private DropdownField _sizeDropdown; 
        private DropdownField _qualityDropdown;
        private DropdownField _countDropdown;
        private Button _generateButton;
        private VisualElement _progressBar;
        // Preset panel elements
        private DropdownField _presetCategoryDropdown;
        private DropdownField _presetDropdown;
        private DropdownField _styleDropdown;
        private Button _applyPresetButton;
        private Button _closePresetButton; 
        private Button _showPresetButton;
        private VisualElement _presetPanelContainer;


        
        // Creations panel elements
        private ScrollView _timelineScrollView;
        private Slider _thumbnailSizeSlider;
        
        // Mask drawing panel elements
        private IMGUIContainer _maskDrawingContainer;
        private Slider _brushSizeSlider;
        private TextField _maskPromptField;
        private Button _drawButton;
        private Button _eraseButton;
        
        // Image preview panel elements
        private Image _previewImage;
        private Label _promptInfoLabel;
        private Label _dateInfoLabel;
        
        // Pagination state
        private int _loadedImageCount = 0;
        private int _totalImageCount = 0;
        private bool _hasMoreImages = false;

        // Settings panel elements
        private TextField _savePathField;
        private TextField _externalPathField;
        private Toggle _useExternalPathToggle;
        private VisualElement _externalPathContainer;
        private DropdownField _defaultModelDropdown;
        private DropdownField _defaultSizeDropdown;
        private DropdownField _defaultQualityDropdown;
        private DropdownField _defaultCountDropdown;
        #endregion

        #region Settings Variables
        // User configurable settings
        private string _savePath = DEFAULT_SAVE_PATH;
        private string _externalSavePath = "";
        private bool _useExternalPath = false;
        private ImageModel _defaultModel = ImageModel.GPTImage1;
        private Size _defaultSize = Size.Size1024x1024;
        private Quality _defaultQuality = Quality.high;
        private int _defaultImageCount = 1;
        #endregion

        #region State Variables
        // Selected panel state
        private PanelType _currentPanel = PanelType.Create;
        
        // Image generation state
        private List<Texture2D> _referenceImages = new List<Texture2D>();
        private string _currentPrompt = DEFAULT_PROMPT;
        private ImageModel _selectedModel = ImageModel.GPTImage1;
        private Size _selectedSize = Size.Size1024x1024;
        private Quality _selectedQuality = Quality.high;
        private int _imageCount = 1;
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        
        // Created images state
        private Dictionary<DateTime, List<CreatedImageEntry>> _createdImages = new Dictionary<DateTime, List<CreatedImageEntry>>();
        private List<CreatedImageEntry> _trashImages = new List<CreatedImageEntry>();
        private CreatedImageEntry _selectedImage = null;
        
        // Mask drawing state
        private CreatedImageEntry _imageBeingEdited = null;
        private Texture2D _maskTexture = null;
        private float _brushSize = 20f;
        private bool _isErasing = false;
        private Vector2 _lastDrawPosition;
        private bool _maskChanged = false;
        
        // Utility brushes for drawing
        private Texture2D _brushTexture;
        private Texture2D _eraserTexture;
        
        private float _thumbnailSize = 100f;
        private const string PREFS_KEY_THUMBNAIL_SIZE = "ThumbnailSize";

        #endregion

        #region Menu Item and Lifecycle Methods
        

        [MenuItem("Assets/uAI/Open in Asset Creator", false, 1041)]
        private static void ColorCorrectImage()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);  
            //get path of the sprite 
            AssetCreatorWindow window = AssetCreatorWindow.GetWindow<AssetCreatorWindow>();
            window.AddReferenceImage(path);
            window.ApplyPrompt("Create a variation of this image I send you.");
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                window.SelectBestFittingSizeByAspectRatio(texture.width, texture.height);
            } 
            window.Focus();
        }

        [MenuItem("Assets/uAI/Open in Asset Creator", true)]
        private static bool ValidateColorCorrectImage()
        {
            UnityEngine.Object selected = Selection.activeObject;
            if (selected == null)
                return false;

            string path = AssetDatabase.GetAssetPath(selected);
            string ext = Path.GetExtension(path).ToLower();

            // Only allow typical image formats
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".bmp" || ext == ".psd";
        }



        [MenuItem("Tools/uAI Creator/Asset Creator", false, 1)]
        public static void ShowWindow()
        {
            AssetCreatorWindow window = GetWindow<AssetCreatorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(1000, 650);
            //check if user is using OpenAI as the provider
            if (GPTClient.currentProvider != AIProvider.OpenAI && GPTClient.currentProvider != AIProvider.OpenAIReasoning && !EditorPrefs.HasKey(PREFS_KEY_PREFIX + "OpenAIWarningShown"))
            {
                window.ShowOpenAIWarning(); 
            }
            
            window.Show();
        }

        private void OnEnable()
        { 
            // Load settings
            AssetCreatorUtility.LoadSettings(PREFS_KEY_PREFIX, ref _savePath, ref _externalSavePath, 
                ref _useExternalPath, ref _defaultModel, ref _defaultSize, ref _defaultQuality, ref _defaultImageCount);
            
            // Initialize collections if needed
            if (_createdImages == null)
                _createdImages = new Dictionary<DateTime, List<CreatedImageEntry>>();
            
            if (_trashImages == null)
                _trashImages = new List<CreatedImageEntry>();
                
            // Set current generation settings from defaults
            _selectedModel = _defaultModel;
            _selectedSize = _defaultSize;
            _selectedQuality = _defaultQuality;
            _imageCount = _defaultImageCount;
            
            // Reset pagination state
            _loadedImageCount = 0;
            _totalImageCount = 0;
            _hasMoreImages = false;
                
            // Load saved data
            InitializeSavedImages();
            LoadTrashImages();
            
            // Initialize brushes
            _brushTexture = AssetCreatorUtility.CreateBrushTexture(32, false);
            _eraserTexture = AssetCreatorUtility.CreateBrushTexture(32, true);
            
            // Set up update for progress animation
            EditorApplication.update += UpdateProgress;
        }

        
        private void InitializeSavedImages()
        {
            // Initialize empty collections
            _createdImages = new Dictionary<DateTime, List<CreatedImageEntry>>();
            
            // Reset pagination state
            _loadedImageCount = 0;
            
            // Look for saved images in the project or external path
            string basePath = _useExternalPath ? _externalSavePath : _savePath;
            if (Directory.Exists(basePath))
            {
                // Get trash image paths to exclude them
                List<string> trashPaths = AssetCreatorUtility.GetTrashImagePaths(PREFS_KEY_PREFIX);
                
                // Load only the first batch of images
                _loadedImageCount = AssetCreatorUtility.LoadImagesFromPath(
                    basePath, 
                    ref _createdImages, 
                    Application.dataPath,
                    out _totalImageCount,
                    DEFAULT_IMAGE_LOAD_LIMIT,
                    0,
                    trashPaths
                );
                
                // Check if there are more images to load
                _hasMoreImages = _loadedImageCount < _totalImageCount;
            }
        }
        
        private void LoadMoreImages()
        {
            string basePath = _useExternalPath ? _externalSavePath : _savePath;
            if (Directory.Exists(basePath))
            {
                // Get trash image paths to exclude them
                List<string> trashPaths = AssetCreatorUtility.GetTrashImagePaths(PREFS_KEY_PREFIX);
                
                // Load the next batch of images
                int newImagesLoaded = AssetCreatorUtility.LoadImagesFromPath(
                    basePath, 
                    ref _createdImages, 
                    Application.dataPath,
                    out _totalImageCount,
                    DEFAULT_IMAGE_LOAD_LIMIT,
                    _loadedImageCount,
                    trashPaths
                );
                
                // Update loaded count
                _loadedImageCount += newImagesLoaded;
                
                // Check if there are more images to load
                _hasMoreImages = _loadedImageCount < _totalImageCount;
                
                // Refresh the UI
                RefreshCreationsPanel();
            }
        }
        
        private void LoadTrashImages()
        {
            _trashImages = AssetCreatorUtility.LoadTrashImages(PREFS_KEY_PREFIX, ref _createdImages);
        }



        private void OnDisable()
        {
            // Save trash images
            AssetCreatorUtility.SaveTrashImages(PREFS_KEY_PREFIX, _trashImages);
            
            // Remove update callback
            EditorApplication.update -= UpdateProgress;
            
            // Clean up resources
            CleanupResources();
        }

        public void CreateGUI()
        {
            _rootElement = rootVisualElement;

            var styleSheet = Resources.Load<StyleSheet>("AssetCreatorStyles");
            if (styleSheet != null)
            {
                _rootElement.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("Failed to load AssetCreatorStyles.uss");
            }

            // Load UXML
            var visualTree = Resources.Load<VisualTreeAsset>("AssetCreatorWindow");
            visualTree.CloneTree(_rootElement);


            // Get references to panels and UI elements
            InitializeUIReferences();

            // Set up event handlers
            SetupEventHandlers();

            // Initialize the first view
            SwitchPanel(_currentPanel);

            // Check if OpenAI is selected as the provider
            if (GPTClient.currentProvider != AIProvider.OpenAI && GPTClient.currentProvider != AIProvider.OpenAIReasoning )
            {
                ShowOpenAIWarning();
            }

            // Populate the model dropdowns
            PopulateModelDropdowns();

            // Update size options based on model
            UpdateSizeOptionsForModel();

            // Initialize reference image fields
            InitializeReferenceImageFields();
            
            
            UpdateReferenceThumbnails();
        } 


        private void InitializeUIReferences()
        {
            // Get panels
            _createPanel = _rootElement.Q<VisualElement>("create-panel");
            _creationsPanel = _rootElement.Q<VisualElement>("creations-panel");
            _trashPanel = _rootElement.Q<VisualElement>("trash-panel");
            _maskDrawingPanel = _rootElement.Q<VisualElement>("mask-drawing-panel");
            _imagePreviewPanel = _rootElement.Q<VisualElement>("image-preview-panel");
            _settingsPanel = _rootElement.Q<VisualElement>("settings-panel");
            _thumbnailSizeSlider = _rootElement.Q<Slider>("thumbnail-size-slider");


            _thumbnailSizeSlider.value = EditorPrefs.GetFloat(PREFS_KEY_PREFIX + PREFS_KEY_THUMBNAIL_SIZE, 100f);
            //find unity-tracker in thumbnail-size-slider
            _thumbnailSizeSlider.Q("unity-tracker").style.backgroundColor = new Color(0.43f, 0.43f, 0.43f, 1);
            _thumbnailSize = _thumbnailSizeSlider.value;

            // Create panel elements
            _promptField = _rootElement.Q<TextField>("prompt-field");
            
            var textInput = _promptField.Q("unity-text-input");
            if (textInput != null)
            {
                textInput.style.backgroundColor = new Color(0, 0, 0, 0);
                textInput.style.borderBottomWidth = 0;
                textInput.style.borderTopWidth = 0;
                textInput.style.borderLeftWidth = 0;
                textInput.style.borderRightWidth = 0;
            } 
            _promptField.value = _currentPrompt;
            
            _loadingOverlay = _rootElement.Q<VisualElement>("loading-overlay");
            _referenceThumbnailsContainer = _rootElement.Q<VisualElement>("reference-thumbnails-container");
            _addReferenceButton = _rootElement.Q<Button>("add-reference-button");
            
            _modelDropdown = _rootElement.Q<DropdownField>("model-dropdown");
            _sizeDropdown = _rootElement.Q<DropdownField>("size-dropdown"); 
            
            _qualityDropdown = _rootElement.Q<DropdownField>("quality-dropdown");
            _countDropdown = _rootElement.Q<DropdownField>("count-dropdown");
            _generateButton = _rootElement.Q<Button>("generate-button");
            _progressBar = _rootElement.Q<VisualElement>("progress-bar");
            
            // Creations panel elements
            _timelineScrollView = _rootElement.Q<ScrollView>("timeline-scroll-view");
            
            // Mask drawing panel elements
            _maskDrawingContainer = _rootElement.Q<IMGUIContainer>("mask-drawing-container");
            _maskDrawingContainer.onGUIHandler = OnMaskDrawingGUI;
            
            _brushSizeSlider = _rootElement.Q<Slider>("brush-size-slider");
            _brushSizeSlider.value = _brushSize;
            
            _maskPromptField = _rootElement.Q<TextField>("mask-prompt-field");
            _drawButton = _rootElement.Q<Button>("draw-button");
            _eraseButton = _rootElement.Q<Button>("erase-button");
            
            // Image preview panel elements
            _previewImage = _rootElement.Q<Image>("preview-image");
            _promptInfoLabel = _rootElement.Q<Label>("prompt-info-label");
            _dateInfoLabel = _rootElement.Q<Label>("date-info-label");
            
            // Settings panel elements
            _savePathField = _rootElement.Q<TextField>("save-path-field");
            _savePathField.value = _savePath;
            
            _externalPathField = _rootElement.Q<TextField>("external-path-field");
            _externalPathField.value = _externalSavePath;
            
            _useExternalPathToggle = _rootElement.Q<Toggle>("use-external-path-toggle");
            _useExternalPathToggle.value = _useExternalPath;
            
            _externalPathContainer = _rootElement.Q<VisualElement>("external-path-container");
            _externalPathContainer.style.display = _useExternalPath ? DisplayStyle.Flex : DisplayStyle.None;
            
            _defaultModelDropdown = _rootElement.Q<DropdownField>("default-model-dropdown");
            _defaultSizeDropdown = _rootElement.Q<DropdownField>("default-size-dropdown");
            _defaultQualityDropdown = _rootElement.Q<DropdownField>("default-quality-dropdown");
            _defaultCountDropdown = _rootElement.Q<DropdownField>("default-count-dropdown");

            // Preset panel elements
            _presetPanelContainer = _rootElement.Q<VisualElement>("preset-selection-overlay");
            _presetCategoryDropdown = _rootElement.Q<DropdownField>("ddCategory");
            _presetDropdown = _rootElement.Q<DropdownField>("ddSubCategory");
            _styleDropdown = _rootElement.Q<DropdownField>("ddStyle");
            _applyPresetButton = _rootElement.Q<Button>("btnSelectPreset");
            _closePresetButton = _rootElement.Q<Button>("btnClosePreset");
            _showPresetButton = _rootElement.Q<Button>("btnShowPreset");
            _presetPanelContainer.style.display = DisplayStyle.None;
            
            // Populate dropdowns
            PopulateDropdowns();
        }

        private void PopulateDropdowns()
        {
            // Populate quality dropdown
            _qualityDropdown.choices = System.Enum.GetNames(typeof(Quality)).ToList();
            _qualityDropdown.index = (int)_selectedQuality;

            // Populate count dropdown
            List<string> countOptions = new List<string>();
            for (int i = 1; i <= 10; i++)
            {
                countOptions.Add(i.ToString());
            }
            _countDropdown.choices = countOptions;
            _countDropdown.index = _imageCount - 1;

            // Populate default dropdowns
            _defaultModelDropdown.choices = System.Enum.GetNames(typeof(ImageModel)).ToList();
            _defaultModelDropdown.index = (int)_defaultModel;

            _defaultSizeDropdown.choices = System.Enum.GetNames(typeof(Size)).ToList();
            _defaultSizeDropdown.index = (int)_defaultSize;

            _defaultQualityDropdown.choices = System.Enum.GetNames(typeof(Quality)).ToList();
            _defaultQualityDropdown.index = (int)_defaultQuality;

            _defaultCountDropdown.choices = countOptions;
            _defaultCountDropdown.index = _defaultImageCount - 1;

            // Populate presets dropdown
            var presetCategoryDropdown = _rootElement.Q<DropdownField>("ddCategory");
            List<string> presetOptions =  new List<string>();
            //its a Dictionary<string, Dictionary<string, string>>  with category as key and a Dictionary of preset name and prompt as value
            presetOptions.AddRange(AssetCreatorUtility.categorizedGameArtPresets.Keys);
            presetCategoryDropdown.choices = presetOptions;
            presetCategoryDropdown.index = 0;

            // Populate subcategory dropdown
            var presetDropdown = _rootElement.Q<DropdownField>("ddSubCategory");
            var selectedCategory = presetCategoryDropdown.value;
            if (AssetCreatorUtility.categorizedGameArtPresets.ContainsKey(selectedCategory))
            {
                var subCategoryOptions = AssetCreatorUtility.categorizedGameArtPresets[selectedCategory].Keys.ToList();
                presetDropdown.choices = subCategoryOptions;
                presetDropdown.index = 0;
            }
            // Populate style dropdown 
 
            _styleDropdown.index = 0;
            List<string> styles = new List<string>();
            styles.AddRange(AssetCreatorUtility.gameArtStyles.Keys);
            _styleDropdown.choices = styles;
            _styleDropdown.index = 0;
        }

        private void PopulateModelDropdowns()
        {
            // Populate model dropdown
            _modelDropdown.choices = System.Enum.GetNames(typeof(ImageModel)).ToList();
            _modelDropdown.index = (int)_selectedModel;
        }

        private void InitializeReferenceImageFields()
        {
            // Create 4 image fields but don't add them to the UI
            for (int i = 0; i < 4; i++)
            {
                var imageField = new ObjectField();
                imageField.objectType = typeof(Texture2D);
                imageField.allowSceneObjects = false;
                int index = i; // Capture the index for the callback
                imageField.RegisterValueChangedCallback(evt => OnReferenceImageChanged(index, evt.newValue as Texture2D));
                
                _referenceImageFields.Add(imageField);
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
            _rootElement.Q<Button>("create-button").clicked += () => SwitchPanel(PanelType.Create);
            _rootElement.Q<Button>("creations-button").clicked += () => SwitchPanel(PanelType.Creations);
            _rootElement.Q<Button>("trash-button").clicked += () => SwitchPanel(PanelType.Trash);
            _rootElement.Q<Button>("settings-button").clicked += () => SwitchPanel(PanelType.Settings);
            //batch-button
            _rootElement.Q<Button>("batch-button").clicked += () => SwitchPanel(PanelType.BatchGeneration);
            _rootElement.Q<Button>("global-settings-button").clicked += () => SettingsWindow.ShowWindow();

            // Prompt field
            _promptField.RegisterValueChangedCallback(evt => _currentPrompt = evt.newValue);

            // Reference images
            _addReferenceButton.clicked += () => AddReferenceImage();

            // Preset dropdown 
            _presetCategoryDropdown.RegisterValueChangedCallback(evt =>
            {
                if (AssetCreatorUtility.categorizedGameArtPresets.ContainsKey(evt.newValue))
                {
                    var subCategoryOptions = AssetCreatorUtility.categorizedGameArtPresets[evt.newValue].Keys.ToList();
                    _presetDropdown.choices = subCategoryOptions;

                    _presetDropdown.index = 0;
                }
            });
            _applyPresetButton.clicked += () =>
            {
                var selectedCategory = _presetCategoryDropdown.value;
                var selectedSubCategory = _presetDropdown.value;
                var selectedStyle = _styleDropdown.value;

                string prompt = AssetCreatorUtility.GetPrompt(selectedCategory, selectedSubCategory, selectedStyle);
                if (!string.IsNullOrEmpty(prompt))
                {
                    ApplyPrompt(prompt);
                    _presetPanelContainer.style.display = DisplayStyle.None;
                }
                else
                {
                    Debug.LogError("Failed to apply preset. Prompt is empty.");
                }
            };
            _closePresetButton.clicked += () =>
            {
                _presetPanelContainer.style.display = DisplayStyle.None;
            };
            _showPresetButton.clicked += () =>
            {
                _presetPanelContainer.style.display = DisplayStyle.Flex;
            };

            // Model dropdown
            _modelDropdown.RegisterValueChangedCallback(evt =>
            {
                _selectedModel = (ImageModel)_modelDropdown.index;
                UpdateUIForModel();
                UpdateSizeOptionsForModel();
                UpdateQualityFromModel();
            });

            _sizeDropdown.RegisterValueChangedCallback(evt =>
            {
                UpdateSizeFromDropdown();
            });

            // Quality and count dropdowns
            _qualityDropdown.RegisterValueChangedCallback(evt =>
            {

                _selectedQuality = GetQualityEnumFromDropdown(evt.newValue);
            });

            _countDropdown.RegisterValueChangedCallback(evt =>
            {
                _imageCount = _countDropdown.index + 1;
            });

            // Generate button
            _generateButton.clicked += OnGenerateClicked;

            // Brush controls
            _brushSizeSlider.RegisterValueChangedCallback(evt =>
            {
                _brushSize = evt.newValue;
            });

            _drawButton.clicked += () => SetDrawMode(false);
            _eraseButton.clicked += () => SetDrawMode(true);
            _rootElement.Q<Button>("clear-mask-button").clicked += ClearMask;

            // Mask generation
            _rootElement.Q<Button>("generate-edit-button").clicked += GenerateEditedImage;
            _rootElement.Q<Button>("cancel-edit-button").clicked += () => SwitchPanel(PanelType.ImagePreview);

            // Preview panel actions
            _rootElement.Q<Button>("back-to-creations-button").clicked += () => SwitchPanel(PanelType.Creations);
            _rootElement.Q<Button>("edit-prompt-button").clicked += EditPrompt;
            _rootElement.Q<Button>("edit-image-button").clicked += EditImage;
            _rootElement.Q<Button>("remove-background-button").clicked += RemoveBackgroundEdit;
            _rootElement.Q<Button>("pixelate-button").clicked += PixelateImageEdit;
            _rootElement.Q<Button>("separateparts-button").clicked += SeparatePartsImageEdit;
            _rootElement.Q<Button>("make3Dmodel-button").clicked += SendTo3DEditor;
            _rootElement.Q<Button>("background-remover-tool-button").clicked += SendToBackgroundRemoverTool;
            _rootElement.Q<Button>("partsplitter-button").clicked += SendToPartSplitterTool;
            _rootElement.Q<Button>("pixelartscaler-button").clicked += SendToPixelArtScalerTool;
            _rootElement.Q<Button>("color-correction-button").clicked += SendToColorCorrectionTool;

            _rootElement.Q<Button>("edit-part-button").clicked += EditPartOfImage;
            _rootElement.Q<Button>("export-image-button").clicked += ExportImage;
            _rootElement.Q<Button>("delete-image-button").clicked += DeleteImage;

            // Trash actions
            _rootElement.Q<Button>("empty-trash-button").clicked += EmptyTrash;

            // Settings panel
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

            _rootElement.Q<Button>("save-settings-button").clicked += SaveSettings;
            _rootElement.Q<Button>("reset-settings-button").clicked += ResetSettings;
            _thumbnailSizeSlider.RegisterValueChangedCallback(evt => {
                _thumbnailSize = evt.newValue;
                EditorPrefs.SetFloat(PREFS_KEY_PREFIX + PREFS_KEY_THUMBNAIL_SIZE, _thumbnailSize);
                RefreshCreationsPanel();
            });
        }

        private Quality GetQualityEnumFromDropdown(string newValue)
        {
            newValue = newValue.ToLower();
            switch (_selectedModel)
            {
                case ImageModel.GPTImage1:
                    return (Quality)Enum.Parse(typeof(Quality), newValue);
                case ImageModel.DallE2:
                    return Quality.standard;
                case ImageModel.DallE3:
                    return (Quality)Enum.Parse(typeof(Quality), newValue);
                default:
                    return Quality.auto;
            }
        }
        #endregion

        #region UI Update Methods
        private void UpdateUIForModel()
        {
            // DALL-E 3 only supports 1 image
            if (_selectedModel == ImageModel.DallE3)
            {
                _countDropdown.value = "1";
                _countDropdown.SetEnabled(false);
                _imageCount = 1;
            }
            else
            {
                _countDropdown.SetEnabled(true);
            }
            
            // Enable or disable size icons based on model
            bool isGPTImage1 = _selectedModel == ImageModel.GPTImage1; 
        }
        private void UpdateSizeOptionsForModel()
        {
            // GPT Image1 supports only 3 sizes
            if (_selectedModel == ImageModel.GPTImage1)
            {
                _sizeDropdown.choices = new List<string> {
                    "1024x1024", "1536x1024", "1024x1536"
                };
                
                // Set to the closest available size
                switch (_selectedSize)
                {
                    case Size.Size1024x1024:
                        _sizeDropdown.index = 0;
                        SelectSize(Size.Size1024x1024);
                        break;
                    case Size.Size1536x1024:
                    case Size.Size1792x1024:
                        _sizeDropdown.index = 1;
                        SelectSize(Size.Size1536x1024);
                        break;
                    case Size.Size1024x1536:
                    case Size.Size1024x1792:
                        _sizeDropdown.index = 2;
                        SelectSize(Size.Size1024x1536);
                        break;
                }
            }else if(_selectedModel == ImageModel.DallE2)
            {
                // DALL-E 2 supports only 1 size
                _sizeDropdown.choices = new List<string> { "256x256" ,"512x512", "1024x1024" };
                _sizeDropdown.index = 0;
                SelectSize(Size.Size1024x1024);
            }
            else if(_selectedModel == ImageModel.DallE3)
            {
                // DALL-E 3 supports only 1 size
                _sizeDropdown.choices = new List<string> { "1024x1024", "1792x1024", "1024x1792" };
                _sizeDropdown.index = 0;
                SelectSize(Size.Size1024x1024);
            } 
        }
        
        private void UpdateQualityFromModel()
        {
            switch (_selectedModel)
            {
                case ImageModel.GPTImage1:
                    _qualityDropdown.choices = new List<string> { "High", "Medium", "Low",  };
                    break;
                case ImageModel.DallE2:
                    _qualityDropdown.choices = new List<string> { "standard" };
                    break;
                case ImageModel.DallE3:
                    _qualityDropdown.choices = new List<string> { "hd", "standard"  };
                    break;
                default:
                    _qualityDropdown.choices = new List<string> { "auto" };
                    break;
            }
            _qualityDropdown.index = 0;
        }
        private void UpdateSizeFromDropdown()
        {
            if (_sizeDropdown.index >= 0)
            {
                // Update selected size based on dropdown index
                switch (_selectedModel)
                {
                    case ImageModel.GPTImage1:
                        switch (_sizeDropdown.index)
                        {
                            case 0: SelectSize(Size.Size1024x1024); break;
                            case 1: SelectSize(Size.Size1536x1024); break;
                            case 2: SelectSize(Size.Size1024x1536); break;
                        }
                        break;
                    case ImageModel.DallE2:
                        switch (_sizeDropdown.index)
                        {
                            case 0: SelectSize(Size.Size256x256); break;
                            case 1: SelectSize(Size.Size512x512); break;
                            case 2: SelectSize(Size.Size1024x1024); break;
                        }
                        break;
                    case ImageModel.DallE3:
                        switch (_sizeDropdown.index)
                        {
                            case 0: SelectSize(Size.Size1024x1024); break;
                            case 1: SelectSize(Size.Size1792x1024); break;
                            case 2: SelectSize(Size.Size1024x1792); break;
                        }
                        break;
                    default:
                        switch (_sizeDropdown.index)
                        {
                            case 0: SelectSize(Size.Size1024x1024); break;
                            case 1: SelectSize(Size.Size1792x1024); break;
                            case 2: SelectSize(Size.Size1024x1792); break;
                            case 3: SelectSize(Size.Size1536x1024); break;
                            case 4: SelectSize(Size.Size1024x1536); break;
                        }
                        break;
                }
            }
        }

        private void SwitchPanel(PanelType panelType)
        {
            // Hide all panels
            _createPanel.style.display = DisplayStyle.None;
            _creationsPanel.style.display = DisplayStyle.None;
            _trashPanel.style.display = DisplayStyle.None;
            _maskDrawingPanel.style.display = DisplayStyle.None;
            _imagePreviewPanel.style.display = DisplayStyle.None;
            _settingsPanel.style.display = DisplayStyle.None;
            
            if(_batchGenerationPanel != null) _batchGenerationPanel.style.display = DisplayStyle.None;
            

            // Show selected panel
            switch (panelType)
            {
                case PanelType.Create:
                    _creationsPanel.style.display = DisplayStyle.Flex;
                    _createPanel.style.display = DisplayStyle.Flex;
                    RefreshCreationsPanel();
                    break;

                case PanelType.Creations:
                    _creationsPanel.style.display = DisplayStyle.Flex;
                    _createPanel.style.display = DisplayStyle.Flex;
                    RefreshCreationsPanel();
                    break;

                case PanelType.Trash:
                    _trashPanel.style.display = DisplayStyle.Flex;
                    RefreshTrashPanel();
                    break;

                case PanelType.MaskDrawing:
                    _maskDrawingPanel.style.display = DisplayStyle.Flex;
                    break;

                case PanelType.ImagePreview:
                    _imagePreviewPanel.style.display = DisplayStyle.Flex;
                    UpdateImagePreview();
                    break;

                case PanelType.Settings:
                    _settingsPanel.style.display = DisplayStyle.Flex;
                    break;

                case PanelType.BatchGeneration: 
                    VisualElement mainContent = _rootElement.Q<VisualElement>("main-content");
                    //check if it has _batchGenerationPanel
                    if (_batchGenerationPanel != null)
                    {
                        _batchGenerationPanel.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        _batchGenerationPanel = new BatchImageGeneratorElement(this);
                        _batchGenerationPanel.name = "batch-generation-panel";
                        mainContent.Add(_batchGenerationPanel);
                        _batchGenerationPanel.style.display = DisplayStyle.Flex;
                        //first child with name batch-image-generator-root also flex
                        VisualElement batchRoot = _batchGenerationPanel.Q<VisualElement>("batch-image-generator-root");
                        if (batchRoot != null)
                        {
                            batchRoot.style.display = DisplayStyle.Flex;
                        }
                        else
                        {
                            Debug.LogError("Batch Image Generator root element not found.");
                        }
                    }
                    break;
            }
            
            // Update sidebar button states
            UpdateSidebarButtonState(panelType);
            
            // Store current panel
            _currentPanel = panelType;
        }

        private void UpdateSidebarButtonState(PanelType activePanel)
        {
            // Reset all buttons
            _rootElement.Query<Button>(className: "sidebar-button").ForEach(btn => {
                btn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            });
            
            // Highlight active button
            string buttonName = "";
            switch (activePanel)
            {
                case PanelType.Create: buttonName = "create-button"; break;
                case PanelType.Creations: buttonName = "creations-button"; break;
                case PanelType.Trash: buttonName = "trash-button"; break;
                case PanelType.Settings: buttonName = "settings-button"; break;
            }
            
            if (!string.IsNullOrEmpty(buttonName))
            {
                var activeButton = _rootElement.Q<Button>(name: buttonName);
                if (activeButton != null)
                {
                    activeButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                }
            }
        }

        private void RefreshCreationsPanel()
        {
            // Clear existing content
            _timelineScrollView.Clear();
            
            // Let the utility handle the UI creation, passing the LoadMoreImages callback
            AssetCreatorUtility.PopulateCreationsPanel(
                _timelineScrollView, 
                _createdImages, 
                OnImageThumbnailClicked, 
                OnImageContextMenuRequest,
                _thumbnailSize,
                LoadMoreImages, 
                _hasMoreImages
            );
            
            // Display image count information
            // Find or create the image count label
            var countInfoContainer = _timelineScrollView.Q<VisualElement>("count-info-container");
            if (countInfoContainer == null)
            {
                countInfoContainer = new VisualElement();
                countInfoContainer.name = "count-info-container";
                countInfoContainer.style.alignSelf = Align.Center;
                countInfoContainer.style.marginTop = 5;
                countInfoContainer.style.marginBottom = 135;
                _timelineScrollView.Add(countInfoContainer);
                
                var countLabel = new Label();
                countLabel.name = "image-count-label";
                countInfoContainer.Add(countLabel);
            }
            
            // Update the count information
            var label = countInfoContainer.Q<Label>("image-count-label");
            if (label != null)
            {
                label.text = $"Showing {_loadedImageCount} of {_totalImageCount} images";
                label.style.fontSize = 12;
                label.style.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }




        private void OnImageThumbnailClicked(CreatedImageEntry image)
        {
            _selectedImage = image;
            SwitchPanel(PanelType.ImagePreview);
        }

        private void OnImageContextMenuRequest(ContextClickEvent evt, CreatedImageEntry image)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("View"), false, () => {
                _selectedImage = image;
                SwitchPanel(PanelType.ImagePreview);
            });
            
            menu.AddItem(new GUIContent("Edit/Edit Prompt"), false, () => {
                _selectedImage = image;
                EditPrompt();
            });
            
            menu.AddItem(new GUIContent("Edit/Edit Image"), false, () => {
                _selectedImage = image;
                EditImage();
            });
            
            menu.AddItem(new GUIContent("Edit/Edit Part of Image"), false, () => {
                _selectedImage = image;
                EditPartOfImage();
            });
            
            menu.AddItem(new GUIContent("Export"), false, () => {
                _selectedImage = image;
                ExportImage();
            });
            
            menu.AddItem(new GUIContent("Delete"), false, () => {
                _selectedImage = image;
                DeleteImage();
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
            AssetCreatorUtility.PopulateTrashPanel(trashScrollView, _trashImages, OnTrashContextMenuRequest);
        }

        private void OnTrashContextMenuRequest(ContextClickEvent evt, CreatedImageEntry image)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Restore"), false, () => {
                RestoreImage(image);
            });
            
            menu.AddItem(new GUIContent("Delete Permanently"), false, () => {
                DeletePermanently(image);
            });
            
            menu.ShowAsContext();
        }

        private void RestoreImage(CreatedImageEntry image)
        {
            AssetCreatorUtility.RestoreImageFromTrash(image, ref _trashImages, ref _createdImages);
            SaveTrashImages();
            RefreshTrashPanel();
            RefreshCreationsPanel();
        }

        private void DeletePermanently(CreatedImageEntry image)
        {
            // Confirm with the user
            bool confirm = EditorUtility.DisplayDialog(
                "Delete Permanently",
                "Are you sure you want to permanently delete this image? This cannot be undone.",
                "Yes", "No");
                
            if (!confirm) return;
            
            // Remove from trash
            _trashImages.Remove(image);
            
            // Save trash state
            SaveTrashImages();
            
            // Refresh UI
            RefreshTrashPanel();
        }

        private void UpdateImagePreview()
        {
            if (_selectedImage == null) return;
            
            // Update the image
            _previewImage.image = _selectedImage.Texture;
 
            // Update info
            _promptInfoLabel.text = _selectedImage.Prompt;
            _dateInfoLabel.text = _selectedImage.CreationDate.ToString("g");
        }

        private void ShowLoadingIndicator()
        {
            if (_loadingOverlay != null)
            {
                _loadingOverlay.style.display = DisplayStyle.Flex;
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
            if (_progressBar != null)
            {
                var progressFill = _progressBar.Q<VisualElement>("progress-fill");
                if (progressFill != null)
                {
                    progressFill.style.width = new Length(_progressValue * 100, LengthUnit.Percent);
                }
            }
            
            // Repaint window to update animations
            Repaint();
        }
        #endregion

        #region Persistence Methods
        private void SaveTrashImages()
        {
            AssetCreatorUtility.SaveTrashImages(PREFS_KEY_PREFIX, _trashImages);
        }
        #endregion

        #region Reference Image Methods
         
        public void AddReferenceImage(string path = "")
        {
            if (path == "")
                path = EditorUtility.OpenFilePanel("Select Reference Image", "", "png,jpg,jpeg");

            if (!string.IsNullOrEmpty(path))
            {
                // Load the image
                Texture2D texture = AssetCreatorUtility.LoadExternalTexture(path);

                if (texture != null)
                {
                    //check if this image is already added
                    if (_referenceImages.Contains(texture))
                    {
                        return;
                    }

                    // Find first available slot
                    int index = -1;
                    for (int i = 0; i < _referenceImages.Count; i++)
                    {
                        if (_referenceImages[i] == null)
                        {
                            index = i;
                            break;
                        }
                    }

                    // If no empty slot, add to the end (if < 4)
                    if (index == -1 && _referenceImages.Count < 4)
                    {
                        index = _referenceImages.Count;
                    }

                    // If we found a slot, add the image
                    if (index >= 0 && index < 4)
                    {
                        // Ensure our list is big enough
                        while (_referenceImages.Count <= index)
                        {
                            _referenceImages.Add(null);
                        }

                        // Add the texture to our references
                        _referenceImages[index] = texture;

                        // Make sure the corresponding object field is updated
                        if (index < _referenceImageFields.Count)
                        {
                            _referenceImageFields[index].value = texture;
                        }

                        // Update thumbnails
                        UpdateReferenceThumbnails();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Maximum References", "You can only add up to 4 reference images.", "OK");
                    }
                }
            }
        }

        private void UpdateReferenceThumbnails()
        {
            // Clear existing thumbnails
            _referenceThumbnailsContainer.Clear();
            
            // Add thumbnails for each reference image
            for (int i = 0; i < _referenceImages.Count; i++)
            {
                if (_referenceImages[i] != null)
                {
                    int index = i; // Capture the current index in a local variable
                    var thumbnailContainer = new VisualElement();
                    thumbnailContainer.AddToClassList("reference-thumbnail");
                    thumbnailContainer.style.width = 60;
                    thumbnailContainer.style.height = 60;
                    thumbnailContainer.style.marginRight = 5;
                    thumbnailContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    thumbnailContainer.style.borderTopLeftRadius = 3;
                    thumbnailContainer.style.borderTopRightRadius = 3;
                    thumbnailContainer.style.borderBottomLeftRadius = 3;
                    thumbnailContainer.style.borderBottomRightRadius = 3;
                    thumbnailContainer.style.overflow = Overflow.Hidden;
                    _referenceThumbnailsContainer.Add(thumbnailContainer);
                    
                    // Image thumbnail
                    var thumbnail = new Image();
                    thumbnail.image = _referenceImages[i];
                    thumbnail.scaleMode = ScaleMode.ScaleToFit;
                    thumbnail.style.width = 60;
                    thumbnail.style.height = 60;
                    thumbnailContainer.Add(thumbnail);
                    
                    // Remove button - use the captured index
                    var removeButton = new Button(() => RemoveReferenceImage(index));
                    removeButton.text = "×";
                    removeButton.AddToClassList("remove-button");
                    removeButton.style.position = Position.Absolute;
                    removeButton.style.top = 0;
                    removeButton.style.right = 0;
                    removeButton.style.width = 16;
                    removeButton.style.height = 16;
                    removeButton.style.fontSize = 12;
                    removeButton.style.backgroundColor = new Color(0, 0, 0, 0.7f);
                    removeButton.style.color = Color.white;
                    removeButton.style.paddingBottom = 0;
                    removeButton.style.paddingTop = 0;
                    removeButton.style.paddingLeft = 0;
                    removeButton.style.paddingRight = 0;
                    removeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
                    thumbnailContainer.Add(removeButton);
                }
            }
        }

        private void RemoveReferenceImage(int index)
        {
            // Remove the image from the list if index is valid
            if (index >= 0 && index < _referenceImages.Count)
            {
                _referenceImages[index] = null;
                
                // Also clear the object field if it exists
                if (index < _referenceImageFields.Count)
                {
                    _referenceImageFields[index].value = null;
                }
                
                // Update the thumbnails
                UpdateReferenceThumbnails();
            }
        }

        private void OnReferenceImageChanged(int index, Texture2D texture)
        {
            // Ensure our list is big enough
            while (_referenceImages.Count <= index)
            {
                _referenceImages.Add(null);
            }
            
            // Update the reference image
            _referenceImages[index] = texture;
            
            // Update thumbnails
            UpdateReferenceThumbnails();
        }
        #endregion

        #region Event Handlers
        public void ApplyPrompt(string presetPrompt)
        {
            _currentPrompt = presetPrompt;
            _promptField.value = presetPrompt;
        }

        private void SelectSize(Size size)
        {
            _selectedSize = size;
            
            // Update dropdown
            int index = 0;
            switch (size)
            {
                case Size.Size1024x1024: index = 0; break;
                case Size.Size1536x1024: 
                case Size.Size1792x1024: index = 1; break;
                case Size.Size1024x1536:
                case Size.Size1024x1792: index = 2; break;
            }
            
            if (_sizeDropdown.choices.Count > index)
                _sizeDropdown.index = index; 
        }

        public void SelectBestFittingSizeByAspectRatio(float width, float height)
        {
            float aspectRatio = width / height;
            // Select the best fitting size based on the aspect ratio
            //check model first
            if (_selectedModel == ImageModel.GPTImage1)
            {
                if (aspectRatio > 1.0f) // Wider than tall
                {
                    SelectSize(Size.Size1536x1024);
                }
                else if (aspectRatio < 1.0f) // Taller than wide
                {
                    SelectSize(Size.Size1024x1536);
                }
                else // Square
                {
                    SelectSize(Size.Size1024x1024);
                }
            }
            else if (_selectedModel == ImageModel.DallE2)
            {
                SelectSize(Size.Size1024x1024); // DALL-E 2 only supports square
            }
            else if (_selectedModel == ImageModel.DallE3)
            {
                if (aspectRatio > 1.0f) // Wider than tall
                {
                    SelectSize(Size.Size1792x1024);
                }
                else if (aspectRatio < 1.0f) // Taller than wide
                {
                    SelectSize(Size.Size1024x1792);
                }
                else // Square
                {
                    SelectSize(Size.Size1024x1024);
                }
            }
        }

        private void OnGenerateClicked()
        {

            //check if user is using OpenAI as the provider
            if (GPTClient.currentProvider != AIProvider.OpenAI && GPTClient.currentProvider != AIProvider.OpenAIReasoning && !EditorPrefs.HasKey(PREFS_KEY_PREFIX + "OpenAIWarningShown"))
            {
                ShowOpenAIWarning();
                return;
            }
            // Validate prompt
            if (string.IsNullOrWhiteSpace(_currentPrompt))
            {
                EditorUtility.DisplayDialog("Empty Prompt", "Please enter a description of the image you want to generate.", "OK");
                return;
            }

            if (_currentPrompt == DEFAULT_PROMPT)
            {
                //ask if user wants to use the default prompt
                bool useDefault = EditorUtility.DisplayDialog("Default Prompt", "You are using the default prompt. Do you want to continue?", "Yes", "No");
                if (!useDefault)
                {
                    return;
                }
            }

            // Check if OpenAI is selected as the provider
            if (GPTClient.currentProvider != AIProvider.OpenAI && GPTClient.currentProvider != AIProvider.OpenAIReasoning)
            {
                ShowOpenAIWarning();
                return;
            }

            // Validate save path
            string savePath = _useExternalPath ? _externalSavePath : _savePath;
            if (string.IsNullOrWhiteSpace(savePath))
            {
                EditorUtility.DisplayDialog("Invalid Save Path", "Please configure a valid save path in the settings.", "OK");
                SwitchPanel(PanelType.Settings);
                return;
            }

            // Start generation
            _isGenerating = true;
            _progressValue = 0f;
            ShowLoadingIndicator();

            // Disable UI during generation
            _generateButton.SetEnabled(false);

            // Generate images
            AssetCreatorUtility.GenerateImages(
                _currentPrompt,
                _selectedModel,
                _selectedSize,
                _selectedQuality,
                _imageCount,
                _referenceImages,
                OnImagesReceived,
                _savePath,
                _externalSavePath,
                _useExternalPath,
                Application.dataPath
            );

            ApplyPrompt("");
            //remove reference images
            for (int i = 0; i < _referenceImageFields.Count; i++)
            {
                _referenceImageFields[i].value = null;
            }
        }

        private void OnImagesReceived(List<CreatedImageEntry> createdEntries, bool success)
        {
            // End generation state
            _isGenerating = false;
            HideLoadingIndicator();
            _generateButton.SetEnabled(true);
            
            if (!success || createdEntries == null || createdEntries.Count == 0)
            {
                EditorUtility.DisplayDialog("Generation Failed", "Failed to generate images. Please try again.", "OK");
                return;
            }
            
            // Add to the dictionary
            DateTime now = DateTime.Now;
            if (_createdImages.ContainsKey(now.Date))
            {
                _createdImages[now.Date].AddRange(createdEntries);
            }
            else
            {
                _createdImages[now.Date] = createdEntries;
            }
            
            // Update image counts
            _loadedImageCount += createdEntries.Count;
            _totalImageCount += createdEntries.Count;
            
            // Refresh the timeline view
            RefreshCreationsPanel();
            
            // Switch to Creations panel
            SwitchPanel(PanelType.Creations);
        }


        private void OnMaskDrawingGUI()
        {
            AssetCreatorUtility.HandleMaskDrawingGUI(
                _imageBeingEdited, 
                ref _maskTexture, 
                _brushSize, 
                _isErasing, 
                ref _lastDrawPosition, 
                ref _maskChanged,
                this
            );
        }

        private void SetDrawMode(bool eraseMode)
        {
            _isErasing = eraseMode;
            
            // Update button appearance
            _drawButton.RemoveFromClassList("selected");
            _eraseButton.RemoveFromClassList("selected");
            
            if (eraseMode)
                _eraseButton.AddToClassList("selected");
            else
                _drawButton.AddToClassList("selected");
        }

        private void ClearMask()
        {
            if (_maskTexture == null) return;
            
            // Clear mask (make it fully transparent)
            Color[] pixels = new Color[_maskTexture.width * _maskTexture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            _maskTexture.SetPixels(pixels);
            _maskTexture.Apply();
            
            _maskChanged = false;
            
            // Force repaint
            Repaint();
        }

        private void GenerateEditedImage()
        {
            if (_imageBeingEdited == null || _maskTexture == null || _maskPromptField == null)
                return;
                
            // Get the prompt
            string prompt = _maskPromptField.value;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                EditorUtility.DisplayDialog("Empty Prompt", "Please enter a description of what to generate in the highlighted areas.", "OK");
                return;
            }
            
            // Check if anything was drawn on the mask
            if (!_maskChanged)
            {
                EditorUtility.DisplayDialog("No Selection", "Please draw on the areas you want to edit.", "OK");
                return;
            }
            
            // Check if OpenAI is selected
            if (GPTClient.currentProvider != AIProvider.OpenAI && GPTClient.currentProvider != AIProvider.OpenAIReasoning)
            {
                ShowOpenAIWarning();
                return;
            }
            
            // Start generation
            _isGenerating = true;
            _progressValue = 0f;
            
            // Disable buttons
            var generateButton = _maskDrawingPanel.Q<Button>("generate-edit-button");
            var cancelButton = _maskDrawingPanel.Q<Button>("cancel-edit-button");

            if (generateButton != null)
            {
                generateButton.SetEnabled(false);
                generateButton.text = "Generating... Don't close this window";
            }
            if (cancelButton != null) cancelButton.SetEnabled(false);
            
            AssetCreatorUtility.GenerateEditedImage(
                _imageBeingEdited,
                _maskTexture,
                prompt,
                OnEditedImageReceived,
                _savePath,
                _externalSavePath,
                _useExternalPath,
                Application.dataPath
            );
        }

        private void OnEditedImageReceived(CreatedImageEntry editedImage, bool success)
        {
            // End generation state
            _isGenerating = false;
            
            // Re-enable buttons
            var generateButton = _maskDrawingPanel.Q<Button>("generate-edit-button");
            var cancelButton = _maskDrawingPanel.Q<Button>("cancel-edit-button");
            
            if (generateButton != null) generateButton.SetEnabled(true);
            if (cancelButton != null) cancelButton.SetEnabled(true);
            
            if (!success || editedImage == null)
            {
                EditorUtility.DisplayDialog("Generation Failed", "Failed to generate the edited image.", "OK");
                return;
            }
            
            // Add to the dictionary
            DateTime now = DateTime.Now;
            if (_createdImages.ContainsKey(now.Date))
            {
                _createdImages[now.Date].Add(editedImage);
            }
            else
            {
                _createdImages[now.Date] = new List<CreatedImageEntry> { editedImage };
            }
            
            // Clean up
            if (_maskTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_maskTexture);
                _maskTexture = null;
            }
            _imageBeingEdited = null;
            _maskChanged = false;
            
            // Refresh UI and go to the creations panel
            RefreshCreationsPanel();
            SwitchPanel(PanelType.Creations);
        }

        private void EditPrompt()
        {
            if (_selectedImage == null) return;
            
            // Set the prompt text
            _currentPrompt = _selectedImage.Prompt;
            _promptField.value = _currentPrompt;
            
            // Clear reference images
            for (int i = 0; i < _referenceImageFields.Count; i++)
            {
                _referenceImageFields[i].value = null;
            }
            _referenceImages.Clear();
            UpdateReferenceThumbnails();
            
            // Switch to create panel
            SwitchPanel(PanelType.Create);
        }
        private void RemoveBackgroundEdit()
        {
            if (_selectedImage == null) return;

            // Set the prompt text
            _currentPrompt = "Remove the background of this image. Keep everything else the same.";
            _promptField.value = _currentPrompt;
            
            // Set the image as the first reference
            if (_referenceImageFields.Count > 0)
            {
                _referenceImageFields[0].value = _selectedImage.Texture;
                OnReferenceImageChanged(0, _selectedImage.Texture);
            }
            
            // Clear other reference images
            for (int i = 1; i < _referenceImageFields.Count; i++)
            {
                _referenceImageFields[i].value = null;
                
                if (i < _referenceImages.Count)
                {
                    _referenceImages[i] = null;
                }
            }
            
            // Update thumbnails
            UpdateReferenceThumbnails();
            
            // Switch to create panel
            SwitchPanel(PanelType.Create);
        }
        private void SendTo3DEditor()
        { 
            if (_selectedImage == null) return;

            //open trellisAPIEditor
            TrellisAPIEditor.OpenWithImage(_selectedImage.AssetPath);  
        }
        private void SendToPartSplitterTool()
        {
            if (_selectedImage == null) return;

            // Open the part splitter tool with the selected image
            ImagePartDetector.OpenWithImage(_selectedImage.AssetPath);
        }

        private void SendToPixelArtScalerTool()
        { 
            if (_selectedImage == null) return;
            PixelartScalerWindow.OpenWithImage(_selectedImage.AssetPath);
        }
        private void SendToColorCorrectionTool()
        {
            if (_selectedImage == null) return;

            // Open the color correction tool with the selected image
            ImageCorrectionWindow.OpenWithImage(_selectedImage.AssetPath);
        }

        private void SendToBackgroundRemoverTool()
        {
            if (_selectedImage == null) return;

            // Open the background remover tool with the selected image
            BackgroundRemoverEditorWindow.OpenWithImage(_selectedImage.AssetPath);
        }

        private void SeparatePartsImageEdit()
        {
            if (_selectedImage == null) return;

            // Set the prompt text
            _currentPrompt = "Separate the object shown on this image into logically pieces. Use the same art style, don't change the aesthetics. We need the pieces clearly separated from each other.";
            _promptField.value = _currentPrompt;

            // Set the image as the first reference
            if (_referenceImageFields.Count > 0)
            {
                _referenceImageFields[0].value = _selectedImage.Texture;
                OnReferenceImageChanged(0, _selectedImage.Texture);
            }

            // Clear other reference images
            for (int i = 1; i < _referenceImageFields.Count; i++)
            {
                _referenceImageFields[i].value = null;

                if (i < _referenceImages.Count)
                {
                    _referenceImages[i] = null;
                }
            }

            // Update thumbnails
            UpdateReferenceThumbnails();

            // Switch to create panel
            SwitchPanel(PanelType.Create);
        }
        private void PixelateImageEdit()
        {
            if (_selectedImage == null) return;

            // Set the prompt text
            _currentPrompt = "Transform this image into a pixel art style.";
            _promptField.value = _currentPrompt;

            // Set the image as the first reference
            if (_referenceImageFields.Count > 0)
            {
                _referenceImageFields[0].value = _selectedImage.Texture;
                OnReferenceImageChanged(0, _selectedImage.Texture);
            }

            // Clear other reference images
            for (int i = 1; i < _referenceImageFields.Count; i++)
            {
                _referenceImageFields[i].value = null;

                if (i < _referenceImages.Count)
                {
                    _referenceImages[i] = null;
                }
            }

            // Update thumbnails
            UpdateReferenceThumbnails();

            // Switch to create panel
            SwitchPanel(PanelType.Create);
        }
        
        private void EditImage()
        {
            if (_selectedImage == null) return;

            // Set the prompt text
            _currentPrompt = _selectedImage.Prompt;
            _promptField.value = _currentPrompt;

            // Set the image as the first reference
            if (_referenceImageFields.Count > 0)
            {
                _referenceImageFields[0].value = _selectedImage.Texture;
                OnReferenceImageChanged(0, _selectedImage.Texture);
            }

            // Clear other reference images
            for (int i = 1; i < _referenceImageFields.Count; i++)
            {
                _referenceImageFields[i].value = null;

                if (i < _referenceImages.Count)
                {
                    _referenceImages[i] = null;
                }
            }

            // Update thumbnails
            UpdateReferenceThumbnails();

            // Switch to create panel
            SwitchPanel(PanelType.Create);
        }

        private void EditPartOfImage()
        {
            if (_selectedImage == null) return;
            
            _imageBeingEdited = _selectedImage;
            
            // Create a new mask texture
            if (_maskTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_maskTexture);
            }
            
            // Create a blank mask (transparent = keep original)
            _maskTexture = AssetCreatorUtility.CreateBlankMask(
                _selectedImage.Texture.width,
                _selectedImage.Texture.height
            );
            
            // Reset drawing state
            _isErasing = false;
            _lastDrawPosition = Vector2.zero;
            _maskChanged = false;
            
            // Set draw mode active
            SetDrawMode(false);
            
            // Set brush size to default
            _brushSizeSlider.value = 20f;
            
            // Pre-populate the prompt field
            _maskPromptField.value = _selectedImage.Prompt;

            
            // Disable buttons
            var generateButton = _maskDrawingPanel.Q<Button>("generate-edit-button"); 

            if (generateButton != null)
            { 
                generateButton.text = "Generate";
            }
            
            // Switch to mask drawing panel
            SwitchPanel(PanelType.MaskDrawing);
        }

        private void ExportImage()
        {
            AssetCreatorUtility.ExportImage(_selectedImage, Application.dataPath);
        }

        private void DeleteImage()
        {
            if (_selectedImage == null) return;
            
            // Confirm with the user
            bool confirm = EditorUtility.DisplayDialog(
                "Delete Image",
                "Are you sure you want to move this image to trash?",
                "Yes", "No");
                
            if (!confirm) return;
            
            // Mark as in trash
            _selectedImage.IsInTrash = true;
            
            // Find and remove the image from created images
            foreach (var date in _createdImages.Keys.ToList())
            {
                var images = _createdImages[date];
                if (images.Contains(_selectedImage))
                {
                    images.Remove(_selectedImage);
                    
                    // Add to trash
                    _trashImages.Add(_selectedImage);
                    
                    // If date group is now empty, remove it
                    if (images.Count == 0)
                    {
                        _createdImages.Remove(date);
                    }
                    
                    break;
                }
            }
            
            // Refresh UI
            RefreshCreationsPanel();
            
            // Save trash state
            SaveTrashImages();
            
            // Go back to creations panel
            SwitchPanel(PanelType.Creations);
        }

        private void EmptyTrash()
        {
            if (_trashImages.Count == 0) return;
            
            // Confirm with the user
            bool confirm = EditorUtility.DisplayDialog(
                "Empty Trash",
                "Are you sure you want to permanently delete all images in the trash? This cannot be undone.",
                "Yes", "No");
                
            if (!confirm) return;
            
            // Delete all trash images
            _trashImages.Clear();
            
            // Save trash state
            SaveTrashImages();
            
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
            // Get values from UI
            _defaultModel = (ImageModel)_defaultModelDropdown.index;
            _defaultSize = (Size)_defaultSizeDropdown.index;
            _defaultQuality = (Quality)_defaultQualityDropdown.index;
            _defaultImageCount = _defaultCountDropdown.index + 1;
            
            // Save all settings to EditorPrefs
            AssetCreatorUtility.SaveSettings(
                PREFS_KEY_PREFIX,
                _savePath, 
                _externalSavePath, 
                _useExternalPath,
                _defaultModel, 
                _defaultSize, 
                _defaultQuality, 
                _defaultImageCount
            );
            
            // Update current generation settings with defaults
            _selectedModel = _defaultModel;
            _selectedSize = _defaultSize;
            _selectedQuality = _defaultQuality;
            _imageCount = _defaultImageCount;
            
            // Update UI
            if (_modelDropdown != null)
                _modelDropdown.index = (int)_selectedModel;
                
            UpdateSizeOptionsForModel();
                
            if (_qualityDropdown != null)
                _qualityDropdown.index = (int)_selectedQuality;
                
            if (_countDropdown != null)
                _countDropdown.index = _imageCount - 1;
            
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
            _savePath = DEFAULT_SAVE_PATH;
            _externalSavePath = "";
            _useExternalPath = false;
            _defaultModel = ImageModel.GPTImage1;
            _defaultSize = Size.Size1024x1024;
            _defaultQuality = Quality.high;
            _defaultImageCount = 1;
            
            // Update UI fields
            _savePathField.value = _savePath;
            _externalPathField.value = _externalSavePath;
            _useExternalPathToggle.value = _useExternalPath;
            _defaultModelDropdown.index = (int)_defaultModel;
            _defaultSizeDropdown.index = (int)_defaultSize;
            _defaultQualityDropdown.index = (int)_defaultQuality;
            _defaultCountDropdown.index = _defaultImageCount - 1;
            
            // Save these defaults
            SaveSettings();
        }
        #endregion

        #region Utility Methods
        private void CancelGeneration()
        {
            if (!_isGenerating) return;
            
            GPTClient.StopGeneration();
            _isGenerating = false;
            HideLoadingIndicator();
            
            // Re-enable button
            _generateButton.SetEnabled(true);
        }


        private void ShowOpenAIWarning()
        {
            bool openSettings = EditorUtility.DisplayDialog(
                "OpenAI API Required",
                "The Asset Creator requires OpenAI as the API provider. Would you like to open settings to change the provider?",
                "Open Settings", "Cancel");

            if (openSettings)
            {
                // SwitchPanel(PanelType.Settings);
                //open global settings window
                SettingsWindow.ShowWindow();
            }
        }

        private void CleanupResources()
        {
            // Clean up brush textures
            if (_brushTexture != null)
                UnityEngine.Object.DestroyImmediate(_brushTexture);
                
            if (_eraserTexture != null)
                UnityEngine.Object.DestroyImmediate(_eraserTexture);
                
            if (_maskTexture != null)
                UnityEngine.Object.DestroyImmediate(_maskTexture);
        }
        #endregion
    }

    #region Models
    public enum PanelType
    {
        Create,
        Creations,
        Trash,
        MaskDrawing,
        ImagePreview,
        Settings,
        BatchGeneration
    }

    [Serializable]
    public class CreatedImageEntry
    {
        public Texture2D Texture;
        public string Prompt;
        public string RevisedPrompt;
        public DateTime CreationDate;
        public ImageModel Model;
        public string AssetPath;
        public bool IsInTrash;
    }

    [Serializable]
    public class TrashImagesData
    {
        public List<string> TrashPaths = new List<string>();
    }
    #endregion
}