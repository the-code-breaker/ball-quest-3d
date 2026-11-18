using System.IO;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    /// <summary>Bake reusable surface maps once; course transitions do not synthesize millions of pixels.</summary>
    public static class PremiumTrackArtGenerator
    {
        const string Folder="Assets/_BallQuest/Resources/TrackArt";
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            Write("SatinMaple",256,512,(x,y)=>
            {
                int plank=x/32;
                float meander=Mathf.Sin(y*.0123f+plank*2.3f)*2+Mathf.Sin(y*.0368f+plank)*.6f;
                float grain=Mathf.Sin(x*.74f+meander)+Mathf.Sin(x*1.43f+meander*1.5f)*.24f;
                float board=((plank*17)%7-3)*.009f;
                float seam=x%32==0?.87f:x%32==1?1.04f:1;
                float value=grain*.013f+board;
                return new Color((.81f+value)*seam,(.635f+value*.86f)*seam,(.405f+value*.65f)*seam);
            });
            foreach(bool night in new[]{false,true})Write(night?"NightWindows":"DayWindows",64,128,(x,y)=>
            {
                bool pane=x%8>1&&y%12>2;float tint=((x/8*17+y/12*31)%7)/7f;
                return pane?night?Color.Lerp(new Color(.16f,.23f,.43f),new Color(1,.68f,.34f),tint):Color.Lerp(new Color(.47f,.7f,.87f),new Color(.87f,.96f,1),tint):new Color(.24f,.35f,.47f);
            });
            Write("HarborWater",128,128,(x,y)=>
            {
                float u=x/128f*Mathf.PI*2,v=y/128f*Mathf.PI*2;
                // Periodic low frequencies survive distance mipmaps; restrained ripples
                // add water depth without an animated material or transparent layers.
                float broad=Mathf.Sin(u+Mathf.Sin(v)*.6f)*.5f+Mathf.Sin(v*2-u)*.3f;
                float ripple=Mathf.Sin(u*9+v*5+Mathf.Sin(v*2))*.025f;
                float value=.84f+broad*.12f+ripple;
                return new Color(value*.87f,value*.96f,value);
            });
        }
        static void Write(string name,int width,int height,System.Func<int,int,Color> pixel)
        {
            string path=Folder+"/"+name+".png";
            if(!File.Exists(path))
            {
                var texture=new Texture2D(width,height,TextureFormat.RGB24,false);var colors=new Color[width*height];
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)colors[y*width+x]=pixel(x,y);
                texture.SetPixels(colors);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)return;
            importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.maxTextureSize=512;importer.SaveAndReimport();
        }
    }
}
