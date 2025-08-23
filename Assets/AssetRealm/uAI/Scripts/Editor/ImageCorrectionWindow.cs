using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;

namespace UAI
{
    public class ImageCorrectionWindow : EditorWindow
    {
        private Texture2D originalTexture;
        private Texture2D workingTexture;
        private Texture2D previewTexture;

        private VisualElement imageContainer;
        private Label statusLabel;
        private Label placeholderLabel;
        private Button saveButton;
        private Button revertButton;

        // Store paths
        private string currentImagePath = "";
        private string currentImageFolder = "";

        // Basic adjustments
        private float hueShift = 0f;
        private float saturation = 1f;
        private float brightness = 1f;
        private float contrast = 1f;

        // New adjustments
        private float gamma = 1f;
        private float exposure = 0f;
        private float blackPoint = 0f;
        private float whitePoint = 1f;
        private float midPoint = 1f;
        private float temperature = 0f;
        private float tint = 0f;
        private bool grayscale = false;
        private bool invert = false;

        // UI Controls - Basic
        private Slider hueSlider;
        private Slider saturationSlider;
        private Slider brightnessSlider;
        private Slider contrastSlider;
        private FloatField hueField;
        private FloatField saturationField;
        private FloatField brightnessField;
        private FloatField contrastField;

        // UI Controls - New
        private Slider gammaSlider;
        private FloatField gammaField;
        private Slider exposureSlider;
        private FloatField exposureField;
        private Slider blackPointSlider;
        private FloatField blackPointField;
        private Slider whitePointSlider;
        private FloatField whitePointField;
        private Slider midPointSlider;
        private FloatField midPointField;
        private Slider temperatureSlider;
        private FloatField temperatureField;
        private Slider tintSlider;
        private FloatField tintField;
        private Toggle grayscaleToggle;
        private Toggle invertToggle;

  
         
        [MenuItem("Assets/uAI/Color Correction", false, 1041)]
        private static void ColorCorrectImage()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            // Debug.Log("Color Correction on: " + path);

            OpenWithImage(path);
        }

        // Validation method to control when the menu is shown
        [MenuItem("Assets/uAI/Color Correction", true)]
        private static bool ValidateColorCorrectImage()
        {
            Object selected = Selection.activeObject;
            if (selected == null)
                return false;

            string path = AssetDatabase.GetAssetPath(selected);
            string ext = Path.GetExtension(path).ToLower();

            // Only allow typical image formats
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".bmp" || ext == ".psd";
        }

        [MenuItem("Tools/uAI Creator/Image Correction", false, 60)]
        public static void ShowWindow()
        {
            var window = GetWindow<ImageCorrectionWindow>();
            window.titleContent = new GUIContent("Image Correction", EditorGUIUtility.IconContent("d_SceneViewFx").image);
            window.minSize = new Vector2(1000, 700);
        }

        public static void OpenWithImage(string imagePath)
        {
            var wnd = GetWindow<ImageCorrectionWindow>();
            wnd.titleContent = new GUIContent("Image Correction", EditorGUIUtility.IconContent("d_SceneViewFx").image);
            wnd.minSize = new Vector2(1000, 700);

            if (string.IsNullOrEmpty(imagePath))
            { 
                return;
            }
            wnd.LoadImageFromPath(imagePath);
        }


        private void CreateGUI()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("ImageCorrectionWindow");
            var styleSheet = Resources.Load<StyleSheet>("ImageCorrectionStyles");

            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
            }
            else
            {
                Debug.LogError("Could not find ImageCorrectionWindow in Resources folder");
                return;
            }

            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("Could not find ImageCorrectionStyles in Resources folder");
            }

            // Get UI references
            imageContainer = rootVisualElement.Q<VisualElement>("image-preview");
            statusLabel = rootVisualElement.Q<Label>("status-label");
            placeholderLabel = rootVisualElement.Q<Label>("placeholder-label");
            saveButton = rootVisualElement.Q<Button>("save-button");
            revertButton = rootVisualElement.Q<Button>("revert-button");

            // Get basic adjustment controls
            hueSlider = rootVisualElement.Q<Slider>("hue-slider");
            saturationSlider = rootVisualElement.Q<Slider>("saturation-slider");
            brightnessSlider = rootVisualElement.Q<Slider>("brightness-slider");
            contrastSlider = rootVisualElement.Q<Slider>("contrast-slider");

            hueField = rootVisualElement.Q<FloatField>("hue-field");
            saturationField = rootVisualElement.Q<FloatField>("saturation-field");
            brightnessField = rootVisualElement.Q<FloatField>("brightness-field");
            contrastField = rootVisualElement.Q<FloatField>("contrast-field");

            // Get new adjustment controls
            gammaSlider = rootVisualElement.Q<Slider>("gamma-slider");
            gammaField = rootVisualElement.Q<FloatField>("gamma-field");
            exposureSlider = rootVisualElement.Q<Slider>("exposure-slider");
            exposureField = rootVisualElement.Q<FloatField>("exposure-field");

            blackPointSlider = rootVisualElement.Q<Slider>("black-point-slider");
            blackPointField = rootVisualElement.Q<FloatField>("black-point-field");
            whitePointSlider = rootVisualElement.Q<Slider>("white-point-slider");
            whitePointField = rootVisualElement.Q<FloatField>("white-point-field");
            midPointSlider = rootVisualElement.Q<Slider>("mid-point-slider");
            midPointField = rootVisualElement.Q<FloatField>("mid-point-field");

            temperatureSlider = rootVisualElement.Q<Slider>("temperature-slider");
            temperatureField = rootVisualElement.Q<FloatField>("temperature-field");
            tintSlider = rootVisualElement.Q<Slider>("tint-slider");
            tintField = rootVisualElement.Q<FloatField>("tint-field");

            grayscaleToggle = rootVisualElement.Q<Toggle>("grayscale-toggle");
            invertToggle = rootVisualElement.Q<Toggle>("invert-toggle");

            // Setup button callbacks
            var loadButton = rootVisualElement.Q<Button>("load-button");
            loadButton.clicked += LoadImage;
            saveButton.clicked += SaveImage;
            revertButton.clicked += RevertChanges;

            // Setup basic adjustment callbacks
            hueSlider.RegisterValueChangedCallback(evt => { hueShift = evt.newValue; hueField.value = evt.newValue; ApplyEffects(); });
            saturationSlider.RegisterValueChangedCallback(evt => { saturation = evt.newValue; saturationField.value = evt.newValue; ApplyEffects(); });
            brightnessSlider.RegisterValueChangedCallback(evt => { brightness = evt.newValue; brightnessField.value = evt.newValue; ApplyEffects(); });
            contrastSlider.RegisterValueChangedCallback(evt => { contrast = evt.newValue; contrastField.value = evt.newValue; ApplyEffects(); });

            hueField.RegisterValueChangedCallback(evt => { hueShift = evt.newValue; hueSlider.value = evt.newValue; ApplyEffects(); });
            saturationField.RegisterValueChangedCallback(evt => { saturation = evt.newValue; saturationSlider.value = evt.newValue; ApplyEffects(); });
            brightnessField.RegisterValueChangedCallback(evt => { brightness = evt.newValue; brightnessSlider.value = evt.newValue; ApplyEffects(); });
            contrastField.RegisterValueChangedCallback(evt => { contrast = evt.newValue; contrastSlider.value = evt.newValue; ApplyEffects(); });

            // Setup new adjustment callbacks
            gammaSlider.RegisterValueChangedCallback(evt => { gamma = evt.newValue; gammaField.value = evt.newValue; ApplyEffects(); });
            gammaField.RegisterValueChangedCallback(evt => { gamma = evt.newValue; gammaSlider.value = evt.newValue; ApplyEffects(); });

            exposureSlider.RegisterValueChangedCallback(evt => { exposure = evt.newValue; exposureField.value = evt.newValue; ApplyEffects(); });
            exposureField.RegisterValueChangedCallback(evt => { exposure = evt.newValue; exposureSlider.value = evt.newValue; ApplyEffects(); });

            blackPointSlider.RegisterValueChangedCallback(evt => { blackPoint = evt.newValue; blackPointField.value = evt.newValue; ApplyEffects(); });
            blackPointField.RegisterValueChangedCallback(evt => { blackPoint = evt.newValue; blackPointSlider.value = evt.newValue; ApplyEffects(); });

            whitePointSlider.RegisterValueChangedCallback(evt => { whitePoint = evt.newValue; whitePointField.value = evt.newValue; ApplyEffects(); });
            whitePointField.RegisterValueChangedCallback(evt => { whitePoint = evt.newValue; whitePointSlider.value = evt.newValue; ApplyEffects(); });

            midPointSlider.RegisterValueChangedCallback(evt => { midPoint = evt.newValue; midPointField.value = evt.newValue; ApplyEffects(); });
            midPointField.RegisterValueChangedCallback(evt => { midPoint = evt.newValue; midPointSlider.value = evt.newValue; ApplyEffects(); });

            temperatureSlider.RegisterValueChangedCallback(evt => { temperature = evt.newValue; temperatureField.value = evt.newValue; ApplyEffects(); });
            temperatureField.RegisterValueChangedCallback(evt => { temperature = evt.newValue; temperatureSlider.value = evt.newValue; ApplyEffects(); });

            tintSlider.RegisterValueChangedCallback(evt => { tint = evt.newValue; tintField.value = evt.newValue; ApplyEffects(); });
            tintField.RegisterValueChangedCallback(evt => { tint = evt.newValue; tintSlider.value = evt.newValue; ApplyEffects(); });

            grayscaleToggle.RegisterValueChangedCallback(evt => { grayscale = evt.newValue; ApplyEffects(); });
            invertToggle.RegisterValueChangedCallback(evt => { invert = evt.newValue; ApplyEffects(); });

            // Setup drag and drop
            imageContainer.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            imageContainer.RegisterCallback<DragPerformEvent>(OnDragPerform);

            UpdateUI();
        }

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (DragAndDrop.paths.Length > 0 && IsImageFile(DragAndDrop.paths[0]))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (DragAndDrop.paths.Length > 0 && IsImageFile(DragAndDrop.paths[0]))
            {
                DragAndDrop.AcceptDrag();
                LoadImageFromPath(DragAndDrop.paths[0]);
            }
        }

        private bool IsImageFile(string path)
        {
            string extension = Path.GetExtension(path).ToLower();
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".tga" || extension == ".bmp";
        }

        private void LoadImage()
        {
            string path = EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg,tga,bmp");
            if (!string.IsNullOrEmpty(path))
            {
                LoadImageFromPath(path);
            }
        }

        public void LoadImageFromPath(string path)
        {
            // Store current paths
            currentImagePath = path;
            currentImageFolder = Path.GetDirectoryName(path);

            // Check if this is a Unity asset
            bool isUnityAsset = path.StartsWith(Application.dataPath);
            Texture2D sourceTexture = null;
            TextureImporter originalImporter = null;

            if (isUnityAsset)
            {
                // Convert to relative path for AssetDatabase
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                originalImporter = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            }

            if (sourceTexture == null)
            {
                // Load from file if not a Unity asset or loading failed
                byte[] fileData = File.ReadAllBytes(path);
                sourceTexture = new Texture2D(2, 2);
                sourceTexture.LoadImage(fileData);
            }

            // Create working textures
            originalTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            originalTexture.SetPixels(sourceTexture.GetPixels());
            originalTexture.Apply();

            workingTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
            workingTexture.SetPixels(originalTexture.GetPixels());
            workingTexture.Apply();

            previewTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
            previewTexture.SetPixels(originalTexture.GetPixels());
            previewTexture.Apply();

            ResetValues();
            UpdateUI();

            statusLabel.text = $"Loaded: {Path.GetFileName(path)} ({originalTexture.width}x{originalTexture.height})";
        }

        private void SaveImage()
        {
            if (previewTexture == null) return;

            string defaultName = "corrected_" + Path.GetFileNameWithoutExtension(currentImagePath);
            string path = EditorUtility.SaveFilePanel("Save Image", currentImageFolder, defaultName, "png");
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = previewTexture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);

                // Check if saved within Unity project
                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.Refresh();

                    // Copy import settings from original if it was a Unity asset
                    if (!string.IsNullOrEmpty(currentImagePath) && currentImagePath.StartsWith(Application.dataPath))
                    {
                        string originalRelativePath = "Assets" + currentImagePath.Substring(Application.dataPath.Length);
                        TextureImporter originalImporter = AssetImporter.GetAtPath(originalRelativePath) as TextureImporter;
                        TextureImporter newImporter = AssetImporter.GetAtPath(relativePath) as TextureImporter;

                        if (originalImporter != null && newImporter != null)
                        {
                            EditorUtility.CopySerialized(originalImporter, newImporter);
                            newImporter.SaveAndReimport();
                        }
                    }
                }

                statusLabel.text = $"Saved: {Path.GetFileName(path)}";

                // Update working texture
                workingTexture.SetPixels(previewTexture.GetPixels());
                workingTexture.Apply();
            }
        }

        private void RevertChanges()
        {
            if (originalTexture == null) return;

            previewTexture.SetPixels(originalTexture.GetPixels());
            previewTexture.Apply();

            ResetValues();
            UpdateUI();

            statusLabel.text = "Reverted to original";
        }

        private void ResetValues()
        {
            // Basic adjustments
            hueShift = 0f;
            saturation = 1f;
            brightness = 1f;
            contrast = 1f;

            // New adjustments
            gamma = 1f;
            exposure = 0f;
            blackPoint = 0f;
            whitePoint = 1f;
            midPoint = 1f;
            temperature = 0f;
            tint = 0f;
            grayscale = false;
            invert = false;

            // Update UI
            if (hueSlider != null) hueSlider.value = 0f;
            if (saturationSlider != null) saturationSlider.value = 1f;
            if (brightnessSlider != null) brightnessSlider.value = 1f;
            if (contrastSlider != null) contrastSlider.value = 1f;

            if (hueField != null) hueField.value = 0f;
            if (saturationField != null) saturationField.value = 1f;
            if (brightnessField != null) brightnessField.value = 1f;
            if (contrastField != null) contrastField.value = 1f;

            if (gammaSlider != null) gammaSlider.value = 1f;
            if (gammaField != null) gammaField.value = 1f;
            if (exposureSlider != null) exposureSlider.value = 0f;
            if (exposureField != null) exposureField.value = 0f;

            if (blackPointSlider != null) blackPointSlider.value = 0f;
            if (blackPointField != null) blackPointField.value = 0f;
            if (whitePointSlider != null) whitePointSlider.value = 1f;
            if (whitePointField != null) whitePointField.value = 1f;
            if (midPointSlider != null) midPointSlider.value = 1f;
            if (midPointField != null) midPointField.value = 1f;

            if (temperatureSlider != null) temperatureSlider.value = 0f;
            if (temperatureField != null) temperatureField.value = 0f;
            if (tintSlider != null) tintSlider.value = 0f;
            if (tintField != null) tintField.value = 0f;

            if (grayscaleToggle != null) grayscaleToggle.value = false;
            if (invertToggle != null) invertToggle.value = false;
        }

        private void ApplyEffects()
        {
            if (workingTexture == null || previewTexture == null) return;

            Color[] pixels = workingTexture.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];

                // 1. Apply Levels first
                pixel.r = ApplyLevels(pixel.r, blackPoint, whitePoint, midPoint);
                pixel.g = ApplyLevels(pixel.g, blackPoint, whitePoint, midPoint);
                pixel.b = ApplyLevels(pixel.b, blackPoint, whitePoint, midPoint);

                // 2. Apply Exposure
                if (exposure != 0f)
                {
                    float multiplier = Mathf.Pow(2f, exposure);
                    pixel.r = Mathf.Clamp01(pixel.r * multiplier);
                    pixel.g = Mathf.Clamp01(pixel.g * multiplier);
                    pixel.b = Mathf.Clamp01(pixel.b * multiplier);
                }

                // 3. Apply Gamma
                if (gamma != 1f)
                {
                    float invGamma = 1f / gamma;
                    pixel.r = Mathf.Pow(pixel.r, invGamma);
                    pixel.g = Mathf.Pow(pixel.g, invGamma);
                    pixel.b = Mathf.Pow(pixel.b, invGamma);
                }

                // 4. Apply Temperature and Tint
                if (temperature != 0f || tint != 0f)
                {
                    ApplyTemperatureTint(ref pixel, temperature, tint);
                }

                // 5. Convert to HSV for hue and saturation
                Color.RGBToHSV(pixel, out float h, out float s, out float v);

                // Apply hue shift
                h = (h + hueShift) % 1f;
                if (h < 0) h += 1f;

                // Apply saturation
                s = Mathf.Clamp01(s * saturation);

                // Convert back to RGB
                pixel = Color.HSVToRGB(h, s, v);
                pixel.a = pixels[i].a; // Preserve alpha

                // 6. Apply brightness
                pixel.r = Mathf.Clamp01(pixel.r * brightness);
                pixel.g = Mathf.Clamp01(pixel.g * brightness);
                pixel.b = Mathf.Clamp01(pixel.b * brightness);

                // 7. Apply contrast
                pixel.r = Mathf.Clamp01(((pixel.r - 0.5f) * contrast) + 0.5f);
                pixel.g = Mathf.Clamp01(((pixel.g - 0.5f) * contrast) + 0.5f);
                pixel.b = Mathf.Clamp01(((pixel.b - 0.5f) * contrast) + 0.5f);

                // 8. Apply grayscale
                if (grayscale)
                {
                    float gray = 0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b;
                    pixel.r = pixel.g = pixel.b = gray;
                }

                // 9. Apply invert (last)
                if (invert)
                {
                    pixel.r = 1f - pixel.r;
                    pixel.g = 1f - pixel.g;
                    pixel.b = 1f - pixel.b;
                }

                newPixels[i] = pixel;
            }

            previewTexture.SetPixels(newPixels);
            previewTexture.Apply();

            UpdateImageDisplay();
        }

        private float ApplyLevels(float value, float blackPt, float whitePt, float midPt)
        {
            // Remap the range
            float normalized = (value - blackPt) / (whitePt - blackPt);
            normalized = Mathf.Clamp01(normalized);

            // Apply midpoint adjustment (gamma-like)
            if (midPt != 1f)
            {
                normalized = Mathf.Pow(normalized, 1f / midPt);
            }

            return normalized;
        }

        private void ApplyTemperatureTint(ref Color pixel, float temp, float tnt)
        {
            // Temperature adjustment (blue-yellow axis)
            if (temp != 0f)
            {
                float tempAmount = temp * 0.01f;
                if (temp > 0f) // Warmer (more yellow/red)
                {
                    pixel.r = Mathf.Clamp01(pixel.r + tempAmount * 0.3f);
                    pixel.g = Mathf.Clamp01(pixel.g + tempAmount * 0.1f);
                    pixel.b = Mathf.Clamp01(pixel.b - tempAmount * 0.5f);
                }
                else // Cooler (more blue)
                {
                    pixel.r = Mathf.Clamp01(pixel.r + tempAmount * 0.5f);
                    pixel.g = Mathf.Clamp01(pixel.g + tempAmount * 0.1f);
                    pixel.b = Mathf.Clamp01(pixel.b - tempAmount * 0.3f);
                }
            }

            // Tint adjustment (green-magenta axis)
            if (tnt != 0f)
            {
                float tintAmount = tnt * 0.01f;
                if (tnt > 0f) // More magenta
                {
                    pixel.r = Mathf.Clamp01(pixel.r + tintAmount * 0.2f);
                    pixel.g = Mathf.Clamp01(pixel.g - tintAmount * 0.3f);
                    pixel.b = Mathf.Clamp01(pixel.b + tintAmount * 0.2f);
                }
                else // More green
                {
                    pixel.r = Mathf.Clamp01(pixel.r + tintAmount * 0.2f);
                    pixel.g = Mathf.Clamp01(pixel.g - tintAmount * 0.3f);
                    pixel.b = Mathf.Clamp01(pixel.b + tintAmount * 0.2f);
                }
            }
        }

        private void UpdateUI()
        {
            bool hasImage = originalTexture != null;

            if (saveButton != null) saveButton.SetEnabled(hasImage);
            if (revertButton != null) revertButton.SetEnabled(hasImage);

            // Enable/disable all controls
            if (hueSlider != null) hueSlider.SetEnabled(hasImage);
            if (saturationSlider != null) saturationSlider.SetEnabled(hasImage);
            if (brightnessSlider != null) brightnessSlider.SetEnabled(hasImage);
            if (contrastSlider != null) contrastSlider.SetEnabled(hasImage);

            if (hueField != null) hueField.SetEnabled(hasImage);
            if (saturationField != null) saturationField.SetEnabled(hasImage);
            if (brightnessField != null) brightnessField.SetEnabled(hasImage);
            if (contrastField != null) contrastField.SetEnabled(hasImage);

            if (gammaSlider != null) gammaSlider.SetEnabled(hasImage);
            if (gammaField != null) gammaField.SetEnabled(hasImage);
            if (exposureSlider != null) exposureSlider.SetEnabled(hasImage);
            if (exposureField != null) exposureField.SetEnabled(hasImage);

            if (blackPointSlider != null) blackPointSlider.SetEnabled(hasImage);
            if (blackPointField != null) blackPointField.SetEnabled(hasImage);
            if (whitePointSlider != null) whitePointSlider.SetEnabled(hasImage);
            if (whitePointField != null) whitePointField.SetEnabled(hasImage);
            if (midPointSlider != null) midPointSlider.SetEnabled(hasImage);
            if (midPointField != null) midPointField.SetEnabled(hasImage);

            if (temperatureSlider != null) temperatureSlider.SetEnabled(hasImage);
            if (temperatureField != null) temperatureField.SetEnabled(hasImage);
            if (tintSlider != null) tintSlider.SetEnabled(hasImage);
            if (tintField != null) tintField.SetEnabled(hasImage);

            if (grayscaleToggle != null) grayscaleToggle.SetEnabled(hasImage);
            if (invertToggle != null) invertToggle.SetEnabled(hasImage);

            UpdateImageDisplay();
        }

        private void UpdateImageDisplay()
        {
            if (imageContainer == null) return;

            if (previewTexture != null)
            {
                imageContainer.style.backgroundImage = new StyleBackground(previewTexture);
                imageContainer.style.unityBackgroundImageTintColor = Color.white;

                // Hide placeholder text
                if (placeholderLabel != null)
                    placeholderLabel.style.display = DisplayStyle.None;
            }
            else
            {
                imageContainer.style.backgroundImage = StyleKeyword.None;

                // Show placeholder text
                if (placeholderLabel != null)
                    placeholderLabel.style.display = DisplayStyle.Flex;
            }
        }
    }
}