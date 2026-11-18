using UnityEngine;

namespace BallQuest
{
    public enum BallArtStyle { Turbine, Soccer, Basketball, Galaxy, Gold, Ice, Lava, Chrome, Rainbow, Earth, Marble, BrushedSteel, Copper, CarbonRubber, Walnut }
    [CreateAssetMenu(menuName = "Ball Quest/Ball Skin")]
    public sealed class BallSkin : ScriptableObject
    {
        public string id;
        public string displayName;
        public Color color = Color.red;
        public Color stripe = Color.white;
        [Range(0, 1)] public float metallic;
        [Range(0, 1)] public float smoothness = 0.6f;
        public bool glow;
        public bool trail;
        public int price;
        public BallArtStyle artStyle;
        public bool explicitAudioMaterial;
        public BallAudioMaterial audioMaterial;
    }
}
