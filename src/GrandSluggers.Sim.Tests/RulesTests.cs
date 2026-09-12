using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// data/rules is the one place for the numbers of play (spec §16). The JSON is the source
/// of truth, the code initializers are only a load fallback, and the validator refuses a
/// misspelled or out-of-range field so <c>cli art</c> catches it.
/// </summary>
public sealed class RulesTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void EveryShippedTableExistsAndLoadsCleanly()
    {
        var dir = Path.Combine(_content.Root, RulesTable.Directory);
        foreach (var name in RulesTable.Files)
            Assert.True(File.Exists(Path.Combine(dir, name + ".json")), name);
        Assert.Empty(RulesTable.Validate(_content.Root));
        Assert.Empty(ContentDataValidator.Validate(_content.Root));
    }

    [Fact]
    public void ShippedJsonEqualsTheCodeFallbackFieldForField()
    {
        // The code defaults are a load fallback, not a second table: a number changed in one
        // place and not the other is exactly the drift this epic removes.
        var loaded = RulesTable.Load(_content.Root);
        var defaults = RulesTable.Defaults;
        var differences = new List<string>();
        Compare(loaded, defaults, "rules", differences);
        Assert.Empty(differences);
    }

    [Fact]
    public void ShippedJsonNamesEveryRuleTheCodeDeclares()
    {
        // A rule that exists only as a C# initializer is a literal hiding from the table.
        var dir = Path.Combine(_content.Root, RulesTable.Directory);
        var missing = new List<string>();
        foreach (var name in RulesTable.Files)
        {
            var json = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, name + ".json")))!.AsObject();
            var section = typeof(RulesTable).GetProperty(Capital(name))!.PropertyType;
            MissingFields(json, section, name, missing);
        }
        Assert.Empty(missing);
    }

    [Fact]
    public void CatalogExposesTheLoadedRulesAndMatchPlaysByThem()
    {
        Assert.NotNull(_content.Rules);
        var match = Match.Slice(_content, innings: 3, seed: 1);
        Assert.Same(_content.Rules, match.Rules);
        Assert.Equal(_content.Rules.Running.Bags.TimeOnBagSec, Rules.Default.Running.Bags.TimeOnBagSec);
    }

    [Fact]
    public void UnknownFieldIsAnErrorNotASilentFallback()
    {
        using var fixture = new RulesFixture();
        fixture.Change("running.json", json => json["bags"]!["tagReachFeet"] = 4);

        var errors = RulesTable.Validate(fixture.Root);
        var error = Assert.Single(errors);
        Assert.Contains("running.bags.tagReachFeet is not a rule this table owns", error);
        var thrown = Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Contains("tagReachFeet", thrown.Message);
    }

    [Fact]
    public void MissingFieldFallsBackToTheCodeDefault()
    {
        using var fixture = new RulesFixture();
        fixture.Change("running.json", json => json["bags"]!.AsObject().Remove("tagReachFt"));

        var errors = new List<string>();
        var table = RulesTable.Load(fixture.Root, errors);
        Assert.Empty(errors);
        Assert.Equal(RulesTable.Defaults.Running.Bags.TagReachFt, table.Running.Bags.TagReachFt);
    }

    [Theory]
    [InlineData("fielding.json", "chem", "badErrorChance", 1.4, "fielding.chem.badErrorChance must be between 0 and 1")]
    [InlineData("fielding.json", "throw", "baseFtPerSec", 0, "fielding.throw.baseFtPerSec must be greater than 0")]
    [InlineData("running.json", "bags", "tagReachFt", -3, "running.bags.tagReachFt must be greater than 0")]
    [InlineData("batting.json", "launch", "noiseDeg", -1, "batting.launch.noiseDeg must be finite and at least 0")]
    public void OutOfRangeValuesAreNamedByPath(string file, string section, string field, double value, string expected)
    {
        using var fixture = new RulesFixture();
        fixture.Change(file, json => json[section]![field] = value);

        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal) && e.Contains(fixture.Path(file), StringComparison.Ordinal));
    }

    [Fact]
    public void MissingTableIsAnError()
    {
        using var fixture = new RulesFixture();
        File.Delete(fixture.Path("stars.json"));

        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("stars.json: required rules table is missing", StringComparison.Ordinal));
    }

    [Fact]
    public void CpuLevelMustBeARung()
    {
        using var fixture = new RulesFixture();
        fixture.Change("cpu.json", json => json["level"] = "brutal");

        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("cpu.level must be one of [easy, hard, normal]", StringComparison.Ordinal));
        Assert.Equal(RulesTable.Defaults.Cpu.Normal.TimingSigmaMul, RulesTable.Defaults.Cpu.Active.TimingSigmaMul);
    }

    [Fact]
    public void NormalDifficultyReadsTheTablesAsWritten()
    {
        var level = _content.Rules.Cpu.Active;
        Assert.Equal(1.0, level.TimingSigmaMul);
        Assert.Equal(1.0, level.ReactionMul);
    }

    [Fact]
    public void ALoadedTableChangesThePlayItGoverns()
    {
        // Not a tune: proof the helpers read the table they are handed rather than a literal.
        var batter = _content.Must("rio");
        var shipped = RunnerSystem.BagSec(batter, _content.Rules);
        var slower = new RulesTable
        {
            Running = new RunningRules { BagSec = new BagSecRules { BaseSec = 9, MaxSec = 9 } }
        };
        Assert.True(RunnerSystem.BagSec(batter, slower) > shipped);
    }

    static void Compare(object a, object b, string path, List<string> differences)
    {
        foreach (var p in a.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0) continue;
            var va = p.GetValue(a);
            var vb = p.GetValue(b);
            var name = path + "." + p.Name;
            if (p.PropertyType == typeof(double) || p.PropertyType == typeof(int) || p.PropertyType == typeof(string))
            {
                if (!Equals(va, vb)) differences.Add($"{name}: json {va} vs code {vb}");
                continue;
            }
            if (va is null || vb is null) continue;
            if (p.PropertyType.IsClass && p.PropertyType.Namespace == typeof(RulesTable).Namespace)
                Compare(va, vb, name, differences);
        }
    }

    static void MissingFields(JsonObject json, Type type, string path, List<string> missing)
    {
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0 || p.GetGetMethod() is null || p.GetSetMethod(true) is null) continue;
            var key = char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1);
            if (!json.TryGetPropertyValue(key, out var node) || node is null)
            {
                missing.Add(path + "." + key);
                continue;
            }
            if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && p.PropertyType.Namespace == typeof(RulesTable).Namespace)
                MissingFields(node.AsObject(), p.PropertyType, path + "." + key, missing);
        }
    }

    static string Capital(string name) => char.ToUpperInvariant(name[0]) + name.Substring(1);

    sealed class RulesFixture : IDisposable
    {
        public RulesFixture()
        {
            Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-rules-" + Guid.NewGuid().ToString("N"));
            var source = ContentCatalog.Load().Root;
            Directory.CreateDirectory(Root);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(System.IO.Path.Combine(Root, System.IO.Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, System.IO.Path.Combine(Root, System.IO.Path.GetRelativePath(source, file)));
        }

        public string Root { get; }

        public string Path(string file) => System.IO.Path.Combine(Root, RulesTable.Directory, file);

        public void Change(string file, Action<JsonObject> change)
        {
            var path = Path(file);
            var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
