using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

namespace UAI
{
    public class BackgroundRemoverEditorWindow : EditorWindow
    {
        public ObjectField textureField;
        private Texture2D sourceTexture; 
        private Texture2D resultTexture;
        private Texture2D selectionOverlay;
 
        private VisualElement imageElement;
        private Label statusLabel;
        private EnumField algorithmField;
        private ColorField colorKeyField;
        private Slider toleranceSlider;
        private Slider edgeSoftnessSlider;
        private Toggle invertSelectionToggle;
        private Toggle preserveAlphaToggle;
        private Toggle adjacentToggle;
        private Button undoButton;
        private Button redoButton;
        private Button clearSelectionButton;
        private Button deleteAreaButton;
        
        // Eraser tool elements
        private Button eraserButton;
        private Slider eraserSizeSlider;
        private bool isEraserActive = false;
        private float eraserSize = 10f;
        
        // Zoom elements
        private Slider zoomSlider;
        private Button resetZoomButton;
        private float zoomLevel = 1f;
        private Vector2 panOffset = Vector2.zero;
        private bool isPanning = false;
        private Vector2 lastMousePosition;

        // Selection management
        private bool[,] currentSelection;
        private Stack<bool[,]> undoStack = new Stack<bool[,]>();
        private Stack<bool[,]> redoStack = new Stack<bool[,]>();
        private bool isAddingToSelection = false;
        private bool isRemovingFromSelection = false;
        private bool isDrawing = false;
        
        // Performance optimization
        private bool needsOverlayUpdate = false;  

        public enum RemovalAlgorithm
        {
            ColorKey,
            MagicWand,
            EdgeDetection,
            Threshold,
            FloodFill
        }

        private RemovalAlgorithm currentAlgorithm = RemovalAlgorithm.MagicWand;
        private Color keyColor = Color.green;
        private float tolerance = 0.2f;
        private float edgeSoftness = 0.0f;
        private bool invertSelection = false;
        private bool preserveAlpha = false;
        private bool adjacentOnly = true;

        [MenuItem("Assets/uAI/Remove background", false, 1040)]
        private static void ColorCorrectImage()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            OpenWithImage(path);
        }

        [MenuItem("Assets/uAI/Remove background", true)]
        private static bool ValidateColorCorrectImage()
        {
            UnityEngine.Object selected = Selection.activeObject;
            if (selected == null)
                return false;

            string path = AssetDatabase.GetAssetPath(selected);
            string ext = Path.GetExtension(path).ToLower();

            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".bmp" || ext == ".psd";
        }

        [MenuItem("Tools/uAI Creator/Background Remover", false, 52)]
        public static void ShowWindow()
        {
            var window = GetWindow<BackgroundRemoverEditorWindow>();
            window.titleContent = new GUIContent("Background Remover");
            window.minSize = new Vector2(800, 600);
        }

        public static void OpenWithImage(string imagePath)
        {
            var wnd = GetWindow<BackgroundRemoverEditorWindow>();
            wnd.titleContent = new GUIContent("Background Remover");
            wnd.minSize = new Vector2(800, 600);

            Texture2D texture = null;

            string projectDataPath = Application.dataPath;
            if (Path.IsPathRooted(imagePath) && imagePath.StartsWith(projectDataPath, StringComparison.OrdinalIgnoreCase))
            {
                string relative = "Assets" + imagePath.Substring(projectDataPath.Length);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relative);
                if (texture == null)
                    Debug.LogError($"Failed to LoadAssetAtPath(\"{relative}\").");
            }
            else
            {
                if (!File.Exists(imagePath))
                {
                    Debug.LogError($"File not found: {imagePath}");
                    return;
                }
                byte[] fileData = File.ReadAllBytes(imagePath);
                texture = new Texture2D(2, 2);
                if (!texture.LoadImage(fileData))
                {
                    Debug.LogError($"Failed to decode image data at {imagePath}");
                    return;
                }
                texture.name = Path.GetFileNameWithoutExtension(imagePath);
            }

            if (texture == null)
            {
                Debug.LogError("Could not load texture from path: " + imagePath);
                return;
            }

            if (wnd.textureField == null)
            {
                Debug.LogError("Texture field not found in the Background Remover window.");
                return;
            }
            texture.filterMode = FilterMode.Point; 


            wnd.textureField.value = texture;
            wnd.sourceTexture = texture;
            wnd.OnTextureLoaded();
        }

        public void CreateGUI()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("BackgroundRemover");
            visualTree.CloneTree(rootVisualElement);

            var styleSheet = Resources.Load<StyleSheet>("BackgroundRemoverStyles");
            rootVisualElement.styleSheets.Add(styleSheet);
            rootVisualElement.focusable = true;

            // Get references
            textureField = rootVisualElement.Q<ObjectField>("SourceTexture");
            imageElement = rootVisualElement.Q<VisualElement>("ImageElement");
            statusLabel = rootVisualElement.Q<Label>("StatusLabel");
            algorithmField = rootVisualElement.Q<EnumField>("AlgorithmField");
            colorKeyField = rootVisualElement.Q<ColorField>("ColorKeyField");
            toleranceSlider = rootVisualElement.Q<Slider>("ToleranceSlider");
            edgeSoftnessSlider = rootVisualElement.Q<Slider>("EdgeSoftnessSlider");
            invertSelectionToggle = rootVisualElement.Q<Toggle>("InvertSelectionToggle");
            preserveAlphaToggle = rootVisualElement.Q<Toggle>("PreserveAlphaToggle");
            adjacentToggle = rootVisualElement.Q<Toggle>("AdjacentToggle");
            undoButton = rootVisualElement.Q<Button>("UndoButton");
            redoButton = rootVisualElement.Q<Button>("RedoButton");
            clearSelectionButton = rootVisualElement.Q<Button>("ClearSelectionButton");
            deleteAreaButton = rootVisualElement.Q<Button>("DeleteAreaButton");

            // Eraser tool elements
            eraserButton = rootVisualElement.Q<Button>("EraserButton");
            eraserSizeSlider = rootVisualElement.Q<Slider>("EraserSizeSlider");

            // Zoom elements
            zoomSlider = rootVisualElement.Q<Slider>("ZoomSlider");
            resetZoomButton = rootVisualElement.Q<Button>("ResetZoomButton");

            var saveButton = rootVisualElement.Q<Button>("SaveButton");
            var resetButton = rootVisualElement.Q<Button>("ResetButton");

            // Setup callbacks
            textureField.RegisterValueChangedCallback(evt =>
            {
                sourceTexture = evt.newValue as Texture2D;
                OnTextureLoaded();
            });

            algorithmField.RegisterValueChangedCallback(evt =>
            {
                currentAlgorithm = (RemovalAlgorithm)evt.newValue;
                UpdateAlgorithmOptions();
                UpdateSelectionPreview();
            });

            colorKeyField.RegisterValueChangedCallback(evt =>
            {
                keyColor = evt.newValue;
                UpdateSelectionPreview();
            });

            toleranceSlider.RegisterValueChangedCallback(evt =>
            {
                tolerance = evt.newValue;
                UpdateSelectionPreview();
            });

            edgeSoftnessSlider.RegisterValueChangedCallback(evt =>
            {
                edgeSoftness = evt.newValue;
                UpdateSelectionPreview();
            });

            invertSelectionToggle.RegisterValueChangedCallback(evt =>
            {
                invertSelection = evt.newValue;
                UpdateSelectionPreview();
            });

            preserveAlphaToggle.RegisterValueChangedCallback(evt => preserveAlpha = evt.newValue);
            adjacentToggle.RegisterValueChangedCallback(evt => adjacentOnly = evt.newValue);

            // Eraser tool
            eraserButton.clicked += ToggleEraserTool;
            eraserSizeSlider.RegisterValueChangedCallback(evt => eraserSize = evt.newValue);

            // Zoom controls
            zoomSlider.RegisterValueChangedCallback(evt =>
            {
                zoomLevel = evt.newValue;
                UpdateImageTransform();
            });
            resetZoomButton.clicked += () =>
            {
                zoomLevel = 1f;
                panOffset = Vector2.zero;
                zoomSlider.value = 1f;
                UpdateImageTransform();
            };

            undoButton.clicked += Undo;
            redoButton.clicked += Redo;
            clearSelectionButton.clicked += ClearSelection;
            deleteAreaButton.clicked += DeleteSelectedArea;
            saveButton.clicked += SaveResult;
            resetButton.clicked += ResetAll;

            // Setup image interaction
            imageElement.RegisterCallback<MouseDownEvent>(OnImageMouseDown);
            imageElement.RegisterCallback<MouseMoveEvent>(OnImageMouseMove);
            imageElement.RegisterCallback<MouseUpEvent>(OnImageMouseUp);
            imageElement.RegisterCallback<MouseLeaveEvent>(OnImageMouseLeave);
            imageElement.RegisterCallback<WheelEvent>(OnImageWheel);

            // Register keyboard shortcuts
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            // Setup drag and drop
            SetupDragAndDrop();

            // Initialize
            algorithmField.Init(currentAlgorithm);
            UpdateAlgorithmOptions();
            UpdateUndoRedoButtons();
            UpdateEraserButton();

            // Start update loop
            EditorApplication.update += OnEditorUpdate; 
        }

        private void SetupDragAndDrop()
        {
            imageElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    var obj = DragAndDrop.objectReferences[0];
                    if (obj is Texture2D)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        evt.StopPropagation();
                    }
                }
            });

            imageElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    var obj = DragAndDrop.objectReferences[0];
                    if (obj is Texture2D texture)
                    {
                        textureField.value = texture;
                        sourceTexture = texture;
                        OnTextureLoaded();
                        evt.StopPropagation();
                    }
                }
            });

            // Also add drag and drop to the entire window
            rootVisualElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.paths.Length > 0)
                {
                    string path = DragAndDrop.paths[0];
                    string ext = Path.GetExtension(path).ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".bmp" || ext == ".psd")
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        evt.StopPropagation();
                    }
                }
            });

            rootVisualElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (DragAndDrop.paths.Length > 0)
                {
                    string path = DragAndDrop.paths[0];
                    string ext = Path.GetExtension(path).ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".bmp" || ext == ".psd")
                    {
                        OpenWithImage(path);
                        DragAndDrop.AcceptDrag();
                        evt.StopPropagation();
                    }
                }
            });
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnEditorUpdate()
        {
            if (needsOverlayUpdate)
            {
                needsOverlayUpdate = false;
                Repaint(); // Force immediate repaint
            }
        }
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            // Debug.Log($"Key pressed: {evt.keyCode}");
            if (evt.keyCode == KeyCode.Delete)
            {
                DeleteSelectedArea();
                evt.StopPropagation();
            }
        }

        private void OnTextureLoaded()
        {
            if (sourceTexture == null)
            {
                statusLabel.text = "No texture loaded";
                return;
            }

            // Make the source texture use point filtering for crisp pixels
            if (sourceTexture.filterMode != FilterMode.Point)
            {
                sourceTexture.filterMode = FilterMode.Point;
            }

            // Make texture readable
            string path = AssetDatabase.GetAssetPath(sourceTexture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(path);
            }
 
            resultTexture = null;
            currentSelection = new bool[sourceTexture.width, sourceTexture.height];
            selectionOverlay = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            selectionOverlay.filterMode = FilterMode.Point;

            // Reset zoom
            zoomLevel = 1f;
            panOffset = Vector2.zero;
            zoomSlider.value = 1f;

            UpdateImageDisplay();
            UpdateSelectionPreview();
            statusLabel.text = $"Loaded: {sourceTexture.name} ({sourceTexture.width}x{sourceTexture.height})";
        }

        private void UpdateSelectionPreview()
        {
            if (sourceTexture == null) return;

            EditorApplication.delayCall += () =>
            {
                if (sourceTexture == null) return;
                
                switch (currentAlgorithm)
                {
                    case RemovalAlgorithm.ColorKey:
                        PreviewColorKey();
                        break;
                    case RemovalAlgorithm.EdgeDetection:
                        PreviewEdgeDetection();
                        break;
                    case RemovalAlgorithm.Threshold:
                        PreviewThreshold();
                        break;
                    case RemovalAlgorithm.FloodFill:
                        PreviewFloodFill();
                        break;
                }
                
                UpdateSelectionOverlay();
                UpdateImageDisplay();
            };
        }

        private void PreviewColorKey()
        {
            Color[] pixels = sourceTexture.GetPixels();
            currentSelection = new bool[sourceTexture.width, sourceTexture.height];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % sourceTexture.width;
                int y = i / sourceTexture.width;
                
                float distance = ColorDistance(pixels[i], keyColor);
                bool selected = distance <= tolerance;
                
                if (invertSelection) selected = !selected;
                currentSelection[x, y] = selected;
            }
        }

        private void PreviewEdgeDetection()
        {
            Color[] pixels = sourceTexture.GetPixels();
            currentSelection = new bool[sourceTexture.width, sourceTexture.height];
            int width = sourceTexture.width;
            int height = sourceTexture.height;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float gx = 0, gy = 0;

                    // Sobel operator
                    gx += GetGrayScale(pixels[(y - 1) * width + (x - 1)]) * -1;
                    gx += GetGrayScale(pixels[y * width + (x - 1)]) * -2;
                    gx += GetGrayScale(pixels[(y + 1) * width + (x - 1)]) * -1;
                    gx += GetGrayScale(pixels[(y - 1) * width + (x + 1)]) * 1;
                    gx += GetGrayScale(pixels[y * width + (x + 1)]) * 2;
                    gx += GetGrayScale(pixels[(y + 1) * width + (x + 1)]) * 1;

                    gy += GetGrayScale(pixels[(y - 1) * width + (x - 1)]) * -1;
                    gy += GetGrayScale(pixels[(y - 1) * width + x]) * -2;
                    gy += GetGrayScale(pixels[(y - 1) * width + (x + 1)]) * -1;
                    gy += GetGrayScale(pixels[(y + 1) * width + (x - 1)]) * 1;
                    gy += GetGrayScale(pixels[(y + 1) * width + x]) * 2;
                    gy += GetGrayScale(pixels[(y + 1) * width + (x + 1)]) * 1;

                    float edge = Mathf.Sqrt(gx * gx + gy * gy);
                    bool selected = edge <= tolerance;
                    
                    if (invertSelection) selected = !selected;
                    currentSelection[x, y] = selected;
                }
            }
        }

        private void PreviewThreshold()
        {
            Color[] pixels = sourceTexture.GetPixels();
            currentSelection = new bool[sourceTexture.width, sourceTexture.height];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % sourceTexture.width;
                int y = i / sourceTexture.width;
                
                float gray = GetGrayScale(pixels[i]);
                bool selected = gray <= tolerance;
                
                if (invertSelection) selected = !selected;
                currentSelection[x, y] = selected;
            }
        }

        private void PreviewFloodFill()
        {
            Color[] pixels = sourceTexture.GetPixels();
            currentSelection = new bool[sourceTexture.width, sourceTexture.height];
            bool[] visited = new bool[pixels.Length];
            int width = sourceTexture.width;
            int height = sourceTexture.height;

            Queue<int> queue = new Queue<int>();

            // Start flood fill from corners
            queue.Enqueue(0);
            queue.Enqueue(width - 1);
            queue.Enqueue((height - 1) * width);
            queue.Enqueue(height * width - 1);

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                if (idx < 0 || idx >= pixels.Length || visited[idx]) continue;

                visited[idx] = true;
                int x = idx % width;
                int y = idx / width;

                currentSelection[x, y] = true;

                // Check neighbors
                if (x > 0 && ColorDistance(pixels[idx], pixels[idx - 1]) < tolerance)
                    queue.Enqueue(idx - 1);
                if (x < width - 1 && ColorDistance(pixels[idx], pixels[idx + 1]) < tolerance)
                    queue.Enqueue(idx + 1);
                if (y > 0 && ColorDistance(pixels[idx], pixels[idx - width]) < tolerance)
                    queue.Enqueue(idx - width);
                if (y < height - 1 && ColorDistance(pixels[idx], pixels[idx + width]) < tolerance)
                    queue.Enqueue(idx + width);
            }
            
            if (invertSelection)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        currentSelection[x, y] = !currentSelection[x, y];
                    }
                }
            }
        }

        private void ToggleEraserTool()
        {
            isEraserActive = !isEraserActive;
            UpdateEraserButton();
        }

        private void UpdateEraserButton()
        {
            if (eraserButton != null)
            {
                eraserButton.text = isEraserActive ? "Eraser (Active)" : "Eraser";
                eraserButton.style.backgroundColor = isEraserActive ? new Color(0.35f, 0.62f, 0.83f) : new Color(0.29f, 0.29f, 0.29f);
            }
        }

        private void OnImageMouseDown(MouseDownEvent evt)
        {
            if (sourceTexture == null) return;

            if (evt.button == 1) // Right mouse button for panning
            {
                isPanning = true;
                lastMousePosition = evt.localMousePosition;
                imageElement.CaptureMouse();
                return;
            }

            if (evt.button != 0) return; // Only process left mouse button
 
            Vector2Int pixelPos = GetPixelPosition(evt.localMousePosition);
            if (pixelPos.x < 0 || pixelPos.x >= sourceTexture.width || 
                pixelPos.y < 0 || pixelPos.y >= sourceTexture.height) return;

            isDrawing = true;
            imageElement.CaptureMouse();
            
            if (isEraserActive)
            {
                SaveUndoState();
                isAddingToSelection = !evt.shiftKey;
                isRemovingFromSelection = evt.shiftKey;
                ApplyEraser(pixelPos.x, pixelPos.y);
            }
            else if (currentAlgorithm == RemovalAlgorithm.MagicWand)
            {
                isAddingToSelection = evt.shiftKey;
                isRemovingFromSelection = evt.ctrlKey;
                
                SaveUndoState();
                PerformMagicWandSelection(pixelPos.x, pixelPos.y);
                UpdateSelectionOverlay();
                UpdateImageDisplay();
            }
        }

        private void OnImageMouseMove(MouseMoveEvent evt)
        {
            if (isPanning)
            {
                Vector2 delta = evt.localMousePosition - lastMousePosition;
                panOffset += delta;
                lastMousePosition = evt.localMousePosition;
                UpdateImageTransform();
                return;
            }

            if (!isDrawing || !isEraserActive || sourceTexture == null) return;

            Vector2Int pixelPos = GetPixelPosition(evt.localMousePosition);
             
            
            if (pixelPos.x >= 0 && pixelPos.x < sourceTexture.width && pixelPos.y >= 0 && pixelPos.y < sourceTexture.height)
            {
                ApplyEraser(pixelPos.x, pixelPos.y);
            }
        }

        private void OnImageMouseUp(MouseUpEvent evt)
        {
            isDrawing = false;
            isPanning = false;
            imageElement.ReleaseMouse();
        }

        private void OnImageMouseLeave(MouseLeaveEvent evt)
        {
            isDrawing = false;
            isPanning = false;
        }

        private void OnImageWheel(WheelEvent evt)
        {
            if (sourceTexture == null || imageElement == null) return;

            float delta = -evt.delta.y * 0.01f;
            float oldZoomLevel = zoomLevel;
            float newZoom = Mathf.Clamp(oldZoomLevel + delta, 1f, 10f);

            if (newZoom != oldZoomLevel)
            {
                // Get mouse position relative to image element
                Vector2 mousePos = evt.localMousePosition;
                Vector2 center = imageElement.layout.center;
                
                // Calculate the point under mouse in unzoomed space
                Vector2 mouseRelativeToCenter = mousePos - center - panOffset;
                Vector2 pointUnderMouse = mouseRelativeToCenter / oldZoomLevel;
                
                // Calculate new pan offset to keep the same point under the mouse
                panOffset = mousePos - center - (pointUnderMouse * newZoom);
                
                zoomLevel = newZoom;
                zoomSlider.value = zoomLevel;
                UpdateImageTransform();
            }

            evt.StopPropagation();
        }

        private void ApplyEraser(int centerX, int centerY)
        {
            int radius = Mathf.RoundToInt(eraserSize);
            bool hasChanged = false;
            
            // Calculate bounds to avoid checking unnecessary pixels
            int minX = Mathf.Max(0, centerX - radius);
            int maxX = Mathf.Min(sourceTexture.width - 1, centerX + radius);
            int minY = Mathf.Max(0, centerY - radius);
            int maxY = Mathf.Min(sourceTexture.height - 1, centerY + radius);
            
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (distance <= radius)
                    {
                        bool wasSelected = currentSelection[x, y];
                        
                        if (isAddingToSelection && !wasSelected)
                        {
                            currentSelection[x, y] = true;
                            hasChanged = true;
                        }
                        else if (isRemovingFromSelection && wasSelected)
                        {
                            currentSelection[x, y] = false;
                            hasChanged = true;
                        }
                    }
                }
            }
            
            if (hasChanged)
            {
                UpdateSelectionOverlayRegion(minX, minY, maxX - minX + 1, maxY - minY + 1);
                needsOverlayUpdate = true;
            }
        }

        private void UpdateSelectionOverlayRegion(int startX, int startY, int width, int height)
        {
            if (selectionOverlay == null) return;
            
            Color selectedColor = new Color(1f, 0f, 0f, 0.3f);
            Color clearColor = Color.clear;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int px = startX + x;
                    int py = startY + y;
                    
                    if (px >= 0 && px < selectionOverlay.width && py >= 0 && py < selectionOverlay.height)
                    {
                        Color targetColor = currentSelection[px, py] ? selectedColor : clearColor;
                        selectionOverlay.SetPixel(px, py, targetColor);
                    }
                }
            }
            
            selectionOverlay.Apply();
            UpdateImageDisplay();
        }

        private Vector2Int GetPixelPosition(Vector2 localMousePos)
        {
            if (sourceTexture == null || currentAlgorithm != RemovalAlgorithm.MagicWand) return new Vector2Int(-1, -1);

            Rect containerRect = imageElement.contentRect;
            if (containerRect.width <= 0 || containerRect.height <= 0) return new Vector2Int(-1, -1);

            // Get source texture dimensions
            float texWidth = sourceTexture.width;
            float texHeight = sourceTexture.height;
            if (texWidth <= 0 || texHeight <= 0) return new Vector2Int(-1, -1);

            float scaleRatioX = containerRect.width / texWidth;
            float scaleRatioY = containerRect.height / texHeight;
            float scale = Mathf.Min(scaleRatioX, scaleRatioY);

            float renderedImageWidth = texWidth * scale;
            float renderedImageHeight = texHeight * scale;

            float offsetX = (containerRect.width - renderedImageWidth) / 2f;
            float offsetY = (containerRect.height - renderedImageHeight) / 2f;
 

            float mouseXOnImage = localMousePos.x - offsetX;
            float mouseYOnImage = localMousePos.y - offsetY;

            if (mouseXOnImage < 0 || mouseXOnImage >= renderedImageWidth ||
                mouseYOnImage < 0 || mouseYOnImage >= renderedImageHeight)
            {
                statusLabel.text = "Clicked outside image area.";
                return new Vector2Int(-1, -1);
            }

            float u = mouseXOnImage / renderedImageWidth;
            float v_ui = mouseYOnImage / renderedImageHeight;

            int pixelX = Mathf.FloorToInt(u * texWidth);
            int pixelY = Mathf.FloorToInt((1f - v_ui) * texHeight);

            pixelX = Mathf.Clamp(pixelX, 0, (int)texWidth - 1);
            pixelY = Mathf.Clamp(pixelY, 0, (int)texHeight - 1);

            return new Vector2Int(pixelX, pixelY);
        }


        private void UpdateImageTransform()
        {
            if (imageElement == null || sourceTexture == null) return;

            imageElement.transform.scale = Vector3.one * zoomLevel;
            imageElement.transform.position = new Vector3(panOffset.x, panOffset.y, 0);

            VisualElement overlay = imageElement.Q("SelectionOverlayElement");
            if (overlay != null)
            {
                overlay.transform.scale = Vector3.one;
                overlay.transform.position = Vector3.zero;
            }
        }

        private void UpdateAlgorithmOptions()
        {
            var algorithmOptions = rootVisualElement.Q<VisualElement>("AlgorithmOptions");
            var magicWandOptions = rootVisualElement.Q<VisualElement>("MagicWandOptions");
            var eraserOptions = rootVisualElement.Q<VisualElement>("EraserOptions");

            // Hide all options first
            colorKeyField.parent.style.display = DisplayStyle.None;
            magicWandOptions.style.display = DisplayStyle.None;
            eraserOptions.style.display = DisplayStyle.Flex; // Always show eraser

            // Show relevant options
            switch (currentAlgorithm)
            {
                case RemovalAlgorithm.ColorKey:
                    colorKeyField.parent.style.display = DisplayStyle.Flex;
                    AutoSelectBackgroundColor();
                    break;
                case RemovalAlgorithm.MagicWand:
                    magicWandOptions.style.display = DisplayStyle.Flex;
                    colorKeyField.parent.style.display = DisplayStyle.None;
                    break;
                default:
                    // For other algorithms, hide all options
                    colorKeyField.parent.style.display = DisplayStyle.None;
                    magicWandOptions.style.display = DisplayStyle.None;
                    break;
            }
        }

        private void AutoSelectBackgroundColor()
        {
            if (sourceTexture == null) return;

            Color[] pixels = sourceTexture.GetPixels();
            int width = sourceTexture.width;
            int height = sourceTexture.height;

            List<Color> samples = new List<Color>();

            // Corners
            samples.Add(pixels[0]);
            samples.Add(pixels[width - 1]);
            samples.Add(pixels[(height - 1) * width]);
            samples.Add(pixels[height * width - 1]);

            // Edge centers
            samples.Add(pixels[width / 2]);
            samples.Add(pixels[(height / 2) * width]);
            samples.Add(pixels[(height - 1) * width + width / 2]);
            samples.Add(pixels[(height / 2) * width + width - 1]);

            Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();
            foreach (var color in samples)
            {
                bool found = false;
                foreach (var kvp in colorCounts.ToList())
                {
                    if (ColorDistance(color, kvp.Key) < 0.1f)
                    {
                        colorCounts[kvp.Key]++;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    colorCounts[color] = 1;
                }
            }

            keyColor = colorCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            colorKeyField.value = keyColor;
        }

        private void PerformMagicWandSelection(int startX, int startY)
        {
            if (!isAddingToSelection && !isRemovingFromSelection)
            {
                currentSelection = new bool[sourceTexture.width, sourceTexture.height];
            }

            Color[] pixels = sourceTexture.GetPixels();
            int width = sourceTexture.width;
            int height = sourceTexture.height;

            Color targetColor = pixels[startY * width + startX];
            colorKeyField.value = targetColor;

            if (adjacentOnly)
            {
                // Flood fill algorithm
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                bool[,] visited = new bool[width, height];

                queue.Enqueue(new Vector2Int(startX, startY));

                while (queue.Count > 0)
                {
                    var pos = queue.Dequeue();
                    int x = pos.x;
                    int y = pos.y;

                    if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y]) continue;

                    visited[x, y] = true;

                    Color pixelColor = pixels[y * width + x];
                    if (ColorDistance(pixelColor, targetColor) <= tolerance)
                    {
                        if (isRemovingFromSelection)
                            currentSelection[x, y] = false;
                        else
                            currentSelection[x, y] = true;

                        // Add neighbors
                        queue.Enqueue(new Vector2Int(x + 1, y));
                        queue.Enqueue(new Vector2Int(x - 1, y));
                        queue.Enqueue(new Vector2Int(x, y + 1));
                        queue.Enqueue(new Vector2Int(x, y - 1));
                    }
                }
            }
            else
            {
                // Select all similar pixels
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color pixelColor = pixels[y * width + x];
                        if (ColorDistance(pixelColor, targetColor) <= tolerance)
                        {
                            if (isRemovingFromSelection)
                                currentSelection[x, y] = false;
                            else
                                currentSelection[x, y] = true;
                        }
                    }
                }
            }
        }

        private void UpdateSelectionOverlay()
        {
            if (selectionOverlay == null || currentSelection == null) return;

            Color[] overlayPixels = new Color[selectionOverlay.width * selectionOverlay.height];
            Color selectedColor = new Color(1f, 0f, 0f, 0.3f);
            Color clearColor = Color.clear;

            for (int y = 0; y < selectionOverlay.height; y++)
            {
                for (int x = 0; x < selectionOverlay.width; x++)
                {
                    int idx = y * selectionOverlay.width + x;
                    overlayPixels[idx] = currentSelection[x, y] ? selectedColor : clearColor;
                }
            }

            selectionOverlay.SetPixels(overlayPixels);
            selectionOverlay.Apply();
        } 
        private void UpdateImageDisplay()
        {
            if (resultTexture != null)
            {
                imageElement.style.backgroundImage = new StyleBackground(resultTexture);
            }
            else if (sourceTexture != null)
            {
                imageElement.style.backgroundImage = new StyleBackground(sourceTexture);
            }
            else
            {
                imageElement.style.backgroundImage = null;
                VisualElement existingOverlay = imageElement.Q("SelectionOverlayElement");
                if (existingOverlay != null) existingOverlay.style.display = DisplayStyle.None;
                return;
            }

            imageElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            imageElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            imageElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            imageElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

            VisualElement overlay = imageElement.Q("SelectionOverlayElement");
            if (overlay == null && selectionOverlay != null && sourceTexture != null)
            {
                overlay = new VisualElement { name = "SelectionOverlayElement" };
                overlay.pickingMode = PickingMode.Ignore; 
                overlay.style.position = Position.Absolute;
                overlay.style.top = 0;
                overlay.style.left = 0;
                overlay.style.right = 0;
                overlay.style.bottom = 0;
                overlay.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                overlay.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                overlay.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                overlay.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                imageElement.Add(overlay);
            }

            if (overlay != null && selectionOverlay != null)
            {
                overlay.style.backgroundImage = new StyleBackground(selectionOverlay);
                overlay.style.display = DisplayStyle.Flex;
            }
            else if (overlay != null)
            {
                overlay.style.display = DisplayStyle.None;
            }
        }

        private void SaveUndoState()
        {
            if (currentSelection != null)
            {
                bool[,] copy = (bool[,])currentSelection.Clone();
                undoStack.Push(copy);
                redoStack.Clear();
                UpdateUndoRedoButtons();
            }
        }

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push((bool[,])currentSelection.Clone());
                currentSelection = undoStack.Pop();
                UpdateSelectionOverlay();
                UpdateImageDisplay();
                UpdateUndoRedoButtons();
            }
        }

        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push((bool[,])currentSelection.Clone());
                currentSelection = redoStack.Pop();
                UpdateSelectionOverlay();
                UpdateImageDisplay();
                UpdateUndoRedoButtons();
            }
        }

        private void ClearSelection()
        {
            SaveUndoState();
            currentSelection = new bool[sourceTexture.width, sourceTexture.height];
            UpdateSelectionOverlay();
            UpdateImageDisplay();
        }

        private void UpdateUndoRedoButtons()
        {
            undoButton.SetEnabled(undoStack.Count > 0);
            redoButton.SetEnabled(redoStack.Count > 0);
        }

        private void DeleteSelectedArea()
        {
            if (sourceTexture == null || currentSelection == null)
            {
                EditorUtility.DisplayDialog("Error", "No selection to delete.", "OK");
                return;
            }

            statusLabel.text = "Deleting selected area...";

            try
            {
                if (resultTexture == null)
                {
                    resultTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
                    resultTexture.filterMode = FilterMode.Point;  
                    resultTexture.SetPixels(sourceTexture.GetPixels());
                    resultTexture.Apply();
                }

                Color[] pixels = resultTexture.GetPixels();
                int width = resultTexture.width;

                for (int i = 0; i < pixels.Length; i++)
                {
                    int x = i % width;
                    int y = i / width;

                    if (currentSelection[x, y])
                    {
                        pixels[i].a = 0f;
                    }
                }

                resultTexture.SetPixels(pixels);
                resultTexture.Apply();

                // Clear selection after deletion
                currentSelection = new bool[sourceTexture.width, sourceTexture.height];
                UpdateSelectionOverlay();
                UpdateImageDisplay();
                
                statusLabel.text = "Selected area deleted!";
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting area: {e.Message}");
                statusLabel.text = "Error deleting area";
            }
        }

        private float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        private float GetGrayScale(Color color)
        {
            return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
        }

        private void SaveResult()
        {
            if (resultTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "No processed image to save.", "OK");
                return;
            }

            string defaultName = sourceTexture != null ? sourceTexture.name + "_no_bg" : "processed_image_no_bg";

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Background Removed Image",
                defaultName,
                "png",
                "Save the processed image"
            );

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    byte[] pngData = resultTexture.EncodeToPNG();
                    File.WriteAllBytes(path, pngData);

                    AssetDatabase.Refresh();

                    TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (textureImporter != null)
                    {
                        textureImporter.textureType = TextureImporterType.Sprite;
                        textureImporter.alphaIsTransparency = true;
                        textureImporter.spriteImportMode = SpriteImportMode.Single;
                        textureImporter.spritePixelsPerUnit = 100;
                        textureImporter.SaveAndReimport();

                        statusLabel.text = $"Saved to: {path} (as Sprite with alpha transparency)";
                    }
                    else
                    {
                        statusLabel.text = $"Saved to: {path} (Error: Could not apply Sprite settings)";
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving or processing image import settings: {e.Message}");
                    statusLabel.text = "Error saving image or applying settings.";
                    EditorUtility.DisplayDialog("Save Error", $"An error occurred: {e.Message}", "OK");
                }
            }
            else
            {
                statusLabel.text = "Save operation cancelled.";
            }
        }

        private void ResetAll()
        {
            sourceTexture = null;
            resultTexture = null; 
            currentSelection = null;
            selectionOverlay = null;
            undoStack.Clear();
            redoStack.Clear();
            isEraserActive = false;
            zoomLevel = 1f;
            panOffset = Vector2.zero;

            imageElement.style.backgroundImage = null;
            imageElement.Clear();
            statusLabel.text = "Ready";

            // Reset parameters
            currentAlgorithm = RemovalAlgorithm.ColorKey;
            keyColor = Color.green;
            tolerance = 0.1f;
            edgeSoftness = 0.0f;
            invertSelection = false;
            preserveAlpha = false;
            adjacentOnly = true;
            eraserSize = 10f;

            // Update UI
            algorithmField.value = currentAlgorithm;
            colorKeyField.value = keyColor;
            toleranceSlider.value = tolerance;
            edgeSoftnessSlider.value = edgeSoftness;
            invertSelectionToggle.value = invertSelection;
            preserveAlphaToggle.value = preserveAlpha;
            adjacentToggle.value = adjacentOnly;
            eraserSizeSlider.value = eraserSize;
            zoomSlider.value = zoomLevel;

            UpdateUndoRedoButtons();
            UpdateEraserButton();
            UpdateImageTransform();
        }
    }
}