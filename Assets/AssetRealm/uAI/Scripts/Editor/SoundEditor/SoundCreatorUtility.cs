using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using UnityEngine.Networking;

namespace UAI
{ 
    [Serializable]
    public class SoundEffectRequest
    {
        public string text;
        public float duration_seconds;
        public float prompt_influence;
    }

    [Serializable]
    public class TtsRequest
    {
        public string text;
        public string model_id;
        public VoiceSettings voice_settings;
        public string apply_text_normalization;
    }

    [Serializable]
    public class VoiceSettings
    {
        public float stability;
        public float similarity_boost;
    }
    
    [Serializable]
    public class SoundFileMetadata 
    {
        public string FilePath;
        public DateTime CreationDate;
        public string Prompt;
        public string GenerationType;
        public bool IsLoaded;
        
        public SoundFileMetadata(string path, DateTime date, string prompt, string type) 
        {
            FilePath = path;
            CreationDate = date;
            Prompt = prompt;
            GenerationType = type;
            IsLoaded = false;
        }
    }
    
    public class SoundCreatorUtility
    {
        private const string ELEVENLABS_API_URL = "https://api.elevenlabs.io/v1"; 

        #region Settings Management
        public static void LoadSettings(
            string prefsKeyPrefix,
            ref string apiKey,
            ref string savePath,
            ref string externalSavePath,
            ref bool useExternalPath,
            ref string defaultVoiceId,
            ref string defaultTtsModel,
            ref string defaultVcModel,
            ref string defaultSfxModel,
            ref string defaultQuality,
            ref bool removeBgNoise)
        {
            apiKey = EditorPrefs.GetString($"{prefsKeyPrefix}ApiKey", apiKey);
            savePath = EditorPrefs.GetString($"{prefsKeyPrefix}SavePath", savePath);
            externalSavePath = EditorPrefs.GetString($"{prefsKeyPrefix}ExternalSavePath", externalSavePath);
            useExternalPath = EditorPrefs.GetBool($"{prefsKeyPrefix}UseExternalPath", useExternalPath);
            defaultVoiceId = EditorPrefs.GetString($"{prefsKeyPrefix}DefaultVoiceId", defaultVoiceId);
            defaultTtsModel = EditorPrefs.GetString($"{prefsKeyPrefix}DefaultTtsModel", defaultTtsModel);
            defaultVcModel = EditorPrefs.GetString($"{prefsKeyPrefix}DefaultVcModel", defaultVcModel);
            defaultSfxModel = EditorPrefs.GetString($"{prefsKeyPrefix}DefaultSfxModel", defaultSfxModel);
            defaultQuality = EditorPrefs.GetString($"{prefsKeyPrefix}DefaultQuality", defaultQuality);
            removeBgNoise = EditorPrefs.GetBool($"{prefsKeyPrefix}RemoveBgNoise", removeBgNoise);
        }

        public static void SaveSettings(
            string prefsKeyPrefix,
            string apiKey,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            string defaultVoiceId,
            string defaultTtsModel,
            string defaultVcModel,
            string defaultSfxModel,
            string defaultQuality,
            bool removeBgNoise)
        {
            EditorPrefs.SetString($"{prefsKeyPrefix}ApiKey", apiKey);
            EditorPrefs.SetString($"{prefsKeyPrefix}SavePath", savePath);
            EditorPrefs.SetString($"{prefsKeyPrefix}ExternalSavePath", externalSavePath);
            EditorPrefs.SetBool($"{prefsKeyPrefix}UseExternalPath", useExternalPath);
            EditorPrefs.SetString($"{prefsKeyPrefix}DefaultVoiceId", defaultVoiceId);
            EditorPrefs.SetString($"{prefsKeyPrefix}DefaultTtsModel", defaultTtsModel);
            EditorPrefs.SetString($"{prefsKeyPrefix}DefaultVcModel", defaultVcModel);
            EditorPrefs.SetString($"{prefsKeyPrefix}DefaultSfxModel", defaultSfxModel);
            EditorPrefs.SetString($"{prefsKeyPrefix}DefaultQuality", defaultQuality);
            EditorPrefs.SetBool($"{prefsKeyPrefix}RemoveBgNoise", removeBgNoise);
        }
        #endregion

        #region Sound Asset Management
        public static void LoadSoundsFromPath(
            string basePath,
            ref Dictionary<DateTime, List<CreatedSoundEntry>> createdSounds,
            string dataPath)
        { 
            createdSounds.Clear();
 
            if (!Directory.Exists(basePath))
            { 
                try
                {
                    Directory.CreateDirectory(basePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create directory {basePath}: {ex.Message}");
                    return;
                }
            }

            // Determine if path is within the project or external
            bool isInProject = basePath.StartsWith("Assets") || basePath.StartsWith(dataPath);

            // Get all mp3 files in the directory and its subdirectories
            string[] audioFiles = Directory.GetFiles(basePath, "*.mp3", SearchOption.AllDirectories);
            string[] wavFiles = Directory.GetFiles(basePath, "*.wav", SearchOption.AllDirectories);

            // Combine the file lists
            List<string> allAudioFiles = new List<string>();
            allAudioFiles.AddRange(audioFiles);
            allAudioFiles.AddRange(wavFiles);

            foreach (string filePath in allAudioFiles)
            {
                try
                {
                    // Extract metadata from filename
                    // Expected format: YYYY-MM-DD_HH-MM-SS_Type_[Prompt].[extension]
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string[] parts = fileName.Split('_');

                    if (parts.Length < 3)
                    {
                        // Invalid filename format
                        continue;
                    }

                    // Parse date
                    string dateStr = parts[0];
                    string timeStr = parts[1];
                    string typeStr = parts[2];

                    // Get prompt
                    string prompt = "";
                    for (int i = 3; i < parts.Length; i++)
                    {
                        prompt += parts[i] + (i < parts.Length - 1 ? "_" : "");
                    }

                    // Try to parse date and time
                    if (!DateTime.TryParseExact(
                        $"{dateStr} {timeStr.Replace('-', ':')}",
                        "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime creationDate))
                    {
                        // Invalid date format
                        continue;
                    }

                    // Load the audio clip
                    AudioClip audioClip = null;

                    if (isInProject)
                    {
                        // Convert to project relative path
                        string assetPath = filePath;
                        if (assetPath.StartsWith(dataPath))
                        {
                            assetPath = "Assets" + assetPath.Substring(dataPath.Length);
                        }

                        // Load from project
                        audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    }
                    else
                    {
                        // Load from external path (needs to be imported first)
                        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.UNKNOWN))
                        {
                            var operation = www.SendWebRequest();
                            while (!operation.isDone) { }

                            if (www.result == UnityWebRequest.Result.Success)
                            {
                                audioClip = DownloadHandlerAudioClip.GetContent(www);
                            }
                            else
                            {
                                Debug.LogError($"Failed to load audio clip from {filePath}: {www.error}");
                            }
                        }

                        // Keep trying until the clip is ready
                        int attempts = 0;
                        while (audioClip != null && audioClip.loadState != AudioDataLoadState.Loaded && attempts < 30)
                        {
                            System.Threading.Thread.Sleep(100);
                            attempts++;
                        }
                    }

                    if (audioClip == null)
                    {
                        Debug.LogWarning($"Failed to load audio clip from {filePath}");
                        continue;
                    }

                    // Create entry
                    CreatedSoundEntry entry = new CreatedSoundEntry
                    {
                        AudioClip = audioClip,
                        Prompt = prompt,
                        GenerationType = typeStr,
                        CreationDate = creationDate,
                        ModelUsed = "Unknown",
                        AssetPath = filePath,
                        IsInTrash = false
                    };

                    // Add to the appropriate date group
                    DateTime dateKey = creationDate.Date;
                    if (!createdSounds.ContainsKey(dateKey))
                    {
                        createdSounds[dateKey] = new List<CreatedSoundEntry>();
                    }

                    createdSounds[dateKey].Add(entry);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading sound from {filePath}: {ex.Message}");
                }
            }

            // Sort each date group by creation time (newest first)
            foreach (var dateGroup in createdSounds.Keys)
            {
                createdSounds[dateGroup].Sort((a, b) => b.CreationDate.CompareTo(a.CreationDate));
            }
        }
        public static void LoadSoundBatch(Dictionary<DateTime, List<SoundFileMetadata>> fileMetadata, ref Dictionary<DateTime, List<CreatedSoundEntry>> createdSounds, int batchSize,bool isInProject, string dataPath)
        {
            int loadedCount = 0;
            
            // Iterate through dates (newest first)
            foreach (var dateKey in fileMetadata.Keys.OrderByDescending(d => d))
            {
                var filesForDate = fileMetadata[dateKey];
                
                // Find unloaded files for this date
                var unloadedFiles = filesForDate.Where(f => !f.IsLoaded).ToList();
                
                // Take up to the remaining batch size
                int toLoad = Math.Min(batchSize - loadedCount, unloadedFiles.Count);
                if (toLoad <= 0) continue;
                
                // Load this batch
                for (int i = 0; i < toLoad; i++)
                {
                    var metadata = unloadedFiles[i];
                    string filePath = metadata.FilePath;
                    
                    try
                    {
                        // Load the audio clip
                        AudioClip audioClip = null;
                        
                        if (isInProject)
                        {
                            // Convert to project relative path
                            string assetPath = filePath;
                            if (assetPath.StartsWith(dataPath))
                            {
                                assetPath = "Assets" + assetPath.Substring(dataPath.Length);
                            }
                            
                            // Load from project
                            audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                        }
                        else
                        {
                            // Load from external path
                            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.UNKNOWN))
                            {
                                var operation = www.SendWebRequest();
                                while (!operation.isDone) { }

                                if (www.result == UnityWebRequest.Result.Success)
                                {
                                    audioClip = DownloadHandlerAudioClip.GetContent(www);
                                }
                                else
                                {
                                    Debug.LogError($"Failed to load audio clip from {filePath}: {www.error}");
                                }
                            }
                            
                            // Keep trying until the clip is ready
                            int attempts = 0;
                            while (audioClip.loadState != AudioDataLoadState.Loaded && attempts < 30)
                            {
                                System.Threading.Thread.Sleep(100);
                                attempts++;
                            }
                        }
                        
                        if (audioClip == null)
                        {
                            Debug.LogWarning($"Failed to load audio clip from {filePath}");
                            continue;
                        }
                        
                        // Create entry
                        CreatedSoundEntry entry = new CreatedSoundEntry
                        {
                            AudioClip = audioClip,
                            Prompt = metadata.Prompt,
                            GenerationType = metadata.GenerationType,
                            CreationDate = metadata.CreationDate,
                            ModelUsed = "Unknown",
                            AssetPath = filePath,
                            IsInTrash = false
                        };
                        
                        // Add to the appropriate date group in createdSounds
                        if (!createdSounds.ContainsKey(dateKey))
                        {
                            createdSounds[dateKey] = new List<CreatedSoundEntry>();
                        }
                        
                        createdSounds[dateKey].Add(entry);
                        
                        // Mark as loaded
                        metadata.IsLoaded = true;
                        loadedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error loading sound from {filePath}: {ex.Message}");
                    }
                }
                
                // Sort the loaded sounds by creation date
                if (createdSounds.ContainsKey(dateKey))
                {
                    createdSounds[dateKey].Sort((a, b) => b.CreationDate.CompareTo(a.CreationDate));
                }
                
                // If we've loaded enough for this batch, stop
                if (loadedCount >= batchSize) break;
            }
        }
        
        public static bool HasMoreSoundsToLoad(Dictionary<DateTime, List<SoundFileMetadata>> fileMetadata)
        {
            foreach (var dateGroup in fileMetadata.Values)
            {
                if (dateGroup.Any(f => !f.IsLoaded))
                {
                    return true;
                }
            }
            return false;
        }

        public static Dictionary<DateTime, List<SoundFileMetadata>> ScanSoundFiles(string basePath, string dataPath)
        {
            Dictionary<DateTime, List<SoundFileMetadata>> result = new Dictionary<DateTime, List<SoundFileMetadata>>();

            // Check if path exists
            if (!Directory.Exists(basePath))
            {
                try
                {
                    Directory.CreateDirectory(basePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create directory {basePath}: {ex.Message}");
                    return result;
                }
            }

            // Get all mp3 and wav files in the directory and its subdirectories
            string[] audioFiles = Directory.GetFiles(basePath, "*.mp3", SearchOption.AllDirectories);
            string[] wavFiles = Directory.GetFiles(basePath, "*.wav", SearchOption.AllDirectories);

            // Combine the file lists
            List<string> allAudioFiles = new List<string>();
            allAudioFiles.AddRange(audioFiles);
            allAudioFiles.AddRange(wavFiles);

            // Process file metadata without loading the audio
            foreach (string filePath in allAudioFiles)
            {
                try
                {
                    // Extract metadata from filename
                    // Expected format: YYYY-MM-DD_HH-MM-SS_Type_[Prompt].[extension]
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string[] parts = fileName.Split('_');

                    if (parts.Length < 3)
                    {
                        // Invalid filename format
                        continue;
                    }

                    // Parse date
                    string dateStr = parts[0];
                    string timeStr = parts[1];
                    string typeStr = parts[2];

                    // Get prompt (everything after the type)
                    string prompt = "";
                    for (int i = 3; i < parts.Length; i++)
                    {
                        prompt += parts[i] + (i < parts.Length - 1 ? "_" : "");
                    }

                    // Try to parse date and time
                    if (!DateTime.TryParseExact(
                        $"{dateStr} {timeStr.Replace('-', ':')}",
                        "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime creationDate))
                    {
                        // Invalid date format
                        continue;
                    }

                    // Create file metadata
                    SoundFileMetadata metadata = new SoundFileMetadata(
                        filePath,
                        creationDate,
                        prompt,
                        typeStr
                    );

                    // Add to the appropriate date group
                    DateTime dateKey = creationDate.Date;
                    if (!result.ContainsKey(dateKey))
                    {
                        result[dateKey] = new List<SoundFileMetadata>();
                    }

                    result[dateKey].Add(metadata);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing metadata for {filePath}: {ex.Message}");
                }
            }

            // Sort each date group by creation time (newest first)
            foreach (var dateGroup in result.Keys)
            {
                result[dateGroup].Sort((a, b) => b.CreationDate.CompareTo(a.CreationDate));
            }

            return result;
        }


        public static List<CreatedSoundEntry> LoadTrashSounds(string prefsKeyPrefix, ref Dictionary<DateTime, List<CreatedSoundEntry>> createdSounds)
        {
            List<CreatedSoundEntry> trashSounds = new List<CreatedSoundEntry>();

            // Load trash paths from EditorPrefs
            string trashJson = EditorPrefs.GetString($"{prefsKeyPrefix}TrashSounds", "{}");
            TrashSoundsData trashData = JsonUtility.FromJson<TrashSoundsData>(trashJson);

            if (trashData == null || trashData.TrashPaths == null)
            {
                trashData = new TrashSoundsData();
                return trashSounds;
            }

            // Check each path
            foreach (string path in trashData.TrashPaths)
            {
                // Skip if file doesn't exist
                if (!File.Exists(path)) continue;

                // Try to load the audio clip
                AudioClip audioClip = null;

                if (path.StartsWith("Assets"))
                {
                    // Load from project
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
                else
                {
                    // Load from external path
                    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
                    {
                        var operation = www.SendWebRequest();
                        while (!operation.isDone) { }

                        if (www.result == UnityWebRequest.Result.Success)
                        {
                            audioClip = DownloadHandlerAudioClip.GetContent(www);
                        }
                        else
                        {
                            Debug.LogError($"Failed to load audio clip from {path}: {www.error}");
                        }
                    }

                    // Keep trying until the clip is ready
                    int attempts = 0;
                    while (audioClip.loadState != AudioDataLoadState.Loaded && attempts < 30)
                    {
                        System.Threading.Thread.Sleep(100);
                        attempts++;
                    }
                }

                if (audioClip == null) continue;

                // Extract metadata from filename
                string fileName = Path.GetFileNameWithoutExtension(path);
                string[] parts = fileName.Split('_');

                if (parts.Length < 3) continue;

                // Parse date
                string dateStr = parts[0];
                string timeStr = parts[1];
                string typeStr = parts[2];

                // Get prompt (everything after the type)
                string prompt = "";
                for (int i = 3; i < parts.Length; i++)
                {
                    prompt += parts[i] + (i < parts.Length - 1 ? "_" : "");
                }

                // Try to parse date and time
                if (!DateTime.TryParseExact(
                    $"{dateStr} {timeStr.Replace('-', ':')}",
                    "yyyy-MM-dd HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime creationDate))
                {
                    continue;
                }

                // Create entry
                CreatedSoundEntry entry = new CreatedSoundEntry
                {
                    AudioClip = audioClip,
                    Prompt = prompt,
                    GenerationType = typeStr,
                    CreationDate = creationDate,
                    ModelUsed = "Unknown",
                    AssetPath = path,
                    IsInTrash = true
                };

                trashSounds.Add(entry);
            }

            // Sort by creation time (newest first)
            trashSounds.Sort((a, b) => b.CreationDate.CompareTo(a.CreationDate));

            return trashSounds;
        }

        public static void SaveTrashSounds(string prefsKeyPrefix, List<CreatedSoundEntry> trashSounds)
        { 
            TrashSoundsData trashData = new TrashSoundsData();

            foreach (var sound in trashSounds)
            {
                trashData.TrashPaths.Add(sound.AssetPath);
            }

            string trashJson = JsonUtility.ToJson(trashData);
            EditorPrefs.SetString($"{prefsKeyPrefix}TrashSounds", trashJson);
        }

        public static void RestoreSoundFromTrash(CreatedSoundEntry sound, ref List<CreatedSoundEntry> trashSounds, ref Dictionary<DateTime, List<CreatedSoundEntry>> createdSounds)
        {
            if (sound == null) return;

            trashSounds.Remove(sound);
            sound.IsInTrash = false;

            DateTime dateKey = sound.CreationDate.Date;
            if (!createdSounds.ContainsKey(dateKey))
            {
                createdSounds[dateKey] = new List<CreatedSoundEntry>();
            }

            createdSounds[dateKey].Add(sound);

            createdSounds[dateKey].Sort((a, b) => b.CreationDate.CompareTo(a.CreationDate));
        }

        public static void ExportSound(CreatedSoundEntry sound, string dataPath)
        {
            if (sound == null || sound.AudioClip == null) return;

            // Default to desktop
            string initialPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // Generate a default filename based on the sound info
            string defaultFilename = $"{sound.GenerationType}_{sound.Prompt.Substring(0, Math.Min(20, sound.Prompt.Length))}";

            // Remove invalid characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                defaultFilename = defaultFilename.Replace(c, '_');
            }

            // Add extension
            string extension = Path.GetExtension(sound.AssetPath);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".mp3";
            }

            // Show save file dialog
            string path = EditorUtility.SaveFilePanel(
                "Export Sound",
                initialPath,
                defaultFilename + extension,
                extension.Substring(1) // Remove the dot
            );

            if (string.IsNullOrEmpty(path)) return;

            // Check if the source is in the project or external
            if (sound.AssetPath.StartsWith("Assets") || sound.AssetPath.StartsWith(dataPath))
            {
                // Convert to absolute path if needed
                string sourcePath = sound.AssetPath;
                if (sourcePath.StartsWith("Assets"))
                {
                    sourcePath = Path.Combine(dataPath, sourcePath.Substring(7));
                }

                // Copy the file
                try
                {
                    File.Copy(sourcePath, path, true);
                    // Debug.Log($"Sound exported to: {path}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to export sound: {ex.Message}");
                    EditorUtility.DisplayDialog("Export Failed", $"Failed to export sound: {ex.Message}", "OK");
                }
            }
            else
            {
                // External file - copy directly
                try
                {
                    File.Copy(sound.AssetPath, path, true);
                    // Debug.Log($"Sound exported to: {path}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to export sound: {ex.Message}");
                    EditorUtility.DisplayDialog("Export Failed", $"Failed to export sound: {ex.Message}", "OK");
                }
            }
        }
        #endregion

        #region UI Building
        public static void PopulateCreationsPanel(ScrollView scrollView, Dictionary<DateTime, List<CreatedSoundEntry>> createdSounds, Action<CreatedSoundEntry> onSoundClicked, Action<ContextClickEvent, CreatedSoundEntry> onContextMenu, Action onLoadMoreClicked, Dictionary<DateTime, List<SoundFileMetadata>> _soundFileMetadata)
        {
            scrollView.Clear();
            var sortedDates = createdSounds.Keys.OrderByDescending(date => date).ToList();
            
            if (sortedDates.Count == 0)
            {
                // No sounds yet
                var emptyLabel = new Label("No sounds created yet. Go to the Create panel to generate sounds.");
                emptyLabel.style.paddingTop = 20;
                emptyLabel.style.paddingBottom = 20;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                scrollView.Add(emptyLabel);
                return;
            }
            
            // Create a section for each date
            foreach (var date in sortedDates)
            {
                // Date header
                var dateHeader = new VisualElement();
                dateHeader.AddToClassList("date-header");
                
                var dateLabel = new Label(date.ToString("dddd, MMMM d, yyyy"));
                dateLabel.AddToClassList("date-label");
                dateHeader.Add(dateLabel);
                
                scrollView.Add(dateHeader);
                
                // Sounds container for this date
                var soundsContainer = new VisualElement();
                soundsContainer.AddToClassList("sounds-container");
                
                foreach (var sound in createdSounds[date])
                {
                    var soundItem = CreateSoundItem(sound, onSoundClicked, onContextMenu);
                    soundsContainer.Add(soundItem);
                }
                
                scrollView.Add(soundsContainer);
            }
            
            if (HasMoreSoundsToLoad(_soundFileMetadata))
            {
                var loadMoreContainer = new VisualElement();
                loadMoreContainer.style.alignItems = Align.Center;
                loadMoreContainer.style.marginTop = 20;
                loadMoreContainer.style.marginBottom = 160;
                
                var loadMoreButton = new Button(onLoadMoreClicked);
                loadMoreButton.text = "Load More Sounds";
                loadMoreButton.AddToClassList("load-more-button");
                
                loadMoreContainer.Add(loadMoreButton);
                scrollView.Add(loadMoreContainer);
            }
        }

        public static void PopulateTrashPanel(ScrollView scrollView, List<CreatedSoundEntry> trashSounds, Action<ContextClickEvent, CreatedSoundEntry> onContextMenu)
        {
            scrollView.Clear();

            if (trashSounds.Count == 0)
            {
                // No sounds in trash
                var emptyLabel = new Label("Trash is empty.");
                emptyLabel.style.paddingTop = 20;
                emptyLabel.style.paddingBottom = 20;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                scrollView.Add(emptyLabel);
                return;
            }

            // Create a container for all trash sounds
            var trashContainer = new VisualElement();
            trashContainer.AddToClassList("trash-container");

            foreach (var sound in trashSounds)
            {
                // Create a sound item similar to the creations panel
                var soundItem = new VisualElement();
                soundItem.AddToClassList("sound-item");
                soundItem.AddToClassList("trash-item");

                // Sound info container
                var infoContainer = new VisualElement();
                infoContainer.AddToClassList("sound-info");

                // Type badge
                var typeBadge = new Label(sound.GenerationType);
                typeBadge.AddToClassList("type-badge");

                // Add appropriate style based on type
                switch (sound.GenerationType.ToLower())
                {
                    case "sfx":
                        typeBadge.AddToClassList("sfx-badge");
                        break;
                    case "tts":
                        typeBadge.AddToClassList("tts-badge");
                        break;
                    case "vc":
                        typeBadge.AddToClassList("vc-badge");
                        break;
                }

                infoContainer.Add(typeBadge);

                // Sound prompt
                var promptLabel = new Label(sound.Prompt);
                promptLabel.AddToClassList("sound-prompt");
                infoContainer.Add(promptLabel);

                // Time label
                var timeLabel = new Label(sound.CreationDate.ToString("HH:mm:ss"));
                timeLabel.AddToClassList("sound-time");
                infoContainer.Add(timeLabel);

                soundItem.Add(infoContainer);

                // Register context menu event
                soundItem.RegisterCallback<ContextClickEvent>(evt => onContextMenu(evt, sound));

                trashContainer.Add(soundItem);
            }

            scrollView.Add(trashContainer);
        }

        private static VisualElement CreateSoundItem(CreatedSoundEntry sound, Action<CreatedSoundEntry> onSoundClicked, Action<ContextClickEvent, CreatedSoundEntry> onContextMenu)
        {
            var soundItem = new VisualElement();
            soundItem.AddToClassList("sound-item");

            var soundIcon = new VisualElement();
            soundIcon.AddToClassList("sound-icon");

            var waveform = new VisualElement();
            waveform.AddToClassList("sound-waveform");

            for (int i = 0; i < 8; i++)
            {
                var bar = new VisualElement();
                bar.AddToClassList("waveform-bar");

                float height = UnityEngine.Random.Range(5f, 20f);
                bar.style.height = height;

                waveform.Add(bar);
            }

            soundIcon.Add(waveform);

            // Play icon overlay
            var playIcon = new VisualElement();
            playIcon.AddToClassList("play-icon");
            soundIcon.Add(playIcon);

            soundItem.Add(soundIcon);

            // Sound info container
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("sound-info");

            // Type badge
            var typeBadge = new Label(sound.GenerationType);
            typeBadge.AddToClassList("type-badge");

            // Add appropriate style based on type
            switch (sound.GenerationType.ToLower())
            {
                case "sfx":
                    typeBadge.AddToClassList("sfx-badge");
                    break;
                case "tts":
                    typeBadge.AddToClassList("tts-badge");
                    break;
                case "vc":
                    typeBadge.AddToClassList("vc-badge");
                    break;
            }

            infoContainer.Add(typeBadge);

            // Sound prompt
            var promptLabel = new Label(sound.Prompt);
            promptLabel.AddToClassList("sound-prompt");
            infoContainer.Add(promptLabel);

            // Time label
            var timeLabel = new Label(sound.CreationDate.ToString("HH:mm:ss"));
            timeLabel.AddToClassList("sound-time");
            infoContainer.Add(timeLabel);

            soundItem.Add(infoContainer);

            // Make item clickable
            soundItem.RegisterCallback<ClickEvent>(evt => onSoundClicked(sound));

            // Register context menu event
            soundItem.RegisterCallback<ContextClickEvent>(evt => onContextMenu(evt, sound));

            return soundItem;
        }
        #endregion

        #region API Functions
        public static async void GenerateSoundEffect(
            string apiKey,
            string prompt,
            float duration,
            float promptInfluence,
            string outputFormat,
            Action<CreatedSoundEntry, bool> callback,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            string dataPath)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("API Key is not set.");
                callback(null, false);
                return;
            }

            string actualSavePath = useExternalPath ? externalSavePath : savePath;

            // Ensure the directory exists
            try
            {
                if (!Directory.Exists(actualSavePath))
                {
                    Directory.CreateDirectory(actualSavePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create directory: {ex.Message}");
                callback(null, false);
                return;
            }

            // Generate a filename based on current date/time
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            // Sanitize prompt for filename
            string sanitizedPrompt = SanitizeForFilename(prompt);
            if (sanitizedPrompt.Length > 50)
            {
                // Truncate long prompts
                sanitizedPrompt = sanitizedPrompt.Substring(0, 50);
            }

            string fileName = $"{timestamp}_SFX_{sanitizedPrompt}.mp3";
            string fullPath = Path.Combine(actualSavePath, fileName);

            // If path is within the project, make it relative to Assets
            string assetPath = fullPath;
            if (!useExternalPath && !assetPath.StartsWith("Assets"))
            {
                if (assetPath.StartsWith(dataPath))
                {
                    assetPath = "Assets" + assetPath.Substring(dataPath.Length);
                }
                else
                {
                    assetPath = Path.Combine("Assets", assetPath);
                }
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set up the request
                    client.BaseAddress = new Uri(ELEVENLABS_API_URL);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Add API key to headers
                    client.DefaultRequestHeaders.Add("xi-api-key", apiKey);

                    // Build a simple JSON string directly - more reliable than JsonUtility for this case
                    string jsonBody = $"{{\"text\":\"{EscapeJsonString(prompt)}\",\"duration_seconds\":{duration.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},\"prompt_influence\":{promptInfluence.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}}}";

                    // Debug.Log($"SFX Request Body: {jsonBody}");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    // Make the request
                    string requestUrl = $"/v1/sound-generation?output_format={outputFormat}";
                    HttpResponseMessage response = await client.PostAsync(requestUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Get the binary audio data
                        byte[] audioData = await response.Content.ReadAsByteArrayAsync();

                        // Save the file
                        File.WriteAllBytes(fullPath, audioData);

                        // Import the asset if it's in the project
                        if (!useExternalPath)
                        {
                            AssetDatabase.Refresh();
                        }

                        // Load the audio clip
                        AudioClip audioClip = null;

                        if (!useExternalPath)
                        {
                            // Load from project with import settings
                            string relativePath = assetPath;
                            if (relativePath.StartsWith(Application.dataPath))
                            {
                                relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                            }

                            // Update the audio import settings for the file
                            AudioImporter importer = AssetImporter.GetAtPath(relativePath) as AudioImporter;
                            if (importer != null)
                            {
                                // Set appropriate import settings
                                AudioImporterSampleSettings settings = new AudioImporterSampleSettings();
                                settings.loadType = AudioClipLoadType.DecompressOnLoad; // Important for immediate playback
                                settings.compressionFormat = AudioCompressionFormat.PCM;
                                settings.quality = 1.0f;

                                importer.defaultSampleSettings = settings;
                                importer.SaveAndReimport();

                                // Wait a bit for Unity to process
                                await Task.Delay(500);
                            }

                            // Now load the audio clip
                            AssetDatabase.Refresh();
                            audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);

                            // If loading failed, try again after a delay
                            if (audioClip == null)
                            {
                                await Task.Delay(1000);
                                AssetDatabase.Refresh();
                                audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                            }

                            if (audioClip != null)
                            {
                                // Force loading of audio data
                                if (!audioClip.LoadAudioData())
                                {
                                    Debug.LogWarning("Failed to load audio data for clip from project");
                                }
                            }
                            else
                            {
                                Debug.LogError($"Failed to load audio clip from path: {relativePath}");
                            }
                        }
                        else
                        {
                            // Load from external path
                            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.UNKNOWN); 
                            var operation = www.SendWebRequest();
                            while (!operation.isDone)
                            {
                                await Task.Delay(100);
                            }

                            if (www.result == UnityWebRequest.Result.Success)
                            {
                                audioClip = DownloadHandlerAudioClip.GetContent(www);

                                // Force loading of audio data
                                if (audioClip != null && !audioClip.LoadAudioData())
                                {
                                    Debug.LogWarning("Failed to load audio data for clip from external path");
                                }

                                // Keep trying until the clip is ready
                                int attempts = 0;
                                while (audioClip != null && audioClip.loadState != AudioDataLoadState.Loaded && attempts < 30)
                                {
                                    await Task.Delay(100);
                                    attempts++;
                                }
                            }
                            else
                            {
                                Debug.LogError($"Failed to load audio clip from {fullPath}: {www.error}");
                            }
                        
                        }

                        if (audioClip == null)
                        {
                            Debug.LogError("Failed to load generated audio clip.");

                            CreatedSoundEntry entry2 = new CreatedSoundEntry
                            {
                                AudioClip = null, 
                                Prompt = prompt,
                                GenerationType = "SFX",
                                CreationDate = DateTime.Now,
                                ModelUsed = "eleven_sfx_v1",
                                AssetPath = useExternalPath ? fullPath : assetPath,
                                IsInTrash = false
                            };

                            callback(entry2, false);
                            return;
                        }

                        // Debug info
                        // Debug.Log($"Audio clip loaded: Length={audioClip.length}s, Samples={audioClip.samples}, Channels={audioClip.channels}, State={audioClip.loadState}");

                        // Create entry
                        CreatedSoundEntry entry = new CreatedSoundEntry
                        {
                            AudioClip = audioClip,
                            Prompt = prompt,
                            GenerationType = "SFX",
                            CreationDate = DateTime.Now,
                            ModelUsed = "eleven_sfx_v1",
                            AssetPath = useExternalPath ? fullPath : assetPath,
                            IsInTrash = false
                        };

                        // Return success
                        callback(entry, true);
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"Failed to generate sound effect. Status code: {response.StatusCode}");
                        Debug.LogError(responseContent);
                        callback(null, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating sound effect: {ex.Message}");
                callback(null, false);
            }
        }


        public static async void GenerateTextToSpeech(
            string apiKey,
            string text,
            string voiceId,
            string modelId,
            string outputFormat,
            bool textNormalization,
            Action<CreatedSoundEntry, bool> callback,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            string dataPath)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("API Key is not set.");
                callback(null, false);
                return;
            }

            string actualSavePath = useExternalPath ? externalSavePath : savePath;

            // Ensure the directory exists
            try
            {
                if (!Directory.Exists(actualSavePath))
                {
                    Directory.CreateDirectory(actualSavePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create directory: {ex.Message}");
                callback(null, false);
                return;
            }

            // Generate a filename based on current date/time
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            // Sanitize text for filename
            string sanitizedText = SanitizeForFilename(text);
            if (sanitizedText.Length > 50)
            {
                // Truncate long text
                sanitizedText = sanitizedText.Substring(0, 50);
            }

            string fileName = $"{timestamp}_TTS_{sanitizedText}.mp3";
            string fullPath = Path.Combine(actualSavePath, fileName);

            // If path is within the project, make it relative to Assets
            string assetPath = fullPath;
            if (!useExternalPath && !assetPath.StartsWith("Assets"))
            {
                if (assetPath.StartsWith(dataPath))
                {
                    assetPath = "Assets" + assetPath.Substring(dataPath.Length);
                }
                else
                {
                    assetPath = Path.Combine("Assets", assetPath);
                }
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set up the request
                    client.BaseAddress = new Uri(ELEVENLABS_API_URL);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Add API key to headers
                    client.DefaultRequestHeaders.Add("xi-api-key", apiKey);

                    // Build the JSON directly
                    string jsonBody = $"{{\"text\":\"{EscapeJsonString(text)}\",\"model_id\":\"{modelId}\",\"voice_settings\":{{\"stability\":0.5,\"similarity_boost\":0.75}},\"apply_text_normalization\":\"{(textNormalization ? "on" : "off")}\"}}";

                    Debug.Log($"TTS Request Body: {jsonBody}");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    // Make the request
                    string requestUrl = $"/v1/text-to-speech/{voiceId}?output_format={outputFormat}";
                    HttpResponseMessage response = await client.PostAsync(requestUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Get the binary audio data
                        byte[] audioData = await response.Content.ReadAsByteArrayAsync();

                        // Save the file
                        File.WriteAllBytes(fullPath, audioData);

                        // Import the asset if it's in the project
                        if (!useExternalPath)
                        {
                            AssetDatabase.Refresh();
                        }

                        // Load the audio clip
                        AudioClip audioClip = null;

                        if (!useExternalPath)
                        {
                            // Load from project
                            AssetDatabase.Refresh();
                            audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

                            // If loading failed, try again after a delay
                            if (audioClip == null)
                            {
                                await Task.Delay(1000);
                                AssetDatabase.Refresh();
                                audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                            }
                        }
                        else
                        {
                            // Load from external path
                            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.UNKNOWN);
                            var operation = www.SendWebRequest();
                            while (!operation.isDone)
                            {
                                await Task.Delay(100);
                            }

                            if (www.result == UnityWebRequest.Result.Success)
                            {
                                audioClip = DownloadHandlerAudioClip.GetContent(www);
                            }
                            else
                            {
                                Debug.LogError($"Failed to load audio clip from {fullPath}: {www.error}");
                            } 

                            // Keep trying until the clip is ready
                            int attempts = 0;
                            while (audioClip.loadState != AudioDataLoadState.Loaded && attempts < 30)
                            {
                                await Task.Delay(100);
                                attempts++;
                            }
                        }

                        if (audioClip == null)
                        {
                            Debug.LogError("Failed to load generated audio clip.");
                            callback(null, false);
                            return;
                        }

                        // Create entry
                        CreatedSoundEntry entry = new CreatedSoundEntry
                        {
                            AudioClip = audioClip,
                            Prompt = text,
                            GenerationType = "TTS",
                            CreationDate = DateTime.Now,
                            ModelUsed = modelId,
                            AssetPath = useExternalPath ? fullPath : assetPath,
                            IsInTrash = false
                        };

                        // Return success
                        callback(entry, true);
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"Failed to generate text-to-speech. Status code: {response.StatusCode}");
                        Debug.LogError(responseContent);
                        callback(null, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating text-to-speech: {ex.Message}");
                callback(null, false);
            }
        }

        public static async void GenerateVoiceChange(
            string apiKey,
            AudioClip sourceClip,
            string voiceId,
            string modelId,
            string outputFormat,
            bool removeBackgroundNoise,
            Action<CreatedSoundEntry, bool> callback,
            string savePath,
            string externalSavePath,
            bool useExternalPath,
            string dataPath)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("API Key is not set.");
                callback(null, false);
                return;
            }

            if (sourceClip == null)
            {
                Debug.LogError("Source audio clip is null.");
                callback(null, false);
                return;
            }

            string actualSavePath = useExternalPath ? externalSavePath : savePath;

            // Ensure the directory exists
            try
            {
                if (!Directory.Exists(actualSavePath))
                {
                    Directory.CreateDirectory(actualSavePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create directory: {ex.Message}");
                callback(null, false);
                return;
            }

            // Generate a filename based on current date/time
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            // Use the source clip name for the filename
            string clipName = SanitizeForFilename(sourceClip.name);
            if (string.IsNullOrEmpty(clipName))
            {
                clipName = "VoiceChanged";
            }

            string fileName = $"{timestamp}_VC_{clipName}.mp3";
            string fullPath = Path.Combine(actualSavePath, fileName);

            // If path is within the project, make it relative to Assets
            string assetPath = fullPath;
            if (!useExternalPath && !assetPath.StartsWith("Assets"))
            {
                if (assetPath.StartsWith(dataPath))
                {
                    assetPath = "Assets" + assetPath.Substring(dataPath.Length);
                }
                else
                {
                    assetPath = Path.Combine("Assets", assetPath);
                }
            }

            try
            {
                // First, we need to convert the AudioClip to a WAV file
                string tempWavPath = Path.Combine(Path.GetTempPath(), $"temp_voice_source_{DateTime.Now.Ticks}.wav");

                // Convert AudioClip to WAV
                if (!AudioClipToWav(sourceClip, tempWavPath))
                {
                    Debug.LogError("Failed to convert AudioClip to WAV.");
                    callback(null, false);
                    return;
                }

                // Read the WAV file as bytes
                byte[] audioBytes = File.ReadAllBytes(tempWavPath);

                using (HttpClient client = new HttpClient())
                {
                    // Set up the request
                    client.BaseAddress = new Uri(ELEVENLABS_API_URL);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Add API key to headers
                    client.DefaultRequestHeaders.Add("xi-api-key", apiKey);

                    // Prepare the multipart form content
                    using (var formData = new MultipartFormDataContent())
                    {
                        // Add the audio file
                        var audioContent = new ByteArrayContent(audioBytes);
                        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                        formData.Add(audioContent, "audio", "input.wav");

                        // Add model ID
                        formData.Add(new StringContent(modelId), "model_id");

                        // Add noise removal parameter
                        formData.Add(new StringContent(removeBackgroundNoise.ToString().ToLower()), "remove_background_noise");

                        // Make the request
                        string requestUrl = $"/v1/speech-to-speech/{voiceId}?output_format={outputFormat}";
                        HttpResponseMessage response = await client.PostAsync(requestUrl, formData);

                        // Clean up temp file
                        try
                        {
                            File.Delete(tempWavPath);
                        }
                        catch { /* Ignore cleanup errors */ }

                        if (response.IsSuccessStatusCode)
                        {
                            // Get the binary audio data
                            byte[] resultAudioData = await response.Content.ReadAsByteArrayAsync();

                            // Save the file
                            File.WriteAllBytes(fullPath, resultAudioData);

                            // Import the asset if it's in the project
                            if (!useExternalPath)
                            {
                                AssetDatabase.Refresh();
                            }

                            // Load the audio clip
                            AudioClip audioClip = null;

                            if (!useExternalPath)
                            {
                                // Load from project
                                AssetDatabase.Refresh();
                                audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

                                // If loading failed, try again after a delay
                                if (audioClip == null)
                                {
                                    await Task.Delay(1000);
                                    AssetDatabase.Refresh();
                                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                                }
                            }
                            else
                            {
                                // Load from external path
                                UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.UNKNOWN);
                                var operation = www.SendWebRequest();
                                while (!operation.isDone)
                                {
                                    await Task.Delay(100);
                                }

                                if (www.result == UnityWebRequest.Result.Success)
                                {
                                    audioClip = DownloadHandlerAudioClip.GetContent(www);
                                }
                                else
                                {
                                    Debug.LogError($"Failed to load audio clip from {fullPath}: {www.error}");
                                } 

                                // Keep trying until the clip is ready
                                int attempts = 0;
                                while (audioClip.loadState != AudioDataLoadState.Loaded && attempts < 30)
                                {
                                    await Task.Delay(100);
                                    attempts++;
                                }
                            }

                            if (audioClip == null)
                            {
                                Debug.LogError("Failed to load generated audio clip.");
                                callback(null, false);
                                return;
                            }

                            // Create entry
                            CreatedSoundEntry entry = new CreatedSoundEntry
                            {
                                AudioClip = audioClip,
                                Prompt = $"Voice change from {sourceClip.name}",
                                GenerationType = "VC",
                                CreationDate = DateTime.Now,
                                ModelUsed = modelId,
                                AssetPath = useExternalPath ? fullPath : assetPath,
                                IsInTrash = false
                            };

                            // Return success
                            callback(entry, true);
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            Debug.LogError($"Failed to generate voice change. Status code: {response.StatusCode}");
                            Debug.LogError(responseContent);
                            callback(null, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating voice change: {ex.Message}");
                callback(null, false);
            }
        }

        private static string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";

            // Replace special characters with escaped versions
            return str
                .Replace("\\", "\\\\")  // Backslash
                .Replace("\"", "\\\"")  // Double quote
                .Replace("\n", "\\n")   // Newline
                .Replace("\r", "\\r")   // Carriage return
                .Replace("\t", "\\t")   // Tab
                .Replace("\b", "\\b")   // Backspace
                .Replace("\f", "\\f");  // Form feed
        }
        #endregion

        #region Helper Methods
        private static string SanitizeForFilename(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Untitled";

            // Remove invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(input);

            for (int i = 0; i < sb.Length; i++)
            {
                if (invalidChars.Contains(sb[i]))
                {
                    sb[i] = '_';
                }
            }

            // Replace spaces with underscores
            return sb.ToString().Replace(' ', '_');
        }

        private static bool AudioClipToWav(AudioClip clip, string path)
        {
            if (clip == null)
                return false;

            try
            {
                // Get audio data
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                // Convert float array to 16-bit PCM
                Int16[] intData = new Int16[samples.Length];
                for (int i = 0; i < samples.Length; i++)
                {
                    intData[i] = (short)(samples[i] * 32767);
                }

                // Create WAV file
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(fs))
                    {
                        // Write WAV header
                        int channelCount = clip.channels;
                        int sampleRate = clip.frequency;
                        int byteRate = sampleRate * channelCount * 2; // 2 bytes per sample
                        int blockAlign = channelCount * 2;
                        int bitsPerSample = 16;

                        // "RIFF" chunk
                        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                        writer.Write(36 + intData.Length * 2); // File size - 8
                        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                        // "fmt " chunk
                        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                        writer.Write(16); // Chunk size
                        writer.Write((short)1); // Audio format (1 = PCM)
                        writer.Write((short)channelCount);
                        writer.Write(sampleRate);
                        writer.Write(byteRate);
                        writer.Write((short)blockAlign);
                        writer.Write((short)bitsPerSample);

                        // "data" chunk
                        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                        writer.Write(intData.Length * 2); // Chunk size

                        // Write the PCM data
                        foreach (short s in intData)
                        {
                            writer.Write(s);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to convert AudioClip to WAV: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}