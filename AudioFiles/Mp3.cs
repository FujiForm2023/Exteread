using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Exteread.Mp3
{
    public struct Mp3Header
    {
        public int Version;
        // 00 - MPEG Version 2.5
        // 01 - Reserved
        // 10 - MPEG Version 2
        // 11 - MPEG Version 1
        public int Layer;
        // 00 - Reserved
        // 01 - Layer III
        // 10 - Layer II
        // 11 - Layer I
        public int ProtectBit;
        // 0 - CRC Check
        // 1 - No CRC Check
        public int Bitrate;
        // Look up table for bitrates
        public int SampleRate;
        // Also look up table for sample rates
        public int Padding;
        // 0 - No padding
        // 1 - Padding add 1 byte
        public int PrivateBit;
        // 0 - No private bit
        // 1 - Private bit
        public int ChannelMode;
        // 00 - Stereo
        // 01 - Joint stereo
        // 10 - Dual channel
        // 11 - Single channel
        public int ModeExtension;
        // Look up table for mode extension
        public int Copyright;
        // 0 - Not copyright
        // 1 - Copyright
        public int Original;
        // 0 - Not original
        // 1 - Original
        public int Emphasis;
        // 00 - No emphasis
        // 01 - 50/15 ms emphasis
        // 10 - Reserved
        // 11 - CCIT J.17
    }
    // I give up
    public class ExtereadMp3
    {   
        public static AudioClip Mp3Path2AudioClip(string filePath)
        {   
            // Get file name and extension
            string fileName = Path.GetFileName(filePath);

            // Read data in bytes
            byte[] mp3Data = File.ReadAllBytes(filePath);

            // Minimum header size
            if (mp3Data == null || mp3Data.Length < 4)
            {
                Debug.LogError("Invalid MP3 file.");
                return null;
            }
            
            List<float> PCMdata = new List<float>();

            // Start index for reading MP3 frames
            int frameSyncIndex = FindFrameSync(ref mp3Data, -1);
            Debug.Log(mp3Data.Length);
            List<int> frameSyncIndexList = new List<int>();
            frameSyncIndexList.Add(frameSyncIndex);
            while (frameSyncIndex < mp3Data.Length - 1)
            {
                Debug.Log(frameSyncIndex);
                // Locate the frame sync dynamically
                Mp3Header mp3Header = GetMp3Header(ref mp3Data, frameSyncIndex);
                Debug.Log(GetFrameSize(mp3Header));
                Debug.Log(CRCCheck(mp3Header, ref mp3Data, frameSyncIndex));
                int frameSize = GetFrameSize(mp3Header);
                frameSyncIndex += (frameSize + 32 < 0 ? -1 : frameSize + 32);
                do {
                    frameSyncIndex = FindFrameSync(ref mp3Data, frameSyncIndex);
                } while (frameSyncIndexList.Contains(frameSyncIndex) || frameSyncIndex < 0 || frameSyncIndex > mp3Data.Length - 1);
                frameSyncIndexList.Add(frameSyncIndex);
            }

            return null; // Placeholder for actual decoding logic
        }


        private static int FindFrameSync(ref byte[] mp3Data, int startIndex = -1)
        {
            // MP3 frame sync is 11 bits of 1s followed by 1 bit of 0
            for (int i = startIndex+1; i < mp3Data.Length - 1; ++i)
            {
                if ((mp3Data[i] == 0xFF) && ((mp3Data[i + 1] & 0xF0) == 0xF0))
                {
                    return i;
                }
            }
            return mp3Data.Length - 1; // Return the last index if no sync found
        }

        private static bool CRCCheck(Mp3Header header, ref byte[] mp3Data, int frameSyncIndex)
        {
            // Check if CRC is enabled
            if (header.ProtectBit == 1)
            {
                // No CRC check needed
                return true;
            }
            else
            {
                int frameSize = GetFrameSize(header);
                byte[] HeaderValue = new byte[]{
                    mp3Data[frameSyncIndex],
                    mp3Data[frameSyncIndex + 1]
                };
                ushort CRCAttached = (ushort)((mp3Data[frameSyncIndex + 2] << 8) | mp3Data[frameSyncIndex + 3]);
                return Crc16Calculator.CalcCcittFalse(ref HeaderValue, 1) == CRCAttached;
            }
        }

        public static Mp3Header GetMp3Header(ref byte[] mp3Data, int frameSyncIndex)
        {
            static int GetBitRateIndex(int bitRateIndexRaw, int version, int layer)
            {
                // MPEG Version II Layer II and III
                int[] bitrateSpecial = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, -1 };
                // Bitrate index table for different versions and layers
                int[][][] bitrateTable = new int[2][][]
                {
                    new int[3][]{
                        bitrateSpecial, // MPEG Version II Layer III
                        bitrateSpecial, // MPEG Version II Layer II
                        new int[16]{ 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, -1 } // MPEG Version II Layer I
                    },
                    new int[3][]{
                        new int[16]{ 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, -1 }, // MPEG Version I Layer III
                        new int[16]{ 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, -1 }, // MPEG Version I Layer II
                        new int[16]{ 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, -1 } // MPEG Version I Layer I
                    } // Reserved
                };
                version = version == 0 ? 2 : version; // Adjust version for indexing
                Debug.Log("Bitrate: " + bitRateIndexRaw + " " + version + " " + layer);
                return bitrateTable[version-2][layer-1][bitRateIndexRaw];
            }

            static int GetSampleRateIndex(int sampleRateIndexRaw, int version)
            {
                // Sample rate table for different versions
                int[,] sampleRateTable = new int[3, 4]
                {
                    { 44100, 48000, 32000, -1 }, // MPEG Version I
                    { 22050, 24000, 16000, -1 }, // MPEG Version II
                    { 11025, 12000, 8000, -1 } // MPEG Version II.V
                };
                version = version == 0 ? 3 : version; // Adjust version for indexing

                return sampleRateTable[version-2, sampleRateIndexRaw];
            }

            Mp3Header header = new Mp3Header
            {
                // Version is 12nd-13rd bit of header
                Version = (mp3Data[frameSyncIndex + 1] >> 3) & 0x3,
                Layer = (mp3Data[frameSyncIndex + 1] >> 1) & 0x3,
                ProtectBit = mp3Data[frameSyncIndex + 1] & 0x1,
                Bitrate = (mp3Data[frameSyncIndex + 2] >> 4) & 0xF,
                SampleRate= (mp3Data[frameSyncIndex + 2] >> 2) & 0x3,
                Padding = (mp3Data[frameSyncIndex + 2] >> 1) & 0x1,
                PrivateBit = mp3Data[frameSyncIndex + 2] & 0x1,
                ChannelMode = (mp3Data[frameSyncIndex + 3] >> 6) & 0x3,
                ModeExtension = (mp3Data[frameSyncIndex + 3] >> 4) &0x3,
                Copyright = (mp3Data[frameSyncIndex + 3] >> 3) & 0x1,
                Original = (mp3Data[frameSyncIndex + 3] >> 2) & 0x1,
                Emphasis = mp3Data[frameSyncIndex + 3] & 0x3
            };

            header.Bitrate = GetBitRateIndex(header.Bitrate, header.Version, header.Layer);
            header.SampleRate = GetSampleRateIndex(header.SampleRate, header.Version);

            return header;
        }

        public static int GetFrameSize(Mp3Header header)
        {
            // Calculate frame size based on header information
            Debug.Log(header.Bitrate + " " + header.SampleRate + " " + header.Padding + " Frame");
            if (header.Layer == 3){
                return (12 * header.Bitrate / header.SampleRate + header.Padding) * 4;
            }
            else if (header.Layer == 2){
                return 144 * header.Bitrate / header.SampleRate + header.Padding;
            }
            else if (header.Layer == 1){
                return 144 * header.Bitrate / header.SampleRate + header.Padding;
            }
            else {
                Debug.LogError("Unsupported layer: " + header.Layer);
                return -1;
            }
        }

        public static byte[] GetSideInformation(Mp3Header header, ref byte[] mp3Data, int frameSyncIndex)
        {
            // Calculate frame size
            int frameSize = GetFrameSize(header);

            // Check if the frame size is valid
            if (frameSize <= 0 || frameSyncIndex + frameSize > mp3Data.Length)
            {
                Debug.LogError("Invalid frame size: " + frameSize);
                return null;
            }

            // Extract side information from the MP3 data
            byte[] sideInfo = new byte[frameSize];
            Array.Copy(mp3Data, frameSyncIndex, sideInfo, 0, frameSize);

            return sideInfo;
        }
    }

    public static class Crc16Calculator {
        private const ushort InitialValue = 0xFFFF;

        private static readonly ushort[] ccittFalseCrc16Table = {
            0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7,
            0x8108, 0x9129, 0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF,
            0x1231, 0x0210, 0x3273, 0x2252, 0x52B5, 0x4294, 0x72F7, 0x62D6,
            0x9339, 0x8318, 0xB37B, 0xA35A, 0xD3BD, 0xC39C, 0xF3FF, 0xE3DE,
            0x2462, 0x3443, 0x0420, 0x1401, 0x64E6, 0x74C7, 0x44A4, 0x5485,
            0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE, 0xF5CF, 0xC5AC, 0xD58D,
            0x3653, 0x2672, 0x1611, 0x0630, 0x76D7, 0x66F6, 0x5695, 0x46B4,
            0xB75B, 0xA77A, 0x9719, 0x8738, 0xF7DF, 0xE7FE, 0xD79D, 0xC7BC,
            0x48C4, 0x58E5, 0x6886, 0x78A7, 0x0840, 0x1861, 0x2802, 0x3823,
            0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B,
            0x5AF5, 0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0x0A50, 0x3A33, 0x2A12,
            0xDBFD, 0xCBDC, 0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A,
            0x6CA6, 0x7C87, 0x4CE4, 0x5CC5, 0x2C22, 0x3C03, 0x0C60, 0x1C41,
            0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD, 0xAD2A, 0xBD0B, 0x8D68, 0x9D49,
            0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13, 0x2E32, 0x1E51, 0x0E70,
            0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A, 0x9F59, 0x8F78,
            0x9188, 0x81A9, 0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E, 0xE16F,
            0x1080, 0x00A1, 0x30C2, 0x20E3, 0x5004, 0x4025, 0x7046, 0x6067,
            0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C, 0xE37F, 0xF35E,
            0x02B1, 0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256,
            0xB5EA, 0xA5CB, 0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D,
            0x34E2, 0x24C3, 0x14A0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
            0xA7DB, 0xB7FA, 0x8799, 0x97B8, 0xE75F, 0xF77E, 0xC71D, 0xD73C,
            0x26D3, 0x36F2, 0x0691, 0x16B0, 0x6657, 0x7676, 0x4615, 0x5634,
            0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9, 0xB98A, 0xA9AB,
            0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x08E1, 0x3882, 0x28A3,
            0xCB7D, 0xDB5C, 0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A,
            0x4A75, 0x5A54, 0x6A37, 0x7A16, 0x0AF1, 0x1AD0, 0x2AB3, 0x3A92,
            0xFD2E, 0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B, 0x9DE8, 0x8DC9,
            0x7C26, 0x6C07, 0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0x0CC1,
            0xEF1F, 0xFF3E, 0xCF5D, 0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8,
            0x6E17, 0x7E36, 0x4E55, 0x5E74, 0x2E93, 0x3EB2, 0x0ED1, 0x1EF0
        };

        public static ushort CalcCcittFalse(ref byte[] bytes, int length) {
            var crc16 = InitialValue;

            var byteIndex = 0;
            while (length-- > 0) {
                var tableIndex = crc16 >> 8 ^ bytes[byteIndex++];
                crc16 = (ushort)(crc16 << 8 ^ ccittFalseCrc16Table[tableIndex]);
            }

            return crc16;
        }
    }
}