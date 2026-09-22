using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The one shared timing window (#844, PH-10-R1), Appendix B.1 rows S-124 … S-127.
///
/// <c>batting.window.shared</c> is the whole child. <b>On</b> — the shipped root since #860, after
/// Jack played the <c>trials/pitch5</c> window and accepted it on September 22, 2026 ("trial was
/// good.") — it is <c>window.frames</c> for every hitter, both swings and every rung, with the star
/// multiplier, the park multiplier and the floor still applying in the same order (<b>S-125</b>).
/// <b>Off</b> — the switch's off path, built in the test (<see cref="SwitchOffPaths"/>) and never
/// read from shipped data — the window is the one that shipped before #860: slap 9 / charge 7,
/// ± (Contact − 5) × 0.4, × the Star Pitch's multiplier × the park's night multiplier × the human
/// rung's, floored (<b>S-124</b>). <b>S-126</b> holds the shipped file to both accepted keys (#883
/// retired the <c>trials/pitch5</c> overlay that last carried #855's <c>geometryOnly</c>), and the
/// validator to the floor. <b>S-127</b> runs the S-29 cohort from a catalog built in the test (the
/// stick switch's off path), in process, and records it; it gates nothing.
///
/// Every root is loaded <b>in process</b>, through a <see cref="DataRoot"/> this class builds from
/// the repository or <see cref="SwitchOffPaths"/> builds from a copy, so nothing here depends on
/// <c>GRAND_SLUGGERS_TRIAL</c> being set and CI is untouched.
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

    double[] Rungs => [ShippedRules.Cpu.Easy.HumanWindowMul, ShippedRules.Cpu.Normal.HumanWindowMul, ShippedRules.Cpu.Hard.HumanWindowMul];

    // ---------------------------------------------------------------------------------
    // S-124  The off path: the switch off is the window that shipped before #860
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S124_TheShippedRootIsTodaysFormulaOverTheWholeGrid()
    {
        // #860: this row was the shipped root; it is now the switch's off path, built in the test.
        // The shipped root plays the shared window (S-125). The method name keeps the row's history.
        var off = SwitchOffPaths.SplitWindowRules;
        var w = off.Batting.Window;
        Assert.False(w.Shared, "the off path is the split window (#844, #860)");

        var rows = 0;
        var floored = 0;
        foreach (var contact in Enumerable.Range(1, 10))
        foreach (var charged in new[] { false, true })
        foreach (var star in Stars)
        foreach (var (park, night) in new[] { (Harbor, false), (Harbor, true), (CrystalRink, false), (CrystalRink, true) })
        foreach (var rung in Rungs)
        {
            // Today's formula, rebuilt here in the order the resolver applies it. Multiplies and a
            // max, so this is the same double, not a rounded one.
            var expected = (charged ? w.ChargeFrames : w.SlapFrames) + (contact - 5) * w.FramesPerContact;
            if (star is not null) expected *= StarSkills.BatterWindowMul(star, Skills);
            expected *= ParkHazards.ContactWindowMul(park, night, off);
            expected *= rung;
            var raw = expected;
            expected = Math.Max(w.FloorFrames, expected);
            if (expected > raw) floored++;

            Assert.Equal(expected, AtBatResolver.ContactWindowFrames(
                contact, charged, star, park, night, off, Skills, rung));
            rows++;
        }

        Assert.Equal(10 * 2 * Stars.Length * 4 * 3, rows);
        Assert.True(floored > 0, "the floor is still doing work somewhere in the grid");

        // The multiplier is a table read: three star pitches narrow the window and the rest do not.
        Assert.Equal(1.0, StarSkills.BatterWindowMul("heatball", Skills));
        Assert.Equal(1.0, StarSkills.BatterWindowMul(null, Skills));
        foreach (var star in new[] { "charmball", "skullball", "fogball" })
            Assert.True(StarSkills.BatterWindowMul(star, Skills) < 1, star);

        // Every term is still live on this root: each one moves the answer by itself.
        var plain = AtBatResolver.ContactWindowFrames(5, false, null, Harbor, false, off, Skills);
        Assert.True(AtBatResolver.ContactWindowFrames(5, true, null, Harbor, false, off, Skills) < plain,
            "a charge still narrows the shipped window");
        Assert.True(AtBatResolver.ContactWindowFrames(10, false, null, Harbor, false, off, Skills) > plain,
            "Contact still widens the shipped window");
        Assert.True(AtBatResolver.ContactWindowFrames(5, false, null, Harbor, false, off, Skills, Rungs[0]) > plain,
            "EASY still widens the shipped window");
        Assert.True(AtBatResolver.ContactWindowFrames(5, false, null, Harbor, false, off, Skills, Rungs[2]) < plain,
            "HARD still narrows the shipped window");
        Assert.True(AtBatResolver.ContactWindowFrames(5, false, "charmball", Harbor, false, off, Skills) < plain,
            "a star pitch still narrows the shipped window");
        Assert.True(AtBatResolver.ContactWindowFrames(5, false, null, CrystalRink, true, off, Skills) < plain,
            "the crystal rink at night still narrows the shipped window");
    }

    /// <summary>
    /// The window half of S-122 (#839), moved here. It asserted that the timing window reads
    /// <c>Stats.Contact</c> "until P2-b removes it"; that is now a statement about the shipped root
    /// only, so it belongs beside the rest of the shipped-root half. The σ, chase, charge, archetype
    /// and sac-bunt halves of S-122 stay in <see cref="ContactPowerScenarioTests"/> untouched.
    /// </summary>
    [Fact]
    public void S124_TheShippedWindowStillFollowsContact_MovedFromS122()
    {
        // #860: the off path, built in the test; on the shipped root the window no longer follows
        // Contact (S-125's last clause).
        var off = SwitchOffPaths.SplitWindowRules;
        var sure = Hitter(contact: 9, power: 2);
        var slugger = Hitter(contact: 2, power: 9);
        Assert.Equal(sure.Stats.Bat, slugger.Stats.Bat);

        Assert.True(
            AtBatResolver.SwingWindowFrames(sure, null, 0, null, Harbor, false, 1, off, Skills)
            > AtBatResolver.SwingWindowFrames(slugger, null, 0, null, Harbor, false, 1, off, Skills),
            "on the off path the window still widens with Contact, and with nothing else");
    }

    // ---------------------------------------------------------------------------------
    // S-125  Shipped since #860: one window for every hitter, every swing, every bat and every rung
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S125_UnderTheTrialEveryHitterAndSwingGetsTheOneWindow()
    {
        // #860 ("trial was good.", September 22, 2026): the shipped root is the accepted trial.
        var trial = ShippedRules;
        var w = trial.Batting.Window;
        Assert.True(w.Shared, "the shipped root plays the shared window (#860)");

        var roster = _shipped.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        Assert.Equal(25, roster.Count);
        var bats = new List<BatItem?> { null };
        bats.AddRange(_shipped.Bats.Values.OrderBy(b => b.Id, StringComparer.Ordinal));
        Assert.Equal(9, bats.Count);
        Assert.Contains(bats, b => b?.ChargeAlwaysFull == true);
        Assert.Contains(bats, b => b?.ContactMod > 0);
        Assert.Contains(bats, b => b?.ContactMod < 0);

        var rows = 0;
        foreach (var who in roster)
        foreach (var hand in new[] { Hand.L, Hand.R })
        foreach (var charge in new[] { 0.0, 1.0 })
        foreach (var bat in bats)
        foreach (var rung in Rungs)
        {
            var window = AtBatResolver.SwingWindowFrames(
                who with { Bats = hand }, bat, charge, null, Harbor, false, rung, trial, Skills);
            Assert.Equal(w.Frames, window);
            rows++;
        }
        Assert.Equal(25 * 2 * 2 * 9 * 3, rows);

        // The trait, the charge and the rung are gone from the window — not merely equal by accident
        // on this roster: the same call over a hand-built Contact 1 and Contact 10 is the same number.
        foreach (var contact in Enumerable.Range(1, 10))
        foreach (var charged in new[] { false, true })
        foreach (var rung in Rungs)
            Assert.Equal(w.Frames, AtBatResolver.ContactWindowFrames(contact, charged, null, Harbor, false, trial, Skills, rung));
    }

    [Fact]
    public void S125_TheStarAndTheParkStillMultiplyTheOneWindowAndTheFloorStillHolds()
    {
        var trial = ShippedRules;
        var w = trial.Batting.Window;
        var charm = StarSkills.BatterWindowMul("charmball", Skills);
        var rink = CrystalRink.NightContactWindowMul;
        Assert.True(charm < 1 && rink < 1, "both multipliers narrow");

        // Same order as the formula: frames × star × park, then the floor.
        Assert.Equal(Math.Max(w.FloorFrames, w.Frames * charm),
            AtBatResolver.ContactWindowFrames(5, false, "charmball", Harbor, false, trial, Skills));
        Assert.Equal(Math.Max(w.FloorFrames, w.Frames * rink),
            AtBatResolver.ContactWindowFrames(5, false, null, CrystalRink, true, trial, Skills));
        Assert.Equal(Math.Max(w.FloorFrames, w.Frames * charm * rink),
            AtBatResolver.ContactWindowFrames(5, false, "charmball", CrystalRink, true, trial, Skills));

        // A day game at the rink is the day multiplier, which is 1: night is a park rule, not a park id.
        Assert.Equal(AtBatResolver.ContactWindowFrames(5, false, null, Harbor, false, trial, Skills),
            AtBatResolver.ContactWindowFrames(5, false, null, CrystalRink, false, trial, Skills));

        // Reported, not asserted as a target: on 9 frames the floor never bites, even at the worst
        // pair of multipliers in the catalog. The floor is still the last step, which the fixture
        // table below shows by putting the window on it.
        var worst = Stars.Where(s => s is not null).Min(s => StarSkills.BatterWindowMul(s, Skills));
        Assert.True(w.Frames * worst * rink > w.FloorFrames,
            $"the trial window never reaches the floor: {w.Frames} x {worst} x {rink} vs {w.FloorFrames}");

        var onTheFloor = new RulesTable
        {
            Batting = new BattingRules
            {
                Window = new ContactWindowRules { Shared = true, Frames = w.FloorFrames, FloorFrames = w.FloorFrames }
            }
        };
        Assert.Equal(w.FloorFrames,
            AtBatResolver.ContactWindowFrames(5, false, "charmball", CrystalRink, true, onTheFloor, Skills));
    }

    [Fact]
    public void S125_TheSpatialHalfOfThePlateIsUntouchedByTheSwitch()
    {
        var trial = ShippedRules;
        var off = SwitchOffPaths.SplitWindowRules;
        // PH-11-R1: a charge trades placement forgiveness, never timing. PH-15-R7: Contact is
        // spatial forgiveness. Both live in the cursor, and the switch does not reach them.
        Assert.Equal(off.Batting.Cursor.ChargeMul, trial.Batting.Cursor.ChargeMul);
        Assert.Equal(off.Batting.Cursor.ScalePerContact, trial.Batting.Cursor.ScalePerContact);

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

        // And the same two hitters that S-124 separates by window on the shipped root are separated
        // by barrel — and by nothing else — under the trial.
        var sure = Hitter(contact: 9, power: 2);
        var slugger = Hitter(contact: 2, power: 9);
        Assert.Equal(
            AtBatResolver.SwingWindowFrames(sure, null, 0, null, Harbor, false, 1, trial, Skills),
            AtBatResolver.SwingWindowFrames(slugger, null, 0, null, Harbor, false, 1, trial, Skills));
        Assert.True(SweetSpot.BarrelScale(sure.Stats.Contact, false, false, 0, trial)
                    > SweetSpot.BarrelScale(slugger.Stats.Contact, false, false, 0, trial));
    }

    // ---------------------------------------------------------------------------------
    // S-126  The overlay is retired, and the validator still floors the window
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Until #883 the overlay's batting file was the shipped file with exactly one key changed:
    /// #855's <c>geometryOnly</c> (PH-12). Before #860 it also changed this child's
    /// <c>window.shared</c>. Jack accepted both in the <c>trials/pitch5</c> window on September 22,
    /// 2026 ("trial was good." for the window, "approve all" for the stick), #860 and #883 turned them
    /// on in <c>data/</c>, and #883 deleted the folder. So the shipped file authors both keys true,
    /// the overlay is gone, and the stick's off path is the shipped file with that one key put back —
    /// built in the test (<see cref="SwitchOffPaths.StickShapes"/>), never kept as a second copy.
    /// </summary>
    [Fact]
    public void S126_TheOverlayIsRetiredAndTheShippedFileAuthorsBothKeys()
    {
        Assert.False(Directory.Exists(Path.Combine(Repo, "trials", "pitch5")), "#883 retired trials/pitch5");

        var shippedJson = Parse(Path.Combine(ShippedRoot, "rules", "batting.json"));
        Assert.True(shippedJson["window"]!["shared"]!.GetValue<bool>(), "the shipped root turns the window switch on (#860)");
        Assert.True(shippedJson["geometryOnly"]!.GetValue<bool>(), "the shipped root turns geometryOnly on (#883)");

        // The off path changes that one key and nothing else, so a row that asserts it measures the
        // switch and only the switch.
        var off = SwitchOffPaths.StickShapes;
        Assert.Empty(RulesTable.Validate(off));
        var offJson = Parse(Path.Combine(off.Shipped, "rules", "batting.json"));
        Assert.False(offJson["geometryOnly"]!.GetValue<bool>(), "the off path turns geometryOnly off");
        offJson["geometryOnly"] = true;
        Assert.Equal(shippedJson.ToJsonString(), offJson.ToJsonString());
    }

    [Fact]
    public void S126_TheShippedFileWritesTheTrialStartValueDownOnce()
    {
        // 9 was the start value because 9 was the quick swing for an average hitter, and Jack
        // accepted it (#860); that has to be readable in the file rather than asserted as a literal
        // here: both keys are read, and compared to each other.
        var window = Parse(Path.Combine(ShippedRoot, "rules", "batting.json"))["window"]!.AsObject();
        Assert.Equal(window["slapFrames"]!.GetValue<double>(), window["frames"]!.GetValue<double>());

        var w = ShippedRules.Batting.Window;
        Assert.Equal(w.SlapFrames, w.Frames);

        // …so on the off path an average hitter's quick swing and the shipped one window are the
        // same number, which is the whole reason 9 was the accepted start (PH-10-R1).
        Assert.Equal(AtBatResolver.ContactWindowFrames(5, false, null, Harbor, false, ShippedRules, Skills),
            AtBatResolver.ContactWindowFrames(5, false, null, Harbor, false, SwitchOffPaths.SplitWindowRules, Skills));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S126_AWindowUnderItsOwnFloorIsRefusedByName(bool shared)
    {
        // The floor is a property of the table, not of the switch: a window the floor would have to
        // rescue on every swing is an authoring mistake either way.
        using var fixture = new RulesFixture();
        fixture.Change("batting.json", json =>
        {
            json["window"]!["shared"] = shared;
            json["window"]!["frames"] = 4.0;
        });

        var errors = RulesTable.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains("batting.window.floorFrames must not exceed its upper bound; got 5 > 4", StringComparison.Ordinal)
                                     && e.Contains(fixture.Path("batting.json"), StringComparison.Ordinal));
    }

    [Fact]
    public void S126_TheSwitchIsARuleTheTableOwnsAndAMissingOneIsOff()
    {
        using var fixture = new RulesFixture();
        fixture.Change("batting.json", json => json["window"]!.AsObject().Remove("shared"));

        // Absent falls back to the code default. Until #860 that was off; since #860 the code
        // default follows the shipped JSON (the JSON = code parity rail), so an absent key plays the
        // shipped, shared window — still the safe direction, which is "what ships". An explicit
        // false is the only way to the off path. The method name keeps the row's history.
        var errors = new List<string>();
        Assert.True(RulesTable.Load(new DataRoot(fixture.Root), errors).Batting.Window.Shared);
        Assert.Empty(errors);
        Assert.False(SwitchOffPaths.SplitWindowRules.Batting.Window.Shared);

        using var misspelled = new RulesFixture();
        misspelled.Change("batting.json", json => json["window"]!["sharedWindow"] = true);
        Assert.Contains(RulesTable.Validate(new DataRoot(misspelled.Root)),
            e => e.Contains("batting.window.sharedWindow is not a rule this table owns", StringComparison.Ordinal));
    }

    [Fact]
    public void S126_GeometryOnlyIsARuleTheTableOwnsAndAMissingOneIsOff()
    {
        // #855's switch is held to the same terms as this one: the table owns it, an absent key falls
        // back to the code default, and a misspelling is refused. Until #883 the default was off;
        // since #883 (Jack, September 22, 2026: "approve all") the code default follows the shipped
        // JSON (the JSON = code parity rail), so an absent key plays the shipped rule — the stick
        // shapes only a bunt or a Star Swing. An explicit false is the only way to the off path. The
        // method name keeps the row's history.
        using var fixture = new RulesFixture();
        fixture.Change("batting.json", json => json.Remove("geometryOnly"));
        var errors = new List<string>();
        Assert.True(RulesTable.Load(new DataRoot(fixture.Root), errors).Batting.GeometryOnly);
        Assert.Empty(errors);
        Assert.False(SwitchOffPaths.StickShapesRules.Batting.GeometryOnly);

        using var misspelled = new RulesFixture();
        misspelled.Change("batting.json", json => json["geometryOnlyMode"] = true);
        Assert.Contains(RulesTable.Validate(new DataRoot(misspelled.Root)),
            e => e.Contains("batting.geometryOnlyMode is not a rule this table owns", StringComparison.Ordinal)
                 && e.Contains(misspelled.Path("batting.json"), StringComparison.Ordinal));

        using var misplaced = new RulesFixture();
        misplaced.Change("batting.json", json =>
        {
            json.Remove("geometryOnly");
            json["spray"]!["geometryOnly"] = true;
        });
        Assert.Contains(RulesTable.Validate(new DataRoot(misplaced.Root)),
            e => e.Contains("batting.spray.geometryOnly is not a rule this table owns", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // S-127  The S-29 cohort from a catalog built in the test — recorded, never gated
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Fifty three-inning CPU-vs-CPU games from a catalog that is <b>not</b> the process's: since #883
    /// retired <c>trials/pitch5</c>, the stick switch's off path (<see cref="SwitchOffPaths.StickShapes"/>).
    /// <b>This row asserts no band.</b> The shipped band (F693-06, 1.8–5 per side) belongs to
    /// <see cref="AtBatScenarioTests.S29_FiftySeedCpuGamesLandInTheBand"/> on the shipped root, and
    /// tuning anything to move these figures is banned by #844, #855 and #883. The figures are in
    /// <c>docs/research/plate-window-p2b.md</c>, <c>docs/research/stick-shaping-p2c.md</c> and
    /// <c>docs/research/promote-stick.md</c>.
    ///
    /// <b>The cohort runs in process.</b> #844 found <see cref="Match.AutoPlay"/>'s in-zone read —
    /// <see cref="AtBatResolver.PitchInZone"/> → <see cref="StrikeZoneGeometry.Contains(PitchCommand, string?, RulesTable?)"/>
    /// → <see cref="PitchFlight.Point(PitchCommand, double, string?, System.ValueTuple{double, double, double}?, RulesTable?)"/>
    /// — resolving the pitch family against the <em>process-wide</em> table rather than the match's,
    /// so an overlay-only family stopped an in-process overlay game by name, and pinned it here. #855
    /// threaded the match's table through every hop (an optional <see cref="RulesTable"/> defaulting
    /// to today's resolution), and <c>Match</c> passes its own. So the pinned branch is gone: the row
    /// asks the read with the catalog's own table, then plays the real cohort from that catalog in a
    /// process rooted at the shipped data.
    /// </summary>
    [Fact]
    public void S127_TheS29CohortOnTheOffPathRunsInProcessAndIsRecordedNotGated()
    {
        // The cohort's parks and diamond come from the process-wide table; the compact profile is a
        // different diamond and a different (equally correct) cohort, and it is not what #844 measures.
        if (TestRoot.Compact) return;

        var content = SwitchOffPaths.StickShapesContent;
        Assert.False(content.Rules.Batting.GeometryOnly, "the cohort plays the stick's off path");

        // (a) The window those fifty games are judged in is the shared one, at every rung.
        Assert.True(content.Rules.Batting.Window.Shared, "the cohort plays the shared window");
        foreach (var rung in Rungs)
        foreach (var charged in new[] { false, true })
            Assert.Equal(content.Rules.Batting.Window.Frames,
                AtBatResolver.ContactWindowFrames(5, charged, null, Harbor, false, content.Rules, content.StarSkills, rung));

        // (b) The repaired read: handed the catalog's table, every hop flies the family it authors,
        //     and the umpire's answer is the crossing's.
        var trialOnly = PitchFamily.Curveball;
        var pitch = new PitchCommand(trialOnly, 0, false);
        Assert.True(content.Rules.Pitching.Families.IsAuthored(trialOnly), "the catalog authors it");
        var crossing = PitchFlight.Point(pitch, 1, rules: content.Rules);
        Assert.Equal(StrikeZoneGeometry.Contains(crossing.X, crossing.Y), StrikeZoneGeometry.Contains(pitch, null, content.Rules));
        Assert.Equal(StrikeZoneGeometry.Contains(pitch, null, content.Rules), AtBatResolver.PitchInZone(pitch, 5, null, content.Rules));

        // Handed no table the read is still the process-wide one, as before: on a shipped-rooted
        // process that table does not author the family and stops by name.
        if (!Rules.Default.Pitching.Families.IsAuthored(trialOnly))
        {
            var stopped = Assert.Throws<InvalidOperationException>(() => AtBatResolver.PitchInZone(pitch, 5));
            Assert.Contains(trialOnly, stopped.Message, StringComparison.Ordinal);
        }

        // (c) The real cohort, in process, from the catalog built in the test — and still no band.
        var report = RaceCohort.Run(content, "s29");
        Assert.Equal(50, report.Games.Count);
        Assert.All(report.Games, g => Assert.True(g.HomeRuns >= 0 && g.AwayRuns >= 0));
        var line = $"off-path S-29: runs {report.MeanAwayRuns:0.00} away / {report.MeanHomeRuns:0.00} home over {report.Games.Count} games";
        Assert.True(report.MeanHomeRuns > 0 && report.MeanAwayRuns > 0, line);
        Assert.True(report.Games.Sum(g => g.Outcomes.GetValueOrDefault("Strikeout")) > 0, line);
        Assert.True(report.Games.Sum(g => g.Outcomes.GetValueOrDefault("Single")) > 0, line);
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
