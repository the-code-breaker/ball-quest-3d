using System;
using UnityEngine;
using UnityEngine.Audio;

namespace BallQuest
{
    public enum BallAudioMaterial { Metal, Wood, Glass, Rubber, Stone, Energy }

    [Serializable]
    public sealed class SurfaceAudioResponse
    {
        public SurfaceAudioKind surface;
        [Range(0, 2)] public float rollingGain = 1;
        [Range(.3f, 2)] public float pitch = 1;
        [Range(0, 2)] public float slidingGain = 1;
        [Range(0, 2)] public float impactGain = 1;
        public AudioClip rollingOverride;
        public AudioClip slidingOverride;
        public AudioClip impactOverride;
    }

    [CreateAssetMenu(menuName = "Ball Quest/Audio/Ball Contact Profile")]
    public sealed class BallAudioProfile : ScriptableObject
    {
        public BallAudioMaterial material;
        [TextArea] public string provenance = TemporaryBallAudio.Provenance;
        public int audioRevision=9;
        public AudioClip rolling;
        public AudioClip sliding;
        public AudioClip impact;
        public AudioMixerGroup outputGroup;
        [Range(0, 1)] public float rollingVolume = .18f;
        [Range(0, 1)] public float slidingVolume = .16f;
        [Range(0, 1)] public float impactVolume = .48f;
        public float referenceSpeed = 9;
        public float minimumRollingSpeed = .12f;
        public float minimumSlipSpeed = .6f;
        public float referenceImpactImpulse = 9;
        public float impactCooldown = .075f;
        public float surfaceCrossfade = .16f;
        public SurfaceAudioResponse[] surfaces;

        public SurfaceAudioResponse Response(SurfaceAudioKind kind)
        {
            if (surfaces != null)
                for (int i = 0; i < surfaces.Length; i++)
                    if (surfaces[i] != null && surfaces[i].surface == kind) return surfaces[i];
            return null;
        }

        public static SurfaceAudioResponse[] DefaultSurfaces()
        {
            return new[]
            {
                Response(SurfaceAudioKind.Wood, 1, .98f, .9f, .95f),
                Response(SurfaceAudioKind.Metal, .84f, 1.04f, 1.05f, 1.1f),
                Response(SurfaceAudioKind.Stone, 1.08f, .94f, 1.15f, 1.05f),
                Response(SurfaceAudioKind.Ice, .44f, 1.07f, .8f, .72f),
                Response(SurfaceAudioKind.Sand, .85f, .93f, 1.22f, .48f),
                Response(SurfaceAudioKind.Glass, .55f, 1.06f, .68f, .85f),
                Response(SurfaceAudioKind.Grass, .5f, .95f, .78f, .4f),
                Response(SurfaceAudioKind.Boost, .88f, 1.02f, .85f, .92f)
            };
        }
        static SurfaceAudioResponse Response(SurfaceAudioKind surface, float gain, float pitch, float slip, float impact)
            => new SurfaceAudioResponse { surface = surface, rollingGain = gain, pitch = pitch, slidingGain = slip, impactGain = impact };

        public static BallAudioMaterial ResolveMaterial(BallProfile physics, BallSkin skin)
        {
            if(skin&&skin.explicitAudioMaterial)return skin.audioMaterial;
            if (physics && physics.ability == BallAbility.Rubber) return BallAudioMaterial.Rubber;
            if (physics && physics.ability == BallAbility.Heavy) return BallAudioMaterial.Metal;
            if (!skin) return BallAudioMaterial.Wood;
            switch (skin.artStyle)
            {
                case BallArtStyle.BrushedSteel:case BallArtStyle.Copper:case BallArtStyle.Chrome: case BallArtStyle.Gold: return BallAudioMaterial.Metal;
                case BallArtStyle.CarbonRubber:return BallAudioMaterial.Rubber;
                case BallArtStyle.Walnut:return BallAudioMaterial.Wood;
                case BallArtStyle.Ice: return BallAudioMaterial.Glass;
                case BallArtStyle.Soccer: case BallArtStyle.Basketball: return BallAudioMaterial.Rubber;
                case BallArtStyle.Marble: case BallArtStyle.Earth: return BallAudioMaterial.Stone;
                case BallArtStyle.Turbine: case BallArtStyle.Galaxy: case BallArtStyle.Lava: return BallAudioMaterial.Energy;
                default: return BallAudioMaterial.Wood;
            }
        }
    }
}
