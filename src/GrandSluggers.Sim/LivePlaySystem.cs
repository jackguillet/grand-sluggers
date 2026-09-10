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
    Reset
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
    FieldingResult? Field = null)
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
        LivePlayCommandSource source = LivePlayCommandSource.System) =>
        new(LivePlayCommandKind.Advance, source, dt, kind, hasBall, throwing, catchMade, dash01);

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
}

public sealed record LivePlaySnapshot(
    bool Active,
    bool Paused,
    double ElapsedSeconds,
    PlayKind PlayKind,
    bool HasBall,
    bool Throwing,
    bool CatchMade,
    InPlay.Occupy Batter,
    InPlay.Occupy? First,
    InPlay.Occupy? Second,
    InPlay.Occupy? Third,
    bool IsTime,
    InPlay.ForceState Forces,
    bool ForceRecorded,
    bool TurnedTwo,
    bool BatterOut,
    int Throws,
    int OutsAtOpen,
    string Caption);

public sealed record LivePlayCommandResult(
    LivePlaySnapshot Snapshot,
    InPlay.GroundThrowStep? Throw = null,
    int? TaggedFromBag = null,
    PlayEvent? CompletedPlay = null);

/// <summary>
/// Owns the mutable state and baseball commands for the ball between contact and Time.
/// It has no Unity dependency: callers supply elapsed time, possession, and glove location,
/// then consume a snapshot and any out/result produced by the command.
/// </summary>
public sealed class LivePlaySystem
{
    readonly Match _match;
    InPlay.Occupy _batter;
    InPlay.Occupy _first;
    InPlay.Occupy _second;
    InPlay.Occupy _third;

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
    public int Throws { get; private set; }
    public int OutsAtOpen { get; private set; }
    public string Caption { get; private set; } = "";

    public LivePlaySnapshot Snapshot => new(
        Active,
        Paused,
        ElapsedSeconds,
        PlayKind,
        HasBall,
        Throwing,
        CatchMade,
        _batter,
        _match.First is not null ? _first : null,
        _match.Second is not null ? _second : null,
        _match.Third is not null ? _third : null,
        IsTime(),
        Forces,
        ForceRecorded,
        TurnedTwo,
        BatterOut,
        Throws,
        OutsAtOpen,
        Caption);

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
        _batter = default;
        _first = default;
        _second = default;
        _third = default;
        Forces = InPlay.ForceState.FromOccupancy(
            _match.First is not null, _match.Second is not null, _match.Third is not null);
        ForceRecorded = false;
        ForceBag = 0;
        TurnedTwo = false;
        BatterOut = false;
        Throws = 0;
        OutsAtOpen = _match.Outs;
        Caption = "";
        return new LivePlayCommandResult(Snapshot);
    }

    LivePlayCommandResult Advance(LivePlayCommand command)
    {
        if (!Active || Paused || command.DeltaSeconds <= 0)
            return new LivePlayCommandResult(Snapshot);
        PlayKind = command.PlayKind;
        HasBall = command.HasBall;
        Throwing = command.Throwing;
        CatchMade = command.CatchMade;
        ElapsedSeconds += command.DeltaSeconds;
        TickOccupancy(command.DeltaSeconds, command.Dash01);
        return new LivePlayCommandResult(Snapshot);
    }

    LivePlayCommandResult Contact(LivePlayCommand command)
    {
        if (!Active || Paused) return new LivePlayCommandResult(Snapshot);
        PlayKind = command.PlayKind;
        HasBall = command.HasBall;
        Throwing = command.Throwing;
        CatchMade = command.CatchMade;
        if (!HasBall || Throwing) return new LivePlayCommandResult(Snapshot);

        var force = TryForce(command);
        if (force is not null) return force;
        return TryTag(command);
    }

    LivePlayCommandResult? TryForce(LivePlayCommand command)
    {
        if (InPlay.LiveBatter(PlayKind, BatterOut) && Forces.At(1))
        {
            var dest = InPlay.BatterDestBag(PlayKind);
            var feet = InPlay.RunFeet(ElapsedSeconds, _match.Batter, command.Dash01);
            var (x, z) = InPlay.AlongBases(feet, dest,
                HomeSet.BatterBodyX(_match.Batter.Bats, _match.BatterContactOffsetX), HomeSet.BatterZ);
            if (InPlay.ForceOnBag(true, 1, true, false, command.GloveX, command.GloveZ, x, z))
                return ApplyThrow(1, runnerBeats: false, command.Fielder);
        }

        for (var bag = 2; bag <= 4; bag++)
        {
            if (!Forces.At(bag)) continue;
            var from = bag - 1;
            var who = _match.RunnerAt(from)?.Who;
            if (who is null) continue;
            var dest = InPlay.OccupiedDestBag(from, PlayKind, _match.SendAll, CatchMade);
            var feet = InPlay.RunFeet(ElapsedSeconds, who);
            var (x, z) = InPlay.TowardBag(from, dest, feet);
            if (InPlay.ForceOnBag(true, bag, true, false, command.GloveX, command.GloveZ, x, z))
                return ApplyThrow(bag, runnerBeats: false, command.Fielder);
        }
        return null;
    }

    LivePlayCommandResult TryTag(LivePlayCommand command)
    {
        if (InPlay.LiveBatter(PlayKind, BatterOut))
        {
            var dest = InPlay.BatterDestBag(PlayKind);
            var feet = InPlay.RunFeet(ElapsedSeconds, _match.Batter, command.Dash01);
            var (x, z) = InPlay.AlongBases(feet, dest,
                HomeSet.BatterBodyX(_match.Batter.Bats, _match.BatterContactOffsetX), HomeSet.BatterZ);
            if (InPlay.Touches(true, false, command.GloveX, command.GloveZ, x, z)
                && ApplyTag(0, command.Fielder))
                return new LivePlayCommandResult(Snapshot, TaggedFromBag: 0);
        }

        for (var bag = 1; bag <= 3; bag++)
        {
            var who = _match.RunnerAt(bag)?.Who;
            if (who is null) continue;
            var dest = InPlay.OccupiedDestBag(bag, PlayKind, _match.SendAll, CatchMade);
            var feet = InPlay.RunFeet(ElapsedSeconds, who);
            var (x, z) = InPlay.TowardBag(bag, dest, feet);
            if (InPlay.Touches(true, false, command.GloveX, command.GloveZ, x, z)
                && ApplyTag(bag, command.Fielder))
                return new LivePlayCommandResult(Snapshot, TaggedFromBag: bag);
        }
        return new LivePlayCommandResult(Snapshot);
    }

    LivePlayCommandResult ThrowArrived(LivePlayCommand command)
    {
        if (!Active || Paused) return new LivePlayCommandResult(Snapshot);
        HasBall = true;
        Throwing = false;
        CatchMade = true;
        return ApplyThrow(command.Bag, command.RunnerBeats, command.Fielder);
    }

    LivePlayCommandResult TagRunner(LivePlayCommand command)
    {
        if (!Active || Paused) return new LivePlayCommandResult(Snapshot);
        var tagged = ApplyTag(command.Bag, command.Fielder);
        return new LivePlayCommandResult(Snapshot, TaggedFromBag: tagged ? command.Bag : null);
    }

    LivePlayCommandResult ApplyThrow(int bag, bool runnerBeats, Character? fielder)
    {
        var present = bag switch
        {
            1 => !BatterOut,
            2 => _match.First is not null,
            3 => _match.Second is not null,
            4 => _match.Third is not null,
            _ => false
        };
        var step = InPlay.ThrowToBag(
            bag, Forces, present, runnerBeats, _match.Outs, ForceRecorded,
            fielder?.Name ?? "", _match.Batter.Name);
        Throws++;
        if (!string.IsNullOrEmpty(step.Caption)) Caption = step.Caption;
        if (step.Force)
        {
            ForceRecorded = true;
            ForceBag = step.Bag;
        }
        if (step.TurnedTwo) TurnedTwo = true;
        if (step.Out) Retire(InPlay.ForceState.FromBag(step.Bag), fielder);
        return new LivePlayCommandResult(Snapshot, step);
    }

    bool ApplyTag(int fromBag, Character? fielder)
    {
        if (_match.Outs >= 3) return false;
        var who = fromBag == 0 ? _match.Batter : _match.RunnerAt(fromBag)?.Who;
        if (who is null || !Retire(fromBag, fielder)) return false;
        Caption = $"{fielder?.Name ?? _match.Pitcher.Name} tags {who.Name}.";
        return true;
    }

    bool Retire(int fromBag, Character? fielder)
    {
        if (fromBag == 0)
        {
            if (BatterOut) return false;
            BatterOut = true;
        }
        if (!_match.RetireLiveRunner(fromBag, fielder)) return false;
        Forces = Forces.AfterOutAt(fromBag + 1);
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

    void TickOccupancy(double dt, double dash01)
    {
        var dest = InPlay.BatterDestBag(PlayKind);
        var feet = InPlay.RunFeet(ElapsedSeconds, _match.Batter, dash01);
        var startX = HomeSet.BatterBodyX(_match.Batter.Bats, _match.BatterContactOffsetX);
        var (bx, bz) = dest > 0
            ? InPlay.AlongBases(feet, dest, startX, HomeSet.BatterZ)
            : (startX, HomeSet.BatterZ);
        _batter = InPlay.TickOccupy(dest > 0 && InPlay.OnThisBag(dest, bx, bz), _batter.Sec, dt);
        _first = TickRunner(1, _match.First, _first, dt);
        _second = TickRunner(2, _match.Second, _second, dt);
        _third = TickRunner(3, _match.Third, _third, dt);
    }

    InPlay.Occupy TickRunner(int fromBag, Character? who, InPlay.Occupy occupy, double dt)
    {
        if (who is null) return default;
        var dest = InPlay.OccupiedDestBag(fromBag, PlayKind, _match.SendAll, CatchMade);
        var feet = InPlay.RunFeet(ElapsedSeconds, who);
        var (x, z) = InPlay.TowardBag(fromBag, dest, feet);
        return InPlay.TickOccupy(InPlay.OnThisBag(dest, x, z), occupy.Sec, dt);
    }

    bool IsTime()
    {
        if (!Active || Paused) return false;
        var batterOut = !InPlay.LiveBatter(PlayKind, BatterOut);
        return InPlay.Time(
            HasBall,
            Throwing,
            _match.Outs,
            _batter,
            _match.First is not null ? _first : null,
            _match.Second is not null ? _second : null,
            _match.Third is not null ? _third : null,
            batterOut);
    }

    LivePlayCommandResult ResetResult()
    {
        Reset();
        return new LivePlayCommandResult(Snapshot);
    }

    internal void Reset()
    {
        Active = false;
        Paused = false;
        ElapsedSeconds = 0;
        PlayKind = PlayKind.Single;
        HasBall = false;
        Throwing = false;
        CatchMade = false;
        _batter = default;
        _first = default;
        _second = default;
        _third = default;
        Forces = InPlay.ForceState.Empty;
        ForceRecorded = false;
        ForceBag = 0;
        TurnedTwo = false;
        BatterOut = false;
        Throws = 0;
        OutsAtOpen = 0;
        Caption = "";
    }
}
