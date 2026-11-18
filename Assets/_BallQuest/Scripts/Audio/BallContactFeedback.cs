using UnityEngine;
using UnityEngine.Rendering;

namespace BallQuest
{
    /// <summary>Bounded world-space contact animation, independent of ball pose and collision.</summary>
    [RequireComponent(typeof(BallController))]
    public sealed class BallContactFeedback:MonoBehaviour
    {
        const int Slots=6;
        // Start near the sphere footprint, then expand beyond it so a ground
        // contact remains visible from the automatic chase camera.
        const float InitialDiameter=.65f;
        readonly MeshRenderer[] rings=new MeshRenderer[Slots];
        readonly float[] ages=new float[Slots];
        readonly float[] sizes=new float[Slots];
        readonly Color[] colors=new Color[Slots];
        GameObject owner;
        Mesh ringMesh;
        Material ringMaterial,particleMaterial;
        MaterialPropertyBlock properties;
        ParticleSystem chips;
        BallController ball;
        int cursor;
        float nextContact;
        public int Capacity=>Slots;
        public int EmittedContacts{get;private set;}
        public int ActiveRings{get{int count=0;for(int i=0;i<Slots;i++)if(rings[i]&&rings[i].enabled)count++;return count;}}
        public SurfaceAudioKind LastSurface{get;private set;}
        public Vector3 LastContactPoint{get;private set;}
        public Vector3 LastContactNormal{get;private set;}
        public GameObject PresentationRoot=>owner;
        void Awake()
        {
            ball=GetComponent<BallController>();
            owner=new GameObject("Bounded ball contact presentation");
            if(GameBootstrap.Instance)owner.transform.SetParent(GameBootstrap.Instance.transform,false);
            ringMesh=PremiumPickupGeometry.Ring();
            var shader=Shader.Find("BallQuest/Reward Ring");
            ringMaterial=new Material(shader?shader:Shader.Find("Universal Render Pipeline/Unlit")){name="Soft material contact ripples",renderQueue=3000};
            properties=new MaterialPropertyBlock();
            for(int i=0;i<Slots;i++)
            {
                var go=new GameObject("Contact ripple "+i,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(owner.transform,false);go.layer=2;
                go.GetComponent<MeshFilter>().sharedMesh=ringMesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=ringMaterial;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;rings[i]=renderer;
            }
            var particles=new GameObject("Contact chips and sparks");particles.transform.SetParent(owner.transform,false);particles.layer=2;chips=particles.AddComponent<ParticleSystem>();
            chips.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=chips.main;main.loop=false;main.playOnAwake=false;main.duration=.3f;main.startLifetime=.2f;main.startSpeed=0;main.startSize=.045f;main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=chips.emission;emission.enabled=false;
            var shape=chips.shape;shape.enabled=false;
            var fade=chips.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            var size=chips.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.1f));
            var template=GameBootstrap.Instance?GameBootstrap.Instance.catalog.particleMaterial:null;
            particleMaterial=template?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            var output=chips.GetComponent<ParticleSystemRenderer>();output.sharedMaterial=particleMaterial;output.shadowCastingMode=ShadowCastingMode.Off;output.receiveShadows=false;
        }
        public bool Emit(Vector3 point,Vector3 normal,SurfaceAudioKind surface,float impulse)
        {
            if(!isActiveAndEnabled||!owner||!ball||!ball.active||Time.timeScale<=0||impulse<.45f||Time.time<nextContact||float.IsNaN(impulse)||float.IsInfinity(impulse))return false;
            nextContact=Time.time+.055f;
            normal=normal.sqrMagnitude>.001f?normal.normalized:ball.MovementUp;
            LastSurface=surface;LastContactPoint=point;LastContactNormal=normal;EmittedContacts++;
            int index=cursor++%Slots;ages[index]=0;sizes[index]=Mathf.Lerp(1.15f,2.15f,Mathf.Clamp01(impulse/14));colors[index]=Tint(surface);
            var ring=rings[index];ring.transform.SetPositionAndRotation(point+normal*.018f,Quaternion.LookRotation(normal));ring.transform.localScale=Vector3.one*InitialDiameter;ring.enabled=true;
            properties.SetColor("_BaseColor",colors[index]);ring.SetPropertyBlock(properties);
            if(surface!=SurfaceAudioKind.Glass&&surface!=SurfaceAudioKind.Ice&&surface!=SurfaceAudioKind.Boost&&surface!=SurfaceAudioKind.Metal)return true;
            chips.Play();
            Vector3 tangent=Vector3.Cross(normal,Mathf.Abs(Vector3.Dot(normal,Vector3.up))>.9f?Vector3.right:Vector3.up).normalized;
            Vector3 bitangent=Vector3.Cross(normal,tangent);int count=Mathf.Clamp(Mathf.CeilToInt(impulse*.45f),2,7);
            for(int i=0;i<count;i++)
            {
                float angle=(i*2.399963f+EmittedContacts*.71f);Vector3 direction=tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle);
                var emit=new ParticleSystem.EmitParams{position=point+normal*.035f,velocity=normal*(.7f+i*.08f)+direction*(.6f+i*.1f),startColor=colors[index],startSize=.035f+i*.003f,startLifetime=.14f+i*.012f};chips.Emit(emit,1);
            }
            return true;
        }
        static Color Tint(SurfaceAudioKind surface)
        {
            switch(surface)
            {
                case SurfaceAudioKind.Metal:return new Color(1,.78f,.32f,.82f);
                case SurfaceAudioKind.Ice:case SurfaceAudioKind.Glass:return new Color(.5f,.9f,1,.72f);
                case SurfaceAudioKind.Sand:return new Color(.86f,.66f,.38f,.55f);
                case SurfaceAudioKind.Grass:return new Color(.52f,.77f,.36f,.5f);
                case SurfaceAudioKind.Stone:return new Color(.75f,.79f,.86f,.65f);
                case SurfaceAudioKind.Boost:return new Color(.22f,.96f,1,.8f);
                default:return new Color(1,.83f,.57f,.62f);
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            if(collision.contactCount==0)return;var contact=collision.GetContact(0);
            if(Mathf.Abs(Vector3.Dot(collision.relativeVelocity,contact.normal))<.65f&&collision.impulse.magnitude<1.6f)return;
            Emit(contact.point,contact.normal,BallSurfaceTag.Identify(collision.collider),collision.impulse.magnitude);
        }
        void LateUpdate()
        {
            if(!owner)return;
            if(Time.timeScale<=0||!ball||!ball.active){Clear();return;}
            for(int i=0;i<Slots;i++)
            {
                if(!rings[i].enabled)continue;
                ages[i]+=Time.deltaTime;float t=ages[i]/.24f;
                if(t>=1){rings[i].enabled=false;continue;}
                rings[i].transform.localScale=Vector3.one*Mathf.Lerp(InitialDiameter,sizes[i],1-(1-t)*(1-t));
                Color color=colors[i];color.a*=1-t;properties.SetColor("_BaseColor",color);rings[i].SetPropertyBlock(properties);
            }
        }
        public void Clear(){for(int i=0;i<Slots;i++)if(rings[i])rings[i].enabled=false;if(chips)chips.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        void OnDisable()=>Clear();
        void OnDestroy(){if(owner)Destroy(owner);if(ringMesh)Destroy(ringMesh);if(ringMaterial)Destroy(ringMaterial);if(particleMaterial)Destroy(particleMaterial);}
    }
}
