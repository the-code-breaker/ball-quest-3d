using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadeWheelGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .495f; var center = rectTransform.rect.center;
            Color[] colors = { new Color(.1f,.68f,1), new Color(.7f,.2f,.96f), new Color(.14f,.83f,.35f), new Color(1,.57f,.05f), new Color(.13f,.7f,.98f), new Color(.96f,.22f,.4f), new Color(.58f,.25f,.96f), new Color(.1f,.78f,.7f) };
            void Fan(float r, Color tint)
            {
                int start = mesh.currentVertCount; mesh.AddVert(center, tint, Vector2.zero);
                for (int i=0;i<=128;i++) { float a=i*Mathf.PI*2/128; mesh.AddVert(center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*r,tint,Vector2.zero); }
                for(int i=0;i<128;i++)mesh.AddTriangle(start,start+i+1,start+i+2);
            }
            Fan(radius,new Color(.39f,.22f,.01f)); Fan(radius-5,new Color(1,.82f,.16f)); Fan(radius-14,new Color(1,.95f,.58f));
            for(int segment=0;segment<8;segment++)
            {
                int start=mesh.currentVertCount;mesh.AddVert(center,colors[segment]*new Color(.68f,.68f,.68f,1),Vector2.zero);
                for(int i=0;i<=16;i++) {float a=(segment+i/16f)*Mathf.PI/4;mesh.AddVert(center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*(radius-20),colors[segment],Vector2.zero);}
                for(int i=0;i<16;i++)mesh.AddTriangle(start,start+i+1,start+i+2);
            }
            Fan(42,new Color(.4f,.23f,.02f));Fan(36,new Color(1,.84f,.21f));Fan(27,new Color(1,.97f,.65f));
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadeTriangleGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;mesh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);mesh.AddVert(new Vector2(r.center.x,r.yMin),color,Vector2.zero);mesh.AddTriangle(0,1,2);
        }
    }
}
