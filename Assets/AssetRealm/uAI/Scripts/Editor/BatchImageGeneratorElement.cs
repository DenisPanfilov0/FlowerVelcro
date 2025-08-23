using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.EditorCoroutines.Editor;

namespace UAI
{
    public class BatchImageGeneratorElement : VisualElement
    {
        private EditorWindow editorWindow;
        private List<ScriptableObject> scriptableObjects = new List<ScriptableObject>();
        private List<MemberInfo> availableMembers = new List<MemberInfo>();
        private MemberInfo selectedSpriteMember;
        private MemberInfo selectedNamingMember;
        private List<MemberInfo> selectedInfoMembers = new List<MemberInfo>();
        
        private Dictionary<ScriptableObject, string> generatedPrompts = new Dictionary<ScriptableObject, string>();
        private bool isAIPromptMode = true;
        private string saveDirectory = "Assets/GeneratedSprites/";
        
        // UI Elements
        private Button addScriptableObjectsBtn;
        private ScrollView scriptableObjectsList;
        private Label soCountLabel;
        private DropdownField spritePropertyDropdown;
        private DropdownField namingPropertyDropdown;
        private ScrollView infoPropertiesList;
        private Button aiPromptBtn;
        private Button templatePromptBtn;
        private VisualElement aiPromptSection;
        private VisualElement templatePromptSection;
        private TextField examplePromptField;
        private TextField templatePromptField;
        private Button generatePromptsBtn;
        private ScrollView generatedPromptsPreview;
        private DropdownField modelDropdown;
        private DropdownField sizeDropdown;
        private TextField savePathField;
        private Button browsePathBtn;
        private Button testGenerateBtn;
        private VisualElement testResultContainer;
        private VisualElement testImageContainer;
        private Button approveTestBtn;
        private Button rejectTestBtn;
        private Button batchGenerateBtn;
        private VisualElement batchProgressContainer;
        private Label progressLabel;
        private VisualElement progressFill;
        private Label progressDetails;
        private VisualElement loadingOverlay;
        private Label loadingText;
        private VisualElement loadingProgressFill;

        // Image Review UI Elements
        private VisualElement imageReviewContainer;
        private ScrollView imageReviewList;
        private Button approveAllBtn;
        private Button saveApprovedBtn;
        private Label reviewStatusLabel;

        // Model and size configurations
        private Dictionary<ImageModel, List<Size>> modelSizes = new Dictionary<ImageModel, List<Size>>
        {
            { ImageModel.DallE2, new List<Size> { Size.Size256x256, Size.Size512x512, Size.Size1024x1024 } },
            { ImageModel.DallE3, new List<Size> { Size.Size1024x1024, Size.Size1536x1024, Size.Size1024x1536, Size.Size1792x1024, Size.Size1024x1792 } },
            { ImageModel.GPTImage1, new List<Size> { Size.Auto, Size.Size1024x1024, Size.Size1536x1024, Size.Size1024x1536, Size.Size1792x1024, Size.Size1024x1792 } }
        };

        private ImageModel selectedModel = ImageModel.GPTImage1;
        private Size selectedSize = Size.Auto;
        private Texture2D testGeneratedImage;
        private float lastGenerationTime = 30f; // Start with 30 seconds, then use actual times
        private float currentGenerationStartTime;
        
        // Progress update mechanism
        private bool isUpdatingProgress = false;
        private System.Action progressUpdateAction;

        // Image review tracking
        private List<ImageReviewItem> imageReviewItems = new List<ImageReviewItem>();

        // Helper class to track image review items
        private class ImageReviewItem
        {
            public ScriptableObject scriptableObject;
            public Texture2D texture;
            public string prompt;
            public bool isApproved;
            public bool isTestImage;
            public bool hasFailed;
            public string failureReason;
            public VisualElement uiElement;
            public Button approveBtn;
            public Button rerollBtn;
            public VisualElement imageDisplay;
            public Label statusLabel;
            public bool isGenerating; // For reroll status
        }

        // Improved parallel generation tracking with proper request management
        private Dictionary<string, GenerationRequest> activeRequests = new Dictionary<string, GenerationRequest>();
        private Queue<string> pendingRequestQueue = new Queue<string>(); // Queue to maintain request order
        private int completedRequests = 0;
        private int totalRequests = 0;
        private float batchStartTime = 0f;
        private float estimatedBatchTime = 0f;
        
        // Request management for parallel processing
        private System.Action<List<DALLEImageResult>> originalCallback;
        private bool isBatchGenerationActive = false;

        // Helper class to track individual generation requests
        private class GenerationRequest
        {
            public string requestId;
            public ScriptableObject scriptableObject;
            public string prompt;
            public bool isCompleted;
            public bool isSuccessful;
            public bool hasFailed;
            public string failureReason;
            public Texture2D resultTexture;
            public float startTime;
            public string fileName;
            public int index;
            public float timeoutTime;
        }

        public new class UxmlFactory : UxmlFactory<BatchImageGeneratorElement, UxmlTraits> { }

        public BatchImageGeneratorElement() : this(null) { }

        public BatchImageGeneratorElement(EditorWindow window)
        {
            editorWindow = window;
            
            // Load UXML
            var visualTree = Resources.Load<VisualTreeAsset>("BatchImageGeneratorElement");
            visualTree.CloneTree(this);

            // Load USS
            var styleSheet = Resources.Load<StyleSheet>("BatchImageGeneratorElementStyles");
            styleSheets.Add(styleSheet);

            InitializeUI();
            SetupEventHandlers();
            LoadPreferences();
            
            // Setup request management
            SetupRequestManagement();
            
            // Ensure cleanup when element is removed
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }

        private void SetupRequestManagement()
        {
            // Store the original callback so we can restore it later
            originalCallback = DALLEClient.Instance.OnResponseReceived;
            
            // Set up our improved callback system
            DALLEClient.Instance.OnResponseReceived = OnDALLEResponseReceived;
        }

        private void OnDALLEResponseReceived(List<DALLEImageResult> results)
        {
            // Only handle responses during batch generation
            if (!isBatchGenerationActive)
            {
                // Route to original callback if not in batch mode
                originalCallback?.Invoke(results);
                return;
            }

            // Handle batch generation responses with improved request matching
            if (pendingRequestQueue.Count > 0)
            {
                var requestId = pendingRequestQueue.Dequeue();
                
                if (activeRequests.ContainsKey(requestId))
                {
                    var request = activeRequests[requestId];
                    
                    if (results != null && results.Count > 0 && results[0].Texture != null)
                    {
                        request.resultTexture = results[0].Texture;
                        request.isSuccessful = true;
                        //Debug.Log(($"Successfully received image for {request.scriptableObject.name} (Index: {request.index})");
                    }
                    else
                    {
                        request.isSuccessful = false;
                        request.hasFailed = true;
                        request.failureReason = "No texture in API response";
                        Debug.LogError($"API returned empty or invalid response for {request.scriptableObject.name}");
                    }
                    
                    request.isCompleted = true;
                    completedRequests++;
                    
                    //Debug.Log(($"Processed response for request {requestId} - {request.scriptableObject.name} (Completed: {completedRequests}/{totalRequests})");
                }
                else
                {
                    Debug.LogError($"Received response for unknown request ID: {requestId}");
                }
            }
            else
            {
                Debug.LogWarning("Received DALLE response but no pending requests in queue");
            }
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            // Clean up any running progress updates
            StopProgressUpdates();
            
            // Restore original callback
            if (originalCallback != null)
            {
                DALLEClient.Instance.OnResponseReceived = originalCallback;
            }
            
            // Clear batch generation state
            isBatchGenerationActive = false;
            pendingRequestQueue.Clear();
        }

        private void InitializeUI()
        {
            // Get existing UI elements
            addScriptableObjectsBtn = this.Q<Button>("add-scriptable-objects-btn");
            scriptableObjectsList = this.Q<ScrollView>("scriptable-objects-list");
            soCountLabel = this.Q<Label>("so-count-label");
            spritePropertyDropdown = this.Q<DropdownField>("sprite-property-dropdown");
            namingPropertyDropdown = this.Q<DropdownField>("naming-property-dropdown");
            infoPropertiesList = this.Q<ScrollView>("info-properties-list");
            aiPromptBtn = this.Q<Button>("ai-prompt-btn");
            templatePromptBtn = this.Q<Button>("template-prompt-btn");
            aiPromptSection = this.Q<VisualElement>("ai-prompt-section");
            templatePromptSection = this.Q<VisualElement>("template-prompt-section");
            examplePromptField = this.Q<TextField>("example-prompt-field");
            templatePromptField = this.Q<TextField>("template-prompt-field");
            generatePromptsBtn = this.Q<Button>("generate-prompts-btn");
            generatedPromptsPreview = this.Q<ScrollView>("generated-prompts-preview");
            modelDropdown = this.Q<DropdownField>("model-dropdown");
            sizeDropdown = this.Q<DropdownField>("size-dropdown");
            savePathField = this.Q<TextField>("save-path-field");
            browsePathBtn = this.Q<Button>("browse-path-btn");
            testGenerateBtn = this.Q<Button>("test-generate-btn");
            testResultContainer = this.Q<VisualElement>("test-result-container");
            testImageContainer = this.Q<VisualElement>("test-image-container");
            approveTestBtn = this.Q<Button>("approve-test-btn");
            rejectTestBtn = this.Q<Button>("reject-test-btn");
            batchGenerateBtn = this.Q<Button>("batch-generate-btn");
            batchProgressContainer = this.Q<VisualElement>("batch-progress-container");
            progressLabel = this.Q<Label>("progress-label");
            progressFill = this.Q<VisualElement>("progress-fill");
            progressDetails = this.Q<Label>("progress-details");
            loadingOverlay = this.Q<VisualElement>("loading-overlay");
            loadingText = this.Q<Label>("loading-text");
            loadingProgressFill = this.Q<VisualElement>("loading-progress-fill");

            // Create Image Review UI
            CreateImageReviewUI();

            // Initialize dropdowns
            InitializeModelDropdown();
            InitializeSizeDropdown();

            // Set default save path
            savePathField.value = "Assets/GeneratedSprites/";
        }

        private void CreateImageReviewUI()
        {
            // Create image review container (initially hidden)
            imageReviewContainer = new VisualElement();
            imageReviewContainer.name = "image-review-container";
            imageReviewContainer.style.display = DisplayStyle.None;
            imageReviewContainer.style.marginTop = 20;
            imageReviewContainer.style.paddingLeft = 15;
            imageReviewContainer.style.paddingRight = 15;
            imageReviewContainer.style.paddingTop = 15;
            imageReviewContainer.style.paddingBottom = 15;
            imageReviewContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Title
            var title = new Label("Review Generated Images");
            title.style.fontSize = 16;
            title.style.marginBottom = 10;
            title.style.color = Color.white;
            imageReviewContainer.Add(title);

            // Status label
            reviewStatusLabel = new Label("");
            reviewStatusLabel.style.marginBottom = 10;
            reviewStatusLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            imageReviewContainer.Add(reviewStatusLabel);

            // Action buttons row
            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginBottom = 15;

            approveAllBtn = new Button(() => ApproveAllImages());
            approveAllBtn.text = "✓ Approve All";
            approveAllBtn.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f);
            approveAllBtn.style.color = Color.white;
            approveAllBtn.style.marginRight = 10;
            approveAllBtn.style.paddingTop = 8;
            approveAllBtn.style.paddingBottom = 8;
            approveAllBtn.style.paddingLeft = 8;
            approveAllBtn.style.paddingRight = 8;

            saveApprovedBtn = new Button(() => SaveApprovedImages());
            saveApprovedBtn.text = "Save Approved Images";
            saveApprovedBtn.style.backgroundColor = new Color(0.2f, 0.4f, 0.7f);
            saveApprovedBtn.style.color = Color.white;
            saveApprovedBtn.style.paddingTop = 8;
            saveApprovedBtn.style.paddingBottom = 8;
            saveApprovedBtn.style.paddingLeft = 8;
            saveApprovedBtn.style.paddingRight = 8;
            saveApprovedBtn.SetEnabled(false);

            actionRow.Add(approveAllBtn);
            actionRow.Add(saveApprovedBtn);
            imageReviewContainer.Add(actionRow);

            // Scroll view for images
            imageReviewList = new ScrollView();
            imageReviewList.style.maxHeight = 600;
            imageReviewList.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            imageReviewList.style.paddingTop = 10;
            imageReviewList.style.paddingBottom = 10;
            imageReviewList.style.paddingLeft = 10;
            imageReviewList.style.paddingRight = 10;
            imageReviewContainer.Add(imageReviewList);

            // Add to main container (after batch progress container)
            var batchSection = this.Q<VisualElement>("batch-progress-container").parent;
            batchSection.Add(imageReviewContainer);
        }

        private void SetupEventHandlers()
        {
            addScriptableObjectsBtn.clicked += AddScriptableObjects;
            aiPromptBtn.clicked += () => SetPromptMode(true);
            templatePromptBtn.clicked += () => SetPromptMode(false);
            generatePromptsBtn.clicked += GeneratePrompts;
            modelDropdown.RegisterValueChangedCallback(OnModelChanged);
            browsePathBtn.clicked += BrowseSaveDirectory;
            testGenerateBtn.clicked += GenerateTestImage;
            approveTestBtn.clicked += ApproveTestImage;
            rejectTestBtn.clicked += RejectTestImage;
            batchGenerateBtn.clicked += StartBatchGeneration;
        }

        private void InitializeModelDropdown()
        {
            var modelNames = new List<string> { "GPT Image 1", "DALL-E 3", "DALL-E 2" };
            modelDropdown.choices = modelNames;
            modelDropdown.value = modelNames[0];
        }

        private void InitializeSizeDropdown()
        {
            UpdateSizeDropdown();
        }

        private void UpdateSizeDropdown()
        {
            var sizes = modelSizes[selectedModel];
            var sizeNames = sizes.Select(s => SizeToString(s)).ToList();
            sizeDropdown.choices = sizeNames;
            sizeDropdown.value = sizeNames.FirstOrDefault();
            selectedSize = sizes.FirstOrDefault();
        }

        private string SizeToString(Size size)
        {
            switch (size)
            {
                case Size.Auto: return "Auto";
                case Size.Size256x256: return "256x256";
                case Size.Size512x512: return "512x512";
                case Size.Size1024x1024: return "1024x1024";
                case Size.Size1536x1024: return "1536x1024";
                case Size.Size1024x1536: return "1024x1536";
                case Size.Size1792x1024: return "1792x1024";
                case Size.Size1024x1792: return "1024x1792";
                default: return "Auto";
            }
        }

        private ImageModel StringToModel(string modelName)
        {
            switch (modelName)
            {
                case "DALL-E 2": return ImageModel.DallE2;
                case "DALL-E 3": return ImageModel.DallE3;
                case "GPT Image 1": return ImageModel.GPTImage1;
                default: return ImageModel.GPTImage1;
            }
        }

        private Size StringToSize(string sizeName)
        {
            switch (sizeName)
            {
                case "Auto": return Size.Auto;
                case "256x256": return Size.Size256x256;
                case "512x512": return Size.Size512x512;
                case "1024x1024": return Size.Size1024x1024;
                case "1536x1024": return Size.Size1536x1024;
                case "1024x1536": return Size.Size1024x1536;
                case "1792x1024": return Size.Size1792x1024;
                case "1024x1792": return Size.Size1024x1792;
                default: return Size.Auto;
            }
        }

        private void AddScriptableObjects()
        {
            var selection = Selection.objects.OfType<ScriptableObject>().ToList();
            
            if (selection.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select one or more ScriptableObjects in the Project window.", "OK");
                return;
            }

            // Check if all selected objects are of the same type
            var firstType = selection[0].GetType();
            if (selection.Any(so => so.GetType() != firstType))
            {
                EditorUtility.DisplayDialog("Mixed Types", "All selected ScriptableObjects must be of the same type.", "OK");
                return;
            }

            // Clear existing if this is a different type
            if (scriptableObjects.Count > 0 && scriptableObjects[0].GetType() != firstType)
            {
                scriptableObjects.Clear();
                generatedPrompts.Clear();
                ClearPropertyDropdowns();
                HideImageReview(); // Hide any existing review
            }

            // Add new objects (avoid duplicates)
            foreach (var so in selection)
            {
                if (!scriptableObjects.Contains(so))
                {
                    scriptableObjects.Add(so);
                }
            }

            RefreshScriptableObjectsList();
            RefreshPropertyDropdowns();
        }

        private void RefreshScriptableObjectsList()
        {
            scriptableObjectsList.Clear();
            
            foreach (var so in scriptableObjects)
            {
                var item = new VisualElement();
                item.AddToClassList("so-list-item");

                var label = new Label(so.name);
                label.style.flexGrow = 1;

                var deleteBtn = new Button(() => RemoveScriptableObject(so));
                deleteBtn.text = "✗";
                deleteBtn.AddToClassList("delete-button");

                item.Add(label);
                item.Add(deleteBtn);
                scriptableObjectsList.Add(item);
            }

            soCountLabel.text = $"{scriptableObjects.Count} objects added";
        }

        private void RemoveScriptableObject(ScriptableObject so)
        {
            scriptableObjects.Remove(so);
            generatedPrompts.Remove(so);
            RefreshScriptableObjectsList();
            
            if (scriptableObjects.Count == 0)
            {
                ClearPropertyDropdowns();
                HideImageReview();
            }
        }

        private void RefreshPropertyDropdowns()
        {
            if (scriptableObjects.Count == 0) return;

            var type = scriptableObjects[0].GetType();
            
            // Get both fields and properties, including inherited ones
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly && !f.IsLiteral) // Exclude readonly and const fields
                .Cast<MemberInfo>();
                
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0) // Exclude indexers
                .Cast<MemberInfo>();

            availableMembers = fields.Concat(properties)
                .Where(m => !IsUnityInternalMember(m)) // Filter out Unity internal members
                .ToList();

            // Sprite members (Sprite type)
            var spriteMembers = availableMembers
                .Where(m => GetMemberType(m) == typeof(Sprite))
                .Select(m => m.Name)
                .ToList();

            spritePropertyDropdown.choices = spriteMembers;
            if (spriteMembers.Count > 0)
                spritePropertyDropdown.value = spriteMembers[0];

            // Naming members (string, int, float, double types)
            var namingMembers = availableMembers
                .Where(m => IsValidNamingType(GetMemberType(m)))
                .Select(m => m.Name)
                .ToList();

            namingPropertyDropdown.choices = namingMembers;
            if (namingMembers.Count > 0)
            {
                // Try to find a property with "name" in it (case insensitive)
                var nameProperty = namingMembers.FirstOrDefault(m => m.ToLower().Contains("name"));
                namingPropertyDropdown.value = nameProperty ?? namingMembers[0];
            }

            // Info members checkboxes
            RefreshInfoMembersList();
        }

        private bool IsUnityInternalMember(MemberInfo member)
        {
            // Filter out Unity internal members that users typically don't want
            var unityInternals = new[] { "hideFlags", "name" };
            return unityInternals.Contains(member.Name);
        }

        private Type GetMemberType(MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo field:
                    return field.FieldType;
                case PropertyInfo property:
                    return property.PropertyType;
                default:
                    return null;
            }
        }

        private bool IsValidNamingType(Type type)
        {
            return type == typeof(string) || 
                   type == typeof(int) || 
                   type == typeof(float) || 
                   type == typeof(double) ||
                   type.IsEnum;
        }

        private bool IsValidInfoType(Type type)
        {
            return type == typeof(string) || 
                   type == typeof(int) || 
                   type == typeof(float) || 
                   type == typeof(double) || 
                   type == typeof(bool) ||
                   type.IsEnum;
        }

        private object GetMemberValue(MemberInfo member, object obj)
        {
            switch (member)
            {
                case FieldInfo field:
                    return field.GetValue(obj);
                case PropertyInfo property:
                    return property.GetValue(obj);
                default:
                    return null;
            }
        }

        private void SetMemberValue(MemberInfo member, object obj, object value)
        {
            switch (member)
            {
                case FieldInfo field:
                    field.SetValue(obj, value);
                    break;
                case PropertyInfo property:
                    property.SetValue(obj, value);
                    break;
            }
        }

        private void RefreshInfoMembersList()
        {
            infoPropertiesList.Clear();

            foreach (var member in availableMembers)
            {
                if (IsValidInfoType(GetMemberType(member)))
                {
                    var container = new VisualElement();
                    container.AddToClassList("property-checkbox");

                    var toggle = new Toggle();
                    toggle.value = selectedInfoMembers.Contains(member);
                    toggle.RegisterValueChangedCallback(evt => 
                    {
                        if (evt.newValue)
                            selectedInfoMembers.Add(member);
                        else
                            selectedInfoMembers.Remove(member);
                    });

                    var label = new Label($"{member.Name} ({GetMemberType(member).Name})");
                    
                    container.Add(toggle);
                    container.Add(label);
                    infoPropertiesList.Add(container);
                }
            }
        }

        private void ClearPropertyDropdowns()
        {
            spritePropertyDropdown.choices = new List<string>();
            namingPropertyDropdown.choices = new List<string>();
            infoPropertiesList.Clear();
            availableMembers.Clear();
            selectedInfoMembers.Clear();
        }

        private void SetPromptMode(bool isAI)
        {
            isAIPromptMode = isAI;
            
            aiPromptBtn.RemoveFromClassList("selected");
            templatePromptBtn.RemoveFromClassList("selected");
            
            if (isAI)
            {
                aiPromptBtn.AddToClassList("selected");
                aiPromptSection.style.display = DisplayStyle.Flex;
                templatePromptSection.style.display = DisplayStyle.None;
            }
            else
            {
                templatePromptBtn.AddToClassList("selected");
                aiPromptSection.style.display = DisplayStyle.None;
                templatePromptSection.style.display = DisplayStyle.Flex;
            }
        }

        private void OnModelChanged(ChangeEvent<string> evt)
        {
            selectedModel = StringToModel(evt.newValue);
            UpdateSizeDropdown();
        }

        private void BrowseSaveDirectory()
        {
            var currentPath = savePathField.value;
            if (string.IsNullOrEmpty(currentPath))
                currentPath = Application.dataPath;

            var selectedPath = EditorUtility.OpenFolderPanel("Select Save Directory", currentPath, "");
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert absolute path to relative Assets path if it's within the project
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    // If outside project, show warning but allow it
                    if (!EditorUtility.DisplayDialog("Path Outside Project", 
                        "The selected path is outside the Unity project. Images will be saved there but won't be automatically imported as Unity assets. Continue?", 
                        "Yes", "Cancel"))
                    {
                        return;
                    }
                }
                
                savePathField.value = selectedPath;
            }
        }

        private void GeneratePrompts()
        {
            if (scriptableObjects.Count == 0)
            {
                ShowStatusMessage("Please add ScriptableObjects first.", "error");
                return;
            }

            if (selectedInfoMembers.Count == 0)
            {
                ShowStatusMessage("Please select at least one info property.", "error");
                return;
            }

            if (string.IsNullOrEmpty(examplePromptField.value))
            {
                ShowStatusMessage("Please provide an example prompt.", "error");
                return;
            }

            EditorCoroutineUtility.StartCoroutine(GeneratePromptsCoroutine(), editorWindow);
        }

        private IEnumerator GeneratePromptsCoroutine()
        {
            ShowLoading("Generating prompts...");
            generatedPrompts.Clear();

            for (int i = 0; i < scriptableObjects.Count; i++)
            {
                var so = scriptableObjects[i];
                loadingText.text = $"Generating prompt {i + 1}/{scriptableObjects.Count}...";
                UpdateLoadingProgress((float)i / scriptableObjects.Count);

                var infoText = GetInfoText(so);
                var promptRequest = $"Based on this example prompt: '{examplePromptField.value}'\n\n" +
                                  $"Generate a similar stylized icon prompt for: {infoText}\n\n" +
                                  $"Keep the same style and format but customize it for the specific object.";

                bool promptGenerated = false;
                string generatedPrompt = "";

                GPTClient.Instance.OnResponseReceived = (response, index) =>
                {
                    generatedPrompt = response.Trim();
                    promptGenerated = true;
                };

                GPTClient.Instance.SendRequest(promptRequest);

                // Wait for response
                float timeout = 30f;
                float elapsedTime = 0f;
                while (!promptGenerated && elapsedTime < timeout)
                {
                    elapsedTime += 0.1f;
                    yield return new WaitForSecondsRealtime(0.1f);
                }

                if (promptGenerated)
                {
                    generatedPrompts[so] = generatedPrompt;
                }
                else
                {
                    ShowStatusMessage($"Failed to generate prompt for {so.name}", "error");
                    HideLoading();
                    yield break;
                }
            }

            UpdateLoadingProgress(1f);
            yield return new WaitForSecondsRealtime(0.5f);
            HideLoading();

            ShowGeneratedPromptsPreview();
            ShowStatusMessage($"Generated {generatedPrompts.Count} prompts successfully!", "success");
        }

        private void ShowGeneratedPromptsPreview()
        {
            generatedPromptsPreview.Clear();
            generatedPromptsPreview.style.display = DisplayStyle.Flex;

            foreach (var kvp in generatedPrompts)
            {
                var container = new VisualElement();
                container.AddToClassList("prompt-preview-item");

                var title = new Label(kvp.Key.name);
                title.AddToClassList("prompt-preview-title");

                var prompt = new Label(kvp.Value);
                prompt.AddToClassList("prompt-preview-text");

                container.Add(title);
                container.Add(prompt);
                generatedPromptsPreview.Add(container);
            }
        }

        private string GetInfoText(ScriptableObject so)
        {
            var infoParts = new List<string>();
            
            foreach (var member in selectedInfoMembers)
            {
                try
                {
                    var value = GetMemberValue(member, so);
                    if (value != null)
                    {
                        infoParts.Add($"{member.Name}: {value}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not read member {member.Name}: {ex.Message}");
                }
            }

            return string.Join(", ", infoParts);
        }

        private void GenerateTestImage()
        {
            if (!ValidateConfiguration())
                return;

            var testSO = scriptableObjects[0];
            var prompt = GetPromptForSO(testSO);

            if (string.IsNullOrEmpty(prompt))
            {
                ShowStatusMessage("No prompt available for test generation.", "error");
                return;
            }

            EditorCoroutineUtility.StartCoroutine(GenerateTestImageCoroutine(prompt), editorWindow);
        }

        private string GetPromptForSO(ScriptableObject so)
        {
            if (isAIPromptMode)
            {
                return generatedPrompts.ContainsKey(so) ? generatedPrompts[so] : "";
            }
            else
            {
                var template = templatePromptField.value;
                var infoText = GetInfoText(so);
                return template.Replace("[INFOS_HERE]", infoText);
            }
        }

        private IEnumerator GenerateTestImageCoroutine(string prompt)
        {
            ShowLoading("Generating test image...");
            UpdateLoadingProgress(0f);
            currentGenerationStartTime = Time.realtimeSinceStartup;

            var request = new ImageGenerationRequest
            {
                Prompt = prompt,
                Model = selectedModel,
                Count = 1,
                Size = selectedSize,
                ResponseFormat = ResponseFormat.URL,
                Quality = Quality.auto
            };

            bool imageGenerated = false;
            DALLEImageResult result = null;

            // Store original callback and set our own
            var tempOriginalCallback = DALLEClient.Instance.OnResponseReceived;
            DALLEClient.Instance.OnResponseReceived = (results) =>
            {
                if (results != null && results.Count > 0)
                {
                    result = results[0];
                    imageGenerated = true;
                }
            };

            DALLEClient.Instance.GenerateImage(request);

            // Start progress update system (for estimated time)
            StartProgressUpdates(() =>
            {
                if (imageGenerated) return;
                
                float elapsedTime = Time.realtimeSinceStartup - currentGenerationStartTime;
                float progress = Mathf.Min(elapsedTime / lastGenerationTime, 0.9f);
                UpdateLoadingProgress(progress);
                
                if (elapsedTime > lastGenerationTime)
                {
                    loadingText.text = $"Still generating... ({Mathf.RoundToInt(elapsedTime)}s)";
                }
                else
                {
                    loadingText.text = $"Generating test image... ({Mathf.RoundToInt(elapsedTime)}s)";
                }
            });

            // Wait for actual completion with longer real timeout
            float realTimeout = 120f; // 2 minutes max
            float elapsedTime = 0f;
            while (!imageGenerated && elapsedTime < realTimeout)
            {
                elapsedTime += 1f; 
                yield return new WaitForSecondsRealtime(1f);
            }

            // Restore callback
            DALLEClient.Instance.OnResponseReceived = tempOriginalCallback;

            if (imageGenerated)
            {
                lastGenerationTime = Time.realtimeSinceStartup - currentGenerationStartTime;
                UpdateLoadingProgress(1f);
                loadingText.text = $"Test image complete! ({Mathf.RoundToInt(lastGenerationTime)}s)";
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // Stop progress updates and ensure UI updates until loading is hidden
            StopProgressUpdates();
            
            // Keep forcing updates until the loading overlay is actually hidden
            EditorCoroutineUtility.StartCoroutine(EnsureLoadingHides(), editorWindow);

            if (imageGenerated && result != null && result.Texture != null)
            {
                testGeneratedImage = result.Texture;
                ShowTestResult();
                ShowStatusMessage($"Test image generated successfully! ({Mathf.RoundToInt(lastGenerationTime)}s)", "success");
            }
            else
            {
                ShowStatusMessage("Failed to generate test image - timeout or error occurred.", "error");
            }
        }

        private IEnumerator EnsureReviewUIShows()
        {
            // Force updates for several frames to ensure the review UI actually appears
            for (int i = 0; i < 30; i++)
            {
                ForceEditorUpdate();
                yield return null;
            }
        }

        private IEnumerator EnsureLoadingHides()
        {
            HideLoading();
            
            // Force updates for a few frames to ensure the UI actually refreshes
            for (int i = 0; i < 30; i++) // Force updates for 30 frames
            {
                ForceEditorUpdate();
                yield return null;
            }
        }

        private void StartProgressUpdates(System.Action updateAction)
        {
            progressUpdateAction = updateAction;
            isUpdatingProgress = true;
            EditorApplication.update += OnEditorUpdate;
        }

        private void StopProgressUpdates()
        {
            isUpdatingProgress = false;
            EditorApplication.update -= OnEditorUpdate;
            progressUpdateAction = null;
        }

        private void OnEditorUpdate()
        {
            if (isUpdatingProgress && progressUpdateAction != null)
            {
                progressUpdateAction.Invoke();
                ForceEditorUpdate();
            }
        }

        private void ForceEditorUpdate()
        {
            // Try multiple approaches to force UI update
            EditorApplication.QueuePlayerLoopUpdate();

            // Force repaint of focused window
            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.Repaint();
            } 
        }

        private void ShowTestResult()
        {
            testResultContainer.style.display = DisplayStyle.Flex;
            testImageContainer.Clear();

            var image = new VisualElement();
            image.style.backgroundImage = new StyleBackground(testGeneratedImage);
            image.style.width = Mathf.Min(testGeneratedImage.width, 256);
            image.style.height = Mathf.Min(testGeneratedImage.height, 256);
            image.AddToClassList("test-image-display");

            testImageContainer.Add(image);
        }

        private void ApproveTestImage()
        {
            batchGenerateBtn.style.display = DisplayStyle.Flex;
            ShowStatusMessage("Test approved! You can now generate all images.", "success");
        }

        private void RejectTestImage()
        {
            testResultContainer.style.display = DisplayStyle.None;
            batchGenerateBtn.style.display = DisplayStyle.None;
            testGeneratedImage = null; // Clear test image so it won't be reused
            ShowStatusMessage("Test rejected. Please adjust your settings and try again.", "warning");
        }

        private void StartBatchGeneration()
        {
            if (!ValidateConfiguration())
                return;

            // Hide the test result container during batch generation
            testResultContainer.style.display = DisplayStyle.None;
            batchGenerateBtn.style.display = DisplayStyle.None;

            //Debug.Log(($"Starting parallel batch generation for {scriptableObjects.Count} objects");
            if (testGeneratedImage != null)
            {
                //Debug.Log(("Test image available for reuse");
            }

            EditorCoroutineUtility.StartCoroutine(ParallelBatchGenerationCoroutine(), editorWindow);
        }

        private IEnumerator ParallelBatchGenerationCoroutine()
        {
            // Initialize batch generation state
            isBatchGenerationActive = true;
            
            // Initialize progress tracking
            batchProgressContainer.style.display = DisplayStyle.Flex;
            progressLabel.text = "Generating images in parallel...";
            
            // Reset tracking variables
            activeRequests.Clear();
            pendingRequestQueue.Clear();
            completedRequests = 0;
            totalRequests = scriptableObjects.Count;
            
            // Calculate estimated time (test time * 1.5)
            estimatedBatchTime = lastGenerationTime * 1.5f;
            batchStartTime = Time.realtimeSinceStartup;

            progressDetails.text = $"{completedRequests} of {totalRequests} done. Generating...";
            UpdateBatchProgress(0f); // Start progress bar at 0

            // Create all requests
            for (int i = 0; i < scriptableObjects.Count; i++)
            {
                var so = scriptableObjects[i];
                var prompt = GetPromptForSO(so);
                var fileName = GetFileName(so);
                var requestId = System.Guid.NewGuid().ToString();

                var request = new GenerationRequest
                {
                    requestId = requestId,
                    scriptableObject = so,
                    prompt = prompt,
                    isCompleted = false,
                    isSuccessful = false,
                    hasFailed = false,
                    failureReason = "",
                    resultTexture = null,
                    startTime = Time.realtimeSinceStartup,
                    fileName = fileName,
                    index = i,
                    timeoutTime = Time.realtimeSinceStartup + 120f // 2 minute timeout per request
                };

                activeRequests[requestId] = request;
            }

            // Handle the test image for the first SO
            // bool hasTestImageReuse = false;
            if (testGeneratedImage != null && activeRequests.Count > 0)
            {
                var firstRequest = activeRequests.Values.OrderBy(r => r.index).First();
                firstRequest.resultTexture = testGeneratedImage;
                firstRequest.isCompleted = true;
                firstRequest.isSuccessful = true;
                completedRequests = 1; // Count the test image 
                // hasTestImageReuse = true;
                
                //Debug.Log(($"Including test image for {firstRequest.scriptableObject.name}");
            }

            // Start requests for remaining items (excluding the one with test image)
            var requestsToGenerate = activeRequests.Values.Where(r => r.resultTexture == null).OrderBy(r => r.index).ToList();
            
            foreach (var request in requestsToGenerate)
            {
                // Add to pending queue in order
                pendingRequestQueue.Enqueue(request.requestId);
                
                // Start the request coroutine
                EditorCoroutineUtility.StartCoroutine(GenerateImageForRequestParallel(request), editorWindow);
                
                yield return new WaitForSecondsRealtime(0.2f); // Small delay to avoid overwhelming the API
            }

            // Start progress update system and timeout monitoring
            StartProgressUpdates(() =>
            {
                // Check for timeouts and update progress
                CheckForTimeouts();
                
                // Time-based progress (goes up to 95%)
                float elapsedTime = Time.realtimeSinceStartup - batchStartTime;
                float timeProgress = Mathf.Min(elapsedTime / estimatedBatchTime, 0.95f);
                
                // Completion-based progress (can go to 100%)
                float completionProgress = (float)completedRequests / totalRequests;
                
                // Use the higher of the two progress values
                float finalProgress = Mathf.Max(timeProgress, completionProgress);
                UpdateBatchProgress(finalProgress);
                
                // Count successful and failed
                var successful = activeRequests.Values.Count(r => r.isCompleted && r.isSuccessful);
                var failed = activeRequests.Values.Count(r => r.isCompleted && r.hasFailed);
                
                // Update the details text
                progressDetails.text = $"{completedRequests} of {totalRequests} done ({successful} success, {failed} failed). Generating...";
                
                // Update the main label with time info
                if (completedRequests < totalRequests)
                {
                    if (elapsedTime < estimatedBatchTime)
                    {
                        progressLabel.text = $"Generating images in parallel... ({Mathf.RoundToInt(elapsedTime)}s / ~{Mathf.RoundToInt(estimatedBatchTime)}s)";
                    }
                    else
                    {
                        progressLabel.text = $"Generating images in parallel... ({Mathf.RoundToInt(elapsedTime)}s - finishing up...)";
                    }
                }
                else
                {
                    progressLabel.text = "Generation complete!";
                }
            });

            // Wait for all requests to complete
            while (completedRequests < totalRequests)
            {
                yield return new WaitForSecondsRealtime(0.1f);
            }

            // Disable batch generation state
            isBatchGenerationActive = false;

            // Stop progress updates
            StopProgressUpdates();

            // Final progress update
            UpdateBatchProgress(1f);
            var finalSuccessful = activeRequests.Values.Count(r => r.isCompleted && r.isSuccessful);
            var finalFailed = activeRequests.Values.Count(r => r.isCompleted && r.hasFailed);
            progressDetails.text = $"Completed! {finalSuccessful} successful, {finalFailed} failed.";
            progressLabel.text = "Generation complete!";
            
            // Hide progress and show image review
            yield return new WaitForSecondsRealtime(0.5f);
            batchProgressContainer.style.display = DisplayStyle.None;
            
            // Prepare image review
            PrepareImageReview();
            
            // Force UI updates to ensure review UI shows up
            EditorCoroutineUtility.StartCoroutine(EnsureReviewUIShows(), editorWindow);
        }

        private void CheckForTimeouts()
        {
            var currentTime = Time.realtimeSinceStartup;
            
            foreach (var request in activeRequests.Values.Where(r => !r.isCompleted))
            {
                if (currentTime > request.timeoutTime)
                {
                    Debug.LogWarning($"Request for {request.scriptableObject.name} timed out after 120 seconds");
                    request.isCompleted = true;
                    request.hasFailed = true;
                    request.failureReason = "Request timed out (120s)";
                    completedRequests++;
                    
                    // Remove from pending queue if it's still there
                    var queueList = pendingRequestQueue.ToList();
                    if (queueList.Contains(request.requestId))
                    {
                        pendingRequestQueue.Clear();
                        foreach (var id in queueList.Where(id => id != request.requestId))
                        {
                            pendingRequestQueue.Enqueue(id);
                        }
                    }
                }
            }
        }

        private IEnumerator GenerateImageForRequestParallel(GenerationRequest request)
        {
            //Debug.Log(($"Starting parallel generation for {request.scriptableObject.name} (Index: {request.index})");
            
            var apiRequest = new ImageGenerationRequest
            {
                Prompt = request.prompt,
                Model = selectedModel,
                Count = 1,
                Size = selectedSize,
                ResponseFormat = ResponseFormat.URL,
                Quality = Quality.auto
            };

            // Check DALLE client status before making request
            if (DALLEClient.Instance.status == DALLEStatus.WaitingForResponse)
            {
                Debug.LogWarning($"DALLE client is busy, waiting before starting {request.scriptableObject.name}");
                // Wait for client to be free
                while (DALLEClient.Instance.status == DALLEStatus.WaitingForResponse)
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            // Send the request
            DALLEClient.Instance.GenerateImage(apiRequest);

            // Wait for this specific request to complete or timeout
            while (!request.isCompleted && Time.realtimeSinceStartup < request.timeoutTime)
            {
                // Check if API returned an error
                if (DALLEClient.Instance.status == DALLEStatus.Error)
                {
                    Debug.LogError($"DALLE API error for {request.scriptableObject.name}");
                    request.isCompleted = true;
                    request.hasFailed = true;
                    request.failureReason = "API Error (Bad Gateway or similar)";
                    completedRequests++;
                    
                    // Remove from pending queue
                    var queueList = pendingRequestQueue.ToList();
                    if (queueList.Contains(request.requestId))
                    {
                        pendingRequestQueue.Clear();
                        foreach (var id in queueList.Where(id => id != request.requestId))
                        {
                            pendingRequestQueue.Enqueue(id);
                        }
                    }
                    
                    // Reset DALLE status for next request
                    DALLEClient.Instance.status = DALLEStatus.Idle;
                    break;
                }
                
                yield return new WaitForSecondsRealtime(0.1f);
            }

            // Log result
            if (request.isCompleted)
            {
                if (request.isSuccessful)
                {
                    //Debug.Log(($"Successfully generated image for {request.scriptableObject.name} (Index: {request.index})");
                }
                else
                {
                    Debug.LogError($"Failed to generate image for {request.scriptableObject.name} (Index: {request.index}) - {request.failureReason}");
                }
            }
        }

        private void PrepareImageReview()
        {
            imageReviewItems.Clear();
            
            // Add all requests to review, sorted by original index to maintain order
            var sortedRequests = activeRequests.Values
                .Where(r => r.isCompleted) // Include both successful and failed
                .OrderBy(r => r.index)
                .ToList();
            
            foreach (var request in sortedRequests)
            {
                var reviewItem = new ImageReviewItem
                {
                    scriptableObject = request.scriptableObject,
                    texture = request.resultTexture,
                    prompt = request.prompt,
                    isApproved = false,
                    isTestImage = request.index == 0 && testGeneratedImage != null && request.resultTexture == testGeneratedImage,
                    hasFailed = request.hasFailed,
                    failureReason = request.failureReason,
                    isGenerating = false
                };
                
                imageReviewItems.Add(reviewItem);
                //Debug.Log(($"Added review item for {request.scriptableObject.name} (Index: {request.index}, Success: {request.isSuccessful}, Failed: {request.hasFailed})");
            }
            
            ShowImageReview();
        }

        private void ShowImageReview()
        {
            imageReviewList.Clear();
            
            foreach (var item in imageReviewItems)
            {
                CreateImageReviewItem(item);
            }
            
            UpdateReviewStatus();
            imageReviewContainer.style.display = DisplayStyle.Flex;
        }

        private void CreateImageReviewItem(ImageReviewItem item)
        {
            // Main container
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginBottom = 15;
            container.style.paddingTop = 10;
            container.style.paddingBottom = 10;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            container.style.minHeight = 150;
            item.uiElement = container;

            // Image display
            var imageContainer = new VisualElement();
            imageContainer.style.width = 150;
            imageContainer.style.height = 150;
            imageContainer.style.marginRight = 15;
            imageContainer.style.flexShrink = 0; // Prevent shrinking

            var imageDisplay = new VisualElement();
            
            if (item.hasFailed)
            {
                // Show error placeholder
                imageDisplay.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f, 1f);
                var errorLabel = new Label("❌\nFailed");
                errorLabel.style.color = Color.white;
                errorLabel.style.fontSize = 16;
                errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                errorLabel.style.width = 150;
                errorLabel.style.height = 150;
                imageDisplay.Add(errorLabel);
            }
            else if (item.texture != null)
            {
                imageDisplay.style.backgroundImage = new StyleBackground(item.texture);
                imageDisplay.style.backgroundColor = Color.gray;
            }
            else
            {
                imageDisplay.style.backgroundColor = Color.gray;
            }
            
            imageDisplay.style.width = 150;
            imageDisplay.style.height = 150;
            imageDisplay.style.flexShrink = 0; // Prevent shrinking
            item.imageDisplay = imageDisplay;

            imageContainer.Add(imageDisplay);

            // Info section
            var infoSection = new VisualElement();
            infoSection.style.flexGrow = 1;
            infoSection.style.marginRight = 15;
            infoSection.style.minWidth = 200; // Ensure minimum width
            infoSection.style.maxWidth = Length.Percent(60); // Max 60% of container

            // Title
            var title = new Label(item.scriptableObject.name);
            title.style.fontSize = 14;
            title.style.color = Color.white;
            title.style.marginBottom = 5;
            title.style.whiteSpace = WhiteSpace.Normal; // Allow text wrapping
            if (item.isTestImage)
            {
                title.text += " (Test Image)";
                title.style.color = new Color(0.8f, 0.9f, 1f);
            }
            else if (item.hasFailed)
            {
                title.style.color = new Color(1f, 0.6f, 0.6f);
            }

            // Prompt preview or error message
            Label promptLabel;
            if (item.hasFailed)
            {
                promptLabel = new Label($"Error: {item.failureReason}");
                promptLabel.style.color = new Color(1f, 0.6f, 0.6f);
            }
            else
            {
                promptLabel = new Label($"Prompt: {(item.prompt.Length > 100 ? item.prompt.Substring(0, 100) + "..." : item.prompt)}");
                promptLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            }
            promptLabel.style.fontSize = 11;
            promptLabel.style.whiteSpace = WhiteSpace.Normal;
            promptLabel.style.marginBottom = 10;

            // Status
            var statusLabel = new Label(item.hasFailed ? "❌ Failed" : "Pending Review");
            statusLabel.style.color = item.hasFailed ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 0.8f, 0.2f);
            statusLabel.style.fontSize = 12;
            item.statusLabel = statusLabel;

            infoSection.Add(title);
            infoSection.Add(promptLabel);
            infoSection.Add(statusLabel);

            // Button section
            var buttonSection = new VisualElement();
            buttonSection.style.flexDirection = FlexDirection.Column;
            buttonSection.style.width = 120;
            buttonSection.style.flexShrink = 0; // Prevent shrinking

            // Approve button (only for successful items)
            var approveBtn = new Button(() => ApproveImage(item));
            if (item.hasFailed)
            {
                approveBtn.text = "❌ Failed";
                approveBtn.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
                approveBtn.SetEnabled(false);
            }
            else
            {
                approveBtn.text = "✓ Approve";
                approveBtn.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f);
            }
            approveBtn.style.color = Color.white;
            approveBtn.style.marginBottom = 5;
            approveBtn.style.height = 30;
            item.approveBtn = approveBtn;

            // Reroll button (always available)
            var rerollBtn = new Button(() => RerollImage(item));
            rerollBtn.text = "Reroll";
            rerollBtn.style.backgroundColor = new Color(0.7f, 0.4f, 0.2f);
            rerollBtn.style.color = Color.white;
            rerollBtn.style.height = 30;
            item.rerollBtn = rerollBtn;

            buttonSection.Add(approveBtn);
            buttonSection.Add(rerollBtn);

            // Add all sections to container
            container.Add(imageContainer);
            container.Add(infoSection);
            container.Add(buttonSection);

            imageReviewList.Add(container);
        }

        private void ApproveImage(ImageReviewItem item)
        {
            if (item.hasFailed) return; // Can't approve failed items
            
            item.isApproved = true;
            item.statusLabel.text = "✓ Approved";
            item.statusLabel.style.color = new Color(0.2f, 0.8f, 0.2f);
            item.approveBtn.text = "✓ Approved";
            item.approveBtn.SetEnabled(false);
            
            UpdateReviewStatus();
        }

        private void RerollImage(ImageReviewItem item)
        {
            if (item.isGenerating) return;
            
            item.isGenerating = true;
            item.statusLabel.text = "🎲 Regenerating...";
            item.statusLabel.style.color = new Color(0.2f, 0.6f, 1f);
            item.rerollBtn.text = "Generating...";
            item.rerollBtn.SetEnabled(false);
            item.approveBtn.SetEnabled(false);
            
            EditorCoroutineUtility.StartCoroutine(RerollImageCoroutine(item), editorWindow);
        }

        private IEnumerator RerollImageCoroutine(ImageReviewItem item)
        {
            //Debug.Log(($"Starting reroll for {item.scriptableObject.name}");
            
            var request = new ImageGenerationRequest
            {
                Prompt = item.prompt,
                Model = selectedModel,
                Count = 1,
                Size = selectedSize,
                ResponseFormat = ResponseFormat.URL,
                Quality = Quality.auto
            };

            bool imageGenerated = false;
            DALLEImageResult result = null;

            // Store original callback and set our own for reroll
            var tempOriginalCallback = DALLEClient.Instance.OnResponseReceived;
            DALLEClient.Instance.OnResponseReceived = (results) =>
            {
                if (results != null && results.Count > 0)
                {
                    result = results[0];
                    imageGenerated = true;
                }
            };

            DALLEClient.Instance.GenerateImage(request);

            // Wait for completion
            float timeout = 120f;
            float elapsedTime = 0f;
            
            while (!imageGenerated && elapsedTime < timeout)
            {
                // Check for API errors
                if (DALLEClient.Instance.status == DALLEStatus.Error)
                {
                    Debug.LogError($"API error during reroll for {item.scriptableObject.name}");
                    break;
                }
                
                elapsedTime += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            // Restore callback
            DALLEClient.Instance.OnResponseReceived = tempOriginalCallback;

            if (imageGenerated && result != null && result.Texture != null)
            {
                // Update the item with new texture
                item.texture = result.Texture;
                item.imageDisplay.Clear();
                item.imageDisplay.style.backgroundImage = new StyleBackground(item.texture);
                item.imageDisplay.style.backgroundColor = Color.gray;
                
                item.isApproved = false;
                item.isTestImage = false; // Rerolled images are no longer test images
                item.hasFailed = false;
                item.failureReason = "";
                
                item.statusLabel.text = "Pending Review";
                item.statusLabel.style.color = new Color(1f, 0.8f, 0.2f);
                
                // Re-enable approve button
                item.approveBtn.text = "Approve";
                item.approveBtn.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f);
                item.approveBtn.SetEnabled(true);
                
                //Debug.Log(($"Successfully rerolled image for {item.scriptableObject.name}");
            }
            else
            {
                item.statusLabel.text = "Reroll Failed";
                item.statusLabel.style.color = new Color(0.8f, 0.2f, 0.2f);
                item.hasFailed = true;
                item.failureReason = "Reroll failed";
                
                Debug.LogError($"Failed to reroll image for {item.scriptableObject.name}");
            }

            // Re-enable buttons
            item.rerollBtn.text = "Reroll";
            item.rerollBtn.SetEnabled(true);
            item.isGenerating = false;
            
            UpdateReviewStatus();
        }

        private void ApproveAllImages()
        {
            foreach (var item in imageReviewItems.Where(i => !i.isGenerating && !i.hasFailed))
            {
                if (!item.isApproved)
                {
                    ApproveImage(item);
                }
            }
        }

        private void SaveApprovedImages()
        {
            var approvedItems = imageReviewItems.Where(i => i.isApproved && !i.hasFailed).ToList();
            if (approvedItems.Count == 0)
            {
                ShowStatusMessage("No images have been approved for saving.", "warning");
                return;
            }

            EditorCoroutineUtility.StartCoroutine(SaveApprovedImagesCoroutine(approvedItems), editorWindow);
        }

        private IEnumerator SaveApprovedImagesCoroutine(List<ImageReviewItem> approvedItems)
        {
            ShowLoading($"Saving {approvedItems.Count} approved images...");
            
            // Ensure save directory exists
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            for (int i = 0; i < approvedItems.Count; i++)
            {
                var item = approvedItems[i];
                loadingText.text = $"Saving {item.scriptableObject.name}... ({i + 1}/{approvedItems.Count})";
                UpdateLoadingProgress((float)i / approvedItems.Count);
                
                // Force UI updates during saving
                ForceEditorUpdate();

                var fileName = GetFileName(item.scriptableObject);
                var uniqueFileName = GetUniqueFileName(fileName);
                var filePath = Path.Combine(saveDirectory, uniqueFileName + ".png");

                //Debug.Log(($"Saving image for {item.scriptableObject.name} to {filePath}");

                // Save texture as PNG
                var bytes = item.texture.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);
                
                // Wait a moment for file system
                yield return new WaitForSecondsRealtime(0.1f);

                // Refresh asset database
                AssetDatabase.Refresh();
                yield return null;
                ForceEditorUpdate();

                // Wait for asset to be imported
                yield return new WaitForSecondsRealtime(0.3f);

                // Configure texture import settings first
                var importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    
                    // Wait for reimport
                    yield return new WaitForSecondsRealtime(0.5f);
                    ForceEditorUpdate();
                }

                // Load as sprite and assign
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
                if (sprite != null && selectedSpriteMember != null)
                {
                    //Debug.Log(($"Assigning sprite to {selectedSpriteMember.Name} of {item.scriptableObject.name}");
                    SetMemberValue(selectedSpriteMember, item.scriptableObject, sprite);
                    EditorUtility.SetDirty(item.scriptableObject);
                    
                    //Debug.Log(($"Successfully saved and assigned sprite for {item.scriptableObject.name} at {filePath}");
                }
                else
                {
                    Debug.LogError($"Failed to load sprite from {filePath} for {item.scriptableObject.name}. Sprite: {sprite}, SelectedSpriteMember: {selectedSpriteMember?.Name}");
                }
            }

            UpdateLoadingProgress(1f);
            yield return new WaitForSecondsRealtime(0.5f);
            
            // Ensure loading hides with force updates
            EditorCoroutineUtility.StartCoroutine(EnsureLoadingHides(), editorWindow);

            // Hide image review and show success
            HideImageReview();
            ShowStatusMessage($"Successfully saved {approvedItems.Count} images to ScriptableObjects!", "success");
            
            // Save preferences
            SavePreferences();
        }

        private void UpdateReviewStatus()
        {
            var totalItems = imageReviewItems.Count;
            var approvedItems = imageReviewItems.Count(i => i.isApproved);
            var failedItems = imageReviewItems.Count(i => i.hasFailed);
            var generatingItems = imageReviewItems.Count(i => i.isGenerating);
            
            reviewStatusLabel.text = $"{approvedItems} of {totalItems} approved";
            if (failedItems > 0)
            {
                reviewStatusLabel.text += $" ({failedItems} failed)";
            }
            if (generatingItems > 0)
            {
                reviewStatusLabel.text += $" ({generatingItems} generating)";
            }
            
            // Enable/disable save button
            saveApprovedBtn.SetEnabled(approvedItems > 0);
            
            if (approvedItems > 0)
            {
                saveApprovedBtn.text = $"Save {approvedItems} Approved Images";
            }
            else
            {
                saveApprovedBtn.text = "Save Approved Images";
            }
        }

        private void HideImageReview()
        {
            imageReviewContainer.style.display = DisplayStyle.None;
            imageReviewItems.Clear();
        }

        private string GetUniqueFileName(string baseFileName)
        {
            string fileName = baseFileName;
            string filePath = Path.Combine(saveDirectory, fileName + ".png");
            int counter = 1;

            // Check if file exists and increment counter until we find a unique name
            while (File.Exists(filePath))
            {
                fileName = $"{baseFileName}_{counter}";
                filePath = Path.Combine(saveDirectory, fileName + ".png");
                counter++;
            }

            return fileName;
        }

        private string GetFileName(ScriptableObject so)
        {
            if (selectedNamingMember != null)
            {
                try
                {
                    var value = GetMemberValue(selectedNamingMember, so);
                    if (value != null)
                    {
                        return value.ToString().Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not read naming member: {ex.Message}");
                }
            }

            return so.name.Replace(" ", "_");
        }

        private bool ValidateConfiguration()
        {
            if (scriptableObjects.Count == 0)
            {
                ShowStatusMessage("Please add ScriptableObjects first.", "error");
                return false;
            }

            if (selectedInfoMembers.Count == 0)
            {
                ShowStatusMessage("Please select at least one info property.", "error");
                return false;
            }

            if (string.IsNullOrEmpty(spritePropertyDropdown.value))
            {
                ShowStatusMessage("Please select a sprite property.", "error");
                return false;
            }

            if (isAIPromptMode && generatedPrompts.Count == 0)
            {
                ShowStatusMessage("Please generate prompts first.", "error");
                return false;
            }

            if (!isAIPromptMode && string.IsNullOrEmpty(templatePromptField.value))
            {
                ShowStatusMessage("Please provide a prompt template.", "error");
                return false;
            }

            // Update selected members
            selectedSpriteMember = availableMembers.FirstOrDefault(m => m.Name == spritePropertyDropdown.value);
            selectedNamingMember = availableMembers.FirstOrDefault(m => m.Name == namingPropertyDropdown.value);

            //Debug.Log(($"Selected sprite member: {selectedSpriteMember?.Name ?? "NULL"} on dropdown value: {spritePropertyDropdown.value}");
            //Debug.Log(($"Available members count: {availableMembers.Count}");

            if (selectedSpriteMember == null)
            {
                ShowStatusMessage("Could not find the selected sprite property. Please reselect it.", "error");
                return false;
            }

            // Update save directory
            saveDirectory = savePathField.value;
            if (string.IsNullOrEmpty(saveDirectory))
            {
                saveDirectory = "Assets/GeneratedSprites/";
            }
            
            // Ensure directory ends with slash
            if (!saveDirectory.EndsWith("/") && !saveDirectory.EndsWith("\\"))
                saveDirectory += "/";
            
            // Create directory if it doesn't exist (only for paths within Assets)
            if (saveDirectory.StartsWith("Assets/"))
            {
                var fullPath = Application.dataPath + saveDirectory.Substring(6); // Remove "Assets"
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                // For external paths, just ensure the directory exists
                if (!Directory.Exists(saveDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(saveDirectory);
                    }
                    catch (Exception ex)
                    {
                        ShowStatusMessage($"Cannot create directory: {ex.Message}", "error");
                        return false;
                    }
                }
            }

            return true;
        }

        private void UpdateBatchProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            progressFill.style.width = Length.Percent(progress * 100);
        }

        private void UpdateLoadingProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            loadingProgressFill.style.width = Length.Percent(progress * 100);
        }

        private void ShowLoading(string message)
        {
            loadingText.text = message;
            loadingOverlay.style.display = DisplayStyle.Flex;
        }

        private void HideLoading()
        {
            loadingOverlay.style.display = DisplayStyle.None;
        }

        private void ShowStatusMessage(string message, string type)
        {
            //Debug.Log(($"[BatchImageGenerator] {message}");
            
            if (type == "error")
            {
                EditorUtility.DisplayDialog("Error", message, "OK");
            }
        }

        private void LoadPreferences()
        {
            var savedPath = EditorPrefs.GetString("BatchImageGenerator_SaveDirectory", "Assets/GeneratedSprites/");
            savePathField.value = savedPath;
            
            var savedModel = EditorPrefs.GetString("BatchImageGenerator_Model", "GPT Image 1");
            if (modelDropdown.choices.Contains(savedModel))
            {
                modelDropdown.value = savedModel;
                selectedModel = StringToModel(savedModel);
                UpdateSizeDropdown();
            }

            // Load last generation time
            lastGenerationTime = EditorPrefs.GetFloat("BatchImageGenerator_LastGenerationTime", 30f);
        }

        private void SavePreferences()
        {
            EditorPrefs.SetString("BatchImageGenerator_SaveDirectory", savePathField.value);
            EditorPrefs.SetString("BatchImageGenerator_Model", modelDropdown.value);
            EditorPrefs.SetFloat("BatchImageGenerator_LastGenerationTime", lastGenerationTime);
        }
    }
}