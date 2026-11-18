using UnityEditor;
using UnityEngine;
namespace BallQuest.Editor
{
    public static class GrandArtImporter
    {
        public static void Generate()
        {
            const string path="Assets/_BallQuest/Resources/GrandCampaign/WorldAtlasV11.png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)return;
            importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.isReadable=false;importer.SaveAndReimport();
        }
    }
}
