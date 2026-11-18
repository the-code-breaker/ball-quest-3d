using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    public sealed class SignatureRuntimeResources : MonoBehaviour
    {
        public readonly List<Material> materials=new List<Material>();
        public readonly List<Mesh> meshes=new List<Mesh>();
        void OnDestroy(){foreach(var value in materials)if(value)Destroy(value);foreach(var value in meshes)if(value)Destroy(value);}
    }

}
