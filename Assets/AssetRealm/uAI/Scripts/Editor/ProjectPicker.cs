using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UAI
{
    public class ProjectPicker
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

        
        private List<ContextFile> _selectedFiles = new List<ContextFile>();
        private string _searchQuery = "";
        private Vector2 _scrollPosition;
        private UnityEngine.Object _objectPickerSelection;
        private bool _searchMode = false;
        private List<UnityEngine.Object> _searchResults = new List<UnityEngine.Object>();
        private int _maxSearchResults = 20;
        private GUIStyle _dragDropArea;
        
        
        public List<ContextFile> SelectedFiles => _selectedFiles;
        
        
        
        
        
        public bool OnGUI()
        {
            bool changed = false;
            InitializeStyles();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Add Scripts for Context", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            
            EditorGUILayout.BeginHorizontal();
            string newSearch = EditorGUILayout.TextField("Search Scripts:", _searchQuery);
            if (newSearch != _searchQuery)
            {
                _searchQuery = newSearch;
                PerformSearch(_searchQuery);
                _searchMode = !string.IsNullOrEmpty(_searchQuery);
                changed = true;
            }
            
            
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                ShowObjectPicker();
            }
            EditorGUILayout.EndHorizontal();
            
            
            if (_searchMode && _searchResults.Count > 0)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(150));
                
                foreach (var result in _searchResults)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    
                    EditorGUILayout.ObjectField(result, typeof(UnityEngine.Object), false, GUILayout.ExpandWidth(true));
                    
                    
                    if (GUILayout.Button("Add", GUILayout.Width(50)))
                    {
                        AddFileToContext(result);
                        changed = true;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            else if (_searchMode)
            {
                EditorGUILayout.HelpBox("No matching scripts found.", MessageType.Info);
            }
            
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Or drag and drop scripts here:", EditorStyles.boldLabel);
            
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag Script Assets Here", _dragDropArea);
            
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;
                    
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        
                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            string path = AssetDatabase.GetAssetPath(draggedObject);
                            
                            
                            if (path.EndsWith(".cs") || path.EndsWith(".json") || path.EndsWith(".txt"))
                            {
                                AddFileToContext(draggedObject);
                                changed = true;
                            }
                        }
                    }
                    
                    evt.Use();
                    break;
            }
            
            
            if (_selectedFiles.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Selected Files:", EditorStyles.boldLabel);
                
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(150));
                
                for (int i = 0; i < _selectedFiles.Count; i++)
                {
                    ContextFile file = _selectedFiles[i];
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(file.asset, typeof(Object), false, GUILayout.ExpandWidth(true));
                    EditorGUI.EndDisabledGroup();
                    
                    
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    GUI.Label(lastRect, new GUIContent("", file.path));
                    
                    
                    if (GUILayout.Button("✕", GUILayout.Width(25)))
                    {
                        _selectedFiles.RemoveAt(i);
                        i--;
                        changed = true;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Clear All", GUILayout.Width(80)))
                {
                    _selectedFiles.Clear();
                    changed = true;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            
            
            if (Event.current.commandName == "ObjectSelectorClosed" && 
                EditorGUIUtility.GetObjectPickerControlID() == 9000)
            {
                _objectPickerSelection = EditorGUIUtility.GetObjectPickerObject();
                
                if (_objectPickerSelection != null)
                {
                    AddFileToContext(_objectPickerSelection);
                    changed = true;
                }
                
                Event.current.Use();
            }
            
            return changed;
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
        
        private void AddFileToContext(UnityEngine.Object obj)
        {
            if (obj == null)
                return;
                
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
            }
        }
        
        private void PerformSearch(string query)
        {
            _searchResults.Clear();
            
            if (string.IsNullOrEmpty(query))
                return;
                
            
            string[] guids = AssetDatabase.FindAssets($"t:Script {query}");
            
            int count = 0;
            foreach (string guid in guids)
            {
                if (count >= _maxSearchResults)
                    break;
                    
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".cs"))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (obj != null)
                    {
                        _searchResults.Add(obj);
                        count++;
                    }
                }
            }
        }
        
        private void ShowObjectPicker()
        {
            int controlID = 9000; 
            EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "t:Script", controlID);
        }
        
        private void InitializeStyles()
        {
            if (_dragDropArea == null)
            {
                _dragDropArea = new GUIStyle(EditorStyles.helpBox);
                _dragDropArea.alignment = TextAnchor.MiddleCenter;
                _dragDropArea.fontStyle = FontStyle.Italic;
                _dragDropArea.fontSize = 12;
            }
        }
    }
}