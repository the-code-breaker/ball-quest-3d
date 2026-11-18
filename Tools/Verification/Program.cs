using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BallQuest;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS " + name); passed++;
}

foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "Assets"), "*.cs", SearchOption.AllDirectories))
{
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.CSharp9), path);
    var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
    Check(errors.Length == 0, "C# 9 syntax: " + Path.GetRelativePath(root, path) + (errors.Length > 0 ? " " + string.Join("; ", errors.Select(e => e.ToString())) : ""));
}

var save = new SaveData();
Check(ProgressionRules.Stars(8, 10, 0, 80, 90) == 3, "three-star criteria");
Check(ProgressionRules.Stars(7, 10, 1, 100, 90) == 1, "completion-only star");
Check(ProgressionRules.Complete(save, 1, 3, 30, 1, 2, 70, 1) == 30 && save.unlockedLevel == 2, "unlock next level and grant rewards");
Check(ProgressionRules.Complete(save, 1, 3, 30, 1, 1, 80, 1) == 0 && save.coins == 30 && save.gems == 1 && save.tokens == 1, "replay cannot duplicate rewards");
Check(ProgressionRules.Complete(save, 1, 3, 36, 2, 3, 60, 2) == 6 && save.coins == 36 && save.levels[0].stars == 3 && save.levels[0].bestSeconds == 60, "improved run grants only reward difference");
bool rejected = false;
try { ProgressionRules.Complete(save, 3, 3, 999, 1, 3, 1); } catch (ArgumentOutOfRangeException) { rejected = true; }
Check(rejected && save.coins == 36, "locked level cannot grant progress");
ProgressionRules.Complete(save, 2, 3, 10, 0, 1, 90);
ProgressionRules.Complete(save, 3, 3, 10, 0, 1, 90);
Check(save.unlockedLevel == 3, "last course does not unlock nonexistent content");

var invalid = new SaveData { version = 1, coins = -1, gems = -2, unlockedLevel = 90000, skins = null, balls = null, selectedSkin = "missing" };
invalid.settings.sensitivity = float.NaN; invalid.settings.music = float.PositiveInfinity;
invalid.levels.Add(new LevelRecord { id = 1, stars = 9 }); invalid.levels.Add(new LevelRecord { id = 1 }); invalid.levels.Add(new LevelRecord { id = -1 });
invalid.Validate();
Check(invalid.version == SaveData.CurrentVersion && invalid.coins == 0 && invalid.gems == 0 && invalid.unlockedLevel == 1000, "migration and numeric validation");
Check(invalid.settings.sensitivity == 1 && invalid.settings.music == .45f && invalid.levels.Count == 1 && invalid.levels[0].stars == 3, "nonfinite settings and duplicate records repaired");
Check(invalid.selectedSkin == "classic-red" && invalid.balls.Contains("standard"), "safe cosmetic recovery");

var directory = Path.Combine(root, "TestResults", "save-fixture-" + Guid.NewGuid().ToString("N"));
var codec = new JsonCodec();
var store = new SaveStore(directory, codec); store.Load();
Check(store.Data.coins == 0, "new profile loads offline defaults");
store.Data.coins = 12; store.Data.selectedSkin = "ocean-blue"; store.Data.settings.sensitivity = 1.8f;
Check(store.Save(), "first save creates complete envelope");
using (var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "ball-quest-save.json"))))
    Check(document.RootElement.GetProperty("data").GetProperty("coins").GetInt32() == 12, "save file is valid JSON");
var loaded = new SaveStore(directory, codec); loaded.Load();
Check(loaded.Data.coins == 12 && loaded.Data.selectedSkin == "ocean-blue" && loaded.Data.settings.sensitivity == 1.8f, "save round trip");
store.Data.coins = 24; Check(store.Save(), "second save writes backup");
var filename = Path.Combine(directory, "ball-quest-save.json");
File.WriteAllText(filename, "broken"); loaded.Load();
Check(loaded.Data.coins == 12, "corrupt primary recovers backup");
File.WriteAllText(filename + ".bak", "broken too"); loaded.Load();
Check(loaded.Data.coins == 0 && loaded.Data.selectedSkin == "classic-red", "both corrupt saves recover defaults");

string future = codec.Encode(new SaveData { version = SaveData.CurrentVersion + 1, coins = 99 });
string hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(future)));
string envelope = "{\"checksum\":\"" + hash + "\",\"data\":" + future + "}";
File.WriteAllText(filename, envelope); loaded.Load();
Check(loaded.ReadOnly && !loaded.Save() && File.ReadAllText(filename) == envelope, "newer saves are preserved without overwrite");

// Deliberately retain the isolated fixture for review; no recursive filesystem cleanup.
Console.WriteLine($"{passed} checks passed. Domain C# compiled and executed. Unity API compilation and playtesting were NOT run.");

sealed class JsonCodec : ISaveCodec
{
    readonly JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields = true };
    public string Encode(SaveData value) => JsonSerializer.Serialize(value, options);
    public SaveData Decode(string text)
    {
        try { return JsonSerializer.Deserialize<SaveData>(text, options); }
        catch (JsonException e) { throw new ArgumentException("Invalid save JSON", e); }
    }
}
