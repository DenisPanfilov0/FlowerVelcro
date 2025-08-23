using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UAI
{
    public static class SyntaxHighlighter
    {
        
        private static Dictionary<string, GUIStyle> _cachedStyles = new Dictionary<string, GUIStyle>();
        private static Dictionary<string, Color> _colorCache = new Dictionary<string, Color>();
        
        
        public static class Colors
        {
            public static Color Keyword => GetColor("Keyword", new Color(0.34f, 0.61f, 0.84f)); 
            public static Color Comment => GetColor("Comment", new Color(0.42f, 0.6f, 0.33f));  
            public static Color String => GetColor("String", new Color(0.81f, 0.57f, 0.47f));   
            public static Color Number => GetColor("Number", new Color(0.71f, 0.81f, 0.66f));   
            public static Color Class => GetColor("Class", new Color(0.31f, 0.79f, 0.69f));     
            public static Color Method => GetColor("Method", new Color(0.86f, 0.86f, 0.67f));   
            public static Color Property => GetColor("Property", new Color(0.61f, 0.86f, 1f));  
            public static Color Directive => GetColor("Directive", new Color(0.77f, 0.53f, 0.75f)); 
            public static Color Background => GetColor("Background", new Color(0.22f, 0.22f, 0.22f)); 
            public static Color Text => GetColor("Text", new Color(0.9f, 0.9f, 0.9f)); 
        }

        
        private static readonly string[] CSharpKeywords = {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while", "var"
        };

        private static readonly string[] UnityTypes = {
            "Vector2", "Vector3", "Vector4", "Quaternion", "Transform", "GameObject", "MonoBehaviour",
            "ScriptableObject", "Component", "Rigidbody", "Collider", "Material", "Texture", "Shader",
            "Color", "RaycastHit", "Debug", "Input", "Time", "Mathf", "Physics", "EditorWindow",
            "Editor", "SerializedObject", "SerializedProperty", "EditorGUILayout", "GUILayout", "GUI"
        };

        private static Color GetColor(string name, Color defaultColor)
        {
            if (!_colorCache.TryGetValue(name, out Color color))
            {
                
                string hexColor = EditorPrefs.GetString("UAI_SyntaxColor_" + name, "");
                if (!string.IsNullOrEmpty(hexColor))
                {
                    ColorUtility.TryParseHtmlString(hexColor, out color);
                }
                else
                {
                    color = defaultColor;
                }
                
                _colorCache[name] = color;
            }
            
            return color;
        }
        public static Vector2 DrawCodeEditor(string code, Vector2 scrollPosition, float height = 0)
        {
            if (string.IsNullOrEmpty(code))
            {
                EditorGUILayout.HelpBox("No code to display", MessageType.Info);
                return scrollPosition;
            }
            
            
            GUIStyle codeStyle = GetCodeStyle(code);
            
            
            if (height <= 0)
            {
                height = Mathf.Min(500, codeStyle.CalcHeight(new GUIContent(code), EditorGUIUtility.currentViewWidth - 30));
            }
            
            
            Rect codeRect = EditorGUILayout.GetControlRect(false, height);
            scrollPosition = GUI.BeginScrollView(codeRect, scrollPosition, 
                new Rect(0, 0, codeRect.width - 20, codeStyle.CalcHeight(new GUIContent(code), codeRect.width - 20)));
            
            
            EditorGUI.DrawRect(new Rect(0, 0, codeRect.width, codeStyle.CalcHeight(new GUIContent(code), codeRect.width)), Colors.Background);
            
            
            GUI.Label(new Rect(5, 5, codeRect.width - 30, codeStyle.CalcHeight(new GUIContent(code), codeRect.width - 30)), 
                code, codeStyle);
            
            GUI.EndScrollView();
            
            return scrollPosition;
        }
        
        private static GUIStyle GetCodeStyle(string code)
        {
            
            string styleKey = code.GetHashCode().ToString();
            
            if (!_cachedStyles.TryGetValue(styleKey, out GUIStyle style))
            {
                style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = Colors.Text;
                style.richText = true;
                style.wordWrap = true;
                style.font = EditorStyles.boldFont;
                style.fontSize = 12;
                style.padding = new RectOffset(5, 5, 5, 5);
                
                _cachedStyles[styleKey] = style;
            }
            
            return style;
        }

        public static void DrawLineNumbers(string code, Rect rect, Vector2 scrollPosition)
        {
            int lineCount = CountLines(code);
            GUIStyle lineStyle = new GUIStyle(EditorStyles.miniLabel);
            lineStyle.alignment = TextAnchor.UpperRight;
            lineStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            
            for (int i = 1; i <= lineCount; i++)
            {
                GUI.Label(new Rect(rect.x, rect.y + (i-1) * lineStyle.lineHeight - scrollPosition.y, 
                    30, lineStyle.lineHeight), i.ToString(), lineStyle);
            }
        }
        
        private static int CountLines(string code)
        {
            if (string.IsNullOrEmpty(code)) return 0;
            return code.Split('\n').Length;
        }
        
        public static string ProcessCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;
            
            
            string[] lines = code.Split('\n');
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            
            
            string keywordStyle = $"<color=#{ColorUtility.ToHtmlStringRGB(Colors.Keyword)}>";
            string commentStyle = $"<color=#{ColorUtility.ToHtmlStringRGB(Colors.Comment)}>";
            string stringStyle = $"<color=#{ColorUtility.ToHtmlStringRGB(Colors.String)}>";
            string numberStyle = $"<color=#{ColorUtility.ToHtmlStringRGB(Colors.Number)}>";
            string typeStyle = $"<color=#{ColorUtility.ToHtmlStringRGB(Colors.Class)}>";
            string directiveStyle = $"<color=#{ColorUtility.ToHtmlStringRGB(Colors.Directive)}>";
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                
                int commentIndex = line.IndexOf("//");
                string codePart = commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
                string commentPart = commentIndex >= 0 ? line.Substring(commentIndex) : "";
                
                
                codePart = ProcessStrings(codePart, stringStyle);
                
                
                codePart = ProcessNumbers(codePart, numberStyle);
                
                
                codePart = ProcessKeywords(codePart, keywordStyle, CSharpKeywords);
                
                
                codePart = ProcessKeywords(codePart, typeStyle, UnityTypes);
                
                
                if (line.TrimStart().StartsWith("#"))
                {
                    codePart = directiveStyle + codePart + "</color>";
                }
                
                
                if (!string.IsNullOrEmpty(commentPart))
                {
                    commentPart = commentStyle + commentPart + "</color>";
                }
                
                
                result.AppendLine(codePart + commentPart);
            }
            
            return result.ToString();
        }
        
        private static string ProcessStrings(string code, string style)
        {
            return Regex.Replace(code, "\"(\\\\.|[^\"])*\"", match => $"{style}{match.Value}</color>");
        }
        
        private static string ProcessNumbers(string code, string style)
        {
            return Regex.Replace(code, @"\b\d+\.?\d*f?\b", match => $"{style}{match.Value}</color>");
        }
        
        private static string ProcessKeywords(string code, string style, string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                code = Regex.Replace(code, $@"\b{keyword}\b", match => $"{style}{match.Value}</color>");
            }
            return code;
        }
    }
}