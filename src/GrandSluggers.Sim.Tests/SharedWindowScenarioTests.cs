using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The one shared timing window (#844, PH-10-R1), Appendix B.1 rows S-125 and S-126.
///
/// Jack played the <c>trials/pitch5</c> window and accepted it on September 22, 2026 ("trial was
/// good."); #860 shipped it and #887 removed the split window it replaced. The window is
/// <c>window.frames</c> for every hitter, both swings, every rung and every star pitch, floored
/// (<b>S-125</b>; the star multiplier went with PH-16-R18 and the park multiplier with FD-11-R2). <b>S-126</b> holds
/// the retired overlay, the removed switch keys and the validator's floor. <b>S-127</b> holds a
/// catalog that is not the process's to its own table on the auto-play path.
///
/// Every root is loaded <b>in process</b>, through a <see cref="DataRoot"/> this class builds from
/// the repository, so nothing here depends on <c>GRAND_SLUGGERS_TRIAL</c> being set and CI is untouched.
///
/// <b>No row stores a window.</b> Every claim is an arithmetic identity recomputed here from the same table, a
/// relationship, or an integer count. The formula is multiplies and a <c>Math.Max</c> — no libm — so
/// two in-process computations of it are equal bit for bit and may be compared exactly (#736).
/// </summary>
public sealed class SharedWindowScenarioTests
{
    static readonly JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    readonly ContentCatalog _shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

    string ShippedRoot => _shipped.Root.Shipped;
    string Repo => Path.GetFullPath(Path.Combine(ShippedRoot, ".."));

    RulesTable ShippedRules => _shipped.Rules;
    StarSkillTable Skills => _shipped.StarSkills;

    Park Harbor => _shipped.Parks[ExhibitionPick.DefaultPark];

    /// <summary>The park that authored the night window multiplier until FD-11-R2 dropped it (F4-d, #895).</summary>
    Park CrystalRink => _shipped.Parks["crystal-rink"];

    // ---------------------------------------------------------------------------------
    // S-125  One window for every hitter, every swing, every bat and every rung
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S125_EveryRungGetsTheOneWindow()
    {
        // #860 ("trial was good.", September 22, 2026). The window reads no hitter, no bat and no
        // charge: ContactWindowFrames takes none of them (#887). What is left to hold is the rung.
        var w = ShippedRules.Batting.Window;
        foreach (var level in CpuRules.Levels)
        {
            var rung = ShippedRules.AtLevel(level);
            Assert.Equal(w.Frames, AtBatResolver.ContactWindowFrames(null, Harbor, false, rung, Skills));
            Assert.Equal(w.Frames, rung.Batting.Window.Frames);
        }
    }

    [Fact]
    public void S125_NoStarAndNoParkMultipliesTheOneWindowAndTheFloorStillHolds()
    {
        // Re-authored to FD-11-R2 (F4-d, #895): the park's night multiplier is gone on both roots, so
        // night at the rink is the day window. Re-authored to PH-16-R18: the star multiplier is gone
        // too, so a charmball at the rink at night is the plain window (S-190 walks every star pitch).
        var rules = ShippedRules;
        var w = rules.Batting.Window;
        Assert.Equal(w.Frames, AtBatResolver.ContactWindowFrames("charmball", Harbor, false, rules, Skills));
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, Harbor, false, rules, Skills),
            AtBatResolver.ContactWindowFrames("charmball", CrystalRink, true, rules, Skills));
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, Harbor, false, rules, Skills),
            AtBatResolver.ContactWindowFrames(null, CrystalRink, true, rules, Skills));

        // Reported, not asserted as a target: on 9 frames the floor never bites. The floor is still
        // the last step, which the fixture table below shows by putting the window on it.
        Assert.True(w.Frames > w.FloorFrames, $"the window is over the floor: {w.Frames} vs {w.FloorFrames}");

        var onTheFloor = rules with { Batting = rules.Batting with { Window = w with { Frames = w.FloorFrames } } };
        Assert.Equal(w.FloorFrames,
            AtBatResolver.ContactWindowFrames("charmball", CrystalRink, true, onTheFloor, Skills));
    }

    [Fact]
    public void S125_TheSpatialHalfOfThePlateStillReadsContactAndTheCharge()
    {
        var rules = ShippedRules;
        // PH-11-R1: a charge trades placement forgiveness, never timing. PH-15-R7: Contact is
        // spatial forgiveness. Both live in the cursor.
        foreach (var contact in Enumerable.Range(1, 10))
        {
            Assert.True(SweetSpot.BarrelScale(contact, true, false, rules)
                        < SweetSpot.BarrelScale(contact, false, false, rules),
                $"a charge still narrows the barrel at Contact {contact}");
            if (contact < 10)
                Assert.True(SweetSpot.BarrelScale(contact + 1, false, false, rules)
                            > SweetSpot.BarrelScale(contact, false, false, rules),
                    $"Contact {contact + 1} still carries a wider barrel than {contact}");
        }

        // Two hitters with the same Bat are separated by barrel: Contact is spatial forgiveness.
        var sure = Hitter(contact: 9, power: 2);
        var slugger = Hitter(contact: 2, power: 9);
        Assert.Equal(sure.Stats.Bat, slugger.Stats.Bat);
        Assert.True(SweetSpot.BarrelScale(sure.Stats.Contact, false, false, rules)
                    > SweetSpot.BarrelScale(slugger.Stats.Contact, false, false, rules));
    }

    // ---------------------------------------------------------------------------------
    // S-126  The overlay and the switches are retired, and the validator still floors the window
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Jack accepted the window ("trial was good.") and the stick ("approve all") in the
    /// <c>trials/pitch5</c> window on September 22, 2026. #860 and #883 turned both switches on in
    /// <c>data/</c>, #883 deleted the folder, and #887 removed the switches and their off paths. So
    /// the overlay is gone, and a table that still authors a removed key is refused by name rather
    /// than silently read as the rule that ships.
    /// </summary>
    [Fact]
    public void S126_TheOverlayAndTheSwitchesAreRetiredAndARemovedKeyIsRefused()
    {
        Assert.False(Directory.Exists(Path.Combine(Repo, "trials", "pitch5")), "#883 retired trials/pitch5");

        var shippedJson = Parse(Path.Combine(ShippedRoot, "rules", "batting.json"));
        Assert.False(shippedJson.ContainsKey("geometryOnly"), "#887 removed the stick switch");
        var window = shippedJson["window"]!.AsObject();
        foreach (var key in new[] { "shared", "slapFrames", "chargeFrames", "framesPerContact" })
            Assert.False(window.ContainsKey(key), $"#887 removed window.{key}");

        foreach (var (key, edit) in new (string, Action<JsonObject>)[]
                 {
                     ("batting.geometryOnly", json => json["geometryOnly"] = false),
                     ("batting.window.shared", json => json["window"]!["shared"] = false),
                     ("batting.window.slapFrames", json => json["window"]!["slapFrames"] = 9.0),
                     ("batting.window.chargeFrames", json => json["window"]!["chargeFrames"] = 7.0),
                     ("batting.window.framesPerContact", json => json["window"]!["framesPerContact"] = 0.4)
                 })
        {
            using var fixture = new RulesFixture();
            fixture.Change("batting.json", edit);
            Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)),
                e => e.Contains($"{key} is not a rule this table owns", StringComparison.Ordinal)
                     && e.Contains(fixture.Path("batting.json"), StringComparison.Ordinal));
        }
    }

    [Fact]
    public void S126_AWindowUnderItsOwnFloorIsRefusedByName()
    {
        // A window the floor would have to rescue on every swing is an authoring mistake.
        using var fixture = new RulesFixture();
        fixture.Change("batting.json", json => json["window"]!["frames"] = 4.0);

        var errors = RulesTable.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains("batting.window.floorFrames must not exceed its upper bound; got 5 > 4", StringComparison.Ordinal)
                                     && e.Contains(fixture.Path("batting.json"), StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // S-127  A catalog that is not the process's reads its own table on the auto-play path
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// #844 found <see cref="Match.AutoPlay"/>'s in-zone read — <see cref="AtBatResolver.PitchInZone"/> →
    /// <see cref="StrikeZoneGeometry.Contains(PitchCommand, string?, RulesTable?)"/> →
    /// <see cref="PitchFlight.Point(PitchCommand, double, string?, System.ValueTuple{double, double, double}?, RulesTable?)"/>
    /// — resolving the pitch family against the <em>process-wide</em> table rather than the match's, and
    /// #855 threaded the match's table through every hop (debug protocol
    /// <c>trial-cohort-reads-the-process-wide-table</c>). Until #887 this row held it with the S-29
    /// cohort on the stick switch's off path; with the switches gone, it builds a catalog whose curveball
    /// drops far further than the process's, so the two tables disagree about the same pitch, and asks
    /// each hop which table it read. Then it plays a whole game from that catalog in process.
    /// </summary>
    [Fact]
    public void S127_ACatalogNotTheProcesssReadsItsOwnTableOnTheAutoPlayPath()
    {
        using var fixture = new RulesFixture();
        fixture.Change("pitching.json", json => json["families"]!["curveball"]!["dropFt"] = 3.0);
        var content = ContentCatalog.Load(new DataRoot(fixture.Root));
        Assert.NotEqual(Rules.Default.Pitching.Families.Of(PitchFamily.Curveball).DropFt,
            content.Rules.Pitching.Families.Of(PitchFamily.Curveball).DropFt);

        // A curveball aimed at the middle: the catalog's drop takes it under the zone, the process's does not.
        var pitch = new PitchCommand(PitchFamily.Curveball, 0, false);
        var crossing = PitchFlight.Point(pitch, 1, rules: content.Rules);
        var inZone = StrikeZoneGeometry.Contains(crossing.X, crossing.Y);
        Assert.Equal(inZone, StrikeZoneGeometry.Contains(pitch, content.Rules, null));
        Assert.Equal(inZone, AtBatResolver.PitchInZone(pitch, 5, content.Rules, null));
        Assert.NotEqual(inZone, AtBatResolver.PitchInZone(pitch, 5, rules: Rules.Default));

        // The whole auto-play path, in process, from the catalog.
        var match = Match.Exhibition(content, "rio", "ashlord", innings: 3, seed: 1);
        match.AutoPlayGame();
        Assert.True(match.Over);
    }

    // ---- helpers ---------------------------------------------------------------------

    /// <summary>
    /// A hitter authored here and nowhere else, the shape <see cref="ContactPowerScenarioTests"/>
    /// uses: the same <c>Bat</c> both times, so a read that still took the aggregate could not tell
    /// the two apart. No shipped roster file authors a trait (S-121).
    /// </summary>
    Character Hitter(int contact, int power)
    {
        var who = _shipped.Must("pip");
        return who with { Stats = new Stats(who.Stats.Pitch, 5, who.Stats.Field, 5) { Contact = contact, Power = power } };
    }

    static JsonObject Parse(string path) =>
        JsonNode.Parse(File.ReadAllText(path), null, JsonComments)!.AsObject();

    /// <summary>The shipped data root copied to a temp folder so one rules table can be rewritten.</summary>
    sealed class RulesFixture : IDisposable
    {
        public RulesFixture()
        {
            Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-window-" + Guid.NewGuid().ToString("N"));
            var source = ContentCatalog.Load().Root.Shipped;
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
