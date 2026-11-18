using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadeRibbonGraphic : MaskableGraphic
    {
        public Color top = Color.green, bottom = Color.green;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect = rectTransform.rect;
            void Polygon(Color shade, params Vector2[] points)
            {
                int first = mesh.currentVertCount;
                foreach (var p in points) mesh.AddVert(rect.center + new Vector2(p.x * rect.width, p.y * rect.height), shade, Vector2.zero);
                for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(first, first + i, first + i + 1);
            }
            Polygon(bottom * new Color(.45f, .45f, .45f, 1), new Vector2(-.49f, .19f), new Vector2(-.40f, .25f), new Vector2(-.40f, -.43f), new Vector2(-.5f, -.38f), new Vector2(-.47f, -.09f), new Vector2(-.5f, .12f));
            Polygon(bottom * new Color(.45f, .45f, .45f, 1), new Vector2(.49f, .19f), new Vector2(.40f, .25f), new Vector2(.40f, -.43f), new Vector2(.5f, -.38f), new Vector2(.47f, -.09f), new Vector2(.5f, .12f));
            Polygon(bottom * new Color(.58f, .58f, .58f, 1), new Vector2(-.445f, .35f), new Vector2(0, .5f), new Vector2(.445f, .35f), new Vector2(.43f, -.36f), new Vector2(0, -.23f), new Vector2(-.43f, -.36f));
            int start = mesh.currentVertCount;
            var points = new[] { new Vector2(-.445f, .4f), new Vector2(0, .54f), new Vector2(.445f, .4f), new Vector2(.43f, -.26f), new Vector2(0, -.13f), new Vector2(-.43f, -.26f) };
            foreach (var p in points) mesh.AddVert(rect.center + new Vector2(p.x * rect.width, p.y * rect.height), Color.Lerp(bottom, top, Mathf.InverseLerp(-.26f, .54f, p.y)), Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
            Polygon(new Color(1, 1, 1, .18f), new Vector2(-.433f, .365f), new Vector2(0, .495f), new Vector2(.433f, .365f), new Vector2(.43f, .31f), new Vector2(0, .44f), new Vector2(-.43f, .31f));
        }
    }

    public sealed class ArcadePop : MonoBehaviour
    {
        float elapsed;
        void OnEnable() { elapsed = 0; transform.localScale = Vector3.one * .3f; }
        void Update()
        {
            elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / .38f); transform.localScale = Vector3.one * Mathf.Max(0, 1 + 2.7f * Mathf.Pow(t - 1, 3) + 1.7f * Mathf.Pow(t - 1, 2)); if (t == 1) enabled = false;
        }
    }
}
