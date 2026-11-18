using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BallQuest.Tests
{
    public sealed class PremiumTrackV7Tests
    {
        readonly List<Mesh> meshes=new List<Mesh>();
        GameObject root;
        Material material;
        [SetUp]public void Setup()
        {
            root=new GameObject("Premium geometry validation");
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        [TearDown]public void Cleanup()
        {
            Object.DestroyImmediate(root);Object.DestroyImmediate(material);
            foreach(var mesh in meshes)if(mesh)Object.DestroyImmediate(mesh);meshes.Clear();
        }
        [Test]
        public void HundredsOfRoundedRailSegmentsUseSpatialBatchesWithoutDecorativeColliders()
        {
            var kit=new PremiumTrackPresentation(root.transform,meshes);
            for(int i=0;i<200;i++)kit.Tube(new Vector3(-4,.3f,i*1.35f),new Vector3(-4,.3f,(i+1)*1.35f),.21f,material,12);
            kit.Flush();
            Assert.AreEqual(200,kit.PrimitiveCount);
            Assert.LessOrEqual(root.GetComponentsInChildren<Renderer>().Length,7,"Repeated rail pieces should be direct geometry batches, not hundreds of renderer objects.");
            Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
            foreach(var mesh in meshes)
            {
                Assert.Greater(mesh.vertexCount,20);Assert.IsFalse(float.IsNaN(mesh.bounds.size.sqrMagnitude));
                Assert.Greater(mesh.bounds.size.z,1);
            }
        }
        [Test]
        public void SculptedSceneryFacesPointOutwardAndRemainFiniteAtPolesAndCaps()
        {
            var kit=new PremiumTrackPresentation(root.transform,meshes);
            kit.Tube(new Vector3(0,0,0),new Vector3(0,0,4),.4f,material,12);
            kit.Ellipsoid(new Vector3(4,3,0),new Vector3(2,3,1),material);
            kit.Terrace(new Vector3(9,3,0),3,4,.3f,material,4);
            kit.Box(new Vector3(14,3,0),new Vector3(3,2,4),material,Quaternion.Euler(13,29,7));
            kit.BeveledBox(new Vector3(20,3,0),new Vector3(5,12,7),.5f,material);
            kit.Flush();int examined=0;
            foreach(var mesh in meshes)
            {
                var v=mesh.vertices;var normals=mesh.normals;var t=mesh.triangles;
                for(int n=0;n<t.Length;n+=3)
                {
                    var cross=Vector3.Cross(v[t[n+1]]-v[t[n]],v[t[n+2]]-v[t[n]]);
                    if(cross.sqrMagnitude<.000001f)continue;
                    var expected=normals[t[n]]+normals[t[n+1]]+normals[t[n+2]];
                    Assert.Greater(Vector3.Dot(cross.normalized,expected.normalized),.12f,"A visible mesh face is reversed relative to its lighting normals.");examined++;
                }
            }
            Assert.Greater(examined,200);
        }
        [Test]
        public void WideRoadsRetainTheSamePlankScaleAsNarrowRoads()
        {
            var parts=new[]{new TrackPiece{position=Vector3.zero,size=new Vector3(7,.8f,10)},new TrackPiece{position=Vector3.right*30,size=new Vector3(21,.8f,10)}};
            var roads=RoadRenderGeometry.Build(parts);meshes.AddRange(roads);
            for(int i=0;i<roads.Length;i++)
            {
                float min=float.PositiveInfinity,max=float.NegativeInfinity;var normal=roads[i].normals;var uv=roads[i].uv;
                for(int n=0;n<uv.Length;n++)if(normal[n].y>.99f){min=Mathf.Min(min,uv[n].x);max=Mathf.Max(max,uv[n].x);}
                Assert.AreEqual(parts[i].size.x/7,max-min,.001f,"A large junction must not stretch one enormous plank across the whole surface.");
            }
        }
    }
}
