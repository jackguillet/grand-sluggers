using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FieldAssistTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void ExhibitionStartsCpuOnTheGloveTrainingStartsThePlayer()
    {
        Assert.False(FieldAssist.PlayerStartsOnGlove(false));
        Assert.True(FieldAssist.PlayerStartsOnGlove(true));
        Assert.True(FieldAssist.HumanOwnsThrow(true));
        Assert.False(FieldAssist.HumanOwnsThrow(false));
        var take = _content.Feel.FieldAssistStick;
        Assert.Equal(0.35, take);
        Assert.False(FieldAssist.StickTakesGlove(0, 0, take, false));
        Assert.False(FieldAssist.StickTakesGlove(0.1, 0.1, take, false));
        Assert.True(FieldAssist.StickTakesGlove(0.4, 0, take, false));
        Assert.True(FieldAssist.StickTakesGlove(0, 0, take, true));
        Assert.True(FieldAssist.StickDead(0, 0, take));
        Assert.False(FieldAssist.StickDead(0.4, 0, take));
        Assert.True(FieldAssist.CpuChases(hasBall: false, throwing: false, stickDead: true));
        Assert.False(FieldAssist.CpuChases(hasBall: true, throwing: false, stickDead: true),
            "with the ball they wait for the throw");
        Assert.False(FieldAssist.CpuChases(hasBall: false, throwing: true, stickDead: true));
        Assert.False(FieldAssist.CpuChases(hasBall: false, throwing: false, stickDead: false));
        Assert.True(FieldAssist.ShowYou(true, "SS"));
        Assert.False(FieldAssist.ShowYou(false, "SS"));
        Assert.False(FieldAssist.ShowYou(true, ""));
    }

    [Fact]
    public void CpuHopperResolvesWithoutASouthPress()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 7);
        Assert.True(match.Top);
        var hopper = new AtBatResult(ContactQuality.Nice, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
        var field = match.ResolveFielding(hopper);
        Assert.Equal(PlayKind.InPlay, field.Kind);
        Assert.NotNull(field.Fielder);
    }

    [Fact]
    public void CoverSpotsAreTheBagsNotTheDirtPads()
    {
        Assert.Equal(Diamond.First, FieldAssist.CoverSpot("1B"));
        Assert.Equal(Diamond.Second, FieldAssist.CoverSpot("2B"));
        Assert.Equal(Diamond.Third, FieldAssist.CoverSpot("3B"));
        Assert.Equal(Diamond.Home, FieldAssist.CoverSpot("C"));
        Assert.Equal(Diamond.Rubber, FieldAssist.CoverSpot("P"));
    }

    [Fact]
    public void TheCoverMapExcludesTheGloveAndThePitcherBackfills()
    {
        // §8.7: 1B covers first (2B when 1B is the glove), 2B / SS cover second (the one not on the
        // ball; both free, the one away from the ball's side), 3B third, C home, P backfills.
        Assert.Equal("2B", FieldAssist.CoverKey(2));
        Assert.Equal("", FieldAssist.CoverKey(0));
        var ss = InPlay.CoverMap("SS", -30);
        Assert.Equal(("1B", "2B", "3B", "C"), (ss[1], ss[2], ss[3], ss[4]));
        var second = InPlay.CoverMap("2B", 30);
        Assert.Equal("SS", second[2]);
        var first = InPlay.CoverMap("1B", 40);
        Assert.Equal("2B", first[1]);
        Assert.Equal("SS", first[2]);
        var third = InPlay.CoverMap("3B", -40);
        Assert.Equal("SS", third[3]);
        Assert.Equal("2B", third[2]);
        var catcher = InPlay.CoverMap("C", 0);
        Assert.Equal("P", catcher[4]);
        var pitcher = InPlay.CoverMap("P", 5);
        Assert.Equal(("1B", "SS", "3B", "C"), (pitcher[1], pitcher[2], pitcher[3], pitcher[4]));
        Assert.Equal("2B", InPlay.CoverMap("LF", -80)[2]);
        Assert.Equal("SS", InPlay.CoverMap("RF", 80)[2]);
    }

    [Fact]
    public void TheCutoffIsTheInfielderOnTheLineAndTheBackupStandsBehindTheTarget()
    {
        var spots = new Dictionary<string, (double X, double Z)>();
        foreach (var kv in Diamond.Positions) spots[kv.Key] = kv.Value;
        // LF throwing home: SS is on the line; 2B is not (§8.7). RF throwing home: 1B or 2B.
        var lf = Diamond.Positions["LF"];
        var cut = InPlay.CutoffFor(lf.X, lf.Z, 0, 0, spots, "LF", "C");
        Assert.NotNull(cut);
        Assert.Equal("SS", cut!.Value.Pos);
        var rf = Diamond.Positions["RF"];
        var cutR = InPlay.CutoffFor(rf.X, rf.Z, 0, 0, spots, "RF", "C");
        Assert.NotNull(cutR);
        Assert.True(cutR!.Value.Pos is "1B" or "2B", cutR.Value.Pos);
        // The cover of the bag and the glove are never the cutoff.
        var cf = Diamond.Positions["CF"];
        var toThird = InPlay.CutoffFor(cf.X, cf.Z, Diamond.Third.X, Diamond.Third.Z, spots, "CF", "3B");
        Assert.NotNull(toThird);
        Assert.NotEqual("3B", toThird!.Value.Pos);
        // Nobody stands between the mound and the plate.
        var ss = Diamond.Positions["SS"];
        Assert.Null(InPlay.CutoffFor(Diamond.Rubber.X, Diamond.Rubber.Z, 0, 0, spots, "P", "C"));
        // The backup spot is 60 ft past the target on the throw line; the pitcher backs up first and home.
        var behind = InPlay.BackupSpot(lf.X, lf.Z, 0, 0, 60);
        Assert.InRange(Diamond.Dist(0, 0, behind.X, behind.Z), 59.9, 60.1);
        Assert.Equal("P", InPlay.BackupPos(4, behind.X, behind.Z, spots, "LF", "C"));
        Assert.Equal("", InPlay.BackupPos(1, 0, 0, spots, "P", "1B"));
        // A throw from short toward second carries on toward right-center: the right fielder backs it up.
        var behindSecond = InPlay.BackupSpot(ss.X, ss.Z, Diamond.Second.X, Diamond.Second.Z, 60);
        Assert.Equal("RF", InPlay.BackupPos(2, behindSecond.X, behindSecond.Z, spots, "SS", "2B"));
        var behindSecondFromRight = InPlay.BackupSpot(Diamond.Positions["1B"].X, Diamond.Positions["1B"].Z, Diamond.Second.X, Diamond.Second.Z, 60);
        Assert.Equal("LF", InPlay.BackupPos(2, behindSecondFromRight.X, behindSecondFromRight.Z, spots, "1B", "2B"));
    }

    [Fact]
    public void AThrowLandsByItsLateralMissAndIsCaughtInsideTheCoverRadius()
    {
        var rules = Rules.Default;
        var to = Diamond.First;
        var straight = InPlay.ThrowLanding(-42, 118, to.X, to.Z, 0);
        Assert.Equal(to.X, straight.X, 6);
        Assert.Equal(to.Z, straight.Z, 6);
        var off = InPlay.ThrowLanding(-42, 118, to.X, to.Z, 12);
        Assert.InRange(Diamond.Dist(off.X, off.Z, to.X, to.Z), 11.9, 12.1);
        Assert.True(InPlay.ThrowCaught(straight.X, straight.Z, to.X, to.Z, rules));
        Assert.False(InPlay.ThrowCaught(off.X, off.Z, to.X, to.Z, rules), "a miss past the cover radius skips by (§8.5)");
        var inside = InPlay.ThrowLanding(-42, 118, to.X, to.Z, rules.Fielding.Cover.RadiusFt - 0.5);
        Assert.True(InPlay.ThrowCaught(inside.X, inside.Z, to.X, to.Z, rules));
    }

    [Fact]
    public void SwapGloveIsTowardStickOrNextNearestToBallNotDiamondOrder()
    {
        var at = new Dictionary<string, (double X, double Z)>
        {
            ["SS"] = Diamond.Positions["SS"],
            ["2B"] = Diamond.Positions["2B"],
            ["1B"] = Diamond.Positions["1B"],
            ["3B"] = Diamond.Positions["3B"],
            ["P"] = Diamond.Positions["P"],
        };
        var ball = Diamond.Positions["SS"];
        var take = _content.Feel.FieldAssistStick;
        Assert.Equal("2B", FieldAssist.SwapGlove("SS", at, ball.X, ball.Z, 1, 0, take));
        Assert.Equal("3B", FieldAssist.SwapGlove("SS", at, ball.X, ball.Z, -1, 0, take));
        Assert.Equal("1B", FieldAssist.SwapGlove("P", at, ball.X, ball.Z, 1, -0.4, take));
        var nearSecond = (Diamond.Positions["2B"].X, Diamond.Positions["2B"].Z + 4);
        Assert.Equal("2B", FieldAssist.SwapGlove("SS", at, nearSecond.Item1, nearSecond.Item2, 0, 0, take));
        Assert.Equal("3B", FieldAssist.SwapGlove("SS", at, ball.X, ball.Z, 0, 0, take));
        Assert.Equal(
            FieldAssist.SwapGlove("SS", at, nearSecond.Item1, nearSecond.Item2, 0, 0, take),
            FieldAssist.SwitchHint("SS", at, nearSecond.Item1, nearSecond.Item2, 0, 0, take));
    }
}
