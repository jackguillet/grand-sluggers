using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// A loose ball is one kind at a time (§8.6): a local bobble falls, rebounds and rolls; everything else that comes loose rolls
/// as an overthrow. A bobble ends when a glove takes the ball back or the ball comes loose again, so a later overthrow in the
/// same play rolls exactly as an overthrow with no bobble before it (#1100).
/// </summary>
public sealed class LooseBallMotionTests
{
    const double Frame = 1.0 / 60;
    static readonly RulesTable Table = Shipped.Content.Rules;
    static readonly GroundZones Zones = GroundZones.Of(Shipped.Content.MustPark(ParkId.Harbor), Table);

    /// <summary>A bobble in the air off a hot take, run for half a second, the way a fumbled grounder spills.</summary>
    static (double X, double Y, double Z) Bobbled(LooseBallMotion m, double x, double z)
    {
        m.Bobble(3, -2, inTheAir: true);
        var at = (X: x, Y: 2.5, Z: z);
        for (var i = 0; i < 30; i++)
            if (m.Tick(at.X, at.Y, at.Z, Frame, i * Frame, Zones, Table) is { } next) at = next;
        return at;
    }

    /// <summary>Both balls, loose from one spot at one speed, frame by frame until both stop.</summary>
    static void RollsTheSame(LooseBallMotion after, LooseBallMotion fresh, (double X, double Z) from, double t0)
    {
        var a = (X: from.X, Y: 0.0, Z: from.Z);
        var b = a;
        for (var i = 1; i <= 600; i++)
        {
            var t = t0 + i * Frame;
            var na = after.Tick(a.X, a.Y, a.Z, Frame, t, Zones, Table);
            var nb = fresh.Tick(b.X, b.Y, b.Z, Frame, t, Zones, Table);
            Assert.Equal(nb, na);
            if (na is { } pa) a = pa;
            if (nb is { } pb) b = pb;
            Assert.Equal(0.0, a.Y);
        }
        Assert.True(after.RestedFor(t0 + 600 * Frame, 0), "the overthrow came to rest");
    }

    [Fact]
    public void AnOverthrowAfterABobbleTheGloveTookBackRollsAsAnOverthrow()
    {
        var after = new LooseBallMotion();
        Bobbled(after, 20, 70);
        after.Held();
        Assert.False(after.Local, "the glove taking the ball back ends the bobble");

        // Later in the play the throw sails past the bag at first and rolls on.
        var fresh = new LooseBallMotion();
        after.Roll(46, 18, 3.0);
        fresh.Roll(46, 18, 3.0);
        Assert.False(after.Local);
        RollsTheSame(after, fresh, (56, 58), 3.0);
    }

    [Fact]
    public void ABallKnockedLooseDuringABobbleRollsAsAnOverthrow()
    {
        var after = new LooseBallMotion();
        var at = Bobbled(after, 20, 70);
        var fresh = new LooseBallMotion();
        after.Roll(0, 0, 1.0);
        fresh.Roll(0, 0, 1.0);
        Assert.False(after.Local, "a ball that comes loose again is an overthrow's roll, not the bobble's");
        RollsTheSame(after, fresh, (at.X, at.Z), 1.0);
    }

    [Fact]
    public void ABobbleIsStillABobbleUntilItEnds()
    {
        var m = new LooseBallMotion();
        Bobbled(m, 20, 70);
        Assert.True(m.Local);
        Assert.True(m.Active);
    }
}
