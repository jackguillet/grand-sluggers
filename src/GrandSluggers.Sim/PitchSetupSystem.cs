namespace GrandSluggers.Sim;

public enum PitchSetupPhase { Set, Windup, Flight }

/// <summary>
/// One pre-contact clock for the pitcher and existing runner bodies. A client reports the
/// hold edge and actual ball release; it never places a runner or decides a balk itself.
/// </summary>
public sealed class PitchSetupSystem
{
    readonly Match _match;
    double _queuedAt = double.NegativeInfinity;
    int _catcherBag;
    bool _explicitTarget;
    LivePadInput _previousRun = LivePadInput.Dead;
    public PitchSetupPhase Phase { get; private set; }
    public bool Committed => Phase != PitchSetupPhase.Set;
    public double ElapsedSeconds { get; private set; }
    public int CatcherBag => _catcherBag;

    internal PitchSetupSystem(Match match) => _match = match;

    public bool BeginCharge()
    {
        if (_match.Over || _match.Paused || _match.LivePlay.Active || Committed) return false;
        Phase = PitchSetupPhase.Windup;
        return true;
    }

    /// <summary>The ball has actually left the hand; holding at MAX never calls this by itself.</summary>
    public bool ReleaseBall()
    {
        if (_match.Over || _match.Paused || _match.LivePlay.Active || Phase == PitchSetupPhase.Flight) return false;
        Phase = PitchSetupPhase.Flight;
        return true;
    }

    /// <summary>Defense may choose before receiving. A short buffered press starts the transfer at possession.</summary>
    public void CatcherInput(LivePadInput pad)
    {
        if (_match.Paused || _match.Over || _match.LivePlay.Active) return;
        _explicitTarget = pad.ExplicitTarget;
        var bag = pad.KeysBag > 0 ? pad.KeysBag : pad.StickBag > 0 ? pad.StickBag : pad.ArrowBag;
        if (bag is >= 1 and <= 4) _catcherBag = bag;
        if (pad.Cancel) _queuedAt = double.NegativeInfinity;
        else if (pad.SouthDown && (!pad.ExplicitTarget || _catcherBag > 0)) _queuedAt = ElapsedSeconds;
    }

    internal LivePadInput? TakeCatcherInput()
    {
        var buffered = ElapsedSeconds - _queuedAt <= _match.Rules.Fielding.Throw.RelayBufferSec;
        var result = _catcherBag > 0 || buffered
            ? new LivePadInput(KeysBag: _catcherBag, SouthDown: buffered, ExplicitTarget: _explicitTarget) : null;
        _queuedAt = double.NegativeInfinity;
        _catcherBag = 0;
        return result;
    }

    public void RunnerInput(LivePadInput pad)
    {
        if (_match.Paused || _match.Over || _match.LivePlay.Active) return;
        RunnerCommands.Apply(_match, pad, ref _previousRun);
    }

    public PlayEvent? Advance(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        if (seconds == 0 || _match.Paused || _match.Over || _match.LivePlay.Active) return null;
        ElapsedSeconds += seconds;
        var mul = Phase == PitchSetupPhase.Flight ? _match.Rules.Running.Steal.AirSpeedMul : 1;
        RunnerSystem.Tick(_match.Runners, seconds,
            new RunnerTickContext(ElapsedSeconds, 0, FlyState.None, _match.Outs,
                _ => false, _ => false, GroundZones.Of(_match.Park, _match.Rules), _ => mul), _match.Rules);
        // A completed departure before the pitch is a stolen base now. During flight the
        // pitch's foul/walk/contact disposition owns it; preserve the bodies until that read.
        return Phase == PitchSetupPhase.Flight ? null : _match.SettleSetupArrivals();
    }

    internal void PitchResolved()
    {
        foreach (var runner in _match.Runners) runner.RebaseClock(ElapsedSeconds);
        Phase = PitchSetupPhase.Set;
        // Preserve buffer age across the transition to catcher possession.
        _queuedAt -= ElapsedSeconds;
        ElapsedSeconds = 0;
    }

    internal void Reset()
    {
        _match.ControllerRunners.Reset();
        Phase = PitchSetupPhase.Set;
        ElapsedSeconds = 0;
        _queuedAt = double.NegativeInfinity;
        _catcherBag = 0;
        _previousRun = LivePadInput.Dead;
    }
}
