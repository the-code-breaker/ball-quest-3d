using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    /// <summary>Fifty-five spatially authored courses. This replaces the old forward-only part recipes.</summary>
    public static class CrazyCampaignGenerator
    {
        const string Root="Assets/_BallQuest";
        public const int Version=24;
        static readonly string[] Families={"Sky Ribbon","Knife Edge","Orbit Tower","Switchback","Figure Eight","Island Descent","Clockwork","Catwalk","Sky Labyrinth","Velocity Canyon","Finale"};
        static readonly string[][] Titles={
            new[]{"Ribbon Reef","Saffron Serpentine","Cloud Bow","Crescent Chain","Ribbon Tempest"},
            new[]{"Needle Walk","Broken Compass","Razor Crown","Glass Needle","Thread The Storm"},
            new[]{"Orbit Ascent","Double Helix","Saturn Stair","Corkscrew Escape","Orbit Collapse"},
            new[]{"Alpine Hairpins","Switchback Citadel","Reverse Summit","Terrace Teeth","Skyhook Traverse"},
            new[]{"Infinity Bridge","Crossed Horizons","Butterfly Overpass","Triple Eight","Infinity Furnace"},
            new[]{"Stepping Skies","Falling Archipelago","Six Island Descent","Broken Waterfall","Meteor Island Relay"},
            new[]{"Clockwork Crossing","Pendulum Court","Doorbell Foundry","Hammer Carousel","Clockwork Catastrophe"},
            new[]{"Crane Catwalk","Frozen Balcony","Suspension Needle","Rooftop Tightrope","Catwalk Blackout"},
            new[]{"Maze Above","Four Courtyards","Diamond Detour","Citadel Maze","Labyrinth Of Stars"},
            new[]{"Turbo Braking School","Canyon Sling","Comet Chicane","Afterburner Descent","Velocity Vortex"},
            new[]{"Sky Trial","Three Peak Trial","Midnight Gauntlet","Impossible Horizon","Crown Of The Sky"}
        };

        sealed class Route
        {
            public readonly List<TrackPiece> tracks=new List<TrackPiece>();
            public readonly List<ObstacleSpec> obstacles=new List<ObstacleSpec>();
            public readonly List<PickupSpec> pickups=new List<PickupSpec>();
            public readonly List<Vector3> points=new List<Vector3>();
            public readonly List<float> widths=new List<float>();
            public readonly List<Vector3> checkpoints=new List<Vector3>();
            public readonly List<Vector3> drops=new List<Vector3>(),dropDirections=new List<Vector3>();
            public Vector3 last,forward=Vector3.forward;
            public float length;
            public Vector3 Right=>Vector3.Cross(Vector3.up,forward).normalized;
            public Route(Vector3 start){last=start;points.Add(start);}
            static TrackSurface Surface(TrackStyle style)=>style==TrackStyle.HighGrip?TrackSurface.HighGrip:style==TrackStyle.Ice?TrackSurface.Ice:style==TrackStyle.Slippery?TrackSurface.Slippery:TrackSurface.Standard;
            public void To(Vector3 target,float width=4.4f,bool rails=true,TrackStyle style=TrackStyle.Deck,bool optional=false)
            {
                Vector3 start=last,delta=target-start;if(delta.sqrMagnitude<.001f)return;
                int count=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/3));
                for(int i=0;i<count;i++)
                {
                    Vector3 a=Vector3.Lerp(start,target,i/(float)count),b=Vector3.Lerp(start,target,(i+1f)/count),direction=b-a;
                    var rotation=Quaternion.LookRotation(direction,Vector3.up);
                    tracks.Add(new TrackPiece{position=(a+b)*.5f-rotation*Vector3.up*.4f,size=new Vector3(width,.8f,direction.magnitude+.08f),rotation=rotation.eulerAngles,style=style,surface=Surface(style),rails=rails,alternate=optional});
                    if(!optional){points.Add(b);widths.Add(width);length+=direction.magnitude;}
                    if(i%3==1&&width>=2.2f)pickups.Add(new PickupSpec{kind=PickupKind.Coin,position=(a+b)*.5f+Vector3.up*.85f});
                }
                last=target;Vector3 planar=Vector3.ProjectOnPlane(delta,Vector3.up);if(planar.sqrMagnitude>.001f)forward=planar.normalized;
            }
            public void Run(float distance,float width=4.4f,float rise=0,bool rails=true,TrackStyle style=TrackStyle.Deck)
                =>To(last+forward*distance+Vector3.up*rise,width,rails,style);
            public void Turn(float angle,float radius=10,float width=4.4f,float rise=0,bool rails=true,TrackStyle style=TrackStyle.Deck)
            {
                Vector3 start=last,heading=forward,right=Right;float sign=Mathf.Sign(angle),r=Mathf.Abs(radius);
                int count=Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(angle)/7));
                for(int i=1;i<=count;i++)
                {
                    float t=i/(float)count,a=Mathf.Abs(angle)*Mathf.Deg2Rad*t;
                    To(start+right*(sign*r*(1-Mathf.Cos(a)))+heading*(r*Mathf.Sin(a))+Vector3.up*(rise*t),width,rails,style);
                }
                forward=(Quaternion.AngleAxis(angle,Vector3.up)*heading).normalized;
            }
            public void Bend(float lateral,float length,float width=4.4f,float rise=0,bool rails=true,TrackStyle style=TrackStyle.Deck)
            {
                Vector3 start=last,heading=forward,right=Right;int count=Mathf.Max(6,Mathf.CeilToInt(length/3));
                for(int i=1;i<=count;i++)
                {float t=i/(float)count;To(start+heading*(length*t)+right*(lateral*Mathf.SmoothStep(0,1,t))+Vector3.up*(rise*t),width,rails,style);}
                forward=heading;
            }
            public void Checkpoint()
            {
                Run(7,6,0,true,TrackStyle.HighGrip);checkpoints.Add(last-forward*2+Vector3.up*.65f);
            }
            public void Beam(float distance,float width)
            {
                Run(3,4,0,true,TrackStyle.HighGrip);Run(distance,width,0,false,TrackStyle.Bridge);Run(4,4,0,true,TrackStyle.HighGrip);
            }
            public void Drop(float height,float catchLength=38,float catchWidth=7)
            {
                Run(7,4.6f,0,false,TrackStyle.Bridge);drops.Add(last);dropDirections.Add(forward);
                catchWidth=Mathf.Max(8,catchWidth);last-=Vector3.up*height;points.Add(last);widths.Add(catchWidth);
                Run(catchLength,catchWidth,0,true,TrackStyle.HighGrip);
            }
            public void Boost(float length=40)
            {
                Vector3 start=last;Run(length,5.8f);
                obstacles.Add(new ObstacleSpec{kind=ObstacleKind.Booster,position=start+forward*7+Vector3.up*.12f,size=new Vector3(4.3f,.22f,5),axis=forward,rotation=Quaternion.LookRotation(forward).eulerAngles,amplitude=1.6f,speed=1});
                Run(10,6.3f,0,true,TrackStyle.HighGrip);
            }
            public void Door(float phase=0)
            {
                Vector3 start=last,heading=forward;Run(20,5.2f,0,true,TrackStyle.HighGrip);
                obstacles.Add(new ObstacleSpec{kind=ObstacleKind.TimedDoor,position=start+heading*11+Vector3.up*1.2f,size=new Vector3(5.5f,2.5f,.65f),axis=Vector3.up,rotation=Quaternion.LookRotation(heading).eulerAngles,speed=.85f,phase=phase,amplitude=0});
            }
            public void Hazard(ObstacleKind kind,float side=1,float width=6.4f,float phase=0)
            {
                Vector3 start=last,heading=forward,right=Right;Run(24,width);
                float offset=kind==ObstacleKind.Pendulum?0:kind==ObstacleKind.Hammer?0:width*.25f*side;
                float height=kind==ObstacleKind.Pendulum?3.5f:kind==ObstacleKind.Hammer?.75f:kind==ObstacleKind.RotatingBlade?1.7f:.65f;
                Vector3 size=kind==ObstacleKind.StaticBlock?new Vector3(.9f,1.3f,.9f):kind==ObstacleKind.Hammer?new Vector3(2.5f,1,1):kind==ObstacleKind.Pendulum?new Vector3(2,1,1):new Vector3(1.0f,1.5f,1.0f);
                obstacles.Add(new ObstacleSpec{kind=kind,position=start+heading*12+right*offset+Vector3.up*height,size=size,axis=kind==ObstacleKind.Pendulum?heading:right,rotation=Quaternion.LookRotation(heading).eulerAngles,speed=kind==ObstacleKind.Hammer?.9f:.7f,amplitude=kind==ObstacleKind.StaticBlock?0:kind==ObstacleKind.Pendulum?.9f:.6f,phase=phase,warningSeconds=1.2f});
            }
            public void Fragile(float width=3.2f)
            {
                Run(4,4.4f);Vector3 start=last;last+=forward*6;points.Add(last);widths.Add(width);
                obstacles.Add(new ObstacleSpec{kind=ObstacleKind.FallingTile,position=(start+last)*.5f-Vector3.up*.35f,size=new Vector3(width,.7f,6.7f),axis=Right,speed=.7f,warningSeconds=1.45f,rotation=Quaternion.LookRotation(forward).eulerAngles});
                Run(7,4.5f,0,true,TrackStyle.HighGrip);
            }
            public void Shuttle(float width=3.4f)
            {
                Run(4,4.8f);Vector3 start=last;last+=forward*6;points.Add(last);widths.Add(width);
                obstacles.Add(new ObstacleSpec{kind=ObstacleKind.MovingBridge,position=(start+last)*.5f-Vector3.up*.35f,size=new Vector3(width,.7f,8.6f),axis=Right,speed=.8f,amplitude=.65f,phase=0,rotation=Quaternion.LookRotation(forward).eulerAngles});
                Run(7,4.8f,0,true,TrackStyle.HighGrip);
            }
            public void Fork(float length,float offset,float width=3.6f)
            {
                Vector3 start=last,heading=forward,right=Right;
                Bend(-offset,length*.5f,width,1.2f);Bend(offset,length*.5f,width,-1.2f);
                Vector3 end=last,heldHeading=forward;
                last=start;forward=heading;Vector3 middle=start+heading*(length*.5f)+right*(offset*.65f);
                To(middle,Mathf.Max(1.7f,width*.6f),false,TrackStyle.Bridge,true);To(end,Mathf.Max(1.7f,width*.6f),false,TrackStyle.Bridge,true);
                pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=middle+Vector3.up*.85f});last=end;forward=heldHeading;
            }
            public void Eight(float xRadius,float zRadius,float turns=1,float rise=10,float width=3.8f)
            {
                Vector3 start=last,heading=forward,right=Right;int count=Mathf.CeilToInt(turns*80);
                for(int i=1;i<=count;i++)
                {float t=i/(float)count,a=t*turns*Mathf.PI*2;To(start+right*(xRadius*Mathf.Sin(a))+heading*(zRadius*Mathf.Sin(a*2))+Vector3.up*(rise*t),width,true);}
            }
            public void SideReward(float side)
            {
                Vector3 origin=last,heading=forward,right=Right;Vector3 end=origin+right*side*10;
                To(end,2.1f,false,TrackStyle.Bridge,true);pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=end+Vector3.up*.85f});
                last=origin;forward=heading;
            }
        }

        static float Needle(int variant)=>Mathf.Max(1.65f,2.25f-variant*.15f);
        static void Ribbon(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Bend(12,30,4.8f);r.Bend(-20,34,3.6f);r.Checkpoint();r.Turn(95,12,4);r.Beam(14,2.4f);r.Bend(11,24,3.6f);break;
                case 1:r.Turn(-70,14,3.8f,3);r.Bend(14,25,3.2f);r.Turn(130,11,3.4f);r.Checkpoint();r.Turn(-100,9,3.2f,-2);r.Beam(16,2.15f);break;
                case 2:r.Bend(-18,22,3.6f,3);r.Turn(150,12,3.1f,4);r.Checkpoint();r.Bend(16,30,2.8f);r.Turn(-210,10,3.2f,-5);r.Beam(17,1.95f);break;
                case 3:r.Turn(240,14,3.0f,8);r.Beam(14,1.9f);r.Checkpoint();r.Turn(-155,11,2.8f,-3);r.Bend(-17,30,2.5f);r.Fragile(2.8f);break;
                default:r.Bend(14,22,3.1f);r.Turn(-150,10,2.8f,5);r.Beam(18,1.7f);r.Checkpoint();r.Turn(265,13,2.6f,9);r.Fragile(2.4f);r.Bend(-16,28,2.6f);break;
            }
        }
        static void Knife(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Beam(18,2.25f);r.Turn(75,10,3.8f);r.Checkpoint();r.Beam(18,2.15f);r.Turn(-105,11,3.8f);r.Hazard(ObstacleKind.StaticBlock);break;
                case 1:r.Beam(22,2.1f);r.Turn(-110,10,3);r.Beam(12,2);r.Checkpoint();r.Turn(65,9,3);r.Beam(24,1.95f);r.Turn(85,10,3);break;
                case 2:r.Turn(120,12,3.2f);r.Beam(15,1.9f);r.Turn(-120,9,2.8f);r.Checkpoint();r.Beam(25,1.8f);r.Turn(-90,10,3);r.Beam(15,1.8f);break;
                case 3:r.Beam(18,1.8f);r.Turn(180,9,2.8f,5);r.Beam(22,1.75f);r.Checkpoint();r.Turn(-80,10,2.7f);r.Run(14,2.4f,2,false,TrackStyle.Ice);r.Beam(18,1.7f);break;
                default:r.Beam(28,1.65f);r.Turn(-160,11,2.7f,5);r.Beam(18,1.7f);r.Checkpoint();r.Turn(95,9,2.6f);r.Fragile(2.2f);r.Beam(26,1.65f);r.Turn(145,10,2.8f,4);break;
            }
        }
        static void Orbit(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Turn(300,13,4,9);r.Checkpoint();r.Run(15,3.8f);r.Turn(-130,10,3.6f);r.Beam(12,2.3f);break;
                case 1:r.Turn(-360,14,3.6f,9);r.Run(13,3.5f);r.Checkpoint();r.Turn(250,10,3.1f,7);r.Beam(16,2.1f);break;
                case 2:r.Turn(450,12,3.2f,14);r.Checkpoint();r.Turn(-100,9,3.2f);r.Fragile(2.8f);r.Turn(-180,12,2.9f,-5);break;
                case 3:r.Turn(-270,16,3,8);r.Beam(16,1.9f);r.Checkpoint();r.Turn(-340,10,2.8f,10);r.Run(15,2.4f,0,false,TrackStyle.Ice);break;
                default:r.Turn(540,13,2.8f,18);r.Checkpoint();r.Turn(-210,10,2.8f,-5);r.Beam(20,1.7f);r.Turn(200,11,2.6f,6);r.Fragile(2.4f);break;
            }
        }
        static void Switchbacks(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Run(22,4,3);r.Turn(180,10,4,3);r.Run(26,3.8f,3);r.Checkpoint();r.Turn(-180,10,3.6f,3);r.Beam(15,2.3f);break;
                case 1:r.Run(32,3.8f,5);r.Turn(-165,11,3.6f,3);r.Run(18,3.2f,2);r.Turn(130,10,3.2f);r.Checkpoint();r.Run(23,2.8f,3);r.Turn(-155,10,3);r.Beam(16,2.1f);break;
                case 2:r.Run(18,3.6f,3);r.Turn(180,9,3.2f,3);r.Run(24,3,3);r.Turn(-180,12,3,3);r.Checkpoint();r.Run(15,2.7f,2);r.Turn(170,9,2.8f,3);r.Fragile(2.8f);break;
                case 3:r.Run(27,3.4f,4);r.Turn(-180,10,3,4);r.Beam(18,1.85f);r.Checkpoint();r.Turn(-180,13,2.9f,5);r.Run(32,2.7f,4);r.Turn(120,9,2.7f);r.Beam(12,1.8f);break;
                default:r.Run(18,3.2f,3);r.Turn(170,9,2.8f,3);r.Run(35,2.8f,4);r.Turn(-145,11,2.8f,4);r.Checkpoint();r.Beam(20,1.7f);r.Turn(210,10,2.7f,5);r.Fragile(2.5f);r.Run(21,2.8f,3);break;
            }
        }
        static void Figure(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Eight(14,14,1,12,4);r.Checkpoint();r.Turn(80,10,3.6f);r.Beam(12,2.3f);break;
                case 1:r.Eight(-19,12,1,13,3.6f);r.Run(15,3.4f);r.Checkpoint();r.Turn(-150,10,3.2f);r.Fragile(3);break;
                case 2:r.Eight(12,19,1.25f,17,3.2f);r.Checkpoint();r.Beam(18,1.95f);r.Turn(100,11,3);break;
                case 3:r.Eight(-18,13,1.5f,22,2.9f);r.Checkpoint();r.Turn(-220,10,2.8f,7);r.Beam(17,1.8f);break;
                default:r.Eight(21,17,1.75f,28,2.8f);r.Checkpoint();r.Turn(135,11,2.7f);r.Fragile(2.4f);r.Beam(20,1.7f);break;
            }
        }
        static void Islands(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Beam(12,2.3f);r.Turn(-65,10,4);r.Checkpoint();r.Drop(4.8f,31,7);r.Turn(110,12,3.6f);r.Beam(12,2.3f);break;
                case 1:r.Turn(95,11,3.5f);r.Drop(6,34,7.2f);r.Checkpoint();r.Turn(-135,10,3.2f);r.Beam(19,2.05f);r.Drop(4.5f,31,7);break;
                case 2:r.Beam(15,1.95f);r.Turn(-120,10,3.2f);r.Drop(6,34,7);r.Turn(90,11,3);r.Checkpoint();r.Fragile(2.8f);r.Drop(5.2f,34,7);r.Turn(90,10,3);break;
                case 3:r.Turn(180,11,3.1f,4);r.Beam(18,1.8f);r.Drop(7,36,7.5f);r.Checkpoint();r.Turn(-150,12,2.8f);r.Fragile(2.7f);r.Drop(6,34,7);break;
                default:r.Beam(16,1.7f);r.Drop(7.5f,38,7.5f);r.Turn(145,11,2.8f);r.Checkpoint();r.Beam(22,1.65f);r.Drop(5,32,7);r.Turn(-210,12,2.8f,6);r.Fragile(2.5f);break;
            }
        }
        static void Clockwork(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Door();r.Turn(90,11,4);r.Hazard(ObstacleKind.Hammer,1,7);r.Checkpoint();r.Turn(-120,11,3.6f);r.Beam(15,2.2f);break;
                case 1:r.Hazard(ObstacleKind.Pendulum,1,7.2f);r.Turn(-135,12,3.4f);r.Checkpoint();r.Door(2);r.Shuttle(3.5f);r.Turn(90,10,3.4f);r.Hazard(ObstacleKind.SlidingWall,-1,5.8f);r.Beam(15,2);break;
                case 2:r.Door(3);r.Turn(135,13,3.2f,5);r.Hazard(ObstacleKind.Hammer,-1,7);r.Checkpoint();r.Turn(-130,10,3);r.Door(1);r.Fragile(2.8f);break;
                case 3:r.Hazard(ObstacleKind.Hammer,1,7);r.Turn(-230,12,3,6);r.Checkpoint();r.Door(4);r.Shuttle(3);r.Beam(17,1.8f);r.Hazard(ObstacleKind.Pendulum,1,7.2f);r.Turn(110,10,2.9f);break;
                default:r.Door(2.8f);r.Turn(160,10,2.9f,4);r.Hazard(ObstacleKind.Pendulum,1,7.2f);r.Checkpoint();r.Beam(20,1.7f);r.Turn(-120,11,2.8f);r.Hazard(ObstacleKind.Hammer,-1,7);r.Door(4.5f);r.Fragile(2.5f);break;
            }
        }
        static void Catwalk(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Beam(24,2.2f);r.Turn(110,12,3.6f,4);r.Checkpoint();r.SideReward(-1);r.Beam(20,2.1f);r.Turn(-70,10,3.6f);break;
                case 1:r.Run(16,3.5f,3,true,TrackStyle.Ice);r.Turn(-90,11,3.4f);r.Beam(20,2);r.Checkpoint();r.Turn(155,10,3.1f,5);r.Beam(17,1.95f);break;
                case 2:r.Beam(28,1.9f);r.Turn(150,12,3.1f,6);r.Fragile(2.8f);r.Checkpoint();r.Turn(-110,9,2.9f);r.Beam(23,1.8f);r.SideReward(1);break;
                case 3:r.Turn(-180,12,3,7);r.Beam(24,1.75f);r.Checkpoint();r.Turn(70,9,2.7f);r.Run(18,2.6f,2,false,TrackStyle.Ice);r.Turn(110,10,2.8f);r.Beam(22,1.7f);break;
                default:r.Beam(32,1.65f);r.Turn(130,11,2.8f,6);r.Fragile(2.5f);r.Checkpoint();r.Turn(-230,12,2.8f,7);r.Beam(27,1.65f);r.SideReward(-1);break;
            }
        }
        static void Maze(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Fork(38,10,3.7f);r.Turn(-90,11,4);r.Checkpoint();r.Run(18,3.8f);r.Turn(120,10,3.6f);r.Fork(30,8,3.5f);break;
                case 1:r.Turn(90,10,3.4f);r.Run(18,3.2f);r.Turn(-90,10,3.3f);r.Fork(35,10,3.3f);r.Checkpoint();r.Turn(-160,11,3);r.SideReward(1);r.Beam(17,2);break;
                case 2:r.Fork(40,12,3.1f);r.Turn(135,11,3);r.Beam(14,1.95f);r.Checkpoint();r.Turn(-90,9,2.9f);r.Fork(32,9,2.9f);r.Turn(80,10,3);break;
                case 3:r.Turn(-120,10,3);r.Fork(32,10,2.9f);r.Turn(180,12,2.9f,5);r.Checkpoint();r.Door(2);r.Fork(40,13,2.8f);r.Beam(18,1.8f);break;
                default:r.Fork(44,14,2.9f);r.Turn(-145,10,2.8f,4);r.Checkpoint();r.Fork(36,11,2.7f);r.Turn(215,11,2.8f,6);r.Beam(22,1.7f);r.Door(3.5f);break;
            }
        }
        static void Velocity(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Boost(40);r.Turn(120,14,4);r.Checkpoint();r.Beam(17,2.2f);r.Turn(-80,12,3.8f);r.Run(22,3.8f,-3);break;
                case 1:r.Run(24,4.4f,-3);r.Boost(42);r.Turn(-160,15,3.6f);r.Checkpoint();r.Fragile(3);r.Turn(85,11,3.3f);r.Beam(18,2);break;
                case 2:r.Boost(44);r.Turn(145,13,3.3f);r.Checkpoint();r.Bend(-15,28,3);r.Turn(-140,12,3);r.Drop(5,35,7);r.Beam(14,1.9f);break;
                case 3:r.Run(25,3.9f,-4);r.Turn(-80,12,3.2f);r.Boost(46);r.Turn(210,14,3,8);r.Checkpoint();r.Beam(22,1.8f);r.Fragile(2.6f);break;
                default:r.Boost(48);r.Turn(-235,14,2.9f,10);r.Checkpoint();r.Beam(18,1.7f);r.Run(23,3.4f,-3);r.Boost(40);r.Turn(155,12,2.9f);r.Fragile(2.5f);break;
            }
        }
        static void Finale(Route r,int v)
        {
            switch(v)
            {
                case 0:r.Turn(135,12,3.8f,4);r.Beam(16,2.2f);r.Checkpoint();r.Door(1.2f);r.Turn(-180,11,3.5f,4);r.Fork(28,8,3.4f);break;
                case 1:r.Run(20,3.6f,4);r.Turn(-180,11,3.2f,4);r.Beam(20,1.95f);r.Checkpoint();r.Drop(5.5f,34,7);r.Turn(110,10,3);r.Door(2);break;
                case 2:r.Turn(270,13,3,9);r.Fragile(2.8f);r.Checkpoint();r.Hazard(ObstacleKind.Pendulum,1,7.2f);r.Turn(-155,10,2.9f);r.Shuttle(3);r.Beam(23,1.8f);r.Fork(35,9,2.8f);break;
                case 3:r.Eight(16,14,1,14,2.9f);r.Beam(24,1.7f);r.Checkpoint();r.Door(3);r.Turn(-180,11,2.8f,5);r.Drop(6.5f,36,7);r.Fragile(2.5f);break;
                default:r.Turn(-300,13,2.8f,10);r.Beam(24,1.65f);r.Checkpoint();r.Hazard(ObstacleKind.Hammer,1,7);r.Turn(180,11,2.8f,5);r.Fragile(2.4f);r.Drop(7.5f,38,7.5f);r.Checkpoint();r.Turn(-135,10,2.7f);r.Beam(28,1.65f);r.Door(4);break;
            }
        }

        [MenuItem("Tools/Ball Quest/Rebuild Distinct Campaign v9")]
        public static void Rebuild()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>(Root+"/ScriptableObjects/GameCatalog.asset");
            if(!catalog)throw new InvalidOperationException("Game catalog missing.");Generate(catalog,true);
        }
        public static void Generate(GameCatalog catalog,bool force=false)
        {
            if(!force&&catalog.contentVersion>=Version)return;
            var levels=new List<LevelDefinition>(55);
            for(int id=1;id<=55;id++)
            {
                int family=(id-1)%11,variant=(id-1)/11;var route=new Route(new Vector3(0,52+variant*2,0));
                route.Run(10,6.2f); // safe entry; the authored footprint begins immediately after it
                switch(family)
                {
                    case 0:Ribbon(route,variant);break;case 1:Knife(route,variant);break;case 2:Orbit(route,variant);break;
                    case 3:Switchbacks(route,variant);break;case 4:Figure(route,variant);break;case 5:Islands(route,variant);break;
                    case 6:Clockwork(route,variant);break;case 7:Catwalk(route,variant);break;case 8:Maze(route,variant);break;
                    case 9:Velocity(route,variant);break;default:Finale(route,variant);break;
                }
                route.Run(12,5.8f,0,true,TrackStyle.HighGrip);
                IncreaseChallenge(route,id,variant);
                string path=Root+"/ScriptableObjects/Levels/Level"+id.ToString("000")+".asset";
                var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if(!level){level=ScriptableObject.CreateInstance<LevelDefinition>();AssetDatabase.CreateAsset(level,path);}
                level.id=id;level.title=Titles[family][variant];level.world=(family+variant)%4;level.expedition=false;
                level.difficulty=id==1?1:id<=4?2:id<23?3:4;
                level.signatureMechanic=Families[family];
                level.mission=family==1||family==7?"Balance on exposed beams. Slow down before each turn.":family==5?"Control each drop and land on the next island.":family==6?"Watch the moving machinery and pass the timed openings.":family==9?"Ride the accelerator, then brake before the bend.":"Read the skyline, follow every checkpoint, and master the changing route.";
                level.hint=id==1?"Repeat swipes to build speed. Brake before the narrow turn.":level.mission;
                level.spawn=route.points[0]+Vector3.forward*3+Vector3.up*.62f;level.finish=route.last-route.forward*4+Vector3.up*.7f;
                level.tracks=route.tracks.ToArray();level.obstacles=route.obstacles.ToArray();level.checkpoints=route.checkpoints.ToArray();
                route.pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=route.last-route.forward*7+route.Right*1.25f+Vector3.up*.9f});
                route.pickups.Add(new PickupSpec{kind=PickupKind.Token,position=route.last-route.forward*5-route.Right*1.25f+Vector3.up*.9f});
                level.pickups=route.pickups.ToArray();level.routePoints=route.points.ToArray();level.routeWidths=route.widths.Concat(new[]{5.8f}).ToArray();
                level.dropPoints=route.drops.ToArray();level.dropDirections=route.dropDirections.ToArray();level.dropStarts=route.drops.Select(p=>p.z).ToArray();
                level.alternateFinishes=Array.Empty<Vector3>();level.mechanics=new[]{Families[family],"Precision",route.drops.Count>0?"Controlled drop":route.obstacles.Any(o=>o.kind==ObstacleKind.TimedDoor)?"Timed gate":"Elevated route"};
                level.killHeight=route.tracks.Min(t=>t.position.y)-12;level.allowJump=false;level.parSeconds=Mathf.Max(65,route.length/4.8f+25);
                EditorUtility.SetDirty(level);levels.Add(level);
            }
            catalog.levels=levels.ToArray();catalog.contentVersion=Version;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();WriteAtlas(levels);
            Debug.Log("Crazy campaign rebuilt: 55 authored spatial layouts across 11 topology families, precision beams, overpasses, drops and machinery; existing level IDs/GUIDs retained.");
        }

        static void WriteAtlas(List<LevelDefinition> levels)
        {
            Directory.CreateDirectory("Documentation");var svg=new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' width='1320' height='770' viewBox='0 0 1320 770'><rect width='1320' height='770' fill='#102336'/>");
            for(int index=0;index<levels.Count;index++)
            {
                var level=levels[index];float minX=level.routePoints.Min(p=>p.x),maxX=level.routePoints.Max(p=>p.x),minZ=level.routePoints.Min(p=>p.z),maxZ=level.routePoints.Max(p=>p.z);
                float scale=Mathf.Min(98/Mathf.Max(1,maxX-minX),105/Mathf.Max(1,maxZ-minZ));float cellX=index%11*120,cellY=index/11*154;
                svg.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"<g transform='translate({0},{1})'><rect x='4' y='4' width='112' height='146' rx='7' fill='#193a54'/><text x='10' y='20' fill='#fff' font-size='11'>{2}</text><polyline fill='none' stroke='#f6c56d' stroke-width='2.4' points='",cellX,cellY,level.id+" "+level.signatureMechanic);
                foreach(var p in level.routePoints)svg.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0:0.0},{1:0.0} ",10+(p.x-minX)*scale,31+(maxZ-p.z)*scale);
                var first=level.routePoints[0];var last=level.routePoints[level.routePoints.Length-1];
                svg.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"'/><circle cx='{0:0.0}' cy='{1:0.0}' r='2.5' fill='#7de4bd'/><circle cx='{2:0.0}' cy='{3:0.0}' r='2.5' fill='#ff7676'/></g>",10+(first.x-minX)*scale,31+(maxZ-first.z)*scale,10+(last.x-minX)*scale,31+(maxZ-last.z)*scale);
            }
            svg.Append("</svg>");File.WriteAllText("Documentation/LEVEL_ATLAS_V10.svg",svg.ToString());
        }
        static void IncreaseChallenge(Route route,int id,int variant)
        {
            // Retain safe entries, wide drop catches, checkpoint pads and the original
            // footprint. Precision grows through narrower exposed runs and less railing.
            for(int i=5;i<route.tracks.Count-5;i++)
            {
                var piece=route.tracks[i];
                if(piece.alternate||piece.size.x>=5.5f||route.checkpoints.Exists(p=>Vector3.Distance(p,piece.position)<7)||route.drops.Exists(p=>Vector3.Distance(p,piece.position)<12))continue;
                if(!piece.rails)
                {
                    float factor=id<5?.96f:id<12?.92f:id<23?.88f:.84f;
                    piece.size.x=Mathf.Max(1.65f,piece.size.x*factor);
                    float closest=float.PositiveInfinity;int segment=-1;
                    for(int p=0;p<route.points.Count-1;p++){float d=Vector3.Distance((route.points[p]+route.points[p+1])*.5f,piece.position+Quaternion.Euler(piece.rotation)*Vector3.up*.4f);if(d<closest){closest=d;segment=p;}}
                    if(segment>=0&&segment<route.widths.Count&&closest<.5f)route.widths[segment]=piece.size.x;
                }
                else if(id>=3&&(i+id)%19<Mathf.Min(7,2+variant)&&piece.size.x>=3)
                    piece.rails=false;
            }
            if(id==1)return;
            int added=0,target=id<12?1:id<34?2:3;
            for(int i=8;i<route.tracks.Count-2&&added<target;i++)
            {
                var piece=route.tracks[i];if(piece.alternate||piece.size.x<4.4f||piece.size.z<2.2f||Mathf.Abs(Mathf.DeltaAngle(piece.rotation.x,0))>2)continue;
                Vector3 top=piece.position+Vector3.up*piece.size.y*.5f;
                if(route.obstacles.Exists(o=>Vector3.Distance(o.position,top)<13)||route.checkpoints.Exists(p=>Vector3.Distance(p,top)<6)||route.drops.Exists(p=>Vector3.Distance(p,top)<14))continue;
                // Reject locations beside a returning section or another height layer.
                bool clear=true;for(int j=0;j<route.tracks.Count;j++)if(Mathf.Abs(j-i)>7&&Vector3.Distance(route.tracks[j].position,top)<6){clear=false;break;}
                if(!clear)continue;
                Quaternion q=Quaternion.Euler(0,piece.rotation.y,0);Vector3 right=q*Vector3.right;
                var kind=(ObstacleKind)((int)ObstacleKind.Piston+(id+added)%3);
                route.obstacles.Add(new ObstacleSpec{kind=kind,position=top+right*(piece.size.x*.5f-.72f)*(added%2==0?1:-1)+Vector3.up*.55f,
                    rotation=new Vector3(0,piece.rotation.y,0),size=new Vector3(.85f,1.1f,.85f),axis=right,amplitude=.22f+variant*.055f,speed=.8f+variant*.16f,phase=id*.7f});added++;
            }
        }
    }
}
