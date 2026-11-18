using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    public static class ArcadeGeometry
    {
        public static Mesh HalfLoopRoad(Vector3 center,float radius,float width,float thickness,int segments=36)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            Vector3 side=Vector3.right*width*.5f;
            for(int i=0;i<segments;i++)
            {
                float a=(-90+i*180f/segments)*Mathf.Deg2Rad,b=(-90+(i+1)*180f/segments)*Mathf.Deg2Rad;
                Vector3 radialA=new Vector3(0,Mathf.Sin(a),Mathf.Cos(a)),radialB=new Vector3(0,Mathf.Sin(b),Mathf.Cos(b));
                Vector3 p=center+radialA*radius,q=center+radialB*radius,outerP=center+radialA*(radius+thickness),outerQ=center+radialB*(radius+thickness);
                float v=i*radius*Mathf.PI/segments*.19f,nextV=(i+1)*radius*Mathf.PI/segments*.19f;
                AddQuad(p-side,q-side,q+side,p+side,-radialA,-radialB,new Vector2(0,v),new Vector2(0,nextV),new Vector2(1,nextV),new Vector2(1,v));
                AddQuad(outerP+side,outerQ+side,outerQ-side,outerP-side,radialA,radialB,Vector2.zero,Vector2.up,Vector2.one,Vector2.right);
                AddQuad(p-side,outerP-side,outerQ-side,q-side,Vector3.left,Vector3.left,Vector2.zero,Vector2.up,Vector2.one,Vector2.right);
                AddQuad(p+side,q+side,outerQ+side,outerP+side,Vector3.right,Vector3.right,Vector2.zero,Vector2.up,Vector2.one,Vector2.right);
                if(i==0)AddQuad(p-side,p+side,outerP+side,outerP-side,Vector3.back,Vector3.back,Vector2.zero,Vector2.up,Vector2.one,Vector2.right);
                if(i==segments-1)AddQuad(q+side,q-side,outerQ-side,outerQ+side,Vector3.back,Vector3.back,Vector2.zero,Vector2.up,Vector2.one,Vector2.right);
            }
            var mesh=new Mesh{name="Continuous gravity loop roadway"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;

            void AddQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normalA,Vector3 normalB,Vector2 uvA,Vector2 uvB,Vector2 uvC,Vector2 uvD)
            {
                int start=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{uvA,uvB,uvC,uvD});
                normals.AddRange(new[]{normalA,normalB,normalB,normalA});
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),(normalA+normalB).normalized)>0)triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                else triangles.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});
            }
        }
        public static Mesh PlatformCylinder(float width,float depth,float thickness,int sides=32)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            vertices.Add(Vector3.up*thickness*.5f);vertices.Add(Vector3.down*thickness*.5f);uv.Add(Vector2.one*.5f);uv.Add(Vector2.one*.5f);
            for(int i=0;i<=sides;i++)
            {
                float angle=i*Mathf.PI*2/sides;float x=Mathf.Cos(angle)*width*.5f,z=Mathf.Sin(angle)*depth*.5f;
                vertices.Add(new Vector3(x,thickness*.5f,z));vertices.Add(new Vector3(x,-thickness*.5f,z));uv.Add(new Vector2(x/width+.5f,z/depth+.5f));uv.Add(new Vector2(x/width+.5f,z/depth+.5f));
                if(i==sides)continue;int a=2+i*2,b=a+2;triangles.AddRange(new[]{0,b,a,1,a+1,b+1,a,b,b+1,a,b+1,a+1});
            }
            var mesh=new Mesh{name="Circular moving platform"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            go.layer=2;return go;
        }
        public static Mesh Road(Vector3 a,Vector3 b,Vector3 left,Vector3 right,float thickness)
        {
            var vertices=new[]{a-left,a+left,b+right,b-right,a-left-Vector3.up*thickness,a+left-Vector3.up*thickness,b+right-Vector3.up*thickness,b-right-Vector3.up*thickness};
            var mesh=new Mesh{name="Mitered wooden skyway"};
            mesh.vertices=vertices;
            mesh.uv=new[]{new Vector2(0,a.z*.19f),new Vector2(1,a.z*.19f),new Vector2(1,b.z*.19f),new Vector2(0,b.z*.19f),Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            mesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,3,7,6,3,6,2,0,1,5,0,5,4};
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Torus(float radius,float tube,int segments=48,int sides=8,float arc=360)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            for(int i=0;i<=segments;i++)
            {
                float a=(float)i/segments*arc*Mathf.Deg2Rad;
                for(int j=0;j<=sides;j++)
                {
                    float b=j*Mathf.PI*2/sides;
                    vertices.Add(new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(b)*tube));
                    uv.Add(new Vector2((float)i/segments,(float)j/sides));
                    if(i==segments||j==sides)continue;
                    int k=i*(sides+1)+j;triangles.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});
                }
            }
            var mesh=new Mesh{name="Rounded arcade ring"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Star(float radius,float depth=.03f)
        {
            var vertices=new List<Vector3>{new Vector3(0,0,-depth)};
            for(int i=0;i<10;i++) {float a=(90+i*36)*Mathf.Deg2Rad,r=i%2==0?radius:radius*.46f;vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,-depth));}
            var indices=new List<int>();for(int i=0;i<10;i++) {indices.Add(0);indices.Add((i+1)%10+1);indices.Add(i+1);}
            var mesh=new Mesh{name="Embossed coin star"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();return mesh;
        }
        public static Mesh Diamond(float radius,float height)
        {
            Vector3 top=Vector3.up*height*.6f,bottom=Vector3.down*height*.55f;
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;
                Vector3 p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),q=new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                int n=vertices.Count;vertices.AddRange(new[]{top,q,p,bottom,p,q});indices.AddRange(new[]{n,n+1,n+2,n+3,n+4,n+5});
            }
            var mesh=new Mesh{name="Cut crystal"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();return mesh;
        }
        public static Mesh Cone(float radius,float height,int sides=10)
        {
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                int n=vertices.Count;vertices.AddRange(new[]{new Vector3(0,height*.5f,0),new Vector3(Mathf.Cos(b)*radius,-height*.5f,Mathf.Sin(b)*radius),new Vector3(Mathf.Cos(a)*radius,-height*.5f,Mathf.Sin(a)*radius)});indices.AddRange(new[]{n,n+1,n+2});
            }
            var mesh=new Mesh{name="Faceted landscape"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();return mesh;
        }
    }
}
