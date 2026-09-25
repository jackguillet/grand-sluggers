using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// data/rules is the one place for the numbers of play (spec §16). The JSON is the only
/// source: the tables carry no code defaults, and the loader refuses a missing, misspelled or
/// out-of-range field so <c>cli art</c> catches it.
/// </summary>
public sealed class RulesTests
{
    /// <summary>The loader skips comments in the rules tables (a tuning note sits beside its number); these tests read them the same way.</summary>
    static readonly System.Text.Json.JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    readonly ContentCatalog _content = Shipped.Content;

    [Fact]
    public void EveryShippedTableExistsAndLoadsCleanly()
    {
        var dir = Path.Combine(_content.Root.Shipped, RulesTable.Directory);
        foreach (var name in RulesTable.Files)
            Assert.True(File.Exists(Path.Combine(dir, name + ".json")), name);
        Assert.Empty(RulesTable.Validate(_content.Root));
        Assert.Empty(ContentDataValidator.Validate(_content.Root));
    }

    /// <summary>
    /// The other direction, which nothing checked until #725 added a file. A table the code does not
    /// name in <see cref="RulesTable.Files"/> is never loaded and never validated — it sits in
    /// <c>data/rules</c> looking authoritative while nothing reads it. No test would have failed; the
    /// file would simply have had no effect.
    /// </summary>
    [Fact]
    public void EveryJsonInTheRulesFolderIsATableTheCodeNames()
    {
        var dir = Path.Combine(_content.Root.Shipped, RulesTable.Directory);
        var onDisk = Directory.EnumerateFiles(dir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(f => f, StringComparer.Ordinal);

        Assert.Equal(RulesTable.Files.OrderBy(f => f, StringComparer.Ordinal), onDisk);
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

    /// <summary>The charge line (§4.1, §5.1): a tap and a charge are both reachable, so 0 &lt; slapBelow &lt; chargeAt ≤ 1.</summary>
    [Theory]
    [InlineData(0.0, 0.55, "match.charge.slapBelow must be above 0")]
    [InlineData(0.6, 0.55, "match.charge.slapBelow must be below match.charge.chargeAt")]
    [InlineData(0.55, 0.55, "match.charge.slapBelow must be below match.charge.chargeAt")]
    [InlineData(0.2, 1.2, "match.charge.chargeAt")]
    public void AnUnreachableChargeLineIsRefusedByName(double slapBelow, double chargeAt, string expected)
    {
        using var fixture = new RulesFixture();
        fixture.Change("match.json", json =>
        {
            json["charge"]!["slapBelow"] = slapBelow;
            json["charge"]!["chargeAt"] = chargeAt;
        });

        Assert.Contains(RulesTable.Validate(fixture.Root), e => e.Contains(expected, StringComparison.Ordinal)
                                                               && e.Contains(fixture.Path("match.json"), StringComparison.Ordinal));
    }

    [Fact]
    public void TheShippedChargeLineIsTheOneThePlayWasTunedOn()
    {
        var charge = Shipped.Content.Rules.Match.Charge;
        Assert.Equal(0.2, charge.SlapBelow);
        Assert.Equal(0.55, charge.ChargeAt);
    }

    [Fact]
    public void AMissingFieldIsAnErrorNotACodeDefault()
    {
        using var fixture = new RulesFixture();
        fixture.Change("running.json", json => json["bags"]!.AsObject().Remove("tagReachFt"));

        var errors = RulesTable.Validate(fixture.Root);
        Assert.True(errors.Count == 1, string.Join("\n", errors));
        var error = errors[0];
        Assert.Contains("running.bags.tagReachFt is missing", error, StringComparison.Ordinal);
        Assert.Contains(fixture.Path("running.json"), error, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
        Assert.Throws<InvalidDataException>(() => Rules.ForProcess(new DataRoot(fixture.Root)));
    }

    /// <summary>
    /// The rail under the one above: no rule table holds a number of its own. A table built with
    /// <c>new</c> is all zeros and empties, so the only way a number reaches play is the JSON; a C#
    /// initializer added tomorrow is a second default and fails here, by field.
    /// </summary>
    [Fact]
    public void TheTablesCarryNoCodeDefaults()
    {
        var defaults = new List<string>();
        NoDefaults(typeof(RulesTable), "rules", defaults, new HashSet<Type>());
        Assert.True(defaults.Count == 0, string.Join("\n", defaults));
    }

    static void NoDefaults(Type type, string path, List<string> defaults, HashSet<Type> seen)
    {
        if (!seen.Add(type)) return;
        var blank = Activator.CreateInstance(type)!;
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0 || p.GetSetMethod(true) is null) continue;
            var name = path + "." + p.Name;
            var value = p.GetValue(blank);
            switch (value)
            {
                case double d when d != 0: defaults.Add($"{name} = {d}"); break;
                case int i when i != 0: defaults.Add($"{name} = {i}"); break;
                case bool b when b: defaults.Add($"{name} = true"); break;
                case string str when str.Length > 0: defaults.Add($"{name} = \"{str}\""); break;
                case Array a when a.Length > 0: defaults.Add($"{name} has {a.Length} entries"); break;
            }
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            if (t.IsClass && t != typeof(string) && t.Namespace == typeof(RulesTable).Namespace
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(t))
            {
                if (value is not null && value.GetType() != t) continue;
                if (value is null && p.GetCustomAttribute<OptionalAttribute>() is null)
                    defaults.Add($"{name} is null in a blank table");
                NoDefaults(t, name, defaults, seen);
            }
        }
    }

    /// <summary>
    /// The pitch family library is named rows, so every guard this file already has reaches inside
    /// one: ranges, unknown keys, and the parity comparison (spec §4.3, #810). A row that slipped
    /// into a Dictionary would load and be checked by nothing.
    /// </summary>
    [Theory]
    [InlineData("fastball", "mph", 0, "pitching.families.fastball.mph must be greater than 0")]
    [InlineData("changeup", "mph", -1, "pitching.families.changeup.mph must be greater than 0")]
    [InlineData("changeup", "hangUntil", 1.2, "pitching.families.changeup.hangUntil must be between 0 and 1")]
    [InlineData("changeup", "hangUntil", -0.1, "pitching.families.changeup.hangUntil must be between 0 and 1")]
    [InlineData("changeup", "staminaCost", -3, "pitching.families.changeup.staminaCost must be finite and at least 0")]
    [InlineData("fastball", "dropFt", -0.5, "pitching.families.fastball.dropFt must be finite and at least 0")]
    public void AFamilyRowIsRangeCheckedLikeEveryOtherRule(string family, string field, double value, string expected)
    {
        using var fixture = new RulesFixture();
        fixture.Change("pitching.json", json => json["families"]![family]![field] = value);

        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal)
                                     && e.Contains(fixture.Path("pitching.json"), StringComparison.Ordinal));
    }

    [Fact]
    public void AnUnknownKeyInsideAFamilyRowIsAnError()
    {
        using var fixture = new RulesFixture();
        fixture.Change("pitching.json", json => json["families"]!["changeup"]!["changeupDropFt"] = 0.9);

        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains(
            "pitching.families.changeup.changeupDropFt is not a rule this table owns", StringComparison.Ordinal));
    }

    [Fact]
    public void AFamilyWhoseDropNeverReachesTheAimIsRefused()
    {
        // The cross-field rule the attributes cannot say (§4.3, #668): hang + dump must arrive by
        // the plate, or the clamp at u=1 hides the miss as a snap.
        using var fixture = new RulesFixture();
        fixture.Change("pitching.json", json => json["families"]!["changeup"]!["dumpRate"] = 0.5);

        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains(
            "pitching.families.changeup must finish its drop in flight", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("fielding.json", "chem", "slantChance", 1.4, "fielding.chem.slantChance must be between 0 and 1")]
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
        Assert.Equal(Rules.Default.Cpu.Normal.TimingSigmaMul, Rules.Default.Cpu.Active.TimingSigmaMul);
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
        var rules = _content.Rules;
        var slower = rules with { Running = rules.Running with { BagSec = rules.Running.BagSec with { BaseSec = 9, MaxSec = 9 } } };
        Assert.True(RunnerSystem.BagSec(batter, slower) > shipped);
    }

    sealed class RulesFixture : IDisposable
    {
        public RulesFixture()
        {
            Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-rules-" + Guid.NewGuid().ToString("N"));
            var source = Shipped.Content.Root.Shipped;
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
            var json = JsonNode.Parse(File.ReadAllText(path), null, JsonComments)!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
