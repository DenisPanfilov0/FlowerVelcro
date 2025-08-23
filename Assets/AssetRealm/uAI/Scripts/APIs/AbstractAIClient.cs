using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UAI
{
    public abstract class AbstractAIClient
    {
        #region Properties
        public string apiKey = "";
        public string response = "";
        public string SystemInitPrompt = "You are a professional assistant. You always answer in " + GPTClient.language;
        public Action<string, int> OnResponseReceived;
        public Action<string> OnPartResponseReceived;
        public string modelName = "";
        public abstract string[] SupportedModels { get; }
        #endregion

        #region Configuration
        public float temperature = 0.7f;
        public int maxTokens = 100000;
        public string endpointUrl = "";
        public int n = 1;
        #endregion

        #region Abstract Methods
        
        public abstract void SendRequest(string promptSend, int index = 0);

        public abstract void SendRequestWithHistory(List<GPTChatMessage> chatMessages);

        public abstract void StopGeneration();
        #endregion

        #region Utility Methods
        public virtual int GetMaxTokens()
        {
            return 100000; // Default value
        }

        public virtual bool SupportsStreaming()
        {
            return true; // Most clients support streaming
        }

        protected abstract object ConvertMessages(List<GPTChatMessage> messages);
        #endregion
    }
}