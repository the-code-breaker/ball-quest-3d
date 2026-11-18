using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    public static class SignatureAdventureGenerator
    {
        const string DirectoryPath = "Assets/_BallQuest/ScriptableObjects/Expeditions";
        const int Version = 3;
        static readonly string[] Kinds={"SkyMaze","ClockworkChaos","GravityLab","HimalayanPass","VolcanoIslands"};
        static readonly string[] Titles={"Sky Maze","Clockwork Chaos","Gravity Lab","Himalayan Pass","Volcano Islands"};
        static readonly string[] Hints={"Choose any lane. Explore, reverse and discover either exit.","Wait for the drawbridge. Ride the gear. Watch the wheel opening.","Cyan fields bend gravity: floor, wall and ceiling. Orbit to explore.","Brake on ice. Balance the suspension bridge or take the cliff path.","Build speed for the ramp jump, or ride the moving stone detour."};
        public static void Generate(GameCatalog catalog)
        {
            if(catalog.expeditionContentVersion>=Version&&catalog.expeditions!=null&&catalog.expeditions.Length==5)return;
            Directory.CreateDirectory(DirectoryPath);AssetDatabase.Refresh();
            var result=new List<LevelDefinition>();
            for(int i=0;i<Kinds.Length;i++)
            {
                string path=DirectoryPath+"/Expedition"+(56+i)+".asset";
                var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if(!level){level=ScriptableObject.CreateInstance<LevelDefinition>();AssetDatabase.CreateAsset(level,path);}
                level.id=56+i;level.title=Titles[i];level.hint=Hints[i];level.expedition=true;level.signatureMechanic=Kinds[i];level.world=i==2?3:i==3?2:i==4?1:0;level.difficulty=2;level.parSeconds=200;level.killHeight=i==4?8:0;level.allowJump=false;level.mission="Explore freely. Every chosen route is controlled by your own gestures.";level.mechanics=new[]{Kinds[i],"Manual360","signature-v5"};
                var routes=SignatureAdventurePaths.Variants(level);level.routePoints=routes[0].points;level.routeWidths=Enumerable.Repeat(8f,level.routePoints.Length).ToArray();
                level.spawn=level.routePoints[0]+Vector3.up*.65f;
                level.finish=level.routePoints[level.routePoints.Length-1]+(i==2?Vector3.down:Vector3.up)*.65f;
                level.checkpoints=new[]{level.routePoints[1]+Vector3.up*.65f};level.alternateFinishes=i==0?new[]{new Vector3(14,24.65f,114)}:Array.Empty<Vector3>();
                level.dropStarts=Array.Empty<float>();level.dropPoints=Array.Empty<Vector3>();level.dropDirections=Array.Empty<Vector3>();
                var build=new Build();
                if(i==0)Maze(build);else if(i==1)Clockwork(build);else if(i==2)Gravity(build);else if(i==3)Mountain(build);else Volcano(build);
                level.tracks=build.tracks.ToArray();level.obstacles=build.obstacles.ToArray();level.pickups=build.pickups.ToArray();
                EditorUtility.SetDirty(level);result.Add(level);
            }
            catalog.expeditions=result.ToArray();catalog.expeditionContentVersion=Version;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        sealed class Build
        {
            public readonly List<TrackPiece> tracks=new List<TrackPiece>();public readonly List<ObstacleSpec> obstacles=new List<ObstacleSpec>();public readonly List<PickupSpec> pickups=new List<PickupSpec>();
            public void Deck(Vector3 a,Vector3 b,float width=8,TrackStyle style=TrackStyle.Deck,bool rails=true,bool optional=false)
            {
                int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/4));
                for(int j=0;j<count;j++)
                {
                    Vector3 p=Vector3.Lerp(a,b,j/(float)count),q=Vector3.Lerp(a,b,(j+1)/(float)count),d=q-p;
                    var rotation=Quaternion.LookRotation(d,Vector3.up);tracks.Add(new TrackPiece{position=(p+q)*.5f-rotation*Vector3.up*.4f,size=new Vector3(width,.8f,d.magnitude+.1f),rotation=rotation.eulerAngles,rails=rails,style=style,surface=style==TrackStyle.Ice?TrackSurface.Ice:style==TrackStyle.HighGrip?TrackSurface.HighGrip:TrackSurface.Standard,alternate=optional});
                    if(j%2==0)pickups.Add(new PickupSpec{kind=PickupKind.Coin,position=(p+q)*.5f+Vector3.up*.9f});
                }
            }
            public void Road(float x,float y,float z,float tx,float ty,float tz,float width=8,TrackStyle style=TrackStyle.Deck,bool rails=true,bool optional=false)=>Deck(new Vector3(x,y,z),new Vector3(tx,ty,tz),width,style,rails,optional);
            public void Area(float x,float y,float z,float width,float depth)=>tracks.Add(new TrackPiece{position=new Vector3(x,y-.4f,z),size=new Vector3(width,.8f,depth),rails=false});
            public void Hazard(ObstacleKind kind,Vector3 position,Vector3 size,float amplitude=2,float speed=.7f,Vector3? axis=null)=>obstacles.Add(new ObstacleSpec{kind=kind,position=position,size=size,amplitude=amplitude,speed=speed,axis=axis??Vector3.right,warningSeconds=2});
        }
        static void Maze(Build b)
        {
            b.Road(0,24,-4,0,24,22,10,rails:false);b.Area(0,24,22,50,10);b.Area(0,24,82,52,10);
            b.Road(-20,24,22,-20,24,42,3.8f,TrackStyle.HighGrip,false,true);b.Road(-20,24,42,-20,24,58,3.8f,TrackStyle.HighGrip,false,true);b.Road(-20,24,58,-20,24,82,3.8f,TrackStyle.HighGrip,false,true);
            b.Road(0,24,22,0,24,82,7,rails:false);b.Hazard(ObstacleKind.Pendulum,new Vector3(2.9f,27.8f,50),new Vector3(1,1,1),1.2f,.6f,Vector3.forward);
            b.Road(20,24,22,20,24,33,9,rails:false,optional:true);b.Road(20,24,33,26,24,43,9,rails:false,optional:true);b.Road(26,24,43,26,24,57,9,rails:false,optional:true);b.Road(26,24,57,20,24,74,9,rails:false,optional:true);b.Road(20,24,74,20,24,82,9,rails:false,optional:true);
            b.Road(-20,24,46,-32,24,46,4,rails:false,optional:true);b.Road(-32,24,46,-32,24,30,4,rails:false,optional:true);b.pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=new Vector3(-32,24.9f,32)});b.pickups.Add(new PickupSpec{kind=PickupKind.Token,position=new Vector3(-32,24.9f,31)});
            b.Road(26,24,55,42,24,55,4,rails:false,optional:true);
            b.Road(0,24,82,0,24,93,10,rails:false);
            foreach(int side in new[]{-1,1}){b.Road(0,24,92,side*14,24,102,8,rails:false,optional:side>0);b.Road(side*14,24,102,side*14,24,119,10,rails:false,optional:side>0);}
        }
        static void Clockwork(Build b)
        {
            b.Road(0,24,-4,0,24,14,10,rails:false);b.Road(0,24,24,0,24,27,10,rails:false);b.Road(0,24,41,0,24,116,9,rails:false);
            b.Road(0,24,44,18,24,44,6,rails:false,optional:true);b.Road(18,24,44,18,24,86,6,rails:false,optional:true);b.Road(18,24,86,0,24,86,6,rails:false,optional:true);
            b.Hazard(ObstacleKind.TimedDoor,new Vector3(0,25.2f,97),new Vector3(9,2.4f,1),2,.7f);b.Hazard(ObstacleKind.SlidingWall,new Vector3(-2.8f,25,82),new Vector3(1.2f,2,1),1.5f,.6f);
        }
        static void Gravity(Build b)
        {
            b.Road(0,18,-5,0,18,24,9,TrackStyle.HighGrip,false);
            // The curved wall/ceiling is built with properly oriented solids by SignatureCourseRuntime.
            b.pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=new Vector3(0,41.1f,5)});
        }
        static void Mountain(Build b)
        {
            b.Road(0,30,-4,0,30,17,10,rails:false);b.Road(0,30,17,12,32,31,7,rails:false);b.Road(12,32,31,12,32,46,3.4f,TrackStyle.HighGrip,false);b.Road(12,32,46,4,32,58,7,TrackStyle.Ice,false);b.Road(4,32,58,-8,32,64,7,TrackStyle.Ice,false);b.Road(-8,32,64,-14,32,74,7,TrackStyle.Ice,false);b.Road(-14,32,74,-14,32,88,7,rails:false);b.Road(-14,32,88,0,30,102,8,rails:false);b.Road(0,30,102,0,30,121,10,rails:false);
            b.Road(0,30,17,-22,30,29,7,rails:false,optional:true);b.Road(-22,30,29,-22,30,56,7,rails:false,optional:true);
            // Finish climbing clear of the higher ice road, then merge at the same height.
            // Overlapping the uphill ramp with that road creates an unintended vertical step.
            b.Road(-22,30,56,-22,32,64,7,rails:false,optional:true);b.Road(-22,32,64,-12,32,70,7,rails:false,optional:true);
            b.Hazard(ObstacleKind.RollingHazard,new Vector3(-16,33,72),new Vector3(1.3f,1.3f,1.3f),2,.55f);b.pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=new Vector3(12,32.9f,39)});
        }
        static void Volcano(Build b)
        {
            b.Road(0,24,-4,0,24,16,12,rails:false);b.Road(0,24,16,0,26,25,9,rails:false);b.Road(0,24,28.2f,0,24,42,14,rails:false);b.Road(0,24,54,0,24,116,10,rails:false);
            b.Hazard(ObstacleKind.CollapsingBridge,new Vector3(0,23.6f,48),new Vector3(9,.8f,12),1,.8f);
            b.Road(0,24,16,18,24,26,8,rails:false,optional:true);b.Road(18,24,26,18,24,37,8,rails:false,optional:true);b.Road(18,24,51,18,24,60,8,rails:false,optional:true);b.Road(18,24,60,0,24,66,8,rails:false,optional:true);
            b.Hazard(ObstacleKind.ForwardPlatform,new Vector3(18,23.6f,44),new Vector3(10,.8f,16),1,.5f,Vector3.forward);b.Hazard(ObstacleKind.VerticalRotor,new Vector3(3.5f,26.5f,81),new Vector3(1,1,1),1,.65f,Vector3.forward);
            b.pickups.Add(new PickupSpec{kind=PickupKind.Token,position=new Vector3(0,24.9f,49)});
        }
    }
}
