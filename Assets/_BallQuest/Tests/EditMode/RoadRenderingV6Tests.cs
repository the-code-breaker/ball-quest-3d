using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BallQuest.Tests
{
    public sealed class RoadRenderingV6Tests
    {
        readonly List<Mesh> meshes=new List<Mesh>();
        static TrackPiece Piece(float x,float z,float width,float length,float yaw=0,float y=0) => new TrackPiece {
            position=new Vector3(x,y-.4f,z),size=new Vector3(width,.8f,length),rotation=new Vector3(0,yaw,0)
        };
        Mesh[] Build(params TrackPiece[] pieces){var value=RoadRenderGeometry.Build(pieces);meshes.AddRange(value);return value;}

        [Test]
        public void AdjacentPaddedRoadsDrawTheirTopOnlyOnceWithoutLosingCoverage()
        {
            var roads=Build(Piece(0,0,8,4.1f),Piece(0,4,8,4.1f));
            Assert.AreEqual(64.8f,TopArea(roads),.002f,"The 0.1m collision padding must contribute only once to visible area.");
            for(float x=-3.843f;x<4;x+=.413f)for(float z=-1.923f;z<6.05f;z+=.193f)
                Assert.AreEqual(1,TopCoverage(roads,new Vector2(x,z)),"Missing or duplicate road at "+x+", "+z);
        }

        [Test]
        public void MazeAreaAndCrossingBranchesHaveOneVisibleSurfaceAtEveryCoveredPoint()
        {
            // These dimensions reproduce the actual Sky Maze area/branch overlap pattern.
            var roads=Build(Piece(0,9,10,26),Piece(0,22,50,10),Piece(0,52,7,60),Piece(-20,52,3.8f,60),Piece(-26,46,4,12,90));
            for(float x=-32.173f;x<25;x+=.731f)for(float z=-3.813f;z<82;z+=.613f)
            {
                bool covered=InRect(x,z,0,9,10,26)||InRect(x,z,0,22,50,10)||InRect(x,z,0,52,7,60)||InRect(x,z,-20,52,3.8f,60)||InRect(x,z,-26,46,12,4);
                Assert.AreEqual(covered?1:0,TopCoverage(roads,new Vector2(x,z)),"Coverage at "+x+", "+z);
            }
        }

        [Test]
        public void SeparateElevationsRemainVisibleAndSidewaysRoadKeepsTextureScale()
        {
            var roads=Build(Piece(0,0,8,10),Piece(0,0,8,10,y:3),Piece(20,0,8,10,90));
            Assert.AreEqual(240,TopArea(roads),.01f,"A higher road is not a coplanar overlap.");
            var mesh=roads[2];var uv=mesh.uv;var normals=mesh.normals;
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            for(int i=0;i<uv.Length;i++)if(normals[i].y>.99f){min=Mathf.Min(min,uv[i].y);max=Mathf.Max(max,uv[i].y);}
            Assert.AreEqual(1.9f,max-min,.001f,"Texture distance must follow a sideways road, rather than collapse because world Z is constant.");
        }

        [Test]
        public void GravityLoopIsAContinuousFloorWallCeilingMeshWithNoOverlappingTopPatches()
        {
            var center=new Vector3(0,30,24);var mesh=ArcadeGeometry.HalfLoopRoad(center,12,8,.8f);meshes.Add(mesh);
            var vertices=mesh.vertices;var triangles=mesh.triangles;var normals=mesh.normals;
            int tops=0;float area=0;bool floor=false,wall=false,ceiling=false;
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                bool inner=OnRadius(vertices[a])&&OnRadius(vertices[b])&&OnRadius(vertices[c]);
                if(!inner)continue;
                tops++;area+=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).magnitude*.5f;
                floor|=normals[a].y>.99f;wall|=normals[a].z<-.99f;ceiling|=normals[a].y<-.99f;
                Assert.Greater(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).normalized,(normals[a]+normals[b]+normals[c]).normalized),.99f);
            }
            Assert.AreEqual(72,tops);Assert.AreEqual(8*24*Mathf.Sin(Mathf.PI/72)*36,area,.01f);Assert.IsTrue(floor&&wall&&ceiling);
            bool OnRadius(Vector3 value){var delta=value-center;delta.x=0;return Mathf.Abs(delta.magnitude-12)<.001f;}
        }

        static bool InRect(float x,float z,float cx,float cz,float width,float length) => Mathf.Abs(x-cx)<width*.5f&&Mathf.Abs(z-cz)<length*.5f;
        static float TopArea(Mesh[] meshes)
        {
            float area=0;
            foreach(var mesh in meshes)
            {
                var vertices=mesh.vertices;var triangles=mesh.triangles;var normals=mesh.normals;
                for(int i=0;i<triangles.Length;i+=3)if(normals[triangles[i]].y>.99f)
                    area+=Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).magnitude*.5f;
            }
            return area;
        }
        static int TopCoverage(Mesh[] meshes,Vector2 point)
        {
            int count=0;
            foreach(var mesh in meshes)
            {
                var vertices=mesh.vertices;var triangles=mesh.triangles;var normals=mesh.normals;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    if(normals[triangles[i]].y<.99f)continue;
                    Vector3 pa=vertices[triangles[i]],pb=vertices[triangles[i+1]],pc=vertices[triangles[i+2]];
                    var a=new Vector2(pa.x,pa.z);var b=new Vector2(pb.x,pb.z);var c=new Vector2(pc.x,pc.z);
                    float d1=Cross(b-a,point-a),d2=Cross(c-b,point-b),d3=Cross(a-c,point-c);
                    if(d1<-.000001f&&d2<-.000001f&&d3<-.000001f||d1>.000001f&&d2>.000001f&&d3>.000001f)count++;
                }
            }
            return count;
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        [TearDown]public void Cleanup(){foreach(var mesh in meshes)if(mesh)Object.DestroyImmediate(mesh);meshes.Clear();}
    }
}
