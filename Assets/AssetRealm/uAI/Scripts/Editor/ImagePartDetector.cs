using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Mathematics;
using UnityEditor.UIElements;

namespace UAI
{
    public class ImagePartDetector : EditorWindow
    {

        public ObjectField sourceImageField;
        private Texture2D sourceTexture;
        private List<PartData> detectedParts = new List<PartData>();
        private List<PartGroup> partGroups = new List<PartGroup>();

        // UI Elements
        private VisualElement previewContainer;
        private ScrollView partsScrollView;
        private Slider paddingSlider;
        private Slider thresholdSlider;
        private ColorField backgroundColorField;
        private Toggle autoDetectBackgroundToggle;
        private Toggle markAsSpritesToggle;
        private IntegerField minWidthField;
        private IntegerField minHeightField;
        private IntegerField groupingDistanceField;
        private Label statusLabel;
        private Label partsCountLabel;
        private Button selectAllButton;
        private Button deselectAllButton;
        private Button groupSelectedButton;

        private Texture2D lastAutoAdjustedTexture;


        // Settings
        private float padding = 5f;
        private float threshold = 0.20f;
        private Color backgroundColor = Color.white;
        private bool autoDetectBackground = true;
        private bool markAsSprites = true;
        private int minWidth = 10;
        private int minHeight = 10;
        private int groupingDistance = 10;

        
        [MenuItem("Assets/uAI/Part Detector and Splitter", false, 1039)]
        private static void ColorCorrectImage()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject); 

            OpenWithImage(path);
        }

        // Validation method to control when the menu is shown
        [MenuItem("Assets/uAI/Part Detector and Splitter", true)]
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

        [MenuItem("Tools/uAI Creator/Image Part Detector", false, 51)]
        public static void ShowWindow()
        {
            var window = GetWindow<ImagePartDetector>();
            window.titleContent = new GUIContent("Image Part Detector");
            window.minSize = new Vector2(800, 600);
        }

        public static void OpenWithImage(string imagePath)
        {
            ImagePartDetector wnd = GetWindow<ImagePartDetector>();
            wnd.titleContent = new GUIContent("Image Part Detector");
            wnd.minSize = new Vector2(800, 600);

            Texture2D texture = null;
            string projectDataPath = Application.dataPath;

            if (Path.IsPathRooted(imagePath) && imagePath.StartsWith(projectDataPath, StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = "Assets" + imagePath.Substring(projectDataPath.Length);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
            }

            if (texture == null)
            {
                byte[] fileData = File.ReadAllBytes(imagePath);
                texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                texture.name = Path.GetFileNameWithoutExtension(imagePath);
            }

            if (wnd.sourceImageField == null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (wnd.sourceImageField != null)
                        wnd.sourceImageField.value = texture;
                    else
                        Debug.LogError("Texture field not found after delay. Manual assignment might be needed or ensure CreateGUI runs first.");

                    wnd.sourceTexture = texture;
                    if (wnd.sourceTexture != null)
                    {
                        wnd.ProcessImage();

                        wnd.AutoAdjustSettings(wnd.sourceTexture);
                    }
                };
                return;
            }

            wnd.sourceImageField.value = texture;
            wnd.sourceTexture = wnd.sourceImageField.value as Texture2D;
            
            if (wnd.sourceTexture != null)
            {
                wnd.ProcessImage();
                    
                wnd.AutoAdjustSettings(wnd.sourceTexture);
            }
        }



        private void CreateGUI()
        {
            var root = rootVisualElement;

            var visualTree = Resources.Load<VisualTreeAsset>("ImagePartDetectorWindow");
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
            }
            else
            {
                Debug.LogError("Could not find ImagePartDetectorWindow.uxml. Please ensure it's in the Resources folder.");
                return;
            }

            var styleSheet = Resources.Load<StyleSheet>("ImagePartDetector");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            sourceImageField = root.Q<ObjectField>("source-image-field");
            autoDetectBackgroundToggle = root.Q<Toggle>("auto-detect-toggle");
            backgroundColorField = root.Q<ColorField>("background-color-field");
            thresholdSlider = root.Q<Slider>("threshold-slider");
            paddingSlider = root.Q<Slider>("padding-slider");
            minWidthField = root.Q<IntegerField>("min-width-field");
            minHeightField = root.Q<IntegerField>("min-height-field");
            groupingDistanceField = root.Q<IntegerField>("grouping-distance-field");
            selectAllButton = root.Q<Button>("select-all-button");
            deselectAllButton = root.Q<Button>("deselect-all-button");
            markAsSpritesToggle = root.Q<Toggle>("mark-sprites-toggle");
            statusLabel = root.Q<Label>("status-label");
            partsCountLabel = root.Q<Label>("parts-count-label");
            partsScrollView = root.Q<ScrollView>("parts-scroll-view");
            previewContainer = root.Q<VisualElement>("preview-container");
            groupSelectedButton = root.Q<Button>("group-selected-button");

            backgroundColorField.SetEnabled(!autoDetectBackground);

            sourceImageField.objectType = typeof(Texture2D);
            sourceImageField.RegisterValueChangedCallback(evt =>
            {
                sourceTexture = evt.newValue as Texture2D;
                lastAutoAdjustedTexture = null;  
                
                if (sourceTexture != null)
                {
                    ProcessImage();
                }
                else
                {
                    detectedParts.Clear();
                    partGroups.Clear();
                    UpdatePartPreviews();
                    UpdateStatus("No image loaded.");
                    UpdatePartsCount();
                }
            });

            autoDetectBackgroundToggle.RegisterValueChangedCallback(evt =>
            {
                autoDetectBackground = evt.newValue;
                backgroundColorField.SetEnabled(!autoDetectBackground);
                if (sourceTexture != null)
                    ProcessImage();
            });

            backgroundColorField.RegisterValueChangedCallback(evt =>
            {
                backgroundColor = evt.newValue;
                if (sourceTexture != null && !autoDetectBackground)
                    ProcessImage();
            });

            thresholdSlider.RegisterValueChangedCallback(evt =>
            {
                threshold = evt.newValue;
                if (sourceTexture != null)
                    ProcessImage();
            });

            paddingSlider.RegisterValueChangedCallback(evt =>
            {
                padding = evt.newValue;
                UpdatePartPreviews();
            });

            minWidthField.RegisterValueChangedCallback(evt =>
            {
                minWidth = evt.newValue;
                if (sourceTexture != null)
                    ProcessImage();
            });

            minHeightField.RegisterValueChangedCallback(evt =>
            {
                minHeight = evt.newValue;
                if (sourceTexture != null)
                    ProcessImage();
            });

            groupingDistanceField.RegisterValueChangedCallback(evt =>
            {
                groupingDistance = evt.newValue;
                if (sourceTexture != null)
                    ProcessImage();
            });

            selectAllButton.clicked += () =>
            {
                foreach (var group in partGroups)
                    group.selected = true;
                UpdatePartPreviews();
            };

            deselectAllButton.clicked += () =>
            {
                foreach (var group in partGroups)
                    group.selected = false;
                UpdatePartPreviews();
            };

            if (groupSelectedButton != null)
            {
                groupSelectedButton.clicked += GroupSelectedParts;
            }

            markAsSpritesToggle.RegisterValueChangedCallback(evt =>
            {
                markAsSprites = evt.newValue;
            });

            root.Q<Button>("export-all-button").clicked += () => ExportParts(true);
            root.Q<Button>("export-selected-button").clicked += () => ExportParts(false);

            if (sourceImageField.value != null) sourceTexture = sourceImageField.value as Texture2D;
            autoDetectBackground = autoDetectBackgroundToggle.value;
            backgroundColor = backgroundColorField.value;
            threshold = thresholdSlider.value;
            padding = paddingSlider.value;
            minWidth = minWidthField.value;
            minHeight = minHeightField.value;
            groupingDistance = groupingDistanceField.value;
            markAsSprites = markAsSpritesToggle.value;

            if (sourceTexture != null)
            {
                ProcessImage();
            }
        }

        private void ProcessImage()
        {
            if (sourceTexture == null) return;

            detectedParts.Clear();
            partGroups.Clear();

            var readableTexture = MakeTextureReadable(sourceTexture);
            if (readableTexture == null)
            {
                UpdateStatus("Failed to make texture readable.");
                return;
            }

            // Only auto-adjust settings when a new image is loaded
            if (lastAutoAdjustedTexture != sourceTexture)
            {
                AutoAdjustSettings(readableTexture);
                lastAutoAdjustedTexture = sourceTexture;
            }

            if (autoDetectBackground)
            {
                backgroundColor = DetectBackgroundColor(readableTexture);
                backgroundColorField.SetValueWithoutNotify(backgroundColor);
            }

            var components = FindConnectedComponents(readableTexture, backgroundColor, threshold);
 
            var componentPixelSets = components.Select(c => new HashSet<Vector2Int>(c.pixels)).ToList();

            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];
                var pixelSet = componentPixelSets[i];

                var width = component.bounds.width + 1;
                var height = component.bounds.height + 1;

                if (width < minWidth || height < minHeight)
                    continue;
 
                var outline = new List<Vector2Int>();
                foreach (var pixel in component.pixels)
                { 
                    if (!pixelSet.Contains(new Vector2Int(pixel.x + 1, pixel.y)) ||
                        !pixelSet.Contains(new Vector2Int(pixel.x - 1, pixel.y)) ||
                        !pixelSet.Contains(new Vector2Int(pixel.x, pixel.y + 1)) ||
                        !pixelSet.Contains(new Vector2Int(pixel.x, pixel.y - 1)))
                    {
                        outline.Add(pixel);
                    }
                } 

                float sumX = 0;
                float sumY = 0;
                foreach (var pixel in component.pixels)
                {
                    sumX += pixel.x;
                    sumY += pixel.y;
                }

                var partData = new PartData
                {
                    bounds = component.bounds,
                    pixels = component.pixels,
                    outlinePixels = outline,  
                    selected = true,
                    medianPosition = new Vector2(sumX / component.pixels.Count, sumY / component.pixels.Count)
                };

                detectedParts.Add(partData);
            }

            GroupPartsByDistance(); 

            UpdatePartPreviews();
            UpdateStatus($"Detected {detectedParts.Count} raw parts, forming {partGroups.Count} groups.");
            UpdatePartsCount();
        }

        private bool HasTransparentBackground(Texture2D texture)
        { 
            int w = texture.width;
            int h = texture.height;
            
            var edgePixels = new List<Color>();
             
            edgePixels.Add(texture.GetPixel(0, 0));
            edgePixels.Add(texture.GetPixel(w - 1, 0));
            edgePixels.Add(texture.GetPixel(0, h - 1));
            edgePixels.Add(texture.GetPixel(w - 1, h - 1));
             
            int sampleCount = Mathf.Min(10, w / 4); 
            for (int i = 1; i < sampleCount; i++)
            {
                int x = (w * i) / sampleCount;
                int y = (h * i) / sampleCount;
                
                edgePixels.Add(texture.GetPixel(x, 0)); 
                edgePixels.Add(texture.GetPixel(x, h - 1)); 
                edgePixels.Add(texture.GetPixel(0, y)); 
                edgePixels.Add(texture.GetPixel(w - 1, y)); 
            }
            
            // Check if majority of edge pixels are transparent or nearly transparent
            int transparentCount = edgePixels.Count(p => p.a < 0.1f);
            return transparentCount >= edgePixels.Count * 0.6f; 
        }

        private void AutoAdjustSettings(Texture2D texture)
        {
            if (texture == null) return;
            
            bool hasTransparentBg = HasTransparentBackground(texture);
            bool isSmallImage = texture.width <= 128 || texture.height <= 128;
            bool isTinyImage = texture.width <= 64 || texture.height <= 64;
            
            // Store original values to show what changed
            float originalThreshold = threshold;
            float originalPadding = padding;
            int originalGroupingDistance = groupingDistance;
            int originalMinWidth = minWidth;
            int originalMinHeight = minHeight;

            if (hasTransparentBg)
            {
                threshold = 0f;
                padding = 0f;

                // Update UI sliders
                thresholdSlider.SetValueWithoutNotify(threshold);
                paddingSlider.SetValueWithoutNotify(padding);
            }
            else
            {
                // Reset to default values for non-transparent backgrounds
                threshold = 0.20f;
                padding = 5f;

                // Update UI sliders
                thresholdSlider.SetValueWithoutNotify(threshold);
                paddingSlider.SetValueWithoutNotify(padding);
            }

            if (isSmallImage)
            {
                // Small images
                groupingDistance = 0;
                minWidth = 5;
                minHeight = 5;

                // Update UI fields
                groupingDistanceField.SetValueWithoutNotify(groupingDistance);
                minWidthField.SetValueWithoutNotify(minWidth);
                minHeightField.SetValueWithoutNotify(minHeight);
            }
            else
            {
                // Larger images
                groupingDistance = 10;
                minWidth = 10;
                minHeight = 10;

                // Update UI fields
                groupingDistanceField.SetValueWithoutNotify(groupingDistance);
                minWidthField.SetValueWithoutNotify(minWidth);
                minHeightField.SetValueWithoutNotify(minHeight);
            }
            
            // Create status message
            var adjustments = new List<string>();
            if (hasTransparentBg)
                adjustments.Add("transparent background detected (threshold/padding → 0)");
            if (isSmallImage)
            {
                string sizeCategory = isTinyImage ? "tiny" : "small";
                adjustments.Add($"{sizeCategory} image detected (reduced min sizes/grouping)");
            }
            
            if (adjustments.Count > 0)
            {
                UpdateStatus($"Auto-adjusted: {string.Join(", ", adjustments)}. Image: {texture.width}x{texture.height}");
                 
            }
        }
        private float CalculatePixelDistance(PartData part1, PartData part2, float maxDistance)
        { 
            float bbDist = CalculateEdgeDistance(part1.bounds, part2.bounds);
            if (bbDist > maxDistance)
            {
                return float.MaxValue;  
            }
        
            float minSqDistance = float.MaxValue;
            float maxSqDistance = maxDistance * maxDistance;

            foreach (var p1 in part1.outlinePixels)
            {
                foreach (var p2 in part2.outlinePixels)
                {
                    float sqDist = (p1 - p2).sqrMagnitude;
                    if (sqDist < minSqDistance)
                    {
                        minSqDistance = sqDist;
                    }
                }
            }
        
            if (minSqDistance <= maxSqDistance)
            {
                return Mathf.Sqrt(minSqDistance);
            }

            return float.MaxValue;
        }

        private void GroupPartsByDistance()
        {
            partGroups.Clear();

            if (detectedParts.Count == 0) return;
        
            if (groupingDistance <= 0)
            {
                foreach (var part in detectedParts)
                {
                    var group = new PartGroup { selected = true };
                    group.parts.Add(part);
                    partGroups.Add(group);
                }
                return;
            }

            List<PartData> remainingParts = new List<PartData>(detectedParts);
            
            while (remainingParts.Count > 0)
            { 
                var newGroup = new PartGroup { selected = true };
                var seedPart = remainingParts[0];
                newGroup.parts.Add(seedPart);
                remainingParts.RemoveAt(0);
        
                var partsToCheck = new Queue<PartData>();
                partsToCheck.Enqueue(seedPart);
        
                while (partsToCheck.Count > 0)
                {
                    var currentPart = partsToCheck.Dequeue();
                    
                    for (int i = remainingParts.Count - 1; i >= 0; i--)
                    {
                        var otherPart = remainingParts[i]; 
                        float distance = CalculatePixelDistance(currentPart, otherPart, groupingDistance);

                        if (distance <= groupingDistance)
                        { 
                            newGroup.parts.Add(otherPart);
                            partsToCheck.Enqueue(otherPart);
                            remainingParts.RemoveAt(i);
                        }
                    }
                }
                
                partGroups.Add(newGroup);
            }
        }

 

        private void GroupSelectedParts()
        {
            List<PartGroup> selectedGroupsToMerge = partGroups.Where(g => g.selected).ToList();

            if (selectedGroupsToMerge.Count < 2)
            {
                UpdateStatus("Select at least two groups to merge.");
                return;
            }

            var newCombinedGroup = new PartGroup { selected = false };
            foreach (var selectedGroup in selectedGroupsToMerge)
            {
                newCombinedGroup.parts.AddRange(selectedGroup.parts);
                partGroups.Remove(selectedGroup);
            }
            partGroups.Add(newCombinedGroup);

            foreach (var group in partGroups)
            {
                group.selected = false;
            }

            UpdatePartPreviews();
            UpdateStatus($"Manually merged {selectedGroupsToMerge.Count} groups. All groups now deselected.");
            UpdatePartsCount();
        }

        private float CalculateEdgeDistance(Rect rect1, Rect rect2)
        {
            // Check for overlap first
            if (rect1.Overlaps(rect2)) return 0;

            float xDistance = 0;
            float yDistance = 0;

            // Calculate horizontal distance
            if (rect1.xMax < rect2.xMin)
                xDistance = rect2.xMin - rect1.xMax;
            else if (rect2.xMax < rect1.xMin)
                xDistance = rect1.xMin - rect2.xMax;

            // Calculate vertical distance
            if (rect1.yMax < rect2.yMin)
                yDistance = rect2.yMin - rect1.yMax;
            else if (rect2.yMax < rect1.yMin)
                yDistance = rect1.yMin - rect2.yMax;

            float distance = Mathf.Sqrt(xDistance * xDistance + yDistance * yDistance);
            

            return distance;
        }

        private void UpdatePartPreviews()
        {
            previewContainer.Clear();

            if (sourceTexture == null && detectedParts.Count == 0 && partGroups.Count == 0)
            {
                UpdatePartsCount();
                return;
            }
            if (sourceTexture == null)
            {
                UpdateStatus("Source texture is missing for preview update.");
                return;
            }

            var readableTexture = MakeTextureReadable(sourceTexture);
            if (readableTexture == null)
            {
                UpdateStatus("Failed to make texture readable for previews.");
                return;
            }

            for (int i = 0; i < partGroups.Count; i++)
            {
                var group = partGroups[i];
                var groupElement = CreateGroupPreview(readableTexture, group, i);
                previewContainer.Add(groupElement);
            }

            UpdatePartsCount();
        }

        private void UpdatePartsCount()
        {
            if (partsCountLabel != null)
            {
                var selectedGroupCount = partGroups.Count(g => g.selected);
                int totalPartsInGroups = partGroups.Sum(g => g.parts.Count);
                partsCountLabel.text = $"Groups: {partGroups.Count} | Selected: {selectedGroupCount} | Parts in Groups: {totalPartsInGroups}";
            }
        }

        private VisualElement CreateGroupPreview(Texture2D source, PartGroup group, int index)
        {
            var container = new VisualElement();
            container.AddToClassList("part-preview");
            if (group.selected)
                container.AddToClassList("selected");

            var groupTexture = ExtractGroup(source, group, (int)padding);

            var image = new Image();
            image.image = groupTexture;
            image.style.width = 150;
            image.style.height = 150;
            image.scaleMode = ScaleMode.ScaleToFit;
            container.Add(image);

            var bounds = GetGroupBounds(group);
            var infoText = $"Group {index + 1}\n{(int)(bounds.width + 1)}x{(int)(bounds.height + 1)}\n{group.parts.Count} part{(group.parts.Count > 1 ? "s" : "")}";
            var infoLabel = new Label(infoText);
            infoLabel.AddToClassList("part-info");
            container.Add(infoLabel);

            // Add name input field
            var nameField = new TextField("Name:");
            nameField.value = group.name ?? "";
            nameField.AddToClassList("part-name-field");
            nameField.RegisterValueChangedCallback(evt =>
            {
                group.name = evt.newValue;
            });
            
            // Prevent click events from bubbling up to the container
            nameField.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
            });
            
            container.Add(nameField);

            var toggle = new Toggle();
            toggle.value = group.selected;
            toggle.RegisterValueChangedCallback(evt =>
            {
                group.selected = evt.newValue;
                if (group.selected)
                {
                    // Set blue border directly in code
                    container.style.borderTopColor = new Color(0.35f, 0.62f, 0.83f, 1f);  
                    container.style.borderBottomColor = new Color(0.35f, 0.62f, 0.83f, 1f);
                    container.style.borderLeftColor = new Color(0.35f, 0.62f, 0.83f, 1f);
                    container.style.borderRightColor = new Color(0.35f, 0.62f, 0.83f, 1f);
                    container.style.borderTopWidth = 2;
                    container.style.borderBottomWidth = 2;
                    container.style.borderLeftWidth = 2;
                    container.style.borderRightWidth = 2;
                    container.style.backgroundColor = new Color(0.23f, 0.23f, 0.23f, 1f);
                    
                    // If selected and no name, focus the name field
                    if (string.IsNullOrWhiteSpace(group.name))
                    {
                        nameField.Focus();
                        nameField.SelectAll();
                    }
                }
                else
                {
                    // Reset to default appearance
                    container.style.borderTopColor = Color.clear;
                    container.style.borderBottomColor = Color.clear;
                    container.style.borderLeftColor = Color.clear;
                    container.style.borderRightColor = Color.clear;
                    container.style.borderTopWidth = 2;
                    container.style.borderBottomWidth = 2;
                    container.style.borderLeftWidth = 2;
                    container.style.borderRightWidth = 2;
                    container.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f); 
                }
                UpdatePartsCount();
            });
            container.Add(toggle);

            container.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == toggle || (evt.target is VisualElement ve && ve.ClassListContains("unity-toggle__checkmark"))) return;
                
                // Don't toggle when clicking name field or its input element
                if (evt.target == nameField) return;
                var inputElement = nameField.Q<VisualElement>(className: "unity-text-field__input");
                if (evt.target == inputElement) return;

                group.selected = !group.selected;
                toggle.SetValueWithoutNotify(group.selected);

                if (group.selected)
                {
                    // Set blue border directly in code
                    container.style.borderTopColor = new Color(0.35f, 0.62f, 0.83f, 1f); 
                    container.style.borderBottomColor = new Color(0.35f, 0.62f, 0.83f, 1f);
                    container.style.borderLeftColor = new Color(0.35f, 0.62f, 0.83f, 1f);
                    container.style.borderRightColor = new Color(0.35f, 0.62f, 0.83f, 1f);
                    container.style.borderTopWidth = 2;
                    container.style.borderBottomWidth = 2;
                    container.style.borderLeftWidth = 2;
                    container.style.borderRightWidth = 2;
                    container.style.backgroundColor = new Color(0.23f, 0.23f, 0.23f, 1f); 
                    
                    // If selected and no name, focus the name field
                    if (string.IsNullOrWhiteSpace(group.name))
                    {
                        nameField.Focus();
                        nameField.SelectAll();
                    }
                }
                else
                {
                    // Reset to default appearance
                    container.style.borderTopColor = Color.clear;
                    container.style.borderBottomColor = Color.clear;
                    container.style.borderLeftColor = Color.clear;
                    container.style.borderRightColor = Color.clear;
                    container.style.borderTopWidth = 2;
                    container.style.borderBottomWidth = 2;
                    container.style.borderLeftWidth = 2;
                    container.style.borderRightWidth = 2;
                    container.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f); 
                }

                UpdatePartsCount();
            });

            return container;
        }

        private Rect GetGroupBounds(PartGroup group)
        {
            if (group.parts.Count == 0) return new Rect();

            var bounds = group.parts[0].bounds;
            for (int i = 1; i < group.parts.Count; i++)
            {
                var partBounds = group.parts[i].bounds;
                bounds.xMin = Mathf.Min(bounds.xMin, partBounds.xMin);
                bounds.yMin = Mathf.Min(bounds.yMin, partBounds.yMin);
                bounds.xMax = Mathf.Max(bounds.xMax, partBounds.xMax);
                bounds.yMax = Mathf.Max(bounds.yMax, partBounds.yMax);
            }

            return bounds;
        }

        private Texture2D ExtractGroup(Texture2D source, PartGroup group, int currentPadding)
        {
            var groupBounds = GetGroupBounds(group);

            int minX = Mathf.Max(0, (int)groupBounds.xMin - currentPadding);
            int minY = Mathf.Max(0, (int)groupBounds.yMin - currentPadding);
            int maxX = Mathf.Min(source.width - 1, (int)groupBounds.xMax + currentPadding);
            int maxY = Mathf.Min(source.height - 1, (int)groupBounds.yMax + currentPadding);

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            if (width <= 0 || height <= 0)
            {
                var emptyTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
                emptyTexture.SetPixel(0, 0, Color.clear);
                emptyTexture.Apply();
                return emptyTexture;
            }

            var groupTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            groupTexture.filterMode = FilterMode.Point; 
            var pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            foreach (var part in group.parts)
            {
                foreach (var pixelPos in part.pixels)
                {
                    int localX = pixelPos.x - minX;
                    int localY = pixelPos.y - minY;

                    if (localX >= 0 && localX < width && localY >= 0 && localY < height)
                    {
                        pixels[localY * width + localX] = source.GetPixel(pixelPos.x, pixelPos.y);
                    }
                }
            }

            groupTexture.SetPixels(pixels);
            groupTexture.Apply();

            return groupTexture;
        }

        private Color DetectBackgroundColor(Texture2D texture)
        {
            var samples = new List<Color>();
            int w = texture.width;
            int h = texture.height;

            samples.Add(texture.GetPixel(0, 0));
            samples.Add(texture.GetPixel(w - 1, 0));
            samples.Add(texture.GetPixel(0, h - 1));
            samples.Add(texture.GetPixel(w - 1, h - 1));

            samples.Add(texture.GetPixel(w / 2, 0));
            samples.Add(texture.GetPixel(w / 2, h - 1));
            samples.Add(texture.GetPixel(0, h / 2));
            samples.Add(texture.GetPixel(w - 1, h / 2));

            var colorGroups = samples.GroupBy(c => new { r = Mathf.RoundToInt(c.r * 255), g = Mathf.RoundToInt(c.g * 255), b = Mathf.RoundToInt(c.b * 255) })
                .Select(g => new { Color = new Color(g.Key.r / 255f, g.Key.g / 255f, g.Key.b / 255f), Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            return colorGroups.Count > 0 ? colorGroups.First().Color : Color.white;
        }

        private List<Component> FindConnectedComponents(Texture2D texture, Color bg, float colorThreshold)
        {
            int w = texture.width;
            int h = texture.height;
            var visited = new bool[w, h];
            var components = new List<Component>();
            var texturePixels = texture.GetPixels();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (visited[x, y]) continue;

                    var pixelColor = texturePixels[y * w + x];
                    if (ColorDistance(pixelColor, bg) < colorThreshold || pixelColor.a < 0.01f)
                    {
                        visited[x, y] = true;
                        continue;
                    }

                    var component = new Component();
                    FloodFill(texturePixels, visited, x, y, w, h, bg, colorThreshold, component);

                    if (component.pixels.Count > 0)
                    {
                        components.Add(component);
                    }
                }
            }

            return components;
        }

        private void FloodFill(Color[] allPixels, bool[,] visited, int startX, int startY, int w, int h, Color bgColor, float colorThreshold, Component currentComponent)
        {
            var q = new Queue<Vector2Int>();
            q.Enqueue(new Vector2Int(startX, startY));

            // Initialize bounds with the starting pixel
            currentComponent.bounds = new Rect(startX, startY, 0, 0);

            while (q.Count > 0)
            {
                var pos = q.Dequeue();
                int x = pos.x;
                int y = pos.y;

                if (x < 0 || x >= w || y < 0 || y >= h || visited[x, y]) continue;

                var pixelColor = allPixels[y * w + x];
                if (ColorDistance(pixelColor, bgColor) < colorThreshold || pixelColor.a < 0.01f)
                {
                    visited[x, y] = true;
                    continue;
                }

                visited[x, y] = true;
                currentComponent.pixels.Add(pos);

                currentComponent.bounds.xMin = Mathf.Min(currentComponent.bounds.xMin, x);
                currentComponent.bounds.xMax = Mathf.Max(currentComponent.bounds.xMax, x);
                currentComponent.bounds.yMin = Mathf.Min(currentComponent.bounds.yMin, y);
                currentComponent.bounds.yMax = Mathf.Max(currentComponent.bounds.yMax, y);

                q.Enqueue(new Vector2Int(x + 1, y));
                q.Enqueue(new Vector2Int(x - 1, y));
                q.Enqueue(new Vector2Int(x, y + 1));
                q.Enqueue(new Vector2Int(x, y - 1));
            }
        }

        private float ColorDistance(Color a, Color b)
        { 
            float rgbDist = Mathf.Sqrt(Mathf.Pow(a.r - b.r, 2) + Mathf.Pow(a.g - b.g, 2) + Mathf.Pow(a.b - b.b, 2));
            float alphaDist = Mathf.Abs(a.a - b.a);

            // If the background is determined by transparency (alpha is low).
            if (b.a < 0.1f)
            { 
                return alphaDist > 0.01f ? 1.0f : 0.0f;
            }
 
            return rgbDist;
        }

        private bool ValidateSelectedPartsHaveNames()
        {
            var selectedGroupsWithoutNames = partGroups.Where(g => g.selected && string.IsNullOrWhiteSpace(g.name)).ToList();
            
            if (selectedGroupsWithoutNames.Count > 0)
            {
                UpdateStatus($"Please name all selected parts before exporting. {selectedGroupsWithoutNames.Count} selected part(s) need names.");
                
                // Focus the first unnamed selected part
                var firstUnnamedGroup = selectedGroupsWithoutNames.First();
                var index = partGroups.IndexOf(firstUnnamedGroup);
                
                // Find the corresponding name field and focus it
                var groupPreview = previewContainer.Children().ElementAtOrDefault(index);
                if (groupPreview != null)
                {
                    var nameField = groupPreview.Q<TextField>();
                    if (nameField != null)
                    {
                        nameField.Focus();
                        nameField.SelectAll();
                    }
                }
                
                return false;
            }
            
            return true;
        }

        private void ExportParts(bool exportAll)
        {
            if (sourceTexture == null || partGroups.Count == 0)
            {
                UpdateStatus("No parts to export");
                return;
            }

            // If export all, select all parts first
            if (exportAll)
            {
                foreach (var group in partGroups)
                    group.selected = true;
                UpdatePartPreviews(); 
            }

            // Validate that selected parts have names
            if (!ValidateSelectedPartsHaveNames())
            {
                return;
            }

            var path = EditorUtility.SaveFolderPanel("Export Parts", "Assets", "Parts_" + sourceTexture.name);
            if (string.IsNullOrEmpty(path)) return;

            var readableTexture = MakeTextureReadable(sourceTexture);
            if (readableTexture == null)
            {
                UpdateStatus("Failed to make texture readable for export.");
                return;
            }
            var exportedCount = 0;
            var isInAssetsFolder = path.StartsWith(Application.dataPath);

            for (int i = 0; i < partGroups.Count; i++)
            {
                var group = partGroups[i];
                if (!group.selected) continue; 
                if (group.parts.Count == 0) continue; 

                var groupTexture = ExtractGroup(readableTexture, group, (int)padding);
                if (groupTexture == null || groupTexture.width <= 1 || groupTexture.height <= 1 && groupTexture.GetPixel(0, 0) == Color.clear)
                {
                    DestroyImmediate(groupTexture);
                    continue;
                }

                var bytes = groupTexture.EncodeToPNG();

                // Use custom name if provided, otherwise fall back to default naming
                string filename;
                if (!string.IsNullOrWhiteSpace(group.name))
                {
                    // Sanitize the name for file system
                    var sanitizedName = group.name;
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        sanitizedName = sanitizedName.Replace(c, '_');
                    }
                    filename = Path.Combine(path, $"{sanitizedName}.png");
                }
                else
                {
                    var suffix = $"_group_{i + 1:D3}";
                    filename = Path.Combine(path, $"{sourceTexture.name}{suffix}.png");
                }

                try
                {
                    File.WriteAllBytes(filename, bytes);
                    exportedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write file {filename}: {e.Message}");
                }
                finally
                {
                    DestroyImmediate(groupTexture);
                }
            }

            if (isInAssetsFolder && exportedCount > 0)
            {
                AssetDatabase.Refresh();

                if (markAsSprites)
                {
                    for (int i = 0; i < partGroups.Count; i++)
                    {
                        var group = partGroups[i];
                        if (!group.selected) continue; 
                        if (group.parts.Count == 0) continue;

                        var relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                        
                        // Use the same naming logic as above
                        string assetPath;
                        if (!string.IsNullOrWhiteSpace(group.name))
                        {
                            var sanitizedName = group.name;
                            foreach (char c in Path.GetInvalidFileNameChars())
                            {
                                sanitizedName = sanitizedName.Replace(c, '_');
                            }
                            assetPath = Path.Combine(relativePath, $"{sanitizedName}.png").Replace("\\", "/");
                        }
                        else
                        {
                            var suffix = $"_group_{i + 1:D3}";
                            assetPath = Path.Combine(relativePath, $"{sourceTexture.name}{suffix}.png").Replace("\\", "/");
                        }

                        // Wait for asset database to recognize the new file
                        EditorApplication.delayCall += () =>
                        {
                            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                            if (importer != null)
                            {
                                importer.textureType = TextureImporterType.Sprite;
                                importer.spriteImportMode = SpriteImportMode.Single;
                                importer.alphaIsTransparency = true;
                                importer.sRGBTexture = true;
                                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                                importer.mipmapEnabled = false;
                                // importer.filterMode = FilterMode.Bilinear;
                                importer.filterMode = FilterMode.Point;
                                importer.SaveAndReimport();
                            }
                            else
                            {
                                Debug.LogWarning($"Could not find importer for asset at path: {assetPath}. Sprite conversion skipped.");
                            }
                        };
                    }
                }
            }

            UpdateStatus($"Exported {exportedCount} groups to {path}");
        }

        private Texture2D MakeTextureReadable(Texture2D originalSource)
        {
            if (originalSource == null) return null;

            if (originalSource.isReadable && originalSource.format == TextureFormat.RGBA32)
            {
                var copy = new Texture2D(originalSource.width, originalSource.height, TextureFormat.RGBA32, false, false);
                copy.SetPixels(originalSource.GetPixels());
                copy.Apply();
                return copy;
            }

            RenderTexture renderTex = null;
            try
            {
                renderTex = RenderTexture.GetTemporary(originalSource.width, originalSource.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

                Graphics.Blit(originalSource, renderTex);

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTex;

                var readableTexture = new Texture2D(originalSource.width, originalSource.height, TextureFormat.RGBA32, false, false);
                readableTexture.filterMode = FilterMode.Point;
                readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                readableTexture.Apply();

                RenderTexture.active = previous;
                return readableTexture;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error making texture readable: {e.Message}");
                return null;
            }
            finally
            {
                if (renderTex != null)
                {
                    RenderTexture.ReleaseTemporary(renderTex);
                }
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
            // Debug.Log($"[ImagePartDetector] {message}");
        }

        private class PartData
        {
            public Rect bounds;
            public List<Vector2Int> pixels = new List<Vector2Int>();
            public List<Vector2Int> outlinePixels = new List<Vector2Int>();
            public bool selected;
            public Vector2 medianPosition;
        }

        private class PartGroup
        {
            public List<PartData> parts = new List<PartData>();
            public bool selected;
            public string name;
        }

        private class Component
        {
            public Rect bounds = new Rect(float.MaxValue, float.MaxValue, 0, 0);
            public List<Vector2Int> pixels = new List<Vector2Int>();

            public Component()
            {
                bounds.xMin = float.MaxValue;
                bounds.yMin = float.MaxValue;
                bounds.xMax = float.MinValue;
                bounds.yMax = float.MinValue;
            }
        }
    }
}