using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    // Resolution-independent original artwork, drawn directly in the native UI mesh.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SkylineGraphic : MaskableGraphic
    {
        public int variant;
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Vector2 P(float x, float y) => new Vector2(r.center.x + x * r.width / 600, r.center.y + y * r.height / 360);
            void Poly(Color c, params Vector2[] points)
            {
                int start = vh.currentVertCount;
                foreach (var p in points) vh.AddVert(P(p.x,p.y), c * color, Vector2.zero);
                for (int i=1;i<points.Length-1;i++) vh.AddTriangle(start,start+i,start+i+1);
            }
            void Ellipse(float x,float y,float rx,float ry,Color inner,Color outer)
            {
                int start=vh.currentVertCount;vh.AddVert(P(x-rx*.18f,y+ry*.2f),inner*color,Vector2.zero);
                const int count=48;
                for(int i=0;i<=count;i++) {float a=i*Mathf.PI*2/count;vh.AddVert(P(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),outer*color,Vector2.zero);}
                for(int i=0;i<count;i++)vh.AddTriangle(start,start+i+1,start+i+2);
            }
            var lilac=C("#DBCEF7"); var plum=C("#65518F");var cream=C("#FFF2D9");var orange=C("#FF854B");
            Ellipse(15,-128,252,28,new Color(.46f,.31f,.65f,.13f),Color.clear);
            Ellipse(-205,70,72,26,C("#FFFDF8"),new Color(1,1,1,.25f));
            Ellipse(-235,81,28,26,C("#FFFFFF"),C("#FFF7EB"));
            Ellipse(225,-58,64,25,C("#FFFFFF"),new Color(1,1,1,.1f));
            Ellipse(203,-43,31,27,C("#FFFFFF"),C("#FFF7EB"));
            // Three floating causeways with a visible height change.
            Vector2[] path={new Vector2(-260,-48),new Vector2(-120,44),new Vector2(20,-15),new Vector2(178,91),new Vector2(274,53)};
            for(int i=0;i<path.Length-1;i++)
            {
                Vector2 a=path[i],b=path[i+1];float w=34;
                var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*w;
                Vector2 drop=Vector2.down*(i==2?35:26);
                Poly(plum,a-n,b-n,b-n+drop,a-n+drop);
                Poly(lilac,a+n,b+n,b+n+drop,a+n+drop);
                Poly(cream,a-n,b-n,b+n,a+n);
                Poly(C("#FFFFFF"),a+n*.96f,b+n*.96f,b+n*.87f,a+n*.87f);
                for(int j=1;j<5;j++)
                {var p=Vector2.Lerp(a,b,j/5f);Poly(C("#EBD6BE"),p-n*.87f,p+n*.87f,p+n*.87f+d*1.5f,p-n*.87f+d*1.5f);}
                if(i==2)
                {
                    for(int j=0;j<3;j++)
                    {var p=Vector2.Lerp(a,b,.2f+j*.14f);Poly(orange,p-n*.65f-d*5,p-d*1,p+n*.65f-d*5,p+n*.65f+d*1,p+d*6,p-n*.65f+d*1);}
                }
            }
            // Suspended pillars and a soft underside convey altitude.
            Poly(C("#B3A0D4"),new Vector2(-133,11),new Vector2(-116,3),new Vector2(-116,-92),new Vector2(-133,-82));
            Poly(C("#E5D9F2"),new Vector2(-116,3),new Vector2(-103,11),new Vector2(-103,-82),new Vector2(-116,-92));
            // Glossy scarlet ball, white racing band, cast shadow and specular glints.
            Ellipse(-64,13,47,15,new Color(.3f,.16f,.43f,.28f),Color.clear);
            Ellipse(-64,60,47,48,C("#FFB28D"),C("#D83E5B"));
            Poly(C("#FFF5DD"),new Vector2(-108,74),new Vector2(-105,84),new Vector2(-21,45),new Vector2(-18,35));
            Ellipse(-80,82,13,8,new Color(1,1,1,.8f),new Color(1,1,1,.0f));
            // Collectible coins are real meshes rather than font symbols.
            for(int i=0;i<4;i++)
            {
                float x=52+i*34,y=48+i*22;
                Ellipse(x+2,y-3,12,17,C("#CB842C"),C("#CB842C"));
                Ellipse(x,y,12,17,C("#FFF1AB"),C("#F9BD46"));
                Ellipse(x,y,7,11,C("#FFDF77"),C("#E9A23B"));
                Poly(C("#FFF4BC"),new Vector2(x-1,y-7),new Vector2(x+2,y-7),new Vector2(x+2,y+7),new Vector2(x-1,y+7));
            }
            // Small orbiting sparkles.
            foreach(var p in new[]{new Vector2(-175,126),new Vector2(31,139),new Vector2(235,133),new Vector2(143,-90)})
                Poly(C("#E6A15C"),p+Vector2.up*9,p+Vector2.right*3,p+Vector2.down*9,p+Vector2.left*3);
        }
    }

    public sealed class UIGradient : BaseMeshEffect
    {
        public Color top = Color.white;
        public Color bottom = Color.white;
        public override void ModifyMesh(VertexHelper vh)
        {
            if(!IsActive())return;
            var r=((RectTransform)transform).rect;var v=new UIVertex();
            for(int i=0;i<vh.currentVertCount;i++)
            {vh.PopulateUIVertex(ref v,i);v.color=(Color)v.color*Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,v.position.y));vh.SetUIVertex(v,i);}
        }
    }
}
