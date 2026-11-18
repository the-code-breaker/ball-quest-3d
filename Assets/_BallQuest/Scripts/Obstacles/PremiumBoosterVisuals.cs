using UnityEngine;

namespace BallQuest
{
    /// <summary>Visual-only accelerator state, updated centrally with course collectibles.</summary>
    public sealed class PremiumBoosterVisuals : MonoBehaviour
    {
        Renderer lights;
        Transform assembly;
        Vector3 restPosition;
        MaterialPropertyBlock properties;
        float pulse;
        public float Activation=>pulse;
        public ObstacleKind Kind {get;private set;}
        internal void Initialize(Renderer renderer,Transform model,ObstacleKind kind)
        {
            lights=renderer;assembly=model;restPosition=model.localPosition;Kind=kind;
            properties=new MaterialPropertyBlock();
            properties.SetFloat("_FlowType",kind==ObstacleKind.JumpPad?1:kind==ObstacleKind.Conveyor?2:0);
            if(kind==ObstacleKind.Conveyor)properties.SetColor("_BaseColor",new Color(.13f,1.5f,.42f));
            Apply();
        }
        public static PremiumBoosterVisuals Attach(Transform padRoot,ObstacleSpec spec,GameCatalog catalog)
        {
            var presentation=padRoot.gameObject.AddComponent<PremiumBoosterVisuals>();
            PremiumCollectibleVisuals.Library(padRoot,catalog).CreatePad(presentation,spec.size,spec.kind);
            return presentation;
        }
        public void Activate(BallController ball)
        {
            pulse=1;Apply();
            var app=GameBootstrap.Instance;
            if(app&&app.Effects&&ball)app.Effects.Boost(ball.transform.position,ball.MovementUp);
        }
        internal void Tick(float dt)
        {
            if(pulse<=0)return;pulse=Mathf.Max(0,pulse-dt*2.5f);Apply();
        }
        void Apply()
        {
            if(!lights)return;properties.SetFloat("_Pulse",pulse);lights.SetPropertyBlock(properties);
            if(assembly)assembly.localPosition=restPosition+Vector3.up*(Mathf.Sin((1-pulse)*Mathf.PI)*.014f);
        }
        public void ResetVisual(){pulse=0;Apply();}
    }
}
