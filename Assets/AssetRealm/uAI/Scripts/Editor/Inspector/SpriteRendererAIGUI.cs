// Assets/Editor/SpriteRendererWithAIButton.cs
using System;
using UnityEngine;
using UnityEditor;
using uAI.PixelArtScaler;

namespace UAI
{
    [CustomEditor(typeof(SpriteRenderer))]
    [CanEditMultipleObjects]
    public class SpriteRendererWithAIButton : Editor
    {
        // Holds Unity's built–in SpriteRendererEditor
        private Editor _defaultEditor;

        void OnEnable()
        {
            // Get the internal SpriteRendererEditor type
            var spriteEditorType = Type.GetType("UnityEditor.SpriteRendererEditor, UnityEditor");
            // Create it once, caching it so that Undo/redo etc. continue to work
            CreateCachedEditor(targets, spriteEditorType, ref _defaultEditor);
        }

        void OnDisable()
        {
            if (_defaultEditor != null)
                DestroyImmediate(_defaultEditor);
        }

        public override void OnInspectorGUI()
        {
            
                GUILayout.BeginHorizontal();
                
                    // 1) Your button at the very top
                    if (GUILayout.Button("AI ▶ Generate Sprite"))
                    {
                        foreach (var t in targets)
                        {
                            var sr = (SpriteRenderer)t; 
                            buttonClicked(sr);
                        }
                    } 

                    if (GUILayout.Button("▼", GUILayout.Width(50)))
                    {
                        GenericMenu menu = new GenericMenu();
                        string texturePath = getPathFromSprite((SpriteRenderer)target);
                        menu.AddItem(new GUIContent("Color Correction"), false, () =>
                        {
                            ImageCorrectionWindow.OpenWithImage(texturePath);
                        });

                        menu.AddItem(new GUIContent("Pixelart Scaler"), false, () =>
                        {
                            PixelartScalerWindow.OpenWithImage(texturePath);
                        });

                        menu.AddItem(new GUIContent("Part Detector"), false, () =>
                        {
                            ImagePartDetector.OpenWithImage(texturePath);
                        });

                        menu.AddItem(new GUIContent("Remove Background"), false, () =>
                        {
                            BackgroundRemoverEditorWindow.OpenWithImage(texturePath);
                        });
                        menu.AddItem(new GUIContent("3D Trellis"), false, () =>
                        {
                            TrellisAPIEditor.OpenWithImage(texturePath);
                        });

                        menu.ShowAsContext();
                    }  
                GUILayout.EndHorizontal();

            // 2) Delegate everything else to Unity’s original SpriteRendererEditor
            _defaultEditor.OnInspectorGUI();
        }

        private string getPathFromSprite(SpriteRenderer objField)
        {
            //check if sprite is assigned
            if (objField != null && objField.sprite != null)
            {
                //get path of the sprite
                return AssetDatabase.GetAssetPath(objField.sprite);
            }
            return string.Empty;
        }

        private void buttonClicked(SpriteRenderer objField)
        {

            AssetCreatorWindow.ShowWindow();
            //check if sprite is assigned
            if (objField != null)
            {
                Sprite sprite = objField.sprite;

                //get path of the sprite
                string path = AssetDatabase.GetAssetPath(sprite);
                AssetCreatorWindow window = AssetCreatorWindow.GetWindow<AssetCreatorWindow>();
                window.AddReferenceImage(path);
                window.ApplyPrompt("Create a variation of this image I send you.");
                window.SelectBestFittingSizeByAspectRatio(sprite.rect.width, sprite.rect.height);

                window.Focus();
            }
        }
    }
}