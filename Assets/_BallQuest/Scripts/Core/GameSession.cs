using System;
using System.Collections;
using UnityEngine;

namespace BallQuest
{
    public enum SessionState
    {
        Menu=0,Playing=1,Paused=2,Fallen=3,Complete=4,
        LevelLoading=5,Ready=6,Falling=7,Respawning=8,GameOver=9,
        MainMenu=Menu,LevelComplete=Complete
    }

    public sealed class GameSession : MonoBehaviour
    {
        public SessionState State {get;private set;}=SessionState.Menu;
        public LevelDefinition Level {get;private set;}
        public BallController Ball {get;private set;}
        public int Coins {get;private set;}
        public int Gems {get;private set;}
        public int Tokens {get;private set;}
        public int Falls {get;private set;}
        public float Seconds {get;private set;}
        public int Stars {get;private set;}
        public int BankedReward {get;private set;}
        public int Boosts {get;private set;}
        public int ChallengeReward {get;private set;}
        public bool ChallengeCompleted {get;private set;}
        public string ChallengeProgress
        {
            get
            {
                if(!Level||Level.challenge==ChallengeKind.None)return "";
                string progress=Level.challenge==ChallengeKind.CoinHunt?"  "+Coins+"/"+Level.challengeTarget:Level.challenge==ChallengeKind.Treasure?"  "+Gems+"/"+Level.challengeTarget:Level.challenge==ChallengeKind.BoostRun?"  "+Boosts+"/"+Level.challengeTarget:Level.challenge==ChallengeKind.TimeTrial?"  "+Mathf.FloorToInt(Seconds)+"s":"  Falls: "+Falls;
                return ChallengeRules.Description(Level.challenge,Level.challengeTarget,Level.challengeSeconds)+progress;
            }
        }
        public string Notice {get;private set;}
        public float NoticeUntil {get;private set;}
        public bool ExternalRace {get;private set;}
        public int CheckpointIndex=>checkpointIndex;
        [Min(0)] public float readySeconds=.35f;
        [Min(0)] public float fallingSeconds=.22f;
        [Min(0)] public float respawnSeconds=.22f;
        public event Action Changed;
        public event Action Respawned;
        public event Action<string> Feedback;
        public event Action<int,Vector3> ResetRequested;
        Vector3 checkpoint,previousPosition;
        int checkpointIndex=-1,levelCount;
        SaveStore save;
        Coroutine transition;
        SessionState beforePause;

        public void Initialize(SaveStore store,int count){save=store;levelCount=count;}
        public void Loading(){StopTransition();ExternalRace=false;if(Ball){Ball.active=false;Ball.Input.active=false;}Ball=null;SetState(SessionState.LevelLoading);}
        public void Begin(LevelDefinition level,BallController ball,bool readySequence=false)
        {
            StopTransition();ExternalRace=false;Prepare(level,ball);
            if(readySequence){Ball.Respawn(level.spawn);Ball.Body.isKinematic=true;SetState(SessionState.Ready);transition=StartCoroutine(ReadySequence());}
            else SetState(SessionState.Playing);
        }
        public void BeginExternalRace(LevelDefinition level,BallController ball)
        {
            StopTransition();ExternalRace=true;Prepare(level,ball);ball.active=false;ball.Input.active=false;State=SessionState.Ready;Changed?.Invoke();
        }
        void Prepare(LevelDefinition level,BallController ball)
        {
            Level=level;Ball=ball;checkpoint=level.spawn;checkpointIndex=-1;previousPosition=ball.Body.position;
            Coins=Gems=Tokens=Falls=Stars=BankedReward=Boosts=ChallengeReward=0;ChallengeCompleted=false;Seconds=0;Notice=level.streamedCampaign?GrandWorldPresentation.Names[level.environment]+" / "+level.signatureMechanic:level.hint;NoticeUntil=8;
        }
        public void MirrorRaceState(SessionState state)
        {
            if(!ExternalRace||State==state)return;State=state;Changed?.Invoke();
        }
        IEnumerator ReadySequence()
        {
            float elapsed=0;while(elapsed<readySeconds||State==SessionState.Paused)
            {if(State==SessionState.Ready)elapsed+=Time.unscaledDeltaTime;else if(State!=SessionState.Paused)yield break;yield return null;}
            if(State!=SessionState.Ready)yield break;
            Ball.Body.isKinematic=false;previousPosition=Ball.Body.position;transition=null;SetState(SessionState.Playing);
        }
        void Update()
        {
            if(ExternalRace||State!=SessionState.Playing||!Ball)return;
            Seconds+=Time.deltaTime;
            float travelled=Vector3.Distance(previousPosition,Ball.Body.position);if(travelled<3)save.Data.lifetimeDistance+=travelled;
            previousPosition=Ball.Body.position;
            if(Ball.Body.position.y<Level.killHeight)
            {
                Falls++;save.Data.falls++;SetState(SessionState.Falling);Feedback?.Invoke("fail");transition=StartCoroutine(FallSequence());return;
            }
            var keyboard=UnityEngine.InputSystem.Keyboard.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)Pause();
        }
        IEnumerator FallSequence()
        {
            float elapsed=0;while(elapsed<fallingSeconds||State==SessionState.Paused)
            {if(State==SessionState.Falling)elapsed+=Time.unscaledDeltaTime;else if(State!=SessionState.Paused)yield break;yield return null;}
            if(State==SessionState.Falling){transition=null;SetState(SessionState.Fallen);}
        }
        public void Collect(PickupKind kind)
        {
            if(State!=SessionState.Playing)return;
            if(kind==PickupKind.Coin){Coins++;save.Data.lifetimeCoins++;}else if(kind==PickupKind.Gem)Gems++;else Tokens++;
            Feedback?.Invoke("coin");
        }
        public void Checkpoint(int index,Vector3 point)
        {
            if(ExternalRace||State!=SessionState.Playing||index!=checkpointIndex+1)return;
            checkpointIndex=index;checkpoint=point;Announce("Checkpoint reached");
        }
        public void Announce(string message,string sound="checkpoint")
        {Notice=message;NoticeUntil=Seconds+2.5f;Feedback?.Invoke(sound);}
        public void RegisterBoost(){if(State==SessionState.Playing){save.Data.lifetimeBoosts++;Boosts++;}}
        public void RegisterDrop(){if(State==SessionState.Playing)save.Data.lifetimeDrops++;}
        public void Respawn()
        {
            if(ExternalRace||State!=SessionState.Fallen||!Ball)return;
            SetState(SessionState.Respawning);transition=StartCoroutine(RespawnSequence());
        }
        IEnumerator RespawnSequence()
        {
            Ball.Body.isKinematic=false;Ball.Respawn(checkpoint);
            ResetRequested?.Invoke(checkpointIndex,checkpoint);Physics.SyncTransforms();
            Ball.Body.isKinematic=true;previousPosition=checkpoint;Respawned?.Invoke();
            float elapsed=0;while(elapsed<respawnSeconds||State==SessionState.Paused)
            {if(State==SessionState.Respawning)elapsed+=Time.unscaledDeltaTime;else if(State!=SessionState.Paused)yield break;yield return null;}
            if(State!=SessionState.Respawning)yield break;
            Ball.Body.isKinematic=false;transition=null;SetState(SessionState.Playing);
        }
        public void Finish()
        {
            if(ExternalRace||State!=SessionState.Playing)return;
            if(checkpointIndex<Level.checkpoints.Length-1){Announce("Follow the checkpoint markers first.");return;}
            int available=0;foreach(var pickup in Level.pickups)if(pickup.kind==PickupKind.Coin)available++;
            Stars=ProgressionRules.Stars(Coins,available,Falls,Seconds,Level.parSeconds);if(Falls==0)save.Data.flawlessFinishes++;
            BankedReward=Level.expedition?ProgressionRules.CompleteExpedition(save.Data,Level.id,Coins+Stars*10,Gems,Stars,Seconds,Tokens):ProgressionRules.Complete(save.Data,Level.id,levelCount,Coins+Stars*10,Gems,Stars,Seconds,Tokens);
            if(!Level.expedition&&Level.challenge!=ChallengeKind.None)
            {ChallengeCompleted=ChallengeRules.Satisfied(Level.challenge,Level.challengeTarget,Level.challengeSeconds,Coins,Falls,Seconds,Gems,Boosts);ChallengeReward=ChallengeRules.Claim(save.Data,Level.id,Level.challengeReward,ChallengeCompleted);BankedReward+=ChallengeReward;}
            if(GameBootstrap.Instance)foreach(var profile in GameBootstrap.Instance.catalog.balls)
                if(save.Data.unlockedLevel>=profile.unlockLevel&&!save.Data.balls.Contains(profile.id))save.Data.balls.Add(profile.id);
            save.Save();SetState(SessionState.Complete);Feedback?.Invoke("victory");
        }
        public void Pause()
        {
            if(ExternalRace||State==SessionState.Paused)return;
            if(State!=SessionState.Playing&&State!=SessionState.Ready&&State!=SessionState.Falling&&State!=SessionState.Respawning)return;
            beforePause=State;SetState(SessionState.Paused);
        }
        public void Resume(){if(!ExternalRace&&State==SessionState.Paused)SetState(beforePause);}
        public void Menu()
        {StopTransition();if(Ball){Ball.active=false;Ball.Input.active=false;}ExternalRace=false;Ball=null;SetState(SessionState.Menu);}
        void SetState(SessionState state)
        {
            State=state;
            if(!ExternalRace)Time.timeScale=state==SessionState.Paused||state==SessionState.Fallen||state==SessionState.Complete||state==SessionState.GameOver?0:1;
            if(Ball&&!ExternalRace){Ball.active=state==SessionState.Playing;Ball.Input.active=Ball.active;Ball.Input.ResetInput();}
            Changed?.Invoke();
        }
        void StopTransition(){if(transition!=null){StopCoroutine(transition);transition=null;}}
        void OnApplicationPause(bool paused){if(paused){if(!ExternalRace)Pause();save?.Save();}}
        void OnDestroy()=>Time.timeScale=1;
    }
}
