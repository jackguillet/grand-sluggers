using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-3 slice 1 (#719, F693-02-catch-reach-envelope and Jack's air-multiplier addition): the trial's stand-up reach is the
/// authored 6 ft for every body without its own <c>reachFt</c>, the dirt pad and the dive add to it (6 / 10 / 14), and an
/// outfielder under a fly runs at the one pursuit speed. The shipped table keeps <c>10 + 0.6 × Field</c> and 0.6 / 0.45.
/// </summary>
public sealed class CatchReachTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly DataRoot Root = new(Control.Root.Shipped, Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(Root);

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheShippedReachIsTheLegacyFormulaAndTheTrialIsSixFeet()
    {
        Assert.Equal(0, Control.Rules.Fielding.Catch.StandUpReachFt);
        Assert.Equal(6.0, Trial.Rules.Fielding.Catch.StandUpReachFt);

        var ashlord = Control.Must("ashlord");   // Field 3, spin-check: no catch bonus
        Assert.Equal(3, ashlord.Stats.Field);
        Assert.Null(ashlord.ReachFt);
        Assert.Equal(10 + 0.6 * 3, FieldingResolver.CatchRadiusFt(ashlord, null, Control.Rules), 9);
        Assert.Equal(6.0, FieldingResolver.CatchRadiusFt(Trial.Must("ashlord"), null, Trial.Rules), 9);

        var rio = Control.Must("rio");           // Field 6, grow: +6 on either root
        Assert.Equal(10 + 0.6 * 6 + 6, FieldingResolver.CatchRadiusFt(rio, null, Control.Rules), 9);
        Assert.Equal(6.0 + 6, FieldingResolver.CatchRadiusFt(Trial.Must("rio"), null, Trial.Rules), 9);

        // An authored reachFt wins over both tables.
        var authored = ashlord with { ReachFt = 7.5 };
        Assert.Equal(7.5, FieldingResolver.CatchRadiusFt(authored, null, Control.Rules), 9);
        Assert.Equal(7.5, FieldingResolver.CatchRadiusFt(authored, null, Trial.Rules), 9);
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheTrialStackIsSixTenFourteen()
    {
        var r = Trial.Rules;
        var standUp = FieldingResolver.CatchRadiusFt(Trial.Must("ashlord"), null, r);
        Assert.Equal(6.0, FieldingResolver.StandUpCatchFt(standUp), 9);
        Assert.Equal(10.0, FieldingResolver.CatchWindowFt(standUp, dive: false, jump: false, r), 9);   // the 4-ft dirt pad
        Assert.Equal(14.0, FieldingResolver.DiveCatchFt(standUp, r), 9);                              // the earned dive
        // Was 18 with the legacy 8-ft jump allowance; slice 3 made the jump the arc (catch.jumpReachFt 0), so the armed window is the dirt pad alone.
        Assert.Equal(10.0, FieldingResolver.CatchWindowFt(standUp, dive: false, jump: true, r), 9);
        // A glove 7 ft off the plant is under the ring on the shipped reach and not on the trial's.
        Assert.True(FlyCatch.Under(7, 0, 0, 0, 0, 0, FieldingResolver.CatchRadiusFt(Control.Must("ashlord"), null, Control.Rules), false, Control.Rules));
        Assert.False(FlyCatch.Under(7, 0, 0, 0, 0, 0, standUp, false, r));
        Assert.True(FlyCatch.Under(5.9, 0, 0, 0, 0, 0, standUp, false, r));
    }

    [Fact]
    public void ThePreviewAndTheRingReadTheTrialReach()
    {
        var home = Trial.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = Trial.Team("Offense", "rio", "boom", "cinder", "grit", "soot", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Trial, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        Assert.Equal(6.0 + FieldAbilities.CatchBonus(preview.Fielder, match.Rules), preview.CatchRadius, 9);
        Assert.Equal(preview.CatchRadius, LandingMark.RadiusFt(preview), 9);
    }

    [Fact]
    [Trait("Kind", "Balance")]
    public void AnOutfielderUnderAFlyRunsAtTheAirMultiplierOnBothTables()
    {
        Assert.Equal((0.6, 0.45), (Control.Rules.Fielding.Chase.OutfieldAirMul, Control.Rules.Fielding.Chase.InfieldAirMul));
        // #719 slice 1 recorded (1.0, 1.0) here — one pursuit profile per body. 3d (#757) measured the copy under the S-29 floor in
        // every cohort with the outfield's multiplier at 1.0, and Jack set it back to the shipped 0.6 on 2026-09-18; the infield's stays 1.0.
        Assert.Equal((0.6, 1.0), (Trial.Rules.Fielding.Chase.OutfieldAirMul, Trial.Rules.Fielding.Chase.InfieldAirMul));
        // Slice 1 recorded the trial's centre fielder at the one speed (× 1.0, 18 ft/s) under this fly; at 0.6 he runs 10.8.
        foreach (var (content, mul) in new[] { (Control, 0.6), (Trial, 0.6) })
        {
            var match = Match.Exhibition(content, "rio", "ashlord", 3, 1, parkId: "harbor-diamond");
            var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
            var preview = match.PreviewHit(hit);
            Assert.Equal("CF", preview.Position);
            var who = preview.Fielder;
            var flat = FieldingResolver.ChaseSpeedFt(who, false, match.Rules);
            Assert.Equal(flat * mul, FieldingResolver.ChaseSpeedFt(who, "CF", preview, match.Rules), 9);
        }
    }
}
