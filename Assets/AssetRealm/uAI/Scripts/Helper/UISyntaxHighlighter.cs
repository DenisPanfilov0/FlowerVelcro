using System.Collections.Generic;
using System.Text.RegularExpressions; 
using UnityEngine;
using UnityEngine.UIElements;

namespace UAI
{
    
    
    
    public class UISyntaxHighlighter
    {
        
        private static Dictionary<string, string> _processedCodeCache = new Dictionary<string, string>();
        
        
        public static class Colors
        {
            public static string Keyword => "#57B1DE"; 
            public static string Comment => "#6B9955"; 
            public static string String => "#CE9178";  
            public static string Number => "#B5CEA8";  
            public static string Class => "#4FC1B1";   
            public static string Method => "#DCDCAA";  
            public static string Property => "#9CDCFE"; 
            public static string Directive => "#C586C0"; 
            public static string Background => "#1E1E1E"; 
            public static string Text => "#E6E6E6"; 
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
        
        
        
        
        
        
        public static void ApplyHighlighting(TextField codeField, string code)
        {
            if (string.IsNullOrEmpty(code) || codeField == null) return;
            
            
            string highlightedCode = GetHighlightedCode(code);
            
            
            codeField.SetValueWithoutNotify(highlightedCode);
            codeField.isReadOnly = true;
            codeField.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f); 
            
            
            codeField.EnableInClassList("syntax-highlighted", true);
        }
        
        
        
        
        
        
        public static string GetHighlightedCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;
            
            
            string cacheKey = code.GetHashCode().ToString();
            if (_processedCodeCache.TryGetValue(cacheKey, out string cachedResult))
            {
                return cachedResult;
            }
            
            
            string[] lines = code.Split('\n');
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            
            foreach (string line in lines)
            {
                string processedLine = ProcessLine(line);
                result.AppendLine(processedLine);
            }
            
            string highlightedCode = result.ToString();
            
            
            _processedCodeCache[cacheKey] = highlightedCode;
            
            return highlightedCode;
        }
        
        
        
        
        private static string ProcessLine(string line)
        {
            
            int commentIndex = line.IndexOf("//");
            string codePart = commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
            string commentPart = commentIndex >= 0 ? line.Substring(commentIndex) : "";
            
            
            codePart = ProcessStrings(codePart);
            
            
            codePart = ProcessNumbers(codePart);
            
            
            codePart = ProcessKeywords(codePart, CSharpKeywords, Colors.Keyword);
            
            
            codePart = ProcessKeywords(codePart, UnityTypes, Colors.Class);
            
            
            if (line.TrimStart().StartsWith("#"))
            {
                codePart = $"<color={Colors.Directive}>{codePart}</color>";
            }
            
            
            if (!string.IsNullOrEmpty(commentPart))
            {
                commentPart = $"<color={Colors.Comment}>{commentPart}</color>";
            }
            
            
            return codePart + commentPart;
        }
        
        private static string ProcessStrings(string code)
        {
            return Regex.Replace(code, "\"(\\\\.|[^\"])*\"", match => 
                $"<color={Colors.String}>{match.Value}</color>");
        }
        
        private static string ProcessNumbers(string code)
        {
            return Regex.Replace(code, @"\b\d+\.?\d*f?\b", match => 
                $"<color={Colors.Number}>{match.Value}</color>");
        }
        
        private static string ProcessKeywords(string code, string[] keywords, string colorHex)
        {
            foreach (string keyword in keywords)
            {
                code = Regex.Replace(code, $@"\b{keyword}\b", match => 
                    $"<color={colorHex}>{match.Value}</color>");
            }
            return code;
        }
        
        
        
        
        public static TextElement CreateHighlightedCodeElement(string code)
        {
            TextElement codeElement = new TextElement();
            codeElement.text = code;
            codeElement.style.whiteSpace = WhiteSpace.Normal;
            codeElement.style.unityTextAlign = TextAnchor.UpperLeft;
            codeElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            codeElement.style.color = new Color(0.9f, 0.9f, 0.9f);
            codeElement.style.paddingLeft = 5;
            codeElement.style.paddingRight = 5;
            codeElement.style.paddingTop = 5;
            codeElement.style.paddingBottom = 5;
            codeElement.style.fontSize = 12;
            codeElement.style.flexGrow = 1;
            codeElement.style.flexShrink = 0; 
            
            
            string highlightedCode = GetHighlightedCode(code);
            codeElement.text = highlightedCode;
            codeElement.enableRichText = true;
            
            return codeElement;
        }
    }
}