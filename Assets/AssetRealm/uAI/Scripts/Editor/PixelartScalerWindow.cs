using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System;

namespace uAI.PixelArtScaler
{
    public class PixelartScalerWindow : EditorWindow
    {
        private PixelartScalerElement pixelartScalerElement;
        
        public static string windowTitle = "Pixelart Scaler";

        
        [MenuItem("Assets/uAI/Pixel Art Scaler", false, 1038)]
        private static void ColorCorrectImage()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject); 
            OpenWithImage(path);
        }

        // Validation method to control when the menu is shown
        [MenuItem("Assets/uAI/Pixel Art Scaler", true)]
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


        [MenuItem("Tools/uAI Creator/Pixelart Scaler", false, 50)]
        public static void ShowWindow()
        {
            GetWindow<PixelartScalerWindow>(windowTitle);
        }
            
        public static void OpenWithImage(string imagePath)
        {
            var wnd = GetWindow<PixelartScalerWindow>();
            wnd.titleContent = new GUIContent("Pixelart Scaler");
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

            wnd.pixelartScalerElement.SetSourceTexture(texture);
        }


        private void OnEnable()
        {
            var root = rootVisualElement;
 
            var scrollView = new ScrollView();
            root.Add(scrollView);
 
            pixelartScalerElement = new PixelartScalerElement();
            scrollView.Add(pixelartScalerElement);
        }
         
        public void SetSourceTexture(Texture2D texture)
        {
            if (pixelartScalerElement != null)
                pixelartScalerElement.SetSourceTexture(texture);
        }
    }
}