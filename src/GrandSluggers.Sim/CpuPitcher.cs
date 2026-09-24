namespace GrandSluggers.Sim;

/// <summary>
/// The CPU pitcher (spec §4.8): the SET's row, the pitch built from the inputs a hand has, and the pickoff read. It owns no
/// state: every draw is on the match's one seeded stream, in the order the match asks, so a game replays exactly.
/// </summary>
public sealed class CpuPitcher
{
    readonly Match _match;

    internal CpuPitcher(Match match) => _match = match;

    RulesTable Rules => _match.Rules;
    SimRandom Rng => _match.Streams.PitchAi;
    Character Pitcher => _match.Pitcher;
    Character Batter => _match.Batter;
    int Balls => _match.Balls;
    int Strikes => _match.Strikes;
    int Outs => _match.Outs;
    bool Over => _match.Over;
    Character? First => _match.First;
    Character? Second => _match.Second;
    Character? Third => _match.Third;
    int LeadBag => _match.LeadBag;
    bool PitcherTired => _match.PitcherTired;
    bool CanStarPitch => _match.CanStarPitch;
    LivePlaySystem LivePlay => _match.LivePlay;
    IEnumerable<Character> RunnersOn() => _match.RunnersOn();
    double PitchSpeedMph(PitchCommand pitch) => _match.PitchSpeedMph(pitch);

    /// <summary>
    /// The CPU pitcher (spec §4.8): one row of the table per SET from the count, the outs and the
    /// runners, built from the inputs a hand has and nothing else (<see cref="PitchByInputs"/>,
    /// PH-18-R1, #823).
    /// </summary>
    public PitchCommand Pitch() => PitchByInputs(out _);

    /// <summary>
    /// The CPU pitcher built from the inputs a human has and nothing else (spec §4.8, §3; PH-18,
    /// PH-18-R1, PH-02-R3/R4/R5, PH-03, PH-04).
    ///
    /// <para>Five rules, in the order this method applies them:</para>
    /// <list type="number">
    /// <item><b>Location is the rubber.</b> The row's location is a <i>horizontal</i> intent in world
    /// feet at the plate — there is no vertical intent, because a hand has no vertical input (PH-03)
    /// — and the body walks the rubber until the family's own crossing lands on it. The solve is
    /// exact because the crossing is affine in the rubber: everything else the flight does
    /// (<see cref="PitchFlight.SweepShiftFt"/>, <see cref="PitchFlight.BreakShiftFt"/>, a Star's
    /// wobble) is the same at every rubber position, so one <see cref="PitchFlight.Crossing"/> of the
    /// same delivery from the middle gives the offset and
    /// <c>r = (intent − X₀) / <see cref="HomeSet.PitcherWalk"/></c>. <c>AimX</c> and <c>AimY</c> stay
    /// 0. The arm is stamped before the solve, so a left-hander's sweep is compensated the right way
    /// (P1-d's finding).</item>
    /// <item><b>Family is presses.</b> The row's per-family weights are filtered to the slots this
    /// pitcher can actually select — in the repertoire <i>and</i> authored
    /// (<see cref="PitchSelection.IsSelectable"/>) — renormalised, and rolled once. The choice is
    /// then the 0 / 1 / 2 cycle presses it is, so the CPU cannot select what a hand cannot reach.</item>
    /// <item><b>Charge and steer are modifiers, not verbs.</b> Two independent rolls: a charged pitch
    /// is still steerable (damped by <c>breakDampedMul</c>, exactly as a human's is). Nice! stays a
    /// roll on charged pitches; Star stays as it was.</item>
    /// <item><b>Steer is what a held stick reaches.</b> <see cref="PitchFlight.BreakReach"/> over
    /// this delivery's own air time, never an instant ±1 no arm could get to.</item>
    /// <item><b>Scatter is a legal mistake.</b> The Gaussian lands on the CPU's own rubber intent, in
    /// X only, and TIRED still widens it. Fatigue lays no random miss on the delivery itself, the CPU's
    /// or a human's (PH-08-R1).</item>
    /// </list>
    /// </summary>
    /// <param name="plan">What the pitch was built from, for a scenario to read: the press count, the
    /// family those presses land on, the charge, the steer direction and the reach it was scaled by,
    /// the solved rubber and the horizontal intent it was solved for.</param>
    public PitchCommand PitchByInputs(out CpuPitchPlan plan)
    {
        var c = Rules.Pitching.Cpu;
        var row = Row();

        // (1) The horizontal intent, plus the arm's own scatter on it. No vertical term exists.
        var intentX = IntentX(row.Location, c.Locations);
        // The CPU arm's miss on its own intent is Control's (§4.8, PH-15-R6).
        var scatter = (11 - Pitcher.Stats.Control) * c.ScatterFtPerPitchStat * (PitcherTired ? c.TiredScatterMul : 1);
        intentX += Rng.Gauss() * scatter;

        // (2) The family, as presses from the fastball every SET resets to (PH-02-R5).
        var presses = Presses(row);
        var family = FamilyAfter(presses);

        // (3) Charge, then Nice! on a charged pitch.
        var charged = Rng.NextDouble() < row.ChargeChance;
        var charge = charged ? 1.0 : c.TapMin + Rng.NextDouble() * c.TapSpan;
        var nice = charged && Rng.NextDouble() < c.NiceChance;
        var star = CanStarPitch && Pitcher.Captain && Rng.NextDouble() < row.StarChance;

        // (4) The stick, held one way from release for as long as this delivery is in the air. The
        // speed is read off the delivery as it stands, which is every term AtBatResolver.PitchSpeedMph
        // looks at (family, charge, Nice!, Star, fatigue); the stick is lateral and does not reach it.
        var delivery = new PitchCommand(family, charge, star, RubberX: 0, Nice: nice, Throws: Pitcher.Throws);
        var airSec = PitchFlight.AirSeconds(PitchSpeedMph(delivery), Rules);
        var reach = PitchFlight.BreakReach(Pitcher.Stats.Control, airSec, Rules);
        var steerDir = Rng.NextDouble() < row.SteerChance ? (Rng.NextDouble() < 0.5 ? -1 : 1) : 0;
        delivery = delivery with { BreakX = steerDir * reach };

        // (5) Walk the rubber until this delivery crosses on the intent. X₀ is where it crosses from
        // the middle of the rubber; everything the flight adds after the straight line is the same
        // there as anywhere, so the difference is the walk. The clamp is the legal rubber range —
        // the one Match.WalkPitcher enforces for a hand on the stick — and a walk that runs into it
        // simply misses short, the way a pitcher who has run out of rubber does.
        var (zeroX, _) = PitchFlight.Crossing(delivery, Rules, Pitcher.StarPitch);
        var rubber = Math.Clamp((intentX - zeroX) / HomeSet.PitcherWalk, -1, 1);
        _match.PitcherOffsetX = rubber;

        plan = new CpuPitchPlan(presses, family, charged, steerDir, reach, rubber, intentX);
        return delivery with { RubberX = rubber };
    }

    /// <summary>
    /// The named location as a <b>horizontal</b> intent in world feet at the plate plane (§4.8): the
    /// sides by batter hand, and no vertical term, because a hand has no vertical input (PH-03).
    /// </summary>
    double IntentX(string location, CpuPitchLocations loc)
    {
        var away = SweetSpot.TipSign(Batter.Bats);
        var halfW = StrikeZoneGeometry.HalfWidth;
        return location switch
        {
            "waste" => away * (halfW + loc.WasteOutFt),
            "middleIn" => -away * loc.MiddleInFt,
            "middle" => 0,
            _ => (Rng.NextDouble() < loc.EdgeAwayChance ? away : -away) * (halfW - loc.EdgeInsetFt)
        };
    }

    /// <summary>
    /// How many cycle presses the CPU spends this SET (§3, §4.8). The candidates are walked the way
    /// a player walks them — <see cref="PitchSelection.Advance"/> from
    /// <see cref="PitchSelectionState.Reset"/>, which skips a slot this pitcher or this table cannot
    /// throw — so the roll is over the families presses actually reach, never over slots 0/1/2.
    /// A row that weights nothing this pitcher can select falls back to no presses at all: the
    /// fastball, the one family every pitcher throws (PH-15-R1) and the one every SET starts on.
    /// </summary>
    int Presses(CpuPitchRow row)
    {
        var authored = Rules.Pitching.Families.Authored;
        Span<double> weights = stackalloc double[Repertoire.Slots];
        var total = 0.0;
        var state = PitchSelectionState.Reset;
        for (var presses = 0; presses < Repertoire.Slots; presses++)
        {
            weights[presses] = row.Families.Of(PitchSelection.FamilyAt(state, Pitcher.Repertoire, authored));
            total += weights[presses];
            state = PitchSelection.Advance(state, true, true, default, default, Pitcher.Repertoire, authored).Next;
            // The cycle wrapped early (an unauthored second and third): there is nothing further to weigh.
            if (state.Slot == 0) { for (var rest = presses + 1; rest < Repertoire.Slots; rest++) weights[rest] = 0; break; }
        }
        if (total <= 0) return 0;

        var roll = Rng.NextDouble() * total;
        var chosen = 0;
        for (var presses = 0; presses < Repertoire.Slots; presses++)
        {
            if (weights[presses] <= 0) continue;
            // The last positive candidate is also the landing place for a roll that runs off the end
            // of the sum by a rounding step, so a zero-weight family can never be selected.
            chosen = presses;
            if (roll < weights[presses]) break;
            roll -= weights[presses];
        }
        return chosen;
    }

    /// <summary>The family this many cycle presses from a SET reset lands on, for this pitcher and this table.</summary>
    public string FamilyAfter(int presses)
    {
        var authored = Rules.Pitching.Families.Authored;
        var state = PitchSelectionState.Reset;
        for (var i = 0; i < presses; i++)
            state = PitchSelection.Advance(state, true, true, default, default, Pitcher.Repertoire, authored).Next;
        return PitchSelection.FamilyAt(state, Pitcher.Repertoire, authored);
    }

    /// <summary>Which row of §4.8 this SET reads.</summary>
    public CpuPitchRow Row()
    {
        var c = Rules.Pitching.Cpu;
        if (Outs == 2 && RunnersOn().Any()) return c.RunnerTwoOuts;
        if (Strikes == 2 && Balls <= 1) return c.Ahead;
        if (Balls >= 2 && Strikes <= 1) return c.Behind;
        return c.Even;
    }

    /// <summary>
    /// The CPU pitcher's pickoff read at SET (§4.5, §4.8): cpu.*.pickoffChance with a runner on, ×
    /// running.cpu.pickoffSeenArmMul when a steal pip is armed in SET (that runner's bag); else the
    /// lead runner, or first on the corners by the table's chance. 0 is no pickoff this SET. One seeded stream.
    /// </summary>
    public int PickoffBag()
    {
        if (Over || Outs >= 3 || LeadBag == 0 || LivePlay.Active) return 0;
        var cpu = Rules.Running.Cpu;
        var seen = _match.Runners.Where(r => r.Live && !r.Broke && r.StealArm == StealArm.Set).OrderByDescending(r => r.Bag).FirstOrDefault();
        var chance = Rules.Cpu.Active.PickoffChance * (seen is not null ? cpu.PickoffSeenArmMul : 1);
        if (Rng.NextDouble() >= chance) return 0;
        if (seen is not null) return seen.Bag;
        if (First is not null && Third is not null && Second is null && Rng.NextDouble() < cpu.PickoffFirstOnCornersChance)
            return 1;
        return LeadBag;
    }
}

/// <summary>
/// What a CPU pitch was built from (spec §4.8, #823), so a
/// scenario can read the inputs rather than infer them from the flight: the presses, the family
/// those presses land on, the charge, the stick and the rubber.
///
/// A fact about one delivery, not state: <see cref="CpuPitcher.PitchByInputs"/> hands it back beside
/// the command and keeps nothing. A readonly record struct, so the hot path allocates nothing for it.
/// </summary>
/// <param name="Presses">Cycle presses from the SET reset, 0 / 1 / 2 (PH-02-R5).</param>
/// <param name="Family">The family <paramref name="Presses"/> presses reach for this pitcher and this table.</param>
/// <param name="Charged">The charge went to MAX. An uncharged pitch still carries the row's tap.</param>
/// <param name="SteerDir">−1, 0 or +1: which way the stick was held from release, if at all.</param>
/// <param name="SteerReach">What a stick held that whole flight reaches (<see cref="PitchFlight.BreakReach"/>); the command's <c>BreakX</c> is this times <paramref name="SteerDir"/>.</param>
/// <param name="RubberX">The solved rubber, in rubber units (<see cref="HomeSet.PitcherWalk"/> feet each), clamped to the legal ±1.</param>
/// <param name="IntentX">The horizontal intent in world feet the rubber was solved for, scatter included.</param>
public readonly record struct CpuPitchPlan(
    int Presses,
    string Family,
    bool Charged,
    int SteerDir,
    double SteerReach,
    double RubberX,
    double IntentX);
