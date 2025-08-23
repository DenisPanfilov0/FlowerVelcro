
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Linq;
using System.Collections;

namespace uAI.PixelArtScaler
{ 
    public class PixelartScalerElement : VisualElement
    {
        #region Private Fields

        private Texture2D sourceTexture;
        private Texture2D gridSnappedTexture;

        private Texture2D GetEffectiveSourceTexture()
        {
            bool enableGridSnapping = enableGridSnappingToggle?.value ?? false;
            
            if (!enableGridSnapping)
                return sourceTexture;
             
            if (gridSnappedTexture != null)
                return gridSnappedTexture;
  
            if (hSpacing <= 1 && vSpacing <= 1)
            {
                Debug.LogWarning("Grid snapping enabled but no pixel grid detected. Run 'Detect Pixel Grid' first.");
                return sourceTexture;
            }
            
            float scale = Mathf.Max(hSpacing, vSpacing);
            var snapResult = PixelDetectorHelper.ApplyGridSnapping(sourceTexture, scale, true);
             
            
            if (snapResult.success && snapResult.snappedTexture != null)
            {
                gridSnappedTexture = snapResult.snappedTexture;
                return gridSnappedTexture;
            }
             
            return sourceTexture;
        }
         
        private Dictionary<string, Texture2D> preQuantizationCache = new Dictionary<string, Texture2D>();
         
        private Texture2D previewTexture;
        private bool isShowingPreview = false;
        private double previewStartTime;
        private const double PREVIEW_DURATION = 2.0f;
 
        private class ScalingResult
        {
            public Texture2D Texture;
            public Texture2D PreQuantizationTexture;  
            public bool IsVisible = true;
            public bool ShowPreQuantization = false;  
            public PixelArtScalingMethods.ScalingMethod Method;
            public int maxColors;
            public int KValue;  
            public string QuantizationInfo = ""; 
            public bool HasPostQuantization = false;  
            public bool hasGridSnapping = false;   
            public AdvancedColorQuantizer.QuantizationStage quantizationStage;

            public string DisplayName => KValue > 0 ? "K-Centroid" : $"{Method}";

            public string DisplaySettings => $"{(Texture != null ? $" {Texture.width}x{Texture.height}" : "")} {(KValue > 0 ? $" (K={KValue})" : "")}\n" +
                $"{(maxColors > 0 ? $" [Max Colors={maxColors}]\n" : "")}" +
                $"{(hasGridSnapping ? "[Snapped]\n" : "")}" + 
                $"{(PreQuantizationTexture != null ? " [Pre-Quantized]\n" : "")}" +
                $"{"Color quantization: " + getPrefixQuantization()}";


            private string getPrefixQuantization()
            {
                switch(quantizationStage)
                {
                    case AdvancedColorQuantizer.QuantizationStage.PreDownscaling:
                        return "Pre scaling";
                    case AdvancedColorQuantizer.QuantizationStage.PostDownscaling:
                        return "Post scaling";
                    case AdvancedColorQuantizer.QuantizationStage.Both:
                        return "Both";
                    default:
                        return "";
                }
            } 
        }
         
        private Dictionary<string, ScalingResult> resultTextures = new Dictionary<string, ScalingResult>();
        private List<string> activeCentroids = new List<string>();
         
        private float hSpacing, vSpacing;
        private int newCentroid = 4;
         
        private float zoomLevel = 1.0f;
        private Vector2 panOffset = Vector2.zero;
        private bool showOriginalSize = false;
        private bool showLayered = true;
          
        private int maxColors = 16;
        private bool enableDithering = false;
         
        private bool autoDetectColors = true;
        private float dominanceThreshold = 0.15f;
        private AdvancedColorQuantizer.DominantColorMethod dominantColorMethod = AdvancedColorQuantizer.DominantColorMethod.WeightedFrequency;
        private AdvancedColorQuantizer.QuantizationStage quantizationStage = AdvancedColorQuantizer.QuantizationStage.Both;
        private int qualityLevel = 3;
        private bool preserveAlphaVariation = true;
        private bool usePerceptualWeighting = true;
        private float perceptualSensitivity = 1.0f;
        
        private const float MAX_ZOOM = 20.0f;
        private const float MIN_ZOOM = 0.1f;
        
        #endregion

        #region UI Elements

        private ObjectField sourceTextureField;
        private Toggle forceUniformSpacingToggle;
        private Button detectPixelGridButton; 
        private Button normalScaleButton;
        private EnumField scaleMethodDropdown;
        private DropdownField dropdownGridSamplingSteps;
        private FloatField hSpacingField;
        private FloatField vSpacingField;
        private IntegerField centroidsField;
        private Label lblQuantizationDone; 
          
        private IntegerField maxColorsField; 
        private Button previewQuantizationButton;  
        private Toggle enableGridSnappingToggle;

         
        private Toggle autoDetectColorsToggle;
        private Slider dominanceThresholdSlider;
        private EnumField dominantColorMethodDropdown;
        private EnumField quantizationStageDropdown;
        private IntegerField qualityLevelField;
        private Toggle preserveAlphaVariationToggle;
        private Toggle usePerceptualWeightingToggle;
        private Slider perceptualSensitivitySlider;
         
        private ScrollView activeResultsContainerScrollView;
        private Toggle showLayeredToggle;
        private Toggle showOriginalSizeToggle;
        private Label zoomLabel;
        private Button zoomOutButton;
        private Button zoomInButton;
        private Button resetViewButton;
        private VisualElement previewContainer;
        private VisualElement overlayContainer;
        private Button saveActiveResultButton;
        private Button saveAllResultsButton;
        private ScrollView previewScrollView;
        private Label originalSizeLabel;
        private Label resultSizeLabel;
         
        private Label previewStatusLabel; 
         
        private bool isDragging = false;
        private Vector2 lastMousePosition;

        #endregion

        #region Events

        public event Action<Texture2D> OnResultSaved;
        public event Action<Dictionary<string, Texture2D>> OnAllResultsSaved;

        #endregion

        #region Factory and Traits
 

        #endregion

        #region Constructor and Initialization

        public PixelartScalerElement()
        {
            Initialize();
            EditorApplication.update += UpdatePreviewTimer;
        }
        
        ~PixelartScalerElement()
        {
            EditorApplication.update -= UpdatePreviewTimer;
        }
        
        public void Initialize()
        {
            
            var visualTree = Resources.Load<VisualTreeAsset>("PixelartScalerElement");
            if (visualTree == null)
            {
                Debug.LogError("Could not find UXML file. Make sure it's in a Resources folder: Resources/PixelartScalerElement.uxml");
                CreateFallbackUI();
                return;
            }
             
            var styleSheet = Resources.Load<StyleSheet>("PixelartScalerElement");
            if (styleSheet == null)
            {
                Debug.LogError("Could not find USS file. Make sure it's in a Resources folder: Resources/PixelartScalerElement.uss");
            }
            else
            {
                this.styleSheets.Add(styleSheet); 
            }
            
            visualTree.CloneTree(this);
            
            InitializeUIElements();
            RegisterCallbacks();
        }

        private void CreateFallbackUI()
        {
            var container = new VisualElement();
            container.style.paddingTop = 10;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.paddingBottom = 10;
            
            var label = new Label("PixelArt Scaler (Fallback UI)");
            label.style.fontSize = 16;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(label);
            
            sourceTextureField = new ObjectField("Source Texture");
            sourceTextureField.objectType = typeof(Texture2D);
            container.Add(sourceTextureField);
            
            detectPixelGridButton = new Button() { text = "Detect Pixel Grid" };
            container.Add(detectPixelGridButton);
             
            
            normalScaleButton = new Button() { text = "Normal Scale" };
            container.Add(normalScaleButton);
            
            previewQuantizationButton = new Button() { text = "Preview Quantization" };
            container.Add(previewQuantizationButton);
            
            previewContainer = new VisualElement();
            previewContainer.style.minHeight = 200;
            previewContainer.style.backgroundColor = Color.gray;
            container.Add(previewContainer);
            
            this.Add(container);
            RegisterCallbacks();
        }

        private void InitializeUIElements()
        {
            
            sourceTextureField = this.Q<ObjectField>("sourceTexture");
            forceUniformSpacingToggle = this.Q<Toggle>("forceUniformSpacing");
            detectPixelGridButton = this.Q<Button>("detectPixelGrid"); 
            normalScaleButton = this.Q<Button>("normalScale");
            scaleMethodDropdown = this.Q<EnumField>("enumScalingMethod");
            dropdownGridSamplingSteps = this.Q<DropdownField>("dropdownGridSamplingSteps");
            hSpacingField = this.Q<FloatField>("hSpacing");
            vSpacingField = this.Q<FloatField>("vSpacing");
            centroidsField = this.Q<IntegerField>("centroids");
            lblQuantizationDone = this.Q<Label>("lblQuantizationDone"); 
            enableGridSnappingToggle = this.Q<Toggle>("enableGridSnapping");

            
             
            maxColorsField = this.Q<IntegerField>("maxColors"); 
            previewQuantizationButton = this.Q<Button>("previewQuantization");
            
            
            autoDetectColorsToggle = this.Q<Toggle>("autoDetectColors");
            dominanceThresholdSlider = this.Q<Slider>("dominanceThreshold");
            dominantColorMethodDropdown = this.Q<EnumField>("dominantColorMethod");
            quantizationStageDropdown = this.Q<EnumField>("quantizationStage");
            qualityLevelField = this.Q<IntegerField>("qualityLevel");
            preserveAlphaVariationToggle = this.Q<Toggle>("preserveAlphaVariation");
            usePerceptualWeightingToggle = this.Q<Toggle>("usePerceptualWeighting");
            perceptualSensitivitySlider = this.Q<Slider>("perceptualSensitivity");
            
            
            activeResultsContainerScrollView = this.Q<ScrollView>("activeResults");
            showLayeredToggle = this.Q<Toggle>("showLayered");
            showOriginalSizeToggle = this.Q<Toggle>("showOriginalSize");
            zoomLabel = this.Q<Label>("zoomLabel");
            zoomOutButton = this.Q<Button>("zoomOut");
            zoomInButton = this.Q<Button>("zoomIn");
            resetViewButton = this.Q<Button>("resetView");
            previewContainer = this.Q<VisualElement>("previewContainer");
            overlayContainer = this.Q<VisualElement>("overlayContainer");
            saveActiveResultButton = this.Q<Button>("saveActiveResult");
            saveAllResultsButton = this.Q<Button>("saveAllResults");
            previewScrollView = this.Q<ScrollView>("previewScrollView");
            VisualElement previewScrollViewContentContainer = previewScrollView.Q<VisualElement>("unity-content-viewport");
            
            previewScrollViewContentContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            previewScrollViewContentContainer.style.flexGrow = 1;
            previewScrollViewContentContainer.style.flexShrink = 0;
            
            originalSizeLabel = this.Q<Label>("originalSizeLabel");
            resultSizeLabel = this.Q<Label>("resultSizeLabel");
            previewStatusLabel = this.Q<Label>("previewStatusLabel");
            
            SetInitialValues();
        }

        private void SetInitialValues()
        {
            
            if (sourceTextureField != null)
            {
                sourceTextureField.objectType = typeof(Texture2D);
                sourceTextureField.tooltip = "Source texture to process. Changing this will clear the quantization cache.";
            }
            if (centroidsField != null)
                centroidsField.value = newCentroid;
            if (showLayeredToggle != null)
                showLayeredToggle.value = showLayered;
            if (showOriginalSizeToggle != null)
                showOriginalSizeToggle.value = showOriginalSize;
            if (forceUniformSpacingToggle != null)
                forceUniformSpacingToggle.value = PixelDetectorHelper.forceUniformSpacing;

            lblQuantizationDone.text = "";
            
             
            if (maxColorsField != null)
            {
                maxColorsField.value = maxColors;
                maxColorsField.tooltip = "Maximum number of colors in quantized result. Changing this clears the cache.";
            } 
            
            
            if (previewQuantizationButton != null)
            {
                previewQuantizationButton.tooltip = "Preview quantized colors for 2 seconds. Uses cached result if available.";
            }
            
            if (autoDetectColorsToggle != null)
            {
                autoDetectColorsToggle.value = autoDetectColors;
                autoDetectColorsToggle.tooltip = "Automatically determine optimal color count. Affects cache when changed.";
            }
            

            if (dominanceThresholdSlider != null)
            {
                dominanceThresholdSlider.value = dominanceThreshold;
                dominanceThresholdSlider.tooltip = "Threshold for determining dominant colors. Lower = more selective.";
            }
            if (dominantColorMethodDropdown != null)
            {
                dominantColorMethodDropdown.Init(dominantColorMethod);
                dominantColorMethodDropdown.value = dominantColorMethod;
                dominantColorMethodDropdown.tooltip = "Method for selecting dominant colors from the image.";
            }
            if (quantizationStageDropdown != null)
            {
                quantizationStageDropdown.Init(quantizationStage);
                quantizationStageDropdown.value = quantizationStage;
                quantizationStageDropdown.tooltip = "When to apply quantization:\n• Pre: Before scaling (cached)\n• Post: After scaling\n• Both: Maximum reduction";
            }
            if (qualityLevelField != null)
            {
                qualityLevelField.value = qualityLevel;
                qualityLevelField.tooltip = "Quality level (1-5). Higher = better quality but slower processing.";
            }
            if (preserveAlphaVariationToggle != null)
            {
                preserveAlphaVariationToggle.value = preserveAlphaVariation;
                preserveAlphaVariationToggle.tooltip = "Preserve alpha channel variations during quantization.";
            }
            if (usePerceptualWeightingToggle != null)
            {
                usePerceptualWeightingToggle.value = usePerceptualWeighting;
                usePerceptualWeightingToggle.tooltip = "Use perceptually uniform color space (CIELAB) for better color matching.";
            }
            if (perceptualSensitivitySlider != null)
            {
                perceptualSensitivitySlider.value = perceptualSensitivity;
                perceptualSensitivitySlider.tooltip = "Sensitivity for perceptual color matching. Higher = more sensitive to differences.";
            }
            
            UpdateMaxColorsFieldState();
            UpdateZoomLabel();
            UpdateSaveButtons();
            UpdatePreviewButtonState();
        }

        #endregion

        #region Event Registration

        private void RegisterCallbacks()
        {

            if (sourceTextureField != null)
            {
                sourceTextureField.RegisterValueChangedCallback(evt =>
                {
                    sourceTexture = evt.newValue as Texture2D;
                    ClearPreQuantizationCache(); 
                    StopPreview(); 
                    UpdatePreview();
                    UpdatePreviewButtonState();

                    resultTextures.Clear();
                    activeCentroids.Clear();
                    activeResultsContainerScrollView.Clear();
                    UpdateSaveButtons();
                    overlayContainer.Clear();



                    hSpacing = 0;
                    vSpacing = 0;
                    gridSnappedTexture = null;
                    if (hSpacingField != null) hSpacingField.value = hSpacing;
                    if (vSpacingField != null) vSpacingField.value = vSpacing;
                    if (centroidsField != null) centroidsField.value = newCentroid;


                    if (detectPixelGridButton != null)
                        detectPixelGridButton.text = "Detect Pixel Grid";

                    if(sourceTexture != null) DetectPixelGrid(); 


                    VisualElement gridSection = this.Q<VisualElement>("grid-section");
                    gridSection.SetEnabled( sourceTexture != null );

                    VisualElement quantizationSection = this.Q<VisualElement>("quantization-section");
                    quantizationSection.SetEnabled( sourceTexture != null ) ;

                    VisualElement scalingSection = this.Q<VisualElement>("scaling-section");
                    scalingSection.SetEnabled( sourceTexture != null );

                    VisualElement exportSection = this.Q<VisualElement>("export-section");
                    exportSection.SetEnabled( sourceTexture != null );
                });
            }
            
            if(scaleMethodDropdown != null)
            {
                scaleMethodDropdown.RegisterValueChangedCallback(evt => 
                {
                    PixelArtScalingMethods.ScalingMethod method = (PixelArtScalingMethods.ScalingMethod)evt.newValue;
                    if (method == PixelArtScalingMethods.ScalingMethod.KCentroid)
                    {

                        if (centroidsField != null)
                            centroidsField.style.display = DisplayStyle.Flex;
                    }
                    else
                    {

                        newCentroid = 4;
                        if (centroidsField != null)
                            centroidsField.style.display = DisplayStyle.None;
                    }
                });
            }
            

            if (forceUniformSpacingToggle != null)
            {
                forceUniformSpacingToggle.RegisterValueChangedCallback(evt =>
                {
                    PixelDetectorHelper.forceUniformSpacing = evt.newValue;
                });
            }
            
            if (detectPixelGridButton != null)
                detectPixelGridButton.clicked += DetectPixelGrid; 
            
            if (normalScaleButton != null)
                normalScaleButton.clicked += NormalScale;
            
            if (hSpacingField != null)
            {
                hSpacingField.RegisterValueChangedCallback(evt => 
                {
                    hSpacing = evt.newValue;
                    InvalidateGridSnappingCache();
                });
            }
            
            if (vSpacingField != null)
            {
                vSpacingField.RegisterValueChangedCallback(evt => 
                {
                    vSpacing = evt.newValue;
                    InvalidateGridSnappingCache();
                });
            }
            

            if (centroidsField != null)
            {
                centroidsField.RegisterValueChangedCallback(evt => 
                {
                    newCentroid = Mathf.Max(1, evt.newValue);
                    centroidsField.value = newCentroid;
                });
            }
             
            enableGridSnappingToggle.RegisterValueChangedCallback(OnEnableGridSnappingChanged);


            RegisterQuantizationCallbacks();
            RegisterPreviewCallbacks();
        }

        private void RegisterQuantizationCallbacks()
        {
 
            if (maxColorsField != null)
            {
                maxColorsField.RegisterValueChangedCallback(evt => 
                {
                    maxColors = Mathf.Max(1, evt.newValue);
                    maxColorsField.value = maxColors;
                    ClearPreQuantizationCache(); 
                    StopPreview(); 
                });
            }
             
             
            
            if (previewQuantizationButton != null)
                previewQuantizationButton.clicked += PreviewQuantization;
            

            if (autoDetectColorsToggle != null)
            {
                autoDetectColorsToggle.RegisterValueChangedCallback(evt => 
                {
                    autoDetectColors = evt.newValue;
                    UpdateMaxColorsFieldState();
                    ClearPreQuantizationCache(); 
                    StopPreview(); 
                });
            }
            
            if (dominanceThresholdSlider != null)
            {
                dominanceThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    dominanceThreshold = evt.newValue;
                    ClearPreQuantizationCache();
                });
            }
            
            if (dominantColorMethodDropdown != null)
            {
                dominantColorMethodDropdown.RegisterValueChangedCallback(evt =>
                {
                    dominantColorMethod = (AdvancedColorQuantizer.DominantColorMethod)evt.newValue;
                    ClearPreQuantizationCache();
                });
            }
            
            if (quantizationStageDropdown != null)
            {
                quantizationStageDropdown.RegisterValueChangedCallback(evt => 
                {
                    quantizationStage = (AdvancedColorQuantizer.QuantizationStage)evt.newValue;
                });
            }
            
            if (qualityLevelField != null)
            {
                qualityLevelField.RegisterValueChangedCallback(evt => 
                {
                    qualityLevel = Mathf.Clamp(evt.newValue, 1, 5);
                    qualityLevelField.value = qualityLevel;
                    ClearPreQuantizationCache(); 
                });
            }
            
            if (preserveAlphaVariationToggle != null)
            {
                preserveAlphaVariationToggle.RegisterValueChangedCallback(evt => 
                {
                    preserveAlphaVariation = evt.newValue;
                    ClearPreQuantizationCache(); 
                });
            }
            
            if (usePerceptualWeightingToggle != null)
            {
                usePerceptualWeightingToggle.RegisterValueChangedCallback(evt => 
                {
                    usePerceptualWeighting = evt.newValue;
                    ClearPreQuantizationCache(); 
                });
            }
            
            if (perceptualSensitivitySlider != null)
            {
                perceptualSensitivitySlider.RegisterValueChangedCallback(evt => 
                {
                    perceptualSensitivity = evt.newValue;
                    ClearPreQuantizationCache(); 
                });
            }
        }

        private void RegisterPreviewCallbacks()
        {
            if (showLayeredToggle != null)
            {
                showLayeredToggle.RegisterValueChangedCallback(evt => 
                {
                    showLayered = evt.newValue;
                    UpdatePreview();
                });
            }
            
            if (showOriginalSizeToggle != null)
            {
                showOriginalSizeToggle.RegisterValueChangedCallback(evt => 
                {
                    showOriginalSize = evt.newValue;
                    UpdatePreview();
                });
            }
            
            if (zoomOutButton != null)
            {
                zoomOutButton.clicked += () => 
                {
                    zoomLevel = Mathf.Clamp(zoomLevel - 0.2f, MIN_ZOOM, MAX_ZOOM);
                    UpdateZoomLabel();
                    UpdatePreview();
                };
            }
            
            if (zoomInButton != null)
            {
                zoomInButton.clicked += () => 
                {
                    zoomLevel = Mathf.Clamp(zoomLevel + 0.2f, MIN_ZOOM, MAX_ZOOM);
                    UpdateZoomLabel();
                    UpdatePreview();
                };
            }
            
            if (resetViewButton != null)
            {
                resetViewButton.clicked += () => 
                {
                    zoomLevel = 1.0f;
                    panOffset = Vector2.zero;
                    UpdateZoomLabel();
                    UpdatePreview();
                };
            }
            
            if (saveActiveResultButton != null)
                saveActiveResultButton.clicked += SaveActiveResult;
            if (saveAllResultsButton != null)
                saveAllResultsButton.clicked += SaveAllResults;
            
            
            if (previewContainer != null)
            {
                previewContainer.RegisterCallback<MouseDownEvent>(evt => {
                    if (evt.button == 0 || evt.button == 1 || evt.button == 2)
                    {
                        isDragging = true;
                        lastMousePosition = evt.mousePosition;
                        evt.StopPropagation();
                    }
                });
            }
            
            if (previewScrollView != null)
            {
                previewScrollView.RegisterCallback<MouseDownEvent>(evt => {
                    if (evt.button == 0 || evt.button == 1 || evt.button == 2)
                    {
                        isDragging = true;
                        lastMousePosition = evt.mousePosition;
                        evt.StopPropagation();
                    }
                });
                
                previewScrollView.RegisterCallback<WheelEvent>(evt => {
                    float scrollDelta = evt.delta.y * 0.05f;
                    zoomLevel = Mathf.Clamp(zoomLevel - scrollDelta, MIN_ZOOM, MAX_ZOOM);
                    UpdateZoomLabel();
                    UpdatePreview();
                    evt.StopPropagation();
                });
            }
            
            this.RegisterCallback<MouseMoveEvent>(evt => {
                if (isDragging)
                {
                    Vector2 delta = evt.mousePosition - lastMousePosition;
                    panOffset += delta;
                    lastMousePosition = evt.mousePosition;
                    UpdatePreview();
                    evt.StopPropagation();
                }
            });
            
            this.RegisterCallback<MouseUpEvent>(evt => {
                if ((evt.button == 0 || evt.button == 1 || evt.button == 2) && isDragging)
                {
                    isDragging = false;
                    evt.StopPropagation();
                }
            });
        }

        #endregion

        #region Public Methods

        public void SetSourceTexture(Texture2D texture)
        {
            sourceTexture = texture;
            if (sourceTextureField != null)
                sourceTextureField.value = texture;
            ClearPreQuantizationCache();
            StopPreview(); 
            UpdatePreview();
            UpdatePreviewButtonState();
        }
        
        public Dictionary<string, Texture2D> GetResults()
        {
            var results = new Dictionary<string, Texture2D>();
            foreach (var kvp in resultTextures)
            {
                results.Add(kvp.Key, kvp.Value.Texture);
            }
            return results;
        }
        
        public Texture2D GetActiveResult()
        {
            if (activeCentroids.Count > 0)
            {
                string key = activeCentroids[0];
                if (resultTextures.TryGetValue(key, out ScalingResult result))
                    return result.ShowPreQuantization && result.PreQuantizationTexture != null ? result.PreQuantizationTexture : result.Texture;
            }
            return null;
        }
        
        private void InvalidateGridSnappingCache()
        {
            if (gridSnappedTexture != null)
            { 
                gridSnappedTexture = null;
            }
        }
        private void OnEnableGridSnappingChanged(ChangeEvent<bool> evt)
        { 
        
            InvalidateGridSnappingCache();
            UpdatePreview();
        }

        #endregion

        #region Caching Methods

        private string GeneratePreQuantizationCacheKey()
        {
            if (sourceTexture == null) return "";
            
            return $"{sourceTexture.GetInstanceID()}_{autoDetectColors}_{maxColors}_{enableDithering}_{dominanceThreshold:F3}_{dominantColorMethod}_{qualityLevel}_{preserveAlphaVariation}_{usePerceptualWeighting}_{perceptualSensitivity:F2}";
        }

        private void ClearPreQuantizationCache()
        {
            int cacheCount = preQuantizationCache.Count;
            preQuantizationCache.Clear();

            if (cacheCount > 0)
            {  
                lblQuantizationDone.text = "";
            }
        } 

        private Texture2D GetOrCreatePreQuantizedTexture()
        {
            string cacheKey = GeneratePreQuantizationCacheKey();
            if (string.IsNullOrEmpty(cacheKey)) return sourceTexture;

            if (preQuantizationCache.TryGetValue(cacheKey, out Texture2D cachedTexture))
            {  
                return cachedTexture;
            }
             
            try
            {
                var settings = CreateQuantizationSettings();
                var result = AdvancedColorQuantizer.QuantizeTexture(sourceTexture, settings);
                preQuantizationCache[cacheKey] = result.quantizedTexture;
                lblQuantizationDone.text = "DONE";
                return result.quantizedTexture;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Pre-quantization failed: {e.Message}");
                return sourceTexture;
            }
        }
         
        #endregion

        #region Preview Methods

        private void UpdatePreviewTimer()
        {
            if (!isShowingPreview) return;

            double elapsedTime = (double)(EditorApplication.timeSinceStartup - previewStartTime);
            double remainingTime = PREVIEW_DURATION - elapsedTime;

            if (remainingTime > 0)
            {
                
                if (previewStatusLabel != null)
                    previewStatusLabel.text = $"Previewing quantization... ({remainingTime:F1}s)";
            }
            else
            {
                
                StopPreview();
            }
        }

        private void UpdatePreviewButtonState()
        {
            if (previewQuantizationButton != null)
            {
                previewQuantizationButton.SetEnabled(sourceTexture != null);
                previewQuantizationButton.text = isShowingPreview ? "Stop Preview" : "Preview Quantization (2s)";
            }
        }

        private void PreviewQuantization()
        {
            if (sourceTexture == null) return;

            if (isShowingPreview)
            {
                
                StopPreview();
            }
            else
            {
                
                StartPreview();
            }
        }

        private void StartPreview()
        {
            
            StopPreview();

            isShowingPreview = true;
            previewStartTime = EditorApplication.timeSinceStartup;
            UpdatePreviewButtonState();

            
            if (previewStatusLabel != null)
                previewStatusLabel.text = "Generating quantization preview...";

            
            previewTexture = GetOrCreatePreQuantizedTexture();
            UpdatePreview();

            
            if (previewStatusLabel != null)
                previewStatusLabel.text = $"Previewing quantization... ({PREVIEW_DURATION:F1}s)";
 
        }

        private void StopPreview()
        {
            if (!isShowingPreview) return;

            isShowingPreview = false;
            previewTexture = null;
            UpdatePreview();
            UpdatePreviewButtonState();

            
            if (previewStatusLabel != null)
                previewStatusLabel.text = ""; 
        }

        #endregion

        #region Core Processing Methods

        private void DetectPixelGrid()
        {
            if (sourceTexture == null)
            {
                Debug.LogWarning("Source texture not set. Cannot perform detection.");
                return;
            }

            try
            {
                
                PixelDetectorHelper.PixelDetect(sourceTexture, out hSpacing, out vSpacing);

                
                if (hSpacingField != null)
                    hSpacingField.value = hSpacing;
                if (vSpacingField != null)
                    vSpacingField.value = vSpacing;

                
                gridSnappedTexture = null;

                UpdateActiveResults();
                UpdatePreview();
                
                if(detectPixelGridButton != null)
                {
                    detectPixelGridButton.text = "Re-Detect Pixel Grid";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Pixel grid detection failed: {e.Message}");
            }
        }



        private void NormalScale()
        {

            
            PixelArtScalingMethods.ScalingMethod currentScalingMethod = PixelArtScalingMethods.ScalingMethod.NearestNeighbor;
            if (scaleMethodDropdown?.value != null)
                currentScalingMethod = (PixelArtScalingMethods.ScalingMethod)scaleMethodDropdown.value;

            if(currentScalingMethod == PixelArtScalingMethods.ScalingMethod.KCentroid)
            {
                
                ProcessWithCentroid(newCentroid);
                return;
            }
                
            var effectiveSourceTexture = GetEffectiveSourceTexture();
            if (effectiveSourceTexture == null)
                return;
                
            if (hSpacing <= 0 || vSpacing <= 0)
            {
                DetectPixelGrid();
                if (hSpacing <= 0 || vSpacing <= 0)
                    return;
            }

            try
            {
                
                int newW = Mathf.RoundToInt(effectiveSourceTexture.width / Mathf.Max(1f, hSpacing));
                int newH = Mathf.RoundToInt(effectiveSourceTexture.height / Mathf.Max(1f, vSpacing));

                
        

                
                var quantizedTexture = ApplyQuantizationIfNeeded(effectiveSourceTexture, AdvancedColorQuantizer.QuantizationStage.PreDownscaling);


                Texture2D beforePostQuantization;
        
                beforePostQuantization = PixelArtScalingMethods.ScaleTexture(quantizedTexture, newW, newH, currentScalingMethod);
                
                
                Texture2D result = ApplyQuantizationIfNeeded(beforePostQuantization, AdvancedColorQuantizer.QuantizationStage.PostDownscaling);
                result.filterMode = FilterMode.Point;
                
                
                string key = $"downscaled_enhanced_{currentScalingMethod}";
                if (autoDetectColors) key += "_auto";
                else key += $"_{maxColors}c";
                if (enableDithering) key += "_dithered";
                if (enableGridSnappingToggle?.value ?? false)
                    key += "_gridsnapped";
                key += $"_{(AdvancedColorQuantizer.QuantizationStage)quantizationStageDropdown.value}";
                
                
                string processingInfo = $"Enhanced downscaled {hSpacing:F1}x using block analysis";
                
                var scalingResult = new ScalingResult
                {
                    Texture = result,
                    PreQuantizationTexture = beforePostQuantization != result ? beforePostQuantization : null,
                    Method = currentScalingMethod,
                    KValue = 0,
                    IsVisible = true,
                    HasPostQuantization = beforePostQuantization != result,
                    QuantizationInfo = processingInfo,
                    hasGridSnapping = enableGridSnappingToggle?.value ?? false, 
                    quantizationStage = (AdvancedColorQuantizer.QuantizationStage)quantizationStageDropdown.value,
                    maxColors = maxColors
                };
                
                StoreResult(key, scalingResult);
            }
            catch (Exception e)
            {
                Debug.LogError($"Enhanced scaling failed: {e.Message}");
            }
        }

        private void ProcessWithCentroid(int k)
        {  
            var sourceTexture = GetEffectiveSourceTexture();
            if (sourceTexture == null)
                return;

            if (hSpacing <= 0 || vSpacing <= 0)
            {
                DetectPixelGrid();
                if (hSpacing <= 0 || vSpacing <= 0)
                    return;
            }

            try
            { 
                int newW = Mathf.RoundToInt(sourceTexture.width / Mathf.Max(1f, hSpacing));
                int newH = Mathf.RoundToInt(sourceTexture.height / Mathf.Max(1f, vSpacing));
  
                var quantizedTexture = ApplyQuantizationIfNeeded(sourceTexture, AdvancedColorQuantizer.QuantizationStage.PreDownscaling);
                Texture2D beforePostQuantization = PixelDetectorHelper.KCentroid(quantizedTexture, newW, newH, k);
                Texture2D result = ApplyQuantizationIfNeeded(beforePostQuantization, AdvancedColorQuantizer.QuantizationStage.PostDownscaling);
                result.filterMode = FilterMode.Point;
 
                string key = $"kcentroid_{k}";
                if (autoDetectColors) key += "_auto";
                else key += $"_{maxColors}c"; 
                if (enableDithering) key += "_dithered";
                if (enableGridSnappingToggle?.value ?? false)
                    key += "_gridsnapped";


                key += $"_{(AdvancedColorQuantizer.QuantizationStage)quantizationStageDropdown.value}";
 
                string processingInfo = $"K-Centroid (K={k})";

                var scalingResult = new ScalingResult
                {
                    Texture = result,
                    PreQuantizationTexture = beforePostQuantization != result ? beforePostQuantization : null,
                    Method = PixelArtScalingMethods.ScalingMethod.NearestNeighbor,
                    KValue = k,
                    IsVisible = true,
                    HasPostQuantization = beforePostQuantization != result,
                    QuantizationInfo = processingInfo,
                    hasGridSnapping = enableGridSnappingToggle?.value ?? false, 
                    quantizationStage = (AdvancedColorQuantizer.QuantizationStage)quantizationStageDropdown.value,
                    maxColors = maxColors
                };

                StoreResult(key, scalingResult);
            }
            catch (Exception e)
            {
                Debug.LogError($"K-Centroid processing failed: {e.Message}");
            }
        }
 
        #endregion

        #region Helper Methods

        private void UpdateMaxColorsFieldState()
        {
            if (maxColorsField != null)
            {
                maxColorsField.SetEnabled(!autoDetectColors);
                maxColorsField.style.opacity = autoDetectColors ? 0.5f : 1.0f;
            }
        }

        private AdvancedColorQuantizer.QuantizationSettings CreateQuantizationSettings()
        {
            return new AdvancedColorQuantizer.QuantizationSettings
            {
                autoDetectColors = this.autoDetectColors,
                maxColors = this.maxColors,
                minColors = 2,
                dominanceThreshold = this.dominanceThreshold,
                dominantMethod = this.dominantColorMethod,
                quantizationStage = this.quantizationStage,
                qualityLevel = this.qualityLevel,
                preserveAlphaVariation = this.preserveAlphaVariation 
            };
        }

        private Texture2D ApplyQuantizationIfNeeded(Texture2D texture, AdvancedColorQuantizer.QuantizationStage currentStage)
        {
            if (quantizationStage == currentStage || quantizationStage == AdvancedColorQuantizer.QuantizationStage.Both)
            {
                if (currentStage == AdvancedColorQuantizer.QuantizationStage.PreDownscaling)
                {
                    
                    return GetOrCreatePreQuantizedTexture();
                }
                else
                {
                    
                    try
                    {
                        var settings = CreateQuantizationSettings();
                        var result = AdvancedColorQuantizer.QuantizeTexture(texture, settings); 
                        return result.quantizedTexture;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Advanced quantization failed at {currentStage} stage: {e.Message}");
                    }
                }
            }
            return texture;
        }

        private void StoreResult(string key, ScalingResult scalingResult)
        {
            if (resultTextures.ContainsKey(key))
            {
                resultTextures[key] = scalingResult;
            }
            else
            {
                resultTextures.Add(key, scalingResult);
            }
            
            if (!activeCentroids.Contains(key))
            {
                activeCentroids.Add(key);
            }
            
            UpdateActiveResults();
            UpdatePreview();
        }

        private void UpdateZoomLabel()
        {
            if (zoomLabel != null)
                zoomLabel.text = $"Zoom: {zoomLevel:F2}x";
        }
        
        private void UpdateSaveButtons()
        {
            bool hasActiveResults = activeCentroids.Count > 0 && resultTextures.Count > 0;
            if (saveActiveResultButton != null)
                saveActiveResultButton.SetEnabled(hasActiveResults);
            if (saveAllResultsButton != null)
                saveAllResultsButton.SetEnabled(resultTextures.Count > 0);
        }

        #endregion

        #region UI Update Methods

        private void UpdateActiveResults()
        {
            if (activeResultsContainerScrollView == null) return;
            
            activeResultsContainerScrollView.Clear();
             
            
            if (resultTextures.Count == 0)
            {
                Label placeholder = new Label("No active results. Use the scaling methods to create results.");
                placeholder.style.fontSize = 11;
                placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
                placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
                placeholder.style.paddingTop = 10;
                placeholder.style.paddingBottom = 10;
                activeResultsContainerScrollView.Add(placeholder);
                return;
            }
            
            foreach (var kvp in resultTextures)
            {
                string key = kvp.Key;
                ScalingResult result = kvp.Value;
                
                VisualElement resultItem = new VisualElement();
                resultItem.AddToClassList("result-item");
                
                Toggle activeToggle = new Toggle(result.DisplayName);
                if (key.Contains("shifted"))
                {
                    string[] parts = key.Split('_');
                    string numberPart = parts[parts.Length - 1];
                    activeToggle.label = result.DisplayName + " (S" + numberPart + ")"; 
                }

                activeToggle.value = activeCentroids.Contains(key);
                activeToggle.AddToClassList("result-toggle");
                
                string keyCaptured = key;
                activeToggle.RegisterValueChangedCallback(evt => 
                {
                    if (evt.newValue)
                    {
                        if (!activeCentroids.Contains(keyCaptured))
                            activeCentroids.Add(keyCaptured);
                    }
                    else
                    {
                        activeCentroids.Remove(keyCaptured);
                    }
                    UpdatePreview();
                });
                
                
                Label settingsLabel = new Label(result.DisplaySettings);
                settingsLabel.AddToClassList("size-label");
                
                Button removeButton = new Button(() => 
                {
                    resultTextures.Remove(keyCaptured);
                    activeCentroids.Remove(keyCaptured);
                    UpdateActiveResults();
                    UpdatePreview();
                    UpdateSaveButtons();
                });
                removeButton.text = "×";
                removeButton.AddToClassList("remove-button");
                
                VisualElement topContainer = new VisualElement();
                topContainer.style.flexDirection = FlexDirection.Row;
                topContainer.style.alignItems = Align.Center;
                topContainer.style.justifyContent = Justify.SpaceBetween;
                topContainer.Add(activeToggle);
                
                VisualElement rightControls = new VisualElement();
                rightControls.style.flexDirection = FlexDirection.Row;
                rightControls.style.alignItems = Align.Center; 
                
                topContainer.Add(rightControls);
                
                VisualElement bottomContainer = new VisualElement();
                bottomContainer.style.flexDirection = FlexDirection.Row;
                bottomContainer.style.alignItems = Align.Center;
                bottomContainer.style.justifyContent = Justify.SpaceBetween;
                
                bottomContainer.Add(settingsLabel);
                bottomContainer.Add(removeButton);
                
                resultItem.Add(topContainer);
                resultItem.Add(bottomContainer);
                
                activeResultsContainerScrollView.Add(resultItem);
            }
            
            UpdateSaveButtons();
        }
        
        private void UpdatePreview()
        {
            if (previewContainer != null)
                previewContainer.Clear();
            if (overlayContainer != null)
                overlayContainer.Clear();
            
            Texture2D textureToShow = isShowingPreview && previewTexture != null ? previewTexture : sourceTexture;
            
            if (textureToShow == null)
                return;
                
            float aspectRatio = (float)textureToShow.width / textureToShow.height;
            float previewWidth = showOriginalSize ? textureToShow.width : 256 * aspectRatio;
            float previewHeight = showOriginalSize ? textureToShow.height : 256;
            
            previewWidth *= zoomLevel;
            previewHeight *= zoomLevel;
            
            if (previewContainer != null)
            {
                previewContainer.style.width = previewWidth;
                previewContainer.style.height = previewHeight;
            }
            if (overlayContainer != null)
            {
                overlayContainer.style.width = previewWidth;
                overlayContainer.style.height = previewHeight;
            }
            
            Image sourceImage = new Image();
            sourceImage.image = textureToShow;
            sourceImage.style.width = previewWidth;
            sourceImage.style.height = previewHeight;
            sourceImage.style.position = Position.Absolute;
            sourceImage.style.left = panOffset.x;
            sourceImage.style.top = panOffset.y;
            
            if (previewContainer != null)
                previewContainer.Add(sourceImage);
            
            
            if (!isShowingPreview && activeCentroids.Count > 0 && resultTextures.Count > 0)
            {
                bool foundVisibleResult = false;
                
                if (!showLayered)
                {
                    foreach (string key in activeCentroids)
                    {
                        if (resultTextures.TryGetValue(key, out ScalingResult result) && result.IsVisible)
                        {
                            Texture2D displayTexture = result.ShowPreQuantization && result.PreQuantizationTexture != null 
                                ? result.PreQuantizationTexture 
                                : result.Texture;
                            
                            
                            if (displayTexture == null)
                            {
                                Debug.LogWarning($"Null texture for key: {key}");
                                continue;
                            }
                            
                            Image resultImage = new Image();
                            resultImage.image = displayTexture;
                            resultImage.style.width = previewWidth;
                            resultImage.style.height = previewHeight;
                            resultImage.style.position = Position.Absolute;
                            resultImage.style.left = panOffset.x;
                            resultImage.style.top = panOffset.y;
                            
                            if (previewContainer != null)
                                previewContainer.Add(resultImage);
                            
                            foundVisibleResult = true;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (string key in activeCentroids)
                    {
                        if (resultTextures.TryGetValue(key, out ScalingResult result) && result.IsVisible)
                        {
                            Texture2D displayTexture = result.ShowPreQuantization && result.PreQuantizationTexture != null 
                                ? result.PreQuantizationTexture 
                                : result.Texture;
                            
                            Image resultImage = new Image();
                            resultImage.image = displayTexture;
                            resultImage.style.width = previewWidth;
                            resultImage.style.height = previewHeight;
                            resultImage.style.position = Position.Absolute;
                            resultImage.style.left = panOffset.x;
                            resultImage.style.top = panOffset.y;
                            
                            if (overlayContainer != null)
                                overlayContainer.Add(resultImage);
                            foundVisibleResult = true;
                        }
                    }
                }
                
                if (!foundVisibleResult && !showLayered)
                {
                    Image resultImage = new Image();
                    resultImage.image = textureToShow;
                    resultImage.style.width = previewWidth;
                    resultImage.style.height = previewHeight;
                    resultImage.style.position = Position.Absolute;
                    resultImage.style.left = panOffset.x;
                    resultImage.style.top = panOffset.y;
                    
                    if (previewContainer != null)
                        previewContainer.Add(resultImage);
                }
            }
            
            if (originalSizeLabel != null)
                originalSizeLabel.text = $"Original Texture: {sourceTexture?.width ?? 0}x{sourceTexture?.height ?? 0}";
            
            if (resultSizeLabel != null)
            {
                if (resultTextures.Count > 0 && activeCentroids.Count > 0)
                {
                    string firstKey = activeCentroids[0];
                    if (resultTextures.TryGetValue(firstKey, out ScalingResult result))
                    {
                        Texture2D displayTexture = result.ShowPreQuantization && result.PreQuantizationTexture != null 
                            ? result.PreQuantizationTexture 
                            : result.Texture;
                        resultSizeLabel.text = $"Result Texture: {displayTexture.width}x{displayTexture.height}";
                    }
                    else
                    {
                        resultSizeLabel.text = "";
                    }
                }
                else
                {
                    resultSizeLabel.text = "";
                }
            }
        }

        #endregion

        #region Save Methods

        private void SaveActiveResult()
        {
            if (activeCentroids.Count == 0)
                return;
                
            string key = activeCentroids[0];
            if (resultTextures.TryGetValue(key, out ScalingResult result))
            {
                string defaultName;
                if (key.StartsWith("downscaled_"))
                {
                    defaultName = $"{result.Method}_Downscaled";
                }
                else if (key.StartsWith("palette_"))
                {
                    defaultName = $"Palette_{result.QuantizationInfo}";
                }
                else
                {
                    defaultName = $"QuantizedTexture_K{result.KValue}";
                }
                
                
                if (result.ShowPreQuantization && result.PreQuantizationTexture != null)
                    defaultName += "_PreQuant";
                
                Texture2D textureToSave = result.ShowPreQuantization && result.PreQuantizationTexture != null 
                    ? result.PreQuantizationTexture 
                    : result.Texture;
                
                Texture2D savedTexture = SaveTexture(textureToSave, defaultName);
                OnResultSaved?.Invoke(savedTexture);
            }
        }
        
        private void SaveAllResults()
        {
            string folderPath = EditorUtility.SaveFolderPanel("Save All Results", "", "");
            
            if (string.IsNullOrEmpty(folderPath))
                return;
                
            string projectPath = Application.dataPath;
            projectPath = projectPath.Substring(0, projectPath.Length - 7);
            
            string relativePath = "";
            if (folderPath.StartsWith(projectPath))
            {
                relativePath = folderPath.Substring(projectPath.Length);
                if (relativePath.StartsWith("/"))
                    relativePath = relativePath.Substring(1);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder within your Unity project.", "OK");
                return;
            }
            
            Dictionary<string, Texture2D> savedTextures = new Dictionary<string, Texture2D>();
            
            foreach (var kvp in resultTextures)
            {
                string key = kvp.Key;
                ScalingResult result = kvp.Value;
                
                string fileName;
                if (key.StartsWith("downscaled_"))
                {
                    fileName = $"{result.Method}_Downscaled.png";
                }
                else if (key.StartsWith("palette_"))
                {
                    fileName = $"Palette_{result.QuantizationInfo}.png";
                }
                else
                {
                    fileName = $"QuantizedTexture_K{result.KValue}.png";
                }
                
                
                SaveResultTexture(result.Texture, fileName, folderPath, relativePath, savedTextures, key);
                
                
                if (result.HasPostQuantization && result.PreQuantizationTexture != null)
                {
                    string preQuantFileName = System.IO.Path.GetFileNameWithoutExtension(fileName) + "_PreQuant.png";
                    SaveResultTexture(result.PreQuantizationTexture, preQuantFileName, folderPath, relativePath, savedTextures, key + "_PreQuant");
                }
            }
            
            AssetDatabase.Refresh();
            
            Dictionary<string, Texture2D> eventTextures = new Dictionary<string, Texture2D>();
            foreach (var kvp in savedTextures)
            {
                eventTextures.Add(kvp.Key, kvp.Value);
            }
            
            OnAllResultsSaved?.Invoke(eventTextures);
        }
        
        private void SaveResultTexture(Texture2D texture, string fileName, string folderPath, string relativePath, Dictionary<string, Texture2D> savedTextures, string key)
        {
            string fullPath = System.IO.Path.Combine(folderPath, fileName);
            string assetPath = System.IO.Path.Combine(relativePath, fileName);
            
            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }
            
            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, bytes);
            
            AssetDatabase.ImportAsset(assetPath);
            
            Texture2D savedAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (savedAsset != null)
            {
                savedTextures.Add(key, savedAsset);
            }
        }
        
        private Texture2D SaveTexture(Texture2D texture, string defaultName)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Scaled Texture",
                defaultName,
                "png",
                "Enter a file name to save the result."
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path);
                
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            
            return null;
        }

        #endregion

        #region Helper Classes

        #endregion
    }
}