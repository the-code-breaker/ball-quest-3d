using UnityEngine;
namespace BallQuest
{
    public static class GrandWorldPresentation
    {
        public static readonly string[] Names={"Lagoon Falls","Sandstone Kingdom","Alpine Bridges","Neon Rooftops","Orbital Observatory","Volcanic Islands","Candy Factory","Bamboo Sanctuary","Lost Temple","Mushroom Forest","Coral Domes","Pirate Harbor","Clockwork Foundry","Autumn Windmills","Crystal Cavern","Cloud Palace","Storm Citadel","Circuit Arena","Toy Workshop","Sunset Canyon"};
        public static readonly string[] ShortNames={"Lagoon","Desert","Alpine","Neon","Orbit","Volcano","Candy","Bamboo","Temple","Mushroom","Coral","Pirate","Clockwork","Autumn","Crystal","Palace","Storm","Circuit","Toy","Canyon"};
        public readonly struct Palette
        {
            public readonly Color sky,horizon,ground,deck,rock,accent,leaf;public readonly int deckKind;
            public Palette(string sky,string horizon,string ground,string deck,string rock,string accent,string leaf,int kind)
            {this.sky=C(sky);this.horizon=C(horizon);this.ground=C(ground);this.deck=C(deck);this.rock=C(rock);this.accent=C(accent);this.leaf=C(leaf);deckKind=kind;}
        }
        static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        static readonly Palette[] palettes={
            new Palette("299DE0","BDEFF5","159BAA","FFF3D7","C5A678","F2CA48","26986E",0),
            new Palette("E6A66B","FFE5B5","BC6847","FFE1A4","CB774B","58CBCE","5C8B63",3),
            new Palette("63A9CF","DFF7FF","487693","F1FDFF","78909E","397EB6","245E61",1),
            new Palette("111B46","6262A5","10172E","4C6B8A","344A66","F86DB3","34B5BB",2),
            new Palette("111332","5C4985","111632","D5DCF4","59628F","A98CFF","56CBE6",1),
            new Palette("68344D","F4AF8B","A93412","9295A7","34333D","FF963E","B25234",3),
            new Palette("8CCDEE","FFE9FA","C5ACD9","FFDDF2","DEA0C9","FF78A6","7EE4BD",1),
            new Palette("539B99","E7EDC3","477C6C","FBE2B4","607D67","E45C47","439970",0),
            new Palette("547D7C","DCE6BA","53694C","E3D5AD","9EAD82","56D1B3","34805A",3),
            new Palette("191E46","626896","151D3C","8DA3D1","3E406A","68DFF5","B06EEB",1),
            new Palette("07516B","54BBC8","076B8B","C7F0F1","77B2BB","F7B3D3","59B9B0",1),
            new Palette("339BCC","D7E9DB","348F9B","FFE8C2","A5A387","DD6750","47977C",0),
            new Palette("49455B","CFB99F","303940","B5C2C5","6A645F","E6B75D","6C9C8C",2),
            new Palette("D9986D","FFEBC8","AF8960","FFDEB0","A88467","E99945","BE6341",0),
            new Palette("153C66","7CC0DD","194E6E","C7F2FF","4F8DAB","66D6F2","7DADF0",1),
            new Palette("5CA5DE","FFF6DC","91BEDB","FFF3D2","B9D3D4","EDCB66","8BC6BA",1),
            new Palette("292B58","AAA1CA","282F58","A5AAC8","4F4F77","C7A3FF","798AE1",3),
            new Palette("061D30","255675","092D43","3E829A","123A50","49EAF0","6FA7F3",2),
            new Palette("77BADE","F0E8C9","ABC4D1","FFE7B3","A2B9BE","F2BE4F","F16F61",0),
            new Palette("D07E6C","FFD3A2","A45C3B","F8D1A4","B86F49","6FC5C4","7E9C6B",3)
        };
        public static Palette Get(int id)=>palettes[Mathf.Clamp(id,0,19)];
        public static void Light(LevelDefinition level,Material sky,Material ground,Camera camera)
        {
            var p=Get(level.environment);if(camera)camera.backgroundColor=p.sky;
            if(sky){sky.SetColor("_Top",p.sky);sky.SetColor("_Horizon",p.horizon);sky.SetColor("_Bottom",p.ground);RenderSettings.skybox=sky;}
            ground.SetColor("_BaseColor",p.ground);ground.SetFloat("_Metallic",.04f);ground.SetFloat("_Smoothness",.35f);
            if(level.environment!=0&&level.environment!=10&&level.environment!=11)ground.SetTexture("_BaseMap",null);
            RenderSettings.fogColor=p.horizon;RenderSettings.fogStartDistance=100;RenderSettings.fogEndDistance=270;
            RenderSettings.ambientSkyColor=Color.Lerp(p.horizon,Color.white,.18f);RenderSettings.ambientEquatorColor=Color.Lerp(p.sky,new Color(.38f,.43f,.5f),.65f);RenderSettings.ambientGroundColor=new Color(.20f,.24f,.29f);
            RenderSettings.ambientLight=Color.Lerp(p.horizon,Color.white,.65f)*.72f;
            foreach(var sun in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(sun.type==LightType.Directional){sun.intensity=1.25f;sun.color=Color.Lerp(Color.white,p.horizon,.12f);}
        }
        public static void Build(PremiumTrackPresentation k,LevelDefinition level,Material rock,Material dark,Material pearl,Material metal,Material accent,Material glow,Material leaves,Material leafLight,Material wood)
        {
            var bounds=new Bounds(level.spawn,Vector3.zero);foreach(var p in level.routePoints)bounds.Encapsulate(p);
            // Batched, distant cloud banks add depth without transparent layers or frame scripts.
            if(level.environment==0||level.environment==2||level.environment==6||level.environment==7||level.environment==11||level.environment==13||level.environment==15||level.environment==18||level.environment==19)
                for(int i=0;i<6;i++)
                {
                    Vector3 center=new Vector3(bounds.center.x+(i%2==0?-1:1)*(bounds.extents.x+45),level.spawn.y+18+(i%3)*8,Mathf.Lerp(bounds.min.z,bounds.max.z+30,i/5f));
                    for(int j=0;j<4;j++)k.Ellipsoid(center+new Vector3((j-1.5f)*5,Mathf.Sin(j*2)*1.1f,0),new Vector3(8,3+(j%2)*2,5),pearl,12,7);
                }
            for(int i=0;i<10;i++)
            {
                int side=i%2==0?-1:1;float z=Mathf.Lerp(bounds.min.z-12,bounds.max.z+24,i/9f);
                Vector3 point=new Vector3(bounds.center.x+side*(bounds.extents.x+24+(i%3)*4),level.spawn.y-4+(i%3)*3,z);
                Landmark(k,level.environment,point,.9f+(i%3)*.18f,level.id*37+i,rock,dark,pearl,metal,accent,glow,leaves,leafLight,wood);
            }
            // Local landmarks follow the road, rather than disappearing beyond a
            // large route's bounding box. Keep every neighboring road clear.
            for(int i=0;i<8;i++)
            {
                int index=Mathf.Clamp((i+1)*level.tracks.Length/9,0,level.tracks.Length-1);var t=level.tracks[index];
                Vector3 side=Quaternion.Euler(t.rotation)*Vector3.right*(i%2==0?-1:1);
                Vector3 point=t.position+side*(t.size.x*.5f+10);bool clear=true;
                foreach(var road in level.tracks)
                {var delta=road.position-point;if(Mathf.Abs(delta.y)<28&&new Vector2(delta.x,delta.z).magnitude<8+road.size.x*.5f){clear=false;break;}}
                if(clear)Landmark(k,level.environment,point-Vector3.up*7,.65f,level.id*17+i,rock,dark,pearl,metal,accent,glow,leaves,leafLight,wood);
            }
            for(int i=4;i<level.tracks.Length;i+=9)
            {
                var t=level.tracks[i];Vector3 p=t.position-Vector3.up*.5f;var q=Quaternion.Euler(t.rotation);var right=q*Vector3.right;
                bool clear=true;for(int j=0;j<level.tracks.Length;j++)if(Mathf.Abs(i-j)>8&&Vector3.Distance(level.tracks[j].position,p+Vector3.down*5)<6){clear=false;break;}if(!clear)continue;
                int style=level.environment%4;
                if(style==0){for(int s=-1;s<=1;s+=2)k.Tube(p+right*s*t.size.x*.43f,p+Vector3.down*7,.16f,wood,8);k.Terrace(p+Vector3.down*7,2.3f,3,.45f,rock,i,9);}
                else if(style==1){k.Terrace(p+Vector3.down*1.2f,Mathf.Max(2,t.size.x*.6f),.5f,.8f,accent,i,10);k.Terrace(p+Vector3.down*1.7f,1.3f,9,.7f,rock,i,9);}
                else if(style==2){k.Tube(p-right*t.size.x*.4f,p+Vector3.down*7,.12f,metal,8);k.Tube(p+right*t.size.x*.4f,p+Vector3.down*7,.12f,metal,8);k.Ellipsoid(p+Vector3.down*8,new Vector3(2,1,2),glow,10,6);}
                else{for(int s=-1;s<=1;s+=2)k.Tube(p+right*s*(t.size.x*.5f+.25f),p+right*s*(t.size.x*.5f+.25f)+Vector3.up*4,.07f,accent,7);k.BeveledBox(p+Vector3.down*1.2f,new Vector3(t.size.x,.6f,1.4f),.2f,metal,pearl);}
            }
        }
        static void Landmark(PremiumTrackPresentation k,int env,Vector3 p,float s,int seed,Material rock,Material dark,Material pearl,Material metal,Material accent,Material glow,Material leaves,Material leafLight,Material wood)
        {
            switch(env)
            {
                case 0:k.FloatingIsland(p,6*s,leaves,rock,dark,wood,leaves,leafLight,seed);k.Box(p+new Vector3(4*s,-8*s,0),new Vector3(.8f,16*s,.2f),glow);Palm(k,p,5*s,wood,leaves,leafLight);break;
                case 1:k.Terrace(p,5*s,18*s,.4f,rock,seed,8);Arch(k,p+Vector3.up*5*s,11*s,11*s,rock,accent);for(int i=0;i<3;i++)k.Terrace(p+new Vector3((i-1)*4*s,-5*s,3*s),2*s,12*s,.35f,dark,seed+i,7);break;
                case 2:k.Terrace(p+Vector3.up*10*s,.1f,27*s,80,dark,seed,9);k.Terrace(p+Vector3.up*11*s,.08f,8*s,40,pearl,seed,9);for(int i=0;i<3;i++)k.Tree(p+new Vector3((i-1)*4*s,-2*s,3*s),1.4f*s,wood,leaves,pearl,seed+i,true);break;
                case 3:Tower(k,p,25*s,7*s,metal,pearl,glow);Crane(k,p+Vector3.up*25*s,12*s,accent,metal);break;
                case 4:k.Ellipsoid(p+Vector3.up*6*s,Vector3.one*5*s,pearl,20,12);Ring(k,p+Vector3.up*6*s,8*s,.15f*s,accent,Quaternion.Euler(24,15,0));Ring(k,p+Vector3.up*6*s,9*s,.10f*s,glow,Quaternion.Euler(70,55,0));k.Ellipsoid(p+new Vector3(8*s,7*s,0),Vector3.one*1.5f*s,accent,12,7);break;
                case 5:k.Terrace(p+Vector3.up*9*s,2*s,25*s,4,dark,seed,9);k.Terrace(p+Vector3.up*9.3f*s,2.1f*s,.35f,1,glow,seed,12);for(int i=0;i<4;i++)k.Ellipsoid(p+new Vector3(i*.8f,12+i*2,0)*s,Vector3.one*(1.2f+i*.5f)*s,rock,10,7);break;
                case 6:k.BeveledBox(p,new Vector3(8,10,8)*s,.8f,pearl,accent);for(int i=0;i<3;i++){Vector3 q=p+new Vector3((i-1)*5,7,0)*s;k.Tube(q-Vector3.up*6*s,q+Vector3.up*2*s,.3f*s,accent,12);k.Ellipsoid(q+Vector3.up*3*s,Vector3.one*2.4f*s,i%2==0?accent:leafLight,18,12);Ring(k,q+Vector3.up*3*s,1.8f*s,.18f*s,pearl,Quaternion.Euler(90,0,0));}break;
                case 7:Torii(k,p,9*s,accent,wood,pearl);for(int i=0;i<5;i++){Vector3 q=p+new Vector3((i-2)*2,0,3)*s;k.Tube(q-Vector3.up*7*s,q+Vector3.up*(7+i%3)*s,.18f*s,leaves,8);for(int n=0;n<3;n++)k.Ellipsoid(q+new Vector3(1,n*2+2,0)*s,new Vector3(1.5f,.15f,.6f)*s,leafLight,10,5);}break;
                case 8:Arch(k,p+Vector3.up*3*s,12*s,13*s,rock,glow);for(int j=0;j<3;j++){k.Terrace(p+new Vector3((j-1)*5,-4,4)*s,1.2f*s,10*s,.92f,pearl,seed+j,10);k.Tube(p+new Vector3((j-1)*5,5,3)*s,p+new Vector3((j-1)*5,-5,3)*s,.25f*s,leaves,8);}break;
                case 9:for(int i=0;i<4;i++)Mushroom(k,p+new Vector3((i-1.5f)*3,0,i%2*3)*s,(i==1?1.7f:1)*s,leaves,accent,glow);break;
                case 10:k.Ellipsoid(p+Vector3.up*5*s,new Vector3(7,6,7)*s,metal,18,10);for(int i=0;i<3;i++)Ring(k,p+Vector3.up*(2+i*3)*s,(6-i)*s,.14f*s,glow,Quaternion.identity);for(int i=0;i<5;i++){Vector3 q=p+new Vector3((i-2)*3,-4,3)*s;k.Tube(q,q+Vector3.up*(3+i%2)*s,.3f*s,accent,8);k.Ellipsoid(q+Vector3.up*3*s,new Vector3(1.2f,2,1)*s,leaves,10,7);}break;
                case 11:Ship(k,p,11*s,wood,pearl,accent,metal);break;
                case 12:Gear(k,p+Vector3.up*7*s,7*s,metal,accent);Gear(k,p+new Vector3(9,10,1)*s,4*s,accent,pearl);for(int i=0;i<3;i++)k.Tube(p+new Vector3((i-1)*6,-7,2)*s,p+new Vector3((i-1)*6,17,2)*s,.45f*s,metal,10);break;
                case 13:k.FloatingIsland(p,7*s,leaves,rock,dark,wood,leaves,leafLight,seed);Windmill(k,p+Vector3.up*s,11*s,pearl,wood,accent);for(int i=0;i<3;i++)k.Tree(p+new Vector3((i-1)*4,1,-3)*s,1.1f*s,wood,leaves,leafLight,seed+i);break;
                case 14:case 16:for(int i=0;i<5;i++){Vector3 q=p+new Vector3((i-2)*2.4f,i%2*3,0)*s;float r=(1.4f+i%3*.3f)*s;k.Terrace(q,r,6*s,.02f,glow,seed+i,6);k.Terrace(q,r,-7*s,.03f,pearl,seed+i,6);}if(env==16)Ring(k,p+Vector3.up*6*s,9*s,.14f*s,accent,Quaternion.Euler(40,0,20));break;
                case 15:Arch(k,p,13*s,15*s,pearl,accent);for(int i=0;i<3;i++){Vector3 q=p+new Vector3((i-1)*7,4,4)*s;k.Terrace(q,2*s,12*s,.8f,pearl,seed,12);k.Terrace(q+Vector3.up*6*s,.1f,6*s,22,accent,seed,10);}for(int i=0;i<3;i++)k.Ellipsoid(p+new Vector3((i-1)*7,-8,0)*s,new Vector3(7,2,4)*s,pearl,12,7);break;
                case 17:for(int i=0;i<3;i++){Vector3 q=p+new Vector3((i-1)*7,2+i%2*3,0)*s;k.BeveledBox(q,new Vector3(5,5,5)*s,.4f,metal,glow);for(int side=-1;side<=1;side+=2)k.Tube(q+new Vector3(side*2.6f,-2.5f,-2.6f)*s,q+new Vector3(side*2.6f,2.5f,-2.6f)*s,.09f*s,glow,6);}Ring(k,p+Vector3.up*12*s,5*s,.16f*s,glow,Quaternion.Euler(90,0,0));break;
                case 18:for(int i=0;i<5;i++){Vector3 q=p+new Vector3((i-2)*3,i%2*4,0)*s;k.BeveledBox(q,new Vector3(3,4+i%3,3)*s,.22f,i%2==0?accent:leaves,pearl);}Crane(k,p+Vector3.up*10*s,8*s,accent,metal);Gear(k,p+new Vector3(0,6,5)*s,5*s,wood,accent);break;
                default:k.Terrace(p+Vector3.up*5*s,7*s,20*s,.45f,rock,seed,8);for(int i=0;i<3;i++)k.Terrace(p+Vector3.up*(2-i*4)*s,7.3f*s,.6f,1,dark,seed,8);Windmill(k,p+Vector3.up*6*s,12*s,pearl,metal,accent);break;
            }
        }
        static void Palm(PremiumTrackPresentation k,Vector3 p,float h,Material trunk,Material leaves,Material bright)
        {k.Tube(p,p+new Vector3(.4f,h,0),.22f,trunk,8);for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Vector3 q=p+Vector3.up*h;k.Ellipsoid(q+new Vector3(Mathf.Cos(a)*2,.2f,Mathf.Sin(a)*2),new Vector3(2.7f,.22f,1),i%2==0?leaves:bright,10,6);}}
        static void Torii(PremiumTrackPresentation k,Vector3 p,float size,Material red,Material dark,Material pearl)
        {for(int side=-1;side<=1;side+=2){k.Tube(p+Vector3.right*side*size*.4f-Vector3.up*3,p+Vector3.right*side*size*.4f+Vector3.up*size,.3f,red,12);k.Tube(p+new Vector3(side*size*.25f,size*.65f,0),p+new Vector3(side*size*.25f,size*.45f,0),.04f,dark,6);k.BeveledBox(p+new Vector3(side*size*.25f,size*.42f,0),new Vector3(.9f,1.2f,.9f),.16f,pearl,red);}k.BeveledBox(p+Vector3.up*size,new Vector3(size*1.25f,.7f,1.4f),.25f,dark,red);k.BeveledBox(p+Vector3.up*size*.73f,new Vector3(size,.4f,.7f),.13f,red);}
        static void Mushroom(PremiumTrackPresentation k,Vector3 p,float s,Material stem,Material cap,Material glow)
        {k.Tube(p-Vector3.up*4*s,p+new Vector3(.5f,2,0)*s,.45f*s,stem,12);k.Tube(p+new Vector3(.5f,2,0)*s,p+Vector3.up*5*s,.38f*s,stem,12);k.Ellipsoid(p+Vector3.up*5.2f*s,new Vector3(3.4f,1.35f,3.4f)*s,cap,20,10);for(int i=0;i<14;i++){float a=i*Mathf.PI/7;k.Tube(p+Vector3.up*4.8f*s,p+new Vector3(Mathf.Cos(a)*2.8f,4.8f,Mathf.Sin(a)*2.8f)*s,.055f*s,glow,6);}for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f;k.Ellipsoid(p+new Vector3(Mathf.Cos(a)*2,6.25f,Mathf.Sin(a)*2)*s,Vector3.one*.19f*s,glow,8,5);}}
        static void Arch(PremiumTrackPresentation k,Vector3 p,float w,float h,Material wall,Material trim)
        {for(int s=-1;s<=1;s+=2){k.BeveledBox(p+new Vector3(s*w*.5f,h*.25f,0),new Vector3(w*.15f,h,2),.3f,wall,trim);k.BeveledBox(p+new Vector3(s*w*.5f,-h*.24f,0),new Vector3(w*.23f,1,3),.3f,trim);}k.BeveledBox(p+Vector3.up*h*.73f,new Vector3(w*1.25f,1.4f,2.7f),.4f,wall,trim);}
        static void Ring(PremiumTrackPresentation k,Vector3 p,float r,float tube,Material m,Quaternion q)
        {for(int i=0;i<48;i++){float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;k.Tube(p+q*new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),p+q*new Vector3(Mathf.Cos(b)*r,0,Mathf.Sin(b)*r),tube,m,8);}}
        static void Gear(PremiumTrackPresentation k,Vector3 p,float r,Material body,Material rim)
        {Ring(k,p,r,.4f,body,Quaternion.Euler(90,0,0));for(int i=0;i<16;i++){float a=i*Mathf.PI/8;var q=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);k.Box(p+radial*(r+.4f),new Vector3(1.2f,1.4f,1),rim,q);if(i%2==0)k.Tube(p,p+radial*(r-.3f),.18f,rim,8);}k.Tube(p-Vector3.forward,p+Vector3.forward,.65f,rim,12);}
        static void Tower(PremiumTrackPresentation k,Vector3 p,float h,float w,Material wall,Material edge,Material light)
        {k.BeveledBox(p+Vector3.up*h*.3f,new Vector3(w,h,w),.7f,wall,edge);for(int i=0;i<6;i++)k.BeveledBox(p+Vector3.up*(i*h/6-h*.15f),new Vector3(w+.1f,.22f,w+.1f),.7f,light);}
        static void Crane(PremiumTrackPresentation k,Vector3 p,float reach,Material beam,Material wire)
        {k.Tube(p,p+Vector3.up*8,.22f,beam,8);k.Tube(p+Vector3.up*7,p+new Vector3(reach,7,0),.18f,beam,8);k.Tube(p+Vector3.up*8,p+new Vector3(reach,7,0),.05f,wire,6);k.Tube(p+new Vector3(reach,7,0),p+new Vector3(reach,-2,0),.04f,wire,6);}
        static void Windmill(PremiumTrackPresentation k,Vector3 p,float h,Material wall,Material shaft,Material edge)
        {k.Terrace(p+Vector3.up*h*.5f,1.7f,h,.75f,wall,5,10);Vector3 center=p+Vector3.up*h;for(int i=0;i<4;i++){var q=Quaternion.Euler(0,0,25+i*90);k.Box(center+q*Vector3.up*h*.24f,new Vector3(.6f,h*.48f,.3f),shaft,q);k.Box(center+q*Vector3.up*h*.34f,new Vector3(1.5f,h*.23f,.34f),edge,q);}k.Ellipsoid(center,Vector3.one*.55f,edge,12,7);}
        static void Ship(PremiumTrackPresentation k,Vector3 p,float length,Material hull,Material sail,Material trim,Material dark)
        {k.Ellipsoid(p,new Vector3(length*.38f,2,length*.75f),hull,16,8);k.BeveledBox(p+Vector3.up*1.2f,new Vector3(length*.65f,.6f,length*1.35f),.5f,hull,trim);for(int i=-1;i<=1;i++){Vector3 mast=p+Vector3.forward*i*length*.38f;k.Tube(mast,mast+Vector3.up*(10-Mathf.Abs(i)*2),.15f,hull,10);k.Box(mast+Vector3.up*6,new Vector3(5-Mathf.Abs(i),5,.12f),sail,Quaternion.Euler(0,15,0));}k.Tube(p+Vector3.forward*length*.7f,p+Vector3.forward*length, .12f,trim,8);}
    }
}
