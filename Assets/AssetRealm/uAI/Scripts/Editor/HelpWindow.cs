using UnityEditor;
using UnityEngine;

namespace UAI{ 
    public class HelpWindow : EditorWindow
    { 
        private static string _secretKey;
        private Vector2 scrollPosition;

        [MenuItem("Tools/uAI Creator/Help", false, 999)]
        public static void ShowWindow()
        {
            HelpWindow window = GetWindow<HelpWindow>("Help"); 
            window.minSize = new Vector2(500, 420);
        }
    
        private void OnGUI()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Space(10);
            GUILayout.Label("If you need help, you can read the documentation or join the Discord Server.", EditorStyles.boldLabel); 
            GUILayout.Space(10);
            
            GUILayout.Label("Help", EditorStyles.boldLabel); 

            if (GUILayout.Button("Open Documentation"))     Application.OpenURL("https://asset-realm.com/uAI-Manual-Documentation.pdf");   


            if (GUILayout.Button("Open Asset Store Page"))  Application.OpenURL("https://u3d.as/3xGe");
            
            if (GUILayout.Button("Open Discord"))           Application.OpenURL("https://discord.gg/znYMdMJR5w");
            
            
            GUILayout.Space(10);
            
            GUILayout.Label("Useful links", EditorStyles.boldLabel); 
            GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                    GUILayout.Label("Trellis", EditorStyles.boldLabel); 
                    if (GUILayout.Button("> GitHub Page"))              Application.OpenURL("https://github.com/microsoft/TRELLIS"); 
                    if (GUILayout.Button(">>  Easy install"))           Application.OpenURL("https://github.com/IgorAherne/trellis-stable-projectorz"); 
                    if (GUILayout.Button(">>  API Server Script"))      Application.OpenURL("https://asset-realm.com/trellis-api-server/"); 
                    if (GUILayout.Button(">>  Blender"))                Application.OpenURL("https://www.blender.org/download/"); 
                GUILayout.EndVertical();


                GUILayout.BeginVertical();
                    GUILayout.Label("Ollama", EditorStyles.boldLabel); 
                    if (GUILayout.Button("> Ollama Website"))           Application.OpenURL("https://ollama.com/");
                GUILayout.EndVertical();
                    
                GUILayout.BeginVertical();
                    GUILayout.Label("LMStudio", EditorStyles.boldLabel); 
                    if (GUILayout.Button("> LMStudio Website"))         Application.OpenURL("https://lmstudio.ai/");
                GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.Label("API Keys", EditorStyles.boldLabel); 
            GUILayout.BeginHorizontal();
                if (GUILayout.Button("OpenAI"))         Application.OpenURL("https://platform.openai.com/account/api-keys"); 
                if (GUILayout.Button("Anthropic"))      Application.OpenURL("https://console.anthropic.com/settings/keys"); 
                if (GUILayout.Button("Google"))         Application.OpenURL("https://aistudio.google.com/app/apikey"); 
                if (GUILayout.Button("DeepSeek"))       Application.OpenURL("https://platform.deepseek.com/api_keys"); 
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.Label("Pricing", EditorStyles.boldLabel); 
            GUILayout.BeginHorizontal();
                if (GUILayout.Button("OpenAI"))      Application.OpenURL("https://openai.com/pricing"); 
                if (GUILayout.Button("Anthropic"))   Application.OpenURL("https://www.anthropic.com/pricing");
                if (GUILayout.Button("Gemini"))      Application.OpenURL("https://ai.google.dev/pricing"); 
                if (GUILayout.Button("DeepSeek "))    Application.OpenURL("https://api-docs.deepseek.com/quick_start/pricing/"); 
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            
            GUILayout.Label("Usage", EditorStyles.boldLabel); 
            GUILayout.BeginHorizontal();
                if (GUILayout.Button("OpenAI")) Application.OpenURL("https://platform.openai.com/account/usage"); 
                if (GUILayout.Button("Anthropic")) Application.OpenURL("https://console.anthropic.com/settings/usage");
                if (GUILayout.Button("Gemini")) Application.OpenURL("https://aistudio.google.com/app/apikey");
                if (GUILayout.Button("DeepSeek")) Application.OpenURL("https://platform.deepseek.com/usage");
            GUILayout.EndHorizontal();
            
 
            GUILayout.EndScrollView();
        }
          
    }
}