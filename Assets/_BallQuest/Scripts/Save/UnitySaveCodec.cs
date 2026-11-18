using UnityEngine;

namespace BallQuest
{
    public sealed class UnitySaveCodec : ISaveCodec
    {
        public string Encode(SaveData data) => JsonUtility.ToJson(data, true);
        public SaveData Decode(string json) => JsonUtility.FromJson<SaveData>(json);
    }
}
