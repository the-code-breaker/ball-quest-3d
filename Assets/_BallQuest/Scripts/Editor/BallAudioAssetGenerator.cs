using System.IO;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    /// <summary>Bakes original v9 contact Foley and migrates old procedural drone profiles once.</summary>
    public static class BallAudioAssetGenerator
    {
        const string Root="Assets/_BallQuest/Resources/BallAudio";
        const string Samples="Assets/_BallQuest/Audio/SFX/ContactFoleyV9";
        [MenuItem("Tools/Ball Quest/Generate Material Contact Audio")]
        public static void GenerateProfiles()
        {
            Directory.CreateDirectory(Root);Directory.CreateDirectory(Samples);
            for(int material=0;material<6;material++)for(int kind=0;kind<3;kind++)
            {
                string path=ClipPath((BallAudioMaterial)material,kind);
                if(!File.Exists(path))WriteWave(path,TemporaryBallAudio.Samples((BallAudioMaterial)material,kind));
            }
            for(int surface=0;surface<8;surface++)foreach(int kind in new[]{0,2})
            {
                string path=SurfacePath((SurfaceAudioKind)surface,kind);
                if(!File.Exists(path))WriteWave(path,TemporaryBallAudio.SurfaceSamples((SurfaceAudioKind)surface,kind));
            }
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Samples,"*.wav"))ConfigureImport(path.Replace('\\','/'));
            for(int material=0;material<6;material++)
            {
                var kind=(BallAudioMaterial)material;string path=Root+"/"+kind+".asset";
                var profile=AssetDatabase.LoadAssetAtPath<BallAudioProfile>(path);
                bool create=!profile;
                if(create){profile=ScriptableObject.CreateInstance<BallAudioProfile>();profile.material=kind;}
                bool upgrade=create||profile.audioRevision<9||string.IsNullOrEmpty(profile.provenance)||!profile.provenance.Contains("Foley v9")||
                    !profile.rolling||!AssetDatabase.GetAssetPath(profile.rolling).StartsWith(Samples);
                if(!upgrade)continue;
                profile.audioRevision=9;profile.provenance=TemporaryBallAudio.Provenance;profile.surfaces=BallAudioProfile.DefaultSurfaces();
                profile.rolling=AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(kind,0));
                profile.sliding=AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(kind,1));
                profile.impact=AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(kind,2));
                profile.rollingVolume=kind==BallAudioMaterial.Glass?.14f:kind==BallAudioMaterial.Stone?.21f:.18f;
                profile.slidingVolume=.16f;profile.impactVolume=kind==BallAudioMaterial.Rubber?.38f:.48f;
                profile.referenceSpeed=10;profile.referenceImpactImpulse=8;profile.impactCooldown=.075f;profile.surfaceCrossfade=.12f;
                foreach(var response in profile.surfaces)
                {
                    response.impactOverride=AssetDatabase.LoadAssetAtPath<AudioClip>(SurfacePath(response.surface,2));
                    // Surface-dominant textures replace smooth-ball tone on dry grit,
                    // grass and glassy/icy tracks. Wood/metal retain ball timbre too.
                    if(response.surface==SurfaceAudioKind.Sand||response.surface==SurfaceAudioKind.Grass||response.surface==SurfaceAudioKind.Ice||response.surface==SurfaceAudioKind.Glass)
                        response.rollingOverride=AssetDatabase.LoadAssetAtPath<AudioClip>(SurfacePath(response.surface,0));
                }
                if(create)AssetDatabase.CreateAsset(profile,path);else EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();WriteAudition();
        }
        static string ClipPath(BallAudioMaterial material,int kind)=>Samples+"/FoleyV9_"+material+"_"+(kind==0?"Rolling":kind==1?"Sliding":"Impact")+".wav";
        static string SurfacePath(SurfaceAudioKind surface,int kind)=>Samples+"/FoleyV9_Surface_"+surface+"_"+(kind==0?"Rolling":"Impact")+".wav";
        static void ConfigureImport(string path)
        {
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;if(!importer)return;
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=path.Contains("Impact")?AudioCompressionFormat.PCM:AudioCompressionFormat.Vorbis;
            settings.quality=.85f;settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=TemporaryBallAudio.SampleRate;settings.preloadAudioData=true;
            // Sources are already mono; avoid forced downmix normalization altering
            // the deliberately quiet friction/transient balance.
            importer.forceToMono=false;importer.loadInBackground=false;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        static void WriteAudition()
        {
            const int sections=14,seconds=3;int rate=TemporaryBallAudio.SampleRate;
            var mix=new float[sections*seconds*rate];var labels=new System.Text.StringBuilder("Original procedural Foley v9 audition, no music. Each section is three seconds: two seconds of rolling followed by one impact.\n");
            for(int section=0;section<sections;section++)
            {
                bool body=section<6;
                var rolling=body?TemporaryBallAudio.Samples((BallAudioMaterial)section,0):TemporaryBallAudio.SurfaceSamples((SurfaceAudioKind)(section-6),0);
                var impact=body?TemporaryBallAudio.Samples((BallAudioMaterial)section,2):TemporaryBallAudio.SurfaceSamples((SurfaceAudioKind)(section-6),2);
                int start=section*seconds*rate;
                for(int i=0;i<2*rate;i++)mix[start+i]=rolling[i]*Mathf.Lerp(.25f,.9f,i/(2f*rate));
                for(int i=0;i<impact.Length;i++)mix[start+2*rate+i]=impact[i]*.85f;
                labels.Append(section*seconds).Append("s ").Append(body?((BallAudioMaterial)section).ToString():"surface "+((SurfaceAudioKind)(section-6))).Append('\n');
            }
            Directory.CreateDirectory("TestResults/Audio");WriteWave("TestResults/Audio/ContactFoleyV9-Audition.wav",mix);File.WriteAllText("TestResults/Audio/ContactFoleyV9-Audition.txt",labels.ToString());
        }
        static void WriteWave(string path,float[] samples)
        {
            using(var stream=File.Create(path))using(var writer=new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);
                writer.Write((short)1);writer.Write((short)1);writer.Write(TemporaryBallAudio.SampleRate);
                writer.Write(TemporaryBallAudio.SampleRate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);
                for(int i=0;i<samples.Length;i++)writer.Write((short)(Mathf.Clamp(samples[i],-1,1)*32767));
            }
        }
    }
}
