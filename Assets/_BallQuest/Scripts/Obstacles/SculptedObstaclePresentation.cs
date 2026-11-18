using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    // The same local mesh batches move with each physical obstacle. Decorative
    // bolts and bevels never add colliders or per-frame behaviours.
    public static class SculptedObstaclePresentation
    {
        public static void RefineMechanism(Transform root,ObstacleSpec spec,List<Mesh> owned,Material metal,Material trim,Material rubber,Material gold,Material glow)
        {
            if(spec.kind!=ObstacleKind.Hammer&&spec.kind!=ObstacleKind.Pendulum&&spec.kind!=ObstacleKind.VerticalRotor&&spec.kind!=ObstacleKind.RotatingBlade)return;
            // Keep the original tested collision shapes, replace only their visible shells.
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            var k=new PremiumTrackPresentation(root,owned,false);
            if(spec.kind==ObstacleKind.Hammer||spec.kind==ObstacleKind.Pendulum)
            {
                bool hanging=spec.kind==ObstacleKind.Pendulum;
                if(hanging)
                {
                    k.Ellipsoid(Vector3.zero,Vector3.one*.3f,gold,16,8);
                    k.Tube(Vector3.down*.15f,Vector3.down*2.7f,.12f,metal,12);
                    Barrel(k,new Vector3(0,-2.75f,0),Vector3.right,1.35f,.625f,metal,trim,rubber,gold);
                }
                else
                {
                    k.BeveledBox(Vector3.zero,new Vector3(spec.size.x,.55f,.65f),.15f,metal,trim);
                    for(int side=-1;side<=1;side+=2)Barrel(k,Vector3.right*(side*spec.size.x*.42f),Vector3.forward,.8f,.525f,metal,trim,rubber,gold);
                    k.Tube(Vector3.down*.4f,Vector3.up*.4f,.25f,gold,16);
                }
            }
            else
            {
                bool vertical=spec.kind==ObstacleKind.VerticalRotor;float length=spec.size.x;
                for(int arm=0;arm<2;arm++)
                {
                    Quaternion q=vertical?Quaternion.Euler(0,0,arm*90):Quaternion.Euler(0,arm*90,0);
                    k.Box(Vector3.zero,new Vector3(length,.30f,.40f),metal,q);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Vector3 tip=q*Vector3.right*side*length*.4f;k.Ellipsoid(tip,new Vector3(.25f,.22f,.25f),rubber,12,7);
                        k.Box(q*Vector3.right*side*length*.25f,new Vector3(length*.16f,.32f,.42f),gold,q);
                    }
                }
                k.Ellipsoid(Vector3.zero,Vector3.one*.42f,trim,16,8);
                k.Ellipsoid(Vector3.back*.39f,Vector3.one*.13f,glow,12,6);
            }
            k.Flush();
        }
        static void Barrel(PremiumTrackPresentation k,Vector3 center,Vector3 axis,float half,float radius,Material metal,Material trim,Material rubber,Material gold)
        {
            k.Tube(center-axis*half,center+axis*half,radius,rubber,24);
            for(int s=-1;s<=1;s+=2)
            {
                k.Tube(center+axis*s*half*.7f,center+axis*s*half*.88f,radius*1.025f,trim,24);
                k.Tube(center+axis*s*half*.91f,center+axis*s*half*.96f,radius*1.02f,gold,24);
                Vector3 capRadii=Vector3.one*radius*.98f;if(Mathf.Abs(axis.x)>.5f)capRadii.x=.10f;else capRadii.z=.10f;
                k.Ellipsoid(center+axis*s*half,capRadii,metal,16,8);
                k.Ellipsoid(center+axis*s*(half+.08f),Vector3.one*.10f,gold,10,6);
            }
        }
        public static bool Build(Transform root,ObstacleSpec spec,List<Mesh> owned,Material metal,Material trim,Material rubber,Material gold,Material glow,Material wood)
        {
            var kind=spec.kind;
            if(kind==ObstacleKind.Booster||kind==ObstacleKind.JumpPad||kind==ObstacleKind.Conveyor||kind==ObstacleKind.Pendulum||kind==ObstacleKind.Hammer||kind==ObstacleKind.VerticalRotor||kind==ObstacleKind.RotatingBlade||kind==ObstacleKind.RollingHazard||kind==ObstacleKind.MovingBridge||kind==ObstacleKind.FallingTile||kind==ObstacleKind.Elevator||kind==ObstacleKind.ForwardPlatform||kind==ObstacleKind.CircularPlatform||kind==ObstacleKind.CollapsingBridge)return false;
            var kit=new PremiumTrackPresentation(root,owned,false);Vector3 size=spec.size;
            root.gameObject.AddComponent<BoxCollider>().size=size;
            if(kind==ObstacleKind.Piston)
            {
                kit.BeveledBox(Vector3.zero,size*.94f,.12f,metal,trim);
                kit.Tube(Vector3.left*size.x*.52f,Vector3.right*size.x*.52f,size.z*.39f,rubber,20);
                for(int s=-1;s<=1;s+=2)
                {
                    kit.Tube(Vector3.right*(s*size.x*.44f),Vector3.right*(s*size.x*.52f),size.z*.45f,gold,16);
                    kit.Ellipsoid(Vector3.right*(s*size.x*.535f),new Vector3(.025f,size.y*.18f,size.z*.18f),trim,12,6);
                }
            }
            else if(kind==ObstacleKind.RollerDrum)
            {
                kit.Tube(Vector3.left*size.x*.45f,Vector3.right*size.x*.45f,Mathf.Min(size.y,size.z)*.47f,rubber,24);
                for(int s=-1;s<=1;s+=2)kit.Tube(Vector3.right*(s*size.x*.43f),Vector3.right*(s*size.x*.5f),size.z*.49f,trim,20);
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/4;Vector3 radial=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a))*Mathf.Min(size.y,size.z)*.44f;
                    kit.Tube(radial+Vector3.left*size.x*.34f,radial+Vector3.right*size.x*.34f,.035f,gold,6);
                }
            }
            else if(kind==ObstacleKind.SweepArm)
            {
                kit.BeveledBox(Vector3.zero,size*.92f,.18f,rubber,metal);
                kit.Tube(Vector3.down*size.y*.54f,Vector3.up*size.y*.54f,size.x*.19f,trim,16);
                for(int s=-1;s<=1;s+=2)kit.BeveledBox(Vector3.right*s*size.x*.39f,new Vector3(size.x*.18f,size.y*.8f,size.z*.93f),.07f,gold);
            }
            else if(kind==ObstacleKind.TimedDoor||kind==ObstacleKind.Laser)
            {
                kit.BeveledBox(Vector3.zero,size*.94f,.17f,metal,trim);
                for(int side=-1;side<=1;side+=2)
                {
                    kit.BeveledBox(new Vector3(side*size.x*.45f,0,0),new Vector3(size.x*.07f,size.y*1.02f,size.z*1.05f),.06f,trim);
                    for(int row=0;row<3;row++)kit.BeveledBox(new Vector3(side*size.x*.25f,(row-1)*size.y*.24f,-size.z*.49f),new Vector3(size.x*.28f,.13f,.06f),.025f,glow);
                }
                kit.Tube(Vector3.down*size.y*.48f,Vector3.up*size.y*.48f,.055f,gold,8);
            }
            else
            {
                bool cargo=kind==ObstacleKind.StaticBlock;
                kit.BeveledBox(Vector3.zero,size*.95f,Mathf.Min(.18f,size.x*.14f),cargo?wood:rubber,metal);
                for(int s=-1;s<=1;s+=2)
                {
                    kit.BeveledBox(new Vector3(s*size.x*.34f,0,-size.z*.49f),new Vector3(.12f,size.y*.94f,.06f),.025f,trim);
                    for(int y=-1;y<=1;y+=2)kit.Ellipsoid(new Vector3(s*size.x*.34f,y*size.y*.35f,-size.z*.54f),Vector3.one*.05f,gold,8,5);
                }
                kit.BeveledBox(new Vector3(0,0,-size.z*.49f),new Vector3(size.x*.9f,.16f,.055f),.02f,gold);
            }
            kit.Flush();return true;
        }
    }
}
