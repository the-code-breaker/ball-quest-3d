using System;
using UnityEngine;

namespace BallQuest
{
    /// <summary>Family-specific native landmarks, all fed into the existing spatial mesh batches.</summary>
    public static class CrazyCoursePresentation
    {
        static readonly string[] Families={"Sky Ribbon","Knife Edge","Orbit Tower","Switchback","Figure Eight","Island Descent","Clockwork","Catwalk","Sky Labyrinth","Velocity Canyon","Finale"};
        public static bool Applies(LevelDefinition level)=>level&&!level.expedition&&Array.IndexOf(Families,level.signatureMechanic)>=0;
        public static void Build(PremiumTrackPresentation kit,LevelDefinition level,Material rock,Material darkRock,Material pearl,Material metal,Material gold,Material glow,Material foliage,Material lightFoliage,Material wood)
        {
            var bounds=new Bounds(level.spawn,Vector3.one);foreach(var track in level.tracks)bounds.Encapsulate(track.position);
            int family=Array.IndexOf(Families,level.signatureMechanic);
            float baseY=level.killHeight-6;
            // Distant silhouette placement follows the actual course bounds, including reverse
            // routes. No scenery collider or anonymous repeat of the old three giant loops.
            for(int i=0;i<8;i++)
            {
                int side=i%2==0?-1:1;float z=Mathf.Lerp(bounds.min.z-10,bounds.max.z+20,(i/2f)/3.5f);
                Vector3 p=new Vector3(bounds.center.x+side*(bounds.extents.x+18+(i%3)*5),baseY,z);
                float height=25+(i*7+level.id*3)%22;
                BiomeLandmark(kit,family,p,height,i+level.id,rock,darkRock,pearl,metal,gold,glow,foliage,lightFoliage,wood);
            }
            for(int i=6;i<level.tracks.Length;i+=Mathf.Max(12,level.tracks.Length/7))
            {
                var t=level.tracks[i];Vector3 side=Quaternion.Euler(t.rotation)*Vector3.right*(i%2==0?1:-1);
                float margin=family==2||family==7||family==9?22:16;
                Vector3 near=t.position+side*(t.size.x*.5f+margin)+Vector3.down*7;
                if(Clear(level,near,margin*.55f))BiomeLandmark(kit,family,near-Vector3.up*10,13,level.id*37+i,rock,darkRock,pearl,metal,gold,glow,foliage,lightFoliage,wood);
            }
            // Each suspended section gets an appropriate visual identity without narrowing
            // physical clearance. Reject a support whenever another road lies beneath it.
            for(int i=2;i<level.tracks.Length;i+=Mathf.Max(5,level.tracks.Length/16))
            {
                var track=level.tracks[i];Quaternion q=Quaternion.Euler(track.rotation);Vector3 top=track.position+q*Vector3.up*track.size.y*.5f;
                if(!ClearBelow(level,top,i))continue;
                if(family==5)
                {
                    kit.Terrace(top+Vector3.down*2.2f,Mathf.Max(2.5f,track.size.x*.7f),4.5f,.25f,darkRock,level.id*47+i,9);
                    kit.Terrace(top+Vector3.down*.85f,Mathf.Max(2.3f,track.size.x*.65f),.65f,.8f,rock,level.id*13+i,9);
                }
                else if(family==7||family==6)
                {
                    Vector3 side=q*Vector3.right*(track.size.x*.45f),foot=top+Vector3.down*7;
                    kit.Tube(top+side+Vector3.down*.7f,foot,.16f,metal,8);kit.Tube(top-side+Vector3.down*.7f,foot,.16f,metal,8);
                    kit.Tube(foot,foot+Vector3.down*9,.3f,gold,8);
                }
                else if(family==2||family==4||family==8)
                {
                    kit.Terrace(top+Vector3.down*7,1.5f,12,.8f,pearl,i+level.id,10);
                    kit.Terrace(top+Vector3.down*1.1f,Mathf.Max(2.1f,track.size.x*.65f),.5f,1,metal,i,10);
                }
            }
        }
        static void BiomeLandmark(PremiumTrackPresentation k,int family,Vector3 p,float h,int seed,Material rock,Material dark,Material pearl,Material metal,Material gold,Material glow,Material leaves,Material lightLeaves,Material wood)
        {
            Vector3 top=p+Vector3.up*h;
            switch(family)
            {
                case 0: // Lagoon terraces, broad canopies and waterfalls.
                    k.FloatingIsland(top-Vector3.up*6,5.5f,leaves,rock,dark,wood,leaves,lightLeaves,seed);
                    for(int j=0;j<3;j++)k.Tree(top+new Vector3((j-1)*2.4f,-5,1),1.1f,wood,leaves,lightLeaves,seed+j);
                    k.Box(top+new Vector3(3,-13,0),new Vector3(.8f,14,.19f),glow);break;
                case 1: // Sculpted desert hoodoos and freestanding arches.
                    for(int j=0;j<3;j++){var a=p+new Vector3((j-1)*4,h*(.5f+j*.08f),j*2);k.Terrace(a,3.6f-j*.5f,h*(.55f+j*.1f),.35f,rock,seed+j,7);k.Terrace(a+Vector3.up*2,4-j*.5f,1.1f,1,dark,seed+j,7);}
                    if(seed%2==0)Arch(k,top+Vector3.up*2,11,10,rock,gold);break;
                case 2: // Orbital observatory: concentric sky rings and satellites.
                    k.Terrace(p+Vector3.up*h*.4f,2,h*.7f,.65f,metal,seed,12);
                    k.Ellipsoid(top,new Vector3(5,5,5),pearl,18,10);
                    for(int ring=0;ring<2;ring++)for(int n=0;n<40;n++)
                    {float a=n*Mathf.PI/20,b=(n+1)*Mathf.PI/20;var q=Quaternion.Euler(ring==0?25:70,seed%90,0);k.Tube(top+q*new Vector3(Mathf.Cos(a)*8,0,Mathf.Sin(a)*8),top+q*new Vector3(Mathf.Cos(b)*8,0,Mathf.Sin(b)*8),.16f,ring==0?gold:glow,6);}
                    k.Ellipsoid(top+new Vector3(8,4,0),Vector3.one*1.25f,gold,12,7);break;
                case 3: // Alpine peaks and snow-loaded conifers.
                    k.Terrace(top,.10f,h,70,dark,seed,8);k.Terrace(top+Vector3.up*2,.05f,h*.27f,65,pearl,seed,8);
                    for(int j=0;j<3;j++)k.Tree(top+new Vector3((j-1)*3,-h*.35f,3),1.3f,wood,leaves,pearl,seed+j,true);break;
                case 4: // Hanging gardens with oversized flowering branches.
                    k.FloatingIsland(top-Vector3.up*8,6,lightLeaves,rock,dark,wood,leaves,lightLeaves,seed);
                    k.Tube(top-Vector3.up*7,top+Vector3.up*2,.55f,wood,10);
                    for(int j=0;j<5;j++){float a=j*Mathf.PI*.4f;Vector3 blossom=top+new Vector3(Mathf.Cos(a)*3,2+Mathf.Sin(a),Mathf.Sin(a)*3);k.Tube(top-Vector3.up*2,blossom,.22f,wood,7);k.Ellipsoid(blossom,new Vector3(2.5f,1.5f,2.2f),j%2==0?pearl:lightLeaves,12,7);}
                    break;
                case 5: // Basalt volcanoes, molten fissures, separate smoke lobes.
                    k.Terrace(top,2,h,4,dark,seed,9);k.Terrace(top+Vector3.up*.3f,2.1f,.3f,.8f,glow,seed,10);
                    for(int j=0;j<4;j++){k.Ellipsoid(top+new Vector3(j*.8f,3+j*2,0),Vector3.one*(1+j*.45f),rock,10,6);k.Tube(p+new Vector3(-2+j,2,0),top+new Vector3(.3f*j,0,1),.12f,glow,6);}break;
                case 6: // Exposed brass gears and industrial pipework.
                    Gear(k,top-Vector3.up*5,6,metal,gold,Quaternion.Euler(0,seed%60,0));
                    Gear(k,top+new Vector3(8,-1,1),3.4f,metal,pearl,Quaternion.identity);
                    for(int s=-1;s<=1;s+=2){k.Tube(p+Vector3.right*s*5,p+Vector3.right*s*5+Vector3.up*(h+4),.45f,metal,10);k.Tube(top+new Vector3(s*5,4,0),top+new Vector3(0,4,0),.45f,gold,10);}break;
                case 7: // Glass skyline with neon edges and crane booms.
                    k.BeveledBox(p+Vector3.up*h*.5f,new Vector3(7,h,8),.7f,metal,pearl);
                    for(int j=1;j<6;j++)k.Box(p+new Vector3(0,h*j/6,0),new Vector3(7.15f,.3f,8.15f),glow);
                    k.Tube(top,top+Vector3.up*8,.22f,gold,8);k.Tube(top+Vector3.up*7,top+new Vector3(12,7,0),.18f,gold,8);k.Tube(top+new Vector3(11,7,0),top+new Vector3(11,-2,0),.045f,metal,6);break;
                case 8: // Ancient courts with arches and broken columns.
                    Arch(k,top-Vector3.up*8,12,14,rock,pearl);
                    for(int j=0;j<3;j++){Vector3 column=top+new Vector3((j-1)*5,-11,3);k.Terrace(column,1.3f,9,1,pearl,seed,10);k.Terrace(column+Vector3.up*.5f,1.8f,.6f,1,gold,seed,10);}
                    k.Tube(top+new Vector3(-5,0,0),top+new Vector3(-5,-9,0),.28f,leaves,8);break;
                case 9: // Broad canyon mesas, strata and wind turbines.
                    k.Terrace(top,6,h,.4f,rock,seed,7);for(int j=0;j<3;j++)k.Terrace(top-Vector3.up*(3+j*4),6.4f,.65f,1,dark,seed,7);
                    k.Tube(top,top+Vector3.up*10,.18f,pearl,8);
                    for(int j=0;j<3;j++){float a=j*Mathf.PI*2/3;k.Tube(top+Vector3.up*10,top+Vector3.up*10+new Vector3(Mathf.Cos(a)*4,Mathf.Sin(a)*4,0),.17f,pearl,8);}break;
                default: // Crystal storm: faceted floating monoliths and halos.
                    for(int j=0;j<3;j++){Vector3 a=top+new Vector3((j-1)*4,j*2,0);k.Terrace(a,2.2f,7,.02f,glow,seed+j,6);k.Terrace(a,2.2f,-6,.02f,pearl,seed+j,6);}
                    for(int n=0;n<36;n++){float a=n*Mathf.PI/18,b=(n+1)*Mathf.PI/18;k.Tube(top+new Vector3(Mathf.Cos(a)*9,2,Mathf.Sin(a)*9),top+new Vector3(Mathf.Cos(b)*9,2,Mathf.Sin(b)*9),.13f,gold,6);}break;
            }
        }
        static bool Clear(LevelDefinition level,Vector3 point,float radius)
        {
            foreach(var piece in level.tracks)
            {
                Vector3 local=Quaternion.Inverse(Quaternion.Euler(piece.rotation))*(point-piece.position);
                if(Mathf.Abs(local.y)<12&&Mathf.Abs(local.x)<piece.size.x*.5f+radius+2&&Mathf.Abs(local.z)<piece.size.z*.5f+radius+2)return false;
            }
            return true;
        }
        static bool ClearBelow(LevelDefinition level,Vector3 top,int own)
        {
            for(int i=0;i<level.tracks.Length;i++)
            {
                if(i==own)continue;var piece=level.tracks[i];Vector3 local=Quaternion.Inverse(Quaternion.Euler(piece.rotation))*(top-piece.position);
                if(local.y>1.5f&&local.y<19&&Mathf.Abs(local.x)<piece.size.x*.5f+3&&Mathf.Abs(local.z)<piece.size.z*.5f+3)return false;
            }
            return true;
        }
        static void Tower(PremiumTrackPresentation kit,Vector3 foot,float radius,float height,Material wall,Material edge,Material accent,bool crown)
        {
            kit.Terrace(foot+Vector3.up*(height*.5f),radius,height,.92f,wall,5,12);
            for(int tier=1;tier<=4;tier++)kit.Terrace(foot+Vector3.up*(height*tier/4),radius+(.4f+tier*.05f),.45f,1,edge,11,12);
            if(crown)for(int tooth=0;tooth<8;tooth++)
            {
                float a=tooth*Mathf.PI/4;Vector3 p=foot+Vector3.up*(height+1)+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius*.83f;
                kit.BeveledBox(p,new Vector3(1.1f,2,1.1f),.18f,edge,accent);
            }
        }
        static void Gear(PremiumTrackPresentation kit,Vector3 center,float radius,Material body,Material trim,Quaternion rotation)
        {
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Vector3 p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),q=new Vector3(Mathf.Cos(b),Mathf.Sin(b),0);
                kit.Tube(center+rotation*p*radius,center+rotation*q*radius,.38f,body,8);
                if(i%2==0){kit.Box(center+rotation*p*(radius+.45f),new Vector3(1.2f,1.1f,.9f),trim,rotation*Quaternion.Euler(0,0,a*Mathf.Rad2Deg));kit.Tube(center,center+rotation*p*(radius-.2f),.18f,trim,8);}
            }
            kit.Tube(center-rotation*Vector3.forward,center+rotation*Vector3.forward,.7f,trim,12);
        }
        static void Arch(PremiumTrackPresentation kit,Vector3 center,float width,float height,Material stone,Material trim)
        {
            for(int side=-1;side<=1;side+=2)kit.BeveledBox(center+new Vector3(side*width*.5f,0,0),new Vector3(1.5f,height,2.2f),.35f,stone,trim);
            kit.BeveledBox(center+Vector3.up*height*.45f,new Vector3(width+1.8f,2,2.5f),.4f,stone,trim);
        }
    }
}
