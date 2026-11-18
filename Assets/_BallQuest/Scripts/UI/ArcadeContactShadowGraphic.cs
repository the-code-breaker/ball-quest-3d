using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadeContactShadowGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;const int count=64;
            mesh.AddVert(r.center,new Color(0,0,0,.24f),Vector2.zero);
            for(int i=0;i<=count;i++)
            {
                float a=i*Mathf.PI*2/count;
                mesh.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width*.5f,Mathf.Sin(a)*r.height*.5f),Color.clear,Vector2.zero);
            }
            for(int i=0;i<count;i++)mesh.AddTriangle(0,i+1,i+2);
        }
    }
}
