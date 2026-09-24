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
    /// The shipped infield, spelled out: 80-ft basepaths, the rubber at 53.78 ft, the corners and
    /// second on that square. If a later change to the table moves a bag, this is the test that
    /// says so out loud instead of letting six hundred routes drift quietly.
    /// </summary>
    [Fact]
    public void TheShippedInfieldIsTheEightyFootSquare()
    {
        var infield = RulesTable.Load(_content.Root).Infield;

        Assert.Equal(80, infield.BaselineFt);
        Assert.Equal(53.78, infield.MoundFt);
        Assert.Equal(56.57, infield.CornerFt);
        Assert.Equal(113.14, infield.SecondFt);

        // The dress, on the same square.
        Assert.Equal(44.44, infield.InnerHalfFt);
        Assert.Equal(81.78, infield.BackArcFt);
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
        // deliberately does not refuse a table over: 0.13 ft on the 80-ft square.
        Assert.Equal(0.13, infield.CornerFt - (infield.InnerHalfFt + ParkDiamond.BagPadR), 2);
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
    /// The shipped starts, spelled out. The left side is read through <see cref="Diamond.Positions"/>
    /// — the path the whole sim uses — and the right side is typed, so a digit mistyped in the JSON
    /// fails here.
    /// </summary>
    [Fact]
    public void TheShippedStartsAreSpelledOut()
    {
        Assert.Equal((69.33d, 64d), Diamond.Positions["1B"]);
        Assert.Equal((37.33d, 104.89d), Diamond.Positions["2B"]);
        Assert.Equal((-69.33d, 64d), Diamond.Positions["3B"]);
        Assert.Equal((-37.33d, 104.89d), Diamond.Positions["SS"]);
        Assert.Equal((-77.09d, 175.19d), Diamond.Positions["LF"]);
        Assert.Equal((0d, 213.5d), Diamond.Positions["CF"]);
        Assert.Equal((77.09d, 175.19d), Diamond.Positions["RF"]);

        // The two that are not in fielders.json: the rubber and the catcher's set.
        Assert.Equal((0d, 53.78d), Diamond.Positions["P"]);
        Assert.Equal((0d, -15d), Diamond.Positions["C"]);
    }

    /// <summary>
    /// Another data root stands another defence: the seven starts are table values, not C#
    /// literals, so a root with a deeper outfield (here the 90-ft field's) stands its fielders there.
    /// </summary>
    [Fact]
    public void AnotherDataRootStandsAnotherDefence()
    {
        using var other = new CopiedRoot();
        other.Change("fielders.json", json =>
        {
            json["center"]!["zFt"] = 305;
            json["left"]!["xFt"] = -110;
            json["left"]!["zFt"] = 250;
        });

        var fielders = RulesTable.Load(other.Root).Fielders;

        Assert.Equal((0d, 305d), fielders.Spot("CF"));
        Assert.Equal((-110d, 250d), fielders.Spot("LF"));

        // The shipped table is untouched, and the running process still stands it.
        Assert.Equal((0d, 213.5d), Diamond.Positions["CF"]);
    }

    /// <summary>
    /// Left field is on the left and right field is on the right, each named on its own. Ordering
    /// the two against each other is not enough: <c>left.xFt == right.xFt == 0</c> satisfies
    /// <c>left &lt;= right</c> and <c>[Signed]</c> permits zero, and that is the one table that
    /// breaks something — <see cref="ChemistryToy.MiniSpot"/> divides by the right fielder's x,
    /// which was the constant 110 until #725 made it a table value.
    /// </summary>
    [Fact]
    public void AnOutfieldFoldedOntoOneSideIsNamedByPath()
    {
        using var flat = new CopiedRoot();
        flat.Change("fielders.json", json =>
        {
            json["left"]!["xFt"] = 0;
            json["right"]!["xFt"] = 0;
        });

        var errors = RulesTable.Validate(flat.Root);
        Assert.Contains(errors, e => e.Contains("fielders.left.xFt"));
        Assert.Contains(errors, e => e.Contains("fielders.right.xFt"));

        // Swapped sides are caught too, by all three rules at once.
        using var swapped = new CopiedRoot();
        swapped.Change("fielders.json", json =>
        {
            json["left"]!["xFt"] = 110;
            json["right"]!["xFt"] = -110;
        });

        var swappedErrors = RulesTable.Validate(swapped.Root);
        Assert.Contains(swappedErrors, e => e.Contains("fielders.left.xFt"));
        Assert.Contains(swappedErrors, e => e.Contains("fielders.right.xFt"));

        // The shipped table passes, which is what makes the guard usable.
        Assert.DoesNotContain(RulesTable.Validate(_content.Root), e => e.Contains("fielders."));
    }

    /// <summary>
    /// A start is a point, so x may be negative and z may not: a fielder behind home is not a
    /// defence, it is a typo. Both halves are named by path rather than found in a trace.
    /// </summary>
    [Fact]
    public void AFielderStartOutOfRangeIsNamedByPath()
    {
        using var copy = new CopiedRoot();
        copy.Change("fielders.json", json => json["center"]!["zFt"] = -5);

        var errors = RulesTable.Validate(copy.Root);
        Assert.Contains(errors, e => e.Contains("fielders.center.zFt") && e.Contains("greater than 0"));

        // The signed half: the left side of the field is negative x and that is not an error.
        Assert.DoesNotContain(errors, e => e.Contains("fielders.left.xFt"));
        Assert.DoesNotContain(errors, e => e.Contains("fielders.third.xFt"));
    }

    /// <summary>A table that puts left field on the right draws a field folded in half.</summary>
    [Fact]
    public void TheCornersOfTheOutfieldMustStayOnTheirOwnSides()
    {
        using var copy = new CopiedRoot();
        copy.Change("fielders.json", json => json["left"]!["xFt"] = 120);

        Assert.Contains(RulesTable.Validate(copy.Root), e => e.Contains("fielders.left.xFt"));
    }

    /// <summary>
    /// Another data root plays another infield (here the 90-ft square), and it never edits the
    /// shipped table to do it.
    /// </summary>
    [Fact]
    public void AnotherDataRootPlaysAnotherInfield()
    {
        using var other = new CopiedRoot();
        other.Change("infield.json", json =>
        {
            json["baselineFt"] = 90;
            json["cornerFt"] = 63.64;
            json["secondFt"] = 127.28;
        });

        var infield = RulesTable.Load(other.Root).Infield;

        Assert.Equal(90, infield.BaselineFt);
        Assert.Equal((63.64, 63.64), (infield.CornerFt, infield.CornerFt));
        Assert.Equal(127.28, infield.SecondFt);
        Assert.Equal(53.78, infield.MoundFt);

        // The shipped table is untouched, and the running process still plays it.
        Assert.Equal(80, Diamond.Baseline);
    }

    /// <summary>
    /// The same point for the dress (#729): another data root draws another infield. Without this
    /// a root that moves the bags would play its diamond on the shipped dirt.
    /// </summary>
    [Fact]
    public void AnotherDataRootPlaysAnotherDress()
    {
        using var other = new CopiedRoot();
        other.Change("infield.json", json =>
        {
            json["innerHalfFt"] = 50;
            json["backArcFt"] = 92;
        });

        var infield = RulesTable.Load(other.Root).Infield;

        Assert.Equal(50, infield.InnerHalfFt);
        Assert.Equal(92, infield.BackArcFt);

        // The shipped table is untouched, and the running process still draws it.
        Assert.Equal(44.44f, ParkDiamond.InnerHalf);
        Assert.Equal(81.78f, ParkDiamond.BackR);
    }

    /// <summary>A data root is named to a whole process, so two roots are two runs to diff.</summary>
    [Fact]
    public void TheNamedRootIsTakenWhenItIsADataRoot()
    {
        using var copy = new CopiedRoot();
        Assert.Equal(copy.Root, ContentCatalog.NamedDataRoot(copy.Root));
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
    /// run the shipped root while the operator believed they were running another one.
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
        using var copy = new CopiedRoot();
        copy.Change("infield.json", json => json["baselineFt"] = 0);

        Assert.Contains(
            RulesTable.Validate(copy.Root),
            e => e.Contains("infield.baselineFt") && e.Contains("greater than 0"));
    }

    /// <summary>Second sits past the corners and past the rubber; a table that says otherwise is not a diamond.</summary>
    [Fact]
    public void SecondMustSitBeyondTheRubberAndTheCorners()
    {
        using var copy = new CopiedRoot();
        copy.Change("infield.json", json => json["secondFt"] = 10);

        var errors = RulesTable.Validate(copy.Root);
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
        using var copy = new CopiedRoot();
        copy.Change("infield.json", json => json["innerHalfFt"] = 200);

        var errors = RulesTable.Validate(copy.Root);
        Assert.Contains(errors, e => e.Contains("infield.innerHalfFt"));
        Assert.Equal(2, errors.Count(e => e.Contains("infield.innerHalfFt")));
    }

    /// <summary>
    /// The geometry must not survive as a bare literal anywhere in the sim, or another root would
    /// give a correct sim and a diamond drawn in the old place — #711 found the bags in the play
    /// view and the mound in the pitch reset, none of which would have moved on their own.
    ///
    /// <c>Rules.cs</c> is the one file allowed to spell the numbers: that is where
    /// <see cref="InfieldRules"/> declares them, and its initializers are the documented load
    /// fallback for a field the JSON does not name, not a second copy of the table.
    /// </summary>
    [Theory]
    [InlineData("56.57")]
    [InlineData("113.14")]
    [InlineData("53.78")]
    public void NoBareInfieldLiteralIsLeftInTheSource(string literal)
    {
        var offenders = Offenders(literal);
        Assert.True(offenders.Count == 0,
            $"{literal} must come from data/rules/infield.json: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// The same guard for the starts (#725), held on centre field's depth, which appears nowhere
    /// else in the sim. The rule is a substring scan, and short numbers such as "64" are spelled by
    /// the sim for unrelated reasons (colours, glove scales), so the other six are held by
    /// <see cref="TheShippedStartsAreSpelledOut"/> instead.
    /// </summary>
    [Theory]
    [InlineData("213.5")]
    public void NoBareFielderStartLiteralIsLeftInTheSource(string literal)
    {
        var offenders = Offenders(literal);
        Assert.True(offenders.Count == 0,
            $"{literal} must come from data/rules/fielders.json: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// Every <c>.cs</c> file outside the tests that spells <paramref name="literal"/> in code.
    /// <c>Rules.cs</c> is the one file allowed to: its comments explain the numbers the JSON holds.
    /// The tables themselves carry no numbers (<c>RulesTests.TheTablesCarryNoCodeDefaults</c>).
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
            if (code.StartsWith("//", StringComparison.Ordinal) || code.StartsWith('*')) return false;
            return code.Contains(literal);
        }
    }

    /// <summary>
    /// A whole copy of the shipped data root, free to be edited. The shipped one never is. An overlay
    /// carries only its own files instead (#716, <see cref="TrialOverlayTests"/>); this fixture is a
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
