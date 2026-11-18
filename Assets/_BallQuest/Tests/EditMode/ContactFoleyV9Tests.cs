using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Tests
{
    public sealed class ContactFoleyV9Tests
    {
        [Serializable]sealed class Signal{public string material;public float rms,peakToneEnergyFraction,peakAutocorrelation;}
        [Serializable]sealed class Signals{public string scope="Original generated source waveform analysis, not a human listening or phone-speaker result";public Signal[] signals;}
        static float Rms(float[] samples,int start,int end)
        {double sum=0;for(int i=start;i<end;i++)sum+=samples[i]*samples[i];return(float)Math.Sqrt(sum/Math.Max(1,end-start));}
        [Test]public void RollingHasAperiodicContactTextureWithoutASustainedMotorFundamental()
        {
            var report=new Signals{signals=new Signal[6]};
            for(int material=0;material<6;material++)
            {
                var data=TemporaryBallAudio.Samples((BallAudioMaterial)material,0);float rms=Rms(data,0,data.Length);
                Assert.That(rms,Is.InRange(.025f,.25f));foreach(float value in data){Assert.IsFalse(float.IsNaN(value)||float.IsInfinity(value));Assert.Less(Mathf.Abs(value),.9f);}
                const int step=4;int count=data.Length/step;double energy=0;for(int i=0;i<count;i++)energy+=data[i*step]*data[i*step];
                float peakTone=0,peakCorrelation=0;
                for(int frequency=25;frequency<=350;frequency++)
                {
                    double coefficient=2*Math.Cos(2*Math.PI*frequency/(TemporaryBallAudio.SampleRate/(double)step)),q0=0,q1=0,q2=0;
                    for(int i=0;i<count;i++){q0=data[i*step]+coefficient*q1-q2;q2=q1;q1=q0;}
                    double power=q1*q1+q2*q2-coefficient*q1*q2;
                    peakTone=Mathf.Max(peakTone,(float)(2*power/(count*energy)));
                }
                for(int lag=TemporaryBallAudio.SampleRate/50;lag<=TemporaryBallAudio.SampleRate/7;lag+=71)
                {
                    double cross=0,a=0,b=0;
                    for(int i=0;i<data.Length-lag;i+=step){cross+=data[i]*data[i+lag];a+=data[i]*data[i];b+=data[i+lag]*data[i+lag];}
                    peakCorrelation=Mathf.Max(peakCorrelation,(float)Math.Abs(cross/Math.Sqrt(a*b)));
                }
                Assert.Less(peakTone,.05f,"A narrow low-frequency fundamental would recreate the reported motor-like drone.");
                Assert.Less(peakCorrelation,.15f,"Contact grains must not form a repeating engine cadence.");
                report.signals[material]=new Signal{material=((BallAudioMaterial)material).ToString(),rms=rms,peakToneEnergyFraction=peakTone,peakAutocorrelation=peakCorrelation};
            }
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/contact-foley-spectrum-v10.json",JsonUtility.ToJson(report,true));
        }
        [Test]public void MaterialImpactsAreShortDistinctTransientsWithDecayingTails()
        {
            var signatures=new System.Collections.Generic.HashSet<int>();
            for(int material=0;material<6;material++)
            {
                var data=TemporaryBallAudio.Samples((BallAudioMaterial)material,2);
                Assert.LessOrEqual(data.Length/(float)TemporaryBallAudio.SampleRate,.3f);
                float early=Rms(data,0,data.Length/3),tail=Rms(data,data.Length*2/3,data.Length);
                Assert.Greater(early,.04f);Assert.Less(tail,early*.15f);
                int hash=17;for(int i=0;i<data.Length;i+=37)hash=unchecked(hash*31+Mathf.RoundToInt(data[i]*100000));Assert.IsTrue(signatures.Add(hash));
            }
        }
        [Test]public void GeneratedProfilesUseV9AssetsAndActualSurfaceImpactOverrides()
        {
            var profiles=Resources.LoadAll<BallAudioProfile>("BallAudio");Assert.AreEqual(6,profiles.Length);
            foreach(var profile in profiles)
            {
                Assert.AreEqual(10,profile.audioRevision);Assert.IsTrue(profile.provenance.Contains("Natural contact v10"));
                foreach(var clip in new[]{profile.rolling,profile.sliding,profile.impact})
                {Assert.IsNotNull(clip);Assert.IsTrue(AssetDatabase.GetAssetPath(clip).Contains("NaturalContactV10"));Assert.IsFalse(clip.name.StartsWith("TEMP_"));}
                Assert.Greater(profile.rolling.length,2);
                foreach(var surface in profile.surfaces){Assert.IsNotNull(surface.impactOverride);Assert.IsTrue(surface.impactOverride.name.Contains(surface.surface.ToString()));}
                Assert.IsNull(profile.Response(SurfaceAudioKind.Ice).rollingOverride,"Surface response must retain the selected ball's recorded timbre.");Assert.IsNull(profile.Response(SurfaceAudioKind.Sand).rollingOverride);
            }
        }
    }
}
