using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-6 slice 2 (#722, decision 6 of #730): the CPU chooses relay or direct by the total time each takes
/// to reach the bag, graded by rung, and the CPU runner reads the same plan. The forced-relay ceiling
/// is set to never, so time decides. Every scenario assertion recomputes the plan from the live
/// positions at the release frame and asks only that the CPU did what its own arithmetic says.
/// </summary>
public sealed class RelayDecisionTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    // ---------------------------------------------------------------------------------
    // The arithmetic
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheCeilingForcesTheRelayAndTheBiasGradesTheRest()
    {
        Assert.True(InPlay.RelayWins(3.0, 4.0, forced: true, relayBiasSec: 99));
        Assert.False(InPlay.RelayWins(3.0, 2.9, forced: false, relayBiasSec: 99));   // the never bias: no relay by time
        Assert.True(InPlay.RelayWins(3.0, 2.9, forced: false, relayBiasSec: 0));     // hard: any saving
        Assert.False(InPlay.RelayWins(3.0, 2.95, forced: false, relayBiasSec: 0.1)); // normal: a tenth is not enough
        Assert.True(InPlay.RelayWins(3.0, 2.85, forced: false, relayBiasSec: 0.1));
        Assert.False(InPlay.RelayWins(3.0, 2.75, forced: false, relayBiasSec: 0.3)); // easy: needs more than three tenths
        Assert.True(InPlay.RelayWins(3.0, 2.65, forced: false, relayBiasSec: 0.3));
        Assert.False(InPlay.RelayWins(3.0, 3.0, forced: false, relayBiasSec: 0));    // a tie is the direct throw
    }

    /// <summary>The ceiling is never, and each rung reads what its table says.</summary>
    [Fact]
    public void TheRungsReadWhatTheirTableSays()
    {
        Assert.Equal(9999, Game.Rules.Fielding.Throw.OnTheFlyFt);
        Assert.Equal((0.3, 0.5, 0.0, 0.0), (Game.Rules.Cpu.Easy.RelayBiasSec, Game.Rules.Cpu.Easy.RunnerReadsArm, Game.Rules.Cpu.Easy.RunnerReadsRelay, Game.Rules.Cpu.Easy.ReadsChemistry));
        Assert.Equal((0.1, 1.0, 1.0, 1.0), (Game.Rules.Cpu.Normal.RelayBiasSec, Game.Rules.Cpu.Normal.RunnerReadsArm, Game.Rules.Cpu.Normal.RunnerReadsRelay, Game.Rules.Cpu.Normal.ReadsChemistry));
        Assert.Equal((0.0, 1.0, 1.0, 1.0), (Game.Rules.Cpu.Hard.RelayBiasSec, Game.Rules.Cpu.Hard.RunnerReadsArm, Game.Rules.Cpu.Hard.RunnerReadsRelay, Game.Rules.Cpu.Hard.ReadsChemistry));
    }

    /// <summary>
    /// The runner's estimate takes the clock the ball carries, for a held ball and for a throw in the
    /// air to another bag; without one it is the neutral flat throw it always was, so a
    /// <see cref="BallSituation"/> built without a clock — every test before this slice — reads as before.
    /// </summary>
    [Fact]
    public void TheRunnerReadsTheClockTheBallCarriesAndTheNeutralThrowWithoutOne()
    {
        var r = Game.Rules;
        var reaction = r.Running.Cpu.ReactionSec;
        var held = new BallSituation(true, false, 0, 0, 0, 250, 3.0, true, 0, 250, 250);
        Assert.Equal(reaction + InPlay.ThrowArrivalSec(0, 250, 4, null, r), RunnerAi.ThrowArrivalSec(held, 4, 3.0, r), 9);

        var clocked = held with { ThrowClock = (x, z, bag) => 1.5 + bag };
        Assert.Equal(reaction + 5.5, RunnerAi.ThrowArrivalSec(clocked, 4, 3.0, r), 9);
        Assert.Equal(reaction + 4.5, RunnerAi.ThrowArrivalSec(clocked, 3, 3.0, r), 9);

        // In the air to second, read for home: the landing plus the reaction plus the clock from second (z 113.14).
        var inAir = new BallSituation(false, true, 2, 4.0, 0, 113.14, 3.0, false, 0, 113.14, 120, ThrowClock: (x, z, bag) => 0.25 * bag + z / 1000);
        Assert.Equal(1.0, RunnerAi.ThrowArrivalSec(inAir, 2, 3.0, r), 9);
        Assert.Equal(1.0 + reaction + 1.0 + 0.11314, RunnerAi.ThrowArrivalSec(inAir, 4, 3.0, r), 6);
    }

    // ---------------------------------------------------------------------------------
    // The CPU centre fielder, throwing home on a tag-up
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A fly to dead centre with konga on third and one out. The gate sends konga, the centre fielder decides how to get the ball to the plate. Weak arm in centre: moss (Field 4);
    /// strong arm: vine (Field 8); the middle arm: soot (Field 5). None carries a throw ability, and none
    /// shares a faction with the cutter or the catcher, so the chemistry read is 1.0 and the arms alone
    /// decide.
    /// </summary>
    [Theory]
    [InlineData("moss", "normal", true)]      // Arm 4 from 245 ft: the relay arrives first
    [InlineData("vine", "normal", false)]     // Arm 8 from 245 ft: direct, the break-even is 258
    [InlineData("soot", "hard", true)]        // Arm 5: the relay saves under three tenths — hard takes it
    [InlineData("soot", "easy", false)]       // … and easy does not
    public void TheCentreFielderRelaysWhenTheRelayArrivesFirstByTheRungsBias(string centre, string difficulty, bool expectRelay)
    {
        var content = Game;
        var (match, cf, cutters) = Defence(content, centre, difficulty);
        var konga = content.Must("konga");
        Assert.True(match.StationRunner(3, konga));
        Assert.True(match.SetOuts(1));
        // The fixture flies on the match's own table (drag 0.0040), so the ball lands where it is asked to.
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);

        var (throws, play, caught) = Run(match, hit, preview);
        var first = throws.FirstOrDefault(t => t.FromPos == "CF");
        Assert.True(first is not null,
            $"{centre} {difficulty}: no throw by CF. caught {caught}; play {play.Kind} \"{play.Caption}\"; throws: {string.Join(" | ", throws.Select(t => $"{t.FromPos}->{t.Bag}/{t.Receiver} from ({t.FromX:0},{t.FromZ:0})"))}");
        var relayed = first!.Bag == 0;

        // The plan, recomputed from the live positions at the release frame with the same arithmetic.
        var rules = match.Rules;
        var level = rules.Cpu.Active;
        var home = Diamond.Bag(4);
        var catcher = match.Defense.Roster.First(c => c.Id == "pewter");
        var cut = InPlay.CutoffFor(first.FromX, first.FromZ, home.X, home.Z, first.Spots, "CF", "C");
        Assert.True(cut is not null, $"{centre} {difficulty}: no cutoff between ({first.FromX:0},{first.FromZ:0}) and home; spots {string.Join(" ", first.Spots.Select(kv => $"{kv.Key}({kv.Value.X:0},{kv.Value.Z:0})"))}");
        var cutter = cutters[cut!.Value.Pos];
        var direct = InPlay.ThrowSec(Diamond.Dist(first.FromX, first.FromZ, home.X, home.Z), Forecast(content, cf, catcher, level, rules), rules);
        var relay = InPlay.ThrowSec(Diamond.Dist(first.FromX, first.FromZ, cut.Value.X, cut.Value.Z), Forecast(content, cf, cutter, level, rules), rules)
                    + InPlay.ThrowReactionSec(cutter, rules)
                    + InPlay.ThrowSec(Diamond.Dist(cut.Value.X, cut.Value.Z, home.X, home.Z), Forecast(content, cutter, catcher, level, rules), rules);
        var forced = Diamond.Dist(first.FromX, first.FromZ, home.X, home.Z) > rules.Fielding.Throw.OnTheFlyFt;
        var why = $"{centre} {difficulty}: from ({first.FromX:0.0},{first.FromZ:0.0}) direct {direct:0.000} relay {relay:0.000} via {cut.Value.Pos} bias {level.RelayBiasSec} forced {forced}";
        Assert.True(InPlay.RelayWins(direct, relay, forced, level.RelayBiasSec) == relayed, why);
        Assert.Equal(expectRelay, relayed);
        if (relayed)
        {
            Assert.Equal(cut.Value.Pos, first.Receiver);
            // The relay continues from the cutoff with the cutoff's own arm — when there is still a play to make. A slow
            // runner scoring against a weak arm leaves the cutoff holding the ball for Time, and that is the table's call.
            var onward = throws.SkipWhile(t => t != first).Skip(1).FirstOrDefault();
            if (onward is not null) Assert.Equal(cut.Value.Pos, onward.FromPos);
        }
        else
        {
            Assert.Equal(4, first.Bag);
            Assert.Equal("C", first.Receiver);
        }
    }

    /// <summary>The forecast the plan uses: the arm and ability exactly, the pair chemistry as far as the rung reads it.</summary>
    static ThrowResult Forecast(ContentCatalog content, Character from, Character to, CpuLevelRules level, RulesTable rules)
    {
        var mul = InPlay.ArmMul(from, rules) * FieldAbilities.ThrowMul(from, rules);
        var pair = content.Chemistry.Between(from, to) switch
        {
            Chemistry.Good => rules.Fielding.Chem.GoodSpeedMul,
            Chemistry.Bad => rules.Fielding.Chem.BadSpeedMul,
            _ => 1.0
        };
        return new ThrowResult(Chemistry.Neutral, mul * (1 + level.ReadsChemistry * (pair - 1)), false, Arm: from.Stats.Arm);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    sealed record ThrowSeen(string FromPos, int Bag, string Receiver, double FromX, double FromZ, IReadOnlyDictionary<string, (double X, double Z)> Spots);

    /// <summary>
    /// A defense with <paramref name="centre"/> in centre field. The roster fills <see cref="Diamond.Order"/> after
    /// the pitcher, so the eighth name is CF; SS and 2B, the two bodies a centre-line throw can cut through, are
    /// marlow (Field 6) and jester (Field 5), neither with a throw ability and neither in the centre fielder's
    /// faction.
    /// </summary>
    static (Match Match, Character Centre, Dictionary<string, Character> Cutters) Defence(ContentCatalog content, string centre, string difficulty)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "jester", "grit", "marlow", "basil", centre, "gull");
        var away = content.Team("Offense", "zig", "dart", "cinder", "nugget", "hex", "boom", "konga", "ashlord", "frost");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: ParkId.Harbor, difficulty: difficulty);
        var cf = content.Must(centre);
        Assert.Equal(centre, cf.Id);
        Assert.Equal(1.0, FieldAbilities.ThrowMul(cf, match.Rules), 9);
        return (match, cf, new Dictionary<string, Character> { ["SS"] = content.Must("marlow"), ["2B"] = content.Must("jester") });
    }

    static (List<ThrowSeen> Throws, PlayEvent Play, bool Caught) Run(Match match, AtBatResult hit, FieldingPreview preview)
    {
        var caught = false;
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var seen = new List<ThrowSeen>();
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var r = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            if (live.Fly == FlyState.Caught) caught = true;
            if (live.Events.Contains(LiveEvent.ThrowPop))
                seen.Add(new ThrowSeen(live.ThrowFromPos, live.ThrowBag, live.CoverPos, live.ThrowFrom.X, live.ThrowFrom.Z,
                    new Dictionary<string, (double X, double Z)>(live.Fielders)));
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        return (seen, play!, caught);
    }
}
