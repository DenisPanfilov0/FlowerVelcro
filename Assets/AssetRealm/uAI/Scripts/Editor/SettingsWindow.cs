using UnityEditor;
using UnityEngine;

namespace UAI{ 
    public class SettingsWindow : EditorWindow
    {
        private static string _secretKey = ""; 
        private static int _selectedModelIndex = 0;
        private static int _selectedProviderIndex = 5;
        private static string[] _providerNames = {"OpenAI", "Claude(Anthropic)", "Gemini(Google)", "DeepSeek", "Ollama", "OpenAI Reasoning", "Custom"};
        
        public static float _temperature = 0.7f;
        public static int _maxTokens = 0;
        public static int _n = 1;

        public static string _apiEndpoint = "";
        public static string _aiLanguage = "english";
        private static int _selectedLanguageIndex = 0;
        public static string[] languages = {"english", "german", "french", "spanish", "italian", "portuguese", "russian", "chinese", "japanese", "korean", "custom"};

        public static string _defaultSavePath = "Assets/";
        public static bool _askForSavePath = true;

        public static string[] _models = new string[0];
        
        // Reasoning-specific settings
        private static int _reasoningEffortIndex = 1; // Default to Medium
        private static bool _includeSummary = false;
        private static string[] _reasoningEffortNames = {"Low", "Medium", "High"};
        
        // For displaying help info about custom providers
        private bool _showCustomProviderHelp = false;
        
        // For handling model loading
        private bool _isLoadingModels = false;
        private string _modelLoadingStatus = "";
        private bool _modelLoadingSuccess = true;

        [MenuItem("Tools/uAI Creator/Settings", false, 1000)]
        public static void ShowWindow()
        { 
            SettingsWindow window = GetWindow<SettingsWindow>("AI Creator Settings"); 
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable() { 
            LoadAPIKey();

            _temperature = EditorPrefs.GetFloat("GPTTemperature", 0.7f);
            _apiEndpoint = EditorPrefs.GetString("UAIEndpoint", AIClientFactory.GetDefaultEndpoint((AIProvider)_selectedProviderIndex));
            _aiLanguage = EditorPrefs.GetString("UAILanguage", "english");
            _maxTokens = EditorPrefs.GetInt("GPTMaxTokens", 0);
            _n = EditorPrefs.GetInt("GPTN", 1);
            _defaultSavePath = EditorPrefs.GetString("GPTDefaultSavePath", "Assets/");
            _askForSavePath = EditorPrefs.GetBool("GPTAskForSavePath", false);

            // Load reasoning settings
            _reasoningEffortIndex = EditorPrefs.GetInt("UAIReasoningEffort", 1);
            _includeSummary = EditorPrefs.GetBool("UAIIncludeSummary", false);

            GPTClient.temperature = _temperature;
            GPTClient.maxTokens = _maxTokens;
            GPTClient.n = _n;
            GPTClient.defaultSavePath = _defaultSavePath;
            GPTClient.askForSavePath = _askForSavePath; 
            GPTClient.apiEndpoint = _apiEndpoint;
            GPTClient.language = _aiLanguage;
            
            // For Ollama, try to fetch the available models
            if ((AIProvider)_selectedProviderIndex == AIProvider.Ollama)
            {
                FetchOllamaModels();
            }
        }

        public static string LoadAPIKey()  
        {
            // Load provider selection
            _selectedProviderIndex = EditorPrefs.GetInt("UAIProvider", 0);
            AIProvider selectedProvider = (AIProvider)_selectedProviderIndex;
            GPTClient.currentProvider = selectedProvider;
            
            // EditorPrefs.SetString("UAISecretKey_" + GPTClient.Instance.model, _secretKey);
            _secretKey = EditorPrefs.GetString("UAISecretKey_" + selectedProvider, "");
            
            // Update the appropriate key in the GPTClient
            GPTClient.providerApiKeys[selectedProvider] = _secretKey;
            GPTClient.Instance.apiKey = _secretKey;
            // Debug.Log("GPTClient.Instance.apiKey = " + GPTClient.Instance.apiKey);
            

            // Load model based on provider
            string modelKey = $"UAI_{selectedProvider}_Model";
            GPTClient.Instance.model = EditorPrefs.GetString(modelKey, AIClientFactory.GetDefaultModel(selectedProvider));

                
            // If no model is saved yet for this provider, use the default model
            if (string.IsNullOrEmpty(GPTClient.Instance.model))
            {
                GPTClient.Instance.model = AIClientFactory.GetDefaultModel(selectedProvider);
            }

            _models = AIClientFactory.GetModelsForProvider(selectedProvider);
            
            // Find model index
            GPTClient.modelIndex = -1;
            for (int i = 0; i < GPTClient.models.Length; i++)
            {
                if (GPTClient.models[i] == GPTClient.Instance.model)
                {
                    GPTClient.modelIndex = i;
                    break;
                }
            }

            if(GPTClient.modelIndex == -1)
            {
                GPTClient.modelIndex = 0;
                GPTClient.Instance.model = GPTClient.models[0];
            }

            _selectedModelIndex = GPTClient.modelIndex;

            return _secretKey;
        }

        public void SaveSettings()
        {
            // Update provider if changed
            if (GPTClient.currentProvider != (AIProvider)_selectedProviderIndex)
            {
                GPTClient.Instance.ChangeProvider((AIProvider)_selectedProviderIndex);
                 
            }
        
            GPTClient.modelIndex = _selectedModelIndex;
            GPTClient.Instance.model = _models[_selectedModelIndex];  
            AIProvider selectedProvider = (AIProvider)_selectedProviderIndex;


            EditorPrefs.SetString("UAISecretKey_" + selectedProvider, _secretKey); 

            EditorPrefs.SetInt("UAIProvider", _selectedProviderIndex);
            EditorPrefs.SetString("GPTModel", _models[_selectedModelIndex]);
            EditorPrefs.SetInt("GPTModelIndex", _selectedModelIndex);
            EditorPrefs.SetFloat("GPTTemperature", _temperature);
            EditorPrefs.SetInt("GPTMaxTokens", _maxTokens);
            EditorPrefs.SetInt("GPTN", 1);//_n);
            EditorPrefs.SetString("GPTDefaultSavePath", _defaultSavePath);
            EditorPrefs.SetBool("GPTAskForSavePath", _askForSavePath);
            EditorPrefs.SetString("UAIEndpoint", _apiEndpoint);
            EditorPrefs.SetString("UAILanguage", _aiLanguage);

            // Save reasoning settings
            EditorPrefs.SetInt("UAIReasoningEffort", _reasoningEffortIndex);
            EditorPrefs.SetBool("UAIIncludeSummary", _includeSummary);

            GPTClient.Instance.apiKey = _secretKey;
            GPTClient.temperature = _temperature;
            GPTClient.maxTokens = _maxTokens;
            GPTClient.n = _n;
            GPTClient.defaultSavePath = _defaultSavePath;
            GPTClient.askForSavePath = _askForSavePath;
            GPTClient.apiEndpoint = _apiEndpoint;
            GPTClient.language = _aiLanguage;
            
            // Apply reasoning settings to client if it's a reasoning client
            ApplyReasoningSettings();
            
            // Update client with new settings
            GPTClient.Instance.UpdateAIClient();
        }

        private void ApplyReasoningSettings()
        {
            if (GPTClient.currentProvider == AIProvider.OpenAIReasoning)
            {
                var reasoningClient = GPTClient.Instance._aiClient as OpenAIClientReasoning;
                if (reasoningClient != null)
                {
                    reasoningClient.ReasoningEffort = (ReasoningEffort)_reasoningEffortIndex;
                    reasoningClient.IncludeSummary = _includeSummary;
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("AI Assistant Settings", EditorStyles.boldLabel);
            
            // AI Provider Selection
            int prevProviderIndex = _selectedProviderIndex;
            _selectedProviderIndex = EditorGUILayout.Popup("AI Provider:", _selectedProviderIndex, _providerNames);
            
            // If provider changed, update API endpoint and model list
            if (prevProviderIndex != _selectedProviderIndex)
            {
                _apiEndpoint = AIClientFactory.GetDefaultEndpoint((AIProvider)_selectedProviderIndex);
                
                // Reset model index when changing provider
                _selectedModelIndex = 0;

                //update API key
                _secretKey = EditorPrefs.GetString("UAISecretKey_" + (AIProvider)_selectedProviderIndex, ""); 
                
                // For Ollama, try to fetch the available models
                if ((AIProvider)_selectedProviderIndex == AIProvider.Ollama)
                {
                    FetchOllamaModels();
                }
                else
                {
                    // For other providers, just get the static model list
                    _models = AIClientFactory.GetModelsForProvider((AIProvider)_selectedProviderIndex);
                    _isLoadingModels = false;
                    _modelLoadingStatus = "";
                }
            }
            
            GUILayout.Space(10);
            
            // Display provider-specific information or notes
            DisplayProviderSpecificNotes();
            
            GUILayout.Space(10);
            
            // API Key label based on provider
            string apiKeyLabel = AIClientFactory.GetProviderResourceName((AIProvider)_selectedProviderIndex, "API Key");
            _secretKey = EditorGUILayout.PasswordField(apiKeyLabel + ":", _secretKey);
 
            // API key links based on provider
            if (GUILayout.Button("Get API Key"))
            {
                switch ((AIProvider)_selectedProviderIndex)
                {
                    case AIProvider.OpenAI:
                    case AIProvider.OpenAIReasoning:
                        Application.OpenURL("https://platform.openai.com/account/api-keys");
                        break;
                    case AIProvider.Anthropic:
                        Application.OpenURL("https://console.anthropic.com/keys");
                        break;
                    case AIProvider.Google:
                        Application.OpenURL("https://aistudio.google.com/app/apikey");
                        break;
                    case AIProvider.DeepSeek:
                        Application.OpenURL("https://platform.deepseek.com/api_keys");
                        break;
                    case AIProvider.Custom:
                        EditorUtility.DisplayDialog("Custom API Integration", 
                            "For custom providers, you may not need an API key, or it will be specific to your provider. " +
                            "Refer to your provider's documentation for details on authentication.", "OK");
                        break;
                }
            } 
            GUILayout.Space(10);

            // API Endpoint label based on provider
            string apiEndpointLabel = AIClientFactory.GetProviderResourceName((AIProvider)_selectedProviderIndex, "API Endpoint");
            _apiEndpoint = EditorGUILayout.TextField(apiEndpointLabel + ":", _apiEndpoint);
            
            // Button to set to default endpoint (not shown for Custom provider)
            if ((AIProvider)_selectedProviderIndex != AIProvider.Custom)
            {
                if(GUILayout.Button("Set to default endpoint"))
                {
                    _apiEndpoint = AIClientFactory.GetDefaultEndpoint((AIProvider)_selectedProviderIndex);
                }
            }
 
            GUILayout.Space(10);
            
            // Models section with refresh button for Ollama
            EditorGUILayout.BeginHorizontal();
            
            if (_isLoadingModels)
            {
                EditorGUILayout.LabelField("Model: Loading...");
                EditorGUILayout.EndHorizontal();
                
                // Show a progress indicator
                Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(progressRect, Mathf.PingPong(Time.realtimeSinceStartup * 0.5f, 1.0f), "Loading models...");
            }
            else
            {
                // Model selection dropdown
                _selectedModelIndex = EditorGUILayout.Popup("Model:", _selectedModelIndex, _models);
                
                // Only show refresh button for Ollama
                if ((AIProvider)_selectedProviderIndex == AIProvider.Ollama)
                {
                    if (GUILayout.Button("Refresh Models", GUILayout.Width(120)))
                    {
                        FetchOllamaModels();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Show model loading status if needed
                if (!string.IsNullOrEmpty(_modelLoadingStatus))
                {
                    EditorGUILayout.HelpBox(_modelLoadingStatus, 
                        _modelLoadingSuccess ? MessageType.Info : MessageType.Warning);
                }
            }
 
            GUILayout.Space(10);

            // Show reasoning settings only for OpenAI Reasoning provider
            if ((AIProvider)_selectedProviderIndex == AIProvider.OpenAIReasoning)
            {
                EditorGUILayout.LabelField("Reasoning Settings", EditorStyles.boldLabel);
                
                _reasoningEffortIndex = EditorGUILayout.Popup("Reasoning Effort:", _reasoningEffortIndex, _reasoningEffortNames);
                EditorGUILayout.HelpBox(GetReasoningEffortDescription(), MessageType.Info);
                
                _includeSummary = EditorGUILayout.Toggle("Include Reasoning Summary:", _includeSummary);
                if (_includeSummary)
                {
                    EditorGUILayout.HelpBox("Reasoning summaries show how the model approached the problem. Note: This may require organization verification.", MessageType.Info);
                }
                
                GUILayout.Space(10);
            }

            _temperature = EditorGUILayout.FloatField("Temperature:", _temperature);
            if(_temperature < 0)
                _temperature = 0;
            if(_temperature > 1)
                _temperature = 1;

            //language
            _selectedLanguageIndex = EditorGUILayout.Popup("AI Language:", _selectedLanguageIndex, languages);   
            // If custom language is selected, allow user to enter a custom value
            if (languages[_selectedLanguageIndex] == "custom")
            {
                //info that AIs may not support custom languages
                EditorGUILayout.HelpBox("Custom languages may not be supported by all AI providers. Use with caution and test your AI's responses.", MessageType.Warning);
                _aiLanguage = EditorGUILayout.TextField("Custom Language:", _aiLanguage);
            }
            else
            {
                // Reset to default language if not custom
                _aiLanguage = languages[_selectedLanguageIndex];
            }
 
            GUILayout.Space(10);

            _defaultSavePath = EditorGUILayout.TextField("Default Save Path:", _defaultSavePath);
            //label
            GUILayout.Label("Ask for Save Path on every creation?");
            _askForSavePath = EditorGUILayout.Toggle("Ask for Save Path?", _askForSavePath); 


    
            GUILayout.Space(10);

            // Check for settings changes
            bool settingsChanged = HasSettingsChanged();
            
            GUI.enabled = settingsChanged;
            if (GUILayout.Button("Save Settings"))
            {
                SaveSettings();
            }
            GUI.enabled = true;

            GUILayout.Space(10);
            
        }

        private bool HasSettingsChanged()
        {
            bool basicSettingsChanged =
                GPTClient.Instance.apiKey != _secretKey ||
                GPTClient.currentProvider != (AIProvider)_selectedProviderIndex ||
                GPTClient.Instance.model != GPTClient.models[_selectedModelIndex] ||
                GPTClient.temperature != _temperature ||
                GPTClient.maxTokens != _maxTokens ||
                GPTClient.defaultSavePath != _defaultSavePath ||
                GPTClient.askForSavePath != _askForSavePath ||
                GPTClient.apiEndpoint != _apiEndpoint ||
                GPTClient.language != _aiLanguage;

            bool reasoningSettingsChanged = false;
            if ((AIProvider)_selectedProviderIndex == AIProvider.OpenAIReasoning)
            {
                int savedReasoningEffort = EditorPrefs.GetInt("UAIReasoningEffort", 1);
                bool savedIncludeSummary = EditorPrefs.GetBool("UAIIncludeSummary", false);
                
                reasoningSettingsChanged = 
                    _reasoningEffortIndex != savedReasoningEffort ||
                    _includeSummary != savedIncludeSummary;
            }

            return basicSettingsChanged || reasoningSettingsChanged;
        }

        private string GetReasoningEffortDescription()
        {
            switch (_reasoningEffortIndex)
            {
                case 0: // Low
                    return "Low effort: Favors speed and economical token usage. Good for simpler tasks.";
                case 1: // Medium
                    return "Medium effort: Balanced approach between speed and reasoning accuracy. Recommended for most tasks.";
                case 2: // High
                    return "High effort: Favors more complete reasoning. Best for complex problems but uses more tokens.";
                default:
                    return "Medium effort: Balanced approach between speed and reasoning accuracy.";
            }
        }
        
        /// <summary>
        /// Fetches the list of available models from the Ollama server.
        /// </summary>
        private void FetchOllamaModels()
        {
            _isLoadingModels = true;
            _modelLoadingStatus = "Connecting to Ollama server...";
            _modelLoadingSuccess = true;
            
            // Use default models while loading
            _models = AIClientFactory.GetModelsForProvider(AIProvider.Ollama);
            
            AIClientFactory.FetchModelsForProvider(AIProvider.Ollama, (models, success) => {
                _isLoadingModels = false;
                _models = models;
                
                if (success)
                {
                    _modelLoadingStatus = $"Found {models.Length} models in your Ollama installation.";
                    _modelLoadingSuccess = true;
                    
                    // Reset model index if it's out of range
                    if (_selectedModelIndex >= _models.Length)
                    {
                        _selectedModelIndex = 0;
                    }
                }
                else
                {
                    _modelLoadingStatus = "Couldn't connect to Ollama. Is it running? Using default model list.";
                    _modelLoadingSuccess = false;
                }
                
                // Force repaint of the window
                Repaint();
            });
        }
        
        /// <summary>
        /// Displays provider-specific notes and help text.
        /// </summary>
        private void DisplayProviderSpecificNotes()
        {
            AIProvider selectedProvider = (AIProvider)_selectedProviderIndex;
            
            switch (selectedProvider)
            {    
                case AIProvider.OpenAIReasoning:
                    EditorGUILayout.HelpBox(
                        "OpenAI Reasoning models (GPT-5, o3, o1 series) are designed for complex problem-solving, " +
                        "coding, scientific reasoning, and multi-step planning. They think before responding, " +
                        "which may take longer but provides more thorough answers.", 
                        MessageType.Info);
                    break;
                        
                case AIProvider.Custom:
                    EditorGUILayout.HelpBox(
                        "Custom provider works with OpenAI-compatible APIs like LMStudio or other local LLM servers. " +
                        "Use the API Endpoint field to specify the server URL.", 
                        MessageType.Info);
                    
                    // Toggle for more detailed help
                    _showCustomProviderHelp = EditorGUILayout.Foldout(_showCustomProviderHelp, "Show more Custom Provider help");
                    
                    if (_showCustomProviderHelp)
                    {
                        EditorGUILayout.HelpBox(
                            "Setup tips:\n" +
                            "1. For local servers, use endpoints like 'http://localhost:1234/v1/chat/completions'\n" +
                            "2. Some providers may require an API key, others don't\n" +
                            "3. The 'custom-model' value is just a placeholder - the actual model is selected on your server. Replace it, if needed.\n" +
                            "4. If you encounter errors, check your server logs for details", 
                            MessageType.None);
                    }
                    break;
            }
        }
    }
}