using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    // Test-only navigation: commands go through the shipped force controller. Never writes a body pose/velocity.
    [DefaultExecutionOrder(-35)]
    public sealed class ExpeditionValidationDriver : MonoBehaviour
    {
        BallController ball;GameSession session;SignatureRoutePlan plan;
        ClockworkMotor[] machinery;QuestObstacle[] hazards;QuestCamera camera;
        public int Point {get;private set;}=1;
        public bool Waiting {get;private set;}
        public void Initialize(GameSession game,LevelBuilder course,SignatureRoutePlan route)
        {
            session=game;ball=game.Ball;plan=route;machinery=course.GetComponentsInChildren<ClockworkMotor>();hazards=course.GetComponentsInChildren<QuestObstacle>();camera=course.FollowCamera;
            ball.SetExternalControl(true);
        }
        void FixedUpdate()
        {
            if(!ball||session.State!=SessionState.Playing)return;
            var up=ball.MovementUp;var normal=ball.GroundNormal;Vector3 position=ball.Body.position;
            while(Point<plan.points.Length-1)
            {
                Vector3 target=plan.points[Point]+up*.52f;
                var delta=target-position;
                if(Vector3.ProjectOnPlane(delta,up).magnitude>.95f||Mathf.Abs(Vector3.Dot(delta,up))>2.5f)break;
                Point++;
            }
            var goal=plan.points[Point]+up*.52f;var difference=Vector3.ProjectOnPlane(goal-position,normal);
            Vector3 forward=difference.sqrMagnitude>.0001f?difference.normalized:ball.PathForward;
            ball.SetPathForward(forward);forward=Vector3.ProjectOnPlane(ball.PathForward,normal).normalized;
            Vector3 right=Vector3.Cross(normal,forward).normalized;
            var relative=ball.Body.linearVelocity-ball.SupportVelocity;var tangent=Vector3.ProjectOnPlane(relative,normal);
            float wanted=plan.speed;
            if(Point<plan.points.Length-1)
            {
                var incoming=plan.points[Point]-plan.points[Mathf.Max(0,Point-1)];var outgoing=plan.points[Point+1]-plan.points[Point];
                if(Vector3.Angle(incoming,outgoing)>25)wanted=Mathf.Min(wanted,Mathf.Sqrt(Mathf.Max(.1f,difference.magnitude)*4.5f));
            }
            Waiting=false;
            foreach(var motor in machinery)
            {
                if(motor.kind==ClockworkMotionKind.Drawbridge&&position.z<13.3f&&position.z>7&&Mathf.Abs(position.x)<5&&!motor.IsTraversable){wanted=0;Waiting=true;}
                if(motor.kind==ClockworkMotionKind.Wheel&&position.z>56&&position.z<60&&Mathf.Abs(position.x)<5&&!motor.WheelWindow((66-position.z)/Mathf.Max(3,plan.speed))){wanted=0;Waiting=true;}
            }
            foreach(var hazard in hazards)
            {
                if(hazard.kind!=ObstacleKind.TimedDoor&&hazard.kind!=ObstacleKind.Laser)continue;
                Vector3 offset=hazard.transform.position-position;
                float along=Vector3.Dot(offset,forward),across=Mathf.Abs(Vector3.Dot(offset,right));
                if(!hazard.IsPassable&&along>0&&along<10&&across<3.5f){wanted=Mathf.Min(wanted,Mathf.Max(0,along-3.2f)*1.5f);Waiting=wanted<.1f;}
            }
            float resistance=ball.profile.rollingResistance+ball.profile.groundedViscousResistance*tangent.magnitude;
            var support=ball.GroundDetector.SupportCollider;
            float surface=support&&support.sharedMaterial?Mathf.Lerp(.2f,1.3f,Mathf.Clamp01(support.sharedMaterial.dynamicFriction)):1;
            Vector3 compensation=tangent.sqrMagnitude>.0001f?tangent.normalized*resistance*surface*ball.SurfaceResistanceMultiplier:Vector3.zero;
            compensation-=Vector3.ProjectOnPlane(ball.GravityVector,normal);
            Vector3 acceleration=(forward*wanted-tangent)*4+compensation;
            float scale=ball.Body.mass/Mathf.Max(.1f,ball.profile.acceleration);
            float steering=Mathf.Clamp(Vector3.Dot(acceleration,right)*scale,-1,1);
            float throttle=Mathf.Clamp(Vector3.Dot(acceleration,forward)*scale,-1,1);
            float remaining=Mathf.Sqrt(Mathf.Max(0,1-steering*steering));throttle=Mathf.Clamp(throttle,-remaining,remaining);
            ball.SubmitControl(new BallControlCommand(throttle,steering));
        }
    }

    public sealed class ExpeditionTraversalTests
    {
        Scene original,loaded;string directory;
        static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
        [UnityTest,Timeout(600000)]
        public IEnumerator AllSignatureRoutesAndMazeExitsFinishUsingOrdinaryPhysicsCommands()
        {
            directory=Path.Combine(Path.GetTempPath(),"BallQuestExpeditions-"+Guid.NewGuid());GameBootstrap.EditorSaveDirectory=directory;
            original=SceneManager.GetActiveScene();yield return SceneManager.LoadSceneAsync("Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity",LoadSceneMode.Additive);
            loaded=SceneManager.GetSceneByPath("Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity");SceneManager.SetActiveScene(loaded);
            float deadline=Time.realtimeSinceStartup+25;while((!GameBootstrap.Instance||!GameBootstrap.Instance.Ready)&&Time.realtimeSinceStartup<deadline)yield return null;
            var app=GameBootstrap.Instance;Assert.IsTrue(app.Ready);Assert.AreEqual(5,app.catalog.expeditions.Length);
            var failures=new System.Collections.Generic.List<string>();var report=new StringBuilder("id,title,route,complete,seconds,falls,checkpoint,wallContacts,ceilingContacts,coins,gems,tokens\n");
            foreach(var level in app.catalog.expeditions)
            {
                int variant=0;
                foreach(var route in SignatureAdventurePaths.Variants(level))
                {
                    app.PlayExpedition(level.id);yield return BootstrapFlowTests.AwaitPlaying(app);
                    if(variant==0)
                    {
                        yield return new WaitForSeconds(.2f);BootstrapFlowTests.Capture("Expedition"+level.id);
                        Preview(level.id);
                    }
                    var driver=app.Session.Ball.gameObject.AddComponent<ExpeditionValidationDriver>();driver.Initialize(app.Session,app.CurrentCourse,route);
                    var field=app.CurrentCourse.GetComponentInChildren<SignatureGravityField>();
                    bool wallCapture=false,ceilingCapture=false;Time.timeScale=6;float end=Time.realtimeSinceStartup+65;
                    while(app.Session.State==SessionState.Playing&&app.Session.Seconds<240&&Time.realtimeSinceStartup<end)
                    {
                        if(field&&!wallCapture&&Vector3.Dot(app.Session.Ball.MovementUp,Vector3.back)>.95f){wallCapture=true;BootstrapFlowTests.Capture("GravityWall");}
                        if(field&&!ceilingCapture&&Vector3.Dot(app.Session.Ball.MovementUp,Vector3.down)>.98f){ceilingCapture=true;BootstrapFlowTests.Capture("GravityCeiling");}
                        yield return null;
                    }
                    bool complete=app.Session.State==SessionState.Complete;
                    if(!complete)failures.Add(level.title+" / "+route.name+" point="+driver.Point+" position="+app.Session.Ball.Body.position+" state="+app.Session.State+" time="+app.Session.Seconds);
                    if(field&&(field.WallContacts==0||field.CeilingContacts==0))failures.Add("Gravity route did not establish real wall AND ceiling contacts");
                    if(route.name.Contains("Hidden")&&app.Session.Gems==0)failures.Add("Hidden reward route did not actually collect its gem");
                    report.AppendLine(string.Format(Invariant,"{0},{1},{2},{3},{4:F2},{5},{6},{7},{8},{9},{10},{11}",level.id,level.title,route.name.Replace(',',';'),complete,app.Session.Seconds,app.Session.Falls,app.Session.CheckpointIndex,field?field.WallContacts:0,field?field.CeilingContacts:0,app.Session.Coins,app.Session.Gems,app.Session.Tokens));
                    Debug.Log("Expedition validation "+level.title+" / "+route.name+" complete="+complete+" point="+driver.Point+" at="+app.Session.Ball.Body.position);
                    Time.timeScale=1;if(!complete)BootstrapFlowTests.Capture("ExpeditionFailure"+level.id+"-"+variant);
                    File.WriteAllText(Path.GetFullPath("TestResults/expedition-traversal.csv"),report.ToString());variant++;
                }
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        static void Preview(int id)
        {
            var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(384,216,24);var texture=new Texture2D(384,216,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,384,216),0,0);texture.Apply();Directory.CreateDirectory("TestResults/Previews");File.WriteAllBytes("TestResults/Previews/Expedition"+id+"-preview.png",texture.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(texture);}
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            if(loaded.IsValid()&&loaded.isLoaded)yield return SceneManager.UnloadSceneAsync(loaded);GameBootstrap.EditorSaveDirectory=null;
            if(directory!=null&&Directory.Exists(directory)&&Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(directory,true);
        }
    }
}
