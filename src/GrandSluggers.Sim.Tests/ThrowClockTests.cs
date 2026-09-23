using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-6 slice 1 (#722): the one throw clock becomes the accepted curve — release, distance over the
/// arm, and a smooth loss of pace past the arm's comfortable range, with chemistry and ability
/// dividing the whole flight once (F693-03-long-throw-numbers). A table with the loss at 0 is the
/// flat clock, checked to the bit, so the switch is the table and nothing else.
/// </summary>
[Trait("Kind", "Balance")]
public sealed class ThrowClockTests
{
    static readonly ContentCatalog Game = ContentCatalog.Load();

    /// <summary>The game's table with the long-throw loss switched off: the flat clock.</summary>
    static readonly RulesTable Flat = PatchedRules("rules/fielding.json", "\"longThrowLossSec\": 0.60", "\"longThrowLossSec\": 0");

    /// <summary>The shipped rules with one file patched in a temporary overlay (a whole file, as an overlay must carry).</summary>
    static RulesTable PatchedRules(string file, string from, string to)
    {
        var dir = Path.Combine(Path.GetTempPath(), "grand-sluggers-throwclock-" + Guid.NewGuid().ToString("N"));
        try
        {
            var text = File.ReadAllText(Path.Combine(Game.Root.Shipped, file));
            Assert.Contains(from, text);
            var dest = Path.Combine(dir, file);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.WriteAllText(dest, text.Replace(from, to));
            return RulesTable.Load(new DataRoot(Game.Root.Shipped, dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>A throw by a rating alone: the arm's speed × a pair-and-ability factor, the arm on record.</summary>
    static ThrowResult Thrower(int arm, RulesTable r, double pair = 1.0) =>
        new(Chemistry.Neutral, InPlay.ArmMul(arm, r) * pair, false, Arm: arm);

    // ---------------------------------------------------------------------------------
    // The switch
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <b>With the loss at 0 the clock is flat, bit for bit.</b> The loss term is
    /// <c>0 × over² / pair</c>, which is exactly 0.0, and adding 0.0 to a double leaves it unchanged; the
    /// linear term is release plus distance over the arm's speed, computed in the same operations.
    /// </summary>
    [Fact]
    public void WithTheLossOffTheClockIsFlatToTheBit()
    {
        var r = Flat;
        var t = r.Fielding.Throw;
        Assert.Equal(0, t.LongThrowLossSec);
        Assert.Equal(160, t.ComfortableRangeFt);
        Assert.Equal(5, t.RangePerArmFt);

        for (var d = 10; d <= 400; d += 10)
        {
            Assert.Equal(t.ReleaseSec + d / t.BaseFtPerSec, InPlay.ThrowSec(d, null, r));
            for (var arm = 1; arm <= 10; arm++)
                foreach (var pair in new[] { 1.0, 1.30, 0.70, 1.45, 1.22, 0.35 })
                {
                    var thr = Thrower(arm, r, pair);
                    var fps = t.BaseFtPerSec * thr.SpeedMul;
                    Assert.Equal(t.ReleaseSec + d / Math.Max(t.MinFtPerSec, fps), InPlay.ThrowSec(d, thr, r));
                }
        }
    }

    /// <summary>The neutral arm is the arm a throw with no thrower is read at, and its multiplier is 1.0.</summary>
    [Fact]
    public void TheNeutralArmIsTheMiddleOfTheScaleAndReadsAsOne()
    {
        Assert.Equal(5, InPlay.NeutralArm);
        Assert.Equal(1.0, InPlay.ArmMul(InPlay.NeutralArm, Game.Rules), 12);
        Assert.Equal(InPlay.NeutralArm, new ThrowResult(Chemistry.Neutral, 1.0, false).Arm);
        Assert.Equal(0.30 + 80 / 88.89, InPlay.ThrowSec(80, null, Game.Rules));
    }

    // ---------------------------------------------------------------------------------
    // The curve
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The accepted samples: 0.30 s release, 0.90 s over 80 ft, proportional
    /// inside the range; 4.80 s from 280 ft and 10.20 s from 400 for the neutral arm, which are the
    /// numbers the relay record was written against; a good pair divides the whole
    /// flight by 1.30 once, so 80 ft is 0.992 s and 280 ft is 3.76.
    /// </summary>
    [Theory]
    [InlineData(40, 1.0, 0.75)]
    [InlineData(80, 1.0, 1.20)]
    [InlineData(100, 1.0, 1.425)]
    [InlineData(120, 1.0, 1.65)]
    [InlineData(280, 1.0, 4.80)]
    [InlineData(400, 1.0, 10.20)]
    [InlineData(80, 1.30, 0.992)]
    [InlineData(280, 1.30, 3.762)]
    public void TheTableIsTheAcceptedCurve(double distFt, double pair, double expectedSec)
    {
        var r = Game.Rules;
        Assert.Equal(0.30, r.Fielding.Throw.ReleaseSec);
        Assert.Equal(88.89, r.Fielding.Throw.BaseFtPerSec);
        Assert.Equal(0.60, r.Fielding.Throw.LongThrowLossSec);
        Assert.InRange(InPlay.ThrowSec(distFt, Thrower(InPlay.NeutralArm, r, pair), r), expectedSec - 0.001, expectedSec + 0.001);
        // No thrower reads as the neutral arm.
        if (pair == 1.0) Assert.InRange(InPlay.ThrowSec(distFt, null, r), expectedSec - 0.001, expectedSec + 0.001);
    }

    /// <summary>
    /// The range is the arm's: 160 ft for the neutral arm, 5 ft per point either side. Inside it the
    /// clock is flat; one 80-ft step past it costs exactly the loss; the loss is divided by the pair
    /// factor like the rest of the flight.
    /// </summary>
    [Theory]
    [InlineData(1, 140)]
    [InlineData(3, 150)]
    [InlineData(5, 160)]
    [InlineData(8, 175)]
    [InlineData(10, 185)]
    public void TheComfortableRangeMovesWithTheArm(int arm, double rangeFt)
    {
        var r = Game.Rules;
        var t = r.Fielding.Throw;
        var thr = Thrower(arm, r);
        double Linear(double d) => t.ReleaseSec + d / (t.BaseFtPerSec * thr.SpeedMul);
        Assert.Equal(Linear(rangeFt - 1), InPlay.ThrowSec(rangeFt - 1, thr, r), 9);
        Assert.Equal(Linear(rangeFt), InPlay.ThrowSec(rangeFt, thr, r), 9);
        Assert.Equal(Linear(rangeFt + 80) + t.LongThrowLossSec, InPlay.ThrowSec(rangeFt + 80, thr, r), 9);
        Assert.Equal(Linear(rangeFt + 160) + 4 * t.LongThrowLossSec, InPlay.ThrowSec(rangeFt + 160, thr, r), 9);

        var good = Thrower(arm, r, r.Fielding.Chem.GoodSpeedMul);
        var flightGood = InPlay.ThrowSec(rangeFt + 80, good, r) - t.ReleaseSec;
        var flightNeutral = InPlay.ThrowSec(rangeFt + 80, thr, r) - t.ReleaseSec;
        Assert.Equal(flightNeutral / r.Fielding.Chem.GoodSpeedMul, flightGood, 9);
    }

    /// <summary>
    /// <b>The break-even, stated from the code rather than asserted (#722's exit criterion).</b> On the
    /// compact centre line the cutoff is SS, whose spot projects onto the line at z 104.89, so the
    /// second leg is 104.89 ft whatever the depth. A relay is the first leg, the cutter's reaction
    /// (§8.8) and the second leg with the cutter's arm; it arrives first beyond a depth that depends
    /// on both arms. A weak thrower should relay from the middle outfield, a strong one only from
    /// the wall; on the flat clock (the loss off) the relay never wins anywhere inside 400 ft, so the
    /// curve is what makes the relay worth throwing.
    /// </summary>
    [Theory]
    [InlineData(3, 5, 221)]
    [InlineData(5, 5, 237)]
    [InlineData(8, 5, 258)]
    [InlineData(10, 5, 272)]
    [InlineData(3, 8, 208)]
    [InlineData(5, 8, 225)]
    [InlineData(8, 8, 247)]
    [InlineData(10, 8, 261)]
    public void ARelayArrivesFirstBeyondADepthThatDependsOnBothArms(int throwerArm, int cutterArm, double breakEvenFt)
    {
        var r = Game.Rules;
        var (sx, sz) = r.Fielders.Spot("SS");
        Assert.Equal(104.89, sz, 2);   // the projection of the compact SS onto the CF → home line
        var leg2 = sz;

        double Direct(double d, RulesTable rules) => InPlay.ThrowSec(d, Thrower(throwerArm, rules), rules);
        double Relay(double d, RulesTable rules)
        {
            var re = rules.Fielding.Reaction;
            var transfer = Math.Max(re.ThrowMinSec, re.ThrowBaseSec - cutterArm * re.ThrowPerFieldSec) * rules.Cpu.Active.ReactionMul;
            return InPlay.ThrowSec(d - leg2, Thrower(throwerArm, rules), rules) + transfer + InPlay.ThrowSec(leg2, Thrower(cutterArm, rules), rules);
        }

        var found = double.NaN;
        for (var d = 120.0; d <= 400; d += 0.1)
            if (Relay(d, r) < Direct(d, r)) { found = d; break; }
        Assert.InRange(found, breakEvenFt - 1.0, breakEvenFt + 1.0);
        Assert.True(Relay(280, r) < Direct(280, r), "from the compact wall everybody relays");

        // Weak arm, strong arm at 220 ft: the direction #732 asked for, in seconds.
        if (throwerArm == 3 && cutterArm == 5) Assert.InRange(Direct(220, r), 3.38, 3.40);
        if (throwerArm == 8 && cutterArm == 5) Assert.InRange(Direct(220, r), 2.75, 2.77);

        var flat = Flat;
        for (var d = 120.0; d <= 400; d += 1)
            Assert.True(Relay(d, flat) >= Direct(d, flat), $"on the flat clock the relay cannot win at {d} ft");
    }

    /// <summary>
    /// <b>Bad chemistry is slow, not random</b> (F693-03-negative-chemistry). Every bad throw flies at
    /// 0.90 and none is slanted; the lateral spread is the thrower's Field accuracy alone, the same σ a
    /// neutral pair has.
    /// </summary>
    [Fact]
    public void BadChemistryIsSlowNotRandom()
    {
        var rio = Game.Must("rio");
        var ashlord = Game.Must("ashlord");
        var nico = Game.Must("nico");
        Assert.Equal(Chemistry.Bad, Game.Chemistry.Between(rio, ashlord));
        Assert.Equal(Chemistry.Good, Game.Chemistry.Between(rio, nico));
        var sigma = Math.Max(0, 11 - rio.Stats.Arm) * Game.Rules.Fielding.Throw.LateralSigmaPerFieldDeficitFt;

        var lateral = new List<double>();
        for (var seed = 0; seed < 2000; seed++)
        {
            var bad = Game.Chemistry.FieldingThrow(rio, ashlord, new Random(seed));
            Assert.Equal(Chemistry.Bad, bad.Relation);
            Assert.False(bad.Slanted);
            Assert.Equal(0.90, bad.SpeedMul);
            lateral.Add(bad.LateralFt);
        }
        var mean = lateral.Average();
        var std = Math.Sqrt(lateral.Sum(x => (x - mean) * (x - mean)) / (lateral.Count - 1));
        Assert.InRange(std, sigma * 0.9, sigma * 1.1);

        // Through the arm: a bad pair's flight is the neutral flight over 0.90, long-range loss included.
        var neutral = Thrower(rio.Stats.Arm, Game.Rules);
        var badThrow = FieldAbilities.ApplyThrow(rio, Game.Chemistry.FieldingThrow(rio, ashlord, new Random(1)), Game.Rules);
        Assert.Equal(rio.Stats.Arm, badThrow.Arm);
        var release = Game.Rules.Fielding.Throw.ReleaseSec;
        Assert.Equal((InPlay.ThrowSec(260, neutral, Game.Rules) - release) / 0.90, InPlay.ThrowSec(260, badThrow, Game.Rules) - release, 9);
    }

    /// <summary>The thrower's rating rides on every throw the game builds, so the range is the thrower's and not the neutral arm's.</summary>
    [Fact]
    public void EveryBuiltThrowCarriesItsThrowersArm()
    {
        var vine = Game.Must("vine");
        var frost = Game.Must("frost");
        var thr = FieldAbilities.ApplyThrow(vine, Game.Chemistry.FieldingThrow(vine, frost, new Random(3)), Game.Rules);
        Assert.Equal(vine.Stats.Arm, thr.Arm);
        Assert.Equal(InPlay.ArmMul(vine, Game.Rules) * FieldAbilities.ThrowMul(vine, Game.Rules), thr.SpeedMul, 9);
    }
}
