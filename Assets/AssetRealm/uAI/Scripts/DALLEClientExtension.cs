using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON; 

namespace UAI
{
    public static class DALLEClientExtension
    {
        public static bool IsExtensionLoaded = true;
        public class MultiImageRequest
        {
            public List<Texture2D> ReferenceImages { get; set; } = new List<Texture2D>();
            public string Prompt { get; set; }
            public ImageModel Model { get; set; } = ImageModel.GPTImage1;
            public int Count { get; set; } = 1;
            public Size Size { get; set; } = Size.Auto;
            public ResponseFormat ResponseFormat { get; set; } = ResponseFormat.URL;
            public Quality Quality { get; set; } = Quality.auto;
            public BackgroundType Background { get; set; } = BackgroundType.Auto;
            public OutputFormat OutputFormat { get; set; } = OutputFormat.PNG;
        }

        public static void GenerateWithReferences(this DALLEClient client, MultiImageRequest request)
        {
            
            
            if (request.ReferenceImages == null || request.ReferenceImages.Count == 0)
            {
                
                
                var standardRequest = new ImageGenerationRequest
                {
                    Prompt = request.Prompt,
                    Model = request.Model,
                    Count = request.Count,
                    Size = request.Size,
                    ResponseFormat = request.ResponseFormat,
                    Quality = request.Quality,
                    Background = request.Background,
                    OutputFormat = request.OutputFormat
                };
                
                client.GenerateImage(standardRequest);
                return;
            }
            
            
            
            CoroutineHelper.StartCor(SendMultiImageRequestCoroutine(client, request));
        }
        
        private static IEnumerator SendMultiImageRequestCoroutine(DALLEClient client, MultiImageRequest request)
        { 
                List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
                
                
                
                
                for (int i = 0; i < request.ReferenceImages.Count; i++)
                {
                    var texture = request.ReferenceImages[i];
                    
                    
                    
                    var processedTexture = EnsureReadableTexture(texture);
                    if (processedTexture == null)
                    {
                        Debug.LogError($"Failed to process reference image {i}");
                        client.status = DALLEStatus.Error;
                        // EditorUtility.DisplayDialog("Reference Image Error", $"Failed to process reference image {i}. Make sure the texture is readable.", "OK");
                        yield break;
                    }
                    
                    byte[] imageData = processedTexture.EncodeToPNG();
                    if (imageData == null || imageData.Length == 0)
                    {
                        Debug.LogError($"Failed to encode reference image {i}");
                        client.status = DALLEStatus.Error;
                        // EditorUtility.DisplayDialog("Reference Image Error", $"Failed to encode reference image {i} to PNG format.", "OK");
                        yield break;
                    }
                    
                    
                    
                    
                    string paramName = $"image[{i}]";
                    formData.Add(new MultipartFormFileSection(paramName, imageData, $"image{i}.png", "image/png"));
                }
                
                
                formData.Add(new MultipartFormDataSection("prompt", request.Prompt));
                formData.Add(new MultipartFormDataSection("model", GetModelName(request.Model)));
                formData.Add(new MultipartFormDataSection("n", request.Count.ToString()));
                
                if (request.Size != UAI.Size.Auto)
                    formData.Add(new MultipartFormDataSection("size", SizeEnumToString(request.Size)));
                
                if (request.Quality != UAI.Quality.auto)
                    formData.Add(new MultipartFormDataSection("quality", request.Quality.ToString().ToLower()));
                
                if (request.Background != UAI.BackgroundType.Auto)
                    formData.Add(new MultipartFormDataSection("background", request.Background.ToString().ToLower()));
                
                
                StringBuilder formDataDebug = new StringBuilder("Form data parameters:\n");
                foreach (var section in formData)
                {
                    formDataDebug.AppendLine($"- {section.GetType().Name}: {section.sectionName}");
                }
                
                
                
                
                
                
                UnityWebRequest www = UnityWebRequest.Post("https://api.openai.com/v1/images/edits", formData);
                www.SetRequestHeader("Authorization", "Bearer " + GPTClient.Instance.apiKey);
                
                string apiKeyPreview = "";
                if (GPTClient.Instance.apiKey != null && GPTClient.Instance.apiKey.Length > 0)
                {
                    apiKeyPreview = GPTClient.Instance.apiKey.Substring(0, 4) + "..." + 
                                    GPTClient.Instance.apiKey.Substring(GPTClient.Instance.apiKey.Length - 4);
                }
                
                
                client.status = DALLEStatus.WaitingForResponse;
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errorText = www.downloadHandler.text;
                    Debug.LogError($"API Error: {www.error}\nResponse: {errorText}");
                    
                    
                    try
                    {
                        JSONNode errorJson = JSON.Parse(errorText);
                        
                        if (errorJson["error"] != null)
                        {
                            string errorMessage = errorJson["error"]["message"];
                            string errorType = errorJson["error"]["type"];
                            
                            
                            if (errorType == "content_policy_violation" || errorMessage.Contains("content policy"))
                            {
                                Debug.LogError("Your request was rejected due to potential content policy violations. Please modify your prompt.");
                            }
                            else if (errorType == "invalid_request_error" && (errorMessage.Contains("size") || errorMessage.Contains("dimensions")))
                            {
                                // EditorUtility.DisplayDialog("Invalid Size", $"The selected image size is not supported: {errorMessage}\n\nPlease select a different size.", "OK");
                                Debug.LogError($"Invalid size error: {errorMessage}");
                            }
                            else if (errorType == "invalid_request_error" && errorMessage.Contains("Duplicate parameter"))
                            {
                                // EditorUtility.DisplayDialog("API Parameter Error", "There was an error with how parameters were sent to the API. This is likely a bug in the tool. " + "Please check the logs for details and contact the developer.", "OK");
                                Debug.LogError($"API parameter error: {errorMessage} - This may indicate a bug in how we're sending parameters to the API.");
                            }
                            else
                            {
                                Debug.LogError($"The OpenAI API returned an error: {errorMessage}");
                            }
                        }
                        else
                        {
                            Debug.LogError("An error occurred while communicating with the OpenAI API. Check the console for details.");
                        }
                    }
                    catch
                    {
                        Debug.LogError("An error occurred while communicating with the OpenAI API. Check the console for details.");
                    }
                    
                    client.status = DALLEStatus.Error;
                }
                else
                {
                    
                    client.status = DALLEStatus.Success;
                    
                    string responseJson = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                    ProcessMultiImageResponse(client, responseJson);
                } 
        }
        
        private static void ProcessMultiImageResponse(DALLEClient client, string responseJson)
        {
            try
            {
                
                
                JSONNode jsonNode = JSON.Parse(responseJson);
                
                
                if (jsonNode["error"] != null)
                {
                    string errorMessage = jsonNode["error"]["message"];
                    string errorType = jsonNode["error"]["type"];
                    Debug.LogError($"OpenAI API Error: {errorType} - {errorMessage}");
                    
                    
                    if (errorType == "content_policy_violation" || errorMessage.Contains("content policy") || 
                        errorMessage.Contains("content filter") || errorMessage.Contains("moderation") ||
                        errorMessage.Contains("safety"))
                    {
                        Debug.LogError("Content policy violation: The request likely contains or would generate content that violates OpenAI's content policy.");
                        // EditorUtility.DisplayDialog("Content Policy Violation", "Your request was rejected by OpenAI's content filter. Please revise your prompt to ensure it complies with OpenAI's content policy.", "OK");
                    }
                    
                    client.status = DALLEStatus.Error;
                    return;
                }
                
                
                if (jsonNode["data"] == null || jsonNode["data"].Count == 0)
                {
                    Debug.LogError("No image data found in API response");
                    client.status = DALLEStatus.Error;
                    return;
                }
                
                List<DALLEImageResult> results = new List<DALLEImageResult>();
                
                var dataNode = jsonNode["data"];
                
                
                for (int i = 0; i < dataNode.Count; i++)
                {
                    var imageData = dataNode[i];
                    DALLEImageResult result = new DALLEImageResult();
                    
                    
                    if (imageData["url"] != null)
                    {
                        result.Url = imageData["url"];
                        result.HasUrl = true;
                        
                    }
                    
                    
                    if (imageData["b64_json"] != null)
                    {
                        result.Base64Json = imageData["b64_json"];
                        result.HasBase64 = true;
                        
                        
                        
                        try {
                            byte[] imageBytes = Convert.FromBase64String(result.Base64Json);
                            result.Texture = new Texture2D(2, 2);
                            result.Texture.LoadImage(imageBytes);
                            
                        }
                        catch (Exception ex) {
                            Debug.LogError($"Failed to convert base64 to texture: {ex.Message}");
                        }
                    }
                    
                    
                    if (imageData["revised_prompt"] != null)
                    {
                        result.RevisedPrompt = imageData["revised_prompt"];
                        
                    }
                    
                    
                    if (result.HasUrl && result.Texture == null)
                    {
                        
                        CoroutineHelper.StartCor(LoadTextureFromUrl(result.Url, (texture) => {
                            result.Texture = texture;
                            
                            
                            
                            if (results.TrueForAll(r => r.Texture != null))
                            {
                                
                                client.OnResponseReceived?.Invoke(results);
                            }
                        }));
                    }
                    
                    results.Add(result);
                }
                
                
                if (results.Count > 0 && !results.Exists(r => r.HasUrl && r.Texture == null))
                {
                    
                    client.OnResponseReceived?.Invoke(results);
                }
                else if (results.Count == 0)
                {
                    Debug.LogWarning("No results generated from API response");
                    client.status = DALLEStatus.Error;
                    // EditorUtility.DisplayDialog("No Images Generated", "The API request completed but no images were generated. This might be due to content policy restrictions. Try modifying your prompt.", "OK");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing multi-image response: {e.Message}\nResponse: {responseJson}");
                client.status = DALLEStatus.Error;
                // EditorUtility.DisplayDialog("Error Processing Response", "There was an error processing the API response. Check the console for details.", "OK");
            }
        }
        
        private static IEnumerator LoadTextureFromUrl(string url, Action<Texture2D> callback)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.ConnectionError || 
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error loading image from URL: {request.error}");
                    callback(null);
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    callback(texture);
                }
            }
        }
        
        private static Texture2D EnsureReadableTexture(Texture2D source)
        {
            if (source == null) return null;
            
            
            if (source.isReadable) return source;
            
            try
            {
                
                RenderTexture tempRT = RenderTexture.GetTemporary(
                    source.width, 
                    source.height, 
                    0, 
                    RenderTextureFormat.ARGB32, 
                    RenderTextureReadWrite.sRGB);
                
                
                Graphics.Blit(source, tempRT);
                
                
                RenderTexture previousRT = RenderTexture.active;
                
                
                RenderTexture.active = tempRT;
                
                
                Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                
                
                readableTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readableTexture.Apply();
                
                
                RenderTexture.active = previousRT;
                
                
                RenderTexture.ReleaseTemporary(tempRT);
                
                return readableTexture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating readable texture: {ex.Message}");
                return null;
            }
        }
        
        private static string GetModelName(ImageModel model)
        {
            switch (model)
            {
                case ImageModel.DallE2: return "dall-e-2";
                case ImageModel.DallE3: return "dall-e-3";
                case ImageModel.GPTImage1: return "gpt-image-1";
                default: return "gpt-image-1";
            }
        }
        
        private static string SizeEnumToString(UAI.Size size)
        {
            switch (size)
            {
                
                case UAI.Size.Size1024x1024: return "1024x1024";
                case UAI.Size.Size1536x1024: return "1536x1024";
                case UAI.Size.Size1024x1536: return "1024x1536";
                case UAI.Size.Size1792x1024: return "1792x1024";
                case UAI.Size.Size1024x1792: return "1024x1792";
                default: return "1024x1024";
            }
        }
    }
}