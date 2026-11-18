using UnityEngine;

namespace BallQuest
{
    public enum SurfaceAudioKind { Wood, Metal, Stone, Ice, Sand, Glass, Grass, Boost }

    /// <summary>Authored contact identity. Audio reads it; physics response is owned separately.</summary>
    public sealed class BallSurfaceTag : MonoBehaviour
    {
        public SurfaceAudioKind surface = SurfaceAudioKind.Wood;
        [Range(.05f, 8)] public float rollingResistanceMultiplier = 1;
        [Range(.1f, 2)] public float rollingGain = 1;
        [Range(.5f, 2)] public float pitch = 1;

        public static SurfaceAudioKind Identify(Collider collider)
        {
            if (!collider) return SurfaceAudioKind.Wood;
            var tag = collider.GetComponentInParent<BallSurfaceTag>();
            if (tag) return tag.surface;
            var material = collider.sharedMaterial;
            string label = material ? material.name.ToLowerInvariant() : collider.name.ToLowerInvariant();
            if (label.Contains("ice") || label.Contains("snow")) return SurfaceAudioKind.Ice;
            if (label.Contains("sand")) return SurfaceAudioKind.Sand;
            if (label.Contains("glass")) return SurfaceAudioKind.Glass;
            if (label.Contains("grass")) return SurfaceAudioKind.Grass;
            if (label.Contains("boost") || label.Contains("turbo")) return SurfaceAudioKind.Boost;
            if (label.Contains("metal") || label.Contains("steel")) return SurfaceAudioKind.Metal;
            if (label.Contains("stone") || label.Contains("rock")) return SurfaceAudioKind.Stone;
            return SurfaceAudioKind.Wood;
        }
    }
}
