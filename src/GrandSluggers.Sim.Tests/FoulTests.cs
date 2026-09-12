using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FoulTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    AtBatInput Square(double sprayAim, double timing = 0, bool starSwing = false) =>
        new(_content.Must("vale"), _content.Must("rio"), _content.Must("nico"), [],
            false, false, timing, false, starSwing,
            _content.Bats["harbor-lumber"], 80, SprayAimDeg: sprayAim, PitchInZone: true);

    [Fact]
    public void SprayPastTheFoulLineIsFoulNotInPlay()
    {
        var park = _content.Parks["harbor-diamond"];
        var r = new AtBatResolver(_content.Chemistry).Resolve(Square(60), park, new Random(1));
        Assert.True(r.Foul);
        Assert.False(r.InPlay);
        Assert.False(r.HomeRun);
        Assert.Equal(BattedBallClass.Foul, BattedBall.Of(r, park).Class);
        var ball = BattedBall.Of(r, park);
        Assert.False(FieldBounds.IsFair(ball.DecidedX, ball.DecidedZ), "the untouched ball is judged where it lands or rolls (§5.6)");
        Assert.True(r.ExitVeloMph > 1);
    }

    [Fact]
    public void SquareContactUpTheMiddleIsFair()
    {
        var park = _content.Parks["harbor-diamond"];
        for (var seed = 0; seed < 40; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry).Resolve(Square(0), park, new Random(seed));
            Assert.False(r.Foul, $"seed {seed} spray {r.SprayDeg} labeled foul inside the lines");
            Assert.True(r.InPlay);
            Assert.NotEqual(BattedBallClass.Foul, BattedBall.Of(r, park).Class);
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
            var r = new AtBatResolver(_content.Chemistry).Resolve(input, park, new Random(seed));
            Assert.True(r.Foul, $"seed {seed} spray {r.SprayDeg}");
            Assert.False(r.HomeRun, $"seed {seed} homer in foul territory carry {r.CarryFt}");
            Assert.False(r.InPlay);
            carry = Math.Max(carry, BallFlight.CarryFeet(r.ExitVeloMph, r.LaunchDeg, 0));
        }
        Assert.True(carry > park.RightFenceFt * 0.6, $"expected a real fly, best open carry {carry}");
    }

    [Fact]
    public void FoulIsAStrikeUnlessTwo()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var paint = new PitchCommand("fastball", 0, 0, false);
        var pull = new SwingCommand(true, 0, 0, false, SprayAimDeg: 60);
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
        Assert.Equal(Rules.Default.Batting.Spray.StickDeg, AtBatResolver.SprayAimDeg(1));
        Assert.True(AtBatResolver.SprayAimDeg(1) < AtBatResolver.FoulLineDeg);
        Assert.Equal(-AtBatResolver.SprayAimDeg(1), AtBatResolver.SprayAimDeg(-1));
        Assert.Equal(0, AtBatResolver.SprayAimDeg(0));
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
            var r = new AtBatResolver(_content.Chemistry).Resolve(
                Square(AtBatResolver.SprayAimDeg(pullSide), timing: -3.5) with
                    { CrossingX = -SweetSpot.TipSign(bats) * 0.95, CrossingY = StrikeZoneGeometry.CenterY },
                park, new Random(seed));
            if (r.Quality == ContactQuality.Miss) continue;
            var ball = BattedBall.Of(r, park);
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
