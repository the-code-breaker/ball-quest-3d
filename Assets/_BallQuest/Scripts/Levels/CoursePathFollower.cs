using UnityEngine;

namespace BallQuest
{
    [RequireComponent(typeof(BallController))]
    public sealed class CoursePathFollower : MonoBehaviour
    {
        public LevelDefinition level;
        BallController ball;
        int segment;
        float[] lengths;
        Vector3[] cachedPoints;
        float distance;
        public int Segment=>segment;
        public float Distance=>distance;
        public float TotalDistance=>lengths!=null&&lengths.Length>0?lengths[lengths.Length-1]:0;
        public float RemainingDistance=>Mathf.Max(0,TotalDistance-distance);
        public float Progress01=>TotalDistance>.01f?Mathf.Clamp01(distance/TotalDistance):0;
        void Awake()=>ball=GetComponent<BallController>();
        void FixedUpdate()
        {
            if(!ball||!level||level.routePoints==null||level.routePoints.Length<2||ball.ExternalControl||GetComponent<RaceCompetitor>())return;
            EnsureRoute();
            var points=level.routePoints;int best=segment;float bestDistance=float.PositiveInfinity;
            for(int i=Mathf.Max(0,segment-1);i<Mathf.Min(points.Length-1,segment+9);i++)
            {
                Vector3 a=points[i],d=points[i+1]-a;float t=Mathf.Clamp01(Vector3.Dot(ball.Body.position-a,d)/Mathf.Max(.001f,d.sqrMagnitude));
                float value=(a+d*t-ball.Body.position).sqrMagnitude;
                if(value<bestDistance){bestDistance=value;best=i;}
            }
            segment=best;
            Vector3 forward=points[segment+1]-points[segment];
            float fraction=Mathf.Clamp01(Vector3.Dot(ball.Body.position-points[segment],forward)/Mathf.Max(.001f,forward.sqrMagnitude));
            distance=lengths[segment]+forward.magnitude*fraction;
        }
        void EnsureRoute()
        {
            if(cachedPoints==level.routePoints)return;cachedPoints=level.routePoints;lengths=new float[cachedPoints.Length];
            for(int i=1;i<lengths.Length;i++)lengths[i]=lengths[i-1]+Vector3.Distance(cachedPoints[i-1],cachedPoints[i]);
            segment=0;distance=0;
        }
        public void ResetToNearest()
        {
            if(!ball||!level||level.routePoints==null||level.routePoints.Length<2)return;
            EnsureRoute();
            var points=level.routePoints;float bestDistance=float.PositiveInfinity;segment=0;float bestFraction=0;
            for(int i=0;i<points.Length-1;i++)
            {
                Vector3 delta=points[i+1]-points[i];float t=Mathf.Clamp01(Vector3.Dot(ball.Body.position-points[i],delta)/Mathf.Max(.001f,delta.sqrMagnitude));
                float value=(points[i]+delta*t-ball.Body.position).sqrMagnitude;
                if(value<bestDistance){bestDistance=value;segment=i;bestFraction=t;}
            }
            distance=lengths[segment]+Vector3.Distance(points[segment],points[segment+1])*bestFraction;
        }
    }
}
