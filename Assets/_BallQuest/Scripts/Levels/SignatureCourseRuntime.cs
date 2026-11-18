using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BallQuest
{
    public static class SignatureCourseRuntime
    {
        sealed class Context
        {
            public readonly Transform root;
            public readonly SignatureRuntimeResources resources;
            public readonly Material gold,steel,cyan,white,stone,dark,lava;
            public Context(Transform parent)
            {
                root=new GameObject("Signature expedition machinery and landmarks").transform;root.SetParent(parent);resources=root.gameObject.AddComponent<SignatureRuntimeResources>();
                gold=Mat("Brushed clockwork brass",new Color(.95f,.57f,.10f),.7f);steel=Mat("Expedition midnight alloy",new Color(.035f,.10f,.17f),.6f);cyan=Mat("Gravity field cyan",new Color(.04f,.86f,1),.4f,true);white=Mat("Mountain snow",new Color(.89f,.96f,1));stone=Mat("Mountain granite",new Color(.24f,.32f,.40f));dark=Mat("Volcanic obsidian",new Color(.07f,.045f,.07f));lava=Mat("Living lava",new Color(1,.15f,.015f),0,true);
            }
            Material Mat(string name,Color color,float metal=0,bool glow=false)
            {
                var material=QuestVisuals.Material(name,color,metal,.45f);if(glow){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*1.8f);}resources.materials.Add(material);return material;
            }
            public GameObject Box(string name,Vector3 position,Vector3 size,Material material,bool collision=true,Transform parent=null,Quaternion? rotation=null)
            {
                var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(parent?parent:root);box.transform.position=position;box.transform.rotation=rotation??Quaternion.identity;box.transform.localScale=size;box.GetComponent<Renderer>().sharedMaterial=material;
                if(!collision){var collider=box.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);box.layer=2;}else{var tag=box.AddComponent<BallSurfaceTag>();tag.surface=material==cyan?SurfaceAudioKind.Glass:material==stone?SurfaceAudioKind.Stone:SurfaceAudioKind.Metal;tag.rollingResistanceMultiplier=material==cyan?.65f:1;}
                return box;
            }
            public void Label(string text,Vector3 position,float size=1.3f,Color? color=null)
            {
                var label=new GameObject(text).AddComponent<TextMeshPro>();label.transform.SetParent(root);label.transform.position=position;label.font=GameBootstrap.Instance?GameBootstrap.Instance.catalog.font:TMP_Settings.defaultFontAsset;label.fontSize=size;label.color=color??Color.white;label.text=text;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(25,7);
                label.gameObject.AddComponent<CourseSignVisibility>();
            }
            public GameObject Mesh(string name,Mesh mesh,Material material,Vector3 position,bool collision=false,bool convex=false,Transform parent=null)
            {
                resources.meshes.Add(mesh);var go=ArcadeGeometry.MeshObject(name,parent?parent:root,mesh,material);go.transform.position=position;if(collision){go.layer=0;var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=convex;go.AddComponent<BallSurfaceTag>().surface=SurfaceAudioKind.Metal;}return go;
            }
        }
        public static void Decorate(Transform root,LevelDefinition level,GameSession session)
        {
            if(!level.expedition)return;var c=new Context(root);
            switch(level.signatureMechanic)
            {
                case "SkyMaze":Maze(c);break;
                case "ClockworkChaos":Clockwork(c);break;
                case "GravityLab":Gravity(c,root,level);break;
                case "HimalayanPass":Mountain(c);break;
                case "VolcanoIslands":Volcano(c,root);break;
            }
            CourseRenderBatcher.Combine(c.root,c.resources.meshes);
        }
        static void Maze(Context c)
        {
            // Both exits are straight +Z roads. The optional east exit must not inherit
            // a diagonal frame from the primary west-only progress description.
            foreach(Transform child in c.root.parent)if(child.name=="Finish arch")child.rotation=Quaternion.identity;
            c.Label("SKY MAZE",new Vector3(0,29,18),2.4f);c.Label("RISK + TREASURE",new Vector3(-20,27,29),1.1f,new Color(1,.75f,.08f));c.Label("HAMMER LANE",new Vector3(0,27,29),1.1f);c.Label("SAFE SCENIC ROUTE",new Vector3(23,27,29),1.1f,new Color(.4f,1,.65f));
            c.Label("DEAD END\nTURN BACK",new Vector3(42,27,55),1.1f);c.Label("LOOK BEHIND YOU",new Vector3(-32,26.8f,46),1);c.Label("TWO EXITS\nYOUR CHOICE",new Vector3(0,28,88),1.5f);
            foreach(float x in new[]{-14f,14f})c.Mesh("Exit halo",ArcadeGeometry.Torus(5.6f,.12f,48,6),c.cyan,new Vector3(x,28,114));
            for(int i=0;i<7;i++)c.Box("Floating maze island edge",new Vector3(-39+i*13,12,54),new Vector3(7,6,7),c.steel,false);
        }
        static void Clockwork(Context c)
        {
            var bridgeRoot=new GameObject("Timed physical drawbridge hinge");bridgeRoot.transform.SetParent(c.root);bridgeRoot.transform.position=new Vector3(0,23.6f,14);
            c.Box("Hinged bridge deck",new Vector3(0,23.6f,19),new Vector3(8,.8f,10),c.gold,true,bridgeRoot.transform);
            bridgeRoot.AddComponent<Rigidbody>();var bridge=bridgeRoot.AddComponent<ClockworkMotor>();bridge.kind=ClockworkMotionKind.Drawbridge;
            for(int side=-1;side<=1;side+=2)c.Box("Drawbridge safety pier",new Vector3(side*5,23,14),new Vector3(1,5,2),c.steel);
            var disc=c.Mesh("Physical rotating brass gear disc",ArcadeGeometry.PlatformCylinder(14,14,.8f,48),c.gold,new Vector3(0,23.6f,34),true,true);disc.AddComponent<Rigidbody>();var rotation=disc.AddComponent<ClockworkMotor>();rotation.kind=ClockworkMotionKind.Disc;rotation.degreesPerSecond=18;
            for(int i=0;i<20;i++){float a=i*Mathf.PI*2/20;c.Box("Brass gear tooth",new Vector3(Mathf.Cos(a)*7,23.6f,34+Mathf.Sin(a)*7),new Vector3(.9f,.8f,.9f),c.steel,false,disc.transform,Quaternion.Euler(0,-i*18,0));}
            var wheel=new GameObject("Giant pass-through clockwork wheel");wheel.transform.SetParent(c.root);wheel.transform.position=new Vector3(0,28.4f,66);
            c.Mesh("Spoked wheel golden rim",ArcadeGeometry.Torus(5.2f,.28f,64,8),c.gold,wheel.transform.position,false,false,wheel.transform);
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24;var position=wheel.transform.position+new Vector3(Mathf.Cos(a)*5.2f,Mathf.Sin(a)*5.2f,0);c.Box("Actual rotating rim collision",position,new Vector3(.4f,1.42f,.5f),c.gold,true,wheel.transform,Quaternion.Euler(0,0,i*15));
            }
            c.Box("Physical wheel spoke A",wheel.transform.position,new Vector3(.5f,10.4f,.6f),c.gold,true,wheel.transform);c.Box("Physical wheel spoke B",wheel.transform.position,new Vector3(10.4f,.5f,.6f),c.gold,true,wheel.transform);
            wheel.AddComponent<Rigidbody>();var rotor=wheel.AddComponent<ClockworkMotor>();rotor.kind=ClockworkMotionKind.Wheel;rotor.axis=Vector3.forward;rotor.degreesPerSecond=18;
            c.Label("CLOCKWORK CHAOS",new Vector3(0,32,7),2.4f);c.Label("WAIT FOR THE BRIDGE",new Vector3(0,28,11),1.3f);c.Label("RIDE THE GEAR",new Vector3(0,29,32),1.5f);c.Label("WATCH THE OPENING\nOR TAKE THE OUTSIDE",new Vector3(0,32,57),1.3f);
        }
        static void Gravity(Context c,Transform levelRoot,LevelDefinition level)
        {
            const float radius=12;var center=new Vector3(0,30,24);
            // Underside roads need their own modest self-lighting; world-space sunlight
            // cannot illuminate the ceiling face sufficiently in the night environment.
            var roadway=new Material(c.steel){name="Readable gravity laboratory roadway"};
            roadway.SetColor("_BaseColor",new Color(.16f,.34f,.46f));roadway.SetFloat("_Metallic",.15f);
            roadway.EnableKeyword("_EMISSION");roadway.SetColor("_EmissionColor",new Color(.08f,.18f,.25f));c.resources.materials.Add(roadway);
            for(int i=0;i<36;i++)
            {
                float a=(-90+i*5)*Mathf.Deg2Rad,b=(-90+(i+1)*5)*Mathf.Deg2Rad;
                Vector3 p=center+new Vector3(0,Mathf.Sin(a)*radius,Mathf.Cos(a)*radius),q=center+new Vector3(0,Mathf.Sin(b)*radius,Mathf.Cos(b)*radius);
                Vector3 normal=(center-(p+q)*.5f).normalized;var orientation=Quaternion.LookRotation(q-p,normal);
                // Keep the forgiving overlapped physical supports, but draw one continuous
                // loop instead of dozens of overlapping box caps at every curve seam.
                c.Box("Curved gravity road "+i,(p+q)*.5f-normal*.4f,new Vector3(8,.8f,Vector3.Distance(p,q)+.09f),roadway,true,rotation:orientation).GetComponent<Renderer>().enabled=false;
                for(int side=-1;side<=1;side+=2)c.Box("Visible gravity-field edge",(p+q)*.5f+Vector3.right*side*3.7f+normal*.12f,new Vector3(.12f,.2f,Vector3.Distance(p,q)+.12f),c.cyan,false,rotation:orientation);
                if(i%4==0)c.Box("Gravity curve depth inlay",(p+q)*.5f+normal*.018f,new Vector3(4.8f,.026f,.07f),c.cyan,false,rotation:orientation);
            }
            c.Mesh("Continuous visible gravity loop",ArcadeGeometry.HalfLoopRoad(center,radius,8,.8f),roadway,Vector3.zero);
            c.Box("Inverted ceiling return road",new Vector3(0,42.4f,8),new Vector3(8,.8f,34),roadway).GetComponent<Renderer>().enabled=false;
            c.Box("Visible ceiling return road",new Vector3(0,42.4f,7.5f),new Vector3(8,.8f,33),roadway,false);
            for(int side=-1;side<=1;side+=2)c.Box("Ceiling field boundary",new Vector3(side*3.75f,41.9f,8),new Vector3(.12f,.12f,34),c.cyan,false);
            for(float z=-8;z<25;z+=4)c.Box("Ceiling depth inlay",new Vector3(0,41.982f,z),new Vector3(4.8f,.026f,.07f),c.cyan,false);
            c.Label("GRAVITY LAB",new Vector3(0,23,11),2.4f,new Color(.2f,1,1));c.Label("CYAN FIELD\nFLOOR > WALL > CEILING",new Vector3(0,23,20),1.1f);
            c.root.gameObject.AddComponent<SignatureGravityField>();
            foreach(Transform child in levelRoot)if(child.name=="Finish arch"&&Vector3.Distance(child.position,level.finish)<3)child.rotation=Quaternion.LookRotation(Vector3.back,Vector3.down);
            for(int side=-1;side<=1;side+=2)
            {
                c.Box("Laboratory support arch",new Vector3(side*7,30,24),new Vector3(1,29,1),c.cyan,false);
                c.Box("Laboratory upper gantry",new Vector3(0,45,24),new Vector3(15,.6f,.6f),c.cyan,false);
            }
        }
        static void Mountain(Context c)
        {
            c.Label("HIMALAYAN PASS",new Vector3(0,35,10),2.1f);c.Label("ICE AHEAD\nBRAKE EARLY",new Vector3(12,35,46),1.2f);
            for(int i=0;i<10;i++)
            {
                int side=i%2==0?-1:1;var p=new Vector3(side*(35+(i%3)*5),6+(i%4),i*13);c.Mesh("Granite mountain",ArcadeGeometry.Cone(15,48+(i%3)*9,9),c.stone,p);c.Mesh("Snow-capped summit",ArcadeGeometry.Cone(8,22,9),c.white,p+Vector3.up*20);
            }
            c.Box("Mountain tunnel left wall",new Vector3(-18.3f,34,81),new Vector3(1.1f,4,14),c.stone);c.Box("Mountain tunnel right wall",new Vector3(-9.7f,34,81),new Vector3(1.1f,4,14),c.stone);c.Box("Mountain tunnel ceiling",new Vector3(-14,36.2f,81),new Vector3(9.7f,.8f,14),c.stone);
            for(int side=-1;side<=1;side+=2)
            {
                c.Box("Suspension bridge rope",new Vector3(12+side*1.65f,32.9f,38.5f),new Vector3(.14f,.15f,15),c.gold);
                foreach(float z in new[]{31f,46f})c.Box("Suspension bridge mast",new Vector3(12+side*1.8f,33.3f,z),new Vector3(.25f,3.3f,.3f),c.stone);
            }
        }
        static void Volcano(Context c,Transform root)
        {
            c.Label("VOLCANO ISLANDS",new Vector3(0,30,10),2.1f,new Color(1,.75f,.25f));c.Label("BUILD SPEED FOR THE GAP",new Vector3(0,29,20),1.2f);c.Label("MOVING STONE DETOUR",new Vector3(18,28,28),1.2f);c.Label("FRAGILE BRIDGE\nKEEP MOVING",new Vector3(0,28,43),1.2f);
            c.Box("Lava sea below the authored failure height",new Vector3(0,8,55),new Vector3(350,.25f,280),c.lava,false);
            for(int i=0;i<8;i++)
            {
                int side=i%2==0?-1:1;var p=new Vector3(side*(32+(i%3)*4),4,i*18+6);c.Mesh("Obsidian volcano",ArcadeGeometry.Cone(14,31+(i%3)*7,9),c.dark,p);c.Mesh("Glowing volcano throat",ArcadeGeometry.Torus(2.5f,.6f,28,8),c.lava,p+Vector3.up*17).transform.rotation=Quaternion.Euler(90,0,0);
            }
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.name=="Ocean horizon")renderer.enabled=false;
        }
    }
}
