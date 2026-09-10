using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class PlayOutcomeTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void ResolvedBuddyCatchCarriesTypedFeatIntoEvent()
    {
        var match = Match.Slice(_content, seed: 7);
        var fielder = _content.Must("nico");
        var buddy = _content.Must("gull");
        var hit = Fly(carry: 300, homeRun: false);
        var at = Diamond.Positions["CF"];
        var preview = Preview(fielder, buddy, at.X, at.Z, homeRunLikely: true);

        var field = match.ResolveFielding(hit, preview);
        var ev = match.FinishAtBat(Pitch(), Swing(), hit, field);

        Assert.Equal(DefensiveFeat.BuddyJump, field.Feat);
        Assert.Equal(DefensiveFeat.BuddyJump, ev.Outcome?.DefensiveFeat);
        Assert.Equal(HighlightBeat.BuddyJump, Highlight.BeatOf(ev with { Caption = "捕球しました。" }));
    }

    [Fact]
    public void ResolvedSuperJumpRobCarriesTypedFeatIntoEvent()
    {
        var match = Match.Slice(_content, seed: 7);
        var fielder = _content.Must("nico");
        var fence = AtBatResolver.FenceAt(match.Park, 0);
        var hit = Fly(fence + 10, homeRun: true);
        var field = match.ResolveFielding(hit, Preview(fielder, null, 0, fence, homeRunLikely: true));
        var ev = match.FinishAtBat(Pitch(), Swing(), hit, field);

        Assert.Equal(PlayKind.FlyOut, field.Kind);
        Assert.Equal(DefensiveFeat.SuperJump, field.Feat);
        Assert.Equal(DefensiveFeat.SuperJump, ev.Outcome?.DefensiveFeat);
        Assert.Equal(HighlightBeat.RobbedHomer, Highlight.BeatOf(ev with { Caption = "Catch." }));
    }

    [Fact]
    public void ResolvedClamberRobCarriesTypedFeatIntoEvent()
    {
        var match = Match.Slice(_content, seed: 7, parkId: "canopy-yard");
        var fielder = _content.Must("konga");
        var fence = AtBatResolver.FenceAt(match.Park, 0);
        var hit = Fly(fence + 12, homeRun: true);
        var field = match.ResolveFielding(hit, Preview(fielder, null, 0, fence, homeRunLikely: true));
        var ev = match.FinishAtBat(Pitch(), Swing(), hit, field);

        Assert.Equal(PlayKind.FlyOut, field.Kind);
        Assert.Equal(DefensiveFeat.Clamber, field.Feat);
        Assert.Equal(DefensiveFeat.Clamber, ev.Outcome?.DefensiveFeat);
        Assert.Equal(HighlightBeat.RobbedHomer, Highlight.BeatOf(ev with { Caption = "Catch." }));
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

    static AtBatResult Fly(double carry, bool homeRun) => new(
        ContactQuality.Perfect, true, false, 102, 28, carry, homeRun, false, null, null);

    static FieldingPreview Preview(
        Character fielder, Character? buddy, double x, double z, bool homeRunLikely, bool line = false) =>
        new(fielder, "CF", buddy, 4, x, z, false, homeRunLikely, false, false, false, 20, Line: line);

    static PitchCommand Pitch() => new("fastball", 0, 0, false);
    static SwingCommand Swing() => new(true, 0, 0, false);
}
