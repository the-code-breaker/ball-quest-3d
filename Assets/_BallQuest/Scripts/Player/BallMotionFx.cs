using UnityEngine;

namespace BallQuest
{
    public sealed class BallMotionFx : MonoBehaviour
    {
        public TrailRenderer trail;
        public bool skinTrail;
        BallController ball;
        Color skinColor;
        void Start() {ball=GetComponent<BallController>();skinColor=trail.startColor;}
        void LateUpdate()
        {
            bool turbo=ball&&ball.active&&ball.Boosted;
            trail.time=turbo?.22f:skinTrail?.25f:0;
            trail.startWidth=turbo?.18f:.12f;
            var color=turbo?new Color(1,.68f,.24f,.7f):skinColor;
            trail.startColor=color;trail.endColor=new Color(color.r,color.g,color.b,0);
            trail.emitting=ball&&ball.active&&(turbo||skinTrail);
            if(!ball||!ball.active||ball.Body.linearVelocity.sqrMagnitude<.05f)trail.Clear();
        }
    }
}
