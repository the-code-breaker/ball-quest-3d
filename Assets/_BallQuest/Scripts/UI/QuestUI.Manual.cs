using System;
using UnityEngine;

namespace BallQuest
{
    public sealed partial class QuestUI
    {
        void CourseBriefing()
        {
            var level=app.Session.Level;if(!level.streamedCampaign&&!CrazyCoursePresentation.Applies(level)){Label("GET READY",52,90,White,true);return;}
            var card=Panel("New route briefing",safe,new Vector2(0,110),new Vector2(548,360),new Color(.025f,.10f,.19f,.92f));Border(card,C("#5DCBFF"),2);
            Text(level.title.ToUpperInvariant(),30,card,new Vector2(0,137),new Vector2(510,57),White,true);
            var map=Rect("Course route overview",card,new Vector2(0,18),new Vector2(460,172));var route=map.gameObject.AddComponent<CourseRouteGraphic>();route.level=level;route.lineWidth=4;route.raycastTarget=false;route.SetVerticesDirty();
            Text(level.streamedCampaign?ChallengeRules.Description(level.challenge,level.challengeTarget,level.challengeSeconds)+" · +"+level.challengeReward+" coins":level.signatureMechanic+" · "+(level.difficulty>=3?"EXPERT":level.difficulty>=2?"HARD":"SKILL"),19,card,new Vector2(0,-103),new Vector2(510,38),Gold,true);
            Text("GET READY",26,card,new Vector2(0,-146),new Vector2(470,42),White,true);
        }
        public void Expeditions()
        {
            Clear(Backdrop.Menu);Header("ADVENTURES",MainMenu,true);
            string[] subtitles={"Choose a path. Find the hidden vault.","Wait, reverse and cross the machinery.","Roll from floor to wall to ceiling.","Ice, cliffs and narrow mountain roads.","Jump islands above the molten river."};
            string[] symbols={"target","gear","ball","flag","boost"};
            var levels=app.catalog.expeditions??Array.Empty<LevelDefinition>();
            for(int i=0;i<levels.Length;i++)
            {
                var level=levels[i];float y=383-i*177;
                var card=Button(level.title,safe,new Vector2(0,y),new Vector2(610,153),()=>app.PlayExpedition(level.id),i==2?Tone.Purple:i==4?Tone.Red:Tone.Blue,"",false);
                var preview=Resources.Load<Texture2D>("ExpeditionPreviews/"+level.id);
                if(preview)Raw(level.title+" live course preview",card,new Vector2(-207,0),new Vector2(185,125),preview);
                else Icon(symbols[Mathf.Min(i,symbols.Length-1)],card,new Vector2(-239,12),80);
                Text(level.title.ToUpperInvariant(),31,card,new Vector2(93,35),new Vector2(390,48),White,true);
                Text(subtitles[Mathf.Min(i,subtitles.Length-1)],20,card,new Vector2(93,-9),new Vector2(390,45),White);
                int stars=app.Save.Data.expeditions.Find(r=>r.id==level.id)?.stars??0;
                for(int n=0;n<3;n++)Star(card,new Vector2(56+n*37,-51),31,n<stars);
            }
            Button("Classic adventures",safe,new Vector2(0,-548),new Vector2(480,72),Worlds,Tone.Quiet,"CLASSIC COURSES",true,25);
        }
        void ControlSettings(Action back)
        {
            Clear(Backdrop.Menu);Header("CONTROLS & CAMERA",()=>Settings(back),false);var settings=app.Save.Data.settings;
            SettingRow("Touch control","ball",383,settings.touchMode==0?"Precision swipe":"Responsive swipe",()=>{settings.touchMode=1-settings.touchMode;app.ApplySettings();ControlSettings(back);});
            var cameraRow=Panel("Automatic camera",safe,new Vector2(0,267),new Vector2(623,94),C("#254961"));Border(cameraRow,C("#365D77"),1.5f);Icon("gear",cameraRow,new Vector2(-258,1),55);
            Text("CAMERA",27,cameraRow,new Vector2(-97,0),new Vector2(210,63),White,true);Text("Automatic follow",23,cameraRow,new Vector2(145,0),new Vector2(278,60),Muted,true);
            SettingToggle("Camera effects","gear",151,settings.cameraEffects,()=>{settings.cameraEffects=!settings.cameraEffects;app.ApplySettings();ControlSettings(back);});
            Text("TOUCH SENSITIVITY",22,safe,new Vector2(0,52),new Vector2(600,40),Muted,true);
            Slider("Touch sensitivity",0,settings.sensitivity,.4f,2.5f,v=>{settings.sensitivity=v;app.ApplySettings();});
            var guide=Panel("Manual rolling guide",safe,new Vector2(0,-184),new Vector2(604,270),C("#17374F"));
            Text("Swipe in any direction to roll your ball.\nRepeat strokes to build speed; lift to coast.\nSwipe against motion to brake and go back.\nEvery finger gesture controls the ball.\nThe camera follows the ball automatically.\nMouse: drag. Keys: WASD / arrows.",24,guide,Vector2.zero,new Vector2(550,244),White);
            Button("Physics laboratory",safe,new Vector2(0,-409),new Vector2(505,85),app.OpenPhysicsLab,Tone.Blue,"PHYSICS LAB",true,30);
            Button("Save controls",safe,new Vector2(0,-550),new Vector2(440,85),()=>{app.ApplySettings();app.Save.Save();Settings(back);},Tone.Green,"SAVE & BACK",true,30);
        }
    }
}
