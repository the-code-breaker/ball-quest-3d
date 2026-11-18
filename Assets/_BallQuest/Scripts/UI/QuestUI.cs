using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallQuest
{
    public sealed partial class QuestUI : MonoBehaviour
    {
        GameBootstrap app;
        RectTransform safe;
        TMP_FontAsset font;
        Material outlined, titleGold, titleBlue;
        Sprite round, pill, solid;
        Texture2D roundTexture, pillTexture, solidTexture;
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        readonly List<Sprite> ownedSprites = new List<Sprite>();
        CanvasGroup group;
        Coroutine fade;
        TMP_Text hudCoins, hudNotice, hudSpeed;
        TMP_Text hudChallenge;
        TMP_Text walletText;
        Image progressFill;
        RectTransform noticePlate, swipeHint, turboStatus;
        float nextAdventureHud;
        string toast;
        string spinToast;
        int skinIndex, skinPage, skinFilter, missionPage, shopPage, shopTab;
        int worldFilter = -1;
        int chapterPage;
        bool spinning;
        float spinAngle;
        enum Backdrop { Home, Menu, Overlay, None }
        enum Tone { Blue, Green, Gold, Red, Quiet, Purple }
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static readonly Color White = C("#FFFDF5"), Muted = C("#ADCAE3"), Dark = C("#092235"), Gold = C("#FFD83C");
        static readonly string[] IconOrder = { "coin", "gem", "calendar", "target", "gift", "cart", "ball", "gear", "trophy", "home", "speaker", "music", "vibrate", "lock", "check", "back" };
        static readonly string[] WorldNames = { "SKY HARBOR", "DESERT CITADEL", "FROZEN HEIGHTS", "NEON CITY" };

        public void Initialize(GameBootstrap bootstrap)
        {
            app = bootstrap; font = app.catalog.font ? app.catalog.font : TMP_Settings.defaultFontAsset;
            if (!font) throw new InvalidOperationException("Generate the Ball Quest UI font before playing.");
            round = Rounded(128, 25, out roundTexture); pill = Rounded(128, 62, out pillTexture);
            solidTexture=new Texture2D(2,2,TextureFormat.RGBA32,false){name="Solid UI fill"};
            solidTexture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});solidTexture.Apply();
            solid=Sprite.Create(solidTexture,new Rect(0,0,2,2),Vector2.one*.5f,100);
            outlined = FontMaterial(C("#092C45"), .105f); titleGold = FontMaterial(C("#602100"), .22f); titleBlue = FontMaterial(C("#032A43"), .2f);
            var atlas = Resources.Load<Texture2D>("Arcade/IconAtlas");
            if (atlas)
            {
                float w = atlas.width / 4f, h = atlas.height / 4f;
                for (int i = 0; i < IconOrder.Length; i++)
                {
                    var sprite = Sprite.Create(atlas, new Rect((i % 4) * w, atlas.height - (i / 4 + 1) * h, w, h), Vector2.one * .5f);
                    icons[IconOrder[i]] = sprite; ownedSprites.Add(sprite);
                }
            }
            var canvas = new GameObject("Ball Quest UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            safe = Rect("Safe area", canvas.transform, Vector2.zero, Vector2.zero); Stretch(safe); safe.gameObject.AddComponent<SafeArea>(); safe.gameObject.AddComponent<ArcadeSafeContentScale>(); group = safe.gameObject.AddComponent<CanvasGroup>();
            app.Session.Changed += StateChanged;
        }
        Material FontMaterial(Color edge, float width)
        {
            var material = new Material(font.material); material.SetColor("_OutlineColor", edge); material.SetFloat("_OutlineWidth", width);
            material.SetFloat("_FaceDilate", width > .15f ? .025f : .075f); material.EnableKeyword("OUTLINE_ON"); return material;
        }
        Sprite Rounded(int size, float radius, out Texture2D texture)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 q = new Vector2(Mathf.Abs(x + .5f - size * .5f), Mathf.Abs(y + .5f - size * .5f)) - Vector2.one * (size * .5f - radius);
                float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(.5f - distance)));
            }
            texture.Apply(); return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
        void StateChanged()
        {
            if(app.Session.State==SessionState.LevelLoading){LoadingScreen();return;}
            if(app.Mode!=GameMode.Adventure)return;
            switch (app.Session.State)
            {
                case SessionState.Playing: Hud(); break;
                case SessionState.Ready: Hud();CourseBriefing();break;
                case SessionState.Falling: Hud();break;
                case SessionState.Respawning: RecoveryScreen();break;
                case SessionState.Paused: PauseMenu(); break;
                case SessionState.Fallen: Fallen(); break;
                case SessionState.Complete: Complete(); break;
            }
        }
        void Clear(Backdrop backdrop)
        {
            if (fade != null) StopCoroutine(fade);
            for (int i = safe.childCount - 1; i >= 0; i--) { var child = safe.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
            hudCoins = hudNotice = hudSpeed = walletText = hudChallenge = null; progressFill = null; noticePlate = swipeHint = turboStatus = null;
            group.alpha = .02f; fade = StartCoroutine(FadeIn());
            if (backdrop == Backdrop.Home) HomeBackdrop();
            if (backdrop == Backdrop.Menu)
            {
                var bg = Panel("Menu background", safe, Vector2.zero, Vector2.zero, Color.white); Stretch(bg, 200); Gradient(bg, C("#244C72"), C("#0C2336"));
                var texture = Resources.Load<Texture2D>("Arcade/HomeBackdrop");
                if (texture) { var art = Raw("Distant world", bg, Vector2.zero, Vector2.zero, texture); Stretch(art); art.GetComponent<RawImage>().color = new Color(.5f, .72f, 1, .055f); }
            }
            if (backdrop == Backdrop.Overlay)
            {
                var shade = Panel("Paused world dimmer", safe, Vector2.zero, Vector2.zero, new Color(.008f, .022f, .04f, .925f)); Stretch(shade, 200); shade.GetComponent<Image>().raycastTarget = true;
            }
        }
        IEnumerator FadeIn()
        {
            while (group && group.alpha < 1) { group.alpha = Mathf.MoveTowards(group.alpha, 1, Time.unscaledDeltaTime * 9); yield return null; }
            fade = null;
        }
        void HomeBackdrop()
        {
            var bg = Panel("Sky blue backdrop", safe, Vector2.zero, Vector2.zero, C("#28AEF5")); Stretch(bg, 200);
            var texture = Resources.Load<Texture2D>("Arcade/HomeBackdrop");
            if (texture)
            {
                var art = Raw("Sky city and wooden skyway", safe, Vector2.zero, Vector2.zero, texture); Stretch(art, 120);
                var fitter = art.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fitter.aspectRatio = (float)texture.width / texture.height;
            }
            var bottom = Panel("Bottom readability", safe, new Vector2(0, -530), new Vector2(1100, 310), Color.white); Gradient(bottom, new Color(.04f, .14f, .2f, 0), new Color(.02f, .1f, .15f, .38f));
        }

        public void Splash()
        {
            Clear(Backdrop.Home); Brand(345); Preview(app.catalog.Skin(app.Save.Data.selectedSkin), new Vector2(0, -190), 320, false);
            var rail = Panel("Loading rail", safe, new Vector2(0, -465), new Vector2(430, 24), C("#123D63"), true);
            var fill = Panel("Loading fill", rail, Vector2.zero, new Vector2(414, 12), Color.white, true); Gradient(fill, C("#BCFF5C"), C("#25BA15")); Label("GET READY TO ROLL!", 27, -525, White, true);
        }
        public void MainMenu()
        {
            Clear(Backdrop.Home); ArcadeProgress.Refresh(app.Save.Data); PlayerPlate(); Wallet(new Vector2(215, 552), true); Brand(365);
            SideAction("Daily rewards", "DAILY", "calendar", -269, 125, DailyRewards, ArcadeProgress.CanClaimDaily(app.Save.Data));
            SideAction("Missions", "MISSIONS", "target", -269, -25, Missions, ArcadeProgress.Missions(app.Save.Data).Any(m => !m.claimed && m.current >= m.target));
            SideAction("Rewards", "REWARDS", "gift", -269, -175, DailyRewards, false);
            SideAction("Spin", "SPIN", "trophy", 269, 125, Spin, ArcadeProgress.CanSpin(app.Save.Data));
            SideAction("Shop", "SHOP", "cart", 269, -25, Shop, false); SideAction("Ball collection", "BALLS", "ball", 269, -175, Collection, false);
            Preview(app.catalog.Skin(app.Save.Data.selectedSkin), new Vector2(0, -222), 300, false);
            int nextLevel=Mathf.Clamp(app.Save.Data.unlockedLevel,1,app.catalog.levels.Length);
            var play = Button("PLAY", safe, new Vector2(0, -455), new Vector2(500, 119),()=>app.Play(nextLevel), Tone.Gold, "PLAY", true, 51);
            Text("Level "+nextLevel+" · "+app.catalog.levels[nextLevel-1].title, 19, play, new Vector2(0, -35), new Vector2(442, 30), White, true);
            Button("Worlds & levels", safe, new Vector2(-126, -576), new Vector2(240, 71), Worlds, Tone.Purple, "WORLDS", true, 25); Icon("trophy", safe, new Vector2(-220, -575), 39);
            Button("Settings", safe, new Vector2(160, -576), new Vector2(202, 71), () => Settings(MainMenu), Tone.Blue, "SETTINGS", true, 22); Icon("gear", safe, new Vector2(86, -576), 34);
            ModeButtons();
            if (!string.IsNullOrEmpty(app.Save.LastError)) Label(app.Save.LastError, 16, -624, Gold);
        }
        void Brand(float y)
        {
            var shadow = Text("QUEST", 113, safe, new Vector2(3, y - 93), new Vector2(620, 145), C("#031F37"), true); shadow.fontSharedMaterial = titleBlue;
            var upper = Text("BALL", 99, safe, new Vector2(2, y + 5), new Vector2(610, 125), C("#6A2500"), true); upper.fontSharedMaterial = titleGold;
            var ball = Text("BALL", 99, safe, new Vector2(0, y + 13), new Vector2(610, 125), White, true); ball.fontSharedMaterial = titleGold;
            ball.enableVertexGradient = true; ball.colorGradient = new VertexGradient(C("#FFF259"), C("#FFF259"), C("#FF940A"), C("#FF940A"));
            var quest = Text("QUEST", 113, safe, new Vector2(0, y - 83), new Vector2(620, 145), White, true); quest.fontSharedMaterial = titleBlue;
            quest.enableVertexGradient = true; quest.colorGradient = new VertexGradient(C("#D2FCFF"), C("#D2FCFF"), C("#1DA6FF"), C("#1DA6FF"));
            ball.rectTransform.localRotation = Quaternion.Euler(0, 0, 2); quest.rectTransform.localRotation = Quaternion.Euler(0, 0, -1);
        }
        void PlayerPlate()
        {
            var root = Panel("Player profile", safe, new Vector2(-177, 552), new Vector2(282, 76), new Color(.035f, .18f, .33f, .84f), true);
            Panel("Avatar ring", root, new Vector2(-103, 0), new Vector2(68, 68), C("#9DEBFF"), true); Raw("Player ball avatar", root, new Vector2(-103, 0), new Vector2(61, 61), BallPreview.GetThumbnail(app.catalog.Skin(app.Save.Data.selectedSkin)));
            Text("Player", 26, root, new Vector2(17, 13), new Vector2(175, 36), White, true).alignment = TextAlignmentOptions.Left;
            Text("Lv. " + (app.Save.Data.levels.Count(r => r.stars > 0) + 1), 21, root, new Vector2(17, -17), new Vector2(175, 30), Gold, true).alignment = TextAlignmentOptions.Left;
        }
        void Wallet(Vector2 at, bool plus = false)
        {
            var root = Panel("Coin wallet", safe, at, new Vector2(216, 68), new Color(.025f, .17f, .3f, .9f), true); Icon("coin", root, new Vector2(-77, 0), 49);
            walletText = Text(app.Save.Data.coins.ToString("N0"), 28, root, new Vector2(-2, 1), new Vector2(128, 50), White, true);
            if (plus) Button("Open shop", root, new Vector2(84, 0), new Vector2(42, 45), Shop, Tone.Green, "+", true, 33);
        }
        void SideAction(string name, string caption, string icon, float x, float y, Action action, bool badge)
        {
            var card = Button(name, safe, new Vector2(x, y), new Vector2(112, 126), action, Tone.Quiet, "", false); Icon(icon, card, new Vector2(0, 17), 77);
            Text(caption, 18, card, new Vector2(0, -42), new Vector2(108, 32), White, true);
            if (badge) { var dot = Panel("New reward", card, new Vector2(43, 52), new Vector2(30, 30), C("#FF443D"), true); Text("!", 22, dot, Vector2.zero, Vector2.one * 28, White, true); }
        }

        public void Worlds()
        {
            Clear(Backdrop.Menu); Header("CHALLENGES", MainMenu, true);
            var groups = CampaignChapters.Courses(app.catalog).GroupBy(l => (l.id-1)/CampaignChapters.Size).OrderBy(g => g.Key).ToArray();
            int chapterPages=Mathf.Max(1,(groups.Length+3)/4);chapterPage=Mathf.Clamp(chapterPage,0,chapterPages-1);
            for (int i = 0; i < Mathf.Min(4,groups.Length-chapterPage*4); i++)
            {
                var courses = groups[chapterPage*4+i].ToArray(); int id = groups[chapterPage*4+i].Key; bool unlocked = courses.Any(l => l.id <= app.Save.Data.unlockedLevel);
                int stars = courses.Sum(l => app.Save.Data.levels.Find(r => r.id == l.id)?.stars ?? 0);
                float height = groups.Length > 3 ? 203 : 259, y = groups.Length > 3 ? 355 - i * 232 : 325 - i * 294;
                var card = Button("Chapter "+(id+1), safe, new Vector2(0, y), new Vector2(626, height), () => { worldFilter=id;adventurePage=0;Levels(); }, Tone.Blue, "", false, 24, unlocked);
                WorldArt(card, id, new Vector2(0, -5), new Vector2(609, height - 15));
                var shade = Panel("World title shade", card, new Vector2(0, height * .5f - 31), new Vector2(609, 54), new Color(.015f, .08f, .16f, .5f));
                Text(CampaignChapters.Title(id)+"  ·  "+courses[0].id+"–"+courses[courses.Length-1].id, 29, shade, new Vector2(-52, 1), new Vector2(470, 48), White, true).alignment = TextAlignmentOptions.Left;
                var count = Panel("World stars", card, new Vector2(205, -height * .5f + 34), new Vector2(172, 49), new Color(.02f, .09f, .16f, .8f), true);
                Star(count, new Vector2(-54, 0), 34, true); Text(stars + "/" + courses.Length * 3, 25, count, new Vector2(25, 0), new Vector2(118, 42), White, true);
                if (!unlocked) Icon("lock", card, new Vector2(230, 17), 80);
            }
            if(chapterPages>1)
            {
                Button("Previous chapter page",safe,new Vector2(-230,-492),new Vector2(120,55),()=>{chapterPage--;Worlds();},Tone.Blue,"<",true,28,chapterPage>0);
                Text((chapterPage+1)+" / "+chapterPages,21,safe,new Vector2(0,-492),new Vector2(150,40),White,true);
                Button("Next chapter page",safe,new Vector2(230,-492),new Vector2(120,55),()=>{chapterPage++;Worlds();},Tone.Blue,">",true,28,chapterPage+1<chapterPages);
            }
            Button("All levels", safe, new Vector2(0, -568), new Vector2(330, 71), () => { worldFilter = -1;adventurePage=0; Levels(); }, Tone.Blue, "ALL CHALLENGES", true, 26);
        }
        void WorldArt(Transform parent, int world, Vector2 at, Vector2 dimensions)
        {
            var grand=Resources.Load<Texture2D>("GrandCampaign/WorldAtlasV11");if(grand){int cell=Mathf.Abs(world)%20;Raw("Generated world illustration",parent,at,dimensions,grand).GetComponent<RawImage>().uvRect=new Rect((cell%5)/5f,(3-cell/5)/4f,1f/5,1f/4);return;}
            var atlas = Resources.Load<Texture2D>("Arcade/WorldAtlas"); if (!atlas) return;
            Raw("World panorama", parent, at, dimensions, atlas).GetComponent<RawImage>().uvRect = new Rect(0, (3 - Mathf.Clamp(world, 0, 3)) * .25f, 1, .25f);
        }
        public void Levels()
        {
            Clear(Backdrop.Menu); Header(worldFilter < 0 ? "CHALLENGES" : CampaignChapters.Title(worldFilter), Worlds, false);
            var levels = CampaignChapters.Courses(app.catalog,worldFilter); int total = levels.Sum(l => app.Save.Data.levels.Find(r => r.id == l.id)?.stars ?? 0);
            int pages=Mathf.Max(1,(levels.Length+11)/12);adventurePage=Mathf.Clamp(adventurePage,0,pages-1);
            Star(safe, new Vector2(-108, 475), 34, true); Text(total + "/" + levels.Length * 3, 26, safe, new Vector2(20, 475), new Vector2(200, 50), White, true);
            for (int i = 0; i < Mathf.Min(12,levels.Length-adventurePage*12); i++)
            {
                var level = levels[adventurePage*12+i]; bool unlocked = level.id <= app.Save.Data.unlockedLevel; int stars = app.Save.Data.levels.Find(r => r.id == level.id)?.stars ?? 0;
                var card = Button(level.id.ToString("00") + " " + level.title, safe, new Vector2((i % 3 - 1) * 207, 337 - i / 3 * 195), new Vector2(181, 168), () => app.Play(level.id), !unlocked ? Tone.Quiet : stars > 0 ? Tone.Green : Tone.Blue, "", false, 30, unlocked);
                Text(level.id.ToString(),level.id>=1000?22:level.id>=100?25:29,card,new Vector2(-54,54),new Vector2(64,40),unlocked?White:Muted,true);
                Text(level.difficulty>=4?"EXTREME":level.difficulty>=2?"HARD":"SKILL",13,card,new Vector2(36,54),new Vector2(101,34),unlocked?Gold:Muted,true);
                var route=Rect("Actual route silhouette",card,new Vector2(0,7),new Vector2(145,61));var graphic=route.gameObject.AddComponent<CourseRouteGraphic>();graphic.level=level;graphic.color=unlocked?Color.white:new Color(.55f,.64f,.72f,.6f);graphic.raycastTarget=false;graphic.SetVerticesDirty();
                if(!unlocked)Icon("lock",card,new Vector2(0,6),37);
                else if(app.Save.Data.levels.Find(r=>r.id==level.id)?.challengeCompleted==true)Icon("check",card,new Vector2(65,-9),27);
                Text(level.title,16,card,new Vector2(0,-38),new Vector2(164,37),unlocked?White:Muted,true);
                for (int star = 0; star < 3; star++) Star(card, new Vector2((star - 1) * 32, -66), 26, star < stars);
            }
            if (levels.Length <= 6)
            {
                Label("Master the route. Collect the stars.", 23, -218, Muted);
                Button("View all courses", safe, new Vector2(0, -335), new Vector2(445, 82), () => { worldFilter = -1;adventurePage=0;Levels(); }, Tone.Blue, "ALL "+app.catalog.levels.Length+" LEVELS", true, 29);
            }
            if(pages>1)
            {
                Button("Previous adventure page",safe,new Vector2(-190,-476),new Vector2(140,62),()=>{adventurePage--;Levels();},Tone.Blue,"<",true,30,adventurePage>0);
                Text((adventurePage+1)+" / "+pages,25,safe,new Vector2(0,-478),new Vector2(160,45),White,true);
                Button("Next adventure page",safe,new Vector2(190,-476),new Vector2(140,62),()=>{adventurePage++;Levels();},Tone.Blue,">",true,30,adventurePage+1<pages);
            }
            Button("Back", safe, new Vector2(0, -571), new Vector2(310, 69), Worlds, Tone.Quiet, "BACK", true, 26);
        }
        void Hud()
        {
            Clear(Backdrop.None); Button("II", safe, new Vector2(-294, 555), new Vector2(76, 78), app.Session.Pause, Tone.Blue, "II", true, 34);
            Text(app.Session.Level.expedition?app.Session.Level.title.ToUpperInvariant():"LEVEL " + app.Session.Level.id, 36, safe, new Vector2(0, 557), new Vector2(300, 62), White, true);
            var wallet = Panel("Run wallet", safe, new Vector2(246, 555), new Vector2(175, 59), new Color(.025f, .16f, .28f, .87f), true); Icon("coin", wallet, new Vector2(-53, 0), 47);
            hudCoins = Text("0", 29, wallet, new Vector2(24, 0), new Vector2(112, 48), White, true);
            var track = Panel("Progress frame", safe, new Vector2(0, 502), new Vector2(286, 18), C("#0A3656"), true); Border(track,C("#092B42"),1.4f);
            Panel("Progress track", track, Vector2.zero, new Vector2(282, 14), C("#28689A"), true);
            progressFill = Panel("Progress fill", track, Vector2.zero, new Vector2(272, 10), White).GetComponent<Image>();
            progressFill.sprite=solid; progressFill.type = Image.Type.Filled; progressFill.fillMethod = Image.FillMethod.Horizontal; progressFill.fillAmount = 0; Gradient(progressFill.rectTransform, C("#D3FAFF"), C("#15C4F8"));
            noticePlate = Panel("Course notice", safe, new Vector2(0, 442), new Vector2(560, 52), new Color(.015f, .13f, .24f, .7f), true); hudNotice = Text(app.Session.Level.hint, 20, noticePlate, Vector2.zero, new Vector2(528, 48), White, true);
            if(app.Session.Level.challenge!=ChallengeKind.None){var objective=Panel("Challenge objective",safe,new Vector2(0,389),new Vector2(620,38),new Color(.02f,.08f,.16f,.76f),true);hudChallenge=Text(app.Session.ChallengeProgress,16,objective,Vector2.zero,new Vector2(600,35),Gold,true);}
            turboStatus = Panel("Turbo active", safe, new Vector2(292, 416), new Vector2(72, 77), C("#173D2E"), true); Text(">>", 35, turboStatus, Vector2.zero, new Vector2(64, 65), C("#72FF28"), true); turboStatus.localRotation = Quaternion.Euler(0, 0, 90);
            var speed = Panel("Speed", safe, new Vector2(-250, -470), new Vector2(140, 45), new Color(.015f, .11f, .23f, .68f), true); hudSpeed = Text("0.0 m/s", 20, speed, Vector2.zero, new Vector2(132, 41), White, true);
            swipeHint = Rect("Swipe to move guide", safe, new Vector2(0, -485), new Vector2(410, 125)); Text("<                  >", 42, swipeHint, new Vector2(0, 28), new Vector2(400, 55), White, true);
            var hand = Rect("Swipe hand", swipeHint, new Vector2(0, -10), new Vector2(70, 85)); hand.gameObject.AddComponent<ArcadeHandGraphic>().raycastTarget = false; Text("SWIPE TO ROLL", 24, swipeHint, new Vector2(0, -65), new Vector2(470, 46), White, true);
            Text("Swipe freely · automatic camera",19,swipeHint,new Vector2(0,-103),new Vector2(470,34),White);
        }
        void Update()
        {
            if(app.Mode!=GameMode.Adventure){UpdateRaceHud();return;}
            if (!hudCoins || !app.Session.Ball || Time.unscaledTime<nextAdventureHud) return;
            nextAdventureHud=Time.unscaledTime+.1f;var ball=app.Session.Ball;hudCoins.SetText("{0}",app.Session.Coins);
            if(hudChallenge)hudChallenge.text=app.Session.ChallengeProgress;
            var path=ball.GetComponent<CoursePathFollower>();
            progressFill.fillAmount=path?path.Progress01:Mathf.InverseLerp(app.Session.Level.spawn.z,app.Session.Level.finish.z,ball.transform.position.z);
            if(ball.Boosted)hudSpeed.SetText("TURBO!");else hudSpeed.SetText("{0:1} m/s",ball.Body.linearVelocity.magnitude);
            noticePlate.gameObject.SetActive(app.Session.Seconds < app.Session.NoticeUntil || ball.Boosted); hudNotice.text = ball.Boosted ? "TURBO BOOST!" : app.Session.Notice;
            turboStatus.gameObject.SetActive(ball.Boosted); swipeHint.gameObject.SetActive(app.Session.Seconds < 6);
        }
        void PauseMenu()
        {
            Clear(Backdrop.Overlay); Label("LEVEL " + app.Session.Level.id, 33, 440, White, true); Banner("PAUSED", 260, C("#55C7FF"), C("#137ADF"));
            Button("Resume", safe, new Vector2(0, 35), new Vector2(525, 105), app.Session.Resume, Tone.Green, "RESUME", true, 42);
            Button("Restart course", safe, new Vector2(0, -105), new Vector2(525, 94), app.RetryCurrent, Tone.Gold, "RESTART", true, 36);
            Button("Settings", safe, new Vector2(0, -238), new Vector2(440, 83), () => Settings(PauseMenu), Tone.Blue, "SETTINGS", true, 31);
            Button("Main menu", safe, new Vector2(0, -365), new Vector2(340, 78), app.MainMenu, Tone.Blue, "HOME", true, 30);
        }
        void Fallen()
        {
            Clear(Backdrop.Overlay); Label("LEVEL " + app.Session.Level.id, 34, 465, White, true); Banner("LEVEL FAILED!", 290, C("#FF5C53"), C("#D71917")); Label("Don't give up!", 28, 180, White, true);
            Preview(app.catalog.Skin(app.Save.Data.selectedSkin), new Vector2(0, -14), 290, false);
            Button("Return to checkpoint", safe, new Vector2(0, -300), new Vector2(555, 111), app.Session.Respawn, Tone.Gold, "RETRY", true, 45);
            Button("Main menu", safe, new Vector2(0, -447), new Vector2(365, 87), app.MainMenu, Tone.Blue, "HOME", true, 33); Icon("home", safe, new Vector2(-105, -445), 44);
            Button("Restart course", safe, new Vector2(0, -555), new Vector2(330, 58), app.RetryCurrent, Tone.Quiet, "RESTART LEVEL", true, 23);
        }
        void Complete()
        {
            Clear(Backdrop.Overlay);
            var confetti = Rect("Completion confetti", safe, Vector2.zero, Vector2.zero); Stretch(confetti); confetti.gameObject.AddComponent<ArcadeConfetti>().Begin();
            Label("LEVEL " + app.Session.Level.id, 36, 492, White, true); Banner("LEVEL COMPLETE!", 343, C("#7BF040"), C("#21B916"));
            var stars = new RectTransform[3]; for (int i = 0; i < 3; i++) stars[i] = Star(safe, new Vector2((i - 1) * 135, i == 1 ? 180 : 158), i == 1 ? 132 : 116, i < app.Session.Stars); StartCoroutine(PopStars(stars));
            var reward = Panel("Reward glass", safe, new Vector2(0, -45), new Vector2(492, 242), new Color(.015f, .025f, .04f, .66f)); Border(reward, new Color(1, 1, 1, .16f), 2);
            Text("Rewards", 29, reward, new Vector2(0, 83), new Vector2(380, 44), White, true); Icon("coin", reward, new Vector2(-89, 20), 65); Text("+" + app.Session.BankedReward, 49, reward, new Vector2(52, 20), new Vector2(238, 71), White, true);
            Icon("gem", reward, new Vector2(-88, -65), 61); Text("+" + app.Session.Gems, 41, reward, new Vector2(50, -65), new Vector2(230, 65), White, true);
            if(app.Session.Level.challenge!=ChallengeKind.None)Text(app.Session.ChallengeCompleted?(app.Session.ChallengeReward>0?"CHALLENGE CLEAR  +"+app.Session.ChallengeReward+" COINS":"CHALLENGE CLEAR · REPLAY"):"BONUS CHALLENGE · TRY AGAIN",21,safe,new Vector2(0,-205),new Vector2(630,50),app.Session.ChallengeCompleted?Gold:Muted,true);
            var nextLevel=app.Session.Level.expedition?app.catalog.Expedition(app.Session.Level.id+1):CampaignChapters.Next(app.catalog,app.Session.Level.id);bool next=nextLevel;
            Button(next ? "Next course" : "Back to challenges", safe, new Vector2(0, -294), new Vector2(555, 110), () => { if (next){if(nextLevel.expedition)app.PlayExpedition(nextLevel.id);else app.Play(nextLevel.id);} else { bool expedition=app.Session.Level.expedition;app.MainMenu();if(expedition)Expeditions();else{worldFilter = -1;adventurePage=0; Levels();} } }, Tone.Green, next ? "NEXT LEVEL   >" : "LEVELS", true, 40);
            Button("Main menu", safe, new Vector2(0, -445), new Vector2(368, 86), app.MainMenu, Tone.Blue, "HOME", true, 33); Icon("home", safe, new Vector2(-106, -444), 42);
            Button("Roll again", safe, new Vector2(0, -550), new Vector2(330, 57), app.RetryCurrent, Tone.Quiet, "ROLL AGAIN", true, 23);
        }
        void Banner(string title, float y, Color top, Color bottom)
        {
            var root = Rect("Result ribbon", safe, new Vector2(0, y), new Vector2(666, 145)); var graphic = root.gameObject.AddComponent<ArcadeRibbonGraphic>(); graphic.top = top; graphic.bottom = bottom; graphic.raycastTarget = false;
            Text(title, 48, root, new Vector2(0, 8), new Vector2(590, 110), White, true); root.gameObject.AddComponent<ArcadePop>();
        }
        IEnumerator PopStars(RectTransform[] stars)
        {
            float t = 0; while (t < .85f)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < stars.Length; i++) { if (!stars[i]) yield break; float p = Mathf.Clamp01((t - i * .12f) / .42f); stars[i].localScale = Vector3.one * Mathf.Max(0, 1 + 2.7f * Mathf.Pow(p - 1, 3) + 1.7f * Mathf.Pow(p - 1, 2)); }
                yield return null;
            }
        }

        public void Collection()
        {
            Clear(Backdrop.Menu); Header("BALLS", MainMenu, true); var skins = FilteredSkins(); if (skins.Length == 0) skins = app.catalog.skins;
            skinIndex = (skinIndex + skins.Length) % skins.Length; var skin = skins[skinIndex]; Preview(skin, new Vector2(0, 293), 316, true);
            Button("Previous ball", safe, new Vector2(-273, 297), new Vector2(62, 80), () => { skinIndex--; Collection(); }, Tone.Quiet, "<", true, 45);
            Button("Next ball", safe, new Vector2(273, 297), new Vector2(62, 80), () => { skinIndex++; Collection(); }, Tone.Quiet, ">", true, 45);
            Text(skin.displayName.ToUpperInvariant(), 28, safe, new Vector2(0, 110), new Vector2(610, 54), White, true);
            Text(BallAudioProfile.ResolveMaterial(null,skin).ToString().ToUpperInvariant()+" · MATERIAL SOUND",14,safe,new Vector2(0,76),new Vector2(480,26),Muted);
            Tab("ALL", -211, 37, skinFilter == 0, () => { skinFilter = 0; skinPage = skinIndex = 0; Collection(); });
            Tab("PREMIUM", 0, 37, skinFilter == 1, () => { skinFilter = 1; skinPage = skinIndex = 0; Collection(); });
            Tab("SPECIAL", 211, 37, skinFilter == 2, () => { skinFilter = 2; skinPage = skinIndex = 0; Collection(); }); skinPage = Mathf.Clamp(skinPage, 0, (skins.Length - 1) / 12);
            for (int cell = 0; cell < 12; cell++)
            {
                int index = skinPage * 12 + cell; if (index >= skins.Length) break; var item = skins[index]; bool owned = app.Save.Data.skins.Contains(item.id), selected = item.id == app.Save.Data.selectedSkin;
                var card = Button("Skin " + item.id, safe, new Vector2((cell % 4 - 1.5f) * 151, -84 - cell / 4 * 144), new Vector2(134, 126), () => { skinIndex = index; Collection(); }, selected ? Tone.Green : Tone.Quiet, "", false);
                Raw("Ball thumbnail", card, new Vector2(0, 1), Vector2.one * 119, BallPreview.GetThumbnail(item)); if (selected) Icon("check", card, new Vector2(48, -43), 35); else if (!owned) Icon("lock", card, new Vector2(49, -42), 29);
            }
            bool hasSkin = app.Save.Data.skins.Contains(skin.id), equipped = app.Save.Data.selectedSkin == skin.id; string label = equipped ? "EQUIPPED" : hasSkin ? "EQUIP" : "UNLOCK  " + skin.price.ToString("N0");
            Button("Equip selected skin", safe, new Vector2(0, -518), new Vector2(464, 83), () =>
            {
                if (!hasSkin) { if (app.Save.Data.coins < skin.price) return; app.Save.Data.coins -= skin.price; app.Save.Data.skins.Add(skin.id); }
                app.Save.Data.selectedSkin = skin.id; app.Save.Save(); Collection();
            }, equipped ? Tone.Blue : Tone.Green, label, true, 30, hasSkin || app.Save.Data.coins >= skin.price); if (!hasSkin) Icon("coin", safe, new Vector2(-169, -518), 39);
            Button("Change ball type", safe, new Vector2(-112, -605), new Vector2(285, 53), BallTypes, Tone.Quiet, "BALL PHYSICS", true, 20); Button("Back", safe, new Vector2(218, -605), new Vector2(162, 53), MainMenu, Tone.Quiet, "BACK", true, 21);
            if (skins.Length > 12)
            {
                Button("Previous skin page", safe, new Vector2(-284, -519), new Vector2(51, 61), () => { skinPage = Mathf.Max(0, skinPage - 1); Collection(); }, Tone.Blue, "<", true, 30);
                Button("Next skin page", safe, new Vector2(284, -519), new Vector2(51, 61), () => { skinPage = Mathf.Min((skins.Length - 1) / 12, skinPage + 1); Collection(); }, Tone.Blue, ">", true, 30);
            }
        }
        BallSkin[] FilteredSkins() => app.catalog.skins.Where(s => skinFilter == 0 || (skinFilter == 1 ? s.price >= 500 : s.glow || s.trail || s.id == "galaxy" || s.id == "rainbow")).ToArray();
        void Tab(string caption, float x, float y, bool selected, Action action, float width = 195) => Button("Tab " + caption, safe, new Vector2(x, y), new Vector2(width, 62), action, selected ? Tone.Blue : Tone.Quiet, caption, true, 24);
        void BallTypes()
        {
            Clear(Backdrop.Menu); Header("BALL PHYSICS", Collection, true); var profiles = app.catalog.balls.Where(p => p.unlockLevel <= app.catalog.levels.Length).ToArray();
            for (int i = 0; i < profiles.Length; i++)
            {
                var profile = profiles[i]; bool owned = app.Save.Data.balls.Contains(profile.id); var card = Panel(profile.displayName, safe, new Vector2(0, 330 - i * 185), new Vector2(615, 163), C("#244D6C")); Border(card, C("#5282A3"), 2);
                Text(profile.displayName.ToUpperInvariant(), 32, card, new Vector2(-66, 38), new Vector2(430, 50), White, true).alignment = TextAlignmentOptions.Left;
                Text(profile.description, 20, card, new Vector2(-67, -13), new Vector2(430, 72), Muted).alignment = TextAlignmentOptions.Left;
                Button("Use " + profile.id, card, new Vector2(198, -26), new Vector2(164, 62), () => { app.Save.Data.selectedBall = profile.id; app.Save.Save(); BallTypes(); }, owned ? Tone.Green : Tone.Quiet, owned ? app.Save.Data.selectedBall == profile.id ? "ACTIVE" : "USE" : "LV. " + profile.unlockLevel, true, 23, owned);
            }
            Label("Every skin keeps your selected ball physics.", 21, -480, Muted); Button("Back", safe, new Vector2(0, -575), new Vector2(330, 72), Collection, Tone.Blue, "BACK", true, 26);
        }
        public void Missions()
        {
            Clear(Backdrop.Menu); Header("MISSIONS", MainMenu, true); var missions = ArcadeProgress.Missions(app.Save.Data); int pages = Mathf.Max(1, (missions.Length + 3) / 4); missionPage = Mathf.Clamp(missionPage, 0, pages - 1);
            Text("ROLL. EXPLORE. GET REWARDED.", 21, safe, new Vector2(0, 458), new Vector2(620, 52), Muted, true);
            for (int i = 0; i < 4; i++)
            {
                int index = missionPage * 4 + i; if (index >= missions.Length) break; var mission = missions[index];
                var card = Panel("Mission " + mission.id, safe, new Vector2(0, 313 - i * 200), new Vector2(622, 178), C("#244C6B")); Border(card, C("#416B89"), 2); Icon(i % 2 == 0 ? "target" : "trophy", card, new Vector2(-249, 15), 76);
                Text(mission.title, 28, card, new Vector2(-19, 52), new Vector2(380, 43), White, true).alignment = TextAlignmentOptions.Left; Text(mission.description, 20, card, new Vector2(-18, 12), new Vector2(382, 52), Muted).alignment = TextAlignmentOptions.Left;
                var track = Panel("Mission progress track", card, new Vector2(-41, -47), new Vector2(321, 16), Dark, true); var fill = Panel("Mission progress fill", track, Vector2.zero, new Vector2(315, 10), C("#6DEA34"), true).GetComponent<Image>(); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = mission.target <= 0 ? 1 : (float)mission.current / mission.target;
                Text(mission.current + "/" + mission.target, 18, card, new Vector2(-43, -72), new Vector2(300, 31), White, true); bool ready = !mission.claimed && mission.current >= mission.target;
                Button("Claim mission " + mission.id, card, new Vector2(223, -30), new Vector2(155, 77), () => { if (ArcadeProgress.ClaimMission(app.Save.Data, mission.id)) app.Save.Save(); Missions(); }, mission.claimed ? Tone.Quiet : ready ? Tone.Green : Tone.Blue, mission.claimed ? "DONE" : ready ? "CLAIM" : "+" + mission.rewardCoins, true, 25, ready); Icon(mission.claimed ? "check" : "coin", card, new Vector2(222, 48), 47);
            }
            Button("Previous missions", safe, new Vector2(-188, -543), new Vector2(160, 66), () => { missionPage--; Missions(); }, Tone.Blue, "<", true, 32, missionPage > 0); Text((missionPage + 1) + " / " + pages, 26, safe, new Vector2(0, -545), new Vector2(150, 49), White, true);
            Button("Next missions", safe, new Vector2(188, -543), new Vector2(160, 66), () => { missionPage++; Missions(); }, Tone.Blue, ">", true, 32, missionPage < pages - 1);
        }
        public void DailyRewards()
        {
            Clear(Backdrop.Menu); Header("DAILY REWARDS", MainMenu, true); int day = ArcadeProgress.DailyDay(app.Save.Data); bool canClaim = ArcadeProgress.CanClaimDaily(app.Save.Data); string[] rewards = { "50", "+1", "100", "+2", "150", "250" };
            for (int i = 0; i < 6; i++)
            {
                bool done = i < day || i == day && !canClaim, today = i == day; var card = Panel("Daily gift " + i, safe, new Vector2((i % 3 - 1) * 210, 309 - i / 3 * 220), new Vector2(191, 197), today ? C("#2E785B") : C("#385B76")); Border(card, today ? C("#A9F194") : C("#7396AF"), today ? 3 : 2);
                Text("Day " + (i + 1), 25, card, new Vector2(0, 65), new Vector2(170, 43), White, true); Icon(i == 1 || i == 3 ? "gem" : "coin", card, new Vector2(0, 2), 100);
                if (done) Icon("check", card, new Vector2(0, -62), 43); else Text(rewards[i], 25, card, new Vector2(0, -65), new Vector2(150, 40), White, true);
            }
            var special = Panel("Seventh day special", safe, new Vector2(0, -231), new Vector2(609, 252), Color.white); Gradient(special, C("#FFF04A"), C("#FFB91A")); Border(special, C("#FFECAF"), 3);
            var galaxy = Array.Find(app.catalog.skins, s => s.id == "galaxy") ?? app.catalog.skins[0]; Raw("Special ball", special, new Vector2(-150, -8), Vector2.one * 221, BallPreview.GetThumbnail(galaxy));
            Text("Day 7", 36, special, new Vector2(120, 72), new Vector2(264, 63), C("#173C4E")); Text("SPECIAL BALL", 30, special, new Vector2(121, 8), new Vector2(279, 61), C("#173C4E")); Text("Galaxy", 25, special, new Vector2(120, -56), new Vector2(260, 44), C("#42526B"));
            if (day == 6 && !canClaim) Icon("check", special, new Vector2(255, -90), 48);
            Button("Claim daily reward", safe, new Vector2(0, -450), new Vector2(563, 105), () => { if (ArcadeProgress.ClaimDaily(app.Save.Data)) { app.Save.Save(); app.Audio.Play("victory"); } DailyRewards(); }, Tone.Green, canClaim ? "CLAIM" : "CLAIMED", true, 42, canClaim);
            Text(ArcadeProgress.DailyStatus(app.Save.Data), 22, safe, new Vector2(0, -550), new Vector2(620, 66), Muted);
        }
        public void Spin()
        {
            Clear(Backdrop.Menu); Header("DAILY SPIN", MainMenu, true); Label("A LITTLE LUCK. A BIG LITTLE REWARD.", 20, 455, Muted, true);
            var wheel = Rect("Prize wheel", safe, new Vector2(0, 96), new Vector2(563, 563)); wheel.gameObject.AddComponent<ArcadeWheelGraphic>().raycastTarget = false; wheel.localRotation = Quaternion.Euler(0, 0, -spinAngle);
            string[] rewards = { "30", "50", "50", "75", "100", "100", "150", "3 GEMS" };
            for (int i = 0; i < 8; i++) { float a = (i + .5f) * Mathf.PI / 4; var label = Text(rewards[i], i == 7 ? 21 : 32, wheel, new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 191, new Vector2(117, 59), White, true); label.rectTransform.localRotation = Quaternion.Euler(0, 0, -(i + .5f) * 45); }
            var pointer = Rect("Spin pointer", safe, new Vector2(0, 392), new Vector2(55, 66)); var arrow = pointer.gameObject.AddComponent<ArcadeTriangleGraphic>(); arrow.color = Gold; arrow.raycastTarget = false;
            var result = Text(string.IsNullOrEmpty(spinToast) ? "One free spin every day" : spinToast, 30, safe, new Vector2(0, -267), new Vector2(620, 80), White, true); bool ready = ArcadeProgress.CanSpin(app.Save.Data);
            Button("Free daily spin", safe, new Vector2(0, -421), new Vector2(550, 107), () => { if (!spinning) StartCoroutine(SpinWheel(wheel, result)); }, Tone.Green, ready ? "FREE SPIN" : "COME BACK TOMORROW", true, ready ? 39 : 28, ready && !spinning); Label("Coins and gems. No ads needed.", 22, -531, Muted);
        }
        IEnumerator SpinWheel(RectTransform wheel, TMP_Text result)
        {
            if (!ArcadeProgress.CanSpin(app.Save.Data)) yield break;
            spinning = true; spinToast = ArcadeProgress.FreeSpin(app.Save.Data); app.Save.Save();
            string[] awards = { "+30 COINS", "+50 COINS", "+50 COINS", "+75 COINS", "+100 COINS", "+100 COINS", "+150 COINS", "+3 GEMS" };
            int sector = Mathf.Max(0, Array.IndexOf(awards, spinToast)); float elapsed = 0, duration = 2.8f, start = spinAngle;
            float end = start + 1440 + Mathf.Repeat(360 - (sector + .5f) * 45 - start, 360);
            var spinButton = Array.Find(GetComponentsInChildren<Button>(), b => b.name == "Free daily spin"); if (spinButton) spinButton.interactable = false;
            while (elapsed < duration && wheel) { elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / duration); wheel.localRotation = Quaternion.Euler(0, 0, -Mathf.Lerp(start, end, 1 - Mathf.Pow(1 - t, 4))); yield return null; }
            spinAngle = Mathf.Repeat(end, 360);
            if (wheel) { app.Audio.Play("victory"); result.text = spinToast; if (walletText) walletText.text = app.Save.Data.coins.ToString("N0"); var confetti = Rect("Spin confetti", safe, Vector2.zero, Vector2.zero); Stretch(confetti); confetti.gameObject.AddComponent<ArcadeConfetti>().Begin(); if (spinButton) { var label = spinButton.GetComponentInChildren<TMP_Text>(); label.text = "COME BACK TOMORROW"; label.fontSize = 28; } }
            spinning = false;
        }

        public void Shop()
        {
            Clear(Backdrop.Menu); Header("SHOP", MainMenu, true);
            Tab("BALLS", -211, 458, shopTab == 0, () => { shopTab = 0; shopPage = 0; Shop(); }); Tab("GEMS", 0, 458, shopTab == 1, () => { shopTab = 1; shopPage = 0; Shop(); }); Tab("SPECIAL", 211, 458, shopTab == 2, () => { shopTab = 2; shopPage = 0; Shop(); });
            if (shopTab == 1)
            {
                Icon("gem", safe, new Vector2(-75, 335), 62); Text(app.Save.Data.gems.ToString(), 39, safe, new Vector2(33, 335), new Vector2(150, 75), White, true);
                for (int i = 0; i < 2; i++)
                {
                    int gems = i == 0 ? 1 : 3, coins = gems * 100; var card = Panel("Gem exchange " + gems, safe, new Vector2(0, 135 - i * 274), new Vector2(610, 234), C("#D6F4FF"));
                    Text(coins + " COINS", 37, card, new Vector2(65, 64), new Vector2(390, 64), C("#173D65")); Icon("coin", card, new Vector2(-192, 5), 132);
                    Button("Exchange " + gems + " gems", card, new Vector2(72, -44), new Vector2(333, 77), () => { if (ArcadeProgress.ExchangeGems(app.Save.Data, gems, coins)) app.Save.Save(); Shop(); }, Tone.Green, gems + (gems == 1 ? " GEM" : " GEMS"), true, 31, app.Save.Data.gems >= gems);
                }
                Label("Gems are earned from courses and daily gifts.", 21, -412, Muted); Label("All shop items use earned game currency.", 21, -466, Muted);
            }
            else
            {
                var skins = app.catalog.skins.Where(s => s.price > 0 && (shopTab == 0 || s.glow || s.trail || s.id == "galaxy")).ToArray(); if (skins.Length == 0) skins = app.catalog.skins; shopPage = Mathf.Clamp(shopPage, 0, (skins.Length - 1) / 4);
                for (int cell = 0; cell < 4; cell++)
                {
                    int index = shopPage * 4 + cell; if (index >= skins.Length) break; var skin = skins[index]; bool owned = app.Save.Data.skins.Contains(skin.id);
                    var card = Panel("Shop " + skin.id, safe, new Vector2(cell % 2 == 0 ? -158 : 158, cell < 2 ? 213 : -140), new Vector2(296, 327), C("#D9F5FF")); Border(card, C("#8CD9F5"), 2);
                    Text(skin.displayName, 25, card, new Vector2(0, 127), new Vector2(270, 47), C("#123B63")); Raw("Shop ball", card, new Vector2(0, 18), Vector2.one * 194, BallPreview.GetThumbnail(skin));
                    Button("Buy " + skin.id, card, new Vector2(0, -113), new Vector2(267, 77), () =>
                    {
                        if (owned) { app.Save.Data.selectedSkin = skin.id; app.Save.Save(); Collection(); return; } if (app.Save.Data.coins < skin.price) return;
                        app.Save.Data.coins -= skin.price; app.Save.Data.skins.Add(skin.id); app.Save.Save(); toast = skin.displayName + " unlocked!"; Shop();
                    }, Tone.Green, owned ? "OWNED" : skin.price.ToString("N0"), true, 31, owned || app.Save.Data.coins >= skin.price); if (!owned) Icon("coin", card, new Vector2(-78, -112), 43);
                }
                int pages = Mathf.Max(1, (skins.Length + 3) / 4); Button("Previous shop page", safe, new Vector2(-213, -420), new Vector2(150, 66), () => { shopPage--; Shop(); }, Tone.Blue, "<", true, 32, shopPage > 0); Text((shopPage + 1) + " / " + pages, 27, safe, new Vector2(0, -421), new Vector2(200, 50), White, true);
                Button("Next shop page", safe, new Vector2(213, -420), new Vector2(150, 66), () => { shopPage++; Shop(); }, Tone.Blue, ">", true, 32, shopPage + 1 < pages); Text(string.IsNullOrEmpty(toast) ? "Unlock with coins earned on your adventures." : toast, 21, safe, new Vector2(0, -515), new Vector2(630, 70), Muted);
            }
        }
        void Settings(Action back)
        {
            Clear(Backdrop.Menu); Header("SETTINGS", back, false); var settings = app.Save.Data.settings;
            SettingToggle("Sound", "speaker", 383, settings.sfx > .01f, () => { settings.sfx = settings.sfx > .01f ? 0 : .8f; app.ApplySettings(); Settings(back); });
            SettingToggle("Music", "music", 267, settings.music > .01f, () => { settings.music = settings.music > .01f ? 0 : .45f; app.ApplySettings(); Settings(back); });
            SettingToggle("Vibration", "vibrate", 151, settings.vibration, () => { settings.vibration = !settings.vibration; Settings(back); });
            SettingRow("Controls & camera", "ball", 35, ">", () => ControlSettings(back));
            string[] qualities = { "Battery", "Balanced", "High" }; SettingRow("Graphics", "gear", -81, qualities[settings.quality], () => { settings.quality = (settings.quality + 1) % 3; app.ApplySettings(); Settings(back); });
            SettingRow("Privacy Policy", "lock", -197, ">", () => Parents(() => Settings(back)));
            SettingRow("Terms of Service", "calendar", -313, ">", () => TextDialog("PLAY FAIR. HAVE FUN.", "Ball Quest is an offline game. Coins and gems are earned through play and have no cash value.\n\nKeep your own device save safe. A backup helps recover accidental corruption. This testing build is still being developed.", () => Settings(back)));
            SettingToggle("Camera effects","gear",-411,settings.cameraEffects,()=>{settings.cameraEffects=!settings.cameraEffects;app.ApplySettings();Settings(back);});
            Text("TOUCH SENSITIVITY",19,safe,new Vector2(0,-484),new Vector2(600,40),Muted,true);Slider("Touch sensitivity",-521,settings.sensitivity,.4f,2.5f,v=>{settings.sensitivity=v;app.ApplySettings();});
            Button("Save & back",safe,new Vector2(0,-581),new Vector2(440,77),()=>{app.ApplySettings();app.Save.Save();back();},Tone.Green,"SAVE & BACK",true,28);
        }
        void SettingToggle(string title, string icon, float y, bool on, Action action)
        {
            var row = Panel(title + " setting", safe, new Vector2(0, y), new Vector2(623, 94), C("#254961")); Border(row, C("#365D77"), 1.5f); Icon(icon, row, new Vector2(-258, 1), 57); Text(title, 28, row, new Vector2(-40, 0), new Vector2(350, 64), White, true).alignment = TextAlignmentOptions.Left;
            var toggle = Button(title, row, new Vector2(229, 0), new Vector2(110, 55), action, on ? Tone.Green : Tone.Quiet, "", true); Panel("Toggle thumb", toggle, new Vector2(on ? 24 : -24, 1), Vector2.one * 46, White, true);
        }
        void SettingRow(string title, string icon, float y, string value, Action action)
        {
            var row = Button(title, safe, new Vector2(0, y), new Vector2(623, 94), action, Tone.Quiet, "", false); Icon(icon, row, new Vector2(-258, 1), 55); Text(title, 27, row, new Vector2(-52, 0), new Vector2(326, 63), White, true).alignment = TextAlignmentOptions.Left; Text(value, 24, row, new Vector2(217, 0), new Vector2(154, 60), Muted, true);
        }
        void Parents(Action back) => TextDialog("PRIVACY", "Ball Quest works offline.\n\nNo advertisements, purchases, chat, accounts, analytics or location tracking initialize in this game.\n\nProgress, earned currency, settings and daily rewards are stored on your device. The game contains no external web links.\n\nNo payment is required for coins or gems.", back);
        void TextDialog(string title, string copy, Action back)
        {
            Clear(Backdrop.Menu); Header(title, back, false); var card = Panel("Information", safe, new Vector2(0, -4), new Vector2(610, 765), C("#23465F")); Border(card, C("#416F8D"), 2); Text(copy, 27, card, Vector2.zero, new Vector2(550, 704), White).alignment = TextAlignmentOptions.TopLeft;
            Button("Back", safe, new Vector2(0, -552), new Vector2(350, 77), back, Tone.Blue, "BACK", true, 28);
        }

        void Header(string title, Action back, bool wallet)
        {
            Button("Header back", safe, new Vector2(-291, 553), new Vector2(77, 78), back, Tone.Blue, "", true); Icon("back", safe, new Vector2(-291, 554), 49);
            Text(title, wallet ? 33 : 37, safe, new Vector2(wallet ? -39 : 0, 555), new Vector2(wallet ? 394 : 492, 76), White, true); if (wallet) Wallet(new Vector2(218, 553));
        }
        void Preview(BallSkin skin, Vector2 at, int size, bool podium)
        {
            if(!podium)
            {
                var contact=Rect("Soft ball contact shadow",safe,at+Vector2.down*(size*.30f),new Vector2(size*.55f,size*.09f));
                contact.gameObject.AddComponent<ArcadeContactShadowGraphic>().raycastTarget=false;
            }
            var rect = Rect("Live ball preview", safe, at, Vector2.one * size); var preview = rect.gameObject.AddComponent<BallPreview>(); preview.podium = podium; preview.Show(skin);
        }
        RectTransform Star(Transform parent, Vector2 at, float size, bool earned)
        {
            var root = Rect("Completion star", parent, at, Vector2.one * size);
            var shadow = Rect("Star depth", root, new Vector2(0, -4), Vector2.one * (size + 4)); shadow.gameObject.AddComponent<StarGraphic>().color = earned ? C("#8C5101") : C("#0D263B");
            var outline = Rect("Star rim", root, Vector2.zero, Vector2.one * size); outline.gameObject.AddComponent<StarGraphic>().color = earned ? C("#FF9D00") : C("#173B56");
            var face = Rect("Star face", root, new Vector2(0, 1), Vector2.one * (size - 8)); face.gameObject.AddComponent<StarGraphic>().color = Color.white; Gradient(face, earned ? C("#FFF77E") : C("#28516C"), earned ? C("#FFC523") : C("#17394F"));
            foreach (var graphic in root.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false; return root;
        }
        RectTransform Rect(string name, Transform parent, Vector2 at, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = (RectTransform)go.transform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = Vector2.one * .5f; rect.pivot = Vector2.one * .5f; rect.anchoredPosition = at; rect.sizeDelta = size; return rect;
        }
        static void Stretch(RectTransform rect, float bleed = 0) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * -bleed; rect.offsetMax = Vector2.one * bleed; }
        RectTransform Panel(string name, Transform parent, Vector2 at, Vector2 dimensions, Color color, bool roundedPill = false)
        {
            var rect = Rect(name, parent, at, dimensions); var image = rect.gameObject.AddComponent<Image>(); image.sprite = roundedPill ? pill : round; image.type = Image.Type.Sliced;
            // Scale border pixels uniformly before slicing: short controls retain circular caps rather than stretched ellipses.
            if(roundedPill && dimensions.x>0 && dimensions.y>0) image.pixelsPerUnitMultiplier=Mathf.Max(1,124f/Mathf.Min(dimensions.x,dimensions.y));
            image.color = color; image.raycastTarget = false; return rect;
        }
        void Gradient(RectTransform rect, Color top, Color bottom) { var gradient = rect.gameObject.AddComponent<UIGradient>(); gradient.top = top; gradient.bottom = bottom; }
        void Border(RectTransform rect, Color color, float thickness)
        {
            var image = rect.GetComponent<Image>(); if (!image) return;
            var child = Rect("True contour border", rect, Vector2.zero, Vector2.zero); Stretch(child);
            var stroke = child.gameObject.AddComponent<ArcadePanelStroke>(); stroke.radius = image.sprite == pill ? Mathf.Min(rect.sizeDelta.x, rect.sizeDelta.y) * .5f : 25;
            stroke.thickness = thickness; stroke.color = color; stroke.raycastTarget = false;
        }
        RectTransform Raw(string name, Transform parent, Vector2 at, Vector2 dimensions, Texture texture)
        {
            var rect = Rect(name, parent, at, dimensions); var image = rect.gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false; return rect;
        }
        TMP_Text Text(string content, float size, Transform parent, Vector2 at, Vector2 dimensions, Color color, bool stroke = false)
        {
            var rect = Rect(content, parent, at, dimensions); var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; if (stroke) { text.fontSharedMaterial = outlined; text.fontStyle = FontStyles.Bold; }
            text.text = content; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.enableAutoSizing = true; text.fontSizeMin = Mathf.Min(15, size); text.fontSizeMax = size; text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        TMP_Text Label(string content, float size, float y, Color color, bool stroke = false) => Text(content, size, safe, new Vector2(0, y), new Vector2(645, 80), color, stroke);
        RectTransform Button(string name, Transform parent, Vector2 at, Vector2 dimensions, Action action, Tone tone = Tone.Blue, string caption = null, bool isPill = true, float fontSize = 28, bool enabled = true)
        {
            Color top, bottom, edge;
            switch (tone)
            {
                case Tone.Green: top = C("#8BFF49"); bottom = C("#22C515"); edge = C("#18752A"); break;
                case Tone.Gold: top = C("#FFF04B"); bottom = C("#FFB600"); edge = C("#B86F08"); break;
                case Tone.Red: top = C("#FF8170"); bottom = C("#EF3129"); edge = C("#8B1D1D"); break;
                case Tone.Purple: top = C("#C76BFF"); bottom = C("#7A2AE0"); edge = C("#431178"); break;
                case Tone.Quiet: top = C("#355E7B"); bottom = C("#17374F"); edge = C("#082538"); break;
                default: top = C("#59CEFF"); bottom = C("#2584E5"); edge = C("#12518F"); break;
            }
            Panel(name + " extrusion", parent, at + Vector2.down * 7, dimensions + new Vector2(2, 2), edge, isPill);
            var root = Panel(name, parent, at, dimensions, Color.white, isPill); Gradient(root, top, bottom); Border(root, enabled ? Color.Lerp(edge, White, .12f) : new Color(.5f, .6f, .67f, .2f), 2.2f);
            var image = root.GetComponent<Image>(); image.raycastTarget = true; var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            var colors = button.colors; colors.highlightedColor = C("#F0FCFF"); colors.pressedColor = C("#B6D1DF"); colors.disabledColor = new Color(.55f, .63f, .69f, .9f); button.colors = colors;
            Panel("Button gloss", root, new Vector2(0, dimensions.y * .31f), new Vector2(Mathf.Max(8, dimensions.x - (isPill ? dimensions.y * .55f : 18)), Mathf.Max(8, dimensions.y * .24f)), new Color(1, 1, 1, tone == Tone.Quiet ? .025f : .16f), isPill);
            Text(caption ?? name, fontSize, root, Vector2.zero, dimensions - new Vector2(21, 8), enabled ? White : C("#93AABD"), true); root.gameObject.AddComponent<PressFeedback>(); button.onClick.AddListener(() => { app.Audio.Play("click"); action?.Invoke(); }); return root;
        }
        void Icon(string icon, Transform parent, Vector2 at, float size)
        {
            var rect = Rect(icon + " icon", parent, at, Vector2.one * size);
            if (icons.TryGetValue(icon, out var sprite)) { var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; }
            else { var graphic = rect.gameObject.AddComponent<QuestIconGraphic>(); graphic.icon = icon == "gear" ? "gear" : icon == "ball" ? "ball" : icon == "back" ? "flag" : "cup"; graphic.color = icon == "coin" ? Gold : White; graphic.raycastTarget = false; }
        }
        void Slider(string title, float y, float value, float min, float max, Action<float> setter)
        {
            var rect = Panel(title, safe, new Vector2(0, y), new Vector2(550, 33), C("#0A243A"), true); rect.GetComponent<Image>().raycastTarget = true; var area = Rect("Slider area", rect, Vector2.zero, new Vector2(513, 14));
            var fill = Panel("Fill", area, Vector2.zero, Vector2.zero, C("#39CEFF"), true); Stretch(fill); var handleArea = Rect("Handle slide area", rect, Vector2.zero, new Vector2(513, 0)); var handle = Panel("Handle", handleArea, Vector2.zero, Vector2.one * 36, White, true);
            var slider = rect.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.minValue = min; slider.maxValue = max; slider.value = value; slider.onValueChanged.AddListener(v => setter(v));
        }
        void OnDestroy()
        {
            if (app && app.Session) app.Session.Changed -= StateChanged; foreach (var sprite in ownedSprites) if (sprite) Destroy(sprite);
            if (round) Destroy(round); if (pill) Destroy(pill); if(solid)Destroy(solid); if (roundTexture) Destroy(roundTexture); if (pillTexture) Destroy(pillTexture); if(solidTexture)Destroy(solidTexture); if (outlined) Destroy(outlined); if (titleGold) Destroy(titleGold); if (titleBlue) Destroy(titleBlue);
        }
    }
}
