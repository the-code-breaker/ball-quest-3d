using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    public sealed class RaceHazard
    {
        public QuestObstacle Obstacle;
        public Rigidbody Body;
        public Collider[] Colliders;
        public float RouteDistance;
        public Bounds Bounds
        {
            get
            {
                bool found=false;var bounds=new Bounds(Obstacle.transform.position,Vector3.one);
                foreach(var collider in Colliders)
                {
                    if(!collider||!collider.enabled||collider.isTrigger)continue;
                    if(!found){bounds=collider.bounds;found=true;}else bounds.Encapsulate(collider.bounds);
                }
                return bounds;
            }
        }
    }

    [DefaultExecutionOrder(50)]
    public sealed class RaceManager : MonoBehaviour
    {
        public RaceState State {get;private set;}=RaceState.Idle;
        public RaceTrackDefinition Definition {get;private set;}
        public RaceFormat Format {get;private set;}
        public RaceDifficulty Difficulty {get;private set;}
        public RaceCompetitor Player {get;private set;}
        public RaceRoute Route {get;private set;}
        public RaceHazard[] Hazards {get;private set;}=Array.Empty<RaceHazard>();
        public RaceResult Result {get;private set;}
        public IReadOnlyList<RaceCompetitor> Competitors=>competitors;
        public IReadOnlyList<RaceCompetitor> Standings=>standings;
        public int PlayerPosition=>Player?Player.Position:1;
        public float Progress=>Player?Player.Progress:0;
        public float DistanceRemaining=>Player?Player.Cursor.RemainingDistance:Route==null?0:Route.Length;
        public float Elapsed {get;private set;}
        public int CollectedCoins {get;private set;}
        public int CollectedGems {get;private set;}
        public int CollectedTokens {get;private set;}
        public string CountdownText=>State==RaceState.Countdown?Mathf.CeilToInt(countdownRemaining).ToString():State==RaceState.Racing&&Elapsed<.7f?"GO!":"";
        public int RemainingCompetitors {get {int count=0;foreach(var value in competitors)if(!value.Eliminated&&!value.Finished)count++;return count;}}
        public bool IsTournament=>tournamentTracks.Length>0;
        public int TournamentRound=>Mathf.Clamp(tournamentIndex+1,1,Mathf.Max(1,tournamentTracks.Length));
        public int TournamentTotalRounds=>tournamentTracks.Length;
        public int TournamentPoints=>tournamentStandings.Count>0?tournamentStandings.Find(value=>value.Id==0)?.Points??0:0;
        public bool TournamentComplete=>IsTournament&&tournamentIndex>=tournamentTracks.Length;
        public RaceTrackDefinition NextTournamentTrack=>IsTournament&&tournamentIndex<tournamentTracks.Length?tournamentTracks[tournamentIndex]:null;
        public IReadOnlyList<TournamentStanding> TournamentStandings=>tournamentStandings;
        public string Notice {get;private set;}
        public event Action Changed;
        public event Action Finished;
        public event Action<RaceCompetitor> Recovered;

        readonly List<RaceCompetitor> competitors=new List<RaceCompetitor>(8);
        readonly List<RaceCompetitor> standings=new List<RaceCompetitor>(8);
        readonly List<FinishRequest> finishRequests=new List<FinishRequest>(8);
        readonly List<TournamentStanding> tournamentStandings=new List<TournamentStanding>(8);
        readonly Collider[] occupancy=new Collider[12];
        readonly Comparison<RaceCompetitor> compare=Compare;
        readonly HashSet<int> eliminationGates=new HashSet<int>();
        RaceTrackDefinition[] tournamentTracks=Array.Empty<RaceTrackDefinition>();
        int tournamentIndex;
        int finishingCount;
        float countdownRemaining;
        float eliminationAt=-1;
        RaceState beforePause;
        SaveStore save;
        int raceCount;
        int lastCountdown;
        bool rewarded;
        LevelBuilder builder;
        CoursePathFollower playerPathFollower;

        struct FinishRequest {public RaceCompetitor Racer;public float Seconds;}

        public void ConfigureTournament(RaceTrackDefinition[] tracks)
        {
            Cancel();var valid=new List<RaceTrackDefinition>();
            if(tracks!=null)foreach(var track in tracks)if(track&&track.course)valid.Add(track);
            if(valid.Count<2)throw new ArgumentException("A tournament needs at least two playable races.");
            tournamentTracks=valid.ToArray();tournamentIndex=0;tournamentStandings.Clear();
        }

        public void Begin(RaceTrackDefinition definition,BallController player,Transform courseRoot,GameCatalog assets,SaveStore store,RaceFormat format=RaceFormat.Obstacle,RaceDifficulty difficulty=RaceDifficulty.Medium,int aiCount=-1,bool keepTournament=false)
        {
            if(!definition||!definition.course||!player||!courseRoot||!assets)throw new ArgumentException("A race needs a track, course, player and catalog.");
            StopRun(!keepTournament);
            Definition=definition;Format=keepTournament&&IsTournament?RaceFormat.Tournament:format;Difficulty=difficulty;save=store;
            raceCount=assets.races==null?5:assets.races.Length;builder=courseRoot.GetComponent<LevelBuilder>();
            if(!builder)throw new ArgumentException("The race course root needs a LevelBuilder.");
            Route=new RaceRoute(definition);Elapsed=0;Result=null;Notice="Ready on the grid";finishingCount=0;rewarded=false;
            CollectedCoins=CollectedGems=CollectedTokens=0;
            player.ApplyProfile(assets.Ball("standard"));
            playerPathFollower=player.GetComponent<CoursePathFollower>();if(playerPathFollower)playerPathFollower.enabled=false;
            countdownRemaining=3;lastCountdown=3;eliminationAt=-1;eliminationGates.Clear();
            int opponents=Format==RaceFormat.TimeTrial?0:Mathf.Clamp(aiCount<0?definition.aiCount:aiCount,3,7);
            float gridDistance=Mathf.Max(6,Route.ProjectAnywhere(player.Body.position));
            Physics.IgnoreLayerCollision(8,8,false);
            for(int i=0;i<=opponents;i++)
            {
                int row=i/3,col=i%3;float distance=Mathf.Max(1,gridDistance-row*2.2f);
                var point=Route.Sample(distance,out var tangent,out float width);
                var forward=Vector3.ProjectOnPlane(tangent,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=Vector3.forward;
                var right=Vector3.Cross(Vector3.up,forward).normalized;
                float lane=col==0?0:col==1?-2.25f:2.25f;
                lane=Mathf.Clamp(lane,-width*.5f+1,width*.5f-1);
                var spawn=point+right*lane+Vector3.up*.62f;
                BallController ball=i==0?player:builder.SpawnCompetitor(assets.Ball("standard"),assets.skins[(i+1)%assets.skins.Length],spawn);
                ball.name=i==0?"Player racing ball":"AI racing ball "+i;ball.Respawn(spawn);ball.SetPathForward(forward);
                var racer=ball.GetComponent<RaceCompetitor>();if(!racer)racer=ball.gameObject.AddComponent<RaceCompetitor>();
                racer.Initialize(this,i,i==0?"YOU":OpponentName(i),ball,Route,spawn,distance);competitors.Add(racer);
                if(i==0)Player=racer;
                else {var driver=ball.gameObject.AddComponent<RaceDriver>();driver.Initialize(this,racer,difficulty);}
                ball.active=false;ball.Input.active=false;ball.Body.isKinematic=true;
            }
            CacheHazards(courseRoot);SortStandings();State=RaceState.Countdown;Time.timeScale=1;
            if(IsTournament&&tournamentStandings.Count==0)
                foreach(var racer in competitors)tournamentStandings.Add(new TournamentStanding{Id=racer.Id,Name=racer.DisplayName});
            Changed?.Invoke();
        }

        static string OpponentName(int id)
        {
            string[] names={"","NOVA","BOLT","COMET","RIFT","SPARK","DASH","ORBIT"};return names[Mathf.Clamp(id,1,7)];
        }

        void CacheHazards(Transform root)
        {
            var obstacles=root.GetComponentsInChildren<QuestObstacle>();var values=new List<RaceHazard>(obstacles.Length);
            foreach(var obstacle in obstacles)values.Add(new RaceHazard{Obstacle=obstacle,Body=obstacle.GetComponent<Rigidbody>(),Colliders=obstacle.GetComponentsInChildren<Collider>(),RouteDistance=Route.ProjectAnywhere(obstacle.transform.position)});
            Hazards=values.ToArray();
        }

        void FixedUpdate()
        {
            if(State==RaceState.Countdown)
            {
                countdownRemaining=Mathf.Max(0,countdownRemaining-Time.fixedDeltaTime);
                int shown=Mathf.CeilToInt(countdownRemaining);if(shown!=lastCountdown){lastCountdown=shown;Changed?.Invoke();}
                if(countdownRemaining<=0)
                {
                    State=RaceState.Racing;Notice="GO!";
                    foreach(var racer in competitors)SetActivity(racer,true);
                    Changed?.Invoke();
                }
                return;
            }
            if(State!=RaceState.Racing)return;
            float dt=Time.fixedDeltaTime;Elapsed+=dt;
            ProcessFinishRequests();
            foreach(var racer in competitors)
            {
                if(racer.Finished||racer.Eliminated)continue;
                if(racer.Recovering){UpdateRecovery(racer,dt);continue;}
                var position=racer.Ball.Body.position;racer.Cursor.Update(position,dt);
                if(racer.IsPlayer&&save!=null)
                {
                    float travelled=Vector3.Distance(position,racer.PreviousPhysicsPosition);
                    if(travelled<3)save.Data.lifetimeDistance+=travelled;
                }
                racer.Cursor.SampleAhead(0,out var tangent,out _);racer.Ball.SetPathForward(Vector3.ProjectOnPlane(tangent,Vector3.up).normalized);
                if(racer.Distance>racer.LastAdvanceDistance+.5f){racer.LastAdvanceDistance=racer.Distance;racer.LastAdvanceAt=Elapsed;}
                bool stuck=!racer.IsPlayer&&Elapsed-racer.LastAdvanceAt>6&&racer.Ball.Body.linearVelocity.magnitude<1&&!(racer.Driver&&racer.Driver.WaitingForHazard);
                if(position.y<Definition.course.killHeight||stuck){BeginRecovery(racer);continue;}
                if(racer.FinishArmed&&!racer.PendingFinish)CheckFinishCrossing(racer);
                racer.PreviousPhysicsPosition=position;
            }
            SortStandings();
            if(Format==RaceFormat.Elimination&&eliminationAt>0&&Elapsed>=eliminationAt){EliminateLast();eliminationAt=-1;}
            if(builder&&builder.FollowCamera)
            {
                int nearby=0;foreach(var rival in competitors)if(!rival.IsPlayer&&!rival.Eliminated&&!rival.Finished&&(rival.Ball.Body.position-Player.Ball.Body.position).sqrMagnitude<64)nearby++;
                builder.FollowCamera.SetRaceContext(nearby);
            }
            bool allResolved=true;foreach(var racer in competitors)if(!racer.Finished&&!racer.Eliminated){allResolved=false;break;}
            float limit=Mathf.Clamp(Definition.timeTarget*2,90,180);
            bool grace=Player.Finished&&Elapsed>Player.FinishTime+20||Player.Eliminated&&Elapsed>Player.EliminatedTime+20;
            if(allResolved||Elapsed>=limit||grace)CompleteRun();
        }

        public void HandleGate(RaceCompetitor racer,bool finish,int index,Vector3 respawn)
        {
            if(State!=RaceState.Racing||!competitors.Contains(racer)||racer.Finished||racer.Eliminated||racer.Recovering)return;
            if(finish){racer.FinishArmed=true;return;}
            if(index!=racer.CheckpointIndex+1)return;
            racer.CheckpointIndex=index;racer.CheckpointPosition=respawn;
            racer.CheckpointDistance=Route.ProjectAnywhere(respawn);
            if(racer.IsPlayer){Notice="Race checkpoint reached";Changed?.Invoke();}
            if(Format==RaceFormat.Elimination&&eliminationGates.Add(index)&&UneliminatedCount()>2)
            {eliminationAt=Elapsed+2;Notice="Last place is eliminated in 2 seconds";Changed?.Invoke();}
        }

        public void Collect(PickupKind kind)
        {
            if(State!=RaceState.Racing||!Player||Player.Finished||Player.Eliminated||Player.Recovering)return;
            if(kind==PickupKind.Coin){CollectedCoins++;if(save!=null)save.Data.lifetimeCoins++;}
            else if(kind==PickupKind.Gem)CollectedGems++;
            else CollectedTokens++;
        }

        void CheckFinishCrossing(RaceCompetitor racer)
        {
            if(racer.CheckpointIndex<Definition.course.checkpoints.Length-1||racer.Progress<.94f||!racer.Cursor.AllGatesPassed)return;
            Route.Sample(Route.Length,out var tangent,out float width);tangent.y=0;if(tangent.sqrMagnitude<.01f)tangent=Vector3.forward;tangent.Normalize();
            var goal=Definition.course.finish;var position=racer.Ball.Body.position;
            float after=Vector3.Dot(position-goal,tangent);if(after<0)return;
            var lateral=Vector3.ProjectOnPlane(position-goal,tangent);lateral.y=0;
            if(lateral.magnitude>Mathf.Max(6,width*.5f+.7f)||Mathf.Abs(position.y-goal.y)>3)return;
            float before=Vector3.Dot(racer.PreviousPhysicsPosition-goal,tangent);
            float fraction=after-before>.0001f?Mathf.Clamp01(-before/(after-before)):1;
            float time=Mathf.Max(0,Elapsed-Time.fixedDeltaTime+fraction*Time.fixedDeltaTime);
            racer.PendingFinish=true;finishRequests.Add(new FinishRequest{Racer=racer,Seconds=time});
        }

        void ProcessFinishRequests()
        {
            if(finishRequests.Count==0)return;
            finishRequests.Sort((a,b)=>{int time=a.Seconds.CompareTo(b.Seconds);return time!=0?time:a.Racer.Id.CompareTo(b.Racer.Id);});
            foreach(var request in finishRequests)
            {
                var racer=request.Racer;if(racer.Finished||racer.Eliminated)continue;
                racer.Finished=true;racer.FinishTime=request.Seconds;racer.Position=++finishingCount;
                racer.Cursor.Reset(Route.Length,racer.Ball.Body.position);
                SetActivity(racer,false);racer.Ball.Body.isKinematic=true;racer.Ball.GetComponent<SphereCollider>().enabled=false;
                if(racer.IsPlayer){Notice="Finish registered - waiting for the field";Changed?.Invoke();}
            }
            finishRequests.Clear();
        }

        void BeginRecovery(RaceCompetitor racer)
        {
            racer.Falls++;racer.Recovering=true;racer.RecoveryRemaining=1.25f;racer.FinishArmed=false;racer.PendingFinish=false;
            if(racer.IsPlayer&&save!=null)save.Data.falls++;
            SetActivity(racer,false);racer.Ball.Body.isKinematic=true;
            if(racer.IsPlayer){Notice="Returning to your last race checkpoint";Changed?.Invoke();}
        }

        void UpdateRecovery(RaceCompetitor racer,float dt)
        {
            racer.RecoveryRemaining-=dt;if(racer.RecoveryRemaining>0)return;
            var spawn=racer.CheckpointPosition;Route.Sample(racer.CheckpointDistance,out var tangent,out float width);
            var right=Vector3.Cross(Vector3.up,tangent).normalized;float lane=Mathf.Clamp(((racer.Id%3)-1)*1.6f,-width*.5f+1,width*.5f-1);spawn+=right*lane;
            int count=Physics.OverlapSphereNonAlloc(spawn,.65f,occupancy,1<<8,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(occupancy[i]&&occupancy[i].attachedRigidbody!=racer.Ball.Body){racer.RecoveryRemaining=.25f;return;}
            if(!Physics.SphereCast(spawn+Vector3.up*1.5f,.35f,Vector3.down,out _,2.5f,~((1<<8)|(1<<10)),QueryTriggerInteraction.Ignore))
            {racer.RecoveryRemaining=.25f;return;}
            racer.Ball.Body.isKinematic=false;racer.Ball.Respawn(spawn);racer.Cursor.Reset(racer.CheckpointDistance,spawn);
            racer.PreviousPhysicsPosition=spawn;racer.LastAdvanceDistance=racer.CheckpointDistance;racer.LastAdvanceAt=Elapsed;
            racer.Recovering=false;racer.Ball.Body.isKinematic=false;SetActivity(racer,true);
            if(racer.IsPlayer){Notice="Back in the race";if(builder&&builder.FollowCamera)builder.FollowCamera.Snap();Changed?.Invoke();}
            Recovered?.Invoke(racer);
        }

        static void SetActivity(RaceCompetitor racer,bool active)
        {
            racer.Ball.active=active;racer.Ball.Input.active=active&&racer.IsPlayer;
            if(active)racer.Ball.Body.isKinematic=false;
            racer.Ball.SetExternalControl(!racer.IsPlayer||racer.Driver);
            if(!active){racer.Ball.Input.ResetInput();racer.Ball.SubmitControl(new BallControlCommand(0,0,1));}
        }

        int UneliminatedCount(){int count=0;foreach(var racer in competitors)if(!racer.Eliminated)count++;return count;}
        void EliminateLast()
        {
            SortStandings();RaceCompetitor last=null;
            for(int i=standings.Count-1;i>=0;i--)if(!standings[i].Eliminated&&!standings[i].Finished){last=standings[i];break;}
            if(!last||UneliminatedCount()<=2)return;
            last.Eliminated=true;last.EliminationPlace=UneliminatedCount()+1;last.EliminatedTime=Elapsed;
            SetActivity(last,false);last.Ball.Body.isKinematic=true;last.Ball.GetComponent<SphereCollider>().enabled=false;
            Notice=last.DisplayName+" was eliminated";SortStandings();Changed?.Invoke();
        }

        void SortStandings()
        {
            standings.Clear();standings.AddRange(competitors);standings.Sort(compare);
            for(int i=0;i<standings.Count;i++)standings[i].Position=i+1;
        }
        static int Compare(RaceCompetitor a,RaceCompetitor b)
        {
            if(a.Finished||b.Finished){if(a.Finished!=b.Finished)return a.Finished?-1:1;return a.FinishTime!=b.FinishTime?a.FinishTime.CompareTo(b.FinishTime):a.Id.CompareTo(b.Id);}
            if(a.Eliminated||b.Eliminated){if(a.Eliminated!=b.Eliminated)return a.Eliminated?1:-1;return a.EliminationPlace.CompareTo(b.EliminationPlace);}
            int distance=b.Distance.CompareTo(a.Distance);return distance!=0?distance:a.Id.CompareTo(b.Id);
        }

        void CompleteRun()
        {
            if(rewarded||State!=RaceState.Racing)return;rewarded=true;ProcessFinishRequests();SortStandings();
            var entries=new RaceResultEntry[standings.Count];
            for(int i=0;i<standings.Count;i++)
            {
                var racer=standings[i];entries[i]=new RaceResultEntry{Id=racer.Id,Name=racer.DisplayName,Position=racer.Position,Seconds=racer.Finished?racer.FinishTime:0,Progress=racer.Progress,Falls=racer.Falls,Finished=racer.Finished,Eliminated=racer.Eliminated,IsPlayer=racer.IsPlayer};
                SetActivity(racer,false);
            }
            bool target=Player.Finished&&Player.FinishTime<=Definition.timeTarget;
            Result=new RaceResult{TrackId=Definition.id,Title=Definition.title,Format=Format,Difficulty=Difficulty,PlayerPosition=Player.Position,Competitors=competitors.Count,Seconds=Player.Finished?Player.FinishTime:Elapsed,Clean=Player.Falls==0&&Player.Contacts==0,Victory=Player.Finished&&(Format==RaceFormat.TimeTrial?target:Player.Position==1),TimeTrialSuccess=target,Standings=entries};
            Result.CollectedCoins=CollectedCoins;Result.CollectedGems=CollectedGems;Result.CollectedTokens=CollectedTokens;
            RaceRewards.Award(save,Result,raceCount,Player.Finished);
            if(IsTournament)FinishTournamentRound(entries);
            State=RaceState.Complete;Time.timeScale=0;Changed?.Invoke();Finished?.Invoke();
        }

        void FinishTournamentRound(RaceResultEntry[] entries)
        {
            int[] points={16,10,7,5,3,2,1,0};
            foreach(var entry in entries)
            {
                var value=tournamentStandings.Find(item=>item.Id==entry.Id);
                if(value==null){value=new TournamentStanding{Id=entry.Id,Name=entry.Name};tournamentStandings.Add(value);}
                if(entry.Finished){value.Points+=points[Mathf.Clamp(entry.Position-1,0,7)];value.Finishes++;}
                value.Seconds+=entry.Finished?entry.Seconds:180;
            }
            tournamentStandings.Sort((a,b)=>a.Points!=b.Points?b.Points.CompareTo(a.Points):a.Seconds!=b.Seconds?a.Seconds.CompareTo(b.Seconds):a.Id.CompareTo(b.Id));
            for(int i=0;i<tournamentStandings.Count;i++)tournamentStandings[i].Position=i+1;
            tournamentIndex++;
            if(TournamentComplete)
            {
                var player=tournamentStandings.Find(value=>value.Id==0);Result.TournamentComplete=true;Result.TournamentPosition=player.Finishes>0?player.Position:0;
                Result.TournamentVictory=player.Finishes>0&&player.Position==1;
                RaceRewards.AwardTournament(save,Result,player.Position,player.Seconds,player.Finishes);
            }
        }

        public void Pause()
        {
            if(State!=RaceState.Countdown&&State!=RaceState.Racing)return;
            beforePause=State;State=RaceState.Paused;Time.timeScale=0;foreach(var racer in competitors)SetActivity(racer,false);Changed?.Invoke();
        }
        public void Resume()
        {
            if(State!=RaceState.Paused)return;
            State=beforePause;Time.timeScale=1;
            if(State==RaceState.Racing)foreach(var racer in competitors)if(!racer.Finished&&!racer.Eliminated&&!racer.Recovering)SetActivity(racer,true);
            Changed?.Invoke();
        }
        public void Cancel(){StopRun(true);Changed?.Invoke();}
        void StopRun(bool clearTournament)
        {
            foreach(var racer in competitors)
            {
                if(!racer||!racer.Ball)continue;
                racer.Ball.active=false;racer.Ball.Input.active=false;racer.Ball.Body.isKinematic=false;
                var collider=racer.Ball.GetComponent<SphereCollider>();if(collider)collider.enabled=true;
                if(!racer.IsPlayer){racer.gameObject.SetActive(false);Destroy(racer.gameObject);}
            }
            competitors.Clear();standings.Clear();finishRequests.Clear();Player=null;Route=null;Hazards=Array.Empty<RaceHazard>();Definition=null;Result=null;
            if(playerPathFollower)playerPathFollower.enabled=true;playerPathFollower=null;
            State=RaceState.Idle;Time.timeScale=1;
            if(clearTournament){tournamentTracks=Array.Empty<RaceTrackDefinition>();tournamentStandings.Clear();tournamentIndex=0;}
        }
        void OnDestroy()=>StopRun(true);
    }
}
