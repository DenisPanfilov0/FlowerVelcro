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
    /// Client for interacting with OpenAI's API from Unity.
    /// </summary>
    public class OpenAIClient : AbstractAIClient
    {
        #region Static Properties
        private static UnityWebRequest _currentRequest;
        private static string[] _supportedModels = new string[] { 
            "chatgpt-4o-latest", "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo", 
            "gpt-4", "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano" 
        };
        public override string[] SupportedModels => _supportedModels;
        #endregion

        #region Private Fields
        private string _responseText = "";
        private string _oldContent = "";
        #endregion

        #region Constructor
        public OpenAIClient()
        {
            this.endpointUrl = "https://api.openai.com/v1/chat/completions";
            this.modelName = "gpt-4o";
        }
        #endregion

        #region Implementation Methods
        public override void SendRequest(string promptSend, int index = 0)
        {
            JSONNode requestBody = CreateBaseRequestBody();
            
            // Add system message
            JSONNode systemInitNode = JSON.Parse("{}");
            systemInitNode["role"] = "system";
            systemInitNode["content"] = SystemInitPrompt;
            requestBody["messages"].Add(systemInitNode);

            // Add user message
            JSONNode promptNode = JSON.Parse("{}");
            promptNode["role"] = "user";
            promptNode["content"] = promptSend;
            requestBody["messages"].Add(promptNode);

            string requestBodyString = requestBody.ToString();
            CoroutineHelper.StartCor(SendRequestCoroutine(requestBodyString, index));
        }

        public override void SendRequestWithHistory(List<GPTChatMessage> chatMessages)
        {
            JSONNode requestBody = CreateBaseRequestBody();

            // Add all chat messages
            foreach (GPTChatMessage chatMessage in chatMessages)
            {
                if (string.IsNullOrEmpty(chatMessage.content))
                    continue;
                    
                JSONNode messageNode = JSON.Parse("{}");
                messageNode["role"] = chatMessage.role;
                messageNode["content"] = chatMessage.content;
                requestBody["messages"].Add(messageNode);
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
            JSONNode messagesArray = new JSONArray();
            
            foreach (GPTChatMessage message in messages)
            {
                if (string.IsNullOrEmpty(message.content))
                    continue;
                    
                JSONNode messageNode = JSON.Parse("{}");
                messageNode["role"] = message.role;
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
            requestBody["messages"] = new JSONArray();
            requestBody["temperature"] = temperature;
            // Only set max_tokens if it's a positive value
            if (maxTokens > 0)
            {
                requestBody["max_tokens"] = maxTokens;
            }
            requestBody["n"] = n;
            requestBody["stream"] = true;
            return requestBody;
        }

        private IEnumerator SendRequestCoroutine(string requestBodyString, int index = 0)
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
            _oldContent = "";
            _responseText = "";
            
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
            List<string> allLines = new List<string>(currentContent.Split('\n'));
            allLines.RemoveAll(string.IsNullOrEmpty);

            if (allLines.Count == 0) 
                return;

            StringBuilder newTextBuilder = new StringBuilder();
            
            foreach (var line in allLines)
            {
                string cleanedLine = line.Trim();
                // Skip the [DONE] marker which indicates end of stream
                if (cleanedLine == "data: [DONE]")
                    continue;
                
                // Remove the "data: " prefix and prepare JSON
                if (cleanedLine.StartsWith("data:"))
                {
                    cleanedLine = cleanedLine.Substring(5).Trim();
                }
                
                // Skip if empty after cleaning
                if (string.IsNullOrEmpty(cleanedLine))
                    continue;
                
                // Fix "object" keyword conflict in JSON
                cleanedLine = cleanedLine.Replace("\"object\"", "\"objectName\"");

                try
                {
                    JSONNode jsonNode = JSON.Parse(cleanedLine);
                    GPTResponseStream response = ParseResponseStream(jsonNode);

                    if (response.choices != null && response.choices.Length > 0)
                    {
                        string content = response.choices[0].delta.content;
                        if (!string.IsNullOrEmpty(content))
                        { 
                            newTextBuilder.Append(content);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing response: {ex.Message}. Line: {cleanedLine}");

                    //print all lines
                    foreach (var l in allLines)
                    {
                        // Debug.Log(l);
                    }
                }
            }

            string newText = newTextBuilder.ToString();
            
            // Calculate only the new content that hasn't been processed yet
            string newContent = !string.IsNullOrEmpty(_oldContent) 
                ? newText.Substring(_oldContent.Length) 
                : newText;

            if (!string.IsNullOrEmpty(newContent))
            {
                _responseText += newContent;
                OnPartResponseReceived?.Invoke(newContent);
            }
            
            _oldContent = newText;

            // Check if request is complete
            if (_currentRequest.isDone)
            {
                if (_currentRequest.result == UnityWebRequest.Result.ConnectionError || 
                    _currentRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Request error: {_currentRequest.error}");
                    GPTClient.status = GPTStatus.Error;
                }
                else
                {
                    GPTClient.status = GPTStatus.Success;
                    
                    try {
                        string fullContent = _currentRequest.downloadHandler.text;
                        if (!string.IsNullOrEmpty(fullContent)) {
                            ProcessFullResponse(fullContent);
                        }
                    }
                    catch (System.Exception ex) {
                        Debug.LogError("Error processing full response: " + ex.Message);
                    }
                }

                OnResponseReceived?.Invoke(_responseText, 0);
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= CheckRequestProgress;
                #endif
            }
        }

        private void ProcessFullResponse(string fullContent)
        {
            List<string> allLines = new List<string>(fullContent.Split('\n'));
            allLines.RemoveAll(string.IsNullOrEmpty);
            
            // Rebuild the full response text to ensure we didn't miss any chunks
            StringBuilder fullResponseBuilder = new StringBuilder();
            bool foundContent = false;
            
            foreach (var line in allLines)
            {
                string cleanedLine = line.Trim();
                // Skip the [DONE] marker
                if (cleanedLine == "data: [DONE]")
                    continue;
                
                // Remove the "data: " prefix and prepare JSON
                if (cleanedLine.StartsWith("data:"))
                {
                    cleanedLine = cleanedLine.Substring(5).Trim();
                }
                
                // Skip if empty after cleaning
                if (string.IsNullOrEmpty(cleanedLine))
                    continue;
                
                // Fix "object" keyword conflict in JSON
                cleanedLine = cleanedLine.Replace("\"object\"", "\"objectName\"");

                try
                {
                    JSONNode jsonNode = JSON.Parse(cleanedLine);
                    GPTResponseStream response = ParseResponseStream(jsonNode);

                    if (response.choices != null && response.choices.Length > 0)
                    {
                        string content = response.choices[0].delta.content;
                        if (!string.IsNullOrEmpty(content))
                        {
                            fullResponseBuilder.Append(content);
                            foundContent = true;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    // Skip invalid JSON chunks
                    Debug.LogWarning($"Skipping invalid JSON in full response processing: {ex.Message}");
                }
            }
            
            // If we found new content that wasn't in our original response
            if (foundContent)
            {
                string fullResponseText = fullResponseBuilder.ToString();
                if (fullResponseText.Length > _responseText.Length)
                {
                    _responseText = fullResponseText;
                }
            }
        }
        
        private GPTResponseStream ParseResponseStream(JSONNode jsonNode)
        {
            GPTResponseStream response = new GPTResponseStream
            {
                id = jsonNode["id"],
                objectName = jsonNode["objectName"],
                created = jsonNode["created"],
                model = jsonNode["model"]
            };

            if (jsonNode["choices"] == null)
            {
                return response;
            }

            JSONArray jsonChoices = jsonNode["choices"].AsArray;
            response.choices = new GPTResponseStream.Choice[jsonChoices.Count];

            for (int i = 0; i < jsonChoices.Count; i++)
            {
                JSONNode choiceNode = jsonChoices[i];
                JSONNode deltaNode = choiceNode["delta"];

                response.choices[i] = new GPTResponseStream.Choice
                {
                    index = choiceNode["index"],
                    delta = new GPTResponseStream.Delta
                    {
                        role = deltaNode["role"],
                        content = deltaNode["content"]
                    }
                };
            }

            return response;
        }
        #endregion
    }
}