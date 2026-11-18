using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BallQuest
{
    /// <summary>Native, spatially batched scenic geometry. No decorative primitive colliders or per-piece behaviours.</summary>
    public sealed class PremiumTrackPresentation
    {
        sealed class Batch
        {
            public Material material;
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<Vector3> normals=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<int> triangles=new List<int>();
        }
        readonly Dictionary<(Material,int,int),Batch> batches=new Dictionary<(Material,int,int),Batch>();
        readonly Transform parent;
        readonly List<Mesh> owned;
        readonly bool spatialCulling;
        public int PrimitiveCount { get; private set; }
        public int RendererCount { get; private set; }
        public PremiumTrackPresentation(Transform parent,List<Mesh> owned,bool spatialCulling=true){this.parent=parent;this.owned=owned;this.spatialCulling=spatialCulling;}
        Batch Get(Material material,Vector3 position)
        {
            var key=(material,spatialCulling?Mathf.FloorToInt(position.x/48):0,spatialCulling?Mathf.FloorToInt(position.z/48):0);
            if(!batches.TryGetValue(key,out var value)){value=new Batch{material=material};batches.Add(key,value);}
            return value;
        }
        static void Quad(Batch target,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var swap=b;b=d;d=swap;}
            int n=target.vertices.Count;
            target.vertices.AddRange(new[]{a,b,c,d});
            target.normals.AddRange(new[]{normal,normal,normal,normal});
            target.uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});
            target.triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        public void Box(Vector3 center,Vector3 size,Material material,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            var batch=Get(material,center);var p=new Vector3[8];
            for(int i=0;i<8;i++)p[i]=center+rotation*Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),size);
            Quad(batch,p[2],p[6],p[7],p[3],rotation*Vector3.up);Quad(batch,p[0],p[1],p[5],p[4],rotation*Vector3.down);
            Quad(batch,p[0],p[4],p[6],p[2],rotation*Vector3.left);Quad(batch,p[1],p[3],p[7],p[5],rotation*Vector3.right);
            Quad(batch,p[4],p[5],p[7],p[6],rotation*Vector3.forward);Quad(batch,p[0],p[2],p[3],p[1],rotation*Vector3.back);PrimitiveCount++;
        }
        public void BeveledBox(Vector3 center,Vector3 size,float bevel,Material material,Material trim=null)
        {
            // Eight vertical sides catch the light at a tower's corners without smooth
            // subdivision, additional render objects, or a collider for distant scenery.
            float x=size.x*.5f,y=size.y*.5f,z=size.z*.5f;
            bevel=Mathf.Clamp(bevel,.001f,Mathf.Min(x,z)*.8f);
            var ring=new[]{new Vector3(-x+bevel,0,-z),new Vector3(x-bevel,0,-z),new Vector3(x,0,-z+bevel),new Vector3(x,0,z-bevel),new Vector3(x-bevel,0,z),new Vector3(-x+bevel,0,z),new Vector3(-x,0,z-bevel),new Vector3(-x,0,-z+bevel)};
            var face=Get(material,center);var edge=trim?Get(trim,center):face;
            for(int i=0;i<ring.Length;i++)
            {
                Vector3 p=center+ring[i],q=center+ring[(i+1)%ring.Length],delta=q-p;
                Quad(i%2==0?face:edge,p-Vector3.up*y,q-Vector3.up*y,q+Vector3.up*y,p+Vector3.up*y,new Vector3(delta.z,0,-delta.x).normalized);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    int n=edge.vertices.Count;Vector3 offset=Vector3.up*(sign*y),normal=Vector3.up*sign;
                    edge.vertices.AddRange(new[]{center+offset,sign>0?q+offset:p+offset,sign>0?p+offset:q+offset});
                    edge.normals.AddRange(new[]{normal,normal,normal});edge.uv.AddRange(new[]{Vector2.one*.5f,Vector2.one,Vector2.zero});edge.triangles.AddRange(new[]{n,n+1,n+2});
                }
            }
            PrimitiveCount++;
        }
        public void Tube(Vector3 a,Vector3 b,float radius,Material material,int sides=10)
        {
            Vector3 axis=b-a;if(axis.sqrMagnitude<.000001f)return;axis.Normalize();
            Vector3 right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.forward:Vector3.up).normalized;
            Vector3 up=Vector3.Cross(axis,right);var batch=Get(material,(a+b)*.5f);int begin=batch.vertices.Count;
            for(int i=0;i<=sides;i++)
            {
                float angle=i*Mathf.PI*2/sides;Vector3 normal=right*Mathf.Cos(angle)+up*Mathf.Sin(angle);
                batch.vertices.Add(a+normal*radius);batch.vertices.Add(b+normal*radius);
                batch.normals.Add(normal);batch.normals.Add(normal);batch.uv.Add(new Vector2(i/(float)sides,0));batch.uv.Add(new Vector2(i/(float)sides,1));
                if(i<sides){int n=begin+i*2;batch.triangles.AddRange(new[]{n,n+3,n+1,n,n+2,n+3});}
            }
            // Small rounded caps make isolated rail ends readable without separate sphere objects.
            int cap=batch.vertices.Count;batch.vertices.Add(a);batch.vertices.Add(b);batch.normals.Add(-axis);batch.normals.Add(axis);batch.uv.Add(Vector2.one*.5f);batch.uv.Add(Vector2.one*.5f);
            for(int i=0;i<sides;i++)batch.triangles.AddRange(new[]{cap,begin+(i+1)*2,begin+i*2,cap+1,begin+i*2+1,begin+(i+1)*2+1});
            PrimitiveCount++;
        }
        public void Ellipsoid(Vector3 center,Vector3 radii,Material material,int longitude=12,int latitude=7)
        {
            var batch=Get(material,center);int begin=batch.vertices.Count;
            for(int y=0;y<=latitude;y++)for(int x=0;x<=longitude;x++)
            {
                float a=x*Mathf.PI*2/longitude,b=y*Mathf.PI/latitude;
                Vector3 normal=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));
                batch.vertices.Add(center+Vector3.Scale(normal,radii));batch.normals.Add(new Vector3(normal.x/radii.x,normal.y/radii.y,normal.z/radii.z).normalized);batch.uv.Add(new Vector2(x/(float)longitude,y/(float)latitude));
                if(y<latitude&&x<longitude){int n=begin+y*(longitude+1)+x;batch.triangles.AddRange(new[]{n,n+1,n+longitude+1,n+1,n+longitude+2,n+longitude+1});}
            }
            PrimitiveCount++;
        }
        public void Terrace(Vector3 center,float radius,float thickness,float taper,Material material,int seed,int sides=12)
        {
            var batch=Get(material,center);float offset=(seed%11)*.07f;
            for(int i=0;i<sides;i++)
            {
                float a=(i+.5f)*Mathf.PI*2/sides,b=(i+1.5f)*Mathf.PI*2/sides;
                float ra=radius*(1+Mathf.Sin(i*7.13f+seed)*.075f),rb=radius*(1+Mathf.Sin((i+1)*7.13f+seed)*.075f);
                var p=center+new Vector3(Mathf.Cos(a+offset)*ra,0,Mathf.Sin(a+offset)*ra);
                var q=center+new Vector3(Mathf.Cos(b+offset)*rb,0,Mathf.Sin(b+offset)*rb);
                var loP=center+Vector3.Scale(p-center,new Vector3(taper,1,taper))+Vector3.down*thickness;
                var loQ=center+Vector3.Scale(q-center,new Vector3(taper,1,taper))+Vector3.down*thickness;
                var normal=Vector3.Cross(loQ-p,loP-p).normalized;Quad(batch,p,loP,loQ,q,normal);
                int top=batch.vertices.Count;batch.vertices.AddRange(new[]{center,q,p});batch.normals.AddRange(new[]{Vector3.up,Vector3.up,Vector3.up});batch.uv.AddRange(new[]{Vector2.one*.5f,Vector2.one,Vector2.zero});batch.triangles.AddRange(new[]{top,top+1,top+2});
            }
            PrimitiveCount++;
        }
        public void Tree(Vector3 ground,float scale,Material bark,Material leaves,Material lightLeaves,int seed,bool snow=false)
        {
            Tube(ground,ground+Vector3.up*3.2f*scale,.22f*scale,bark,7);
            if(snow)
            {
                for(int i=0;i<3;i++)Terrace(ground+Vector3.up*(3.3f+i*.9f)*scale,.04f*scale,1.8f*scale,(1.5f-i*.32f)/.04f,i==2?lightLeaves:leaves,seed+i,9);
            }
            else
            {
                Ellipsoid(ground+Vector3.up*3.7f*scale,new Vector3(1.8f,2,1.7f)*scale,leaves,10,6);
                Ellipsoid(ground+new Vector3(.9f,4.5f,.2f)*scale,new Vector3(1.3f,1.5f,1.3f)*scale,lightLeaves,10,6);
                Ellipsoid(ground+new Vector3(-1.1f,3.9f,.3f)*scale,new Vector3(1.1f,1.4f,1.2f)*scale,leaves,10,6);
            }
        }
        public void FloatingIsland(Vector3 center,float radius,Material top,Material cliff,Material shadowCliff,Material bark,Material leaves,Material lightLeaves,int seed,bool snow=false)
        {
            Terrace(center,radius,.65f,1.06f,top,seed,14);
            Terrace(center+Vector3.down*.7f,radius*1.055f,2.3f,.84f,cliff,seed,14);
            Terrace(center+Vector3.down*3,radius*.89f,3.5f,.62f,shadowCliff,seed,14);
            Terrace(center+Vector3.down*6.5f,radius*.55f,4.1f,.06f,cliff,seed,14);
            for(int i=0;i<3;i++)
            {
                float angle=(i*2.1f+seed)*1.3f;
                Vector3 point=center+new Vector3(Mathf.Cos(angle)*radius*.48f,.07f,Mathf.Sin(angle)*radius*.48f);
                Tree(point,.65f+i*.12f,bark,leaves,lightLeaves,seed+i,snow);
            }
            Ellipsoid(center+new Vector3(radius*.38f,.4f,-radius*.34f),new Vector3(1.1f,.8f,1.3f),shadowCliff,8,5);
            Ellipsoid(center+new Vector3(radius*.58f,.2f,-radius*.18f),new Vector3(.7f,.5f,.8f),cliff,8,5);
        }
        public void DeckEdge(Vector3 a,Vector3 b,Vector3 outward,Material cap,Material side,Material inset)
        {
            var up=Vector3.up;
            Tube(a-up*.08f,b-up*.08f,.09f,cap,8);
            Tube(a-up*.62f,b-up*.62f,.065f,inset,8);
            var batch=Get(side,(a+b)*.5f);
            Quad(batch,a-up*.12f+outward*.03f,a-up*.57f+outward*.03f,b-up*.57f+outward*.03f,b-up*.12f+outward*.03f,outward);
        }
        public void Flush()
        {
            foreach(var pair in batches)
            {
                var data=pair.Value;if(data.vertices.Count==0)continue;
                var mesh=new Mesh{name="Premium scenery and track batch"};if(data.vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(data.vertices);mesh.SetNormals(data.normals);mesh.SetUVs(0,data.uv);mesh.SetTriangles(data.triangles,0);mesh.RecalculateBounds();owned.Add(mesh);
                var go=ArcadeGeometry.MeshObject("Premium mesh batch "+data.material.name,parent,mesh,data.material);
                go.GetComponent<Renderer>().shadowCastingMode=data.material.IsKeywordEnabled("_EMISSION")?ShadowCastingMode.Off:ShadowCastingMode.On;
                go.GetComponent<Renderer>().receiveShadows=true;RendererCount++;
            }
            batches.Clear();
        }
    }
}
