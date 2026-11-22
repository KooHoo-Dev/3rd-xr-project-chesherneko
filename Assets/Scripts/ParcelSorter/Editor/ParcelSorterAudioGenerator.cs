using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ParcelSorter;

internal static class ParcelSorterAudioGenerator
{
    internal static AudioClip[] Generate()
    {
        string folder = "Assets/Audios/ParcelSorter";
        Directory.CreateDirectory(folder);
        Cue[] cues = (Cue[])Enum.GetValues(typeof(Cue));
        AudioClip[] clips = new AudioClip[cues.Length];
        foreach (Cue cue in cues)
        {
            string path = $"{folder}/{cue}.wav";
            Write(path, cue);
            AssetDatabase.ImportAsset(path);
            clips[(int)cue] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return clips;
    }

    private static void Write(string path, Cue cue)
    {
        const int rate = 22050;
        float[] lengths = { 0.09f, 0.28f, 0.32f, 0.2f, 0.06f, 0.07f, 0.72f };
        float[] pitches = { 540, 660, 105, 130, 860, 1100, 440 };
        int count = Mathf.RoundToInt(rate * lengths[(int)cue]);
        using BinaryWriter writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + count * 2);
        writer.Write("WAVEfmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(rate);
        writer.Write(rate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data".ToCharArray());
        writer.Write(count * 2);
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float pitch = pitches[(int)cue];
            if (cue == Cue.Correct && t > 0.36f) pitch *= 1.5f;
            if (cue == Cue.Finish) pitch *= Mathf.Pow(1.26f, Mathf.Floor(t * 4));
            if (cue == Cue.Miss || cue == Cue.Grab) pitch *= 1f - t * 0.65f;
            phase += 2f * Mathf.PI * pitch / rate;
            float wave = Mathf.Sin(phase);
            if (cue == Cue.Wrong) wave = Mathf.Clamp(wave * 3f, -1f, 1f);
            float envelope = Mathf.Min(1f, i / 64f) * (1f - t) * (1f - t);
            writer.Write((short)(wave * envelope * 19000f));
        }
    }
}
