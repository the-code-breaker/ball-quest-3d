using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class QuestIconGraphic : MaskableGraphic
    {
        public string icon;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Vector2 P(float x,float y)=>r.center+new Vector2(x*r.width,y*r.height);
            void Poly(params Vector2[] points)
            {
                int first=vh.currentVertCount;foreach(var p in points)vh.AddVert(P(p.x,p.y),color,Vector2.zero);
                for(int i=1;i<points.Length-1;i++)vh.AddTriangle(first,first+i,first+i+1);
            }
            void Line(Vector2 a,Vector2 b,float width)
            {var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width/2;Poly(a+n,b+n,b-n,a-n);}
            void Ring(float radius,float width)
            {
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;
                    Poly(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,
                        new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius-width),new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius-width));
                }
            }
            if(icon=="gear")
            {
                Ring(.28f,.075f);Ring(.09f,.045f);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(d*.24f,d*.4f,.11f);}
            }
            else if(icon=="ball")
            {
                Ring(.38f,.06f);Line(new Vector2(-.28f,.19f),new Vector2(.28f,-.19f),.07f);
                Line(new Vector2(-.24f,.25f),new Vector2(.32f,-.11f),.025f);
            }
            else if(icon=="flag")
            {
                Line(new Vector2(-.24f,-.38f),new Vector2(-.24f,.39f),.07f);
                Poly(new Vector2(-.21f,.36f),new Vector2(.36f,.36f),new Vector2(.24f,.13f),new Vector2(.36f,-.07f),new Vector2(-.21f,-.07f));
                Line(new Vector2(-.38f,-.38f),new Vector2(-.08f,-.38f),.06f);
            }
            else
            {
                Poly(new Vector2(-.27f,.35f),new Vector2(.27f,.35f),new Vector2(.19f,-.03f),new Vector2(.08f,-.14f),new Vector2(-.08f,-.14f),new Vector2(-.19f,-.03f));
                Line(new Vector2(0,-.1f),new Vector2(0,-.33f),.09f);
                Line(new Vector2(-.22f,-.35f),new Vector2(.22f,-.35f),.07f);
                Line(new Vector2(-.27f,.24f),new Vector2(-.42f,.24f),.06f);Line(new Vector2(-.42f,.24f),new Vector2(-.38f,-.01f),.06f);Line(new Vector2(-.38f,-.01f),new Vector2(-.16f,-.08f),.06f);
                Line(new Vector2(.27f,.24f),new Vector2(.42f,.24f),.06f);Line(new Vector2(.42f,.24f),new Vector2(.38f,-.01f),.06f);Line(new Vector2(.38f,-.01f),new Vector2(.16f,-.08f),.06f);
            }
        }
    }
}
