using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FoulTests
{
    readonly ContentCatalog _content = Shipped.Content;

    AtBatInput Square(double sprayAim, double timing = 0, bool starSwing = false) =>
        new(_content.Must("vale"), _content.Must("rio"), _content.Must("nico"), [],
            false, false, timing, false, starSwing,
            _content.Bats["harbor-lumber"], 80, SprayAimDeg: sprayAim, PitchInZone: true);

    /// <summary>
    /// A press at the early edge of the window: timing pulls the ball ≈ <c>spray.timingDeg</c> toward
    /// the pull line (§5.3), past the chalk. An ordinary swing ignores the stick since #883, so the
    /// 60° aim that was the device here played only on the switch's off path, retired by #887.
    /// </summary>
    double EarlyEdge(Park park) =>
        -(AtBatResolver.ContactWindowFrames(null, park, false, _content.Rules, _content.StarSkills) / 2 - 0.05);

    [Fact]
    public void SprayPastTheFoulLineIsFoulNotInPlay()
    {
        var park = _content.Parks["harbor-diamond"];
        var r = new AtBatResolver(_content.Chemistry, _content.Rules, _content.StarSkills).Resolve(Square(0, timing: EarlyEdge(park)), park, new Random(1));
        Assert.True(r.Foul);
        Assert.False(r.InPlay);
        Assert.False(r.HomeRun);
        Assert.Equal(BattedBallClass.Foul, BattedBall.Of(r, park, rules: Rules.Default).Class);
        var ball = BattedBall.Of(r, park, rules: Rules.Default);
        Assert.False(FieldBounds.IsFair(ball.DecidedX, ball.DecidedZ), "the untouched ball is judged where it lands or rolls (§5.6)");
        Assert.True(r.ExitVeloMph > 1);
    }

    [Fact]
    public void SquareContactUpTheMiddleIsFair()
    {
        var park = _content.Parks["harbor-diamond"];
        for (var seed = 0; seed < 40; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry, rules: Rules.Default).Resolve(Square(0), park, new Random(seed));
            Assert.False(r.Foul, $"seed {seed} spray {r.SprayDeg} labeled foul inside the lines");
            Assert.True(r.InPlay);
            Assert.NotEqual(BattedBallClass.Foul, BattedBall.Of(r, park, rules: Rules.Default).Class);
        }
    }

    [Fact]
    public void PastThePoleIsFoulNotAHomer()
    {
        var park = _content.Parks["harbor-diamond"];
        var input = new AtBatInput(
            _content.Must("vale"), _content.Must("ashlord"), _content.Must("cinder"), [],
            false, false, 0, false, true,
            _content.Bats["furnace-club"], 80, SprayAimDeg: 70, PitchInZone: true, Charge01: 1);
        var carry = 0.0;
        for (var seed = 0; seed < 20; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry, rules: Rules.Default).Resolve(input, park, new Random(seed));
            Assert.True(r.Foul, $"seed {seed} spray {r.SprayDeg}");
            Assert.False(r.HomeRun, $"seed {seed} homer in foul territory carry {r.CarryFt}");
            Assert.False(r.InPlay);
            carry = Math.Max(carry, BallFlight.CarryFeet(r.ExitVeloMph, r.LaunchDeg, 0, rules: Rules.Default));
        }
        Assert.True(carry > park.RightFenceFt * 0.6, $"expected a real fly, best open carry {carry}");
    }

    [Fact]
    public void FoulIsAStrikeUnlessTwo()
    {
        // A press at the early edge of the window is the device that makes each swing foul (EarlyEdge).
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var paint = new PitchCommand("fastball", 0, false);
        var pull = new SwingCommand(true, 0, EarlyEdge(match.Park), false);
        var batter = match.Batter.Id;

        // A foul is a live ball (§7.11): contact enters play, and the play commits FOUL when the ball is dead.
        PlayEvent FoulPlay()
        {
            Assert.True(match.BeginAtBat(paint, pull, out var hit, out _), "a foul flight is live from contact");
            Assert.True(hit.Foul);
            var preview = match.PreviewHit(hit);
            var dead = new FieldingResult(PlayKind.Foul, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false);
            return match.FinishAtBat(paint, pull, hit, dead);
        }

        var first = FoulPlay();
        Assert.Equal(PlayKind.Foul, first.Kind);
        Assert.Equal("Foul.", first.Caption);
        Assert.Equal(1, match.Strikes);
        Assert.Equal(0, match.Outs);
        Assert.Equal(batter, match.Batter.Id);

        var second = FoulPlay();
        Assert.Equal(PlayKind.Foul, second.Kind);
        Assert.Equal(2, match.Strikes);

        var third = FoulPlay();
        Assert.Equal(PlayKind.Foul, third.Kind);
        Assert.Equal(2, match.Strikes);
        Assert.Equal(0, match.Outs);
        Assert.Equal(batter, match.Batter.Id);
        Assert.False(InPlay.FairContactSendsBatter(third.AtBat));
        Assert.Equal(0, third.Outcome?.BatterToBag);
    }

    [Fact]
    public void FullStickShiftsTheRangeNotTheWholeField()
    {
        // Spec §5.3: the stick shifts direction by ±12°; timing across the window does the rest.
        Assert.Equal(Rules.Default.Batting.Spray.StickDeg, AtBatResolver.SprayAimDeg(1, rules: Rules.Default));
        Assert.True(AtBatResolver.SprayAimDeg(1, rules: Rules.Default) < AtBatResolver.FoulLineDeg);
        Assert.Equal(-AtBatResolver.SprayAimDeg(1, rules: Rules.Default), AtBatResolver.SprayAimDeg(-1, rules: Rules.Default));
        Assert.Equal(0, AtBatResolver.SprayAimDeg(0, rules: Rules.Default));
    }

    [Fact]
    public void SourPullFliesIntoFoulTerritory()
    {
        // A sour swing (the handle side of the bat) pulled to the pull line by an early press.
        var park = _content.Parks["harbor-diamond"];
        var fouls = 0;
        var bats = _content.Must("rio").Bats;
        var pullSide = -SweetSpot.TipSign(bats);
        for (var seed = 0; seed < 80; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry, rules: Rules.Default).Resolve(
                Square(AtBatResolver.SprayAimDeg(pullSide, rules: Rules.Default), timing: -3.5) with
                    { CrossingX = -SweetSpot.TipSign(bats) * 0.95, CrossingY = StrikeZoneGeometry.CenterY },
                park, new Random(seed));
            if (r.Quality == ContactQuality.Miss) continue;
            var ball = BattedBall.Of(r, park, rules: Rules.Default);
            if (r.Foul)
            {
                fouls++;
                Assert.False(FieldBounds.IsFair(ball.DecidedX, ball.DecidedZ),
                    $"foul spray {r.SprayDeg} judged fair at ({ball.DecidedX:0},{ball.DecidedZ:0})");
            }
            else
                Assert.True(FieldBounds.IsFair(ball.DecidedX, ball.DecidedZ));
        }
        Assert.True(fouls > 8, $"expected sitting-visible fouls off a pulled sour swing, got {fouls}");
    }

    [Fact]
    public void FairContactSendsTheBatterFoulDoesNot()
    {
        var fair = new AtBatResult(ContactQuality.Nice, true, false, 90, 18, 180, false, false, null, null, 12);
        var foul = fair with { InPlay = false, Foul = true, SprayDeg = 52 };
        Assert.True(InPlay.FairContactSendsBatter(fair));
        Assert.False(InPlay.FairContactSendsBatter(foul));
    }
}
