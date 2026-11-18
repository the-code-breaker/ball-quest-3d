using UnityEngine;

namespace BallQuest
{
    /// <summary>Decision-making only. Every movement command is executed by the same BallController as human input.</summary>
    [DefaultExecutionOrder(-30)]
    public sealed class RaceDriver : MonoBehaviour
    {
        RaceManager manager;
        RaceCompetitor competitor;
        BallController ball;
        RaceDifficulty difficulty;
        float decisionClock;
        float plannedSpeed;
        float plannedLane;
        int avoidingHazard=-1;
        float preferredLane;
        float committedLane;
        [Header("Force-controller planning")]
        [Min(.1f)] public float lateralPositionGain=1.4f;
        [Min(.1f)] public float lateralVelocityGain=4.5f;
        [Min(.1f)] public float forwardVelocityGain=4;
        [Min(.1f)] public float maximumLaneSpeed=5;
        public bool WaitingForHazard {get;private set;}
        public float PlannedSpeed=>plannedSpeed;
        public float PlannedLane=>plannedLane;
        public BallControlCommand LastCommand {get;private set;}

        public void Initialize(RaceManager race,RaceCompetitor value,RaceDifficulty tier)
        {
            manager=race;competitor=value;ball=value.Ball;difficulty=tier;
            preferredLane=((value.Id%3)-1)*1.65f;
            ball.SetExternalControl(true);value.Driver=this;decisionClock=1;
        }

        void FixedUpdate()
        {
            if(!manager||!ball||manager.State!=RaceState.Racing||competitor.Finished||competitor.Eliminated||competitor.Recovering)
            {
                if(ball)ball.SubmitControl(new BallControlCommand(0,0,1));
                return;
            }
            decisionClock+=Time.fixedDeltaTime;
            float reaction=difficulty==RaceDifficulty.Easy?.13f:difficulty==RaceDifficulty.Medium?.085f:.045f;
            if(decisionClock>=reaction){decisionClock=0;Plan();}
            competitor.Cursor.SampleAhead(0,out var tangent,out _);
            var forward=Vector3.ProjectOnPlane(tangent,ball.MovementUp).normalized;
            if(forward.sqrMagnitude<.1f)forward=ball.PathForward;
            ball.SetPathForward(forward);
            forward=Vector3.ProjectOnPlane(ball.PathForward,ball.GroundNormal).normalized;
            var relativeVelocity=ball.Body.linearVelocity-ball.SupportVelocity;
            float speed=Vector3.Dot(relativeVelocity,forward);
            float lookAhead=Mathf.Clamp(Mathf.Abs(speed)*.28f,2.6f,6);
            var target=competitor.Cursor.SampleAhead(lookAhead,out _,out _);
            var right=Vector3.Cross(ball.GroundNormal,forward).normalized;
            target+=right*plannedLane;
            float error=Vector3.Dot(target-ball.Body.position,right);
            float lateral=Vector3.Dot(relativeVelocity,right);
            float acceleration=Mathf.Max(.1f,ball.profile.acceleration/ball.Body.mass);
            Vector3 compensation=ResistanceCompensation(relativeVelocity);
            float desiredLateral=Mathf.Clamp(error*lateralPositionGain,-maximumLaneSpeed,maximumLaneSpeed);
            float steering=Mathf.Clamp(((desiredLateral-lateral)*lateralVelocityGain+Vector3.Dot(compensation,right))/acceleration,-1,1);
            float throttle=0,brake=0;
            float forwardAcceleration=(plannedSpeed-speed)*forwardVelocityGain+Vector3.Dot(compensation,forward);
            if(forwardAcceleration<0)brake=Mathf.Clamp01(-forwardAcceleration/Mathf.Max(1,ball.profile.braking));
            else
            {
                float ceiling=difficulty==RaceDifficulty.Easy?.75f:difficulty==RaceDifficulty.Medium?.9f:1;
                // Reserve the shared unit-circle command budget for steering rather than silently scaling it away.
                float available=Mathf.Sqrt(Mathf.Max(0,1-steering*steering));
                throttle=Mathf.Min(ceiling,Mathf.Min(available,forwardAcceleration/acceleration));
            }
            LastCommand=new BallControlCommand(throttle,steering,brake);
            ball.SubmitControl(LastCommand);
        }

        void Plan()
        {
            var route=manager.Route;var position=ball.Body.position;
            if(difficulty==RaceDifficulty.Hard&&competitor.Id%2==0&&competitor.Cursor.ChosenAlternative==null)
            {
                for(int option=0;option<route.Alternatives.Length;option++)
                {
                    var alternative=route.Alternatives[option];float before=alternative.EntryDistance-competitor.Distance;
                    float reachable=ball.Boosted?ball.profile.boostSpeed:ball.profile.maxSpeed;
                    if(before>=1&&before<25&&alternative.Definition.minimumSpeed<=reachable)
                    {competitor.Cursor.ChooseAlternative(option);break;}
                }
            }
            competitor.Cursor.SampleAhead(0,out var forward,out float width);
            competitor.Cursor.SampleAhead(18,out var futureForward,out _);
            forward=Vector3.ProjectOnPlane(forward,ball.MovementUp).normalized;
            futureForward=Vector3.ProjectOnPlane(futureForward,ball.MovementUp).normalized;
            if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
            var right=Vector3.Cross(ball.MovementUp,forward).normalized;
            float angle=Vector3.Angle(forward,futureForward);
            float care=difficulty==RaceDifficulty.Easy?.028f:difficulty==RaceDifficulty.Medium?.021f:.015f;
            float cruise=ball.profile.maxSpeed;
            plannedSpeed=Mathf.Clamp(cruise/(1+angle*care),Mathf.Min(7,cruise),cruise);
            if(ball.Boosted&&angle<8)plannedSpeed=Mathf.Min(22,ball.profile.boostSpeed);
            var choice=competitor.Cursor.ChosenAlternative;
            if(choice!=null)
            {
                float wanted=Mathf.Min(ball.Boosted?ball.profile.boostSpeed:ball.profile.maxSpeed,choice.Definition.minimumSpeed+1);
                plannedSpeed=Mathf.Max(plannedSpeed,wanted);
                preferredLane=0;
            }
            float usable=Mathf.Clamp(width*.5f-.95f,.45f,3.2f);
            plannedLane=Mathf.Clamp(preferredLane,-usable,usable);WaitingForHazard=false;
            if(difficulty==RaceDifficulty.Easy)
                plannedLane+=Mathf.Sin(manager.Elapsed*.7f+competitor.Id*2.17f)*.13f;

            float nearest=20;int chosen=-1;
            for(int i=0;i<manager.Hazards.Length;i++)
            {
                var hazard=manager.Hazards[i];if(!hazard.Obstacle)continue;
                if(choice!=null&&(hazard.Obstacle.transform.position-position).sqrMagnitude>400)continue;
                var kind=hazard.Obstacle.kind;
                if(kind==ObstacleKind.Booster||kind==ObstacleKind.JumpPad||kind==ObstacleKind.Conveyor||kind==ObstacleKind.MovingBridge||kind==ObstacleKind.Elevator||kind==ObstacleKind.FallingTile)continue;
                float distance=hazard.RouteDistance-competitor.Distance;
                if(distance<-3||distance>20||distance>nearest)continue;
                if((kind==ObstacleKind.Laser||kind==ObstacleKind.TimedDoor)&&hazard.Obstacle.IsPassable)continue;
                nearest=distance;chosen=i;
            }
            if(chosen>=0)
            {
                var hazard=manager.Hazards[chosen];var bounds=hazard.Bounds;
                float speed=Mathf.Max(5,Vector3.Dot(ball.Body.linearVelocity,forward));
                float arrival=Mathf.Clamp(nearest/speed,0,2);
                var prediction=bounds.center;
                if(hazard.Body)prediction+=Vector3.ClampMagnitude(hazard.Body.GetPointVelocity(bounds.center)*arrival,3);
                var center=route.Sample(hazard.RouteDistance,out _,out float hazardWidth);
                float across=Vector3.Dot(prediction-center,right);
                var kind=hazard.Obstacle.kind;
                bool closesWholeRoad=(kind==ObstacleKind.TimedDoor||kind==ObstacleKind.Laser)&&bounds.size.magnitude>hazardWidth*.8f;
                if(closesWholeRoad)
                {
                    plannedSpeed=Mathf.Min(plannedSpeed,Mathf.Max(0,nearest-3)*.65f);
                    WaitingForHazard=true;
                }
                else
                {
                    if(avoidingHazard!=chosen||nearest>12)
                    {
                        float avoid=Mathf.Min(usable,difficulty==RaceDifficulty.Easy?2.9f:2.65f);
                        committedLane=across>=0?-avoid:avoid;avoidingHazard=chosen;
                    }
                    plannedLane=Mathf.Clamp(committedLane,-usable,usable);
                    if(avoidingHazard==chosen)
                    {
                        float avoid=Mathf.Min(usable,2.7f);
                        // Once committed, pass on a predictable side rather than oscillating through a moving gate.
                        if(Mathf.Abs(plannedLane)<.6f)plannedLane=across>=0?-avoid:avoid;
                    }
                    plannedSpeed=Mathf.Min(plannedSpeed,kind==ObstacleKind.Pendulum?7.5f:difficulty==RaceDifficulty.Hard?10:8.5f);
                }
            }
            else avoidingHazard=-1;

            foreach(var rival in manager.Competitors)
            {
                if(rival==competitor||rival.Eliminated||rival.Finished||rival.Recovering)continue;
                float gap=rival.Distance-competitor.Distance;
                if(gap<=0||gap>8)continue;
                float rivalLane=Vector3.Dot(rival.Ball.Body.position-position,right);
                if(Mathf.Abs(rivalLane)>1.5f)continue;
                if(chosen<0)
                {
                    float left=Mathf.Clamp(plannedLane-2.1f,-usable,usable),rightLane=Mathf.Clamp(plannedLane+2.1f,-usable,usable);
                    if(LaneClear(rightLane,forward,right))plannedLane=rightLane;
                    else if(LaneClear(left,forward,right))plannedLane=left;
                    else plannedSpeed=Mathf.Min(plannedSpeed,Mathf.Max(3,Vector3.Dot(rival.Ball.Body.linearVelocity,forward)-.3f));
                }
                if(gap<2)plannedSpeed=Mathf.Min(plannedSpeed,Mathf.Max(1,Vector3.Dot(rival.Ball.Body.linearVelocity,forward)-1));
            }
            plannedLane=Mathf.Clamp(plannedLane,-usable,usable);
        }

        Vector3 ResistanceCompensation(Vector3 relativeVelocity)
        {
            if(!ball.Grounded)return Vector3.zero;
            Vector3 tangent=Vector3.ProjectOnPlane(relativeVelocity,ball.GroundNormal);
            float surface=1;var support=ball.GroundDetector.SupportCollider;
            if(support&&support.sharedMaterial)surface=Mathf.Lerp(.2f,1.3f,Mathf.Clamp01(support.sharedMaterial.dynamicFriction));
            float resistance=(ball.profile.rollingResistance+ball.profile.groundedViscousResistance*tangent.magnitude)*surface*Mathf.Clamp(ball.SurfaceResistanceMultiplier,.02f,10);
            // Planning feedforward compensates known resistance/slope through ordinary commands only.
            return tangent.normalized*resistance-Vector3.ProjectOnPlane(ball.GravityVector,ball.GroundNormal);
        }

        bool LaneClear(float lane,Vector3 forward,Vector3 right)
        {
            var center=manager.Route.Sample(competitor.Distance,out _,out _);
            foreach(var rival in manager.Competitors)
            {
                if(rival==competitor||rival.Eliminated||rival.Finished||rival.Recovering)continue;
                float longitudinal=Vector3.Dot(rival.Ball.Body.position-ball.Body.position,forward);
                float lateral=Vector3.Dot(rival.Ball.Body.position-center,right);
                if(longitudinal>-3&&longitudinal<8&&Mathf.Abs(lateral-lane)<1.5f)return false;
            }
            return true;
        }
    }
}
