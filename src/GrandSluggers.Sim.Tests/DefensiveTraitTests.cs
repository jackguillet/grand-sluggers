using Xunit;
using System.Linq;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Arm / Hands / reach split (F693-02-defensive-trait-mapping, CH-07). Arm (throw speed) and Hands
/// are authored sub-stats; the Field bar is their rounded mean. Each moves its own consumers and nobody
/// else's, and neither sizes the catch reach.
/// </summary>
public class DefensiveTraitTests
{
    [Fact]
    public void TheFieldBarIsTheRoundedMeanOfHandsAndArm()
    {
        Assert.Equal(6, (Stats.Even(5, 5, 5, 5) with { Hands = 5, Arm = 7 }).Field);
        Assert.Equal(8, (Stats.Even(5, 5, 5, 5) with { Hands = 7, Arm = 8 }).Field); // 7.5 rounds half up
        Assert.Equal(7, (Stats.Even(5, 5, 5, 5) with { Hands = 6, Arm = 7 }).Field); // 6.5 rounds half up
    }

    [Fact]
    public void AClampHoldsHandsAndArmAndTheBarFollows()
    {
        var clamped = (Stats.Even(5, 5, 5, 5) with { Hands = 99, Arm = -3 }).Clamp();
        Assert.Equal(10, clamped.Hands);
        Assert.Equal(1, clamped.Arm);
        Assert.Equal(6, clamped.Field);
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
            FieldingResolver.CatchRadiusFt(weak, null, rules: Rules.Default, air: true),
            FieldingResolver.CatchRadiusFt(strong, null, rules: Rules.Default, air: true), 6);
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
            FieldingResolver.CatchRadiusFt(clumsy, null, rules: Rules.Default, air: true),
            FieldingResolver.CatchRadiusFt(sure, null, rules: Rules.Default, air: true), 6);
    }

    [Fact]
    public void TheBodyClassSizesTheReachAndFieldDoesNot()
    {
        var rules = Rules.Default;
        var catchRules = rules.Fielding.Catch;
        Assert.Equal(4.0, catchRules.StandUpReachFt);

        // Unclassed: the table's stand-up reach, the same for every Field.
        foreach (var field in new[] { 1, 5, 10 })
            Assert.Equal(4.0, FieldingResolver.CatchRadiusFt(Character(field: field, bodyClass: ""), null, rules, air: true), 6);

        // Classed: the class row's reach, and changing Field cannot resize it.
        var row = rules.BodyClasses.Of("ape");
        foreach (var field in new[] { 1, 10 })
        {
            Assert.Equal(row.FlyReachFt, FieldingResolver.CatchRadiusFt(Character(field: field, bodyClass: "ape"), null, rules, air: true), 6);
            Assert.Equal(row.GroundReachFt, FieldingResolver.CatchRadiusFt(Character(field: field, bodyClass: "ape"), null, rules, air: false), 6);
        }
    }

    readonly ContentCatalog _content = Shipped.Content;

    Character Character(int field = 5, int arm = 0, int hands = 0, string? bodyClass = null)
    {
        var rio = _content.Must("rio");
        return rio with
        {
            Stats = rio.Stats with { Arm = arm > 0 ? arm : field, Hands = hands > 0 ? hands : field },
            BodyClass = bodyClass ?? rio.BodyClass,
            FieldAbility = ""
        };
    }
}
