namespace GrandSluggers.Sim;

/// <summary>The seat that produced a live-play command. Baseball does not branch by seat.</summary>
public enum LivePlayCommandSource
{
    System,
    Human,
    Cpu
}

public enum LivePlayCommandKind
{
    Begin,
    Advance,
    Contact,
    ThrowArrived,
    TagRunner,
    Pause,
    Resume,
    Complete,
    Reset,
    /// <summary>Contact: the sim takes the ball, the gloves, and the seats and plays it out through <see cref="Tick"/>.</summary>
    BeginLive,
    /// <summary>One frame of the live ball with both pads.</summary>
    Tick,
    /// <summary>A steal is armed after a take or a miss: the catcher's throw play.</summary>
    BeginSteal,
    /// <summary>The offense threw an item at the play glove (client verb; the ball's result changes here).</summary>
    ApplyItem,
    /// <summary>The defense smashed the flying item: an out that became a hit goes back to an out.</summary>
    SmashItem
}

/// <summary>
/// Portable input for one live ball. Unity translates pad and scene observations into these
/// commands; CPU and human seats use the same command stream after that translation.
/// </summary>
public sealed record LivePlayCommand(
    LivePlayCommandKind Kind,
    LivePlayCommandSource Source = LivePlayCommandSource.System,
    double DeltaSeconds = 0,
    PlayKind PlayKind = PlayKind.Single,
    bool HasBall = false,
    bool Throwing = false,
    bool CatchMade = false,
    double Dash01 = 0,
    int Bag = 0,
    bool RunnerBeats = false,
    double GloveX = 0,
    double GloveZ = 0,
    Character? Fielder = null,
    PitchCommand? Pitch = null,
    SwingCommand? Swing = null,
    AtBatResult? Hit = null,
    FieldingResult? Field = null,
    FieldingPreview? Preview = null,
    LiveSeats? Seats = null,
    LivePadInput? FieldPad = null,
    LivePadInput? RunPad = null,
    bool EffectInFlight = false,
    string? ItemId = null,
    PlayEvent? StealPitch = null)
{
    public static LivePlayCommand Begin(PlayKind kind, LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.Begin, source, PlayKind: kind);

    public static LivePlayCommand Advance(
        double dt,
        PlayKind kind,
        bool hasBall,
        bool throwing,
        bool catchMade,
        double dash01,
        LivePlayCommandSource source = LivePlayCommandSource.System,
        Character? fielder = null) =>
        new(LivePlayCommandKind.Advance, source, dt, kind, hasBall, throwing, catchMade, dash01, Fielder: fielder);

    public static LivePlayCommand Contact(
        PlayKind kind,
        bool hasBall,
        bool throwing,
        bool catchMade,
        double gloveX,
        double gloveZ,
        double dash01,
        Character? fielder,
        LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.Contact, source, PlayKind: kind, HasBall: hasBall, Throwing: throwing,
            CatchMade: catchMade, Dash01: dash01, GloveX: gloveX, GloveZ: gloveZ, Fielder: fielder);

    public static LivePlayCommand ThrowArrived(
        int bag,
        bool runnerBeats,
        Character? fielder,
        LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.ThrowArrived, source, Bag: bag, RunnerBeats: runnerBeats, Fielder: fielder);

    public static LivePlayCommand TagRunner(
        int fromBag,
        Character? fielder,
        LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.TagRunner, source, Bag: fromBag, Fielder: fielder);

    public static LivePlayCommand Pause(LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.Pause, source);

    public static LivePlayCommand Resume(LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.Resume, source);

    public static LivePlayCommand Complete(
        PitchCommand pitch,
        SwingCommand swing,
        AtBatResult hit,
        FieldingResult field,
        LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.Complete, source, Pitch: pitch, Swing: swing, Hit: hit, Field: field);

    public static LivePlayCommand Reset() => new(LivePlayCommandKind.Reset);

    /// <summary>Fair contact. The sim computes the flight, seats the gloves, and owns the ball until Time.</summary>
    public static LivePlayCommand BeginLive(
        PitchCommand pitch,
        SwingCommand swing,
        AtBatResult hit,
        FieldingPreview preview,
        FieldingResult? cpuField,
        LiveSeats seats,
        double dash01 = 0,
        LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.BeginLive, source, Dash01: dash01, Pitch: pitch, Swing: swing, Hit: hit,
            Field: cpuField, Preview: preview, Seats: seats);

    public static LivePlayCommand Tick(
        double dt,
        LivePadInput? fieldPad = null,
        LivePadInput? runPad = null,
        bool effectInFlight = false,
        LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.Tick, source, dt, FieldPad: fieldPad, RunPad: runPad, EffectInFlight: effectInFlight);

    public static LivePlayCommand BeginSteal(PlayEvent pitch, LiveSeats seats, LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.BeginSteal, source, Seats: seats, StealPitch: pitch);

    public static LivePlayCommand ApplyItem(string itemId, Character? target, LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.ApplyItem, source, Fielder: target, ItemId: itemId);

    public static LivePlayCommand SmashItem(LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.SmashItem, source);
}

/// <summary>A runner as the client and the harness see it this frame (spec §9.1).</summary>
public sealed record RunnerView(
    Character Who,
    int FromBag,
    int Bag,
    double Feet,
    RunnerPhase Phase,
    int DestBag,
    bool Forced,
    double X,
    double Z,
    double OnBagSec,
    bool Held,
    bool StealArmed);

public sealed record LivePlaySnapshot(
    bool Active,
    bool Paused,
    double ElapsedSeconds,
    PlayKind PlayKind,
    bool HasBall,
    bool Throwing,
    bool CatchMade,
    IReadOnlyList<RunnerView> Runners,
    bool IsTime,
    InPlay.ForceState Forces,
    bool ForceRecorded,
    bool TurnedTwo,
    bool BatterOut,
    int Throws,
    int OutsAtOpen,
    string Caption,
    FlyState Fly);

public sealed record LivePlayCommandResult(
    LivePlaySnapshot Snapshot,
    InPlay.GroundThrowStep? Throw = null,
    int? TaggedFromBag = null,
    PlayEvent? CompletedPlay = null,
    bool FlightDone = false);

/// <summary>
/// The last thing the live ball decided, as typed facts. <see cref="LivePlaySystem.Caption"/>
/// is narrated from it; Match composes the play's caption from it and never reads the text.
/// </summary>
public sealed record LiveMoment(InPlay.ThrowVerdict Verdict, int Bag, Character? Fielder, Character? Runner)
{
    public bool NarratesBatterAtFirst => InPlay.NarratesBatterAtFirst(Verdict);

    public string Narrate(string batterName, string defaultFielderName) =>
        InPlay.Narrate(Verdict, Bag, Fielder?.Name ?? defaultFielderName, batterName, Runner?.Name);
}

/// <summary>
/// Owns the mutable state and baseball commands for the ball between contact and Time.
/// It has no Unity dependency: callers supply elapsed time, possession, and glove location,
/// then consume a snapshot and any out/result produced by the command. The runners are
/// <see cref="Match.Runners"/>: bodies this system moves and judges (spec §9).
/// </summary>
public sealed partial class LivePlaySystem
{
    readonly Match _match;
    LivePadInput _prevRun = LivePadInput.Dead;
    bool _catchRecorded;
    bool _aiPending;
    bool _wasHolding;
    bool _wasThrowing;

    internal LivePlaySystem(Match match) => _match = match;

    public bool Active { get; private set; }
    public bool Paused { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public PlayKind PlayKind { get; private set; } = PlayKind.Single;
    public bool HasBall { get; private set; }
    public bool Throwing { get; private set; }
    public bool CatchMade { get; private set; }
    public InPlay.ForceState Forces { get; private set; } = InPlay.ForceState.Empty;
    public bool ForceRecorded { get; private set; }
    public int ForceBag { get; private set; }
    public bool TurnedTwo { get; private set; }
    public bool BatterOut { get; private set; }
    /// <summary>A throw to first arrived after the batter: the batter-runner is on the bag.</summary>
    public bool BatterSafeAtFirst { get; private set; }
    public int Throws { get; private set; }
    public int OutsAtOpen { get; private set; }
    /// <summary>The ball as the runners read it (spec §9.5): in the air they hold, at the catch they tag, at the drop they run.</summary>
    public FlyState Fly { get; private set; }
    /// <summary>The last narrated decision of this live ball, or null when nothing has been decided yet.</summary>
    public LiveMoment? LastMoment { get; private set; }
    /// <summary>Narrated from <see cref="LastMoment"/>, last. Presentation only; no rule reads it.</summary>
    public string Caption => LastMoment?.Narrate(_match.Batter.Name, _match.Pitcher.Name) ?? "";

    /// <summary>The bodies on the path this play, the batter-runner included.</summary>
    public IReadOnlyList<Runner> Runners => _match.Runners;

    public LivePlaySnapshot Snapshot => new(
        Active,
        Paused,
        ElapsedSeconds,
        PlayKind,
        HasBall,
        Throwing,
        CatchMade,
        Runners.Select(View).ToList(),
        IsTime(),
        Forces,
        ForceRecorded,
        TurnedTwo,
        BatterOut,
        Throws,
        OutsAtOpen,
        Caption,
        Fly);

    static RunnerView View(Runner r)
    {
        var (x, z) = r.Position;
        return new RunnerView(r.Who, r.FromBag, r.Bag, r.Feet, r.Phase, r.DestBag, r.Forced, x, z, r.OnBagSec, r.Held, r.StealArmed);
    }

    public LivePlayCommandResult Apply(LivePlayCommand command)
    {
        return command.Kind switch
        {
            LivePlayCommandKind.Begin => Begin(command),
            LivePlayCommandKind.Advance => Advance(command),
            LivePlayCommandKind.Contact => Contact(command),
            LivePlayCommandKind.ThrowArrived => ThrowArrived(command),
            LivePlayCommandKind.TagRunner => TagRunner(command),
            LivePlayCommandKind.Pause => SetPaused(true),
            LivePlayCommandKind.Resume => SetPaused(false),
            LivePlayCommandKind.Complete => Complete(command),
            LivePlayCommandKind.Reset => ResetResult(),
            LivePlayCommandKind.BeginLive => BeginLive(command),
            LivePlayCommandKind.Tick => Tick(command),
            LivePlayCommandKind.BeginSteal => BeginSteal(command),
            LivePlayCommandKind.ApplyItem => ApplyItem(command),
            LivePlayCommandKind.SmashItem => SmashItem(),
            _ => new LivePlayCommandResult(Snapshot)
        };
    }

    LivePlayCommandResult Begin(LivePlayCommand command)
    {
        if (Active) return new LivePlayCommandResult(Snapshot);
        _match.PrepareLivePlay();
        Active = true;
        Paused = _match.Paused;
        ElapsedSeconds = 0;
        PlayKind = command.PlayKind;
        HasBall = false;
        Throwing = false;
        CatchMade = false;
        Forces = InPlay.ForceState.FromOccupancy(
            _match.First is not null, _match.Second is not null, _match.Third is not null);
        ForceRecorded = false;
        ForceBag = 0;
        TurnedTwo = false;
        BatterOut = false;
        BatterSafeAtFirst = false;
        Throws = 0;
        OutsAtOpen = _match.Outs;
        LastMoment = null;
        _catchRecorded = false;
        _wasHolding = false;
        _wasThrowing = false;
        _prevRun = LivePadInput.Dead;
        // Contact: the batter is a body in the box, every runner snapshots its force (§9.1).
        _match.BeginRunners(HomeSet.BatterBodyX(_match.Batter.Bats, _match.BatterContactOffsetX), HomeSet.BatterZ);
        Fly = FlyStateNow(command.PlayKind, command.CatchMade);
        _aiPending = true;
        return new LivePlayCommandResult(Snapshot);
    }

    /// <summary>One frame of the bodies: the scripted path (no flight) and the tick share it.</summary>
    LivePlayCommandResult Advance(LivePlayCommand command)
    {
        if (!Active || Paused || command.DeltaSeconds <= 0)
            return new LivePlayCommandResult(Snapshot);
        PlayKind = command.PlayKind;
        HasBall = command.HasBall;
        Throwing = command.Throwing;
        CatchMade = command.CatchMade;
        ElapsedSeconds += command.DeltaSeconds;
        UpdateFly(FlyStateNow(command.PlayKind, command.CatchMade));
        RecordCatchOut(command.Fielder);
        if (HasBall && !_wasHolding) _aiPending = true;
        if (Throwing && !_wasThrowing) _aiPending = true;
        _wasHolding = HasBall;
        _wasThrowing = Throwing;
        TickRunners(command.DeltaSeconds, command.Dash01);
        return new LivePlayCommandResult(Snapshot);
    }

    /// <summary>What the runners read off the ball this frame (§9.5).</summary>
    FlyState FlyStateNow(PlayKind kind, bool catchMade)
    {
        if (Preview is not null && Path is not null)
        {
            if (Preview.Grounder) return FlyState.None;
            if (HoldsBall) return Call == FairFoulCall.Caught ? FlyState.Caught : FlyState.Dropped;
            return ElapsedSeconds < Hang ? FlyState.InAir : FlyState.Dropped;
        }
        if (kind == PlayKind.FlyOut) return catchMade ? FlyState.Caught : FlyState.InAir;
        return FlyState.None;
    }

    void UpdateFly(FlyState next)
    {
        if (next == Fly) return;
        var was = Fly;
        Fly = next;
        if (next == FlyState.Caught && was != FlyState.Caught)
        {
            // Firmly caught (§9.5, §10.5): a runner off the start bag owes a retouch; one waiting with the send goes.
            foreach (var r in Runners)
            {
                if (!r.Live || r.IsBatter) continue;
                if (r.Bag != r.FromBag || r.Feet > 0) r.MarkLeftEarly();
                else if (r.TagAndGo) r.Send(r.NextBag, human: true);
            }
            _aiPending = true;
        }
        else if (next == FlyState.Dropped)
        {
            foreach (var r in Runners)
                if (r.Live && !r.IsBatter && r.TagAndGo && r.OnBag) r.Send(r.NextBag, human: true);
            _aiPending = true;
        }
    }

    /// <summary>A fly, liner, or pop held in the air is the out (§10.1): recorded once, the moment the glove owns it.</summary>
    void RecordCatchOut(Character? fielder)
    {
        if (_catchRecorded || BatterOut) return;
        if (Fly != FlyState.Caught || PlayKind != PlayKind.FlyOut) return;
        _catchRecorded = true;
        Retire(0, 0, OutType.Catch, fielder ?? PlayFielder());
    }

    void TickRunners(double dt, double dash01)
    {
        var ctx = new RunnerTickContext(
            ElapsedSeconds,
            dash01,
            Fly,
            _match.Outs,
            bag => Forces.At(bag),
            TagThreatAt);
        // Decide, then move (a scripted step is one long frame); a bag touched this frame is read at once.
        Decide(dash01);
        RunnerSystem.Tick(Runners, dt, ctx, _match.Rules);
        foreach (var r in Runners)
            if (r.ArrivedThisTick) _aiPending = true;
        Decide(dash01);
    }

    void Decide(double dash01)
    {
        if (!_aiPending) return;
        _aiPending = false;
        if (Seats.HumanRuns) return;
        RunnerAi.Decide(Runners, AiContext(dash01), bag => Forces.At(bag), _match.Rules);
    }

    /// <summary>A throw armed to the bag, or a glove with the ball close to it: the runner slides in (§9.4).</summary>
    bool TagThreatAt(int bag)
    {
        var at = Diamond.Bag(bag);
        if (Throwing && ThrowBag == bag) return true;
        if (HasBall && !Throwing && Diamond.Dist(GloveX, GloveZ, at.X, at.Z) <= _match.Rules.Running.Bags.SlideThreatFt) return true;
        return false;
    }

    LivePlayCommandResult Contact(LivePlayCommand command)
    {
        if (!Active || Paused) return new LivePlayCommandResult(Snapshot);
        PlayKind = command.PlayKind;
        HasBall = command.HasBall;
        Throwing = command.Throwing;
        CatchMade = command.CatchMade;
        UpdateFly(FlyStateNow(command.PlayKind, command.CatchMade));
        RecordCatchOut(command.Fielder);
        if (!HasBall || Throwing) return new LivePlayCommandResult(Snapshot);

        var force = TryForce(command.GloveX, command.GloveZ, command.Fielder);
        if (force is not null) return force;
        return TryTag(command.GloveX, command.GloveZ, command.Fielder);
    }

    /// <summary>The runner a throw to <paramref name="bag"/> is for: the forced one behind it, else whoever is heading there.</summary>
    Runner? RunnerForBag(int bag)
    {
        if (Forces.At(bag))
        {
            var forced = _match.RunnerAt(bag - 1);
            if (forced is not null && forced.Live && forced.Bag < bag) return forced;
        }
        return RunnerSystem.HeadingTo(Runners, bag);
    }

    /// <summary>Glove with the ball on a force bag ahead of the forced runner, or on the start bag of a runner doubled off (§10.1, §10.5).</summary>
    LivePlayCommandResult? TryForce(double gloveX, double gloveZ, Character? fielder)
    {
        var bags = _match.Rules.Running.Bags;
        for (var bag = 1; bag <= 4; bag++)
        {
            if (!Forces.At(bag)) continue;
            var runner = _match.RunnerAt(bag - 1);
            if (runner is null || !runner.Live || runner.Bag >= bag) continue;
            if (Fly == FlyState.InAir) continue;
            if (!InPlay.OnThisBag(bag, gloveX, gloveZ, bags.OccupyRadiusFt)) continue;
            return ApplyThrow(bag, runnerBeats: false, fielder);
        }
        foreach (var runner in Runners)
        {
            if (!runner.Live || !runner.LeftEarly || _match.Outs >= 3) continue;
            if (!InPlay.OnThisBag(runner.FromBag, gloveX, gloveZ, bags.OccupyRadiusFt)) continue;
            if (!Retire(runner.FromBag, runner.FromBag, OutType.Force, fielder)) continue;
            LastMoment = new LiveMoment(InPlay.ThrowVerdict.DoubledOff, runner.FromBag, fielder, runner.Who);
            return new LivePlayCommandResult(Snapshot, TaggedFromBag: runner.FromBag);
        }
        return null;
    }

    /// <summary>Glove with the ball touching a body off a bag (§10.3). Home is not a bag for the batter leaving the box.</summary>
    LivePlayCommandResult TryTag(double gloveX, double gloveZ, Character? fielder)
    {
        var bags = _match.Rules.Running.Bags;
        foreach (var runner in Runners.OrderBy(r => r.FromBag == 0 ? -1 : r.FromBag))
        {
            if (!runner.Live) continue;
            var (x, z) = runner.Position;
            // The plate is not a bag for the batter leaving the box (§10.3).
            var onBag = !(runner.IsBatter && runner.Bag == 0) && InPlay.OccupyingBag(x, z, bags.TagSafeRadiusFt);
            if (InPlay.Touches(true, false, gloveX, gloveZ, x, z, onBag, _match.Rules, runner.Sliding)
                && ApplyTag(runner.FromBag, fielder))
                return new LivePlayCommandResult(Snapshot, TaggedFromBag: runner.FromBag);
        }
        return new LivePlayCommandResult(Snapshot);
    }

    LivePlayCommandResult ThrowArrived(LivePlayCommand command)
    {
        if (!Active || Paused) return new LivePlayCommandResult(Snapshot);
        HasBall = true;
        Throwing = false;
        CatchMade = true;
        return ApplyThrow(command.Bag, command.RunnerBeats, command.Fielder, scripted: true);
    }

    LivePlayCommandResult TagRunner(LivePlayCommand command)
    {
        if (!Active || Paused) return new LivePlayCommandResult(Snapshot);
        var tagged = ApplyTag(command.Bag, command.Fielder);
        return new LivePlayCommandResult(Snapshot, TaggedFromBag: tagged ? command.Bag : null);
    }

    /// <summary>
    /// The ball is at <paramref name="bag"/>; the verdict is <see cref="InPlay.ThrowToBag"/> over the
    /// runner it is for. A scripted verdict (the harness saying who won) is about the runner on the
    /// bag behind when nobody's body is heading there.
    /// </summary>
    LivePlayCommandResult ApplyThrow(int bag, bool runnerBeats, Character? fielder, bool scripted = false)
    {
        var target = RunnerForBag(bag);
        if (target is null && scripted)
            target = Runners.FirstOrDefault(r => r.Live && r.Bag == bag - 1 && (bag == 1 ? r.IsBatter : !r.IsBatter));
        var standing = Runners.FirstOrDefault(r => r.Live && r.IsOn(bag));
        var present = target is not null || standing is not null;
        if (target is null && standing is not null) runnerBeats = true;
        var step = InPlay.ThrowToBag(
            bag, Forces, present, runnerBeats, _match.Outs, ForceRecorded,
            fielder?.Name ?? "", _match.Batter.Name);
        Throws++;
        // A silent verdict (nothing decided, or the batter simply safe at first) keeps the last narrated one.
        if (step.Verdict is not (InPlay.ThrowVerdict.None or InPlay.ThrowVerdict.BatterBeat))
            LastMoment = new LiveMoment(step.Verdict, step.Bag, fielder, target?.Who);
        if (step.BatterSafe) BatterSafeAtFirst = true;
        if (step.Force)
        {
            ForceRecorded = true;
            ForceBag = step.Bag;
        }
        if (step.TurnedTwo) TurnedTwo = true;
        if (step.Out)
            Retire(target?.FromBag ?? InPlay.ForceState.FromBag(step.Bag), step.Bag, step.OutType, fielder);
        else if (runnerBeats && target is not null && step.Verdict is not InPlay.ThrowVerdict.None)
            target.Arrive(bag, ElapsedSeconds); // beat the throw: the bag is theirs
        return new LivePlayCommandResult(Snapshot, step);
    }

    bool ApplyTag(int fromBag, Character? fielder)
    {
        if (_match.Outs >= 3) return false;
        var who = _match.RunnerAt(fromBag)?.Who;
        if (who is null || !Retire(fromBag, 0, OutType.Tag, fielder)) return false;
        LastMoment = new LiveMoment(InPlay.ThrowVerdict.TagRunner, 0, fielder, who);
        return true;
    }

    bool Retire(int fromBag, int atBag, OutType type, Character? fielder)
    {
        if (fromBag == 0)
        {
            if (BatterOut) return false;
            BatterOut = true;
        }
        if (!_match.RetireLiveRunner(fromBag, atBag, type, fielder)) return false;
        Forces = Forces.AfterOutAt(fromBag + 1);
        _aiPending = true;
        return true;
    }

    LivePlayCommandResult Complete(LivePlayCommand command)
    {
        if (!Active || Paused || command.Pitch is null || command.Swing is null || command.Hit is null || command.Field is null)
            return new LivePlayCommandResult(Snapshot);
        var play = _match.FinishAtBat(command.Pitch, command.Swing, command.Hit, command.Field);
        return new LivePlayCommandResult(Snapshot, CompletedPlay: play);
    }

    LivePlayCommandResult SetPaused(bool paused)
    {
        if (!Active) return new LivePlayCommandResult(Snapshot);
        Paused = paused;
        _match.SetPausedFromLive(paused);
        return new LivePlayCommandResult(Snapshot);
    }

    internal void SetPausedFromMatch(bool paused) => Paused = paused && Active;

    /// <summary>
    /// Time (§10.6): three outs, or an infielder holding the ball unthrown with every live runner
    /// settled on a bag. Nobody left to play on (every runner out or home) is Time wherever the
    /// ball is; so is a ball lying at rest that nobody picked up once every body has settled.
    /// </summary>
    bool IsTime()
    {
        if (!Active || Paused) return false;
        if (_match.Outs >= 3) return true;
        var nobodyLive = Runners.All(r => !r.Live);
        if (nobodyLive && !Throwing) return true;
        var resting = Path is not null && !HasBall && !Throwing
                      && ElapsedSeconds >= Rest + _match.Rules.Flight.DeadBall.RestHoldSec;
        if (resting)
            return InPlay.Time(true, false, _match.Outs, true, Runners, _match.Rules);
        var heldInInfield = Path is null || InPlay.HeldInInfield(GloveX, GloveZ, _match.Rules);
        return InPlay.Time(HasBall, Throwing, _match.Outs, heldInInfield, Runners, _match.Rules);
    }

    LivePlayCommandResult ResetResult()
    {
        Reset();
        return new LivePlayCommandResult(Snapshot);
    }

    internal void Reset()
    {
        ResetField();
        Active = false;
        Paused = false;
        ElapsedSeconds = 0;
        PlayKind = PlayKind.Single;
        HasBall = false;
        Throwing = false;
        CatchMade = false;
        Forces = InPlay.ForceState.Empty;
        ForceRecorded = false;
        ForceBag = 0;
        TurnedTwo = false;
        BatterOut = false;
        BatterSafeAtFirst = false;
        Throws = 0;
        OutsAtOpen = 0;
        LastMoment = null;
        Fly = FlyState.None;
        _catchRecorded = false;
        _aiPending = false;
        _wasHolding = false;
        _wasThrowing = false;
        _prevRun = LivePadInput.Dead;
    }
}
