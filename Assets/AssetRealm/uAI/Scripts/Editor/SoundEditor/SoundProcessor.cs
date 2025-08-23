using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UAI
{
    public class SoundProcessor
    {
        #region Audio Processing Methods
        public static AudioClip NormalizeAudio(AudioClip sourceClip, float targetLevel = 0.95f)
        {
            if (sourceClip == null) return null;
            
            // Get the audio data
            float[] samples = new float[sourceClip.samples * sourceClip.channels];
            sourceClip.GetData(samples, 0);
            
            // Find the peak amplitude
            float peakAmplitude = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float absoluteValue = Mathf.Abs(samples[i]);
                if (absoluteValue > peakAmplitude)
                {
                    peakAmplitude = absoluteValue;
                }
            }
            
            // Calculate normalization factor
            float normalizationFactor = 1f;
            if (peakAmplitude > 0)
            {
                normalizationFactor = targetLevel / peakAmplitude;
            }
            
            // Apply normalization to all samples
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= normalizationFactor;
            }
            
            // Create a new clip with the normalized data
            AudioClip normalizedClip = AudioClip.Create(
                sourceClip.name + "_normalized",
                sourceClip.samples,
                sourceClip.channels,
                sourceClip.frequency,
                false
            );
            
            normalizedClip.SetData(samples, 0);
            return normalizedClip;
        }
        
        public static AudioClip ApplyFades(AudioClip sourceClip, float fadeInDuration = 0.1f, float fadeOutDuration = 0.3f)
        {
            if (sourceClip == null) return null;
            
            // Get the audio data
            float[] samples = new float[sourceClip.samples * sourceClip.channels];
            sourceClip.GetData(samples, 0);
            
            // Calculate fade sample counts
            int fadeInSamples = Mathf.FloorToInt(fadeInDuration * sourceClip.frequency * sourceClip.channels);
            int fadeOutSamples = Mathf.FloorToInt(fadeOutDuration * sourceClip.frequency * sourceClip.channels);
            
            // Apply fade in
            for (int i = 0; i < fadeInSamples && i < samples.Length; i += sourceClip.channels)
            {
                float fadeMultiplier = (float)i / fadeInSamples;
                
                for (int c = 0; c < sourceClip.channels; c++)
                {
                    if (i + c < samples.Length)
                        samples[i + c] *= fadeMultiplier;
                }
            }
            
            // Apply fade out
            int fadeOutStart = samples.Length - fadeOutSamples;
            for (int i = fadeOutStart; i < samples.Length; i += sourceClip.channels)
            {
                float fadeMultiplier = (float)(samples.Length - i) / fadeOutSamples;
                
                for (int c = 0; c < sourceClip.channels; c++)
                {
                    if (i + c < samples.Length)
                        samples[i + c] *= fadeMultiplier;
                }
            }
            
            // Create a new clip with the faded data
            AudioClip fadedClip = AudioClip.Create(
                sourceClip.name + "_faded",
                sourceClip.samples,
                sourceClip.channels,
                sourceClip.frequency,
                false
            );
            
            fadedClip.SetData(samples, 0);
            return fadedClip;
        }
        
        public static AudioClip TrimSilence(AudioClip sourceClip, float threshold = 0.01f)
        {
            if (sourceClip == null) return null;
            
            // Get the audio data
            float[] samples = new float[sourceClip.samples * sourceClip.channels];
            sourceClip.GetData(samples, 0);
            
            // Find start index (first non-silent sample)
            int startIndex = 0;
            for (int i = 0; i < samples.Length; i += sourceClip.channels)
            {
                bool isSilent = true;
                for (int c = 0; c < sourceClip.channels; c++)
                {
                    if (Mathf.Abs(samples[i + c]) > threshold)
                    {
                        isSilent = false;
                        break;
                    }
                }
                
                if (!isSilent)
                {
                    startIndex = i;
                    break;
                }
            }
            
            // Find end index (last non-silent sample)
            int endIndex = samples.Length - 1;
            for (int i = samples.Length - sourceClip.channels; i >= 0; i -= sourceClip.channels)
            {
                bool isSilent = true;
                for (int c = 0; c < sourceClip.channels; c++)
                {
                    if (Mathf.Abs(samples[i + c]) > threshold)
                    {
                        isSilent = false;
                        break;
                    }
                }
                
                if (!isSilent)
                {
                    endIndex = i + sourceClip.channels - 1;
                    break;
                }
            }
            
            // Calculate the new sample count
            int newSampleCount = (endIndex - startIndex + 1) / sourceClip.channels;
            if (newSampleCount <= 0)
                return sourceClip; // No non-silent samples found
                
            // Create a new array for the trimmed data
            float[] trimmedSamples = new float[newSampleCount * sourceClip.channels];
            Array.Copy(samples, startIndex, trimmedSamples, 0, trimmedSamples.Length);
            
            // Create a new clip with the trimmed data
            AudioClip trimmedClip = AudioClip.Create(
                sourceClip.name + "_trimmed",
                newSampleCount,
                sourceClip.channels,
                sourceClip.frequency,
                false
            );
            
            trimmedClip.SetData(trimmedSamples, 0);
            return trimmedClip;
        }
        
        public static AudioClip CreateSeamlessLoop(AudioClip sourceClip, float crossfadeDuration = 0.1f)
        {
            if (sourceClip == null) return null;

            float[] samples = new float[sourceClip.samples * sourceClip.channels];
            sourceClip.GetData(samples, 0);

            int frequency = sourceClip.frequency;
            int channels = sourceClip.channels;
            int totalSampleFrames = sourceClip.samples; // This is samples per channel

            // Calculate crossfade length in sample frames
            int crossfadeSampleFrames = Mathf.FloorToInt(crossfadeDuration * frequency);

            // Validate crossfadeSampleFrames
            if (crossfadeSampleFrames <= 0)
            {
                Debug.LogWarning("Crossfade duration is too short or zero. Skipping seamless loop processing.");
                return sourceClip; 
            }
            if (totalSampleFrames < 2) // Not enough samples for a meaningful crossfade
            {
                Debug.LogWarning("Source clip is too short for seamless loop processing.");
                return sourceClip;
            }
            // Prevent crossfade from being longer than half the clip's length (or whole clip if it's just 1 sample long after division)
            crossfadeSampleFrames = Mathf.Min(crossfadeSampleFrames, totalSampleFrames / 2);
            if (crossfadeSampleFrames == 0 && totalSampleFrames >=1 ) crossfadeSampleFrames = 1; // Ensure at least 1 frame if possible and duration > 0
            if (crossfadeSampleFrames == 0) return sourceClip; // Still no frames to crossfade

            int crossfadeSamplesInterleaved = crossfadeSampleFrames * channels;

            // Iterate over each frame in the crossfade region
            for (int frame = 0; frame < crossfadeSampleFrames; frame++)
            {
                float fadeInMultiplier;
                float fadeOutMultiplier;

                if (crossfadeSampleFrames == 1)
                {
                    // For a single frame crossfade, typically an equal mix
                    fadeInMultiplier = 0.5f;
                    fadeOutMultiplier = 0.5f;
                }
                else
                {
                    // Linear fade:
                    // fadeInMultiplier for the original head segment (goes 0 to 1)
                    fadeInMultiplier = (float)frame / (crossfadeSampleFrames - 1);
                    // fadeOutMultiplier for the original tail segment (goes 1 to 0)
                    fadeOutMultiplier = 1.0f - fadeInMultiplier;
                }

                for (int c = 0; c < channels; c++)
                {
                    // Index for the current sample in the head section
                    int headSampleIndex = frame * channels + c;
                    
                    // Index for the corresponding sample in the tail section 
                    int tailSampleOriginalFrame = totalSampleFrames - crossfadeSampleFrames + frame;
                    int tailSampleIndex = tailSampleOriginalFrame * channels + c;

                    // Safety checks, though clamping should mostly prevent issues
                    if (headSampleIndex >= samples.Length || tailSampleIndex >= samples.Length)
                    {
                        Debug.LogError($"Sample index out of bounds during crossfade. Head: {headSampleIndex}, Tail: {tailSampleIndex}, Max: {samples.Length-1}");
                        continue;
                    }

                    float headSample = samples[headSampleIndex]; // Original sample from the start of the clip
                    float tailSample = samples[tailSampleIndex]; // Original sample from the end of the clip

                    // New sample at the start of the clip is a mix
                    samples[headSampleIndex] = (headSample * fadeInMultiplier) + (tailSample * fadeOutMultiplier);
                }
            }

            AudioClip loopedClip = AudioClip.Create(
                sourceClip.name + "_looped",
                totalSampleFrames, // Use totalSampleFrames (samples per channel)
                channels,
                frequency,
                false
            );

            loopedClip.SetData(samples, 0);
            return loopedClip;
        }

        public static AudioClip ChangePitch(AudioClip sourceClip, float pitchFactor)
        {
            if (sourceClip == null) return null;
            
            // Get the audio data
            float[] samples = new float[sourceClip.samples * sourceClip.channels];
            sourceClip.GetData(samples, 0);
            
            // Calculate new frequency
            int newFrequency = Mathf.RoundToInt(sourceClip.frequency * pitchFactor);
            
            // Create a new clip with the pitch-shifted data
            AudioClip pitchedClip = AudioClip.Create(
                sourceClip.name + "_pitched",
                sourceClip.samples,
                sourceClip.channels,
                newFrequency,
                false
            );
            
            pitchedClip.SetData(samples, 0);
            return pitchedClip;
        }
        
        public static AudioClip ApplyLowPassFilter(AudioClip sourceClip, float cutoffFrequency = 0.5f)
        {
            if (sourceClip == null) return null;
            
            // Get the audio data
            float[] samples = new float[sourceClip.samples * sourceClip.channels];
            sourceClip.GetData(samples, 0);
            
            // Simple one-pole filter coefficient
            float alpha = cutoffFrequency;
            if (alpha < 0.01f) alpha = 0.01f;
            if (alpha > 0.99f) alpha = 0.99f;
            
            // Apply the filter (separately for each channel)
            for (int c = 0; c < sourceClip.channels; c++)
            {
                float lastSample = 0f;
                
                for (int i = c; i < samples.Length; i += sourceClip.channels)
                {
                    // Low-pass filter formula: y[n] = alpha * x[n] + (1-alpha) * y[n-1]
                    float filteredSample = alpha * samples[i] + (1f - alpha) * lastSample;
                    samples[i] = filteredSample;
                    lastSample = filteredSample;
                }
            }
            
            // Create a new clip with the filtered data
            AudioClip filteredClip = AudioClip.Create(
                sourceClip.name + "_filtered",
                sourceClip.samples,
                sourceClip.channels,
                sourceClip.frequency,
                false
            );
            
            filteredClip.SetData(samples, 0);
            return filteredClip;
        }
        #endregion
        
        #region Batch Processing
        public static List<CreatedSoundEntry> BatchProcess(
            List<CreatedSoundEntry> entries,
            bool normalize = false,
            bool trimSilence = false,
            bool applyFades = false,
            bool makeLoopable = false,
            float pitchShift = 1.0f,
            string outputFolder = null,
            float loopCrossfadeDuration = 0.1f)
        {
            List<CreatedSoundEntry> processedEntries = new List<CreatedSoundEntry>();
            
            foreach (var entry in entries)
            {
                if (entry.AudioClip == null) continue;
                
                AudioClip processedClip = entry.AudioClip;
                string suffix = "_processed";
                
                // Apply the requested processing operations
                if (trimSilence)
                {
                    processedClip = TrimSilence(processedClip);
                    suffix += "_trim";
                }
                
                if (normalize)
                {
                    processedClip = NormalizeAudio(processedClip);
                    suffix += "_norm";
                }
                
                if (applyFades)
                {
                    processedClip = ApplyFades(processedClip);
                    suffix += "_fade";
                }
                
                if (Mathf.Abs(pitchShift - 1.0f) > 0.01f)
                {
                    processedClip = ChangePitch(processedClip, pitchShift);
                    suffix += $"_pitch{pitchShift:F1}";
                }
                
                if (makeLoopable)
                {
                    // Use the passed-in loopCrossfadeDuration
                    processedClip = CreateSeamlessLoop(processedClip, loopCrossfadeDuration); 
                    suffix += "_loop";
                }
                
                // Save the processed clip
                string savePath = entry.AssetPath;
                
                if (!string.IsNullOrEmpty(outputFolder))
                {
                    // Use the specified output folder
                    string fileName = Path.GetFileNameWithoutExtension(entry.AssetPath) + suffix + Path.GetExtension(entry.AssetPath);
                    savePath = Path.Combine(outputFolder, fileName);
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                }
                else
                {
                    // Use the original path with a suffix
                    string directory = Path.GetDirectoryName(entry.AssetPath);
                    string fileName = Path.GetFileNameWithoutExtension(entry.AssetPath) + suffix + Path.GetExtension(entry.AssetPath);
                    savePath = Path.Combine(directory, fileName);
                }
                
                // Determine if the path is within the project
                bool isInProject = savePath.StartsWith("Assets");
                
                if (isInProject)
                {
                    // Save to project asset
                    if (savePath.EndsWith(".wav"))
                    {
                        SavWav.Save(savePath, processedClip);
                    }
                    else
                    {
                        // Default to WAV if not MP3
                        string wavPath = Path.ChangeExtension(savePath, "wav");
                        SavWav.Save(wavPath, processedClip);
                        savePath = wavPath;
                    }
                    
                    AssetDatabase.Refresh();
                    processedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(savePath);
                }
                else
                {
                    // Save to external location
                    if (savePath.EndsWith(".wav"))
                    {
                        SavWav.Save(savePath, processedClip);
                    }
                    else
                    {
                        // Default to WAV if not MP3
                        savePath = Path.ChangeExtension(savePath, "wav");
                        SavWav.Save(savePath, processedClip);
                    }
                }
                
                // Create a new entry for the processed sound
                CreatedSoundEntry processedEntry = new CreatedSoundEntry
                {
                    AudioClip = processedClip,
                    Prompt = entry.Prompt + " (Processed)",
                    GenerationType = entry.GenerationType,
                    CreationDate = DateTime.Now,
                    ModelUsed = entry.ModelUsed,
                    AssetPath = savePath,
                    IsInTrash = false
                };
                
                processedEntries.Add(processedEntry);
            }
            
            return processedEntries;
        }
        #endregion
        
        #region Game Audio Utilities
        public static void CreateSoundBank(List<CreatedSoundEntry> entries, string bankName, string outputFolder)
        {
            if (entries == null || entries.Count == 0) return;
            
            // Ensure output directory exists
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            
            // Create a new SoundBank ScriptableObject
            SoundBank soundBank = ScriptableObject.CreateInstance<SoundBank>();
            soundBank.bankName = bankName;
            
            // Add all clips to the bank
            foreach (var entry in entries)
            {
                if (entry.AudioClip != null)
                {
                    SoundBankEntry bankEntry = new SoundBankEntry
                    {
                        clip = entry.AudioClip,
                        name = string.IsNullOrEmpty(entry.AudioClip.name) ? entry.Prompt : entry.AudioClip.name,
                        volume = 1.0f,
                        pitch = 1.0f,
                        description = entry.Prompt
                    };
                    
                    soundBank.entries.Add(bankEntry);
                }
            }
            
            // Save the sound bank asset
            string assetPath = Path.Combine(outputFolder, $"{bankName}.asset");
            AssetDatabase.CreateAsset(soundBank, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Debug.Log($"Created sound bank at {assetPath} with {soundBank.entries.Count} sounds.");
        }
        
        public static RandomizedSoundSet CreateRandomizedSoundSet(List<CreatedSoundEntry> entries, string setName, string outputFolder)
        {
            if (entries == null || entries.Count == 0) return null;
            
            // Ensure output directory exists
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            
            // Create a new RandomizedSoundSet ScriptableObject
            RandomizedSoundSet soundSet = ScriptableObject.CreateInstance<RandomizedSoundSet>();
            soundSet.setName = setName;
            
            // Add all clips to the set
            foreach (var entry in entries)
            {
                if (entry.AudioClip != null)
                {
                    soundSet.clips.Add(entry.AudioClip);
                }
            }
            
            // Save the sound set asset
            string assetPath = Path.Combine(outputFolder, $"{setName}.asset");
            AssetDatabase.CreateAsset(soundSet, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Debug.Log($"Created randomized sound set at {assetPath} with {soundSet.clips.Count} variations.");
            
            return soundSet;
        }
        
        public static void OptimizeAudioImportSettings(List<CreatedSoundEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.AudioClip == null || string.IsNullOrEmpty(entry.AssetPath)) continue;
                
                // Skip if not in project
                if (!entry.AssetPath.StartsWith("Assets")) continue;
                
                AudioImporter importer = AssetImporter.GetAtPath(entry.AssetPath) as AudioImporter;
                if (importer == null) continue;
                
                // Determine settings based on sound type
                AudioImporterSampleSettings settings = new AudioImporterSampleSettings();
                
                switch (entry.GenerationType.ToLower())
                {
                    case "sfx":
                        // Sound effects are typically compressed and loaded at runtime
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        settings.compressionFormat = AudioCompressionFormat.Vorbis;
                        settings.quality = 0.6f; // Medium quality
                        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                        break;
                        
                    case "tts":
                        // Voice lines require higher quality
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        settings.compressionFormat = AudioCompressionFormat.Vorbis;
                        settings.quality = 0.8f; // Higher quality for voices
                        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                        break;
                        
                    case "vc":
                        // Voice lines require higher quality
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        settings.compressionFormat = AudioCompressionFormat.Vorbis;
                        settings.quality = 0.8f; // Higher quality for voices
                        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                        break;
                        
                    default:
                        // Default settings
                        settings.loadType = AudioClipLoadType.DecompressOnLoad;
                        settings.compressionFormat = AudioCompressionFormat.Vorbis;
                        settings.quality = 0.7f;
                        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                        break;
                }
                
                // Apply the settings
                importer.defaultSampleSettings = settings;
                
                // Force ambisonic to off for all clips (unless specifically needed)
                var platformSettings = importer.GetOverrideSampleSettings("Standalone");
                platformSettings.loadType = settings.loadType;
                platformSettings.compressionFormat = settings.compressionFormat;
                platformSettings.quality = settings.quality;
                platformSettings.sampleRateSetting = settings.sampleRateSetting;
                importer.SetOverrideSampleSettings("Standalone", platformSettings);
                
                // Apply changes
                importer.SaveAndReimport();
            }
        }
        #endregion
    }
    
    #region Audio Asset Types
    public class SoundBank : ScriptableObject
    {
        public string bankName;
        public List<SoundBankEntry> entries = new List<SoundBankEntry>();
        
        public AudioClip GetClipByName(string name)
        {
            var entry = entries.Find(e => e.name == name);
            return entry?.clip;
        }
        
        public SoundBankEntry GetEntryByName(string name)
        {
            return entries.Find(e => e.name == name);
        }
    }
    
    [Serializable]
    public class SoundBankEntry
    {
        public string name;
        public AudioClip clip;
        public float volume = 1.0f;
        public float pitch = 1.0f;
        public string description;
    }
    
    public class RandomizedSoundSet : ScriptableObject
    {
        public string setName;
        public List<AudioClip> clips = new List<AudioClip>();
        public bool preventRepeat = true;
        
        private int lastIndex = -1;
        
        public AudioClip GetRandomClip()
        {
            if (clips.Count == 0) return null;
            if (clips.Count == 1) return clips[0];
            
            int index;
            if (preventRepeat)
            {
                do
                {
                    index = UnityEngine.Random.Range(0, clips.Count);
                } while (index == lastIndex && clips.Count > 1);
                
                lastIndex = index;
            }
            else
            {
                index = UnityEngine.Random.Range(0, clips.Count);
            }
            
            return clips[index];
        }
    }
    #endregion
    
    #region WAV Utility
    // Class for saving AudioClips as WAV files
    // Based on https://gist.github.com/darktable/2317063
    public static class SavWav
    {
        const int HEADER_SIZE = 44;
        
        public static bool Save(string path, AudioClip clip)
        {
            if (!path.ToLower().EndsWith(".wav"))
            {
                path = path + ".wav";
            }
            
            // Make sure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            
            using (var fileStream = CreateEmpty(path))
            {
                ConvertAndWrite(fileStream, clip);
                WriteHeader(fileStream, clip);
            }
            
            return true;
        }
        
        static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();
            
            for (int i = 0; i < HEADER_SIZE; i++)
            {
                fileStream.WriteByte(emptyByte);
            }
            
            return fileStream;
        }
        
        static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            
            // Convert to 16-bit PCM
            Int16[] intData = new Int16[samples.Length];
            
            // Convert floating point samples to 16-bit integers
            Byte[] bytesData = new Byte[samples.Length * 2];
            int rescaleFactor = 32767; // Convert float to Int16
            
            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                Byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }
            
            fileStream.Write(bytesData, 0, bytesData.Length);
        }
        
        static void WriteHeader(FileStream fileStream, AudioClip clip)
        {
            int hz = clip.frequency;
            int channels = clip.channels;
            int samples = clip.samples;
            
            fileStream.Seek(0, SeekOrigin.Begin);
            
            // RIFF chunk
            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);
            
            // File size
            Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);
            
            // WAVE format
            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);
            
            // fmt chunk
            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);
            
            // Chunk size
            Byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);
            
            // Audio format (PCM = 1)
            UInt16 audioFormat = 1;
            Byte[] audioFormatBytes = BitConverter.GetBytes(audioFormat);
            fileStream.Write(audioFormatBytes, 0, 2);
            
            // Channels
            UInt16 numChannels = (UInt16)channels;
            Byte[] numChannelsBytes = BitConverter.GetBytes(numChannels);
            fileStream.Write(numChannelsBytes, 0, 2);
            
            // Sample rate
            Byte[] sampleRateBytes = BitConverter.GetBytes(hz);
            fileStream.Write(sampleRateBytes, 0, 4);
            
            // Byte rate
            Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * channels * bytesPerSample
            fileStream.Write(byteRate, 0, 4);
            
            // Block align
            UInt16 blockAlign = (UInt16)(channels * 2);
            Byte[] blockAlignBytes = BitConverter.GetBytes(blockAlign);
            fileStream.Write(blockAlignBytes, 0, 2);
            
            // Bits per sample
            UInt16 bitsPerSample = 16;
            Byte[] bitsPerSampleBytes = BitConverter.GetBytes(bitsPerSample);
            fileStream.Write(bitsPerSampleBytes, 0, 2);
            
            // data chunk
            Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(dataString, 0, 4);
            
            // Data chunk size
            Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
            
            fileStream.Close();
        }
    }
    #endregion
}