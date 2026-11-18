using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    public sealed partial class QuestUI
    {
        int adventurePage;
        RaceFormat raceFormat=RaceFormat.Obstacle;
        RaceDifficulty raceDifficulty=RaceDifficulty.Medium;
        int raceOpponents=4;
        bool tournamentSelection,showCup;
        RaceState shownRaceState=(RaceState)(-1);
        TMP_Text racePlace,raceClock,raceDistance,raceStatus,raceCountdown,raceSpeed;
        TMP_Text[] opponentLabels;
        Image raceProgress;
        float nextRaceHud;
        string lastRaceNotice;
        float noticeTime;

        void ModeButtons()
        {
            Button("Adventure mode",safe,new Vector2(-172,-363),new Vector2(164,54),Expeditions,Tone.Green,"ADVENTURE",true,19);
            Button("Race mode",safe,new Vector2(0,-363),new Vector2(156,54),()=>Races(false),Tone.Blue,"RACE",true,22);
            Button("Tournament mode",safe,new Vector2(172,-363),new Vector2(164,54),()=>Races(true),Tone.Purple,"TOURNAMENT",true,18);
        }
        void LoadingScreen()
        {
            Clear(Backdrop.Menu);Label("GETTING THE TRACK READY",37,110,White,true);Label("Your next challenge is loading...",25,10,Muted,true);
        }
        void RecoveryScreen()
        {
            Clear(Backdrop.Overlay);Label("BACK TO YOUR CHECKPOINT",34,85,White,true);Label("Steady. Ready. Roll.",26,-5,Muted,true);
        }
        public void Races(bool tournament=false)
        {
            if(app.Mode!=GameMode.Adventure){app.MainMenu();}
            tournamentSelection=tournament;shownRaceState=(RaceState)(-1);Clear(Backdrop.Menu);Header(tournament?"TOURNAMENT":"RACE MODE",MainMenu,true);
            if(app.catalog.races==null||app.catalog.races.Length==0){Label("Race tracks are being prepared.",28,0,Muted);return;}
            Button("Race difficulty",safe,new Vector2(-164,452),new Vector2(290,66),()=>{raceDifficulty=(RaceDifficulty)(((int)raceDifficulty+1)%3);Races(tournament);},Tone.Blue,raceDifficulty.ToString().ToUpperInvariant(),true,26);
            Button("Race opponents",safe,new Vector2(164,452),new Vector2(290,66),()=>{raceOpponents=raceOpponents>=7?3:raceOpponents+1;Races(tournament);},Tone.Blue,raceOpponents+" AI BALLS",true,25,raceFormat!=RaceFormat.TimeTrial||tournament);
            if(tournament)
            {
                Label("THE THREE-RACE CUP",31,334,White,true);Label("Cumulative points. One champion.",24,275,Muted,true);
                for(int i=0;i<Mathf.Min(3,app.catalog.races.Length);i++)
                {
                    var track=app.catalog.races[i];var row=Panel("Cup round "+(i+1),safe,new Vector2(0,145-i*151),new Vector2(610,124),C("#214B6B"));Border(row,C("#4B809F"),2);
                    Text((i+1).ToString("00"),44,row,new Vector2(-244,0),new Vector2(90,80),Gold,true);
                    Text(track.title,30,row,new Vector2(24,14),new Vector2(450,54),White,true);Text(track.course.world<WorldNames.Length?WorldNames[track.course.world]:"RACING",19,row,new Vector2(24,-33),new Vector2(450,36),Muted,true);
                }
                bool unlocked=app.Save.Data.unlockedRace>=3;
                Label(unlocked?"Equal ball stats. Real collisions.":"Complete Race 2 to qualify for the cup.",22,-335,Muted,true);
                Button("Start tournament",safe,new Vector2(0,-448),new Vector2(535,101),()=>app.StartTournament(raceDifficulty,raceOpponents),Tone.Green,unlocked?"START TOURNAMENT":"CUP LOCKED",true,31,unlocked);
            }
            else
            {
                Button("Race format",safe,new Vector2(0,355),new Vector2(595,68),()=>{raceFormat=(RaceFormat)(((int)raceFormat+1)%4);Races(false);},Tone.Purple,FormatName(raceFormat)+"   >",true,27);
                for(int i=0;i<app.catalog.races.Length;i++)
                {
                    var track=app.catalog.races[i];bool unlocked=track.id<=app.Save.Data.unlockedRace;
                    var record=app.Save.Data.races.Find(r=>r.id==track.id&&r.format==(int)raceFormat);
                    var row=Button("Race track "+track.id,safe,new Vector2(0,220-i*133),new Vector2(616,111),()=>app.PlayRace(track.id,raceFormat,raceDifficulty,raceOpponents),unlocked?Tone.Blue:Tone.Quiet,"",false,26,unlocked);
                    Text(track.id.ToString("00"),39,row,new Vector2(-248,2),new Vector2(92,78),unlocked?Gold:Muted,true);
                    Text(track.title,28,row,new Vector2(15,20),new Vector2(420,48),White,true);
                    string detail=!unlocked?"Finish the previous race to unlock":raceFormat==RaceFormat.TimeTrial?"TARGET  "+track.timeTarget.ToString("0.0")+"s":record!=null&&record.bestPlace<99?"BEST  #"+record.bestPlace:"PHYSICS RACE  /  "+(raceOpponents+1)+" BALLS";
                    Text(detail,18,row,new Vector2(15,-28),new Vector2(420,35),Muted,true);
                    if(!unlocked)Icon("lock",row,new Vector2(259,0),48);
                }
            }
            Button("Back",safe,new Vector2(0,-574),new Vector2(320,70),MainMenu,Tone.Blue,"BACK",true,25);
        }
        static string FormatName(RaceFormat format)=>format==RaceFormat.TimeTrial?"TIME TRIAL":format==RaceFormat.Elimination?"ELIMINATION":format==RaceFormat.Sprint?"SPRINT RACE":format==RaceFormat.Tournament?"TOURNAMENT":"OBSTACLE RACE";
        public void RaceStateChanged()
        {
            if(app.Mode==GameMode.Adventure||!app.Race)return;
            var state=app.Race.State;if(shownRaceState==state)return;shownRaceState=state;
            if(state==RaceState.Paused){RacePause();return;}
            if(state==RaceState.Complete){showCup=false;RaceResults();return;}
            if(state==RaceState.Countdown||state==RaceState.Racing)RaceHud();
        }
        void RaceHud()
        {
            Clear(Backdrop.None);var race=app.Race;
            Button("Pause race",safe,new Vector2(-294,555),new Vector2(76,78),app.PauseCurrent,Tone.Blue,"II",true,34);
            racePlace=Text("1 / "+race.Competitors.Count,37,safe,new Vector2(0,554),new Vector2(300,62),White,true);
            var coins=Panel("Race wallet",safe,new Vector2(247,555),new Vector2(175,59),new Color(.02f,.13f,.24f,.85f),true);Icon("coin",coins,new Vector2(-52,0),45);hudCoins=Text("0",28,coins,new Vector2(24,0),new Vector2(105,45),White,true);
            var frame=Panel("Race progress frame",safe,new Vector2(0,501),new Vector2(286,18),C("#123D61"),true);
            raceProgress=Panel("Race progress",frame,Vector2.zero,new Vector2(272,10),White).GetComponent<Image>();raceProgress.sprite=solid;raceProgress.type=Image.Type.Filled;raceProgress.fillMethod=Image.FillMethod.Horizontal;Gradient(raceProgress.rectTransform,C("#D8FBFF"),C("#12BCEC"));
            raceClock=Text("0:00.0",25,safe,new Vector2(0,459),new Vector2(300,48),White,true);
            string label=race.IsTournament?"ROUND "+race.TournamentRound+" / "+race.TournamentTotalRounds:FormatName(race.Format);
            Text(label,20,safe,new Vector2(-209,464),new Vector2(255,50),White,true);
            var board=Panel("Live race order",safe,new Vector2(257,230),new Vector2(164,335),new Color(.025f,.12f,.2f,.67f));Border(board,new Color(.3f,.6f,.8f,.4f),1);
            Text("POSITION",18,board,new Vector2(0,137),new Vector2(155,40),Muted,true);opponentLabels=new TMP_Text[8];
            for(int i=0;i<8;i++)opponentLabels[i]=Text("",19,board,new Vector2(0,99-i*34),new Vector2(151,35),White,true);
            raceCountdown=Text("3",132,safe,new Vector2(0,116),new Vector2(500,190),Gold,true);
            raceStatus=Text("",22,safe,new Vector2(0,375),new Vector2(600,72),White,true);
            raceDistance=Text("",23,safe,new Vector2(0,-535),new Vector2(610,50),White,true);
            raceSpeed=Text("",20,safe,new Vector2(-240,-477),new Vector2(180,42),White,true);
            nextRaceHud=0;lastRaceNotice=null;UpdateRaceHud();
        }
        void UpdateRaceHud()
        {
            if(!racePlace||!app.Race||app.Race.State==RaceState.Complete)return;
            if(Time.unscaledTime<nextRaceHud)return;nextRaceHud=Time.unscaledTime+.1f;
            var race=app.Race;racePlace.SetText("{0} / {1}",race.PlayerPosition,race.Competitors.Count);raceClock.SetText("{0:0}:{1:00}",Mathf.FloorToInt(race.Elapsed/60),Mathf.FloorToInt(race.Elapsed)%60);
            raceProgress.fillAmount=race.Progress;hudCoins.SetText("{0}",race.CollectedCoins);raceCountdown.text=race.CountdownText;
            string status=race.Notice;
            if(status!=lastRaceNotice){lastRaceNotice=status;noticeTime=Time.unscaledTime+3;}
            if(race.Player&&race.Player.Recovering)status="RETURNING TO CHECKPOINT";
            else if(race.Player&&race.Player.Eliminated)status="ELIMINATED  /  WAITING FOR THE FIELD";
            else if(race.Player&&race.Player.Finished)status="FINISH REGISTERED  /  WAITING FOR THE FIELD";
            else if(Time.unscaledTime>noticeTime)status="";
            raceStatus.text=status;
            if(race.Format==RaceFormat.TimeTrial)raceDistance.SetText("TARGET {0:1}s / {1}m LEFT",race.Definition.timeTarget,Mathf.CeilToInt(race.DistanceRemaining));
            else raceDistance.SetText("{0}m LEFT / {1} STILL RACING",Mathf.CeilToInt(race.DistanceRemaining),race.RemainingCompetitors);
            if(race.Player&&race.Player.Ball)raceSpeed.SetText("{0:1} m/s",race.Player.Ball.Body.linearVelocity.magnitude);else raceSpeed.SetText("");
            for(int i=0;i<opponentLabels.Length;i++)
            {
                if(i>=race.Standings.Count){opponentLabels[i].text="";continue;}
                var rival=race.Standings[i];opponentLabels[i].text=rival.Position+"  "+rival.DisplayName+(rival.Finished?"  +":rival.Eliminated?"  X":"");opponentLabels[i].color=rival.IsPlayer?Gold:White;
            }
        }
        void RacePause()
        {
            Clear(Backdrop.Overlay);Banner("RACE PAUSED",280,C("#69D6FF"),C("#1877D8"));
            Button("Resume race",safe,new Vector2(0,80),new Vector2(525,104),app.ResumeCurrent,Tone.Green,"RESUME",true,40);
            Button("Restart race",safe,new Vector2(0,-62),new Vector2(525,95),app.RetryCurrent,Tone.Gold,app.Race.IsTournament?"RESTART CUP":"RESTART RACE",true,32);
            Button("Race settings",safe,new Vector2(0,-200),new Vector2(440,80),()=>Settings(RacePause),Tone.Blue,"SETTINGS",true,28);
            Button("Main menu",safe,new Vector2(0,-340),new Vector2(360,80),app.MainMenu,Tone.Blue,"HOME",true,30);
        }
        void RaceResults()
        {
            Clear(Backdrop.Overlay);var race=app.Race;var result=race.Result;if(result==null)return;
            bool playerFinished=result.Standings.Any(r=>r.IsPlayer&&r.Finished);
            bool success=result.TournamentComplete?result.TournamentVictory:result.Victory;
            string title=result.TournamentComplete?(result.TournamentVictory?"CUP CHAMPION!":"CUP COMPLETE!"):!playerFinished?"DID NOT FINISH":result.Format==RaceFormat.TimeTrial?(result.TimeTrialSuccess?"TARGET BEAT!":"TIME TRIAL COMPLETE"):result.Victory?"1ST PLACE!":"RACE COMPLETE!";
            Label(result.Title.ToUpperInvariant(),27,550,White,true);Banner(title,419,success?C("#81F34B"):playerFinished?C("#64C8FF"):C("#FF7664"),success?C("#1DBE12"):playerFinished?C("#197BD8"):C("#CE2525"));
            Text((result.TournamentComplete?(result.TournamentPosition>0?"CUP #"+result.TournamentPosition:"CUP DNF"):"#"+result.PlayerPosition+" / "+result.Competitors)+"   "+RaceTime(result.Seconds),32,safe,new Vector2(0,302),new Vector2(620,61),White,true);
            if(race.IsTournament)Button("Toggle cup standings",safe,new Vector2(0,246),new Vector2(323,52),()=>{showCup=!showCup;RaceResults();},Tone.Purple,showCup?"ROUND RESULTS":"CUP STANDINGS",true,21);
            var table=Panel("Race standings",safe,new Vector2(0,20),new Vector2(612,390),new Color(.015f,.035f,.06f,.85f));Border(table,new Color(1,1,1,.15f),2);
            Text(showCup?"CUMULATIVE POINTS":"FINAL STANDINGS",19,table,new Vector2(0,168),new Vector2(580,38),Muted,true);
            if(showCup)
            {
                for(int i=0;i<Mathf.Min(8,race.TournamentStandings.Count);i++)
                {
                    var row=race.TournamentStandings[i];float y=120-i*39;
                    Text(row.Position+"   "+row.Name,22,table,new Vector2(-105,y),new Vector2(330,36),row.IsPlayer?Gold:White,true).alignment=TextAlignmentOptions.Left;
                    Text(row.Points+" PTS",22,table,new Vector2(182,y),new Vector2(180,36),White,true);
                }
            }
            else
            {
                for(int i=0;i<Mathf.Min(8,result.Standings.Length);i++)
                {
                    var row=result.Standings[i];float y=120-i*39;
                    Text(row.Position+"   "+row.Name,22,table,new Vector2(-105,y),new Vector2(330,36),row.IsPlayer?Gold:White,true).alignment=TextAlignmentOptions.Left;
                    Text(row.Finished?RaceTime(row.Seconds):row.Eliminated?"OUT":"DNF",21,table,new Vector2(184,y),new Vector2(175,36),row.Finished?White:Muted,true);
                }
            }
            var rewards=Panel("Race rewards",safe,new Vector2(0,-235),new Vector2(510,78),new Color(.015f,.035f,.06f,.76f),true);
            Icon("coin",rewards,new Vector2(-167,0),52);Text("+"+result.Coins,33,rewards,new Vector2(-73,0),new Vector2(145,62),White,true);
            Icon("gem",rewards,new Vector2(68,0),50);Text("+"+result.Gems,31,rewards,new Vector2(155,0),new Vector2(120,58),White,true);
            Button("Next race",safe,new Vector2(0,-348),new Vector2(532,92),app.NextRace,Tone.Green,race.IsTournament&&!race.TournamentComplete?"NEXT ROUND  >":race.TournamentComplete?"TOURNAMENTS":"NEXT RACE  >",true,32);
            Button("Retry race",safe,new Vector2(0,-455),new Vector2(440,76),app.RetryCurrent,Tone.Gold,race.IsTournament?"RESTART CUP":"RETRY RACE",true,26);
            Button("Main menu",safe,new Vector2(0,-558),new Vector2(340,70),app.MainMenu,Tone.Blue,"HOME",true,28);
            if(success){var confetti=Rect("Race celebration",safe,Vector2.zero,Vector2.zero);Stretch(confetti);confetti.gameObject.AddComponent<ArcadeConfetti>().Begin();}
        }
        static string RaceTime(float seconds)
        {int minutes=(int)seconds/60;return minutes+":"+(seconds%60).ToString("00.0");}
    }
}
