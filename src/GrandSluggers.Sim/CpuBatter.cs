namespace GrandSluggers.Sim;

/// <summary>
/// The CPU batter (spec §5.9): the swing read from one crossing, the sac-bunt square decided at SET, and the offense's
/// steal decision. It owns the decisions it has made for this pitch and this plate appearance, and the last crossing each
/// offense saw; every draw is on the match's one seeded stream, in the order the match asks.
/// </summary>
public sealed class CpuBatter
{
    readonly Match _match;
    bool _squareDecided;
    bool _squared;
    BuntSide _buntSide;
    bool _stealDecided;
    double? _awayLastCrossingX, _homeLastCrossingX;

    internal CpuBatter(Match match) => _match = match;

    RulesTable Rules => _match.Rules;
    SimRandom Rng => _match.Streams.BatAi;
    Character Pitcher => _match.Pitcher;
    Character Batter => _match.Batter;
    int Balls => _match.Balls;
    int Strikes => _match.Strikes;
    int Outs => _match.Outs;
    bool Top => _match.Top;
    bool Over => _match.Over;
    int HomeScore => _match.HomeScore;
    int AwayScore => _match.AwayScore;
    Character? First => _match.First;
    Character? Second => _match.Second;
    Character? Third => _match.Third;
    bool CanStarSwing => _match.CanStarSwing;
    LivePlaySystem LivePlay => _match.LivePlay;
    IEnumerable<Character> RunnersOn() => _match.RunnersOn();
    double PitchSpeedMph(PitchCommand pitch) => _match.PitchSpeedMph(pitch);
    bool RubberMovedSinceLastPitch => _match.RubberMovedSinceLastPitch;

    /// <summary>The pitch was thrown: the square was spent on its swing, and the next pitch reads it again at SET.</summary>
    internal void PitchSpent()
    {
        _squareDecided = false;
        _squared = false;
        _buntSide = BuntSide.None;
    }

    /// <summary>A new hitter: the steal is read again at the next SET.</summary>
    internal void NextBatter() => _stealDecided = false;

    /// <summary>The crossing this offense just saw, for the next failed re-read's guess.</summary>
    internal void SawCrossing(double x)
    {
        if (Top) _awayLastCrossingX = x;
        else _homeLastCrossingX = x;
    }

    /// <summary>
    /// The CPU batter (spec §5.9): a table read from one crossing, the same whoever is pitching. It
    /// commits from what it can see (PH-18): <see cref="ReadPitch"/>, the flight as it stands at the
    /// commit instant, and the zone, the swing / take and the box are all judged on that read; the
    /// umpire and the bat still meet the ball that is thrown. No side effects: the box it stands in and
    /// the swing it makes are the returned command. Steals are the runner AI's (<see cref="ArmSteal"/>).
    /// </summary>
    /// <param name="breakAtCommit">The stick's break as it stood at the commit instant, when the
    /// caller watched it (a client ticking the flight, a scenario steering late). Absent, the steer is
    /// taken as held one way from release, the CPU pitcher's own (S-117).</param>
    public SwingCommand Swing(PitchCommand pitch, double? breakAtCommit = null)
    {
        var c = Rules.Batting.Cpu;
        var level = Rules.Cpu.Active;
        // Two traits, not one rating (§5.9, PH-15-R5): making contact is Contact's — whether it
        // offers at a ball it cannot square up, and how far off the ball its bat arrives — while
        // swinging for it is Power's.
        var contact = Batter.Stats.Contact;
        var power = Batter.Stats.Power;
        // Commit from what can be seen: the read pitch replaces the final one for every decision below.
        pitch = ReadPitch(pitch, breakAtCommit);
        var inZone = AtBatResolver.PitchInZone(pitch, Pitcher.Stats.Control, Rules, Pitcher.StarPitch);
        var (cx, cy) = PitchFlight.Crossing(pitch, Rules, Pitcher.StarPitch);
        var zone = ZoneClass(cx, cy, inZone, c);
        var take = new SwingCommand(false, 0, 0, false);

        // Sac bunt (§5.9, §7.3): the square and its side were read at SET (<see cref="SquaresBunt"/>); in the
        // zone the held bat meets the ball (§5.8: no timed press, the same held bunt a pad lays down), out of it the
        // batter pulls the bat back and takes — the corners are in either way, that is the tell's cost.
        if (SquaresBunt())
        {
            var squareSec = c.SacBuntSquareSec;
            if (!inZone) return take with { SquareSec = squareSec };
            return SwingCommand.HeldBunt(_buntSide, TrackedBox(cx, c, level), squareSec, human: false);
        }

        var swing = zone switch
        {
            Zone.Middle => true,
            Zone.Edge => Strikes == 2 || Rng.NextDouble() < c.EdgeSwingChance,
            // Chase: a better-Contact hitter lays off the pitch it cannot square up.
            Zone.Near => Rng.NextDouble() * 100 < (Strikes == 2 ? c.ChaseTwoStrikesBase : c.ChaseBase) - contact,
            _ => false
        };
        if (!swing) return take;

        var star = CanStarSwing && Batter.Captain && inZone && (RunnersOn().Any() || Strikes == 2)
                   && Rng.NextDouble() < c.StarChance;
        var risp = Second is not null || Third is not null;
        // Forced charge: swinging for it on a hitter's count is Power's read, not Contact's.
        var forcedCharge = zone == Zone.Middle
                           && (((Balls, Strikes) is (2, 0) or (3, 1) or (3, 0)) && power >= c.ChargeBatMin
                               || risp && Outs < 2 && power >= c.RispChargeBatMin);
        var charge = forcedCharge || Rng.NextDouble() < ChargeChance(Batter, c.Archetype) ? 1.0 : 0;

        var tracked = Rng.NextDouble() < c.TrackPerfectChance;
        // Timing sigma: how far off the ball the bat arrives is Contact's (⚠️ P2-b re-reads this one).
        var err = Rng.Gauss() * (11 - contact) * c.ErrorFramesPerBatStat * level.TimingSigmaMul;
        var offSpeed = Rules.Pitching.Families.Of(pitch.Type).OffSpeed;
        if (!tracked && (offSpeed || ChargeFeel.IsCharge(pitch.Charge01)))
        {
            // Fooled: an off-speed family pulls the bat early past the ball (late), a charged pitch beats it (early).
            var fooled = c.FooledMinFrames + Rng.NextDouble() * c.FooledSpanFrames;
            err += offSpeed ? fooled : -fooled;
        }
        var box = tracked ? Math.Clamp(cx / HomeSet.BatterWalk, -1, 1) : TrackedBox(cx, c, level);
        // The stick at contact (§5.9, PH-12, PH-18). No human's stick shapes an ordinary swing, so
        // the CPU holds none: 0 / 0 and neither Gaussian is drawn. A Star Swing still steers and
        // draws both aims; the sac bunt above holds a side instead (§5.8).
        if (!AtBatResolver.StickShapesContact(bunt: false, star))
            return new SwingCommand(true, charge, err, star, BoxOffsetX: box);
        return new SwingCommand(true, charge, err, star, Rng.Gauss() * c.SpraySigmaDeg,
            LaunchAim: Rng.Gauss() * c.LaunchAimSigma, BoxOffsetX: box);
    }

    /// <summary>
    /// The pitch the CPU batter can see at its commit instant (spec §3, §5.9; PH-18, #892): the same
    /// delivery with the stick's break frozen where it stood at plate − <c>batting.cpu.decideLeadSec</c>
    /// − <c>batting.window.leadSec</c> (<see cref="AtBatMotion.CpuDecisionTime"/>). Its crossing is the
    /// flight as it stands — the family's own movement, the rubber and the break so far, with no future
    /// steering. Given no <paramref name="breakAtCommit"/>, the steer is the stick held one way from
    /// release (<see cref="PitchFlight.BreakReach"/> over the time to the commit, never more than the
    /// command carries): exactly what a hand holding it, or the CPU pitcher's drawn steer, has reached
    /// by then. Pure: no draw, no state.
    /// </summary>
    public PitchCommand ReadPitch(PitchCommand pitch, double? breakAtCommit = null)
    {
        if (breakAtCommit is { } seen) return pitch with { BreakX = Math.Clamp(seen, -1, 1) };
        if (pitch.BreakX == 0) return pitch;
        var airSec = PitchFlight.AirSeconds(PitchSpeedMph(pitch), Rules);
        var commitSec = Math.Max(0, AtBatMotion.CpuDecisionTime(airSec, Rules));
        var soFar = Math.Min(Math.Abs(pitch.BreakX), PitchFlight.BreakReach(Pitcher.Stats.Control, commitSec, Rules));
        return pitch with { BreakX = Math.Sign(pitch.BreakX) * soFar };
    }

    enum Zone { Middle, Edge, Near, Far }

    static Zone ZoneClass(double x, double y, bool inZone, CpuBatterRules c)
    {
        var dx = Math.Abs(x);
        var dy = Math.Abs(y - StrikeZoneGeometry.CenterY);
        if (inZone)
            return dx <= StrikeZoneGeometry.HalfWidth * c.MiddleFraction && dy <= StrikeZoneGeometry.Height / 2 * c.MiddleFraction
                ? Zone.Middle
                : Zone.Edge;
        var outX = Math.Max(0, dx - StrikeZoneGeometry.HalfWidth);
        var outY = Math.Max(0, dy - StrikeZoneGeometry.Height / 2);
        return Math.Sqrt(outX * outX + outY * outY) <= c.NearFt ? Zone.Near : Zone.Far;
    }

    /// <summary>
    /// The box after a failed re-read (spec §5.9 tracking): the guess is the last crossing this
    /// offense saw (the first pitch guesses the middle) plus a fixed offset; the miss is likelier
    /// when the pitcher moved on the rubber since the last pitch.
    /// </summary>
    double TrackedBox(double crossingX, CpuBatterRules c, CpuLevelRules level)
    {
        var last = Top ? _awayLastCrossingX : _homeLastCrossingX;
        var chance = Math.Min(c.MistrackChanceMax, (RubberMovedSinceLastPitch ? c.MistrackMovedChance : c.MistrackChance) * level.MistrackMul);
        var guess = Rng.NextDouble() < chance ? (last ?? 0) : crossingX;
        var offset = (c.MistrackMinFt + Rng.NextDouble() * c.MistrackSpanFt) * (Rng.NextDouble() < 0.5 ? -1 : 1);
        return Math.Clamp((guess + offset) / HomeSet.BatterWalk, -1, 1);
    }

    /// <summary>
    /// Charge vs slap by archetype (spec §5.9). The technique gate is <b>Contact</b> and Run — the
    /// hitter who can both square it up and beat it out slaps — while the slugger-vs-speedster split
    /// is <b>Power</b> against Run (PH-15-R5).
    /// </summary>
    public static double ChargeChance(Character who, CpuArchetypeRules a)
    {
        var contact = who.Stats.Contact;
        var power = who.Stats.Power;
        var run = who.Stats.Run;
        if (contact >= a.TechniqueMin && run >= a.TechniqueMin) return a.Technique;
        if (power - run >= a.SplitStat) return a.Power;
        if (run - power >= a.SplitStat) return a.Speed;
        return a.Balanced;
    }

    /// <summary>The CPU batter is squared to bunt on this pitch (§7.3): the tell a human pitcher sees before the pitch.</summary>
    public bool Squared => _squared;

    /// <summary>
    /// The side the squared CPU batter holds on this pitch (§5.8, §5.9; PH-14-R2), decided at SET with the square so
    /// the bat angle is a tell before the pitch. <see cref="BuntSide.None"/> when it is not squared.
    /// </summary>
    public BuntSide BuntSide => _squared ? _buntSide : BuntSide.None;

    /// <summary>
    /// The CPU batter's sac-bunt read (§5.9's row), once per pitch at SET so the square is a tell the defense
    /// reads before the pitch (§7.3): runner on first only, no outs, a light bat, a close game, at the table's
    /// chance, on the one seeded stream. At the plate plane the square is the bunt if the pitch is in the zone
    /// and a take otherwise (<see cref="Swing"/>). Idempotent for the pitch; <see cref="Match.BeginAtBat"/> clears it.
    /// </summary>
    public bool SquaresBunt()
    {
        if (_squareDecided) return _squared;
        if (Over || Outs >= 3 || LivePlay.Active) return false;
        _squareDecided = true;
        var c = Rules.Batting.Cpu;
        var trailing = Top ? HomeScore - AwayScore : AwayScore - HomeScore;
        // The light bat that gives itself up is the weak-Contact hitter (§5.9).
        _squared = First is not null && Second is null && Third is null && Outs == 0
                      && Batter.Stats.Contact <= c.SacBuntBatMax && trailing <= c.SacBuntTrailMax
                      && Rng.NextDouble() < c.SacBuntChance;
        // The side is held with the square (PH-14-R2): one draw, only when it squares.
        _buntSide = _squared
            ? (Rng.NextDouble() < c.SacBuntFirstSideChance ? BuntSide.First : BuntSide.Third)
            : BuntSide.None;
        return _squared;
    }

    /// <summary>
    /// The CPU offense's steal decision (§11.6): the runner AI's table, once per at-bat at SET, on
    /// the one seeded stream. A chosen runner departs now and is exposed to a legal pitcher throw.
    /// </summary>
    public bool ArmSteal()
    {
        if (_stealDecided || Over || Outs >= 3 || LivePlay.Active) return false;
        _stealDecided = true;
        var trailing = Top ? HomeScore - AwayScore : AwayScore - HomeScore;
        var plan = RunnerAi.StealPlan(_match.Runners, Batter.Captain, Outs, trailing, Rng, Rules);
        var any = false;
        foreach (var (runner, _) in plan)
        {
            any |= _match.StartStealAt(runner.FromBag);
        }
        return any;
    }
}
