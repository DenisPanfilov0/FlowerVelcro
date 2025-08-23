using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UAI
{
    
    
    
    public class UIProjectPicker
    {
        
        [Serializable]
        public class ContextFile
        {
            public UnityEngine.Object asset;
            public string path;
            public string content;
            
            public ContextFile(UnityEngine.Object asset)
            {
                this.asset = asset;
                this.path = AssetDatabase.GetAssetPath(asset);
                
                try
                {
                    if (File.Exists(path))
                    {
                        this.content = File.ReadAllText(path);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read file content: {ex.Message}");
                }
            }
        }
        
        
        private VisualElement _root;
        private TextField _searchField;
        private ListView _searchResultsList;
        private ListView _selectedFilesList;
        private VisualElement _dropArea;
        private Label _noResultsLabel;
        private Button _clearAllButton;
        
        
        private List<ContextFile> _selectedFiles = new List<ContextFile>();
        private List<Object> _searchResults = new List<Object>();
        private string _searchQuery = "";
        private int _maxSearchResults = 20;
        
        
        public List<ContextFile> SelectedFiles => _selectedFiles;
        
        
        public event Action OnSelectionChanged;
        
        
        
        
        
        public void Initialize(VisualElement rootElement)
        {
            _root = rootElement;
            
            
            CreateSearchUI();
            CreateDropAreaUI();
            CreateSelectedFilesUI();
            
            
            UpdateClearButtonState();
        }
        
        
        
        
        public void Refresh()
        {
            if (_selectedFilesList != null)
            {
                _selectedFilesList.itemsSource = _selectedFiles;
                _selectedFilesList.Rebuild();
            }
            
            UpdateClearButtonState();
        }
        
        
        
        
        private void CreateSearchUI()
        {
            var searchContainer = new VisualElement();
            searchContainer.style.marginBottom = 10;
            _root.Add(searchContainer);
            
            
            var searchHeader = new Label("Search Scripts");
            searchHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            searchHeader.style.marginBottom = 5;
            searchContainer.Add(searchHeader);
            
            
            var searchRow = new VisualElement();
            searchRow.style.flexDirection = FlexDirection.Row;
            searchContainer.Add(searchRow);
            
            _searchField = new TextField();
            _searchField.style.flexGrow = 1;
            _searchField.RegisterValueChangedCallback(evt => {
                _searchQuery = evt.newValue;
                PerformSearch(_searchQuery);
            });
            searchRow.Add(_searchField);
            
            var browseButton = new Button(ShowObjectPicker) { text = "Browse" };
            browseButton.style.width = 70;
            searchRow.Add(browseButton);
            
            
            _searchResultsList = new ListView();
            _searchResultsList.style.height = 150;
            _searchResultsList.style.display = DisplayStyle.None;
            _searchResultsList.makeItem = () => new ObjectRow();
            _searchResultsList.bindItem = (element, i) => {
                var row = (ObjectRow)element;
                row.SetObject(_searchResults[i], AddFileToContext);
            };
            searchContainer.Add(_searchResultsList);
            
            
            _noResultsLabel = new Label("No matching scripts found.");
            _noResultsLabel.style.paddingTop = 10;
            _noResultsLabel.style.paddingBottom = 10;
            _noResultsLabel.style.display = DisplayStyle.None;
            searchContainer.Add(_noResultsLabel);
        }
        
        
        
        
        private void CreateDropAreaUI()
        {
            var dropContainer = new VisualElement();
            dropContainer.style.marginBottom = 10;
            _root.Add(dropContainer);
            
            var dropHeader = new Label("Or drag and drop scripts here:");
            dropHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            dropHeader.style.marginBottom = 5;
            dropContainer.Add(dropHeader);
            
            _dropArea = new VisualElement();
            _dropArea.style.height = 50;

            _dropArea.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            _dropArea.style.borderTopWidth = 1;
            _dropArea.style.borderBottomWidth = 1;
            _dropArea.style.borderLeftWidth = 1;
            _dropArea.style.borderRightWidth = 1;
            Color BorderColor =new Color(0.4f, 0.4f, 0.4f);
            _dropArea.style.borderBottomColor = BorderColor;
            _dropArea.style.borderTopColor = BorderColor;
            _dropArea.style.borderLeftColor = BorderColor;
            _dropArea.style.borderRightColor = BorderColor;
            _dropArea.style.alignItems = Align.Center;
            _dropArea.style.justifyContent = Justify.Center;
            
            var dropLabel = new Label("Drag Script Assets Here");
            dropLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            _dropArea.Add(dropLabel);
            
            dropContainer.Add(_dropArea);
            
            
            _dropArea.RegisterCallback<DragEnterEvent>(OnDragEnter);
            _dropArea.RegisterCallback<DragLeaveEvent>(OnDragLeave);
            _dropArea.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _dropArea.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }
        
        
        
        
        private void CreateSelectedFilesUI()
        {
            var selectedContainer = new VisualElement();
            _root.Add(selectedContainer);
            
            var selectedHeader = new Label("Selected Files:");
            selectedHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            selectedHeader.style.marginBottom = 5;
            selectedContainer.Add(selectedHeader);
            
            _selectedFilesList = new ListView();
            _selectedFilesList.style.height = 150;
            _selectedFilesList.itemsSource = _selectedFiles;
            _selectedFilesList.makeItem = () => new ContextFileRow();
            _selectedFilesList.bindItem = (element, i) => {
                var row = (ContextFileRow)element;
                row.SetContextFile(_selectedFiles[i], RemoveFileFromContext);
            };
            selectedContainer.Add(_selectedFilesList);
            
            
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.FlexEnd;
            buttonRow.style.marginTop = 5;
            selectedContainer.Add(buttonRow);
            
            _clearAllButton = new Button(ClearAllFiles) { text = "Clear All" };
            _clearAllButton.style.width = 80;
            buttonRow.Add(_clearAllButton);
        }
        
        
        
        
        private void AddFileToContext(Object obj)
        {
            if (obj == null) return;
            
            string path = AssetDatabase.GetAssetPath(obj);
            
            
            if (!path.EndsWith(".cs") && !path.EndsWith(".json") && !path.EndsWith(".txt"))
            {
                EditorUtility.DisplayDialog("Invalid File Type", 
                    "Only C# scripts, JSON and text files can be added as context.", "OK");
                return;
            }
            
            
            bool alreadyExists = false;
            foreach (var file in _selectedFiles)
            {
                if (file.path == path)
                {
                    alreadyExists = true;
                    break;
                }
            }
            
            if (!alreadyExists)
            {
                _selectedFiles.Add(new ContextFile(obj));
                _selectedFilesList.Rebuild();
                UpdateClearButtonState();
                OnSelectionChanged?.Invoke();
            }
        }
        
        
        
        
        private void RemoveFileFromContext(ContextFile file)
        {
            _selectedFiles.Remove(file);
            _selectedFilesList.Rebuild();
            UpdateClearButtonState();
            OnSelectionChanged?.Invoke();
        }
        
        
        
        
        private void ClearAllFiles()
        {
            _selectedFiles.Clear();
            _selectedFilesList.Rebuild();
            UpdateClearButtonState();
            OnSelectionChanged?.Invoke();
        }
        
        
        
        
        private void UpdateClearButtonState()
        {
            if (_clearAllButton != null)
            {
                _clearAllButton.SetEnabled(_selectedFiles.Count > 0);
            }
        }
        
        
        
        
        private void PerformSearch(string query)
        {
            _searchResults.Clear();
            
            if (string.IsNullOrEmpty(query))
            {
                _searchResultsList.style.display = DisplayStyle.None;
                _noResultsLabel.style.display = DisplayStyle.None;
                return;
            }
            
            
            string[] guids = AssetDatabase.FindAssets($"t:Script {query}");
            
            int count = 0;
            foreach (string guid in guids)
            {
                if (count >= _maxSearchResults)
                    break;
                    
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".cs"))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (obj != null)
                    {
                        _searchResults.Add(obj);
                        count++;
                    }
                }
            }
            
            
            if (_searchResults.Count > 0)
            {
                _searchResultsList.itemsSource = _searchResults;
                _searchResultsList.style.display = DisplayStyle.Flex;
                _noResultsLabel.style.display = DisplayStyle.None;
            }
            else
            {
                _searchResultsList.style.display = DisplayStyle.None;
                _noResultsLabel.style.display = DisplayStyle.Flex;
            }
        }
        
        
        
        
        private void ShowObjectPicker()
        {
            int controlID = 9001; 
            EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "t:Script", controlID);
            
            
            EditorApplication.update += CheckObjectPickerResult;
        }
        
        
        
        
        private void CheckObjectPickerResult()
        {
            if (Event.current != null && Event.current.commandName == "ObjectSelectorClosed" && 
                EditorGUIUtility.GetObjectPickerControlID() == 9001)
            {
                Object obj = EditorGUIUtility.GetObjectPickerObject();
                if (obj != null)
                {
                    AddFileToContext(obj);
                }
                
                
                EditorApplication.update -= CheckObjectPickerResult;
                Event.current.Use();
            }
        }
        
        
        
        
        public string GetSelectedFilesContext()
        {
            if (_selectedFiles.Count == 0)
                return string.Empty;
            
            System.Text.StringBuilder context = new System.Text.StringBuilder();
            context.AppendLine("\nHere are the relevant files from the project to use as context:");
            
            foreach (var file in _selectedFiles)
            {
                if (file.asset == null || string.IsNullOrEmpty(file.content))
                    continue;
                
                context.AppendLine($"\nFile: {file.asset.name} ({file.path})");
                
                if (file.path.EndsWith(".cs"))
                {
                    context.AppendLine("```csharp");
                    context.AppendLine(file.content);
                    context.AppendLine("```");
                }
                else
                {
                    context.AppendLine("```");
                    context.AppendLine(file.content);
                    context.AppendLine("```");
                }
            }
            
            context.AppendLine("\nPlease make sure the code you generate will work with these existing files.");
            
            return context.ToString();
        }
        
        #region Drag and Drop Handlers
        private void OnDragEnter(DragEnterEvent evt)
        {
            _dropArea.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        }
        
        private void OnDragLeave(DragLeaveEvent evt)
        {
            _dropArea.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        }
        
        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.StopPropagation();
        }
        
        private void OnDragPerform(DragPerformEvent evt)
        {
            DragAndDrop.AcceptDrag();
            
            foreach (var draggedObject in DragAndDrop.objectReferences)
            {
                string path = AssetDatabase.GetAssetPath(draggedObject);
                
                
                if (path.EndsWith(".cs") || path.EndsWith(".json") || path.EndsWith(".txt"))
                {
                    AddFileToContext(draggedObject);
                }
            }
            
            _dropArea.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            evt.StopPropagation();
        }
        #endregion
        
        #region Helper Classes
        
        
        
        private class ObjectRow : VisualElement
        {
            private ObjectField _objectField;
            private Button _addButton;
            private Object _obj;
            private Action<Object> _addCallback;
            
            public ObjectRow()
            {
                style.flexDirection = FlexDirection.Row;
                style.marginBottom = 2;
                
                _objectField = new ObjectField();
                _objectField.style.flexGrow = 1;
                _objectField.SetEnabled(false);
                Add(_objectField);
                
                _addButton = new Button() { text = "Add" };
                _addButton.style.width = 50;
                _addButton.clicked += () => _addCallback?.Invoke(_obj);
                Add(_addButton);
            }
            
            public void SetObject(Object obj, Action<Object> addCallback)
            {
                _obj = obj;
                _addCallback = addCallback;
                _objectField.value = obj;
            }
        }
        
        
        
        
        private class ContextFileRow : VisualElement
        {
            private ObjectField _objectField;
            private Button _removeButton;
            private ContextFile _file;
            private Action<ContextFile> _removeCallback;
            
            public ContextFileRow()
            {
                style.flexDirection = FlexDirection.Row;
                style.marginBottom = 2;
                
                _objectField = new ObjectField();
                _objectField.style.flexGrow = 1;
                _objectField.SetEnabled(false);
                Add(_objectField);
                
                _removeButton = new Button() { text = "✕" };
                _removeButton.style.width = 25;
                _removeButton.clicked += () => _removeCallback?.Invoke(_file);
                Add(_removeButton);
            }
            
            public void SetContextFile(ContextFile file, Action<ContextFile> removeCallback)
            {
                _file = file;
                _removeCallback = removeCallback;
                _objectField.value = file.asset;
            }
        }
        #endregion
    }
}