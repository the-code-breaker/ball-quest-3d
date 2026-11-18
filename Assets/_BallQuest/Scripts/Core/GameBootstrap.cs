using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BallQuest
{
    public enum GameMode {Adventure,Race,Tournament}

    public sealed class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance {get;private set;}
#if UNITY_EDITOR
        public static string EditorSaveDirectory;
#endif
        public GameCatalog catalog;
        public SaveStore Save {get;private set;}
        public GameSession Session {get;private set;}
        public QuestAudio Audio {get;private set;}
        public EffectPool Effects {get;private set;}
        public QuestUI UI {get;private set;}
        public RaceManager Race {get;private set;}
        public GameMode Mode {get;private set;}
        public LevelBuilder CurrentCourse=>level;
        public bool Ready {get;private set;}
        public bool Loading=>loading!=null;
        public double LastCourseBuildMilliseconds {get;private set;}
        LevelBuilder level;
        UniversalRenderPipelineAsset runtimePipeline;
        Coroutine loading;
        int loadToken;
        RaceTrackDefinition lastRace;
        RaceFormat lastFormat;
        RaceDifficulty lastDifficulty;
        int lastOpponentCount=-1;

        IEnumerator Start()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);yield break;}Instance=this;
            if(!catalog||catalog.levels==null||catalog.levels.Length<3||catalog.palette==null||catalog.palette.Length<9||catalog.balls==null||catalog.balls.Length==0||catalog.skins==null||catalog.skins.Length==0)
            {Debug.LogError("Ball Quest catalog is incomplete. Run content generation.");yield break;}
            if(catalog.pipeline){runtimePipeline=Instantiate(catalog.pipeline);GraphicsSettings.defaultRenderPipeline=runtimePipeline;QualitySettings.renderPipeline=runtimePipeline;}
            Application.targetFrameRate=60;Screen.sleepTimeout=SleepTimeout.NeverSleep;
            string directory=Application.persistentDataPath;
#if UNITY_EDITOR
            if(!string.IsNullOrEmpty(EditorSaveDirectory))directory=EditorSaveDirectory;
#endif
            Save=new SaveStore(directory,new UnitySaveCodec());Save.Load();ArcadeProgress.Refresh(Save.Data);
            if(!System.Array.Exists(catalog.skins,s=>s.id==Save.Data.selectedSkin))Save.Data.selectedSkin="classic-red";
            if(!System.Array.Exists(catalog.balls,b=>b.id==Save.Data.selectedBall&&b.unlockLevel<=catalog.levels.Length))Save.Data.selectedBall="standard";
            Session=gameObject.AddComponent<GameSession>();Session.Initialize(Save,catalog.levels.Length);
            Session.Feedback+=Feedback;Session.ResetRequested+=ResetCourseAtCheckpoint;
            Audio=gameObject.AddComponent<QuestAudio>();Audio.Apply(Save.Data.settings);Effects=gameObject.AddComponent<EffectPool>();
            Race=gameObject.AddComponent<RaceManager>();Race.Changed+=RaceChanged;Race.Finished+=RaceFinished;
            var cameraObject=new GameObject("Main Camera");cameraObject.transform.SetParent(transform);cameraObject.tag="MainCamera";
            var camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();
            camera.transform.SetPositionAndRotation(new Vector3(0,10,-16),Quaternion.Euler(28,0,0));
            camera.backgroundColor=new Color(.19f,.65f,.96f);camera.clearFlags=CameraClearFlags.SolidColor;
            if(catalog.skyMaterial){RenderSettings.skybox=catalog.skyMaterial;camera.clearFlags=CameraClearFlags.Skybox;}
            camera.nearClipPlane=.25f;camera.farClipPlane=350;camera.allowHDR=true;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;cameraObject.AddComponent<CinemachineBrain>();
            var atmosphere=new GameObject("Arcade atmosphere");atmosphere.transform.SetParent(transform);var volume=atmosphere.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;volume.sharedProfile=Resources.Load<VolumeProfile>("Arcade/ArcadePostFX");
            var sun=new GameObject("Warm sun");sun.transform.SetParent(transform);sun.transform.rotation=Quaternion.Euler(50,-35,0);
            var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.color=new Color(1,.97f,.91f);light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.56f,.65f,.77f);
            RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture=BallLook.EnvironmentReflection;RenderSettings.reflectionIntensity=.85f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=65;RenderSettings.fogEndDistance=245;RenderSettings.fogColor=camera.backgroundColor;
            if(!EventSystem.current){var events=new GameObject("Input events");events.transform.SetParent(transform);events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>();}
            UI=gameObject.AddComponent<QuestUI>();UI.Initialize(this);ApplySettings();UI.Splash();
            yield return new WaitForSecondsRealtime(.7f);UI.MainMenu();Ready=true;
        }

        public void Play(int id)
        {
            PlayDefinition(catalog.Level(id));
        }
        public void PlayExpedition(int id)=>PlayDefinition(catalog.Expedition(id));
        void PlayDefinition(LevelDefinition definition)
        {
            if(!Ready||!definition||!definition.expedition&&definition.id>Save.Data.unlockedLevel)return;
            CancelLoading();Mode=GameMode.Adventure;Race.Cancel();Session.Loading();ClearLevel();
            loading=StartCoroutine(LoadAdventure(definition,++loadToken));
        }
        IEnumerator LoadAdventure(LevelDefinition definition,int token)
        {
            yield return null;if(token!=loadToken)yield break;
            BuildCourse(definition);Session.readySeconds=definition.streamedCampaign||CrazyCoursePresentation.Applies(definition)?1.05f:.35f;Session.Begin(level.RuntimeLevel,level.Ball,true);ApplySettings();loading=null;
        }
        void BuildCourse(LevelDefinition definition)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            var root=new GameObject("Course "+definition.id+" - "+definition.title);root.transform.SetParent(transform,false);
            level=root.AddComponent<LevelBuilder>();level.Build(catalog,definition,Session,Save.Data);
            var current=level;Audio.ball=current.Ball;
            current.Ball.Jumped+=()=>Audio.Play("jump");
            current.Ball.Landed+=force=>{Audio.Play("land");if(force>7)Session.RegisterDrop();Effects.Burst(current.Ball.transform.position+Vector3.down*.4f,Color.white,force>7?16:5);};
            LastCourseBuildMilliseconds=watch.Elapsed.TotalMilliseconds;
        }
        public void PlayRace(int id,RaceFormat format=RaceFormat.Obstacle,RaceDifficulty difficulty=RaceDifficulty.Medium,int aiCount=-1)
        {
            if(!Ready||catalog.races==null||id>Save.Data.unlockedRace)return;
            var definition=System.Array.Find(catalog.races,r=>r&&r.id==id);if(!definition||!definition.course)return;
            BeginRaceLoad(definition,format,difficulty,aiCount,false);
        }
        public void StartTournament(RaceDifficulty difficulty=RaceDifficulty.Medium,int aiCount=4)
        {
            if(!Ready||catalog.races==null||catalog.races.Length<3||Save.Data.unlockedRace<3){UI.Races(true);return;}
            Race.Cancel();var tracks=new RaceTrackDefinition[Mathf.Min(3,catalog.races.Length)];System.Array.Copy(catalog.races,tracks,tracks.Length);
            Race.ConfigureTournament(tracks);BeginRaceLoad(Race.NextTournamentTrack,RaceFormat.Tournament,difficulty,aiCount,true);
        }
        void BeginRaceLoad(RaceTrackDefinition definition,RaceFormat format,RaceDifficulty difficulty,int aiCount,bool keepTournament)
        {
            CancelLoading();if(!keepTournament)Race.Cancel();Mode=format==RaceFormat.Tournament?GameMode.Tournament:GameMode.Race;
            lastRace=definition;lastFormat=format;lastDifficulty=difficulty;lastOpponentCount=aiCount;
            Session.Loading();ClearLevel();loading=StartCoroutine(LoadRace(definition,format,difficulty,aiCount,keepTournament,++loadToken));
        }
        IEnumerator LoadRace(RaceTrackDefinition definition,RaceFormat format,RaceDifficulty difficulty,int opponents,bool keepTournament,int token)
        {
            yield return null;if(token!=loadToken)yield break;
            BuildCourse(definition.course);
            foreach(var follower in level.GetComponentsInChildren<CoursePathFollower>())follower.enabled=false;
            Session.BeginExternalRace(definition.course,level.Ball);
            Race.Begin(definition,level.Ball,level.transform,catalog,Save,format,difficulty,opponents,keepTournament);
            ApplySettings();loading=null;
        }
        public void RetryCurrent()
        {
            if(Mode==GameMode.Adventure){if(Session.Level){if(Session.Level.expedition)PlayExpedition(Session.Level.id);else Play(Session.Level.id);}return;}
            if(lastFormat==RaceFormat.Tournament)StartTournament(lastDifficulty,lastOpponentCount);
            else if(lastRace)PlayRace(lastRace.id,lastFormat,lastDifficulty,lastOpponentCount);
        }
        public void NextRace()
        {
            if(Race.IsTournament)
            {
                var next=Race.NextTournamentTrack;
                if(!Race.TournamentComplete&&next){BeginRaceLoad(next,RaceFormat.Tournament,lastDifficulty,lastOpponentCount,true);return;}
                MainMenu();UI.Races(true);return;
            }
            int id=lastRace?lastRace.id+1:1;
            if(catalog.races!=null&&id<=catalog.races.Length&&id<=Save.Data.unlockedRace)PlayRace(id,lastFormat,lastDifficulty,lastOpponentCount);
            else {MainMenu();UI.Races(false);}
        }
        public void PauseCurrent(){if(Mode==GameMode.Adventure)Session.Pause();else Race.Pause();}
        public void ResumeCurrent(){if(Mode==GameMode.Adventure)Session.Resume();else Race.Resume();}
        public void MainMenu()
        {
            CancelLoading();Mode=GameMode.Adventure;if(Race)Race.Cancel();Session.Menu();ClearLevel();UI.MainMenu();Save.Save();
        }
        void CancelLoading(){loadToken++;if(loading!=null){StopCoroutine(loading);loading=null;}}
        void ClearLevel(){Audio.ball=null;if(level){level.gameObject.SetActive(false);Destroy(level.gameObject);level=null;}}
        void ResetCourseAtCheckpoint(int index,Vector3 point){if(level)level.ResetAtCheckpoint(index);}
        void RaceChanged()
        {
            if(!UI||Mode==GameMode.Adventure)return;
            SessionState state=Race.State==RaceState.Countdown?SessionState.Ready:Race.State==RaceState.Racing?SessionState.Playing:Race.State==RaceState.Paused?SessionState.Paused:Race.State==RaceState.Complete?SessionState.Complete:SessionState.Menu;
            Session.MirrorRaceState(state);UI.RaceStateChanged();
        }
        void RaceFinished()
        {
            if(Race.Result==null)return;Audio.Play(Race.Result.Victory?"victory":"checkpoint");
            if(Race.Player&&Race.Player.Ball)Effects.Burst(Race.Player.Ball.transform.position+Vector3.up,new Color(1,.78f,.17f),60);
            UI.RaceStateChanged();
        }
        void Feedback(string id)
        {
            Audio.Play(id);if(Save.Data.settings.vibration&&(id=="victory"||id=="checkpoint"||id=="boost")&&Application.isMobilePlatform)Handheld.Vibrate();
            if(id=="victory"&&Session.Ball)Effects.Burst(Session.Ball.transform.position+Vector3.up,new Color(1,.75f,.2f),70);
        }
        public void ApplySettings()
        {
            var settings=Save.Data.settings;Audio.Apply(settings);Application.targetFrameRate=settings.quality==0?30:60;
            QualitySettings.shadows=settings.quality==0?UnityEngine.ShadowQuality.Disable:UnityEngine.ShadowQuality.All;QualitySettings.shadowDistance=settings.quality==2?40:25;
            if(runtimePipeline){runtimePipeline.shadowDistance=settings.quality==0?0:settings.quality==2?40:25;runtimePipeline.shadowCascadeCount=settings.quality==0?1:2;runtimePipeline.cascade2Split=.35f;runtimePipeline.msaaSampleCount=settings.quality==0?1:4;runtimePipeline.renderScale=settings.quality==0?.85f:1;}
            if(Camera.main)Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=settings.quality>0;
            if(Session.Ball){Session.Ball.Input.sensitivity=settings.sensitivity;Session.Ball.Input.touchMode=(BallTouchMode)settings.touchMode;}
            if(level&&level.FollowCamera){level.FollowCamera.effectsEnabled=settings.cameraEffects;level.FollowCamera.mode=BallCameraMode.AdaptiveChase;}
        }
        public void OpenPhysicsLab()
        {
            CancelLoading();Race.Cancel();Session.Menu();Save.Save();Time.timeScale=1;
            UnityEngine.SceneManagement.SceneManager.LoadScene("PhysicsLab");
        }
        void OnApplicationPause(bool paused){if(paused&&Save!=null){if(Mode!=GameMode.Adventure&&Race)Race.Pause();Save.Save();}}
        void OnApplicationQuit()=>Save?.Save();
        void OnDestroy()
        {
            if(Instance==this)Instance=null;
            if(Race){Race.Changed-=RaceChanged;Race.Finished-=RaceFinished;Race.Cancel();}
            if(runtimePipeline){if(GraphicsSettings.defaultRenderPipeline==runtimePipeline)GraphicsSettings.defaultRenderPipeline=catalog.pipeline;if(QualitySettings.renderPipeline==runtimePipeline)QualitySettings.renderPipeline=catalog.pipeline;Destroy(runtimePipeline);}
        }
    }
}
