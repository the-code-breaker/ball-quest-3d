using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    // Deterministic authored grammar: 25 topology recipes, secondary sections,
    // twenty environments, width/height/radius variation and actual objectives.
    public static class GrandCampaignGenerator
    {
        public const int Count=1000,Version=31;
        public static readonly string[] Families={"Ribbon Voyage","Terrace Switchbacks","Helix Tower","Diamond Crossing","Needle Balance","Island Freefall","Clockwork Gates","Sky Shuttles","Slalom Court","Velocity Circuit","Pinwheel Ascent","Rooftop Stairway","Infinity Overpass","Crescent Traverse","Fragile Causeway","Wave Crest","Twin Orbit","Forked Expedition","Barrel Gauntlet","Falling Spiral","Chicane Relay","Reverse Summit","Braided Heights","Mechanical Crossing","Crown Gauntlet"};
        sealed class Route
        {
            public readonly List<Vector3> points=new List<Vector3>();public readonly List<float> widths=new List<float>();public readonly List<int> flags=new List<int>();
            public readonly List<ObstacleSpec> hazards=new List<ObstacleSpec>();public readonly List<PickupSpec> pickups=new List<PickupSpec>();public readonly List<TrackPiece> bonus=new List<TrackPiece>();
            public readonly List<Vector3> checkpoints=new List<Vector3>(),drops=new List<Vector3>(),dropDirections=new List<Vector3>();
            public Vector3 last,forward=Vector3.forward;public Vector3 Right=>Vector3.Cross(Vector3.up,forward).normalized;public float length;int coinStep;
            public Route(){last=new Vector3(0,52,0);points.Add(last);}
            public void To(Vector3 target,float width,bool rails=true,TrackStyle style=TrackStyle.Deck)
            {
                Vector3 start=last,delta=target-start;int n=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/3));
                for(int i=1;i<=n;i++)
                {
                    var p=Vector3.Lerp(start,target,i/(float)n);points.Add(p);widths.Add(width);flags.Add((rails?1:0)|((int)style<<1));length+=Vector3.Distance(last,p);
                    if(++coinStep%3==0)pickups.Add(new PickupSpec{kind=PickupKind.Coin,position=(last+p)*.5f+Vector3.up*.9f});last=p;
                }
                var planar=Vector3.ProjectOnPlane(delta,Vector3.up);if(planar.sqrMagnitude>.001f)forward=planar.normalized;
            }
            public void Run(float d,float w,float rise=0,bool rails=true,TrackStyle style=TrackStyle.Deck)=>To(last+forward*d+Vector3.up*rise,w,rails,style);
            public void Turn(float angle,float radius,float width,float rise=0,bool rails=true)
            {
                Vector3 start=last,heading=forward,right=Right;int n=Mathf.CeilToInt(Mathf.Abs(angle)/8);float sign=Mathf.Sign(angle);
                for(int i=1;i<=n;i++){float t=i/(float)n,a=Mathf.Abs(angle)*Mathf.Deg2Rad*t;To(start+right*sign*radius*(1-Mathf.Cos(a))+heading*radius*Mathf.Sin(a)+Vector3.up*rise*t,width,rails);}
                forward=Quaternion.AngleAxis(angle,Vector3.up)*heading;
            }
            public void Bend(float side,float distance,float width,float rise=0,bool rails=true,TrackStyle style=TrackStyle.Deck)
            {
                Vector3 start=last,heading=forward,right=Right;int n=Mathf.CeilToInt(distance/3);
                for(int i=1;i<=n;i++){float t=i/(float)n;To(start+heading*distance*t+right*side*Mathf.SmoothStep(0,1,t)+Vector3.up*rise*t,width,rails,style);}forward=heading;
            }
            public void Beam(float d,float w){Run(4,5,0,true,TrackStyle.HighGrip);Run(d,w,0,false,TrackStyle.Bridge);Run(5,5.5f,0,true,TrackStyle.HighGrip);}
            public void Checkpoint(){Run(9,7,0,true,TrackStyle.HighGrip);checkpoints.Add(last-forward*3+Vector3.up*.65f);}
            public void Hazard(ObstacleKind kind,float phase,float speed=1)
            {
                Vector3 start=last,heading=forward,right=Right;Run(24,6.4f,0,true,TrackStyle.HighGrip);
                bool gate=kind==ObstacleKind.TimedDoor;bool hammer=kind==ObstacleKind.Hammer,pendulum=kind==ObstacleKind.Pendulum;
                hazards.Add(new ObstacleSpec{kind=kind,position=start+heading*12+right*(gate||hammer||pendulum?0:1.6f*(phase%2<1?1:-1))+Vector3.up*(gate?1.2f:pendulum?3.5f:.65f),
                    rotation=Quaternion.LookRotation(heading).eulerAngles,size=gate?new Vector3(6.8f,2.5f,.6f):hammer?new Vector3(2.5f,1,1):pendulum?new Vector3(2,1,1):new Vector3(.95f,1.25f,.95f),
                    axis=pendulum?Vector3.forward:right,amplitude=gate?0:pendulum?.8f:.30f,speed=speed,phase=phase});
            }
            public void Boost(float d)
            {
                Vector3 start=last,heading=forward;Run(d,6.3f);hazards.Add(new ObstacleSpec{kind=ObstacleKind.Booster,position=start+heading*8+Vector3.up*.12f,size=new Vector3(4.5f,.22f,5),rotation=Quaternion.LookRotation(heading).eulerAngles,axis=heading});Run(14,7,0,true,TrackStyle.HighGrip);
            }
            public void Gap(bool fragile,float width)
            {
                Run(6,5.6f,0,true,TrackStyle.HighGrip);Vector3 start=last;last+=forward*6;points.Add(last);widths.Add(width);flags.Add(CampaignGeometry.Skip);length+=6;
                hazards.Add(new ObstacleSpec{kind=fragile?ObstacleKind.FallingTile:ObstacleKind.MovingBridge,position=(start+last)*.5f-Vector3.up*.35f,size=new Vector3(width,.7f,fragile?6.9f:8.8f),rotation=Quaternion.LookRotation(forward).eulerAngles,axis=Right,amplitude=fragile?0:.5f,speed=.75f,warningSeconds=1.8f});Run(8,5.8f,0,true,TrackStyle.HighGrip);
            }
            public void Drop(float height)
            {
                Run(7,4.8f,0,false);drops.Add(last);dropDirections.Add(forward);last-=Vector3.up*height;points.Add(last);widths.Add(9);flags.Add(CampaignGeometry.Skip);length+=height;Run(34,9,0,true,TrackStyle.HighGrip);
            }
            public void Fork(float side,float d,float width)
            {
                Vector3 start=last,heading=forward,right=Right;Bend(-side,d*.5f,width,1);Bend(side,d*.5f,width,-1);Vector3 end=last,middle=(start+end)*.5f+right*side;
                foreach(var ab in new[]{(a:start,b:middle),(a:middle,b:end)}){var q=Quaternion.LookRotation(ab.b-ab.a);bonus.Add(new TrackPiece{position=(ab.a+ab.b)*.5f-q*Vector3.up*.4f,size=new Vector3(2.1f,.8f,Vector3.Distance(ab.a,ab.b)+.1f),rotation=q.eulerAngles,rails=false,alternate=true});}
                pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=middle+Vector3.up*.9f});forward=heading;
            }
            public void Eight(float x,float z,float rise,float width)
            {
                Vector3 start=last,heading=forward,right=Right;
                for(int i=1;i<=80;i++){float t=i/80f,a=t*Mathf.PI*2;To(start+right*x*Mathf.Sin(a)+heading*z*Mathf.Sin(a*2)+Vector3.up*rise*t,width);}
                forward=heading;
            }
        }
        public static void Generate(GameCatalog catalog)
        {
            if(catalog.contentVersion>=Version&&catalog.levels.Length==Count)return;
            var output=new List<LevelDefinition>(Count);var fingerprints=new HashSet<string>();var summary=new StringBuilder("id,title,environment,archetype,points,challenge,seed\n");
            AssetDatabase.StartAssetEditing();
            try
            {
                for(int id=1;id<=Count;id++)
                {
                    int kind=((id-1)/20+(id-1)*7)%25;if(id==1)kind=0;
                    Route route=null;int seed=0;bool valid=false;
                    for(int attempt=0;attempt<80;attempt++)
                    {
                        seed=id*7919+attempt*104729;route=Compose(id,kind,seed);
                        string hash=Fingerprint(route.points);if(Safe(route)&&fingerprints.Add(hash)){valid=true;break;}
                    }
                    if(!valid)throw new InvalidOperationException("Course grammar has no safe unique candidate: "+id+" family "+kind);
                    string path="Assets/_BallQuest/ScriptableObjects/Levels/Level"+id.ToString(id<=55?"000":"0000")+".asset";
                    var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);if(!level){level=ScriptableObject.CreateInstance<LevelDefinition>();AssetDatabase.CreateAsset(level,path);}
                    int env=(id-1)%20;level.id=id;level.title=GrandWorldPresentation.Names[env]+" · "+Families[kind]+" "+((id-1)/500+1);
                    // Include the challenge number in the title to make catalog search unambiguous.
                    level.title=GrandWorldPresentation.ShortNames[env]+" "+new[]{"Ribbons","Terraces","Helix","Crossing","Needles","Freefall","Gates","Shuttles","Slalom","Velocity","Pinwheel","Stairway","Infinity","Crescent","Fragile","Crest","Orbits","Forks","Barrels","Spiral","Chicane","Summit","Braids","Machines","Gauntlet"}[kind]+" "+id;
                    level.streamedCampaign=true;level.environment=env;level.archetype=kind;level.layoutSeed=seed;level.world=env%4;level.expedition=false;level.signatureMechanic=Families[kind];level.difficulty=id==1?1:id<=8?2:id<200?3:4;
                    level.routePoints=route.points.ToArray();level.routeWidths=route.widths.Concat(new[]{6.5f}).ToArray();level.roadFlags=route.flags.ToArray();level.bonusTracks=route.bonus.ToArray();level.tracks=Array.Empty<TrackPiece>();
                    level.spawn=route.points[0]+Vector3.forward*3+Vector3.up*.62f;level.finish=route.last-route.forward*4+Vector3.up*.7f;level.checkpoints=route.checkpoints.ToArray();level.obstacles=route.hazards.ToArray();
                    route.pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=route.last-route.forward*8+route.Right*1.3f+Vector3.up*.9f});level.pickups=route.pickups.ToArray();
                    level.dropPoints=route.drops.ToArray();level.dropDirections=route.dropDirections.ToArray();level.dropStarts=route.drops.Select(p=>p.z).ToArray();level.killHeight=route.points.Min(p=>p.y)-12;level.allowJump=false;level.alternateFinishes=Array.Empty<Vector3>();
                    level.parSeconds=Mathf.Max(75,route.length/3.1f+20+route.hazards.Count(h=>h.kind==ObstacleKind.TimedDoor)*5);level.challengeSeconds=level.parSeconds+10;
                    level.challenge=(ChallengeKind)(1+(id-1)%4);if(kind==9||kind==20)level.challenge=ChallengeKind.BoostRun;
                    level.challengeTarget=level.challenge==ChallengeKind.CoinHunt?Mathf.Max(3,Mathf.CeilToInt(route.pickups.Count(p=>p.kind==PickupKind.Coin)*.6f)):1;level.challengeReward=30+level.difficulty*10;
                    level.mission=ChallengeRules.Description(level.challenge,level.challengeTarget,level.challengeSeconds);level.hint=id==1?"Repeat swipes to build speed. Hold still to coast.":level.mission;level.mechanics=new[]{Families[kind],GrandWorldPresentation.Names[env],level.challenge.ToString()};
                    EditorUtility.SetDirty(level);output.Add(level);summary.AppendLine(id+","+level.title+","+env+","+kind+","+route.points.Count+","+level.challenge+","+seed);
                }
                catalog.levels=output.ToArray();catalog.contentVersion=Version;EditorUtility.SetDirty(catalog);
            }
            finally{AssetDatabase.StopAssetEditing();}
            AssetDatabase.SaveAssets();Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/grand-campaign-catalog-v11.csv",summary.ToString());Debug.Log("Grand campaign: 1000 unique compact courses, 25 topology recipes, 20 biomes, per-level objectives; only active collision geometry expands.");
        }
        static Route Compose(int id,int kind,int seed)
        {
            var random=new System.Random(seed);float t=Mathf.Clamp01((id-1)/350f),w=Mathf.Lerp(4.8f,3.15f,t),needle=Mathf.Lerp(2.45f,1.7f,t);
            float a=9+(float)random.NextDouble()*8,b=23+(float)random.NextDouble()*13,sign=random.Next(2)==0?-1:1,radius=10+(float)random.NextDouble()*5;
            var r=new Route();r.Run(12,6.5f);
            switch(kind)
            {
                case 0:for(int i=0;i<3;i++)r.Bend((i%2==0?1:-1)*a*sign,b,w,i==1?2:0);break;
                case 1:r.Run(b,w,3);r.Turn(180*sign,radius,w,6);r.Run(b*.9f,w,3);r.Turn(-180*sign,radius+2,w,6);r.Run(18,w);break;
                case 2:r.Turn(sign*360,radius,w,17);r.Run(15,w);if(id>100)r.Turn(-sign*300,radius*.85f,w,16);break;
                case 3:r.Bend(a,b,w,3);r.Bend(-a*2,b,w,3);r.Bend(a,b,w,3);r.Run(20,w);break;
                case 4:r.Beam(b,needle);r.Turn(sign*80,radius,w,3);r.Beam(b*.8f,needle+.1f);r.Turn(-sign*75,radius,w,3);break;
                case 5:r.Bend(sign*a,20,w);r.Drop(3.6f+(float)random.NextDouble());r.Bend(-sign*a,22,w);r.Drop(3.8f);break;
                case 6:r.Hazard(ObstacleKind.TimedDoor,(float)random.NextDouble()*4,.85f);r.Bend(sign*a,b,w,3);r.Hazard(ObstacleKind.Hammer,1,.85f+t*.3f);break;
                case 7:r.Bend(a*sign,20,w,2);r.Gap(false,3.6f);r.Bend(-a*sign,22,w,2);r.Gap(false,3.7f);break;
                case 8:for(int i=0;i<3;i++){r.Bend((i%2==0?1:-1)*a,b*.6f,w);r.Hazard(i%2==0?ObstacleKind.Piston:ObstacleKind.RollerDrum,i*.7f,.8f+t*.4f);}break;
                case 9:r.Boost(42+b*.2f);r.Turn(sign*120,16,w+.7f,3);r.Checkpoint();r.Beam(18,needle+.2f);break;
                case 10:for(int i=0;i<4;i++){r.Run(12+i,w,2);r.Turn(sign*90,radius,w,4);}r.Run(18,w);break;
                case 11:for(int i=0;i<4;i++){r.Run(18+i*2,w,3);r.Bend(sign*(i%2==0?a:-a),12,w,1);}break;
                case 12:r.Eight(a+7,b*.65f,18+(float)random.NextDouble()*5,w);r.Run(18,w);break;
                case 13:r.Turn(sign*220,radius+3,w,12);r.Beam(b*.75f,needle);r.Turn(-sign*110,radius,w,7);break;
                case 14:r.Bend(a*sign,b,w,2);r.Gap(true,Mathf.Max(2.8f,w));r.Bend(-a*sign,b,w,2);r.Gap(true,Mathf.Max(2.8f,w));break;
                case 15:for(int i=0;i<4;i++){r.Bend((i%2==0?1:-1)*a,20,w,i%2==0?3:-2);}break;
                case 16:r.Turn(sign*340,radius,w,18);r.Run(19,w,2);r.Turn(-sign*330,radius*.8f,w,18);break;
                case 17:r.Fork(a*.75f,b+15,w);r.Turn(sign*70,radius,w,3);r.Fork(a*.65f,b+10,w);break;
                case 18:r.Hazard(ObstacleKind.RollerDrum,.2f,1);r.Turn(sign*90,radius,w,3);r.Hazard(ObstacleKind.SweepArm,1.4f,1+t*.4f);r.Turn(-sign*90,radius,w,3);break;
                case 19:r.Turn(sign*270,radius,w,14);r.Run(12,w);r.Drop(4);r.Turn(-sign*90,radius,w,2);break;
                case 20:r.Boost(44);r.Bend(sign*a,28,w+1);r.Checkpoint();r.Bend(-sign*a*1.3f,29,w+.5f);r.Hazard(ObstacleKind.Piston,2,1);break;
                case 21:r.Run(b,w,5);r.Turn(sign*180,radius,w,8);r.Beam(b*.75f,needle);r.Turn(-sign*180,radius+2,w,8);r.Run(18,w);break;
                case 22:r.Eight(a+4,b*.6f,21,w);r.Run(20,w,3);r.Fork(a*.6f,29,w);break;
                case 23:r.Hazard(ObstacleKind.Pendulum,.7f,.7f);r.Bend(sign*a,b,w,2);r.Gap(false,3.6f);r.Turn(-sign*85,radius,w,5);break;
                default:r.Turn(sign*270,radius,w,17);r.Beam(21,needle);r.Checkpoint();r.Hazard(ObstacleKind.Hammer,.8f,1);r.Gap(true,3.1f);r.Turn(-sign*90,radius,w,6);break;
            }
            if(id>500)
            {
                // A full additional late-campaign act prevents the second set of
                // biome/topology combinations from becoming seed-only reprises.
                r.Checkpoint();r.Run(12,6.2f,5,true,TrackStyle.HighGrip);
                switch((id/13)%8)
                {
                    case 0:for(int i=0;i<3;i++)r.Bend(sign*(i%2==0?1:-1)*a*1.7f,b*1.25f,w,4);r.Beam(23,needle);break;
                    case 1:r.Hazard(ObstacleKind.RollerDrum,1.4f,1.2f);r.Bend(sign*a*1.5f,32,w,4);r.Hazard(ObstacleKind.Piston,.4f,1.25f);r.Beam(22,needle+.1f);break;
                    case 2:r.Drop(4.4f);r.Bend(-sign*a*1.5f,31,w,2);r.Drop(4.1f);break;
                    case 3:r.Turn(sign*240,radius+6,w,22);r.Run(22,w,3);r.Beam(21,needle);break;
                    case 4:r.Turn(sign*150,radius+5,w,10);r.Beam(28,needle);r.Turn(-sign*150,radius+7,w,10);r.Run(18,w);break;
                    case 5:r.Gap(false,3.1f);r.Bend(sign*a*1.8f,35,w,5);r.Gap(true,3.0f);r.Bend(-sign*a,25,w,3);break;
                    case 6:r.Boost(48);r.Turn(sign*70,24,w+1,6);r.Run(15,7,0,true,TrackStyle.HighGrip);r.Beam(28,needle);break;
                    default:r.Fork(a*1.2f,43,w);r.Bend(-sign*a,24,w,4);r.Fork(a,40,w);r.Hazard(ObstacleKind.SweepArm,.8f,1.2f);break;
                }
            }
            r.Checkpoint();
            // Different secondary sections change the experience as well as the seed.
            switch((id/25)%6)
            {
                case 0:r.Bend(-sign*a*.55f,18,w,2);r.Run(11+(id%9),w);break;
                case 1:r.Beam(13+(id%13),needle+.15f);break;
                case 2:r.Hazard(ObstacleKind.Piston,id*.17f,.7f+t*.4f);r.Turn(sign*45,radius,w,3);break;
                case 3:r.Fork(a*.6f,29+(id%11),w);break;
                case 4:r.Bend(sign*a*.8f,22,w,3,true,id%3==0?TrackStyle.Ice:TrackStyle.HighGrip);r.Bend(-sign*a*.5f,19,w,1);break;
                default:r.Gap(id%2==0,3.5f);r.Turn(-sign*55,radius,w,3);break;
            }
            r.Run(14,6.5f,0,true,TrackStyle.HighGrip);return r;
        }
        public static bool Safe(LevelDefinition level)=>Safe(level.routePoints,level.routeWidths,level.roadFlags);
        static bool Safe(Route route)=>Safe(route.points.ToArray(),route.widths.Concat(new[]{6.5f}).ToArray(),route.flags.ToArray());
        static bool Safe(Vector3[] p,float[] widths,int[] flags)
        {
            for(int i=0;i<p.Length-1;i++)for(int j=i+10;j<p.Length-1;j++)
            {
                if((flags[i]&CampaignGeometry.Skip)!=0||(flags[j]&CampaignGeometry.Skip)!=0)continue;
                Vector3 a=p[i],b=p[i+1],c=p[j],d=p[j+1];Vector2 ab=new Vector2(b.x-a.x,b.z-a.z),cd=new Vector2(d.x-c.x,d.z-c.z),offset=new Vector2(c.x-a.x,c.z-a.z);
                float den=ab.x*cd.y-ab.y*cd.x;if(Mathf.Abs(den)<.00001f)continue;
                float t=(offset.x*cd.y-offset.y*cd.x)/den,u=(offset.x*ab.y-offset.y*ab.x)/den;
                if(t>=0&&t<=1&&u>=0&&u<=1&&Mathf.Abs(Mathf.Lerp(a.y,b.y,t)-Mathf.Lerp(c.y,d.y,u))<4)return false;
            }
            return true;
        }
        public static string Fingerprint(IList<Vector3> points)
        {
            var lengths=new float[points.Count];var bounds=new Bounds(points[0],Vector3.zero);
            for(int i=1;i<points.Count;i++){lengths[i]=lengths[i-1]+Vector3.Distance(points[i-1],points[i]);bounds.Encapsulate(points[i]);}
            float scale=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));var text=new StringBuilder();int segment=0;
            for(int i=0;i<40;i++){float distance=lengths[lengths.Length-1]*i/39f;while(segment<points.Count-2&&lengths[segment+1]<distance)segment++;float t=(distance-lengths[segment])/Mathf.Max(.001f,lengths[segment+1]-lengths[segment]);Vector3 p=(Vector3.Lerp(points[segment],points[segment+1],t)-points[0])/scale;text.Append(Mathf.RoundToInt(p.x*100000)).Append(',').Append(Mathf.RoundToInt(p.y*100000)).Append(',').Append(Mathf.RoundToInt(p.z*100000)).Append(';');}
            using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
        }
    }
}
