using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace UAI
{
    /// <summary>
    /// Client for interacting with OpenAI's Reasoning models (GPT-5, o3, etc.) using the Responses API.
    /// </summary>
    public class OpenAIClientReasoning : AbstractAIClient
    {
        #region Static Properties
        private static UnityWebRequest _currentRequest;
        private static string[] _supportedModels = new string[] { "gpt-5", "gpt-5-mini", "gpt-5-nano", "o3", "o3-mini", "o1" };
        public override string[] SupportedModels => _supportedModels;
        #endregion

        #region Private Fields
        private string _responseText = "";
        private ReasoningEffort _reasoningEffort = ReasoningEffort.Medium;
        private bool _includeSummary = false;
        private string _summaryType = "auto";
        private List<string> _processedEvents = new List<string>();
        #endregion

        #region Public Properties
        /// <summary>
        /// Controls how much reasoning the model should do. Low favors speed, High favors thoroughness.
        /// </summary>
        public ReasoningEffort ReasoningEffort
        {
            get => _reasoningEffort;
            set => _reasoningEffort = value;
        }

        /// <summary>
        /// Whether to include reasoning summary in the response.
        /// </summary>
        public bool IncludeSummary 
        { 
            get => _includeSummary; 
            set => _includeSummary = value; 
        }

        /// <summary>
        /// Type of summary to include (auto, concise, detailed).
        /// </summary>
        public string SummaryType 
        { 
            get => _summaryType; 
            set => _summaryType = value; 
        }
        #endregion

        #region Constructor
        public OpenAIClientReasoning()
        {
            this.endpointUrl = "https://api.openai.com/v1/responses";
            this.modelName = "gpt-5";
        }
        #endregion

        #region Implementation Methods
        public override void SendRequest(string promptSend, int index = 0)
        {
            List<GPTChatMessage> messages = new List<GPTChatMessage>();
            
            // Add system message if provided
            if (!string.IsNullOrEmpty(SystemInitPrompt))
            {
                messages.Add(new GPTChatMessage
                {
                    role = "system",
                    content = SystemInitPrompt
                });
            }
            
            // Add user message
            messages.Add(new GPTChatMessage
            {
                role = "user",
                content = promptSend
            });
            
            SendRequestWithHistory(messages);
        }

        public override void SendRequestWithHistory(List<GPTChatMessage> chatMessages)
        {
            JSONNode requestBody = CreateBaseRequestBody();
            
            // Convert messages to input format for Responses API
            JSONArray inputArray = new JSONArray();
            
            foreach (GPTChatMessage message in chatMessages)
            {
                if (string.IsNullOrEmpty(message.content))
                    continue;
                
                JSONNode messageNode = JSON.Parse("{}");
                messageNode["role"] = message.role;
                messageNode["content"] = message.content;
                inputArray.Add(messageNode);
            }
            
            requestBody["input"] = inputArray;
            
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
            JSONArray inputArray = new JSONArray();
            
            foreach (GPTChatMessage message in messages)
            {
                if (string.IsNullOrEmpty(message.content))
                    continue;
                    
                JSONNode messageNode = JSON.Parse("{}");
                messageNode["role"] = message.role;
                messageNode["content"] = message.content;
                inputArray.Add(messageNode);
            }
            
            return inputArray;
        }
        #endregion

        #region Private Methods
        private JSONNode CreateBaseRequestBody()
        {
            JSONNode requestBody = JSON.Parse("{}");
            requestBody["model"] = modelName;
            requestBody["stream"] = true; // Enable streaming for reasoning models
            
            // Add reasoning configuration
            JSONNode reasoningNode = JSON.Parse("{}");
            reasoningNode["effort"] = GetReasoningEffortString();
            
            if (_includeSummary)
            {
                reasoningNode["summary"] = _summaryType;
            }
            
            requestBody["reasoning"] = reasoningNode;
            
            // Use max_output_tokens instead of max_tokens for reasoning models
            if (maxTokens > 0)
            {
                requestBody["max_output_tokens"] = maxTokens;
            }
            
            // Note: Reasoning models don't use temperature in the same way
            // but we can include it for compatibility
            if (temperature != 0.7f) // Only include if not default
            {
                requestBody["temperature"] = temperature;
            }
            
            return requestBody;
        }

        private string GetReasoningEffortString()
        {
            switch (_reasoningEffort)
            {
                case ReasoningEffort.Low:
                    return "low";
                case ReasoningEffort.Medium:
                    return "medium";
                case ReasoningEffort.High:
                    return "high";
                default:
                    return "medium";
            }
        }

        private IEnumerator SendRequestCoroutine(string requestBodyString)
        {
            // Create and configure the web request
            _currentRequest = new UnityWebRequest(endpointUrl, "POST");
            _currentRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
            _currentRequest.SetRequestHeader("Content-Type", "application/json");
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyString);
            _currentRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            _currentRequest.downloadHandler = new DownloadHandlerBuffer();

            // Initialize state
            GPTClient.status = GPTStatus.WaitingForResponse;
            _responseText = "";
            _processedEvents.Clear();
            
            // Send the request
            _currentRequest.SendWebRequest();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.update += CheckRequestProgress;
            #else
            yield return new WaitForSeconds(0.05f);
            while (!_currentRequest.isDone)
            {
                CheckRequestProgress();
                yield return new WaitForSeconds(0.05f);
            }
            // Ensure we process the final part of the response
            yield return new WaitForSeconds(0.2f);
            CheckRequestProgress();
            #endif

            yield return null;
        }

        private void CheckRequestProgress()
        {
            if (_currentRequest == null || string.IsNullOrEmpty(_currentRequest.downloadHandler.text))
                return;

            string currentContent = _currentRequest.downloadHandler.text;
            
            // Split by double newlines to separate events
            string[] eventChunks = currentContent.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var eventChunk in eventChunks)
            {
                if (_processedEvents.Contains(eventChunk))
                    continue;
                
                _processedEvents.Add(eventChunk);
                ProcessSemanticEvent(eventChunk);
            }

            // Check if request is complete
            if (_currentRequest.isDone)
            {
                if (_currentRequest.result == UnityWebRequest.Result.ConnectionError || 
                    _currentRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"OpenAI Reasoning request error: {_currentRequest.error}");
                    Debug.LogError($"Response text: {_currentRequest.downloadHandler.text}");
                    GPTClient.status = GPTStatus.Error;
                    OnResponseReceived?.Invoke("Error: " + _currentRequest.error, 0);
                }
                else
                {
                    GPTClient.status = GPTStatus.Success;
                    
                    // Send final response
                    if (!string.IsNullOrEmpty(_responseText))
                    {
                        OnResponseReceived?.Invoke(_responseText, 0);
                    }
                }
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= CheckRequestProgress;
                #endif
            }
        }

        private void ProcessSemanticEvent(string eventChunk)
        {
            string[] lines = eventChunk.Split('\n');
            string eventType = "";
            string data = "";
            
            foreach (var line in lines)
            {
                if (line.StartsWith("event:"))
                {
                    eventType = line.Substring(6).Trim();
                }
                else if (line.StartsWith("data:"))
                {
                    data = line.Substring(5).Trim();
                }
            }
            
            if (string.IsNullOrEmpty(data) || data == "[DONE]")
                return;
            
            try
            {
                JSONNode eventNode = JSON.Parse(data);
                string type = eventNode["type"];
                
                switch (type)
                {
                    case "response.created":
                        // Response started
                        _responseText = "";
                        break;
                        
                    case "response.output_text.delta":
                        HandleTextDelta(eventNode);
                        break;
                        
                    case "response.completed":
                        // Response finished
                        LogUsageInformation(eventNode);
                        break;
                        
                    case "response.failed":
                        string errorMessage = eventNode["error"]?["message"] ?? "Unknown error";
                        Debug.LogError($"Reasoning response failed: {errorMessage}");
                        GPTClient.status = GPTStatus.Error;
                        OnResponseReceived?.Invoke($"Error: {errorMessage}", 0);
                        break;
                        
                    case "error":
                        string err = eventNode["message"] ?? "Unknown error";
                        Debug.LogError($"Reasoning API error: {err}");
                        GPTClient.status = GPTStatus.Error;
                        OnResponseReceived?.Invoke($"API Error: {err}", 0);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing reasoning event: {ex.Message}. Event: {eventChunk}");
            }
        }
        
        private void HandleTextDelta(JSONNode eventNode)
        {
            string deltaText = eventNode["delta"]; 

            if (!string.IsNullOrEmpty(deltaText))
            {
                _responseText += deltaText;
                OnPartResponseReceived?.Invoke(deltaText);
            }
        }

        public override bool SupportsStreaming()
        {
            return true; // Reasoning models do support streaming via semantic events
        }

        private void LogUsageInformation(JSONNode eventNode)
        {
            // Usage information might be in the response.completed event
            JSONNode usageNode = eventNode["usage"];
            if (usageNode != null)
            {
                int inputTokens = usageNode["input_tokens"].AsInt;
                int outputTokens = usageNode["output_tokens"].AsInt;
                int totalTokens = usageNode["total_tokens"].AsInt;
                
                // Check for reasoning tokens in output details
                JSONNode outputDetails = usageNode["output_tokens_details"];
                if (outputDetails != null)
                {
                    int reasoningTokens = outputDetails["reasoning_tokens"].AsInt;
                    Debug.Log($"Token Usage - Input: {inputTokens}, Output: {outputTokens}, Reasoning: {reasoningTokens}, Total: {totalTokens}");
                }
                else
                {
                    Debug.Log($"Token Usage - Input: {inputTokens}, Output: {outputTokens}, Total: {totalTokens}");
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Enum for reasoning effort levels
    /// </summary>
    public enum ReasoningEffort
    {
        Low,    // Favors speed and economical token usage
        Medium, // Balance between speed and reasoning accuracy
        High    // Favors more complete reasoning
    }
}