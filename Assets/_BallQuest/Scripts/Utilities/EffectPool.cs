using UnityEngine;

namespace BallQuest
{
    public sealed class EffectPool : MonoBehaviour
    {
        readonly ParticleSystem[] pool = new ParticleSystem[12];
        readonly Renderer[] rings=new Renderer[12];
        readonly float[] ringAge=new float[12];
        readonly float[] ringScale=new float[12];
        readonly Color[] ringColor=new Color[12];
        int cursor;
        int ringCursor;
        Material material,ringMaterial;
        Mesh ringMesh;
        MaterialPropertyBlock ringProperties;
        public int Capacity=>pool.Length;
        public int RingCapacity=>rings.Length;
        void Awake()
        {
            var template = GameBootstrap.Instance ? GameBootstrap.Instance.catalog.particleMaterial : null;
            material = template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("Pooled spark " + i); go.transform.SetParent(transform);
                var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.loop = false; main.playOnAwake = false; main.duration = 0.7f;
                main.startLifetime = 0.7f; main.startSpeed = 3; main.startSize = 0.14f; main.gravityModifier = 0.5f; main.maxParticles = 80;
                main.useUnscaledTime = true;
                main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission = ps.emission; emission.enabled = false;
                var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.3f;
                var fade=ps.colorOverLifetime;fade.enabled=true;
                var colors=new Gradient();colors.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.25f),new GradientAlphaKey(0,1)});fade.color=colors;
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,.05f));
                var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                pool[i] = ps;
            }
            ringMesh=PremiumPickupGeometry.Ring();
            ringMaterial=new Material(Shader.Find("BallQuest/Reward Ring")){name="Pooled soft reward rings",renderQueue=3000};
            ringProperties=new MaterialPropertyBlock();
            for(int i=0;i<rings.Length;i++)
            {
                var go=new GameObject("Pooled reward ring "+i,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.layer=2;
                go.GetComponent<MeshFilter>().sharedMesh=ringMesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=ringMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;rings[i]=renderer;
            }
        }
        public void Burst(Vector3 position, Color color, int count)
        {
            var ps = pool[cursor++ % pool.Length];
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.transform.position = position;var main=ps.main;main.startSpeed=3;ps.Play();
            var emit = new ParticleSystem.EmitParams { startColor = color }; ps.Emit(emit, Mathf.Clamp(count,0,80));
        }
        public void Pickup(Vector3 position,Color color)
        {
            var ps=pool[cursor++%pool.Length];ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.transform.position=position;var main=ps.main;main.startSpeed=1.65f;ps.Play();
            var emit=new ParticleSystem.EmitParams{startColor=color,startLifetime=.36f,startSize=.085f};ps.Emit(emit,7);
            var camera=Camera.main;Ring(position,color,camera?camera.transform.forward:Vector3.forward,1.3f);
        }
        public void Boost(Vector3 position,Vector3 up)
        {
            Burst(position,new Color(.14f,.92f,1,.9f),12);
            Ring(position,new Color(.15f,.95f,1),up,3.2f);
        }
        void Ring(Vector3 position,Color color,Vector3 normal,float size)
        {
            int index=ringCursor++%rings.Length;var renderer=rings[index];renderer.enabled=true;
            renderer.transform.SetPositionAndRotation(position,Quaternion.LookRotation(normal.sqrMagnitude>.001f?normal:Vector3.forward));
            renderer.transform.localScale=Vector3.one*.2f;ringAge[index]=0;ringColor[index]=color;ringScale[index]=size;
            ringProperties.SetColor("_BaseColor",color);renderer.SetPropertyBlock(ringProperties);
        }
        void LateUpdate()
        {
            for(int i=0;i<rings.Length;i++)
            {
                var renderer=rings[i];if(!renderer||!renderer.enabled)continue;
                ringAge[i]+=Time.unscaledDeltaTime;float t=ringAge[i]/.42f;
                if(t>=1){renderer.enabled=false;continue;}
                renderer.transform.localScale=Vector3.one*Mathf.Lerp(.2f,ringScale[i],1-(1-t)*(1-t));
                Color color=ringColor[i];color.a=(1-t)*(1-t)*.8f;ringProperties.SetColor("_BaseColor",color);renderer.SetPropertyBlock(ringProperties);
            }
        }
        void OnDestroy() { if (material) Destroy(material);if(ringMaterial)Destroy(ringMaterial);if(ringMesh)Destroy(ringMesh); }
    }
}
