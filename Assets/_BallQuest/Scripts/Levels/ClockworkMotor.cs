using UnityEngine;

namespace BallQuest
{
    public enum ClockworkMotionKind { Drawbridge, Disc, Wheel }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class ClockworkMotor : MonoBehaviour, ICourseResettable
    {
        public ClockworkMotionKind kind;
        public float degreesPerSecond = 22;
        public Vector3 axis = Vector3.up;
        public float Clock { get; private set; }
        public float Angle { get; private set; }
        public bool IsTraversable => kind != ClockworkMotionKind.Drawbridge ? kind != ClockworkMotionKind.Wheel || Mathf.Abs(Mathf.Sin(Angle * 2 * Mathf.Deg2Rad)) > .72f : Angle < 3 && Mathf.Repeat(Clock, 12) < 3;
        Rigidbody body;
        Vector3 origin;
        Quaternion orientation;
        void Awake()
        {
            body=GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            origin=transform.position;orientation=transform.rotation;
        }
        void FixedUpdate()
        {
            Clock+=Time.fixedDeltaTime;
            if(kind==ClockworkMotionKind.Drawbridge)
            {
                float phase=Mathf.Repeat(Clock,12);
                Angle=phase<5?0:phase<6?(phase-5)*78:phase<10?78:(12-phase)*39;
                body.MoveRotation(orientation*Quaternion.AngleAxis(-Angle,Vector3.right));
            }
            else {Angle=Clock*degreesPerSecond;body.MoveRotation(orientation*Quaternion.AngleAxis(Angle,axis.normalized));}
        }
        public bool WheelWindow(float secondsAhead)
        {
            float predicted=Angle+secondsAhead*degreesPerSecond;
            return Mathf.Abs(Mathf.Sin(predicted*2*Mathf.Deg2Rad))>.88f;
        }
        public void ResetCourseState()
        {
            if(!body)return;Clock=Angle=0;body.position=origin;body.rotation=orientation;
        }
    }

}
