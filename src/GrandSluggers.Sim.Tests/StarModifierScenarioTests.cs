using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The held special modifier (spec §12, PH-16-R10, R11, R12, R17), Appendix B.1 rows S-200 … S-205.
///
/// The client reads LB (pad) or Q (player 1's keys) at the accepted release of the pitch or the swing and sends the
/// request; <see cref="Match"/> settles it. <see cref="StarModifier"/> is the pure step behind the read and its leak
/// guard; <see cref="Match.PitchStarRequest"/> / <see cref="Match.SwingStarRequest"/> are the typed request the
/// release shows and the play records; <see cref="BroadcastHud.StarUnavailable(StarRequest)"/> is the couch tell.
/// </summary>
public sealed class StarModifierScenarioTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));
    static double CenterY => StrikeZoneGeometry.CenterY;

    static void DrainDefense(Match m)
    {
        for (var guard = 0; m.CanStarPitch && guard < 20; guard++)
            m.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take);
        Assert.False(m.CanStarPitch);
    }

    static void DrainOffense(Match m)
    {
        for (var guard = 0; m.CanStarSwing && guard < 20; guard++)
            m.Play(Scenario.PitchAt(0, CenterY), new SwingCommand(true, 0, 40, true));
        Assert.False(m.CanStarSwing);
    }

    // ---------------------------------------------------------------------------------
    // S-200  The modifier is read at the accepted release, and a hold that asked is spent
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S200_TheModifierIsReadAtTheReleaseAndAHoldThatAskedIsSpentUntilItComesUp()
    {
        var rest = default(StarModifierState);
        Assert.True(StarModifier.IsFree(rest));

        // Held at the release: the special. Up at the release: the ordinary action, nothing spent.
        var asked = StarModifier.Release(rest, held: true);
        Assert.True(asked.Request);
        Assert.False(StarModifier.IsFree(asked.Next));
        var plain = StarModifier.Release(rest, held: false);
        Assert.False(plain.Request);
        Assert.True(StarModifier.IsFree(plain.Next));

        // Changes while charging are free: only the release tick reads it. A hold pressed and let go before the release
        // leaves nothing behind; one pressed late in the charge counts.
        var state = rest;
        foreach (var held in new[] { true, true, false, false })
            state = StarModifier.Tick(state, held);
        Assert.False(StarModifier.Release(state, held: false).Request);
        state = StarModifier.Tick(rest, held: true);
        Assert.True(StarModifier.Release(state, held: true).Request);

        // The spent hold, still down, is no other verb and asks for nothing at the next release.
        state = asked.Next;
        for (var tick = 0; tick < 30; tick++)
        {
            state = StarModifier.Tick(state, held: true);
            Assert.False(StarModifier.IsFree(state));
        }
        var carried = StarModifier.Release(state, held: true);
        Assert.False(carried.Request);
        Assert.False(StarModifier.IsFree(carried.Next));

        // Up: free. Held again: the next release asks.
        state = StarModifier.Tick(carried.Next, held: false);
        Assert.True(StarModifier.IsFree(state));
        state = StarModifier.Tick(state, held: true);
        Assert.True(StarModifier.Release(state, held: true).Request);
    }

    // ---------------------------------------------------------------------------------
    // S-201  The request the release reads is the request the play records
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void S201_TheRequestTheReleaseReadsIsTheRequestThePlayRecords(int seed, bool drained)
    {
        // The pitch: affordable and not.
        var m = new Scenario(Shipped, seed).Match;
        if (drained) DrainDefense(m);
        var atRelease = m.PitchStarRequest;
        Assert.Equal(!drained, atRelease.Afforded);
        var play = m.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take);
        Assert.Equal(atRelease, Assert.Single(play.Outcome!.Stars));
        Assert.Equal(!drained, play.Pitch.Star);

        // The swing: affordable and not.
        m = new Scenario(Shipped, seed).Match;
        if (drained) DrainOffense(m);
        atRelease = m.SwingStarRequest;
        Assert.Equal(!drained, atRelease.Afforded);
        play = m.Play(Scenario.PitchAt(0, CenterY), new SwingCommand(true, 0, 40, true));
        Assert.Equal(atRelease, Assert.Single(play.Outcome!.Stars));
        Assert.Equal(!drained, play.Swing.Star);
    }

    // ---------------------------------------------------------------------------------
    // S-202  The unavailable tell names the team from the typed request, never a caption
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S202_TheUnavailableTellIsBuiltFromTheTypedRequestAndSitsUnderTheScorebug()
    {
        var m = new Scenario(Shipped, 1).Match;
        Assert.Null(BroadcastHud.StarUnavailable(m.PitchStarRequest));
        DrainDefense(m);
        var request = m.PitchStarRequest;
        var tell = BroadcastHud.StarUnavailable(request);
        Assert.NotNull(tell);
        Assert.Equal(StarAction.Pitch, tell!.Value.Action);
        // The defense in the top half is the home team: the home row of the scorebug (row 1).
        Assert.True(m.Top);
        Assert.Equal(1, tell.Value.Row);

        var play = m.Play(Scenario.PitchAt(2.5, CenterY) with { Star = true }, Scenario.Take);
        Assert.Equal(tell, BroadcastHud.StarUnavailable(play.Outcome!.Stars));
        Assert.Null(BroadcastHud.StarUnavailable(PlayOutcome.Empty.Stars));

        // The offense in the top half is away: row 0; the words name the swing.
        var swingTell = BroadcastHud.StarUnavailable(new StarRequest(StarAction.Swing, false, "rio", "heat-swing", 1, 0, false));
        Assert.Equal(0, swingTell!.Value.Row);
        Assert.Contains("SWING", swingTell.Value.Words);
        Assert.Contains("PITCH", tell.Value.Words);

        // It starts red, flips, and ends.
        Assert.True(BroadcastHud.StarUnavailableRed(0));
        Assert.False(BroadcastHud.StarUnavailableRed(1.0 / BroadcastHud.StarUnavailableFlips + 0.001));
        Assert.False(BroadcastHud.StarUnavailableShows(BroadcastHud.StarUnavailableSeconds));
        Assert.False(BroadcastHud.StarUnavailableRed(BroadcastHud.StarUnavailableSeconds + 0.01));

        var score = BroadcastHud.Standard.Score;
        var line = BroadcastHud.StarUnavailableLine(score);
        Assert.True(BroadcastHud.OnScorebug(score, line));
        foreach (var (w, h) in new[] { (1280.0, 800.0), (1920.0, 1080.0), (1024.0, 768.0) })
            Assert.True(BroadcastHud.InFrame(line, w, h), $"{w}×{h}");
        Assert.False(BroadcastHud.IsScreenCenterCard(line));
    }

    // ---------------------------------------------------------------------------------
    // S-203  The unavailable lesson: the player's held request on a short pool, recorded by the play
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S203_TheUnavailableLessonCreditsOnlyThePlayersHeldRequestOnAShortPool()
    {
        var catalog = TutorialCatalog.Load(Shipped);
        Assert.Empty(catalog.Validate(Shipped));
        var run = new TutorialSession(Shipped, catalog, "T-G03-U"); run.Begin();
        for (var n = 1; n <= 3; n++)
        {
            Assert.False(run.Match.CanStarPitch);
            var before = run.Match.DefenseStars;
            Assert.True(run.Pitch(new("fastball", 0, true)));
            Assert.True(run.Feedback!.Success, run.Feedback.Detail);
            Assert.Equal(before, run.Match.DefenseStars);
            Assert.False(run.LastPlay!.Pitch.Star);
            var request = Assert.Single(run.LastPlay.Outcome!.Stars);
            Assert.False(request.Afforded);
            Assert.Equal(n, run.Successes);
            Assert.Equal(run.Feedback, TutorialSession.Replay(Shipped, catalog, run.Recording()).Feedback);
            run.Retry();
        }
        Assert.True(run.Passed);

        // No modifier held: nothing was asked, nothing is taught.
        run = new TutorialSession(Shipped, catalog, "T-G03-U"); run.Begin();
        Assert.True(run.Pitch(new("fastball", 0, false)));
        Assert.False(run.Feedback!.Success);
        Assert.Equal("star-not-asked", run.Feedback.Code);
        // The CPU cannot earn it, and a demonstration earns nothing.
        run = new TutorialSession(Shipped, catalog, "T-G03-U"); run.Begin();
        Assert.False(run.Pitch(new("fastball", 0, true), LivePlayCommandSource.Cpu));
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(Shipped, catalog, "T-G03-U"); run.Begin(demonstration: true);
        Assert.True(run.Pitch(new("fastball", 0, true), LivePlayCommandSource.Cpu));
        Assert.Equal("demonstration", run.Feedback!.Code);
    }

    [Fact]
    public void S203_TheValidatorRefusesAnUnavailableLessonWhosePoolCanPay()
    {
        var paid = TutorialCatalog.Load(Shipped);
        var index = Array.FindIndex(paid.Setups, s => s.Id == paid.Lesson("T-G03-U").Setup);
        paid.Setups[index] = paid.Setups[index] with { PoolStars = Shipped.Rules.Stars.MeterMax };
        Assert.Contains(paid.Validate(Shipped), e => e.Contains("T-G03-U needs the named pitcher on a pool short", StringComparison.Ordinal));
        var negative = TutorialCatalog.Load(Shipped);
        negative.Setups[index] = negative.Setups[index] with { PoolStars = -1 };
        Assert.Contains(negative.Validate(Shipped), e => e.Contains("T-G03-U has invalid pool stars", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // S-204  The couch map: LB / Q held at the release; North arms nothing; all-advance is after contact
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S204_TheBookAndTheSchemeTeachTheHeldModifierAndWhereAllAdvanceLives()
    {
        Assert.Equal("LB hold at release", Scheme.Pad("star"));
        Assert.Equal("Q hold at release", Scheme.Keys("star"));
        Assert.Contains("after contact", Scheme.Pad("all-advance"));

        foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
        {
            var hold = scheme == InputScheme.Pad ? "LB" : "Q";
            foreach (var role in new[] { "batting", "pitching" })
            {
                var row = RoleTables.Of(scheme).Single(b => b.Id == role).Rows
                    .Single(r => r.Verb is "Star swing" or "Star pitch");
                Assert.Contains("Hold " + hold, row.Press);
                Assert.Contains("let go", row.Press);
            }
            var advance = RoleTables.Of(scheme).Single(b => b.Id == "running").Rows.Single(r => r.Verb == "All advance");
            Assert.Contains("after contact", advance.Press);
            Assert.Contains(HowToPlay.Must("stars").Shown(scheme), l => l.Contains("Hold " + hold) && l.Contains("let go"));
            Assert.Contains(HowToPlay.Must("stars").Shown(scheme), l => l.Contains("flash red"));
            Assert.Contains(HowToPlay.Must("running").Shown(scheme), l => l.Contains("After contact"));
        }
        // North / Q no longer select or arm a star anywhere in the book.
        var every = HowToPlay.Pages.SelectMany(p => p.Lines.Concat(p.KeyLines ?? [])).ToArray();
        Assert.DoesNotContain(every, l => l.Contains("North + South") || l.Contains("Q + Space") || l.Contains("Q+Space")
            || l.Contains("selects the named skill") || l.Contains("North selects"));
        Assert.DoesNotContain(ControlDiagram.PadCallouts, c => c.Id == "north" && (c.Offense + c.Defense).Contains("Star"));
        // PH-16-R16: the draft does not move the starting Stars; the book says both teams start even.
        Assert.DoesNotContain(every, l => l.Contains("Stars jump") || l.Contains("start with more stars"));
        Assert.Contains(HowToPlay.Must("lineup").Lines, l => l.Contains("same stars"));
    }

    // ---------------------------------------------------------------------------------
    // S-205  Every star lesson teaches the held modifier and its revision moved with the verb
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S205_EveryStarLessonTeachesTheHeldModifier()
    {
        var catalog = TutorialCatalog.Load(Shipped);
        var stars = catalog.Lessons.Where(l => l.Objective is "star-pitch" or "star-swing" or "star-resource" or "star-unavailable").ToArray();
        Assert.Contains(stars, l => l.Id == "T-G03-U");
        foreach (var lesson in stars)
        {
            Assert.True(lesson.Revision >= (lesson.Id == "T-G03-U" ? 1 : 3), lesson.Id + " revision did not move with the verb");
            var pad = HowToPlay.TutorialControls(lesson.Id, InputScheme.Pad);
            var keys = HowToPlay.TutorialControls(lesson.Id, InputScheme.Keys);
            Assert.Contains("hold LB as you let go of South", pad, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("hold Q as you let go of Space", keys, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("North", pad);
        }
    }
}
