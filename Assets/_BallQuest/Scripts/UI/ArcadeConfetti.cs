using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadeConfetti : MaskableGraphic
    {
        struct Piece { public Vector2 start,velocity; public float rotation,speed,size;public Color tint; }
        Piece[] pieces;
        float elapsed;
        public void Begin()
        {
            raycastTarget=false;elapsed=0;pieces=new Piece[56];var random=new System.Random(5031);
            Color[] colors={new Color(1,.82f,.12f),new Color(.29f,1,.16f),new Color(1,.24f,.44f),new Color(.27f,.74f,1),new Color(.77f,.34f,1)};
            for(int i=0;i<pieces.Length;i++)pieces[i]=new Piece{
                start=new Vector2((float)random.NextDouble()-.5f,.44f+(float)random.NextDouble()*.45f),
                velocity=new Vector2(((float)random.NextDouble()-.5f)*.1f,-.04f-(float)random.NextDouble()*.13f),
                rotation=(float)random.NextDouble()*6.28f,speed=(float)random.NextDouble()*5-2.5f,size=5+(float)random.NextDouble()*5,tint=colors[i%colors.Length]};
            SetVerticesDirty();
        }
        void Update() {if(pieces==null)return;elapsed+=Time.unscaledDeltaTime;SetVerticesDirty();if(elapsed>6)enabled=false;}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(pieces==null)return;var r=rectTransform.rect;
            foreach(var p in pieces)
            {
                var at=p.start+p.velocity*elapsed+Vector2.down*(elapsed*elapsed*.012f);
                var center=r.center+new Vector2(at.x*r.width,at.y*r.height);float angle=p.rotation+elapsed*p.speed;
                var right=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*p.size;var up=new Vector2(-right.y,right.x)*Mathf.Cos(elapsed*3+p.rotation)*.55f;
                Color tint=p.tint;tint.a=Mathf.Clamp01((6-elapsed)*.5f);int first=mesh.currentVertCount;
                mesh.AddVert(center-right-up,tint,Vector2.zero);mesh.AddVert(center-right+up,tint,Vector2.zero);mesh.AddVert(center+right+up,tint,Vector2.zero);mesh.AddVert(center+right-up,tint,Vector2.zero);mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
            }
        }
    }
}
