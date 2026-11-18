using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    // Vector UI geometry stays crisp on different portrait resolutions without an imported sprite.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StarGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.48f;
            mesh.AddVert(rect.center, color, Vector2.one * 0.5f);
            for (int i = 0; i < 10; i++)
            {
                float angle = (90 + i * 36) * Mathf.Deg2Rad;
                float distance = i % 2 == 0 ? radius : radius * 0.45f;
                Vector2 point = rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                mesh.AddVert(point, color, Vector2.zero);
            }
            for (int i = 0; i < 10; i++) mesh.AddTriangle(0, i + 1, (i + 1) % 10 + 1);
        }
    }
}
