using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    // An input-only driver. It never writes body position/rotation/velocity or moves obstacles.
    [DefaultExecutionOrder(-40)]
    public sealed class AdventureValidationDriver : MonoBehaviour
    {
        BallController ball;GameSession session;RaceRoute route;RaceRouteCursor cursor;
        QuestObstacle[] hazards;int checkpoint=-1;float committedLane;QuestObstacle avoiding;bool precisionCampaign;
        static readonly float[] PrecisionLookAhead={3,7,14};
        public float Distance=>cursor==null?0:cursor.Distance;
        public Vector3 PlannedTarget {get;private set;}
        public float PlannedSpeed {get;private set;}
        public float PlannedWidth {get;private set;}
        public int RouteSegment=>route==null?0:route.FindSegment(Distance);
        public int NextGate=>cursor==null?0:cursor.NextGate;
        public void Initialize(GameSession game,LevelBuilder builder)
        {
            session=game;ball=game.Ball;hazards=builder.GetComponentsInChildren<QuestObstacle>();
            precisionCampaign=game.Level.streamedCampaign||(game.Level.id<=55&&!string.IsNullOrEmpty(game.Level.signatureMechanic));
            var definition=ScriptableObject.CreateInstance<RaceTrackDefinition>();definition.course=game.Level;definition.route=game.Level.routePoints;
            route=new RaceRoute(definition);UnityEngine.Object.Destroy(definition);cursor=new RaceRouteCursor(route,ball.Body.position);
            cursor.Reset(route.ProjectAnywhere(ball.Body.position),ball.Body.position);ball.SetExternalControl(true);
        }
        void FixedUpdate()
        {
            if(!ball||session.State!=SessionState.Playing)return;
            if(checkpoint!=session.CheckpointIndex||Vector3.Distance(route.Sample(cursor.Distance,out _,out _),ball.Body.position)>18)
            {checkpoint=session.CheckpointIndex;cursor.Reset(route.ProjectAnywhere(ball.Body.position),ball.Body.position);}
            cursor.Update(ball.Body.position,Time.fixedDeltaTime);
            cursor.SampleAhead(0,out var tangent,out float width);var forward=Vector3.ProjectOnPlane(tangent,ball.MovementUp).normalized;
            if(forward.sqrMagnitude<.1f)forward=ball.PathForward;ball.SetPathForward(forward);
            forward=Vector3.ProjectOnPlane(ball.PathForward,ball.GroundNormal).normalized;
            var relativeVelocity=ball.Body.linearVelocity-ball.SupportVelocity;
            var right=Vector3.Cross(ball.GroundNormal,forward).normalized;float speed=Vector3.Dot(relativeVelocity,forward);
            cursor.SampleAhead(15,out var nextTangent,out _);float bend=Vector3.Angle(Vector3.ProjectOnPlane(forward,ball.MovementUp),Vector3.ProjectOnPlane(nextTangent,ball.MovementUp));
            float cruise=ball.profile.maxSpeed;
            float targetSpeed=Mathf.Clamp(cruise/(1+bend*.025f),Mathf.Min(6,cruise),cruise);if(ball.Boosted&&bend<9)targetSpeed=Mathf.Min(22,ball.profile.boostSpeed);
            if(precisionCampaign)
            {
                width=AuthoredWidth(cursor.Distance);float upcoming=width;
                foreach(float ahead in PrecisionLookAhead)upcoming=Mathf.Min(upcoming,AuthoredWidth(cursor.Distance+ahead));
                float precisionSpeed=upcoming<=2.05f?3.0f:upcoming<=2.5f?3.8f:upcoming<=3.4f?4.8f:6.8f;
                targetSpeed=Mathf.Clamp(precisionSpeed/(1+bend*.025f),1.9f,precisionSpeed);
                if(ball.Boosted&&bend<9&&upcoming>=5.5f)targetSpeed=22;
                foreach(var platform in hazards)
                {
                    if(platform.kind!=ObstacleKind.FallingTile&&platform.kind!=ObstacleKind.CollapsingBridge)continue;
                    float ahead=Vector3.Dot(platform.transform.position-ball.Body.position,forward);
                    if(ahead>-4&&ahead<9&&Mathf.Abs(Vector3.Dot(platform.transform.position-ball.Body.position,right))<3)targetSpeed=Mathf.Max(targetSpeed,6.8f);
                }
            }
            float desiredLane=0,nearest=18;QuestObstacle selected=null;
            foreach(var hazard in hazards)
            {
                if(hazard.IsPlatform||hazard.kind==ObstacleKind.Booster||hazard.kind==ObstacleKind.JumpPad||hazard.kind==ObstacleKind.Conveyor)continue;
                if(precisionCampaign&&Mathf.Abs(hazard.transform.position.y-ball.Body.position.y)>4.2f)continue;
                float along=Vector3.Dot(hazard.transform.position-ball.Body.position,forward);
                if(along<-2||along>nearest)continue;
                float across=Mathf.Abs(Vector3.Dot(hazard.transform.position-ball.Body.position,right));if(across>width+3)continue;
                if((hazard.kind==ObstacleKind.TimedDoor||hazard.kind==ObstacleKind.Laser)&&hazard.IsPassable)continue;
                selected=hazard;nearest=along;
            }
            if(selected)
            {
                if(selected.kind==ObstacleKind.TimedDoor||selected.kind==ObstacleKind.Laser)targetSpeed=Mathf.Max(0,nearest-3)*.65f;
                else
                {
                    if(avoiding!=selected)
                    {
                        var center=route.Sample(route.ProjectAnywhere(selected.transform.position),out _,out _);
                        float offset=Vector3.Dot(selected.PredictPosition(Mathf.Clamp(nearest/Mathf.Max(5,speed),0,1.5f))-center,right);
                        float hazardWidth=precisionCampaign?AuthoredWidth(route.ProjectAnywhere(selected.transform.position)):width;
                        committedLane=(offset>=0?-1:1)*Mathf.Min(Mathf.Max(0,hazardWidth*.5f-(precisionCampaign?.74f:1.2f)),2.9f);avoiding=selected;
                    }
                    desiredLane=precisionCampaign?Mathf.Clamp(committedLane,-Mathf.Max(0,width*.5f-.74f),Mathf.Max(0,width*.5f-.74f)):committedLane;targetSpeed=Mathf.Min(targetSpeed,selected.kind==ObstacleKind.Pendulum?7.5f:8.5f);
                }
            }
            else avoiding=null;
            float lookAhead=precisionCampaign?Mathf.Clamp(Mathf.Abs(speed)*.25f,1.1f,width<=2.5f?1.7f:2.8f):Mathf.Clamp(Mathf.Abs(speed)*.28f,2.8f,5.5f);
            var target=cursor.SampleAhead(lookAhead,out _,out _)+right*desiredLane;
            PlannedTarget=target;PlannedSpeed=targetSpeed;PlannedWidth=width;
            float error=Vector3.Dot(target-ball.Body.position,right),lateral=Vector3.Dot(relativeVelocity,right);
            float acceleration=Mathf.Max(.1f,ball.profile.acceleration/ball.Body.mass);
            Vector3 compensation=ResistanceCompensation(relativeVelocity);
            float desiredLateral=Mathf.Clamp(error*1.4f,-5,5);
            float steering=Mathf.Clamp(((desiredLateral-lateral)*4.5f+Vector3.Dot(compensation,right))/acceleration,-1,1);
            float forwardAcceleration=(targetSpeed-speed)*4+Vector3.Dot(compensation,forward);
            float throttle=forwardAcceleration>0?Mathf.Min(Mathf.Sqrt(Mathf.Max(0,1-steering*steering)),forwardAcceleration/acceleration):0;
            float brake=forwardAcceleration<0?Mathf.Clamp01(-forwardAcceleration/Mathf.Max(1,ball.profile.braking)):0;
            ball.SubmitControl(new BallControlCommand(throttle,steering,brake));
        }
        float AuthoredWidth(float distance)
        {
            var widths=session.Level.routeWidths;int index=route.FindSegment(distance);
            return widths!=null&&index<widths.Length?widths[index]:route.Widths[Mathf.Min(index,route.Widths.Length-1)];
        }
        Vector3 ResistanceCompensation(Vector3 relativeVelocity)
        {
            if(!ball.Grounded)return Vector3.zero;
            Vector3 tangent=Vector3.ProjectOnPlane(relativeVelocity,ball.GroundNormal);
            float surface=1;var support=ball.GroundDetector.SupportCollider;
            if(support&&support.sharedMaterial)surface=Mathf.Lerp(.2f,1.3f,Mathf.Clamp01(support.sharedMaterial.dynamicFriction));
            float resistance=(ball.profile.rollingResistance+ball.profile.groundedViscousResistance*tangent.magnitude)*surface*Mathf.Clamp(ball.SurfaceResistanceMultiplier,.02f,10);
            return tangent.normalized*resistance-Vector3.ProjectOnPlane(ball.GravityVector,ball.GroundNormal);
        }
    }
    // Capture after the shipping brain's LateUpdate, rather than a pre-camera test-coroutine phase.
    [DefaultExecutionOrder(10000)]
    public sealed class CampaignNativeCaptureV9 : MonoBehaviour
    {
        public string RequestedName;
        void LateUpdate()
        {
            if(string.IsNullOrEmpty(RequestedName))return;
            BootstrapFlowTests.Capture(RequestedName);RequestedName=null;
        }
    }
    public sealed class SkylineTraversalTests
    {
        Scene original,loaded;string directory;
        [Serializable]sealed class FailureState
        {public int id,segment,nextGate,checkpoint,falls;public string title,state;public Vector3 position,velocity,target,forward;public float distance,targetSpeed,width;public bool grounded;}
        [UnityTest,Timeout(1200000)]public IEnumerator RepresentativeGrandCoursesReachTheirOrderedCheckpointsAndFinishes()
        {yield return Traverse(new[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,50,51,120,121,250,500,750,999,1000},"grand-traversal-v11.csv");}
        [UnityTest,Timeout(650000)]public IEnumerator SixRepresentativeCrazyCoursesCompleteTheirPhysicalPilot()
        {yield return Traverse(new[]{1,4,6,8,19,1000},"grand-pilot-v11.csv");}
        [UnityTest,Timeout(1000000)]public IEnumerator LateGrandGauntletsCompleteEveryAdditionalActAndTheFinalCourses()
        {yield return Traverse(new[]{501,507,520,533,546,559,572,585,750,999,1000},"grand-late-v11.csv");}
        IEnumerator Traverse(int[] selectedIds,string reportName)
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestProduction-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();yield return SceneManager.LoadSceneAsync("Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity",LoadSceneMode.Additive);
            loaded=SceneManager.GetSceneByPath("Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity");SceneManager.SetActiveScene(loaded);
            float deadline=Time.realtimeSinceStartup+25;while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<deadline)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app.Ready);app.Save.Data.unlockedLevel=1000;
            var failures=new List<string>();var reports=new StringBuilder("id,title,complete,seconds,falls,checkpoint,totalCheckpoints,editorBuildMs,editorAllocatedMiB\n");int selectedCount=0;
            foreach(var level in app.catalog.levels)
            {
                if(selectedIds!=null&&Array.IndexOf(selectedIds,level.id)<0)continue;selectedCount++;
                app.Play(level.id);yield return BootstrapFlowTests.AwaitPlaying(app);
                var driver=app.Session.Ball.gameObject.AddComponent<AdventureValidationDriver>();driver.Initialize(app.Session,app.CurrentCourse);
                var capture=app.CurrentCourse.gameObject.AddComponent<CampaignNativeCaptureV9>();bool captured=false;
                Time.timeScale=6;float end=Time.realtimeSinceStartup+95;
                float routeLength=0;for(int point=1;point<level.routePoints.Length;point++)routeLength+=Vector3.Distance(level.routePoints[point-1],level.routePoints[point]);
                float routeTimeout=Mathf.Max(180,routeLength/2+70);
                while(app.Session.State!=SessionState.Complete&&app.Session.Seconds<routeTimeout&&Time.realtimeSinceStartup<end)
                {
                    if(!captured&&(level.id<=11||level.id>=45)&&driver.Distance>=routeLength*.25f)
                    {capture.RequestedName="V11Campaign"+level.id+"Actual";captured=true;}
                    if(app.Session.State==SessionState.Fallen)
                    {if(app.Session.Falls>4)break;app.Session.Respawn();while(app.Session.State==SessionState.Respawning)yield return null;Time.timeScale=6;}
                    yield return null;
                }
                bool complete=app.Session.State==SessionState.Complete;
                if(!complete)
                {
                    failures.Add(level.id+" "+level.title+" at "+app.Session.Ball.Body.position+" time="+app.Session.Seconds+" checkpoint="+app.Session.CheckpointIndex);BootstrapFlowTests.Capture("FailureCourse"+level.id);
                    var body=app.Session.Ball;var failure=new FailureState{id=level.id,title=level.title,segment=driver.RouteSegment,nextGate=driver.NextGate,checkpoint=app.Session.CheckpointIndex,falls=app.Session.Falls,state=app.Session.State.ToString(),position=body.Body.position,velocity=body.Body.linearVelocity,target=driver.PlannedTarget,forward=body.PathForward,distance=driver.Distance,targetSpeed=driver.PlannedSpeed,width=driver.PlannedWidth,grounded=body.Grounded};
                    File.WriteAllText("TestResults/V11Campaign"+level.id+"FailureState.json",JsonUtility.ToJson(failure,true));
                }
                reports.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3:F2},{4},{5},{6},{7:F2},{8:F2}",level.id,level.title,complete,app.Session.Seconds,app.Session.Falls,app.Session.CheckpointIndex,level.checkpoints.Length,app.LastCourseBuildMilliseconds,UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0));
                Debug.Log("Grand "+selectedIds.Length+"-course sample"+" traversal: "+level.id+" "+level.title+" complete="+complete+" time="+app.Session.Seconds+" falls="+app.Session.Falls+" checkpoints="+(app.Session.CheckpointIndex+1)+"/"+level.checkpoints.Length);
                Time.timeScale=1;if(complete&&(level.id==1||level.id==55)){yield return new WaitForSecondsRealtime(.3f);BootstrapFlowTests.Capture("CompleteProduction"+level.id);}
                File.WriteAllText(Path.GetFullPath("TestResults/"+reportName),reports.ToString());
            }
            Assert.AreEqual(selectedIds==null?55:selectedIds.Length,selectedCount,"The report must contain its declared route scope.");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {Time.timeScale=1;if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);GameBootstrap.EditorSaveDirectory=null;if(directory!=null&&Directory.Exists(directory))Directory.Delete(directory,true);}
    }
}
