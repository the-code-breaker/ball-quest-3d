using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BallQuest
{
    /// <summary>One animation loop and one mesh/material library per course, never per coin.</summary>
    public sealed class PremiumCollectibleVisuals : MonoBehaviour
    {
        struct Entry
        {
            public Transform pickup,visual;
            public Renderer front,back;
            public float phase;
            public PickupKind kind;
        }
        readonly List<Entry> entries=new List<Entry>();
        readonly List<PremiumBoosterVisuals> pads=new List<PremiumBoosterVisuals>();
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        Mesh coinBody,coinEmblem,jewelBase,jewelCrown,padBody,padTrim,padArrows;
        Material gold,coinHighlight,amethyst,amethystHighlight,cyan,cyanHighlight,housing,trim,flow,launchFlow;
        bool initialized;
        Camera view;
        float farTick;
        public int RegisteredPickupCount=>entries.Count;
        public int RegisteredBoosterCount=>pads.Count;
        public int SharedMeshCount=>meshes.Count;
        public int SharedMaterialCount=>materials.Count;

        internal static PremiumCollectibleVisuals Library(Transform item,GameCatalog catalog)
        {
            var course=item.GetComponentInParent<LevelBuilder>();
            Transform owner=course?course.transform:item.parent?item.parent:item;
            var library=owner.GetComponent<PremiumCollectibleVisuals>();
            if(!library)library=owner.gameObject.AddComponent<PremiumCollectibleVisuals>();
            library.Initialize(catalog);return library;
        }
        void Initialize(GameCatalog catalog)
        {
            if(initialized)return;initialized=true;
            coinBody=Own(PremiumPickupGeometry.CoinBody());coinEmblem=Own(PremiumPickupGeometry.CoinEmblem());
            jewelBase=Own(PremiumPickupGeometry.Jewel(false));jewelCrown=Own(PremiumPickupGeometry.Jewel(true));
            padBody=Own(PremiumPickupGeometry.PadHousing());padTrim=Own(PremiumPickupGeometry.PadTrim());padArrows=Own(PremiumPickupGeometry.PadArrows());
            gold=Metal("Minted honey-gold coin body",new Color(1,.65f,.035f),.27f,.72f,catalog,new Color(.15f,.065f,.002f));
            coinHighlight=Metal("Raised champagne star and faceted rim",new Color(1,.93f,.55f),.34f,.84f,catalog,new Color(.14f,.10f,.023f));
            amethyst=Metal("Amethyst gem pavilion",new Color(.37f,.045f,.72f),.35f,.85f,catalog,new Color(.13f,.01f,.2f));
            amethystHighlight=Metal("Amethyst gem polished crown",new Color(.80f,.24f,1),.2f,.9f,catalog,new Color(.15f,.025f,.24f));
            cyan=Metal("Sky crystal pavilion",new Color(.015f,.34f,.64f),.42f,.88f,catalog,new Color(.005f,.08f,.12f));
            cyanHighlight=Metal("Sky crystal polished crown",new Color(.28f,.94f,1),.24f,.9f,catalog,new Color(.035f,.2f,.24f));
            housing=Metal("Accelerator graphite ribbed runway",new Color(.024f,.045f,.07f),.42f,.55f,catalog,Color.black);
            trim=Metal("Accelerator champagne-alloy direction plates",new Color(1,.78f,.20f),.36f,.74f,catalog,new Color(.045f,.018f,.001f));
            var shader=Shader.Find("BallQuest/Boost Flow");
            flow=Own(new Material(shader?shader:Shader.Find("Universal Render Pipeline/Unlit")){name="Cyan traveling accelerator light",enableInstancing=true});
            flow.SetColor("_BaseColor",new Color(.025f,.96f,1.55f));
            launchFlow=Own(new Material(flow){name="Amber traveling launcher light"});launchFlow.SetColor("_BaseColor",new Color(1.6f,.5f,.035f));
        }
        Mesh Own(Mesh mesh){meshes.Add(mesh);return mesh;}
        Material Own(Material material){materials.Add(material);return material;}
        Material Metal(string name,Color color,float metallic,float smoothness,GameCatalog catalog,Color glow)
        {
            Material template=catalog&&catalog.palette!=null&&catalog.palette.Length>0?catalog.palette[0]:null;
            var material=template?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name=name;material.enableInstancing=true;material.SetTexture("_BaseMap",null);
            material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smoothness);
            if(glow.maxColorComponent>0){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",glow);}
            else{material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);}
            return Own(material);
        }
        internal static Renderer Part(Transform parent,string name,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.layer=2;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Simple;
            return renderer;
        }
        public static Transform Attach(Transform pickupRoot,PickupKind kind,GameCatalog catalog)
        {
            var library=Library(pickupRoot,catalog);var visual=new GameObject("Polished "+kind+" visual").transform;visual.SetParent(pickupRoot,false);
            Renderer first,second;
            if(kind==PickupKind.Coin)
            {
                first=Part(visual,"Beveled gold coin body",library.coinBody,library.gold);
                second=Part(visual,"Raised star on both coin faces",library.coinEmblem,library.coinHighlight);
            }
            else
            {
                first=Part(visual,"Cut jewel pavilion",library.jewelBase,kind==PickupKind.Gem?library.amethyst:library.cyan);
                second=Part(visual,"Polished jewel crown",library.jewelCrown,kind==PickupKind.Gem?library.amethystHighlight:library.cyanHighlight);
            }
            library.entries.Add(new Entry{pickup=pickupRoot,visual=visual,front=first,back=second,kind=kind,phase=pickupRoot.position.z*.31f+pickupRoot.position.x*.17f});
            return visual;
        }
        internal void CreatePad(PremiumBoosterVisuals pad,Vector3 size,ObstacleKind kind)
        {
            var visual=new GameObject("Segmented drive accelerator assembly").transform;visual.SetParent(pad.transform,false);
            // Authored triggers sit about 0.1m above their roadway. Recess the assembly
            // so its drive plates meet the deck, while the side fins retain real depth.
            visual.localPosition=Vector3.down*.11f;
            visual.localScale=new Vector3(Mathf.Max(1.15f,size.x),1,Mathf.Max(.7f,size.z));
            var chassis=Part(visual,"Open dark-metal cassette and sculpted fins",padBody,housing);
            chassis.shadowCastingMode=ShadowCastingMode.On;chassis.receiveShadows=true;
            var plates=Part(visual,"Raised pearl-alloy drive plates",padTrim,trim);plates.receiveShadows=true;plates.shadowCastingMode=ShadowCastingMode.On;
            var luminous=Part(visual,"Recessed traveling direction lights",padArrows,kind==ObstacleKind.JumpPad?launchFlow:flow);
            pad.Initialize(luminous,visual,kind);pads.Add(pad);
        }
        void LateUpdate()
        {
            if(Time.timeScale<=0)return;
            if(!view)view=Camera.main;
            if(!view)return;
            Animate(Time.time,Time.deltaTime,view.transform.position);
        }
        /// <summary>Shared animation pass; explicit inputs permit isolated allocation measurement.</summary>
        public void Animate(float time,float deltaTime,Vector3 cameraPosition)
        {
            bool farUpdate=time>=farTick;if(farUpdate)farTick=time+.1f;
            for(int i=0;i<entries.Count;i++)
            {
                var entry=entries[i];if(!entry.pickup||!entry.pickup.gameObject.activeInHierarchy)continue;
                float distance=(entry.pickup.position-cameraPosition).sqrMagnitude;
                // Rewards behind the ball can pass very close to the chase lens.
                // Ease only the cosmetic model away instead of filling the HUD with
                // a huge coin; the stationary collection trigger remains active.
                float lensScale=distance>=81?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(6.25f,9f,Mathf.Sqrt(distance)));
                bool visible=distance<180*180&&lensScale>.001f;
                if(entry.front.enabled!=visible){entry.front.enabled=visible;entry.back.enabled=visible;}
                if(visible)entry.visual.localScale=Vector3.one*lensScale;
                if(!visible||distance>45*45&&!farUpdate)continue;
                entry.visual.localPosition=Vector3.up*(Mathf.Sin(time*2.3f+entry.phase)*.075f);
                if(entry.kind==PickupKind.Coin)
                {
                    Vector3 toward=Vector3.ProjectOnPlane(cameraPosition-entry.pickup.position,Vector3.up);
                    if(toward.sqrMagnitude>.01f)
                    {
                        Quaternion facing=Quaternion.LookRotation(toward.normalized,Vector3.up);
                        // A gentle presentation turn catches bevel highlights without ever hiding
                        // the icon edge-on. Trigger transforms never move with this animation.
                        entry.visual.rotation=facing*Quaternion.Euler(Mathf.Sin(time*1.4f+entry.phase)*8,Mathf.Sin(time*1.7f+entry.phase)*27,Mathf.Sin(time*.9f+entry.phase)*5);
                    }
                }
                else entry.visual.localRotation=Quaternion.Euler(0,time*42+entry.phase*30,Mathf.Sin(time*1.3f+entry.phase)*8);
            }
            for(int i=0;i<pads.Count;i++)if(pads[i])pads[i].Tick(deltaTime);
        }
        void OnDestroy()
        {
            foreach(var value in meshes)if(value)Destroy(value);
            foreach(var value in materials)if(value)Destroy(value);
            entries.Clear();pads.Clear();
        }
    }
}
