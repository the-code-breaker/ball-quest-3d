using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    public sealed class QuestAudio : MonoBehaviour
    {
        AudioSource music;
        AudioSource sfx;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        float sfxVolume = .8f;
        public BallController ball;
        BallController observed;
        BallContactAudio contact;
        public BallContactAudio ContactAudio => contact;
        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>(); music.loop = true;
            sfx = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = sfx.playOnAwake = false;
            clips["coin"] = Tone("Coin", 880, 0.1f, 1320);
            clips["jump"] = Tone("Jump", 320, 0.18f, 700);
            clips["land"] = Tone("Land", 90, 0.12f, 45);
            clips["checkpoint"] = Tone("Checkpoint", 520, 0.3f, 1040);
            clips["victory"] = Melody();
            clips["fail"] = Tone("Fall", 280, 0.4f, 70);
            clips["click"] = Tone("Click", 650, 0.055f, 500);
            clips["boost"] = BoostWhoosh();
            var bootstrap = GameBootstrap.Instance;
            music.clip = bootstrap && bootstrap.catalog ? bootstrap.catalog.soundtrack : null;
            if (music.clip) music.Play();
        }
        public void Apply(GameSettings settings)
        {
            if (settings == null) return;
            music.volume = settings.music * 0.65f; sfxVolume = settings.sfx;
            if (contact) contact.masterVolume = sfxVolume;
        }
        public void Play(string id) { if (clips.TryGetValue(id, out var clip)) sfx.PlayOneShot(clip, sfxVolume * 0.5f); }
        void Update()
        {
            if (observed != ball) BindBall(ball);
        }
        public void BindBall(BallController value, BallSkin skin = null)
        {
            if (contact) contact.enabled = false;
            ball = observed = value; contact = null;
            if (!value) return;
            if (!skin)
            {
                var bootstrap = GameBootstrap.Instance;
                if (bootstrap && bootstrap.catalog && bootstrap.Save != null)
                    skin = System.Array.Find(bootstrap.catalog.skins, entry => entry && entry.id == bootstrap.Save.Data.selectedSkin);
            }
            contact = value.GetComponent<BallContactAudio>();
            if (!contact) contact = value.gameObject.AddComponent<BallContactAudio>();
            contact.Configure(skin, sfxVolume); contact.enabled = true;
        }
        static AudioClip BoostWhoosh()
        {
            const int rate=22050,count=5292;var samples=new float[count];uint seed=94729;float low=0;
            for(int i=0;i<count;i++)
            {seed=seed*1664525u+1013904223u;float noise=(seed>>8)/8388607.5f-1,t=i/(float)count;low=Mathf.Lerp(low,noise,.14f);float envelope=Mathf.Sin(Mathf.PI*t)*Mathf.Exp(-t*2.8f);samples[i]=(noise*.12f+low*.72f)*envelope*.42f;}
            var clip=AudioClip.Create("Airflow accelerator whoosh",count,1,rate,false);clip.SetData(samples,0);return clip;
        }
        static AudioClip Tone(string name, float from, float duration, float to, bool loop = false)
        {
            const int rate = 22050; var samples = new float[Mathf.CeilToInt(rate * duration)];
            double phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += Mathf.Lerp(from, to, t) * 2 * Mathf.PI / rate;
                float envelope = loop ? 0.25f : Mathf.Sin(t * Mathf.PI) * (1 - t);
                samples[i] = (float)System.Math.Sin(phase) * envelope * 0.35f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        static AudioClip Melody(bool background = false)
        {
            const int rate = 22050;
            float[] notes = background ? new[] { 261.63f, 329.63f, 392f, 329.63f, 293.66f, 349.23f, 440f, 392f, 261.63f, 329.63f, 523.25f, 392f, 349.23f, 329.63f, 293.66f, 392f } : new[] { 523.25f, 659.25f, 783.99f, 1046.5f };
            float step = background ? 0.5f : 0.18f;
            var data = new float[Mathf.CeilToInt(notes.Length * step * rate)];
            for (int i = 0; i < data.Length; i++)
            {
                int note = Mathf.Min(notes.Length - 1, (int)(i / (step * rate)));
                float t = (i / (float)rate) % step;
                data[i] = Mathf.Sin(2 * Mathf.PI * notes[note] * t) * Mathf.Exp(-t * 7) * Mathf.Min(1, t * 100) * 0.15f;
            }
            var clip = AudioClip.Create(background ? "Temporary sky melody" : "Victory", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        void OnDestroy() { foreach (var clip in clips.Values) Destroy(clip); }
    }
}
