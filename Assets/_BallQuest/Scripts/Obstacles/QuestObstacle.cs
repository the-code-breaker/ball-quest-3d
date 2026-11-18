using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class QuestObstacle : MonoBehaviour, ICourseResettable
    {
        public ObstacleKind kind;
        public Vector3 axis=Vector3.right;
        public float amplitude=2, speed=1, phase;
        public float warningSeconds=1.1f;
        Rigidbody body;
        Vector3 origin;
        Quaternion rotation;
        float clock,collapseAt=-1;
        Renderer[] visuals;
        Vector3[] visualOrigins;
        float gateLift;
        Collider[] solids;
        MaterialPropertyBlock block;
        bool open=true,fallen;
        PremiumBoosterVisuals padPresentation;
        public bool IsPassable=>open&&(kind==ObstacleKind.TimedDoor||kind==ObstacleKind.Laser);
        public bool IsPlatform=>kind==ObstacleKind.MovingBridge||kind==ObstacleKind.Elevator||kind==ObstacleKind.ForwardPlatform||kind==ObstacleKind.CircularPlatform||kind==ObstacleKind.FallingTile||kind==ObstacleKind.CollapsingBridge;
        public Vector3 Origin=>origin;
        public Vector3 Velocity=>body?body.linearVelocity:Vector3.zero;
        public bool Collapsing=>collapseAt>=0;

        void Awake()
        {
            body=GetComponent<Rigidbody>();body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            origin=transform.position;rotation=transform.rotation;
            visuals=GetComponentsInChildren<Renderer>();
            visualOrigins=new Vector3[visuals.Length];for(int i=0;i<visuals.Length;i++)visualOrigins[i]=visuals[i].transform.localPosition;
            var list=new List<Collider>();foreach(var value in GetComponentsInChildren<Collider>())if(value.enabled&&!value.isTrigger)list.Add(value);solids=list.ToArray();
            block=new MaterialPropertyBlock();
            padPresentation=GetComponent<PremiumBoosterVisuals>();
        }
        void Start()=>ResetCourseState();
        public Vector3 PredictPosition(float seconds)
        {
            float wave=Mathf.Sin((clock+seconds)*speed+phase);
            switch(kind)
            {
                case ObstacleKind.MovingBridge:case ObstacleKind.SlidingWall:case ObstacleKind.Elevator:
                case ObstacleKind.ForwardPlatform:case ObstacleKind.PushBlock:case ObstacleKind.CrushingWall:case ObstacleKind.Piston:
                    return origin+SafeAxis()*(wave*amplitude);
                case ObstacleKind.CircularPlatform:
                    return origin+new Vector3(Mathf.Sin((clock+seconds)*speed+phase)*amplitude,0,(Mathf.Cos((clock+seconds)*speed+phase)-Mathf.Cos(phase))*amplitude*.45f);
                default:return body?body.position:origin;
            }
        }
        Vector3 SafeAxis()=>axis.sqrMagnitude>.01f?axis.normalized:Vector3.right;
        void FixedUpdate()
        {
            clock+=Time.fixedDeltaTime;float wave=Mathf.Sin(clock*speed+phase);
            switch(kind)
            {
                case ObstacleKind.Hammer:
                    body.MoveRotation(rotation*Quaternion.AngleAxis(clock*speed*42,Vector3.up));break;
                case ObstacleKind.SweepArm:
                    body.MoveRotation(rotation*Quaternion.AngleAxis(wave*amplitude*38,Vector3.up));break;
                case ObstacleKind.RollerDrum:
                    body.MoveRotation(rotation*Quaternion.AngleAxis(clock*speed*50,Vector3.right));break;
                case ObstacleKind.Pendulum:
                    body.MoveRotation(rotation*Quaternion.AngleAxis(wave*amplitude*18,SafeAxis()));break;
                case ObstacleKind.VerticalRotor:case ObstacleKind.RotatingBlade:
                    body.MoveRotation(rotation*Quaternion.AngleAxis(clock*speed*38,SafeAxis()));break;
                case ObstacleKind.MovingBridge:case ObstacleKind.SlidingWall:case ObstacleKind.Elevator:
                case ObstacleKind.ForwardPlatform:case ObstacleKind.PushBlock:case ObstacleKind.CrushingWall:case ObstacleKind.CircularPlatform:case ObstacleKind.Piston:
                    body.MovePosition(PredictPosition(0));break;
                case ObstacleKind.FallingTile:case ObstacleKind.CollapsingBridge:
                    if(collapseAt>=0&&!fallen)
                    {
                        float remaining=collapseAt-clock;
                        if(remaining>0)Tint(Mathf.Repeat(remaining*6,1)>.5f?new Color(1,.68f,.1f):new Color(1,.25f,.05f));
                        else {fallen=true;body.isKinematic=false;body.useGravity=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;}
                    }
                    break;
                case ObstacleKind.Laser:case ObstacleKind.TimedDoor:
                    float cycle=Mathf.Repeat(clock*speed+phase,6);
                    open=cycle<3.8f;
                    foreach(var solid in solids)if(solid)solid.enabled=!open;
                    Tint(cycle<3.25f?new Color(.12f,.84f,.4f):cycle<3.8f?new Color(1,.66f,.05f):new Color(.94f,.06f,.08f));
                    PoseGate(cycle,false);
                    break;
                case ObstacleKind.RollingHazard:
                    Vector3 target=origin+SafeAxis()*(wave*amplitude);
                    Vector3 error=Vector3.ProjectOnPlane(target-body.position,Vector3.up);
                    Vector3 horizontal=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up);
                    body.AddForce(Vector3.ClampMagnitude(error*8-horizontal*3,9),ForceMode.Acceleration);
                    if(body.position.y<origin.y-8)ResetCourseState();
                    break;
            }
        }
        void Tint(Color color)
        {
            block.SetColor("_BaseColor",color);foreach(var visual in visuals)if(visual)visual.SetPropertyBlock(block);
        }
        void PoseGate(float cycle,bool snap)
        {
            if(kind!=ObstacleKind.TimedDoor&&kind!=ObstacleKind.Laser)return;
            float fraction=cycle<3.25f?1:cycle<3.8f?1-Mathf.InverseLerp(3.25f,3.8f,cycle):0;
            float target=fraction*3.6f;gateLift=snap?target:Mathf.MoveTowards(gateLift,target,Time.fixedDeltaTime*11);
            for(int i=0;i<visuals.Length;i++)
            {
                if(!visuals[i])continue;
                if(kind==ObstacleKind.Laser)visuals[i].enabled=cycle>=3.25f;
                // Never move the rigidbody/root or its physical collider for a cosmetic lift.
                else if(visuals[i].transform!=transform)visuals[i].transform.localPosition=visualOrigins[i]+Vector3.up*gateLift;
            }
        }
        void OnCollisionEnter(Collision collision)=>Contact(collision);
        void OnCollisionStay(Collision collision)=>Contact(collision);
        void Contact(Collision collision)
        {
            if(!collision.rigidbody||!collision.rigidbody.TryGetComponent<BallController>(out _))return;
            if((kind==ObstacleKind.FallingTile||kind==ObstacleKind.CollapsingBridge)&&collapseAt<0)collapseAt=clock+Mathf.Max(.65f,warningSeconds);
        }
        void OnTriggerStay(Collider other)
        {
            if(!other.TryGetComponent<BallController>(out var ball)||!ball.active)return;
            if(kind==ObstacleKind.Conveyor)ball.Body.AddForce(SafeAxis()*(amplitude*4),ForceMode.Acceleration);
        }
        void OnTriggerEnter(Collider other)
        {
            if(!other.TryGetComponent<BallController>(out var ball)||!ball.active)return;
            if(kind==ObstacleKind.Booster)
            {
                ball.Boost(SafeAxis());
                if(padPresentation)padPresentation.Activate(ball);
                if(GameBootstrap.Instance&&GameBootstrap.Instance.Session.Ball==ball)
                {GameBootstrap.Instance.Session.RegisterBoost();GameBootstrap.Instance.Session.Announce("TURBO! Ride the rush","boost");}
            }
            else if(kind==ObstacleKind.JumpPad)
            {
                ball.Body.AddForce(Vector3.up*Mathf.Max(0,amplitude*2-ball.Body.linearVelocity.y),ForceMode.VelocityChange);
                if(padPresentation)padPresentation.Activate(ball);
            }
        }
        public void ResetCourseState()
        {
            if(!body)body=GetComponent<Rigidbody>();
            if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            body.isKinematic=true;body.useGravity=false;body.position=origin;body.rotation=rotation;
            clock=0;collapseAt=-1;fallen=false;open=true;
            foreach(var solid in solids)if(solid)solid.enabled=true;
            foreach(var visual in visuals)if(visual)visual.SetPropertyBlock(null);
            if(padPresentation)padPresentation.ResetVisual();
            if(kind==ObstacleKind.RollingHazard)
            {body.isKinematic=false;body.useGravity=true;body.mass=2.5f;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;}
            else if(kind==ObstacleKind.MovingBridge||kind==ObstacleKind.Elevator||kind==ObstacleKind.ForwardPlatform||kind==ObstacleKind.SlidingWall||kind==ObstacleKind.PushBlock||kind==ObstacleKind.CrushingWall||kind==ObstacleKind.CircularPlatform||kind==ObstacleKind.Piston)
                body.position=PredictPosition(0);
            else if(kind==ObstacleKind.TimedDoor||kind==ObstacleKind.Laser)
            {float cycle=Mathf.Repeat(phase,6);open=cycle<3.8f;foreach(var solid in solids)if(solid)solid.enabled=!open;PoseGate(cycle,true);}
        }
    }
}
