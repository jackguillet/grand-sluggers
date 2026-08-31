using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ClosePlayTests
{
    [Fact]
    public void OnlyThirdAndHomeWhenARunnerIsRacingThere()
    {
        Assert.False(ClosePlay.Offered(1, true, true));
        Assert.False(ClosePlay.Offered(2, true, false));
        Assert.True(ClosePlay.Offered(3, secondOccupied: true, thirdOccupied: false));
        Assert.True(ClosePlay.Offered(4, secondOccupied: true, thirdOccupied: true));
        Assert.False(ClosePlay.Offered(3, secondOccupied: true, thirdOccupied: true));
        Assert.False(ClosePlay.Offered(4, false, false));
    }

    [Fact]
    public void FirstPressWinsAndATieIsSafe()
    {
        Assert.True(ClosePlay.OffenseSafe(0.10, 0.20));
        Assert.False(ClosePlay.OffenseSafe(0.30, 0.12));
        Assert.True(ClosePlay.OffenseSafe(0.18, 0.18));
        Assert.True(ClosePlay.CpuReactionSec(10) < ClosePlay.CpuReactionSec(1));
        Assert.Contains("SAFE", ClosePlay.Caption(4, true));
        Assert.Contains("OUT", ClosePlay.Caption(3, false));
    }

    [Fact]
    public void AttackSmashesAFlyingItemAndKicksNearby()
    {
        Assert.True(FieldDash.KickOffered(12));
        Assert.False(FieldDash.KickOffered(40));
        Assert.True(FieldDash.DestroysItem(true, true, 10));
        Assert.False(FieldDash.DestroysItem(false, true, 10));
        Assert.False(FieldDash.DestroysItem(true, false, 10));
        var field = new FieldingResult(PlayKind.Single, null, null, 1, 0, 40, false, false, Item: "banana");
        var smashed = ErrorItems.Smash(field, grounder: true);
        Assert.Equal(PlayKind.GroundOut, smashed.Kind);
        Assert.Null(smashed.Item);
    }

    [Fact]
    public void HaltFreezesOneRunnerWithoutWalkingThemBack()
    {
        var match = Match.Slice(ContentCatalog.Load(), seed: 1);
        Assert.False(match.HaltAt(1));
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.First);
        Assert.True(match.TakeLead(0.5));
        Assert.InRange(match.Lead01, 0.49, 0.51);
        Assert.True(match.HaltAt(1));
        Assert.InRange(match.Lead01, 0.49, 0.51);
        Assert.False(match.Returning);
        Assert.False(match.StealOn);
    }

    [Fact]
    public void HowToPlayBookLeftHalfIsBack()
    {
        var nav = HowToPlay.HitNav(200, 360, 1280, 720, 8);
        var panel = HowToPlay.BookPanel(1280, 720, 8);
        Assert.True(panel.W > 700);
        var left = HowToPlay.HitNav(panel.X + 20, panel.Y + 40, 1280, 720, 8);
        var right = HowToPlay.HitNav(panel.X + panel.W - 20, panel.Y + 40, 1280, 720, 8);
        Assert.Equal(-1, left);
        Assert.Equal(1, right);
        Assert.Equal(0, HowToPlay.HitNav(4, 4, 1280, 720, 8));
        _ = nav;
    }
}
