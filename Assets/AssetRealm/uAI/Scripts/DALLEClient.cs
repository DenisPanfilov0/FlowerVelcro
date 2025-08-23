using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text;

namespace UAI
{
    public class DALLEClient
    {
        private static DALLEClient _instance;

        public static DALLEClient Instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new DALLEClient();
                }
                return _instance;
            }
        }

        public DALLEClient()
        {
            _instance = this;
        }

        
        private readonly string apiUrlGenerations = "https://api.openai.com/v1/images/generations";
        private readonly string apiUrlEdits = "https://api.openai.com/v1/images/edits";
        private readonly string apiUrlVariations = "https://api.openai.com/v1/images/variations";

        [HideInInspector]
        public DALLEStatus status = DALLEStatus.Idle;
        
        [HideInInspector]
        public string SystemInitPrompt = "You are an AI trained to create images from text descriptions.";

        public Action<List<DALLEImageResult>> OnResponseReceived;

        #region Image Generation Methods

        
        
        
        public void GenerateImage(ImageGenerationRequest request)
        {
            JSONNode requestBody = JSON.Parse("{}");

            requestBody["prompt"] = request.Prompt; 
            requestBody["model"] = GetModelName(request.Model); 

            
            switch (request.Model)
            {
                case ImageModel.GPTImage1:
                    if (request.Background != BackgroundType.Auto)
                        requestBody["background"] = request.Background.ToString().ToLower();
                    
                    if (request.Moderation != Moderation.Auto)
                        requestBody["moderation"] = request.Moderation.ToString().ToLower();
                    
                    if (request.OutputCompression != 100)
                        requestBody["output_compression"] = request.OutputCompression;
                    
                    if (request.OutputFormat != OutputFormat.PNG)
                        requestBody["output_format"] = request.OutputFormat.ToString().ToLower();
                    
                    if (request.Quality != Quality.auto)
                        requestBody["quality"] = request.Quality.ToString().ToLower();
                    
                    if (request.Size != Size.Auto)
                        requestBody["size"] = sizeEnumToString(request.Size);
                    break;
                
                case ImageModel.DallE3:
                    if (request.Quality != Quality.auto)
                        requestBody["quality"] = request.Quality == Quality.high ? "hd" : "standard";
                    
                    if (request.Style != Style.Vivid)
                        requestBody["style"] = request.Style.ToString().ToLower();
                    
                    if (request.Size != Size.Auto)
                        requestBody["size"] = sizeEnumToString(request.Size);
                    
                    requestBody["n"] = 1; 
                    break;
                
                case ImageModel.DallE2:
                    if (request.Size != Size.Auto)
                        requestBody["size"] = sizeEnumToString(request.Size);
                    
                    requestBody["n"] = request.Count;
                    break;
            }

            
            if (request.Model != ImageModel.DallE3 && request.Count != 1)
                requestBody["n"] = request.Count;
            
            if (request.ResponseFormat != ResponseFormat.URL && request.Model != ImageModel.GPTImage1)
                requestBody["response_format"] = request.ResponseFormat.ToString().ToLower();

            string requestBodyString = requestBody.ToString();
            CoroutineHelper.StartCor(SendRequestCoroutine(apiUrlGenerations, requestBodyString));
        }

        
        
        
        public void EditImage(ImageEditRequest request)
        {
            CoroutineHelper.StartCor(SendImageEditRequestCoroutine(request));
        }

        
        
        
        public void CreateVariation(ImageVariationRequest request)
        {
            CoroutineHelper.StartCor(SendVariationRequestCoroutine(request));
        }

        #endregion 

        #region Coroutines

        private IEnumerator SendRequestCoroutine(string url, string requestBodyString)
        {
            UnityWebRequest request = new UnityWebRequest(url, "POST");

            request.SetRequestHeader("Authorization", "Bearer " + GPTClient.Instance.apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBodyString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            status = DALLEStatus.WaitingForResponse;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"DALL-E API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                status = DALLEStatus.Error;
            }
            else
            {
                status = DALLEStatus.Success;

                string responseJson = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                ProcessImageResponse(responseJson);
            }
        }

        private string GetModelName(ImageModel model)
        {
            switch (model)
            {
                case ImageModel.DallE2:
                    return "dall-e-2";
                case ImageModel.DallE3:
                    return "dall-e-3";
                case ImageModel.GPTImage1:
                    return "gpt-image-1";
                default:
                    return "gpt-image-1";
            }
        }
        private string sizeEnumToString(Size size)
        {
            switch (size)
            {
                case Size.Size256x256:
                    return "256x256";
                case Size.Size512x512:
                    return "512x512";
                case Size.Size1024x1024:
                    return "1024x1024";
                case Size.Size1536x1024:
                    return "1536x1024";
                case Size.Size1024x1536:
                    return "1024x1536";
                case Size.Size1792x1024:
                    return "1792x1024";
                case Size.Size1024x1792:
                    return "1024x1792";
                default:
                    return "auto";
            }
        }

        private IEnumerator SendImageEditRequestCoroutine(ImageEditRequest request)
        {
            
            
                List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
                
                
                byte[] imageData = null;
                try
                {
                    imageData = request.Image.EncodeToPNG();
                    if (imageData == null || imageData.Length == 0)
                    {
                        Debug.LogError("Failed to encode image to PNG. The texture may be compressed or in an unsupported format.");
                        status = DALLEStatus.Error;
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error encoding image: {ex.Message}");
                    status = DALLEStatus.Error;
                    yield break;
                }
                
                formData.Add(new MultipartFormFileSection("image", imageData, "image.png", "image/png"));
                
                
                if (request.Mask != null)
                {
                    byte[] maskData = null;
                    try
                    {
                        maskData = request.Mask.EncodeToPNG();
                        if (maskData == null || maskData.Length == 0)
                        {
                            Debug.LogError("Failed to encode mask to PNG. The texture may be compressed or in an unsupported format.");
                            status = DALLEStatus.Error;
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error encoding mask: {ex.Message}");
                        status = DALLEStatus.Error;
                        yield break;
                    }
                    
                    formData.Add(new MultipartFormFileSection("mask", maskData, "mask.png", "image/png"));
                }
                
                
                formData.Add(new MultipartFormDataSection("prompt", request.Prompt));
                formData.Add(new MultipartFormDataSection("model", GetModelName(request.Model)));
                formData.Add(new MultipartFormDataSection("n", request.Count.ToString()));
                
                if (request.Size != Size.Auto)
                    formData.Add(new MultipartFormDataSection("size", sizeEnumToString(request.Size)));
                
                if (request.ResponseFormat != ResponseFormat.URL && request.Model != ImageModel.GPTImage1)
                    formData.Add(new MultipartFormDataSection("response_format", request.ResponseFormat.ToString().ToLower()));
                
                if (request.Model == ImageModel.GPTImage1)
                {
                    if (request.Background != BackgroundType.Auto)
                        formData.Add(new MultipartFormDataSection("background", request.Background.ToString().ToLower()));
                    
                    if (request.Quality != Quality.auto)
                        formData.Add(new MultipartFormDataSection("quality", request.Quality.ToString().ToLower()));
                }

                UnityWebRequest www = UnityWebRequest.Post(apiUrlEdits, formData);
                www.SetRequestHeader("Authorization", "Bearer " + GPTClient.Instance.apiKey);

                status = DALLEStatus.WaitingForResponse;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"DALL-E API Error: {www.error}\nResponse: {www.downloadHandler.text}");
                    status = DALLEStatus.Error;
                }
                else
                {
                    status = DALLEStatus.Success;

                    string responseJson = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                    ProcessImageResponse(responseJson);
                }
            
            
            
            
            
            
        }
        private IEnumerator SendVariationRequestCoroutine(ImageVariationRequest request)
        {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            
            
            Texture2D processedTexture = EnsureUncompressedTexture(request.Image);
            
            if (processedTexture == null)
            {
                Debug.LogError("Failed to process texture for API submission. The texture may be in an unsupported format.");
                status = DALLEStatus.Error;
                yield break;
            }
            
            
            byte[] imageData = processedTexture.EncodeToPNG();
            
            if (imageData == null || imageData.Length == 0)
            {
                Debug.LogError("Failed to encode image to PNG.");
                status = DALLEStatus.Error;
                yield break;
            }
            
            formData.Add(new MultipartFormFileSection("image", imageData, "image.png", "image/png"));
            
            
            formData.Add(new MultipartFormDataSection("model", "dall-e-2")); 
            formData.Add(new MultipartFormDataSection("n", request.Count.ToString()));
            
            if (request.Size != Size.Auto)
                formData.Add(new MultipartFormDataSection("size", sizeEnumToString(request.Size)));
            
            if (request.ResponseFormat != ResponseFormat.URL)
                formData.Add(new MultipartFormDataSection("response_format", request.ResponseFormat.ToString().ToLower()));

            UnityWebRequest www = UnityWebRequest.Post(apiUrlVariations, formData);
            www.SetRequestHeader("Authorization", "Bearer " + GPTClient.Instance.apiKey);

            status = DALLEStatus.WaitingForResponse;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"DALL-E API Error: {www.error}\nResponse: {www.downloadHandler.text}");
                status = DALLEStatus.Error;
            }
            else
            {
                status = DALLEStatus.Success;

                string responseJson = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                ProcessImageResponse(responseJson);
            }
        }


        private Texture2D EnsureUncompressedTexture(Texture2D source)
        {
            if (source == null) return null;
            
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
                
                
                Texture2D uncompressedTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                
                
                uncompressedTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                uncompressedTexture.Apply();
                
                
                RenderTexture.active = previousRT;
                
                
                RenderTexture.ReleaseTemporary(tempRT);
                
                return uncompressedTexture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating uncompressed texture: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Response Processing

        private void ProcessImageResponse(string responseJson)
        {
            try
            {
                JSONNode jsonNode = JSON.Parse(responseJson);
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
                    }

                    
                    if (imageData["revised_prompt"] != null)
                    {
                        result.RevisedPrompt = imageData["revised_prompt"];
                    }

                    
                    if (result.HasUrl)
                    {
                        CoroutineHelper.StartCor(LoadTextureFromUrl(result.Url, (texture) => {
                            result.Texture = texture;
                            
                            
                            if (results.TrueForAll(r => r.Texture != null))
                            {
                                OnResponseReceived?.Invoke(results);
                            }
                        }));
                    }
                    
                    else if (result.HasBase64)
                    {
                        byte[] imageBytes = Convert.FromBase64String(result.Base64Json);
                        result.Texture = new Texture2D(2, 2);
                        result.Texture.LoadImage(imageBytes);
                    }

                    results.Add(result);
                }

                
                if (OnResponseReceived != null)
                {
                    
                    if (results.Count > 0 && !results.Exists(r => r.HasUrl && r.Texture == null))
                    {
                        OnResponseReceived.Invoke(results);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing DALL-E response: {e.Message}\nResponse: {responseJson}");
                status = DALLEStatus.Error;
            }
        }

        private IEnumerator LoadTextureFromUrl(string url, Action<Texture2D> callback)
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
        
        #endregion
    }

    #region Enums and Data Classes

    public enum DALLEStatus
    {
        Idle,
        WaitingForResponse,
        Success,
        Error
    }

    public enum ImageModel
    {
        DallE2,
        DallE3,
        GPTImage1
    }

    public enum Size
    {
        Auto,
        Size256x256,
        Size512x512,
        Size1024x1024,
        Size1536x1024,
        Size1024x1536,
        Size1792x1024,
        Size1024x1792
    }

    public enum ResponseFormat
    {
        URL,
        B64Json
    }

    public enum Quality
    {
        auto,
        high,
        medium,
        low,
        hd, 
        standard
    }

    public enum Style
    {
        Vivid,
        Natural
    }

    public enum BackgroundType
    {
        Auto,
        Transparent,
        Opaque
    }

    public enum OutputFormat
    {
        PNG,
        JPEG,
        WebP
    }

    public enum Moderation
    {
        Auto,
        Low
    }

    
    
    
    public class ImageGenerationRequest
    {
        public string Prompt { get; set; }
        public ImageModel Model { get; set; } = ImageModel.GPTImage1;
        public int Count { get; set; } = 1;
        public Size Size { get; set; } = Size.Auto;
        public ResponseFormat ResponseFormat { get; set; } = ResponseFormat.URL;
        public Quality Quality { get; set; } = Quality.auto;
        public Style Style { get; set; } = Style.Vivid;
        public BackgroundType Background { get; set; } = BackgroundType.Auto;
        public OutputFormat OutputFormat { get; set; } = OutputFormat.PNG;
        public int OutputCompression { get; set; } = 100;
        public Moderation Moderation { get; set; } = Moderation.Auto;
    }

    
    
    
    public class ImageEditRequest
    {
        public Texture2D Image { get; set; }
        public Texture2D Mask { get; set; }
        public string Prompt { get; set; }
        public ImageModel Model { get; set; } = ImageModel.GPTImage1;
        public int Count { get; set; } = 1;
        public Size Size { get; set; } = Size.Auto;
        public ResponseFormat ResponseFormat { get; set; } = ResponseFormat.URL;
        public Quality Quality { get; set; } = Quality.auto;
        public BackgroundType Background { get; set; } = BackgroundType.Auto;
    }

    
    
    
    public class ImageVariationRequest
    {
        public Texture2D Image { get; set; }
        public int Count { get; set; } = 1;
        public Size Size { get; set; } = Size.Auto;
        public ResponseFormat ResponseFormat { get; set; } = ResponseFormat.URL;
    }

    
    
    
    public class DALLEImageResult
    {
        public string Url { get; set; }
        public bool HasUrl { get; set; }
        public string Base64Json { get; set; }
        public bool HasBase64 { get; set; }
        public string RevisedPrompt { get; set; }
        public Texture2D Texture { get; set; }

        
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    #endregion
}