using Xunit;
using System.Linq;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Arm / Hands / reach split (#710, F693-02-defensive-trait-mapping). The rule these encode is
/// that an <b>unauthored</b> roster behaves exactly as it did before the split, and that once a trait
/// is authored it moves its own consumers and nobody else's.
/// </summary>
public class DefensiveTraitTests
{
    readonly ContentCatalog _content = Shipped.Content;

    static Stats Seeded(int field) => new(5, 5, field, 5);

    [Fact]
    public void UnauthoredTraitsSeedFromField()
    {
        foreach (var field in new[] { 1, 5, 10 })
        {
            var stats = Seeded(field);
            Assert.Equal(field, stats.Arm);
            Assert.Equal(field, stats.Hands);
            Assert.False(stats.ArmAuthored);
            Assert.False(stats.HandsAuthored);
        }
    }

    [Fact]
    public void EveryShippedCharacterIsStillSeeded()
    {
        // The migration authors no ratings, so the whole roster must still track Field.
        foreach (var c in _content.Characters.Values)
        {
            Assert.Equal(c.Stats.Field, c.Stats.Arm);
            Assert.Equal(c.Stats.Field, c.Stats.Hands);
            Assert.Null(c.ReachFt);
        }
    }

    [Fact]
    public void AnUnauthoredTraitKeepsTrackingFieldThroughAClamp()
    {
        var clamped = new Stats(5, 5, 99, 5).Clamp();
        Assert.Equal(10, clamped.Field);
        Assert.Equal(10, clamped.Arm);
        Assert.Equal(10, clamped.Hands);
        Assert.False(clamped.ArmAuthored);

        var authored = new Stats(5, 5, 99, 5) { Arm = 99 }.Clamp();
        Assert.Equal(10, authored.Arm);
        Assert.True(authored.ArmAuthored);
    }

    [Fact]
    public void ArmMovesTheThrowAndNothingElse()
    {
        var weak = Character(arm: 1);
        var strong = Character(arm: 10);
        Assert.True(InPlay.ArmMul(strong, rules: Rules.Default) > InPlay.ArmMul(weak, rules: Rules.Default), "a better arm throws harder");

        // Hands and reach are untouched by the arm rating.
        Assert.Equal(FieldingResolver.RecoilSec(weak, 100, Rules.Default), FieldingResolver.RecoilSec(strong, 100, Rules.Default), 6);
        Assert.Equal(
            FieldingResolver.CatchRadiusFt(weak, null, rules: Rules.Default),
            FieldingResolver.CatchRadiusFt(strong, null, rules: Rules.Default), 6);
    }

    [Fact]
    public void HandsMovesRecoilAndNothingElse()
    {
        var clumsy = Character(hands: 1);
        var sure = Character(hands: 10);
        Assert.True(FieldingResolver.RecoilSec(sure, 100, Rules.Default) < FieldingResolver.RecoilSec(clumsy, 100, Rules.Default),
            "better hands recover sooner");

        Assert.Equal(InPlay.ArmMul(clumsy, rules: Rules.Default), InPlay.ArmMul(sure, rules: Rules.Default), 6);
        Assert.Equal(
            FieldingResolver.CatchRadiusFt(clumsy, null, rules: Rules.Default),
            FieldingResolver.CatchRadiusFt(sure, null, rules: Rules.Default), 6);
    }

    [Fact]
    public void AuthoredReachReplacesTheTablesReachAndFieldNoLongerSizesIt()
    {
        var rules = Rules.Default;
        var catchRules = rules.Fielding.Catch;
        Assert.Equal(4.0, catchRules.StandUpReachFt);

        // Unauthored: the table's stand-up reach, the same for every Field.
        foreach (var field in new[] { 1, 5, 10 })
            Assert.Equal(4.0, FieldingResolver.CatchRadiusFt(Character(field: field), null, rules), 6);

        // Authored: the number wins over the table, and changing Field cannot resize it.
        var small = Character(field: 1, reachFt: 4);
        var big = Character(field: 10, reachFt: 4);
        Assert.Equal(4, FieldingResolver.CatchRadiusFt(small, null, rules), 6);
        Assert.Equal(4, FieldingResolver.CatchRadiusFt(big, null, rules), 6);
    }

    [Fact]
    public void ArmStaysOutOfRosterFill()
    {
        // Teams.Tools sums four ratings. While Arm equals Field, including it would double-count
        // defence and change which players the auto-fill picks (F693-02-arm-rating-migration).
        var (home, away) = PresetTeams.Pair(_content, "rio", "ashlord");
        foreach (var c in home.Roster.Concat(away.Roster))
            Assert.False(c.Stats.ArmAuthored, $"{c.Id} would change roster fill once Arm is authored");
    }

    Character Character(int field = 5, int arm = 0, int hands = 0, double? reachFt = null)
    {
        var rio = _content.Must("rio");
        return rio with
        {
            Stats = new Stats(rio.Stats.Pitch, rio.Stats.Bat, field, rio.Stats.Run) { Arm = arm, Hands = hands },
            ReachFt = reachFt,
            FieldAbility = ""
        };
    }
}
