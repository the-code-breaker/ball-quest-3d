using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    // Unlike UI.Outline this emits only a contour, so translucent panels stay truly dark inside.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadePanelStroke : MaskableGraphic
    {
        public float radius=24;
        public float thickness=2;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            float outer=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);
            float width=Mathf.Clamp(thickness,0,outer),inner=outer-width;
            const int corners=4,steps=12;
            for(int corner=0;corner<corners;corner++)
            {
                float start=corner*Mathf.PI*.5f;
                var center=r.center+new Vector2(corner==0||corner==3?r.width*.5f-outer:-r.width*.5f+outer,corner<2?r.height*.5f-outer:-r.height*.5f+outer);
                for(int step=0;step<=steps;step++)
                {
                    float a=start+step*Mathf.PI*.5f/steps;
                    var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    mesh.AddVert(center+d*outer,color,Vector2.zero);mesh.AddVert(center+d*inner,color,Vector2.zero);
                }
            }
            int pairs=corners*(steps+1);
            for(int i=0;i<pairs;i++)
            {
                int next=(i+1)%pairs;mesh.AddTriangle(i*2,next*2,next*2+1);mesh.AddTriangle(i*2,next*2+1,i*2+1);
            }
        }
    }

    public sealed class ArcadeSafeContentScale : MonoBehaviour
    {
        RectTransform rect;
        void Awake()=>rect=(RectTransform)transform;
        void LateUpdate()
        {
            float fit=Mathf.Min(1,Mathf.Min(rect.rect.width/720,rect.rect.height/1280));
            rect.localScale=Vector3.one*Mathf.Max(.2f,fit);
        }
    }
}
