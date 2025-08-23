using System; 
using UnityEngine; 

namespace UAI
{
    [System.Serializable] 
    public class AudioRegion
    {
        public Guid Id { get; private set; }
        public float[] AudioData { get; set; }
        public float TimelineStartTime { get; set; } 
        public float Duration { get; set; }
        public int OriginalChannels { get; set; }
        public int OriginalFrequency { get; set; }

        public float[] WaveformData { get; set; } 

        public AudioRegion(float[] audioData, float timelineStartTime, int channels, int frequency)
        {
            Id = Guid.NewGuid();
            AudioData = audioData;
            TimelineStartTime = timelineStartTime;
            OriginalChannels = channels;
            OriginalFrequency = frequency;
            Duration = (float)audioData.Length / channels / frequency;
            GenerateRegionWaveformData(); 
        }

        public void GenerateRegionWaveformData(int resolution = 2048) 
        {
            if (AudioData == null || AudioData.Length == 0 || OriginalChannels == 0)
            {
                WaveformData = new float[0];
                return;
            }

            int regionTotalSamplesPerChannel = AudioData.Length / OriginalChannels;
            if (regionTotalSamplesPerChannel == 0)
            {
                WaveformData = new float[0];
                return;
            }

            int waveformSamples = Mathf.Min(regionTotalSamplesPerChannel, resolution);
            if (waveformSamples <= 0) waveformSamples = 1; 

            WaveformData = new float[waveformSamples];
            int samplesPerPoint = regionTotalSamplesPerChannel / waveformSamples;
            if (samplesPerPoint < 1) samplesPerPoint = 1;

            for (int i = 0; i < waveformSamples; i++)
            {
                float maxPeak = 0f;
                int startSampleFrame = i * samplesPerPoint;
                int endSampleFrame = Mathf.Min(startSampleFrame + samplesPerPoint, regionTotalSamplesPerChannel);

                for (int sf = startSampleFrame; sf < endSampleFrame; sf++)
                {
                    for (int c = 0; c < OriginalChannels; c++)
                    {
                        int sampleIndex = sf * OriginalChannels + c;
                        if (sampleIndex < AudioData.Length)
                        {
                            float sampleAbs = Mathf.Abs(AudioData[sampleIndex]);
                            if (sampleAbs > maxPeak)
                            {
                                maxPeak = sampleAbs;
                                WaveformData[i] = AudioData[sampleIndex];
                            }
                        }
                    }
                }
                 if (endSampleFrame == startSampleFrame && startSampleFrame < regionTotalSamplesPerChannel) 
                {
                     WaveformData[i] = AudioData[startSampleFrame * OriginalChannels]; 
                }
            }
        }
    }
}