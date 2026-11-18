using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    /// <summary>Small authored mesh recipes shared by every reward and accelerator in a course.</summary>
    public static class PremiumPickupGeometry
    {
        sealed class Builder
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Vector3> normals=new List<Vector3>();
            readonly List<Vector2> uvs=new List<Vector2>();
            readonly List<int> triangles=new List<int>();
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3 normal)
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var swap=b;b=c;c=swap;}
                int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                normals.Add(normal);normals.Add(normal);normals.Add(normal);
                uvs.Add(new Vector2(a.x+.5f,a.z+.5f));uvs.Add(new Vector2(b.x+.5f,b.z+.5f));uvs.Add(new Vector2(c.x+.5f,c.z+.5f));
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
            {Triangle(a,b,c,normal);Triangle(a,c,d,normal);}
            public void SmoothQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 na,Vector3 nb,Vector3 nc,Vector3 nd)
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),na+nb+nc)<0)
                {var swap=b;b=d;d=swap;swap=nb;nb=nd;nd=swap;}
                int start=vertices.Count;vertices.AddRange(new[]{a,b,c,d});normals.AddRange(new[]{na,nb,nc,nd});
                uvs.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            public Mesh Finish(string name)
            {var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;}
        }

        // Profile is axial depth/radius. The raised outer rim and bevel catch sunlight
        // while both inset faces have a real thickness rather than a one-sided decal.
        public static Mesh CoinBody()
        {
            var b=new Builder();const int segments=48;
            var profile=new[]{new Vector2(-.14f,.34f),new Vector2(-.14f,.43f),new Vector2(-.105f,.49f),new Vector2(-.065f,.52f),new Vector2(.065f,.52f),new Vector2(.105f,.49f),new Vector2(.14f,.43f),new Vector2(.14f,.34f)};
            for(int ring=0;ring<profile.Length-1;ring++)for(int n=0;n<segments;n++)
            {
                float a=n*Mathf.PI*2/segments,z=(n+1)*Mathf.PI*2/segments;
                Vector2 p=profile[ring],q=profile[ring+1];
                Vector3 v0=Radial(a,p.y,p.x),v1=Radial(z,p.y,p.x),v2=Radial(z,q.y,q.x),v3=Radial(a,q.y,q.x);
                Vector2 slope=new Vector2(q.x-p.x,p.y-q.y).normalized;
                b.SmoothQuad(v0,v1,v2,v3,new Vector3(Mathf.Cos(a)*slope.x,Mathf.Sin(a)*slope.x,slope.y),new Vector3(Mathf.Cos(z)*slope.x,Mathf.Sin(z)*slope.x,slope.y),new Vector3(Mathf.Cos(z)*slope.x,Mathf.Sin(z)*slope.x,slope.y),new Vector3(Mathf.Cos(a)*slope.x,Mathf.Sin(a)*slope.x,slope.y));
            }
            for(int side=-1;side<=1;side+=2)for(int n=0;n<segments;n++)
            {
                float a=n*Mathf.PI*2/segments,z=(n+1)*Mathf.PI*2/segments;
                b.Triangle(new Vector3(0,0,side*.092f),Radial(a,.341f,side*.092f),Radial(z,.341f,side*.092f),Vector3.forward*side);
                b.Quad(Radial(a,.34f,side*.092f),Radial(z,.34f,side*.092f),Radial(z,.34f,side*.115f),Radial(a,.34f,side*.115f),new Vector3(-Mathf.Cos(a),-Mathf.Sin(a),0));
            }
            return b.Finish("Reward coin - bevel, inset faces and machined rim");
        }
        public static Mesh CoinEmblem()
        {
            var b=new Builder();
            for(int side=-1;side<=1;side+=2)
            {
                const float front=.151f,back=.096f;
                var star=new Vector3[10];
                for(int i=0;i<10;i++){float angle=(90+i*36)*Mathf.Deg2Rad;star[i]=Radial(angle,i%2==0?.30f:.148f,side*front);}
                for(int i=0;i<10;i++)
                {
                    int next=(i+1)%10;var crown=new Vector3(0,0,side*(front+.067f));
                    // A real peaked face needs sloped facet normals. Giving all ten
                    // triangles a forward normal makes even raised geometry look flat.
                    var normal=Vector3.Cross(star[i]-crown,star[next]-crown).normalized;
                    if(normal.z*side<0)normal=-normal;
                    b.Triangle(crown,star[i],star[next],normal);
                    var a=star[i];var z=star[next];var c=new Vector3(z.x,z.y,side*back);var d=new Vector3(a.x,a.y,side*back);
                    b.Quad(a,z,c,d,new Vector3(a.x+z.x,a.y+z.y,0).normalized);
                }
                // Polished bevel border is part of the same highlight draw call.
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI*2/48,z=(i+1)*Mathf.PI*2/48;
                    var v0=Radial(a,.404f,side*.147f);var v1=Radial(z,.404f,side*.147f);
                    var v2=Radial(z,.448f,side*.137f);var v3=Radial(a,.448f,side*.137f);
                    var normal=Vector3.Cross(v1-v0,v2-v0).normalized;if(normal.z*side<0)normal=-normal;
                    b.Quad(v0,v1,v2,v3,normal);
                }
                // Shallow intermittent edge reeding, merged into the emblem mesh.
                for(int i=0;i<32;i++)
                {
                    float a=(i+.08f)*Mathf.PI*2/32,z=(i+.43f)*Mathf.PI*2/32;
                    Vector3 ridge=Radial((a+z)*.5f,.477f,side*.15f);
                    b.Triangle(Radial(a,.456f,side*.137f),Radial(z,.456f,side*.137f),ridge,(Vector3.forward*side+Radial((a+z)*.5f,1,0)*.3f).normalized);
                    b.Triangle(Radial(z,.492f,side*.108f),Radial(a,.492f,side*.108f),ridge,(Vector3.forward*side+Radial((a+z)*.5f,1,0)*.6f).normalized);
                }
            }
            return b.Finish("Double sided raised star and engraved rim highlights");
        }
        static Vector3 Radial(float angle,float radius,float depth)=>new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,depth);

        public static Mesh Jewel(bool crown)
        {
            var b=new Builder();const int sides=8;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,z=(i+1)*Mathf.PI*2/sides;
                var p=new Vector3(Mathf.Cos(a)*.37f,.08f,Mathf.Sin(a)*.37f);var q=new Vector3(Mathf.Cos(z)*.37f,.08f,Mathf.Sin(z)*.37f);
                if(crown)
                {
                    var r=new Vector3(Mathf.Cos(z)*.19f,.32f,Mathf.Sin(z)*.19f);var s=new Vector3(Mathf.Cos(a)*.19f,.32f,Mathf.Sin(a)*.19f);
                    b.Quad(p,q,r,s,Vector3.Cross(r-p,q-p).normalized);
                    b.Triangle(Vector3.up*.32f,s,r,Vector3.up);
                }
                else b.Triangle(p,new Vector3(0,-.43f,0),q,Vector3.Cross(q-p,new Vector3(0,-.43f,0)-p).normalized);
            }
            return b.Finish(crown?"Jewel crown and polished table":"Jewel pavilion facets");
        }

        public static Mesh PadHousing()
        {
            var b=new Builder();
            // A recessed, continuous cassette seats the three raised drive plates.
            // Their thick dark bevels remain separate from the bright inset faces.
            PadTray(b);
            for(int row=0;row<3;row++)PadChevron(b,-.29f+row*.29f,.375f,.063f,.014f,.025f);
            for(int side=-1;side<=1;side+=2)PadRail(b,side,false);
            for(int end=-1;end<=1;end+=2)
            {
                PadBox(b,new Vector3(0,-.08f,end*.44f),new Vector3(.88f,.05f,.055f));
            }
            // Raised cross-ribs read as a manufactured runway rather than a flat tile.
            for(int i=0;i<6;i++)PadBox(b,new Vector3(0,.019f,-.40f+i*.16f),new Vector3(.78f,.014f,.022f));
            return b.Finish("Recessed accelerator cassette - three dark beveled plinths and curved fins");
        }
        public static Mesh PadTrim()
        {
            var b=new Builder();
            for(int row=0;row<3;row++)
            {
                var face=Chevron(.329f,-.29f+row*.29f+.002f,.071f,.104f,.146f);
                b.Quad(face[0],face[1],face[4],face[5],Vector3.up);b.Quad(face[1],face[2],face[3],face[4],Vector3.up);
            }
            for(int side=-1;side<=1;side+=2)PadRail(b,side,true);
            for(int end=-1;end<=1;end+=2)PadBox(b,new Vector3(0,.023f,end*.455f),new Vector3(.86f,.018f,.028f));
            // Recessed fastener heads belong to the same alloy mesh/material.
            for(int side=-1;side<=1;side+=2)for(int row=0;row<3;row++)
            {
                Vector3 p=new Vector3(side*.362f,.019f,-.29f+row*.29f);
                const int count=8;for(int i=0;i<count;i++)
                {float a=i*Mathf.PI*2/count,z=(i+1)*Mathf.PI*2/count;b.Triangle(p,p+new Vector3(Mathf.Cos(a)*.01f,0,Mathf.Sin(a)*.01f),p+new Vector3(Mathf.Cos(z)*.01f,0,Mathf.Sin(z)*.01f),Vector3.up);}
            }
            return b.Finish("Sculpted pearl-alloy chevron plates and fin bevels");
        }
        public static Mesh PadArrows()
        {
            var b=new Builder();
            for(int row=0;row<3;row++)
            {
                float z=-.29f+row*.29f;
                var path=Chevron(.266f,z+.032f,.074f,.025f,.117f);
                b.Quad(path[0],path[1],path[4],path[5],Vector3.up);b.Quad(path[1],path[2],path[3],path[4],Vector3.up);
            }
            // Six inset lamps per fin provide depth and motion cues without another
            // renderer, particle system or per-pad material instance.
            for(int side=-1;side<=1;side+=2)for(int n=0;n<6;n++)
            {
                float z=-.34f+n*.13f,u=(z+.47f)/.94f;
                float x=side*(.417f+.012f*Mathf.Sin(u*Mathf.PI));
                float y=.058f+.165f*Mathf.Sin(u*Mathf.PI)-.010f;
                b.Quad(new Vector3(x-.012f,y,z-.027f),new Vector3(x+.012f,y,z-.027f),new Vector3(x+.012f,y,z+.027f),new Vector3(x-.012f,y,z+.027f),Vector3.up);
            }
            return b.Finish("Recessed drive chevrons and segmented fin lamps");
        }
        static Vector3[] Chevron(float width,float z,float y,float band,float rise=.15f)
            =>new[]{new Vector3(-width,y,z-.095f),new Vector3(0,y,z-.095f+rise),new Vector3(width,y,z-.095f),new Vector3(width,y,z-.095f+band),new Vector3(0,y,z-.095f+rise+band),new Vector3(-width,y,z-.095f+band)};
        static void PadTray(Builder b)
        {
            var outer=Rounded(.499f,.49f,.055f,-.025f);var inner=Rounded(.468f,.46f,.05f,.014f);
            for(int i=0;i<outer.Length;i++)
            {
                int next=(i+1)%outer.Length;
                b.Triangle(new Vector3(0,.014f,0),inner[i],inner[next],Vector3.up);
                Vector3 outward=new Vector3(outer[i].x+outer[next].x,0,outer[i].z+outer[next].z).normalized;
                PadFace(b,outer[i],outer[next],inner[next],inner[i],outward+Vector3.up);
                Vector3 low=outer[i];low.y=-.115f;Vector3 lowNext=outer[next];lowNext.y=-.115f;
                PadFace(b,low,lowNext,outer[next],outer[i],outward);
            }
        }
        static void PadChevron(Builder b,float z,float width,float top,float bottom,float bevel)
        {
            var outer=Chevron(width,z,top-bevel,.128f);
            var upper=Chevron(width*.948f,z,top,.112f);
            for(int i=0;i<upper.Length;i++)upper[i].z=z+(upper[i].z-z)*.94f;
            b.Quad(upper[0],upper[1],upper[4],upper[5],Vector3.up);b.Quad(upper[1],upper[2],upper[3],upper[4],Vector3.up);
            for(int i=0;i<outer.Length;i++)
            {
                int next=(i+1)%outer.Length;
                Vector3 edge=outer[next]-outer[i],outward=new Vector3(edge.z,0,-edge.x).normalized;
                PadFace(b,outer[i],outer[next],upper[next],upper[i],outward+Vector3.up);
                Vector3 low=outer[i];low.y=bottom;Vector3 lowNext=outer[next];lowNext.y=bottom;
                PadFace(b,low,lowNext,outer[next],outer[i],outward);
            }
        }
        static void PadRail(Builder b,int side,bool trim)
        {
            const int segments=16;
            for(int n=0;n<segments;n++)
            {
                Vector3[] a=RailSection(side,n/(float)segments),z=RailSection(side,(n+1f)/segments);
                if(trim)
                {
                    // Bright curved endcaps and a narrow outer rim frame a long dark
                    // lamp recess. Covering the entire shoulder read as a white bar.
                    float fraction=n<4||n>=12?1:.22f;
                    PadFace(b,a[3],z[3],Vector3.Lerp(z[3],z[4],fraction),Vector3.Lerp(a[3],a[4],fraction),Vector3.up-Vector3.right*side);
                    continue;
                }
                for(int edge=0;edge<5;edge++)
                {
                    if(edge==3)
                    {
                        if(n>=4&&n<12)PadFace(b,Vector3.Lerp(a[3],a[4],.22f),Vector3.Lerp(z[3],z[4],.22f),z[4],a[4],Vector3.up-Vector3.right*side);
                        continue;
                    }
                    int next=(edge+1)%5;
                    Vector3 outward=edge==0?Vector3.down:edge==4?-Vector3.right*side:edge==2?Vector3.up+Vector3.right*side:Vector3.right*side;
                    PadFace(b,a[edge],z[edge],z[next],a[next],outward);
                }
                if(n==0)for(int edge=1;edge<4;edge++)b.Triangle(a[0],a[edge],a[edge+1],Vector3.back);
                if(n==segments-1)for(int edge=1;edge<4;edge++)b.Triangle(z[0],z[edge],z[edge+1],Vector3.forward);
            }
        }
        static Vector3[] RailSection(int side,float u)
        {
            float z=Mathf.Lerp(-.47f,.47f,u),arch=Mathf.Sin(u*Mathf.PI);
            float center=side*(.437f+.012f*arch),top=.058f+.165f*arch;
            return new[]{new Vector3(center-side*.055f,-.12f,z),new Vector3(center+side*.055f,-.12f,z),new Vector3(center+side*.055f,top-.035f,z),new Vector3(center+side*.017f,top,z),new Vector3(center-side*.055f,top-.025f,z)};
        }
        static void PadBox(Builder b,Vector3 center,Vector3 size)
        {
            Vector3 lo=center-size*.5f,hi=center+size*.5f;
            b.Quad(new Vector3(lo.x,hi.y,lo.z),new Vector3(hi.x,hi.y,lo.z),new Vector3(hi.x,hi.y,hi.z),new Vector3(lo.x,hi.y,hi.z),Vector3.up);
            b.Quad(new Vector3(lo.x,lo.y,lo.z),new Vector3(lo.x,hi.y,lo.z),new Vector3(hi.x,hi.y,lo.z),new Vector3(hi.x,lo.y,lo.z),Vector3.back);
            b.Quad(new Vector3(hi.x,lo.y,hi.z),new Vector3(hi.x,hi.y,hi.z),new Vector3(lo.x,hi.y,hi.z),new Vector3(lo.x,lo.y,hi.z),Vector3.forward);
            b.Quad(new Vector3(lo.x,lo.y,hi.z),new Vector3(lo.x,hi.y,hi.z),new Vector3(lo.x,hi.y,lo.z),new Vector3(lo.x,lo.y,lo.z),Vector3.left);
            b.Quad(new Vector3(hi.x,lo.y,lo.z),new Vector3(hi.x,hi.y,lo.z),new Vector3(hi.x,hi.y,hi.z),new Vector3(hi.x,lo.y,hi.z),Vector3.right);
        }
        static void PadFace(Builder b,Vector3 a,Vector3 z,Vector3 c,Vector3 d,Vector3 expected)
        {
            Vector3 normal=Vector3.Cross(z-a,c-a).normalized;
            if(Vector3.Dot(normal,expected)<0)normal=-normal;
            if(normal.sqrMagnitude>.5f)b.Quad(a,z,c,d,normal);
        }
        static Vector3[] Rounded(float x,float z,float radius,float y)
        {
            const int perCorner=7;var points=new Vector3[perCorner*4];
            for(int corner=0;corner<4;corner++)for(int n=0;n<perCorner;n++)
            {
                float a=(corner*90+n*90f/(perCorner-1))*Mathf.Deg2Rad;
                float cx=corner==0||corner==3?x-radius:-x+radius,cz=corner<2?z-radius:-z+radius;
                points[corner*perCorner+n]=new Vector3(cx+Mathf.Cos(a)*radius,y,cz+Mathf.Sin(a)*radius);
            }
            return points;
        }
        public static Mesh Ring()
        {
            var b=new Builder();const int count=48;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count,z=(i+1)*Mathf.PI*2/count;
                b.Quad(Radial(a,.5f,0),Radial(z,.5f,0),Radial(z,.455f,0),Radial(a,.455f,0),Vector3.back);
                b.Quad(Radial(a,.5f,0),Radial(z,.5f,0),Radial(z,.455f,0),Radial(a,.455f,0),Vector3.forward);
            }
            return b.Finish("Pooled reward energy ring");
        }
    }
}
