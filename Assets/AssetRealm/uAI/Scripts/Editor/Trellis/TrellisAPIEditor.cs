using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System; 
using UnityEngine.Video;

namespace UAI
{
    // --- DATA MODELS ---
    [System.Serializable]
    public class TrellisHealthStatus
    {
        public string status;
        public bool pipeline_loaded;
        public bool cuda_available;
        public int active_sessions;
    }

    [System.Serializable]
    public class TrellisGenerationSettings
    {
        public int seed = 42;
        public bool randomize_seed = false;
        public float ss_guidance_strength = 7.5f;
        public int ss_sampling_steps = 12;
        public float slat_guidance_strength = 3.0f;
        public int slat_sampling_steps = 12;
        public string multiimage_algo = "stochastic";
    }

    [System.Serializable]
    public class TrellisGLBSettings
    {
        public float mesh_simplify = 0.95f;
        public int texture_size = 1024;
    }

    [System.Serializable]
    public class TrellisAPIResponse
    {
        public string session_id;
        public string message;
        public string status;
        public string video_path;
        public string download_url;
        public object detail;
    }

    [System.Serializable]
    public class TrellisProgressResponse
    {
        public string session_id;
        public int current_step;
        public int total_steps;
        public string stage;
        public string message;
        public float progress_percent;
        public string status;
        public bool video_ready;
        public string video_url;
    }

    public class TrellisAPIEditor : EditorWindow
    {
        // --- Configuration ---
        private string baseUrl = "http://localhost:8000";

        // --- UI Elements ---
        private VisualElement root;
        private TextField baseUrlField;
        private Label connectionStatus;
        private VisualElement imageList;
        private Button generateBtn;
        private VisualElement extractionSection;
        private Button extractGlbBtn;
        private Button extractPlyBtn;
        private Button extractFbxBtn;
        private VisualElement videoContainer;
        private VisualElement videoPlaceholder;
        private Label progressStage;
        private Label progressMessage;
        private ProgressBar progressBar;
        private Label progressPercent;
        private VisualElement consoleContent;
        private ScrollView consoleScroll;
        private Label statusLabel;

        // --- State ---
        private List<string> imagePaths = new List<string>();
        private List<Texture2D> imageTextures = new List<Texture2D>();
        private string currentSessionId = "";
        private bool isGenerating = false;
        private bool isConnected = false;

        // Video player for IMGUI rendering
        private IMGUIContainer videoIMGUIContainer;
        private bool shouldUpdateVideo = false;
        private VideoPlayer videoPlayer;
        private RenderTexture videoTexture;
        private bool videoReady = false;
        private string videoPath = "";

        // --- Settings ---
        private TrellisGenerationSettings generationSettings = new TrellisGenerationSettings();
        private TrellisGLBSettings glbSettings = new TrellisGLBSettings();

        
        [MenuItem("Assets/uAI/Open in 3D Creator", false, 1043)]
        private static void ColorCorrectImage()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject); 

            OpenWithImage(path);
        }

        // Validation method to control when the menu is shown
        [MenuItem("Assets/uAI/Open in 3D Creator", true)]
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

        [MenuItem("Tools/uAI Creator/3D Creator", false, 4)]
        public static void ShowWindow()
        {
            TrellisAPIEditor wnd = GetWindow<TrellisAPIEditor>();
            wnd.titleContent = new GUIContent("3D Creator");
            wnd.minSize = new Vector2(800, 600);
        }

        public static void OpenWithImage(string imagePath)
        {
            TrellisAPIEditor wnd = GetWindow<TrellisAPIEditor>();
            wnd.titleContent = new GUIContent("3D Creator");
            wnd.minSize = new Vector2(800, 600); 
            wnd.setImageRow(imagePath,0);
        }

        public void CreateGUI()
        {
            // Load UXML from Resources
            var visualTree = Resources.Load<VisualTreeAsset>("TrellisAPIEditorWindow");
            if (visualTree == null)
            {
                Debug.LogError("Failed to load TrellisAPIEditorWindow.uxml from Resources folder. Make sure it's in Assets/Editor/Resources/");
                return;
            }
            root = visualTree.Instantiate();
            rootVisualElement.Add(root);

            // Load USS from Resources
            var styleSheet = Resources.Load<StyleSheet>("TrellisAPIEditorStyle");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning("Failed to load TrellisAPIEditorStyle.uss from Resources folder. Continuing without custom styles.");
            }

            // Initialize UI elements
            InitializeUIElements();

            // Set up event handlers
            SetupEventHandlers();

            // Initialize state
            UpdateConnectionStatus(false);
            AddImageRow();
            UpdateExtractionUI(false);

            LogToConsole("Trellis API Editor initialized", "info");

            //try to connect to the server
            _ = HealthCheckAsync();
            UpdateConnectionStatus(isConnected);
        }

        private void InitializeUIElements()
        {
            // Get references to UI elements
            baseUrlField = root.Q<TextField>("base-url");
            connectionStatus = root.Q<Label>("connection-status");
            imageList = root.Q<VisualElement>("image-list");
            generateBtn = root.Q<Button>("generate-btn");
            extractionSection = root.Q<VisualElement>("extraction-section");
            extractGlbBtn = root.Q<Button>("extract-glb-btn");
            extractPlyBtn = root.Q<Button>("extract-ply-btn");
            extractFbxBtn = root.Q<Button>("extract-fbx-btn");
            videoContainer = root.Q<VisualElement>("video-container");
            videoPlaceholder = root.Q<VisualElement>("video-placeholder");
            progressStage = root.Q<Label>("progress-stage");
            progressMessage = root.Q<Label>("progress-message");
            progressBar = root.Q<ProgressBar>("progress-bar");
            progressPercent = root.Q<Label>("progress-percent");
            consoleContent = root.Q<VisualElement>("console-content");
            consoleScroll = root.Q<ScrollView>("console-scroll");
            statusLabel = root.Q<Label>("status-label");

            // Set initial values
            baseUrlField.value = baseUrl;
        }

        private void SetupEventHandlers()
        {
            // Connection
            root.Q<Button>("health-check-btn").clicked += () => _ = HealthCheckAsync();
            baseUrlField.RegisterValueChangedCallback(evt => baseUrl = evt.newValue);

            // Image management
            root.Q<Button>("add-image-btn").clicked += AddImageRow;

            // Generation
            generateBtn.clicked += () => _ = GenerateModelAsync();

            // Extraction
            extractGlbBtn.clicked += () => _ = ExtractGLBAsync();
            extractPlyBtn.clicked += () => _ = ExtractPLYAsync();
            extractFbxBtn.clicked += () => _ = ExtractFBXAsync();

            // Console
            root.Q<Button>("clear-console-btn").clicked += ClearConsole;

            // Settings change handlers
            SetupSettingsHandlers();
        }

        private void SetupSettingsHandlers()
        {
            // Initialize dropdown choices
            var multiImageDropdown = root.Q<DropdownField>("multiimage-algo");
            multiImageDropdown.choices = new List<string> { "stochastic", "multidiffusion" };
            multiImageDropdown.index = 0;

            // Generation settings
            root.Q<IntegerField>("seed").RegisterValueChangedCallback(evt => generationSettings.seed = evt.newValue);
            root.Q<Toggle>("randomize-seed").RegisterValueChangedCallback(evt => generationSettings.randomize_seed = evt.newValue);
            root.Q<Slider>("ss-guidance").RegisterValueChangedCallback(evt => generationSettings.ss_guidance_strength = evt.newValue);
            root.Q<SliderInt>("ss-steps").RegisterValueChangedCallback(evt => generationSettings.ss_sampling_steps = evt.newValue);
            root.Q<Slider>("slat-guidance").RegisterValueChangedCallback(evt => generationSettings.slat_guidance_strength = evt.newValue);
            root.Q<SliderInt>("slat-steps").RegisterValueChangedCallback(evt => generationSettings.slat_sampling_steps = evt.newValue);
            multiImageDropdown.RegisterValueChangedCallback(evt => generationSettings.multiimage_algo = evt.newValue);

            // Extraction settings
            root.Q<Slider>("mesh-simplify").RegisterValueChangedCallback(evt => glbSettings.mesh_simplify = evt.newValue);
            root.Q<SliderInt>("texture-size").RegisterValueChangedCallback(evt => glbSettings.texture_size = evt.newValue);
        }

        private void AddImageRow()
        {
            var imageItem = new VisualElement();
            imageItem.AddToClassList("image-item");

            // Header with controls
            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("image-item-header");

            var imageField = new TextField();
            imageField.AddToClassList("image-path");
            imageField.style.flexGrow = 1;
            imageField.style.flexShrink = 1;

            imageField.label = $"Image {imagePaths.Count + 1}";

            var browseBtn = new Button(() => BrowseForImage(imageItem));
            browseBtn.text = "...";
            browseBtn.AddToClassList("image-browse-btn");

            var removeBtn = new Button(() => RemoveImageRow(imageItem));
            removeBtn.text = "-";
            removeBtn.AddToClassList("image-remove-btn");

            headerContainer.Add(imageField);
            headerContainer.Add(browseBtn);
            headerContainer.Add(removeBtn);

            // Image preview
            var previewContainer = new VisualElement();
            previewContainer.AddToClassList("image-preview");

            var previewContent = new VisualElement();
            previewContent.AddToClassList("image-preview-content");
            previewContent.name = "preview-content"; 

            var placeholderLabel = new Label("No image selected");
            placeholderLabel.AddToClassList("image-preview-placeholder");
            placeholderLabel.name = "placeholder-label"; 
            previewContent.Add(placeholderLabel);

            previewContainer.Add(previewContent);

            imageItem.Add(headerContainer);
            imageItem.Add(previewContainer);

            imageList.Add(imageItem);
            imagePaths.Add("");
            imageTextures.Add(null);

            // Store references to avoid index issues
            imageField.RegisterValueChangedCallback(evt =>
            {
                var index = imageList.IndexOf(imageItem);
                if (index >= 0 && index < imagePaths.Count)
                {
                    imagePaths[index] = evt.newValue;
                    LoadImagePreview(evt.newValue, imageItem, index);
                    // Update generate button state when image paths change
                    generateBtn.SetEnabled(isConnected && !isGenerating && HasValidImages());
                }
            });
        }

        private void setImageRow(string imagePath, int index)
        {
            if (index < 0 || index >= imageList.childCount)
            {
                Debug.LogError($"Invalid index {index} for image row");
                return;
            }

            var imageItem = imageList[index];
            var headerContainer = imageItem.Q<VisualElement>(className: "image-item-header");
            var imageField = headerContainer?.Q<TextField>();

            if (imageField != null)
            {
                imageField.value = imagePath;
                imagePaths[index] = imagePath; 
                LoadImagePreview(imagePath, imageItem, index);
            }
            else
            {
                Debug.LogError("Could not find TextField in image item header");
            }
        }

        private void LoadImagePreview(string imagePath, VisualElement imageItem, int index)
        {
            // Find preview elements with better querying
            var previewContent = imageItem.Q<VisualElement>("preview-content");
            var placeholderLabel = imageItem.Q<Label>("placeholder-label");

            if (previewContent == null || placeholderLabel == null)
            {
                Debug.LogError("Could not find preview elements in image item");
                return;
            }

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                // Show placeholder
                previewContent.style.backgroundImage = StyleKeyword.None;
                placeholderLabel.text = string.IsNullOrEmpty(imagePath) ? "No image selected" : "Image not found";
                placeholderLabel.style.display = DisplayStyle.Flex;

                // Clear stored texture
                if (index < imageTextures.Count && imageTextures[index] != null)
                {
                    DestroyImmediate(imageTextures[index]);
                    imageTextures[index] = null;
                }
                return;
            }

            try
            {  
                // Load image as texture
                byte[] fileData = File.ReadAllBytes(imagePath);
                var texture = new Texture2D(2, 2);

                if (texture.LoadImage(fileData))
                {  
                    // Store texture
                    if (index < imageTextures.Count)
                    {
                        if (imageTextures[index] != null)
                        {
                            DestroyImmediate(imageTextures[index]);
                        }
                        imageTextures[index] = texture;
                    }

                    // Apply to preview
                    previewContent.style.backgroundImage = Background.FromTexture2D(texture);
                    // previewContent.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    previewContent.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    placeholderLabel.style.display = DisplayStyle.None; 
                }
                else
                {
                    Debug.LogError("Failed to load image data into texture");
                    placeholderLabel.text = "Invalid image format";
                    placeholderLabel.style.display = DisplayStyle.Flex;
                    DestroyImmediate(texture);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading image preview: {e.Message}");
                placeholderLabel.text = "Error loading image";
                placeholderLabel.style.display = DisplayStyle.Flex;
            }
        }

        private void BrowseForImage(VisualElement imageItem)
        {
            string path = EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path))
            {
                // Find the index of this item
                var index = imageList.IndexOf(imageItem);
                if (index >= 0 && index < imagePaths.Count)
                {
                    // Update the text field
                    var headerContainer = imageItem.Q<VisualElement>(className: "image-item-header");
                    var imageField = headerContainer.Q<TextField>();
                    if (imageField != null)
                    {
                        imageField.value = path;
                    }

                    // Update the path and preview directly
                    imagePaths[index] = path;
                    LoadImagePreview(path, imageItem, index);

                    // Update generate button state
                    generateBtn.SetEnabled(isConnected && !isGenerating && HasValidImages());
                }
            }
        }

        private void RemoveImageRow(VisualElement imageItem)
        {
            var index = imageList.IndexOf(imageItem);
            if (index >= 0 && index < imagePaths.Count)
            {
                // Clean up texture
                if (index < imageTextures.Count && imageTextures[index] != null)
                {
                    DestroyImmediate(imageTextures[index]);
                    imageTextures.RemoveAt(index);
                }

                imagePaths.RemoveAt(index);
                imageList.Remove(imageItem);
                UpdateImageLabels();

                // Update generate button state after removing image
                generateBtn.SetEnabled(isConnected && !isGenerating && HasValidImages());
            }
        }

        private void UpdateImageLabels()
        {
            for (int i = 0; i < imageList.childCount; i++)
            {
                var imageItem = imageList[i];
                var headerContainer = imageItem.Q<VisualElement>(className: "image-item-header");
                var imageField = headerContainer?.Q<TextField>();
                if (imageField != null)
                {
                    imageField.label = $"Image {i + 1}";
                }
            }
        }

        private void UpdateConnectionStatus(bool connected)
        {
            isConnected = connected;
            connectionStatus.text = connected ? "●" : "●";
            connectionStatus.RemoveFromClassList(connected ? "disconnected" : "connected");
            connectionStatus.AddToClassList(connected ? "connected" : "disconnected");

            generateBtn.SetEnabled(connected && !isGenerating && HasValidImages());
        }

        private bool HasValidImages()
        {
            // Check if we have at least one valid image path
            for (int i = 0; i < imagePaths.Count; i++)
            {
                if (!string.IsNullOrEmpty(imagePaths[i]) && File.Exists(imagePaths[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateExtractionUI(bool canExtract)
        {
            extractionSection.SetEnabled(canExtract);
            if (canExtract)
            {
                extractionSection.RemoveFromClassList("disabled");
            }
            else
            {
                extractionSection.AddToClassList("disabled");
            }
        }

        private void LogToConsole(string message, string type = "info")
        {
            var logElement = new Label(message);
            logElement.AddToClassList("console-message");
            logElement.AddToClassList(type);

            consoleContent.Add(logElement);

            // Auto-scroll to bottom
            EditorApplication.delayCall += () =>
            {
                consoleScroll.verticalScroller.value = consoleScroll.verticalScroller.highValue;
            };

            // Also log to Unity console
            switch (type)
            {
                case "error":
                    Debug.LogError($"TrellisAPI: {message}");
                    break;
                case "warning":
                    Debug.LogWarning($"TrellisAPI: {message}");
                    break;
                default:
                    // Debug.Log($"TrellisAPI: {message}");
                    break;
            }
        }

        private void ClearConsole()
        {
            consoleContent.Clear();
        }

        private void SetStatus(string message)
        {
            statusLabel.text = message;
        }

        private async Task HealthCheckAsync()
        {
            LogToConsole("Checking server connection...", "info");
            SetStatus("Checking connection...");

            string url = $"{baseUrl.TrimEnd('/')}/health";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var health = JsonConvert.DeserializeObject<TrellisHealthStatus>(request.downloadHandler.text);
                    UpdateConnectionStatus(true);
                    LogToConsole($"Server connected: Pipeline={health.pipeline_loaded}, CUDA={health.cuda_available}", "success");
                    SetStatus($"Connected - Sessions: {health.active_sessions}");
                }
                else
                {
                    UpdateConnectionStatus(false);
                    LogToConsole($"Connection failed: {request.error}", "error");
                    SetStatus("Connection failed");
                }
            }
        }

        private async Task GenerateModelAsync()
        {
            if (!HasValidImages())
            {
                LogToConsole("Please select at least one valid image file", "error");
                return;
            }

            // DEBUG: Log current settings before sending
            LogToConsole("=== DEBUGGING SETTINGS TRANSMISSION ===", "info");
            LogToConsole($"Current generationSettings.ss_sampling_steps: {generationSettings.ss_sampling_steps}", "info");
            LogToConsole($"Current generationSettings.slat_sampling_steps: {generationSettings.slat_sampling_steps}", "info");
            LogToConsole($"Current generationSettings.ss_guidance_strength: {generationSettings.ss_guidance_strength}", "info");
            LogToConsole($"Current generationSettings.slat_guidance_strength: {generationSettings.slat_guidance_strength}", "info");
            LogToConsole($"Current generationSettings.seed: {generationSettings.seed}", "info");
            LogToConsole($"Current generationSettings.randomize_seed: {generationSettings.randomize_seed}", "info");
            LogToConsole($"Current generationSettings.multiimage_algo: {generationSettings.multiimage_algo}", "info");

            LogToConsole("Starting 3D model generation...", "info");
            SetStatus("Generating model...");
            isGenerating = true;
            generateBtn.SetEnabled(false);
            UpdateExtractionUI(false);

            try
            {
                string url = $"{baseUrl.TrimEnd('/')}/generate_3d";
                List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

                // Add images
                var validPaths = imagePaths.FindAll(p => !string.IsNullOrEmpty(p) && File.Exists(p));
                foreach (string imgPath in validPaths)
                {
                    byte[] fileData = File.ReadAllBytes(imgPath);
                    formData.Add(new MultipartFormFileSection("images", fileData, Path.GetFileName(imgPath), "image/png"));
                }

                // Add settings as individual form fields (NOT as a JSON file)
                formData.Add(new MultipartFormDataSection("seed", generationSettings.seed.ToString()));
                formData.Add(new MultipartFormDataSection("randomize_seed", generationSettings.randomize_seed.ToString().ToLower()));
                formData.Add(new MultipartFormDataSection("ss_guidance_strength", generationSettings.ss_guidance_strength.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)));
                formData.Add(new MultipartFormDataSection("ss_sampling_steps", generationSettings.ss_sampling_steps.ToString()));
                formData.Add(new MultipartFormDataSection("slat_guidance_strength", generationSettings.slat_guidance_strength.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)));
                formData.Add(new MultipartFormDataSection("slat_sampling_steps", generationSettings.slat_sampling_steps.ToString()));
                formData.Add(new MultipartFormDataSection("multiimage_algo", generationSettings.multiimage_algo));

                // DEBUG: Log the form data being sent
                LogToConsole($"=== FORM DATA BEING SENT ===", "info");
                foreach (var field in formData)
                {
                    if (field is MultipartFormDataSection dataSection)
                    {
                        LogToConsole($"{dataSection.sectionName}: {System.Text.Encoding.UTF8.GetString(dataSection.sectionData)}", "info");
                    }
                    else if (field is MultipartFormFileSection fileSection)
                    {
                        LogToConsole($"{fileSection.sectionName}: [FILE] {fileSection.fileName}", "info");
                    }
                }
                LogToConsole("=== END FORM DATA ===", "info");

                using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
                {
                    // DEBUG: Log the request details
                    LogToConsole($"Sending POST request to: {url}", "info");
                    LogToConsole($"Form data sections count: {formData.Count}", "info");

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<TrellisAPIResponse>(request.downloadHandler.text);
                        currentSessionId = response.session_id;
                        LogToConsole($"Generation started - Session: {currentSessionId}", "success");

                        // DEBUG: Log the full response
                        LogToConsole($"=== SERVER RESPONSE ===", "info");
                        LogToConsole(request.downloadHandler.text, "info");
                        LogToConsole("=== END SERVER RESPONSE ===", "info");

                        // Start progress monitoring
                        _ = MonitorProgressAsync();
                    }
                    else
                    {
                        HandleError(request, "Generation failed");
                        isGenerating = false;
                        generateBtn.SetEnabled(true);
                    }
                }
            }
            catch (Exception e)
            {
                LogToConsole($"Generation error: {e.Message}", "error");
                isGenerating = false;
                generateBtn.SetEnabled(true);
            }
        }



        private async Task MonitorProgressAsync()
        {
            while (isGenerating && !string.IsNullOrEmpty(currentSessionId))
            {
                try
                {
                    string url = $"{baseUrl.TrimEnd('/')}/progress/{currentSessionId}";
                    using (UnityWebRequest request = UnityWebRequest.Get(url))
                    {
                        await request.SendWebRequest();

                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            var progress = JsonConvert.DeserializeObject<TrellisProgressResponse>(request.downloadHandler.text);

                            // Update UI on main thread
                            EditorApplication.delayCall += () => UpdateProgressUI(progress);

                            if (progress.status == "completed")
                            {
                                LogToConsole("Generation completed successfully!", "success");
                                isGenerating = false;
                                generateBtn.SetEnabled(true);
                                UpdateExtractionUI(true);

                                if (progress.video_ready && !string.IsNullOrEmpty(progress.video_url))
                                {
                                    _ = LoadVideoAsync(progress.video_url);
                                }
                                break;
                            }
                            else if (progress.status == "error")
                            {
                                LogToConsole($"Generation failed: {progress.message}", "error");
                                isGenerating = false;
                                generateBtn.SetEnabled(true);
                                break;
                            }
                        }
                        else
                        {
                            LogToConsole($"Progress check failed: {request.error}", "warning");
                        }
                    }
                }
                catch (Exception e)
                {
                    LogToConsole($"Progress monitoring error: {e.Message}", "error");
                }

                // Wait before next check
                await Task.Delay(2000);
            }
        }

        private void UpdateProgressUI(TrellisProgressResponse progress)
        {
            progressStage.text = progress.stage;
            progressMessage.text = progress.message;
            progressBar.value = progress.progress_percent;
            progressPercent.text = $"{progress.progress_percent:F1}%";

            SetStatus($"{progress.stage}: {progress.progress_percent:F1}%");

            // Log progress milestones
            if (progress.current_step % 10 == 0 || progress.status == "completed")
            {
                LogToConsole($"{progress.stage}: {progress.message} ({progress.progress_percent:F1}%)", "info");
            }
        }

        private async Task LoadVideoAsync(string videoUrl)
        {
            LogToConsole("Loading video preview...", "info");

            try
            {
                string url = $"{baseUrl.TrimEnd('/')}{videoUrl}";
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Create unique filename to avoid conflicts
                        string fileName = $"trellis_preview_{System.DateTime.Now.Ticks}.mp4";
                        string tempPath = Path.Combine(Application.temporaryCachePath, fileName);

                        // Write file with proper disposal
                        using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await fileStream.WriteAsync(request.downloadHandler.data, 0, request.downloadHandler.data.Length);
                            await fileStream.FlushAsync();
                        } // FileStream is disposed here, releasing the lock

                        LogToConsole($"Video saved to: {tempPath}", "info");

                        // Add a small delay to ensure file handle is completely released
                        await Task.Delay(500);

                        // Verify file exists and is accessible
                        if (File.Exists(tempPath))
                        {
                            try
                            {
                                // Test if we can read the file
                                using (var testStream = File.OpenRead(tempPath))
                                {
                                    LogToConsole($"File is accessible, size: {testStream.Length} bytes", "info");
                                }

                                // Setup video player on main thread
                                EditorApplication.delayCall += () => SetupVideoPlayer(tempPath);
                                LogToConsole("Video preview loaded successfully", "success");
                            }
                            catch (Exception accessEx)
                            {
                                LogToConsole($"File access test failed: {accessEx.Message}", "error");

                                // Try with file:// URL as alternative
                                EditorApplication.delayCall += () => SetupVideoPlayer($"file://{tempPath}");
                            }
                        }
                        else
                        {
                            LogToConsole("Video file was not created properly", "error");
                        }
                    }
                    else
                    {
                        LogToConsole($"Failed to load video: {request.error}", "error");
                    }
                }
            }
            catch (Exception e)
            {
                LogToConsole($"Video loading error: {e.Message}", "error");
            }
        }
        private void CleanupVideoResources()
        {
            LogToConsole("=== CLEANING UP VIDEO RESOURCES ===", "info");

            // Stop update loop
            StopVideoUpdates();

            // Clean up VideoPlayer
            if (videoPlayer != null)
            {
                LogToConsole("Destroying existing VideoPlayer", "info");
                videoPlayer.Stop();
                videoPlayer.targetTexture = null;
                DestroyImmediate(videoPlayer.gameObject);
                videoPlayer = null;
            }

            // Clean up RenderTexture
            if (videoTexture != null)
            {
                LogToConsole("Releasing existing RenderTexture", "info");
                videoTexture.Release();
                DestroyImmediate(videoTexture);
                videoTexture = null;
            }

            // Reset state
            videoReady = false;
            shouldUpdateVideo = false;

            // Clear UI container
            if (videoIMGUIContainer != null)
            {
                videoContainer?.Remove(videoIMGUIContainer);
                videoIMGUIContainer = null;
            }

            // Force garbage collection to free up memory
            System.GC.Collect();

            LogToConsole("Video resources cleaned up", "success");
        }


        private void SetupVideoPlayer(string localVideoPath)
        {
            CleanupVideoResources();

            videoPath = localVideoPath;
            var totalStartTime = System.DateTime.Now;
            LogToConsole($"=== TIMING: SetupVideoPlayer started at {totalStartTime:HH:mm:ss.fff} ===", "info");

            if (videoPlaceholder != null)
            {
                videoPlaceholder.style.display = DisplayStyle.None;
            }

            // Create IMGUIContainer first
            videoIMGUIContainer = new IMGUIContainer(() =>
            {
                var rect = GUILayoutUtility.GetRect(512, 256, GUILayout.ExpandWidth(true));

                if (videoReady && videoTexture != null && videoPlayer != null)
                {
                    GUI.DrawTexture(rect, videoTexture, ScaleMode.ScaleToFit);

                    var controlRect = new Rect(rect.x, rect.y + rect.height - 30, rect.width, 25);
                    GUI.BeginGroup(controlRect);

                    if (GUI.Button(new Rect(5, 0, 60, 20), videoPlayer.isPlaying ? "Pause" : "Play"))
                    {
                        if (videoPlayer.isPlaying)
                        {
                            videoPlayer.Pause();
                            shouldUpdateVideo = false;
                        }
                        else
                        {
                            videoPlayer.Play();
                            shouldUpdateVideo = true;
                        }
                    }

                    if (GUI.Button(new Rect(70, 0, 60, 20), "Restart"))
                    {
                        videoPlayer.time = 0;
                        videoPlayer.Play();
                        shouldUpdateVideo = true;
                    }

                    GUI.Label(new Rect(140, 0, 100, 20), $"Frame: {videoPlayer.frame}");
                    GUI.EndGroup();
                }
                else
                {
                    GUI.Box(rect, "");
                    var labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                    var elapsed = (System.DateTime.Now - totalStartTime).TotalSeconds;
                    GUI.Label(rect, $"Loading Video... {elapsed:F1}s", labelStyle);
                }
            });

            videoIMGUIContainer.style.height = 280;
            videoIMGUIContainer.style.backgroundColor = new StyleColor(Color.black);
            videoContainer.Clear();
            videoContainer.Add(videoIMGUIContainer);

            // Create VideoPlayer with optimized settings
            if (videoPlayer == null)
            {
                LogToConsole("TIMING: Creating optimized VideoPlayer...", "info");
                var videoGameObject = new GameObject("TrellisVideoPlayer");
                videoGameObject.hideFlags = HideFlags.DontSave;
                videoPlayer = videoGameObject.AddComponent<VideoPlayer>();

                // **PERFORMANCE OPTIMIZATIONS**
                // Use smaller texture for faster preparation
                videoTexture = new RenderTexture(512, 256, 0, RenderTextureFormat.RGB565);
                videoTexture.Create();
                videoPlayer.targetTexture = videoTexture;

                // Optimize VideoPlayer settings for SPEED
                videoPlayer.isLooping = true;
                videoPlayer.playOnAwake = false;
                videoPlayer.skipOnDrop = true;
                videoPlayer.waitForFirstFrame = false;     // Don't wait for first frame
                videoPlayer.sendFrameReadyEvents = false;  // Disable events
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None; // Disable audio processing

                // **CRITICAL**: Set source to VideoUrl (not VideoClip)
                videoPlayer.source = VideoSource.Url;
            }

            // Set up event handlers
            videoPlayer.errorReceived += (player, message) =>
            {
                var elapsed = (System.DateTime.Now - totalStartTime).TotalSeconds;
                LogToConsole($"TIMING: VideoPlayer error after {elapsed:F1}s total: {message}", "error");
            };

            videoPlayer.prepareCompleted += (player) =>
            {
                var elapsed = (System.DateTime.Now - totalStartTime).TotalSeconds;
                LogToConsole($"TIMING: *** TOTAL TIME TO PREPARE: {elapsed:F1}s ***", "success");
                LogToConsole($"Video info: {player.width}x{player.height}, {player.frameCount} frames", "info");

                player.Play();
                videoReady = true;
                shouldUpdateVideo = true;
                StartVideoUpdates();
            };

            // **FIX 1**: Use file:// URL format for better compatibility
            string fileUrl = $"file://{localVideoPath.Replace('\\', '/')}";
            LogToConsole($"TIMING: Setting VideoPlayer URL to: {fileUrl}", "info");

            var prepareStartTime = System.DateTime.Now;
            videoPlayer.url = fileUrl;

            // **FIX 2**: Add a small delay before Prepare() to ensure file handle is released
            EditorApplication.delayCall += () =>
            {
                var urlSetTime = System.DateTime.Now;
                LogToConsole($"TIMING: *** CALLING VideoPlayer.Prepare() NOW *** at {urlSetTime:HH:mm:ss.fff}", "info");
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

                // videoPlayer.Prepare();
                videoPlayer.Play();
                var prepareCallTime = System.DateTime.Now;
                LogToConsole($"TIMING: Prepare() call returned in {(prepareCallTime - urlSetTime).TotalMilliseconds:F0}ms", "info");
                LogToConsole($"TIMING: *** NOW WAITING FOR prepareCompleted EVENT ***", "info");
            };
        }
        private void StartVideoUpdates()
        {
            // Hook into Unity's update loop to force video refreshes
            EditorApplication.update += ForceVideoUpdate;
            LogToConsole("Video update loop started", "info");
        }

        private void StopVideoUpdates()
        {
            // Unhook from update loop
            EditorApplication.update -= ForceVideoUpdate;
            shouldUpdateVideo = false;
            LogToConsole("Video update loop stopped", "info");
        }

        private void ForceVideoUpdate()
        {
            // Only update if we should and the container exists
            if (shouldUpdateVideo && videoIMGUIContainer != null && videoPlayer != null && videoPlayer.isPlaying)
            {
                // Force the IMGUIContainer to redraw
                videoIMGUIContainer.MarkDirtyRepaint();
            }
        }

        void OnGUI()
        {
            // Only render video if we have one and it's ready
            if (videoReady && videoTexture != null && videoPlayer != null && videoPlayer.isPlaying)
            {
                // Get the video container position in screen coordinates
                var videoContainer = root?.Q<VisualElement>("video-container");
                if (videoContainer != null)
                {
                    var containerRect = videoContainer.worldBound;

                    // Convert to screen coordinates (IMGUI uses screen space from top-left)
                    var screenRect = new Rect(
                        containerRect.x,
                        containerRect.y,
                        containerRect.width,
                        containerRect.height
                    );

                    // Render video texture in IMGUI
                    GUI.DrawTexture(screenRect, videoTexture, ScaleMode.ScaleToFit);

                    // Add simple controls overlay
                    var controlsRect = new Rect(screenRect.x + 10, screenRect.y + screenRect.height - 40, 200, 30);
                    GUILayout.BeginArea(controlsRect);
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button(videoPlayer.isPlaying ? "Pause" : "Play", GUILayout.Width(50)))
                    {
                        if (videoPlayer.isPlaying)
                            videoPlayer.Pause();
                        else
                            videoPlayer.Play();
                    }

                    if (GUILayout.Button("Restart", GUILayout.Width(60)))
                    {
                        videoPlayer.time = 0;
                        videoPlayer.Play();
                    }

                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(videoPath);
                        }
                        catch (Exception e)
                        {
                            LogToConsole($"Error opening video: {e.Message}", "error");
                        }
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                }
            }
        }

        private async Task ExtractGLBAsync()
        {
            if (string.IsNullOrEmpty(currentSessionId))
            {
                LogToConsole("No active session for extraction", "error");
                return;
            }

            LogToConsole("Extracting GLB model...", "info");
            extractGlbBtn.SetEnabled(false);

            try
            {
                string url = $"{baseUrl.TrimEnd('/')}/extract_glb?session_id={currentSessionId}";
                string jsonData = JsonConvert.SerializeObject(glbSettings);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<TrellisAPIResponse>(request.downloadHandler.text);
                        LogToConsole("GLB extraction completed", "success");

                        // Auto-download the file
                        await DownloadFileAsync("model.glb");
                    }
                    else
                    {
                        HandleError(request, "GLB extraction failed");
                    }
                }
            }
            catch (Exception e)
            {
                LogToConsole($"GLB extraction error: {e.Message}", "error");
            }
            finally
            {
                extractGlbBtn.SetEnabled(true);
            }
        }
        private async Task ExtractFBXAsync()
        {
            if (string.IsNullOrEmpty(currentSessionId))
            {
                LogToConsole("No active session for FBX extraction", "error");
                return;
            }

            LogToConsole("Extracting FBX model…", "info");
            extractFbxBtn.SetEnabled(false);

            try
            {
                string url = $"{baseUrl.TrimEnd('/')}/extract_fbx?session_id={currentSessionId}";
                string jsonData = JsonConvert.SerializeObject(glbSettings);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<TrellisAPIResponse>(request.downloadHandler.text);
                        LogToConsole("FBX extraction completed", "success");

                        // Download FBX and textures together
                        await DownloadFBXWithTexturesAsync();
                    }
                    else
                    {
                        HandleError(request, "FBX extraction failed");
                    }
                }
            }
            catch (Exception e)
            {
                LogToConsole($"FBX extraction error: {e.Message}", "error");
            }
            finally
            {
                extractFbxBtn.SetEnabled(true);
            }
        }

        private async Task DownloadFBXWithTexturesAsync()
        {
            // Let user choose directory
            string saveDir = EditorUtility.SaveFolderPanel("Save FBX and Textures", "", "");
            if (string.IsNullOrEmpty(saveDir))
            {
                LogToConsole("Download cancelled", "warning");
                return;
            }

            LogToConsole($"Downloading FBX and textures to: {saveDir}", "info");

            try
            {
                // Download FBX
                string fbxUrl = $"{baseUrl.TrimEnd('/')}/download/{currentSessionId}/model.fbx";
                string fbxPath = Path.Combine(saveDir, "model.fbx");

                using (UnityWebRequest request = UnityWebRequest.Get(fbxUrl))
                {
                    await request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(fbxPath, request.downloadHandler.data);
                        LogToConsole("FBX downloaded", "success");
                    }
                    else
                    {
                        HandleError(request, "FBX download failed");
                        return;
                    }
                }

                // Download texture - it's in the textures subfolder
                string textureUrl = $"{baseUrl.TrimEnd('/')}/download/{currentSessionId}/textures/Image_0.png";
                string texturePath = Path.Combine(saveDir, "Image_0.png");

                using (UnityWebRequest request = UnityWebRequest.Get(textureUrl))
                {
                    await request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(texturePath, request.downloadHandler.data);
                        LogToConsole("Texture Image_0.png downloaded", "success");
                    }
                    else
                    {
                        LogToConsole($"Failed to download texture: {request.error}", "warning");
                    }
                }

                LogToConsole("FBX and texture downloaded successfully", "success");

                // Check if we're in the Assets folder for auto-import
                if (saveDir.StartsWith(Application.dataPath))
                {
                    AssetDatabase.Refresh();
                    LogToConsole("Assets imported to Unity", "success");
                    
                    // Automatically set bake axis conversion for this specific Trellis FBX file
                    string relativeFbxPath = GetRelativePathToAssets(fbxPath);
                    if (!string.IsNullOrEmpty(relativeFbxPath))
                    {
                        // Wait a moment for the asset to be fully imported
                        await Task.Delay(500);
                        
                        ModelImporter modelImporter = AssetImporter.GetAtPath(relativeFbxPath) as ModelImporter;
                        if (modelImporter != null)
                        {
                            LogToConsole("Applying bake axis conversion to Trellis FBX...", "info");
                            modelImporter.bakeAxisConversion = true;
                            
                            // Optional: Set other import settings specific to Trellis models
                            modelImporter.importBlendShapes = false;  // Trellis doesn't use blend shapes
                            modelImporter.importCameras = false;      // Trellis doesn't export cameras
                            modelImporter.importLights = false;       // Trellis doesn't export lights
                            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                            modelImporter.importNormals = ModelImporterNormals.Calculate;
                            modelImporter.normalCalculationMode = ModelImporterNormalCalculationMode.AngleWeighted;
                            modelImporter.normalSmoothingSource = ModelImporterNormalSmoothingSource.FromAngle;
                            modelImporter.normalSmoothingAngle = 60f; 
                            
                            // Save and reimport with new settings
                            modelImporter.SaveAndReimport();
                            LogToConsole("Bake axis conversion applied successfully", "success");
                        }
                        else
                        {
                            LogToConsole("Could not find ModelImporter for FBX file", "warning");
                        }
                    }
                    else
                    {
                        LogToConsole("Could not determine relative path for FBX import settings", "warning");
                    }
                }
                else
                {
                    LogToConsole("Files saved outside Assets folder. Drag them into Unity to import.", "info");
                }

                // Open folder
                EditorUtility.RevealInFinder(saveDir);
            }
            catch (Exception e)
            {
                LogToConsole($"Download error: {e.Message}", "error");
            }
        }


        private string GetRelativePathToAssets(string absolutePath)
        {
            string assetsPath = Application.dataPath;
            if (absolutePath.StartsWith(assetsPath))
            {
                return "Assets" + absolutePath.Substring(assetsPath.Length);
            }
            return null;
        }


        private async Task ExtractPLYAsync()
        {
            if (string.IsNullOrEmpty(currentSessionId))
            {
                LogToConsole("No active session for extraction", "error");
                return;
            }

            LogToConsole("Extracting PLY model...", "info");
            extractPlyBtn.SetEnabled(false);

            try
            {
                string url = $"{baseUrl.TrimEnd('/')}/extract_gaussian?session_id={currentSessionId}";

                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<TrellisAPIResponse>(request.downloadHandler.text);
                        LogToConsole("PLY extraction completed", "success");

                        // Auto-download the file
                        await DownloadFileAsync("model.ply");
                    }
                    else
                    {
                        HandleError(request, "PLY extraction failed");
                    }
                }
            }
            catch (Exception e)
            {
                LogToConsole($"PLY extraction error: {e.Message}", "error");
            }
            finally
            {
                extractPlyBtn.SetEnabled(true);
            }
        }

        private async Task DownloadFileAsync(string filename)
        {
            string savePath = EditorUtility.SaveFilePanel($"Save {filename}", "", filename, Path.GetExtension(filename).Substring(1));
            if (string.IsNullOrEmpty(savePath))
            {
                LogToConsole("Download cancelled", "warning");
                return;
            }

            LogToConsole($"Downloading {filename}...", "info");

            try
            {
                string url = $"{baseUrl.TrimEnd('/')}/download/{currentSessionId}/{filename}";
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(savePath, request.downloadHandler.data);
                        LogToConsole($"{filename} downloaded successfully", "success");
                        EditorUtility.RevealInFinder(savePath);
                    }
                    else
                    {
                        HandleError(request, $"Download of {filename} failed");
                    }
                }
            }
            catch (Exception e)
            {
                LogToConsole($"Download error: {e.Message}", "error");
            }
        }

        private void HandleError(UnityWebRequest request, string contextMessage)
        {
            string error = $"{contextMessage}: {request.responseCode} - {request.error}";
            string responseText = request.downloadHandler?.text;

            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    var apiError = JsonConvert.DeserializeObject<TrellisAPIResponse>(responseText);
                    if (apiError?.detail != null)
                    {
                        error += $" | {JsonConvert.SerializeObject(apiError.detail)}";
                    }
                }
                catch { }
            }

            LogToConsole(error, "error");
        }

        private void OnDestroy()
        {
            // Stop video updates
            StopVideoUpdates();

            // Cleanup image textures
            foreach (var texture in imageTextures)
            {
                if (texture != null)
                {
                    DestroyImmediate(texture);
                }
            }
            imageTextures.Clear();

            // Cleanup video resources
            if (videoPlayer != null)
            {
                DestroyImmediate(videoPlayer.gameObject);
                videoPlayer = null;
            }
            if (videoTexture != null)
            {
                videoTexture.Release();
                DestroyImmediate(videoTexture);
                videoTexture = null;
            }
            videoReady = false;
        }
        private void OnLostFocus()
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                shouldUpdateVideo = false; // Pause forcing updates to save CPU
            }
        }

        private void OnFocus()
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                shouldUpdateVideo = true; // Resume forcing updates
            }
        }

    }
}