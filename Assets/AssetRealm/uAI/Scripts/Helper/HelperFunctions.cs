using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UAI{ 
    public class HelperFunctions 
    {   
        public static string RemoveScriptTagFromOpenAIResponse(string text)
        {  
            if(text.Contains("```csharp"))
            {
                text = text.Replace("```csharp", "```");  
            }
            if(text.Contains("```C#"))
            {
                text = text.Replace("```C#", "```");  
            } 
            if(text.Contains("```cs"))
            {
                text = text.Replace("```cs", "```");  
            }
            if(text.Contains("```c#"))
            {
                text = text.Replace("```c#", "```");  
            } 

            if(text.Contains("csharp"))
                text = text.Replace("csharp", "");   

            return text;
        } 
    }
}
 