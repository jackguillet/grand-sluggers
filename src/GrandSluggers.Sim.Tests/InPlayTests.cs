using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class InPlayTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void EnergyScalesWithExitAndQuality()
    {
        var soft = Hit(ContactQuality.Cheap, 60);
        var hard = Hit(ContactQuality.Perfect, 100);
        Assert.True(InPlay.Energy(hard) > InPlay.Energy(soft),
            $"hard {InPlay.Energy(hard)} vs soft {InPlay.Energy(soft)}");
    }

    [Fact]
    public void HighEnergyBobblesMoreThanADyingRoller()
    {
        var rio = _content.Must("rio");
        var hard = Hit(ContactQuality.Perfect, 110);
        var dying = Hit(ContactQuality.Cheap, 40);
        var hardN = 0;
        var dyingN = 0;
        const int n = 80;
        for (var i = 0; i < n; i++)
        {
            if (InPlay.Bobbles(InPlay.Energy(hard), rio, new Random(i))) hardN++;
            if (InPlay.Bobbles(InPlay.Energy(dying), rio, new Random(i))) dyingN++;
        }
        Assert.True(hardN > dyingN, $"hard bobbles {hardN} vs dying {dyingN}");
        Assert.Equal(0, dyingN);
    }

    [Fact]
    public void FastBatterBeatsASlowThrow()
    {
        var dart = _content.Must("dart");
        var brick = _content.Must("brondo");
        Assert.True(InPlay.HomeToFirstSec(dart) < InPlay.HomeToFirstSec(brick),
            $"dart {InPlay.HomeToFirstSec(dart)} vs brondo {InPlay.HomeToFirstSec(brick)}");

        var hit = Hit(ContactQuality.Solid, 72, launch: 8, carry: 45);
        var slow = new ThrowResult(Chemistry.Bad, 0.55, false);
        var field = new FieldingResult(PlayKind.GroundOut, _content.Must("vale"), _content.Must("nico"),
            0.4, -40, 90, false, false, slow);
        Assert.True(InPlay.BatterBeatsThrow(dart, hit, field));

        var laser = new ThrowResult(Chemistry.Good, 1.6, false);
        var outPlay = field with { HangTimeSec = 1.6, LandingX = 50, LandingZ = 70, Throw = laser };
        Assert.False(InPlay.BatterBeatsThrow(brick, hit, outPlay));
    }

    [Fact]
    public void ScoopMissIsASingleNotASilentGroundOut()
    {
        var match = Match.Slice(_content, seed: 4);
        var fielding = new FieldingResolver(_content.Chemistry);
        // Deep hopper: landing is past the infield so the nearest glove cannot scoop it.
        var hit = new AtBatResult(ContactQuality.Solid, true, false, 72, 8, 360, false, false, null, null, SprayDeg: 2);
        var rng = new Random(4);
        var pre = fielding.Preview(hit, match.Park, match.Defense.Roster, match.Pitcher, rng);
        Assert.True(pre.Grounder, "launch 8 must be a hopper");
        var field = fielding.Resolve(hit, match.Park, match.Defense.Roster, match.Pitcher, rng, pre: pre);
        Assert.Equal(PlayKind.Single, field.Kind);
        Assert.NotEqual(PlayKind.GroundOut, field.Kind);
        Assert.False(InPlay.BatterBeatsThrow(match.Batter, hit, field));
    }

    [Fact]
    public void GroundThrowBagsForceThenFirstOnADoublePlayRace()
    {
        Assert.Equal(new[] { 2, 1 }, InPlay.GroundThrowBags(true, false));
        Assert.Equal(new[] { 2 }, InPlay.GroundThrowBags(true, true));
        Assert.Equal(new[] { 1 }, InPlay.GroundThrowBags(false, false));
        Assert.Empty(InPlay.GroundThrowBags(false, true));
        Assert.Equal(new[] { 3 }, InPlay.GroundThrowBags(false, true, false, false));
        Assert.Equal(new[] { 4 }, InPlay.GroundThrowBags(false, true, true, false));
        Assert.Equal(4, InPlay.TagBag(true, true));
        Assert.Equal(3, InPlay.TagBag(true, false));
        Assert.Equal(0, InPlay.TagBag(false, false));
    }

    [Fact]
    public void HardHopperCanBobbleIntoASingle()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var hit = new AtBatResult(ContactQuality.Perfect, true, false, 110, 8, 45, false, false, null, null, SprayDeg: 2);
        var bobbles = 0;
        var outs = 0;
        for (var i = 0; i < 80; i++)
        {
            var field = fielding.Resolve(hit, match.Park, match.Defense.Roster, match.Pitcher, new Random(i));
            if (field.Bobble)
            {
                bobbles++;
                Assert.Equal(PlayKind.Single, field.Kind);
                Assert.Equal(0, field.KnockbackSec);
            }
            else if (field.Kind == PlayKind.GroundOut)
            {
                outs++;
                Assert.False(field.Bobble);
                Assert.True(field.KnockbackSec > 0, "a 110 mph perfect hopper must shove the fielder");
            }
        }
        Assert.True(bobbles > 0, "a rocket at the shins must eat someone in 80 tries");
        Assert.True(outs > 0, "the same rocket is still an out when the glove holds");
    }

    [Fact]
    public void DyingRollerDoesNotBobbleOrKnockBack()
    {
        var match = Match.Slice(_content, seed: 2);
        var fielding = new FieldingResolver(_content.Chemistry);
        var hit = new AtBatResult(ContactQuality.Cheap, true, false, 40, 6, 30, false, false, null, null, SprayDeg: 0);
        for (var i = 0; i < 40; i++)
        {
            var field = fielding.Resolve(hit, match.Park, match.Defense.Roster, match.Pitcher, new Random(i));
            Assert.False(field.Bobble);
            Assert.Equal(0, field.KnockbackSec);
        }
    }

    [Fact]
    public void TheaterShotSplitsGrounderPullAndFly()
    {
        var hopper = new AtBatResult(ContactQuality.Solid, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
        var pull = hopper with { SprayDeg = -20 };
        var fly = hopper with { LaunchDeg = 32, CarryFt = 280 };
        var homer = hopper with { LaunchDeg = 32, CarryFt = 420, HomeRun = true };
        var line = hopper with { LaunchDeg = 18, ExitVeloMph = 95, CarryFt = 180 };
        var star = hopper with { LaunchDeg = 28, StarSwingUsed = "heat-swing" };
        Assert.Equal("diamond-grounder", InPlay.TheaterShot(hopper));
        Assert.Equal("diamond-pull", InPlay.TheaterShot(pull));
        Assert.Equal("diamond-line", InPlay.TheaterShot(line));
        Assert.Equal("diamond", InPlay.TheaterShot(fly));
        Assert.Equal("diamond-homer", InPlay.TheaterShot(homer));
        Assert.Equal("smash", InPlay.TheaterShot(star));
        Assert.True(FieldingResolver.IsLine(line));
        Assert.True(FieldingResolver.IsGrounder(hopper));
        Assert.False(FieldingResolver.IsGrounder(fly));
    }

    static AtBatResult Hit(ContactQuality q, double exit, double launch = 22, double carry = 200) =>
        new(q, true, false, exit, launch, carry, false, false, null, null);
}
