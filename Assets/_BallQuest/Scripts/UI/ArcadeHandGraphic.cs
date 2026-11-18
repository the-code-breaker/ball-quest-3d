using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadeHandGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r = rectTransform.rect;
            Vector2[] outline = {
                new Vector2(-.32f,-.02f),new Vector2(-.18f,-.13f),new Vector2(-.16f,.37f),new Vector2(-.12f,.44f),new Vector2(-.03f,.44f),new Vector2(.02f,.37f),new Vector2(.02f,.03f),
                new Vector2(.09f,.07f),new Vector2(.17f,.05f),new Vector2(.19f,-.01f),new Vector2(.26f,.01f),new Vector2(.33f,-.04f),new Vector2(.34f,-.13f),
                new Vector2(.4f,-.15f),new Vector2(.43f,-.24f),new Vector2(.37f,-.45f),new Vector2(.01f,-.48f),new Vector2(-.13f,-.34f),new Vector2(-.36f,-.17f),new Vector2(-.38f,-.09f)
            };
            void Draw(float scale,Vector2 offset,Color tint)
            {
                int first=mesh.currentVertCount;mesh.AddVert(r.center+new Vector2(.05f*r.width,-.19f*r.height)+offset,tint,Vector2.zero);
                foreach(var p in outline)mesh.AddVert(r.center+new Vector2(p.x*r.width,p.y*r.height)*scale+offset,tint,Vector2.zero);
                for(int i=0;i<outline.Length;i++)mesh.AddTriangle(first,first+1+i,first+1+(i+1)%outline.Length);
            }
            Draw(1.1f,new Vector2(1,-2),new Color(.18f,.19f,.18f,.8f));Draw(1,Vector2.zero,new Color(1,.99f,.91f,1));
        }
    }
}
