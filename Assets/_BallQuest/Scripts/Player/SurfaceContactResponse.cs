using UnityEngine;

namespace BallQuest
{
    // Physical material response remains active independently of audio and volume preferences.
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(BallController))]
    public sealed class SurfaceContactResponse : MonoBehaviour
    {
        BallController ball;
        Collider support;
        BallSurfaceTag tag;
        void Awake()=>ball=GetComponent<BallController>();
        void FixedUpdate()
        {
            var next=ball.GroundDetector.SupportCollider;
            if(next!=support){support=next;tag=support?support.GetComponentInParent<BallSurfaceTag>():null;}
            ball.SurfaceResistanceMultiplier=tag?tag.rollingResistanceMultiplier:1;
        }
    }
}
