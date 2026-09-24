namespace GrandSluggers.Sim;

public sealed partial class Match
{
    public const int DefaultInnings = 3;

    readonly AtBatResolver _atBat;
    readonly FieldingResolver _fielding;
    /// <summary>The only random stream that may decide a play. Seeded per match; every roll is a sim call.</summary>
    readonly Random _rng;
    readonly Dictionary<string, int> _mvp = new(StringComparer.OrdinalIgnoreCase);
    readonly List<PlayEvent> _log = [];

    public ContentCatalog Content { get; }
    /// <summary>
    /// The park this match plays on, resolved once from the catalog's park (<see cref="PlayedPark.Of"/>,
    /// FD-11, FD-10-R1): the day's hazard instances, then the night block's when the match is at
    /// <see cref="Night"/>, then — with <see cref="Hazards"/> off — every hazard instance removed and
    /// every other member the same value (<see cref="HazardPattern.HazardsOff"/>). A park with no night
    /// block, played with hazards on, is the catalog's own object. Every reader — both seats, the CPU,
    /// the live ball, the trace, the presentation — reads this one, so no seat can play a hazard the
    /// others do not, and nothing reads a night block but the resolution.
    /// </summary>
    public Park Park { get; }
    public bool Night { get; }
    /// <summary>
    /// Park hazards on (the default) or off (FD-10, §14, SF-24). Off removes the instances whose
    /// pattern <see cref="HazardPattern.IsHazard">counts as a hazard</see>, the night block's with the
    /// day's (FD-10-R1), and changes no other rule: the park keeps its size, fence, walls, air, wind,
    /// ground zones, foul territory and depth, and the table this match plays on is the one it plays
    /// on with hazards on.
    /// </summary>
    public bool Hazards { get; }
    public Team Away { get; private set; }
    public Team Home { get; private set; }
    public IReadOnlyList<Character> AwayOrder { get; }
    public IReadOnlyList<Character> HomeOrder { get; }
    public int Innings { get; }
    public int Inning { get; private set; } = 1;
    public bool Top { get; private set; } = true;
    public int Outs { get; private set; }
    public int Balls { get; private set; }
    public int Strikes { get; private set; }
    public int AwayScore { get; private set; }
    public int HomeScore { get; private set; }
    /// <summary>
    /// The bodies on the basepath (spec §9.1): the seated runners between plays, plus the
    /// batter-runner from contact to Complete. <see cref="RunnerAt"/> finds one by the bag it
    /// started the play on; the same objects are re-seated at Complete, never rebuilt.
    /// </summary>
    readonly List<Runner> _runners = [];
    public IReadOnlyList<Runner> Runners => _runners;
    /// <summary>The runner who started this play on first / second / third (between plays: who stands there).</summary>
    public Character? First => RunnerAt(1)?.Who;
    public Character? Second => RunnerAt(2)?.Who;
    public Character? Third => RunnerAt(3)?.Who;
    /// <summary>Mercy rule on (spec §1): off below the table's scheduled-innings floor whatever this says.</summary>
    public bool Mercy { get; }
    public bool StarsEnabled { get; }
    int _selectedBag;
    bool _pickedRunner;
    public int AwayBatter { get; private set; }
    public int HomeBatter { get; private set; }
    public double AwayStars { get; private set; }
    public double HomeStars { get; private set; }
    /// <summary>Each character's own arm for the match (spec §4.7), keyed by id; a pool is opened on first use.</summary>
    readonly Dictionary<string, int> _stamina = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>The defense in glove order (Diamond.Order after the pitcher); a swap trades two slots.</summary>
    readonly List<Character> _homeDefense;
    readonly List<Character> _awayDefense;
    bool _swappedThisHalf;
    /// <summary>What the CPU batter remembers between pitches (spec §5.9 tracking): the last crossing seen by each offense, and the rubber the last pitch left from.</summary>
    double? _awayLastCrossingX, _homeLastCrossingX;
    double _lastPitchRubberX;
    Character _homePitcher;
    Character _awayPitcher;
    public bool Over { get; private set; }
    public IReadOnlyList<PlayEvent> Log => _log;
    public ChemistryTable Chemistry => Content.Chemistry;
    readonly RulesTable _rules;
    /// <summary>The rule numbers this match plays by (data/rules), at this match's difficulty rung (§16 <c>cpu.json</c>).</summary>
    public RulesTable Rules => _rules;
    /// <summary>The difficulty rung in play: easy / normal / hard (<see cref="CpuRules.Levels"/>).</summary>
    public string Difficulty => Rules.Cpu.Level;
    /// <summary>Portable command boundary for the ball between contact and Time.</summary>
    public LivePlaySystem LivePlay { get; }
    /// <summary>The live runners and pitcher commitment before contact or the catch.</summary>
    public PitchSetupSystem PitchSetup { get; }
    public RunnerOrders ControllerRunners { get; } = new();
    /// <summary>The seed this match was constructed with. Tracing and <c>cli match --seed</c> both read it.</summary>
    public int Seed { get; }
    bool _tracing;
    readonly List<PlayTrace> _traces = [];
    /// <summary>
    /// Opt-in per-play geometry dump (agent-rails R3). Off, AutoPlay is unchanged. On, each
    /// <see cref="Play"/> / <see cref="Pickoff"/> freezes a <see cref="PlayTrace"/> of ball,
    /// runners, glove, and bags. The dump does not decide baseball.
    /// </summary>
    public bool Tracing
    {
        get => _tracing;
        set
        {
            _tracing = value;
            LivePlay.Recording = value;
            _traces.Clear();
        }
    }
    public IReadOnlyList<PlayTrace> Traces => _traces;
    public PlayTraceLog TraceLog() =>
        new(Seed, Home.Captain.Id, Away.Captain.Id, Park.Id, _traces.ToArray(),
            SchemaVersion: 2,
            Identity: PlayTraceIdentity.Capture(this),
            Trial: PlayTraceTrial.For(Content.Root), StarsEnabled: StarsEnabled ? null : false);

    public Match(ContentCatalog content, Team away, Team home, Park park, int innings = DefaultInnings, int seed = 1, bool night = false, bool mercy = true, string? difficulty = null, bool hazards = true, bool stars = true)
    {
        Content = content;
        // The table resolves from the park as the catalog authored it, so neither the switch nor the
        // night block can reach a rule: a hazards-off or a night match plays the very table its
        // hazards-on day twin plays (SF-01, FD-10, FD-11-R2).
        _rules = content.Rules.AtLevel(difficulty).AtPark(park);
        Mercy = mercy;
        StarsEnabled = stars;
        Away = away;
        Home = home;
        Hazards = hazards;
        Night = night;
        // The one resolution (FD-11, F4-d): the night block's instances join the day's at night, then
        // the switch removes every hazard when hazards are off. No other line reads a night block.
        Park = PlayedPark.Of(park, night, hazards, _rules.Hazards);
        Innings = innings;
        Seed = seed;
        _rng = new Random(seed);
        // Both resolvers play on the match's resolved table (§0.3, FD-03, FR-01), never the catalog's
        // global one: the at-bat's own flight and the fielding preview are the first ball of a play, so a
        // park that names its air has to reach them the same way it reaches the live ball's continuation.
        // The rung comes with it — the preview's CPU reaction lockouts are the ones LivePlaySystem waits.
        _atBat = new AtBatResolver(content.Chemistry, _rules, content.StarSkills);
        _fielding = new FieldingResolver(content.Chemistry, _rules);
        LivePlay = new LivePlaySystem(this);
        PitchSetup = new PitchSetupSystem(this);
        AwayOrder = away.BattingOrder;
        HomeOrder = home.BattingOrder;
        // One pool per team, the same usable reserve for both, set once here and never again (PH-16-R4, R6, R16):
        // chemistry does not move it, and no half, role change or restart of a play grants it again.
        AwayStars = stars ? _rules.Stars.StartingReserve : 0;
        HomeStars = stars ? _rules.Stars.StartingReserve : 0;
        _homePitcher = home.Pitcher;
        _awayPitcher = away.Pitcher;
        _homeDefense = home.Roster.ToList();
        _awayDefense = away.Roster.ToList();
        HomeBat = GearMesh.SignatureBat(content, home.Captain.Id);
        AwayBat = GearMesh.SignatureBat(content, away.Captain.Id);
        // The sides' gloves are the match rules' (match.json), checked against data/gloves by the content validator.
        HomeGlove = content.Gloves[_rules.Match.HomeGlove];
        AwayGlove = content.Gloves[_rules.Match.AwayGlove];
    }

    public BatItem HomeBat { get; private set; } = null!;
    public BatItem AwayBat { get; private set; } = null!;
    public GloveItem HomeGlove { get; private set; } = null!;
    public GloveItem AwayGlove { get; private set; } = null!;
    public BatItem OffenseBat => Top ? AwayBat : HomeBat;
    public GloveItem DefenseGlove => Top ? HomeGlove : AwayGlove;

    public void CycleBat(bool home)
    {
        var ids = Content.Bats.Keys.OrderBy(k => k).ToList();
        if (ids.Count == 0) return;
        if (home)
            HomeBat = Content.Bats[ids[(ids.IndexOf(HomeBat.Id) + 1) % ids.Count]];
        else
            AwayBat = Content.Bats[ids[(ids.IndexOf(AwayBat.Id) + 1) % ids.Count]];
    }

    public void CycleGlove(bool home)
    {
        var ids = Content.Gloves.Keys.OrderBy(k => k).ToList();
        if (ids.Count == 0) return;
        if (home)
            HomeGlove = Content.Gloves[ids[(ids.IndexOf(HomeGlove.Id) + 1) % ids.Count]];
        else
            AwayGlove = Content.Gloves[ids[(ids.IndexOf(AwayGlove.Id) + 1) % ids.Count]];
    }

    public static Match Slice(ContentCatalog content, int innings = DefaultInnings, int seed = 1, string parkId = ExhibitionPick.DefaultPark, bool night = false) =>
        new(content, PresetTeams.EmberCourt(content), PresetTeams.SparkAllStars(content), content.MustPark(parkId), innings, seed, night);

    public static Match Exhibition(
        ContentCatalog content,
        string homeCaptain = "rio",
        string awayCaptain = "ashlord",
        int innings = DefaultInnings,
        int seed = 1,
        string? parkId = null,
        bool night = false,
        string? difficulty = null,
        bool hazards = true,
        bool mercy = true,
        bool stars = true)
    {
        var (home, away) = PresetTeams.Pair(content, homeCaptain, awayCaptain);
        return Exhibition(content, home, away, innings, seed, parkId ?? PresetTeams.HomeParkId(content, homeCaptain), night, difficulty, hazards, mercy, stars);
    }

    public static Match Exhibition(
        ContentCatalog content,
        Team home,
        Team away,
        int innings = DefaultInnings,
        int seed = 1,
        string? parkId = null,
        bool night = false,
        string? difficulty = null,
        bool hazards = true,
        bool mercy = true,
        bool stars = true)
    {
        parkId ??= PresetTeams.HomeParkId(content, home.Captain.Id);
        return new Match(content, away, home, content.MustPark(parkId), innings, seed, night, difficulty: difficulty, hazards: hazards, mercy: mercy, stars: stars);
    }

    /// <summary>
    /// Still-gate only. Flip the top without playing three outs so Play can
    /// photograph batting SET / scoop / star without a pad grinding the half.
    /// </summary>
    public void SkipToHomeHalf()
    {
        if (Over || !Top) return;
        Outs = 0;
        Balls = 0;
        Strikes = 0;
        ClearBags();
        Top = false;
    }

    /// <summary>Home captain in the box. Still-gate plate / star.</summary>
    public void SkipToHomeCaptainAtBat()
    {
        if (Over) return;
        SkipToHomeHalf();
        for (var i = 0; i < HomeOrder.Count; i++)
        {
            if (!HomeOrder[i].Id.Equals(Home.Captain.Id, StringComparison.OrdinalIgnoreCase))
                continue;
            HomeBatter = i;
            return;
        }
    }

    public void GiveOffenseStars(double n)
    {
        if (!StarsEnabled) return;
        n = Math.Clamp(n, 0, 5);
        if (Top) AwayStars = Math.Max(AwayStars, n);
        else HomeStars = Math.Max(HomeStars, n);
    }

    public void GiveDefenseStars(double n)
    {
        if (!StarsEnabled) return;
        n = Math.Clamp(n, 0, Rules.Stars.MeterMax);
        if (Top) HomeStars = Math.Max(HomeStars, n);
        else AwayStars = Math.Max(AwayStars, n);
    }

    /// <summary>
    /// A lesson's short pool (§12, PH-16-R12): the defense's Stars set to exactly <paramref name="n"/>, so the Star
    /// Pitch it asks for can be unaffordable. Only a tutorial setup that names <c>poolStars</c> calls it; a match's
    /// pools start on the one reserve and move only by gains and prices.
    /// </summary>
    internal void SetDefenseStarsForLesson(double n)
    {
        if (!StarsEnabled) return;
        n = Math.Clamp(n, 0, Rules.Stars.MeterMax);
        if (Top) HomeStars = n;
        else AwayStars = n;
    }

    public Team Offense => Top ? Away : Home;
    public Team Defense => Top ? Home : Away;
    public Character Batter => (Top ? AwayOrder : HomeOrder)[Top ? AwayBatter : HomeBatter];
    public Character Pitcher => Top ? _homePitcher : _awayPitcher;
    /// <summary>The defense in glove order: <see cref="FieldingResolver.Assign"/> reads it, the swap reorders it.</summary>
    public IReadOnlyList<Character> DefenseRoster => Top ? _homeDefense : _awayDefense;
    /// <summary>The pitcher's own arm (spec §4.7). Can go below zero: exhausted.</summary>
    public int PitcherStamina => StaminaOf(Pitcher);
    public int PitcherStaminaMax => StaminaPool(Pitcher);
    public bool PitcherExhausted => PitcherStamina < 0;
    /// <summary>Once per half-inning (spec §4.7).</summary>
    public bool CanSwapPitcher => !_swappedThisHalf && !Over;
    public Character? OnDeck
    {
        get
        {
            var order = Top ? AwayOrder : HomeOrder;
            var i = ((Top ? AwayBatter : HomeBatter) + 1) % order.Count;
            return order[i];
        }
    }

    public IEnumerable<Character> RunnersOn()
    {
        if (First is not null) yield return First;
        if (Second is not null) yield return Second;
        if (Third is not null) yield return Third;
    }

    public double OffenseStars => Top ? AwayStars : HomeStars;
    public double DefenseStars => Top ? HomeStars : AwayStars;
    /// <summary>A steal is armed on any runner (§11.1): the HUD tell and the CPU pitcher's read.</summary>
    public bool StealOn => _runners.Any(r => r.Live && r.StealArmed);
    public double Dash01 { get; set; }
    /// <summary>The CPU steal table runs once per at-bat (§11.6).</summary>
    bool _cpuStealDecided;
    // The CPU batter's square (§5.9, §7.3): read once per pitch at SET, spent at the plate plane.
    bool _cpuSquareDecided;
    bool _cpuSquared;
    BuntSide _cpuBuntSide;

    sealed record PlayOrigin(PlayContext Context, Character Batter, Character Pitcher);
    PlayOrigin? _pendingPlay;
    /// <summary>Every out and every runner placement on the current play, in order. Emit stamps them on the outcome.</summary>
    readonly List<OutRecord> _outsThisPlay = [];
    readonly List<RunnerMove> _movesThisPlay = [];
    /// <summary>The specials released on the current pitch, as settled (§12, PH-16-R12). Emit stamps them on the outcome.</summary>
    readonly List<StarRequest> _starRequestsThisPlay = [];
    int _outsOnCurrentPlay => _outsThisPlay.Count;
    public double PitcherOffsetX { get; private set; }
    public double BatterOffsetX { get; private set; }
    /// <summary>World-X box offset held from bat-ball contact into the live run.</summary>
    public double BatterContactOffsetX { get; private set; }
    public bool PitcherTired => PitcherStamina < Rules.Pitching.Stamina.TiredBelow;
    public bool Paused { get; private set; }
    /// <summary>
    /// All-advance armed before the pitch or before the catch: every runner tags and goes at the
    /// catch (spec §9.5). During a live ball the same press sends every runner now.
    /// </summary>
    public bool SendAll { get; private set; }
    /// <summary>Furthest occupied bag. Default selection until the pad names a runner.</summary>
    public Character? LeadRunner => Third ?? Second ?? First;
    public int LeadBag => Third is not null ? 3 : Second is not null ? 2 : First is not null ? 1 : 0;
    /// <summary>Pad-named runner by the bag they started on (1/2/3; 0 the batter-runner during a live ball). Defaults to the lead runner.</summary>
    public int SelectedBag => _selectedBag;
    public Character? SelectedRunner => SelectedState?.Who;
    public Runner? SelectedState => RunnerAt(SelectedBag);
    public bool StealAttempt => SelectedState?.StealArmed ?? false;
    /// <summary>The lead occupied bag whose steal is armed, else 0 (any number may be armed, §11.1).</summary>
    public int ArmedStealBag
    {
        get
        {
            for (var bag = 3; bag >= 1; bag--)
                if (RunnerAt(bag)?.StealArmed == true) return bag;
            return 0;
        }
    }
    /// <summary>The bag the lead armed runner is stealing, else the selected runner's next bag (home included, D10).</summary>
    public int StealTargetBag
    {
        get
        {
            var armed = RunnerAt(ArmedStealBag)?.StealTarget ?? 0;
            return armed > 0 ? armed : Baserunning.StealTarget(SelectedBag);
        }
    }
    public bool CanSteal => CanStealFrom(SelectedBag);

    /// <summary>A steal is offered toward an open bag, or toward a bag whose runner is armed too (the double steal, §11.1).</summary>
    public bool CanStealFrom(int fromBag)
    {
        var body = RunnerAt(fromBag);
        if (Over || Paused || Outs >= 3 || body is null || body.Bag is < 1 or > 3) return false;
        return !_runners.Any(ahead => ahead != body && ahead.Live && ahead.Bag == body.NextBag && !ahead.Advancing);
    }
    /// <summary>
    /// A runner broke on the pitch and the ball is dead in the catcher's glove (§11.3): the catcher's
    /// throw play is next — <see cref="RunStealPlay"/> headless, or the client's ticks.
    /// </summary>
    public bool StealThrowPending =>
        !Over && Outs < 3 && !LivePlay.Active && _runners.Any(r => (r.Live || r.Scored) && r.Broke);

    /// <summary>The live runner who started the play on <paramref name="fromBag"/> (0 the batter-runner), or null.</summary>
    public Runner? RunnerAt(int fromBag)
    {
        foreach (var r in _runners)
            if (r.FromBag == fromBag && r.Live) return r;
        return null;
    }

    /// <summary>The batter as a body, from contact until Complete seats or retires them.</summary>
    public Runner? BatterRunner => RunnerAt(0);

    /// <summary>A live runner's last touched bag is this one: the mini diamond's pip (between plays: seated there).</summary>
    public bool Occupied(int bag)
    {
        foreach (var r in _runners)
            if (r.Live && r.Bag == bag) return true;
        return false;
    }

    /// <summary>D-pad: right 1B, up 2B, left 3B; down (4) is the batter-runner while the ball is live (§9.3).</summary>
    public bool SelectRunner(int bag)
    {
        if (bag == 4)
        {
            if (BatterRunner is null) return false;
            _selectedBag = 0;
            _pickedRunner = true;
            return true;
        }
        if (!Baserunning.CanSelect(bag, First is not null, Second is not null, Third is not null))
            return false;
        _selectedBag = bag;
        _pickedRunner = true;
        return true;
    }

    /// <summary>Stick back on the selected runner: return from the current position in every phase (§9.3).</summary>
    public bool ReturnToBag() => ReturnToBagAt(SelectedBag);

    public bool ReturnToBagAt(int fromBag)
    {
        var state = RunnerAt(fromBag);
        if (state is null) return false;
        state.CancelSteal();
        state.Return(human: true);
        return true;
    }

    /// <summary>Stick toward the next bag on the selected runner during a live ball: send them (§9.3). Forced runners are already going.</summary>
    public bool SendRunner() => SendRunnerAt(SelectedBag);

    public bool SendRunnerAt(int fromBag)
    {
        var state = RunnerAt(fromBag);
        if (state is null) return false;
        if (!LivePlay.Active) return StartStealAt(fromBag);
        state.Send(state.NextBag, human: true);
        return true;
    }

    /// <summary>All-advance: live, send every runner (§9.3, §9.5); before the pitch, depart immediately. The client reads LB for it only once the ball is live: during SET and the flight LB is the held special modifier (PH-16-R17).</summary>
    public bool AdvanceAll()
    {
        if (Over || Outs >= 3) return false;
        if (!LivePlay.Active)
        {
            var sent = false;
            foreach (var runner in _runners.Where(r => r.Live).OrderByDescending(r => r.Progress))
                sent |= StartStealAt(runner.FromBag);
            return sent;
        }
        var any = false;
        foreach (var r in _runners)
        {
            if (!r.Live) continue;
            any = true;
            if (LivePlay.Active)
            {
                if (LivePlay.Fly == FlyState.InAir && !r.IsBatter) r.SetTagAndGo(true);
                else r.Send(r.NextBag, human: true);
            }
        }
        if (any) SendAll = true;
        return any;
    }

    /// <summary>RB: before the pitch, cancel every steal and the tag-and-go; live, every runner comes back (§9.3).</summary>
    public bool ReturnAll()
    {
        var any = false;
        foreach (var r in _runners)
        {
            if (!r.Live) continue;
            any = true;
            r.SetTagAndGo(false);
            r.Return(human: true);
        }
        SendAll = false;
        ClearSteal();
        return any;
    }

    /// <summary>Both shoulders: hold every runner where they stand (§9.3). Forced runners keep going.</summary>
    public bool FreezeRunners()
    {
        if (_runners.All(r => !r.Live)) return false;
        SendAll = false;
        ClearSteal();
        foreach (var r in _runners)
            if (r.Live) r.Halt();
        return true;
    }

    /// <summary>Stick toward a bag + halt: freeze that runner only.</summary>
    public bool HaltAt(int fromBag)
    {
        var state = RunnerAt(fromBag);
        if (state is null) return false;
        state.CancelSteal();
        state.Halt();
        return true;
    }

    /// <summary>
    /// Start the selected body now (§11.1). The compatibility timestamp is not a movement credit;
    /// PitchSetup.Advance moves the body until contact or catcher possession. Other runners keep their orders.
    /// </summary>
    public bool StartSteal(double windupSec = -1) => StartStealAt(SelectedBag, windupSec);

    public bool StartStealAt(int fromBag, double windupSec = -1)
    {
        if (!CanStealFrom(fromBag) || LivePlay.Active) return false;
        var state = RunnerAt(fromBag);
        if (state is null || state.Bag >= 4) return false;
        // A press is a departure. The legacy timestamp parameter grants no retroactive jump.
        state.ArmSteal(StealArm.Set);
        state.Break(state.Feet);
        return true;
    }

    /// <summary>West / South near the bag: slide in even without a tag threatened (§9.4).</summary>
    public bool Slide() => SlideAt(SelectedBag);

    public bool SlideAt(int fromBag)
    {
        var state = RunnerAt(fromBag);
        if (state is null) return false;
        state.RequestSlide();
        return true;
    }

    /// <summary>Station a runner for a drill or a live setup. Does not arm a steal.</summary>
    public bool StationRunner(int bag, Character who)
    {
        if (Over || who is null || bag is < 1 or > 3 || LivePlay.Active) return false;
        _runners.RemoveAll(r => r.Bag == bag || r.Who.Id == who.Id);
        _runners.Add(new Runner(who, bag));
        SyncSelection();
        return true;
    }

    /// <summary>Set the out count for a drill or a scenario (0–2). Not during a live ball.</summary>
    public bool SetOuts(int outs)
    {
        if (Over || LivePlay.Active || outs is < 0 or > 2) return false;
        Outs = outs;
        return true;
    }

    /// <summary>
    /// Retire the batter (bag 0) or a runner by their bag at contact. Every live out
    /// passes through here so occupancy and the contact-time force chain cannot diverge.
    /// </summary>
    internal bool RetireLiveRunner(int fromBag, int atBag, OutType type, Character? fielder)
    {
        var runner = RunnerAt(fromBag);
        if (runner is null || fromBag is < 0 or > 3) return false;
        var batterShortOfFirst = runner.IsBatter && runner.Bag < 1;
        runner.Retire();
        RecordOut(type, atBag, fromBag, runner.Who, fielder, batterShortOfFirst);
        AddMvp(fielder?.Id ?? Pitcher.Id, Rules.Stars.Mvp.PutOut);
        AddStars(defense: true, Rules.Stars.Gains.LiveOut);
        SyncSelection();
        return true;
    }

    /// <summary>Play seconds of the third out of a live ball, and whether it wipes the runs that crossed before it (spec §1).</summary>
    double _thirdOutAt = double.PositiveInfinity;
    bool _thirdOutKillsRuns;

    /// <summary>The one place an out is counted: the tally and the typed record move together.</summary>
    void RecordOut(OutType type, int atBag, int fromBag, Character runner, Character? fielder, bool batterShortOfFirst = false)
    {
        Outs++;
        _outsThisPlay.Add(new OutRecord(type, atBag, fromBag, runner, fielder));
        if (Outs >= 3 && LivePlay.Active && double.IsPositiveInfinity(_thirdOutAt))
        {
            _thirdOutAt = LivePlay.ElapsedSeconds;
            // A run does not count when the third out is a force, or the batter-runner is retired before first (§1).
            _thirdOutKillsRuns = type is OutType.Force or OutType.ThrowOutAtFirst || batterShortOfFirst;
        }
    }

    /// <summary>
    /// Contact: the batter becomes a body in the box, every seated runner snapshots its force
    /// (spec §9.1) and the all-advance intent (tag and go, §9.5). Called by the live ball's Begin.
    /// </summary>
    internal void BeginRunners(double startX, double startZ)
    {
        _runners.RemoveAll(r => r.IsBatter || r.Out);
        var forces = InPlay.ForceState.FromOccupancy(First is not null, Second is not null, Third is not null);
        foreach (var r in _runners)
        {
            r.BeginPlay(forces.At(r.Bag + 1), SendAll);
            // A runner who broke on the pitch is on the path with their head start (§11.2): the steal is running now (S-64).
        }
        _runners.Add(Runner.BatterRunner(Batter, startX, startZ));
        _thirdOutAt = double.PositiveInfinity;
        _thirdOutKillsRuns = false;
    }

    /// <summary>
    /// The dead-ball runner play (§11.3, §11.4): the catcher's throw after a take or a miss, or the
    /// pickoff from the rubber. The pitch's play continues (its strikeout stays on the play: two outs
    /// on one pitch is a DOUBLE PLAY), there is no batter body, and each departing runner retains the position and direction already reached.
    /// </summary>
    internal void BeginRunnerPlay(PlayEvent? pitch, bool pickoff)
    {
        if (pitch?.Context is { } context)
            _pendingPlay = new PlayOrigin(context, pitch.Batter, pitch.Pitcher);
        else
            CurrentPlay();
        _runners.RemoveAll(r => r.IsBatter || r.Out);
        foreach (var r in _runners)
        {
            r.BeginPlay(forced: false, tagAndGo: false);
        }
        _thirdOutAt = double.PositiveInfinity;
        _thirdOutKillsRuns = false;
    }

    /// <summary>
    /// Time on a runner play (§10.6, §11.3): the bodies are seated where they stand and the pitch's
    /// event is completed from the typed facts — a runner out is CAUGHT STEALING (PICKED OFF on the
    /// pickoff play), a runner who took a bag is a STOLEN BASE, and a body back on its bag leaves the
    /// pitch as it was. A run that crossed (the steal of home, D10) counts by §1.
    /// </summary>
    internal PlayEvent FinishRunnerPlay(PlayEvent pitch, int pickoffBag, int firstThrowBag)
    {
        CurrentPlay();
        var pickoff = pickoffBag > 0;
        var movesBefore = _movesThisPlay.Count;
        var bodies = LivePlay.BodiesNow();
        var (runs, scorers) = SettleRunners(out _);
        var outs = _outsThisPlay.Where(o => o.FromBag != 0).ToList();
        var advances = _movesThisPlay.Skip(movesBefore).Where(m => m.FromBag != 0 && m.ToBag > m.FromBag).ToList();
        var kind = pitch.Kind;
        var caption = pitch.Caption;
        var result = RunnerPlayResult.None;
        var fromBag = 0;
        var toBag = 0;
        var error = false;
        if (outs.Count > 0)
        {
            kind = PlayKind.CaughtStealing;
            result = pickoff ? RunnerPlayResult.PickedOff : RunnerPlayResult.CaughtStealing;
            fromBag = outs[0].FromBag;
            toBag = outs[0].Bag;
            var live = LivePlay.Caption;
            var named = string.Join(" ", outs.Select(o => $"{o.Runner.Name} {(pickoff ? "picked off" : "caught stealing")}."));
            caption = string.IsNullOrEmpty(live) ? $"{caption}  {named}" : $"{caption}  {live} {named}";
        }
        else if (advances.Count > 0)
        {
            kind = PlayKind.StolenBase;
            result = RunnerPlayResult.StolenBase;
            var lead = advances.OrderByDescending(m => m.ToBag).First();
            fromBag = lead.FromBag;
            toBag = lead.ToBag;
            // A sailed throw that let the runner take the bag is the ERROR (§8.5), not the steal.
            error = LivePlay.ThrowSailed;
            foreach (var m in advances)
            {
                AddMvp(m.Runner.Id, Rules.Stars.Mvp.StolenBase);
                AddStars(defense: false, Rules.Stars.Gains.StolenBase);
            }
            var named = string.Join(" ", advances.OrderByDescending(m => m.ToBag).Select(m => $"{m.Runner.Name} steals {InPlay.BagName(m.ToBag)}."));
            caption = $"{caption}  {named}";
        }
        else if (pickoff)
        {
            var back = _runners.FirstOrDefault(r => r.Live && r.Bag == pickoffBag)?.Who;
            caption = back is not null ? $"{back.Name} back to the bag." : caption;
        }
        var facts = (pitch.Outcome ?? PlayOutcome.Empty) with
        {
            RunnerResult = result,
            RunnerFromBag = fromBag,
            RunnerToBag = toBag,
            ThrowEndpoint = new ThrowEndpoint(pickoff ? ThrowOrigin.PitcherRubber : ThrowOrigin.Catcher, firstThrowBag > 0 ? firstThrowBag : pickoffBag),
            Error = error,
            Bodies = bodies
        };
        var ev = pitch with
        {
            Kind = kind,
            Caption = caption,
            RunsScored = pitch.RunsScored + runs,
            Scorers = pitch.Scorers.Concat(scorers).ToList(),
            Fielder = LivePlay.LastMoment?.Fielder ?? pitch.Fielder,
            Throw = LivePlay.ArmedThrow ?? pitch.Throw,
            Outcome = facts
        };
        LivePlay.Reset();
        CheckInning();
        EndIfWalkOff();
        return FinishEvent(ev);
    }

    /// <summary>Runs that crossed before the third out (spec §1): with fewer than three outs every crossing counts.</summary>
    bool RunCounts(Runner runner) =>
        runner.Scored && (Outs < 3 || (runner.ScoredAt < _thirdOutAt && !_thirdOutKillsRuns));

    /// <summary>The runner's bag on the last pitch, before this play's placement: the walk / steal / Complete tables read it by object.</summary>
    void PruneRunners()
    {
        _runners.RemoveAll(r => !r.Live);
        SyncSelection();
    }

    void RecordMove(Character runner, int fromBag, int toBag)
    {
        if (fromBag != toBag) _movesThisPlay.Add(new RunnerMove(runner, fromBag, toBag));
    }

    internal void PrepareLivePlay() => CurrentPlay();

    /// <summary>One authoritative handling outcome per qualifying take (#721, F693-02-ordinary-handling-error-chance): a draw only when there is a chance.</summary>
    internal bool RollHandling(double chance) => chance > 0 && _rng.NextDouble() < chance;

    /// <summary>One seeded directional result per failed contact (F693-02-uniform-error-direction): uniform in ±<paramref name="spreadDeg"/>.</summary>
    internal double RollSpreadDeg(double spreadDeg) => (_rng.NextDouble() * 2 - 1) * spreadDeg;

    /// <summary>A hazard's draw from the match's seeded stream (F4-c, FD-08-R1): which of <paramref name="count"/> it picks. It decides what the hazard does, never a result.</summary>
    internal int DrawIndex(int count) => _rng.Next(count);

    /// <summary>
    /// A drop on the catch is allowed only for star effects (§8.6, fielding.drops): a heatball, a
    /// phony swing, a glove the heart swing froze (<paramref name="frozen"/> is
    /// <see cref="FieldingPreview.Frozen"/>, which only that special sets). Plain baseball never rolls a
    /// drop, and neither does a park: a glove a status volume slowed is decided by the glove and the
    /// ball (F4-b, #896; FD-08-R1, SF-22), so the park's use of <c>drops.frozen</c> is retired and the
    /// special's is the one left. One seeded stream (S-92).
    /// </summary>
    internal bool RollDrop(AtBatResult hit, bool frozen)
    {
        var d = Rules.Fielding.Drops;
        var heat = hit.StarPitchUsed is "heatball" or "caskball";
        if (heat && _rng.NextDouble() < d.Heatball) return true;
        if (hit.StarSwingUsed == "phony-swing" && _rng.NextDouble() < d.PhonySwing) return true;
        if (frozen && _rng.NextDouble() < d.Frozen) return true;
        return false;
    }

    /// <summary>The CPU catcher's release on a steal, from the one seeded stream (S-92).</summary>
    internal double RollCatcherRelease(Character catcher) =>
        StealThrow.CpuReleaseSec(catcher, _rng, Rules);

    /// <summary>The Star Pitch on the mound's price now (§12, PH-16-R7): its tier, plus the guest-captain surcharge.</summary>
    public int PitchStarCost => StarSkills.PitchCost(Pitcher, Defense.Captain, Rules, Content.StarSkills);
    /// <summary>The Star Swing at the plate's price now: the same rule as <see cref="PitchStarCost"/>.</summary>
    public int SwingStarCost => StarSkills.SwingCost(Batter, Offense.Captain, Rules, Content.StarSkills);
    /// <summary>
    /// The defense can pay for its Star Pitch. A read for the clients and the CPU; the match itself settles a released
    /// special in <see cref="BeginAtBat"/>, where an unaffordable one is thrown as the ordinary pitch (PH-16-R12).
    /// </summary>
    public bool CanStarPitch => StarsEnabled && DefenseStars >= PitchStarCost;
    public bool CanStarSwing => StarsEnabled && OffenseStars >= SwingStarCost;

    /// <summary>
    /// The Star Pitch request a release now would record (§12, PH-16-R12): the same typed <see cref="StarRequest"/>
    /// the play stamps on <see cref="PlayOutcome.Stars"/> when the match settles it. The client reads it at the
    /// accepted release to show the "special unavailable" tell on that tick; nothing changes the pool between the
    /// release and the settle, so the two agree (S-201).
    /// </summary>
    public StarRequest PitchStarRequest =>
        new(StarAction.Pitch, Top, Pitcher.Id, Pitcher.StarPitch, PitchStarCost, DefenseStars, CanStarPitch);

    /// <summary>The Star Swing request a release now would record: the same rule as <see cref="PitchStarRequest"/>.</summary>
    public StarRequest SwingStarRequest =>
        new(StarAction.Swing, !Top, Batter.Id, Batter.StarSwing, SwingStarCost, OffenseStars, CanStarSwing);

    /// <summary>
    /// Settle a released Star Pitch (§12, PH-16-R12): affordable, it is paid now and flies as the special; not, it
    /// is the ordinary pitch of the family already selected, at the same timing, and costs nothing. Either way the
    /// play records a typed <see cref="StarRequest"/>. A pitch with no special asked for passes untouched.
    /// </summary>
    PitchCommand SettleStarPitch(PitchCommand pitch)
    {
        if (!pitch.Star) return pitch;
        var request = PitchStarRequest;
        _starRequestsThisPlay.Add(request);
        if (!request.Afforded) return pitch with { Star = false };
        if (Top) HomeStars = request.StarsBefore - request.Cost;
        else AwayStars = request.StarsBefore - request.Cost;
        return pitch;
    }

    /// <summary>
    /// Settle a released Star Swing (§12, PH-16-R3, PH-16-R12): affordable, the full price is paid at the release,
    /// before the bat meets anything, so a whiff pays exactly what contact would; not, it is the ordinary swing and
    /// costs nothing. A take is not a swing and settles nothing.
    /// </summary>
    SwingCommand SettleStarSwing(SwingCommand swing)
    {
        if (!swing.Star || !swing.Swing) return swing;
        var request = SwingStarRequest;
        _starRequestsThisPlay.Add(request);
        if (!request.Afforded) return swing with { Star = false };
        if (Top) AwayStars = request.StarsBefore - request.Cost;
        else HomeStars = request.StarsBefore - request.Cost;
        return swing;
    }

    /// <summary>
    /// The commands as this play settled them: a special the team could not afford is the ordinary action on the log,
    /// in the trace and in the live ball, whatever the client sent (PH-16-R12).
    /// </summary>
    PitchCommand Settled(PitchCommand pitch) =>
        pitch.Star && _starRequestsThisPlay.Any(r => r.Action == StarAction.Pitch && !r.Afforded) ? pitch with { Star = false } : pitch;

    SwingCommand Settled(SwingCommand swing) =>
        swing.Star && _starRequestsThisPlay.Any(r => r.Action == StarAction.Swing && !r.Afforded) ? swing with { Star = false } : swing;

    public bool WalkPitcher(double delta)
    {
        if (Over) return false;
        PitcherOffsetX = Math.Clamp(PitcherOffsetX + delta, -1, 1);
        return true;
    }

    /// <summary>
    /// One SET tick of the mound's pitch selection (spec §3, PH-02-R3/R4/R5), against the pitcher
    /// who is on the mound <b>right now</b> and this match's own rules table — never
    /// <see cref="Rules.Default"/>, so a trial overlay's families are the ones that cycle.
    ///
    /// A read: the selection is the caller's state, nothing here is stored, no <c>_rng</c> draw
    /// moves, and no pitch is built. The mound wiring that will call it every SET frame is P1-f.
    /// </summary>
    public PitchSelectionStep SelectPitch(
        PitchSelectionState state,
        bool cyclePressed,
        bool selectable,
        ChargeButtonState prevButton,
        ChargeButtonStep buttonStep) =>
        PitchSelection.Advance(state, cyclePressed, selectable, prevButton, buttonStep,
            Pitcher.Repertoire, Rules.Pitching.Families);

    /// <summary>The family a selection names for the current pitcher and this match's table (§4.3).</summary>
    public string FamilyAt(PitchSelectionState state) =>
        PitchSelection.FamilyAt(state, Pitcher.Repertoire, Rules.Pitching.Families);

    public bool ResetPitcher()
    {
        PitcherOffsetX = 0;
        return true;
    }

    public bool WalkBatter(double delta)
    {
        if (Over) return false;
        BatterOffsetX = Math.Clamp(BatterOffsetX + delta, -1, 1);
        return true;
    }

    public bool ResetBatter()
    {
        BatterOffsetX = 0;
        return true;
    }

    public bool TogglePause()
    {
        Paused = !Paused;
        LivePlay.SetPausedFromMatch(Paused);
        return Paused;
    }

    public void SetPaused(bool on)
    {
        Paused = on;
        LivePlay.SetPausedFromMatch(on);
    }

    internal void SetPausedFromLive(bool on) => Paused = on;

    /// <summary>
    /// Named-bag pickoff in SET (§4.5, §11.4, D3). A runner on the bag is always safe: the beat, no play,
    /// no count, no stamp. A runner armed in SET broke on the pitcher's first motion and is between
    /// bags: the throw to the bag is a live ball — the receiver throws ahead or chases, and the tag or
    /// the rundown decides by geometry. True when the live play began (the client ticks it);
    /// false with <paramref name="dead"/> set for the beat, or null when there was nobody to throw at.
    /// </summary>
    public bool BeginPickoff(int bag, LiveSeats seats, out PlayEvent? dead, LivePlayCommandSource source = LivePlayCommandSource.Cpu)
    {
        dead = null;
        if (Over || Outs >= 3 || bag is < 1 or > 4 || LivePlay.Active) return false;
        if (PitchSetup.Phase == PitchSetupPhase.Flight || Paused) return false;
        if (PitchSetup.Committed)
        {
            dead = RecordBalk();
            return false;
        }
        PitchSetup.PitchResolved();
        BeginPlay();
        var fake = new PitchCommand(PitchFamily.Fastball, 0, false);
        var take = new SwingCommand(false, 0, 0, false);
        var ev = Emit(PlayKind.Pickoff, fake, take, EmptyHit(true), "Pickoff.", 0, [], Pitcher);
        LivePlay.Apply(LivePlayCommand.BeginPickoff(ev, bag, seats, source));
        return LivePlay.Active;
    }

    /// <summary>The pickoff played out headlessly (tests, the CPU game): the beat, or the live play to Time.</summary>
    public PlayEvent? Pickoff(int bag, LiveSeats? seats = null)
    {
        if (!BeginPickoff(bag, seats ?? LiveSeats.CpuOnly, out var dead))
        {
            CollectTrace(dead);
            return dead;
        }
        var ev = RunRunnerPlayTicks(LivePlayCommandSource.Cpu);
        CollectTrace(ev);
        return ev;
    }

    void CollectTrace(PlayEvent? ev)
    {
        if (!_tracing || ev is null) return;
        _traces.Add(LivePlay.TakeTrace(ev));
    }

    /// <summary>
    /// The CPU pitcher's pickoff read at SET (§4.5, §4.8): cpu.*.pickoffChance with a runner on, ×
    /// running.cpu.pickoffSeenArmMul when a steal pip is armed in SET (that runner's bag); else the
    /// lead runner, or first on the corners by the table's chance. 0 is no pickoff this SET. One seeded stream.
    /// </summary>
    public int CpuPickoffBag()
    {
        if (Over || Outs >= 3 || LeadBag == 0 || LivePlay.Active) return 0;
        var cpu = Rules.Running.Cpu;
        var seen = _runners.Where(r => r.Live && !r.Broke && r.StealArm == StealArm.Set).OrderByDescending(r => r.Bag).FirstOrDefault();
        var chance = Rules.Cpu.Active.PickoffChance * (seen is not null ? cpu.PickoffSeenArmMul : 1);
        if (_rng.NextDouble() >= chance) return 0;
        if (seen is not null) return seen.Bag;
        if (First is not null && Third is not null && Second is null && _rng.NextDouble() < cpu.PickoffFirstOnCornersChance)
            return 1;
        return LeadBag;
    }

    /// <summary>L3 / Z: the selected runner's steal on or off (§11.1).</summary>
    public bool ToggleSteal(double windupSec = -1)
    {
        var state = SelectedState;
        if (state is not null && state.StealArmed && !state.Broke)
        {
            state.CancelSteal();
            return false;
        }
        return StartSteal(windupSec);
    }

    public PlayEvent Play(PitchCommand pitch, SwingCommand swing, string? item = null)
    {
        pitch = PreparePitch(pitch);
        // Headless callers traverse the same pre-contact runner clock as the client.
        if (PitchSetup.Phase != PitchSetupPhase.Flight)
        {
            PitchSetup.BeginCharge();
            var setupPlay = PitchSetup.Advance(Motion.PitchRelease);
            if (Over && setupPlay is not null) return setupPlay;
            PitchSetup.ReleaseBall();
            PitchSetup.Advance(PitchFlight.AirSeconds(PitchSpeedMph(pitch), Rules));
        }
        PlayEvent ev;
        if (!BeginAtBat(pitch, swing, out var hit, out var finished))
            ev = StealThrowPending ? RunStealPlay(finished!) : finished!;
        else
        {
            // The live ball carries the settled commands: an unaffordable special went out as the ordinary action.
            pitch = Settled(pitch);
            swing = Settled(swing);
            var preview = PreviewHit(hit, swing);
            var field = ResolveFielding(hit, preview);
            field = ApplyOffenseItem(hit, field, item);
            ev = RunLive(pitch, swing, hit, preview, field, LiveSeats.CpuOnly);
        }
        CollectTrace(ev);
        return ev;
    }

    /// <summary>The headless frame: the live ball ticks at 60 Hz whoever drives it (S-90 needs one clock).</summary>
    public const double HeadlessTickSec = 1.0 / 60.0;
    /// <summary>A live ball that has not reached Time by this long is committed where it stands: the play never hangs.</summary>
    public const double HeadlessMaxSec = 45;

    /// <summary>
    /// Every batted ball is one live ball (spec §0.1): bodies, gloves, throws, and Time, run here
    /// with dead pads for the seats nobody holds. The CPU-vs-CPU game, the CLI, and a direct
    /// <see cref="FinishAtBat"/> all go through it, so a runner is placed by where they stand and
    /// never by a table.
    /// </summary>
    public PlayEvent RunLive(PitchCommand pitch, SwingCommand swing, AtBatResult hit, FieldingPreview preview,
        FieldingResult? field, LiveSeats seats, LivePlayCommandSource source = LivePlayCommandSource.Cpu)
    {
        LivePlay.Apply(LivePlayCommand.BeginLive(pitch, swing, hit, preview, field, seats, Dash01, source));
        var ticks = (int)Math.Ceiling(HeadlessMaxSec / HeadlessTickSec);
        for (var i = 0; i < ticks; i++)
        {
            var result = LivePlay.Apply(LivePlayCommand.Tick(HeadlessTickSec, LivePadInput.Dead, LivePadInput.Dead, false, source));
            if (result.CompletedPlay is not null) return result.CompletedPlay;
            if (!LivePlay.Active) break;
        }
        // Nothing decided it (a glove that never throws, a body that never settles): commit where things stand.
        var forced = LivePlay.Apply(LivePlayCommand.Complete(pitch, swing, hit, field ?? LivePlay.Field ?? LivePlay.LastFieldResult
            ?? new FieldingResult(PlayKind.Single, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, preview.Heatball, preview.Furnace), source));
        return forced.CompletedPlay ?? throw new InvalidOperationException("the live ball did not complete");
    }

    /// <summary>
    /// The catcher's throw play after a take or a miss with a runner who broke (§11.3), run headlessly
    /// with dead pads: the same live ball the client ticks. The pitch event is completed into the
    /// steal's result.
    /// </summary>
    public PlayEvent RunStealPlay(PlayEvent pitch, LiveSeats? seats = null, LivePlayCommandSource source = LivePlayCommandSource.Cpu)
    {
        LivePlay.Apply(LivePlayCommand.BeginSteal(pitch, seats ?? LiveSeats.CpuOnly, source));
        if (!LivePlay.Active) return pitch;
        return RunRunnerPlayTicks(source);
    }

    PlayEvent RunRunnerPlayTicks(LivePlayCommandSource source)
    {
        var ticks = (int)Math.Ceiling(HeadlessMaxSec / HeadlessTickSec);
        for (var i = 0; i < ticks; i++)
        {
            var result = LivePlay.Apply(LivePlayCommand.Tick(HeadlessTickSec, LivePadInput.Dead, LivePadInput.Dead, false, source));
            if (result.CompletedPlay is not null) return result.CompletedPlay;
            if (!LivePlay.Active) break;
        }
        var forced = LivePlay.Apply(LivePlayCommand.CompleteRunnerPlay(source));
        return forced.CompletedPlay ?? throw new InvalidOperationException("the runner play did not complete");
    }

    /// <summary>Stamp the delivery once, before flight, so the visible pitch is the judged pitch.</summary>
    public PitchCommand PreparePitch(PitchCommand pitch)
    {
        if (pitch.DeliveryPrepared) return pitch;
        // The arm is stamped from the pitcher on the mound, because the flight mirrors a family's
        // natural sweep by it (#818). It rides on the command rather than being read from the match
        // so that one delivery is one object: a trace, a harness and the Unity client all hold the
        // same pitch, and the shipped families sweep 0, so this moves nothing that flies today.
        var ready = pitch with { RubberX = pitch.RubberX != 0 ? pitch.RubberX : PitcherOffsetX,
            DeliveryPrepared = true, Throws = Pitcher.Throws };
        // Fatigue (spec §4.7) takes steering room. It is never a random miss (PH-08-R1): the arm lands
        // where it was aimed and steered.
        var breakMul = Rules.Pitching.Stamina.BreakMul(PitcherStamina);
        if (breakMul != 1) ready = ready with { BreakMul = breakMul };
        return ready;
    }

    /// <summary>The pitch's speed from this arm: shape, stat, charge, Nice!, the star skill, and fatigue (spec §4.7).</summary>
    public double PitchSpeedMph(PitchCommand pitch)
    {
        var penalty = Rules.Pitching.Stamina.MphLost(PitcherStamina);
        return AtBatResolver.PitchSpeedMph(pitch, Pitcher, Rules, Content.StarSkills, penalty);
    }

    // ---- stamina (spec §4.7) --------------------------------------------------------

    public int StaminaPool(Character who) =>
        Rules.Pitching.Stamina.PoolBase + who.Stats.Endurance * Rules.Pitching.Stamina.PoolPerPitch;

    public int StaminaOf(Character who) =>
        _stamina.TryGetValue(who.Id, out var pool) ? pool : StaminaPool(who);

    void ChargeArm(Character who, int cost)
    {
        if (cost == 0) return;
        _stamina[who.Id] = StaminaOf(who) - cost;
    }

    /// <summary>
    /// The window this swing is judged in, from the batter at the plate now (spec §5.3, D13): the
    /// resolver's number, read at the press so the take can warp its Contact mark onto the ball.
    /// </summary>
    public double SwingWindowFrames(PitchCommand pitch) =>
        AtBatResolver.ContactWindowFrames(pitch.Star ? Pitcher.StarPitch : null, Park, Night, Rules, Content.StarSkills);

    public bool BeginAtBat(PitchCommand pitch, SwingCommand swing, out AtBatResult hit, out PlayEvent? finished)
    {
        hit = EmptyHit(true);
        finished = null;
        if (Over) throw new InvalidOperationException("game over");
        PitchSetup.PitchResolved();
        BeginPlay();
        // The square was spent on this pitch's swing; the next pitch reads it again at SET (§5.9).
        _cpuSquareDecided = false;
        _cpuSquared = false;
        _cpuBuntSide = BuntSide.None;

        pitch = PreparePitch(pitch);
        // A released special is settled before anything reads it (PH-16-R12): the flight, the stamina, the
        // resolver and the log all see the pitch the team could pay for.
        pitch = SettleStarPitch(pitch);
        // One crossing for the umpire, the body, and the bat: the shown pitch is the judged pitch (§3).
        var crossing = PitchFlight.Point(pitch, 1, Rules, Pitcher.StarPitch);
        var inZone = StrikeZoneGeometry.Contains(crossing.X, crossing.Y);
        SpendPitch(pitch);
        if (Top) _awayLastCrossingX = crossing.X; else _homeLastCrossingX = crossing.X;
        // Runner positions already came from the pre-contact clock; never recalculate a head start.
        var box = swing.BoxOffsetX != 0 ? swing.BoxOffsetX : BatterOffsetX;
        BatterContactOffsetX = box;

        if (!swing.Swing)
        {
            finished = AtBatResolver.HitsBatter(box, crossing.X, crossing.Y, Rules, Batter.Bats)
                ? FinishHitByPitch(pitch, swing, EmptyHit(inZone))
                : FinishTake(pitch, swing, inZone);
            EndIfWalkOff();
            finished = FinishEvent(finished);
            return false;
        }

        swing = SettleStarSwing(swing);

        var bat = OffenseBat;
        var input = new AtBatInput(
            Pitcher, Batter, OnDeck, RunnersOn().ToList(),
            ChargeFeel.IsCharge(pitch.Charge01), Rules.Pitching.Families.Of(pitch.Type).OffSpeed,
            swing.TimingErrorFrames, pitch.Star, swing.Star, bat,
            PitcherStamina,
            swing.SprayAimDeg, inZone, swing.Bunt, swing.LaunchAim,
            swing.Charge01, box, crossing.X, crossing.Y, swing.BuntSide);

        hit = _atBat.Resolve(input, Park, _rng, Night);
        if (hit.Quality == ContactQuality.Miss)
        {
            finished = FinishStrike(pitch, swing, hit, swinging: true);
            return false;
        }
        // Every batted ball is live from here, foul territory included (§7.11): the flight is
        // fielded, a caught foul fly is an out, and the call is made where the ball lands or is
        // first touched. FinishInPlay stamps FOUL when it is dead.
        PitchSetup.TakeCatcherInput(); // contact supersedes a queued catcher throw
        return true;
    }

    /// <summary>
    /// Complete the batted ball. With the live ball running this is Complete (spec §10.6); called
    /// cold with a fielding result it runs the ball headlessly first so the runners are bodies
    /// either way.
    /// </summary>
    public PlayEvent FinishAtBat(PitchCommand pitch, SwingCommand swing, AtBatResult hit, FieldingResult field)
    {
        if (!LivePlay.Active)
            return RunLive(pitch, swing, hit, PreviewHit(hit, swing), field, LiveSeats.CpuOnly);
        CurrentPlay();
        var played = FinishInPlay(pitch, swing, hit, field);
        EndIfWalkOff();
        return FinishEvent(played);
    }

    /// <summary>
    /// The defense's read of the batted ball (§8.2). With the batter squared (<paramref name="swing"/>, §7.3) the
    /// routes start from the bodies the square left — the corners in, the middle on the bags — so the glove picked
    /// at contact is the crashing corner when it gets there first.
    /// </summary>
    public FieldingPreview PreviewHit(AtBatResult hit, SwingCommand? swing = null) =>
        _fielding.Preview(hit, Park, Defense.Roster, Pitcher, _rng, Night, Defense.Gloves, SquareSpots(swing));

    /// <summary>The defense as the batter's square left it (§7.3), or null with no square.</summary>
    public Dictionary<string, (double X, double Z)>? SquareSpots(SwingCommand? swing) =>
        BuntDefense.Squared(swing)
            ? BuntDefense.Spots(FieldingResolver.Assign(DefenseRoster, Pitcher, Defense.Gloves), swing!.SquareSec, Rules)
            : null;

    public FieldingResult ResolveFielding(AtBatResult hit, FieldingPreview? preview = null) =>
        _fielding.Resolve(hit, Park, Defense.Roster, Pitcher, _rng, DefenseGlove, preview, Night, Defense.Gloves);

    /// <summary>
    /// Pitcher swap (spec §4.7): any fielder takes the mound (the best Pitch stat when nobody is
    /// named) with their own arm; the old pitcher takes the glove they vacated. Once per half-inning.
    /// </summary>
    public bool SwapPitcher(Character? next = null)
    {
        if (!CanSwapPitcher) return false;
        var defense = Top ? _homeDefense : _awayDefense;
        var cur = Pitcher;
        next ??= defense
            .Where(c => !c.Id.Equals(cur.Id, StringComparison.OrdinalIgnoreCase))
            // The displayed aggregate picks the arm, as the SET pick does (PitcherSwapPick): a selection,
            // not a rating's read (§4.7, PH-15-R6).
            .OrderByDescending(c => c.Stats.Pitch)
            .FirstOrDefault();
        if (next is null || next.Id.Equals(cur.Id, StringComparison.OrdinalIgnoreCase)) return false;
        var from = defense.FindIndex(c => c.Id.Equals(cur.Id, StringComparison.OrdinalIgnoreCase));
        var to = defense.FindIndex(c => c.Id.Equals(next.Id, StringComparison.OrdinalIgnoreCase));
        if (from < 0 || to < 0) return false;
        (defense[from], defense[to]) = (defense[to], defense[from]);
        if (Top) _homePitcher = next;
        else _awayPitcher = next;
        _swappedThisHalf = true;
        PitcherOffsetX = 0;
        return true;
    }

    /// <summary>The CPU swaps at TIRED with a lead of cpuSwapLead, or at exhaustion always (spec §4.7).</summary>
    public bool CpuConsidersSwap()
    {
        if (!CanSwapPitcher) return false;
        var lead = Top ? HomeScore - AwayScore : AwayScore - HomeScore;
        if (PitcherExhausted || (PitcherTired && lead >= Rules.Pitching.Stamina.CpuSwapLead))
            return SwapPitcher();
        return false;
    }

    public ThrowResult ThrowBetween(Character from, Character to) =>
        FieldAbilities.ApplyThrow(from, Content.Chemistry.FieldingThrow(from, to, _rng), Rules);

    public FieldingResult ApplyOffenseItem(AtBatResult hit, FieldingResult field, string? playerItem, Character? target = null)
    {
        if (!hit.ChemistryItemOffered) return field;
        // A foul flight cannot become a hit (§7.11): a dropped foul is still foul, so no item plays on it.
        if (hit.Foul) return field;
        if (playerItem == "") return field;
        var who = target ?? field.Fielder;
        if (!string.IsNullOrEmpty(playerItem))
            return ThrowItem(field, playerItem, who);
        return CpuWouldThrowItem(hit, field) ? ThrowItem(field, ErrorItems.CpuPick(hit.Class, RunnersOn().Any()), who) : field;
    }

    /// <summary>
    /// The CPU offense reads the play the way its runners do (§9.9): the batter's slack at first against
    /// the glove's throw from the landing. Under batting.items.cpuThrowMarginSec the item would matter and
    /// it is thrown; a ball that is already a hit, or a fly it cannot help, keeps the item. No roll.
    /// </summary>
    public bool CpuWouldThrowItem(AtBatResult hit, FieldingResult field)
    {
        if (field.Fielder is null || hit.HomeRun) return false;
        var batterAt = Rules.Running.BagSec.BatterStartSec + RunnerSystem.BagSec(Batter, Rules);
        var throwAt = field.HangTimeSec + InPlay.ThrowReactionSec(field.Fielder, Rules)
                      + InPlay.ThrowArrivalSec(field.LandingX, field.LandingZ, 1, null, Rules);
        return throwAt - batterAt < Rules.Batting.Items.CpuThrowMarginSec;
    }

    /// <summary>
    /// After contact: throw a banana / rocket / POW at a fielder. Empty or unknown item is a no-op.
    /// </summary>
    public FieldingResult ThrowItem(FieldingResult field, string? item, Character? target)
    {
        if (string.IsNullOrEmpty(item)) return field;
        return ErrorItems.Apply(field, item, target);
    }

    /// <summary>
    /// The CPU pitcher (spec §4.8): one row of the table per SET from the count, the outs and the
    /// runners, built from the inputs a hand has and nothing else (<see cref="CpuPitchByInputs"/>,
    /// PH-18-R1, #823).
    /// </summary>
    public PitchCommand CpuPitch() => CpuPitchByInputs(out _);

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
    public PitchCommand CpuPitchByInputs(out CpuPitchPlan plan)
    {
        var c = Rules.Pitching.Cpu;
        var row = CpuPitchRow();

        // (1) The horizontal intent, plus the arm's own scatter on it. No vertical term exists.
        var intentX = CpuPitchIntentX(row.Location, c.Locations);
        // The CPU arm's miss on its own intent is Control's (§4.8, PH-15-R6).
        var scatter = (11 - Pitcher.Stats.Control) * c.ScatterFtPerPitchStat * (PitcherTired ? c.TiredScatterMul : 1);
        intentX += Gauss() * scatter;

        // (2) The family, as presses from the fastball every SET resets to (PH-02-R5).
        var presses = CpuPitchPresses(row);
        var family = CpuFamilyAfter(presses);

        // (3) Charge, then Nice! on a charged pitch.
        var charged = _rng.NextDouble() < row.ChargeChance;
        var charge = charged ? 1.0 : c.TapMin + _rng.NextDouble() * c.TapSpan;
        var nice = charged && _rng.NextDouble() < c.NiceChance;
        var star = CanStarPitch && Pitcher.Captain && _rng.NextDouble() < row.StarChance;

        // (4) The stick, held one way from release for as long as this delivery is in the air. The
        // speed is read off the delivery as it stands, which is every term AtBatResolver.PitchSpeedMph
        // looks at (family, charge, Nice!, Star, fatigue); the stick is lateral and does not reach it.
        var delivery = new PitchCommand(family, charge, star, RubberX: 0, Nice: nice, Throws: Pitcher.Throws);
        var airSec = PitchFlight.AirSeconds(PitchSpeedMph(delivery), Rules);
        var reach = PitchFlight.BreakReach(Pitcher.Stats.Control, airSec, Rules);
        var steerDir = _rng.NextDouble() < row.SteerChance ? (_rng.NextDouble() < 0.5 ? -1 : 1) : 0;
        delivery = delivery with { BreakX = steerDir * reach };

        // (5) Walk the rubber until this delivery crosses on the intent. X₀ is where it crosses from
        // the middle of the rubber; everything the flight adds after the straight line is the same
        // there as anywhere, so the difference is the walk. The clamp is the legal rubber range —
        // the one Match.WalkPitcher enforces for a hand on the stick — and a walk that runs into it
        // simply misses short, the way a pitcher who has run out of rubber does.
        var (zeroX, _) = PitchFlight.Crossing(delivery, Rules, Pitcher.StarPitch);
        var rubber = Math.Clamp((intentX - zeroX) / HomeSet.PitcherWalk, -1, 1);
        PitcherOffsetX = rubber;

        plan = new CpuPitchPlan(presses, family, charged, steerDir, reach, rubber, intentX);
        return delivery with { RubberX = rubber };
    }

    /// <summary>
    /// The named location as a <b>horizontal</b> intent in world feet at the plate plane (§4.8): the
    /// sides by batter hand, and no vertical term, because a hand has no vertical input (PH-03).
    /// </summary>
    double CpuPitchIntentX(string location, CpuPitchLocations loc)
    {
        var away = SweetSpot.TipSign(Batter.Bats);
        var halfW = StrikeZoneGeometry.HalfWidth;
        return location switch
        {
            "waste" => away * (halfW + loc.WasteOutFt),
            "middleIn" => -away * loc.MiddleInFt,
            "middle" => 0,
            _ => (_rng.NextDouble() < loc.EdgeAwayChance ? away : -away) * (halfW - loc.EdgeInsetFt)
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
    int CpuPitchPresses(CpuPitchRow row)
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

        var roll = _rng.NextDouble() * total;
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
    public string CpuFamilyAfter(int presses)
    {
        var authored = Rules.Pitching.Families.Authored;
        var state = PitchSelectionState.Reset;
        for (var i = 0; i < presses; i++)
            state = PitchSelection.Advance(state, true, true, default, default, Pitcher.Repertoire, authored).Next;
        return PitchSelection.FamilyAt(state, Pitcher.Repertoire, authored);
    }

    /// <summary>Which row of §4.8 this SET reads.</summary>
    public CpuPitchRow CpuPitchRow()
    {
        var c = Rules.Pitching.Cpu;
        if (Outs == 2 && RunnersOn().Any()) return c.RunnerTwoOuts;
        if (Strikes == 2 && Balls <= 1) return c.Ahead;
        if (Balls >= 2 && Strikes <= 1) return c.Behind;
        return c.Even;
    }

    /// <summary>
    /// The CPU batter (spec §5.9): a table read from one crossing, the same whoever is pitching. It
    /// commits from what it can see (PH-18): <see cref="CpuReadPitch"/>, the flight as it stands at the
    /// commit instant, and the zone, the swing / take and the box are all judged on that read; the
    /// umpire and the bat still meet the ball that is thrown. No side effects: the box it stands in and
    /// the swing it makes are the returned command. Steals are the runner AI's (<see cref="CpuArmSteal"/>).
    /// </summary>
    /// <param name="breakAtCommit">The stick's break as it stood at the commit instant, when the
    /// caller watched it (a client ticking the flight, a scenario steering late). Absent, the steer is
    /// taken as held one way from release, the CPU pitcher's own (S-117).</param>
    public SwingCommand CpuSwing(PitchCommand pitch, double? breakAtCommit = null)
    {
        var c = Rules.Batting.Cpu;
        var level = Rules.Cpu.Active;
        // Two traits, not one rating (§5.9, PH-15-R5): making contact is Contact's — whether it
        // offers at a ball it cannot square up, and how far off the ball its bat arrives — while
        // swinging for it is Power's.
        var contact = Batter.Stats.Contact;
        var power = Batter.Stats.Power;
        // Commit from what can be seen: the read pitch replaces the final one for every decision below.
        pitch = CpuReadPitch(pitch, breakAtCommit);
        var inZone = AtBatResolver.PitchInZone(pitch, Pitcher.Stats.Pitch, Rules, Pitcher.StarPitch);
        var (cx, cy) = PitchFlight.Crossing(pitch, Rules, Pitcher.StarPitch);
        var zone = CpuZoneClass(cx, cy, inZone, c);
        var take = new SwingCommand(false, 0, 0, false);

        // Sac bunt (§5.9, §7.3): the square and its side were read at SET (<see cref="CpuSquaresBunt"/>); in the
        // zone the held bat meets the ball (§5.8: no timed press, the same held bunt a pad lays down), out of it the
        // batter pulls the bat back and takes — the corners are in either way, that is the tell's cost.
        if (CpuSquaresBunt())
        {
            var squareSec = c.SacBuntSquareSec;
            if (!inZone) return take with { SquareSec = squareSec };
            return SwingCommand.HeldBunt(_cpuBuntSide, CpuTrackedBox(cx, c, level), squareSec, human: false);
        }

        var swing = zone switch
        {
            CpuZone.Middle => true,
            CpuZone.Edge => Strikes == 2 || _rng.NextDouble() < c.EdgeSwingChance,
            // Chase: a better-Contact hitter lays off the pitch it cannot square up.
            CpuZone.Near => _rng.NextDouble() * 100 < (Strikes == 2 ? c.ChaseTwoStrikesBase : c.ChaseBase) - contact,
            _ => false
        };
        if (!swing) return take;

        var star = CanStarSwing && Batter.Captain && inZone && (RunnersOn().Any() || Strikes == 2)
                   && _rng.NextDouble() < c.StarChance;
        var risp = Second is not null || Third is not null;
        // Forced charge: swinging for it on a hitter's count is Power's read, not Contact's.
        var forcedCharge = zone == CpuZone.Middle
                           && (((Balls, Strikes) is (2, 0) or (3, 1) or (3, 0)) && power >= c.ChargeBatMin
                               || risp && Outs < 2 && power >= c.RispChargeBatMin);
        var charge = forcedCharge || _rng.NextDouble() < CpuChargeChance(Batter, c.Archetype) ? 1.0 : 0;

        var tracked = _rng.NextDouble() < c.TrackPerfectChance;
        // Timing sigma: how far off the ball the bat arrives is Contact's (⚠️ P2-b re-reads this one).
        var err = Gauss() * (11 - contact) * c.ErrorFramesPerBatStat * level.TimingSigmaMul;
        var offSpeed = Rules.Pitching.Families.Of(pitch.Type).OffSpeed;
        if (!tracked && (offSpeed || ChargeFeel.IsCharge(pitch.Charge01)))
        {
            // Fooled: an off-speed family pulls the bat early past the ball (late), a charged pitch beats it (early).
            var fooled = c.FooledMinFrames + _rng.NextDouble() * c.FooledSpanFrames;
            err += offSpeed ? fooled : -fooled;
        }
        var box = tracked ? Math.Clamp(cx / HomeSet.BatterWalk, -1, 1) : CpuTrackedBox(cx, c, level);
        // The stick at contact (§5.9, PH-12, PH-18). No human's stick shapes an ordinary swing, so
        // the CPU holds none: 0 / 0 and neither Gaussian is drawn. A Star Swing still steers and
        // draws both aims; the sac bunt above holds a side instead (§5.8).
        if (!AtBatResolver.StickShapesContact(bunt: false, star))
            return new SwingCommand(true, charge, err, star, BoxOffsetX: box);
        return new SwingCommand(true, charge, err, star, Gauss() * c.SpraySigmaDeg,
            LaunchAim: Gauss() * c.LaunchAimSigma, BoxOffsetX: box);
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
    public PitchCommand CpuReadPitch(PitchCommand pitch, double? breakAtCommit = null)
    {
        if (breakAtCommit is { } seen) return pitch with { BreakX = Math.Clamp(seen, -1, 1) };
        if (pitch.BreakX == 0) return pitch;
        var airSec = PitchFlight.AirSeconds(PitchSpeedMph(pitch), Rules);
        var commitSec = Math.Max(0, AtBatMotion.CpuDecisionTime(airSec, Rules));
        var soFar = Math.Min(Math.Abs(pitch.BreakX), PitchFlight.BreakReach(Pitcher.Stats.Pitch, commitSec, Rules));
        return pitch with { BreakX = Math.Sign(pitch.BreakX) * soFar };
    }

    enum CpuZone { Middle, Edge, Near, Far }

    static CpuZone CpuZoneClass(double x, double y, bool inZone, CpuBatterRules c)
    {
        var dx = Math.Abs(x);
        var dy = Math.Abs(y - StrikeZoneGeometry.CenterY);
        if (inZone)
            return dx <= StrikeZoneGeometry.HalfWidth * c.MiddleFraction && dy <= StrikeZoneGeometry.Height / 2 * c.MiddleFraction
                ? CpuZone.Middle
                : CpuZone.Edge;
        var outX = Math.Max(0, dx - StrikeZoneGeometry.HalfWidth);
        var outY = Math.Max(0, dy - StrikeZoneGeometry.Height / 2);
        return Math.Sqrt(outX * outX + outY * outY) <= c.NearFt ? CpuZone.Near : CpuZone.Far;
    }

    /// <summary>
    /// The box after a failed re-read (spec §5.9 tracking): the guess is the last crossing this
    /// offense saw (the first pitch guesses the middle) plus a fixed offset; the miss is likelier
    /// when the pitcher moved on the rubber since the last pitch.
    /// </summary>
    double CpuTrackedBox(double crossingX, CpuBatterRules c, CpuLevelRules level)
    {
        var last = Top ? _awayLastCrossingX : _homeLastCrossingX;
        var chance = Math.Min(0.95, (RubberMovedSinceLastPitch ? c.MistrackMovedChance : c.MistrackChance) * level.MistrackMul);
        var guess = _rng.NextDouble() < chance ? (last ?? 0) : crossingX;
        var offset = (c.MistrackMinFt + _rng.NextDouble() * c.MistrackSpanFt) * (_rng.NextDouble() < 0.5 ? -1 : 1);
        return Math.Clamp((guess + offset) / HomeSet.BatterWalk, -1, 1);
    }

    /// <summary>The pitcher walked the rubber since the last pitch this offense saw.</summary>
    public bool RubberMovedSinceLastPitch => Math.Abs(PitcherOffsetX - _lastPitchRubberX) > 0.05;

    /// <summary>
    /// Charge vs slap by archetype (spec §5.9). The technique gate is <b>Contact</b> and Run — the
    /// hitter who can both square it up and beat it out slaps — while the slugger-vs-speedster split
    /// is <b>Power</b> against Run (PH-15-R5).
    /// </summary>
    public static double CpuChargeChance(Character who, CpuArchetypeRules a)
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
    public bool CpuSquared => _cpuSquared;

    /// <summary>
    /// The side the squared CPU batter holds on this pitch (§5.8, §5.9; PH-14-R2), decided at SET with the square so
    /// the bat angle is a tell before the pitch. <see cref="BuntSide.None"/> when it is not squared.
    /// </summary>
    public BuntSide CpuBuntSide => _cpuSquared ? _cpuBuntSide : BuntSide.None;

    /// <summary>
    /// The CPU batter's sac-bunt read (§5.9's row), once per pitch at SET so the square is a tell the defense
    /// reads before the pitch (§7.3): runner on first only, no outs, a light bat, a close game, at the table's
    /// chance, on the one seeded stream. At the plate plane the square is the bunt if the pitch is in the zone
    /// and a take otherwise (<see cref="CpuSwing"/>). Idempotent for the pitch; <see cref="BeginAtBat"/> clears it.
    /// </summary>
    public bool CpuSquaresBunt()
    {
        if (_cpuSquareDecided) return _cpuSquared;
        if (Over || Outs >= 3 || LivePlay.Active) return false;
        _cpuSquareDecided = true;
        var c = Rules.Batting.Cpu;
        var trailing = Top ? HomeScore - AwayScore : AwayScore - HomeScore;
        // The light bat that gives itself up is the weak-Contact hitter (§5.9).
        _cpuSquared = First is not null && Second is null && Third is null && Outs == 0
                      && Batter.Stats.Contact <= c.SacBuntBatMax && trailing <= c.SacBuntTrailMax
                      && _rng.NextDouble() < c.SacBuntChance;
        // The side is held with the square (PH-14-R2): one draw, only when it squares.
        _cpuBuntSide = _cpuSquared
            ? (_rng.NextDouble() < c.SacBuntFirstSideChance ? BuntSide.First : BuntSide.Third)
            : BuntSide.None;
        return _cpuSquared;
    }

    /// <summary>
    /// The CPU offense's steal decision (§11.6): the runner AI's table, once per at-bat at SET, on
    /// the one seeded stream. A chosen runner departs now and is exposed to a legal pitcher throw.
    /// </summary>
    public bool CpuArmSteal()
    {
        if (_cpuStealDecided || Over || Outs >= 3 || LivePlay.Active) return false;
        _cpuStealDecided = true;
        var trailing = Top ? HomeScore - AwayScore : AwayScore - HomeScore;
        var plan = RunnerAi.StealPlan(_runners, Batter.Captain, Outs, trailing, _rng, Rules);
        var any = false;
        foreach (var (runner, _) in plan)
        {
            any |= StartStealAt(runner.FromBag);
        }
        return any;
    }

    public PlayEvent AutoPlay()
    {
        CpuConsidersSwap();
        CpuArmSteal();
        CpuSquaresBunt();
        var pickoffBag = CpuPickoffBag();
        if (pickoffBag > 0 && Pickoff(pickoffBag) is { } pickoff)
            return pickoff;
        var pitch = PreparePitch(CpuPitch());
        var swing = CpuSwing(pitch);
        return Play(pitch, swing);
    }

    public void AutoPlayGame()
    {
        var guard = 0;
        while (!Over && guard++ < 2000)
            AutoPlay();
    }

    /// <summary>
    /// The MVP (§12, stars.json mvp): a walk-off hit names its hitter; otherwise the most points on
    /// either roster, the winning pitcher's points included once the game is over.
    /// </summary>
    public (Character Who, int Points, string Why) Mvp()
    {
        var m = Rules.Stars.Mvp;
        var points = new Dictionary<string, int>(_mvp, StringComparer.OrdinalIgnoreCase);
        if (Over && HomeScore != AwayScore && _leadPitcherId is { } arm)
            points[arm] = points.GetValueOrDefault(arm) + m.WinningPitcher;
        var walkOff = WalkOffHitter();
        string id;
        if (walkOff is not null) id = walkOff.Id;
        else if (points.Count == 0) return (Home.Captain, 0, "showed up");
        else id = points.OrderByDescending(kv => kv.Value).First().Key;
        var who = Away.Roster.Concat(Home.Roster).First(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        var pts = points.GetValueOrDefault(id);
        var why = pts >= m.TookOverAt ? "took over the diamond" : pts >= m.KeptMovingAt ? "kept the line moving" : "did the little things";
        return (who, pts, why);
    }

    /// <summary>The hitter whose hit ended the game in the home half with the winning run (§12), or null.</summary>
    Character? WalkOffHitter()
    {
        if (!Over || _log.Count == 0) return null;
        var last = _log[^1];
        if (last.Context is not { Top: false } ctx || last.RunsScored <= 0) return null;
        if (last.Kind is not (PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun)) return null;
        return last.HomeScoreAfter > last.AwayScoreAfter && ctx.HomeScoreBefore <= ctx.AwayScoreBefore ? last.Batter : null;
    }

    public string BoxLine() =>
        $"G{Away.Name} {AwayScore}  {Home.Name} {HomeScore}  {(Over ? "F" : (Top ? "T" : "B") + Inning)}  {Outs} out";

    PlayEvent FinishTake(PitchCommand pitch, SwingCommand swing, bool inZone)
    {
        var empty = EmptyHit(inZone);
        if (inZone)
        {
            if (Strikes >= 2)
                return FinishStrike(pitch, swing, empty, swinging: false);
            Strikes++;
            return AfterPitch(Emit(PlayKind.TakeStrike, pitch, swing, empty, $"Strike {Strikes} looking.", 0, []));
        }
        Balls++;
        if (Balls >= 4)
            return FinishWalk(pitch, swing, empty);
        return AfterPitch(Emit(PlayKind.TakeBall, pitch, swing, empty, $"Ball {Balls}.", 0, []));
    }

    PlayEvent FinishStrike(PitchCommand pitch, SwingCommand swing, AtBatResult hit, bool swinging, string? how = null)
    {
        Strikes++;
        if (Strikes < 3)
        {
            var cap = swinging ? $"Strike {Strikes}." : $"Strike {Strikes} looking.";
            return AfterPitch(Emit(swinging ? PlayKind.SwingMiss : PlayKind.TakeStrike, pitch, swing, hit, cap, 0, []));
        }
        ClearSteal();
        AddMvp(Pitcher.Id, Rules.Stars.Mvp.Strikeout);
        AddStars(defense: true, Rules.Stars.Gains.Strikeout);
        RecordOut(OutType.Strikeout, 0, 0, Batter, Pitcher);
        how ??= swinging ? "goes down swinging." : "is caught looking.";
        var ev = Emit(PlayKind.Strikeout, pitch, swing, hit, $"{Batter.Name} {how}", 0, []);
        NextBatter();
        CheckInning();
        return AfterPitch(FinishEvent(ev));
    }

    PlayEvent FinishWalk(PitchCommand pitch, SwingCommand swing, AtBatResult hit)
    {
        ClearSteal();
        var ledBefore = OffenseLead;
        var (runs, scorers) = PlaceByWalk(Batter);
        CreditBatter(Rules.Stars.Mvp.Walk, runs, ledBefore);
        var ev = Emit(PlayKind.Walk, pitch, swing, hit, $"{Batter.Name} walks.", runs, scorers,
            outcome: new PlayOutcome(BatterToBag: 1));
        NextBatter();
        return AfterPitch(ev);
    }

    PlayEvent FinishHitByPitch(PitchCommand pitch, SwingCommand swing, AtBatResult hit)
    {
        ClearSteal();
        var ledBefore = OffenseLead;
        var (runs, scorers) = PlaceByWalk(Batter);
        CreditBatter(Rules.Stars.Mvp.HitByPitch, runs, ledBefore);
        var ev = Emit(PlayKind.HitByPitch, pitch, swing, hit, $"{Batter.Name} is hit.", runs, scorers,
            outcome: new PlayOutcome(BatterToBag: 1));
        NextBatter();
        return AfterPitch(ev);
    }

    /// <summary>
    /// Complete (spec §10.6): runs are the runners who crossed home before the third out (§1),
    /// outs are already recorded, and every runner is seated where they stand. The kind is read
    /// off the batter's body: out, or the bag they hold. The only placements by rule are the dead
    /// balls: a homer, a ground-rule double, a foul.
    /// </summary>
    PlayEvent FinishInPlay(PitchCommand pitch, SwingCommand swing, AtBatResult hit, FieldingResult field)
    {
        ClearSteal();
        SendAll = false;
        var kind = field.Kind;
        var caption = "";
        var runs = 0;
        var ledBefore = OffenseLead;
        var mvp = Rules.Stars.Mvp;
        IReadOnlyList<string> scorers = [];
        var batterToBag = 0;
        // True once a branch built its caption from the live ball's own narration; otherwise
        // an open live moment is prefixed at the end. Typed, so no branch inspects caption text.
        var liveNarrated = false;
        var moment = LivePlay.LastMoment;
        // The bodies at Time (§10.6, #574), read before any branch seats, flips, or resets the field.
        var bodies = LivePlay.BodiesNow();

        switch (kind)
        {
            case PlayKind.Foul:
                // A foul bunt with two strikes is strike three (spec §1, §5.8).
                if (swing.Bunt && Strikes >= 2)
                {
                    ResetRunnersToBags();
                    LivePlay.Reset();
                    return FinishStrike(pitch, swing, hit, swinging: true, how: "bunts foul for strike three.");
                }
                // Dead where it landed, rolled foul, or left the field, or was first touched foul (§5.6).
                // Fewer than two strikes adds one; runners return; the at-bat continues.
                if (Strikes < 2) Strikes++;
                ResetRunnersToBags();
                caption = "Foul.";
                break;
            case PlayKind.HomeRun:
                // Dead at the crossing (§7.10): everyone circles; the trot is presentation.
                (runs, scorers) = ScoreEveryone();
                batterToBag = 4;
                CreditBatter(mvp.HomeRun, runs, ledBefore);
                AddStars(defense: false, Rules.Stars.Gains.HomeRun);
                caption = hit.StarSwingUsed is "furnace" or "heat-swing"
                    ? $"{Batter.Name} {hit.StarSwingUsed!.ToUpperInvariant()} - it's gone."
                    : $"{Batter.Name} goes deep.";
                NextBatter();
                break;
            default:
                if (field.GroundRule)
                {
                    // Bounced then over (§1): the batter and every runner exactly two bases, by rule.
                    (runs, scorers) = AwardBases(2);
                    batterToBag = 2;
                    kind = PlayKind.Double;
                    CreditBatter(mvp.Hit, runs, ledBefore);
                    AddStars(defense: false, Rules.Stars.Gains.ExtraBaseHit);
                    caption = $"{Batter.Name} - over the fence on a hop. Ground-rule double.";
                    NextBatter();
                    break;
                }
                var batter = _runners.FirstOrDefault(r => r.IsBatter);
                var batterOut = batter is null || batter.Out;
                var catchOut = _outsThisPlay.Any(o => o.Type == OutType.Catch);
                (runs, scorers) = SettleRunners(out batterToBag);
                // An out on the play stamps OUT whoever made it; the batter safe at first behind it is a
                // fielder's choice (§10.4). No out: the batter's bag names the hit.
                kind = batterOut || _outsThisPlay.Count > 0
                    ? catchOut ? PlayKind.FlyOut : PlayKind.GroundOut
                    : batterToBag switch
                    {
                        4 => PlayKind.HomeRun,
                        3 => PlayKind.Triple,
                        2 => PlayKind.Double,
                        1 => PlayKind.Single,
                        _ => LivePlay.CatchMade ? PlayKind.FlyOut : PlayKind.GroundOut
                    };
                switch (kind)
                {
                    case PlayKind.HomeRun:
                        CreditBatter(mvp.HomeRun, runs, ledBefore);
                        AddStars(defense: false, Rules.Stars.Gains.HomeRun);
                        caption = $"{Batter.Name} - all the way around!";
                        break;
                    case PlayKind.Triple:
                        CreditBatter(mvp.Hit, runs, ledBefore);
                        AddStars(defense: false, Rules.Stars.Gains.ExtraBaseHit);
                        caption = $"{Batter.Name} triples.";
                        break;
                    case PlayKind.Double:
                        CreditBatter(mvp.Hit, runs, ledBefore);
                        AddStars(defense: false, Rules.Stars.Gains.ExtraBaseHit);
                        caption = $"{Batter.Name} doubles.";
                        break;
                    case PlayKind.Single:
                        CreditBatter(mvp.Hit, runs, ledBefore);
                        AddStars(defense: false, Rules.Stars.Gains.Single);
                        caption = moment is not null
                            ? moment.NarratesBatterAtFirst ? LivePlay.Caption : $"{LivePlay.Caption} {Batter.Name} in at first."
                            : field.Warped ? $"{Batter.Name} - it went through a {CarnivalFront.RedirectName(field.RedirectType)}!"
                            : field.Heatball ? $"{Batter.Name} - it drops! Heatball."
                            : $"{Batter.Name} singles.";
                        liveNarrated = true;
                        break;
                    default:
                        // A run driven in by an out (the sac fly) is still the batter's RBI (§12); a chain of outs is the defense's star gain.
                        if (runs > 0) CreditBatter(0, runs, ledBefore);
                        if (_outsThisPlay.Count >= 2) AddStars(defense: true, Rules.Stars.Gains.DoublePlay);
                        // A leap that took a ball clearing the fence (§8.4): the robbed homer, and the buddy who jumped with them.
                        if (kind == PlayKind.FlyOut && field.Feat is DefensiveFeat.SuperJump or DefensiveFeat.Clamber or DefensiveFeat.BuddyJump && hit.HomeRun)
                        {
                            AddMvp(field.Fielder?.Id, mvp.RobbedHomer);
                            if (field.Feat == DefensiveFeat.BuddyJump) AddMvp(field.Buddy?.Id, mvp.RobbedHomer);
                            AddStars(defense: true, Rules.Stars.Gains.RobbedHomer);
                        }
                        // A chain of outs names the chain (§10.4, §10.7) ahead of the last decision it narrated.
                        var chain = _outsThisPlay.Count >= 3 ? "Triple play! "
                            : _outsThisPlay.Count == 2 && moment is not { Verdict: InPlay.ThrowVerdict.TurnedTwo } ? "Double play. "
                            : "";
                        caption = moment is not null ? $"{chain}{LivePlay.Caption}"
                            : kind == PlayKind.FlyOut && field.Feat == DefensiveFeat.BuddyJump && field.Buddy is not null
                                ? $"{field.Fielder?.Name} + {field.Buddy.Name} BUDDY JUMP!"
                            : kind == PlayKind.FlyOut && field.Feat == DefensiveFeat.Clamber
                                ? $"{field.Fielder?.Name} CLAMBERS the wall!"
                            : kind == PlayKind.FlyOut && field.Feat == DefensiveFeat.SuperJump
                                ? $"{field.Fielder?.Name} SUPER JUMP!"
                            : kind == PlayKind.FlyOut
                                ? $"{field.Fielder?.Name} puts it away."
                                : $"{field.Fielder?.Name} to first.";
                        if (kind == PlayKind.FlyOut && runs > 0)
                            caption = $"{caption} Sac fly.";
                        if (batterToBag == 1 && !(moment?.NarratesBatterAtFirst ?? false))
                            caption = $"{caption} {Batter.Name} in at first.";
                        // The batter safe at first behind an out on another body is the fielder's choice (§10.4): the stamp reads the typed flag.
                        if (FieldersChoiceNow(batterToBag))
                            caption = $"{caption} Fielder's choice.";
                        liveNarrated = true;
                        break;
                }
                NextBatter();
                CheckInning();
                break;
        }

        // A reward target the live ball hit (F4-c): read off the play, never off where the ball landed.
        if (LivePlay.RewardThisPlay is not null &&
            kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun or PlayKind.FlyOut)
        {
            AddStars(defense: false, Rules.Stars.Gains.Billboard);
            caption += "  Billboard STAR!";
        }

        // The item that mattered (§12): it landed and the batter reached.
        if (LivePlay.ItemLanded && batterToBag >= 1 && kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun)
            AddMvp(Batter.Id, mvp.ItemMattered);

        if (field.Item is { } item)
        {
            caption += item switch
            {
                "banana" => "  Banana slip!",
                "rocket" => "  Rocket daze!",
                "pow" => "  POW!",
                _ => $"  {item}!"
            };
        }

        // A live decision nobody narrated above leads the caption. Typed: no branch reads caption text.
        if (moment is not null && !liveNarrated)
            caption = $"{LivePlay.Caption} {caption}";

        // The ERROR (§8.5, §8.6): a throw that skipped past its cover let the offense take what it took.
        // A bobble is only time; it is never the error by itself.
        var error = kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple && field.ThrowSailed;
        LivePlay.Reset();
        // A foul keeps the at-bat and a hit ends it; either way the pitch is over and the box recenters (D12).
        return AfterPitch(Emit(kind, pitch, swing, hit, caption, runs, scorers,
            field.Fielder, field.Throw, field.HangTimeSec, field.LandingX, field.LandingZ,
            field.Heatball, field.Furnace,
            new PlayOutcome(DefensiveFeat: field.Feat, BatterToBag: batterToBag, Error: error,
                GroundRuleDouble: kind == PlayKind.Double && field.GroundRule, Bodies: bodies)));
    }

    /// <summary>
    /// Seat every live runner where they stand and score the ones who crossed before the third
    /// out (§1, §10.6). The batter-runner's bag is the play's kind; 0 means they never reached one.
    /// </summary>
    (int Runs, IReadOnlyList<string> Scorers) SettleRunners(out int batterToBag)
    {
        var scorers = new List<string>();
        batterToBag = 0;
        var elapsed = LivePlay.ElapsedSeconds;
        // A batter nobody retired is safe at first (fair contact always sends them, §7); with the
        // inning over where they stood is moot. A forced runner still on a bag the batter now takes
        // moves up the chain, by the force, never by a table (scripted plays without a tick).
        if (BatterRunner is { Live: true, Bag: 0 } batter && Outs < 3)
        {
            for (var bag = 3; bag >= 1; bag--)
            {
                var forced = RunnerAt(bag);
                if (forced is null || !forced.Live || !forced.Forced || forced.Bag != bag || forced.Feet > 0) continue;
                if (!_runners.Any(o => o != forced && o.Live && o.IsOn(bag) && o.FromBag < bag) && bag != 1) continue;
                forced.Arrive(bag + 1, elapsed);
            }
            batter.Arrive(1, elapsed);
        }
        foreach (var r in _runners.OrderByDescending(x => x.FromBag).ToList())
        {
            if (r.Scored)
            {
                if (RunCounts(r))
                {
                    Score(r.Who);
                    scorers.Add(r.Who.Name);
                    RecordMove(r.Who, r.FromBag, 4);
                }
                if (r.IsBatter) batterToBag = RunCounts(r) ? 4 : 0;
                continue;
            }
            if (!r.Live) continue;
            var bag = r.Bag;
            if (r.IsBatter) batterToBag = bag;
            if (bag < 1)
            {
                // The batter short of first when the third out fell elsewhere: nobody to seat.
                r.Retire();
                continue;
            }
            RecordMove(r.Who, r.FromBag, bag);
            r.Seat(bag);
        }
        PruneRunners();
        return (scorers.Count, scorers);
    }

    PlayEvent Emit(
        PlayKind kind, PitchCommand pitch, SwingCommand swing, AtBatResult hit, string caption,
        int runs, IReadOnlyList<string> scorers,
        Character? fielder = null, ThrowResult? throwRes = null,
        double hang = 0, double lx = 0, double lz = 0,
        bool heat = false, bool furnace = false,
        PlayOutcome? outcome = null)
    {
        var origin = CurrentPlay();
        var next = CaptureMatchState();
        pitch = Settled(pitch);
        swing = Settled(swing);
        var ev = new PlayEvent(
            kind, hit, pitch, swing, origin.Batter, origin.Pitcher, fielder, throwRes, runs, scorers, caption,
            heat, furnace, hang, lx, lz, next.Outs, next.AwayScore, next.HomeScore,
            _outsOnCurrentPlay, origin.Context, next, WithPlayFacts(outcome));
        _log.Add(ev);
        _pendingPlay = null;
        return ev;
    }

    /// <summary>Stamp the play's outs and placements on the outcome. Fielder's choice falls out of them.</summary>
    PlayOutcome WithPlayFacts(PlayOutcome? outcome)
    {
        var facts = outcome ?? PlayOutcome.Empty;
        var outs = _outsThisPlay.ToList();
        var moves = _movesThisPlay.ToList();
        var stars = _starRequestsThisPlay.Count > 0 ? _starRequestsThisPlay.ToList() : facts.StarRequests;
        return facts with { Outs = outs, Advances = moves, FieldersChoice = FieldersChoiceNow(facts.BatterToBag), StarRequests = stars };
    }

    /// <summary>The batter on first with an out recorded on another body this play (§10.4).</summary>
    bool FieldersChoiceNow(int batterToBag) => batterToBag == 1 && _outsThisPlay.Any(o => o.FromBag != 0);

    PlayOrigin BeginPlay()
    {
        var batter = Batter;
        var pitcher = Pitcher;
        var context = new PlayContext(
            batter.Id, pitcher.Id, Inning, Top, Outs, Balls, Strikes, AwayScore, HomeScore,
            First?.Id, Second?.Id, Third?.Id);
        _pendingPlay = new PlayOrigin(context, batter, pitcher);
        _outsThisPlay.Clear();
        _movesThisPlay.Clear();
        _starRequestsThisPlay.Clear();
        return _pendingPlay;
    }

    PlayOrigin CurrentPlay() => _pendingPlay ?? BeginPlay();

    MatchState CaptureMatchState() => new(
        Batter.Id, Pitcher.Id, Inning, Top, Outs, Balls, Strikes, AwayScore, HomeScore, Over,
        First?.Id, Second?.Id, Third?.Id);

    PlayEvent FinishEvent(PlayEvent ev)
    {
        var next = CaptureMatchState();
        var result = ev with
        {
            OutsOnPlay = _outsOnCurrentPlay,
            OutsAfter = next.Outs,
            AwayScoreAfter = next.AwayScore,
            HomeScoreAfter = next.HomeScore,
            NextState = next,
            Outcome = WithPlayFacts(ev.Outcome)
        };
        if (_log.Count > 0)
            _log[^1] = result;
        return result;
    }

    /// <summary>
    /// The plate appearance is complete: the one seam every strikeout, walk, hit-by-pitch and ball in play passes
    /// once (§12). Both teams earn the base gain here (PH-16-R5), each into its own pool, whoever won the
    /// appearance, on top of any bonus the play earned. A half that ends on a runner out between pitches (a caught
    /// stealing or a pickoff for the third out) never reaches it: that appearance did not complete, and its batter
    /// leads off his side's next half with a fresh count.
    /// </summary>
    void NextBatter()
    {
        var pa = Rules.Stars.Gains.PlateAppearance;
        if (pa > 0)
        {
            AddStars(defense: true, pa);
            AddStars(defense: false, pa);
        }
        Balls = 0;
        Strikes = 0;
        _cpuStealDecided = false;
        // The next hitter starts centered (the box recenters after every pitch too, D12: AfterPitch).
        ResetBatter();
        if (Top) AwayBatter = (AwayBatter + 1) % AwayOrder.Count;
        else HomeBatter = (HomeBatter + 1) % HomeOrder.Count;
    }

    void EndIfWalkOff()
    {
        if (!Top && Inning >= Innings && HomeScore > AwayScore)
            Over = true;
    }

    /// <summary>
    /// Three outs: the half ends (spec §1, D8). The bottom is skipped when home leads after the
    /// top of the last inning; a tie plays extra innings to the cap; mercy ends it at the end of
    /// a half when the side that just batted is the trailing one by the table's margin.
    /// </summary>
    void CheckInning()
    {
        if (Outs < 3) return;
        Outs = 0;
        Balls = 0;
        Strikes = 0;
        _swappedThisHalf = false;
        PitcherOffsetX = 0;
        ClearBags();
        if (Top)
        {
            if (Inning >= Innings && HomeScore > AwayScore)
            {
                Over = true;
                return;
            }
            if (MercyEnds(HomeScore - AwayScore))
            {
                Over = true;
                return;
            }
            Top = false;
            return;
        }

        if (MercyEnds(AwayScore - HomeScore))
        {
            Over = true;
            return;
        }
        if (Inning >= Innings)
        {
            if (HomeScore != AwayScore || Inning >= Innings + Rules.Match.ExtraInningsCap)
            {
                Over = true;
                return;
            }
        }
        Inning++;
        Top = true;
    }

    /// <summary>Mercy (§1): the side that just batted trails by the table's runs, from its first inning on, in a game long enough.</summary>
    public bool MercyEnds(int leadOverSideThatJustBatted)
    {
        var m = Rules.Match.Mercy;
        if (!Mercy || Innings < m.MinScheduledInnings || Inning < m.FromInning) return false;
        return leadOverSideThatJustBatted >= m.Runs;
    }

    /// <summary>Past the scheduled innings (D8): a tie here plays on to the cap.</summary>
    public bool ExtraInnings => Inning > Innings;

    /// <summary>A walk or a hit by pitch (§7.12): the batter to first and only the forced runners one bag, by rule.</summary>
    (int Runs, IReadOnlyList<string> Scorers) PlaceByWalk(Character batter)
    {
        var scorers = new List<string>();
        foreach (var scored in _runners.Where(r => r.Scored).ToList())
        {
            Score(scored.Who);
            scorers.Add(scored.Who.Name);
            RecordMove(scored.Who, scored.FromBag, 4);
            _runners.Remove(scored);
        }
        var f = RunnerAt(1);
        var second = RunnerAt(2);
        var third = RunnerAt(3);
        if (f is not null && second is not null && third is not null)
        {
            Score(third.Who);
            scorers.Add(third.Who.Name);
            RecordMove(third.Who, 3, 4);
            third.Score(0);
        }
        if (f is not null && second is not null)
        {
            second.Seat(3);
            RecordMove(second.Who, 2, 3);
        }
        if (f is not null)
        {
            f.Seat(2);
            RecordMove(f.Who, 1, 2);
        }
        _runners.RemoveAll(r => r.IsBatter);
        _runners.Add(new Runner(batter, 1));
        RecordMove(batter, 0, 1);
        PruneRunners();
        return (scorers.Count, scorers);
    }

    /// <summary>A ground-rule double (§1): the batter and every runner exactly <paramref name="bases"/> bases, by rule.</summary>
    (int Runs, IReadOnlyList<string> Scorers) AwardBases(int bases)
    {
        var scorers = new List<string>();
        if (BatterRunner is null) _runners.Add(Runner.BatterRunner(Batter, HomeSet.BatterBodyX(Batter.Bats, BatterContactOffsetX), HomeSet.BatterZ));
        foreach (var r in _runners.OrderByDescending(x => x.FromBag).ToList())
        {
            if (!r.Live) continue;
            var dest = Math.Min(4, r.FromBag + bases);
            RecordMove(r.Who, r.FromBag, dest);
            if (dest >= 4)
            {
                Score(r.Who);
                scorers.Add(r.Who.Name);
                r.Score(LivePlay.ElapsedSeconds);
            }
            else
                r.Seat(dest);
        }
        PruneRunners();
        return (scorers.Count, scorers);
    }

    /// <summary>A home run (§7.10): everyone scores, the batter last.</summary>
    (int Runs, IReadOnlyList<string> Scorers) ScoreEveryone()
    {
        var scorers = new List<string>();
        foreach (var r in _runners.OrderByDescending(x => x.FromBag))
        {
            if (r.Out || r.IsBatter) continue;
            Score(r.Who);
            scorers.Add(r.Who.Name);
            RecordMove(r.Who, r.FromBag, 4);
        }
        Score(Batter);
        scorers.Add(Batter.Name);
        RecordMove(Batter, 0, 4);
        ClearBags();
        return (scorers.Count, scorers);
    }

    /// <summary>A dead foul (§5.6): the batter goes back to the box, every runner back to their bag.</summary>
    void ResetRunnersToBags()
    {
        _runners.RemoveAll(r => r.IsBatter || r.Out);
        foreach (var r in _runners) r.Seat(r.FromBag);
        SyncSelection();
    }

    void ClearBags()
    {
        _runners.Clear();
        _selectedBag = 0;
        _pickedRunner = false;
    }

    void SyncSelection()
    {
        if (_pickedRunner && _selectedBag == 0 && BatterRunner is not null) return;
        var next = Baserunning.SyncSelected(
            _selectedBag, _pickedRunner, First is not null, Second is not null, Third is not null, LeadBag);
        if (next != _selectedBag || next == 0)
            _pickedRunner = false;
        _selectedBag = next;
    }

    /// <summary>Every arm that has not broken comes off (a foul, a dead pitch); a body that broke is a body (§11.2).</summary>
    void ClearSteal()
    {
        foreach (var r in _runners) r.CancelSteal();
    }

    void Score(Character who)
    {
        _ = who;
        var ledBefore = OffenseLead;
        if (Top) AwayScore++;
        else HomeScore++;
        // The arm that will be the winning pitcher (§12): the offense's own, whenever the offense takes the lead.
        if (ledBefore <= 0 && OffenseLead > 0)
            _leadPitcherId = (Top ? _awayPitcher : _homePitcher).Id;
    }

    /// <summary>The offense's lead in runs (negative when trailing).</summary>
    int OffenseLead => Top ? AwayScore - HomeScore : HomeScore - AwayScore;

    /// <summary>The pitcher of the side that last took the lead (the winning pitcher if it holds, §12).</summary>
    string? _leadPitcherId;

    /// <summary>
    /// The batter's MVP credit for the play (§12, stars.json mvp): the base points of the hit / walk /
    /// HBP, an RBI per run driven in, and the go-ahead RBI on top when the play put the offense ahead.
    /// </summary>
    void CreditBatter(int basePoints, int runs, int ledBefore)
    {
        var m = Rules.Stars.Mvp;
        var goAhead = runs > 0 && ledBefore <= 0 && OffenseLead > 0;
        AddMvp(Batter.Id, basePoints + runs * m.Rbi + (goAhead ? m.GoAheadRbi : 0));
    }

    /// <summary>The seat that won a close play at a bag (§9.6, §12): the runner called safe, or the glove that tagged.</summary>
    internal void CreditClosePlay(Character? who) => AddMvp(who?.Id, Rules.Stars.Mvp.ClosePlayWon);

    void SpendPitch(PitchCommand pitch)
    {
        var st = Rules.Pitching.Stamina;
        var cost = st.PitchCost
                   + (ChargeFeel.IsCharge(pitch.Charge01) ? st.ChargeCost : 0)
                   + Rules.Pitching.Families.Of(pitch.Type).StaminaCost
                   + (pitch.BreakX != 0 ? st.BreakCost : 0)
                   + (pitch.Star ? StarSkills.StaminaCost(Pitcher.StarPitch, Content.StarSkills) : 0);
        ChargeArm(Pitcher, cost);
        _lastPitchRubberX = pitch.RubberX;
    }

    /// <summary>A dead pitch. The random pickoff that used to ride here is gone with the leads (D1, D3).</summary>
    /// <summary>
    /// The pitch is over (spec §3, D12, #607): whatever it was — a ball, a strike, a foul, a walk, a
    /// plunk, a strikeout, a ball in play — the box recenters for the next SET. The swing that used the
    /// walk already latched it (<see cref="BatterContactOffsetX"/>). Every pitch's finish returns
    /// through here; a pickoff in SET is not a pitch and leaves the box alone.
    /// </summary>
    PlayEvent AfterPitch(PlayEvent ev)
    {
        if (!StealThrowPending || ev.Kind is not (PlayKind.TakeBall or PlayKind.TakeStrike or PlayKind.SwingMiss or PlayKind.Strikeout))
            PitchSetup.TakeCatcherInput();
        ResetBatter();
        return ev;
    }

    void AddStars(bool defense, double amount)
    {
        if (!StarsEnabled) return;
        var max = Rules.Stars.MeterMax;
        if (defense)
        {
            if (Top) HomeStars = Math.Min(max, HomeStars + amount);
            else AwayStars = Math.Min(max, AwayStars + amount);
        }
        else
        {
            if (Top) AwayStars = Math.Min(max, AwayStars + amount);
            else HomeStars = Math.Min(max, HomeStars + amount);
        }
    }

    void AddMvp(string? id, int pts)
    {
        if (string.IsNullOrEmpty(id) || pts <= 0) return;
        _mvp[id] = _mvp.GetValueOrDefault(id) + pts;
    }

    AtBatResult EmptyHit(bool inZone) => new(
        ContactQuality.Miss, false, inZone, 0, 0, 0, false, false, null, null, 0, false, inZone);

    double Gauss()
    {
        var u1 = 1.0 - _rng.NextDouble();
        var u2 = _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}

/// <summary>
/// What a CPU pitch was built from (spec §4.8, #823), so a
/// scenario can read the inputs rather than infer them from the flight: the presses, the family
/// those presses land on, the charge, the stick and the rubber.
///
/// A fact about one delivery, not state: <see cref="Match.CpuPitchByInputs"/> hands it back beside
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
