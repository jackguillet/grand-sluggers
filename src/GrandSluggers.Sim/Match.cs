namespace GrandSluggers.Sim;

public sealed class Match
{
    public const int DefaultInnings = 3;

    readonly AtBatResolver _atBat;
    readonly FieldingResolver _fielding;
    /// <summary>The only random stream that may decide a play. Seeded per match; every roll is a sim call.</summary>
    readonly Random _rng;
    readonly Dictionary<string, int> _mvp = new(StringComparer.OrdinalIgnoreCase);
    readonly List<PlayEvent> _log = [];

    public ContentCatalog Content { get; }
    public Park Park { get; }
    public bool Night { get; }
    public Team Away { get; }
    public Team Home { get; }
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
    /// <summary>The rule numbers this match plays by (data/rules).</summary>
    public RulesTable Rules => Content.Rules;
    /// <summary>Portable command boundary for the ball between contact and Time.</summary>
    public LivePlaySystem LivePlay { get; }

    public Match(ContentCatalog content, Team away, Team home, Park park, int innings = DefaultInnings, int seed = 1, bool night = false, bool mercy = true)
    {
        Content = content;
        Mercy = mercy;
        Away = away;
        Home = home;
        Park = park;
        Night = night;
        Innings = innings;
        _rng = new Random(seed);
        _atBat = new AtBatResolver(content.Chemistry, content.Rules, content.StarSkills);
        _fielding = new FieldingResolver(content.Chemistry, content.Rules);
        LivePlay = new LivePlaySystem(this);
        AwayOrder = away.BattingOrder;
        HomeOrder = home.BattingOrder;
        AwayStars = content.Chemistry.StartingStars(away);
        HomeStars = content.Chemistry.StartingStars(home);
        _homePitcher = home.Pitcher;
        _awayPitcher = away.Pitcher;
        _homeDefense = home.Roster.ToList();
        _awayDefense = away.Roster.ToList();
        HomeBat = GearMesh.SignatureBat(content, home.Captain.Id);
        AwayBat = GearMesh.SignatureBat(content, away.Captain.Id);
        HomeGlove = content.Gloves.GetValueOrDefault("web-back") ?? content.Gloves.Values.First();
        AwayGlove = content.Gloves.GetValueOrDefault("lucky-mitt") ?? content.Gloves.Values.First();
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

    public static Match Slice(ContentCatalog content, int innings = DefaultInnings, int seed = 1, string parkId = "harbor-diamond", bool night = false)
    {
        if (!content.Parks.TryGetValue(parkId, out var park))
            park = content.Parks["harbor-diamond"];
        return new Match(content, PresetTeams.EmberCourt(content), PresetTeams.SparkAllStars(content), park, innings, seed, night);
    }

    public static Match Exhibition(
        ContentCatalog content,
        string homeCaptain = "rio",
        string awayCaptain = "ashlord",
        int innings = DefaultInnings,
        int seed = 1,
        string? parkId = null,
        bool night = false)
    {
        var (home, away) = PresetTeams.Pair(content, homeCaptain, awayCaptain);
        return Exhibition(content, home, away, innings, seed, parkId ?? PresetTeams.HomeParkId(homeCaptain), night);
    }

    public static Match Exhibition(
        ContentCatalog content,
        Team home,
        Team away,
        int innings = DefaultInnings,
        int seed = 1,
        string? parkId = null,
        bool night = false)
    {
        parkId ??= PresetTeams.HomeParkId(home.Captain.Id);
        if (!content.Parks.TryGetValue(parkId, out var park))
            park = content.Parks["harbor-diamond"];
        return new Match(content, away, home, park, innings, seed, night);
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
        n = Math.Clamp(n, 0, 5);
        if (Top) AwayStars = Math.Max(AwayStars, n);
        else HomeStars = Math.Max(HomeStars, n);
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
    public bool StealOn { get; private set; }
    public double Dash01 { get; set; }

    sealed record PlayOrigin(PlayContext Context, Character Batter, Character Pitcher);
    PlayOrigin? _pendingPlay;
    /// <summary>Every out and every runner placement on the current play, in order. Emit stamps them on the outcome.</summary>
    readonly List<OutRecord> _outsThisPlay = [];
    readonly List<RunnerMove> _movesThisPlay = [];
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
    /// <summary>Occupied bag whose steal is armed, else 0.</summary>
    public int ArmedStealBag
    {
        get
        {
            for (var bag = 1; bag <= 2; bag++)
                if (RunnerAt(bag)?.StealArmed == true) return bag;
            return 0;
        }
    }
    public int StealTargetBag
    {
        get
        {
            var armed = RunnerAt(ArmedStealBag)?.StealTarget ?? 0;
            if (armed is 2 or 3) return armed;
            return Baserunning.StealTarget(SelectedBag);
        }
    }
    public bool CanSteal =>
        !Over && Outs < 3 &&
        Baserunning.CanSteal(SelectedBag, First is not null, Second is not null, Third is not null);
    /// <summary>
    /// Steal is armed after a take or swing-and-miss. Resolve with
    /// <see cref="GunSteal"/> (CPU) or <see cref="ResolveStealThrow"/> (player throw).
    /// </summary>
    public bool StealThrowPending =>
        StealOn && !Over && Outs < 3 && ArmedStealBag is 1 or 2;

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

    /// <summary>Stick back on the selected runner: before the pitch it cancels their steal; live, it brings them back (§9.3).</summary>
    public bool ReturnToBag() => ReturnToBagAt(SelectedBag);

    public bool ReturnToBagAt(int fromBag)
    {
        var state = RunnerAt(fromBag);
        if (state is null) return false;
        state.CancelSteal();
        if (LivePlay.Active) state.Return(human: true);
        if (ArmedStealBag == 0) StealOn = false;
        return true;
    }

    /// <summary>Stick toward the next bag on the selected runner during a live ball: send them (§9.3). Forced runners are already going.</summary>
    public bool SendRunner() => SendRunnerAt(SelectedBag);

    public bool SendRunnerAt(int fromBag)
    {
        var state = RunnerAt(fromBag);
        if (state is null || !LivePlay.Active) return false;
        state.Send(state.NextBag, human: true);
        return true;
    }

    /// <summary>LB: before the pitch, arm tag-and-go; live, send every runner (§9.3, §9.5).</summary>
    public bool AdvanceAll()
    {
        if (Over || Outs >= 3) return false;
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
            if (LivePlay.Active) r.Return(human: true);
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
            if (r.Live && LivePlay.Active) r.Halt();
        return true;
    }

    /// <summary>Stick toward a bag + halt: freeze that runner only.</summary>
    public bool HaltAt(int fromBag)
    {
        var state = RunnerAt(fromBag);
        if (state is null) return false;
        state.CancelSteal();
        if (LivePlay.Active) state.Halt();
        if (ArmedStealBag == 0) StealOn = false;
        return true;
    }

    public bool StartSteal()
    {
        if (!CanSteal) return false;
        var state = SelectedState;
        if (state is null) return false;
        var target = Baserunning.StealTarget(SelectedBag);
        if (target is not 2 and not 3) return false;
        foreach (var r in _runners)
            if (r != state) r.CancelSteal();
        state.ArmSteal();
        StealOn = true;
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
        AddMvp(fielder?.Id ?? Pitcher.Id, 2);
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
        _runners.RemoveAll(r => r.IsBatter || !r.Live);
        var forces = InPlay.ForceState.FromOccupancy(First is not null, Second is not null, Third is not null);
        foreach (var r in _runners)
            r.BeginPlay(forces.At(r.Bag + 1), SendAll);
        _runners.Add(Runner.BatterRunner(Batter, startX, startZ));
        _thirdOutAt = double.PositiveInfinity;
        _thirdOutKillsRuns = false;
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

    /// <summary>Bobble on a live scoop, from the one seeded stream (S-92).</summary>
    internal bool RollBobble(double energy, Character who) =>
        InPlay.Bobbles(energy, who, _rng, DefenseGlove, Rules);

    /// <summary>
    /// A drop on the catch is allowed only for star effects (§8.6, fielding.drops): a heatball, a
    /// phony swing, a frozen glove. Plain baseball never rolls a drop. One seeded stream (S-92).
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

    public int StarCost(Character who, Character teamCaptain) =>
        who.Captain && !who.Id.Equals(teamCaptain.Id, StringComparison.OrdinalIgnoreCase)
            ? Rules.Stars.Costs.GuestCaptain
            : Rules.Stars.Costs.Own;

    public int PitchStarCost => StarCost(Pitcher, Defense.Captain);
    public int SwingStarCost => StarCost(Batter, Offense.Captain);
    public bool CanStarPitch => DefenseStars >= PitchStarCost;
    public bool CanStarSwing => OffenseStars >= SwingStarCost;

    public bool WalkPitcher(double delta)
    {
        if (Over) return false;
        PitcherOffsetX = Math.Clamp(PitcherOffsetX + delta, -1, 1);
        return true;
    }

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
    /// Named-bag pickoff before the pitch. Glued runner (no lead, no steal) returns, never a free out.
    /// Steal-on or a walking lead can be caught.
    /// </summary>
    public PlayEvent? Pickoff(int bag)
    {
        if (Over || Outs >= 3 || bag is < 1 or > 3) return null;
        var runner = bag == 1 ? First : bag == 2 ? Second : Third;
        var state = RunnerAt(bag);
        if (runner is null || state is null) return null;
        BeginPlay();
        // D1 / D3: a runner on the bag is always safe; only an armed runner is caught (P6 lands the real break).
        if (!StealOn || !state.StealArmed)
        {
            state.CancelSteal();
            StealOn = false;
            var stay = Emit(PlayKind.TakeBall, new PitchCommand("fastball", 0, false),
                new SwingCommand(false, 0, 0, false), EmptyHit(true),
                $"{runner.Name} back to the bag.", 0, []);
            return stay;
        }
        var fake = new PitchCommand("fastball", 0, false);
        var take = new SwingCommand(false, 0, 0, false);
        StealOn = true;
        state.ArmSteal();
        var ev = Emit(PlayKind.TakeBall, fake, take, EmptyHit(true), "Pickoff.", 0, []);
        return GunSteal(ev, pickoff: true);
    }

    public bool ToggleSteal()
    {
        if (StealOn)
        {
            ClearSteal();
            return false;
        }
        if (!CanSteal) return false;
        return StartSteal();
    }

    public PlayEvent Play(PitchCommand pitch, SwingCommand swing, string? item = null)
    {
        pitch = PreparePitch(pitch);
        if (!BeginAtBat(pitch, swing, out var hit, out var finished))
            return StealThrowPending ? GunSteal(finished!) : finished!;
        var preview = PreviewHit(hit);
        var field = ResolveFielding(hit, preview);
        field = ApplyOffenseItem(hit, field, item);
        return RunLive(pitch, swing, hit, preview, field, LiveSeats.CpuOnly);
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

    /// <summary>Sample delivery error once, before flight, so the visible pitch is the judged pitch.</summary>
    public PitchCommand PreparePitch(PitchCommand pitch)
    {
        if (pitch.DeliveryPrepared) return pitch;
        var ready = pitch with { RubberX = pitch.RubberX != 0 ? pitch.RubberX : PitcherOffsetX,
            DeliveryPrepared = true };
        if (PitcherTired)
        {
            // TIRED (spec §4.7): a crossing wobble in feet and less break; exhausted is worse.
            var st = Rules.Pitching.Stamina;
            var wobble = PitcherExhausted ? st.ExhaustedWobbleFt : st.TiredWobbleFt;
            ready = ready with
            {
                AimX = ready.AimX + Gauss() * wobble / PitchFlight.PlateScaleX,
                AimY = ready.AimY + Gauss() * wobble / PitchFlight.PlateScaleY,
                BreakMul = st.TiredBreakMul
            };
        }
        return ready;
    }

    /// <summary>The pitch's speed from this arm: shape, stat, charge, Nice!, the star skill, and fatigue (spec §4.7).</summary>
    public double PitchSpeedMph(PitchCommand pitch)
    {
        var st = Rules.Pitching.Stamina;
        var penalty = PitcherExhausted ? st.ExhaustedMph : PitcherTired ? st.TiredMph : 0;
        return AtBatResolver.PitchSpeedMph(pitch, Pitcher, Rules, Content.StarSkills, penalty);
    }

    // ---- stamina (spec §4.7) --------------------------------------------------------

    public int StaminaPool(Character who) =>
        Rules.Pitching.Stamina.PoolBase + who.Stats.Pitch * Rules.Pitching.Stamina.PoolPerPitch;

    public int StaminaOf(Character who) =>
        _stamina.TryGetValue(who.Id, out var pool) ? pool : StaminaPool(who);

    void ChargeArm(Character who, int cost)
    {
        if (cost == 0) return;
        _stamina[who.Id] = StaminaOf(who) - cost;
    }

    public bool BeginAtBat(PitchCommand pitch, SwingCommand swing, out AtBatResult hit, out PlayEvent? finished)
    {
        hit = EmptyHit(true);
        finished = null;
        if (Over) throw new InvalidOperationException("game over");
        BeginPlay();

        pitch = PreparePitch(pitch);
        // One crossing for the umpire, the body, and the bat: the shown pitch is the judged pitch (§3).
        var crossing = PitchFlight.Point(pitch, 1, Pitcher.StarPitch, rules: Rules);
        var inZone = StrikeZoneGeometry.Contains(crossing.X, crossing.Y);
        SpendPitch(pitch);
        if (Top) _awayLastCrossingX = crossing.X; else _homeLastCrossingX = crossing.X;
        var box = swing.BoxOffsetX != 0 ? swing.BoxOffsetX : BatterOffsetX;
        BatterContactOffsetX = box;

        if (!swing.Swing)
        {
            finished = AtBatResolver.HitsBatter(box, crossing.X, crossing.Y, Batter.Bats, Rules)
                ? FinishHitByPitch(pitch, swing, EmptyHit(inZone))
                : FinishTake(pitch, swing, inZone);
            EndIfWalkOff();
            finished = FinishEvent(finished);
            return false;
        }

        SpendSwing(swing);

        var bat = OffenseBat;
        var input = new AtBatInput(
            Pitcher, Batter, OnDeck, RunnersOn().ToList(),
            ChargeFeel.IsCharge(pitch.Charge01), pitch.Changeup || pitch.Type == "changeup",
            swing.TimingErrorFrames, pitch.Star, swing.Star, bat,
            PitcherStamina,
            swing.SprayAimDeg, inZone, swing.Bunt, swing.LaunchAim,
            swing.Charge01, box, crossing.X, crossing.Y);

        hit = _atBat.Resolve(input, Park, _rng, Night);
        if (hit.Quality == ContactQuality.Miss)
        {
            finished = FinishStrike(pitch, swing, hit, swinging: true);
            return false;
        }
        // Every batted ball is live from here, foul territory included (§7.11): the flight is
        // fielded, a caught foul fly is an out, and the call is made where the ball lands or is
        // first touched. FinishInPlay stamps FOUL when it is dead.
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
            return RunLive(pitch, swing, hit, PreviewHit(hit), field, LiveSeats.CpuOnly);
        CurrentPlay();
        var played = FinishInPlay(pitch, swing, hit, field);
        EndIfWalkOff();
        return FinishEvent(played);
    }

    public FieldingPreview PreviewHit(AtBatResult hit) =>
        _fielding.Preview(hit, Park, Defense.Roster, Pitcher, _rng, Night, Defense.Gloves);

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
        if (_rng.NextDouble() < Rules.Batting.Items.CpuThrowChance)
            return ThrowItem(field, ErrorItems.Pick(_rng), who);
        return field;
    }

    /// <summary>
    /// After contact: throw a banana / rocket / POW at a fielder. Empty or unknown item is a no-op.
    /// </summary>
    public FieldingResult ThrowItem(FieldingResult field, string? item, Character? target)
    {
        if (string.IsNullOrEmpty(item)) return field;
        return ErrorItems.Apply(field, item, _rng, target, Rules);
    }

    /// <summary>
    /// The CPU pitcher (spec §4.8): one row of the table per SET from the count, the outs and the
    /// runners; a location target in world feet (never dead center), a verb from the row's mix,
    /// scatter σ = (11 − Pitch) × scatterFtPerPitchStat around the target, TIRED noise on top.
    /// Walking the rubber is a real verb here too: the batter may mistrack it (§5.9).
    /// </summary>
    public PitchCommand CpuPitch()
    {
        var c = Rules.Pitching.Cpu;
        var row = CpuPitchRow();
        if (_rng.NextDouble() < c.RubberWalkChance)
            PitcherOffsetX = (_rng.NextDouble() * 2 - 1) * c.RubberWalkMax;

        var (tx, ty) = CpuPitchTarget(row.Location, c.Locations);
        var scatter = (11 - Pitcher.Stats.Pitch) * c.ScatterFtPerPitchStat * (PitcherTired ? c.TiredScatterMul : 1);
        tx += Gauss() * scatter;
        ty += Gauss() * scatter;

        var verb = CpuPitchVerb(row);
        var charged = verb == "charge";
        var changeup = verb == "changeup";
        var breakX = verb == "break" ? (_rng.NextDouble() < 0.5 ? -1.0 : 1.0) : 0;
        var charge = charged ? 1.0 : c.TapMin + _rng.NextDouble() * c.TapSpan;
        var star = CanStarPitch && Pitcher.Captain && _rng.NextDouble() < row.StarChance;
        var delivery = new PitchCommand(changeup ? "changeup" : "fastball", charge, star,
            Changeup: changeup, BreakX: breakX, RubberX: PitcherOffsetX,
            Nice: charged && _rng.NextDouble() < c.NiceChance);
        // The row names a crossing; the rubber and the break are compensated into the aim.
        return PitchFlight.AimForCrossing(delivery, tx / PitchFlight.PlateScaleX,
            (ty - PitchFlight.PlateY) / PitchFlight.PlateScaleY, Pitcher.StarPitch, Rules);
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

    /// <summary>The named location in world feet at the plate plane; the away side is by batter hand.</summary>
    (double X, double Y) CpuPitchTarget(string location, CpuPitchLocations loc)
    {
        var away = SweetSpot.TipSign(Batter.Bats);
        var halfW = StrikeZoneGeometry.HalfWidth;
        var halfH = StrikeZoneGeometry.Height / 2;
        var cy = StrikeZoneGeometry.CenterY;
        switch (location)
        {
            case "waste":
                return (away * (halfW + loc.WasteOutFt), cy + (_rng.NextDouble() < 0.5 ? -1 : 1) * halfH / 2);
            case "middleIn":
                return (-away * loc.MiddleInFt, cy);
            case "middle":
                return (0, cy + (_rng.NextDouble() * 2 - 1) * loc.MiddleYSpreadFt);
            default:
                var side = _rng.NextDouble() < loc.EdgeAwayChance ? away : -away;
                var vertical = _rng.NextDouble() < 0.5 ? -1 : 1;
                return (side * (halfW - loc.EdgeInsetFt), cy + vertical * (halfH - loc.EdgeInsetFt));
        }
    }

    string CpuPitchVerb(CpuPitchRow row)
    {
        var total = row.Normal + row.Charge + row.Changeup + row.Break;
        var roll = _rng.NextDouble() * total;
        if (roll < row.Normal) return "normal";
        roll -= row.Normal;
        if (roll < row.Charge) return "charge";
        roll -= row.Charge;
        if (roll < row.Changeup) return "changeup";
        return "break";
    }

    /// <summary>
    /// The CPU batter (spec §5.9): a table read at the plate plane from the final crossing, the
    /// same whoever is pitching. No side effects: the box it stands in and the swing it makes are
    /// the returned command. Steals are the runner AI's (<see cref="CpuArmSteal"/>).
    /// </summary>
    public SwingCommand CpuSwing(PitchCommand pitch, bool inZone)
    {
        var c = Rules.Batting.Cpu;
        var level = Rules.Cpu.Active;
        var bat = Batter.Stats.Bat;
        var (cx, cy) = PitchFlight.Crossing(pitch, Pitcher.StarPitch, Rules);
        var zone = CpuZoneClass(cx, cy, inZone, c);
        var take = new SwingCommand(false, 0, 0, false);

        // Sac bunt: runner on first only, no outs, a light bat, close game.
        var trailing = (Top ? HomeScore - AwayScore : AwayScore - HomeScore);
        if (inZone && First is not null && Second is null && Third is null && Outs == 0
            && bat <= c.SacBuntBatMax && trailing <= c.SacBuntTrailMax && _rng.NextDouble() < c.SacBuntChance)
            return new SwingCommand(true, 0, Gauss() * c.SacBuntErrorSigma * level.TimingSigmaMul, false,
                Gauss() * c.SacBuntSpraySigma, Bunt: true, LaunchAim: c.SacBuntLaunchAim,
                BoxOffsetX: CpuTrackedBox(cx, c, level));

        var swing = zone switch
        {
            CpuZone.Middle => true,
            CpuZone.Edge => Strikes == 2 || _rng.NextDouble() < c.EdgeSwingChance,
            CpuZone.Near => _rng.NextDouble() * 100 < (Strikes == 2 ? c.ChaseTwoStrikesBase : c.ChaseBase) - bat,
            _ => false
        };
        if (!swing) return take;

        var star = CanStarSwing && Batter.Captain && inZone && (RunnersOn().Any() || Strikes == 2)
                   && _rng.NextDouble() < c.StarChance;
        var risp = Second is not null || Third is not null;
        var forcedCharge = zone == CpuZone.Middle
                           && (((Balls, Strikes) is (2, 0) or (3, 1) or (3, 0)) && bat >= c.ChargeBatMin
                               || risp && Outs < 2 && bat >= c.RispChargeBatMin);
        var charge = forcedCharge || _rng.NextDouble() < CpuChargeChance(Batter, c.Archetype) ? 1.0 : 0;

        var tracked = _rng.NextDouble() < c.TrackPerfectChance;
        var err = Gauss() * (11 - bat) * c.ErrorFramesPerBatStat * level.TimingSigmaMul;
        if (!tracked && (pitch.IsChangeup || ChargeFeel.IsCharge(pitch.Charge01)))
        {
            // Fooled: a changeup pulls the bat early past the ball (late), a charged pitch beats it (early).
            var fooled = c.FooledMinFrames + _rng.NextDouble() * c.FooledSpanFrames;
            err += pitch.IsChangeup ? fooled : -fooled;
        }
        var box = tracked ? Math.Clamp(cx / HomeSet.BatterWalk, -1, 1) : CpuTrackedBox(cx, c, level);
        return new SwingCommand(true, charge, err, star, Gauss() * c.SpraySigmaDeg,
            LaunchAim: Gauss() * c.LaunchAimSigma, BoxOffsetX: box);
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

    /// <summary>Charge vs slap by archetype (spec §5.9), from the Bat / Run split.</summary>
    public static double CpuChargeChance(Character who, CpuArchetypeRules a)
    {
        var bat = who.Stats.Bat;
        var run = who.Stats.Run;
        if (bat >= a.TechniqueMin && run >= a.TechniqueMin) return a.Technique;
        if (bat - run >= a.SplitStat) return a.Power;
        if (run - bat >= a.SplitStat) return a.Speed;
        return a.Balanced;
    }

    /// <summary>
    /// The CPU offense's steal roll for this SET (running.cpu). It used to ride inside the swing;
    /// it is a SET verb of the runner. TODO(P6 #568): move into the runner AI (spec §11.6).
    /// </summary>
    public bool CpuArmSteal()
    {
        var run = Rules.Running.Cpu;
        if (!CanSteal || Batter.Stats.Run < run.StealMinRun || _rng.NextDouble() >= run.StealChance) return false;
        return StartSteal();
    }

    public PlayEvent AutoPlay()
    {
        CpuConsidersSwap();
        CpuArmSteal();
        var pitch = PreparePitch(CpuPitch());
        var inZone = AtBatResolver.PitchInZone(pitch, Pitcher.Stats.Pitch, Pitcher.StarPitch);
        var swing = CpuSwing(pitch, inZone);
        return Play(pitch, swing);
    }

    public void AutoPlayGame()
    {
        var guard = 0;
        while (!Over && guard++ < 2000)
            AutoPlay();
    }

    public (Character Who, int Points, string Why) Mvp()
    {
        if (_mvp.Count == 0)
            return (Home.Captain, 0, "showed up");
        var id = _mvp.OrderByDescending(kv => kv.Value).First().Key;
        var who = Away.Roster.Concat(Home.Roster).First(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        var pts = _mvp[id];
        var why = pts >= 8 ? "took over the diamond" : pts >= 4 ? "kept the line moving" : "did the little things";
        return (who, pts, why);
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
        AddMvp(Pitcher.Id, 2);
        AddStars(defense: true, Rules.Stars.Gains.Strikeout);
        RecordOut(OutType.Strikeout, 0, 0, Batter, Pitcher);
        how ??= swinging ? "goes down swinging." : "is caught looking.";
        var ev = Emit(PlayKind.Strikeout, pitch, swing, hit, $"{Batter.Name} {how}", 0, []);
        NextBatter();
        CheckInning();
        return FinishEvent(ev);
    }

    PlayEvent FinishWalk(PitchCommand pitch, SwingCommand swing, AtBatResult hit)
    {
        ClearSteal();
        var (runs, scorers) = PlaceByWalk(Batter);
        AddMvp(Batter.Id, 1 + runs);
        var ev = Emit(PlayKind.Walk, pitch, swing, hit, $"{Batter.Name} walks.", runs, scorers,
            outcome: new PlayOutcome(BatterToBag: 1));
        NextBatter();
        return ev;
    }

    PlayEvent FinishHitByPitch(PitchCommand pitch, SwingCommand swing, AtBatResult hit)
    {
        ClearSteal();
        var (runs, scorers) = PlaceByWalk(Batter);
        AddMvp(Batter.Id, 1 + runs);
        var ev = Emit(PlayKind.HitByPitch, pitch, swing, hit, $"{Batter.Name} is hit.", runs, scorers,
            outcome: new PlayOutcome(BatterToBag: 1));
        NextBatter();
        return ev;
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
        IReadOnlyList<string> scorers = [];
        var batterToBag = 0;
        // True once a branch built its caption from the live ball's own narration; otherwise
        // an open live moment is prefixed at the end. Typed, so no branch inspects caption text.
        var liveNarrated = false;
        var moment = LivePlay.LastMoment;

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
                AddMvp(Batter.Id, 0);
                ResetRunnersToBags();
                caption = "Foul.";
                break;
            case PlayKind.HomeRun:
                // Dead at the crossing (§7.10): everyone circles; the trot is presentation.
                ChargeArm(Pitcher, Rules.Pitching.Stamina.HomerCost);
                (runs, scorers) = ScoreEveryone();
                batterToBag = 4;
                AddMvp(Batter.Id, 5 + runs);
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
                    AddMvp(Batter.Id, 2 + runs);
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
                        AddMvp(Batter.Id, 5 + runs);
                        AddStars(defense: false, Rules.Stars.Gains.HomeRun);
                        caption = $"{Batter.Name} - all the way around!";
                        break;
                    case PlayKind.Triple:
                        AddMvp(Batter.Id, 3 + runs);
                        AddStars(defense: false, Rules.Stars.Gains.ExtraBaseHit);
                        caption = $"{Batter.Name} triples.";
                        break;
                    case PlayKind.Double:
                        AddMvp(Batter.Id, 2 + runs);
                        AddStars(defense: false, Rules.Stars.Gains.ExtraBaseHit);
                        caption = $"{Batter.Name} doubles.";
                        break;
                    case PlayKind.Single:
                        AddMvp(Batter.Id, 2 + runs);
                        AddStars(defense: false, Rules.Stars.Gains.Single);
                        caption = moment is not null
                            ? moment.NarratesBatterAtFirst ? LivePlay.Caption : $"{LivePlay.Caption} {Batter.Name} in at first."
                            : field.Warped ? $"{Batter.Name} - it hopped a {ParkHazards.WarpName(Park)}!"
                            : field.Heatball ? $"{Batter.Name} - it drops! Heatball."
                            : $"{Batter.Name} singles.";
                        liveNarrated = true;
                        break;
                    default:
                        caption = moment is not null ? LivePlay.Caption
                            : kind == PlayKind.FlyOut && field.Feat == DefensiveFeat.BuddyJump && field.Buddy is not null
                                ? $"{field.Fielder?.Name} + {field.Buddy.Name} BUDDY JUMP!"
                            : kind == PlayKind.FlyOut && field.Feat == DefensiveFeat.Clamber
                                ? $"{field.Fielder?.Name} CLAMBERS the wall!"
                            : kind == PlayKind.FlyOut && field.Feat == DefensiveFeat.SuperJump
                                ? $"{field.Fielder?.Name} SUPER JUMP!"
                            : field.Chomped
                                ? "A chomper ate it!"
                            : kind == PlayKind.FlyOut
                                ? $"{field.Fielder?.Name} puts it away."
                                : $"{field.Fielder?.Name} to first.";
                        if (kind == PlayKind.FlyOut && runs > 0)
                            caption = $"{caption} Sac fly.";
                        if (batterToBag == 1 && !(moment?.NarratesBatterAtFirst ?? false))
                            caption = $"{caption} {Batter.Name} in at first.";
                        liveNarrated = true;
                        break;
                }
                NextBatter();
                CheckInning();
                break;
        }

        if (ParkHazards.HitStarSign(Park, field.LandingX, field.LandingZ) &&
            kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun or PlayKind.FlyOut)
        {
            AddStars(defense: false, Rules.Stars.Gains.Billboard);
            caption += "  Billboard STAR!";
        }

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
        return Emit(kind, pitch, swing, hit, caption, runs, scorers,
            field.Fielder, field.Throw, field.HangTimeSec, field.LandingX, field.LandingZ,
            field.Heatball, field.Furnace,
            new PlayOutcome(DefensiveFeat: field.Feat, BatterToBag: batterToBag, Error: error,
                GroundRuleDouble: kind == PlayKind.Double && field.GroundRule));
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
        var choice = facts.BatterToBag == 1 && outs.Any(o => o.FromBag != 0);
        return facts with { Outs = outs, Advances = moves, FieldersChoice = choice };
    }

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

    void NextBatter()
    {
        Balls = 0;
        Strikes = 0;
        // The box persists across the pitches of one at-bat (§3); the next hitter starts centered.
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
        _runners.RemoveAll(r => r.IsBatter || !r.Live);
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

    void ClearSteal()
    {
        StealOn = false;
        foreach (var r in _runners) r.CancelSteal();
    }

    void Score(Character who)
    {
        if (Top) AwayScore++;
        else HomeScore++;
        AddMvp(who.Id, 1);
        // Each run allowed costs the arm on the mound (spec §4.7).
        ChargeArm(Pitcher, Rules.Pitching.Stamina.RunCost);
    }

    void SpendPitch(PitchCommand pitch)
    {
        var st = Rules.Pitching.Stamina;
        var cost = st.PitchCost
                   + (ChargeFeel.IsCharge(pitch.Charge01) ? st.ChargeCost : 0)
                   + (pitch.IsChangeup ? st.ChangeupCost : 0)
                   + (pitch.BreakX != 0 ? st.BreakCost : 0)
                   + (pitch.Star ? StarSkills.StaminaCost(Pitcher.StarPitch, Content.StarSkills) : 0);
        ChargeArm(Pitcher, cost);
        _lastPitchRubberX = pitch.RubberX;
        if (pitch.Star)
        {
            var starsCost = PitchStarCost;
            if (Top) HomeStars = Math.Max(0, HomeStars - starsCost);
            else AwayStars = Math.Max(0, AwayStars - starsCost);
        }
    }

    void SpendSwing(SwingCommand swing)
    {
        if (!swing.Star) return;
        var cost = SwingStarCost;
        if (Top) AwayStars = Math.Max(0, AwayStars - cost);
        else HomeStars = Math.Max(0, HomeStars - cost);
    }

    /// <summary>A dead pitch. The random pickoff that used to ride here is gone with the leads (D1, D3).</summary>
    PlayEvent AfterPitch(PlayEvent ev) => ev;

    /// <summary>Dead-stick CPU catcher still guns. 1P vs CPU does not require the throw.</summary>
    public PlayEvent GunSteal(PlayEvent ev, bool pickoff = false)
    {
        if (!StealThrowPending) return ev;
        if (!TryStealActors(out var fromBag, out var target, out var state, out var runner, out var catcher))
        {
            ClearSteal();
            return ev;
        }
        var throwBag = pickoff ? fromBag : target;
        var cover = FieldingResolver.Assign(Defense, Pitcher).GetValueOrDefault(StealThrow.CoverPos(throwBag));
        var defender = pickoff ? Pitcher : catcher;
        var thr = cover != null ? ThrowBetween(defender, cover) : ThrowBetween(defender, runner);
        var caught = StealThrow.CpuOut(runner, catcher, target, thr, _rng, Rules);
        return ApplySteal(ev, fromBag, target, runner, defender, thr, caught,
            pickoff ? ThrowOrigin.PitcherRubber : ThrowOrigin.Catcher, throwBag, pickoff);
    }

    /// <summary>
    /// Live catcher throw. Early + beat the runner is out; late or the wrong bag is a steal.
    /// Independent of the old one-roll.
    /// </summary>
    public PlayEvent ResolveStealThrow(PlayEvent ev, int throwBag, double releaseSec, ThrowResult? thr)
    {
        if (!StealThrowPending) return ev;
        if (!TryStealActors(out var fromBag, out var target, out var state, out var runner, out var catcher))
        {
            ClearSteal();
            return ev;
        }
        thr ??= ThrowBetween(catcher, runner);
        var caught = throwBag == fromBag
            ? StealThrow.PickoffOut(throwBag, releaseSec, thr, runner, Rules)
            : StealThrow.PlayerOut(throwBag, target, releaseSec, thr, runner, Rules);
        var destination = throwBag is >= 1 and <= 4 ? throwBag : target;
        return ApplySteal(ev, fromBag, target, runner, catcher, thr, caught,
            ThrowOrigin.Catcher, destination, pickoff: false);
    }

    bool TryStealActors(
        out int fromBag, out int target, out Runner state, out Character runner, out Character catcher)
    {
        fromBag = ArmedStealBag;
        target = 0;
        state = null!;
        runner = null!;
        var map = FieldingResolver.Assign(Defense, Pitcher);
        catcher = map.GetValueOrDefault("C") ?? Pitcher;
        var live = RunnerAt(fromBag);
        if (live?.Who is null || fromBag is not 1 and not 2)
            return false;
        target = live.StealTarget is 2 or 3 ? live.StealTarget : Baserunning.StealTarget(fromBag);
        if (target is not 2 and not 3) return false;
        if (target == 2 && Second is not null) return false;
        if (target == 3 && Third is not null) return false;
        state = live;
        runner = live.Who;
        return true;
    }

    PlayEvent ApplySteal(
        PlayEvent ev, int fromBag, int target, Character runner, Character defender, ThrowResult thr, bool caught,
        ThrowOrigin throwOrigin, int throwBag, bool pickoff)
    {
        StealOn = false;
        PlayEvent result;
        var body = RunnerAt(fromBag);
        if (!caught)
        {
            body?.Seat(target);
            SyncSelection();
            RecordMove(runner, fromBag, target);
            AddMvp(runner.Id, 2);
            AddStars(defense: false, Rules.Stars.Gains.StolenBase);
            result = ev with
            {
                Kind = PlayKind.StolenBase,
                Caption = ev.Caption + $"  {runner.Name} steals {(target == 3 ? "third" : "second")}.",
                Fielder = defender,
                Throw = thr,
                Outcome = new PlayOutcome(
                    RunnerResult: RunnerPlayResult.StolenBase,
                    RunnerFromBag: fromBag,
                    RunnerToBag: target,
                    ThrowEndpoint: new ThrowEndpoint(throwOrigin, throwBag))
            };
        }
        else
        {
            body?.Retire();
            PruneRunners();
            RecordOut(OutType.Tag, pickoff ? fromBag : throwBag, fromBag, runner, defender);
            AddMvp(defender.Id, 2);
            AddStars(defense: true, Rules.Stars.Gains.CaughtStealing);
            result = ev with
            {
                Kind = PlayKind.CaughtStealing,
                Caption = pickoff ? $"{runner.Name} picked off." : $"{runner.Name} caught stealing.",
                Fielder = defender,
                Throw = thr,
                Outcome = new PlayOutcome(
                    RunnerResult: pickoff ? RunnerPlayResult.PickedOff : RunnerPlayResult.CaughtStealing,
                    RunnerFromBag: fromBag,
                    RunnerToBag: pickoff ? fromBag : throwBag,
                    ThrowEndpoint: new ThrowEndpoint(throwOrigin, throwBag))
            };
            CheckInning();
        }

        ClearSteal();
        return FinishEvent(result);
    }

    void AddStars(bool defense, double amount)
    {
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
