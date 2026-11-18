using System;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallQuest.Editor
{
    /// <summary>Independent, manually playable diagnostic scene. It does not expand the adventure campaign.</summary>
    public static class PhysicsLabGenerator
    {
        public const string ScenePath = "Assets/_BallQuest/Scenes/Gameplay/PhysicsLab.unity";
        static readonly string[] Names = { "Flat / rest / coast", "Uphill 12 degrees", "Downhill 12 degrees", "Narrow balance bridge", "Circular platform", "Curved road", "Ice / low traction", "Sand / high resistance", "Moving support", "Collision test", "Natural jump gap", "Reverse / turn arena" };
        [MenuItem("Tools/Ball Quest/Generate Physics Laboratory")]
        public static void Generate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(ProjectGenerator.CatalogPath);
            if (!catalog) throw new InvalidOperationException("Generate the existing project catalog before the physics lab.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var parent = new GameObject("Twelve independent physics stations").transform;
            var spawns = new Vector3[Names.Length];
            var normal = Material("Lab maple", new Color(.68f, .44f, .19f));
            var iceVisual = Material("Lab ice", new Color(.28f, .78f, .95f));
            var sandVisual = Material("Lab sand", new Color(.93f, .76f, .36f));
            var rubber = Material("Lab safety blocks", new Color(.92f, .14f, .14f));
            var normalContact = Contact("Lab standard contact", .55f);
            var iceContact = Contact("Lab ice contact", .025f);
            var sandContact = Contact("Lab sand contact", .95f);
            for (int i = 0; i < Names.Length; i++)
            {
                Vector3 center = new Vector3((i % 3) * 90, 8, (i / 3) * 95);
                var station = new GameObject((i + 1).ToString("00") + " " + Names[i]).transform; station.SetParent(parent);
                station.position = center;
                spawns[i] = center + new Vector3(0, 1.02f, -10);
                Label(station, Names[i], catalog.font, center + new Vector3(0, 2, 13));
                switch (i)
                {
                    case 0: case 11:
                        Box(station, "Wide freely explored arena", center, new Vector3(72, .8f, 72), normal, normalContact);
                        break;
                    case 1: case 2:
                        float tilt = i == 1 ? -12 : 12;
                        Box(station, "Level entry", center + Vector3.back * 11, new Vector3(10, .8f, 7), normal, normalContact);
                        Box(station, "Inclined physical ramp", center + new Vector3(0, i == 1 ? 2 : -2, 2), new Vector3(10, .8f, 20), normal, normalContact, Quaternion.Euler(tilt, 0, 0));
                        Box(station, "Level runout", center + new Vector3(0, i == 1 ? 4 : -4, 14), new Vector3(12, .8f, 7), normal, normalContact);
                        break;
                    case 3:
                        Box(station, "Entry", center + Vector3.back * 10, new Vector3(12, .8f, 8), normal, normalContact);
                        Box(station, "Unrailed two metre balance beam", center + Vector3.forward * 1, new Vector3(2, .8f, 15), normal, normalContact);
                        Box(station, "Exit", center + Vector3.forward * 12, new Vector3(12, .8f, 8), normal, normalContact);
                        break;
                    case 4:
                        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); disc.name = "Circular driving disc"; disc.transform.SetParent(station); disc.transform.position = center; disc.transform.localScale = new Vector3(27, .4f, 27); disc.GetComponent<Renderer>().sharedMaterial = normal; disc.GetComponent<Collider>().sharedMaterial = normalContact; disc.AddComponent<BallSurfaceTag>().surface = SurfaceAudioKind.Wood;
                        UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
                        var discCollider = disc.AddComponent<MeshCollider>(); discCollider.sharedMesh = disc.GetComponent<MeshFilter>().sharedMesh; discCollider.sharedMaterial = normalContact;
                        break;
                    case 5:
                        for (int n = 0; n < 15; n++)
                        {
                            float angle = (-60 + n * 8) * Mathf.Deg2Rad;
                            Vector3 p = center + new Vector3(Mathf.Sin(angle) * 13, 0, Mathf.Cos(angle) * 13 - 5);
                            Box(station, "Curved road section", p, new Vector3(6, .8f, 2.2f), normal, normalContact, Quaternion.Euler(0, -60 + n * 8, 0));
                        }
                        spawns[i] = center + new Vector3(-11, 1.02f, 1.5f);
                        break;
                    case 6: case 7:
                        Box(station, "Material response arena", center, new Vector3(25, .8f, 28), i == 6 ? iceVisual : sandVisual, i == 6 ? iceContact : sandContact);
                        break;
                    case 8:
                        Box(station, "Waiting deck", center + Vector3.back * 10, new Vector3(14, .8f, 8), normal, normalContact);
                        var platform = Box(station, "Physical oscillating support", center + Vector3.forward * 2, new Vector3(12, .8f, 16), normal, sandContact);
                        platform.AddComponent<Rigidbody>(); var motor = platform.AddComponent<QuestObstacle>(); motor.kind = ObstacleKind.MovingBridge; motor.axis = Vector3.right; motor.amplitude = 3; motor.speed = .65f;
                        break;
                    case 9:
                        Box(station, "Collision arena", center, new Vector3(28, .8f, 28), normal, normalContact);
                        Box(station, "Impact wall", center + Vector3.forward * 7 + Vector3.up * 1, new Vector3(13, 2, 1), rubber, normalContact);
                        var impact = GameObject.CreatePrimitive(PrimitiveType.Sphere); impact.name = "Dynamic collision sphere"; impact.transform.SetParent(station); impact.transform.position = center + new Vector3(3, 1, 2); impact.transform.localScale = Vector3.one * 1.8f; impact.GetComponent<Renderer>().sharedMaterial = rubber; impact.AddComponent<Rigidbody>().mass = 2;
                        break;
                    case 10:
                        Box(station, "Approach", center + Vector3.back * 7, new Vector3(10, .8f, 15), normal, normalContact);
                        Box(station, "Takeoff ramp", center + new Vector3(0, 1.15f, 3), new Vector3(10, .8f, 8), normal, normalContact, Quaternion.Euler(-18, 0, 0));
                        Box(station, "Lower catch deck after gap", center + new Vector3(0, -.5f, 17), new Vector3(20, .8f, 16), normal, normalContact);
                        break;
                }
            }
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "Manual physics laboratory ball"; sphere.transform.position = spawns[0];
            sphere.AddComponent<Rigidbody>(); var input = sphere.AddComponent<BallInput>(); var ball = sphere.AddComponent<BallController>(); ball.profile = catalog.Ball("standard"); ball.active = true; input.active = true;
            BallLook.Apply(sphere, catalog.Skin("ocean-blue"), catalog);
            sphere.AddComponent<BallContactAudio>().SetMaterial(BallAudioMaterial.Energy);
            var cameraGo = new GameObject("Main Camera"); cameraGo.tag = "MainCamera"; var camera = cameraGo.AddComponent<Camera>(); camera.backgroundColor = new Color(.35f, .7f, .96f); camera.clearFlags = CameraClearFlags.SolidColor; cameraGo.AddComponent<AudioListener>(); cameraGo.AddComponent<CinemachineBrain>();
            camera.nearClipPlane=.25f;camera.farClipPlane=350;
            var chase = new GameObject("Independent laboratory camera").AddComponent<CinemachineCamera>(); chase.Follow = sphere.transform; chase.LookAt = sphere.transform; chase.gameObject.AddComponent<QuestCamera>().target = ball;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(42, -35, 0);
            RenderSettings.ambientLight = new Color(.65f, .7f, .8f); RenderSettings.fog = false;
            var lab = new GameObject("Physics lab telemetry").AddComponent<PhysicsLabTelemetry>(); lab.ball = ball; lab.stationSpawns = spawns; lab.stationNames = Names; lab.recordCsv = true;
            Ui(lab, catalog.font);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)); EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Physics laboratory generated: " + ScenePath + ". Twelve manually playable stations; CSV instrumentation records actual Rigidbody state.");
            }
            finally
            {
                if (previous.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }
        static GameObject Box(Transform parent, string name, Vector3 p, Vector3 size, Material material, PhysicsMaterial contact, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent); go.transform.position = p; go.transform.localScale = size; go.transform.rotation = rotation ?? Quaternion.identity;
            go.GetComponent<Renderer>().sharedMaterial = material; go.GetComponent<Collider>().sharedMaterial = contact;
            var tag = go.AddComponent<BallSurfaceTag>(); tag.surface = material.name.Contains("ice") ? SurfaceAudioKind.Ice : material.name.Contains("sand") ? SurfaceAudioKind.Sand : SurfaceAudioKind.Wood; tag.rollingResistanceMultiplier = tag.surface == SurfaceAudioKind.Sand ? 3.5f : tag.surface == SurfaceAudioKind.Ice ? .15f : 1;
            return go;
        }
        static void Label(Transform parent, string title, TMP_FontAsset font, Vector3 p)
        {
            var text = new GameObject("Station label").AddComponent<TextMeshPro>(); text.transform.SetParent(parent); text.transform.position = p; text.font = font; text.text = title; text.fontSize = 2; text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.rectTransform.sizeDelta = new Vector2(30, 5);
        }
        static Material Material(string name, Color color)
        {
            string path = "Assets/_BallQuest/Art/Materials/" + name.Replace(" ", "") + ".mat";
            var value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!value) { value = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(value, path); }
            value.SetColor("_BaseColor", color); EditorUtility.SetDirty(value); return value;
        }
        static PhysicsMaterial Contact(string name, float friction)
        {
            string path = "Assets/_BallQuest/Art/Materials/" + name.Replace(" ", "") + ".physicMaterial";
            var value = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (!value) { value = new PhysicsMaterial(name); AssetDatabase.CreateAsset(value, path); }
            value.dynamicFriction = friction; value.staticFriction = friction; value.bounciness = .015f; value.frictionCombine = friction < .1f ? PhysicsMaterialCombine.Minimum : PhysicsMaterialCombine.Average; EditorUtility.SetDirty(value); return value;
        }
        static void Ui(PhysicsLabTelemetry lab, TMP_FontAsset font)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var go = new GameObject("Laboratory station selector", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(720, 1280);
            for (int i = 0; i < Names.Length; i++)
            {
                int index = i; var button = new GameObject("Select " + Names[i], typeof(RectTransform), typeof(Image), typeof(Button)); button.transform.SetParent(go.transform, false); var rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(12 + (i % 3) * 232, -12 - (i / 3) * 55); rect.sizeDelta = new Vector2(224, 49); button.GetComponent<Image>().color = new Color(.025f, .12f, .24f, .88f);
                // UnityEvent delegates are runtime-only; persist the integer station argument in the scene.
                UnityEditor.Events.UnityEventTools.AddIntPersistentListener(button.GetComponent<Button>().onClick, lab.SelectStation, index);
                var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(button.transform, false); var lr = (RectTransform)label.transform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(3, 3); lr.offsetMax = new Vector2(-3, -3); var tmp = label.GetComponent<TextMeshProUGUI>(); tmp.font = font; tmp.text = (i + 1) + ". " + Names[i]; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            }
            var readout = new GameObject("Actual physics readout", typeof(RectTransform), typeof(TextMeshProUGUI)); readout.transform.SetParent(go.transform, false); var rr = (RectTransform)readout.transform; rr.anchorMin = rr.anchorMax = new Vector2(.5f, 0); rr.pivot = new Vector2(.5f, 0); rr.anchoredPosition = new Vector2(0, 15); rr.sizeDelta = new Vector2(690, 155); var rt = readout.GetComponent<TextMeshProUGUI>(); rt.font = font; rt.fontSize = 22; rt.alignment = TextAlignmentOptions.Center; rt.raycastTarget = false; rt.text = "Physics laboratory: choose a station, then drag to roll."; lab.readout = rt;
            var back = new GameObject("Return to main game", typeof(RectTransform), typeof(Image), typeof(Button)); back.transform.SetParent(go.transform, false); var br = (RectTransform)back.transform; br.anchorMin = br.anchorMax = new Vector2(0, 1); br.pivot = new Vector2(0, 1); br.anchoredPosition = new Vector2(12, -240); br.sizeDelta = new Vector2(180, 48); back.GetComponent<Image>().color = new Color(.05f, .27f, .53f, .9f); UnityEditor.Events.UnityEventTools.AddPersistentListener(back.GetComponent<Button>().onClick, lab.GoBack);
            var backText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); backText.transform.SetParent(back.transform, false); var btr = (RectTransform)backText.transform; btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one; btr.offsetMin = btr.offsetMax = Vector2.zero; var bt = backText.GetComponent<TextMeshProUGUI>(); bt.font = font; bt.text = "BACK TO GAME"; bt.fontSize = 20; bt.alignment = TextAlignmentOptions.Center; bt.raycastTarget = false;
        }
    }
}
