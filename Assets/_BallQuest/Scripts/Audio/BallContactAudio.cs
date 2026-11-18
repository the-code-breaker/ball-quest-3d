using UnityEngine;

namespace BallQuest
{
    /// <summary>Contact-driven audio presentation; it never applies forces or changes physics tuning.</summary>
    [RequireComponent(typeof(BallController), typeof(Rigidbody))]
    public sealed class BallContactAudio : MonoBehaviour
    {
        public BallAudioProfile profile;
        [Range(0, 1)] public float masterVolume = .8f;
        public float RollingLevel { get; private set; }
        public float SlidingLevel { get; private set; }
        public float ContactSpeed { get; private set; }
        public float ContactSlipSpeed { get; private set; }
        public SurfaceAudioKind CurrentSurface { get; private set; }
        public BallAudioMaterial CurrentMaterial => profile ? profile.material : BallAudioMaterial.Wood;
        public int PlayedImpactCount { get; private set; }
        public int LoopStartCount { get; private set; }
        public float RollingPlaybackPitch { get; private set; }=1;
        public SurfaceAudioKind LastImpactSurface { get; private set; }
        public AudioClip LastImpactClip { get; private set; }
        public BallContactFeedback Feedback { get; private set; }
        public int ActiveImpactVoices
        {
            get { int count = 0; for (int i = 0; i < impactEnds.Length; i++) if (impactEnds[i] > Time.time) count++; return count; }
        }
        BallController ball;
        readonly AudioSource[] rolling = new AudioSource[2];
        readonly bool[] rollingStarted = new bool[2];
        AudioSource sliding;
        bool slidingStarted;
        readonly AudioSource[] impacts = new AudioSource[4];
        readonly float[] impactEnds = new float[4];
        Collider lastSupport;
        BallSurfaceTag tag;
        SurfaceAudioResponse response;
        BallAudioProfile lastProfile;
        int slot;
        float fade = 1, nextImpact, surfaceGain = 1, surfacePitch = 1;

        void Awake()
        {
            ball = GetComponent<BallController>();
            Feedback=GetComponent<BallContactFeedback>();if(!Feedback)Feedback=gameObject.AddComponent<BallContactFeedback>();
            for (int i = 0; i < rolling.Length; i++) rolling[i] = Source(true);
            sliding = Source(true);
            for (int i = 0; i < impacts.Length; i++) impacts[i] = Source(false);
            if (!profile) SetMaterial(BallAudioProfile.ResolveMaterial(ball.profile, null));
        }
        AudioSource Source(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = loop;
            source.spatialBlend = 0; source.dopplerLevel = 0; source.priority = loop ? 150 : 100; source.volume = 0;
            return source;
        }
        public void Configure(BallSkin skin, float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            SetMaterial(BallAudioProfile.ResolveMaterial(ball ? ball.profile : null, skin));
        }
        public void SetMaterial(BallAudioMaterial material) { profile = TemporaryBallAudio.Profile(material); lastSupport = null; }

        void Update()
        {
            if (!ball || !ball.Body || !ball.active || !ball.Grounded || Time.timeScale == 0 || masterVolume <= 0)
            {
                ContactSpeed = ContactSlipSpeed = RollingLevel = SlidingLevel = 0; StopLoops();
                if (!ball || !ball.active || Time.timeScale == 0 || masterVolume <= 0) StopImpacts();
                return;
            }
            var support = ball.GroundDetector.SupportCollider;
            if (support != lastSupport || lastProfile != profile || (tag && tag.surface != CurrentSurface))
            {
                lastSupport = support; lastProfile = profile;
                tag = support ? support.GetComponentInParent<BallSurfaceTag>() : null;
                CurrentSurface = BallSurfaceTag.Identify(support);
                response = profile ? profile.Response(CurrentSurface) : null;
                ChangeClips();
            }
            if (!profile) return;
            Vector3 relative = ball.Body.linearVelocity - ball.SupportVelocity;
            Vector3 tangent = Vector3.ProjectOnPlane(relative, ball.GroundNormal);
            ContactSpeed = tangent.magnitude;
            Vector3 arm = -ball.GroundNormal * ball.GroundDetector.Radius;
            ContactSlipSpeed = Vector3.ProjectOnPlane(relative + Vector3.Cross(ball.Body.angularVelocity, arm), ball.GroundNormal).magnitude;
            float responseTime = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(.01f, profile.surfaceCrossfade));
            surfaceGain = Mathf.Lerp(surfaceGain, response != null ? response.rollingGain : 1, responseTime);
            surfacePitch = Mathf.Lerp(surfacePitch, (response != null ? response.pitch : 1) * (tag ? tag.pitch : 1), responseTime);
            float level = Mathf.Sqrt(Mathf.Clamp01((ContactSpeed - profile.minimumRollingSpeed) / Mathf.Max(.1f, profile.referenceSpeed)));
            RollingLevel = level * profile.rollingVolume * surfaceGain * (tag ? tag.rollingGain : 1) * masterVolume;
            SlidingLevel = Mathf.Clamp01((ContactSlipSpeed - profile.minimumSlipSpeed) / 8) * profile.slidingVolume * (response != null ? response.slidingGain : 1) * masterVolume;
            fade = Mathf.MoveTowards(fade, 1, Time.deltaTime / Mathf.Max(.01f, profile.surfaceCrossfade));
            for (int i = 0; i < 2; i++)
            {
                rolling[i].volume = RollingLevel * (i == slot ? fade : 1 - fade);
                // Speed changes presence and grain energy, rather than revving a pitched motor.
                RollingPlaybackPitch=Mathf.Clamp((.98f+level*.05f)*surfacePitch,.88f,1.14f);
                rolling[i].pitch = RollingPlaybackPitch;
                Loop(rolling[i], ref rollingStarted[i], rolling[i].volume);
            }
            sliding.volume = SlidingLevel; sliding.pitch = Mathf.Clamp(.97f + ContactSlipSpeed * .003f, .94f, 1.1f);
            Loop(sliding, ref slidingStarted, SlidingLevel);
        }
        void ChangeClips()
        {
            if (!profile) return;
            for (int i = 0; i < rolling.Length; i++) rolling[i].outputAudioMixerGroup = profile.outputGroup;
            for (int i = 0; i < impacts.Length; i++) impacts[i].outputAudioMixerGroup = profile.outputGroup;
            sliding.outputAudioMixerGroup = profile.outputGroup;
            AudioClip clip = response != null && response.rollingOverride ? response.rollingOverride : profile.rolling;
            if (rolling[slot].clip != clip)
            {
                slot = 1 - slot; rolling[slot].Stop(); rollingStarted[slot] = false;
                rolling[slot].clip = clip; fade = 0;
            }
            AudioClip slip = response != null && response.slidingOverride ? response.slidingOverride : profile.sliding;
            if (sliding.clip != slip) { sliding.Stop(); slidingStarted = false; sliding.clip = slip; }
        }
        void Loop(AudioSource source, ref bool started, float level)
        {
            if (source.clip && level > .0001f)
            {
                if (!started) { source.Play(); started = true; LoopStartCount++; }
            }
            else if (started) { source.Stop(); started = false; }
        }
        void StopLoops()
        {
            for (int i = 0; i < 2; i++) { if (rolling[i]) { rolling[i].Stop(); rolling[i].volume = 0; } rollingStarted[i] = false; }
            if (sliding) { sliding.Stop(); sliding.volume = 0; } slidingStarted = false;
        }
        void StopImpacts()
        {
            for (int i = 0; i < impacts.Length; i++)
            {
                if (impacts[i]) { impacts[i].Stop(); impacts[i].volume = 0; }
                impactEnds[i] = 0;
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            var point=collision.contactCount>0?collision.GetContact(0):default;
            // Adjacent road mesh contacts are not new impacts. A genuine landing
            // or wall strike needs normal approach speed, avoiding seam chatter.
            if(collision.contactCount>0&&Mathf.Abs(Vector3.Dot(collision.relativeVelocity,point.normal))<.65f&&collision.impulse.magnitude<1.6f)return;
            ReportImpactAt(collision.impulse.magnitude,collision.collider,collision.contactCount>0?point.point:ball.Body.position,
                collision.contactCount>0?point.normal:ball.MovementUp);
        }
        public bool ReportImpact(float impulse)
            =>ReportImpactAt(impulse,lastSupport,ball?ball.Body.position-ball.GroundNormal*ball.GroundDetector.Radius:transform.position,ball?ball.GroundNormal:Vector3.up);
        public bool ReportImpactAt(float impulse,Collider struckSurface,Vector3 point,Vector3 normal)
        {
            if (!profile || !ball || !ball.active || masterVolume <= 0 || Time.timeScale == 0 || impulse < .45f || Time.time < nextImpact || float.IsNaN(impulse) || float.IsInfinity(impulse)) return false;
            var impactSurface=BallSurfaceTag.Identify(struckSurface);
            var impactResponse=profile.Response(impactSurface);
            var clip = impactResponse != null && impactResponse.impactOverride ? impactResponse.impactOverride : profile.impact;
            if (!clip) return false;
            nextImpact = Time.time + Mathf.Max(.025f, profile.impactCooldown);
            int index = 0;
            for (int i = 0; i < impacts.Length; i++) if (impactEnds[i] < impactEnds[index]) index = i;
            var source = impacts[index]; source.Stop(); source.clip = clip;
            source.volume = Mathf.Clamp01(impulse / Mathf.Max(.1f, profile.referenceImpactImpulse)) * profile.impactVolume * (impactResponse != null ? impactResponse.impactGain : 1) * masterVolume;
            source.pitch = (CurrentMaterial == BallAudioMaterial.Glass ? 1.05f : CurrentMaterial == BallAudioMaterial.Stone ? .94f : 1);
            source.Play(); impactEnds[index] = Time.time + clip.length / source.pitch;
            LastImpactSurface=impactSurface;LastImpactClip=clip;
            if(Feedback)Feedback.Emit(point,normal,impactSurface,impulse);
            PlayedImpactCount++; return true;
        }
        void OnDisable()
        {
            StopLoops(); RollingLevel = SlidingLevel = 0;
            StopImpacts();
            if(Feedback)Feedback.Clear();
        }
    }
}
