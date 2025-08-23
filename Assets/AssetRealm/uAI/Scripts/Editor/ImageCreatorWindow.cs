using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace UAI
{
    public class ImageCreatorWindow : EditorWindow
    {
        // Constants
        private const string k_WindowTitle = "AI Image Creator";
        private const string k_WindowMenuPath = "Tools/uAI Creator/Image Creator";
        private const string k_StylePath = "ImageCreatorStyles";
        private const string k_UxmlPath = "ImageCreatorWindow"; 

        // UI Elements
        private TabButtonStrip m_TabStrip;
        private VisualElement m_ContentContainer;
        private VisualElement[] m_TabContents;

        // Tab pages
        private ImageGenerationPage m_GenerationPage;
        private ImageSeamlessPage m_SeamlessPage;
        private ImageVariationPage m_VariationPage;
        private ImageMaskDrawingPage m_MaskDrawingPage;
        public static ImageCreatorWindow window => GetWindow<ImageCreatorWindow>();
        // State
        private int m_ActiveTabIndex = 0;

        [MenuItem(k_WindowMenuPath, false, 158)]
        public static void ShowWindow()
        {
            var wd = GetWindow<ImageCreatorWindow>();
            wd.titleContent = new GUIContent(k_WindowTitle);
            wd.minSize = new Vector2(700, 650);
        }

        public void CreateGUI()
        {
            // Load and apply styles
            StyleSheet styleSheet = Resources.Load<StyleSheet>(k_StylePath);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            // Load main layout
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(k_UxmlPath);
            if (visualTree != null)
                visualTree.CloneTree(rootVisualElement);

            // Get references to key UI elements
            m_TabStrip = rootVisualElement.Q<TabButtonStrip>("tab-strip");
            m_ContentContainer = rootVisualElement.Q<VisualElement>("content-container");

            // Create tab pages
            InitializeTabs();

            // Register callbacks
            m_TabStrip.onTabSelected += OnTabSelected;

            // Load previous session state
            LoadEditorPrefs();
            
            // Set initial tab
            SwitchToTab(m_ActiveTabIndex);
        }


        private void InitializeTabs()
        {
            // Initialize page handlers
            m_GenerationPage = new ImageGenerationPage(this);
            m_SeamlessPage = new ImageSeamlessPage(this);
            m_VariationPage = new ImageVariationPage(this);
            m_MaskDrawingPage = new ImageMaskDrawingPage(this); // Add this line
            
            // Create tab contents
            m_TabContents = new VisualElement[] 
            {
                m_GenerationPage.CreateUI(),
                m_SeamlessPage.CreateUI(),
                m_VariationPage.CreateUI(),
                m_MaskDrawingPage.CreateUI() // Add this line
            };

            // Add tab contents to the window
            foreach (var content in m_TabContents)
            {
                m_ContentContainer.Add(content);
                content.style.display = DisplayStyle.None;
            }

            // Add tabs to the tab strip
            m_TabStrip.AddTab("Image Generation");
            m_TabStrip.AddTab("Seamless Texture");
            m_TabStrip.AddTab("Image Variation");
            m_TabStrip.AddTab("Mask Drawing"); // Add this line
        }

        private void OnTabSelected(int tabIndex)
        {
            SwitchToTab(tabIndex);
        }

        private void SwitchToTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= m_TabContents.Length)
                return;

            // Hide all tabs
            foreach (var content in m_TabContents)
            {
                content.style.display = DisplayStyle.None;
            }

            // Show selected tab
            m_TabContents[tabIndex].style.display = DisplayStyle.Flex;
            
            // Update state
            m_ActiveTabIndex = tabIndex;
            m_TabStrip.SetActiveTab(tabIndex);
            
            // Save state
            SaveEditorPrefs();
        }

        #region Editor Prefs

        private void SaveEditorPrefs()
        {
            EditorPrefs.SetInt($"UAI_ImageCreator_ActiveTab", m_ActiveTabIndex);
            
            // Save page-specific settings
            m_GenerationPage.SaveSettings();
            m_SeamlessPage.SaveSettings();
            m_VariationPage.SaveSettings();
            m_MaskDrawingPage.SaveSettings();
        }

        private void LoadEditorPrefs()
        {
            m_ActiveTabIndex = EditorPrefs.GetInt($"UAI_ImageCreator_ActiveTab", 0);
            
            // Load page-specific settings
            m_GenerationPage.LoadSettings();
            m_SeamlessPage.LoadSettings();
            m_VariationPage.LoadSettings();
            m_MaskDrawingPage.LoadSettings(); 
        }

        #endregion
    }

    #region Tab Button Strip

    // Custom TabButtonStrip control for UIElements
    public class TabButtonStrip : VisualElement
    {
        private List<Button> m_TabButtons = new List<Button>();
        public event Action<int> onTabSelected;

        private int m_ActiveTabIndex = -1;

        public new class UxmlFactory : UxmlFactory<TabButtonStrip, UxmlTraits> { }


        public TabButtonStrip()
        {
            style.flexDirection = FlexDirection.Row;
            style.justifyContent = Justify.Center;
            style.marginTop = 10;
            style.marginBottom = 10;
        }

        public void AddTab(string label)
        {
            Button tabButton = new Button(() => OnTabButtonClicked(m_TabButtons.Count))
            {
                text = label
            };
            
            tabButton.AddToClassList("tab-button");
            tabButton.style.width = 150;
            tabButton.style.height = 30;
            tabButton.style.marginLeft = 5;
            tabButton.style.marginRight = 5;
            
            m_TabButtons.Add(tabButton);
            Add(tabButton);
            
            // If this is the first tab, select it
            if (m_TabButtons.Count == 1 && m_ActiveTabIndex == -1)
            {
                SetActiveTab(0);
            }

            // Set the tab button's click event
            tabButton.RegisterCallback<ClickEvent>(evt =>
            {
                SetActiveTab(m_TabButtons.IndexOf(tabButton));
                onTabSelected?.Invoke(m_TabButtons.IndexOf(tabButton));
            });
        }

        public void SetActiveTab(int index)
        {
            if (index < 0 || index >= m_TabButtons.Count)
                return;

            // Deactivate current tab
            if (m_ActiveTabIndex >= 0 && m_ActiveTabIndex < m_TabButtons.Count)
            {
                m_TabButtons[m_ActiveTabIndex].RemoveFromClassList("tab-button-active");
            }

            // Activate new tab
            m_TabButtons[index].AddToClassList("tab-button-active");
            m_ActiveTabIndex = index;
 
        }

        private void OnTabButtonClicked(int index)
        {
            SetActiveTab(index);
            onTabSelected?.Invoke(index);
        }
    }

    #endregion
    #region Base Tab Page

    #endregion

    #region Image Generation Page

    // Page for generating images from text prompts
    public class ImageGenerationPage : TabPageBase
    {
        // UI Elements
        private TextField m_PromptField;
        private SliderInt m_CountSlider;
        private PopupField<string> m_SizeField;
        private EnumField m_ModelField;
        private EnumField m_StyleField;
        private EnumField m_QualityField;
        private Toggle m_AdvancedToggle;
        private VisualElement m_AdvancedPanel;
        private EnumField m_BackgroundField;
        private EnumField m_OutputFormatField;
        private SliderInt m_CompressionSlider;
        private EnumField m_ModerationField;
        private Button m_GenerateButton;
        private Label m_StatusLabel;
        private ScrollView m_ResultsScrollView;
        private VisualElement m_PreviewContainer;
        private Image m_PreviewImage;
        private Button m_SaveButton;

        private VisualElement resultsContainer;
        // State
        private List<DALLEImageResult> m_GeneratedImages = new List<DALLEImageResult>();
        private DALLEImageResult m_SelectedImage;
        private string m_Prompt = "A futuristic city with flying cars.";
        private ImageModel m_SelectedModel = ImageModel.GPTImage1;
        private int m_ImageCount = 1;
        private Size m_SelectedSize = Size.Size1024x1024;
        private Quality m_SelectedQuality = Quality.high;
        private Style m_SelectedStyle = Style.Vivid;

        // Dictionaries for dropdowns
        private Dictionary<ImageModel, string[]> m_ModelSizeOptions = new Dictionary<ImageModel, string[]>
        {
            { ImageModel.DallE2, new[] { "256x256", "512x512", "1024x1024" } },
            { ImageModel.DallE3, new[] { "1024x1024", "1792x1024", "1024x1792" } },
            { ImageModel.GPTImage1, new[] { "1024x1024", "1536x1024", "1024x1536" } }
        };

        private Dictionary<string, Size> m_SizeEnumMap = new Dictionary<string, Size>
        {
            { "256x256", Size.Size256x256 },
            { "512x512", Size.Size512x512 },
            { "1024x1024", Size.Size1024x1024 },
            { "1792x1024", Size.Size1792x1024 },
            { "1024x1792", Size.Size1024x1792 },
            { "1536x1024", Size.Size1536x1024 },
            { "1024x1536", Size.Size1024x1536 }
        };

        public ImageGenerationPage(ImageCreatorWindow window) : base(window)
        {
        }

        public override VisualElement CreateUI()
        {
            m_Root = new VisualElement();
            m_Root.AddToClassList("tab-content");

            // Header
            var header = new Label("Image Generation");
            header.AddToClassList("header-label");
            m_Root.Add(header);

            // Description
            var description = new Label("Describe the image you want to generate");
            description.AddToClassList("description-label");
            m_Root.Add(description);

            // Prompt field
            m_PromptField = new TextField
            {
                multiline = true,
                value = m_Prompt
            };
            m_PromptField.AddToClassList("prompt-field");
            m_PromptField.RegisterValueChangedCallback(e => m_Prompt = e.newValue); 
            m_PromptField.Q("unity-text-input").style.alignSelf = Align.Auto;
            m_Root.Add(m_PromptField);

            // Settings panel
            var settingsPanel = new VisualElement();
            settingsPanel.AddToClassList("settings-panel");
            m_Root.Add(settingsPanel);

            // Basic settings
            var basicSettings = new VisualElement();
            basicSettings.AddToClassList("basic-settings");
            settingsPanel.Add(basicSettings);

            // Model selection
            var modelContainer = new VisualElement();
            modelContainer.AddToClassList("setting-row");
            basicSettings.Add(modelContainer); 

            m_ModelField = new EnumField("Model", m_SelectedModel);
            m_ModelField.RegisterValueChangedCallback(e =>
            {
                m_SelectedModel = (ImageModel)e.newValue;
                UpdateUIForModel();
            });
            modelContainer.Add(m_ModelField);

            // Size selection
            var sizeContainer = new VisualElement();
            sizeContainer.AddToClassList("setting-row");
            basicSettings.Add(sizeContainer);

            var sizeLabel = new Label("Size:");
            sizeContainer.Add(sizeLabel);

            m_SizeField = new PopupField<string>(
                m_ModelSizeOptions[m_SelectedModel].ToList(),
                0,
                formatSelectedValueCallback: (s) => s,
                formatListItemCallback: (s) => s
            );
            m_SizeField.RegisterValueChangedCallback(e =>
            {
                if (m_SizeEnumMap.TryGetValue(e.newValue, out Size size))
                {
                    m_SelectedSize = size;
                }
            });
            sizeContainer.Add(m_SizeField);

            // Count slider
            var countContainer = new VisualElement();
            countContainer.AddToClassList("setting-row");
            basicSettings.Add(countContainer);

            var countLabel = new Label("Number of Images:");
            countContainer.Add(countLabel);

            m_CountSlider = new SliderInt(1, 10)
            {
                value = m_ImageCount,
                showInputField = true
            };
            m_CountSlider.RegisterValueChangedCallback(e => m_ImageCount = e.newValue);
            countContainer.Add(m_CountSlider);

            // Quality
            var qualityContainer = new VisualElement();
            qualityContainer.AddToClassList("setting-row");
            basicSettings.Add(qualityContainer); 
            m_QualityField = new EnumField("Quality", m_SelectedQuality);
            m_QualityField.RegisterValueChangedCallback(e => m_SelectedQuality = (Quality)e.newValue);
            qualityContainer.Add(m_QualityField);

            // Style (for DALL-E 3)
            var styleContainer = new VisualElement();
            styleContainer.AddToClassList("setting-row");
            basicSettings.Add(styleContainer);
 
            m_StyleField = new EnumField("Style", m_SelectedStyle);
            m_StyleField.RegisterValueChangedCallback(e => m_SelectedStyle = (Style)e.newValue);
            styleContainer.Add(m_StyleField);

            // Advanced settings toggle
            m_AdvancedToggle = new Toggle("Advanced Settings");
            m_AdvancedToggle.AddToClassList("advanced-toggle");
            m_AdvancedToggle.RegisterValueChangedCallback(e =>
            {
                m_AdvancedPanel.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            basicSettings.Add(m_AdvancedToggle);

            // Advanced settings panel
            m_AdvancedPanel = new VisualElement();
            m_AdvancedPanel.AddToClassList("advanced-panel");
            m_AdvancedPanel.style.display = DisplayStyle.None;
            settingsPanel.Add(m_AdvancedPanel);

            // Background
            var backgroundContainer = new VisualElement();
            backgroundContainer.AddToClassList("setting-row");
            m_AdvancedPanel.Add(backgroundContainer);
 

            m_BackgroundField = new EnumField("Background", BackgroundType.Auto);
            m_BackgroundField.RegisterValueChangedCallback(e => { /* Handle background change */ });
            backgroundContainer.Add(m_BackgroundField);

            // Output format
            var formatContainer = new VisualElement();
            formatContainer.AddToClassList("setting-row");
            m_AdvancedPanel.Add(formatContainer);

            var formatLabel = new Label("Output Format:");
            formatContainer.Add(formatLabel);

            m_OutputFormatField = new EnumField("Format", OutputFormat.PNG);
            m_OutputFormatField.RegisterValueChangedCallback(e => { /* Handle format change */ });
            formatContainer.Add(m_OutputFormatField);

            // Compression slider (for JPEG/WebP)
            var compressionContainer = new VisualElement();
            compressionContainer.AddToClassList("setting-row");
            m_AdvancedPanel.Add(compressionContainer);

            var compressionLabel = new Label("Compression:");
            compressionContainer.Add(compressionLabel);

            m_CompressionSlider = new SliderInt(0, 100)
            {
                value = 100,
                showInputField = true
            };
            compressionContainer.Add(m_CompressionSlider);

            // Moderation
            var moderationContainer = new VisualElement();
            moderationContainer.AddToClassList("setting-row");
            m_AdvancedPanel.Add(moderationContainer);
 

            m_ModerationField = new EnumField("Moderation", Moderation.Auto);
            moderationContainer.Add(m_ModerationField);

            // Generate button
            m_GenerateButton = new Button(OnGenerateClicked)
            {
                text = "Generate Image"
            };
            m_GenerateButton.AddToClassList("generate-button");
            m_Root.Add(m_GenerateButton);

            // Status label
            m_StatusLabel = new Label();
            m_StatusLabel.AddToClassList("status-label");
            m_Root.Add(m_StatusLabel);

            // Results container
            resultsContainer = new VisualElement();
            resultsContainer.AddToClassList("results-container");
            if (m_GeneratedImages.Count > 0){
                m_Window.minSize = new Vector2(700, 900);
                m_Root.Add(resultsContainer); 
            }

            // Thumbnails scrollview
            m_ResultsScrollView = new ScrollView(ScrollViewMode.Vertical);
            m_ResultsScrollView.AddToClassList("thumbnails-scroll");
            resultsContainer.Add(m_ResultsScrollView);

            // Preview container
            m_PreviewContainer = new VisualElement();
            m_PreviewContainer.AddToClassList("preview-container");
            resultsContainer.Add(m_PreviewContainer);

            var previewLabel = new Label("Preview");
            previewLabel.AddToClassList("preview-label");
            m_PreviewContainer.Add(previewLabel);

            m_PreviewImage = new Image();
            m_PreviewImage.AddToClassList("preview-image");
            m_PreviewContainer.Add(m_PreviewImage);

            m_SaveButton = new Button(OnSaveClicked)
            {
                text = "Save Image"
            };
            m_SaveButton.AddToClassList("save-button");
            m_PreviewContainer.Add(m_SaveButton);

            // Initial UI setup
            UpdateUIForModel();

            return m_Root;
        }

        private void UpdateUIForModel()
        {
            // Update size options based on selected model
            if (m_ModelSizeOptions.TryGetValue(m_SelectedModel, out string[] sizes))
            {
                var currentValue = m_SizeField.value;
                m_SizeField.choices = sizes.ToList();
                
                // Try to keep the current size if it's valid for this model
                if (sizes.Contains(currentValue))
                {
                    m_SizeField.value = currentValue;
                }
                else
                {
                    m_SizeField.value = sizes[0];
                    if (m_SizeEnumMap.TryGetValue(sizes[0], out Size size))
                    {
                        m_SelectedSize = size;
                    }
                }
            }

            // Update count slider based on model
            bool isCountEnabled = m_SelectedModel != ImageModel.DallE3;
            m_CountSlider.SetEnabled(isCountEnabled);
            
            if (m_SelectedModel == ImageModel.DallE3)
            {
                m_CountSlider.value = 1;
                m_ImageCount = 1;
            }

            // Show/hide style field based on model
            m_StyleField.parent.style.display = 
                m_SelectedModel == ImageModel.DallE3 ? DisplayStyle.Flex : DisplayStyle.None;

            // Update quality options based on model
            bool isDallE3 = m_SelectedModel == ImageModel.DallE3;
            bool isGPTImage = m_SelectedModel == ImageModel.GPTImage1;
            
            // Update advanced panel fields based on model
            m_BackgroundField.parent.style.display = isGPTImage ? DisplayStyle.Flex : DisplayStyle.None;
            m_OutputFormatField.parent.style.display = isGPTImage ? DisplayStyle.Flex : DisplayStyle.None;
            m_CompressionSlider.parent.style.display = isGPTImage ? DisplayStyle.Flex : DisplayStyle.None;
            m_ModerationField.parent.style.display = isGPTImage ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnGenerateClicked()
        {
            // Validate prompt
            if (string.IsNullOrWhiteSpace(m_Prompt))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a prompt.", "OK");
                return;
            }

            // Clear previous results
            m_GeneratedImages.Clear();
            m_SelectedImage = null;
            m_ResultsScrollView.Clear();
            m_PreviewImage.image = null;
            m_SaveButton.SetEnabled(false);

            // Show status
            m_StatusLabel.text = "Generating images...";
            m_GenerateButton.SetEnabled(false);

            // Create request
            var request = new ImageGenerationRequest
            {
                Prompt = m_Prompt,
                Model = m_SelectedModel,
                Count = m_ImageCount,
                Size = m_SelectedSize,
                Quality = m_SelectedQuality,
                Style = m_SelectedStyle
            };

            // Add advanced settings if enabled
            if (m_AdvancedToggle.value && m_SelectedModel == ImageModel.GPTImage1)
            {
                request.Background = (BackgroundType)m_BackgroundField.value;
                request.OutputFormat = (OutputFormat)m_OutputFormatField.value;
                request.OutputCompression = m_CompressionSlider.value;
                request.Moderation = (Moderation)m_ModerationField.value;
            }

            // Register callback
            DALLEClient.Instance.OnResponseReceived = null;
            DALLEClient.Instance.OnResponseReceived += OnImagesReceived;

            // Send request
            DALLEClient.Instance.GenerateImage(request);
        }

        private void OnImagesReceived(List<DALLEImageResult> images)
        {
            
            m_Root.Add(resultsContainer);
            ImageCreatorWindow.window.minSize = new Vector2(700, 900);

            m_GeneratedImages = images;
            m_StatusLabel.text = $"Generated {images.Count} images.";
            m_GenerateButton.SetEnabled(true);

            // Clear thumbnails
            m_ResultsScrollView.Clear();
            
            // Show thumbnails
            foreach (var image in images)
            {
                var thumbnail = CreateThumbnail(image);
                m_ResultsScrollView.Add(thumbnail);
            }

            // Select first image if available
            if (images.Count > 0)
            {
                SelectImage(images[0]);
            } 
        }

        private VisualElement CreateThumbnail(DALLEImageResult image)
        {
            var thumbnail = new VisualElement();
            thumbnail.AddToClassList("thumbnail");

            var thumbnailImage = new Image
            {
                image = image.Texture,
                scaleMode = UnityEngine.ScaleMode.ScaleToFit
            };
            thumbnailImage.AddToClassList("thumbnail-image");
            thumbnail.Add(thumbnailImage);

            thumbnail.RegisterCallback<ClickEvent>(e => SelectImage(image));

            return thumbnail;
        }

        private void SelectImage(DALLEImageResult image)
        {
            m_SelectedImage = image;
            m_PreviewImage.image = image.Texture;
            m_SaveButton.SetEnabled(true);
            
            // Show revised prompt if available
            if (!string.IsNullOrEmpty(image.RevisedPrompt))
            {
                m_StatusLabel.text = $"Revised prompt: {image.RevisedPrompt}";
            }
        }

        private void OnSaveClicked()
        {
            if (m_SelectedImage == null || m_SelectedImage.Texture == null)
                return;

            string defaultName = "GeneratedImage.png";
            string path = EditorUtility.SaveFilePanel("Save Image", "", defaultName, "png");
            
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = m_SelectedImage.Texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                
                // Notify asset database if the path is within the project
                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.ImportAsset(relativePath);
                    
                    // Select the asset in the Project view
                    var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                    if (asset != null)
                    {
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = asset;
                    }
                }
            }
        }

        public override void SaveSettings()
        {
            EditorPrefs.SetString($"UAI_ImageCreator_Prompt", m_Prompt);
            EditorPrefs.SetInt($"UAI_ImageCreator_Model", (int)m_SelectedModel);
            EditorPrefs.SetInt($"UAI_ImageCreator_Count", m_ImageCount);
            EditorPrefs.SetInt($"UAI_ImageCreator_Size", (int)m_SelectedSize);
            EditorPrefs.SetInt($"UAI_ImageCreator_Quality", (int)m_SelectedQuality);
            EditorPrefs.SetInt($"UAI_ImageCreator_Style", (int)m_SelectedStyle);
        }

        public override void LoadSettings()
        {
            m_Prompt = EditorPrefs.GetString($"UAI_ImageCreator_Prompt", "A futuristic city with flying cars.");
            m_SelectedModel = (ImageModel)EditorPrefs.GetInt($"UAI_ImageCreator_Model", (int)ImageModel.DallE3);
            m_ImageCount = EditorPrefs.GetInt($"UAI_ImageCreator_Count", 1);
            m_SelectedSize = (Size)EditorPrefs.GetInt($"UAI_ImageCreator_Size", (int)Size.Size1024x1024);
            m_SelectedQuality = (Quality)EditorPrefs.GetInt($"UAI_ImageCreator_Quality", (int)Quality.high);
            m_SelectedStyle = (Style)EditorPrefs.GetInt($"UAI_ImageCreator_Style", (int)Style.Vivid);
        }
    }

    #endregion

    #region Image Seamless Page

    // Page for creating seamless textures
    public class ImageSeamlessPage : TabPageBase
    {
        // UI Elements
        private ObjectField m_ImageField;
        private TextField m_PromptField;
        private SliderInt m_SeamThicknessSlider;
        private Image m_PreviewImage;
        private Button m_GenerateButton;
        private Label m_StatusLabel;
        private ScrollView m_ResultsScrollView;
        private VisualElement m_PreviewContainer;
        private Image m_FullPreviewImage;
        private Button m_SaveButton;

        // State
        private Texture2D m_SourceTexture;
        private Texture2D m_BorderedTexture;
        private string m_Prompt = "A seamless texture pattern.";
        private int m_SeamThickness = 10;
        private List<DALLEImageResult> m_GeneratedImages = new List<DALLEImageResult>();
        private DALLEImageResult m_SelectedImage;

        
        private VisualElement resultsContainer;

        public ImageSeamlessPage(ImageCreatorWindow window) : base(window)
        {
        }

        public override VisualElement CreateUI()
        {
            m_Root = new VisualElement();
            m_Root.AddToClassList("tab-content");

            // Header
            var header = new Label("Seamless Texture Creator");
            header.AddToClassList("header-label");
            m_Root.Add(header);

            // Description
            var description = new Label("Create seamless textures by editing the borders of an existing texture");
            description.AddToClassList("description-label");
            m_Root.Add(description);

            // Settings container
            var settingsContainer = new VisualElement();
            settingsContainer.AddToClassList("settings-container");
            m_Root.Add(settingsContainer);

            // Left column (settings)
            var settingsColumn = new VisualElement();
            settingsColumn.AddToClassList("settings-column");
            settingsContainer.Add(settingsColumn);

            // Image field
            var imageFieldContainer = new VisualElement();
            imageFieldContainer.AddToClassList("setting-row");
            settingsColumn.Add(imageFieldContainer);

            var imageLabel = new Label("Source Texture:");
            imageFieldContainer.Add(imageLabel);

            m_ImageField = new ObjectField
            {
                objectType = typeof(Texture2D),
                allowSceneObjects = false
            };
            m_ImageField.RegisterValueChangedCallback(e =>
            {
                m_SourceTexture = e.newValue as Texture2D;
                UpdatePreview();
            });
            imageFieldContainer.Add(m_ImageField);

            // Prompt field
            var promptLabel = new Label("Describe the seamless texture:");
            promptLabel.AddToClassList("setting-label");
            settingsColumn.Add(promptLabel);

            m_PromptField = new TextField
            {
                multiline = true,
                value = m_Prompt
            };
            m_PromptField.AddToClassList("prompt-field");
            m_PromptField.RegisterValueChangedCallback(e => m_Prompt = e.newValue);
            m_PromptField.Q("unity-text-input").style.alignSelf = Align.Auto;
            settingsColumn.Add(m_PromptField);

            // Seam thickness slider
            var thicknessContainer = new VisualElement();
            thicknessContainer.AddToClassList("setting-row");
            settingsColumn.Add(thicknessContainer);

            var thicknessLabel = new Label("Seam Thickness:");
            thicknessContainer.Add(thicknessLabel);

            m_SeamThicknessSlider = new SliderInt(1, 100)
            {
                value = m_SeamThickness,
                showInputField = true
            };
            m_SeamThicknessSlider.RegisterValueChangedCallback(e =>
            {
                m_SeamThickness = e.newValue;
                UpdatePreview();
            });
            thicknessContainer.Add(m_SeamThicknessSlider);

            // Right column (preview)
            var previewColumn = new VisualElement();
            previewColumn.AddToClassList("preview-column");
            settingsContainer.Add(previewColumn);

            var previewLabel = new Label("Border Preview");
            previewLabel.AddToClassList("preview-label");
            previewColumn.Add(previewLabel);

            m_PreviewImage = new Image();
            m_PreviewImage.AddToClassList("border-preview");
            previewColumn.Add(m_PreviewImage);

            // Generate button
            m_GenerateButton = new Button(OnGenerateClicked)
            {
                text = "Generate Seamless Texture"
            };
            m_GenerateButton.AddToClassList("generate-button");
            m_Root.Add(m_GenerateButton);

            // Status label
            m_StatusLabel = new Label();
            m_StatusLabel.AddToClassList("status-label");
            m_Root.Add(m_StatusLabel);

            // Results container
            resultsContainer = new VisualElement();
            resultsContainer.AddToClassList("results-container");
            if (m_GeneratedImages.Count > 0){
                m_Window.minSize = new Vector2(700, 900);
                m_Root.Add(resultsContainer); 
            }

            // Results scrollview
            m_ResultsScrollView = new ScrollView(ScrollViewMode.Vertical);
            m_ResultsScrollView.AddToClassList("thumbnails-scroll");
            resultsContainer.Add(m_ResultsScrollView);

            // Full preview container
            m_PreviewContainer = new VisualElement();
            m_PreviewContainer.AddToClassList("preview-container");
            resultsContainer.Add(m_PreviewContainer);

            var fullPreviewLabel = new Label("Preview");
            fullPreviewLabel.AddToClassList("preview-label");
            m_PreviewContainer.Add(fullPreviewLabel);

            m_FullPreviewImage = new Image();
            m_FullPreviewImage.AddToClassList("preview-image");
            m_PreviewContainer.Add(m_FullPreviewImage);

            m_SaveButton = new Button(OnSaveClicked)
            {
                text = "Save Seamless Texture"
            };
            m_SaveButton.AddToClassList("save-button");
            m_PreviewContainer.Add(m_SaveButton);

            return m_Root;
        }

        private void UpdatePreview()
        {
            if (m_SourceTexture == null)
            {
                m_PreviewImage.image = null;
                return;
            }

            // Create bordered preview
            m_BorderedTexture = ApplyBorder(m_SourceTexture, m_SeamThickness);
            m_PreviewImage.image = m_BorderedTexture;
        }

        private Texture2D ApplyBorder(Texture2D original, int seamThicknessInPixels)
        {
            int width = original.width;
            int height = original.height;
            
            // Create a readable copy of the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(original, tmp);
            
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            
            Texture2D readableTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readableTexture.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            // Create bordered texture
            Texture2D borderedTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // If the pixel is within the border, set it to white
                    if (x < seamThicknessInPixels || 
                        y < seamThicknessInPixels || 
                        x >= width - seamThicknessInPixels || 
                        y >= height - seamThicknessInPixels)
                    {
                        borderedTexture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        borderedTexture.SetPixel(x, y, readableTexture.GetPixel(x, y));
                    }
                }
            }
            
            borderedTexture.Apply();
            return borderedTexture;
        }

        private Texture2D CreateMask(int width, int height, int seamThicknessInPixel)
        {
            Texture2D mask = new Texture2D(width, height, TextureFormat.ARGB32, false);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color color = Color.black;
                    if (i < seamThicknessInPixel || 
                        j < seamThicknessInPixel || 
                        i > width - seamThicknessInPixel || 
                        j > height - seamThicknessInPixel)
                    {
                        color.a = 0;
                    }
                    else
                    {
                        color.a = 1;
                    }
                    mask.SetPixel(i, j, color);
                }
            }

            mask.Apply();
            return mask;
        }

        private Texture2D ShiftTexture(Texture2D original)
        {
            int width = original.width;
            int height = original.height;
            
            // Create a readable copy of the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(original, tmp);
            
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            
            Texture2D readableTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readableTexture.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
            
            // Create shifted texture
            Texture2D shiftedTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int shifted_i = (i + width / 2) % width;
                    int shifted_j = (j + height / 2) % height;
                    shiftedTexture.SetPixel(i, j, readableTexture.GetPixel(shifted_i, shifted_j));
                }
            }
            
            shiftedTexture.Apply();
            return shiftedTexture;
        }

        private void OnGenerateClicked()
        {
            // Validate inputs
            if (m_SourceTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a source texture.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(m_Prompt))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a prompt for the seamless texture.", "OK");
                return;
            }

            // Clear previous results
            m_GeneratedImages.Clear();
            m_SelectedImage = null;
            m_ResultsScrollView.Clear();
            m_FullPreviewImage.image = null;
            m_SaveButton.SetEnabled(false);

            // Show status
            m_StatusLabel.text = "Generating seamless texture...";
            m_GenerateButton.SetEnabled(false);

            // Prepare the image and mask
            Texture2D shiftedTexture = ShiftTexture(m_SourceTexture);
            Texture2D mask = CreateMask(m_SourceTexture.width, m_SourceTexture.height, m_SeamThickness);
            Texture2D shiftedMask = ShiftTexture(mask);

            // Create request
            var request = new ImageEditRequest
            {
                Prompt = m_Prompt,
                Model = ImageModel.DallE2, // Only DALL-E 2 supports image edits
                Count = 1,
                Image = shiftedTexture,
                Mask = shiftedMask
            };

            // Set size based on source texture
            if (m_SourceTexture.width == 256)
                request.Size = Size.Size256x256;
            else if (m_SourceTexture.width == 512)
                request.Size = Size.Size512x512;
            else
                request.Size = Size.Size1024x1024;

            // Register callback
            DALLEClient.Instance.OnResponseReceived = null;
            DALLEClient.Instance.OnResponseReceived += OnImagesReceived;

            // Send request
            DALLEClient.Instance.EditImage(request);
        }

        private void OnImagesReceived(List<DALLEImageResult> images)
        {
            m_Root.Add(resultsContainer);
            ImageCreatorWindow.window.minSize = new Vector2(700, 900);
            
            m_GeneratedImages = new List<DALLEImageResult>();
            m_StatusLabel.text = $"Generated {images.Count} seamless textures.";
            m_GenerateButton.SetEnabled(true);

            // Clear thumbnails
            m_ResultsScrollView.Clear();
            
            // Process and show results
            foreach (var image in images)
            {
                // Shift the image back to make it seamless
                var processedImage = new DALLEImageResult
                {
                    Texture = ShiftTexture(image.Texture),
                    Url = image.Url,
                    RevisedPrompt = image.RevisedPrompt
                };
                
                m_GeneratedImages.Add(processedImage);
                
                var thumbnail = CreateThumbnail(processedImage);
                m_ResultsScrollView.Add(thumbnail);
            }

            // Select first image if available
            if (m_GeneratedImages.Count > 0)
            {
                SelectImage(m_GeneratedImages[0]);
            } 
        }

        private VisualElement CreateThumbnail(DALLEImageResult image)
        {
            var thumbnail = new VisualElement();
            thumbnail.AddToClassList("thumbnail");

            var thumbnailImage = new Image
            {
                image = image.Texture,
                scaleMode = UnityEngine.ScaleMode.ScaleToFit
            };
            thumbnailImage.AddToClassList("thumbnail-image");
            thumbnail.Add(thumbnailImage);

            thumbnail.RegisterCallback<ClickEvent>(e => SelectImage(image));

            return thumbnail;
        }

        private void SelectImage(DALLEImageResult image)
        {
            m_SelectedImage = image;
            m_FullPreviewImage.image = image.Texture;
            m_SaveButton.SetEnabled(true);
        }

        private void OnSaveClicked()
        {
            if (m_SelectedImage == null || m_SelectedImage.Texture == null)
                return;

            string defaultName = "SeamlessTexture.png";
            string path = EditorUtility.SaveFilePanel("Save Seamless Texture", "", defaultName, "png");
            
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = m_SelectedImage.Texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                
                // Notify asset database if the path is within the project
                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.ImportAsset(relativePath);
                    
                    // Select the asset in the Project view
                    var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                    if (asset != null)
                    {
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = asset;
                    }
                }
            }
        }

        public override void SaveSettings()
        {
            EditorPrefs.SetString($"UAI_ImageCreator_SeamlessPrompt", m_Prompt);
            EditorPrefs.SetInt($"UAI_ImageCreator_SeamThickness", m_SeamThickness);
        }

        public override void LoadSettings()
        {
            m_Prompt = EditorPrefs.GetString($"UAI_ImageCreator_SeamlessPrompt", "A seamless texture pattern.");
            m_SeamThickness = EditorPrefs.GetInt($"UAI_ImageCreator_SeamThickness", 10);
        }
    }

    #endregion

    #region Image Variation Page
 
    public class ImageVariationPage : TabPageBase
    {
        // UI Elements
        private ObjectField m_SourceImageField;
        private PopupField<string> m_SizeField;
        private SliderInt m_CountSlider;
        private Button m_GenerateButton;
        private Label m_StatusLabel;
        private ScrollView m_ResultsScrollView;
        private VisualElement m_PreviewContainer;
        private Image m_PreviewImage;
        private Button m_SaveButton;

        // State
        private Texture2D m_SourceTexture;
        private List<DALLEImageResult> m_GeneratedImages = new List<DALLEImageResult>();
        private DALLEImageResult m_SelectedImage;
        private int m_VariationCount = 4;
        private Size m_SelectedSize = Size.Size1024x1024;
        private VisualElement resultsContainer;

        // Size options for DALL-E 2
        private readonly string[] m_SizeOptions = new[] { "256x256", "512x512", "1024x1024" };
        private readonly Dictionary<string, Size> m_SizeMap = new Dictionary<string, Size>
        {
            { "256x256", Size.Size256x256 },
            { "512x512", Size.Size512x512 },
            { "1024x1024", Size.Size1024x1024 }
        };

        public ImageVariationPage(ImageCreatorWindow window) : base(window)
        {
        }

        public override VisualElement CreateUI()
        {
            m_Root = new VisualElement();
            m_Root.AddToClassList("tab-content");

            // Header
            var header = new Label("Image Variations");
            header.AddToClassList("header-label");
            m_Root.Add(header);

            // Description
            var description = new Label("Create variations of an existing image");
            description.AddToClassList("description-label");
            m_Root.Add(description);

            // Settings panel
            var settingsPanel = new VisualElement();
            settingsPanel.AddToClassList("settings-panel");
            m_Root.Add(settingsPanel);

            // Image field
            var imageContainer = new VisualElement();
            imageContainer.AddToClassList("setting-row");
            settingsPanel.Add(imageContainer);

            var imageLabel = new Label("Source Image:");
            imageContainer.Add(imageLabel);

            m_SourceImageField = new ObjectField
            {
                objectType = typeof(Texture2D),
                allowSceneObjects = false
            };
            m_SourceImageField.RegisterValueChangedCallback(e => m_SourceTexture = e.newValue as Texture2D);
            imageContainer.Add(m_SourceImageField);

            // Size selection
            var sizeContainer = new VisualElement();
            sizeContainer.AddToClassList("setting-row");
            settingsPanel.Add(sizeContainer);

            var sizeLabel = new Label("Size:");
            sizeContainer.Add(sizeLabel);

            m_SizeField = new PopupField<string>(
                m_SizeOptions.ToList(),
                2, // Default to 1024x1024
                formatSelectedValueCallback: (s) => s,
                formatListItemCallback: (s) => s
            );
            m_SizeField.RegisterValueChangedCallback(e =>
            {
                if (m_SizeMap.TryGetValue(e.newValue, out Size size))
                {
                    m_SelectedSize = size;
                }
            });
            sizeContainer.Add(m_SizeField);

            // Count slider
            var countContainer = new VisualElement();
            countContainer.AddToClassList("setting-row");
            settingsPanel.Add(countContainer);

            var countLabel = new Label("Number of Variations:");
            countContainer.Add(countLabel);

            m_CountSlider = new SliderInt(1, 10)
            {
                value = m_VariationCount,
                showInputField = true
            };
            m_CountSlider.RegisterValueChangedCallback(e => m_VariationCount = e.newValue);
            countContainer.Add(m_CountSlider);

            // Note about DALL-E 2
            var noteLabel = new Label("Note: Image variations are only available with DALL-E 2");
            noteLabel.AddToClassList("note-label");
            settingsPanel.Add(noteLabel);

            // Generate button
            m_GenerateButton = new Button(OnGenerateClicked)
            {
                text = "Generate Variations"
            };
            m_GenerateButton.AddToClassList("generate-button");
            m_Root.Add(m_GenerateButton);

            // Status label
            m_StatusLabel = new Label();
            m_StatusLabel.AddToClassList("status-label");
            m_Root.Add(m_StatusLabel);

            // Results container
            resultsContainer = new VisualElement();
            resultsContainer.AddToClassList("results-container");
            
            if (m_GeneratedImages.Count > 0){
                m_Window.minSize = new Vector2(700, 900);
                m_Root.Add(resultsContainer); 
            }

            // Thumbnails scrollview
            m_ResultsScrollView = new ScrollView(ScrollViewMode.Vertical);
            m_ResultsScrollView.AddToClassList("thumbnails-scroll");
            resultsContainer.Add(m_ResultsScrollView);

            // Preview container
            m_PreviewContainer = new VisualElement();
            m_PreviewContainer.AddToClassList("preview-container");
            resultsContainer.Add(m_PreviewContainer);

            var previewLabel = new Label("Preview");
            previewLabel.AddToClassList("preview-label");
            m_PreviewContainer.Add(previewLabel);

            m_PreviewImage = new Image();
            m_PreviewImage.AddToClassList("preview-image");
            m_PreviewContainer.Add(m_PreviewImage);

            m_SaveButton = new Button(OnSaveClicked)
            {
                text = "Save Variation"
            };
            m_SaveButton.AddToClassList("save-button");
            m_PreviewContainer.Add(m_SaveButton);

            return m_Root;
        }

        private void OnGenerateClicked()
        {
            // Validate inputs
            if (m_SourceTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a source image.", "OK");
                return;
            }

            // Check if texture is square (required by DALL-E 2)
            if (m_SourceTexture.width != m_SourceTexture.height)
            {
                EditorUtility.DisplayDialog("Error", "The source image must be square (equal width and height).", "OK");
                return;
            }
            
            // Check if the texture is readable
            try
            {
                m_SourceTexture.GetPixel(0, 0);
            }
            catch
            {
                // Texture is not readable
                if (!EditorUtility.DisplayDialog("Texture Not Readable", 
                    "The source texture is not readable. Would you like to make it readable now?\n\n" +
                    "This will modify the texture import settings.",
                    "Yes, make it readable", "Cancel"))
                {
                    return;
                }
                
                // Try to make the texture readable
                MakeTextureReadable(m_SourceTexture);
                
                // Re-check if it's readable now
                try
                {
                    m_SourceTexture.GetPixel(0, 0);
                }
                catch
                {
                    EditorUtility.DisplayDialog("Error", 
                        "Failed to make the texture readable. Please check the texture import settings manually.", "OK");
                    return;
                }
            }

            // Clear previous results
            m_GeneratedImages.Clear();
            m_SelectedImage = null;
            m_ResultsScrollView.Clear();
            m_PreviewImage.image = null;
            m_SaveButton.SetEnabled(false);

            // Show status
            m_StatusLabel.text = "Generating variations...";
            m_GenerateButton.SetEnabled(false);

            // Create request
            var request = new ImageVariationRequest
            {
                Image = m_SourceTexture,
                Count = m_VariationCount,
                Size = m_SelectedSize
            };

            // Register callback
            DALLEClient.Instance.OnResponseReceived = null;
            DALLEClient.Instance.OnResponseReceived += OnImagesReceived;

            // Send request
            DALLEClient.Instance.CreateVariation(request);
        }

        // Add this helper method to make texture readable
        private void MakeTextureReadable(Texture2D texture)
        {
            // Get the asset path
            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Could not find asset path for texture");
                return;
            }
            
            // Get the importer
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("Could not get texture importer");
                return;
            }
            
            // Make it readable
            importer.isReadable = true;
            
            // Apply changes
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }


        private void OnImagesReceived(List<DALLEImageResult> images)
        {
            m_Root.Add(resultsContainer);
            ImageCreatorWindow.window.minSize = new Vector2(700, 900);
            
            m_GeneratedImages = images;
            m_StatusLabel.text = $"Generated {images.Count} variations.";
            m_GenerateButton.SetEnabled(true);

            // Clear thumbnails
            m_ResultsScrollView.Clear();
            
            // Show thumbnails
            foreach (var image in images)
            {
                var thumbnail = CreateThumbnail(image);
                m_ResultsScrollView.Add(thumbnail);
            }

            // Select first image if available
            if (images.Count > 0)
            {
                SelectImage(images[0]);
            } 
        }

        private VisualElement CreateThumbnail(DALLEImageResult image)
        {
            var thumbnail = new VisualElement();
            thumbnail.AddToClassList("thumbnail");

            var thumbnailImage = new Image
            {
                image = image.Texture,
                scaleMode = UnityEngine.ScaleMode.ScaleToFit 
            };
            thumbnailImage.AddToClassList("thumbnail-image");
            thumbnail.Add(thumbnailImage);

            thumbnail.RegisterCallback<ClickEvent>(e => SelectImage(image));

            return thumbnail;
        }

        private void SelectImage(DALLEImageResult image)
        {
            m_SelectedImage = image;
            m_PreviewImage.image = image.Texture;
            m_SaveButton.SetEnabled(true);
        }

        private void OnSaveClicked()
        {
            if (m_SelectedImage == null || m_SelectedImage.Texture == null)
                return;

            string defaultName = "ImageVariation.png";
            string path = EditorUtility.SaveFilePanel("Save Image Variation", "", defaultName, "png");
            
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = m_SelectedImage.Texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                
                // Notify asset database if the path is within the project
                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.ImportAsset(relativePath);
                    
                    // Select the asset in the Project view
                    var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                    if (asset != null)
                    {
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = asset;
                    }
                }
            }
        }

        public override void SaveSettings()
        {
            EditorPrefs.SetInt($"UAI_ImageCreator_VariationCount", m_VariationCount);
            EditorPrefs.SetInt($"UAI_ImageCreator_VariationSize", (int)m_SelectedSize);
        }

        public override void LoadSettings()
        {
            m_VariationCount = EditorPrefs.GetInt($"UAI_ImageCreator_VariationCount", 4);
            m_SelectedSize = (Size)EditorPrefs.GetInt($"UAI_ImageCreator_VariationSize", (int)Size.Size1024x1024);
        }
    }

    #endregion
}
