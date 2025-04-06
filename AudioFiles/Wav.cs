using UnityEngine;
using System;
using System.IO;

namespace Exteread.Wav
{
    public class ExtereadWav
    {
        public static AudioClip WavPath2AudioClip(string filePath)
        {
            // Read data in bytes
            byte[] wavData = File.ReadAllBytes(filePath);

            // Minimum header size
            if (wavData == null || wavData.Length < 44)
            {
                Debug.LogError("Invalid WAV header.");
                return null;
            }

            // SampleRate, Channel number, Bit Depth
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int numChannels = BitConverter.ToInt16(wavData, 22);
            int bitDepth = BitConverter.ToInt16(wavData, 34);
            
            // Locate the "data" chunk dynamically
            int dataIndex = FindDataChunk(ref wavData);

            // Unknown "data" starting position
            if (dataIndex == -1)
            {
                Debug.LogError("WAV file missing 'data' chunk.");
                return null;
            }

            // Data Size, Number of Samples of All Channels
            int dataSize = BitConverter.ToInt32(wavData, dataIndex + 4);
            int numSamples = dataSize / (bitDepth / 8);

            // Debuging Purpose
            // Debug.Log($"Sample Rate: {sampleRate}, Channels: {numChannels}, Bit Depth: {bitDepth}, Data Size: {dataSize}");

            // Support only 8, 16, 24, 32 bit depth
            if (bitDepth != 8 && bitDepth != 16 && bitDepth != 24 && bitDepth != 32)
            {
                Debug.LogError("Unsupported bit depth: " + bitDepth);
                return null;
            }

            // Prepare to lower the value to PCM data
            float[] floatData = new float[numSamples];
            
            // Read samples based on bit depth
            for (int i = 0, j = dataIndex + 8; i < numSamples; i++)
            {
                // Need to find a better way to not use infinitely if else statement
                if (bitDepth == 8)
                {
                    // 8-bit PCM
                    sbyte sample = (sbyte)wavData[j];
                    floatData[i] = sample / 128.0f;
                    j++;
                }
                else if (bitDepth == 16)
                {
                    // 16-bit PCM
                    short sample = (short)(wavData[j] | (wavData[j + 1] << 8));
                    floatData[i] = sample / 32768.0f;
                    j += 2;
                }
                else if (bitDepth == 24)
                {
                    // 24-bit PCM
                    int sample = wavData[j] | (wavData[j + 1] << 8) | (wavData[j + 2] << 16);
                    floatData[i] = sample / 8388608.0f;
                    j += 3;
                }
                else if (bitDepth == 32)
                {
                    // 32-bit PCM
                    int sample = BitConverter.ToInt32(wavData, j);
                    floatData[i] = sample / 2147483648.0f;
                    j += 4;
                }
            }

            // Create AudioClip from the float data
            AudioClip audioClip = AudioClip.Create(Path.GetFileName(filePath), numSamples / numChannels, numChannels, sampleRate, false);
            audioClip.SetData(floatData, 0);
            return audioClip;
        }

        // Skip header
        private static int FindDataChunk(ref byte[] wavData)
        {
            for (int i = 12; i < wavData.Length - 8; i += 2)
            {
                if (wavData[i] == 'd' && wavData[i + 1] == 'a' && wavData[i + 2] == 't' && wavData[i + 3] == 'a')
                {
                    return i;
                }
            }
            return -1; // Data chunk not found
        }
    }
}
