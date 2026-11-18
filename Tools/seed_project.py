"""Create deterministic Unity source assets without requiring the Editor. Never overwrites existing assets.

The checked-in output is ready for Unity import. Unity's ProjectGenerator creates URP, TMP and prefab
assets using the real Editor APIs on first import. Python is not needed to open or build the game.
"""
from pathlib import Path
import json
import math
import uuid

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets/_BallQuest"


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, "ballquest3d/" + str(path).replace("\\", "/")).hex


def write(path, text):
    path = ROOT / path
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        path.write_text(text, encoding="utf-8", newline="\n")


def v(x=0, y=0, z=0):
    return {"x": x, "y": y, "z": z}


def track(x, start, end, top=0, width=8, style=0, rails=True):
    return dict(position=v(x, top - .4, (start + end) / 2), size=v(width, .8, end - start), rotation=v(), style=style, rails=rails)


def ramp(start, end, low, high):
    angle = -math.degrees(math.atan2(high - low, end - start))
    return dict(position=v(0, (low + high) / 2 - .4, (start + end) / 2), size=v(8, .8, math.hypot(end - start, high - low)), rotation=v(angle, 0, 0), style=1, rails=True)


def obstacle(kind, x, y, z, size, axis=v(1), amplitude=2, speed=1, phase=0):
    return dict(kind=kind, position=v(x, y, z), size=v(*size), axis=axis, amplitude=amplitude, speed=speed, phase=phase)


def pickup(x, y, z, kind=0):
    return dict(kind=kind, position=v(x, y, z))


def campaign():
    offsets = [0, 0, 1.5, 1.5, -1, -1, 0, 0, 1.5, 1.5, 0, 0]
    first = dict(id=1, title="First Flight", hint="Drag up to roll. Drag sideways to steer. Release to brake.", world=0,
                 spawn=v(0, 1.4, 2), finish=v(0, 1, 235), checkpoints=[v(-1, 1, 94), v(1.5, 1, 184)],
                 tracks=[track(x, i * 20, (i + 1) * 20) for i, x in enumerate(offsets)],
                 obstacles=[obstacle(0, 1.5, .9, 53, (5.5, .6, .5), speed=.75),
                            obstacle(1, -1, .9, 108, (2, 1.8, 1), amplitude=2.1, speed=.85),
                            obstacle(0, 0, .9, 151, (5.3, .6, .5), speed=.65, phase=.7),
                            obstacle(1, 1.5, .8, 204, (1.6, 1.6, 1), amplitude=2, speed=.7)],
                 pickups=[pickup(offsets[min(11, int(z // 20))] + math.sin(z * .08) * 1.2, .9, z) for z in range(12, 228, 6)] + [pickup(2.2, .9, 217, 1)],
                 parSeconds=95, killHeight=-12, allowJump=False)
    tracks2 = [track(0, 0, 28), track(-1.5, 28, 52), track(-1.5, 52, 76, width=7), track(0, 76, 100),
               track(0, 112, 136), track(2, 136, 162), track(2, 162, 188, width=6.5), track(0, 188, 212),
               track(-1.5, 212, 236), track(0, 236, 264)]
    second = dict(id=2, title="Clockwork Crossing", hint="Watch the barriers. Brake, wait, then roll through.", world=0,
                  spawn=v(0, 1.4, 2), finish=v(0, 1, 257), checkpoints=[v(0, 1, 88), v(0, 1, 196)], tracks=tracks2,
                  obstacles=[obstacle(2, -1.5, 1.5, 43, (5.6, .6, .7), axis=v(0, 0, 1), amplitude=2.5, speed=1.1),
                             obstacle(1, -1.5, .9, 65, (2, 1.8, 1), amplitude=1.8, speed=1.2),
                             obstacle(3, 0, -.4, 106, (6.5, .8, 14), amplitude=1.6, speed=.65),
                             obstacle(0, 2, .9, 150, (4.6, .6, .6), speed=.9),
                             obstacle(1, 2, .9, 178, (1.5, 1.8, 1), amplitude=1.7, speed=1),
                             obstacle(2, -1.5, 1.3, 224, (5.4, .5, .6), axis=v(0, 0, 1), amplitude=2, speed=1.2)],
                  pickups=[pickup(0 if z < 28 or 76 <= z < 136 or 188 <= z < 212 or z >= 236 else -1.5 if z < 76 or z >= 212 else 2, .9, z) for z in range(12, 253, 6)] + [pickup(2.7, .9, 130, 1)],
                  parSeconds=115, killHeight=-12, allowJump=False)
    third = dict(id=3, title="Cloudstep Summit", hint="Climb the ramps. Tap JUMP or press Space for the high coins.", world=0,
                 spawn=v(0, 1.4, 2), finish=v(0, 1, 277), checkpoints=[v(0, 3, 98), v(0, 1, 200)],
                 tracks=[track(0, 0, 28), track(0, 28, 52), ramp(52, 72, 0, 2), track(0, 72, 92, 2), track(0, 92, 112, 2),
                         ramp(112, 132, 2, 0), track(0, 132, 160), track(0, 172, 200), track(-1.5, 200, 224, width=6.5),
                         track(-1.5, 224, 248, width=6.5), track(0, 248, 284)],
                 obstacles=[obstacle(1, 0, .9, 31, (1.8, 1.8, 1), amplitude=2.2, speed=1),
                            obstacle(5, 0, 2.08, 83, (3, .15, 2), amplitude=2, speed=1),
                            obstacle(0, 0, 2.9, 104, (5.2, .6, .5), speed=.6),
                            obstacle(3, 0, -.4, 166, (6, .8, 14), amplitude=1.5, speed=.9),
                            obstacle(1, -1.5, .9, 215, (1.5, 1.8, .8), amplitude=1.8, speed=1.3),
                            obstacle(0, 0, .9, 261, (5.2, .6, .5), speed=.85)],
                 pickups=[], parSeconds=135, killHeight=-12, allowJump=True)
    for z in range(12, 273, 6):
        height = 0 if z < 52 or z >= 132 else (z - 52) * .1 if z < 72 else 2 if z <= 112 else 2 - (z - 112) * .1
        third["pickups"].append(pickup(-1.5 if 200 <= z < 248 else 0, height + .9, z))
    third["pickups"] += [pickup(0, 4.8, 89, 1), pickup(2.1, 2.9, 94, 2)]
    return {"levels": [first, second, third]}


def scalar(value):
    if isinstance(value, bool): return "1" if value else "0"
    if isinstance(value, str): return json.dumps(value, ensure_ascii=True)
    if value is None: return ""
    return str(value)


def yaml_fields(data, indent=2):
    lines = []
    for key, value in data.items():
        prefix = " " * indent + key + ":"
        if isinstance(value, dict):
            lines.append(prefix + " {" + ", ".join(k + ": " + scalar(val) for k, val in value.items()) + "}")
        elif isinstance(value, list):
            lines.append(prefix + (" []" if not value else ""))
            for item in value:
                if isinstance(item, dict):
                    fields = yaml_fields(item, indent + 2)
                    lines.append(" " * indent + "- " + fields[0].lstrip())
                    lines += fields[1:]
                else: lines.append(" " * indent + "- " + scalar(item))
        else: lines.append(prefix + " " + scalar(value))
    return lines


def asset(relative, script, data):
    header = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(script)}, type: 3}}
  m_Name: {Path(relative).stem}
  m_EditorClassIdentifier:
"""
    write(relative, header + "\n".join(yaml_fields(data)) + "\n")


def reference(relative):
    return {"fileID": 11400000, "guid": guid(relative), "type": 2}


def main():
    content_path = "Assets/_BallQuest/Content/OpeningCampaign.json"
    write(content_path, json.dumps(campaign(), indent=2) + "\n")
    data = json.loads((ROOT / content_path).read_text(encoding="utf-8"))
    level_paths = []
    for level in data["levels"]:
        path = f"Assets/_BallQuest/ScriptableObjects/Levels/Level{level['id']:03d}.asset"
        asset(path, "Assets/_BallQuest/Scripts/Levels/LevelDefinition.cs", level); level_paths.append(path)

    profiles = [
        ("standard", "Standard", 0, 1, 13, 6.5, 3.2, .22, 5.8, .08, .65, 1, "Balanced grip and momentum."),
        ("heavy", "Heavy Metal", 1, 3, 10, 6, 2, .12, 4, .02, .75, 12, "High mass, strong momentum. Puzzle interactions arrive later."),
        ("rubber", "Rubber", 2, .8, 12, 6.2, 3, .3, 7.5, .38, .8, 3, "Higher bounce and a stronger jump."),
        ("magnetic", "Magnetic", 3, 1.3, 12, 6.4, 3.5, .2, 5.8, .05, .7, 31, "Magnetic puzzles are planned for later worlds."),
        ("feather", "Feather", 4, .3, 11, 6, 4, .6, 6.5, .1, .55, 45, "Light handling; falling speed is capped at 3 m/s."),
        ("energy", "Energy", 5, 1, 14, 7, 3.2, .3, 6, .1, .65, 81, "Powered mechanisms are planned for Neon City.")]
    keys = ["id", "displayName", "ability", "mass", "acceleration", "maxSpeed", "braking", "airControl", "jumpSpeed", "bounce", "friction", "unlockLevel", "description"]
    ball_paths = []
    for profile in profiles:
        path = f"Assets/_BallQuest/ScriptableObjects/Balls/{profile[0]}.asset"
        asset(path, "Assets/_BallQuest/Scripts/Player/BallProfile.cs", dict(zip(keys, profile))); ball_paths.append(path)
    skins = [
        ("classic-red", "Classic Red", (1, .15, .22), (.96, .96, 1), 0, False, False, 0),
        ("ocean-blue", "Ocean Blue", (.08, .5, 1), (.2, .95, .85), .2, False, False, 0),
        ("golden", "Golden Sphere", (1, .67, .15), (.4, .22, .06), .8, False, False, 30),
        ("rainbow", "Rainbow Ball", (1, .3, .5), (1, 1, 1), .1, False, True, 45),
        ("galaxy", "Galaxy Ball", (.12, .07, .3), (.5, .18, .8), .4, True, True, 50),
        ("lava", "Lava Ball", (.12, .03, .03), (1, .3, .05), .3, True, True, 55),
        ("crystal", "Crystal Ball", (.3, .8, 1), (.9, .95, 1), .75, False, True, 40),
        ("neon", "Neon Ball", (.1, 1, .7), (.03, .15, .2), .3, True, True, 50),
        ("soccer", "Soccer Ball", (.95, .95, .95), (.05, .07, .12), 0, False, False, 25),
        ("marble", "Marble Ball", (.2, .4, .6), (.9, .95, 1), .3, False, False, 35)]
    skin_paths = []
    for name, title, color, stripe, metal, glow, trail, price in skins:
        path = f"Assets/_BallQuest/ScriptableObjects/Skins/{name}.asset"
        rgba = lambda rgb: dict(zip(["r", "g", "b", "a"], [*rgb, 1]))
        asset(path, "Assets/_BallQuest/Scripts/Player/BallSkin.cs", dict(id=name, displayName=title, color=rgba(color), stripe=rgba(stripe), metallic=metal, smoothness=.7, glow=glow, trail=trail, price=price)); skin_paths.append(path)
    # References must be YAML inline maps, not nested serialized objects.
    path = "Assets/_BallQuest/ScriptableObjects/GameCatalog.asset"
    asset(path, "Assets/_BallQuest/Scripts/Levels/GameCatalog.cs", {})
    catalog = (ROOT / path).read_text(encoding="utf-8")
    if "  levels:" not in catalog:
        for key, paths in [("levels", level_paths), ("balls", ball_paths), ("skins", skin_paths)]:
            catalog += "  " + key + ":\n"
            for item in paths: catalog += "  - {fileID: 11400000, guid: " + guid(item) + ", type: 2}\n"
        catalog += "  palette: []\n  font: {fileID: 0}\n  pipeline: {fileID: 0}\n"
        (ROOT / path).write_text(catalog, encoding="utf-8", newline="\n")
    write("Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity", scene())
    write("ProjectSettings/EditorBuildSettings.asset", f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity
    guid: {guid('Assets/_BallQuest/Scenes/Bootstrap/BallQuest.unity')}
  m_configObjects: {{}}
""")
    write("ProjectSettings/ProjectSettings.asset", """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 28
  companyName: Ball Quest Studio
  productName: Ball Quest 3D
  bundleVersion: 0.1.0
  defaultScreenWidth: 540
  defaultScreenHeight: 960
  defaultScreenOrientation: 0
  usePlayerLog: 1
  activeInputHandler: 1
  applicationIdentifier:
    Android: com.ballqueststudio.ballquest3d
    iPhone: com.ballqueststudio.ballquest3d
  AndroidMinSdkVersion: 26
  AndroidTargetSdkVersion: 0
  AndroidTargetArchitectures: 2
  scriptingBackend:
    Android: 1
    iPhone: 1
  apiCompatibilityLevelPerPlatform: {}
""")
    write("ProjectSettings/TimeManager.asset", """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!5 &1
TimeManager:
  m_ObjectHideFlags: 0
  Fixed Timestep: 0.016666668
  Maximum Allowed Timestep: 0.1
  m_TimeScale: 1
  Maximum Particle Timestep: 0.03
""")
    metadata()
    print("Seed assets present: 3 levels, 6 profiles, 10 skins, catalog, entry scene and project settings.")


def scene():
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1001}}
  - component: {{fileID: 1002}}
  m_Layer: 0
  m_Name: Ball Quest Bootstrap
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &1001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &1002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid('Assets/_BallQuest/Scripts/Core/GameBootstrap.cs')}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
  catalog: {{fileID: 11400000, guid: {guid('Assets/_BallQuest/ScriptableObjects/GameCatalog.asset')}, type: 2}}
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {{fileID: 1001}}
"""


def metadata():
    for path in sorted((ROOT / "Assets").rglob("*")):
        if path.suffix == ".meta": continue
        rel = path.relative_to(ROOT).as_posix()
        if path.is_dir():
            content = f"fileFormatVersion: 2\nguid: {guid(rel)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n"
        else:
            importer = "MonoImporter" if path.suffix == ".cs" else "AssemblyDefinitionImporter" if path.suffix == ".asmdef" else "NativeFormatImporter" if path.suffix == ".asset" else "DefaultImporter"
            content = f"fileFormatVersion: 2\nguid: {guid(rel)}\n{importer}:\n  externalObjects: {{}}\n"
            if path.suffix == ".cs": content += "  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n"
            if path.suffix == ".asset": content += "  mainObjectFileID: 11400000\n"
            content += "  userData:\n  assetBundleName:\n  assetBundleVariant:\n"
        write(rel + ".meta", content)


if __name__ == "__main__": main()
