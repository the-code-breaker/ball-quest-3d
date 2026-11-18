using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    public static class ArcadeArtGenerator
    {
        const string Root="Assets/_BallQuest";
        public static void Generate(GameCatalog catalog)
        {
            var skins=new List<BallSkin>();
            skins.Add(Skin("classic-red","ION BLUE",BallArtStyle.Turbine,new Color(.015f,.5f,.95f),new Color(.03f,.86f,1),0,.7f,.72f,true));
            skins.Add(Skin("ocean-blue","VOLT GREEN",BallArtStyle.Turbine,new Color(.24f,.8f,.05f),new Color(.38f,1,.04f),0,.65f,.72f,true));
            skins.Add(Skin("neon","CRIMSON RUSH",BallArtStyle.Turbine,new Color(.85f,.075f,.13f),new Color(1,.16f,.14f),600,.65f,.72f,true));
            skins.Add(Skin("soccer","STREET STRIKER",BallArtStyle.Soccer,Color.white,new Color(.03f,.04f,.06f),350,.1f,.48f));
            skins.Add(Skin("basketball","COURTSIDE",BallArtStyle.Basketball,new Color(1,.4f,.04f),Color.black,400,.03f,.4f));
            skins.Add(Skin("galaxy","GALAXY",BallArtStyle.Galaxy,new Color(.3f,.08f,.7f),new Color(.08f,.7f,1),1000,.5f,.88f,true));
            skins.Add(Skin("golden","GOLD STANDARD",BallArtStyle.Gold,new Color(1,.7f,.1f),new Color(1,.95f,.5f),1300,.93f,.88f));
            skins.Add(Skin("crystal","FROSTBYTE",BallArtStyle.Ice,new Color(.1f,.65f,1),new Color(.8f,1,1),850,.58f,.87f));
            skins.Add(Skin("lava","MAGMA",BallArtStyle.Lava,new Color(.08f,.025f,.03f),new Color(1,.25f,0),900,.55f,.68f,true));
            skins.Add(Skin("chrome","LIQUID CHROME",BallArtStyle.Chrome,new Color(.6f,.7f,.8f),Color.white,1100,.96f,.92f));
            skins.Add(Skin("rainbow","SPECTRUM",BallArtStyle.Rainbow,new Color(.8f,.1f,.8f),Color.white,750,.45f,.86f));
            skins.Add(Skin("earth","BLUE PLANET",BallArtStyle.Earth,new Color(.05f,.35f,.8f),new Color(.2f,.7f,.25f),700,.35f,.76f));
            catalog.skins=skins.ToArray();EditorUtility.SetDirty(catalog);
            foreach(string file in Directory.GetFiles(Root+"/Resources/Arcade","*.png"))
            {
                var importer=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(!importer)continue;
                bool icon=file.EndsWith("IconAtlas.png");
                importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=icon;importer.mipmapEnabled=false;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;
                importer.maxTextureSize=file.EndsWith("HomeBackdrop.png")?2048:2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                if(icon)importer.isReadable=false;
                importer.SaveAndReimport();
            }
            var appIcon=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Resources/Arcade/AppIcon.png");
            if(appIcon)PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown,new[]{appIcon},IconKind.Any);
            AssetDatabase.SaveAssets();
        }
        static BallSkin Skin(string id,string title,BallArtStyle style,Color color,Color accent,int price,float metallic,float smoothness,bool glow=false)
        {
            string path=Root+"/ScriptableObjects/Skins/"+id+".asset";
            var skin=AssetDatabase.LoadAssetAtPath<BallSkin>(path);
            if(!skin){skin=ScriptableObject.CreateInstance<BallSkin>();AssetDatabase.CreateAsset(skin,path);}
            skin.id=id;skin.displayName=title;skin.artStyle=style;skin.color=color;skin.stripe=accent;skin.price=price;skin.metallic=metallic;skin.smoothness=smoothness;skin.glow=glow;skin.trail=glow;
            EditorUtility.SetDirty(skin);return skin;
        }
    }
}
