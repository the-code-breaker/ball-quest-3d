using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    public static class ProductionCampaignGenerator
    {
        const string Root="Assets/_BallQuest";
        enum Part { Roll,Left,Right,Zigzag,Hill,Slalom,Narrow,Hammer,Pendulum,Wall,Turbo,Jump,Ice,Grip,Wheel,Falling,Lift,Shuttle,Door,Roller,Push,Crush,Conveyor,Bounce,Drop,Spiral,Split,Blade,Forward,Circle }
        sealed class Recipe
        {
            public string title;public Part[] parts;
            public Recipe(string title,params Part[] parts){this.title=title;this.parts=parts;}
        }
        static readonly Recipe[] Recipes={
            new Recipe("Harbor Launch",Part.Roll,Part.Right,Part.Roll,Part.Slalom),
            new Recipe("Crane Alley",Part.Left,Part.Right,Part.Roll,Part.Left),
            new Recipe("Skywheel Sprint",Part.Grip,Part.Hill,Part.Right,Part.Roll),
            new Recipe("Canyon Courier",Part.Roll,Part.Split,Part.Left,Part.Roll),
            new Recipe("Temple Switchback",Part.Slalom,Part.Left,Part.Right,Part.Grip),
            new Recipe("Dune Dive",Part.Roll,Part.Pendulum,Part.Right,Part.Grip),
            new Recipe("Glacier Glide",Part.Left,Part.Hammer,Part.Grip,Part.Right),
            new Recipe("Frostline Bridge",Part.Right,Part.Narrow,Part.Left,Part.Hill),
            new Recipe("Polar Plunge",Part.Roll,Part.Jump,Part.Grip,Part.Right),
            new Recipe("Neon Overdrive",Part.Roll,Part.Turbo,Part.Left,Part.Hill),
            new Recipe("Signal Slalom",Part.Slalom,Part.Wall,Part.Right,Part.Narrow),
            new Recipe("Final Ascent",Part.Hill,Part.Ice,Part.Grip,Part.Left),
            new Recipe("Wheel Window",Part.Roll,Part.Wheel,Part.Right,Part.Narrow),
            new Recipe("Harbor Weave",Part.Left,Part.Right,Part.Hammer,Part.Slalom),
            new Recipe("Gate Graduation",Part.Narrow,Part.Wall,Part.Jump,Part.Grip,Part.Pendulum),
            new Recipe("Falling Footsteps",Part.Roll,Part.Falling,Part.Grip,Part.Left,Part.Slalom),
            new Recipe("Lift Above",Part.Hill,Part.Lift,Part.Right,Part.Hammer,Part.Grip),
            new Recipe("Shuttle Crossing",Part.Left,Part.Shuttle,Part.Narrow,Part.Wall,Part.Right),
            new Recipe("Push And Pass",Part.Roll,Part.Push,Part.Left,Part.Wheel,Part.Grip),
            new Recipe("Door Rhythm",Part.Right,Part.Door,Part.Hill,Part.Pendulum,Part.Roll),
            new Recipe("Rolling Quarry",Part.Slalom,Part.Roller,Part.Right,Part.Narrow,Part.Grip),
            new Recipe("Banked Lookout",Part.Left,Part.Spiral,Part.Grip,Part.Wall,Part.Roll),
            new Recipe("Three Lane Choice",Part.Split,Part.Slalom,Part.Push,Part.Right,Part.Jump),
            new Recipe("Crush Clock",Part.Roll,Part.Crush,Part.Grip,Part.Wheel,Part.Narrow),
            new Recipe("Bounce Board",Part.Right,Part.Bounce,Part.Roll,Part.Hammer,Part.Grip),
            new Recipe("Conveyor Curve",Part.Left,Part.Conveyor,Part.Right,Part.Wall,Part.Shuttle),
            new Recipe("Vertical Motion",Part.Wheel,Part.Hill,Part.Forward,Part.Grip,Part.Pendulum),
            new Recipe("Fragile Bridge",Part.Narrow,Part.Falling,Part.Right,Part.Lift,Part.Grip),
            new Recipe("First Freefall",Part.Hill,Part.Drop,Part.Grip,Part.Slalom,Part.Wall),
            new Recipe("Midterm Circuit",Part.Hammer,Part.Jump,Part.Shuttle,Part.Door,Part.Grip,Part.Split),
            new Recipe("Crane Rush",Part.Turbo,Part.Pendulum,Part.Right,Part.Door,Part.Narrow,Part.Jump),
            new Recipe("Ice Gauntlet",Part.Ice,Part.Wall,Part.Grip,Part.Hammer,Part.Narrow,Part.Left),
            new Recipe("Long Leap",Part.Hill,Part.Jump,Part.Turbo,Part.Jump,Part.Grip,Part.Push),
            new Recipe("Switchback Surge",Part.Turbo,Part.Left,Part.Right,Part.Zigzag,Part.Pendulum,Part.Grip),
            new Recipe("Falling Chicane",Part.Falling,Part.Left,Part.Wall,Part.Falling,Part.Right,Part.Grip),
            new Recipe("Sky Elevator",Part.Lift,Part.Narrow,Part.Hammer,Part.Forward,Part.Hill,Part.Jump),
            new Recipe("Twin Rolling",Part.Roller,Part.Split,Part.Wall,Part.Roller,Part.Narrow,Part.Grip),
            new Recipe("Double Door",Part.Door,Part.Left,Part.Wheel,Part.Right,Part.Door,Part.Turbo),
            new Recipe("Hammer Tips",Part.Pendulum,Part.Narrow,Part.Hammer,Part.Grip,Part.Narrow,Part.Shuttle),
            new Recipe("Dune Drop",Part.Turbo,Part.Hill,Part.Drop,Part.Grip,Part.Roll,Part.Crush),
            new Recipe("Neon Crossfire",Part.Door,Part.Wheel,Part.Push,Part.Zigzag,Part.Blade,Part.Grip),
            new Recipe("Spiral Overlook",Part.Spiral,Part.Hammer,Part.Grip,Part.Circle,Part.Right,Part.Jump),
            new Recipe("Lane Splitter",Part.Split,Part.Hill,Part.Roller,Part.Split,Part.Wall,Part.Narrow),
            new Recipe("Glacier Shuttle",Part.Ice,Part.Shuttle,Part.Grip,Part.Wheel,Part.Ice,Part.Forward),
            new Recipe("Hanging Gauntlet",Part.Pendulum,Part.Right,Part.Pendulum,Part.Left,Part.Hammer,Part.Pendulum),
            new Recipe("Catch And Climb",Part.Drop,Part.Grip,Part.Hill,Part.Lift,Part.Door,Part.Narrow),
            new Recipe("Crush Alley",Part.Crush,Part.Narrow,Part.Push,Part.Right,Part.Crush,Part.Wheel),
            new Recipe("Speed Weave",Part.Turbo,Part.Zigzag,Part.Blade,Part.Grip,Part.Turbo,Part.Split),
            new Recipe("Skyline Steps",Part.Bounce,Part.Falling,Part.Grip,Part.Jump,Part.Hill,Part.Forward),
            new Recipe("Grand Tour",Part.Hammer,Part.Lift,Part.Ice,Part.Door,Part.Drop,Part.Grip,Part.Split),
            new Recipe("Skyline Mastery",Part.Split,Part.Wheel,Part.Roller,Part.Narrow,Part.Pendulum,Part.Jump,Part.Drop),
            new Recipe("Fragile Spiral",Part.Falling,Part.Spiral,Part.Grip,Part.Door,Part.Circle,Part.Falling,Part.Right),
            new Recipe("Precision Relay",Part.Narrow,Part.Push,Part.Shuttle,Part.Blade,Part.Narrow,Part.Grip,Part.Jump),
            new Recipe("Bonus Coin Rush",Part.Turbo,Part.Split,Part.Hill,Part.Jump,Part.Grip,Part.Drop,Part.Roll),
            new Recipe("Summit Finale",Part.Hill,Part.Spiral,Part.Wheel,Part.Lift,Part.Falling,Part.Grip,Part.Pendulum,Part.Drop)
        };
        sealed class Route
        {
            public readonly List<TrackPiece> tracks=new List<TrackPiece>();
            public readonly List<ObstacleSpec> obstacles=new List<ObstacleSpec>();
            public readonly List<PickupSpec> pickups=new List<PickupSpec>();
            public readonly List<Vector3> points=new List<Vector3>();
            public readonly List<float> widths=new List<float>();
            public readonly List<Vector3> checkpoints=new List<Vector3>();
            public readonly List<float> drops=new List<float>();
            public readonly List<Vector3> dropPoints=new List<Vector3>();
            public readonly List<Vector3> dropDirections=new List<Vector3>();
            public readonly List<RaceRouteOption> alternatives=new List<RaceRouteOption>();
            public Vector3 last,forward=Vector3.forward;
            public Vector3 Right=>Vector3.Cross(Vector3.up,forward).normalized;
            public Route(Vector3 start){last=start;points.Add(start);}
            static TrackSurface Surface(TrackStyle style)=>style==TrackStyle.Ice?TrackSurface.Ice:style==TrackStyle.HighGrip?TrackSurface.HighGrip:style==TrackStyle.Slippery?TrackSurface.Slippery:style==TrackStyle.Bouncy?TrackSurface.Bouncy:style==TrackStyle.Moving?TrackSurface.Moving:TrackSurface.Standard;
            public void To(Vector3 target,float width,TrackStyle style=TrackStyle.Deck,bool rails=true,bool curve=false,float bank=0,bool optional=false)
            {
                Vector3 a=last;int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,target)/4));
                for(int i=1;i<=steps;i++)
                {
                    float t=(float)i/steps,v=(float)(i-1)/steps,u=curve?Mathf.SmoothStep(0,1,t):t,w=curve?Mathf.SmoothStep(0,1,v):v;
                    Vector3 delta=target-a;
                    Vector3 p=curve?a+forward*(Vector3.Dot(delta,forward)*v)+Right*(Vector3.Dot(delta,Right)*w)+Vector3.up*(delta.y*w):Vector3.Lerp(a,target,v);
                    Vector3 q=curve?a+forward*(Vector3.Dot(delta,forward)*t)+Right*(Vector3.Dot(delta,Right)*u)+Vector3.up*(delta.y*u):Vector3.Lerp(a,target,t),d=q-p;
                    if(d.sqrMagnitude<.00001f)continue;
                    var rotation=Quaternion.LookRotation(d,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);
                    tracks.Add(new TrackPiece{position=(p+q)*.5f-rotation*Vector3.up*.4f,size=new Vector3(width,.8f,d.magnitude+.08f),rotation=rotation.eulerAngles,rails=rails,style=style,surface=Surface(style),alternate=optional});
                    if(!optional){points.Add(q);widths.Add(width);}
                    if(i%2==0||steps<3)pickups.Add(new PickupSpec{kind=PickupKind.Coin,position=(p+q)*.5f+Vector3.up*.85f});
                }
                last=target;
            }
            public void Local(float sideways,float rise,float length,float width,TrackStyle style=TrackStyle.Deck,bool rails=true,bool curve=false,float bank=0)
                =>To(last+Right*sideways+Vector3.up*rise+forward*length,width,style,rails,curve,bank);
            public void Gap(float length,float drop,float width)
            {last+=forward*length-Vector3.up*drop;points.Add(last);widths.Add(width);}
            public void Checkpoint(float width)
            {Local(0,0,7,width,TrackStyle.HighGrip);checkpoints.Add(last-forward*2+Vector3.up*.65f);}
            public void Hazard(ObstacleKind kind,Vector3 point,float width,float amplitude=1.8f,float speed=.7f,float phase=0,Vector3? axis=null,Vector3? size=null)
            {
                Vector3 dimensions=size??new Vector3(width,kind==ObstacleKind.Pendulum?1:kind==ObstacleKind.StaticBlock?1.5f:2,1);
                float y=kind==ObstacleKind.Pendulum?3.5f:kind==ObstacleKind.Hammer?.75f:kind==ObstacleKind.VerticalRotor||kind==ObstacleKind.RotatingBlade?1.7f:dimensions.y*.5f;
                obstacles.Add(new ObstacleSpec{kind=kind,position=point+Vector3.up*y,size=dimensions,axis=axis??(kind==ObstacleKind.Pendulum||kind==ObstacleKind.VerticalRotor?Vector3.forward:Right),amplitude=amplitude,speed=speed,phase=phase,rotation=Quaternion.LookRotation(forward,Vector3.up).eulerAngles,warningSeconds=1.45f});
            }
            public void Pad(Vector3 point,float width,float strength=2.2f)
            {obstacles.Add(new ObstacleSpec{kind=ObstacleKind.Booster,position=point+Vector3.up*.13f,size=new Vector3(Mathf.Min(width-1,7),.25f,5),axis=forward,amplitude=strength,speed=1,rotation=Quaternion.LookRotation(forward,Vector3.up).eulerAngles});}
            public void Platform(ObstacleKind kind,float width,float span,float amplitude,Vector3 axis)
            {
                Vector3 start=last;float gap=6;Local(0,0,5,width);
                start=last;Gap(gap,0,width);Vector3 center=(start+last)*.5f-Vector3.up*.35f;
                obstacles.Add(new ObstacleSpec{kind=kind,position=center,size=new Vector3(width,.7f,span),axis=axis,amplitude=amplitude,speed=.65f,phase=0,rotation=Quaternion.LookRotation(forward).eulerAngles,warningSeconds=1.45f});
                Local(0,0,11,width,TrackStyle.HighGrip);
            }
            public void Spiral(float width)
            {
                Vector3 start=last,initial=forward,right=Right;const float radius=12,arc=210;int count=24;
                for(int i=1;i<=count;i++)
                {
                    float t=i/(float)count,a=t*arc*Mathf.Deg2Rad;
                    Vector3 p=start+right*(radius*(1-Mathf.Cos(a)))+initial*(radius*Mathf.Sin(a))+Vector3.up*(t*12);
                    To(p,width,TrackStyle.Banked,true,false,-5);
                }
                forward=(Quaternion.AngleAxis(arc,Vector3.up)*initial).normalized;
            }
            public void Split(float width)
            {
                Vector3 entry=last,start=last,heading=forward,right=Right;int entryIndex=points.Count-1;
                Local(-7,1,21,width,TrackStyle.Bridge,true,true);Local(7,-1,24,width,TrackStyle.Deck,true,true);Vector3 exit=last;int exitIndex=points.Count-1;
                int before=points.Count;Vector3 held=last;
                last=start;To(start+right*4+heading*20,Mathf.Max(4.8f,width*.55f),TrackStyle.HighGrip,true,true,0,true);To(exit,Mathf.Max(4.8f,width*.55f),TrackStyle.Bridge,true,true,0,true);
                last=held;pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=start+right*4+heading*20+Vector3.up*.9f});
                alternatives.Add(new RaceRouteOption{id="Narrow inside line",entryRouteIndex=entryIndex,exitRouteIndex=exitIndex,points=new[]{start,start+right*4+heading*20,exit},minimumSpeed=5,risky=true});
            }
        }
        static int Difficulty(int id)=>id<=5?0:id<=15?1:id<=30?2:id<=50?3:4;
        static void BuildPart(Route r,Part part,int id,int difficulty,float width,int ordinal)
        {
            float side=id%2==0?-1:1,bend=difficulty<1?3:difficulty<3?5:7;
            Vector3 start=r.last;
            switch(part)
            {
                case Part.Roll:r.Local(0,0,24,width);break;
                case Part.Left:r.Local(-bend,0,28,width,TrackStyle.Deck,true,true);break;
                case Part.Right:r.Local(bend,0,28,width,TrackStyle.Deck,true,true);break;
                case Part.Zigzag:r.Local(bend,1,22,width,TrackStyle.Banked,true,true,-4);r.Local(-bend*1.7f,-1,27,width,TrackStyle.Banked,true,true,4);break;
                case Part.Hill:r.Local(0,difficulty<2?2:3.5f,20,width,TrackStyle.Ramp);r.Local(0,difficulty<2?-2:-3.5f,21,width,TrackStyle.Ramp);break;
                case Part.Grip:r.Local(0,0,23,width,TrackStyle.HighGrip);break;
                case Part.Ice:r.Local(0,0,29,width,TrackStyle.Ice);break;
                case Part.Slalom:
                    r.Local(0,0,31,width);
                    r.Hazard(ObstacleKind.StaticBlock,start+r.forward*10+r.Right*(width*.22f),1.1f,0,0);
                    r.Hazard(ObstacleKind.StaticBlock,start+r.forward*22-r.Right*(width*.22f),1.1f,0,0);break;
                case Part.Narrow:r.Local(0,0,8,width);r.Local(0,0,25,Mathf.Max(5.5f,width-2.2f),TrackStyle.Bridge);r.Local(0,0,8,width);break;
                case Part.Hammer:r.Local(0,0,33,width);r.Hazard(ObstacleKind.Hammer,start+r.forward*17,3,1.2f,.65f+difficulty*.06f,id*.31f);break;
                case Part.Pendulum:r.Local(0,0,34,width);r.Hazard(ObstacleKind.Pendulum,start+r.forward*18,3,1.15f,.65f+difficulty*.045f,id*.19f);break;
                case Part.Wall:r.Local(0,0,32,width);r.Hazard(ObstacleKind.SlidingWall,start+r.forward*16,1.2f,Mathf.Min(2.4f,width*.24f),.65f,id*.17f);break;
                case Part.Wheel:r.Local(0,0,34,width);r.Hazard(ObstacleKind.VerticalRotor,start+r.forward*18,3.2f,1,.6f,id*.3f,Vector3.forward);break;
                case Part.Blade:r.Local(0,0,34,width);r.Hazard(ObstacleKind.RotatingBlade,start+r.forward*18,3.3f,1,.7f,id*.13f,Vector3.up);break;
                case Part.Turbo:r.Local(0,0,42,Mathf.Max(width,9));r.Pad(start+r.forward*12,width);break;
                case Part.Jump:
                    r.Local(0,1.4f,12,width,TrackStyle.Ramp);r.Local(0,0,5,width);r.Gap(difficulty>=3?3.3f:2,1.8f,width);r.Local(0,0,29,Mathf.Max(width,10),TrackStyle.HighGrip);break;
                case Part.Falling:r.Platform(ObstacleKind.FallingTile,width,8.2f,0,r.Right);r.Platform(ObstacleKind.CollapsingBridge,width,8.2f,0,r.Right);break;
                case Part.Lift:r.Platform(ObstacleKind.Elevator,width,12,.28f,Vector3.up);break;
                case Part.Shuttle:r.Platform(ObstacleKind.MovingBridge,width,12,1.2f,r.Right);break;
                case Part.Forward:r.Platform(ObstacleKind.ForwardPlatform,width,12,1,r.forward);break;
                case Part.Circle:r.Platform(ObstacleKind.CircularPlatform,width,12,1.2f,r.Right);break;
                case Part.Door:r.Local(0,0,40,width,TrackStyle.HighGrip);r.Hazard(ObstacleKind.TimedDoor,start+r.forward*22,width+.8f,0,.8f,id%3,Vector3.up,new Vector3(width+.8f,2.5f,.7f));break;
                case Part.Roller:r.Local(0,0,37,width);r.Hazard(ObstacleKind.RollingHazard,start+r.forward*20,1.5f,width*.25f,.65f,id*.11f,r.Right,new Vector3(1.5f,1.5f,1.5f));break;
                case Part.Push:r.Local(0,0,34,width);r.Hazard(ObstacleKind.PushBlock,start+r.forward*18-r.Right*(width*.3f),1.4f,1.6f,.8f,id*.21f,r.Right);break;
                case Part.Crush:
                    r.Local(0,0,40,width,TrackStyle.HighGrip);
                    r.Hazard(ObstacleKind.CrushingWall,start+r.forward*22-r.Right*(width*.4f),1.5f,1.2f,.7f,0,r.Right);
                    r.Hazard(ObstacleKind.CrushingWall,start+r.forward*22+r.Right*(width*.4f),1.5f,1.2f,.7f,Mathf.PI,r.Right);break;
                case Part.Conveyor:
                    r.Local(bend*.5f,0,35,width,TrackStyle.Deck,true,true);
                    r.obstacles.Add(new ObstacleSpec{kind=ObstacleKind.Conveyor,position=start+r.forward*16+Vector3.up*.1f,size=new Vector3(width-1,.18f,7),axis=r.forward,amplitude=1.1f,rotation=Quaternion.LookRotation(r.forward).eulerAngles});break;
                case Part.Bounce:r.Local(0,0,8,width);r.Gap(0,1.3f,width);r.Local(0,0,30,Mathf.Max(width,10),TrackStyle.Bouncy);r.Local(0,0,8,width,TrackStyle.HighGrip);break;
                case Part.Drop:
                    r.Local(0,0,14,width,TrackStyle.Deck,true);r.Local(0,0,7,width,TrackStyle.Bridge,false);r.drops.Add(r.last.z);r.dropPoints.Add(r.last);r.dropDirections.Add(r.forward);r.Gap(0,difficulty>=3?10:7,width);r.Local(0,0,57,Mathf.Max(12,width),TrackStyle.HighGrip);break;
                case Part.Spiral:r.Spiral(Mathf.Max(width,7.8f));break;
                case Part.Split:r.Split(width);break;
            }
            if(ordinal%2==1)r.Checkpoint(width);
        }
        static void PhysicsAssets()
        {
            string folder=Root+"/Resources/TrackPhysics";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            float[] dynamic={.55f,.16f,.95f,.85f,.55f,.025f};float[] statics={.6f,.18f,1,.9f,.6f,.035f};
            for(int i=0;i<6;i++)
            {
                var surface=(TrackSurface)i;string path=folder+"/"+surface+".physicMaterial";var material=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
                if(!material){material=new PhysicsMaterial(surface+" contact");AssetDatabase.CreateAsset(material,path);}
                material.dynamicFriction=dynamic[i];material.staticFriction=statics[i];material.bounciness=surface==TrackSurface.Bouncy?.42f:0;material.frictionCombine=surface==TrackSurface.Ice||surface==TrackSurface.Slippery?PhysicsMaterialCombine.Minimum:PhysicsMaterialCombine.Average;material.bounceCombine=PhysicsMaterialCombine.Maximum;EditorUtility.SetDirty(material);
            }
        }
        static void Apply(LevelDefinition level,Route route,int id,string title,int world,int difficulty,string mission)
        {
            route.Local(0,0,22,13,TrackStyle.HighGrip);
            level.id=id;level.title=title;level.world=world;level.difficulty=difficulty;level.mission=mission;
            level.hint=id==1?"Repeat swipes to build speed. Hold still to coast.":mission;
            level.spawn=route.points[0]+Vector3.forward*4+Vector3.up*.62f;level.finish=route.last-route.forward*6+Vector3.up*.7f;
            level.tracks=route.tracks.ToArray();level.obstacles=route.obstacles.ToArray();level.pickups=route.pickups.ToArray();level.checkpoints=route.checkpoints.ToArray();
            level.routePoints=route.points.ToArray();level.routeWidths=route.widths.Concat(new[]{13f}).ToArray();level.dropStarts=route.drops.ToArray();
            level.dropPoints=route.dropPoints.ToArray();level.dropDirections=route.dropDirections.ToArray();
            level.killHeight=route.tracks.Min(t=>t.position.y)-11;level.parSeconds=65+route.points.Count*.6f;level.allowJump=false;
            EditorUtility.SetDirty(level);
        }
        public static void Generate(GameCatalog catalog)
        {
            PhysicsAssets();if(catalog.contentVersion>=21)return;
            var levels=new List<LevelDefinition>();
            for(int i=0;i<Recipes.Length;i++)
            {
                int id=i+1,difficulty=Difficulty(id);float width=difficulty==0?11:difficulty==1?9.5f:difficulty==2?9:8.5f;
                string path=Root+"/ScriptableObjects/Levels/Level"+id.ToString("000")+".asset";var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                int world=level&&id<=12?level.world:Mathf.Clamp((id-13)/11,0,3);
                if(!level){level=ScriptableObject.CreateInstance<LevelDefinition>();AssetDatabase.CreateAsset(level,path);}
                var recipe=Recipes[i];var route=new Route(new Vector3(0,34+(id%3)*2,0));route.Local(0,0,16,width);
                for(int part=0;part<recipe.parts.Length;part++)BuildPart(route,recipe.parts[part],id,difficulty,width,part);
                if(route.obstacles.Count==0)route.Hazard(ObstacleKind.StaticBlock,route.points[4]+route.Right*(width*.3f),1,0,0);
                if(!route.pickups.Any(p=>p.kind==PickupKind.Gem))route.pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=route.last-route.forward*3+route.Right*2.2f+Vector3.up*.9f});
                route.pickups.Add(new PickupSpec{kind=PickupKind.Token,position=route.last-route.forward*6-route.Right*2.2f+Vector3.up*.9f});
                string mission=string.Join(" / ",recipe.parts.Select(p=>p.ToString()).Distinct().Take(4));
                Apply(level,route,id,recipe.title,world,difficulty,mission);level.mechanics=recipe.parts.Select(p=>p.ToString()).Distinct().ToArray();levels.Add(level);
            }
            catalog.levels=levels.ToArray();GenerateRaces(catalog);
            foreach(var profile in catalog.balls)
            {
                profile.maxSpeed=14;profile.acceleration=18;profile.braking=14;profile.airControl=.2f;profile.friction=.55f;profile.bounce=profile.ability==BallAbility.Rubber?.16f:.025f;
                if(profile.ability==BallAbility.Magnetic||profile.ability==BallAbility.Energy)profile.unlockLevel=121;
                EditorUtility.SetDirty(profile);
            }
            catalog.contentVersion=21;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("Production campaign authored: 55 curated adventure recipes and 5 dedicated race tracks, persistent asset GUIDs retained.");
        }
        static void GenerateRaces(GameCatalog catalog)
        {
            string folder=Root+"/ScriptableObjects/Races";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            string[] titles={"Harbor Five","Hammer Docks","Shuttle Speedway","Canyon Choice","Alpine Precision"};
            Part[][] parts={new[]{Part.Roll,Part.Right,Part.Hill,Part.Turbo,Part.Left},new[]{Part.Roll,Part.Hammer,Part.Pendulum,Part.Wall,Part.Turbo},new[]{Part.Left,Part.Shuttle,Part.Hill,Part.Forward,Part.Right},new[]{Part.Roll,Part.Split,Part.Wall,Part.Spiral,Part.Grip},new[]{Part.Narrow,Part.Wheel,Part.Lift,Part.Jump,Part.Turbo}};
            var races=new List<RaceTrackDefinition>();
            for(int i=0;i<5;i++)
            {
                string coursePath=folder+"/RaceCourse"+(i+1).ToString("00")+".asset";var course=AssetDatabase.LoadAssetAtPath<LevelDefinition>(coursePath);
                if(!course){course=ScriptableObject.CreateInstance<LevelDefinition>();AssetDatabase.CreateAsset(course,coursePath);}
                var route=new Route(new Vector3(0,38,0));route.Local(0,0,25,18);
                for(int part=0;part<parts[i].Length;part++)BuildPart(route,parts[i][part],i+1,2,14,part);
                Apply(course,route,1001+i,titles[i],i==3?1:i==4?2:0,2,"Race the line, read the machinery, and reach the finish.");course.mechanics=parts[i].Select(p=>p.ToString()).ToArray();
                string path=folder+"/Race"+(i+1).ToString("00")+".asset";var race=AssetDatabase.LoadAssetAtPath<RaceTrackDefinition>(path);
                if(!race){race=ScriptableObject.CreateInstance<RaceTrackDefinition>();AssetDatabase.CreateAsset(race,path);}
                race.id=i+1;race.title=titles[i];race.course=course;race.route=course.routePoints;race.aiCount=4;race.timeTarget=80+i*7;race.unlockLevel=i+1;race.format=i==0?RaceFormat.Sprint:RaceFormat.Obstacle;
                race.alternatives=route.alternatives.ToArray();
                EditorUtility.SetDirty(race);races.Add(race);
            }
            catalog.races=races.ToArray();
        }
    }
}

