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
    /// Client for interacting with AI APIs from Unity.
    /// Maintains backwards compatibility with existing tools while supporting multiple AI providers.
    /// </summary>
    public class GPTClient
    {
        #region Singleton Implementation
        private static GPTClient _instance;
        public static GPTClient Instance 
        {
            get 
            {
                if (_instance == null)
                {
                    _instance = new GPTClient();
                }
                
                #if UNITY_EDITOR
                _instance.LoadSavedConfiguration();
                #endif
                
                return _instance;
            }
        }
        #endregion

        #region Public Properties
        public string apiKey = "";
        public string response = "";
        public string SystemInitPrompt = "You are a professional programmer. You always answer in " + GPTClient.language;
        public Action<string, int> OnResponseReceived;
        public Action<string> OnPartResponseReceived;
        
        /// <summary>
        /// Public accessor for the current AI client
        /// </summary>
        public AbstractAIClient _aiClient { get; private set; }
        #endregion

        #region Static Properties
        // These arrays will be dynamically filled based on current provider
        public static string[] models = new string[] { "none"  };
        public static Dictionary<string, float> modelCosts = new Dictionary<string, float>();
        public static int modelIndex = 0;
        public static bool showSettings = false;
        public static string apiEndpoint = "";
        public static string language = "english";
        public static string defaultOpenAIURL = "https://api.openai.com/v1/chat/completions";
        public static GPTStatus status = GPTStatus.Idle;
        public static float temperature = 0.7f;
        public static int maxTokens = 3000;
        public static string defaultSavePath = "Assets/";
        public static bool askForSavePath = true;
        public static int n = 1;
        public static float cost = 0f;
        public static AIProvider currentProvider = AIProvider.OpenAI;
        
        // API Keys for each provider
        public static Dictionary<AIProvider, string> providerApiKeys = new Dictionary<AIProvider, string>()
        {
            { AIProvider.OpenAI, "" },
            { AIProvider.Anthropic, "" },
            { AIProvider.Google, "" },
            { AIProvider.DeepSeek, "" },
            { AIProvider.Ollama, "" },
            { AIProvider.OpenAIReasoning, "" },
            { AIProvider.Custom, "" }
        };
        
        // Endpoints for each provider (initialized with defaults but can be customized)
        public static Dictionary<AIProvider, string> providerEndpoints = new Dictionary<AIProvider, string>(); 
        #endregion

        #region Private Fields
        private string _model = "gpt-4o";
        
        //getter setter
        public string model
        {
            get { return _model; }
            set { _model = value; }
        }
        #endregion

        /// <summary>
        /// Constructor for GPTClient.
        /// </summary>
        public GPTClient()
        {
            _instance = this;
            _aiClient = AIClientFactory.CreateClient(currentProvider);
        }
        
        /// <summary>
        /// Updates the AI client when settings change.
        /// </summary>
        public void UpdateAIClient()
        {
            // Create new client of the current type
            _aiClient = AIClientFactory.CreateClient(currentProvider);
            
            // Configure client
            _aiClient.apiKey = apiKey;
            _aiClient.SystemInitPrompt = SystemInitPrompt;
            _aiClient.temperature = temperature;
            _aiClient.maxTokens = maxTokens;
            _aiClient.endpointUrl = apiEndpoint;
            _aiClient.modelName = _model; 
            
            // Hook up events
            _aiClient.OnResponseReceived = null;
            _aiClient.OnPartResponseReceived = null;
            
            _aiClient.OnResponseReceived = (response, index) => {
                this.response = response;
                this.OnResponseReceived?.Invoke(response, index);
            };
            
            _aiClient.OnPartResponseReceived = (partResponse) => {
                this.OnPartResponseReceived?.Invoke(partResponse);
            };
            
            // Update models list based on provider
            models = AIClientFactory.GetModelsForProvider(currentProvider);
            
            // Apply reasoning-specific settings if applicable
            ApplyReasoningSettings();
            
            // // Reset model index if out of range
            // if (modelIndex >= models.Length)
            // {
            //     modelIndex = 0;
            // }
            
            // // Set model
            // _model = models[modelIndex];
            // _aiClient.modelName = _model;
        }

        /// <summary>
        /// Apply reasoning-specific settings to the client if it's a reasoning client
        /// </summary>
        private void ApplyReasoningSettings()
        {
            if (currentProvider == AIProvider.OpenAIReasoning && _aiClient is OpenAIClientReasoning reasoningClient)
            {
                #if UNITY_EDITOR
                // Load reasoning settings from EditorPrefs
                int reasoningEffortIndex = UnityEditor.EditorPrefs.GetInt("UAIReasoningEffort", 1);
                bool includeSummary = UnityEditor.EditorPrefs.GetBool("UAIIncludeSummary", false);
                
                reasoningClient.ReasoningEffort = (ReasoningEffort)reasoningEffortIndex;
                reasoningClient.IncludeSummary = includeSummary;
                #endif
            }
        }

        #region Public Methods
        /// <summary>
        /// Send a prompt to the AI API.
        /// </summary>
        /// <param name="promptSend">The prompt to send.</param>
        /// <param name="index">Optional index parameter.</param>
        public void SendRequest(string promptSend, int index = 0)
        {
            // Ensure client is configured
            UpdateClientConfiguration();
            
            // Delegate to the current AI client
            _aiClient.SendRequest(promptSend, index);
        }

        /// <summary>
        /// Send a request with a full chat history.
        /// </summary>
        /// <param name="chatMessages">List of chat messages for the conversation history.</param>
        public void SendRequestWithHistory(List<GPTChatMessage> chatMessages)
        {
            // Ensure client is configured
            UpdateClientConfiguration();
            
            // Set the current model
            _model = models[modelIndex];
            _aiClient.modelName = _model;
            
            // Delegate to the current AI client
            _aiClient.SendRequestWithHistory(chatMessages);
        }

        /// <summary>
        /// Stop the current request.
        /// </summary>
        public static void StopGeneration()
        {
            if (Instance._aiClient != null)
            {
                Instance._aiClient.StopGeneration();
                status = GPTStatus.Idle;
            }
        }
        
        /// <summary>
        /// Change the AI provider and refresh models and settings.
        /// </summary>
        /// <param name="provider">The new AI provider.</param>
        public void ChangeProvider(AIProvider provider)
        {
            currentProvider = provider;
            
            // Update default endpoint and model based on provider
            apiEndpoint = AIClientFactory.GetDefaultEndpoint(provider);
            _model = AIClientFactory.GetDefaultModel(provider);
            
            // Update models list
            models = AIClientFactory.GetModelsForProvider(provider);
            
            // Reset model index
            modelIndex = 0;
            
            // Update the AI client
            UpdateAIClient();
            
            // Save configuration
            #if UNITY_EDITOR
            SaveConfiguration();
            #endif
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Update the AI client configuration before sending requests.
        /// </summary>
        private void UpdateClientConfiguration()
        {
            _aiClient.apiKey = apiKey; 
            _aiClient.SystemInitPrompt = SystemInitPrompt;
            _aiClient.temperature = temperature;
            _aiClient.maxTokens = maxTokens;
            _aiClient.modelName = _model;
            _aiClient.endpointUrl = apiEndpoint;
            
            // Apply reasoning settings if applicable
            ApplyReasoningSettings();
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Load saved configuration from EditorPrefs.
        /// </summary>
        private void LoadSavedConfiguration()
        {
            // Load provider
            int providerIndex = UnityEditor.EditorPrefs.GetInt("UAIProvider", 0);
            currentProvider = (AIProvider)providerIndex;
            
            apiKey = UnityEditor.EditorPrefs.GetString("UAISecretKey_" + currentProvider.ToString(), "");
            
            // Load endpoint based on provider
            apiEndpoint = UnityEditor.EditorPrefs.GetString("UAIEndpoint", AIClientFactory.GetDefaultEndpoint(currentProvider));
            
            // Load model name (we'll handle model index differently)
            _model = UnityEditor.EditorPrefs.GetString("GPTModel", AIClientFactory.GetDefaultModel(currentProvider));
            
            // For Ollama specifically, we need special handling since models are dynamic
            if (currentProvider == AIProvider.Ollama)
            {
                // Get the default models first
                models = AIClientFactory.GetModelsForProvider(currentProvider);
                
                // Check if saved model exists in the default models
                bool modelFound = false;
                for (int i = 0; i < models.Length; i++)
                {
                    if (models[i] == _model)
                    {
                        modelIndex = i;
                        modelFound = true;
                        break;
                    }
                }
                
                // If model not found in defaults, add it to OllamaClient's available models
                if (!modelFound)
                {
                    // Add the saved model to the available models list
                    OllamaClient.AddCustomModel(_model);
                    
                    // Refresh models list
                    models = AIClientFactory.GetModelsForProvider(currentProvider);
                    
                    // Find the index after adding
                    for (int i = 0; i < models.Length; i++)
                    {
                        if (models[i] == _model)
                        {
                            modelIndex = i;
                            break;
                        }
                    } 
                }
            }
            else
            {
                // For other providers, load models normally
                models = AIClientFactory.GetModelsForProvider(currentProvider);
                modelIndex = UnityEditor.EditorPrefs.GetInt("GPTModelIndex", 0);
                
                // Ensure model index is valid
                if (modelIndex >= models.Length)
                {
                    modelIndex = 0;
                    _model = models[0];
                }
            }
            
            // Load other settings
            temperature = UnityEditor.EditorPrefs.GetFloat("GPTTemperature", 0.7f);
            maxTokens = UnityEditor.EditorPrefs.GetInt("GPTMaxTokens", 3000);
            n = 1; // Fixed to 1
            defaultSavePath = UnityEditor.EditorPrefs.GetString("GPTDefaultSavePath", "Assets/");
            askForSavePath = UnityEditor.EditorPrefs.GetBool("GPTAskForSavePath", false);
            
            // Update the AI client
            UpdateAIClient();  
        }
        
        /// <summary>
        /// Save configuration to EditorPrefs.
        /// </summary>
        private void SaveConfiguration()
        {
            UnityEditor.EditorPrefs.SetInt("UAIProvider", (int)currentProvider);

            UnityEditor.EditorPrefs.SetString("UAISecretKey", apiKey);
            UnityEditor.EditorPrefs.SetString("UAIEndpoint", apiEndpoint);
            UnityEditor.EditorPrefs.SetString("GPTModel", _model);
            UnityEditor.EditorPrefs.SetInt("GPTModelIndex", modelIndex);
            UnityEditor.EditorPrefs.SetFloat("GPTTemperature", temperature);
            UnityEditor.EditorPrefs.SetInt("GPTMaxTokens", maxTokens);
            UnityEditor.EditorPrefs.SetString("GPTDefaultSavePath", defaultSavePath);
            UnityEditor.EditorPrefs.SetBool("GPTAskForSavePath", askForSavePath);
        }
        #endif
        #endregion
    }

    /// <summary>
    /// Enum representing the status of an AI request.
    /// </summary>
    public enum GPTStatus
    {
        Idle,
        WaitingForResponse,
        Success,
        Error
    }
}