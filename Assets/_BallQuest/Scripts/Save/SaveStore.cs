using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BallQuest
{
    public interface ISaveCodec { string Encode(SaveData data); SaveData Decode(string json); }

    public sealed class SaveStore
    {
        readonly string path;
        readonly ISaveCodec codec;
        const string EnvelopePrefix = "{\"checksum\":\"";
        const string EnvelopeDivider = "\",\"data\":";
        public SaveData Data { get; private set; } = new SaveData();
        public string LastError { get; private set; }
        public bool ReadOnly { get; private set; }
        public SaveStore(string directory, ISaveCodec codec)
        {
            path = Path.Combine(directory, "ball-quest-save.json"); this.codec = codec;
        }

        public void Load()
        {
            LastError = null;
            ReadOnly = false;
            foreach (var candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    string contents = File.ReadAllText(candidate);
                    string json, checksum;
                    if (contents.StartsWith(EnvelopePrefix, StringComparison.Ordinal) && contents.EndsWith("}", StringComparison.Ordinal))
                    {
                        int divider = contents.IndexOf(EnvelopeDivider, StringComparison.Ordinal);
                        if (divider < EnvelopePrefix.Length) throw new InvalidDataException("Save envelope is incomplete.");
                        checksum = contents.Substring(EnvelopePrefix.Length, divider - EnvelopePrefix.Length);
                        int start = divider + EnvelopeDivider.Length;
                        json = contents.Substring(start, contents.Length - start - 1);
                    }
                    else
                    {
                        // Migrate the earliest development envelope without discarding existing progress.
                        int newline = contents.IndexOf('\n');
                        if (newline < 0) throw new InvalidDataException("Save envelope is incomplete.");
                        json = contents.Substring(newline + 1); checksum = contents.Substring(0, newline).Trim();
                    }
                    if (checksum != Digest(json)) throw new InvalidDataException("Save checksum mismatch.");
                    Data = codec.Decode(json) ?? throw new InvalidDataException("Save is empty.");
                    if (Data.version > SaveData.CurrentVersion)
                    {
                        ReadOnly = true;
                        Data = new SaveData();
                        LastError = "Newer save preserved. This game version uses a temporary profile.";
                        return;
                    }
                    Data.Validate();
                    return;
                }
                catch (Exception e) when (e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException || e is FormatException)
                { LastError = "Recovered save defaults or backup: " + e.Message; }
            }
            Data = new SaveData();
        }

        public bool Save()
        {
            if (ReadOnly) return false;
            try
            {
                Data.Validate();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json = codec.Encode(Data);
                string temp = path + ".tmp";
                File.WriteAllText(temp, EnvelopePrefix + Digest(json) + EnvelopeDivider + json + "}", new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    // File.Replace keeps the prior complete file as backup; fallback for mobile filesystems.
                    try { File.Replace(temp, path, path + ".bak"); }
                    catch (PlatformNotSupportedException) { File.Copy(path, path + ".bak", true); File.Copy(temp, path, true); File.Delete(temp); }
                }
                else File.Move(temp, path);
                LastError = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { LastError = "Could not save: " + e.Message; return false; }
        }

        static string Digest(string text)
        {
            using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }
}
