using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The diamond every park shares is a rules table, not a C# constant (R3, #711), and so are the
/// spots the nine bodies start on (#725). The JSON is the source of truth; <see cref="Diamond"/>
/// reports what it says; and a process pointed at a different data root plays a different infield
/// — and stands a different defence — without the shipped one being touched.
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

        // #729's two, at the constants they replaced on ParkDiamond.
        Assert.Equal(50, infield.InnerHalfFt);
        Assert.Equal(92, infield.BackArcFt);
    }

    /// <summary>
    /// The ground dress reports the table too (#729). <see cref="ParkDiamond"/> draws the dirt the
    /// bags sit on, and it is measured from them — the grass diamond from second and the mound, the
    /// back arc from the rubber — so a root that moves the bags without moving these draws the old
    /// field under the new one.
    ///
    /// <para>
    /// The rest of <see cref="ParkDiamond"/> stays in feet on purpose. Path width, the bag pads, the
    /// home pad, the mound table and the warning track are bodies and equipment; a 10-ft path is
    /// 10 ft because a fielder is the size a fielder is, and a smaller diamond does not shrink them.
    /// </para>
    /// </summary>
    [Fact]
    public void ParkDiamondReportsWhatTheShippedTableSays()
    {
        var infield = RulesTable.Load(_content.Root).Infield;

        Assert.Equal((float)infield.InnerHalfFt, ParkDiamond.InnerHalf);
        Assert.Equal((float)infield.BackArcFt, ParkDiamond.BackR);
        Assert.Equal((float)(infield.MoundFt + infield.BackArcFt), ParkDiamond.DirtMaxZ);
        Assert.Equal((float)infield.BackArcFt, ParkDiamond.DirtMaxX);

        // The dress the decision left in feet, named so a later reader does not migrate them too.
        Assert.Equal(10f, ParkDiamond.PathWidth);
        Assert.Equal(12f, ParkDiamond.BagPadR);
        Assert.Equal(18f, ParkDiamond.HomePackedR);

        // The grass vertex clears the bag pad it points at. This is the hairline the validator
        // deliberately does not refuse a table over: 1.64 ft shipped, and only 0.13 ft at C80.
        Assert.Equal(1.64, infield.CornerFt - (infield.InnerHalfFt + ParkDiamond.BagPadR), 2);
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
    /// The seven that migrated in #725 forward to <c>fielders.json</c>, and the two that did not are
    /// named here so the split is a decision on the record rather than an omission: the pitcher is
    /// the rubber <c>infield.json</c> already names, the catcher is <see cref="HomeSet.CatcherZ"/>.
    /// </summary>
    [Fact]
    public void DiamondReportsTheStartsTheShippedTableSays()
    {
        var fielders = RulesTable.Load(_content.Root).Fielders;

        foreach (var pos in new[] { "1B", "2B", "3B", "SS", "LF", "CF", "RF" })
            Assert.Equal(fielders.Spot(pos), Diamond.Positions[pos]);

        Assert.Equal(Diamond.Rubber, Diamond.Positions["P"]);
        Assert.Equal((0d, HomeSet.CatcherZ), Diamond.Positions["C"]);
        Assert.Equal(Diamond.Order.Length, Diamond.Positions.Count);
    }

    /// <summary>
    /// The values #725 migrated, spelled out. This is the parity proof in this process: the left side
    /// is read through <see cref="Diamond.Positions"/> — the path the whole sim uses — and the right
    /// side is typed, so it is independent of both the JSON and the C# initializers the JSON is
    /// compared against. <c>RulesTests.ShippedJsonEqualsTheCodeFallbackFieldForField</c> is not a
    /// parity proof for this change: it pins the file to the new initializers, and a digit typed the
    /// same way in both would pass it.
    /// </summary>
    [Fact]
    public void TheShippedStartsAreTheOnesThatWereMigrated()
    {
        Assert.Equal((78d, 72d), Diamond.Positions["1B"]);
        Assert.Equal((42d, 118d), Diamond.Positions["2B"]);
        Assert.Equal((-78d, 72d), Diamond.Positions["3B"]);
        Assert.Equal((-42d, 118d), Diamond.Positions["SS"]);
        Assert.Equal((-110d, 250d), Diamond.Positions["LF"]);
        Assert.Equal((0d, 305d), Diamond.Positions["CF"]);
        Assert.Equal((110d, 250d), Diamond.Positions["RF"]);

        // The two that did not migrate, at the numbers they have always been.
        Assert.Equal((0d, 60.5d), Diamond.Positions["P"]);
        Assert.Equal((0d, -15d), Diamond.Positions["C"]);
    }

    /// <summary>
    /// The point of the migration, for the starts: another data root stands another defence. Before
    /// #725 no root could — the seven were C# literals, so a compact park was played by fielders
    /// standing where a 90-ft field put them.
    /// </summary>
    [Fact]
    public void AnotherDataRootStandsAnotherDefence()
    {
        using var trial = new CopiedRoot();
        trial.Change("fielders.json", json =>
        {
            json["center"]!["zFt"] = 213.5;
            json["left"]!["xFt"] = -77.09;
            json["left"]!["zFt"] = 175.19;
        });

        var fielders = RulesTable.Load(trial.Root).Fielders;

        Assert.Equal((0d, 213.5d), fielders.Spot("CF"));
        Assert.Equal((-77.09d, 175.19d), fielders.Spot("LF"));

        // The shipped table is untouched, and the running process still stands it.
        Assert.Equal((0d, 305d), Diamond.Positions["CF"]);
    }

    /// <summary>
    /// A start is a point, so x may be negative and z may not: a fielder behind home is not a
    /// defence, it is a typo. Both halves are named by path rather than found in a trace.
    /// </summary>
    [Fact]
    public void AFielderStartOutOfRangeIsNamedByPath()
    {
        using var trial = new CopiedRoot();
        trial.Change("fielders.json", json => json["center"]!["zFt"] = -5);

        var errors = RulesTable.Validate(trial.Root);
        Assert.Contains(errors, e => e.Contains("fielders.center.zFt") && e.Contains("greater than 0"));

        // The signed half: the left side of the field is negative x and that is not an error.
        Assert.DoesNotContain(errors, e => e.Contains("fielders.left.xFt"));
        Assert.DoesNotContain(errors, e => e.Contains("fielders.third.xFt"));
    }

    /// <summary>A table that puts left field on the right draws a field folded in half.</summary>
    [Fact]
    public void TheCornersOfTheOutfieldMustStayOnTheirOwnSides()
    {
        using var trial = new CopiedRoot();
        trial.Change("fielders.json", json => json["left"]!["xFt"] = 120);

        Assert.Contains(RulesTable.Validate(trial.Root), e => e.Contains("fielders.left.xFt"));
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

    /// <summary>
    /// The same point for the dress (#729): another data root draws another infield. Without this
    /// the compact profile would play an 80-ft diamond on 90-ft dirt.
    /// </summary>
    [Fact]
    public void AnotherDataRootPlaysAnotherDress()
    {
        using var trial = new CopiedRoot();
        trial.Change("infield.json", json =>
        {
            json["innerHalfFt"] = 44.44;
            json["backArcFt"] = 81.78;
        });

        var infield = RulesTable.Load(trial.Root).Infield;

        Assert.Equal(44.44, infield.InnerHalfFt);
        Assert.Equal(81.78, infield.BackArcFt);

        // The shipped table is untouched, and the running process still draws it.
        Assert.Equal(50f, ParkDiamond.InnerHalf);
        Assert.Equal(92f, ParkDiamond.BackR);
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
    /// The drawn grass stays inside the bag it points at and inside the arc behind it (#729). A
    /// dress that swallows the bags is a lake, not a diamond, and it is named by path rather than
    /// discovered in a screenshot.
    /// </summary>
    [Fact]
    public void TheGrassDiamondMustStayInsideTheBagsAndTheArc()
    {
        using var trial = new CopiedRoot();
        trial.Change("infield.json", json => json["innerHalfFt"] = 200);

        var errors = RulesTable.Validate(trial.Root);
        Assert.Contains(errors, e => e.Contains("infield.innerHalfFt"));
        Assert.Equal(2, errors.Count(e => e.Contains("infield.innerHalfFt")));
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
        var offenders = Offenders(literal);
        Assert.True(offenders.Count == 0,
            $"{literal} must come from data/rules/infield.json: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// The same guard for the starts (#725), and it can only hold one of the seven. The rule the
    /// test above applies is a substring scan, and six of the migrated numbers are plain two- and
    /// three-digit integers that the sim spells for unrelated reasons: on this branch "78" still
    /// appears on 24 lines, "72" on 34 and "42" on 28 — colours, glove scales and one irrational
    /// constant — and "118", "110" and "250" on four or five each (<c>Palette.Sky</c> is
    /// <c>C(118, 186, 232)</c>). Adding them would fail on lines that have nothing to do with a
    /// fielder.
    ///
    /// <para>
    /// Centre field's depth is the exception: it appears nowhere else in the sim, so this one
    /// permanently refuses a re-introduced copy of it. The other six are held by
    /// <see cref="TheShippedStartsAreTheOnesThatWereMigrated"/> and by the byte-diff of a
    /// <c>cli match</c> run, which is the gate the PR actually leans on.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData("305")]
    public void NoBareFielderStartLiteralIsLeftInTheSource(string literal)
    {
        var offenders = Offenders(literal);
        Assert.True(offenders.Count == 0,
            $"{literal} must come from data/rules/fielders.json: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// Every <c>.cs</c> file outside the tests that spells <paramref name="literal"/> in code.
    /// <c>Rules.cs</c> is the one file allowed to: that is where the tables declare their numbers,
    /// and the initializers are the documented load fallback, not a second copy.
    /// </summary>
    IReadOnlyList<string> Offenders(string literal)
    {
        var src = Path.GetFullPath(Path.Combine(_content.Root.Shipped, "..", "src"));
        return Directory
            .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".Tests") && Path.GetFileName(f) != "Rules.cs")
            .Where(f => File.ReadLines(f).Any(Spells))
            .Select(f => Path.GetRelativePath(src, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

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
