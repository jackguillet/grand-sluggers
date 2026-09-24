using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-3 slice 1 (#719, F693-02-catch-reach-envelope and Jack's air-multiplier addition): the stand-up reach is the authored
/// 6 ft for every body without its own <c>reachFt</c>, the dirt pad and the dive add to it (6 / 10 / 14), and an outfielder
/// under a fly runs at the air multiplier.
/// </summary>
public sealed class CatchReachTests
{
    static readonly ContentCatalog Game = Shipped.Content;

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheStandUpReachIsSixFeetAndAnAuthoredReachWins()
    {
        Assert.Equal(4.0, Game.Rules.Fielding.Catch.StandUpReachFt);

        var ashlord = Game.Must("ashlord");   // Field 3, spin-check: no catch bonus
        Assert.Equal(3, ashlord.Stats.Field);
        Assert.Null(ashlord.ReachFt);
        Assert.Equal(4.0, FieldingResolver.CatchRadiusFt(ashlord, null, Game.Rules), 9);

        var rio = Game.Must("rio");           // Field 6, grow: +6
        Assert.Equal(4.0 + 6, FieldingResolver.CatchRadiusFt(rio, null, Game.Rules), 9);

        // An authored reachFt wins over the table.
        var authored = ashlord with { ReachFt = 7.5 };
        Assert.Equal(7.5, FieldingResolver.CatchRadiusFt(authored, null, Game.Rules), 9);
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheStackIsSixTenFourteen()
    {
        var r = Game.Rules;
        var standUp = FieldingResolver.CatchRadiusFt(Game.Must("ashlord"), null, r);
        Assert.Equal(4.0, FieldingResolver.StandUpCatchFt(standUp), 9);
        Assert.Equal(10.0, FieldingResolver.CatchWindowFt(standUp, dive: false, jump: false, r), 9);   // the 4-ft dirt pad
        Assert.Equal(14.0, FieldingResolver.DiveCatchFt(standUp, r), 9);                              // the earned dive
        // The jump is the arc (catch.jumpReachFt 0), so the armed window is the dirt pad alone.
        Assert.Equal(10.0, FieldingResolver.CatchWindowFt(standUp, dive: false, jump: true, r), 9);
        // A glove 7 ft off the plant is not under the ring; 5.9 ft is.
        Assert.False(FlyCatch.Under(7, 0, 0, 0, 0, 0, standUp, false, r));
        Assert.True(FlyCatch.Under(5.9, 0, 0, 0, 0, 0, standUp, false, r));
    }

    [Fact]
    public void ThePreviewAndTheRingReadTheReach()
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = Game.Team("Offense", "rio", "boom", "cinder", "grit", "soot", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        Assert.Equal(4.0 + FieldAbilities.CatchBonus(preview.Fielder, match.Rules), preview.CatchRadius, 9);
        Assert.Equal(preview.CatchRadius, LandingMark.RadiusFt(preview), 9);
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void AnOutfielderUnderAFlyRunsAtTheAirMultiplier()
    {
        // #719 slice 1 recorded (1.0, 1.0) here — one pursuit profile per body. 3d (#757) measured the compact field under the
        // S-29 floor in every cohort with the outfield's multiplier at 1.0, and Jack set it back to 0.6 on 2026-09-18; the
        // infield's stays 1.0.
        Assert.Equal((0.6, 1.0), (Game.Rules.Fielding.Chase.OutfieldAirMul, Game.Rules.Fielding.Chase.InfieldAirMul));
        var match = Match.Exhibition(Game, "rio", "ashlord", 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var who = preview.Fielder;
        var flat = FieldingResolver.ChaseSpeedFt(who, false, match.Rules);
        Assert.Equal(flat * 0.6, FieldingResolver.ChaseSpeedFt(who, "CF", preview, match.Rules), 9);
    }
}
