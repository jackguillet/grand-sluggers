using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The diamond every park shares is a rules table, not a C# constant (R3, #711). The JSON is
/// the source of truth; <see cref="Diamond"/> reports what it says; and a process pointed at a
/// different data root plays a different infield without the shipped one being touched.
/// </summary>
public sealed class InfieldGeometryTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    static readonly JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    [Fact]
    public void DiamondReportsWhatTheShippedTableSays()
    {
        var infield = RulesTable.Load(_content.Root).Infield;

        Assert.Equal(infield.BaselineFt, Diamond.Baseline);
        Assert.Equal(infield.MoundFt, Diamond.Mound);
        Assert.Equal((0d, 0d), Diamond.Home);
        Assert.Equal((infield.CornerFt, infield.CornerFt), Diamond.First);
        Assert.Equal((0d, infield.SecondFt), Diamond.Second);
        Assert.Equal((-infield.CornerFt, infield.CornerFt), Diamond.Third);
        Assert.Equal((0d, infield.MoundFt), Diamond.Rubber);
    }

    /// <summary>
    /// The values #711 migrated, spelled out. If a later change to the table moves a bag, this is
    /// the test that says so out loud instead of letting six hundred routes drift quietly.
    /// </summary>
    [Fact]
    public void TheShippedInfieldIsTheOneThatWasMigrated()
    {
        var infield = RulesTable.Load(_content.Root).Infield;

        Assert.Equal(90, infield.BaselineFt);
        Assert.Equal(60.5, infield.MoundFt);
        Assert.Equal(63.64, infield.CornerFt);
        Assert.Equal(127.28, infield.SecondFt);
    }

    [Fact]
    public void BagReadsTheTableAndHomeIsTheOrigin()
    {
        Assert.Equal(Diamond.Home, Diamond.Bag(0));
        Assert.Equal(Diamond.First, Diamond.Bag(1));
        Assert.Equal(Diamond.Second, Diamond.Bag(2));
        Assert.Equal(Diamond.Third, Diamond.Bag(3));
        Assert.Equal(Diamond.Home, Diamond.Bag(4));
    }

    /// <summary>The corners are one baseline apart from home, and from second, to within the rounding they were authored at.</summary>
    [Fact]
    public void TheShippedInfieldIsStillASquare()
    {
        var b = Diamond.Baseline;
        Assert.Equal(b, Diamond.Dist(Diamond.Home.X, Diamond.Home.Z, Diamond.First.X, Diamond.First.Z), 2);
        Assert.Equal(b, Diamond.Dist(Diamond.Home.X, Diamond.Home.Z, Diamond.Third.X, Diamond.Third.Z), 2);
        Assert.Equal(b, Diamond.Dist(Diamond.First.X, Diamond.First.Z, Diamond.Second.X, Diamond.Second.Z), 2);
        Assert.Equal(b, Diamond.Dist(Diamond.Third.X, Diamond.Third.Z, Diamond.Second.X, Diamond.Second.Z), 2);
    }

    /// <summary>The rubber is one number, so the plate frame must forward to it rather than keep a compile-time copy.</summary>
    [Fact]
    public void ThePlateFrameReadsTheSameRubber()
    {
        Assert.Equal(Diamond.Mound, PitchFlight.MoundZ);
        Assert.Equal(Diamond.Rubber.Z, PitchFlight.MoundZ);
    }

    /// <summary>The pitcher stands on the rubber the table names, not on a spot of his own.</summary>
    [Fact]
    public void ThePitcherStartsOnTheRubber()
    {
        Assert.Equal(Diamond.Rubber, Diamond.Positions["P"]);
    }

    /// <summary>
    /// The point of the migration: another data root plays another infield. This is what the
    /// compact profile will supply, and it never edits the shipped table to do it.
    /// </summary>
    [Fact]
    public void AnotherDataRootPlaysAnotherInfield()
    {
        using var trial = new CopiedRoot();
        trial.Change("infield.json", json =>
        {
            json["baselineFt"] = 80;
            json["cornerFt"] = 56.57;
            json["secondFt"] = 113.14;
        });

        var infield = RulesTable.Load(trial.Root).Infield;

        Assert.Equal(80, infield.BaselineFt);
        Assert.Equal((56.57, 56.57), (infield.CornerFt, infield.CornerFt));
        Assert.Equal(113.14, infield.SecondFt);
        Assert.Equal(60.5, infield.MoundFt);

        // The shipped table is untouched, and the running process still plays it.
        Assert.Equal(90, Diamond.Baseline);
    }

    /// <summary>A trial root is named to a whole process, so the control and the trial are two runs to diff.</summary>
    [Fact]
    public void TheNamedRootIsTakenWhenItIsADataRoot()
    {
        using var trial = new CopiedRoot();
        Assert.Equal(trial.Root, ContentCatalog.NamedDataRoot(trial.Root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoNamedRootFallsBackToTheRootAboveTheBinary(string? value)
    {
        Assert.Null(ContentCatalog.NamedDataRoot(value));
    }

    /// <summary>
    /// Pointing at the rules folder instead of the data root must stop. Falling back silently would
    /// run the control while the operator believed they were running the trial.
    /// </summary>
    [Fact]
    public void ARootThatIsNotADataRootIsRefusedRatherThanIgnored()
    {
        var rules = Path.Combine(_content.Root.Shipped, RulesTable.Directory);
        var ex = Assert.Throws<DirectoryNotFoundException>(() => ContentCatalog.NamedDataRoot(rules));
        Assert.Contains(ContentCatalog.DataRootVariable, ex.Message);
    }

    [Fact]
    public void AnInfieldOutOfRangeIsNamedByPath()
    {
        using var trial = new CopiedRoot();
        trial.Change("infield.json", json => json["baselineFt"] = 0);

        Assert.Contains(
            RulesTable.Validate(trial.Root),
            e => e.Contains("infield.baselineFt") && e.Contains("greater than 0"));
    }

    /// <summary>Second sits past the corners and past the rubber; a table that says otherwise is not a diamond.</summary>
    [Fact]
    public void SecondMustSitBeyondTheRubberAndTheCorners()
    {
        using var trial = new CopiedRoot();
        trial.Change("infield.json", json => json["secondFt"] = 10);

        var errors = RulesTable.Validate(trial.Root);
        Assert.Contains(errors, e => e.Contains("infield.moundFt"));
        Assert.Contains(errors, e => e.Contains("infield.cornerFt"));
    }

    /// <summary>
    /// The geometry must not survive as a bare literal anywhere in the sim, or a trial root would
    /// give a correct sim and a diamond drawn in the old place — #711 found the bags in the play
    /// view and the mound in the pitch reset, none of which would have moved on their own.
    ///
    /// <c>Rules.cs</c> is the one file allowed to spell the numbers: that is where
    /// <see cref="InfieldRules"/> declares them, and its initializers are the documented load
    /// fallback for a field the JSON does not name, not a second copy of the table.
    /// </summary>
    [Theory]
    [InlineData("63.64")]
    [InlineData("127.28")]
    [InlineData("60.5")]
    public void NoBareInfieldLiteralIsLeftInTheSource(string literal)
    {
        var src = Path.GetFullPath(Path.Combine(_content.Root.Shipped, "..", "src"));
        var offenders = Directory
            .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".Tests") && Path.GetFileName(f) != "Rules.cs")
            .Where(f => File.ReadLines(f).Any(Spells))
            .Select(f => Path.GetRelativePath(src, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"{literal} must come from data/rules/infield.json: " + string.Join(", ", offenders));

        bool Spells(string line)
        {
            var code = line.TrimStart();
            if (code.StartsWith("//") || code.StartsWith("*")) return false;
            return code.Contains(literal);
        }
    }

    /// <summary>
    /// A whole copy of the shipped data root, free to be edited. The shipped one never is. A trial
    /// carries only its own diff instead (#716, <see cref="TrialOverlayTests"/>); this fixture is a
    /// second root, which is what <see cref="ContentCatalog.DataRootVariable"/> points a process at.
    /// </summary>
    sealed class CopiedRoot : IDisposable
    {
        public CopiedRoot()
        {
            Root = Path.Combine(Path.GetTempPath(), "grand-sluggers-infield-" + Guid.NewGuid().ToString("N"));
            var source = ContentCatalog.Load().Root.Shipped;
            Directory.CreateDirectory(Root);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(Root, Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(Root, Path.GetRelativePath(source, file)));
        }

        public string Root { get; }

        public void Change(string file, Action<JsonObject> change)
        {
            var path = Path.Combine(Root, RulesTable.Directory, file);
            var json = JsonNode.Parse(File.ReadAllText(path), null, JsonComments)!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
