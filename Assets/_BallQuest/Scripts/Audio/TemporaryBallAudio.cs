using UnityEngine;

namespace BallQuest
{
    /// <summary>Original deterministic contact Foley. Name retained for serialized/API compatibility.</summary>
    public static class TemporaryBallAudio
    {
        public const int SampleRate=22050;
        public const string Provenance="Original procedural contact Foley v9: aperiodic friction, irregular microcontacts and short material impacts; not a recording or copied game audio.";
        static readonly BallAudioProfile[] fallback=new BallAudioProfile[6];
        static BallAudioProfile[] authored;

        public static BallAudioProfile Profile(BallAudioMaterial material)
        {
            if(authored==null||authored.Length==0)authored=Resources.LoadAll<BallAudioProfile>("BallAudio");
            for(int i=0;i<authored.Length;i++)if(authored[i]&&authored[i].material==material)return authored[i];
            int index=(int)material;
            if(!fallback[index])
            {
                var value=ScriptableObject.CreateInstance<BallAudioProfile>();
                value.name="Contact Foley v9 "+material;value.material=material;value.provenance=Provenance;value.audioRevision=9;
                value.surfaces=BallAudioProfile.DefaultSurfaces();value.rolling=Clip(material,0);value.sliding=Clip(material,1);value.impact=Clip(material,2);
                fallback[index]=value;
            }
            return fallback[index];
        }
        public static AudioClip Clip(BallAudioMaterial material,int kind)
        {
            var samples=Samples(material,kind);
            var clip=AudioClip.Create("Contact v9 "+material+(kind==0?" roll":kind==1?" slip":" impact"),samples.Length,1,SampleRate,false);
            clip.SetData(samples,0);return clip;
        }
        public static float[] Samples(BallAudioMaterial material,int kind)=>Synthesize(material,kind,0);
        public static float[] SurfaceSamples(SurfaceAudioKind surface,int kind)
        {
            var material=surface==SurfaceAudioKind.Metal?BallAudioMaterial.Metal:surface==SurfaceAudioKind.Stone?BallAudioMaterial.Stone:
                surface==SurfaceAudioKind.Ice||surface==SurfaceAudioKind.Glass?BallAudioMaterial.Glass:
                surface==SurfaceAudioKind.Boost?BallAudioMaterial.Rubber:BallAudioMaterial.Wood;
            return Synthesize(material,kind,(int)surface+1);
        }
        static float Noise(ref uint state){state=state*1664525u+1013904223u;return(state>>8)/8388607.5f-1;}
        static float Unit(ref uint state)=>(Noise(ref state)+1)*.5f;
        static float[] Synthesize(BallAudioMaterial material,int kind,int surfaceVariant)
        {
            float duration=kind==2?.24f:2.4f;
            var samples=new float[Mathf.CeilToInt(SampleRate*duration)];
            uint random=(uint)(48193+(int)material*2749+kind*193+surfaceVariant*1009);
            bool sand=surfaceVariant==(int)SurfaceAudioKind.Sand+1,grass=surfaceVariant==(int)SurfaceAudioKind.Grass+1;
            bool ice=surfaceVariant==(int)SurfaceAudioKind.Ice+1;
            float alpha=material==BallAudioMaterial.Stone?.075f:material==BallAudioMaterial.Rubber?.045f:material==BallAudioMaterial.Glass?.30f:.16f;
            float grainRate=material==BallAudioMaterial.Stone?48:material==BallAudioMaterial.Wood?34:material==BallAudioMaterial.Rubber?18:29;
            if(sand||grass){alpha=sand?.22f:.11f;grainRate=sand?90:43;}
            float low=0,deep=0,grain=0,phase=0,grainFrequency=900;
            float bodyFrequency=material==BallAudioMaterial.Metal?840:material==BallAudioMaterial.Glass?1730:material==BallAudioMaterial.Rubber?112:material==BallAudioMaterial.Stone?245:material==BallAudioMaterial.Energy?1180:330;
            float bodyDecay=material==BallAudioMaterial.Metal?24:material==BallAudioMaterial.Glass?27:material==BallAudioMaterial.Rubber?43:31;
            for(int i=0;i<samples.Length;i++)
            {
                float white=Noise(ref random),t=i/(float)SampleRate;
                low+=alpha*(white-low);deep+=.012f*(white-deep);
                float texture=low-deep,high=white-low,value;
                if(kind==2)
                {
                    // A short damped contact resonance is tactile; sustained oscillator
                    // fundamentals are deliberately absent from rolling and slipping.
                    float transient=(texture*.65f+high*.22f)*Mathf.Exp(-t*105);
                    float resonance=Mathf.Sin(2*Mathf.PI*bodyFrequency*t)*Mathf.Exp(-t*bodyDecay)*.42f;
                    resonance+=Mathf.Sin(2*Mathf.PI*bodyFrequency*2.37f*t)*Mathf.Exp(-t*(bodyDecay+21))*.16f;
                    if(sand||grass)resonance*=.11f;
                    value=(transient+resonance)*Mathf.Min(1,t*1800);
                    if(ice)value*=.7f;
                }
                else
                {
                    if(Unit(ref random)<grainRate/SampleRate)
                    {grain=.25f+Unit(ref random)*.75f;phase=Unit(ref random)*Mathf.PI*2;grainFrequency=480+Unit(ref random)*(material==BallAudioMaterial.Glass?2600:1500);}
                    grain*=kind==1?.971f:material==BallAudioMaterial.Rubber?.978f:.982f;
                    phase+=2*Mathf.PI*grainFrequency/SampleRate;
                    float microcontact=(texture*.8f+Mathf.Sin(phase)*.10f)*grain;
                    value=kind==1?texture*.45f+high*.095f+microcontact*.24f:texture*.66f+high*(material==BallAudioMaterial.Glass?.055f:.018f)+microcontact*.46f;
                    if(sand)value=texture*.77f+high*.07f+microcontact*.12f;
                    if(grass)value=(texture*.45f+high*.025f+microcontact*.16f)*.72f;
                    if(ice)value=texture*.35f+high*.035f+microcontact*.18f;
                    value*=Mathf.Min(1,Mathf.Min(i/110f,(samples.Length-1-i)/110f));
                }
                samples[i]=Mathf.Clamp(value,-.88f,.88f);
            }
            return samples;
        }
    }
}
