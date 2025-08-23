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
    public class ImageMaskDrawingPage : TabPageBase
    {
        // UI Elements
        private ObjectField m_SourceImageField;
        private TextField m_PromptField;
        private SliderInt m_BrushSizeSlider;
        private Button m_ClearMaskButton;
        private Button m_InvertMaskButton;
        private Toggle m_ShowMaskToggle;
        private PopupField<string> m_SizeField;
        private SliderInt m_CountSlider;
        private EnumField m_ModelField;
        private EnumField m_QualityField;
        private Button m_GenerateButton;
        private Label m_StatusLabel;
        private ScrollView m_ResultsScrollView;
        private VisualElement m_PreviewContainer;
        private Image m_PreviewImage;
        private Button m_SaveButton;

        // Drawing canvas elements
        private VisualElement m_CanvasContainer;
        private Image m_CanvasSourceImage;
        private Image m_CanvasMaskPreview;
        private VisualElement m_DrawingOverlay;
        private VisualElement resultsContainer;

        // Drawing state variables
        private Texture2D m_SourceTexture;
        private Texture2D m_MaskTexture;
        private string m_Prompt = "Edit this image.";
        private int m_BrushSize = 10;
        private bool m_ShowMask = true;
        private bool m_IsDrawing = false;
        private Vector2 m_LastDrawPosition;
        private ImageModel m_SelectedModel = ImageModel.GPTImage1;
        private int m_ImageCount = 1;
        private Size m_SelectedSize = Size.Size1024x1024;
        private Quality m_SelectedQuality = Quality.high;
        private List<DALLEImageResult> m_GeneratedImages = new List<DALLEImageResult>();
        private DALLEImageResult m_SelectedImage;
        private bool m_IsMaskDirty = false;
        
        // Improved drawing variables
        private float m_LastApplyTime = 0f;
        private List<Vector2Int> m_PendingPixels = new List<Vector2Int>();
        private Color m_BrushColor = new Color(1, 1, 1, 1); 
        private float m_RefreshRate = 0.016f; 
        private bool m_UseHighQualityPreview = true; 
        private VisualElement m_BrushPreview; 

        // Size options
        private readonly string[] m_GPTImageSizeOptions = new[] { "1024x1024", "1536x1024", "1024x1536" };
        private readonly string[] m_DallE2SizeOptions = new[] { "256x256", "512x512", "1024x1024" };
        private readonly Dictionary<string, Size> m_SizeMap = new Dictionary<string, Size>
        {
            { "256x256", Size.Size256x256 },
            { "512x512", Size.Size512x512 },
            { "1024x1024", Size.Size1024x1024 },
            { "1536x1024", Size.Size1536x1024 },
            { "1024x1536", Size.Size1024x1536 }
        };

        public ImageMaskDrawingPage(ImageCreatorWindow window) : base(window)
        {
        }

        public override VisualElement CreateUI()
        {
            m_Root = new VisualElement();
            m_Root.AddToClassList("tab-content");

            // Header
            var header = new Label("Mask Drawing Tool");
            header.AddToClassList("header-label");
            m_Root.Add(header);

            // Description
            var description = new Label("Edit images by drawing a mask to specify areas to modify");
            description.AddToClassList("description-label");
            m_Root.Add(description);

            // Main container (two columns)
            var mainContainer = new VisualElement();
            mainContainer.style.flexDirection = FlexDirection.Row;
            mainContainer.style.minHeight = 400;
            m_Root.Add(mainContainer);

            // Left column (settings & canvas)
            var leftColumn = new VisualElement();
            leftColumn.style.width = Length.Percent(60);
            leftColumn.style.marginRight = 10;
            mainContainer.Add(leftColumn);

            // Settings panel
            var settingsPanel = new VisualElement();
            settingsPanel.AddToClassList("settings-panel");
            leftColumn.Add(settingsPanel);

            // Image field
            var imageFieldContainer = new VisualElement();
            imageFieldContainer.AddToClassList("setting-row");
            imageFieldContainer.style.minHeight = 20;
            imageFieldContainer.style.marginBottom = 10;
            settingsPanel.Add(imageFieldContainer);

            var imageLabel = new Label("Source Image:");
            imageLabel.style.minWidth = 100;
            imageFieldContainer.Add(imageLabel);

            m_SourceImageField = new ObjectField
            {
                objectType = typeof(Texture2D),
                allowSceneObjects = false
            };
            m_SourceImageField.style.flexGrow = 1;
            m_SourceImageField.RegisterValueChangedCallback(e =>
            {
                Texture2D newTexture = e.newValue as Texture2D;
                if (newTexture != null)
                {
                    // Always accept the texture, but check if it's readable
                    bool isReadable = IsTextureReadable(newTexture);
                    if (!isReadable)
                    {
                        m_StatusLabel.text = "Warning: Texture is not readable. Make it readable in Import Settings (select texture in Project view → Inspector → check 'Read/Write Enabled')";
                    }
                    
                    m_SourceTexture = newTexture;
                    m_IsMaskDirty = false;
                    UpdateCanvas();
                }
                else
                {
                    m_SourceTexture = null;
                    m_StatusLabel.text = "";
                    UpdateCanvas();
                }
            });
            imageFieldContainer.Add(m_SourceImageField);

            // Add performance settings row
            var performanceContainer = new VisualElement();
            performanceContainer.AddToClassList("setting-row");
            performanceContainer.style.marginBottom = 10;
            settingsPanel.Add(performanceContainer);

            var performanceLabel = new Label("Drawing Performance:");
            performanceLabel.style.minWidth = 100;
            performanceContainer.Add(performanceLabel);

            // Add a dropdown for refresh rate options
            var refreshRateChoices = new List<string> { "Fastest (120fps)", "Smoother (60fps)", "Balanced (30fps)", "Better Quality (20fps)" };
            var refreshRateField = new PopupField<string>(refreshRateChoices, 0);
            refreshRateField.RegisterValueChangedCallback(e => {
                switch (e.newValue) {
                    case "Fastest (120fps)":
                        m_RefreshRate = 0.008f;
                        m_UseHighQualityPreview = false;
                        break;
                    case "Smoother (60fps)":
                        m_RefreshRate = 0.016f;
                        m_UseHighQualityPreview = true;
                        break;
                    case "Balanced (30fps)":
                        m_RefreshRate = 0.033f;
                        m_UseHighQualityPreview = true;
                        break;
                    case "Better Quality (20fps)":
                        m_RefreshRate = 0.05f;
                        m_UseHighQualityPreview = true;
                        break;
                }
            });
            refreshRateField.style.flexGrow = 1;
            performanceContainer.Add(refreshRateField);

            // Canvas container
            m_CanvasContainer = new VisualElement();
            m_CanvasContainer.AddToClassList("canvas-container");
            m_CanvasContainer.style.marginTop = 10;
            m_CanvasContainer.style.marginBottom = 10;
            m_CanvasContainer.style.height = 300;
            m_CanvasContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            m_CanvasContainer.style.position = Position.Relative;
            leftColumn.Add(m_CanvasContainer);

            // Source image display
            m_CanvasSourceImage = new Image();
            m_CanvasSourceImage.style.position = Position.Absolute;
            m_CanvasSourceImage.style.width = Length.Percent(100);
            m_CanvasSourceImage.style.height = Length.Percent(100);
            m_CanvasSourceImage.scaleMode = UnityEngine.ScaleMode.ScaleToFit;
            m_CanvasContainer.Add(m_CanvasSourceImage);

            // Mask preview overlay
            m_CanvasMaskPreview = new Image();
            m_CanvasMaskPreview.style.position = Position.Absolute;
            m_CanvasMaskPreview.style.width = Length.Percent(100);
            m_CanvasMaskPreview.style.height = Length.Percent(100);
            m_CanvasMaskPreview.scaleMode = UnityEngine.ScaleMode.ScaleToFit;
            m_CanvasContainer.Add(m_CanvasMaskPreview);

            // Brush preview element
            m_BrushPreview = new VisualElement();
            m_BrushPreview.style.position = Position.Absolute;
            m_BrushPreview.style.backgroundColor = new Color(1f, 1f, 1f, 0.5f); 
            m_BrushPreview.style.borderTopLeftRadius = 50; 
            m_BrushPreview.style.borderTopRightRadius = 50;
            m_BrushPreview.style.borderBottomLeftRadius = 50;
            m_BrushPreview.style.borderBottomRightRadius = 50;
            m_BrushPreview.style.visibility = Visibility.Hidden; 
            m_CanvasContainer.Add(m_BrushPreview);

            // Drawing overlay
            m_DrawingOverlay = new VisualElement();
            m_DrawingOverlay.style.position = Position.Absolute;
            m_DrawingOverlay.style.width = Length.Percent(100);
            m_DrawingOverlay.style.height = Length.Percent(100);
            m_CanvasContainer.Add(m_DrawingOverlay);

            // Register drawing events
            m_DrawingOverlay.RegisterCallback<MouseDownEvent>(OnDrawingMouseDown);
            m_DrawingOverlay.RegisterCallback<MouseUpEvent>(OnDrawingMouseUp);
            m_DrawingOverlay.RegisterCallback<MouseMoveEvent>(OnDrawingMouseMove);
            m_DrawingOverlay.RegisterCallback<MouseLeaveEvent>(OnDrawingMouseLeave);
            m_DrawingOverlay.RegisterCallback<MouseEnterEvent>(OnDrawingMouseEnter);

            // Canvas tools
            var canvasToolsPanel = new VisualElement();
            canvasToolsPanel.style.flexDirection = FlexDirection.Row;
            canvasToolsPanel.style.justifyContent = Justify.SpaceBetween;
            canvasToolsPanel.style.marginTop = 5;
            leftColumn.Add(canvasToolsPanel);

            // Brush size slider
            var brushSizeContainer = new VisualElement();
            brushSizeContainer.style.flexDirection = FlexDirection.Row;
            brushSizeContainer.style.alignItems = Align.Center;
            brushSizeContainer.style.width = Length.Percent(50);
            canvasToolsPanel.Add(brushSizeContainer);

            var brushSizeLabel = new Label("Brush Size:");
            brushSizeLabel.style.marginRight = 5;
            brushSizeContainer.Add(brushSizeLabel);

            m_BrushSizeSlider = new SliderInt(1, 50)
            {
                value = m_BrushSize,
                showInputField = true
            };
            m_BrushSizeSlider.style.flexGrow = 1;
            m_BrushSizeSlider.RegisterValueChangedCallback(e => {
                m_BrushSize = e.newValue;
                UpdateBrushPreview();
            });
            brushSizeContainer.Add(m_BrushSizeSlider);

            // Mask display toggle
            m_ShowMaskToggle = new Toggle("Show Mask");
            m_ShowMaskToggle.value = m_ShowMask;
            m_ShowMaskToggle.RegisterValueChangedCallback(e =>
            {
                m_ShowMask = e.newValue;
                m_CanvasMaskPreview.style.display = m_ShowMask ? DisplayStyle.Flex : DisplayStyle.None;
            });
            canvasToolsPanel.Add(m_ShowMaskToggle);

            // Mask operation buttons
            var maskButtonsContainer = new VisualElement();
            maskButtonsContainer.style.flexDirection = FlexDirection.Row;
            maskButtonsContainer.style.marginTop = 5;
            leftColumn.Add(maskButtonsContainer);

            m_ClearMaskButton = new Button(ClearMask)
            {
                text = "Clear Mask"
            };
            m_ClearMaskButton.style.marginRight = 5;
            maskButtonsContainer.Add(m_ClearMaskButton);

            m_InvertMaskButton = new Button(InvertMask)
            {
                text = "Invert Mask"
            };
            maskButtonsContainer.Add(m_InvertMaskButton);

            // Right column (prompt & settings)
            var rightColumn = new VisualElement();
            rightColumn.style.width = Length.Percent(40);
            rightColumn.style.marginLeft = 10;
            mainContainer.Add(rightColumn);

            // Prompt field
            var promptLabel = new Label("Describe what to edit:");
            promptLabel.AddToClassList("setting-label");
            rightColumn.Add(promptLabel);

            m_PromptField = new TextField
            {
                multiline = true,
                value = m_Prompt
            };
            m_PromptField.AddToClassList("prompt-field");
            m_PromptField.style.height = 100;
            m_PromptField.RegisterValueChangedCallback(e => m_Prompt = e.newValue);
            rightColumn.Add(m_PromptField);

            // Model selection
            var modelContainer = new VisualElement();
            modelContainer.AddToClassList("setting-row");
            modelContainer.style.marginTop = 10;
            rightColumn.Add(modelContainer);
 
            m_ModelField = new EnumField("Model", m_SelectedModel);
            m_ModelField.style.flexGrow = 1;
            m_ModelField.RegisterValueChangedCallback(e =>
            {
                m_SelectedModel = (ImageModel)e.newValue;
                UpdateUIForModel();
            });
            modelContainer.Add(m_ModelField);

            // Size selection
            var sizeContainer = new VisualElement();
            sizeContainer.AddToClassList("setting-row");
            sizeContainer.style.marginTop = 10;
            rightColumn.Add(sizeContainer);

            var sizeLabel = new Label("Size:");
            sizeLabel.style.minWidth = 100;
            sizeContainer.Add(sizeLabel);

            m_SizeField = new PopupField<string>(
                m_GPTImageSizeOptions.ToList(),
                0,
                formatSelectedValueCallback: (s) => s,
                formatListItemCallback: (s) => s
            );
            m_SizeField.style.flexGrow = 1;
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
            countContainer.style.marginTop = 10;
            rightColumn.Add(countContainer);

            var countLabel = new Label("Number of Images:");
            countLabel.style.minWidth = 100;
            countContainer.Add(countLabel);

            m_CountSlider = new SliderInt(1, 10)
            {
                value = m_ImageCount,
                showInputField = true
            };
            m_CountSlider.style.flexGrow = 1;
            m_CountSlider.RegisterValueChangedCallback(e => m_ImageCount = e.newValue);
            countContainer.Add(m_CountSlider);

            // Quality
            var qualityContainer = new VisualElement();
            qualityContainer.AddToClassList("setting-row");
            qualityContainer.style.marginTop = 10;
            rightColumn.Add(qualityContainer); 

            m_QualityField = new EnumField("Quality", m_SelectedQuality);
            m_QualityField.style.flexGrow = 1;
            m_QualityField.RegisterValueChangedCallback(e => m_SelectedQuality = (Quality)e.newValue);
            qualityContainer.Add(m_QualityField);

            // Generate button
            m_GenerateButton = new Button(OnGenerateClicked)
            {
                text = "Generate Edited Image"
            };
            m_GenerateButton.AddToClassList("generate-button");
            m_GenerateButton.style.marginTop = 20;
            m_Root.Add(m_GenerateButton);

            // Status label
            m_StatusLabel = new Label();
            m_StatusLabel.AddToClassList("status-label");
            m_Root.Add(m_StatusLabel);

            // Results container - initially hidden
            resultsContainer = new VisualElement();
            resultsContainer.AddToClassList("results-container");
            if (m_GeneratedImages.Count > 0)
            { 
                m_Root.Add(resultsContainer);
                ImageCreatorWindow.window.minSize = new Vector2(700, 900);
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
                text = "Save Edited Image"
            };
            m_SaveButton.AddToClassList("save-button");
            m_PreviewContainer.Add(m_SaveButton);

            // Initial UI state
            m_SaveButton.SetEnabled(false);
            UpdateUIForModel();
            
            // Register editor update for continuous drawing updates
            EditorApplication.update += OnEditorUpdate;

            return m_Root;
        }

        private void OnEditorUpdate()
        {
            // Only process when drawing and there are pending pixels
            if (m_IsDrawing && m_PendingPixels.Count > 0)
            {
                float currentTime = (float)EditorApplication.timeSinceStartup;
                if (currentTime - m_LastApplyTime >= m_RefreshRate)
                {
                    ApplyPendingPixels();
                }
            }
        }

        private void ApplyPendingPixels()
        {
            if (m_MaskTexture == null || m_PendingPixels.Count == 0)
                return;

            // Apply all pending pixels
            foreach (var pixelPos in m_PendingPixels)
            {
                if (pixelPos.x >= 0 && pixelPos.x < m_MaskTexture.width &&
                    pixelPos.y >= 0 && pixelPos.y < m_MaskTexture.height)
                {
                    m_MaskTexture.SetPixel(pixelPos.x, pixelPos.y, m_BrushColor);
                }
            }

            // Apply changes to texture
            m_MaskTexture.Apply();
            m_LastApplyTime = (float)EditorApplication.timeSinceStartup;
            
            // Clear the list after applying
            m_PendingPixels.Clear();
            
            // Force UI repaint
            EditorApplication.QueuePlayerLoopUpdate();
            m_CanvasMaskPreview.MarkDirtyRepaint();
        }

        private void UpdateBrushPreview()
        {
            if (m_BrushPreview != null)
            {
                // Scale based on current brush size 
                int pixelSize = m_BrushSize * 2; 
                m_BrushPreview.style.width = pixelSize;
                m_BrushPreview.style.height = pixelSize;
            }
        }

        private void OnDrawingMouseEnter(MouseEnterEvent evt)
        {
            if (m_BrushPreview != null)
            {
                m_BrushPreview.style.visibility = Visibility.Visible;
                UpdateBrushPreview();
            }
        }

        private void UpdateUIForModel()
        {
            // Update for model capabilities
            bool isGPTImage = m_SelectedModel == ImageModel.GPTImage1;
            bool isDallE2 = m_SelectedModel == ImageModel.DallE2;
            
            // Check if model supports image edits
            if (!isGPTImage && !isDallE2)
            {
                // DALL-E 3 doesn't support image edits, so default to GPT-Image-1
                m_ModelField.value = ImageModel.GPTImage1;
                m_SelectedModel = ImageModel.GPTImage1;
                isGPTImage = true;
                
                // Show warning
                m_StatusLabel.text = "DALL-E 3 does not support image edits. Switched to GPT-Image-1.";
            }
            else
            {
                m_StatusLabel.text = "";
            }
            
            // Update size options based on selected model
            if (isGPTImage)
            {
                m_SizeField.choices = new List<string>(m_GPTImageSizeOptions);
                m_SizeField.parent.style.display = DisplayStyle.None; // Hide size field for GPT-Image-1
            }
            else if (isDallE2)
            {
                m_SizeField.choices = new List<string>(m_DallE2SizeOptions);
                m_SizeField.parent.style.display = DisplayStyle.Flex;
            }
            
            // Keep the first size option
            m_SizeField.index = 0;
            if (m_SizeMap.TryGetValue(m_SizeField.value, out Size size))
            {
                m_SelectedSize = size;
            }
        }

        private void UpdateCanvas()
        {
            if (m_SourceTexture == null)
            {
                m_CanvasSourceImage.image = null;
                m_CanvasMaskPreview.image = null;
                m_MaskTexture = null;
                return;
            }

            // Display source image
            m_CanvasSourceImage.image = m_SourceTexture;
            
            // Create a new mask texture if needed
            if (m_MaskTexture == null || 
                m_MaskTexture.width != m_SourceTexture.width || 
                m_MaskTexture.height != m_SourceTexture.height)
            {
                // Create a new readable texture
                m_MaskTexture = new Texture2D(m_SourceTexture.width, m_SourceTexture.height, TextureFormat.RGBA32, false);
                
                // Initialize with black pixels
                Color[] colors = new Color[m_MaskTexture.width * m_MaskTexture.height];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color(0, 0, 0, 0);
                }
                m_MaskTexture.SetPixels(colors);
                m_MaskTexture.Apply();
            }
            
            // Display mask
            m_CanvasMaskPreview.image = m_MaskTexture;
            m_CanvasMaskPreview.style.display = m_ShowMask ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Check if a texture is readable
        private bool IsTextureReadable(Texture2D texture)
        {
            try
            {
                texture.GetPixel(0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
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

        #region Drawing Functionality

        private void OnDrawingMouseDown(MouseDownEvent evt)
        {
            if (m_SourceTexture == null || m_MaskTexture == null) return;
            
            m_IsDrawing = true;
            m_LastDrawPosition = evt.localMousePosition;
            
            // Draw at the initial position
            DrawAtPosition(m_LastDrawPosition);
            m_IsMaskDirty = true;
        }

        private void OnDrawingMouseUp(MouseUpEvent evt)
        {
            if (m_IsDrawing && m_MaskTexture != null)
            {
                // Make sure to apply any remaining pending pixels
                ApplyPendingPixels();
            }
            m_IsDrawing = false;
        }

        private void OnDrawingMouseMove(MouseMoveEvent evt)
        {
            if (m_SourceTexture == null || m_MaskTexture == null) return;
            
            Vector2 currentPosition = evt.localMousePosition;
            
            // Update brush preview position
            if (m_BrushPreview != null)
            {
                m_BrushPreview.style.left = currentPosition.x - m_BrushPreview.layout.width / 2;
                m_BrushPreview.style.top = currentPosition.y - m_BrushPreview.layout.height / 2;
            }
            
            if (!m_IsDrawing) return;
            
            // Use optimized line drawing for better performance
            DrawLineOptimized(m_LastDrawPosition, currentPosition);
            
            m_LastDrawPosition = currentPosition;
        }

        private void OnDrawingMouseLeave(MouseLeaveEvent evt)
        {
            if (m_IsDrawing && m_MaskTexture != null)
            {
                // Apply any pending pixels before leaving
                ApplyPendingPixels();
            }
            
            if (m_BrushPreview != null)
            {
                m_BrushPreview.style.visibility = Visibility.Hidden;
            }
            
            m_IsDrawing = false;
        }

        private void DrawAtPosition(Vector2 position)
        {
            if (m_MaskTexture == null) return;
             
            Vector2 canvasSize = m_CanvasContainer.layout.size;
            Rect textureRect = GetTextureRect(canvasSize, new Vector2(m_MaskTexture.width, m_MaskTexture.height));
             
            if (!textureRect.Contains(position)) return;
             
            int x = Mathf.RoundToInt((position.x - textureRect.x) / textureRect.width * m_MaskTexture.width);
            int y = Mathf.RoundToInt((position.y - textureRect.y) / textureRect.height * m_MaskTexture.height);
             
            y = m_MaskTexture.height - y;
             
            DrawCircle(x, y, m_BrushSize);
             
            if (!m_UseHighQualityPreview)
            { 
                float currentTime = (float)EditorApplication.timeSinceStartup;
                if (currentTime - m_LastApplyTime >= m_RefreshRate * 0.5f)
                {
                    ApplyPendingPixels();
                }
            }
        }
 
        private void DrawLineOptimized(Vector2 start, Vector2 end)
        {
            if (m_MaskTexture == null) return;
             
            Vector2 canvasSize = m_CanvasContainer.layout.size;
            Rect textureRect = GetTextureRect(canvasSize, new Vector2(m_MaskTexture.width, m_MaskTexture.height));
             
            float x1 = (start.x - textureRect.x) / textureRect.width * m_MaskTexture.width;
            float y1 = (start.y - textureRect.y) / textureRect.height * m_MaskTexture.height;
            float x2 = (end.x - textureRect.x) / textureRect.width * m_MaskTexture.width;
            float y2 = (end.y - textureRect.y) / textureRect.height * m_MaskTexture.height;
             
            y1 = m_MaskTexture.height - y1;
            y2 = m_MaskTexture.height - y2;
             
            float distance = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
             
            if (distance < 1)
            {
                DrawCircle(Mathf.RoundToInt(x1), Mathf.RoundToInt(y1), m_BrushSize);
                return;
            }
             
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (m_BrushSize * 0.5f)));
             
            steps = Mathf.Min(steps, 50);
            
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x1, x2, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y1, y2, t));
                
                DrawCircle(x, y, m_BrushSize);
            }
        }

        private void DrawCircle(int centerX, int centerY, int radius)
        {
            if (m_MaskTexture == null) return;
            
            int width = m_MaskTexture.width;
            int height = m_MaskTexture.height;
             
            for (int y = centerY - radius; y <= centerY + radius; y++)
            { 
                if (y < 0 || y >= height) continue;
                
                for (int x = centerX - radius; x <= centerX + radius; x++)
                { 
                    if (x < 0 || x >= width) continue;
                     
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radius * radius)
                    { 
                        m_PendingPixels.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        private Rect GetTextureRect(Vector2 canvasSize, Vector2 textureSize)
        {
            float canvasRatio = canvasSize.x / canvasSize.y;
            float textureRatio = textureSize.x / textureSize.y;
            
            Rect result = new Rect();
            
            if (canvasRatio > textureRatio)
            {
                // Canvas is wider than texture - fit by height
                float scaledWidth = canvasSize.y * textureRatio;
                result.x = (canvasSize.x - scaledWidth) / 2;
                result.y = 0;
                result.width = scaledWidth;
                result.height = canvasSize.y;
            }
            else
            {
                // Canvas is taller than texture - fit by width
                float scaledHeight = canvasSize.x / textureRatio;
                result.x = 0;
                result.y = (canvasSize.y - scaledHeight) / 2;
                result.width = canvasSize.x;
                result.height = scaledHeight;
            }
            
            return result;
        }

        private void ClearMask()
        {
            if (m_MaskTexture == null) return;
            
            // Clear mask (set all pixels to transparent black)
            Color[] colors = new Color[m_MaskTexture.width * m_MaskTexture.height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(0, 0, 0, 0);
            }
            
            m_MaskTexture.SetPixels(colors);
            m_MaskTexture.Apply();
            m_IsMaskDirty = false;
            
            // Clear any pending pixels
            m_PendingPixels.Clear();
        }

        private void InvertMask()
        {
            if (m_MaskTexture == null) return;
             
            ApplyPendingPixels();
             
            Color[] pixels = m_MaskTexture.GetPixels();
            
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                // Invert alpha channel
                pixels[i] = new Color(1, 1, 1, 1 - pixel.a);
            }
            
            m_MaskTexture.SetPixels(pixels);
            m_MaskTexture.Apply();
            m_IsMaskDirty = true;
        }

        #endregion

        private void OnGenerateClicked()
        {
            // Validate inputs
            if (m_SourceTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a source image.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(m_Prompt))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a prompt for the image edit.", "OK");
                return;
            }

            if (m_MaskTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Mask texture not created. Please try again.", "OK");
                return;
            }

            // Apply any pending pixels before checking if mask is dirty
            ApplyPendingPixels();

            if (!m_IsMaskDirty)
            {
                EditorUtility.DisplayDialog("Error", "Please draw a mask on the image to indicate which areas to edit.", "OK");
                return;
            }

            // Check if source texture is readable
            if (!IsTextureReadable(m_SourceTexture))
            {
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
                if (!IsTextureReadable(m_SourceTexture))
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
            m_ResultsScrollView.parent.style.display = DisplayStyle.None;

            // Show status
            m_StatusLabel.text = "Generating edited image...";
            m_GenerateButton.SetEnabled(false);

            // Create uncompressed copies of textures
            Texture2D processedSourceTexture = EnsureUncompressedTexture(m_SourceTexture);
            
            // Invert the mask for API submission (API expects transparent areas to be edited)
            Texture2D invertedMask = InvertMaskForAPI(m_MaskTexture);
            Texture2D processedMaskTexture = EnsureUncompressedTexture(invertedMask);
            
            if (processedSourceTexture == null || processedMaskTexture == null)
            {
                m_StatusLabel.text = "Error: Failed to process textures for API submission.";
                m_GenerateButton.SetEnabled(true);
                return;
            }

            // Create request
            var request = new ImageEditRequest
            {
                Prompt = m_Prompt,
                Model = m_SelectedModel,
                Count = m_ImageCount,
                Quality = m_SelectedQuality,
                Image = processedSourceTexture,
                Mask = processedMaskTexture
            };

            // Only set size for DALL-E 2, not for GPT-Image-1
            if (m_SelectedModel == ImageModel.DallE2)
            {
                request.Size = m_SelectedSize;
            }

            // Register callback
            DALLEClient.Instance.OnResponseReceived = null;
            DALLEClient.Instance.OnResponseReceived += OnImagesReceived;

            try
            {
                // Send request
                DALLEClient.Instance.EditImage(request);
            }
            catch (Exception ex)
            {
                m_StatusLabel.text = $"Error: {ex.Message}";
                m_GenerateButton.SetEnabled(true);
                Debug.LogException(ex);
            }
        }
        
        // Helper method to ensure texture is in an uncompressed format that can be encoded to PNG
        private Texture2D EnsureUncompressedTexture(Texture2D source)
        {
            if (source == null) return null;
            
            try
            { 
                RenderTexture tempRT = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                 
                Graphics.Blit(source, tempRT);
                 
                RenderTexture previousRT = RenderTexture.active;
                 
                RenderTexture.active = tempRT;
                 
                Texture2D uncompressedTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                 
                uncompressedTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                uncompressedTexture.Apply();
                 
                RenderTexture.active = previousRT;
                 
                RenderTexture.ReleaseTemporary(tempRT);
                
                return uncompressedTexture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating uncompressed texture: {ex.Message}");
                return null;
            }
        }

        private Texture2D InvertMaskForAPI(Texture2D sourceMask)
        {
            if (sourceMask == null) return null;
             
            Texture2D invertedMask = new Texture2D(sourceMask.width, sourceMask.height, TextureFormat.RGBA32, false);
             
            Color[] pixels = sourceMask.GetPixels();
            Color[] invertedPixels = new Color[pixels.Length];
             
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i]; 
                invertedPixels[i] = new Color(1, 1, 1, 1 - pixel.a);
            }
            
            invertedMask.SetPixels(invertedPixels);
            invertedMask.Apply();
            
            return invertedMask;
        }
        
        private void OnImagesReceived(List<DALLEImageResult> images)
        { 
            m_Root.Add(resultsContainer);
            ImageCreatorWindow.window.minSize = new Vector2(700, 900);

            m_GeneratedImages = images;
            m_StatusLabel.text = $"Generated {images.Count} edited images.";
            m_GenerateButton.SetEnabled(true);
 
            m_ResultsScrollView.Clear();
             
            foreach (var image in images)
            {
                var thumbnail = CreateThumbnail(image);
                m_ResultsScrollView.Add(thumbnail);
            }
 
            m_ResultsScrollView.parent.style.display = DisplayStyle.Flex;
            
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

            string defaultName = "EditedImage.png";
            string path = EditorUtility.SaveFilePanel("Save Edited Image", "", defaultName, "png");
            
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
            EditorPrefs.SetString($"UAI_ImageCreator_MaskDrawingPrompt", m_Prompt);
            EditorPrefs.SetInt($"UAI_ImageCreator_MaskDrawingBrushSize", m_BrushSize);
            EditorPrefs.SetInt($"UAI_ImageCreator_MaskDrawingModel", (int)m_SelectedModel);
            EditorPrefs.SetInt($"UAI_ImageCreator_MaskDrawingCount", m_ImageCount);
            EditorPrefs.SetInt($"UAI_ImageCreator_MaskDrawingSize", (int)m_SelectedSize);
            EditorPrefs.SetInt($"UAI_ImageCreator_MaskDrawingQuality", (int)m_SelectedQuality);
            EditorPrefs.SetBool($"UAI_ImageCreator_MaskDrawingShowMask", m_ShowMask);
            EditorPrefs.SetFloat($"UAI_ImageCreator_MaskDrawingRefreshRate", m_RefreshRate);
        }

        public override void LoadSettings()
        {
            m_Prompt = EditorPrefs.GetString($"UAI_ImageCreator_MaskDrawingPrompt", "Edit this image.");
            m_BrushSize = EditorPrefs.GetInt($"UAI_ImageCreator_MaskDrawingBrushSize", 10);
            m_SelectedModel = (ImageModel)EditorPrefs.GetInt($"UAI_ImageCreator_MaskDrawingModel", (int)ImageModel.GPTImage1);
            m_ImageCount = EditorPrefs.GetInt($"UAI_ImageCreator_MaskDrawingCount", 1);
            m_SelectedSize = (Size)EditorPrefs.GetInt($"UAI_ImageCreator_MaskDrawingSize", (int)Size.Size1024x1024);
            m_SelectedQuality = (Quality)EditorPrefs.GetInt($"UAI_ImageCreator_MaskDrawingQuality", (int)Quality.high);
            m_ShowMask = EditorPrefs.GetBool($"UAI_ImageCreator_MaskDrawingShowMask", true);
            m_RefreshRate = EditorPrefs.GetFloat($"UAI_ImageCreator_MaskDrawingRefreshRate", 0.016f);
        }
        
        // Clean up when the window is destroyed
        public void OnDestroy()
        {
            // Unregister editor update callback
            EditorApplication.update -= OnEditorUpdate;
        }
    }
}