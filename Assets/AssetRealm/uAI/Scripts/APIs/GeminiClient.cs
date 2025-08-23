using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace UAI
{
    public class GeminiClient : AbstractAIClient
    {
        #region Static Properties
        private static UnityWebRequest _currentRequest;
        
        private static string[] _supportedModels = new string[] { 
            "gemini-2.5-flash-preview-04-17",
            "gemini-2.5-pro-preview-05-06",
            "gemini-2.0-flash",
            "gemini-2.0-flash-preview-image-generation",
            "gemini-2.0-flash-lite",
            "gemini-1.5-pro",
            "gemini-1.5-flash",
            "gemini-1.5-flash-8b" 
        };
        public override string[] SupportedModels => _supportedModels;
        #endregion

        #region Private Fields
        private string _responseText = "";
        private StringBuilder _fullResponseBuilder = new StringBuilder();
        private string _apiVersion = "v1beta";  
        private List<string> _processedEvents = new List<string>();
        private bool _receivedDoneEvent = false;
        #endregion

        #region Constructor
        public GeminiClient()
        {
            this.modelName = "gemini-2.5-pro-preview-05-06";  
            UpdateEndpointUrl();
        }
        #endregion

        #region Implementation Methods
        public override void SendRequest(string promptSend, int index = 0)
        {
            List<GPTChatMessage> messages = new List<GPTChatMessage>();
            
            if (!string.IsNullOrEmpty(SystemInitPrompt))
            {
                messages.Add(new GPTChatMessage
                {
                    role = "user",
                    content = SystemInitPrompt
                });
                
                messages.Add(new GPTChatMessage
                {
                    role = "model",
                    content = "I understand."
                });
            }
            
            messages.Add(new GPTChatMessage
            {
                role = "user",
                content = promptSend
            });
            
            SendRequestWithHistory(messages);
        }

        public override void SendRequestWithHistory(List<GPTChatMessage> chatMessages)
        {
            UpdateEndpointUrl();
            
            JSONNode requestBody = CreateBaseRequestBody();
            
            JSONArray contentsArray = new JSONArray();
            
            foreach (GPTChatMessage message in chatMessages)
            {
                if (string.IsNullOrEmpty(message.content))
                    continue;
                
                string role = "user";
                if (message.role == "assistant")
                    role = "model";
                else if (message.role == "system")
                    role = "user"; 
                
                JSONNode contentNode = JSON.Parse("{}");
                contentNode["role"] = role;
                
                JSONArray partsArray = new JSONArray();
                JSONNode textPart = JSON.Parse("{}");
                textPart["text"] = message.content;
                partsArray.Add(textPart);
                
                contentNode["parts"] = partsArray;
                contentsArray.Add(contentNode);
            }
            
            requestBody["contents"] = contentsArray;
            
            if (!requestBody["generationConfig"].HasKey("maxOutputTokens") || 
                requestBody["generationConfig"]["maxOutputTokens"].AsInt < 1024)
            {
                requestBody["generationConfig"]["maxOutputTokens"] = 4096;
            }
            
            string requestBodyString = requestBody.ToString();
            CoroutineHelper.StartCor(SendRequestCoroutine(requestBodyString));
        }

        public override void StopGeneration()
        {
            if (_currentRequest != null)
            {
                _currentRequest.Abort();
                GPTClient.status = GPTStatus.Idle;
            }
        }

        protected override object ConvertMessages(List<GPTChatMessage> messages)
        {
            JSONArray contentsArray = new JSONArray();
            
            foreach (GPTChatMessage message in messages)
            {
                if (string.IsNullOrEmpty(message.content))
                    continue;
                
                string role = "user";
                if (message.role == "assistant")
                    role = "model";
                else if (message.role == "system")
                    role = "user"; 
                
                JSONNode contentNode = JSON.Parse("{}");
                contentNode["role"] = role;
                
                // Text parts
                JSONArray partsArray = new JSONArray();
                JSONNode textPart = JSON.Parse("{}");
                textPart["text"] = message.content;
                partsArray.Add(textPart);
                
                contentNode["parts"] = partsArray;
                contentsArray.Add(contentNode);
            }
            
            return contentsArray;
        }
        #endregion

        #region Private Methods
        private void UpdateEndpointUrl()
        {
            // Use alt=sse for Server-Sent Events streaming
            this.endpointUrl = $"https://generativelanguage.googleapis.com/{_apiVersion}/models/{modelName}:streamGenerateContent?alt=sse&key={apiKey}";
        }
        
        private JSONNode CreateBaseRequestBody()
        {
            JSONNode requestBody = JSON.Parse("{}");
            
            JSONNode generationConfig = JSON.Parse("{}");
            generationConfig["temperature"] = temperature;
            generationConfig["maxOutputTokens"] = maxTokens > 0 ? maxTokens : 65536;  
            requestBody["generationConfig"] = generationConfig;
            
            return requestBody;
        }

        private IEnumerator SendRequestCoroutine(string requestBodyString)
        {
            _currentRequest = new UnityWebRequest(endpointUrl, "POST");
            _currentRequest.SetRequestHeader("Content-Type", "application/json");
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyString);
            _currentRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            _currentRequest.downloadHandler = new DownloadHandlerBuffer();

            GPTClient.status = GPTStatus.WaitingForResponse;
            _responseText = "";
            _fullResponseBuilder.Clear();
            _processedEvents.Clear();
            _receivedDoneEvent = false;
            
            _currentRequest.SendWebRequest();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.update += CheckRequestProgress;
            #else
            float checkInterval = 0.1f;
            while (!_currentRequest.isDone)
            {
                CheckRequestProgress();
                yield return new WaitForSeconds(checkInterval);
            }
            
            float finalProcessingDelay = 0.5f;
            yield return new WaitForSeconds(finalProcessingDelay);
            
            CheckRequestProgress();
            #endif

            ProcessFullResponse();
            
            yield return null;
        }
        
        private void ProcessFullResponse()
        {
            if (_currentRequest == null || string.IsNullOrEmpty(_currentRequest.downloadHandler.text))
                return;

            string fullContent = _currentRequest.downloadHandler.text;

            try
            {
                
                StringBuilder completeResponseBuilder = new StringBuilder();
                bool foundContent = false;

                string[] events = fullContent.Split(new string[] { "data:" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var eventText in events)
                {
                    string cleanedEvent = eventText.Trim();

                    // Skip [DONE] markers
                    if (cleanedEvent == "[DONE]")
                    {
                        _receivedDoneEvent = true;
                        continue;
                    }

                    try
                    {
                        JSONNode jsonNode = JSON.Parse(cleanedEvent);

                        // Process the candidate response
                        if (jsonNode["candidates"] != null && jsonNode["candidates"].Count > 0)
                        {
                            JSONNode candidate = jsonNode["candidates"][0];

                            if (candidate["content"] != null && candidate["content"]["parts"] != null &&
                                candidate["content"]["parts"].Count > 0)
                            {
                                string text = candidate["content"]["parts"][0]["text"];
                                if (!string.IsNullOrEmpty(text))
                                {
                                    completeResponseBuilder.Append(text);
                                    foundContent = true;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Skip invalid JSON chunks
                    }
                }

                if (foundContent)
                {
                    string completeResponse = completeResponseBuilder.ToString();

                    // Only update if we found a longer/more complete response
                    if (completeResponse.Length > _responseText.Length)
                    {
                        _responseText = completeResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing full Gemini response: {ex.Message}");
            }

            // Ensure we send the final complete response
            if (!string.IsNullOrEmpty(_responseText))
            {
                OnResponseReceived?.Invoke(_responseText, 0);
            }
        }

        private void CheckRequestProgress()
        {
            if (_currentRequest == null || string.IsNullOrEmpty(_currentRequest.downloadHandler.text))
                return;

            string currentContent = _currentRequest.downloadHandler.text;
            
            // Check for the [DONE] marker which signals the end of the stream
            if (currentContent.Contains("data: [DONE]") && !_receivedDoneEvent)
            {
                _receivedDoneEvent = true;
            }
            
            // Process SSE format - separate by "data:" lines
            string[] events = currentContent.Split(new string[] { "data:" }, StringSplitOptions.RemoveEmptyEntries);
            
            StringBuilder newTextBuilder = new StringBuilder();
            bool foundNewContent = false;
            
            foreach (var eventText in events)
            {
                string cleanedEvent = eventText.Trim();
                
                // Skip empty events or already processed ones
                if (string.IsNullOrEmpty(cleanedEvent) || _processedEvents.Contains(cleanedEvent))
                    continue;
                
                // Add to processed events to avoid duplication
                _processedEvents.Add(cleanedEvent);
                
                // Check for the [DONE] marker
                if (cleanedEvent == "[DONE]")
                {
                    _receivedDoneEvent = true;
                    continue;
                }
                
                try
                {
                    // For SSE events from Gemini, we need to parse the JSON
                    JSONNode jsonNode = JSON.Parse(cleanedEvent);
                    
                    // Check if this is a candidate response
                    if (jsonNode["candidates"] != null && jsonNode["candidates"].Count > 0)
                    {
                        JSONNode candidate = jsonNode["candidates"][0];
                        
                        if (candidate["content"] != null && candidate["content"]["parts"] != null && 
                            candidate["content"]["parts"].Count > 0)
                        {
                            string text = candidate["content"]["parts"][0]["text"];
                            if (!string.IsNullOrEmpty(text))
                            {
                                newTextBuilder.Append(text);
                                foundNewContent = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip invalid JSON - this could be "[DONE]" or partial chunks
                    if (!cleanedEvent.Contains("[DONE]"))
                    {
                        Debug.LogWarning($"Error parsing Gemini response chunk: {ex.Message}");
                    }
                }
            }

            // If we found new content
            if (foundNewContent)
            {
                string newText = newTextBuilder.ToString();

                // Add to our full response builder
                _fullResponseBuilder.Append(newText);
                _responseText = _fullResponseBuilder.ToString();
                
                // Send only the new content via streaming
                OnPartResponseReceived?.Invoke(newText);
            }

            // Check if request is complete
            if (_currentRequest.isDone || _receivedDoneEvent)
            {
                if (_currentRequest.result == UnityWebRequest.Result.ConnectionError || 
                    _currentRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Gemini request error: {_currentRequest.error}");
                    GPTClient.status = GPTStatus.Error;
                }
                else
                {
                    GPTClient.status = GPTStatus.Success;
                }

                if (!string.IsNullOrEmpty(_responseText))
                {
                    // Send the final complete response when done
                    OnResponseReceived?.Invoke(_responseText, 0);
                }
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= CheckRequestProgress;
                #endif
            }
        }
        #endregion
    }
}