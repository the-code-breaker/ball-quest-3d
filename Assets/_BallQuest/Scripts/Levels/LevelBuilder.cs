using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

namespace BallQuest
{
    public sealed class LevelBuilder : MonoBehaviour
    {
        public BallController Ball { get; private set; }
        public QuestCamera FollowCamera { get; private set; }
        GameCatalog catalog;
        GameSession session;
        LevelDefinition definition;
        LevelDefinition runtimeDefinition;
        public LevelDefinition RuntimeLevel=>definition;
        readonly PhysicsMaterial[] contactSurfaces=new PhysicsMaterial[6];
        readonly List<PhysicsMaterial> ownedPhysics=new List<PhysicsMaterial>();
        Transform architecture;
        readonly List<Material> ownedMaterials=new List<Material>();
        readonly List<Texture2D> ownedTextures=new List<Texture2D>();
        readonly List<Mesh> ownedMeshes=new List<Mesh>();
        Material wood,walnut,steel,gold,black,red,white,blue,water,glass,glassDark,gem,neon,glassPearl,glassSlate;
        Material runtimeSky;
        Material turf,foliage,foliageLight,cliff,cliffDark,edgePearl;
        Material ceramicDeck,alloyDeck,basaltDeck;
        PremiumTrackPresentation premium;
        int world;
        int railSequence;

        Material Surface(string name,Color color,float metallic=0,float smooth=.4f,bool emission=false)
        {
            var value=new Material(emission?catalog.glowMaterial:catalog.palette[0]){name=name,enableInstancing=true};
            value.SetColor("_BaseColor",color);value.SetFloat("_Metallic",metallic);value.SetFloat("_Smoothness",smooth);
            if(emission){value.EnableKeyword("_EMISSION");value.SetColor("_EmissionColor",color*.6f);}
            ownedMaterials.Add(value);return value;
        }
        void CreateMaterials()
        {
            wood=Surface("Satin maple running deck",new Color(1,.99f,.97f),0,.27f);
            walnut=Surface("Deep wood edge",new Color(.34f,.16f,.055f),0,.3f);
            steel=Surface("Anodized midnight bridge frame",new Color(.035f,.12f,.20f),.55f,.55f);
            gold=Surface("Golden rail and coin",new Color(1,.65f,.035f),.48f,.66f);
            black=Surface("Charcoal rail",new Color(.045f,.065f,.08f),.3f,.5f);
            red=Surface("Scarlet impact rubber",new Color(.93f,.035f,.055f),.12f,.65f);
            white=Surface("Cloud pearl",new Color(.92f,.975f,1),.03f,.34f);
            blue=Surface("City azure",new Color(.07f,.42f,.84f),.3f,.6f);
            water=Surface("Harbor water",world==3?new Color(.045f,.095f,.22f):world==1?new Color(.12f,.49f,.59f):new Color(.18f,.62f,.79f),.25f,.78f);
            glass=Surface("Reflecting windows",world==3?new Color(.15f,.1f,.4f):new Color(.43f,.66f,.84f),.25f,.8f);
            glassPearl=Surface("Pearl city windows",world==3?new Color(.27f,.13f,.35f):new Color(.78f,.85f,.91f),.22f,.7f);
            glassSlate=Surface("Slate city windows",world==3?new Color(.08f,.19f,.27f):new Color(.24f,.39f,.55f),.3f,.7f);
            glassDark=Surface("Skyline architecture",world==1?new Color(.68f,.35f,.14f):world==2?new Color(.46f,.7f,.9f):world==3?new Color(.035f,.045f,.13f):new Color(.25f,.43f,.59f),.1f,.45f);
            gem=Surface("Amethyst crystal",new Color(.62f,.13f,1),.4f,.9f,true);
            neon=Surface("Neon accent",world==3?new Color(.06f,.9f,1):new Color(.4f,.85f,1),.15f,.8f,true);
            edgePearl=Surface("Ceramic bridge edge",new Color(.82f,.93f,.96f),.16f,.54f);
            turf=Surface("Island surface",world==1?new Color(.91f,.68f,.32f):world==2?new Color(.86f,.96f,1):world==3?new Color(.07f,.27f,.32f):new Color(.3f,.64f,.27f),0,.28f);
            foliage=Surface("Sculpted tree canopy",world==2?new Color(.28f,.48f,.52f):new Color(.045f,.39f,.24f),0,.25f);
            foliageLight=Surface("Sunlit tree canopy",world==2?new Color(.88f,.97f,1):new Color(.25f,.66f,.29f),0,.3f);
            cliff=Surface("Floating island sandstone",world==1?new Color(.74f,.40f,.18f):world==2?new Color(.37f,.51f,.62f):new Color(.58f,.48f,.36f),0,.25f);
            cliffDark=Surface("Floating island rock strata",world==1?new Color(.48f,.24f,.13f):world==2?new Color(.23f,.34f,.46f):new Color(.32f,.35f,.35f),0,.2f);
            wood.SetTexture("_BaseMap",WoodTexture());var windowTexture=WindowTexture();glass.SetTexture("_BaseMap",windowTexture);glassPearl.SetTexture("_BaseMap",windowTexture);glassSlate.SetTexture("_BaseMap",windowTexture);
            var oceanMap=Resources.Load<Texture2D>("TrackArt/HarborWater");if(oceanMap)water.SetTexture("_BaseMap",oceanMap);
            if(definition.streamedCampaign||CrazyCoursePresentation.Applies(definition))
            {
                SetBiomePalette();
                ceramicDeck=Surface("Ivory ceramic route deck",new Color(.88f,.96f,1),.06f,.48f);ceramicDeck.SetTexture("_BaseMap",Resources.Load<Texture2D>("TrackArt/CeramicV9"));
                alloyDeck=Surface("Brushed alloy route deck",new Color(.55f,.69f,.76f),.48f,.54f);alloyDeck.SetTexture("_BaseMap",Resources.Load<Texture2D>("TrackArt/AlloyV9"));
                basaltDeck=Surface("Basalt stone route deck",new Color(.79f,.83f,.92f),.08f,.31f);basaltDeck.SetTexture("_BaseMap",Resources.Load<Texture2D>("TrackArt/BasaltV9"));
                if(definition.streamedCampaign)
                {
                    var p=GrandWorldPresentation.Get(definition.environment);cliff.SetColor("_BaseColor",p.rock);cliffDark.SetColor("_BaseColor",Color.Lerp(p.rock,Color.black,.55f));
                    foliage.SetColor("_BaseColor",p.leaf);foliageLight.SetColor("_BaseColor",Color.Lerp(p.leaf,Color.white,.30f));
                    neon.SetColor("_BaseColor",p.accent);neon.SetColor("_EmissionColor",p.accent*.55f);gold.SetColor("_BaseColor",p.accent);blue.SetColor("_BaseColor",p.accent);
                    edgePearl.SetColor("_BaseColor",Color.Lerp(p.deck,Color.white,.35f));steel.SetColor("_BaseColor",Color.Lerp(p.rock,new Color(.025f,.055f,.08f),.72f));
                    ceramicDeck.SetColor("_BaseColor",p.deck);alloyDeck.SetColor("_BaseColor",p.deck);basaltDeck.SetColor("_BaseColor",p.deck);
                }
            }
        }
        void SetBiomePalette()
        {
            string family=definition.signatureMechanic;
            Color rockTint=new Color(.66f,.48f,.3f),deep=new Color(.25f,.28f,.31f),leaf=new Color(.04f,.39f,.24f),accent=new Color(.16f,.8f,.92f);
            switch(family)
            {
                case "Sky Ribbon":rockTint=new Color(.84f,.69f,.46f);deep=new Color(.26f,.45f,.44f);break;
                case "Knife Edge":rockTint=new Color(.93f,.52f,.22f);deep=new Color(.56f,.23f,.13f);accent=new Color(1,.64f,.13f);break;
                case "Orbit Tower":rockTint=new Color(.23f,.26f,.47f);deep=new Color(.09f,.13f,.26f);accent=new Color(.52f,.31f,1);break;
                case "Switchback":rockTint=new Color(.52f,.63f,.70f);deep=new Color(.19f,.30f,.38f);leaf=new Color(.10f,.30f,.27f);break;
                case "Figure Eight":rockTint=new Color(.63f,.65f,.43f);deep=new Color(.28f,.40f,.29f);leaf=new Color(.19f,.50f,.28f);accent=new Color(1,.48f,.65f);break;
                case "Island Descent":rockTint=new Color(.31f,.27f,.29f);deep=new Color(.10f,.11f,.15f);accent=new Color(1,.24f,.035f);break;
                case "Clockwork":rockTint=new Color(.52f,.34f,.19f);deep=new Color(.16f,.21f,.24f);accent=new Color(1,.66f,.15f);break;
                case "Catwalk":rockTint=new Color(.14f,.25f,.41f);deep=new Color(.045f,.09f,.18f);accent=new Color(.95f,.22f,.65f);break;
                case "Sky Labyrinth":rockTint=new Color(.81f,.72f,.52f);deep=new Color(.43f,.48f,.31f);leaf=new Color(.09f,.38f,.20f);break;
                case "Velocity Canyon":rockTint=new Color(.83f,.37f,.18f);deep=new Color(.46f,.17f,.12f);accent=new Color(.08f,.86f,.83f);break;
                case "Finale":rockTint=new Color(.28f,.26f,.51f);deep=new Color(.10f,.13f,.28f);accent=new Color(.60f,.37f,1);break;
            }
            cliff.SetColor("_BaseColor",rockTint);cliffDark.SetColor("_BaseColor",deep);foliage.SetColor("_BaseColor",leaf);foliageLight.SetColor("_BaseColor",Color.Lerp(leaf,Color.white,.30f));
            neon.SetColor("_BaseColor",accent);neon.SetColor("_EmissionColor",accent*.7f);
        }
        Material RoadFinish(TrackPiece piece,out SurfaceAudioKind identity)
        {
            identity=SurfaceAudioKind.Wood;if(piece.style==TrackStyle.Ice){identity=SurfaceAudioKind.Ice;return white;}
            if(definition.streamedCampaign){int kind=GrandWorldPresentation.Get(definition.environment).deckKind;identity=kind==0?SurfaceAudioKind.Wood:kind==2?SurfaceAudioKind.Metal:SurfaceAudioKind.Stone;return kind==0?wood:kind==1?ceramicDeck:kind==2?alloyDeck:basaltDeck;}
            if(!CrazyCoursePresentation.Applies(definition))return wood;
            switch(definition.signatureMechanic)
            {
                case "Orbit Tower":case "Sky Labyrinth":case "Switchback":identity=SurfaceAudioKind.Stone;return ceramicDeck;
                case "Clockwork":case "Catwalk":case "Figure Eight":identity=SurfaceAudioKind.Metal;return alloyDeck;
                case "Island Descent":case "Velocity Canyon":case "Finale":identity=SurfaceAudioKind.Stone;return basaltDeck;
                default:return wood;
            }
        }
        Texture2D WoodTexture()
        {
            var baked=Resources.Load<Texture2D>("TrackArt/SatinMaple");if(baked)return baked;
            const int width=256,height=512;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,true){name="Maple grain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
            var colors=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int plank=x/32;
                float meander=Mathf.Sin(y*.0123f+plank*2.3f)*2+Mathf.Sin(y*.0368f+plank)*.6f;
                float grain=Mathf.Sin(x*.74f+meander)+Mathf.Sin(x*1.43f+meander*1.5f)*.24f;
                float board=((plank*17)%7-3)*.009f;
                float seam=x%32==0?.87f:x%32==1?1.04f:1;
                float value=grain*.013f+board;
                colors[y*width+x]=new Color((.81f+value)*seam,(.635f+value*.86f)*seam,(.405f+value*.65f)*seam);
            }
            texture.SetPixels(colors);texture.Apply();ownedTextures.Add(texture);return texture;
        }
        Texture2D WindowTexture()
        {
            var baked=Resources.Load<Texture2D>(world==3?"TrackArt/NightWindows":"TrackArt/DayWindows");if(baked)return baked;
            const int width=64,height=128;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,true){name="Skyline window grid",wrapMode=TextureWrapMode.Repeat};
            var colors=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                bool pane=x%8>1&&y%12>2;
                float tint=((x/8*17+y/12*31)%7)/7f;
                colors[y*width+x]=pane?world==3?Color.Lerp(new Color(.2f,.3f,.55f),new Color(1,.67f,.34f),tint):Color.Lerp(new Color(.4f,.66f,.9f),new Color(.87f,.96f,1),tint):new Color(.2f,.32f,.48f);
            }
            texture.SetPixels(colors);texture.Apply();ownedTextures.Add(texture);return texture;
        }
        public void Build(GameCatalog assets,LevelDefinition level,GameSession game,SaveData save)
        {
            if(level.streamedCampaign){runtimeDefinition=Instantiate(level);runtimeDefinition.tracks=CampaignGeometry.Expand(level);level=runtimeDefinition;}
            catalog=assets;session=game;definition=level;world=level.world;CreateMaterials();SetWorldLighting(world);
            architecture=new GameObject("Static authored skyway").transform;architecture.SetParent(transform);
            LoadContactSurfaces();
            var roadVisuals=RoadRenderGeometry.Build(level.tracks);
            ownedMeshes.AddRange(roadVisuals);
            for(int i=0;i<level.tracks.Length;i++)Track(level.tracks,i,roadVisuals[i]);
            Scenery(level);Landmarks(level);Combine(architecture);
            foreach(var obstacle in level.obstacles)Obstacle(obstacle);
            foreach(var pickup in level.pickups)Pickup(pickup);
            for(int i=0;i<level.checkpoints.Length;i++)Gate(level.checkpoints[i],false,i);
            Gate(level.finish,true,0);
            foreach(var exit in level.alternateFinishes)Gate(exit,true,0);
            DropMarkers(level);CreateBall(level,save);
            if(level.expedition)SignatureCourseRuntime.Decorate(transform,level,session);
            var camera=new GameObject("Cinemachine follow");camera.transform.SetParent(transform);
            var cm=camera.AddComponent<CinemachineCamera>();cm.Follow=Ball.transform;cm.LookAt=Ball.transform;cm.Lens.FieldOfView=54;
            FollowCamera=camera.AddComponent<QuestCamera>();FollowCamera.target=Ball;
            session.Respawned+=FollowCamera.Snap;Ball.Landed+=FollowCamera.Landing;
        }
        void LoadContactSurfaces()
        {
            float[] friction={.55f,.16f,.95f,.85f,.55f,.025f};
            for(int i=0;i<contactSurfaces.Length;i++)
            {
                var surface=(TrackSurface)i;
                var value=Resources.Load<PhysicsMaterial>("TrackPhysics/"+surface);
                if(!value)
                {
                    value=new PhysicsMaterial(surface+" contact"){dynamicFriction=friction[i],staticFriction=Mathf.Min(1,friction[i]+.05f),bounciness=surface==TrackSurface.Bouncy?.42f:0,frictionCombine=surface==TrackSurface.Ice?PhysicsMaterialCombine.Minimum:PhysicsMaterialCombine.Average,bounceCombine=PhysicsMaterialCombine.Maximum};
                    ownedPhysics.Add(value);
                }
                contactSurfaces[i]=value;
            }
        }
        public void ResetAtCheckpoint(int checkpointIndex)
        {
            foreach(var behaviour in GetComponentsInChildren<MonoBehaviour>(true))if(behaviour is ICourseResettable resettable)resettable.ResetCourseState();
            if(Ball&&Ball.TryGetComponent<CoursePathFollower>(out var follower))follower.ResetToNearest();
            Physics.SyncTransforms();
        }
        void SetWorldLighting(int theme)
        {
            Color[] sky={new Color(.16f,.62f,.95f),new Color(.99f,.65f,.36f),new Color(.42f,.76f,.98f),new Color(.035f,.065f,.2f)};
            var camera=Camera.main;if(camera)camera.backgroundColor=sky[theme];
            RenderSettings.fogColor=theme==3?new Color(.105f,.15f,.28f):Color.Lerp(sky[theme],new Color(.72f,.87f,.96f),.32f);RenderSettings.fogStartDistance=95;RenderSettings.fogEndDistance=245;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=theme==3?new Color(.36f,.43f,.64f):new Color(.51f,.65f,.82f);
            RenderSettings.ambientEquatorColor=theme==3?new Color(.25f,.31f,.45f):new Color(.30f,.40f,.5f);
            RenderSettings.ambientGroundColor=new Color(.16f,.20f,.25f);
            foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)
            {light.intensity=theme==3?1.05f:1.25f;light.color=theme==1?new Color(1,.88f,.7f):theme==3?new Color(.76f,.84f,1):new Color(1,.96f,.87f);}
            if(catalog.skyMaterial)
            {
                runtimeSky=new Material(catalog.skyMaterial);ownedMaterials.Add(runtimeSky);
                if(runtimeSky.HasProperty("_Top"))runtimeSky.SetColor("_Top",theme==3?new Color(.02f,.025f,.11f):sky[theme]);
                if(runtimeSky.HasProperty("_Horizon"))runtimeSky.SetColor("_Horizon",theme==3?new Color(.18f,.12f,.36f):theme==1?new Color(1,.88f,.61f):new Color(.63f,.86f,1));
                if(runtimeSky.HasProperty("_Bottom"))runtimeSky.SetColor("_Bottom",theme==3?new Color(.03f,.09f,.2f):new Color(.48f,.75f,.91f));
                RenderSettings.skybox=runtimeSky;
            }
            if(definition.streamedCampaign){GrandWorldPresentation.Light(definition,runtimeSky,water,camera);return;}
            if(CrazyCoursePresentation.Applies(definition)&&runtimeSky)
            {
                string f=definition.signatureMechanic;
                Color top=f=="Orbit Tower"||f=="Finale"?new Color(.08f,.09f,.27f):f=="Catwalk"?new Color(.045f,.10f,.23f):f=="Island Descent"?new Color(.34f,.18f,.27f):f=="Knife Edge"||f=="Velocity Canyon"?new Color(.69f,.53f,.37f):f=="Figure Eight"?new Color(.30f,.65f,.69f):new Color(.18f,.61f,.86f);
                Color horizon=Color.Lerp(top,new Color(.80f,.89f,.93f),.58f);
                runtimeSky.SetColor("_Top",top);runtimeSky.SetColor("_Horizon",horizon);runtimeSky.SetColor("_Bottom",Color.Lerp(top,horizon,.6f));RenderSettings.fogColor=horizon;
                if(camera)camera.backgroundColor=top;
                water.SetColor("_BaseColor",Color.Lerp(top,new Color(.1f,.43f,.53f),.5f));
                bool night=f=="Orbit Tower"||f=="Catwalk"||f=="Finale";
                RenderSettings.ambientSkyColor=night?new Color(.34f,.40f,.58f):new Color(.53f,.66f,.80f);
                RenderSettings.ambientEquatorColor=night?new Color(.24f,.29f,.42f):new Color(.34f,.43f,.51f);
                foreach(var sun in FindObjectsByType<Light>(FindObjectsSortMode.None))if(sun.type==LightType.Directional)
                {sun.intensity=night?1.15f:1.3f;sun.color=f=="Knife Edge"||f=="Velocity Canyon"?new Color(1,.88f,.72f):new Color(1,.96f,.9f);}
            }
        }
        static void Ends(TrackPiece piece,out Vector3 a,out Vector3 b,out Vector3 right)
        {
            var q=Quaternion.Euler(piece.rotation);var up=q*Vector3.up;var forward=q*Vector3.forward;
            a=piece.position+up*piece.size.y*.5f-forward*piece.size.z*.5f;
            b=piece.position+up*piece.size.y*.5f+forward*piece.size.z*.5f;right=q*Vector3.right;
        }
        void Track(TrackPiece[] pieces,int index,Mesh visualMesh)
        {
            if(premium==null)premium=new PremiumTrackPresentation(architecture,ownedMeshes);
            var piece=pieces[index];Ends(piece,out var a,out var b,out var right);
            Vector3 startRight=right,endRight=right;
            if(index>0)
            {
                Ends(pieces[index-1],out _,out var previous,out var previousRight);
                if(Vector3.Distance(previous,a)<.3f)startRight=(right+previousRight).normalized;
            }
            if(index+1<pieces.Length)
            {
                Ends(pieces[index+1],out var next,out _,out var nextRight);
                if(Vector3.Distance(next,b)<.3f)endRight=(right+nextRight).normalized;
            }
            startRight*=piece.size.x*.5f/Mathf.Max(.8f,Vector3.Dot(startRight,right));
            endRight*=piece.size.x*.5f/Mathf.Max(.8f,Vector3.Dot(endRight,right));
            var mesh=ArcadeGeometry.Road(a,b,startRight,endRight,.74f);ownedMeshes.Add(mesh);
            var finish=RoadFinish(piece,out var contactIdentity);
            var deck=ArcadeGeometry.MeshObject("Wooden racing surface",architecture,visualMesh,finish);deck.layer=0;
            var collider=deck.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
            var surface=piece.style==TrackStyle.Ice?TrackSurface.Ice:piece.surface;
            collider.sharedMaterial=contactSurfaces[(int)surface];
            var audioSurface=deck.AddComponent<BallSurfaceTag>();
            audioSurface.surface=surface==TrackSurface.Ice?SurfaceAudioKind.Ice:surface==TrackSurface.Moving?SurfaceAudioKind.Metal:surface==TrackSurface.Slippery?SurfaceAudioKind.Glass:surface==TrackSurface.HighGrip?SurfaceAudioKind.Stone:contactIdentity;
            // Follow exposed boundaries only: broad maze junctions must stay visibly open.
            for(int side=-1;side<=1;side+=2)
            {
                var edgeA=a+startRight*side;var edgeB=b+endRight*side;var outward=right*side;
                int strips=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(edgeA,edgeB)/2.5f));
                for(int n=0;n<strips;n++)
                {
                    var p=Vector3.Lerp(edgeA,edgeB,n/(float)strips);var q=Vector3.Lerp(edgeA,edgeB,(n+1f)/strips);
                    if(ExposedRoadEdge((p+q)*.5f+outward*.06f,pieces,index))premium.DeckEdge(p,q,outward,edgePearl,steel,gold);
                }
            }
            if(surface!=TrackSurface.Standard)
            {
                Material marking=surface==TrackSurface.Bouncy?gold:surface==TrackSurface.HighGrip?neon:blue;
                for(int side=-1;side<=1;side+=2)
                {
                    premium.Box((a+b)*.5f+right*(side*(piece.size.x*.5f-.36f))+Quaternion.Euler(piece.rotation)*Vector3.up*.045f,new Vector3(.12f,.025f,Mathf.Max(.1f,piece.size.z-.15f)),marking,Quaternion.Euler(piece.rotation));
                }
            }
            if(piece.rails)
            {
                Rail(a-startRight,b-endRight);Rail(a+startRight,b+endRight);
            }
            if(!definition.streamedCampaign&&index%5==0&&SupportClearOfRoads((a+b)*.5f,pieces,index))
            {
                var center=(a+b)*.5f;
                premium.Box(center+Vector3.down*.96f,new Vector3(piece.size.x+.1f,.5f,1.1f),steel,Quaternion.Euler(piece.rotation));
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 shoulder=center+right*side*(piece.size.x*.45f)+Vector3.down*1.2f;
                    Vector3 foot=center+right*side*(piece.size.x*.2f)+Vector3.down*4.2f;
                    premium.Tube(shoulder,foot,.18f,edgePearl,8);
                }
                premium.Tube(center+Vector3.down*3.8f,center+Vector3.down*12,.35f,steel,10);
                premium.Terrace(center+Vector3.down*12,1.4f,.55f,.75f,blue,index,12);
            }
            // Plank detail belongs in the mipmapped grain texture. Millimetre-thin seam
            // cubes shimmer and self-shadow against the road when viewed from a chase camera.
        }
        static bool ExposedRoadEdge(Vector3 point,TrackPiece[] pieces,int ownIndex)
        {
            for(int i=0;i<pieces.Length;i++)
            {
                if(i==ownIndex)continue;var other=pieces[i];
                Vector3 local=Quaternion.Inverse(Quaternion.Euler(other.rotation))*(point-other.position);
                if(Mathf.Abs(local.y-other.size.y*.5f)<.055f&&Mathf.Abs(local.x)<other.size.x*.5f+.015f&&Mathf.Abs(local.z)<other.size.z*.5f+.015f)return false;
            }
            return true;
        }
        static bool SupportClearOfRoads(Vector3 point,TrackPiece[] pieces,int ownIndex)
        {
            for(int i=0;i<pieces.Length;i++)
            {
                if(i==ownIndex)continue;var other=pieces[i];
                Ends(other,out var a,out var b,out _);var close=RaceRoute.Closest(point,a,b,out _);
                float below=point.y-close.y;
                if(below>1&&below<17&&Vector2.Distance(new Vector2(point.x,point.z),new Vector2(close.x,close.z))<other.size.x*.5f+1)return false;
            }
            return true;
        }
        void Rail(Vector3 a,Vector3 b)
        {
            a+=Vector3.up*.29f;b+=Vector3.up*.29f;
            float length=Vector3.Distance(a,b);int segments=Mathf.Max(1,Mathf.CeilToInt(length/1.35f));
            for(int i=0;i<segments;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,(float)i/segments),q=Vector3.Lerp(a,b,(float)(i+1)/segments);
                premium.Tube(p,q,.21f,railSequence++%2==0?gold:steel,12);
            }
            var guard=new GameObject("Rounded rail collision");guard.transform.SetParent(architecture);guard.transform.SetPositionAndRotation((a+b)*.5f,Quaternion.LookRotation(b-a));
            guard.AddComponent<BoxCollider>().size=new Vector3(.43f,.58f,length+.08f);
            guard.AddComponent<BallSurfaceTag>().surface=SurfaceAudioKind.Metal;
        }
        GameObject Shape(string name,PrimitiveType primitive,Transform parent,Vector3 position,Vector3 scale,Material material,bool collision=false)
        {
            var go=QuestVisuals.Shape(name,primitive,parent,position,scale,material,collision);
            if(!collision)go.GetComponent<Renderer>().receiveShadows=true;
            return go;
        }
        void Obstacle(ObstacleSpec spec)
        {
            var root=new GameObject(spec.kind+" machinery");root.transform.SetParent(transform);root.transform.SetPositionAndRotation(spec.position,Quaternion.Euler(spec.rotation));
            bool pad=spec.kind==ObstacleKind.Booster||spec.kind==ObstacleKind.JumpPad||spec.kind==ObstacleKind.Conveyor;
            var audioTag=root.AddComponent<BallSurfaceTag>();
            bool woodenPlatform=spec.kind==ObstacleKind.MovingBridge||spec.kind==ObstacleKind.Elevator||spec.kind==ObstacleKind.ForwardPlatform||spec.kind==ObstacleKind.CircularPlatform||spec.kind==ObstacleKind.FallingTile||spec.kind==ObstacleKind.CollapsingBridge;
            audioTag.surface=pad?SurfaceAudioKind.Boost:woodenPlatform||spec.kind==ObstacleKind.StaticBlock?SurfaceAudioKind.Wood:SurfaceAudioKind.Metal;
            if(SculptedObstaclePresentation.Build(root.transform,spec,ownedMeshes,steel,edgePearl,red,gold,neon,walnut)){}
            else if(pad)
            {
                var trigger=root.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(spec.size.x,.85f,spec.size.z);
                PremiumBoosterVisuals.Attach(root.transform,spec,catalog);
            }
            else if(spec.kind==ObstacleKind.MovingBridge||spec.kind==ObstacleKind.Elevator||spec.kind==ObstacleKind.ForwardPlatform||spec.kind==ObstacleKind.CircularPlatform||spec.kind==ObstacleKind.FallingTile||spec.kind==ObstacleKind.CollapsingBridge)
            {
                if(spec.kind==ObstacleKind.CircularPlatform)
                {
                    var mesh=ArcadeGeometry.PlatformCylinder(spec.size.x,spec.size.z,spec.size.y);ownedMeshes.Add(mesh);
                    var visual=ArcadeGeometry.MeshObject("Circular moving deck",root.transform,mesh,wood);
                    var collider=root.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;collider.sharedMaterial=contactSurfaces[(int)TrackSurface.Moving];
                }
                else
                {
                    root.AddComponent<BoxCollider>().size=spec.size;
                    root.GetComponent<Collider>().sharedMaterial=contactSurfaces[(int)TrackSurface.Moving];
                    Shape("Dynamic maple platform",PrimitiveType.Cube,root.transform,Vector3.zero,spec.size,wood);
                }
                for(int side=-1;side<=1;side+=2)
                {
                    if(spec.kind==ObstacleKind.CircularPlatform)continue;
                    var rail=Shape("Platform safety edge",PrimitiveType.Cube,root.transform,new Vector3(side*(spec.size.x*.5f-.12f),spec.size.y*.5f+.20f,0),new Vector3(.22f,.40f,spec.size.z),spec.kind==ObstacleKind.FallingTile||spec.kind==ObstacleKind.CollapsingBridge?gold:blue,true);
                    rail.GetComponent<Collider>().sharedMaterial=contactSurfaces[(int)TrackSurface.Moving];
                }
                Shape("Dynamic platform warning trim",PrimitiveType.Cube,root.transform,new Vector3(0,-spec.size.y*.5f,0),new Vector3(spec.size.x+.08f,.12f,spec.size.z),neon);
            }
            else if(spec.kind==ObstacleKind.RollingHazard)
            {
                var sphere=root.AddComponent<SphereCollider>();sphere.radius=spec.size.x*.5f;
                sphere.sharedMaterial=contactSurfaces[(int)TrackSurface.Standard];
                Shape("Rolling stone",PrimitiveType.Sphere,root.transform,Vector3.zero,Vector3.one*spec.size.x,steel);
                var band=ArcadeGeometry.Torus(spec.size.x*.48f,.045f,32,6);ownedMeshes.Add(band);ArcadeGeometry.MeshObject("Rolling hazard warning band",root.transform,band,gold);
            }
            else if(spec.kind==ObstacleKind.VerticalRotor||spec.kind==ObstacleKind.RotatingBlade)
            {
                Shape("Rotating wheel hub",PrimitiveType.Cylinder,root.transform,Vector3.zero,new Vector3(.7f,.3f,.7f),steel).transform.localRotation=Quaternion.Euler(90,0,0);
                for(int arm=0;arm<2;arm++)
                {
                    var blade=Shape("Rotating padded barrier",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(spec.size.x,.32f,.42f),spec.kind==ObstacleKind.RotatingBlade?gold:red,true);
                    blade.transform.localRotation=spec.kind==ObstacleKind.RotatingBlade?Quaternion.Euler(0,arm*90,0):Quaternion.Euler(0,0,arm*90);
                    Shape("Wheel spoke white stripe",PrimitiveType.Cube,blade.transform,new Vector3(0,0,-.53f),new Vector3(.85f,.7f,.06f),white);
                }
                if(spec.kind==ObstacleKind.VerticalRotor)
                {
                    var mesh=ArcadeGeometry.Torus(spec.size.x*.55f,.10f,32,6);ownedMeshes.Add(mesh);ArcadeGeometry.MeshObject("Rotating wheel hoop",root.transform,mesh,gold);
                }
            }
            else if(spec.kind==ObstacleKind.Pendulum)
            {
                Shape("Suspension pivot",PrimitiveType.Sphere,root.transform,Vector3.zero,Vector3.one*.45f,gold);
                Shape("Oak swing arm",PrimitiveType.Cylinder,root.transform,new Vector3(0,-1.35f,0),new Vector3(.18f,1.35f,.18f),walnut);
                var head=Shape("Red swinging hammer",PrimitiveType.Cylinder,root.transform,new Vector3(0,-2.75f,0),new Vector3(1.25f,1.35f,1.25f),red,true);
                head.transform.localRotation=Quaternion.Euler(0,0,90);
                var stripe=Shape("Silver hammer stripe",PrimitiveType.Cylinder,root.transform,new Vector3(0,-2.75f,0),new Vector3(1.28f,.25f,1.28f),white);
                stripe.transform.localRotation=Quaternion.Euler(0,0,90);
                var blackStripe=Shape("Dark hammer grip",PrimitiveType.Cylinder,root.transform,new Vector3(.55f,-2.75f,0),new Vector3(1.29f,.12f,1.29f),black);
                blackStripe.transform.localRotation=Quaternion.Euler(0,0,90);
                for(int side=-1;side<=1;side+=2)
                {
                    Shape("Hammer endcap",PrimitiveType.Sphere,root.transform,new Vector3(side*1.34f,-2.75f,0),new Vector3(.15f,1.13f,1.13f),red);
                    Shape("Support pylon",PrimitiveType.Cylinder,transform,spec.position+new Vector3(side*5,-1.9f,0),new Vector3(.22f,1.9f,.22f),steel);
                }
            }
            else if(spec.kind==ObstacleKind.Hammer)
            {
                root.AddComponent<BoxCollider>().size=new Vector3(spec.size.x,.7f,.8f);
                Shape("Rotating hammer bar",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(spec.size.x,.55f,.65f),steel);
                for(int side=-1;side<=1;side+=2)
                {
                    var head=Shape("Rotating red barrel",PrimitiveType.Cylinder,root.transform,new Vector3(side*spec.size.x*.42f,0,0),new Vector3(1.05f,.8f,1.05f),red);
                    head.transform.localRotation=Quaternion.Euler(90,0,0);
                    var band=Shape("Barrel white band",PrimitiveType.Cylinder,root.transform,new Vector3(side*spec.size.x*.42f,0,0),new Vector3(1.08f,.2f,1.08f),white);
                    band.transform.localRotation=Quaternion.Euler(90,0,0);
                }
                Shape("Turntable base",PrimitiveType.Cylinder,transform,spec.position+Vector3.down*.7f,new Vector3(.65f,.2f,.65f),gold);
            }
            else
            {
                var collider=root.AddComponent<BoxCollider>();collider.size=spec.size;
                Shape("Moving red obstacle",PrimitiveType.Cube,root.transform,Vector3.zero,spec.size,red);
                Shape("White safety band",PrimitiveType.Cube,root.transform,new Vector3(0,0,-spec.size.z*.51f),new Vector3(spec.size.x+.015f,.27f,.035f),white);
                Shape("Dark safety band",PrimitiveType.Cube,root.transform,new Vector3(0,-.38f,-spec.size.z*.51f),new Vector3(spec.size.x+.015f,.18f,.035f),black);
                Shape("Rounded obstacle crown",PrimitiveType.Capsule,root.transform,new Vector3(0,spec.size.y*.35f,0),new Vector3(spec.size.x*.95f,.23f,spec.size.z*.95f),red);
            }
            SculptedObstaclePresentation.RefineMechanism(root.transform,spec,ownedMeshes,steel,edgePearl,red,gold,neon);
            root.AddComponent<Rigidbody>().isKinematic=true;
            var motion=root.AddComponent<QuestObstacle>();motion.kind=spec.kind;motion.axis=spec.axis;motion.speed=spec.speed;motion.amplitude=spec.amplitude;motion.phase=spec.phase;motion.warningSeconds=spec.warningSeconds;
        }
        void Pickup(PickupSpec spec)
        {
            var trigger=new GameObject(spec.kind+" pickup");trigger.transform.SetParent(transform);trigger.transform.position=spec.position;trigger.layer=2;
            var sphere=trigger.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=.8f;
            var collectible=trigger.AddComponent<Collectible>();collectible.kind=spec.kind;collectible.session=session;
            PremiumCollectibleVisuals.Attach(trigger.transform,spec.kind,catalog);
        }
        void Gate(Vector3 point,bool finish,int index)
        {
            RouteFrame(definition,point,out var direction,out var width);
            var root=new GameObject(finish?"Finish arch":"Checkpoint "+(index+1));root.transform.SetParent(transform);root.transform.SetPositionAndRotation(point,Quaternion.LookRotation(direction,Vector3.up));
            var material=finish?gold:blue;
            // The chase camera rides about five metres above the deck. A header at
            // 2.7m passes directly between its lens and the ball after a checkpoint,
            // producing a screen-wide blue obstruction despite an unobstructed road.
            const float headerHeight=6.4f;
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Gate column",PrimitiveType.Cylinder,root.transform,new Vector3(side*(width*.5f+.1f),(headerHeight-.4f)*.5f,0),new Vector3(.28f,(headerHeight+.4f)*.5f,.28f),material);
                Shape("Gate finial",PrimitiveType.Sphere,root.transform,new Vector3(side*(width*.5f+.1f),headerHeight+.05f,0),Vector3.one*.42f,gold);
            }
            Shape("Arch header",PrimitiveType.Cube,root.transform,new Vector3(0,headerHeight,0),new Vector3(width+.4f,.65f,.45f),material);
            if(finish)
            {
                for(int x=0;x<16;x++)for(int row=0;row<2;row++)
                    Shape("Checkered finish banner",PrimitiveType.Cube,root.transform,new Vector3(-3.75f+x*.5f,headerHeight-.15f+row*.25f,-.235f),new Vector3(.49f,.24f,.025f),(x+row)%2==0?white:black);
            }
            // A gate spans the complete usable deck, including the wide landing-platform edges.
            // The ornamental arch is narrower; reaching the finish must not depend on entering its center.
            var box=root.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(width+1.5f,4,1.4f);
            var gate=root.AddComponent<CourseTrigger>();gate.session=session;gate.finish=finish;gate.index=index;gate.respawn=point+Vector3.up*.15f;
        }
        public static void RouteFrame(LevelDefinition level,Vector3 point,out Vector3 direction,out float width)
        {
            direction=Vector3.forward;width=12;float nearest=float.PositiveInfinity;
            if(level.routePoints!=null&&level.routePoints.Length>1)
            {
                for(int i=0;i<level.routePoints.Length-1;i++)
                {
                    Vector3 delta=level.routePoints[i+1]-level.routePoints[i];float t=Mathf.Clamp01(Vector3.Dot(point-level.routePoints[i],delta)/Mathf.Max(.001f,delta.sqrMagnitude));
                    float distance=(level.routePoints[i]+delta*t-point).sqrMagnitude;
                    if(distance>=nearest)continue;nearest=distance;direction=Vector3.ProjectOnPlane(delta,Vector3.up).normalized;
                    width=level.routeWidths!=null&&i<level.routeWidths.Length?level.routeWidths[i]:10;
                }
            }
            if(direction.sqrMagnitude<.1f)direction=Vector3.forward;
        }
        void CreateBall(LevelDefinition level,SaveData save)
        {
            Ball=SpawnCompetitor(catalog.Ball(save.selectedBall),catalog.Skin(save.selectedSkin),level.spawn);Ball.name="Player ball";Ball.allowJump=level.allowJump;Ball.Input.sensitivity=save.settings.sensitivity;
            var follower=Ball.gameObject.AddComponent<CoursePathFollower>();follower.level=level;
        }
        public BallController SpawnCompetitor(BallProfile profile,BallSkin skin,Vector3 position)
        {
            if(!profile)profile=catalog.balls[0];if(!skin)skin=catalog.skins[0];
            var go=QuestVisuals.Shape("Racing ball",PrimitiveType.Sphere,transform,position,Vector3.one,black);go.layer=8;
            go.AddComponent<Rigidbody>();go.AddComponent<BallInput>();var ball=go.AddComponent<BallController>();ball.ApplyProfile(profile);
            BallLook.Apply(go,skin,catalog);
            var trail=go.AddComponent<TrailRenderer>();trail.time=skin.trail?.25f:0;trail.startWidth=.15f;trail.endWidth=0;trail.sharedMaterial=catalog.particleMaterial;
            trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;trail.minVertexDistance=.08f;trail.numCapVertices=5;trail.numCornerVertices=4;
            trail.startColor=skin.color;trail.endColor=new Color(skin.color.r,skin.color.g,skin.color.b,0);
            var motion=go.AddComponent<BallMotionFx>();motion.skinTrail=skin.trail;motion.trail=trail;
            return ball;
        }
        void Scenery(LevelDefinition level)
        {
            if(premium==null)premium=new PremiumTrackPresentation(architecture,ownedMeshes);
            var random=new System.Random(level.id*991);float sea=level.killHeight-10;
            var bounds=new Bounds(level.spawn,Vector3.one);
            foreach(var piece in level.tracks)bounds.Encapsulate(piece.position);
            bounds.Encapsulate(level.finish);
            water.SetTextureScale("_BaseMap",new Vector2((bounds.size.x+800)/90,(bounds.size.z+700)/90));
            Shape("Ocean horizon",PrimitiveType.Cube,architecture,new Vector3(bounds.center.x,sea,bounds.center.z),new Vector3(bounds.size.x+800,.25f,bounds.size.z+700),water);
            if(level.streamedCampaign){GrandWorldPresentation.Build(premium,level,cliff,cliffDark,white,steel,gold,neon,foliage,foliageLight,walnut);return;}
            if(CrazyCoursePresentation.Applies(level))
            {
                if(level.signatureMechanic=="Island Descent"){water.SetColor("_BaseColor",new Color(.3f,.045f,.008f));water.EnableKeyword("_EMISSION");water.SetColor("_EmissionColor",new Color(.5f,.065f,.003f));}
                CrazyCoursePresentation.Build(premium,level,cliff,cliffDark,white,steel,gold,neon,foliage,foliageLight,walnut);
                return;
            }

            // The near scene is a designed floating archipelago, with layered rocks, rounded
            // trees and structural deck trim. Detail is below the driving plane and never
            // creates decorative colliders or blocks a camera view along the track.
            int islandCount=Mathf.Clamp(level.tracks.Length/7,8,16);
            for(int i=0;i<islandCount;i++)
            {
                int trackIndex=Mathf.Min(level.tracks.Length-1,Mathf.RoundToInt((i+.4f)/islandCount*(level.tracks.Length-1)));
                if(trackIndex<0)break;
                var piece=level.tracks[trackIndex];Ends(piece,out var a,out var b,out var right);
                int side=i%2==0?-1:1;float radius=4.5f+(i%3)*.8f;
                var point=(a+b)*.5f+right*side*(piece.size.x*.5f+radius+6)+Vector3.down*(7.8f+(i%3)*1.2f);
                if(!IslandClearOfRoads(point,radius,level.tracks))point+=right*side*(radius+12);
                if(!IslandClearOfRoads(point,radius,level.tracks))continue;
                premium.FloatingIsland(point,radius,turf,cliff,cliffDark,walnut,foliage,foliageLight,level.id*13+i,world==2);
                // Pearl waterfall ribbons are geometric rather than transparent overdraw.
                if(world==0&&i%3==1)
                {
                    var fall=point+right*side*radius*.75f;
                    premium.Box(fall+Vector3.down*3.4f,new Vector3(.65f,6.7f,.16f),neon);
                    premium.Box(fall+Vector3.down*3.6f+Vector3.right*.25f,new Vector3(.13f,6.9f,.18f),white);
                }
            }
            // Layered silhouettes and occasional tall landmarks give depth without filling
            // the mobile view with dozens of same-shaped block towers.
            int distantCount=Mathf.Clamp(Mathf.CeilToInt(bounds.size.z/45),5,12);
            for(int i=0;i<distantCount;i++)
            for(int side=-1;side<=1;side+=2)
            {
                float z=Mathf.Lerp(bounds.min.z-10,bounds.max.z+75,i/(float)Mathf.Max(1,distantCount-1));
                float x=bounds.center.x+side*(bounds.size.x*.5f+37+(i%3)*11);
                float height=23+(i*17+level.id*3)%39;
                Vector3 center=new Vector3(x,sea+height*.5f,z);
                if(world==0||world==3)
                {
                    float width=5+(i%3)*1.7f;
                    var facade=i%3==0?glassPearl:i%3==1?glass:glassSlate;
                    premium.BeveledBox(center,new Vector3(width,height,6),.5f,facade,edgePearl);
                    premium.BeveledBox(center+Vector3.up*(height*.5f+.3f),new Vector3(width+.7f,.6f,6.7f),.65f,edgePearl);
                    premium.BeveledBox(center+Vector3.up*(height*.5f+2),new Vector3(width*.65f,3,4.2f),.4f,facade,edgePearl);
                    premium.BeveledBox(center+Vector3.up*(height*.5f+3.6f),new Vector3(width*.65f+.3f,.22f,4.5f),.4f,edgePearl);
                    if(i%3==1)premium.BeveledBox(center+Vector3.up*(height*.2f),new Vector3(width+.18f,.3f,6.18f),.53f,edgePearl);
                    if(i%3==0)premium.Tube(center+Vector3.up*(height*.5f+3.5f),center+Vector3.up*(height*.5f+9),.12f,gold,8);
                }
                else
                {
                    float radius=8+(i%3)*2;
                    premium.Terrace(center+Vector3.up*height*.25f,radius,height*.7f,.07f,world==2?white:cliff,i*7+level.id,10);
                    premium.Terrace(center+Vector3.up*height*.25f,radius*.78f,height*.25f,.8f,world==2?white:turf,i*7+level.id,10);
                }
                if(i%2==0)
                {
                    var cloud=new Vector3(x*.53f,level.spawn.y-13,z+9);
                    premium.Ellipsoid(cloud,new Vector3(10,2.2f,5),white,12,6);
                    premium.Ellipsoid(cloud+new Vector3(-3,1.4f,0),new Vector3(4,3.3f,3.8f),white,10,6);
                    premium.Ellipsoid(cloud+new Vector3(4,1,0),new Vector3(4.6f,2.6f,3.8f),white,10,6);
                }
            }
            for(int i=0;i<4;i++)
            {
                var cloud=new Vector3(bounds.center.x+(i%2==0?-1:1)*(32+i*4),level.spawn.y+12+(i%3)*3,bounds.min.z+55+i*73);
                premium.Ellipsoid(cloud,new Vector3(9,1.9f,4.6f),white,12,6);
                premium.Ellipsoid(cloud+new Vector3(-2,1.1f,0),new Vector3(3.2f,2.5f,3),white,10,6);
            }
        }
        static bool IslandClearOfRoads(Vector3 center,float radius,TrackPiece[] pieces)
        {
            foreach(var piece in pieces)
            {
                Ends(piece,out var a,out var b,out _);var closest=RaceRoute.Closest(center,a,b,out _);
                if(closest.y<center.y-12||closest.y>center.y+6)continue;
                if(Vector2.Distance(new Vector2(center.x,center.z),new Vector2(closest.x,closest.z))<radius+piece.size.x*.5f+3)return false;
            }
            return true;
        }
        void Building(Vector3 center,float height,System.Random random)
        {
            float width=5+(float)random.NextDouble()*7,depth=5+(float)random.NextDouble()*6;
            var facade=height>49?glassPearl:height<34?glassSlate:glass;
            Shape("Glass skyline tower",PrimitiveType.Cube,architecture,center,new Vector3(width,height,depth),facade);
            for(int side=-1;side<=1;side+=2)
                Shape("Tower vertical frame",PrimitiveType.Cube,architecture,center+new Vector3(side*width*.49f,0,-depth*.51f),new Vector3(.3f,height+.3f,.3f),world==3?neon:glassDark);
            Shape("Building roof",PrimitiveType.Cube,architecture,center+Vector3.up*(height*.5f+.2f),new Vector3(width+1,.5f,depth+1),glassDark);
            Shape("Roof service house",PrimitiveType.Cube,architecture,center+Vector3.up*(height*.5f+1),new Vector3(width*.45f,1.5f,depth*.5f),blue);
            if(height>43)Shape("Tower antenna",PrimitiveType.Cylinder,architecture,center+Vector3.up*(height*.5f+4),new Vector3(.16f,3,.16f),gold);
            if(world==3)
            {
                Shape("Neon facade crown",PrimitiveType.Cube,architecture,center+new Vector3(0,height*.4f,-depth*.52f),new Vector3(width,.25f,.15f),gem);
                Shape("Neon light band",PrimitiveType.Cube,architecture,center+new Vector3(0,-height*.15f,-depth*.52f),new Vector3(width,.2f,.15f),neon);
            }
        }
        void DesertIsland(Vector3 center,float height,System.Random random)
        {
            float width=9+(float)random.NextDouble()*8;
            var rock=ArcadeGeometry.Cone(width*.55f,height,9);ownedMeshes.Add(rock);ArcadeGeometry.MeshObject("Faceted sandstone mesa",architecture,rock,glassDark).transform.localPosition=center;
            Shape("Sandstone plateau",PrimitiveType.Cylinder,architecture,center+Vector3.up*(height*.25f),new Vector3(width,.6f,width),wood);
            var arch=ArcadeGeometry.Torus(width*.27f,.55f,20,6,180);ownedMeshes.Add(arch);
            var archGo=ArcadeGeometry.MeshObject("Canyon stone arch",architecture,arch,walnut);archGo.transform.localPosition=center+Vector3.up*(height*.25f+2);
            for(int side=-1;side<=1;side+=2)
            {
                var trunk=Shape("Palm trunk",PrimitiveType.Cylinder,architecture,center+new Vector3(side*width*.3f,height*.25f+2,0),new Vector3(.25f,2,.25f),walnut);
                for(int i=0;i<4;i++)
                {
                    var frond=Shape("Palm crown",PrimitiveType.Capsule,architecture,trunk.transform.localPosition+Vector3.up*2,new Vector3(.5f,2.2f,.25f),blue);
                    frond.transform.localRotation=Quaternion.Euler(70,i*90,0);
                }
            }
        }
        void SnowIsland(Vector3 center,float height,System.Random random)
        {
            var rock=ArcadeGeometry.Cone(11,height,8);ownedMeshes.Add(rock);ArcadeGeometry.MeshObject("Glacier mountain",architecture,rock,glassDark).transform.localPosition=center;
            var snow=ArcadeGeometry.Cone(6,height*.55f,8);ownedMeshes.Add(snow);ArcadeGeometry.MeshObject("Snow peak",architecture,snow,white).transform.localPosition=center+Vector3.up*height*.24f;
            if(random.NextDouble()>.45)
            {
                var basePoint=center+new Vector3(9,-height*.1f,0);
                Shape("Snow-covered fir trunk",PrimitiveType.Cylinder,architecture,basePoint,new Vector3(.4f,3,.4f),steel);
                for(int i=0;i<3;i++)
                {
                    var fir=ArcadeGeometry.Cone(3-i*.6f,4.5f,8);ownedMeshes.Add(fir);ArcadeGeometry.MeshObject("Snow-covered fir branch",architecture,fir,white).transform.localPosition=basePoint+Vector3.up*(2+i*1.6f);
                }
            }
        }
        void Landmarks(LevelDefinition level)
        {
            if(level.streamedCampaign||CrazyCoursePresentation.Applies(level)){premium?.Flush();return;}
            for(int dropIndex=0;dropIndex<level.dropStarts.Length;dropIndex++)
            {
                float drop=level.dropStarts[dropIndex];
                Vector3 direction=level.dropDirections!=null&&dropIndex<level.dropDirections.Length?level.dropDirections[dropIndex]:Vector3.forward;
                Vector3 p=level.dropPoints!=null&&dropIndex<level.dropPoints.Length?ClosestPathPoint(level,level.dropPoints[dropIndex]+direction*25):PointAt(level,drop+25);
                var facing=Quaternion.LookRotation(direction,Vector3.up);Vector3 right=facing*Vector3.right;
                var ringMesh=ArcadeGeometry.Torus(6.8f,.37f,56,8);ownedMeshes.Add(ringMesh);
                var ring=ArcadeGeometry.MeshObject("Skywheel route landmark",architecture,ringMesh,world==3?neon:steel);ring.transform.localPosition=p+Vector3.up*5.7f;
                ring.transform.localRotation=facing;
                var goldMesh=ArcadeGeometry.Torus(7.05f,.12f,56,6);ownedMeshes.Add(goldMesh);
                var rim=ArcadeGeometry.MeshObject("Golden skywheel trim",architecture,goldMesh,gold);rim.transform.localPosition=ring.transform.localPosition;
                rim.transform.localRotation=facing;
                for(int i=0;i<12;i++)
                {
                    float angle=i*Mathf.PI*2/12;
                    var marker=Shape("Skywheel caution marker",PrimitiveType.Cube,architecture,p+Vector3.up*5.7f+right*(Mathf.Cos(angle)*6.8f)+Vector3.up*(Mathf.Sin(angle)*6.8f),new Vector3(.55f,.18f,.85f),gold);
                    marker.transform.localRotation=facing*Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
                }
            }
            for(int i=0;i<3;i++)
            {
                float z=55+i*150,x=(i%2==0?-1:1)*(38+i*5);
                var mesh=ArcadeGeometry.Torus(14+i*3,1.2f,52,8,315);ownedMeshes.Add(mesh);
                var ring=ArcadeGeometry.MeshObject("Distant looping skyway",architecture,mesh,world==3?gem:gold);ring.transform.localPosition=new Vector3(x,level.spawn.y+2,z);ring.transform.localRotation=Quaternion.Euler(0,i%2==0?24:-24,0);
                var inner=ArcadeGeometry.Torus(12.8f+i*3,.17f,52,6,315);ownedMeshes.Add(inner);
                var innerGo=ArcadeGeometry.MeshObject("Loop inner rail",architecture,inner,steel);innerGo.transform.SetPositionAndRotation(ring.transform.position,ring.transform.rotation);
            }
            premium?.Flush();
        }
        public static Vector3 PointAt(LevelDefinition level,float z)
        {
            foreach(var track in level.tracks)
            {
                Ends(track,out var a,out var b,out _);
                if(z>=a.z-.05f&&z<=b.z+.05f)return Vector3.Lerp(a,b,Mathf.InverseLerp(a.z,b.z,z));
            }
            return level.spawn;
        }
        public static Vector3 ClosestPathPoint(LevelDefinition level,Vector3 point)
        {
            if(level.routePoints==null||level.routePoints.Length<2)return point;
            Vector3 result=level.routePoints[0];float nearest=float.PositiveInfinity;
            for(int i=0;i<level.routePoints.Length-1;i++)
            {
                Vector3 delta=level.routePoints[i+1]-level.routePoints[i];float t=Mathf.Clamp01(Vector3.Dot(point-level.routePoints[i],delta)/Mathf.Max(.001f,delta.sqrMagnitude));
                Vector3 candidate=level.routePoints[i]+delta*t;float distance=(candidate-point).sqrMagnitude;
                if(distance<nearest){nearest=distance;result=candidate;}
            }
            return result;
        }
        void DropMarkers(LevelDefinition level)
        {
            for(int dropIndex=0;dropIndex<level.dropStarts.Length;dropIndex++)
            {
                float z=level.dropStarts[dropIndex];
                Vector3 direction=level.dropDirections!=null&&dropIndex<level.dropDirections.Length?level.dropDirections[dropIndex]:Vector3.forward;
                Vector3 p=level.dropPoints!=null&&dropIndex<level.dropPoints.Length?level.dropPoints[dropIndex]-direction*2:PointAt(level,z-2);
                var facing=Quaternion.LookRotation(direction,Vector3.up);Vector3 right=facing*Vector3.right;
                for(int i=-2;i<=2;i++)
                {
                    var stripe=Shape("Drop edge warning",PrimitiveType.Cube,transform,p+right*(i*1.2f)+Vector3.up*.025f,new Vector3(.5f,.03f,1.3f),i%2==0?gold:black);stripe.transform.rotation=facing*Quaternion.Euler(0,-30,0);
                }
                var sign=new GameObject("Freefall route cue");sign.transform.SetParent(transform);sign.transform.SetPositionAndRotation(p+Vector3.up*2.4f-direction*2,facing);
                var text=sign.AddComponent<TMPro.TextMeshPro>();text.font=catalog.font;text.text="DROP AHEAD";text.fontSize=2.3f;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(1,.88f,.22f);text.outlineColor=new Color(.03f,.09f,.15f);text.outlineWidth=.2f;
                text.rectTransform.sizeDelta=new Vector2(8,1);text.renderer.shadowCastingMode=ShadowCastingMode.Off;
                sign.AddComponent<CourseSignVisibility>();
            }
        }
        void Combine(Transform root)
        {
            CourseRenderBatcher.Combine(root,ownedMeshes);
        }
        void OnDestroy()
        {
            if(runtimeDefinition)Destroy(runtimeDefinition);
            if(session&&FollowCamera)session.Respawned-=FollowCamera.Snap;
            if(runtimeSky&&RenderSettings.skybox==runtimeSky)RenderSettings.skybox=catalog.skyMaterial;
            foreach(var material in ownedPhysics)if(material)Destroy(material);
            foreach(var material in ownedMaterials)if(material)Destroy(material);
            foreach(var texture in ownedTextures)if(texture)Destroy(texture);
            foreach(var mesh in ownedMeshes)if(mesh)Destroy(mesh);
        }
    }
}
