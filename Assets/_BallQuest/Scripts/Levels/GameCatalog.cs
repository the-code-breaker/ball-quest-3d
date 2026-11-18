using UnityEngine;

namespace BallQuest
{
    [CreateAssetMenu(menuName = "Ball Quest/Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public LevelDefinition[] levels;
        public LevelDefinition[] expeditions = System.Array.Empty<LevelDefinition>();
        public RaceTrackDefinition[] races;
        public BallProfile[] balls;
        public BallSkin[] skins;
        public Material[] palette;
        public Material glowMaterial;
        public Material particleMaterial;
        public AudioClip soundtrack;
        public Material skyMaterial;
        public int contentVersion;
        public int manualControlVersion;
        public int expeditionContentVersion;
        public int manualRaceVersion;
        public TMPro.TMP_FontAsset font;
        public UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset pipeline;
        public LevelDefinition Level(int id) => System.Array.Find(levels, l => l && l.id == id) ?? System.Array.Find(expeditions ?? System.Array.Empty<LevelDefinition>(), l => l && l.id == id);
        public LevelDefinition Expedition(int id)=>System.Array.Find(expeditions??System.Array.Empty<LevelDefinition>(),l=>l&&l.id==id);
        public BallProfile Ball(string id) => System.Array.Find(balls, b => b && b.id == id) ?? balls[0];
        public BallSkin Skin(string id) => System.Array.Find(skins, s => s && s.id == id) ?? skins[0];
    }
}
