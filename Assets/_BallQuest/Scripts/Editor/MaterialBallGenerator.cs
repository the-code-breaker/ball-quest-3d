using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BallQuest.Editor
{
    public static class MaterialBallGenerator
    {
        public static void Generate(GameCatalog catalog)
        {
            var steel=Make("brushed-steel","Brushed Steel",BallArtStyle.BrushedSteel,new Color(.74f,.83f,.9f),new Color(.12f,.22f,.3f),.95f,.82f,0,BallAudioMaterial.Metal);
            var rubber=Make("carbon-rubber","Carbon Rubber",BallArtStyle.CarbonRubber,new Color(.045f,.065f,.075f),new Color(.96f,.49f,.07f),0,.30f,0,BallAudioMaterial.Rubber);
            var copper=Make("copper-core","Copper Core",BallArtStyle.Copper,new Color(.94f,.49f,.22f),new Color(.14f,.25f,.23f),.94f,.76f,450,BallAudioMaterial.Metal);
            var walnut=Make("walnut","Turned Walnut",BallArtStyle.Walnut,new Color(.55f,.26f,.09f),new Color(.19f,.07f,.025f),0,.42f,300,BallAudioMaterial.Wood);
            catalog.skins=catalog.skins.Where(s=>s&&(s.id=="classic-red"||s.id=="ocean-blue")).Concat(new[]{steel,rubber,copper,walnut}).Concat(catalog.skins.Where(s=>s&&s!=steel&&s!=rubber&&s!=copper&&s!=walnut&&s.id!="classic-red"&&s.id!="ocean-blue")).Distinct().ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        static BallSkin Make(string id,string title,BallArtStyle art,Color color,Color stripe,float metallic,float smooth,int price,BallAudioMaterial sound)
        {
            string path="Assets/_BallQuest/ScriptableObjects/Skins/"+id+".asset";var skin=AssetDatabase.LoadAssetAtPath<BallSkin>(path);if(skin)return skin;
            skin=ScriptableObject.CreateInstance<BallSkin>();skin.id=id;skin.displayName=title;skin.artStyle=art;skin.color=color;skin.stripe=stripe;skin.metallic=metallic;skin.smoothness=smooth;skin.price=price;skin.explicitAudioMaterial=true;skin.audioMaterial=sound;
            AssetDatabase.CreateAsset(skin,path);return skin;
        }
    }
}
