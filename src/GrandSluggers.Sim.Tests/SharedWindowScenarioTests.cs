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
/// <c>window.frames</c> for every hitter, both swings and every rung, with the star multiplier, the
/// park multiplier and the floor still applying in the same order (<b>S-125</b>). <b>S-126</b> holds
/// the retired overlay, the removed switch keys and the validator's floor.
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

    /// <summary>The one park that authors a night window multiplier (§14, <c>nightContactWindowMul</c>).</summary>
    Park CrystalRink => _shipped.Parks["crystal-rink"];

    /// <summary>Every star pitch id, plus "no star": the multiplier is a table read, never a literal.</summary>
    static readonly string?[] Stars = [null, "charmball", "skullball", "fogball", "heatball"];

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
    public void S125_TheStarAndTheParkStillMultiplyTheOneWindowAndTheFloorStillHolds()
    {
        var trial = ShippedRules;
        var w = trial.Batting.Window;
        var charm = StarSkills.BatterWindowMul("charmball", Skills);
        var rink = CrystalRink.NightContactWindowMul;
        Assert.True(charm < 1 && rink < 1, "both multipliers narrow");

        // The multiplier is a table read: three star pitches narrow the window and the rest do not.
        Assert.Equal(1.0, StarSkills.BatterWindowMul("heatball", Skills));
        Assert.Equal(1.0, StarSkills.BatterWindowMul(null, Skills));
        foreach (var star in new[] { "charmball", "skullball", "fogball" })
            Assert.True(StarSkills.BatterWindowMul(star, Skills) < 1, star);

        // Same order as the formula: frames × star × park, then the floor.
        Assert.Equal(Math.Max(w.FloorFrames, w.Frames * charm),
            AtBatResolver.ContactWindowFrames("charmball", Harbor, false, trial, Skills));
        Assert.Equal(Math.Max(w.FloorFrames, w.Frames * rink),
            AtBatResolver.ContactWindowFrames(null, CrystalRink, true, trial, Skills));
        Assert.Equal(Math.Max(w.FloorFrames, w.Frames * charm * rink),
            AtBatResolver.ContactWindowFrames("charmball", CrystalRink, true, trial, Skills));

        // A day game at the rink is the day multiplier, which is 1: night is a park rule, not a park id.
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, Harbor, false, trial, Skills),
            AtBatResolver.ContactWindowFrames(null, CrystalRink, false, trial, Skills));

        // Reported, not asserted as a target: on 9 frames the floor never bites, even at the worst
        // pair of multipliers in the catalog. The floor is still the last step, which the fixture
        // table below shows by putting the window on it.
        var worst = Stars.Where(s => s is not null).Min(s => StarSkills.BatterWindowMul(s, Skills));
        Assert.True(w.Frames * worst * rink > w.FloorFrames,
            $"the window never reaches the floor: {w.Frames} x {worst} x {rink} vs {w.FloorFrames}");

        var onTheFloor = new RulesTable
        {
            Batting = new BattingRules
            {
                Window = new ContactWindowRules { Frames = w.FloorFrames, FloorFrames = w.FloorFrames }
            }
        };
        Assert.Equal(w.FloorFrames,
            AtBatResolver.ContactWindowFrames("charmball", CrystalRink, true, onTheFloor, Skills));
    }

    [Fact]
    public void S125_TheSpatialHalfOfThePlateStillReadsContactAndTheCharge()
    {
        var trial = ShippedRules;
        // PH-11-R1: a charge trades placement forgiveness, never timing. PH-15-R7: Contact is
        // spatial forgiveness. Both live in the cursor.
        foreach (var contact in Enumerable.Range(1, 10))
        {
            Assert.True(SweetSpot.BarrelScale(contact, true, false, 0, trial)
                        < SweetSpot.BarrelScale(contact, false, false, 0, trial),
                $"a charge still narrows the barrel at Contact {contact}");
            if (contact < 10)
                Assert.True(SweetSpot.BarrelScale(contact + 1, false, false, 0, trial)
                            > SweetSpot.BarrelScale(contact, false, false, 0, trial),
                    $"Contact {contact + 1} still carries a wider barrel than {contact}");
        }

        // Two hitters with the same Bat are separated by barrel: Contact is spatial forgiveness.
        var sure = Hitter(contact: 9, power: 2);
        var slugger = Hitter(contact: 2, power: 9);
        Assert.Equal(sure.Stats.Bat, slugger.Stats.Bat);
        Assert.True(SweetSpot.BarrelScale(sure.Stats.Contact, false, false, 0, trial)
                    > SweetSpot.BarrelScale(slugger.Stats.Contact, false, false, 0, trial));
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
