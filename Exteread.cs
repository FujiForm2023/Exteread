using UnityEngine;
using System;
using System.IO;

using Exteread.Mp3;
using Exteread.Wav;

namespace Exteread
{
    public class Exteread
    {
        public static AudioClip WavPath2AudioClip(string filePath) => ExtereadWav.WavPath2AudioClip(filePath);
        public static AudioClip Mp3Path2AudioClip(string filePath) => ExtereadMp3.Mp3Path2AudioClip(filePath);
    }
}