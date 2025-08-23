using System;
using System.IO;
using UnityEngine;

namespace UAI
{
    public static class SaveWave
    {
        const int HEADER_SIZE = 44;

        public static bool Save(string filename, AudioClip clip, int offsetSamples = 0, int samples = -1)
        {
            if (!filename.ToLower().EndsWith(".wav"))
            {
                filename += ".wav";
            }

            var filepath = filename;

            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            if (samples < 0)
            {
                samples = clip.samples;
            }

            using (FileStream fileStream = CreateEmpty(filepath))
            {
                ConvertAndWrite(fileStream, clip, offsetSamples, samples);
                WriteHeader(fileStream, clip, samples);
            }

            return true;
        }

        static FileStream CreateEmpty(string filepath)
        {
            FileStream fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++)
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }

        static void ConvertAndWrite(FileStream fileStream, AudioClip clip, int offsetSamples, int samples)
        {
            float[] data = new float[samples * clip.channels];
            clip.GetData(data, offsetSamples);

            Int16[] intData = new Int16[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                intData[i] = (short)(data[i] * 32767);
            }

            byte[] byteData = new byte[intData.Length * 2];
            for (int i = 0; i < intData.Length; i++)
            {
                byteData[i * 2] = (byte)(intData[i] & 0xff);
                byteData[i * 2 + 1] = (byte)((intData[i] >> 8) & 0xff);
            }

            fileStream.Write(byteData, 0, byteData.Length);
        }

        static void WriteHeader(FileStream fileStream, AudioClip clip, int samples)
        {
            var hz = clip.frequency;
            var channels = clip.channels;
            var sampleCount = samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            // RIFF header
            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            // ChunkSize
            var chunkSize = BitConverter.GetBytes((UInt32)(44 + (sampleCount * channels * 2) - 8));
            fileStream.Write(chunkSize, 0, 4);

            // WAVEfmt
            byte[] wav = System.Text.Encoding.UTF8.GetBytes("WAVEfmt ");
            fileStream.Write(wav, 0, 8);

            // Subchunk1Size
            byte[] fmtSize = BitConverter.GetBytes((UInt32)16);
            fileStream.Write(fmtSize, 0, 4);

            // AudioFormat (PCM)
            UInt16 one = 1;
            byte[] audioFormat = BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            // NumChannels
            byte[] numChannels = BitConverter.GetBytes((UInt16)channels);
            fileStream.Write(numChannels, 0, 2);

            // SampleRate
            byte[] sampleRate = BitConverter.GetBytes((UInt32)hz);
            fileStream.Write(sampleRate, 0, 4);

            // ByteRate
            byte[] byteRate = BitConverter.GetBytes((UInt32)(hz * channels * 2));
            fileStream.Write(byteRate, 0, 4);

            // BlockAlign
            UInt16 blockAlign = (UInt16)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            // BitsPerSample
            UInt16 bps = 16;
            byte[] bitsPerSample = BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            // "data"
            byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(dataString, 0, 4);

            // Subchunk2Size
            byte[] subchunk2 = BitConverter.GetBytes((UInt32)(sampleCount * channels * 2));
            fileStream.Write(subchunk2, 0, 4);

            fileStream.Seek(HEADER_SIZE, SeekOrigin.Begin);
        }
    }
}