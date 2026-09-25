using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// A rules table's diamond (§8, §16): a match, a trial or a test that builds its own infield table plays its own bags and
/// starts, not the process table's (#1067). The process-wide <see cref="Diamond"/> is the default table's diamond to the bit.
/// </summary>
public sealed class DiamondGeometryTests
{
    static readonly RulesTable Table = Rules.Default;

    /// <summary>A table with a wider infield: 90-ft corners and a deeper second, a farther rubber.</summary>
    static RulesTable Wider() => Table with
    {
        Infield = Table.Infield with
        {
            CornerFt = Table.Infield.CornerFt * 1.125,
            SecondFt = Table.Infield.SecondFt * 1.125,
            MoundFt = Table.Infield.MoundFt + 3,
            BaselineFt = Table.Infield.BaselineFt * 1.125
        }
    };

    [Fact]
    public void TheProcessDiamondIsTheDefaultTablesDiamondToTheBit()
    {
        var d = DiamondGeometry.Of(Table);
        Assert.Equal(d.Baseline, Diamond.Baseline);
        Assert.Equal(d.Mound, Diamond.Mound);
        Assert.Equal(d.Rubber, Diamond.Rubber);
        for (var bag = 0; bag <= 3; bag++) Assert.Equal(d.Bag(bag), Diamond.Bag(bag));
        Assert.Equal(Diamond.Order, d.Positions.Keys);
        foreach (var pos in Diamond.Order) Assert.Equal(d.Positions[pos], Diamond.Positions[pos]);
    }

    [Fact]
    public void TheBagsAndTheRubberAreTheTablesInfield()
    {
        var t = Wider();
        var d = DiamondGeometry.Of(t);
        Assert.Equal((t.Infield.CornerFt, t.Infield.CornerFt), d.First);
        Assert.Equal((0.0, t.Infield.SecondFt), d.Second);
        Assert.Equal((-t.Infield.CornerFt, t.Infield.CornerFt), d.Third);
        Assert.Equal((0.0, 0.0), d.Home);
        Assert.Equal((0.0, t.Infield.MoundFt), d.Rubber);
        Assert.Equal(d.Rubber, d.Positions["P"]);
        Assert.Equal(t.Infield.BaselineFt, d.Baseline);
        Assert.NotEqual(Diamond.First, d.First);
    }

    [Fact]
    public void TheStartsAreTheTablesFieldersAndTheCatcherStandsAtHomeSet()
    {
        var d = DiamondGeometry.Of(Table);
        foreach (var pos in new[] { "1B", "2B", "3B", "SS", "LF", "CF", "RF" }) Assert.Equal(Table.Fielders.Spot(pos), d.Positions[pos]);
        Assert.Equal((0.0, HomeSet.CatcherZ), d.Positions["C"]);
    }

    [Fact]
    public void OneTableIsBuiltOnceAndTwoTablesNeverShareADiamond()
    {
        var t = Wider();
        Assert.Same(DiamondGeometry.Of(t), DiamondGeometry.Of(t));
        Assert.NotSame(DiamondGeometry.Of(Table), DiamondGeometry.Of(t));
    }

    /// <summary>A park's starts are cached per table as well: a second table with the same park reads its own pitcher, never the first table's.</summary>
    [Fact]
    public void AParksStartsAreThatTablesStarts()
    {
        var park = global::GrandSluggers.Sim.Tests.Shipped.Content.MustPark(ParkId.Harbor);
        var t = Wider();
        Assert.Equal(Diamond.Rubber, OutfieldStarts.Of(park, Table)["P"]);
        Assert.Equal(DiamondGeometry.Of(t).Rubber, OutfieldStarts.Of(park, t)["P"]);
    }

    /// <summary>A runner runs the bags of the table it was seated with: its spot, its path length and its progress are that table's.</summary>
    [Fact]
    public void ARunnerRunsItsTablesBags()
    {
        var t = Wider();
        var d = DiamondGeometry.Of(t);
        var who = Shipped.Content.Must("rio");
        var onFirst = new Runner(who, 1, t);
        Assert.Equal(d.First, onFirst.Position);
        Assert.Equal(d.Baseline, onFirst.SegmentFt);
        Assert.Equal(d.Baseline, onFirst.Progress);
        var shipped = new Runner(who, 1, Table);
        Assert.Equal(Diamond.First, shipped.Position);
        Assert.NotEqual(shipped.Position, onFirst.Position);
    }

    /// <summary>A grounder is judged at the bag circle of the table it plays (§5.6), not the process table's.</summary>
    [Fact]
    public void TheFairFoulBagCircleIsTheTablesBaseline()
    {
        var t = Wider();
        var between = (Table.Infield.BaselineFt + t.Infield.BaselineFt) / 2;
        var (x, z) = (between * Math.Sin(Math.PI / 4), between * Math.Cos(Math.PI / 4));
        Assert.True(FieldBounds.PastTheBags(x, z, Table));
        Assert.False(FieldBounds.PastTheBags(x, z, t));
    }

    /// <summary>The flight's edge is the table's boundary: a wider foul offset moves the rail the ball meets.</summary>
    [Fact]
    public void TheFlightsEdgeIsTheTablesBoundary()
    {
        var park = Shipped.Content.MustPark(ParkId.Harbor);
        var t = Table with { Boundary = Table.Boundary with { FoulOffsetFt = Table.Boundary.FoulOffsetFt + 6 } };
        Assert.Equal(ParkBoundary.For(park, Table), ParkBoundary.For(park));
        Assert.NotEqual(ParkBoundary.For(park, Table), ParkBoundary.For(park, t));
        Assert.NotSame(FieldBounds.Of(park, Table), FieldBounds.Of(park, t));
    }
}
