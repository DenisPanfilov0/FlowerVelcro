using System;
using System.IO;
using UnityEngine;

namespace UAI
{
    public static class WaveformGenerator
    {
        private static bool debugMode = false;

        private static void DebugLog(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[WaveformGenerator] {message}");
            }
        }

        public static float[] GenerateWaveformData(string filePath, int sampleCount = 60)
        {
            float[] waveformData = new float[sampleCount];

            DebugLog($"Generating waveform for: {filePath}");
            DebugLog($"File exists check: {File.Exists(filePath)}");

            try
            {
                if (!File.Exists(filePath))
                {
                    DebugLog($"File not found: {filePath}");
                    return GenerateFallbackWaveform(sampleCount, filePath.GetHashCode());
                }

                
                FileInfo fileInfo = new FileInfo(filePath);
                DebugLog($"File size: {fileInfo.Length} bytes");

                
                if (filePath.ToLower().EndsWith(".wav"))
                {
                    DebugLog("Detected WAV file, using PCM data reader");
                    float[] data = ReadWaveFile(filePath, sampleCount);
                    DebugLog($"WAV data read complete, first few samples: {string.Join(", ", data.Length > 5 ? new[] { data[0], data[1], data[2], data[3], data[4] } : data)}");
                    return data;
                }

                
                DebugLog($"Using compressed audio reader for format: {Path.GetExtension(filePath)}");
                float[] compressedData = ReadCompressedAudioFile(filePath, sampleCount);
                DebugLog($"Compressed data read complete, first few samples: {string.Join(", ", compressedData.Length > 5 ? new[] { compressedData[0], compressedData[1], compressedData[2], compressedData[3], compressedData[4] } : compressedData)}");
                return compressedData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating waveform data: {ex.Message}\nStack trace: {ex.StackTrace}");
                return GenerateFallbackWaveform(sampleCount, filePath.GetHashCode());
            }
        }

        
        private static float[] ReadWaveFile(string filePath, int sampleCount)
        {
            float[] result = new float[sampleCount];

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                DebugLog($"Read {fileBytes.Length} bytes from WAV file");

                
                
                int headerSize = 44; 

                
                int channels = BitConverter.ToInt16(fileBytes, 22);
                int sampleRate = BitConverter.ToInt32(fileBytes, 24);
                int bitsPerSample = BitConverter.ToInt16(fileBytes, 34);

                DebugLog($"WAV header: Channels={channels}, Sample Rate={sampleRate}, Bits Per Sample={bitsPerSample}");

                int dataStart = headerSize;

                
                for (int i = 0; i < fileBytes.Length - 4; i++)
                {
                    if (fileBytes[i] == 'd' && fileBytes[i + 1] == 'a' &&
                        fileBytes[i + 2] == 't' && fileBytes[i + 3] == 'a')
                    {
                        dataStart = i + 8; 
                        DebugLog($"Found data chunk at byte position {i}, data starts at {dataStart}");
                        break;
                    }
                }

                DebugLog($"Data starts at byte position: {dataStart}");

                
                int bytesPerSample = bitsPerSample / 8;
                int bytesPerFrame = bytesPerSample * channels;

                DebugLog($"Bytes per sample: {bytesPerSample}, Bytes per frame: {bytesPerFrame}");

                
                int dataSize = fileBytes.Length - dataStart;
                int totalFrames = dataSize / bytesPerFrame;

                DebugLog($"Data size: {dataSize} bytes, Total frames: {totalFrames}");

                
                for (int i = 0; i < sampleCount; i++)
                {
                    
                    int framePosition = (int)((float)i / sampleCount * totalFrames);
                    framePosition = Math.Min(framePosition, totalFrames - 1);

                    
                    int bytePosition = dataStart + (framePosition * bytesPerFrame);

                    if (i == 0 || i == sampleCount / 2 || i == sampleCount - 1)
                    {
                        DebugLog($"Sample {i}: Frame pos={framePosition}, Byte pos={bytePosition}");
                    }

                    
                    float sampleValue = 0;

                    if (bytePosition >= fileBytes.Length)
                    {
                        DebugLog($"Warning: Byte position {bytePosition} exceeds file length {fileBytes.Length}");
                        continue;
                    }

                    if (bitsPerSample == 16)
                    {
                        
                        if (bytePosition + 1 < fileBytes.Length)
                        {
                            short value = BitConverter.ToInt16(fileBytes, bytePosition);
                            sampleValue = Math.Abs(value / 32768f); 

                            if (i == 0 || i == sampleCount / 2 || i == sampleCount - 1)
                            {
                                DebugLog($"Sample {i}: Raw value={value}, Normalized={sampleValue}");
                            }
                        }
                    }
                    else if (bitsPerSample == 8)
                    {
                        
                        if (bytePosition < fileBytes.Length)
                        {
                            byte value = fileBytes[bytePosition];
                            sampleValue = Math.Abs((value - 128) / 128f); 

                            if (i == 0 || i == sampleCount / 2 || i == sampleCount - 1)
                            {
                                DebugLog($"Sample {i}: Raw value={value}, Normalized={sampleValue}");
                            }
                        }
                    }
                    else if (bitsPerSample == 24)
                    {
                        
                        if (bytePosition + 2 < fileBytes.Length)
                        {
                            int value = (fileBytes[bytePosition] << 8) |
                                        (fileBytes[bytePosition + 1] << 16) |
                                        (fileBytes[bytePosition + 2] << 24);
                            value = value >> 8; 
                            sampleValue = Math.Abs(value / 8388608f); 

                            if (i == 0 || i == sampleCount / 2 || i == sampleCount - 1)
                            {
                                DebugLog($"Sample {i}: Raw value={value}, Normalized={sampleValue}");
                            }
                        }
                    }
                    else if (bitsPerSample == 32)
                    {
                        
                        if (bytePosition + 3 < fileBytes.Length)
                        {
                            try
                            {
                                float value = BitConverter.ToSingle(fileBytes, bytePosition);
                                sampleValue = Math.Abs(value); 

                                if (i == 0 || i == sampleCount / 2 || i == sampleCount - 1)
                                {
                                    DebugLog($"Sample {i}: Raw float value={value}, Normalized={sampleValue}");
                                }
                            }
                            catch
                            {
                                
                                int value = BitConverter.ToInt32(fileBytes, bytePosition);
                                sampleValue = Math.Abs(value / 2147483648f); 

                                if (i == 0 || i == sampleCount / 2 || i == sampleCount - 1)
                                {
                                    DebugLog($"Sample {i}: Raw int value={value}, Normalized={sampleValue}");
                                }
                            }
                        }
                    }

                    
                    result[i] = Mathf.Clamp01(sampleValue);

                    
                    if (result[i] < 0.05f) result[i] = 0.05f;
                }

                
                float maxAmp = 0;
                for (int i = 0; i < result.Length; i++)
                {
                    maxAmp = Mathf.Max(maxAmp, result[i]);
                }

                DebugLog($"Maximum amplitude in result: {maxAmp}");

                
                if (maxAmp < 0.1f)
                {
                    DebugLog("Low amplitude detected, normalizing range");
                    
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = 0.05f + (result[i] / maxAmp) * 0.7f;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading WAV file: {ex.Message}\nStack trace: {ex.StackTrace}");
                return GenerateFallbackWaveform(sampleCount, filePath.GetHashCode());
            }

            return result;
        }

        
        private static float[] ReadCompressedAudioFile(string filePath, int sampleCount)
        {
            float[] result = new float[sampleCount];

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                DebugLog($"Read {fileBytes.Length} bytes from compressed audio file");

                
                if (filePath.ToLower().EndsWith(".mp3"))
                {
                    DebugLog("Using MP3-specific amplitude estimation");

                    
                    

                    int fileSize = fileBytes.Length;
                    int sectionsToAnalyze = sampleCount;
                    int bytesPerSection = fileSize / sectionsToAnalyze;

                    DebugLog($"Dividing {fileSize} bytes into {sectionsToAnalyze} sections of {bytesPerSection} bytes each");

                    int frameHeadersFound = 0;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        int sectionStart = i * bytesPerSection;
                        int sectionEnd = Math.Min(sectionStart + bytesPerSection, fileSize);

                        DebugLog($"Section {i}: Analyzing bytes {sectionStart} to {sectionEnd}");

                        
                        float maxAmp = 0;
                        bool foundFrameInSection = false;

                        for (int pos = sectionStart; pos < sectionEnd - 4; pos++)
                        {
                            
                            if ((fileBytes[pos] == 0xFF) && ((fileBytes[pos + 1] & 0xE0) == 0xE0))
                            {
                                foundFrameInSection = true;
                                frameHeadersFound++;

                                byte b1 = fileBytes[pos + 2];
                                byte b2 = fileBytes[pos + 3];

                                
                                float amp = ((b1 & 0x0F) + (b2 & 0xF0)) / 255f;
                                maxAmp = Math.Max(maxAmp, amp);

                                if (i == 0 || i == sampleCount / 2 || i == sampleCount - 1)
                                {
                                    DebugLog($"Found frame header at pos {pos}, bytes: {fileBytes[pos]:X2} {fileBytes[pos + 1]:X2} {b1:X2} {b2:X2}, amp: {amp}");
                                }
                            }
                        }

                        
                        if (!foundFrameInSection)
                        {
                            
                            byte prevByte = 0;
                            int changes = 0;
                            float avgChange = 0;

                            for (int pos = sectionStart; pos < sectionEnd; pos++)
                            {
                                byte curByte = fileBytes[pos];
                                if (pos > sectionStart)
                                {
                                    int diff = Math.Abs(curByte - prevByte);
                                    if (diff > 10)
                                    {
                                        changes++;
                                        avgChange += diff;
                                    }
                                }
                                prevByte = curByte;
                            }

                            if (changes > 0)
                            {
                                avgChange /= changes;
                                maxAmp = Math.Min(avgChange / 128f, 1.0f);

                                DebugLog($"Section {i}: No frames, using pattern analysis. Changes: {changes}, Avg: {avgChange}, amp: {maxAmp}");
                            }
                        }

                        
                        result[i] = Mathf.Clamp01(maxAmp * 0.8f + 0.2f);
                    }

                    DebugLog($"MP3 analysis complete. Found {frameHeadersFound} potential frame headers.");
                }
                else
                {
                    DebugLog($"Using generic byte-pattern analysis for {Path.GetExtension(filePath)}");

                    
                    
                    int fileSize = fileBytes.Length;
                    int sectionsToAnalyze = sampleCount;
                    int bytesPerSection = fileSize / sectionsToAnalyze;

                    DebugLog($"Dividing {fileSize} bytes into {sectionsToAnalyze} sections of {bytesPerSection} bytes each");

                    
                    float[] sectionValues = new float[sampleCount];

                    for (int i = 0; i < sampleCount; i++)
                    {
                        int sectionStart = i * bytesPerSection;
                        int sectionEnd = Math.Min(sectionStart + bytesPerSection, fileSize);

                        
                        byte[] sectionBytes = new byte[sectionEnd - sectionStart];
                        Array.Copy(fileBytes, sectionStart, sectionBytes, 0, sectionEnd - sectionStart);

                        
                        double mean = 0;
                        foreach (byte b in sectionBytes)
                        {
                            mean += b;
                        }
                        mean /= sectionBytes.Length;

                        double variance = 0;
                        foreach (byte b in sectionBytes)
                        {
                            variance += Math.Pow(b - mean, 2);
                        }
                        variance /= sectionBytes.Length;

                        
                        float stdDev = (float)Math.Sqrt(variance);
                        float amp = Mathf.Clamp01(stdDev / 64f); 

                        sectionValues[i] = amp;

                        if (i == 0 || i == sampleCount / 2 || i == sampleCount - 1)
                        {
                            DebugLog($"Section {i}: Mean={mean}, Variance={variance}, StdDev={stdDev}, Amp={amp}");
                        }
                    }

                    
                    float minVal = 1f;
                    float maxVal = 0f;
                    float avgVal = 0f;

                    foreach (float val in sectionValues)
                    {
                        minVal = Mathf.Min(minVal, val);
                        maxVal = Mathf.Max(maxVal, val);
                        avgVal += val;
                    }
                    avgVal /= sectionValues.Length;

                    DebugLog($"Distribution: Min={minVal}, Max={maxVal}, Avg={avgVal}");

                    
                    float range = maxVal - minVal;
                    if (range < 0.3f)
                    {
                        DebugLog($"Range too small ({range}), enhancing contrast");

                        
                        for (int i = 0; i < sampleCount; i++)
                        {
                            
                            float normalized = (sectionValues[i] - minVal) / (range > 0 ? range : 1);
                            sectionValues[i] = normalized;
                        }
                    }

                    
                    for (int i = 0; i < sampleCount; i++)
                    {
                        result[i] = 0.1f + sectionValues[i] * 0.8f;
                    }
                }

                
                bool allZero = true;
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] > 0.05f)
                    {
                        allZero = false;
                        break;
                    }
                }

                if (allZero)
                {
                    DebugLog("All values near zero, using fallback waveform instead");
                    return GenerateFallbackWaveform(sampleCount, filePath.GetHashCode());
                }

                DebugLog($"Final waveform first few values: {string.Join(", ", result.Length > 5 ? new[] { result[0], result[1], result[2], result[3], result[4] } : result)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading compressed audio file: {ex.Message}\nStack trace: {ex.StackTrace}");
                return GenerateFallbackWaveform(sampleCount, filePath.GetHashCode());
            }

            return result;
        }

        
        private static float[] GenerateFallbackWaveform(int sampleCount, int seed)
        {
            DebugLog($"Generating fallback waveform with seed {seed}");

            float[] result = new float[sampleCount];
            UnityEngine.Random.State previousState = UnityEngine.Random.state;

            
            UnityEngine.Random.InitState(seed);

            
            for (int i = 0; i < sampleCount; i++)
            {
                float phase = (float)i / sampleCount;
                float amp1 = Mathf.Sin(phase * 2 * Mathf.PI * (2.0f + UnityEngine.Random.value));
                float amp2 = Mathf.Sin(phase * 2 * Mathf.PI * (4.0f + UnityEngine.Random.value * 2)) * 0.5f;
                float amp3 = Mathf.Sin(phase * 2 * Mathf.PI * (6.0f + UnityEngine.Random.value * 3)) * 0.25f;

                
                result[i] = Mathf.Clamp01((Mathf.Abs(amp1 + amp2 + amp3) / 1.75f) * 0.8f);

                
                float edgeFade = Mathf.Min(phase * 4, (1 - phase) * 4, 1.0f);
                result[i] *= edgeFade;

                
                result[i] = Mathf.Max(result[i], 0.08f);
            }

            
            for (int i = 0; i < sampleCount; i++)
            {
                result[i] *= (0.8f + 0.4f * UnityEngine.Random.value);
            }

            
            UnityEngine.Random.state = previousState;

            DebugLog($"Fallback waveform first few values: {string.Join(", ", result.Length > 5 ? new[] { result[0], result[1], result[2], result[3], result[4] } : result)}");

            return result;
        }
    }
}