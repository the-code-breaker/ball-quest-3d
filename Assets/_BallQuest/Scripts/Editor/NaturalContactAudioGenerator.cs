using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace BallQuest.Editor
{
    public static class NaturalContactAudioGenerator
    {
        public const string Samples="Assets/_BallQuest/Audio/SFX/NaturalContactV10";
        public const string Provenance="Natural contact v10: CC0 ball/contact recordings by slamsal, 89o_dev, SomeoneCool15, RyanKingArt and Ian_G; edited and material/surface mixed. See AUDIO_CREDITS.md.";
        public static void Generate()
        {
            if(!Directory.Exists(Samples))throw new InvalidOperationException("Prepare the licensed contact WAVs first.");
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Samples,"*.wav"))
            {
                var importer=AssetImporter.GetAtPath(path.Replace('\\','/')) as AudioImporter;if(!importer)continue;
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=path.Contains("Rolling")||path.Contains("Sliding")?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
                settings.quality=.9f;settings.preloadAudioData=true;settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=22050;
                importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
            }
            foreach(BallAudioMaterial kind in Enum.GetValues(typeof(BallAudioMaterial)))
            {
                string path="Assets/_BallQuest/Resources/BallAudio/"+kind+".asset";var profile=AssetDatabase.LoadAssetAtPath<BallAudioProfile>(path);
                bool create=!profile;if(create)profile=ScriptableObject.CreateInstance<BallAudioProfile>();
                if(profile.audioRevision>=10&&!create)continue;
                profile.material=kind;profile.audioRevision=10;profile.provenance=Provenance;
                profile.rolling=Clip(kind,"Rolling");profile.sliding=Clip(kind,"Sliding");profile.impact=Clip(kind,"Impact");
                profile.rollingVolume=kind==BallAudioMaterial.Rubber?.14f:.20f;profile.slidingVolume=.055f;profile.impactVolume=.65f;
                profile.referenceSpeed=12;profile.minimumRollingSpeed=.25f;profile.minimumSlipSpeed=1.4f;profile.surfaceCrossfade=.18f;profile.impactCooldown=.10f;profile.referenceImpactImpulse=7;
                profile.surfaces=BallAudioProfile.DefaultSurfaces();
                foreach(var surface in profile.surfaces)surface.impactOverride=Clip(kind,"On_"+surface.surface);
                if(create)AssetDatabase.CreateAsset(profile,path);else EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
        }
        static AudioClip Clip(BallAudioMaterial material,string suffix)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Samples+"/NaturalV10_"+material+"_"+suffix+".wav");
            if(!clip)throw new InvalidOperationException("Missing recorded contact: "+material+" "+suffix);return clip;
        }
    }
}
