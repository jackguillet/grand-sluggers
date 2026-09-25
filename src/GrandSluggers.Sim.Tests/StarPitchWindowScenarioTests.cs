using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Star Pitches keep the ordinary window, and no star pitch turns contact into a miss (spec §5.3,
/// §13; Appendix B.1 rows S-190 … S-195).
///
/// <b>PH-16-R1</b> (Jack, September 21, 2026): a Star Pitch's challenge is readable ball behaviour —
/// speed and path — never a narrower timing window. <b>PH-16-R18</b>: Charmball, Skullball and
/// Fogball lose <c>batterWindowMul</c>; until each is reviewed they keep their speed change and, for
/// Charmball, its wobble. <b>PH-16-R19</b>: the Phonyball roll that turned non-Perfect contact into
/// a miss is gone; its decoy path is the whole effect.
///
/// No row stores a window or a count: every claim is read from the table it asserts about, or is
/// an identity between a star pitch and the same pitch thrown plain.
/// </summary>
public sealed class StarPitchWindowScenarioTests
{
    readonly ContentCatalog _content = Shipped.Content;

    Park Harbor => _content.Parks[ExhibitionPick.DefaultPark];

    // ---------------------------------------------------------------------------------
    // S-190  Every star pitch in the catalog is judged in the ordinary window
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S190_EveryStarPitchInTheCatalogKeepsTheOrdinaryWindowOnEveryRung()
    {
        var skills = _content.StarSkills;
        Assert.NotEmpty(skills.Pitches);
        foreach (var level in CpuRules.Levels)
        {
            var rung = _content.Rules.AtLevel(level);
            var plain = AtBatResolver.ContactWindowFrames(null, Harbor, false, rung, skills);
            Assert.Equal(rung.Batting.Window.Frames, plain);
            foreach (var star in skills.Pitches.Keys)
            {
                Assert.Equal(plain, AtBatResolver.ContactWindowFrames(star, Harbor, false, rung, skills));
                foreach (var park in _content.Parks.Values)
                foreach (var night in new[] { false, true })
                    Assert.Equal(plain, AtBatResolver.ContactWindowFrames(star, park, night, rung, skills));
            }
        }
    }

    [Fact]
    public void S190_TheResolverMeetsEveryStarPitchJustInsideTheWindowAndMissesItJustOutside()
    {
        // The swing that just meets the plain pitch meets the star pitch, and the swing that just
        // misses the plain pitch misses the star pitch: the edge is the same frame for every star.
        var resolver = new AtBatResolver(_content.Chemistry, _content.Rules, _content.StarSkills);
        var frames = _content.Rules.Batting.Window.Frames;
        foreach (var star in _content.StarSkills.Pitches.Keys)
        foreach (var err in new[] { -(frames / 2 - 0.1), frames / 2 - 0.1 })
        {
            var inside = Input(star, err, useStar: true);
            Assert.NotEqual(ContactQuality.Miss, resolver.Resolve(inside, Harbor, new Random(1)).Quality);
            Assert.Equal(ContactQuality.Miss,
                resolver.Resolve(inside with { TimingErrorFrames = err + Math.Sign(err) * 0.2 }, Harbor, new Random(1)).Quality);
        }
    }

    // ---------------------------------------------------------------------------------
    // S-191  Every captain's star pitch, from either dugout, reads the plain window in a match
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S191_EveryCaptainsStarPitchReadsThePlainWindowInAMatchFromEitherDugout()
    {
        var captains = _content.Characters.Values.Where(c => c.Captain).Select(c => c.Id).ToList();
        Assert.NotEmpty(captains);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var captain in captains)
        foreach (var (home, away) in new[] { (captain, "rio"), ("rio", captain) })
        {
            if (home == away) continue;
            var match = Match.Exhibition(_content, home, away, innings: 3, seed: 1);
            var star = match.Pitcher.StarPitch;
            Assert.False(string.IsNullOrEmpty(star), match.Pitcher.Id);
            seen.Add(match.Pitcher.Id);
            var plain = match.SwingWindowFrames(new PitchCommand(PitchFamily.Fastball, 0, false));
            Assert.Equal(_content.Rules.Batting.Window.Frames, plain);
            Assert.Equal(plain, match.SwingWindowFrames(new PitchCommand(PitchFamily.Fastball, 0, true)));
            Assert.Equal(plain, match.SwingWindowFrames(new PitchCommand(PitchFamily.Changeup, 1, true)));
        }
        foreach (var captain in captains)
            Assert.Contains(captain, seen);
    }

    // ---------------------------------------------------------------------------------
    // S-192  The window multiplier is not a key of the star skills file
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S192_TheRetiredWindowMultiplierIsRefusedByNameAndTheShippedFileAuthorsNone()
    {
        var shipped = JsonNode.Parse(File.ReadAllText(Path.Combine(_content.Root.Shipped, "abilities", "star-skills.json")))!;
        foreach (var (id, row) in shipped["pitches"]!.AsObject())
            Assert.False(row!.AsObject().ContainsKey("batterWindowMul"), id);

        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json => json["pitches"]!["charmball"]!["batterWindowMul"] = 0.75);
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains(fixture.Path("abilities/star-skills.json"), StringComparison.Ordinal)
                                     && e.Contains("pitches.charmball.batterWindowMul is not a key this file declares", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
    }

    [Fact]
    public void S192_AnyKeyTheStarSkillsDoNotDeclareIsRefusedOnPitchesAndSwings()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["fogball"]!["windowMul"] = 0.5;
            json["swings"]!["heat-swing"]!["contactMul"] = 2.0;
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("pitches.fogball.windowMul is not a key this file declares", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("swings.heat-swing.contactMul is not a key this file declares", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // S-193  A phonyball met by the bat is contact: the same result as the plain pitch
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S193_APhonyballMetByTheBatIsTheSameContactAsThePlainPitchOnEverySeed()
    {
        // The roll drew once per non-Perfect contact and turned 40 % of them into a miss. Now the
        // phonyball draws nothing at the plate, so the same swing on the same seed resolves exactly
        // as the plain pitch does: same quality, same exit, same launch, same spray.
        var resolver = new AtBatResolver(_content.Chemistry, _content.Rules, _content.StarSkills);
        var frames = _content.Rules.Batting.Window.Frames;
        var nonPerfect = 0;
        for (var seed = 0; seed < 40; seed++)
        foreach (var err in new[] { 0.0, frames / 2 - 0.1, -(frames / 2 - 0.1) })
        foreach (var crossingX in new[] { -1.1, -0.6, 0.0, 0.6, 1.1 })
        {
            var plain = Input("phonyball", err, useStar: false) with { CrossingX = crossingX };
            var star = plain with { UseStarPitch = true };
            var a = resolver.Resolve(plain, Harbor, new Random(seed));
            var b = resolver.Resolve(star, Harbor, new Random(seed));
            Assert.Equal("phonyball", b.StarPitchUsed);
            Assert.Equal(a, b with { StarPitchUsed = null });
            if (a.Quality is ContactQuality.Nice or ContactQuality.Sour) nonPerfect++;
        }
        Assert.True(nonPerfect > 0, "the grid reaches non-Perfect contact");
    }

    // ---------------------------------------------------------------------------------
    // S-194  The whiff number is not a rule of the batting table
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S194_TheRetiredWhiffChanceIsRefusedByName()
    {
        var file = Path.Combine(_content.Root.Shipped, "rules", "batting.json");
        Assert.False(Parse(file)["star"]!.AsObject().ContainsKey("phonyballWhiff"), file);

        using var fixture = new ContentFixture();
        var path = fixture.Path("rules/batting.json");
        var json = Parse(path);
        json["star"]!["phonyballWhiff"] = 0.4;
        File.WriteAllText(path, json.ToJsonString());
        Assert.Contains(RulesTable.Validate(new DataRoot(fixture.Root)),
            e => e.Contains("batting.star.phonyballWhiff is not a rule this table owns", StringComparison.Ordinal)
                 && e.Contains(fixture.Path("rules/batting.json"), StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // S-195  What each reworked star pitch still does: its speed and its path
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S195_TheReworkedStarPitchesKeepTheirSpeedAndPathAndNothingElse()
    {
        // PH-16-R18's interim: Charmball keeps its speed and its visible wobble; Skullball and Fogball
        // are a plain fast and a plain slow pitch. PH-16-R19: Phonyball keeps its decoy switch.
        var skills = _content.StarSkills;
        var rules = _content.Rules;
        foreach (var star in new[] { "charmball", "skullball", "fogball" })
            Assert.NotEqual(1.0, StarSkills.PitchSpeedMul(star, skills));
        Assert.True(StarSkills.PitchSpeedMul("skullball", skills) > 1, "skullball is fast");
        Assert.True(StarSkills.PitchSpeedMul("fogball", skills) < 1, "fogball is slow");

        var pitch = new PitchCommand(PitchFamily.Fastball, 0, true);
        var charmMoves = false;
        var phonyMoves = false;
        for (var i = 0; i <= 20; i++)
        {
            var u = i / 20.0;
            var plain = PitchFlight.Point(pitch, u, rules, null);
            Assert.Equal(plain, PitchFlight.Point(pitch, u, rules, "skullball"));
            Assert.Equal(plain, PitchFlight.Point(pitch, u, rules, "fogball"));
            charmMoves |= PitchFlight.Point(pitch, u, rules, "charmball") != plain;
            phonyMoves |= PitchFlight.Point(pitch, u, rules, "phonyball") != plain;
        }
        Assert.True(charmMoves, "the charmball keeps its wobble");
        Assert.True(phonyMoves, "the phonyball keeps its decoy path");
        var early = PitchFlight.Point(pitch, rules.Pitching.StarShapes.PhonyballSwitchAt - 0.01, rules, "phonyball").X;
        var late = PitchFlight.Point(pitch, rules.Pitching.StarShapes.PhonyballSwitchAt + 0.01, rules, "phonyball").X;
        Assert.True(Math.Abs(late - early) > 1, $"the decoy switches sides late: {early} → {late}");
    }

    // ---- helpers ---------------------------------------------------------------------

    static readonly System.Text.Json.JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>A rules file carries comments; read it the way the loader does.</summary>
    static JsonObject Parse(string path) =>
        JsonNode.Parse(File.ReadAllText(path), null, JsonComments)!.AsObject();

    /// <summary>
    /// Vale throwing <paramref name="star"/> at a Pitch-5 arm (pitch factor × 1) to Rio, crossing the
    /// middle of the zone. The star id is set on the arm so every star in the catalog is thrown by
    /// the same body.
    /// </summary>
    AtBatInput Input(string star, double err, bool useStar)
    {
        var arm = _content.Must("vale");
        arm = arm with { StarPitch = star, Stats = arm.Stats.WithPitch(5) };
        return new AtBatInput(
            arm, _content.Must("rio"), null, [],
            ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: err,
            UseStarPitch: useStar, UseStarSwing: false, Bat: _content.Bats["harbor-lumber"], PitcherStamina: 80,
            CrossingX: 0);
    }
}
