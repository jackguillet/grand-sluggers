using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Solid bodies and the timed mover (§14; F4-f, FD-09, <c>SF-28</c>): the ball caroms off a body below its top, a fielder
/// cannot stand in one and the route goes around it, and a mover's place is a function of the play clock alone.
/// </summary>
public sealed class SolidBodyTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;
    static RulesTable Rules => Catalog.Rules;
    static SolidBody Tree(double x, double z) => new(0, HazardType.Tree, x, z, 6, Rules.Hazards.Of(HazardType.Tree));

    /// <summary><c>SF-28</c>: a ball rolling straight into a body comes back at the row's restitution; one over its top passes; one leaving is left alone.</summary>
    [Fact]
    public void SF28_TheBallCaromsOffABodyBelowItsTop()
    {
        var tree = Tree(0, 100);
        var e = tree.Row.Restitution!.Value;
        var hit = SolidBodies.Carom([tree], 0, 0, 1, 95, 0, 40);
        Assert.NotNull(hit);
        Assert.Equal((0.0, 94.0), (hit!.Value.X, hit.Value.Z));
        Assert.Equal(-40 * e, hit.Value.Vz, 9);
        Assert.Equal(0, hit.Value.Vx, 9);
        Assert.Null(SolidBodies.Carom([tree], 0, 0, tree.Row.HeightFt!.Value + 1, 95, 0, 40));
        Assert.Null(SolidBodies.Carom([tree], 0, 0, 1, 95, 0, -40));
        // A glancing ball keeps its speed along the body.
        var glance = SolidBodies.Carom([tree], 0, 5, 1, 97, 10, 30)!.Value;
        Assert.True(glance.Vx > 0);
    }

    /// <summary><c>SF-28</c>: a point inside a body is pushed to the rim; one outside is left where it is.</summary>
    [Fact]
    public void SF28_NobodyStandsInABody()
    {
        var tree = Tree(0, 100);
        var (x, z) = SolidBodies.PushOut([tree], 0, 0, 97);
        Assert.Equal((0.0, 94.0), (x, z), new NearPoint());
        Assert.Equal((0.0, 80.0), SolidBodies.PushOut([tree], 0, 0, 80));
    }

    /// <summary><c>SF-28</c>: the route to a goal past a body always goes around it, however little the detour saves.</summary>
    [Fact]
    public void SF28_TheRouteGoesAroundABody()
    {
        var tree = Tree(0, 50);
        var plan = VolumeRoute.Plan((0, 0), (0, 100), [tree.AsVolume(0)], 25, 1e-6, Rules.Fielding.Chase.VolumeClearFt);
        Assert.True(plan.Around);
        var at = (X: 0.3, Z: 0.0);
        for (var i = 0; i < 600; i++)
        {
            var w = VolumeRoute.Waypoint(at, (0, 100), [tree.AsVolume(0)], 25, 1e-6, Rules.Fielding.Chase.VolumeClearFt);
            at = FieldingResolver.StepToward(at.X, at.Z, w.X, w.Z, 25, 1.0 / 60, rules: Rules);
            Assert.True(Diamond.Dist(0, 50, at.X, at.Z) > tree.RadiusFt, $"inside at frame {i}");
            if (Diamond.Dist(at.X, at.Z, 0, 100) < 0.5) break;
        }
    }

    /// <summary>
    /// <c>SF-28</c>: the train's place is a function of the play clock — its spot at 0 and at every whole period, the row's
    /// run to either side along the fence at the quarters — so the same second is always the same place.
    /// </summary>
    [Fact]
    public void SF28_TheMoversPlaceIsAFunctionOfThePlayClock()
    {
        var funfair = Catalog.MustPark(ParkIds.Funfair);
        var train = Assert.Single(SolidBodies.Of(funfair, Rules), b => b.Moves);
        var row = train.Row;
        Assert.Equal((train.X, train.Z), train.At(0));
        var p = row.PeriodSec!.Value;
        Assert.Equal(train.X, train.At(p).X, 9);
        Assert.Equal(train.Z, train.At(p).Z, 9);
        var q = train.At(p / 4);
        Assert.Equal(row.TravelFt!.Value, Diamond.Dist(train.X, train.Z, q.X, q.Z), 9);
        // Along the fence: at right angles to its bearing from home.
        var (rx, rz) = (train.X, train.Z);
        Assert.Equal(0, (q.X - train.X) * rx + (q.Z - train.Z) * rz, 6);
        Assert.Equal(train.At(3.3), train.At(3.3));
    }

    /// <summary>The shipped bodies: three types of solid body in three parks, and Funfair's train.</summary>
    [Fact]
    public void EveryBodyParkListsItsBodies()
    {
        Assert.Single(SolidBodies.Of(Catalog.MustPark(ParkIds.Ember), Rules));
        Assert.Single(SolidBodies.Of(Catalog.MustPark(ParkIds.Rooftop), Rules));
        Assert.Equal(4, SolidBodies.Of(Catalog.MustPark(ParkIds.Canopy), Rules).Count);
        Assert.Empty(SolidBodies.Of(Catalog.MustPark(ParkIds.Harbor), Rules));
    }

    sealed class NearPoint : IEqualityComparer<(double, double)>
    {
        public bool Equals((double, double) a, (double, double) b) => Math.Abs(a.Item1 - b.Item1) < 1e-9 && Math.Abs(a.Item2 - b.Item2) < 1e-9;
        public int GetHashCode((double, double) p) => 0;
    }
}
