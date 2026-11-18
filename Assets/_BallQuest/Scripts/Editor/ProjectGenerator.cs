using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace BallQuest.Editor
{
    [InitializeOnLoad]
    public static class ProjectGenerator
    {
        public const string Root = "Assets/_BallQuest";
        public const string CatalogPath = Root + "/ScriptableObjects/GameCatalog.asset";
        public const string ScenePath = Root + "/Scenes/Bootstrap/BallQuest.unity";
        static ProjectGenerator()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Application.isBatchMode) return;
                var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
                if (catalog && !catalog.pipeline)
                {
                    try { GenerateProject(); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            };
        }

        [MenuItem("Tools/Ball Quest/Generate Project Content")]
        public static void GenerateProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before generating content.");
            string[] folders = { "Art/Materials", "Art/Models", "Art/Textures", "Art/VFX", "Art/Rendering", "Audio/Music", "Audio/SFX", "Prefabs/Balls", "Prefabs/Tracks", "Prefabs/Obstacles", "Prefabs/Collectibles", "Prefabs/UI", "Scenes/MainMenu", "Scenes/Gameplay", "ScriptableObjects/Worlds" };
            foreach (var folder in folders) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (!catalog) throw new InvalidOperationException("Missing checked-in GameCatalog.asset. Restore project source files.");
            GenerateLevels();
            UpgradeSkyline(catalog);
            RegisterDefinitions(catalog);
            CreateMaterials(catalog);
            CreateFont(catalog);
            CreateRendering(catalog);
            CreatePrefabs(catalog);
            ConfigureSettings();
            ArcadeArtGenerator.Generate(catalog);
            ArcadeCampaignGenerator.Generate(catalog);
            ProductionCampaignGenerator.Generate(catalog);
            ManualGameplayUpgradeGenerator.Generate(catalog);
            NaturalContactAudioGenerator.Generate();
            SignatureAdventureGenerator.Generate(catalog);
            ManualRaceCourseGenerator.Generate(catalog);
            CrazyCampaignGenerator.Generate(catalog);
            GrandCampaignGenerator.Generate(catalog);
            CreateArcadePostFX();
            PremiumTrackArtGenerator.Generate();
            CrazyTrackArtGenerator.Generate();
            foreach(var level in catalog.levels)if(level&&level.id==1)
            {level.hint="Repeat swipes to build speed. Hold still to coast.";EditorUtility.SetDirty(level);}
            MaterialBallGenerator.Generate(catalog);
            GrandArtImporter.Generate();
            PlayerSettings.bundleVersion="0.11.0";
            PlayerSettings.Android.bundleVersionCode=11;
            EnsureScene(catalog);
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            ValidateProject();
            PhysicsLabGenerator.Generate();
            Debug.Log("Ball Quest content generated. Open " + ScenePath + " and press Play. Existing authored assets were retained.");
        }

        static void CreateArcadePostFX()
        {
            const string path=Root+"/Resources/Arcade/ArcadePostFX.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(.28f);bloom.threshold.Override(1);bloom.scatter.Override(.58f);
            if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}
            tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}
            vignette.intensity.Override(.13f);vignette.smoothness.Override(.5f);
            EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(tone);EditorUtility.SetDirty(vignette);EditorUtility.SetDirty(profile);
        }

        static void UpgradeSkyline(GameCatalog catalog)
        {
            if (catalog.contentVersion >= 4) return;
            var seeds = JsonUtility.FromJson<CampaignSeed>(File.ReadAllText(Root + "/Content/SkylineCampaign.json"));
            var levels = new List<LevelDefinition>();
            foreach (var seed in seeds.levels)
            {
                string path=Root+"/ScriptableObjects/Levels/Level"+seed.id.ToString("000")+".asset";
                var level=AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if(!level) {level=ScriptableObject.CreateInstance<LevelDefinition>();AssetDatabase.CreateAsset(level,path);}
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(seed),level);EditorUtility.SetDirty(level);levels.Add(level);
            }
            catalog.levels=levels.ToArray();catalog.contentVersion=4;
            foreach(string guid in AssetDatabase.FindAssets("t:BallProfile",new[]{Root+"/ScriptableObjects/Balls"}))
            {
                var profile=AssetDatabase.LoadAssetAtPath<BallProfile>(AssetDatabase.GUIDToAssetPath(guid));
                profile.maxSpeed=9.5f;profile.acceleration=18;profile.bounce=.025f;profile.airControl=.35f;
                EditorUtility.SetDirty(profile);
            }
            PlayerSettings.bundleVersion="0.2.0";PlayerSettings.Android.bundleVersionCode=2;
        }

        static void RegisterDefinitions(GameCatalog catalog)
        {
            var profiles = AssetDatabase.FindAssets("t:BallProfile", new[] { Root + "/ScriptableObjects/Balls" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<BallProfile>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(profile => profile).OrderBy(profile => profile.ability).ToArray();
            var skins = AssetDatabase.FindAssets("t:BallSkin", new[] { Root + "/ScriptableObjects/Skins" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<BallSkin>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(skin => skin).OrderBy(skin => skin.id == "classic-red" ? 0 : skin.id == "ocean-blue" ? 1 : 2).ThenBy(skin => skin.displayName).ToArray();
            catalog.balls = (catalog.balls ?? Array.Empty<BallProfile>()).Where(profile => profile).Concat(profiles).Distinct().ToArray();
            catalog.skins = (catalog.skins ?? Array.Empty<BallSkin>()).Where(skin => skin).Concat(skins).Distinct().ToArray();
            EditorUtility.SetDirty(catalog);
        }

        [Serializable] sealed class CampaignSeed { public LevelSeed[] levels; }
        [Serializable] sealed class LevelSeed
        {
            public int id; public string title; public string hint; public int world;
            public Vector3 spawn; public Vector3 finish; public Vector3[] checkpoints;
            public TrackPiece[] tracks; public ObstacleSpec[] obstacles; public PickupSpec[] pickups;
            public float parSeconds; public float killHeight; public bool allowJump;
        }

        [MenuItem("Tools/Ball Quest/Generate Levels")]
        public static void GenerateLevels()
        {
            var seeds = JsonUtility.FromJson<CampaignSeed>(File.ReadAllText(Root + "/Content/OpeningCampaign.json"));
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            var levels = catalog.levels == null ? new List<LevelDefinition>() : catalog.levels.Where(l => l).ToList();
            foreach (var seed in seeds.levels)
            {
                string path = Root + "/ScriptableObjects/Levels/Level" + seed.id.ToString("000") + ".asset";
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (!level)
                {
                    level = ScriptableObject.CreateInstance<LevelDefinition>();
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(seed), level); AssetDatabase.CreateAsset(level, path);
                }
                if (!levels.Any(l => l.id == level.id)) levels.Add(level);
            }
            catalog.levels = levels.OrderBy(l => l.id).ToArray(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }

        static void CreateMaterials(GameCatalog catalog)
        {
            Color[] colors = { new Color(0.82f, 0.64f, 0.4f), new Color(0.35f, 0.21f, 0.17f), new Color(0.15f, 0.25f, 0.36f), new Color(1, 0.74f, 0.17f), new Color(0.96f, 0.24f, 0.29f), new Color(0.12f, 0.9f, 0.75f), new Color(0.95f, 0.98f, 1), new Color(0.48f, 0.84f, 1), new Color(0.23f, 0.68f, 0.42f) };
            string[] names = { "Honey deck", "Walnut", "Midnight rock", "Sunshine gold", "Coral hazard", "Mint energy", "Cloud white", "Glacier", "Island green" };
            catalog.palette = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                string path = Root + "/Art/Materials/" + names[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = QuestVisuals.Material(names[i], colors[i], i == 3 ? 0.45f : 0, i == 7 ? 0.8f : 0.35f); AssetDatabase.CreateAsset(material, path); }
                catalog.palette[i] = material;
                // Skyline art direction: porcelain surfaces, violet structure, warm coral acceleration pads.
                Color[] skyline={new Color(.94f,.85f,.71f),new Color(.69f,.54f,.8f),new Color(.3f,.23f,.44f),new Color(1,.68f,.2f),new Color(.92f,.3f,.4f),new Color(1,.48f,.25f),new Color(1,.97f,.91f),new Color(.74f,.79f,.96f),new Color(.65f,.7f,.82f)};
                material.SetColor("_BaseColor",skyline[i]);material.SetFloat("_Smoothness",i==3?.65f:.25f);EditorUtility.SetDirty(material);
            }
            string glowPath = Root + "/Art/Materials/Runtime Glow.mat";
            catalog.glowMaterial = AssetDatabase.LoadAssetAtPath<Material>(glowPath);
            if (!catalog.glowMaterial)
            {
                catalog.glowMaterial = QuestVisuals.Material("Runtime Glow", Color.white);
                catalog.glowMaterial.EnableKeyword("_EMISSION"); catalog.glowMaterial.SetColor("_EmissionColor", Color.white * 0.7f);
                AssetDatabase.CreateAsset(catalog.glowMaterial, glowPath);
            }
            string particlesPath = Root + "/Art/Materials/Pooled Sparks.mat";
            catalog.particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(particlesPath);
            if (!catalog.particleMaterial)
            {
                catalog.particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Pooled Sparks" };
                AssetDatabase.CreateAsset(catalog.particleMaterial, particlesPath);
            }
            string softPath=Root+"/Art/Textures/Soft spark.png";
            var soft=AssetDatabase.LoadAssetAtPath<Texture2D>(softPath);
            if(!soft)
            {
                var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    float radius=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;
                    texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),1.4f)));
                }
                texture.Apply();File.WriteAllBytes(softPath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(softPath,ImportAssetOptions.ForceSynchronousImport);
                soft=AssetDatabase.LoadAssetAtPath<Texture2D>(softPath);
            }
            var spark=catalog.particleMaterial;spark.SetTexture("_BaseMap",soft);spark.SetColor("_BaseColor",Color.white);
            spark.SetFloat("_Surface",1);spark.SetFloat("_Blend",0);spark.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);spark.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            spark.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);spark.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            spark.SetFloat("_ZWrite",0);spark.SetFloat("_Cull",0);spark.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");spark.SetOverrideTag("RenderType","Transparent");spark.renderQueue=3000;
            EditorUtility.SetDirty(spark);
        }

        static void CreateRendering(GameCatalog catalog)
        {
            string path = Root + "/Art/Rendering/MobileURP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (!pipeline)
            {
                string rendererPath = Root + "/Art/Rendering/MobileRenderer.asset";
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (!renderer) { renderer = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(renderer, rendererPath); }
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 2; pipeline.renderScale = 1; pipeline.supportsHDR = false;
                pipeline.shadowDistance = 30; pipeline.shadowCascadeCount = 2;
                AssetDatabase.CreateAsset(pipeline, path);
            }
            catalog.pipeline = pipeline;
            pipeline.supportsHDR=true;
            pipeline.shadowCascadeCount=2;
            pipeline.cascade2Split=.35f;
            TrackRenderingUpgradeGenerator.ConfigurePipeline(pipeline);
            pipeline.useSRPBatcher=true;
            EditorUtility.SetDirty(pipeline);
            string skyPath=Root+"/Art/Materials/Skyline Sky.mat";
            catalog.skyMaterial=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(!catalog.skyMaterial)
            {
                catalog.skyMaterial=new Material(Shader.Find("BallQuest/Skyline Sky"));
                AssetDatabase.CreateAsset(catalog.skyMaterial,skyPath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
            string[] shaders = { "TextMeshPro/Distance Field", "TextMeshPro/Mobile/Distance Field", "UI/Default" };
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var list = graphics.FindProperty("m_AlwaysIncludedShaders");
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var shader = list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (!shader || shader.name != "Universal Render Pipeline/Lit" && shader.name != "Universal Render Pipeline/Particles/Unlit") continue;
                list.GetArrayElementAtIndex(i).objectReferenceValue = null; list.DeleteArrayElementAtIndex(i);
            }
            foreach (string name in shaders)
            {
                var shader = Shader.Find(name); if (!shader) continue;
                bool exists = false;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) exists = true;
                if (!exists) { int i = list.arraySize++; list.GetArrayElementAtIndex(i).objectReferenceValue = shader; }
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateFont(GameCatalog catalog)
        {
            const string skylineFontPath=Root+"/Art/Fonts/Fredoka-SDF.asset";
            var skylineFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(skylineFontPath);
            if(!skylineFont)
            {
                var sourceFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Art/Fonts/Fredoka-SemiBold.ttf");
                if(sourceFont)
                {
                    skylineFont=TMP_FontAsset.CreateFontAsset(sourceFont,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024);
                    AssetDatabase.CreateAsset(skylineFont,skylineFontPath);AssetDatabase.AddObjectToAsset(skylineFont.material,skylineFont);
                    foreach(var texture in skylineFont.atlasTextures)AssetDatabase.AddObjectToAsset(texture,skylineFont);
                    skylineFont.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?%+-*/<>()[]&'_=|",out _);
                    EditorUtility.SetDirty(skylineFont);
                }
            }
            if(skylineFont) {catalog.font=skylineFont;}
            string musicPath=Root+"/Audio/Music/Skyline Reverie.wav";
            var importer=AssetImporter.GetAtPath(musicPath) as AudioImporter;
            if(importer)
            {
                var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.Streaming;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.75f;
                importer.defaultSampleSettings=sample;importer.SaveAndReimport();catalog.soundtrack=AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
            }
            if (catalog.font) return;
            string path = Root + "/Art/Textures/BallQuestFont.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing) { catalog.font = existing; return; }
            const string includedFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var includedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(includedFontPath);
            if (!includedFont)
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                string essentials = Path.Combine(info.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage");
                if (!File.Exists(essentials)) throw new FileNotFoundException("Import TMP Essential Resources with Window > TextMeshPro > Import TMP Essential Resources, then rerun generation.", essentials);
                AssetDatabase.ImportPackage(essentials, false);
                AssetDatabase.Refresh();
                includedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(includedFontPath);
            }
            if (includedFont) { catalog.font = includedFont; return; }
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (!source) throw new InvalidOperationException("TMP Essential Resources did not provide the required font.");
            var font = TMP_FontAsset.CreateFontAsset(source);
            if (!font) throw new InvalidOperationException("Could not create the Ball Quest font from included font data.");
            font.name = "Ball Quest UI";
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?%+-*/<>()[]&'_=", out _);
            catalog.font = font; EditorUtility.SetDirty(font);
        }

        static void CreatePrefabs(GameCatalog catalog)
        {
            foreach (var profile in catalog.balls)
            {
                string path = Root + "/Prefabs/Balls/" + profile.id + ".prefab";
                if (File.Exists(path)) continue;
                var go = QuestVisuals.Shape(profile.displayName, PrimitiveType.Sphere, null, Vector3.zero, Vector3.one, catalog.palette[4]);
                go.layer = 8; go.AddComponent<Rigidbody>(); go.AddComponent<BallInput>();
                go.AddComponent<BallController>().profile = profile;
                PrefabUtility.SaveAsPrefabAsset(go, path); UnityEngine.Object.DestroyImmediate(go);
            }
            foreach (ObstacleKind kind in Enum.GetValues(typeof(ObstacleKind)))
            {
                string path = Root + "/Prefabs/Obstacles/" + kind + ".prefab"; if (File.Exists(path)) continue;
                var go = QuestVisuals.Shape(kind.ToString(), PrimitiveType.Cube, null, Vector3.zero, Vector3.one, catalog.palette[4]);
                go.AddComponent<Rigidbody>().isKinematic = true; go.AddComponent<QuestObstacle>().kind = kind;
                go.GetComponent<Collider>().isTrigger = kind == ObstacleKind.JumpPad || kind == ObstacleKind.Booster || kind == ObstacleKind.Conveyor;
                PrefabUtility.SaveAsPrefabAsset(go, path); UnityEngine.Object.DestroyImmediate(go);
            }
            string trackPath = Root + "/Prefabs/Tracks/Deck.prefab";
            if (!File.Exists(trackPath))
            {
                var go = QuestVisuals.Shape("Deck", PrimitiveType.Cube, null, Vector3.zero, new Vector3(8, 0.8f, 16), catalog.palette[0]);
                PrefabUtility.SaveAsPrefabAsset(go, trackPath); UnityEngine.Object.DestroyImmediate(go);
            }
            string uiPath = Root + "/Prefabs/UI/SafeCanvas.prefab";
            if (!File.Exists(uiPath))
            {
                var go = new GameObject("Safe Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(720, 1280);
                var safe = new GameObject("Safe Area", typeof(RectTransform), typeof(SafeArea)); safe.transform.SetParent(go.transform, false);
                PrefabUtility.SaveAsPrefabAsset(go, uiPath); UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void ConfigureSettings()
        {
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany") PlayerSettings.companyName = "Ball Quest Studio";
            if (string.IsNullOrEmpty(PlayerSettings.productName)) PlayerSettings.productName = "Ball Quest 3D";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/Skyline Icon.png");
            if(icon)PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown,new[]{icon},IconKind.Any);
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
            foreach (var target in new[] { UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.Build.NamedBuildTarget.iOS })
            {
                string identifier = PlayerSettings.GetApplicationIdentifier(target);
                if (string.IsNullOrEmpty(identifier) || identifier.StartsWith("com.DefaultCompany"))
                    PlayerSettings.SetApplicationIdentifier(target, "com.ballqueststudio.ballquest3d");
            }
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            Time.fixedDeltaTime = 1f / 60; Time.maximumDeltaTime = 0.1f;
            Physics.defaultSolverIterations = 8; Physics.defaultSolverVelocityIterations = 2;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler"); if (input != null) input.intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue = "Player"; tags.ApplyModifiedPropertiesWithoutUndo();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(PhysicsLabGenerator.ScenePath,true) };
        }

        static void EnsureScene(GameCatalog catalog)
        {
            if (File.Exists(ScenePath)) return;
            var original = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Ball Quest Bootstrap").AddComponent<GameBootstrap>().catalog = catalog;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.RestoreSceneManagerSetup(original);
        }

        [MenuItem("Tools/Ball Quest/Validate Project")]
        public static void ValidateProject()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            var errors = new List<string>();
            if (!catalog) errors.Add("Catalog missing.");
            else
            {
                if (!catalog.pipeline) errors.Add("URP pipeline missing.");
                if (!catalog.font) errors.Add("TMP font missing.");
                if (!catalog.glowMaterial || !catalog.particleMaterial) errors.Add("Runtime shader material references missing.");
                if (catalog.palette == null || catalog.palette.Length < 9 || catalog.palette.Any(m => !m || !m.shader)) errors.Add("Palette incomplete.");
                if (catalog.balls == null || catalog.balls.Length == 0 || catalog.balls.Any(b => !b || b.mass <= 0 || b.maxSpeed <= 0)) errors.Add("Invalid or missing ball profiles.");
                if (catalog.skins == null || catalog.skins.Length == 0 || catalog.skins.Any(s => !s)) errors.Add("Invalid or missing skins.");
                if (catalog.levels == null || catalog.levels.Length < 3) errors.Add("Three opening courses are required.");
                else for (int i = 0; i < catalog.levels.Length; i++)
                {
                    var level = catalog.levels[i];
                    if (!level || level.id != i + 1) { errors.Add("Level IDs must be contiguous."); continue; }
                    var tracks=CampaignGeometry.Expand(level);
                    bool challenge=level.challenge!=ChallengeKind.None||level.obstacles.Length>0||tracks.Any(t=>t.size.x<=2.4f&&!t.rails);
                    if(tracks.Length==0||!challenge||level.pickups.Length==0||level.routePoints==null||level.routePoints.Length<2||Vector3.Distance(level.finish,level.spawn)<2)errors.Add("Incomplete level "+level.id);
                    foreach (var track in tracks) if (track.size.x <= 1 || track.size.y <= 0 || track.size.z <= 0) errors.Add("Invalid track in level " + level.id);
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var child in go.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0) errors.Add("Missing script: " + go.name);
                var ball = go.GetComponent<BallController>();
                if (ball && (!go.GetComponent<Rigidbody>() || !go.GetComponent<SphereCollider>() || !go.GetComponent<BallInput>() || !ball.profile))
                    errors.Add("Ball prefab missing required components/profile: " + go.name);
                var obstacle = go.GetComponent<QuestObstacle>();
                if (obstacle && (!go.GetComponent<Rigidbody>() || !go.GetComponent<Collider>())) errors.Add("Obstacle missing body/collider: " + go.name);
            }
            if (!File.Exists(ScenePath)) errors.Add("Bootstrap scene missing.");
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Debug.Log("Ball Quest structural validation passed. This does not establish level solvability or device performance.");
        }
    }
}
