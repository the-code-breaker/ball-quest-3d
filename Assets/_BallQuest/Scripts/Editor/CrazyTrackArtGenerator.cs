using System.IO;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    public static class CrazyTrackArtGenerator
    {
        public static void Generate()
        {
            const string folder="Assets/_BallQuest/Resources/TrackArt";Directory.CreateDirectory(folder);
            foreach(string name in new[]{"CeramicV9","AlloyV9","BasaltV9"})
            {
                string path=folder+"/"+name+".png";
                if(!File.Exists(path))
                {
                    const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGB24,false);var pixels=new Color[size*size];
                    for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                    {
                        float noise=((x*7919+y*1543+x*y*31)%101)/100f-.5f;
                        bool joint=x%32<2||y%32<2;
                        float value;
                        if(name=="CeramicV9")value=joint?.58f:.89f+noise*.025f;
                        else if(name=="AlloyV9")value=joint?.38f:.69f+noise*.045f+Mathf.Sin(y*2.4f)*.018f;
                        else value=joint?.26f:.48f+noise*.09f;
                        pixels[y*size+x]=new Color(value,value*(name=="BasaltV9"?1.025f:1),value*(name=="CeramicV9"?1.025f:1));
                    }
                    texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                }
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)continue;
                importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.maxTextureSize=128;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
        }
    }
}
