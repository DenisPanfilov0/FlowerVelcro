using System;
using System.Collections.Generic;
using UnityEngine;

namespace UAI
{
    public enum AIProvider
    {
        OpenAI,
        Anthropic,
        Google,
        DeepSeek,
        Ollama,
        OpenAIReasoning,
        Custom
    }

    public static class AIClientFactory
    {
        public static AbstractAIClient CreateClient(AIProvider provider)
        {
            switch (provider)
            {
                case AIProvider.OpenAI:
                    return new OpenAIClient();
                
                case AIProvider.Anthropic:
                    return new ClaudeClient();
                
                case AIProvider.Google:
                    return new GeminiClient();
                
                case AIProvider.DeepSeek:
                    return new DeepSeekClient();
                
                case AIProvider.Ollama:
                    return new OllamaClient();
                
                case AIProvider.OpenAIReasoning:
                    return new OpenAIClientReasoning();
                
                case AIProvider.Custom:
                    return new CustomClient();
                
                default:
                    Debug.LogWarning($"Unknown AI provider: {provider}. Falling back to OpenAI.");
                    return new OpenAIClient();
            }
        }

        public static string[] GetModelsForProvider(AIProvider provider)
        {
            AbstractAIClient client = CreateClient(provider);
            return client.SupportedModels;
        }
        
        public static void FetchModelsForProvider(AIProvider provider, Action<string[], bool> callback)
        {
            switch (provider)
            {
                case AIProvider.Ollama:
                    OllamaClient.FetchAvailableModels(callback);
                    break;
                
                default:
                    // For providers that don't support dynamic fetching, just return the static list
                    AbstractAIClient client = CreateClient(provider);
                    callback?.Invoke(client.SupportedModels, true);
                    break;
            }
        }
        public static string GetProviderResourceName(AIProvider provider, string resourceType)
        {
            if (resourceType == "API Key")
            {
                switch (provider)
                {
                    case AIProvider.OpenAI:
                        return "OpenAI API Key";
                    case AIProvider.Anthropic:
                        return "Anthropic API Key";
                    case AIProvider.Google:
                        return "Google API Key";
                    case AIProvider.DeepSeek:
                        return "DeepSeek API Key";
                    case AIProvider.Ollama:
                        return "API Key (Not Required)";
                    case AIProvider.OpenAIReasoning:
                        return "OpenAI API Key (Reasoning)";
                    case AIProvider.Custom:
                        return "API Key (Optional)";
                    default:
                        return "API Key";
                }
            }
            else if (resourceType == "API Endpoint")
            {
                switch (provider)
                {
                    case AIProvider.OpenAI:
                        return "OpenAI API Endpoint";
                    case AIProvider.Anthropic:
                        return "Anthropic API Endpoint";
                    case AIProvider.Google:
                        return "Google API Endpoint";
                    case AIProvider.DeepSeek:
                        return "DeepSeek API Endpoint";
                    case AIProvider.Ollama:
                        return "Ollama Server URL";
                    case AIProvider.OpenAIReasoning:
                        return "OpenAI Reasoning API Endpoint";
                    case AIProvider.Custom:
                        return "API Endpoint";
                    default:
                        return "API Endpoint";
                }
            }
            
            return resourceType;
        }
        
        public static string GetDefaultEndpoint(AIProvider provider)
        {
            switch (provider)
            {
                case AIProvider.OpenAI:
                    return "https://api.openai.com/v1/chat/completions";
                
                case AIProvider.Anthropic:
                    return "https://api.anthropic.com/v1/messages";
                
                case AIProvider.Google:
                    return "https://generativelanguage.googleapis.com/v1/models/{model}:streamGenerateContent";
                
                case AIProvider.DeepSeek:
                    return "https://api.deepseek.com/v1/chat/completions";
                
                case AIProvider.Ollama:
                    return "http://localhost:11434/api/chat";
                
                case AIProvider.OpenAIReasoning:
                    return "https://api.openai.com/v1/responses";
                
                case AIProvider.Custom:
                    return "http://localhost:1234/v1/chat/completions";
                
                default:
                    return "https://api.openai.com/v1/chat/completions";
            }
        }
        
        public static string GetDefaultModel(AIProvider provider)
        {
            switch (provider)
            {
                case AIProvider.OpenAI:
                    return "gpt-4o";
                
                case AIProvider.Anthropic:
                    return "claude-sonnet-4-20250514";
                
                case AIProvider.Google:
                    return "gemini-2.5-pro-preview-05-06";
                
                case AIProvider.DeepSeek:
                    return "deepseek-chat";
                
                case AIProvider.Ollama:
                    return "llama3";
                
                case AIProvider.OpenAIReasoning:
                    return "gpt-5";
                
                case AIProvider.Custom:
                    return "custom-model";
                
                default:
                    return "gpt-4o";
            }
        }
    }
}