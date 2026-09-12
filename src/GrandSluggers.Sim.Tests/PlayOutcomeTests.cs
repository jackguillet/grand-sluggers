using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class PlayOutcomeTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void LiveBuddyJumpRobCarriesTypedFeatIntoEvent()
    {
        // CF nico with LF gull (same faction): two good-chem outfielders under a homer by ten feet;
        // the CPU bodies plant at the wall and the buddy jump's rob height (18) takes it (§8.3, §8.4).
        var match = RobMatch("harbor-diamond", "gull", "nico", "dart");
        var hit = FlightFixtures.OverTheFence(match.Park, 10, 0);
        var preview = match.PreviewHit(hit);
        Assert.Equal("nico", preview.Fielder.Id);
        Assert.NotNull(preview.Buddy);
        Assert.True(FieldingResolver.BuddyJumpOffered(preview));
        var field = match.ResolveFielding(hit, preview);
        Assert.Equal(PlayKind.HomeRun, field.Kind);

        var ev = match.FinishAtBat(Pitch(), Swing(), hit, field);

        Assert.Equal(PlayKind.FlyOut, ev.Kind);
        Assert.Equal(DefensiveFeat.BuddyJump, ev.Outcome?.DefensiveFeat);
        Assert.Equal(HighlightBeat.BuddyJump, Highlight.BeatOf(ev with { Caption = "捕球しました。" }));
    }

    [Fact]
    public void LiveSuperJumpRobCarriesTypedFeatIntoEvent()
    {
        // CF nico (Super Jump, rob height 18) between two neutral outfielders: the leap at the wall is the glove's own.
        var match = RobMatch("harbor-diamond", "zig", "nico", "dart");
        var hit = FlightFixtures.OverTheFence(match.Park, 10, 0);
        var preview = match.PreviewHit(hit);
        Assert.Equal("nico", preview.Fielder.Id);
        Assert.False(FieldingResolver.BuddyJumpOffered(preview));
        var field = match.ResolveFielding(hit, preview);
        Assert.Equal(PlayKind.HomeRun, field.Kind);

        var ev = match.FinishAtBat(Pitch(), Swing(), hit, field);

        Assert.Equal(PlayKind.FlyOut, ev.Kind);
        Assert.Equal(DefensiveFeat.SuperJump, ev.Outcome?.DefensiveFeat);
        Assert.Equal(HighlightBeat.RobbedHomer, Highlight.BeatOf(ev with { Caption = "Catch." }));
    }

    [Fact]
    public void LiveClamberRobCarriesTypedFeatIntoEvent()
    {
        // CF konga (Clamber) at Canopy Yard's climb wall, between two neutral outfielders: rob height 28 takes a homer by twelve feet.
        var match = RobMatch("canopy-yard", "frost", "konga", "hex");
        var hit = FlightFixtures.OverTheFence(match.Park, 12, 0);
        var preview = match.PreviewHit(hit);
        Assert.Equal("konga", preview.Fielder.Id);
        Assert.False(FieldingResolver.BuddyJumpOffered(preview));
        var field = match.ResolveFielding(hit, preview);
        Assert.Equal(PlayKind.HomeRun, field.Kind);

        var ev = match.FinishAtBat(Pitch(), Swing(), hit, field);

        Assert.Equal(PlayKind.FlyOut, ev.Kind);
        Assert.Equal(DefensiveFeat.Clamber, ev.Outcome?.DefensiveFeat);
        Assert.Equal(HighlightBeat.RobbedHomer, Highlight.BeatOf(ev with { Caption = "Catch." }));
    }

    [Fact]
    public void LiveHomerNobodyCanRobIsAHomeRun()
    {
        // CF rio (no leap ability) between two neutral outfielders under the same ten-foot homer: the window is there, the reach is not (§8.4).
        var match = RobMatch("harbor-diamond", "frost", "rio", "hex", captain: "pip");
        var hit = FlightFixtures.OverTheFence(match.Park, 10, 0);
        var preview = match.PreviewHit(hit);
        Assert.Equal("rio", preview.Fielder.Id);
        Assert.False(FieldingResolver.BuddyJumpOffered(preview));
        var ev = match.FinishAtBat(Pitch(), Swing(), hit, match.ResolveFielding(hit, preview));
        Assert.Equal(PlayKind.HomeRun, ev.Kind);
    }

    /// <summary>A home nine on defense (the top half) with the named LF / CF / RF; the rest in roster order.</summary>
    Match RobMatch(string parkId, string lf, string cf, string rf, string captain = "rio")
    {
        var everyone = new[] { "rio", "nico", "pip", "marlow", "vale", "lace", "zig", "dart", "vine", "gull", "konga", "frost", "hex" };
        var infield = everyone.Where(id => id != captain && id != lf && id != cf && id != rf).Take(5).ToArray();
        var home = _content.Team("Rob", captain, infield[0], infield[1], infield[2], infield[3], infield[4], lf, cf, rf);
        var match = Match.Exhibition(_content, home, PresetTeams.EmberCourt(_content), seed: 7, parkId: parkId);
        Assert.True(match.Top, "the home nine is on defense");
        Assert.Equal(cf, FieldingResolver.Assign(match.Defense, match.Pitcher)["CF"].Id);
        return match;
    }

    [Fact]
    public void LiveCatchFeatRequiresThePlayerToPerformTheVerb()
    {
        var park = _content.Parks["canopy-yard"];
        var konga = Preview(_content.Must("konga"), null, 0, 360, homeRunLikely: true);
        var nico = Preview(_content.Must("nico"), null, 0, 360, homeRunLikely: true);
        var buddy = Preview(_content.Must("nico"), _content.Must("gull"), 0, 360, homeRunLikely: true);

        Assert.Equal(DefensiveFeat.None, FieldingResolver.PlayerCatchFeat(konga, park, false, false));
        Assert.Equal(DefensiveFeat.Clamber, FieldingResolver.PlayerCatchFeat(konga, park, false, true));
        Assert.Equal(DefensiveFeat.SuperJump, FieldingResolver.PlayerCatchFeat(nico, park, false, true));
        Assert.Equal(DefensiveFeat.BuddyJump, FieldingResolver.PlayerCatchFeat(buddy, park, true, true));
    }

    static FieldingPreview Preview(
        Character fielder, Character? buddy, double x, double z, bool homeRunLikely, bool line = false) =>
        FlightFixtures.Preview(fielder, "CF", homeRunLikely ? BattedBallClass.Homer : line ? BattedBallClass.Liner : BattedBallClass.Fly,
            4, x, z, buddy, 20);

    static PitchCommand Pitch() => new("fastball", 0, false);
    static SwingCommand Swing() => new(true, 0, 0, false);
}
