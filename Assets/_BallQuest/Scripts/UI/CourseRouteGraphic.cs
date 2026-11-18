using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    // An actual route silhouette makes spatially different courses recognizable in the menu.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourseRouteGraphic : MaskableGraphic
    {
        public LevelDefinition level;
        public float lineWidth=2.7f;
        protected CourseRouteGraphic(){useLegacyMeshGeneration=false;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!level||level.routePoints==null||level.routePoints.Length<2)return;
            Vector2 min=Vector2.one*float.PositiveInfinity,max=Vector2.one*float.NegativeInfinity;
            foreach(var point in level.routePoints){Vector2 p=Project(point);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            Rect area=rectTransform.rect;Vector2 span=max-min;
            float scale=Mathf.Min((area.width-10)/Mathf.Max(1,span.x),(area.height-10)/Mathf.Max(1,span.y));
            Vector2 center=(min+max)*.5f;
            for(int i=0;i<level.routePoints.Length-1;i++)
            {
                Vector2 a=(Project(level.routePoints[i])-center)*scale,b=(Project(level.routePoints[i+1])-center)*scale;
                Vector2 d=b-a;if(d.sqrMagnitude<.01f)continue;Vector2 n=new Vector2(-d.y,d.x).normalized*(lineWidth*.5f);
                Color tint=Color.Lerp(new Color(.28f,.85f,1),new Color(1,.78f,.2f),i/(float)(level.routePoints.Length-1))*color;
                int start=vh.currentVertCount;vh.AddVert(a-n,tint,Vector2.zero);vh.AddVert(a+n,tint,Vector2.up);vh.AddVert(b+n,tint,Vector2.one);vh.AddVert(b-n,tint,Vector2.right);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
        static Vector2 Project(Vector3 p)=>new Vector2(p.x+p.z*.28f,p.z*.58f+p.y*.75f);
    }
}
