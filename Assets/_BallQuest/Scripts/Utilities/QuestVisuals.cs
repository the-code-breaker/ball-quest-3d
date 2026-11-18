using UnityEngine;

namespace BallQuest
{
    public static class QuestVisuals
    {
        static readonly System.Collections.Generic.Dictionary<BallSkin,Texture2D> skinTextures=new System.Collections.Generic.Dictionary<BallSkin,Texture2D>();
        public static GameObject Shape(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 size, Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(primitive); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision)
            {
                go.layer = 2; var collider=go.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.GetComponent<Renderer>().receiveShadows = false;
            }
            return go;
        }
        public static Material Material(string name, Color color, float metallic = 0, float smoothness = 0.35f)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
            return material;
        }
        public static Texture2D SkinTexture(BallSkin skin)
        {
            if(skinTextures.TryGetValue(skin,out var cached)&&cached)return cached;
            const int width=512,height=256;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,true){name=skin.id+" detailed spherical finish",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            var colors = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u=(x+.5f)/width,v=(y+.5f)/height;
                float phi=u*Mathf.PI*2,latitude=(v-.5f)*Mathf.PI;
                var normal=new Vector3(Mathf.Sin(phi)*Mathf.Cos(latitude),Mathf.Sin(latitude),Mathf.Cos(phi)*Mathf.Cos(latitude));
                float noise=Mathf.PerlinNoise(u*29+5,v*17+2),fine=((x*73+y*29)%97)/97f;
                Color color=skin.color;
                switch(skin.artStyle)
                {
                    case BallArtStyle.Turbine:
                        float weave=(Mathf.Sin((x+y)*1.2f)*Mathf.Sin((x-y)*1.2f)+1)*.5f;
                        float panel=Mathf.Min(Mathf.Repeat(u*8,1),Mathf.Repeat(v*5,1));
                        color=Color.Lerp(new Color(.035f,.045f,.056f),new Color(.105f,.125f,.14f),weave*.55f+noise*.25f);
                        if(panel<.018f)color*=.38f;
                        break;
                    case BallArtStyle.Soccer:
                        float best=-2;Vector3 nearest=Vector3.up;
                        foreach(var point in IcosahedronDirections)
                        {float dot=Vector3.Dot(point,normal);if(dot>best){best=dot;nearest=point;}}
                        Vector3 right=Vector3.Cross(nearest,Mathf.Abs(nearest.y)>.9f?Vector3.forward:Vector3.up).normalized;
                        Vector3 up=Vector3.Cross(right,nearest);
                        float angle=Mathf.Atan2(Vector3.Dot(normal,up),Vector3.Dot(normal,right));
                        float segment=Mathf.Repeat(angle+Mathf.PI/5,Mathf.PI*2/5)-Mathf.PI/5;
                        float pentagon=.28f/Mathf.Cos(segment);
                        float distance=Mathf.Acos(Mathf.Clamp(best,-1,1));
                        color=distance<pentagon?new Color(.035f,.045f,.06f):new Color(.92f,.95f,.97f);
                        if(Mathf.Abs(distance-pentagon)<.012f)color*=.55f;
                        color*=.96f+fine*.07f;
                        break;
                    case BallArtStyle.Basketball:
                        color=Color.Lerp(new Color(.73f,.20f,.035f),new Color(1,.48f,.08f),noise*.5f+fine*.4f);
                        if(Mathf.Abs(normal.x)<.023f||Mathf.Abs(normal.y)<.023f||Mathf.Abs(normal.z+.3f*Mathf.Sin(normal.y*4))<.028f)color=new Color(.035f,.025f,.02f);
                        break;
                    case BallArtStyle.Galaxy:
                        float nebula=Mathf.PerlinNoise(u*6,v*4)*Mathf.PerlinNoise(u*13+3,v*8+2);
                        color=Color.Lerp(new Color(.015f,.012f,.07f),Color.Lerp(new Color(.17f,.05f,.5f),new Color(.02f,.58f,.88f),noise),Mathf.Clamp01(nebula*3));
                        if((x*17+y*79)%661<2)color=Color.white;
                        break;
                    case BallArtStyle.Gold:
                        color=Color.Lerp(new Color(.68f,.34f,.025f),new Color(1,.81f,.24f),.5f+Mathf.Sin(v*180)*.04f+noise*.15f);break;
                    case BallArtStyle.Ice:
                        float facet=Mathf.Floor(noise*9)/9;
                        color=Color.Lerp(new Color(.08f,.38f,.66f),new Color(.64f,.96f,1),facet);
                        if(Mathf.Abs(Mathf.Sin(u*70+Mathf.Sin(v*40)*3))<.045f)color=new Color(.87f,1,1);
                        break;
                    case BallArtStyle.Lava:
                        float fissure=Mathf.Pow(Mathf.Abs(Mathf.Sin(u*63+Mathf.Sin(v*29)*2+noise*3)),24);
                        color=Color.Lerp(new Color(.055f,.025f,.035f),new Color(1,.36f,.025f),fissure);break;
                    case BallArtStyle.BrushedSteel:case BallArtStyle.Copper:
                        float brushing=Mathf.Sin(v*1700+Mathf.Sin(u*19)*.35f)*.018f+Mathf.Sin(v*631)*.012f;
                        float seam=Mathf.Abs(Mathf.Sin(v*Mathf.PI*6))<.018f?.55f:1;
                        color=skin.color*(.87f+brushing+noise*.06f)*seam;break;
                    case BallArtStyle.CarbonRubber:
                        float grip=(x%8<2&&y%8<2)?.68f:1;
                        color=skin.color*(.86f+noise*.28f)*grip;
                        if(Mathf.Abs(v-.5f)<.018f)color=skin.stripe;break;
                    case BallArtStyle.Walnut:
                        float timber=.5f+.5f*Mathf.Sin(u*95+Mathf.Sin(v*18)*4+noise*2);
                        color=Color.Lerp(skin.stripe,skin.color,.35f+timber*.65f);break;
                    case BallArtStyle.Chrome:
                        color=Color.Lerp(new Color(.32f,.40f,.49f),new Color(.8f,.88f,.95f),noise*.35f+.4f);
                        if(Mathf.Abs(Mathf.Sin(v*16))<.035f)color*=.45f;
                        break;
                    case BallArtStyle.Rainbow:
                        color=Color.HSVToRGB(Mathf.Repeat(u+v*.5f+noise*.12f,1),.86f,1);break;
                    case BallArtStyle.Earth:
                        float land=Mathf.PerlinNoise(u*7+20,v*4+12)+Mathf.PerlinNoise(u*15+2,v*10)*.22f;
                        color=land>.65f?Color.Lerp(new Color(.12f,.46f,.14f),new Color(.72f,.68f,.3f),noise):Color.Lerp(new Color(.015f,.12f,.42f),new Color(.02f,.48f,.79f),noise);
                        if(Mathf.Abs(normal.y)>.92f)color=Color.Lerp(color,Color.white,.9f);
                        break;
                    default:
                        float vein=Mathf.Pow(Mathf.Abs(Mathf.Sin(x*.09f+Mathf.Sin(y*.13f)*3+noise*2)),16);
                        color=Color.Lerp(skin.color,skin.stripe,vein);break;
                }
                colors[y * width + x] = color;
            }
            texture.SetPixels(colors);texture.Apply(true,true);skinTextures[skin]=texture;return texture;
        }

        static readonly Vector3[] IcosahedronDirections=CreateIcosahedronDirections();
        static Vector3[] CreateIcosahedronDirections()
        {
            float phi=(1+Mathf.Sqrt(5))*.5f;var values=new System.Collections.Generic.List<Vector3>();
            for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)
            {values.Add(new Vector3(0,a,b*phi).normalized);values.Add(new Vector3(a,b*phi,0).normalized);values.Add(new Vector3(b*phi,0,a).normalized);}
            return values.ToArray();
        }
    }
}
