using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BallQuest
{
    // Actual material batches retain spatial culling. Source collider solids remain intact.
    public static class CourseRenderBatcher
    {
        readonly struct Key : IEquatable<Key>
        {
            public readonly Material material;
            public readonly int x,z;
            public readonly ShadowCastingMode shadows;
            public readonly bool receive;
            public Key(Material material,Vector3 point,ShadowCastingMode shadows,bool receive)
            {this.material=material;x=Mathf.FloorToInt(point.x/32);z=Mathf.FloorToInt(point.z/32);this.shadows=shadows;this.receive=receive;}
            public bool Equals(Key other)=>material==other.material&&x==other.x&&z==other.z&&shadows==other.shadows&&receive==other.receive;
            public override bool Equals(object other)=>other is Key key&&Equals(key);
            public override int GetHashCode()=>((material?material.GetInstanceID():0)*397^x)*397^z*31^(int)shadows^(receive?1:0);
        }
        public static int Combine(Transform root,List<Mesh> ownedMeshes)
        {
            var groups=new Dictionary<Key,List<CombineInstance>>();
            var sources=new List<MeshRenderer>();
            Matrix4x4 local=root.worldToLocalMatrix;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.GetComponentInParent<Rigidbody>()||renderer.GetComponent<TMPro.TMP_Text>())continue;
                if(!renderer.TryGetComponent<MeshFilter>(out var filter)||!filter.sharedMesh)continue;
                var materials=renderer.sharedMaterials;
                if(materials.Length!=1||filter.sharedMesh.subMeshCount!=1||!materials[0]||materials[0].renderQueue>=3000)continue;
                var key=new Key(materials[0],renderer.bounds.center,renderer.shadowCastingMode,renderer.receiveShadows);
                if(!groups.TryGetValue(key,out var instances)){instances=new List<CombineInstance>();groups.Add(key,instances);}
                instances.Add(new CombineInstance{mesh=filter.sharedMesh,transform=local*filter.transform.localToWorldMatrix});sources.Add(renderer);
            }
            int batches=0;
            foreach(var group in groups)
            {
                var mesh=new Mesh{name="Course batch "+group.Key.material.name+" "+group.Key.x+","+group.Key.z,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(group.Value.ToArray(),true,true);ownedMeshes.Add(mesh);
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.layer=2;
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var output=go.GetComponent<MeshRenderer>();output.sharedMaterial=group.Key.material;output.shadowCastingMode=group.Key.shadows;output.receiveShadows=group.Key.receive;
                batches++;
            }
            foreach(var renderer in sources){renderer.enabled=false;UnityEngine.Object.Destroy(renderer);}
            return batches;
        }
    }
}
