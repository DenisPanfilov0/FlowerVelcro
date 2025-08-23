using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace UAI
{
    public static class GPTClientExtension
    {
        
        public delegate void StructuredResponseHandler<T>(T responseData);
        
        
        public static bool IsExtensionLoaded => true;
        
        public static void SendStructuredRequest<T>(string systemPrompt, string userPrompt, StructuredResponseHandler<T> responseHandler) where T : class
        {
            
            List<GPTChatMessage> chatMessages = new List<GPTChatMessage>
            {
                new GPTChatMessage { role = "system", content = systemPrompt },
                new GPTChatMessage { role = "user", content = userPrompt }
            };
            
            
            GPTClient.Instance.OnResponseReceived += (response, index) => {
                try
                {
                    // Debug.Log($"Received structured response: {response}");
                    
                    
                    T parsedResponse = JsonUtility.FromJson<T>(response);
                    
                    if (parsedResponse != null)
                    {
                        // Debug.Log($"Successfully parsed response of type {typeof(T).Name}");
                        responseHandler?.Invoke(parsedResponse);
                    }
                    else
                    {
                        
                        Debug.LogWarning("JsonUtility failed to parse, trying SimpleJSON as fallback");
                        JSONNode jsonNode = JSON.Parse(response);
                        
                        
                        T instance = Activator.CreateInstance<T>();
                        
                        
                        foreach (var property in typeof(T).GetProperties())
                        {
                            if (jsonNode[property.Name] != null)
                            {
                                if (property.PropertyType == typeof(bool))
                                {
                                    property.SetValue(instance, jsonNode[property.Name].AsBool);
                                }
                                else if (property.PropertyType == typeof(int))
                                {
                                    property.SetValue(instance, jsonNode[property.Name].AsInt);
                                }
                                else if (property.PropertyType == typeof(float))
                                {
                                    property.SetValue(instance, jsonNode[property.Name].AsFloat);
                                }
                                else
                                {
                                    property.SetValue(instance, jsonNode[property.Name]);
                                }
                            }
                        }
                        
                        foreach (var field in typeof(T).GetFields())
                        {
                            if (jsonNode[field.Name] != null)
                            {
                                if (field.FieldType == typeof(bool))
                                {
                                    field.SetValue(instance, jsonNode[field.Name].AsBool);
                                }
                                else if (field.FieldType == typeof(int))
                                {
                                    field.SetValue(instance, jsonNode[field.Name].AsInt);
                                }
                                else if (field.FieldType == typeof(float))
                                {
                                    field.SetValue(instance, jsonNode[field.Name].AsFloat);
                                }
                                else
                                {
                                    field.SetValue(instance, jsonNode[field.Name]);
                                }
                            }
                        }
                        
                        responseHandler?.Invoke(instance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing structured response: {ex.Message}. Response: {response}");
                }
                
                
                GPTClient.Instance.OnResponseReceived -= (string r, int i) => {};
            };
            
            
            GPTClient.Instance.SendRequestWithHistory(chatMessages);
        }
    }
}