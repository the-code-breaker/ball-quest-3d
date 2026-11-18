using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BallQuest
{
    // The cosmetic shell is geometry attached to the actual physics sphere, so markings roll naturally.
    public sealed class BallLook : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        Texture2D texture;
        Transform shell;
        static Mesh sphere;
        static Cubemap reflection;
        static Mesh sharedShoulders,sharedBevels,sharedInsets;
        public static Cubemap EnvironmentReflection { get { if(!reflection)reflection=StudioReflection();return reflection; } }

        public static BallLook Apply(GameObject target,BallSkin skin,GameCatalog catalog)
        {
            var look=target.GetComponent<BallLook>();
            if(!look)look=target.AddComponent<BallLook>();
            look.Clear();look.Build(skin,catalog);return look;
        }

        void Build(BallSkin skin,GameCatalog catalog)
        {
            var baseRenderer=GetComponent<MeshRenderer>();
            if(!baseRenderer)baseRenderer=gameObject.AddComponent<MeshRenderer>();
            var filter=GetComponent<MeshFilter>();if(!filter)filter=gameObject.AddComponent<MeshFilter>();
            if(!sphere)sphere=SphereMesh();filter.sharedMesh=sphere;
            reflection=EnvironmentReflection;
            var probe=GetComponent<ReflectionProbe>();if(!probe)probe=gameObject.AddComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=reflection;probe.size=Vector3.one*2.4f;probe.blendDistance=.2f;probe.intensity=1;probe.boxProjection=false;
            baseRenderer.reflectionProbeUsage=ReflectionProbeUsage.Simple;
            texture=QuestVisuals.SkinTexture(skin);
            bool turbine=skin.artStyle==BallArtStyle.Turbine;
            var core=CreateMaterial(catalog,"Sculpted "+skin.displayName,Color.white,skin.metallic,skin.smoothness);
            core.SetTexture("_BaseMap",texture);baseRenderer.sharedMaterial=core;
            baseRenderer.shadowCastingMode=ShadowCastingMode.On;baseRenderer.receiveShadows=true;
            if(skin.artStyle==BallArtStyle.Galaxy||skin.artStyle==BallArtStyle.Lava)
            {core.EnableKeyword("_EMISSION");core.SetTexture("_EmissionMap",texture);core.SetColor("_EmissionColor",skin.artStyle==BallArtStyle.Galaxy?new Color(.3f,.15f,.7f):new Color(.8f,.2f,.03f));}
            shell=new GameObject("Precision ball shell").transform;shell.SetParent(transform,false);
            if(turbine)
            {
                var edge=CreateMaterial(catalog,"Anodized turbine rims",skin.color,.78f,.79f);
                var bright=CreateMaterial(catalog,"Luminous inset chevrons",skin.stripe,.45f,.65f,true);
                bright.SetColor("_EmissionColor",skin.stripe*1.2f);
                var dark=CreateMaterial(catalog,"Chevron rubber bevels",new Color(.018f,.025f,.033f),.15f,.48f);
                EnsureSharedShell();
                Child("Machined colored shoulders",sharedShoulders,edge);
                Child("Sculpted carbon chevron borders",sharedBevels,dark);
                Child("Radiant chevrons and fine rims",sharedInsets,bright);
            }
            else if(skin.artStyle==BallArtStyle.BrushedSteel||skin.artStyle==BallArtStyle.Copper)
            {
                var seams=new List<Mesh>();for(int band=-1;band<=1;band++)seams.Add(Ring(band*.27f,Mathf.Sqrt(.25f-band*band*.27f*.27f)+.001f,.006f));
                var mesh=Merge(seams,"Recessed machined material seams");meshes.Add(mesh);
                Child("Machined metal seams",mesh,CreateMaterial(catalog,"Oxidized alloy seams",skin.stripe,.78f,.55f));
            }
        }

        static void EnsureSharedShell()
        {
            if(sharedShoulders&&sharedBevels&&sharedInsets)return;
            var shoulders=new List<Mesh>();var bevels=new List<Mesh>();var insets=new List<Mesh>();
            for(int side=-1;side<=1;side+=2){shoulders.Add(Ring(side*.31f,.397f,.018f));insets.Add(Ring(side*.348f,.365f,.011f));}
            for(int face=0;face<4;face++)for(int mark=-1;mark<=1;mark++)
            {
                bevels.Add(Chevron(face*Mathf.PI*.5f,mark*.48f,.54f,.17f,.511f));
                insets.Add(Chevron(face*Mathf.PI*.5f,mark*.48f,.43f,.105f,.515f));
            }
            sharedShoulders=Merge(shoulders,"Shared machined shoulder geometry");
            sharedBevels=Merge(bevels,"Shared carbon chevron geometry");
            sharedInsets=Merge(insets,"Shared luminous inset geometry");
        }
        static Mesh Merge(List<Mesh> source,string name)
        {
            var instances=new CombineInstance[source.Count];
            for(int i=0;i<source.Count;i++)instances[i]=new CombineInstance{mesh=source[i],transform=Matrix4x4.identity};
            var result=new Mesh{name=name};result.CombineMeshes(instances,true,false);
            foreach(var mesh in source)Destroy(mesh);
            return result;
        }

        Material CreateMaterial(GameCatalog catalog,string name,Color color,float metallic,float smoothness,bool emission=false)
        {
            var template=catalog?(emission?catalog.glowMaterial:catalog.palette[4]):null;
            var material=template?new Material(template):QuestVisuals.Material(name,color,metallic,smoothness);
            material.name=name;material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smoothness);material.SetFloat("_Cull",0);
            if(emission)material.EnableKeyword("_EMISSION");else {material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);}
            materials.Add(material);return material;
        }

        void Child(string name,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(shell,false);go.layer=gameObject.layer;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        static Vector3 Point(float longitude,float latitude,float radius)=>new Vector3(Mathf.Sin(longitude)*Mathf.Cos(latitude),Mathf.Sin(latitude),Mathf.Cos(longitude)*Mathf.Cos(latitude))*radius;
        static Mesh Chevron(float longitude,float center,float halfWidth,float thickness,float radius)
        {
            const int steps=20;var vertices=new Vector3[(steps+1)*2];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];var indices=new int[steps*6];
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps*2-1;float angle=longitude+t*halfWidth;float latitude=center+.14f-Mathf.Abs(t)*.29f;
                vertices[i*2]=Point(angle,latitude-thickness*.5f,radius);vertices[i*2+1]=Point(angle,latitude+thickness*.5f,radius);
                normals[i*2]=vertices[i*2].normalized;normals[i*2+1]=vertices[i*2+1].normalized;uv[i*2]=new Vector2(i/(float)steps,0);uv[i*2+1]=new Vector2(i/(float)steps,1);
                if(i<steps){int n=i*6,a=i*2;indices[n]=a;indices[n+1]=a+2;indices[n+2]=a+1;indices[n+3]=a+1;indices[n+4]=a+2;indices[n+5]=a+3;}
            }
            var mesh=new Mesh{name="Curved inset chevron"};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateBounds();return mesh;
        }

        static Mesh Ring(float x,float radius,float thickness)
        {
            const int segments=64,sides=8;var vertices=new Vector3[(segments+1)*(sides+1)];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];var indices=new int[segments*sides*6];int index=0;
            for(int i=0;i<=segments;i++)for(int j=0;j<=sides;j++)
            {
                float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;
                var normal=new Vector3(Mathf.Sin(b),Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a)*Mathf.Cos(b));
                int n=i*(sides+1)+j;vertices[n]=new Vector3(x,Mathf.Cos(a)*radius,Mathf.Sin(a)*radius)+normal*thickness;normals[n]=normal;uv[n]=new Vector2(i/(float)segments,j/(float)sides);
                if(i<segments&&j<sides){int p=n,q=n+sides+1;indices[index++]=p;indices[index++]=q;indices[index++]=p+1;indices[index++]=p+1;indices[index++]=q;indices[index++]=q+1;}
            }
            var mesh=new Mesh{name="Anodized machined sphere rim"};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateBounds();return mesh;
        }

        static Mesh SphereMesh()
        {
            const int rows=40,columns=64;var vertices=new Vector3[(rows+1)*(columns+1)];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];var tangents=new Vector4[vertices.Length];var triangles=new int[rows*columns*6];int index=0;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float theta=y*Mathf.PI/rows,phi=x*Mathf.PI*2/columns;var normal=new Vector3(Mathf.Sin(theta)*Mathf.Sin(phi),Mathf.Cos(theta),Mathf.Sin(theta)*Mathf.Cos(phi));int n=y*(columns+1)+x;
                vertices[n]=normal*.5f;normals[n]=normal;uv[n]=new Vector2(x/(float)columns,1-y/(float)rows);tangents[n]=new Vector4(Mathf.Cos(phi),0,-Mathf.Sin(phi),1);
                if(y<rows&&x<columns){int q=n+columns+1;triangles[index++]=n;triangles[index++]=q;triangles[index++]=q+1;triangles[index++]=n;triangles[index++]=q+1;triangles[index++]=n+1;}
            }
            var mesh=new Mesh{name="Premium smooth ball sphere"};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.tangents=tangents;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
        }

        static Cubemap StudioReflection()
        {
            const int size=64;var cube=new Cubemap(size,TextureFormat.RGBAHalf,true){name="Original sky and softbox reflection",filterMode=FilterMode.Trilinear};
            for(int face=0;face<6;face++)
            {
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;
                    Vector3 direction=face==0?new Vector3(1,v,-u):face==1?new Vector3(-1,v,u):face==2?new Vector3(u,1,-v):face==3?new Vector3(u,-1,v):face==4?new Vector3(u,v,1):new Vector3(-u,v,-1);
                    direction.Normalize();
                    Color color=Color.Lerp(new Color(.07f,.12f,.21f),new Color(.48f,.7f,.93f),direction.y*.5f+.5f);
                    float key=Mathf.Pow(Mathf.Max(0,Vector3.Dot(direction,new Vector3(-.5f,.65f,-.5f).normalized)),28);
                    float rim=Mathf.Pow(Mathf.Max(0,Vector3.Dot(direction,new Vector3(.8f,.28f,.3f).normalized)),42);
                    color+=new Color(1.4f,1.5f,1.6f)*key+new Color(1,.87f,.62f)*rim;pixels[y*size+x]=color;
                }
                cube.SetPixels(pixels,(CubemapFace)face);
            }
            cube.Apply(true,false);return cube;
        }

        void Clear()
        {
            if(shell){shell.gameObject.SetActive(false);Destroy(shell.gameObject);shell=null;}
            foreach(var material in materials)if(material)Destroy(material);materials.Clear();
            foreach(var mesh in meshes)if(mesh)Destroy(mesh);meshes.Clear();texture=null;
        }
        void OnDestroy()=>Clear();
    }
}
