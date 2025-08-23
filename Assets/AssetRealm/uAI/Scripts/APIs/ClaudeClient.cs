using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace UAI
{
    public class ClaudeClient : AbstractAIClient
    {
        #region Static Properties
        private static UnityWebRequest _currentRequest;
        private static string[] _supportedModels = new string[] { 
            "claude-opus-4-1-20250805",
            "claude-opus-4-20250514",
            "claude-sonnet-4-20250514",
            "claude-3-7-sonnet-20250219",
            "claude-3-5-haiku-20241022", 
            "claude-3-opus-20240229", 
            "claude-3-5-sonnet-20241022",
            "claude-3-5-sonnet-20240620" 
        };
        public override string[] SupportedModels => _supportedModels;
        #endregion

        #region Private Fields
        private string _responseText = "";
        private Dictionary<int, StringBuilder> _contentBlocks = new Dictionary<int, StringBuilder>();
        private string _anthropicVersion = "2023-06-01"; 
        private List<string> _processedEvents = new List<string>(); 
        #endregion

        #region Constructor
        public ClaudeClient()
        {
            this.endpointUrl = "https://api.anthropic.com/v1/messages";
            this.modelName = "claude-sonnet-4-20250514";
        }
        #endregion

        #region Implementation Methods
        public override void SendRequest(string promptSend, int index = 0)
        {
            List<GPTChatMessage> messages = new List<GPTChatMessage>();
            
            // Add system message
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
            
            JSONArray messagesArray = new JSONArray();
            
            foreach (GPTChatMessage message in chatMessages)
            {
                if (string.IsNullOrEmpty(message.content))
                    continue;
                
                if (message.role == "system")
                    continue;
                
                string role = message.role;
                
                if (role == "assistant")
                    role = "assistant";
                else if (role == "user")
                    role = "user";
                
                JSONNode messageNode = JSON.Parse("{}");
                messageNode["role"] = role;
                messageNode["content"] = message.content;
                messagesArray.Add(messageNode);
            }
            
            requestBody["messages"] = messagesArray;
            
            string systemMessage = "";
            foreach (GPTChatMessage message in chatMessages)
            {
                if (message.role == "system")
                {
                    systemMessage = message.content;
                    break;
                }
            }
            
            if (!string.IsNullOrEmpty(systemMessage))
            {
                requestBody["system"] = systemMessage;
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
            JSONArray messagesArray = new JSONArray();
            
            foreach (GPTChatMessage message in messages)
            {
                if (string.IsNullOrEmpty(message.content) || message.role == "system")
                    continue;
                
                string role = message.role;
                
                // Claude uses "assistant" instead of "assistant" role
                if (role == "assistant")
                    role = "assistant";
                else if (role == "user")
                    role = "user";
                
                JSONNode messageNode = JSON.Parse("{}");
                messageNode["role"] = role;
                messageNode["content"] = message.content;
                messagesArray.Add(messageNode);
            }
            
            return messagesArray;
        }
        #endregion

        #region Private Methods
        private JSONNode CreateBaseRequestBody()
        {
            JSONNode requestBody = JSON.Parse("{}");
            requestBody["model"] = modelName;
            requestBody["temperature"] = temperature;
            requestBody["max_tokens"] = maxTokens > 0 ? maxTokens : 4096;
            requestBody["stream"] = true;
            return requestBody;
        }

        private IEnumerator SendRequestCoroutine(string requestBodyString)
        {
            // Create and configure the web request
            _currentRequest = new UnityWebRequest(endpointUrl, "POST");
            _currentRequest.SetRequestHeader("x-api-key", apiKey);
            _currentRequest.SetRequestHeader("anthropic-version", _anthropicVersion);
            _currentRequest.SetRequestHeader("Content-Type", "application/json");
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyString);
            _currentRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            _currentRequest.downloadHandler = new DownloadHandlerBuffer();

            // Initialize state
            GPTClient.status = GPTStatus.WaitingForResponse;
            _responseText = "";
            _contentBlocks.Clear();
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
            
            string[] eventChunks = currentContent.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var eventChunk in eventChunks)
            {
                if (_processedEvents.Contains(eventChunk))
                    continue;
                
                _processedEvents.Add(eventChunk);
                ProcessEvent(eventChunk);
            }

            if (_currentRequest.isDone)
            {
                if (_currentRequest.result == UnityWebRequest.Result.ConnectionError || 
                    _currentRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Claude request error: {_currentRequest.error}");
                    GPTClient.status = GPTStatus.Error;
                }
                else
                {
                    GPTClient.status = GPTStatus.Success;
                }

                if (!string.IsNullOrEmpty(_responseText))
                {
                    OnResponseReceived?.Invoke(_responseText, 0);
                }
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= CheckRequestProgress;
                #endif
            }
        }
        
        private void ProcessEvent(string eventChunk)
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
            
            if (eventType == "ping" || string.IsNullOrEmpty(data))
                return;
            
            // try
            // {
                JSONNode jsonNode = JSON.Parse(data);
                string type = jsonNode["type"];
                
                switch (type)
                {
                    case "message_start":
                        _responseText = "";
                        _contentBlocks.Clear();
                        break;
                        
                    case "content_block_start":
                        int blockIndex = jsonNode["index"].AsInt;
                        
                        if (!_contentBlocks.ContainsKey(blockIndex))
                        {
                            _contentBlocks[blockIndex] = new StringBuilder();
                        }
                        break;
                        
                    case "content_block_delta":
                        HandleContentBlockDelta(jsonNode);
                        break;
                        
                    case "content_block_stop":
                        break;
                        
                    case "message_stop":
                        SendFinalResponseIfNeeded();
                        break;
                }
            // }
            // catch (Exception ex)
            // {
            //     // Debug.LogError($"Error parsing Claude event: {ex.Message}. Event: {eventChunk}");
            // }
        }
        
        private void HandleContentBlockDelta(JSONNode jsonNode)
        {
            int blockIndex = jsonNode["index"].AsInt;
            JSONNode deltaNode = jsonNode["delta"];
            string deltaType = deltaNode["type"];
            
            if (!_contentBlocks.ContainsKey(blockIndex))
            {
                _contentBlocks[blockIndex] = new StringBuilder();
            }
            
            if (deltaType == "text_delta")
            {
                string newText = deltaNode["text"];
                
                if (!string.IsNullOrEmpty(newText))
                {
                    string previousBlockContent = _contentBlocks[blockIndex].ToString();
                    
                    _contentBlocks[blockIndex].Append(newText);
                    UpdateResponseText();
                    OnPartResponseReceived?.Invoke(newText);
                }
            }
        }
        
        private void UpdateResponseText()
        {
            StringBuilder fullResponse = new StringBuilder();
            var keys = new List<int>(_contentBlocks.Keys);
            keys.Sort();
            
            foreach (int key in keys)
            {
                fullResponse.Append(_contentBlocks[key]);
            }
            
            _responseText = fullResponse.ToString();
        }
        
        private void SendFinalResponseIfNeeded()
        {
            if (!string.IsNullOrEmpty(_responseText))
            {
                OnResponseReceived?.Invoke(_responseText, 0);
            }
        }
        #endregion
    }
}