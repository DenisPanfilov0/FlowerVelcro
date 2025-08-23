using uAI.PixelArtScaler;
using UAI;
using UnityEditor;
using UnityEngine;

namespace uAI {
    [InitializeOnLoad]
    public static class Texture2DInspectorExtension
    {
        private static bool isSubscribed = false; 
 


        static Texture2DInspectorExtension()
        {
            if (!isSubscribed)
            {
                Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
                isSubscribed = true;
            }
        }


        private static void OnPostHeaderGUI(Editor editor)
        { 
            string texturePath = "";
            if (editor.target.GetType() == typeof(TextureImporter))
            {
                texturePath = AssetDatabase.GetAssetPath(editor.target);
            }else if (editor.target.GetType() == typeof(Material))
            {  
                Material material = (Material)editor.target;
                Texture mainTexture = material.mainTexture;

                if (mainTexture != null)
                { 
                    texturePath = AssetDatabase.GetAssetPath(mainTexture);
                }
            }

            if (texturePath != "")
            { 
                GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("uAI Tools ▼", GUILayout.Width(120)))
                    {
                        GenericMenu menu = new GenericMenu();

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
                        menu.AddItem(new GUIContent("Open in AssetCreator"), false, () =>
                        { 
                            //get path of the sprite 
                            AssetCreatorWindow window = AssetCreatorWindow.GetWindow<AssetCreatorWindow>();
                            window.AddReferenceImage(texturePath);
                            window.ApplyPrompt("Create a variation of this image I send you.");
                            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                            if (texture != null)
                            {
                                window.SelectBestFittingSizeByAspectRatio(texture.width, texture.height);
                            } 
                            window.Focus();
                        });

                        menu.ShowAsContext();
                    }  
                GUILayout.EndHorizontal();
            } 
        }
    }
}
