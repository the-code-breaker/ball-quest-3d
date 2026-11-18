using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    /// <summary>Authored route profiles: twelve distinct forward-moving courses across four visual worlds.</summary>
    public static class ArcadeCampaignGenerator
    {
        sealed class Route
        {
            public readonly List<TrackPiece> tracks=new List<TrackPiece>();
            public readonly List<ObstacleSpec> obstacles=new List<ObstacleSpec>();
            public readonly List<PickupSpec> pickups=new List<PickupSpec>();
            public Vector3 last;
            public Route(Vector3 start) {last=start;}
            public void To(Vector3 target,float width,TrackStyle style=TrackStyle.Deck,bool rails=true,bool curve=false)
            {
                Vector3 a=last;
                int steps=Mathf.Max(1,Mathf.CeilToInt((target.z-a.z)/5));
                for(int i=1;i<=steps;i++)
                {
                    float t=(float)i/steps,previous=(float)(i-1)/steps;
                    float u=curve?Mathf.SmoothStep(0,1,t):t,v=curve?Mathf.SmoothStep(0,1,previous):previous;
                    Vector3 p=new Vector3(Mathf.Lerp(a.x,target.x,v),Mathf.Lerp(a.y,target.y,v),Mathf.Lerp(a.z,target.z,previous));
                    Vector3 q=new Vector3(Mathf.Lerp(a.x,target.x,u),Mathf.Lerp(a.y,target.y,u),Mathf.Lerp(a.z,target.z,t));
                    Vector3 d=q-p;
                    var rotation=Quaternion.LookRotation(d,Vector3.up);
                    tracks.Add(new TrackPiece{position=(p+q)*.5f-rotation*Vector3.up*.4f,size=new Vector3(width,.8f,d.magnitude+.08f),rotation=rotation.eulerAngles,rails=rails,style=style});
                    if(i%2==0||steps<5)pickups.Add(new PickupSpec{kind=PickupKind.Coin,position=(p+q)*.5f+Vector3.up*.9f});
                }
                last=target;
            }
            public Vector3 At(float z)
            {
                foreach(var piece in tracks)
                {
                    var rotation=Quaternion.Euler(piece.rotation);
                    Vector3 half=rotation*Vector3.forward*piece.size.z*.5f;
                    Vector3 a=piece.position+rotation*Vector3.up*.4f-half,b=piece.position+rotation*Vector3.up*.4f+half;
                    if(z<a.z-.05f||z>b.z+.05f)continue;
                    return Vector3.Lerp(a,b,Mathf.InverseLerp(a.z,b.z,z));
                }
                return last;
            }
            public void Challenge(ObstacleKind kind,float z,float width,float amplitude,float phase=0)
            {
                Vector3 point=At(z);
                float height=kind==ObstacleKind.Pendulum?3.5f:kind==ObstacleKind.Hammer?.8f:1;
                obstacles.Add(new ObstacleSpec{kind=kind,position=point+Vector3.up*height,size=new Vector3(width,kind==ObstacleKind.SlidingWall?2:1,1.05f),
                    axis=kind==ObstacleKind.Pendulum?Vector3.forward:Vector3.right,amplitude=amplitude,speed=.75f+phase*.045f,phase=phase});
            }
            public void Turbo(float z)
            {
                obstacles.Add(new ObstacleSpec{kind=ObstacleKind.Booster,position=At(z)+Vector3.up*.13f,size=new Vector3(5.5f,.25f,5.4f),axis=Vector3.forward,amplitude=2.2f,speed=1});
            }
        }

        static readonly string[] Titles={"Harbor Launch","Crane Alley","Skywheel Sprint","Canyon Courier","Temple Switchback","Dune Dive","Glacier Glide","Frostline Bridge","Polar Plunge","Neon Overdrive","Signal Slalom","Final Ascent"};
        static readonly string[] Missions={"Learn the flick. Catch both lower paths.","Thread the moving gates above the harbor.","Follow the rings and hold your landing line.","Ride the canyon ridge and collect the gold.","Read the turns before the temple descent.","Build speed for the longest desert drop.","Glide across the ice without oversteering.","Balance along the narrow suspended bridge.","Commit to two deep falls over the frozen bay.","Chain turbo runways through the neon skyline.","Read the crossing gates and keep your rhythm.","Climb the summit, then master the final descent."};

        public static void Generate(GameCatalog catalog)
        {
            if(catalog.contentVersion>=10)return;
            var levels=new List<LevelDefinition>();
            for(int i=0;i<12;i++)
            {
                int world=i/3;float mirror=i%2==0?1:-1;
                float[] bends={4,9,11,7,13,5,10,12,6,12,15,16};
                float[] heights={3,5,4,6,8,5,4,6,7,5,7,9};
                float[] launchZ={94,112,120,108,128,105,116,122,130,112,125,138};
                float[] dropA={8,10,11,10,12,15,10,11,15,10,12,14};
                float[] dropB={10,11,13,11,12,14,12,13,15,12,14,15};
                float baseHeight=42+(i%3)*5,high=baseHeight+heights[i],low=high-dropA[i];
                float a=bends[i]*mirror,b=-bends[i]*.7f*mirror;
                float drop1=launchZ[i],catch1=drop1+66;
                float midLength=88+(i%3)*14,drop2=catch1+midLength+24;
                float bottom=low+4-dropB[i],catch2=drop2+68;
                var route=new Route(new Vector3(0,baseHeight,0));
                route.To(new Vector3(0,baseHeight,27),9.5f);
                route.To(new Vector3(a,high,drop1-34),i<3?9:8,TrackStyle.Ramp,true,true);
                route.To(new Vector3(a,high,drop1-8),i==7?6.5f:8.1f,world==2?TrackStyle.Ice:TrackStyle.Deck);
                route.To(new Vector3(a,high,drop1),7,TrackStyle.Deck,false);
                route.last=new Vector3(a,low,drop1-2);
                route.To(new Vector3(a,low,catch1),12);
                float middle=catch1+midLength*.5f;
                if(i%3==0)
                {
                    route.To(new Vector3(b,low+2,middle),7.6f,TrackStyle.Bridge,true,true);
                    route.To(new Vector3(b*.65f,low+4,catch1+midLength),7.3f,TrackStyle.Ramp,true,true);
                }
                else if(i%3==1)
                {
                    route.To(new Vector3(-a*.35f,low+4,middle-5),7.1f,TrackStyle.Ramp,true,true);
                    route.To(new Vector3(b,low+4,catch1+midLength),6.8f,TrackStyle.Bridge,true,true);
                }
                else
                {
                    route.To(new Vector3(a*.35f,low+1,middle),7.7f,world==2?TrackStyle.Ice:TrackStyle.Bridge,true,true);
                    route.To(new Vector3(b,low+4,catch1+midLength),6.5f,TrackStyle.Ramp,true,true);
                }
                float launchX=route.last.x;
                route.To(new Vector3(launchX,low+4,drop2-8),7.3f);
                route.To(new Vector3(launchX,low+4,drop2),6.6f,TrackStyle.Bridge,false);
                route.last=new Vector3(launchX,bottom,drop2-2);
                route.To(new Vector3(launchX,bottom,catch2),12);
                route.To(new Vector3(0,bottom,catch2+35),8.5f,TrackStyle.Deck,true,true);
                route.To(new Vector3(0,bottom,catch2+57),11);
                route.Turbo(drop1-24);route.Turbo(drop2-18);
                route.Challenge(ObstacleKind.SlidingWall,Mathf.Min(drop1-48,59),1.25f,2.7f,i*.42f);
                route.Challenge(i%2==0?ObstacleKind.Pendulum:ObstacleKind.Hammer,catch1+24,3,1.3f,i*.6f);
                route.Challenge(ObstacleKind.SlidingWall,catch2+23,1.3f,2.5f,i*.53f);
                if(i>2)route.Challenge(ObstacleKind.Pendulum,drop1-43,3,1.4f,i*.74f);
                if(i>7)route.Challenge(ObstacleKind.Hammer,middle+17,3.2f,1.2f,i*.63f);
                route.pickups.Add(new PickupSpec{kind=PickupKind.Gem,position=route.At(drop1+47)+new Vector3(2.8f,1,0)});
                route.pickups.Add(new PickupSpec{kind=PickupKind.Token,position=route.At(drop2+47)+new Vector3(-2.8f,1,0)});
                string path=ProjectGenerator.Root+"/ScriptableObjects/Levels/Level"+(i+1).ToString("000")+".asset";
                var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if(!level){level=ScriptableObject.CreateInstance<LevelDefinition>();AssetDatabase.CreateAsset(level,path);}
                level.id=i+1;level.title=Titles[i];level.world=world;level.mission=Missions[i];
                level.hint=i==0?"Flick up to roll. Faster flicks build more speed. Swipe down to slow.":Missions[i];
                level.spawn=new Vector3(0,baseHeight+.62f,4);
                level.finish=new Vector3(0,bottom+.72f,catch2+48);
                level.checkpoints=new[]{route.At(drop1-6)+Vector3.up*.65f,route.At(catch1-6)+Vector3.up*.65f,route.At(drop2-7)+Vector3.up*.65f,route.At(catch2-6)+Vector3.up*.65f};
                level.tracks=route.tracks.ToArray();level.obstacles=route.obstacles.ToArray();level.pickups=route.pickups.ToArray();
                level.parSeconds=70+(i%3)*8;level.killHeight=bottom-12;level.allowJump=false;level.dropStarts=new[]{drop1,drop2};
                EditorUtility.SetDirty(level);levels.Add(level);
            }
            catalog.levels=levels.ToArray();catalog.contentVersion=10;EditorUtility.SetDirty(catalog);
            foreach(var profile in catalog.balls)
            {
                profile.maxSpeed=14;profile.acceleration=18;profile.friction=.55f;profile.bounce=profile.ability==BallAbility.Rubber?.18f:.025f;profile.airControl=.2f;
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Arcade campaign authored: twelve distinct routes, four worlds, two long catch-platform drops per course.");
        }
    }
}
