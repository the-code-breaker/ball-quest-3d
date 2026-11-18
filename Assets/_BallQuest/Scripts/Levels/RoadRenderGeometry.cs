using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    /// <summary>
    /// Builds a visible union of road faces. Authoring deliberately overlaps solid pieces to
    /// make collision seams safe; those coplanar overlaps must not also be drawn twice.
    /// Physics continues to use the original complete solid meshes.
    /// </summary>
    public static class RoadRenderGeometry
    {
        const float PlaneTolerance = .001f;
        const float EdgeTolerance = .00001f;
        static readonly int[][] Faces = {
            new[]{0,3,2,1}, new[]{4,5,6,7}, new[]{0,4,7,3},
            new[]{1,2,6,5}, new[]{3,7,6,2}, new[]{0,1,5,4}
        };

        struct Vertex
        {
            public Vector3 point;
            public Vector2 uv;
            public Vertex(Vector3 point,Vector2 uv) { this.point=point; this.uv=uv; }
            public static Vertex Lerp(Vertex a,Vertex b,float t) => new Vertex(Vector3.LerpUnclamped(a.point,b.point,t),Vector2.LerpUnclamped(a.uv,b.uv,t));
        }
        sealed class Face
        {
            public List<Vertex> polygon;
            public Vector3 normal;
            public float plane;
            public Bounds bounds;
        }

        public static Mesh[] Build(TrackPiece[] pieces)
        {
            var result=new Mesh[pieces.Length];
            var previous=new List<Face>(pieces.Length*6);
            for(int index=0;index<pieces.Length;index++)
            {
                var piece=pieces[index];
                var rotation=Quaternion.Euler(piece.rotation);
                var up=rotation*Vector3.up;
                var forward=rotation*Vector3.forward;
                var right=rotation*Vector3.right;
                Vector3 a=piece.position+up*piece.size.y*.5f-forward*piece.size.z*.5f;
                Vector3 b=piece.position+up*piece.size.y*.5f+forward*piece.size.z*.5f;
                Vector3 startRight=right,endRight=right;
                if(index>0)
                {
                    var p=pieces[index-1];var q=Quaternion.Euler(p.rotation);
                    var end=p.position+q*Vector3.up*p.size.y*.5f+q*Vector3.forward*p.size.z*.5f;
                    if(Vector3.Distance(end,a)<.3f)startRight=(right+q*Vector3.right).normalized;
                }
                if(index+1<pieces.Length)
                {
                    var p=pieces[index+1];var q=Quaternion.Euler(p.rotation);
                    var start=p.position+q*Vector3.up*p.size.y*.5f-q*Vector3.forward*p.size.z*.5f;
                    if(Vector3.Distance(start,b)<.3f)endRight=(right+q*Vector3.right).normalized;
                }
                startRight*=piece.size.x*.5f/Mathf.Max(.8f,Vector3.Dot(startRight,right));
                endRight*=piece.size.x*.5f/Mathf.Max(.8f,Vector3.Dot(endRight,right));
                var corners=new[]{a-startRight,a+startRight,b+endRight,b-endRight,a-startRight-Vector3.up*.74f,a+startRight-Vector3.up*.74f,b+endRight-Vector3.up*.74f,b-endRight-Vector3.up*.74f};
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();
                float startV=Vector3.Dot(a,forward)*.19f,endV=Vector3.Dot(b,forward)*.19f;
                float boardScale=Mathf.Max(.2f,piece.size.x/7);
                var topUvs=new[]{new Vector2(0,startV),new Vector2(boardScale,startV),new Vector2(boardScale,endV),new Vector2(0,endV)};
                var current=new List<Face>(6);
                foreach(var faceIndices in Faces)
                {
                    var polygon=new List<Vertex>(4);
                    for(int i=0;i<4;i++)
                    {
                        int corner=faceIndices[i];
                        polygon.Add(new Vertex(corners[corner],corner<4?topUvs[corner]:new Vector2(i==1||i==2?1:0,i>=2?1:0)));
                    }
                    var normal=Vector3.Cross(polygon[1].point-polygon[0].point,polygon[2].point-polygon[0].point).normalized;
                    if(normal.sqrMagnitude<.1f)continue;
                    float plane=Vector3.Dot(normal,polygon[0].point);
                    // A miter on a changing bank may produce a non-planar quad. Split it
                    // before clipping rather than projecting its fourth corner to a new plane.
                    if(Mathf.Abs(Vector3.Dot(normal,polygon[3].point)-plane)>PlaneTolerance)
                    {
                        AddFace(new List<Vertex>{polygon[0],polygon[1],polygon[2]},previous,current,vertices,normals,uvs,triangles);
                        AddFace(new List<Vertex>{polygon[0],polygon[2],polygon[3]},previous,current,vertices,normals,uvs,triangles);
                    }
                    else AddFace(polygon,previous,current,vertices,normals,uvs,triangles);
                }
                previous.AddRange(current);
                var mesh=new Mesh{name="Non-overlapping skyway render "+index};
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
                result[index]=mesh;
            }
            return result;
        }

        static void AddFace(List<Vertex> polygon,List<Face> previous,List<Face> current,List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<int> triangles)
        {
            var normal=Vector3.Cross(polygon[1].point-polygon[0].point,polygon[2].point-polygon[0].point).normalized;
            var face=new Face{polygon=polygon,normal=normal,plane=Vector3.Dot(normal,polygon[0].point),bounds=Bounds(polygon)};
            current.Add(face);
            var fragments=new List<List<Vertex>>{polygon};
            foreach(var blocker in previous)
            {
                if(Vector3.Dot(normal,blocker.normal)<.999999f||Mathf.Abs(face.plane-blocker.plane)>PlaneTolerance)continue;
                // Broad-phase bounds avoid polygon work for distant coplanar roads.
                if(!face.bounds.Intersects(blocker.bounds))continue;
                var remaining=new List<List<Vertex>>();
                foreach(var fragment in fragments)Subtract(fragment,blocker,remaining);
                fragments=remaining;if(fragments.Count==0)break;
            }
            foreach(var fragment in fragments)
            {
                int first=vertices.Count;
                foreach(var vertex in fragment){vertices.Add(vertex.point);normals.Add(normal);uv.Add(vertex.uv);}
                for(int i=1;i<fragment.Count-1;i++)
                {
                    if(Vector3.Cross(fragment[i].point-fragment[0].point,fragment[i+1].point-fragment[0].point).sqrMagnitude<.0000000001f)continue;
                    triangles.Add(first);triangles.Add(first+i);triangles.Add(first+i+1);
                }
            }
        }

        static Bounds Bounds(List<Vertex> polygon)
        {
            var value=new Bounds(polygon[0].point,Vector3.zero);
            for(int i=1;i<polygon.Count;i++)value.Encapsulate(polygon[i].point);
            value.Expand(PlaneTolerance*2);return value;
        }
        static void Subtract(List<Vertex> polygon,Face blocker,List<List<Vertex>> result)
        {
            var inside=polygon;
            for(int i=0;i<blocker.polygon.Count&&inside.Count>=3;i++)
            {
                Vector3 start=blocker.polygon[i].point,end=blocker.polygon[(i+1)%blocker.polygon.Count].point;
                Vector3 inward=Vector3.Cross(blocker.normal,end-start).normalized;
                var outside=Clip(inside,start,-inward);
                if(outside.Count>=3)result.Add(outside);
                inside=Clip(inside,start,inward);
            }
            // Anything remaining inside all blocker edges was already rendered by an earlier piece.
        }
        static List<Vertex> Clip(List<Vertex> polygon,Vector3 edge,Vector3 inward)
        {
            var result=new List<Vertex>(polygon.Count+1);
            if(polygon.Count==0)return result;
            Vertex previous=polygon[polygon.Count-1];float previousDistance=Vector3.Dot(previous.point-edge,inward);
            foreach(var next in polygon)
            {
                float distance=Vector3.Dot(next.point-edge,inward);
                bool previousInside=previousDistance>=-EdgeTolerance,nextInside=distance>=-EdgeTolerance;
                if(previousInside!=nextInside)
                {
                    float fraction=previousDistance/(previousDistance-distance);
                    result.Add(Vertex.Lerp(previous,next,Mathf.Clamp01(fraction)));
                }
                if(nextInside)result.Add(next);
                previous=next;previousDistance=distance;
            }
            return result;
        }
    }
}
