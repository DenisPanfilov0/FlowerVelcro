using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace UAI
{
    public class OllamaClient : AbstractAIClient
    {
        #region Static Properties
        private static UnityWebRequest _currentRequest;
        private static string[] _defaultModels = new string[] { 
            "gemma3", "llama3", "llama3:latest", "llama3.2", "llama2", "mistral", "codellama",
            "phi3", "vicuna", "orca-mini", "neural-chat", "stable-code"
        };
        private static List<string> _availableModels = new List<string>(_defaultModels);
        public override string[] SupportedModels => _availableModels.ToArray();
        #endregion

        #region Private Fields
        private string _responseText = "";
        private StringBuilder _fullResponseBuilder = new StringBuilder();
        private bool _responseComplete = false;
        private List<string> _processedObjects = new List<string>();
        #endregion

        #region Constructor
        public OllamaClient()
        {
            this.endpointUrl = "http://localhost:11434/api/chat";
            this.modelName = "gemma3"; // Default model
        }
        #endregion

        #region Public Methods
        
        public static void FetchAvailableModels(Action<string[], bool> callback)
        {
            // Start a coroutine using our helper
            CoroutineHelper.StartCor(FetchModelsCoroutine(callback));
        }
        
        private static IEnumerator FetchModelsCoroutine(Action<string[], bool> callback)
        {
            string baseUrl = "http://localhost:11434"; 
            string apiEndpoint = baseUrl + "/api/tags";
            
            #if UNITY_EDITOR
            string savedEndpoint = UnityEditor.EditorPrefs.GetString("UAIEndpoint", "");
            if (!string.IsNullOrEmpty(savedEndpoint) && savedEndpoint.Contains("11434"))
            {
                Uri uri = new Uri(savedEndpoint);
                baseUrl = $"{uri.Scheme}://{uri.Authority}";
                apiEndpoint = baseUrl + "/api/tags";
            }
            #endif
            
            using (UnityWebRequest request = UnityWebRequest.Get(apiEndpoint))
            {
                // Send the request
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.ConnectionError || 
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error fetching Ollama models: {request.error}");
                    // If there's an error, return the default models
                    _availableModels = new List<string>(_defaultModels);
                    callback?.Invoke(_availableModels.ToArray(), false);
                }
                else
                {
                    try
                    {
                        string responseText = request.downloadHandler.text;
                        JSONNode jsonResponse = JSON.Parse(responseText);
                        
                        if (jsonResponse != null && jsonResponse["models"] != null)
                        {
                            List<string> modelNames = new List<string>();
                            JSONArray modelsArray = jsonResponse["models"].AsArray;
                            
                            for (int i = 0; i < modelsArray.Count; i++)
                            {
                                string modelName = modelsArray[i]["name"];
                                if (!string.IsNullOrEmpty(modelName))
                                {
                                    modelNames.Add(modelName);
                                }
                            }
                            
                            if (modelNames.Count > 0)
                            {
                                // Sort the model names alphabetically
                                modelNames.Sort();
                                _availableModels = modelNames;
                                callback?.Invoke(_availableModels.ToArray(), true);
                            }
                            else
                            {
                                Debug.LogWarning("No models found in Ollama response.");
                                _availableModels = new List<string>(_defaultModels);
                                callback?.Invoke(_availableModels.ToArray(), false);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Invalid JSON response from Ollama API.");
                            _availableModels = new List<string>(_defaultModels);
                            callback?.Invoke(_availableModels.ToArray(), false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error parsing Ollama models response: {ex.Message}");
                        _availableModels = new List<string>(_defaultModels);
                        callback?.Invoke(_availableModels.ToArray(), false);
                    }
                }
            }
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
            
            // Add all messages
            JSONArray messagesArray = new JSONArray();
            foreach (GPTChatMessage message in chatMessages)
            {
                if (string.IsNullOrEmpty(message.content))
                    continue;
                
                JSONNode messageNode = JSON.Parse("{}");
                messageNode["role"] = message.role;
                messageNode["content"] = message.content;
                messagesArray.Add(messageNode);
            }
            
            requestBody["messages"] = messagesArray;
            
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
            
            requestBody["stream"] = true;
            
            JSONNode optionsNode = JSON.Parse("{}");
            
            optionsNode["temperature"] = temperature;
            
            if (maxTokens > 0)
            {
                optionsNode["num_predict"] = maxTokens;
            }
            else
            {
                // Set a reasonable default if not specified
                optionsNode["num_predict"] = 8182;
            }
            
            optionsNode["num_ctx"] = 16384;
            
            requestBody["keep_alive"] = "10m";
            
            requestBody["options"] = optionsNode;
            
            return requestBody;
        }

        public static bool AddCustomModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return false;
                
            if (_availableModels.Contains(modelName))
                return false;
                
            _availableModels.Add(modelName);
            
            _availableModels.Sort();
            
            return true;
        }
        private IEnumerator SendRequestCoroutine(string requestBodyString)
        {
            // Create and configure the web request
            _currentRequest = new UnityWebRequest(endpointUrl, "POST");
            _currentRequest.SetRequestHeader("Content-Type", "application/json");
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyString);
            _currentRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            _currentRequest.downloadHandler = new DownloadHandlerBuffer();

            GPTClient.status = GPTStatus.WaitingForResponse;
            _responseText = "";
            _fullResponseBuilder.Clear();
            _responseComplete = false;
            _processedObjects.Clear();
            
            // Send the request
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
            // Ensure we process the final part of the response
            float finalProcessingDelay = 0.2f;
            yield return new WaitForSeconds(finalProcessingDelay);
            CheckRequestProgress();
            #endif

            
            yield return null;
        }
        
        private void ProcessFullResponse()
        {
            if (_currentRequest == null || string.IsNullOrEmpty(_currentRequest.downloadHandler.text))
                return;
                
            string fullContent = _currentRequest.downloadHandler.text;
            
            try
            {
                // Try to extract the last message which should contain the complete response
                string[] jsonLines = fullContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string lastCompleteMessage = "";
                
                for (int i = jsonLines.Length - 1; i >= 0; i--)
                {
                    string line = jsonLines[i];
                    if (string.IsNullOrEmpty(line))
                        continue;
                        
                    try
                    {
                        JSONNode jsonNode = JSON.Parse(line);
                        
                        // Skip "done" messages without content
                        if (jsonNode["done"].AsBool && (!jsonNode.HasKey("message") || jsonNode["message"] == null))
                            continue;
                        
                        JSONNode messageNode = jsonNode["message"];
                        if (messageNode != null && messageNode["role"] == "assistant")
                        {
                            string content = messageNode["content"];
                            if (!string.IsNullOrEmpty(content))
                            {
                                lastCompleteMessage = content;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid JSON lines
                        continue;
                    }
                }
                
                if (!string.IsNullOrEmpty(lastCompleteMessage) && lastCompleteMessage.Length > _responseText.Length)
                {
                    // Debug.Log($"Found more complete response in final processing: {lastCompleteMessage.Length} chars vs current {_responseText.Length} chars");
                    _responseText = lastCompleteMessage;
                    OnResponseReceived?.Invoke(_responseText, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in final Ollama response processing: {ex.Message}");
            }
        }

        private void CheckRequestProgress()
        {
            if (_currentRequest == null || string.IsNullOrEmpty(_currentRequest.downloadHandler.text))
                return;

            string currentContent = _currentRequest.downloadHandler.text;
            
            string[] jsonLines = currentContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder newTextBuilder = new StringBuilder();
            
            foreach (string jsonLine in jsonLines)
            {
                if (string.IsNullOrEmpty(jsonLine))
                    continue;
                
                // Skip already processed objects
                if (_processedObjects.Contains(jsonLine))
                    continue;
                
                // Add to processed objects to avoid duplicates
                _processedObjects.Add(jsonLine);
                
                try
                {
                    JSONNode jsonNode = JSON.Parse(jsonLine);
                    
                    // Check if this is the final response
                    bool isDone = jsonNode["done"].AsBool;
                    
                    if (isDone)
                    {
                        _responseComplete = true;
                    }
                    
                    // Get message content
                    JSONNode messageNode = jsonNode["message"];
                    if (messageNode != null && messageNode["role"] == "assistant")
                    {
                        string content = messageNode["content"];
                        if (!string.IsNullOrEmpty(content))
                        {
                            // Ollama sends individual tokens as separate JSON objects
                            // Just append each token to build the full response
                            newTextBuilder.Append(content);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing Ollama response: {ex.Message}. Line: {jsonLine}");
                }
            }
            
            string newText = newTextBuilder.ToString();
            
            if (!string.IsNullOrEmpty(newText))
            {
                // Add to the full response
                _fullResponseBuilder.Append(newText);
                _responseText = _fullResponseBuilder.ToString();
                
                // Send new content via streaming callback
                OnPartResponseReceived?.Invoke(newText);
            }

            // Check if request is complete
            if (_currentRequest.isDone || _responseComplete)
            {
                if (_currentRequest.result == UnityWebRequest.Result.ConnectionError || 
                    _currentRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Ollama request error: {_currentRequest.error}");
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