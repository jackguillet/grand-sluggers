using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ClosePlayTests
{
    [Fact]
    public void OnlyThirdAndHomeWhenARunnerIsRacingThere()
    {
        var tag = InPlay.ForceState.FromOccupancy(false, true, false);
        Assert.False(ClosePlay.Offered(1, tag, true));
        Assert.False(ClosePlay.Offered(2, tag, true));
        Assert.True(ClosePlay.Offered(3, tag, runnerHeadingThere: true));
        Assert.False(ClosePlay.Offered(3, tag, runnerHeadingThere: false), "nobody coming is no play");
        Assert.True(ClosePlay.Offered(4, InPlay.ForceState.FromOccupancy(false, false, true), true));
        var loaded = InPlay.ForceState.FromOccupancy(true, true, true);
        Assert.False(ClosePlay.Offered(4, loaded, true), "force at home is the throw, not a mash");
        var corner = InPlay.ForceState.FromOccupancy(true, true, false);
        Assert.False(ClosePlay.Offered(3, corner, true), "force at third is the throw, not a mash");
    }

    [Fact]
    public void FirstPressWinsAndATieIsSafe()
    {
        Assert.True(ClosePlay.OffenseSafe(0.10, 0.20));
        Assert.False(ClosePlay.OffenseSafe(0.30, 0.12));
        Assert.True(ClosePlay.OffenseSafe(0.18, 0.18));
        Assert.True(ClosePlay.CpuReactionSec(10, rules: Rules.Default) < ClosePlay.CpuReactionSec(1, rules: Rules.Default));
        Assert.Contains("SAFE", ClosePlay.Caption(4, true));
        Assert.Contains("OUT", ClosePlay.Caption(3, false));
    }

    [Fact]
    public void AttackSmashesAFlyingItemAndKicksNearby()
    {
        Assert.True(FieldDash.KickOffered(12, rules: Rules.Default));
        Assert.False(FieldDash.KickOffered(40, rules: Rules.Default));
        Assert.True(FieldDash.DestroysItem(true, true, 10, rules: Rules.Default));
        Assert.False(FieldDash.DestroysItem(false, true, 10, rules: Rules.Default));
        Assert.False(FieldDash.DestroysItem(true, false, 10, rules: Rules.Default));
        var field = new FieldingResult(PlayKind.InPlay, null, null, 1, 0, 40, false, false, Item: "banana", ItemHit: true);
        var smashed = ErrorItems.Smash(field, grounder: true);
        Assert.Equal(PlayKind.InPlay, smashed.Kind);
        Assert.Null(smashed.Item);
        Assert.False(smashed.ItemHit, "a smashed item never lands");
    }

    [Fact]
    public void HaltBeforeThePitchHoldsTheDepartedRunnerWhereTheyStand()
    {
        // A departure creates a live body; halt stops it without restoring the bag.
        var match = Match.Slice(ContentCatalog.Load(), seed: 1);
        Assert.False(match.HaltAt(1));
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.First);
        Assert.True(match.SelectRunner(1));
        Assert.True(match.StartSteal());
        Assert.True(match.StealOn);
        match.PitchSetup.Advance(.3);
        var feet = match.RunnerAt(1)!.Feet;
        Assert.True(feet > 0);
        Assert.True(match.HaltAt(1));
        match.PitchSetup.Advance(.3);
        Assert.Equal(feet, match.RunnerAt(1)!.Feet);
        Assert.True(match.RunnerAt(1)!.Held);
    }
}
